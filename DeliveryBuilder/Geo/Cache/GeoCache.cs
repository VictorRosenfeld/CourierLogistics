
namespace DeliveryBuilder.Geo.Cache
{
    using DeliveryBuilder.BuilderParameters;
    using DeliveryBuilder.Log;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Гео-кэш
    /// </summary>
    public class GeoCache
    {
        /// <summary>
        /// Объект сихронизации
        /// </summary>
        private object syncRoot = new object();

        /// <summary>
        /// Флаг: true - экземпляр создан; false - экземпляр не создан
        /// </summary>
        public bool IsCreated { get; private set; }

        /// <summary>
        /// Последнее прерывание
        /// </summary>
        public Exception LastException { get; private set; }

        /// <summary>
        /// Емкость гео-кэша
        /// </summary>
        private int capacity;

        /// <summary>
        /// Емкость гео-кэша
        /// </summary>
        public int Capacity => capacity;

        /// <summary>
        /// Интервал актуальности данных, мин
        /// </summary>
        private int savingInterval;

        /// <summary>
        /// Интервал актуальности данных, мин
        /// </summary>
        public int SavingInterval => savingInterval;

        /// <summary>
        /// Количество способов передвижения 
        /// </summary>
        private int vehicleTypeCount;

        /// <summary>
        /// Количество способов передвижения 
        /// </summary>
        public int VehicleTypeCount => vehicleTypeCount;

        /// <summary>
        /// Данные кэша для всех способов доставки
        /// </summary>
        private Dictionary<ulong, GeoCacheItem>[] vehicleGeoData;

        /// <summary>
        /// Данные кэша для всех способов доставки
        /// </summary>
        public Dictionary<ulong, GeoCacheItem>[] VehicleGeoData => vehicleGeoData;

        /// <summary>
        /// Количество элементов в гео-кэше
        /// </summary>
        public int Count
        {
            get
            {
                lock (syncRoot)
                {
                    if (!IsCreated || vehicleGeoData == null)
                        return 0;
                    int count = 0;
                    for (int i = 0; i < vehicleGeoData.Length; i++)
                    { count += vehicleGeoData[i].Count; }

                    return count;
                }
            }
        }

        /// <summary>
        /// Создание экземпляра гео-кэша
        /// </summary>
        /// <param name="parameters">Параметры кэша</param>
        /// <param name="vehicleTypes">Количество способов передвижения в гео-кэше</param>
        /// <returns></returns>
        public int Create(GeoCacheParameters parameters, int vehicleTypes)
        {
            lock (syncRoot)
            {
                // 1. Иициализация
                int rc = 1;
                IsCreated = false;
                LastException = null;
                capacity = 0;
                savingInterval = 0;
                this.vehicleTypeCount = 0;
                vehicleGeoData = null;

                try
                {
                    // 2. Проверяем исходные данные
                    rc = 2;
                    //Logger.WriteToLog(111, MessageSeverity.Warn, $"capacity = {parameters.Capacity}, saving_interval = {parameters.SavingInterval}, vehicleTypeCount = {vehicleTypeCount}");
                    if (parameters == null ||
                        parameters.Capacity <= 0 || parameters.SavingInterval <= 0)
                        return rc;
                    if (vehicleTypes <= 0)
                        return rc;

                    // 3. Сохраяем параметры гео-кэша
                    rc = 3;
                    capacity = parameters.Capacity;
                    savingInterval = parameters.SavingInterval;
                    this.vehicleTypeCount = vehicleTypes;

                    // 4. Создаём коллекции хранимых элементов для всех способов доставки
                    rc = 4;
                    vehicleGeoData = new Dictionary<ulong, GeoCacheItem>[vehicleTypes];
                    int collectionCapacity = capacity / vehicleTypes;
                    if (collectionCapacity < 16)
                        collectionCapacity = 16;

                    for (int i = 0; i < vehicleGeoData.Length; i++)
                    { vehicleGeoData[i] = new Dictionary<ulong, GeoCacheItem>(collectionCapacity); }

                    // 5. Выход - Ok
                    rc = 0;
                    return rc;
                }
                catch (Exception ex)
                {
                    LastException = ex;
                    Logger.WriteToLog(669, MessageSeverity.Error, string.Format(Messages.MSG_669, $"{nameof(GeoCache)}.{nameof(this.Create)}", rc, (ex.InnerException == null ? ex.Message : ex.InnerException.Message)));
                    return rc;
                }
            }
        }

        /// <summary>
        /// Построение hash-значения для координат (latitude, longitude)
        /// </summary>
        /// <param name="latitude">Широта</param>
        /// <param name="longitude">Долгота</param>
        /// <returns>Hash-значение координат</returns>
        private static uint GetCoordinateHash(double latitude, double longitude)
        {
            double plat = Math.Abs(latitude);
            uint ilat = (uint)plat;
            uint flat = (uint)((plat - ilat + 0.00005) * 10000);
            double plon = Math.Abs(longitude);
            uint ilon = (uint)plon;
            uint flon = (uint)((plon - ilon + 0.00005) * 10000);
            ilat ^= ilon;
            ilat %= 42;
            return (ilat * 100000000) + (flat * 10000) + flon;
        }

        /// <summary>
        /// Построение ключа для пары хэш-значений точек
        /// </summary>
        /// <param name="hash1">Хэш 1</param>
        /// <param name="hash2">Хэш 2</param>
        /// <returns>Ключ</returns>
        private static ulong GetKey(uint hash1, uint hash2)
        {
            return (ulong)hash1 << 32 | hash2;
        }

        /// <summary>
        /// Удаление устаревших данных и 
        /// наиболее старых данных сверх
        /// заданного размера на момент вызова
        /// </summary>
        /// <returns></returns>
        public int Refresh()
        {
            lock (syncRoot)
            {
                // 1. Инициализация
                int rc = 1;
                LastException = null;
                int saveCount = Count;

                try
                {
                    // 2. Проверяем исходные данные
                    rc = 2;
                    if (!IsCreated)
                        return rc;

                    // 3. Удаляем устаревшие данные
                    rc = 3;
                    DateTime timeReceivedLimit = DateTime.Now.AddMinutes(-savingInterval);
                    int count = 0;

                    for (int i = 0; i < vehicleGeoData.Length; i++)
                    {
                        Dictionary<ulong, GeoCacheItem> currentVehicleCollection = vehicleGeoData[i];
                        Dictionary<ulong, GeoCacheItem> newVehicleCollection = new Dictionary<ulong, GeoCacheItem>(currentVehicleCollection.Count);

                        foreach (KeyValuePair<ulong, GeoCacheItem> kvp in currentVehicleCollection)
                        {
                            if (kvp.Value.TimeReceived > timeReceivedLimit)
                            {
                                newVehicleCollection.Add(kvp.Key, kvp.Value);
                            }
                        }

                        count += newVehicleCollection.Count;

                        if (newVehicleCollection.Count >= currentVehicleCollection.Count)
                        { newVehicleCollection = null; }
                        else
                        {
                            vehicleGeoData[i] = newVehicleCollection;
                            currentVehicleCollection = null;
                        }
                    }

                    // 4. Если емкость кэша переполнена
                    rc = 4;
                    if (count > capacity)
                    {
                        // 4.1 Выбираем время получения всех элементов кэша
                        rc = 41;
                        int removeCount = capacity - count;
                        GeoCacheItemInfo[] itemInfo = new GeoCacheItemInfo[count];
                        DateTime[] receivedTime = new DateTime[count];
                        count = 0;

                        for (int i = 0; i < vehicleGeoData.Length; i++)
                        {
                            Dictionary<ulong, GeoCacheItem> vehicleCollection = vehicleGeoData[i];

                            foreach (KeyValuePair<ulong, GeoCacheItem> kvp in vehicleCollection)
                            {
                                itemInfo[count].VehicleTypeIndex = i;
                                itemInfo[count].Key = kvp.Key;
                                receivedTime[count++] = kvp.Value.TimeReceived;
                            }
                        }

                        // 4.2 Сортируем по возрастанию времени
                        rc = 42;
                        Array.Sort(receivedTime, itemInfo);

                        // 4.3 Удаляем требуемое число элементов
                        rc = 43;

                        for (int i = 0; i < removeCount; i++)
                        {
                            GeoCacheItemInfo item = itemInfo[i];
                            vehicleGeoData[item.VehicleTypeIndex].Remove(item.Key);
                        }

                        itemInfo = null;
                        receivedTime = null;
                    }

                    // 5. Выход - Ok
                    rc = 0;
                    return rc;
                }
                catch (Exception ex)
                {
                    LastException = ex;
                    Logger.WriteToLog(669, MessageSeverity.Error, string.Format(Messages.MSG_669, $"{nameof(GeoCache)}.{nameof(this.Refresh)}", rc, (ex.InnerException == null ? ex.Message : ex.InnerException.Message)));
                    return rc;
                }
                finally
                {
                    Logger.WriteToLog(75, MessageSeverity.Info, string.Format(Messages.MSG_075, saveCount, Count));
                }
            }
        }

        /// <summary>
        /// Запрос попарных расстояний и времени движения между точками
        /// в прямом и обратном направлениях
        /// </summary>
        /// <param name="points">Координаты точек</param>
        /// <param name="yandexTypeIndex">Идекс способа передвижения</param>
        /// <param name="dataTable">Таблица результата</param>
        /// <returns>0 - таблица построена; иначе - таблица не построена</returns>
        public int GetPointsDataTable(uint[] pointHashes, int yandexTypeIndex, out Point[,] dataTable)
        {
            lock (syncRoot)
            {
                // 1. Инициализация
                int rc = 1;
                dataTable = null;

                try
                {
                    // 2. Проверяем исходные данные
                    rc = 2;
                    if (!IsCreated)
                        return rc;
                    if (yandexTypeIndex < 0 || yandexTypeIndex >= vehicleGeoData.Length)
                        return rc;
                    if (pointHashes == null || pointHashes.Length <= 1)
                        return rc;

                    // 3. Заполняем таблицу результата
                    rc = 3;
                    int n = pointHashes.Length;
                    dataTable = new Point[n, n];
                    Dictionary<ulong, GeoCacheItem> vehicleData = vehicleGeoData[yandexTypeIndex];

                    for (int i = 0; i < n; i++)
                    {
                        uint hash1 = pointHashes[i];

                        for (int j = i + 1; j < n; j++)
                        {
                            uint hash2 = pointHashes[j];

                            if (hash1 != hash2)
                            {
                                GeoCacheItem item;
                                if (vehicleData.TryGetValue(GetKey(hash1, hash2), out item))
                                {
                                    dataTable[i, j].X = item.Distance;
                                    dataTable[i, j].Y = item.Duration;
                                }
                                else
                                {
                                    dataTable[i, j].X = int.MinValue;
                                    dataTable[i, j].Y = int.MinValue;
                                }

                                if (vehicleData.TryGetValue(GetKey(hash2, hash1), out item))
                                {
                                    dataTable[j, i].X = item.Distance;
                                    dataTable[j, i].Y = item.Duration;
                                }
                                else
                                {
                                    dataTable[j, i].X = int.MinValue;
                                    dataTable[j, i].Y = int.MinValue;
                                }
                            }
                        }
                    }

                    // 4. Выход - Ok
                    rc = 0;
                    return rc;
                }
                catch (Exception ex)
                {
                    LastException = ex;
                    Logger.WriteToLog(669, MessageSeverity.Error, string.Format(Messages.MSG_669, $"{nameof(GeoCache)}.{nameof(this.GetPointsDataTable)}", rc, (ex.InnerException == null ? ex.Message : ex.InnerException.Message)));
                    return rc;
                }
            }
        }

        /// <summary>
        /// Сохранение данных в гео-кэше
        /// </summary>
        /// <param name="timeReceived">Временная ометка</param>
        /// <param name="origins">Координаты исходных точек</param>
        /// <param name="destinations">Координаты точек назначения</param>
        /// <param name="yandexTypeIndex">Идекс способа передвижения</param>
        /// <param name="dataTable">Таблица с гео-данными</param>
        /// <returns>0 - данные сохранены; иначе - данные не сохранены</returns>
        public int PutGeoData(DateTime timeReceived, GeoPoint[] origins, GeoPoint[] destinations, int yandexTypeIndex, Point[,,] dataTable)
        {
            lock (syncRoot)
            {
                // 1. Инициализация
                int rc = 1;

                try
                {
                    // 2. Проверяем исходные данные
                    rc = 2;
                    if (!IsCreated)
                        return rc;
                    if (yandexTypeIndex < 0 || yandexTypeIndex >= vehicleGeoData.Length)
                        return rc;
                    if (origins == null || origins.Length <= 0)
                        return rc;
                    if (destinations == null || destinations.Length <= 0)
                        return rc;
                    if (dataTable == null ||
                        dataTable.GetLength(0) != origins.Length ||
                        dataTable.GetLength(1) != destinations.Length ||
                        dataTable.GetLength(2) != 1)
                        return rc;

                    // 3. Строим hash для координат
                    rc = 3;
                    int m = origins.Length;
                    int n = destinations.Length;
                    uint[] originHashes = new uint[m];
                    uint[] destinationHashes = new uint[n];

                    for (int i = 0; i < m; i++)
                    {
                        originHashes[i] = GetCoordinateHash(origins[i].Latitude, origins[i].Longitude);
                    }

                    for (int i = 0; i < n; i++)
                    {
                        destinationHashes[i] = GetCoordinateHash(destinations[i].Latitude, destinations[i].Longitude);
                    }

                    // 4. Сохраняем переданные данные
                    rc = 4;
                    Dictionary<ulong, GeoCacheItem> vehicleData = vehicleGeoData[yandexTypeIndex];

                    for (int i = 0; i < m; i++)
                    {
                        uint hash1 = originHashes[i];

                        for (int j = 0; j < n; j++)
                        {
                            uint hash2 = destinationHashes[j];
                            if (hash1 != hash2)
                            {
                                Point pt = dataTable[i, j, 0];
                                if (pt.X >= 0)
                                {
                                    GeoCacheItem item;
                                    ulong key = GetKey(hash1, hash2);
                                    if (vehicleData.TryGetValue(key, out item))
                                    {
                                        item.SetData(timeReceived, pt.X, pt.Y);
                                    }
                                    else
                                    {
                                        vehicleData.Add(key, new GeoCacheItem(timeReceived, pt.X, pt.Y));
                                    }
                                }
                            }
                        }
                    }

                    // 5. Выход - Ok
                    rc = 0;
                    return rc;
                }
                catch (Exception ex)
                {
                    LastException = ex;
                    Logger.WriteToLog(669, MessageSeverity.Error, string.Format(Messages.MSG_669, $"{nameof(GeoCache)}.{nameof(this.PutGeoData)}", rc, (ex.InnerException == null ? ex.Message : ex.InnerException.Message)));
                    return rc;
                }
            }
        }

        /// <summary>
        /// Построение хэш-значений координат
        /// </summary>
        /// <param name="points">Координаты точек</param>
        /// <returns>Хэш-значения координат</returns>
        public static uint[] GetPointHashes(GeoPoint[] points)
        {
            // 1. Инициализация

            try
            {
                // 2. Проверяем исходные данные
                if (points == null || points.Length <= 0)
                    return new uint[0];

                // 3. Строим hash для координат
                uint[] pointHashes = new uint[points.Length];

                for (int i = 0; i < points.Length; i++)
                {
                    pointHashes[i] = GetCoordinateHash(points[i].Latitude, points[i].Longitude);
                }

                // 5. Выход - Ok
                return pointHashes;
            }
            catch
            { return new uint[0]; }
        }
    }
}
