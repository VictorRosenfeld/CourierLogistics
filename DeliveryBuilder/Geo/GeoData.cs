
namespace DeliveryBuilder.Geo
{
    using DeliveryBuilder.BuilderParameters;
    using DeliveryBuilder.Geo.Cache;
    using DeliveryBuilder.Geo.Yandex;
    using DeliveryBuilder.Log;
    using DeliveryBuilder.Orders;
    using DeliveryBuilder.Shops;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

    /// <summary>
    /// Получение гео-данных
    /// </summary>
    public class GeoData
    {
        /// <summary>
        /// Флаг: true - экземпляр создан; false - экземпляр не создан
        /// </summary>
        public bool IsCreated { get; private set; }

        /// <summary>
        /// Последнее прерывание
        /// </summary>
        public Exception LastException { get; private set; }

        /// <summary>
        /// Гео-кэш
        /// </summary>
        private GeoCache cache;

        /// <summary>
        /// Гео-кэш
        /// </summary>
        public GeoCache Cache => cache;

        /// <summary>
        /// Yandex-гео
        /// </summary>
        private GeoYandex yandex;

        /// <summary>
        /// Yandex-гео
        /// </summary>
        public GeoYandex Yandex => yandex;

        /// <summary>
        /// Идентификаторы способов передвижения
        /// </summary>
        private int[] yandexTypeIds;

        /// <summary>
        /// Имена способов передвижения
        /// </summary>
        private string[] yandexTypeNames;

        /// <summary>
        /// Идентификаторы способов передвижения
        /// </summary>
        private int[] YandexTypeIds => yandexTypeIds;

        /// <summary>
        /// Имена способов передвижения
        /// </summary>
        public string[] YandexTypeNames => yandexTypeNames;

        /// <summary>
        /// Кэффициент пресчета времени 'walking' в 'cycling':
        ///   cycling_duration = cyclingRatio * walking_duration
        /// </summary>
        private double cyclingRatio;

        /// <summary>
        /// Кэффициент пресчета времени 'walking' в 'cycling':
        ///   cycling_duration = cyclingRatio * walking_duration
        /// </summary>
        public double CyclingRatio => cyclingRatio;

        /// <summary>
        /// Наименование режима 'walking'
        /// </summary>
        private const string WalkingMode = "walking";

        /// <summary>
        /// Наименование режима 'cycling'
        /// </summary>
        private const string CyclingMode = "cycling";

        /// <summary>
        /// Наименование режима 'driving'
        /// </summary>
        private const string DrivingMode = "driving";

        /// <summary>
        /// Индекс способа доставки 'walking'
        /// </summary>
        private int walkingIndex;

        /// <summary>
        /// Индекс способа доставки 'cycling'
        /// </summary>
        private int cyclingIndex;

        /// <summary>
        /// Индекс способа доставки 'driving'
        /// </summary>
        private int drivingIndex;

        /// <summary>
        /// Создание экземпляра GeoData
        /// </summary>
        /// <param name="config">Ппараметры</param>
        /// <returns>0 - экземпляр создан; иначе - экземпляр не создан</returns>
        public int Create(BuilderConfig config)
        {
            // 1. Инициализация
            int rc = 1;
            IsCreated = false;
            LastException = null;
            cache = null;
            yandex = null;
            yandexTypeNames = null;
            cyclingRatio = 0;
            walkingIndex = -1;
            cyclingIndex = -1;
            drivingIndex = -1;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (config == null || config.Parameters == null ||
                    config.Parameters.GeoCache == null ||
                    config.Parameters.GeoYandex == null ||
                    config.Parameters.GeoYandex.TypeNames == null || config.Parameters.GeoYandex.TypeNames.Length <= 0 ||
                    config.Parameters.GeoYandex.CyclingRatio <= 0 || config.Parameters.GeoYandex.CyclingRatio > 1)
                    return rc;

                // 3. Извлекаем параметры для дальнейшей работы
                rc = 3;
                yandexTypeNames = new string[config.Parameters.GeoYandex.TypeNames.Length];
                yandexTypeIds = new int[yandexTypeNames.Length];
                for (int i = 0; i < yandexTypeNames.Length; i++)
                {
                    yandexTypeNames[i] = config.Parameters.GeoYandex.TypeNames[i].Name;
                    yandexTypeIds[i] = config.Parameters.GeoYandex.TypeNames[i].Id;
                }

                Array.Sort(yandexTypeIds, yandexTypeNames);

                cyclingRatio =  config.Parameters.GeoYandex.CyclingRatio;

                for (int i = 0; i < yandexTypeNames.Length; i++)
                {
                    string typeName = yandexTypeNames[i];
                    if (WalkingMode.Equals(typeName, StringComparison.CurrentCultureIgnoreCase))
                    {
                        walkingIndex = i;
                    }
                    else if (CyclingMode.Equals(typeName, StringComparison.CurrentCultureIgnoreCase))
                    {
                        cyclingIndex = i;
                    }
                    else if (DrivingMode.Equals(typeName, StringComparison.CurrentCultureIgnoreCase))
                    {
                        drivingIndex = i;
                    }
                }

                if (walkingIndex < 0)
                {
                    Logger.WriteToLog(10, MessageSeverity.Error, Messages.MSG_010);
                    return rc;
                }

                if (cyclingIndex < 0)
                {
                    Logger.WriteToLog(11, MessageSeverity.Error, Messages.MSG_011);
                    return rc;
                }

                if (drivingIndex < 0)
                {
                    Logger.WriteToLog(12, MessageSeverity.Error, Messages.MSG_012);
                    return rc;
                }

                // 4. Создаём Geo Cache
                rc = 4;
                cache = new GeoCache();
                int rc1 = cache.Create(config.Id, config.Parameters.GeoCache, yandexTypeNames.Length);
                if (rc1 != 0)
                    return rc = 1000 * rc + rc1;

                // 5. Создаём Geo Yandex
                rc = 5;
                yandex = new GeoYandex();
                rc1 = yandex.Create(config.Parameters.GeoYandex);
                if (rc1 != 0)
                    return rc = 1000 * rc + rc1;

                // 6. Выход
                rc = 0;
                IsCreated = true;
                return rc;
            }
            catch (Exception ex)
            {
                LastException = ex;
                Logger.WriteToLog(669, MessageSeverity.Error, string.Format(Messages.MSG_669, $"{nameof(GeoData)}.{nameof(this.Create)}", rc, (ex.InnerException == null ? ex.Message : ex.InnerException.Message)));
                return rc;
            }
        }

        /// <summary>
        /// Запрос гео-данных в прямом и обратном направлениях
        /// </summary>
        /// <param name="yandexTypeId">Id способа доставки</param>
        /// <param name="points">Точки, для пар которых запрашиваются гео-данные</param>
        /// <param name="geoData">Результат запроса</param>
        /// <returns>0 - данные получены; иначе - данные не получены</returns>
        public int GetData(int yandexTypeId, GeoPoint[] points, out Point[,] geoData)
        {
            // 1. Иициализация
            int rc = 1;
            Stopwatch sw = new Stopwatch();
            sw.Start();
            LastException = null;
            geoData = null;
            Logger.WriteToLog(96, MessageSeverity.Info, string.Format(Messages.MSG_096, rc, yandexTypeId, (points == null ? 0 : points.Length)));

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (!IsCreated)
                    return rc;
                if (points == null || points.Length <= 1)
                    return rc;

                int yandexTypeIndex = Array.BinarySearch(yandexTypeIds, yandexTypeId);
                if (yandexTypeIndex < 0)
                {
                    Logger.WriteToLog(13, MessageSeverity.Warn, string.Format(Messages.MSG_013, yandexTypeId));
                    return rc;
                }

                // 3. Округляем значения координат
                rc = 3;
                GeoPoint[] roundedPoints = new GeoPoint[points.Length];
                for (int i = 0; i < points.Length; i++)
                {
                    roundedPoints[i] = RoundCoordinate(points[i]);
                }

                // 4. Запрашиваем хэши округленных точек
                rc = 4;
                uint[] pointHashes = GeoCache.GetPointHashes(roundedPoints);

                // 5. Отбираем точки с разными хэш-значениями
                rc = 5;
                Dictionary<uint, int> pointHashIndex = new Dictionary<uint, int>(pointHashes.Length);
                int[] pointIndex = new int[pointHashes.Length];
                GeoPoint[] roundedPointsSel;

                for (int i = 0; i < pointHashes.Length; i++)
                {
                    int index;
                    if (pointHashIndex.TryGetValue(pointHashes[i], out index))
                    {
                        pointIndex[i] = index;
                    }
                    else
                    {
                        index = pointHashIndex.Count;
                        pointHashIndex.Add(pointHashes[i], index);
                        pointIndex[i] = index;
                    }
                }

                if (pointHashIndex.Count < pointHashes.Length)
                {
                    Array.Resize(ref pointHashes, pointHashIndex.Count);
                    pointHashIndex.Keys.CopyTo(pointHashes, 0);

                    roundedPointsSel = new GeoPoint[pointHashes.Length];
                    for (int i = 0; i < pointIndex.Length; i++)
                    {
                        roundedPointsSel[pointIndex[i]] = roundedPoints[i];
                    }
                }
                else
                {
                    roundedPointsSel = roundedPoints;
                }

                // 6. Выбираем данные, имеющиеся в кэше 
                rc = 6;
                Point[,] data;
                int rc1 = cache.GetPointsDataTable(pointHashes, yandexTypeIndex, out data);
                if (rc1 != 0)
                    return rc = 1000 * rc + rc1;

                // 7. Отбираем неизвестные данные в прямом направлении
                rc = 7;
                bool[] originChecked = new bool[pointHashes.Length];
                bool[] destinationChecked = new bool[pointHashes.Length];
                GeoPoint[] originsSelected = new GeoPoint[pointHashes.Length];
                GeoPoint[] destinationSelected = new GeoPoint[pointHashes.Length];
                int[] originIndex = new int[pointHashes.Length];
                int[] destinationIndex = new int[pointHashes.Length];
                int originCount = 0;
                int destinationCount = 0;

                for (int i = 0; i < pointHashes.Length; i++)
                {
                    for (int j = i + 1; j < pointHashes.Length; j++)
                    {
                        if (data[i, j].X < 0)
                        {
                            if (!originChecked[i])
                            {
                                originChecked[i] = true;
                                originIndex[originCount] = i;
                                originsSelected[originCount++] = roundedPointsSel[i];
                            }
                            if (!destinationChecked[j])
                            {
                                destinationChecked[j] = true;
                                destinationIndex[destinationCount] = j;
                                destinationSelected[destinationCount++] = roundedPointsSel[j];
                            }
                        }
                    }
                }

                if (originCount > 0 && destinationCount > 0)
                {
                    // 8. Запрашиваем неизвестные данные в прямом направлении
                    rc = 8;
                    GeoPoint[] origins = new GeoPoint[originCount];
                    GeoPoint[] destinations = new GeoPoint[destinationCount];
                    Array.Copy(originsSelected, origins, originCount);
                    Array.Copy(destinationSelected, destinations, destinationCount);

                    int yandexIndex = (yandexTypeIndex == cyclingIndex ? walkingIndex : yandexTypeIndex);
                    string mode = yandexTypeNames[yandexIndex];
                    GeoYandexRequest request = new GeoYandexRequest(new string[] { mode }, origins, destinations);
                    rc1 = yandex.Request(request);
                    if (rc1 != 0)
                        return rc = 1000 * rc + rc1;

                    DateTime now = DateTime.Now;

                    // 9. Сохраняем полученые данные в гео-кэше
                    rc = 9;
                    Point[,,] result = request.Result;
                    rc1 = cache.PutGeoData(now, origins, destinations, yandexIndex, result);

                    if (yandexTypeIndex == cyclingIndex)
                    {
                        for (int i = 0; i < originCount; i++)
                        {
                            for (int j = 0; j < destinationCount; j++)
                            {
                                int duration = result[i, j, 0].Y;
                                if (duration > 0)
                                {
                                    duration = (int)(cyclingRatio * duration + 0.5);
                                    result[i, j, 0].Y = duration;
                                }
                            }
                        }

                        rc1 = cache.PutGeoData(now, origins, destinations, yandexTypeIndex, result);
                    }

                    // 10. Переносим данные из result в data
                    rc = 10;
                    for (int i = 0; i < originCount; i++)
                    {
                        int ii = originIndex[i];

                        for (int j = 0; j < destinationCount; j++)
                        {
                            int jj = destinationIndex[j];
                            if (ii != jj)
                            {
                                Point pt = result[i, j, 0];
                                if (pt.X >= 0)
                                {
                                    data[ii, jj] = pt;
                                }
                            }
                        }
                    }
                }

                // 11. Отбираем неизвестные данные в обратном направлении
                rc = 11;
                Array.Clear(originChecked, 0, originChecked.Length);
                Array.Clear(originChecked, 0, destinationChecked.Length);
                originCount = 0;
                destinationCount = 0;

                for (int i = pointHashes.Length - 1; i >= 0; i--)
                {
                    for (int j = i - 1; j >= 0; j--)
                    {
                        if (data[i, j].X < 0)
                        {
                            if (!originChecked[i])
                            {
                                originChecked[i] = true;
                                originIndex[originCount] = i;
                                originsSelected[originCount++] = roundedPointsSel[i];
                            }
                            if (!destinationChecked[j])
                            {
                                destinationChecked[j] = true;
                                destinationIndex[destinationCount] = j;
                                destinationSelected[destinationCount++] = roundedPointsSel[j];
                            }
                        }
                    }
                }

                if (originCount > 0 && destinationCount > 0)
                {
                    // 12. Запрашиваем неизвестные данные в обратном направлении
                    rc = 12;
                    GeoPoint[] origins = new GeoPoint[originCount];
                    GeoPoint[] destinations = new GeoPoint[destinationCount];
                    Array.Copy(originsSelected, origins, originCount);
                    Array.Copy(destinationSelected, destinations, destinationCount);

                    int yandexIndex = (yandexTypeIndex == cyclingIndex ? walkingIndex : yandexTypeIndex);
                    string mode = yandexTypeNames[yandexIndex];
                    GeoYandexRequest request = new GeoYandexRequest(new string[] { mode }, origins, destinations);
                    rc1 = yandex.Request(request);
                    if (rc1 != 0)
                        return rc = 1000 * rc + rc1;

                    DateTime now = DateTime.Now;

                    // 13. Сохраняем полученые данные в гео-кэше
                    rc = 13;
                    Point[,,] result = request.Result;
                    rc1 = cache.PutGeoData(now, origins, destinations, yandexIndex, result);

                    if (yandexTypeIndex == cyclingIndex)
                    {
                        for (int i = 0; i < originCount; i++)
                        {
                            for (int j = 0; j < destinationCount; j++)
                            {
                                int duration = result[i, j, 0].Y;
                                if (duration > 0)
                                {
                                    duration = (int)(cyclingRatio * duration + 0.5);
                                    result[i, j, 0].Y = duration;
                                }
                            }
                        }

                        rc1 = cache.PutGeoData(now, origins, destinations, yandexTypeIndex, result);
                    }

                    // 14. Переносим данные из result в data
                    rc = 14;
                    for (int i = 0; i < originCount; i++)
                    {
                        int ii = originIndex[i];

                        for (int j = 0; j < destinationCount; j++)
                        {
                            int jj = destinationIndex[j];
                            if (ii != jj)
                            {
                                Point pt = result[i, j, 0];
                                if (pt.X >= 0)
                                {
                                    data[ii, jj] = pt;
                                }
                            }
                        }
                    }
                }

                // 15. Финальный перенос из data в geoData
                rc = 15;
                if (pointHashIndex.Count >= points.Length)
                {
                    geoData = data;
                }
                else
                {
                    geoData = new Point[points.Length, points.Length];
                    for (int i = 0; i < points.Length; i++)
                    {
                        int ii = pointIndex[i];

                        for (int j = i + 1; j < points.Length; j++)
                        {
                            int jj = pointIndex[j];

                            if (ii != jj)
                            {
                                geoData[i, j] = data[ii, jj];
                                geoData[j, i] = data[jj, ii];
                            }
                        }
                    }
                }

                // 16. Выход - Ok 
                rc = 0;
                return rc;
            }
            catch (Exception ex)
            {
                LastException = ex;
                Logger.WriteToLog(669, MessageSeverity.Error, string.Format(Messages.MSG_669, $"{nameof(GeoData)}.{nameof(this.GetData)}", rc, (ex.InnerException == null ? ex.Message : ex.InnerException.Message)));
                return rc;
            }
            finally
            {
                Logger.WriteToLog(97, MessageSeverity.Info, string.Format(Messages.MSG_097, rc, yandexTypeId, (points == null ? 0 : points.Length), sw.ElapsedMilliseconds));
            }
        }

        /// <summary>
        /// Запрос гео-даных для пары точек
        /// </summary>
        /// <param name="yandexTypeId">Id способа доставки</param>
        /// <param name="point1">Координаты точки 1</param>
        /// <param name="point2">Координаты точки 2</param>
        /// <param name="geoData12">Гео-данные для point1 -- point2</param>
        /// <param name="geoData21">Гео-данные для point2 -- point1</param>
        /// <returns>0 - данные получены; иначе - даные не получены</returns>
        public int GetData(int yandexTypeId, GeoPoint point1, GeoPoint point2, out Point geoData12, out Point geoData21)
        {
            // 1. Иициализация
            int rc = 1;
            geoData12 = new Point(0, 0);
            geoData21 = new Point(0, 0);

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (!IsCreated)
                    return rc;

                int yandexTypeIndex = Array.BinarySearch(yandexTypeIds, yandexTypeId);
                if (yandexTypeIndex < 0)
                {
                    Logger.WriteToLog(13, MessageSeverity.Warn, string.Format(Messages.MSG_013, yandexTypeId));
                    return rc;
                }

                // 3. Округляем значения координат
                rc = 3;
                GeoPoint[] roundedPoints = new GeoPoint[2];
                roundedPoints[0] = RoundCoordinate(point1);
                roundedPoints[1] = RoundCoordinate(point2);

                // 4. Запрашиваем хэши округленных точек
                rc = 4;
                uint[] pointHashes = GeoCache.GetPointHashes(roundedPoints);
                if (pointHashes[0] == pointHashes[1])
                    return rc = 0;

                // 5. Выбираем данные, имеющиеся в кэше 
                rc = 5;
                Point[,] data;
                int rc1 = cache.GetPointsDataTable(pointHashes, yandexTypeIndex, out data);
                if (rc1 != 0)
                    return rc = 1000 * rc + rc1;

                // 6. Запрашиваем недостающие данные
                rc = 6;
                int yandexIndex = (yandexTypeIndex == cyclingIndex ? walkingIndex : yandexTypeIndex);
                string mode = yandexTypeNames[yandexIndex];

                if (data[0, 1].X < 0)
                {
                    GeoPoint[] origins = new GeoPoint[] { point1 };
                    GeoPoint[] destinations = new GeoPoint[] { point2 };

                    GeoYandexRequest request = new GeoYandexRequest(new string[] { mode }, origins, destinations);
                    rc1 = yandex.Request(request);
                    if (rc1 != 0)
                        return rc = 1000 * rc + rc1;

                    DateTime now = DateTime.Now;
                    Point[,,] result = request.Result;

                    rc1 = cache.PutGeoData(now, origins, destinations, yandexIndex, result);
                    if (yandexTypeIndex == cyclingIndex)
                    {
                        int duration = result[0, 1, 0].Y;
                        if (duration > 0)
                        {
                            duration = (int)(cyclingRatio * duration + 0.5);
                            result[0, 1, 0].Y = duration;
                            data[0, 1].Y = duration;
                            rc1 = cache.PutGeoData(now, origins, destinations, yandexTypeIndex, result);
                        }
                    }
                }

                if (data[1, 0].X < 0)
                {
                    GeoPoint[] origins = new GeoPoint[] { point2 };
                    GeoPoint[] destinations = new GeoPoint[] { point1 };

                    GeoYandexRequest request = new GeoYandexRequest(new string[] { mode }, origins, destinations);
                    rc1 = yandex.Request(request);
                    if (rc1 != 0)
                        return rc = 1000 * rc + rc1;

                    DateTime now = DateTime.Now;
                    Point[,,] result = request.Result;

                    rc1 = cache.PutGeoData(now, origins, destinations, yandexIndex, result);
                    if (yandexTypeIndex == cyclingIndex)
                    {
                        int duration = result[0, 1, 0].Y;
                        if (duration > 0)
                        {
                            duration = (int)(cyclingRatio * duration + 0.5);
                            result[0, 1, 0].Y = duration;
                            data[1, 0].Y = duration;
                            rc1 = cache.PutGeoData(now, origins, destinations, yandexTypeIndex, result);
                        }
                    }
                }

                geoData12 = data[0, 1];
                geoData21 = data[1, 0];

                // 7. Выход - Ok
                rc = 0;
                return rc;
            }
            catch (Exception ex)
            {
                LastException = ex;
                Logger.WriteToLog(669, MessageSeverity.Error, string.Format(Messages.MSG_669, $"{nameof(GeoData)}.{nameof(this.GetData)}", rc, (ex.InnerException == null ? ex.Message : ex.InnerException.Message)));
                return rc;
            }
        }

        /// <summary>
        /// Выбор требуемых гео-данных
        /// </summary>
        /// <param name="yandexTypeId">ID способа передвижения Yandex</param>
        /// <param name="shop">Магазин</param>
        /// <param name="orders">Заказы</param>
        /// <param name="geoData">Гео-данные
        /// (Индексы точек: i - orders[i]; i = orders.Length - shop)
        /// </param>
        /// <returns>0 - гео-данные выбраны; гео-данные не выбраны</returns>
        public int Select(int yandexTypeId, Shop shop, Order[] orders, out Point[,] geoData)
        {
            // 1. Инициализация
            int rc = 1;
            geoData = null;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (!IsCreated)
                    return rc;
                if (shop == null || shop.Latitude == 0 || shop.Longitude == 0)
                    return rc;
                if (orders == null || orders.Length <= 0)
                    return rc;

                // 3. Выбираем координаты
                rc = 3;
                GeoPoint[] points = new GeoPoint[orders.Length + 1];
                for (int i = 0; i < orders.Length; i++)
                {
                    points[i] = new GeoPoint(orders[i].Latitude, orders[i].Longitude);
                }
                points[orders.Length] = new GeoPoint(shop.Latitude, shop.Longitude);

                // 4. Возвращаем результат
                rc = 4;
                return GetData(yandexTypeId, points, out geoData);
            }
            catch (Exception ex)
            {
                LastException = ex;
                Logger.WriteToLog(669, MessageSeverity.Error, string.Format(Messages.MSG_669, $"{nameof(GeoData)}.{nameof(this.Select)}", rc, (ex.InnerException == null ? ex.Message : ex.InnerException.Message)));
                return rc;
            }
        }

        /// <summary>
        /// Округление координат до 4-х знаков после запятой 
        /// </summary>
        /// <param name="point">Исходная точка</param>
        /// <returns>округленная точка</returns>
        public static GeoPoint RoundCoordinate(GeoPoint point)
        {
            return new GeoPoint(Math.Round(point.Latitude, 4), Math.Round(point.Longitude, 4));
        }
    }
}
