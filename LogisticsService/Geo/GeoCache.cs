
namespace LogisticsService.Geo
{
    using LogisticsService.API;
    using LogisticsService.Couriers;
    using LogisticsService.Log;
    using LogisticsService.ServiceParameters;
    using System;
    using System.Collections.Generic;
    using System.Device.Location;
    using System.Drawing;

    /// <summary>
    /// Координатный кэш
    /// </summary>
    public class GeoCache
    {
        //#region vechicle ID constants

        ///// <summary>
        ///// ID передвижения с помощью автомобиля
        ///// </summary>
        //private const string DELIVERY_BY_CAR = "driving";

        ///// <summary>
        ///// ID передвижения с помощью велосипеда
        ///// </summary>
        //private const string DELIVERY_BY_BICYCLE = "cycling";

        ///// <summary>
        /////  ID пешего передвижения
        ///// </summary>
        //private const string DELIVERY_BY_ONFOOT = "walking";

        ///// <summary>
        ///// Названия методов отгрузки
        ///// </summary>
        //private string[] deliveryMethodName;

        //#endregion vechicle ID constants

        /// <summary>
        /// Способы доставки
        /// </summary>
        private Dictionary<CourierVehicleType, GeoDeliveryMethod> deliveryMethods;

        /// <summary>
        /// Количество различных типов перемещений в Yandex-запросах
        /// </summary>
        private int yandexTypeCount;

        /// <summary>
        /// Экземпляр для получения хэша координат
        /// </summary>
        private GeoCoordinate coordinateHash;

        /// <summary>
        /// Ёмкость кэша
        /// (число хранимых попарных расстояний и времени движения)
        /// </summary>
        public int HashCapacity { get; private set; }

        /// <summary>
        /// Преобразование (hash1, hash2) в индекс элемента кэша
        /// </summary>
        private Dictionary<ulong, int> cacheItemIndex;

        /// <summary>
        /// Элементы кэша
        /// </summary>
        private CacheItem[] cacheItems;
        private int cacheItemCount;

        /// <summary>
        /// Число элементов, которые помещались в кэш
        /// </summary>
        public int CacheItemCount => cacheItemCount;

        /// <summary>
        /// Параметрический конструктор класса GeoCache
        /// </summary>
        /// <param name="config">Параметры программы</param>
        /// <param name="hashCapacity">Ёмкость кэша (число хранимых попарных расстояний и времени движения)</param>
        public GeoCache(ServiceConfig config, int hashCapacity)
        {
            // 1. Инициализация
            HashCapacity = hashCapacity;
            cacheItems = new CacheItem[hashCapacity];
            cacheItemCount = 0;
            cacheItemIndex = new Dictionary<ulong, int>(hashCapacity);
            coordinateHash = new GeoCoordinate();
            //deliveryMethodName = new string[] { DELIVERY_BY_CAR, DELIVERY_BY_BICYCLE, DELIVERY_BY_ONFOOT };

            // 2. Построение словаря способов доставки
            yandexTypeCount = 0;
            deliveryMethods = new Dictionary<CourierVehicleType, GeoDeliveryMethod>(64);
            Dictionary<string, int> yandexTypeToIndex = new Dictionary<string, int>(16);
            int deliveryIndex = 0;

            foreach (YandexVehicleMapper item in config.yandex_vehicle_mapper)
            {
                if (item.VechicleTypes != null && item.VechicleTypes.Length > 0)
                {
                    if (!yandexTypeToIndex.TryGetValue(item.YandexType, out deliveryIndex))
                    {
                        deliveryIndex = yandexTypeCount++;
                        yandexTypeToIndex.Add(item.YandexType, deliveryIndex);
                    }

                    foreach (CourierVehicleType vehicleType in item.VechicleTypes)
                    {
                        if (!deliveryMethods.ContainsKey(vehicleType))
                        {
                            deliveryMethods.Add(vehicleType, new GeoDeliveryMethod(deliveryIndex, vehicleType, item.YandexType));
                        }
                    }
                }
            }

            if (yandexTypeCount <= 0)
                yandexTypeCount = 1;
        }

        ///// <summary>
        ///// Построение хэш-знчения для координат
        ///// </summary>
        ///// <param name="latitude">Широта</param>
        ///// <param name="longitude">Долгота</param>
        ///// <returns>Хэш-значение координат</returns>
        //public int GetHashCode(double latitude, double longitude)
        //{
        //    coordinateHash.Latitude = latitude;
        //    coordinateHash.Longitude = longitude;
        //    return coordinateHash.GetHashCode();
        //}

        /// <summary>
        /// Запрос неизвестных расстояний и времени движения
        /// между заданными парами точек
        /// </summary>
        /// <param name="pointHash">Хэш точки</param>
        /// <param name="vehicleType">Тип курьера или такси</param>
        /// <returns>0 - запрошенные данные получены; иначе - запрошенные данные не получены</returns>
        public int PutLocationInfo_old(double[] latitude, double[] longitude, CourierVehicleType vehicleType)
        {
            // 1. Инициализация 
            int rc = 1;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (latitude == null || latitude.Length <= 0)
                    return rc;
                if (longitude == null || longitude.Length !=  latitude.Length)
                    return rc;

                // 3. Запрашиваем индекс способа доставки
                rc = 3;
                int deliveryMethodIndex = GetDeliveryMethodIndex(vehicleType);
                if (deliveryMethodIndex < 0)
                    return rc;

                // 4. Находим хэш-значения координат
                rc = 4;
                int n = latitude.Length;
                int[] pointHash = new int[n];

                for (int i = 0; i < n; i++)
                {
                    coordinateHash.Latitude = latitude[i];
                    coordinateHash.Longitude = longitude[i];
                    pointHash[i] = coordinateHash.GetHashCode();
                }

                // 5. Выделяем пары, для которых расстояния неизвестны
                rc = 5;
                int sourceCount = 0;
                int destinationCount = 0;
                bool[] isSource = new bool[n];
                bool[] isDestination = new bool[n];
                int[] sourceLocationIndex = new int[n];
                int[] destinationLocationIndex = new int[n];

                for (int i = 0; i < n; i++)
                {
                    int hash1 = pointHash[i];

                    for (int j = i + 1; j < n; j++)
                    {
                        int hash2 = pointHash[j];

                        bool isEmpty = true;

                        ulong key = GetKey(hash1, hash2);
                        int index;
                        if (cacheItemIndex.TryGetValue(key, out index))
                        {
                            isEmpty = cacheItems[index].IsEmpty(deliveryMethodIndex);
                        }
                        if (!isEmpty)
                        {
                            key = GetKey(hash2, hash1);
                            if (cacheItemIndex.TryGetValue(key, out index))
                            {
                                isEmpty = cacheItems[index].IsEmpty(deliveryMethodIndex);
                            }
                        }

                        if (isEmpty)
                        {
                            if (!isSource[i])
                            {
                                sourceLocationIndex[sourceCount++] = i;
                                isSource[i] = true;
                            }
                            if (!isDestination[j])
                            {
                                destinationLocationIndex[destinationCount++] = j;
                                isDestination[j] = true;
                            }
                        }
                    }
                }

                // 6. Если расстояния между всеми парами известны
                rc = 6;
                if (sourceCount <= 0)
                    return rc = 0;

                // 7. Запрашиваем расстояния и время движения, которые неизвестны в прямом направлении
                rc = 7;
                GetShippingInfo.ShippingInfoRequestEx requestData = new GetShippingInfo.ShippingInfoRequestEx();
                //string deliveryMethod = deliveryMethodName[deliveryMethodIndex];
                string deliveryMethod = VehicleTypeToDeliveryMethod(vehicleType);
                requestData.modes = new string[] { deliveryMethod };
                double[][] source_points = new double[sourceCount][];
                double[][] destination_points = new double[destinationCount][];

                for (int i = 0; i < sourceCount; i++)
                {
                    int index = sourceLocationIndex[i];
                    source_points[i] = new double[] { latitude[index], longitude[index] };
                }

                for (int i = 0; i < destinationCount; i++)
                {
                    int index = destinationLocationIndex[i];
                    destination_points[i] = new double[] { latitude[index], longitude[index] };
                }

                requestData.origins = source_points;
                requestData.destinations = destination_points;

                GetShippingInfo.ShippingInfoResponse responseData;
                int rc1 = GetShippingInfo.GetInfo(requestData, out responseData);
                if (rc1 != 0)
                {
                    //Helper.WriteErrorToLog($"LocationManager1.PutLocationInfo deliveryMethod = {deliveryMethod}, origin_count = {sourceCount}, destination_count = {destinationCount}, rc = {rc1}");
                    Logger.WriteToLog(string.Format(MessagePatterns.GEO_CACHE_PUT_INFO_ERROR, deliveryMethod, sourceCount, destinationCount, rc1));
                    return rc = 1000 * rc + rc1;
                }

                // 8. Сохраняем полученные данные в кэше
                rc = 8;
                GetShippingInfo.PointsInfo[][] data = null;
                if (responseData.driving != null)
                {
                    data = responseData.driving;
                }
                else if (responseData.cycling != null)
                {
                    data = responseData.cycling;
                }
                else if (responseData.walking != null)
                {
                    data = responseData.walking;
                }

                if (data == null)
                    return rc;

                for (int i = 0; i < sourceCount; i++)
                {
                    int i1 = sourceLocationIndex[i];
                    int hash1 = pointHash[i1];
                    GetShippingInfo.PointsInfo[] postInfoRow = data[i];

                    for (int j = 0; j < destinationCount; j++)
                    {
                        int j1 = destinationLocationIndex[j];
                        if (j1 != i1)
                        {
                            int hash2 = pointHash[j1];
                            SaveCacheItem(hash1, hash2, new Point(postInfoRow[j].distance, postInfoRow[j].duration), deliveryMethodIndex);
                            //ulong s =(ulong) (pointHash[2]);
                            //byte[] b8 = new byte[8];
                            //System.BitConverter.GetBytes(pointHash[2]).CopyTo(b8, 4);
                            //System.BitConverter.GetBytes(pointHash[2]).CopyTo(b8, 0);
                            //ulong xx = System.BitConverter.ToUInt64(b8, 0);
                            //ulong zz = GetKey(pointHash[2], pointHash[2]);
                        }
                    }
                }

                // 9. Запрашиваем расстояния и время движения, которые неизвестны в обратном направлении
                rc = 9;
                requestData.origins = destination_points;
                requestData.destinations = source_points;

                rc1 = GetShippingInfo.GetInfo(requestData, out responseData);
                if (rc1 != 0)
                {
                    //Helper.WriteErrorToLog($"LocationManager1.PutLocationInfo deliveryMethod = {deliveryMethod}, origin_count = {destinationCount}, destination_count = {sourceCount}, rc = {rc1}");
                    Logger.WriteToLog(string.Format(MessagePatterns.GEO_CACHE_PUT_INFO_ERROR, deliveryMethod, destinationCount, sourceCount, rc1));

                    return rc = 1000 * rc + rc1;
                }

                // 10. Сохраняем результат destination ---> source
                rc = 10;
                if (responseData.driving != null)
                {
                    data = responseData.driving;
                }
                else if (responseData.cycling != null)
                {
                    data = responseData.cycling;
                }
                else if (responseData.walking != null)
                {
                    data = responseData.walking;
                }

                if (data == null)
                    return rc;

                for (int i = 0; i < destinationCount; i++)
                {
                    int i1 = destinationLocationIndex[i];
                    int hash1 = pointHash[i1];
                    GetShippingInfo.PointsInfo[] postInfoRow = data[i];

                    for (int j = 0; j < sourceCount; j++)
                    {
                        int j1 = sourceLocationIndex[j];
                        if (j1 != i1)
                        {
                            int hash2 = pointHash[j1];
                            SaveCacheItem(hash1, hash2, new Point(postInfoRow[j].distance, postInfoRow[j].duration), deliveryMethodIndex);
                        }
                    }
                }

                // 11. Выход - Ok
                rc = 0;
                return rc;
            }
            catch
            {
                return rc;
            }
        }

        /// <summary>
        /// Запрос неизвестных расстояний и времени движения
        /// между заданными парами точек
        /// </summary>
        /// <param name="pointHash">Хэш точки</param>
        /// <param name="vehicleType">Тип курьера или такси</param>
        /// <returns>0 - запрошенные данные получены; иначе - запрошенные данные не получены</returns>
        public int PutLocationInfo(double[] latitude, double[] longitude, CourierVehicleType vehicleType)
        {
            // 1. Инициализация 
            int rc = 1;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (latitude == null || latitude.Length <= 0)
                    return rc;
                if (longitude == null || longitude.Length !=  latitude.Length)
                    return rc;

                // 3. Запрашиваем индекс способа доставки
                rc = 3;
                int deliveryMethodIndex = GetDeliveryMethodIndex(vehicleType);
                if (deliveryMethodIndex < 0)
                    return rc;

                // 4. Находим хэш-значения координат
                rc = 4;
                int n = latitude.Length;
                int[] pointHash = new int[n];

                for (int i = 0; i < n; i++)
                {
                    coordinateHash.Latitude = latitude[i];
                    coordinateHash.Longitude = longitude[i];
                    pointHash[i] = coordinateHash.GetHashCode();
                }

                // 5. Выделяем пары, для которых расстояния неизвестны в прямом (i < j)
                //    и обратном (j > i) направлениях
                rc = 5;
                bool[] source1 = new bool[n];
                bool[] dest1 = new bool[n];
                bool yes1 = false;
                bool[] source2 = new bool[n];
                bool[] dest2 = new bool[n];
                bool yes2 = false;

                for (int i = 0; i < n; i++)
                {
                    int hash1 = pointHash[i];

                    for (int j = i + 1; j < n; j++)
                    {
                        int hash2 = pointHash[j];

                        // i < j
                        ulong key = GetKey(hash1, hash2);
                        int index;
                        bool isEmpty = true;
                        if (cacheItemIndex.TryGetValue(key, out index))
                        {
                            isEmpty = cacheItems[index].IsEmpty(deliveryMethodIndex);
                        }

                        if (isEmpty)
                        {
                            source1[i] = true;
                            dest1[j] = true;
                            yes1 = true;
                        }

                        // i > j
                        key = GetKey(hash2, hash1);
                        isEmpty = true;
                        if (cacheItemIndex.TryGetValue(key, out index))
                        {
                            isEmpty = cacheItems[index].IsEmpty(deliveryMethodIndex);
                        }

                        if (isEmpty)
                        {
                            source2[j] = true;
                            dest2[i] = true;
                            yes2 = true;
                        }
                    }
                }

                // 6. Если расстояния между всеми парами известны
                rc = 6;
                if (!yes1 && !yes2)
                    return rc = 0;

                // 7. Запрашиваем данные для прямого направления
                rc = 7;

                if (yes1)
                {
                    // 7.1 Считаем количество исходных точек и точек назначения
                    rc = 71;
                    int sourceCount1 = 0;
                    int destCount1 = 0;

                    for (int i = 0; i < n; i++)
                    {
                        if (source1[i]) sourceCount1++;
                        if (dest1[i]) destCount1++;
                    }

                    // 7.2 Выделяем память под данные
                    rc = 72;
                    double[][] source_points1 = new double[sourceCount1][];
                    double[][] destination_points1 = new double[destCount1][];
                    int[] sourceIndex = new int[sourceCount1];
                    int[] destIndex = new int[destCount1];

                    // 7.3 Строим запрос гео-данных
                    rc = 73;
                    sourceCount1 = 0;
                    destCount1 = 0;

                    for (int i = 0; i < n; i++)
                    {
                        if (source1[i])
                        {
                            sourceIndex[sourceCount1] = i;
                            source_points1[sourceCount1++] = new double[] { latitude[i], longitude[i] };
                        }
                        if (dest1[i])
                        {
                            destIndex[destCount1] = i;
                            destination_points1[destCount1++] = new double[] { latitude[i], longitude[i] };
                        }
                    }

                    GetShippingInfo.ShippingInfoRequestEx requestData = new GetShippingInfo.ShippingInfoRequestEx();
                    string deliveryMethod = VehicleTypeToDeliveryMethod(vehicleType);
                    requestData.modes = new string[] { deliveryMethod };
                    requestData.origins = source_points1;
                    requestData.destinations = destination_points1;  

                    // 7.4 Запрашиваем гео-данные 
                    rc = 74;
                    GetShippingInfo.ShippingInfoResponse responseData;
                    int rc1 = GetShippingInfo.GetInfo(requestData, out responseData);
                    if (rc1 != 0)
                    {
                        //Helper.WriteErrorToLog($"LocationManager1.PutLocationInfo deliveryMethod = {deliveryMethod}, origin_count = {sourceCount}, destination_count = {destinationCount}, rc = {rc1}");
                        Logger.WriteToLog(string.Format(MessagePatterns.GEO_CACHE_PUT_INFO_ERROR, deliveryMethod, sourceCount1, destCount1, rc1));
                        return rc = 1000 * rc + rc1;
                    }
                    
                    // 7.5 Сохраняем полученные гео-данные 
                    rc = 75;
                    GetShippingInfo.PointsInfo[][] data = null;
                    if (responseData.driving != null)
                    {
                        data = responseData.driving;
                    }
                    else if (responseData.cycling != null)
                    {
                        data = responseData.cycling;
                    }
                    else if (responseData.walking != null)
                    {
                        data = responseData.walking;
                    }

                    if (data == null)
                        return rc;

                    for (int i = 0; i < sourceCount1; i++)
                    {
                        int i1 = sourceIndex[i];
                        int hash1 = pointHash[i1];
                        GetShippingInfo.PointsInfo[] postInfoRow = data[i];

                        for (int j = 0; j < destCount1; j++)
                        {
                            int j1 = destIndex[j];
                            if (j1 != i1)
                            {
                                int hash2 = pointHash[j1];
                                SaveCacheItem(hash1, hash2, new Point(postInfoRow[j].distance, postInfoRow[j].duration), deliveryMethodIndex);
                            }
                        }
                    }
                }

                // 8. Запрашиваем данные в обратном направлении
                rc = 8;

                if (yes2)
                {
                    // 8.1 Считаем количество исходных точек и точек назначения
                    rc = 81;
                    int sourceCount2 = 0;
                    int destCount2 = 0;

                    for (int i = 0; i < n; i++)
                    {
                        if (source2[i]) sourceCount2++;
                        if (dest2[i]) destCount2++;
                    }

                    // 8.2 Выделяем память под данные
                    rc = 82;
                    double[][] source_points2 = new double[sourceCount2][];
                    double[][] destination_points2 = new double[destCount2][];
                    int[] sourceIndex = new int[sourceCount2];
                    int[] destIndex = new int[destCount2];

                    // 8.3 Строим запрос гео-данных
                    rc = 83;
                    sourceCount2 = 0;
                    destCount2 = 0;

                    for (int i = 0; i < n; i++)
                    {
                        if (source2[i])
                        {
                            sourceIndex[sourceCount2] = i;
                            source_points2[sourceCount2++] = new double[] { latitude[i], longitude[i] };
                        }
                        if (dest2[i])
                        {
                            destIndex[destCount2] = i;
                            destination_points2[destCount2++] = new double[] { latitude[i], longitude[i] };
                        }
                    }

                    GetShippingInfo.ShippingInfoRequestEx requestData = new GetShippingInfo.ShippingInfoRequestEx();
                    string deliveryMethod = VehicleTypeToDeliveryMethod(vehicleType);
                    requestData.modes = new string[] { deliveryMethod };
                    requestData.origins = source_points2;
                    requestData.destinations = destination_points2;  

                    // 8.4 Запрашиваем гео-данные 
                    rc = 84;
                    GetShippingInfo.ShippingInfoResponse responseData;
                    int rc1 = GetShippingInfo.GetInfo(requestData, out responseData);
                    if (rc1 != 0)
                    {
                        //Helper.WriteErrorToLog($"LocationManager1.PutLocationInfo deliveryMethod = {deliveryMethod}, origin_count = {sourceCount}, destination_count = {destinationCount}, rc = {rc1}");
                        Logger.WriteToLog(string.Format(MessagePatterns.GEO_CACHE_PUT_INFO_ERROR, deliveryMethod, sourceCount2, destCount2, rc1));
                        return rc = 1000 * rc + rc1;
                    }
                    
                    // 8.5 Сохраняем полученные гео-данные 
                    rc = 85;
                    GetShippingInfo.PointsInfo[][] data = null;
                    if (responseData.driving != null)
                    {
                        data = responseData.driving;
                    }
                    else if (responseData.cycling != null)
                    {
                        data = responseData.cycling;
                    }
                    else if (responseData.walking != null)
                    {
                        data = responseData.walking;
                    }

                    if (data == null)
                        return rc;

                    for (int i = 0; i < sourceCount2; i++)
                    {
                        int i1 = sourceIndex[i];
                        int hash1 = pointHash[i1];
                        GetShippingInfo.PointsInfo[] postInfoRow = data[i];

                        for (int j = 0; j < destCount2; j++)
                        {
                            int j1 = destIndex[j];
                            if (j1 != i1)
                            {
                                int hash2 = pointHash[j1];
                                SaveCacheItem(hash1, hash2, new Point(postInfoRow[j].distance, postInfoRow[j].duration), deliveryMethodIndex);
                            }
                        }
                    }
                }

                // 9. Выход - Ok
                rc = 0;
                return rc;
            }
            catch
            {
                return rc;
            }
        }

        /// <summary>
        /// Запрос неизвестных расстояний и времени движения
        /// из исходных точек в точки назначения
        /// </summary>
        /// <param name="srcLatitude">Широта исходных точек</param>
        /// <param name="srcLongitude">Долгота исходных точек</param>
        /// <param name="dstLatitude">Широта точек назначения</param>
        /// <param name="dstLongitude">Долгота точек назначения</param>
        /// <param name="vehicleType">Тип способа доставки</param>
        /// <returns>0 - запрошенные данные получены; иначе - запрошенные данные не получены</returns>
        public int PutLocationInfo(
            double[] srcLatitude, double[] srcLongitude, 
            double[] dstLatitude, double[] dstLongitude, 
            CourierVehicleType vehicleType)
        {
            // 1. Инициализация 
            int rc = 1;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (srcLatitude == null || srcLatitude.Length <= 0)
                    return rc;
                if (srcLongitude == null || srcLongitude.Length !=  srcLatitude.Length)
                    return rc;
                if (dstLatitude == null || dstLatitude.Length <= 0)
                    return rc;
                if (dstLongitude == null || dstLongitude.Length !=  dstLatitude.Length)
                    return rc;

                // 3. Запрашиваем индекс способа доставки
                rc = 3;
                int deliveryMethodIndex = GetDeliveryMethodIndex(vehicleType);
                if (deliveryMethodIndex < 0)
                    return rc;

                // 4. Находим хэш-значения координат
                rc = 4;
                int n1 = srcLatitude.Length;
                int[] srcPointHash = new int[n1];
                int n2 = dstLatitude.Length;
                int[] dstPointHash = new int[n2];

                for (int i = 0; i < n1; i++)
                {
                    coordinateHash.Latitude = srcLatitude[i];
                    coordinateHash.Longitude = srcLongitude[i];
                    srcPointHash[i] = coordinateHash.GetHashCode();
                }

                for (int i = 0; i < n2; i++)
                {
                    coordinateHash.Latitude = dstLatitude[i];
                    coordinateHash.Longitude = dstLongitude[i];
                    dstPointHash[i] = coordinateHash.GetHashCode();
                }

                // 5. Выделяем пары, для которых расстояния неизвестны
                rc = 5;
                int sourceCount = 0;
                int destinationCount = 0;
                bool[] isSource = new bool[n1];
                bool[] isDestination = new bool[n2];
                int[] sourceLocationIndex = new int[n1];
                int[] destinationLocationIndex = new int[n2];

                for (int i = 0; i < n1; i++)
                {
                    int hash1 = srcPointHash[i];

                    for (int j = 0; j < n2; j++)
                    {
                        int hash2 = dstPointHash[j];

                        bool isEmpty = true;

                        ulong key = GetKey(hash1, hash2);
                        int index;
                        if (cacheItemIndex.TryGetValue(key, out index))
                        {
                            isEmpty = cacheItems[index].IsEmpty(deliveryMethodIndex);
                        }

                        if (isEmpty)
                        {
                            if (!isSource[i])
                            {
                                sourceLocationIndex[sourceCount++] = i;
                                isSource[i] = true;
                            }
                            if (!isDestination[j])
                            {
                                destinationLocationIndex[destinationCount++] = j;
                                isDestination[j] = true;
                            }
                        }
                    }
                }

                // 6. Если все расстояния между всеми парами известны
                rc = 6;
                if (sourceCount <= 0)
                    return rc = 0;

                // 7. Запрашиваем расстояния и время движения, которые неизвестны в прямом направлении
                rc = 7;
                GetShippingInfo.ShippingInfoRequestEx requestData = new GetShippingInfo.ShippingInfoRequestEx();
                //string deliveryMethod = deliveryMethodName[deliveryMethodIndex];
                string deliveryMethod = VehicleTypeToDeliveryMethod(vehicleType);
                requestData.modes = new string[] { deliveryMethod };
                double[][] source_points = new double[sourceCount][];
                double[][] destination_points = new double[destinationCount][];

                for (int i = 0; i < sourceCount; i++)
                {
                    int index = sourceLocationIndex[i];
                    source_points[i] = new double[] { srcLatitude[index], srcLongitude[index] };
                }

                for (int i = 0; i < destinationCount; i++)
                {
                    int index = destinationLocationIndex[i];
                    destination_points[i] = new double[] { dstLatitude[index], dstLongitude[index] };
                }

                requestData.origins = source_points;
                requestData.destinations = destination_points;

                GetShippingInfo.ShippingInfoResponse responseData;
                int rc1 = GetShippingInfo.GetInfo(requestData, out responseData);
                if (rc1 != 0)
                {
                    //Helper.WriteErrorToLog($"LocationManager1.PutLocationInfo deliveryMethod = {deliveryMethod}, origin_count = {sourceCount}, destination_count = {destinationCount}, rc = {rc1}");
                    Logger.WriteToLog(string.Format(MessagePatterns.GEO_CACHE_PUT_INFO_ERROR, deliveryMethod, sourceCount, destinationCount, rc1));
                    return rc = 1000 * rc + rc1;
                }

                // 8. Сохраняем полученные данные в кэше
                rc = 8;
                GetShippingInfo.PointsInfo[][] data = null;
                if (responseData.driving != null)
                {
                    data = responseData.driving;
                }
                else if (responseData.cycling != null)
                {
                    data = responseData.cycling;
                }
                else if (responseData.walking != null)
                {
                    data = responseData.walking;
                }

                if (data == null)
                    return rc;

                for (int i = 0; i < sourceCount; i++)
                {
                    int i1 = sourceLocationIndex[i];
                    int hash1 = srcPointHash[i1];
                    GetShippingInfo.PointsInfo[] postInfoRow = data[i];

                    for (int j = 0; j < destinationCount; j++)
                    {
                        int j1 = destinationLocationIndex[j];
                        if (j1 != i1)
                        {
                            int hash2 = dstPointHash[j1];
                            SaveCacheItem(hash1, hash2, new Point(postInfoRow[j].distance, postInfoRow[j].duration), deliveryMethodIndex);
                        }
                    }
                }

                // 9. Выход - Ok
                rc = 0;
                return rc;
            }
            catch
            {
                return rc;
            }
        }

        /// <summary>
        /// Запрос попарных расстояний и времени движения между точками
        /// </summary>
        /// <param name="latitude">Широта</param>
        /// <param name="longitude">Долгота</param>
        /// <param name="vehicleType">Способ доставки</param>
        /// <param name="dataTable">Таблица результата</param>
        /// <returns>0 - таблица построена; иначе - таблица не построена</returns>
        public int GetPointsDataTable(double[] latitude, double[] longitude, CourierVehicleType vehicleType, out Point[,] dataTable)
        {
            // 1. Инициализация
            int rc = 1;
            dataTable = null;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (latitude == null || latitude.Length <= 0)
                    return rc;
                if (longitude == null || longitude.Length !=  latitude.Length)
                    return rc;

                // 21. Запрашиваем индекс способа доставки
                rc = 21;
                int deliveryMethodIndex = GetDeliveryMethodIndex(vehicleType);
                if (deliveryMethodIndex < 0)
                    return rc;

                // 3. Запрашиваем неизвестную информацию
                //rc = 3;
                //int rc1 = PutLocationInfo(latitude, longitude, vehicleType);
                //if (rc1 != 0)
                //    return rc = 1000 * rc + rc1;

                // 4. Находим хэш-значения координат
                rc = 4;
                int n = latitude.Length;
                int[] pointHash = new int[n];

                for (int i = 0; i < n; i++)
                {
                    coordinateHash.Latitude = latitude[i];
                    coordinateHash.Longitude = longitude[i];
                    pointHash[i] = coordinateHash.GetHashCode();
                }

                // 5. Заполняем таблицу результата
                rc = 5;
                //int rc3 = PutLocationInfo(latitude, longitude, vehicleType);

                dataTable = new Point[n, n];

                for (int i = 0; i < n; i++)
                {
                    int hash1 = pointHash[i];

                    for (int j = i + 1; j < n; j++)
                    {
                        int hash2 = pointHash[j];

                        if (hash1 != hash2)
                        {
                            ulong key1 = GetKey(hash1, hash2);
                            ulong key2 = GetKey(hash2, hash1);

                            CacheItem item1 = cacheItems[cacheItemIndex[GetKey(hash1, hash2)]];
                            CacheItem item2 = cacheItems[cacheItemIndex[GetKey(hash2, hash1)]];
                            dataTable[i, j] = item1.Data[deliveryMethodIndex];
                            dataTable[j, i] = item2.Data[deliveryMethodIndex];
                        }
                    }
                }

                // 6. Выход - Ok
                rc = 0;
                return rc;
            }
            catch (Exception ex)
            {
                Logger.WriteToLog($"[debug] GetPointsDataTable. finnaly rc = {rc} vehicle_type = {vehicleType}  latitude = ({Helper.ArrayToString(latitude)}) longitude = ({Helper.ArrayToString(longitude)})");
                Logger.WriteToLog(string.Format(MessagePatterns.METHOD_FAIL, " GetPointsDataTable", ex.ToString()));

                return rc;
            }
        }

        /// <summary>
        /// Сохранение элемента кэша
        /// </summary>
        /// <param name="hash1">Хэш-значение точки 1</param>
        /// <param name="hash2">Хэш-значение точки 2</param>
        /// <param name="distTime">Расстояние и время движения между точками</param>
        /// <param name="deliveryMethodIndex">Индекс способа доставки</param>
        /// <returns>0 - элемент сохранен; элемент не сохранен</returns>
        private int SaveCacheItem(int hash1, int hash2, Point distTime, int deliveryMethodIndex)
        {
            // 1. Инициализация
            int rc = 1;

            try
            {
                // 2. Находим индекс элемента кэша
                rc = 2;
                int itemIndex = -1;
                ulong key = GetKey(hash1, hash2);
                CacheItem item = null;

                if (!cacheItemIndex.TryGetValue(key, out itemIndex))
                {
                    if (cacheItemCount < cacheItems.Length)
                    {
                        itemIndex = cacheItemCount++;
                    }
                    else
                    {
                        itemIndex = cacheItemCount % cacheItems.Length;
                        cacheItemCount++;
                        item = cacheItems[itemIndex];
                        if (item != null)
                            cacheItemIndex.Remove(item.Key);
                    }

                    cacheItemIndex.Add(key, itemIndex);
                }

                // 3. Заполняем элемента кэша
                rc = 3;
                item = cacheItems[itemIndex];
                if (item == null)
                {
                    item = new CacheItem(key, yandexTypeCount);
                    cacheItems[itemIndex] = item;
                }

                item.SetData(deliveryMethodIndex, distTime);

                // 4. Выход - Ok
                rc = 0;
                return rc;
            }
            catch (Exception ex)
            {
                Logger.WriteToLog(string.Format(MessagePatterns.METHOD_CALL, "GeoCache.SaveCacheItem", $"hash1 = {hash1}, hash2 = {hash2}, deliveryMethodIndex = {deliveryMethodIndex}, data={distTime}"));
                Logger.WriteToLog(string.Format(MessagePatterns.METHOD_RC, "GeoCache.SaveCacheItem", rc));
                Logger.WriteToLog(string.Format(MessagePatterns.METHOD_FAIL, "GeoCache.SaveCacheItem", ex.ToString()));
                return rc;
            }
        }

        /// <summary>
        /// Построение ключа для пары хэш-значений точек
        /// </summary>
        /// <param name="hash1">Хэш 1</param>
        /// <param name="hash2">Хэш 2</param>
        /// <returns>Ключ</returns>
        private static ulong GetKey(int hash1, int hash2)
        {
            return (((ulong)hash1) << 32 & 0xFFFFFFFF00000000) | ((ulong)hash2 & 0x00000000FFFFFFFF);
        }

        /// <summary>
        /// Преобразование способа доставки в название способа доставки для запроса API-данных
        /// </summary>
        /// <param name="vehicleType">CourierVehicleType</param>
        /// <returns>API-название способа доставки</returns>
        private string VehicleTypeToDeliveryMethod(CourierVehicleType vehicleType)
        {
            string deliveryName;
            GeoDeliveryMethod gdm;
            if (deliveryMethods.TryGetValue(vehicleType, out gdm))
            {
                deliveryName = gdm.Name;
            }
            else
            {
                deliveryName = null;
            }

            return deliveryName;
        }

        /// <summary>
        /// Преобразование способа доставки курьером
        /// в название метода доставки для запроса данных
        /// </summary>
        /// <param name="vehicleType"></param>
        /// <returns></returns>
        private int GetDeliveryMethodIndex(CourierVehicleType vehicleType)
        {
            int deliveryIndex;
            GeoDeliveryMethod gdm;
            if (deliveryMethods.TryGetValue(vehicleType, out gdm))
            {
                deliveryIndex = gdm.Index;
            }
            else
            {
                deliveryIndex = -1;
            }

            return deliveryIndex;
        }
    }
}
