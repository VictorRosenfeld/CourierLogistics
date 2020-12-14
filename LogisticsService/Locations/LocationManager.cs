
//namespace LogisticsService.Locations
//{
//    using LogisticsService.Couriers;
//    using System.Collections.Generic;
//    using System.Device.Location;
//    using System.Drawing;
//    using LogisticsService.API;
//    using System;

//    /// <summary>
//    /// Обеспечение расстояния и времени движения
//    /// между парами точек, определенных широтой и долготой
//    /// </summary>
//    public class LocationManager
//    {        
//        #region vechicle ID constants

//        /// <summary>
//        /// ID передвижения с помощью автомобиля
//        /// </summary>
//        private const string DELIVERY_BY_CAR = "driving";

//        /// <summary>
//        /// ID передвижения с помощью велосипеда
//        /// </summary>
//        private const string DELIVERY_BY_BICYCLE = "cycling";

//        /// <summary>
//        ///  ID пешего передвижения
//        /// </summary>
//        private const string DELIVERY_BY_ONFOOT = "walking";

//        #endregion vechicle ID constants

//        #region private fields

//        /// <summary>
//        /// Индексы точек
//        /// (hash_координат ---> индекс точки)
//        /// </summary>
//        private Dictionary<int, int> locationIndex;

//        /// <summary>
//        /// Широта точек, которым присвоен индекс
//        /// </summary>
//        private double[] locationLatitude;

//        /// <summary>
//        /// Долгота точек, которым присвоен индекс
//        /// </summary>
//        private double[] locationLongitude;

//        /// <summary>
//        /// Количество точек
//        /// </summary>
//        private int locationCount;

//        /// <summary>
//        /// Информация о рассстоянии и времени
//        /// движения для пешего курьера
//        /// </summary>
//        private Point[,] onFootMap;
      
//        /// <summary>
//        /// Информация о рассстоянии и времени
//        /// движения для курьера на велосипеде
//        /// </summary>
//        private Point[,] bicycleMap;

//        /// <summary>
//        /// Информация о рассстоянии и времени
//        /// движения для курьера на авто
//        /// </summary>
//        private Point[,] carMap;

//        /// <summary>
//        /// Рабочий массив
//        /// </summary>
//        private int[] sourceLocationIndex;

//        /// <summary>
//        /// Рабочий массив
//        /// </summary>
//        private int[] destinationLocationIndex;

//        #endregion private fields

//        #region properties

//        /// <summary>
//        /// Количество точек
//        /// </summary>
//        public int LocationCount => locationCount;

//        /// <summary>
//        /// Широта точек, которым присвоен индекс
//        /// </summary>
//        public double[] LocationLatitude => locationLatitude;

//        /// <summary>
//        /// Долгота точек, которым присвоен индекс
//        /// </summary>
//        public double[] LocationLongitude => locationLongitude;

//        /// <summary>
//        /// Информация о рассстоянии и времени
//        /// движения для пешего курьера
//        /// </summary>
//        public Point[,] OnFootMap => onFootMap;

//        /// <summary>
//        /// Информация о рассстоянии и времени
//        /// движения для курьера на велосипеде
//        /// </summary>
//        public Point[,] BicycleMap => bicycleMap;

//        /// <summary>
//        /// Информация о рассстоянии и времени
//        /// движения для курьера на авто
//        /// </summary>
//        public Point[,] CarMap => carMap;

//        #endregion properties

//        /// <summary>
//        /// Параметрический конструктор класса LocationManager
//        /// </summary>
//        /// <param name="cpacity">Максимальное количество точек</param>
//        public LocationManager(int cpacity)
//        {
//            locationCount = 0;
//            locationLatitude = new double[cpacity];
//            locationLongitude = new double[cpacity];
//            locationIndex = new Dictionary<int, int>(cpacity);
//            onFootMap = new Point[cpacity, cpacity];
//            bicycleMap = new Point[cpacity, cpacity];
//            carMap = new Point[cpacity, cpacity];
//            sourceLocationIndex = new int[cpacity];
//            destinationLocationIndex = new int[cpacity];
//        }

//        /// <summary>
//        /// Получить индекс точки
//        /// </summary>
//        /// <param name="latitude">Широта точки</param>
//        /// <param name="longitude">Долгота точки</param>
//        /// <returns>Индекс точки</returns>
//        public int GetLocationIndex(double latitude, double longitude)
//        {
//            GeoCoordinate pt = new GeoCoordinate(latitude, longitude);
//            int hash = pt.GetHashCode();
//            int index;
//            if (!locationIndex.TryGetValue(hash, out index))
//            {
//                locationLatitude[locationCount] = latitude;
//                locationLongitude[locationCount] = longitude;
//                index = locationCount++;
//                locationIndex.Add(hash, index);

//                if (locationCount > sourceLocationIndex.Length)
//                {
//                    Helper.WriteErrorToLog($"LocationCount > {sourceLocationIndex.Length}");
//                }
//            }

//            return index;
//        }

//        /// <summary>
//        /// Запрос расстояний и времени движения
//        /// между заданными парами точек
//        /// </summary>
//        /// <param name="locIndex">Индексы точек, из которых образуются пары</param>
//        /// <param name="vehicleType">Тип курьера или такси</param>
//        /// <param name="locMatrix">Расстояния и время движения между парами точек</param>
//        /// <returns>0 - запрошенные данные получены; иначе - запрошенные данные не получены</returns>
//        public int GetLocationInfo(int[] locIndex, CourierVehicleType vehicleType, out Point[,] locMatrix)
//        {
//            locMatrix = null;

//            switch (vehicleType)
//            {
//                case CourierVehicleType.Car:
//                case CourierVehicleType.GettTaxi:
//                case CourierVehicleType.YandexTaxi:
//                    return GetLocationInfo(locIndex, carMap, DELIVERY_BY_CAR, out locMatrix);
//                case CourierVehicleType.Bicycle:
//                    return GetLocationInfo(locIndex, bicycleMap, DELIVERY_BY_BICYCLE, out locMatrix);
//                case CourierVehicleType.OnFoot:
//                    return GetLocationInfo(locIndex, onFootMap, DELIVERY_BY_ONFOOT, out locMatrix);
//                default:
//                    return -1;
//            }
//        }

//        /// <summary>
//        /// Запрос попарных расстояний и времени
//        /// перемещения между точками
//        /// </summary>
//        /// <param name="locIndex">Индексы точек</param>
//        /// <param name="vehicleMap">Общая карта попарных расстояний и времени перемещения между точками</param>
//        /// <param name="deliveryMethod">Метод отгрузки</param>
//        /// <param name="locMatrix">Матрица попарных расстояний между точками</param>
//        /// <returns></returns>
//        private int GetLocationInfo(int[] locIndex, Point[,] vehicleMap, string deliveryMethod, out Point[,] locMatrix)
//        {
//            // 1. Инициализация
//            int rc = 1;
//            locMatrix = null;

//            try
//            {
//                // 2. Проверяем исходные данные
//                rc = 2;
//                if (locIndex == null || locIndex.Length <= 0)
//                    return rc;
//                if (vehicleMap == null || vehicleMap.Length <= 0)
//                    return rc;
//                int n = vehicleMap.GetLength(0);
//                if (vehicleMap.GetLength(1) != n)
//                    return rc;

//                if (string.IsNullOrEmpty(deliveryMethod))
//                    return rc;

//                // 3. Выделяем память под результат
//                rc = 3;
//                locMatrix = new Point[n, n];
//                int sourceCount = 0;
//                int destinationCount = 0;
//                bool[] isSource = new bool[n];
//                bool[] isDestination = new bool[n];

//                for (int i = 0; i < n; i++)
//                {
//                    int locIndex1 = locIndex[i];

//                    for(int j = i + 1; j < n; j++)
//                    {
//                        int locIndex2 = locIndex[j];
//                        if (vehicleMap[locIndex1, locIndex2].IsEmpty || vehicleMap[locIndex2, locIndex1].IsEmpty)
//                        {
//                            if (!isSource[i])
//                            {
//                                sourceLocationIndex[sourceCount++] = i;
//                                isSource[i] = true;
//                            }
//                            if (!isDestination[j])
//                            {
//                                destinationLocationIndex[destinationCount++] = j;
//                                isDestination[j] = true;
//                            }
//                        }
//                        else
//                        {
//                            locMatrix[i, j] = vehicleMap[locIndex1, locIndex2];
//                            locMatrix[j, i] = vehicleMap[locIndex2, locIndex1];
//                        }
//                    }
//                }

//                // 4. Если расстояния между всеми парами известны
//                rc = 4;
//                if (sourceCount <= 0)
//                    return rc = 0;

//                // 5. Запрашиваем расстояния и время движения, которые неизвестны в прямом направлении
//                rc = 5;
//                GetShippingInfo.ShippingInfoRequestEx requestData = new GetShippingInfo.ShippingInfoRequestEx();
//                requestData.modes = new string[] { deliveryMethod };
//                double[][] source_points = new double[sourceCount][];
//                double[][] destination_points = new double[destinationCount][];

//                for (int i = 0; i < sourceCount; i++)
//                {
//                    int index = locIndex[sourceLocationIndex[i]];
//                    source_points[i] = new double[] { locationLatitude[index], locationLongitude[index] };
//                }

//                for (int i = 0; i < destinationCount; i++)
//                {
//                    int index = locIndex[destinationLocationIndex[i]];
//                    destination_points[i] = new double[] { locationLatitude[index], locationLongitude[index] };
//                }

//                requestData.origins = source_points;
//                requestData.destinations = destination_points;

//                GetShippingInfo.ShippingInfoResponse responseData;
//                int rc1 = GetShippingInfo.GetInfo(requestData, out responseData);
//                if (rc1 != 0)
//                    return rc = 1000 * rc + rc1;

//                // 6. Сохраняем результат source ---> destination
//                rc = 6;
//                GetShippingInfo.PointsInfo[][] data = null;
//                if (responseData.driving != null)
//                {
//                    data = responseData.driving;
//                }
//                else if (responseData.cycling != null)
//                {
//                    data = responseData.cycling;
//                }
//                else if (responseData.walking != null)
//                {
//                    data = responseData.cycling;
//                }

//                if (data == null)
//                    return rc;

//                for (int i = 0; i < sourceCount; i++)
//                {
//                    int i1 = sourceLocationIndex[i];
//                    int loc1 = locIndex[i1];
//                    GetShippingInfo.PointsInfo[] postInfoRow = data[i];

//                    for (int j = 0; j < destinationCount; j++)
//                    {
//                        int j1 = destinationLocationIndex[j];
//                        int loc2 = locIndex[j1];
//                        Point pairInfo = new Point(postInfoRow[j].distance, postInfoRow[j].duration);
//                        vehicleMap[loc1, loc2] = pairInfo;
//                        locMatrix[i1, j1] = pairInfo;
//                    }
//                }

//                // 7. Запрашиваем расстояния и время движения, которые неизвестны в обратном направлении
//                rc = 7;
//                requestData.origins = destination_points;
//                requestData.destinations = source_points;

//                rc1 = GetShippingInfo.GetInfo(requestData, out responseData);
//                if (rc1 != 0)
//                    return rc = 1000 * rc + rc1;

//                // 8. Сохраняем результат destination ---> source
//                rc = 8;
//                if (responseData.driving != null)
//                {
//                    data = responseData.driving;
//                }
//                else if (responseData.cycling != null)
//                {
//                    data = responseData.cycling;
//                }
//                else if (responseData.walking != null)
//                {
//                    data = responseData.cycling;
//                }

//                if (data == null)
//                    return rc;

//                for (int i = 0; i < destinationCount; i++)
//                {
//                    int i1 = destinationLocationIndex[i];
//                    int loc1 = locIndex[i1];
//                    GetShippingInfo.PointsInfo[] postInfoRow = data[i];

//                    for (int j = 0; j < sourceCount; j++)
//                    {
//                        int j1 = sourceLocationIndex[j];
//                        int loc2 = locIndex[j1];
//                        Point pairInfo = new Point(postInfoRow[j].distance, postInfoRow[j].duration);
//                        vehicleMap[loc1, loc2] = pairInfo;
//                        locMatrix[i1, j1] = pairInfo;
//                    }
//                }

//                // 9. Выход - Ok
//                rc = 0;
//                return 0;
//            }
//            catch
//            {
//                return rc;
//            }
//        }

//        /// <summary>
//        /// Запрос неизвестных расстояний и времени движения
//        /// между заданными парами точек
//        /// </summary>
//        /// <param name="locIndex">Индексы точек, из которых образуются пары</param>
//        /// <param name="vehicleType">Тип курьера или такси</param>
//        /// <returns>0 - запрошенные данные получены; иначе - запрошенные данные не получены</returns>
//        public int PutLocationInfo(int[] locIndex, CourierVehicleType vehicleType)
//        {
//            switch (vehicleType)
//            {
//                case CourierVehicleType.Car:
//                case CourierVehicleType.GettTaxi:
//                case CourierVehicleType.YandexTaxi:
//                    return PutLocationInfo(locIndex, carMap, DELIVERY_BY_CAR);
//                case CourierVehicleType.Bicycle:
//                    return PutLocationInfo(locIndex, bicycleMap, DELIVERY_BY_BICYCLE);
//                case CourierVehicleType.OnFoot:
//                    return PutLocationInfo(locIndex, onFootMap, DELIVERY_BY_ONFOOT);
//                default:
//                    return -1;
//            }
//        }

//        /// <summary>
//        /// Запрос попарных расстояний и времени
//        /// перемещения между парами из заданных точек
//        /// </summary>
//        /// <param name="locIndex">Индексы точек, из которых образуются пары</param>
//        /// <param name="vehicleMap">Пополняемая карта попарных расстояний и времени перемещения между точками</param>
//        /// <param name="deliveryMethod">Способ доставки</param>
//        /// <returns></returns>
//        private int PutLocationInfo(int[] locIndex, Point[,] vehicleMap, string deliveryMethod)
//        {
//            // 1. Инициализация
//            int rc = 1;

//            try
//            {
//                // 2. Проверяем исходные данные
//                rc = 2;
//                if (locIndex == null || locIndex.Length <= 0)
//                    return rc;
//                if (vehicleMap == null || vehicleMap.Length <= 0)
//                    return rc;
//                int n = vehicleMap.GetLength(0);
//                if (vehicleMap.GetLength(1) != n)
//                    return rc;

//                if (string.IsNullOrEmpty(deliveryMethod))
//                    return rc;

//                // 3. Находим пары, для которых нет информации
//                rc = 3;
//                n = locIndex.Length;
//                int sourceCount = 0;
//                int destinationCount = 0;
//                bool[] isSource = new bool[n];
//                bool[] isDestination = new bool[n];

//                for (int i = 0; i < n; i++)
//                {
//                    int locIndex1 = locIndex[i];

//                    for(int j = i + 1; j < n; j++)
//                    {
//                        int locIndex2 = locIndex[j];
//                        if (locIndex1 != locIndex2 && (vehicleMap[locIndex1, locIndex2].IsEmpty || vehicleMap[locIndex2, locIndex1].IsEmpty))
//                        {
//                            if (!isSource[i])
//                            {
//                                sourceLocationIndex[sourceCount++] = i;
//                                isSource[i] = true;
//                            }
//                            if (!isDestination[j])
//                            {
//                                destinationLocationIndex[destinationCount++] = j;
//                                isDestination[j] = true;
//                            }
//                        }
//                    }
//                }

//                // 4. Если расстояния между всеми парами известны
//                rc = 4;
//                if (sourceCount <= 0)
//                    return rc = 0;

//                // 5. Запрашиваем расстояния и время движения, которые неизвестны в прямом направлении
//                rc = 5;
//                GetShippingInfo.ShippingInfoRequestEx requestData = new GetShippingInfo.ShippingInfoRequestEx();
//                requestData.modes = new string[] { deliveryMethod };
//                double[][] source_points = new double[sourceCount][];
//                double[][] destination_points = new double[destinationCount][];

//                for (int i = 0; i < sourceCount; i++)
//                {
//                    int index = locIndex[sourceLocationIndex[i]];
//                    source_points[i] = new double[] { locationLatitude[index], locationLongitude[index] };
//                }

//                for (int i = 0; i < destinationCount; i++)
//                {
//                    int index = locIndex[destinationLocationIndex[i]];
//                    destination_points[i] = new double[] { locationLatitude[index], locationLongitude[index] };
//                }

//                requestData.origins = source_points;
//                requestData.destinations = destination_points;

//                GetShippingInfo.ShippingInfoResponse responseData;
//                int rc1 = GetShippingInfo.GetInfo(requestData, out responseData);
//                if (rc1 != 0)
//                {
//                    Helper.WriteErrorToLog($"LocationManager1.PutLocationInfo deliveryMethod = {deliveryMethod}, origin_count = {sourceCount}, destination_count = {destinationCount}, rc = {rc1}");
//                    return rc = 1000 * rc + rc1;
//                }

//                // 6. Сохраняем результат source ---> destination
//                rc = 6;
//                GetShippingInfo.PointsInfo[][] data = null;
//                if (responseData.driving != null)
//                {
//                    data = responseData.driving;
//                }
//                else if (responseData.cycling != null)
//                {
//                    data = responseData.cycling;
//                }
//                else if (responseData.walking != null)
//                {
//                    data = responseData.walking;
//                }

//                if (data == null)
//                    return rc;

//                for (int i = 0; i < sourceCount; i++)
//                {
//                    int i1 = sourceLocationIndex[i];
//                    int loc1 = locIndex[i1];
//                    GetShippingInfo.PointsInfo[] postInfoRow = data[i];

//                    for (int j = 0; j < destinationCount; j++)
//                    {
//                        int j1 = destinationLocationIndex[j];
//                        int loc2 = locIndex[j1];
//                        Point pairInfo = new Point(postInfoRow[j].distance, postInfoRow[j].duration);
//                        vehicleMap[loc1, loc2] = pairInfo;
//                    }
//                }

//                // 7. Запрашиваем расстояния и время движения, которые неизвестны в обратном направлении
//                rc = 7;
//                requestData.origins = destination_points;
//                requestData.destinations = source_points;

//                rc1 = GetShippingInfo.GetInfo(requestData, out responseData);
//                if (rc1 != 0)
//                {
//                    Helper.WriteErrorToLog($"LocationManager1.PutLocationInfo deliveryMethod = {deliveryMethod}, origin_count = {sourceCount}, destination_count = {destinationCount}, rc = {rc1}");
//                    return rc = 1000 * rc + rc1;
//                }

//                // 8. Сохраняем результат destination ---> source
//                rc = 8;
//                if (responseData.driving != null)
//                {
//                    data = responseData.driving;
//                }
//                else if (responseData.cycling != null)
//                {
//                    data = responseData.cycling;
//                }
//                else if (responseData.walking != null)
//                {
//                    data = responseData.walking;
//                }

//                if (data == null)
//                    return rc;

//                for (int i = 0; i < destinationCount; i++)
//                {
//                    int i1 = destinationLocationIndex[i];
//                    int loc1 = locIndex[i1];
//                    GetShippingInfo.PointsInfo[] postInfoRow = data[i];

//                    for (int j = 0; j < sourceCount; j++)
//                    {
//                        int j1 = sourceLocationIndex[j];
//                        int loc2 = locIndex[j1];
//                        Point pairInfo = new Point(postInfoRow[j].distance, postInfoRow[j].duration);
//                        vehicleMap[loc1, loc2] = pairInfo;
//                    }
//                }

//                // 9. Выход - Ok
//                rc = 0;
//                return 0;
//            }
//            catch (Exception ex)
//            {
//                Helper.WriteErrorToLog($"LocationManager1.PutLocationInfo deliveryMethod = {deliveryMethod}, rc = {rc}");
//                Logger.WriteToLog(ex.ToString());
//                return rc;
//            }
//        }

//        /// <summary>
//        /// Получить расстояния и время движения
//        /// между точками для заданного способа доставки
//        /// </summary>
//        /// <param name="vehicleType"></param>
//        /// <returns>Расстояния и время или null</returns>
//        public Point[,] GetLocationInfoForType(CourierVehicleType vehicleType)
//        {
//            switch (vehicleType)
//            {
//                case CourierVehicleType.Car:
//                case CourierVehicleType.YandexTaxi:
//                case CourierVehicleType.GettTaxi:
//                    return carMap;
//                case CourierVehicleType.Bicycle:
//                    return bicycleMap;
//                case CourierVehicleType.OnFoot:
//                    return onFootMap;
//            }

//            return null;
//        }
//    }
//}
