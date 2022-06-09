
namespace DeliveryBuilder.Geo.Yandex
{
    using DeliveryBuilder.BuilderParameters;
    using DeliveryBuilder.Log;
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Net;
    using System.Text;
    using System.Threading;

    /// <summary>
    /// Получение гео-данных от Yandex
    /// </summary>
    public class GeoYandex
    {
        #region Способы передвижения

        /// <summary>
        /// Способ передвжения - на авто
        /// </summary>
        private const string MODE_DRIVING = "driving";

        /// <summary>
        /// Способ передвжения - на велосипеде
        /// </summary>
        private const string MODE_CYCLING = "cycling";

        /// <summary>
        /// Способ передвжения - пешком
        /// </summary>
        private const string MODE_WALKING = "walking";

        /// <summary>
        /// Способ передвжения - на общественном транспорте
        /// </summary>
        private const string MODE_TRANSIT = "transit";

        /// <summary>
        /// Способ передвжения - грузовик
        /// </summary>
        private const string MODE_TRUCK = "truck";

        #endregion Способы передвижения

        /// <summary>
        /// User Agent для Http-запроса
        /// </summary>
        private const string USER_AGENT = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/74.0.3729.169 Safari/537.36";

        /// <summary>
        /// Флаг: true - экземпляр создан; false - экземпляр не создан
        /// </summary>
        public bool IsCreated { get; private set; }

        /// <summary>
        /// Последнее прерывание
        /// </summary>
        public Exception LastException { get; private set; }

        /// <summary>
        /// API Key
        /// </summary>
        private string apiKey;

        /// <summary>
        /// GET-запрос
        /// </summary>
        private string url;

        /// <summary>
        /// Максимальное количество пар точек в одно запросе
        /// </summary>
        private int pairLimit;

        /// <summary>
        /// Timeout для отклика, мсек
        /// </summary>
        private int responseTimeout;

        /// <summary>
        /// API Key
        /// </summary>
        public string ApiKey => apiKey;

        /// <summary>
        /// GET-запрос
        /// </summary>
        public string Url => url;

        /// <summary>
        /// Максимальное количество пар точек в одно запросе
        /// </summary>
        public int PairLimit => pairLimit;

        /// <summary>
        /// Timeout для отклика, мсек
        /// </summary>
        public int ResponseTimeout => responseTimeout;

        /// <summary>
        /// Создание экземпляра GeoYandex
        /// </summary>
        /// <param name="parameters">Параметры</param>
        /// <returns>0 - экземпляр создан; иначе - экземпляр не создан</returns>
        public int Create(GeoYandexParameters parameters)
        {
            // 1. Инициализация
            int rc = 1;
            IsCreated = false;
            LastException = null;
            apiKey = null;
            url = null;
            pairLimit = 0;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (parameters == null ||
                    string.IsNullOrWhiteSpace(parameters.ApiKey) ||
                    string.IsNullOrWhiteSpace(parameters.Url) ||
                    parameters.PairLimit <= 0 ||
                    parameters.Timeout <= 0)
                    return rc;

                // 3. Сохраняем значение параметров
                rc = 3;
                apiKey = parameters.ApiKey;
                url = parameters.Url;
                pairLimit = parameters.PairLimit;
                responseTimeout = parameters.Timeout;

                // 4. Выход - Ok
                rc = 0;
                IsCreated = true;
                return rc;
            }
            catch (Exception ex)
            {
                LastException = ex;
                Logger.WriteToLog(669, MessageSeverity.Error, string.Format(Messages.MSG_669, $"{nameof(GeoYandex)}.{nameof(this.Create)}", rc, (ex.InnerException == null ? ex.Message : ex.InnerException.Message)));
                return rc;
            }
        }

        /// <summary>
        /// Запрос гео-данных Yandex
        /// </summary>
        /// <param name="request">Исходные данные запроса</param>
        /// <returns>0 - результат получен; иначе - результат не получен</returns>
        public int Request(GeoYandexRequest request)
        {
            // 1. Инициализация
            Stopwatch sw = new Stopwatch();
            sw.Start();
            int rc = 1;
            int rc1 = 1;
            request.Result = null;
            GeoYandexThreadContext[] contexts = null;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (!IsCreated)
                    return rc;
                if (request.Modes == null || request.Modes.Length <= 0)
                    return rc;
                if (request.Origins == null || request.Origins.Length <= 0)
                    return rc;
                if (request.Destinations == null || request.Destinations.Length <= 0)
                    return rc;

                Logger.WriteToLog(98, MessageSeverity.Info, string.Format(Messages.MSG_098, request.Modes.Length, request.Origins.Length, request.Destinations.Length));

                // 4. Строим контексты запросов
                rc = 4;
                GeoYandexRequestData[] requestData;
                rc1 = GetGeoContext(request, pairLimit, out requestData);
                if (rc1 != 0)
                    return rc = 10000 * rc + rc1;

                Point[,,] geoData = requestData[0].GeoData;

                // 5. Определяем число потоков
                rc = 5;
                int requestPerThread = 4;
                int threadCount = (requestData.Length + requestPerThread - 1) / requestPerThread;
                if (threadCount < 1)
                { threadCount = 1; }
                else if (threadCount > 8)
                { threadCount = 8; }

                // 6. Запускаем обработку и дожидаемся её завершения
                rc = 6;
                if (threadCount <= 1)
                {
                    GeoYandexThreadContext context = new GeoYandexThreadContext(url, apiKey, requestData, 0, 1, null, responseTimeout);
                    GeoThread(context);
                    rc1 = context.ExitCode;
                }
                else
                {
                    contexts = new GeoYandexThreadContext[threadCount];

                    for (int i = 0; i < threadCount; i++)
                    {
                        int m = i;
                        contexts[m] = new GeoYandexThreadContext(url, apiKey, requestData, m, threadCount, new ManualResetEvent(false), responseTimeout);
                        ThreadPool.QueueUserWorkItem(GeoThread, contexts[m]);
                    }

                    rc1 = 0;

                    for (int i = 0; i < threadCount; i++)
                    {
                        contexts[i].SyncEvent.WaitOne();
                        contexts[i].SyncEvent.Dispose();
                        contexts[i].SyncEvent = null;
                        if (contexts[i].ExitCode != 0)
                            rc1 = contexts[i].ExitCode;
                    }

                    contexts = null;
                }

                // 7. Если во время обработки произошли ошибки
                rc = 7;
                if (rc1 != 0)
                    return rc = 1000000 * rc + rc1;
                request.Result = geoData;

                // 8. Выход - Ok
                rc = 0;
                return rc;
            }
            catch (Exception ex)
            {
                Logger.WriteToLog(669, MessageSeverity.Error, string.Format(Messages.MSG_669, $"{nameof(GeoYandex)}.{nameof(this.Request)}", rc, (ex.InnerException == null ? ex.Message : ex.InnerException.Message)));
                return rc;
            }
            finally
            {
                if (contexts != null && contexts.Length > 0)
                {
                    for (int i = 0; i < contexts.Length; i++)
                    {
                        if (contexts[i] != null && contexts[i].SyncEvent != null)
                        {
                            contexts[i].SyncEvent.Dispose();
                            contexts[i].SyncEvent = null;
                            contexts[i] = null;
                        }
                    }
                }
                Logger.WriteToLog(99, MessageSeverity.Info, string.Format(Messages.MSG_099, rc, 
                    (request == null || request.Modes == null ? 0 : request.Modes.Length), 
                    (request == null || request.Origins == null ? 0 : request.Origins.Length), 
                    (request == null || request.Destinations == null ? 0 : request.Destinations.Length), 
                    sw.ElapsedMilliseconds));

            }
        }

        /// <summary>
        /// Построение контекстов запросов к Yandex
        /// </summary>
        /// <param name="requestArgs">Данные для запросов</param>
        /// <param name="pairLimit">Максимальное количество пар в одном запросе</param>
        /// <param name="geoContext">Результат - контексты запросов</param>
        /// <returns>0 - контексты построены; контексты не построены</returns>
        private static int GetGeoContext_old(GeoYandexRequest requestArgs, int pairLimit, out GeoYandexRequestData[] geoContext)
        {
            // 1. Инициализация
            int rc = 1;
            geoContext = null;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (pairLimit <= 0)
                    return rc;
                if (requestArgs.Origins == null || requestArgs.Origins.Length <= 0)
                    return rc;
                if (requestArgs.Destinations == null || requestArgs.Destinations.Length <= 0)
                    return rc;
                if (requestArgs.Modes == null || requestArgs.Modes.Length <= 0)
                    return rc;

                // 3. Выделяем память под результат
                rc = 5;
                GeoPoint[] origins = requestArgs.Origins;
                GeoPoint[] destinations = requestArgs.Destinations;
                string[] modes = requestArgs.Modes;
                int originCount = origins.Length;
                int destinationCount = destinations.Length;
                int modeCount = modes.Length;
                Point[,,] geoData = new Point[originCount, destinationCount, modeCount];

                // 6. Два частных случая - один или два запроса и два частных случая pairLimit = 1, 2
                rc = 6;
                int pairLimit2 = pairLimit + pairLimit;

                if (originCount * destinationCount <= pairLimit)
                {
                    geoContext = new GeoYandexRequestData[] { new GeoYandexRequestData(origins, 0, originCount, destinations, 0, destinationCount, modes, geoData) };
                    return rc = 0;
                }
                else if (originCount * destinationCount <= pairLimit2 && (originCount % 2) == 0)
                {
                    int length = originCount / 2;
                    geoContext = new GeoYandexRequestData[] {
                        new GeoYandexRequestData(origins, 0, length, destinations, 0, destinationCount, modes, geoData),
                        new GeoYandexRequestData(origins, length, originCount - length, destinations, 0, destinationCount, modes, geoData)
                        };
                    return rc = 0;
                }
                else if (originCount * destinationCount <= pairLimit2 && (destinationCount % 2) == 0)
                {
                    int length = destinationCount / 2;
                    geoContext = new GeoYandexRequestData[] {
                        new GeoYandexRequestData(origins, 0, originCount, destinations, 0, length, modes, geoData),
                        new GeoYandexRequestData(origins, 0, originCount, destinations, length, destinationCount - length, modes, geoData)
                        };
                    return rc = 0;
                }
                //else if (originCount * destinationCount <= pairLimit2)
                //{
                //    if ((originCount % 2) == 0)
                //    {
                //        int length = originCount / 2;
                //        geoContext = new GeoYandexRequestData[] {
                //        new GeoYandexRequestData(origins, 0, length, destinations, 0, destinationCount, modes, geoData),
                //        new GeoYandexRequestData(origins, length, originCount - length, destinations, 0, destinationCount, modes, geoData)
                //        };
                //    }
                //    else if ((destinationCount % 2) == 0)
                //    {
                //        int length = destinationCount / 2;
                //        geoContext = new GeoYandexRequestData[] {
                //        new GeoYandexRequestData(origins, 0, originCount, destinations, 0, length, modes, geoData),
                //        new GeoYandexRequestData(origins, 0, originCount, destinations, length, destinationCount - length, modes, geoData)
                //        };
                //    }
                //    else
                //    {
                //        int length = originCount / 2;
                //        geoContext = new GeoYandexRequestData[] {
                //        new GeoYandexRequestData(origins, 0, length, destinations, 0, destinationCount, modes, geoData),
                //        new GeoYandexRequestData(origins, length, originCount - length, destinations, 0, destinationCount, modes, geoData)
                //        };
                //    }

                //    return rc = 0;
                //}
                else if (pairLimit == 1)
                {
                    geoContext = new GeoYandexRequestData[originCount * destinationCount];
                    int cnt = 0;

                    for (int i = 0; i < originCount; i++)
                    {
                        for (int j = 0; j < destinationCount; j++)
                        {
                            geoContext[cnt++] = new GeoYandexRequestData(origins, i, 1, destinations, j, 1, modes, geoData);
                        }
                    }

                    return rc = 0;
                }
                else if (pairLimit == 2)
                {
                    geoContext = new GeoYandexRequestData[(originCount * destinationCount + 1) / 2];
                    int cnt = 0;

                    for (int i = 0; i < originCount; i++)
                    {
                        for (int j = 0; j < destinationCount - 1; j += 2)
                        {
                            geoContext[cnt++] = new GeoYandexRequestData(origins, i, 1, destinations, j, 2, modes, geoData);
                        }
                    }

                    if ((destinationCount % 2) != 0)
                    {
                        for (int i = 0; i < originCount - 1; i += 2)
                        {
                            geoContext[cnt++] = new GeoYandexRequestData(origins, i, 2, destinations, destinationCount - 1, 1, modes, geoData);
                        }

                        if ((originCount % 2) != 0)
                        {
                            geoContext[cnt++] = new GeoYandexRequestData(origins, originCount - 1, 1, destinations, destinationCount - 1, 1, modes, geoData);
                        }
                    }

                    return rc = 0;
                }

                // 7. Выделяем память под результат
                rc = 7;
                int size = (originCount * destinationCount + pairLimit - 1) / pairLimit + 2;
                GeoYandexRequestData[] result = new GeoYandexRequestData[size];
                int count = 0;

                // 8. Делим destination-точки на группы по pairLimit точек + остаток (от 0 до pairLimit - 1);
                rc = 8;
                int destinationGroupCount = destinationCount / pairLimit;
                int destinationRemainder = destinationCount % pairLimit;
                int destinationStartIndex = pairLimit * destinationGroupCount;

                if (destinationGroupCount > 0)
                {
                    //                          [origins]
                    //                              ↓
                    // destinations = [{pairLimit} ... {pairLimit} {destinationRemainder}]     (destinationRemainder < pairLimit)
                    //                                              ↑
                    //                                    (destinationStartIndex)

                    int lim = destinationGroupCount * pairLimit;
                    for (int i = 0; i < originCount; i++)
                    {
                        for (int j = 0; j < lim; j += pairLimit)
                        {
                            result[count++] = new GeoYandexRequestData(origins, i, 1, destinations, j, pairLimit, modes, geoData);
                        }
                    }
                }

                if (destinationRemainder == 0)
                    goto Fin;

                //                          [origins]
                //                              ↓
                //                    [destinationRemainder]     (0 < destinationRemainder < pairLimit)
                //                     ↑
                //           (destinationStartIndex)

                // 9. Делим origin-точки на группы по pairLimit точек + остаток (от 0 до pairLimit - 1);
                rc = 9;
                int originGroupCount = originCount / pairLimit;
                int originRemainder = originCount % pairLimit;
                int originStartIndex = pairLimit * originGroupCount;

                if (originGroupCount > 0)
                {
                    // origins = [{pairLimit} ... {pairLimit} {originRemainder}]     (originRemainder < pairLimit)
                    //                              ↓
                    //      destinations = [destinationRemainder]                    (0 < destinationRemainder < pairLimit)
                    //                      ↑
                    //            (destinationStartIndex)

                    int lim = originGroupCount * pairLimit;
                    for (int i = destinationStartIndex; i < destinationCount; i++)
                    {
                        for (int j = 0; j < lim; j += pairLimit)
                        {
                            result[count++] = new GeoYandexRequestData(origins, j, pairLimit, destinations, i, 1, modes, geoData);
                        }
                    }
                }

                if (originRemainder == 0)
                    goto Fin;

                //                   (originStartIndex)
                //                           ↓
                //                          [originRemainder]        (0 < originRemainder < pairLimit)
                //                                 ↓
                //                       [destinationRemainder]     (0 < destinationRemainder < pairLimit)
                //                        ↑
                //              (destinationStartIndex)

                // 10. Находим все пары множителей x, y такие, что
                //     0 ≤ pairLimit - x * y ≤ 1 (y ≥ x > 0)
                rc = 10;
                Point[] multiplierPairs = GetMultiplierPairs(pairLimit, 1);
                if (multiplierPairs == null || multiplierPairs.Length <= 0)
                    return rc;

                // 11. Цикл дальнейшего построения
                rc = 11;
                GeoYandexIterationData[] iterationData = new GeoYandexIterationData[originRemainder * destinationRemainder];
                iterationData[0] = new GeoYandexIterationData(originStartIndex, originRemainder, destinationStartIndex, destinationRemainder);
                int iterDataCount = 1;

                while (iterDataCount > 0)
                {
                    // 11.0 Извлекаем параметры для очередной итерации
                    rc = 110;
                    GeoYandexIterationData data = iterationData[--iterDataCount];
                    originRemainder = data.OriginRemainder;
                    destinationRemainder = data.DestinationRemainder;
                    if (originRemainder <= 0 || destinationRemainder <= 0)
                        continue;
                    originStartIndex = data.OriginStartIndex;
                    destinationStartIndex = data.DestinationStartIndex;

                    // 11.1 Два частных случая - один или два запроса
                    rc = 111;
                    size = originRemainder * destinationRemainder;

                    if (size <= pairLimit)
                    {
                        result[count++] = new GeoYandexRequestData(origins, originStartIndex, originRemainder, destinations, destinationStartIndex, destinationRemainder, modes, geoData);
                        continue;
                    }
                    else if (size <= pairLimit2)
                    {
                        if ((originRemainder % 2) == 0)
                        {
                            int length = originRemainder / 2;
                            result[count++] = new GeoYandexRequestData(origins, originStartIndex, length, destinations, destinationStartIndex, destinationRemainder, modes, geoData);
                            result[count++] = new GeoYandexRequestData(origins, originStartIndex + length, originRemainder - length, destinations, destinationStartIndex, destinationRemainder, modes, geoData);
                        }
                        else if ((destinationCount % 2) == 0)
                        {
                            int length = destinationRemainder / 2;
                            result[count++] = new GeoYandexRequestData(origins, originStartIndex, originRemainder, destinations, destinationStartIndex, length, modes, geoData);
                            result[count++] = new GeoYandexRequestData(origins, originStartIndex, originRemainder, destinations, destinationStartIndex + length, destinationRemainder - length, modes, geoData);
                        }
                        else
                        {
                            int length = originRemainder / 2;
                            result[count++] = new GeoYandexRequestData(origins, originStartIndex, length, destinations, destinationStartIndex, destinationRemainder, modes, geoData);
                            result[count++] = new GeoYandexRequestData(origins, originStartIndex + length, originRemainder - length, destinations, destinationStartIndex, destinationRemainder, modes, geoData);
                        }

                        continue;
                    }

                    // originReminder * destinationReminder > 2 * pairLimit

                    // 11.2 Считаем допустимые потери
                    rc = 112;
                    //int allowableLosses = (size / pairLimit);
                    int allowableLosses = (size % pairLimit);
                    if (allowableLosses > 0)
                        allowableLosses = pairLimit - allowableLosses;

                    // 11.3 Частный случай: допустимые потери = 0, т.е originReminder * destinationReminder = m * pairLimit
                    rc = 113;
                    if (allowableLosses == 0)
                    {
                        for (int i = 0; i < multiplierPairs.Length; i++)
                        {
                            int x = multiplierPairs[i].X;
                            int y = multiplierPairs[i].Y;
                            if ((pairLimit - x * y) != 0)
                                continue;

                            if ((originRemainder % x) == 0 && (destinationRemainder % y) == 0)
                            {
                                for (int j = originStartIndex; j < originStartIndex + originRemainder; j += x)
                                {
                                    for (int k = destinationStartIndex; k < destinationStartIndex + destinationRemainder; k += y)
                                    {
                                        result[count++] = new GeoYandexRequestData(origins, j, x, destinations, k, y, modes, geoData);
                                    }
                                }

                                goto WhileEnd;
                            }
                            else if ((originRemainder % y) == 0 && (destinationRemainder % x) == 0)
                            {
                                for (int j = originStartIndex; j < originStartIndex + originRemainder; j += y)
                                {
                                    for (int k = destinationStartIndex; k < destinationStartIndex + destinationRemainder; k += x)
                                    {
                                        result[count++] = new GeoYandexRequestData(origins, j, y, destinations, k, x, modes, geoData);
                                    }
                                }

                                goto WhileEnd;
                            }
                        }
                    }

                    // 11.4 Делаем шаг вперед с минимальными потерями
                    rc = 114;
                    int minLosses = int.MaxValue;
                    int losses = 0;
                    int minX = 0;
                    int minY = 0;
                    int caseXY = -1;

                    for (int i = 0; i < multiplierPairs.Length; i++)
                    {
                        // 11.4.1 Извлекаем множители
                        rc = 1141;
                        int x = multiplierPairs[i].X;
                        int y = multiplierPairs[i].Y;

                        // 11.4.2 Потери за x * y пар
                        rc = 1142;
                        int opLosses = pairLimit - x * y;
                        losses = -1;

                        // 11.4.3 Случай
                        //               originReminder = n1 * x + r1
                        //               destinationReminder = n2 * y + r2
                        rc = 1143;
                        if (originRemainder >= x && destinationRemainder >= y)
                        {
                            // 11.4.3.1 Раскладываем по множителю
                            int n1 = originRemainder / x;
                            int r1 = originRemainder % x;
                            int n2 = destinationRemainder / y;
                            int r2 = destinationRemainder % y;
                            losses = opLosses * n1 * n2;

                            // 11.4.3.2 Считаем потери для варианта 1:
                            //          (originReminder = n1 * x + r1  --> r2  && r1 --> n2 * y)
                            int losses1 = (originRemainder * r2) % pairLimit;
                            if (losses1 > 0)
                                losses1 = pairLimit - losses1;
                            int r = (r1 * n2 * y) % pairLimit;
                            if (r > 0)
                                losses1 += (pairLimit - r);

                            // 11.4.3.3 Считаем потери для варианта 2:
                            //          (n1 * x  --> r2  && r1 --> destinationReminder = n2 * y + r2)
                            int losses2 = (n1 * x * r2) % pairLimit;
                            if (losses2 > 0)
                                losses2 = pairLimit - losses2;
                            r = (r1 * destinationRemainder) % pairLimit;
                            if (r > 0)
                                losses2 += (pairLimit - r);

                            // 11.4.3.4 Подсчитываем общие потери
                            losses += (losses2 <= losses1 ? losses2 : losses1);

                            // 11.4.3.5 Выбираме наилучший вариант
                            if (losses < minLosses)
                            {
                                minLosses = losses;
                                minX = x;
                                minY = y;
                                caseXY = (losses2 <= losses1 ? 1 : 0);
                            }
                        }

                        // 11.4.4 Случай
                        //               originReminder = n1 * y + r1
                        //               destinationReminder = n2 * x + r2
                        if (x != y && originRemainder >= y && destinationRemainder >= x)
                        {
                            // 14.4.4.0 Меняем x и y местами
                            losses = x;
                            x = y;
                            y = losses;

                            // 11.4.4.1 Раскладываем по множителю
                            int n1 = originRemainder / x;
                            int r1 = originRemainder % x;
                            int n2 = destinationRemainder / y;
                            int r2 = destinationRemainder % y;
                            losses = opLosses * n1 * n2;

                            // 11.4.4.2 Считаем потери для варианта 1:
                            //          (originReminder = n1 * x + r1  --> r2  && r1 --> n2 * y)
                            int losses1 = (originRemainder * r2) % pairLimit;
                            if (losses1 > 0)
                                losses1 = pairLimit - losses1;
                            int r = (r1 * n2 * y) % pairLimit;
                            if (r > 0)
                                losses1 += (pairLimit - r);

                            // 11.4.4.3 Считаем потери для варианта 2:
                            //          (n1 * x  --> r2  && r1 --> destinationReminder = n2 * y + r2)
                            int losses2 = (n1 * x * r2) % pairLimit;
                            if (losses2 > 0)
                                losses2 = pairLimit - losses2;
                            r = (r1 * destinationRemainder) % pairLimit;
                            if (r > 0)
                                losses2 += (pairLimit - r);

                            // 11.4.4.4 Подсчитываем общие потери
                            losses += (losses2 <= losses1 ? losses2 : losses2);

                            // 11.4.4.5 Выбираме наилучший вариант
                            if (losses < minLosses)
                            {
                                minLosses = losses;
                                minX = x;
                                minY = y;
                                caseXY = (losses2 <= losses1 ? 1 : 0);
                            }
                        }
                    }

                    // 11.5 Делаем шаг вперед
                    rc = 115;

                    if (minLosses == int.MaxValue)
                    {
                        losses = losses;
                    }
                    else
                    {
                        int n1 = originRemainder / minX;
                        int r1 = originRemainder % minX;
                        int n2 = destinationRemainder / minY;
                        int r2 = destinationRemainder % minY;

                        int originEndIndex = originStartIndex + n1 * minX;
                        int destinatioEndIndex = destinationStartIndex + n2 * minY;

                        for (int i = originStartIndex; i < originEndIndex; i += minX)
                        {
                            for (int j = destinationStartIndex; j < destinatioEndIndex; j += minY)
                            {
                                result[count++] = new GeoYandexRequestData(origins, i, minX, destinations, j, minY, modes, geoData);
                            }
                        }

                        if (caseXY == 0)
                        {
                            // 11.5.1 Вариант 1: originReminder = n1 * x + r1  --> r2  && r1 --> n2 * y
                            rc = 1151;

                            if (r2 > 0)
                            {
                                // originReminder = n1 * x + r1  --> r2
                                int destinationStartIndex1 = destinationStartIndex + n2 * minY;
                                int destinationRemainder1 = r2;
                                iterationData[iterDataCount++] = new GeoYandexIterationData(originStartIndex, originRemainder, destinationStartIndex1, destinationRemainder1);
                            }

                            if (r1 > 0)
                            {
                                // 11.5.1.2 r1 --> n2 * y
                                int originStartIndex1 = originStartIndex + n1 * minX;
                                int originRemainder1 = r1;
                                int destinationRemainder2 = n2 * minY;
                                iterationData[iterDataCount++] = new GeoYandexIterationData(originStartIndex1, originRemainder1, destinationStartIndex, destinationRemainder2);
                            }
                        }
                        else
                        {
                            // 11.5.2  Вариант 2: n1 * x  --> r2  && r1 --> destinationReminder = n2 * y + r2
                            rc = 1152;
                            if (r2 > 0)
                            {
                                // 11.5.2.1 n1 * x  --> r2
                                int originStartIndex1 = originStartIndex;
                                int originRemainder1 = n1 * minX;
                                int destinationStartIndex1 = destinationStartIndex + n2 * minY;
                                int destinationRemainder1 = r2;
                                iterationData[iterDataCount++] = new GeoYandexIterationData(originStartIndex1, originRemainder1, destinationStartIndex1, destinationRemainder1);
                            }

                            if (r1 > 0)
                            {
                                // 11.5.2.2 r1 --> destinationReminder = n2 * y + r2
                                int originStartIndex2 = originStartIndex + n1 * minX;
                                int originRemainder2 = r1;
                                iterationData[iterDataCount++] = new GeoYandexIterationData(originStartIndex2, originRemainder2, destinationStartIndex, destinationRemainder);
                            }
                        }
                    }

                    WhileEnd: ;
                }


                // 12. Завершение обработки
                Fin:
                rc = 12;
                if (count < result.Length)
                {
                    Array.Resize(ref result, count);
                }

                geoContext = result;

                // 13. Выход - Ok
                rc = 0;
                return rc;
            }
            catch
            {
                return rc;
            }
        }

        /// <summary>
        /// Построение контекстов запросов к Yandex
        /// </summary>
        /// <param name="requestArgs">Данные для запросов</param>
        /// <param name="pairLimit">Максимальное количество пар в одном запросе</param>
        /// <param name="geoContext">Результат - контексты запросов</param>
        /// <returns>0 - контексты построены; контексты не построены</returns>
        private static int GetGeoContext(GeoYandexRequest requestArgs, int pairLimit, out GeoYandexRequestData[] geoContext)
        {
            // 1. Инициализация
            int rc = 1;
            geoContext = null;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (pairLimit <= 0)
                    return rc;
                if (requestArgs.Origins == null || requestArgs.Origins.Length <= 0)
                    return rc;
                if (requestArgs.Destinations == null || requestArgs.Destinations.Length <= 0)
                    return rc;
                if (requestArgs.Modes == null || requestArgs.Modes.Length <= 0)
                    return rc;

                // 3. Два частных случая - разбиениие только origins или destinations
                rc = 3;
                int rc1 = GetGeoContextSpecialCase(requestArgs, pairLimit, out geoContext);
                if (rc1 == 0)
                    return rc = 0;

                // 4. Выделяем память под результат
                rc = 4;
                GeoPoint[] origins = requestArgs.Origins;
                GeoPoint[] destinations = requestArgs.Destinations;
                string[] modes = requestArgs.Modes;
                int originCount = origins.Length;
                int destinationCount = destinations.Length;
                int modeCount = modes.Length;
                Point[,,] geoData = new Point[originCount, destinationCount, modeCount];

                // 5. Ещё два частных случая - pairLimit = 1, 2
                rc = 5;
                int pairLimit2 = pairLimit + pairLimit;

                if (pairLimit == 1)
                {
                    geoContext = new GeoYandexRequestData[originCount * destinationCount];
                    int cnt = 0;

                    for (int i = 0; i < originCount; i++)
                    {
                        for (int j = 0; j < destinationCount; j++)
                        {
                            geoContext[cnt++] = new GeoYandexRequestData(origins, i, 1, destinations, j, 1, modes, geoData);
                        }
                    }

                    return rc = 0;
                }
                else if (pairLimit == 2)
                {
                    geoContext = new GeoYandexRequestData[(originCount * destinationCount + 1) / 2];
                    int cnt = 0;

                    for (int i = 0; i < originCount; i++)
                    {
                        for (int j = 0; j < destinationCount - 1; j += 2)
                        {
                            geoContext[cnt++] = new GeoYandexRequestData(origins, i, 1, destinations, j, 2, modes, geoData);
                        }
                    }

                    if ((destinationCount % 2) != 0)
                    {
                        for (int i = 0; i < originCount - 1; i += 2)
                        {
                            geoContext[cnt++] = new GeoYandexRequestData(origins, i, 2, destinations, destinationCount - 1, 1, modes, geoData);
                        }

                        if ((originCount % 2) != 0)
                        {
                            geoContext[cnt++] = new GeoYandexRequestData(origins, originCount - 1, 1, destinations, destinationCount - 1, 1, modes, geoData);
                        }
                    }

                    return rc = 0;
                }

                // 6. Выделяем память под результат
                rc = 6;
                int size = (originCount * destinationCount + pairLimit - 1) / pairLimit + 2;
                GeoYandexRequestData[] result = new GeoYandexRequestData[size];
                int count = 0;

                // 7. Делим destination-точки на группы по pairLimit точек + остаток (от 0 до pairLimit - 1);
                rc = 7;
                int destinationGroupCount = destinationCount / pairLimit;
                int destinationRemainder = destinationCount % pairLimit;
                int destinationStartIndex = pairLimit * destinationGroupCount;

                if (destinationGroupCount > 0)
                {
                    //                          [origins]
                    //                              ↓
                    // destinations = [{pairLimit} ... {pairLimit} {destinationRemainder}]     (destinationRemainder < pairLimit)
                    //                                              ↑
                    //                                    (destinationStartIndex)

                    int lim = destinationGroupCount * pairLimit;
                    for (int i = 0; i < originCount; i++)
                    {
                        for (int j = 0; j < lim; j += pairLimit)
                        {
                            result[count++] = new GeoYandexRequestData(origins, i, 1, destinations, j, pairLimit, modes, geoData);
                        }
                    }
                }

                if (destinationRemainder == 0)
                    goto Fin;

                //                          [origins]
                //                              ↓
                //                    [destinationRemainder]     (0 < destinationRemainder < pairLimit)
                //                     ↑
                //           (destinationStartIndex)

                // 8. Делим origin-точки на группы по pairLimit точек + остаток (от 0 до pairLimit - 1);
                rc = 8;
                int originGroupCount = originCount / pairLimit;
                int originRemainder = originCount % pairLimit;
                int originStartIndex = pairLimit * originGroupCount;

                if (originGroupCount > 0)
                {
                    // origins = [{pairLimit} ... {pairLimit} {originRemainder}]     (originRemainder < pairLimit)
                    //                              ↓
                    //      destinations = [destinationRemainder]                    (0 < destinationRemainder < pairLimit)
                    //                      ↑
                    //            (destinationStartIndex)

                    int lim = originGroupCount * pairLimit;
                    for (int i = destinationStartIndex; i < destinationCount; i++)
                    {
                        for (int j = 0; j < lim; j += pairLimit)
                        {
                            result[count++] = new GeoYandexRequestData(origins, j, pairLimit, destinations, i, 1, modes, geoData);
                        }
                    }
                }

                if (originRemainder == 0)
                    goto Fin;

                //                   (originStartIndex)
                //                           ↓
                //                          [originRemainder]        (0 < originRemainder < pairLimit)
                //                                 ↓
                //                       [destinationRemainder]     (0 < destinationRemainder < pairLimit)
                //                        ↑
                //              (destinationStartIndex)

                // 9. Находим все пары множителей x, y такие, что
                //     0 ≤ pairLimit - x * y ≤ 1 (y ≥ x > 0)
                rc = 9;
                Point[] multiplierPairs = GetMultiplierPairs(pairLimit, 1);
                if (multiplierPairs == null || multiplierPairs.Length <= 0)
                    return rc;

                // 10. Цикл дальнейшего построения
                rc = 10;
                GeoYandexIterationData[] iterationData = new GeoYandexIterationData[originRemainder * destinationRemainder];
                iterationData[0] = new GeoYandexIterationData(originStartIndex, originRemainder, destinationStartIndex, destinationRemainder);
                int iterDataCount = 1;

                while (iterDataCount > 0)
                {
                    // 10.0 Извлекаем параметры для очередной итерации
                    rc = 100;
                    GeoYandexIterationData data = iterationData[--iterDataCount];
                    originRemainder = data.OriginRemainder;
                    destinationRemainder = data.DestinationRemainder;
                    if (originRemainder <= 0 || destinationRemainder <= 0)
                        continue;
                    originStartIndex = data.OriginStartIndex;
                    destinationStartIndex = data.DestinationStartIndex;

                    // 10.1 Два частных случая - один или два запроса
                    rc = 101;
                    size = originRemainder * destinationRemainder;

                    if (size <= pairLimit)
                    {
                        result[count++] = new GeoYandexRequestData(origins, originStartIndex, originRemainder, destinations, destinationStartIndex, destinationRemainder, modes, geoData);
                        continue;
                    }
                    else if (size <= pairLimit2)
                    {
                        if ((originRemainder % 2) == 0)
                        {
                            int length = originRemainder / 2;
                            result[count++] = new GeoYandexRequestData(origins, originStartIndex, length, destinations, destinationStartIndex, destinationRemainder, modes, geoData);
                            result[count++] = new GeoYandexRequestData(origins, originStartIndex + length, originRemainder - length, destinations, destinationStartIndex, destinationRemainder, modes, geoData);
                            continue;
                        }
                        else if ((destinationCount % 2) == 0)
                        {
                            int length = destinationRemainder / 2;
                            result[count++] = new GeoYandexRequestData(origins, originStartIndex, originRemainder, destinations, destinationStartIndex, length, modes, geoData);
                            result[count++] = new GeoYandexRequestData(origins, originStartIndex, originRemainder, destinations, destinationStartIndex + length, destinationRemainder - length, modes, geoData);
                            continue;
                        }
                        //else
                        //{
                        //    int length = originRemainder / 2;
                        //    result[count++] = new GeoYandexRequestData(origins, originStartIndex, length, destinations, destinationStartIndex, destinationRemainder, modes, geoData);
                        //    result[count++] = new GeoYandexRequestData(origins, originStartIndex + length, originRemainder - length, destinations, destinationStartIndex, destinationRemainder, modes, geoData);
                        //}
                    }

                    // originReminder * destinationReminder > 2 * pairLimit

                    // 10.2 Считаем допустимые потери
                    rc = 102;
                    //int allowableLosses = (size / pairLimit);
                    int allowableLosses = (size % pairLimit);
                    if (allowableLosses > 0)
                        allowableLosses = pairLimit - allowableLosses;

                    // 10.3 Частный случай: допустимые потери = 0, т.е originReminder * destinationReminder = m * pairLimit
                    rc = 103;
                    if (allowableLosses == 0)
                    {
                        for (int i = 0; i < multiplierPairs.Length; i++)
                        {
                            int x = multiplierPairs[i].X;
                            int y = multiplierPairs[i].Y;
                            if ((pairLimit - x * y) != 0)
                                continue;

                            if ((originRemainder % x) == 0 && (destinationRemainder % y) == 0)
                            {
                                for (int j = originStartIndex; j < originStartIndex + originRemainder; j += x)
                                {
                                    for (int k = destinationStartIndex; k < destinationStartIndex + destinationRemainder; k += y)
                                    {
                                        result[count++] = new GeoYandexRequestData(origins, j, x, destinations, k, y, modes, geoData);
                                    }
                                }

                                goto WhileEnd;
                            }
                            else if ((originRemainder % y) == 0 && (destinationRemainder % x) == 0)
                            {
                                for (int j = originStartIndex; j < originStartIndex + originRemainder; j += y)
                                {
                                    for (int k = destinationStartIndex; k < destinationStartIndex + destinationRemainder; k += x)
                                    {
                                        result[count++] = new GeoYandexRequestData(origins, j, y, destinations, k, x, modes, geoData);
                                    }
                                }

                                goto WhileEnd;
                            }
                        }
                    }

                    // 10.4 Делаем шаг вперед с минимальными потерями
                    rc = 104;
                    int minLosses = int.MaxValue;
                    int losses = 0;
                    int minX = 0;
                    int minY = 0;
                    int caseXY = -1;

                    for (int i = 0; i < multiplierPairs.Length; i++)
                    {
                        // 10.4.1 Извлекаем множители
                        rc = 1041;
                        int x = multiplierPairs[i].X;
                        int y = multiplierPairs[i].Y;

                        // 10.4.2 Потери за x * y пар
                        rc = 1042;
                        int opLosses = pairLimit - x * y;
                        losses = -1;

                        // 10.4.3 Случай
                        //               originReminder = n1 * x + r1
                        //               destinationReminder = n2 * y + r2
                        rc = 1043;
                        if (originRemainder >= x && destinationRemainder >= y)
                        {
                            // 10.4.3.1 Раскладываем по множителю
                            int n1 = originRemainder / x;
                            int r1 = originRemainder % x;
                            int n2 = destinationRemainder / y;
                            int r2 = destinationRemainder % y;
                            losses = opLosses * n1 * n2;

                            // 10.4.3.2 Считаем потери для варианта 1:
                            //          (originReminder = n1 * x + r1  --> r2  && r1 --> n2 * y)
                            int losses1 = (originRemainder * r2) % pairLimit;
                            if (losses1 > 0)
                                losses1 = pairLimit - losses1;
                            int r = (r1 * n2 * y) % pairLimit;
                            if (r > 0)
                                losses1 += (pairLimit - r);

                            // 10.4.3.3 Считаем потери для варианта 2:
                            //          (n1 * x  --> r2  && r1 --> destinationReminder = n2 * y + r2)
                            int losses2 = (n1 * x * r2) % pairLimit;
                            if (losses2 > 0)
                                losses2 = pairLimit - losses2;
                            r = (r1 * destinationRemainder) % pairLimit;
                            if (r > 0)
                                losses2 += (pairLimit - r);

                            // 10.4.3.4 Подсчитываем общие потери
                            losses += (losses2 <= losses1 ? losses2 : losses1);

                            // 10.4.3.5 Выбираме наилучший вариант
                            if (losses < minLosses)
                            {
                                minLosses = losses;
                                minX = x;
                                minY = y;
                                caseXY = (losses2 <= losses1 ? 1 : 0);
                            }
                        }

                        // 10.4.4 Случай
                        //               originReminder = n1 * y + r1
                        //               destinationReminder = n2 * x + r2
                        if (x != y && originRemainder >= y && destinationRemainder >= x)
                        {
                            // 10.4.4.0 Меняем x и y местами
                            losses = x;
                            x = y;
                            y = losses;

                            // 10.4.4.1 Раскладываем по множителю
                            int n1 = originRemainder / x;
                            int r1 = originRemainder % x;
                            int n2 = destinationRemainder / y;
                            int r2 = destinationRemainder % y;
                            losses = opLosses * n1 * n2;

                            // 10.4.4.2 Считаем потери для варианта 1:
                            //          (originReminder = n1 * x + r1  --> r2  && r1 --> n2 * y)
                            int losses1 = (originRemainder * r2) % pairLimit;
                            if (losses1 > 0)
                                losses1 = pairLimit - losses1;
                            int r = (r1 * n2 * y) % pairLimit;
                            if (r > 0)
                                losses1 += (pairLimit - r);

                            // 10.4.4.3 Считаем потери для варианта 2:
                            //          (n1 * x  --> r2  && r1 --> destinationReminder = n2 * y + r2)
                            int losses2 = (n1 * x * r2) % pairLimit;
                            if (losses2 > 0)
                                losses2 = pairLimit - losses2;
                            r = (r1 * destinationRemainder) % pairLimit;
                            if (r > 0)
                                losses2 += (pairLimit - r);

                            // 10.4.4.4 Подсчитываем общие потери
                            losses += (losses2 <= losses1 ? losses2 : losses2);

                            // 10.4.4.5 Выбираме наилучший вариант
                            if (losses < minLosses)
                            {
                                minLosses = losses;
                                minX = x;
                                minY = y;
                                caseXY = (losses2 <= losses1 ? 1 : 0);
                            }
                        }
                    }

                    // 10.5 Делаем шаг вперед
                    rc = 105;

                    if (minLosses == int.MaxValue)
                    {
                        losses = losses;
                    }
                    else
                    {
                        int n1 = originRemainder / minX;
                        int r1 = originRemainder % minX;
                        int n2 = destinationRemainder / minY;
                        int r2 = destinationRemainder % minY;

                        int originEndIndex = originStartIndex + n1 * minX;
                        int destinatioEndIndex = destinationStartIndex + n2 * minY;

                        for (int i = originStartIndex; i < originEndIndex; i += minX)
                        {
                            for (int j = destinationStartIndex; j < destinatioEndIndex; j += minY)
                            {
                                result[count++] = new GeoYandexRequestData(origins, i, minX, destinations, j, minY, modes, geoData);
                            }
                        }

                        if (caseXY == 0)
                        {
                            // 10.5.1 Вариант 1: originReminder = n1 * x + r1  --> r2  && r1 --> n2 * y
                            rc = 1051;

                            if (r2 > 0)
                            {
                                // originReminder = n1 * x + r1  --> r2
                                int destinationStartIndex1 = destinationStartIndex + n2 * minY;
                                int destinationRemainder1 = r2;
                                iterationData[iterDataCount++] = new GeoYandexIterationData(originStartIndex, originRemainder, destinationStartIndex1, destinationRemainder1);
                            }

                            if (r1 > 0)
                            {
                                // 11.5.1.2 r1 --> n2 * y
                                int originStartIndex1 = originStartIndex + n1 * minX;
                                int originRemainder1 = r1;
                                int destinationRemainder2 = n2 * minY;
                                iterationData[iterDataCount++] = new GeoYandexIterationData(originStartIndex1, originRemainder1, destinationStartIndex, destinationRemainder2);
                            }
                        }
                        else
                        {
                            // 10.5.2  Вариант 2: n1 * x  --> r2  && r1 --> destinationReminder = n2 * y + r2
                            rc = 1052;
                            if (r2 > 0)
                            {
                                // 11.5.2.1 n1 * x  --> r2
                                int originStartIndex1 = originStartIndex;
                                int originRemainder1 = n1 * minX;
                                int destinationStartIndex1 = destinationStartIndex + n2 * minY;
                                int destinationRemainder1 = r2;
                                iterationData[iterDataCount++] = new GeoYandexIterationData(originStartIndex1, originRemainder1, destinationStartIndex1, destinationRemainder1);
                            }

                            if (r1 > 0)
                            {
                                // 10.5.2.2 r1 --> destinationReminder = n2 * y + r2
                                int originStartIndex2 = originStartIndex + n1 * minX;
                                int originRemainder2 = r1;
                                iterationData[iterDataCount++] = new GeoYandexIterationData(originStartIndex2, originRemainder2, destinationStartIndex, destinationRemainder);
                            }
                        }
                    }

                    WhileEnd: ;
                }

                // 11. Завершение обработки
                Fin:
                rc = 11;
                if (count < result.Length)
                {
                    Array.Resize(ref result, count);
                }

                geoContext = result;

                // 12. Выход - Ok
                rc = 0;
                return rc;
            }
            catch
            {
                return rc;
            }
        }

        /// <summary>
        /// Построение контекстов запросов к Yandex
        /// в специальных случаях точного миимального решения
        /// </summary>
        /// <param name="requestArgs">Данные для запросов</param>
        /// <param name="pairLimit">Максимальное количество пар в одном запросе</param>
        /// <param name="geoContext">Результат - контексты запросов</param>
        /// <returns>0 - контексты построены; контексты не построены</returns>
        private static int GetGeoContextSpecialCase(GeoYandexRequest requestArgs, int pairLimit, out GeoYandexRequestData[] geoContext)
        {
            // 1. Инициализация
            int rc = 1;
            geoContext = null;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (pairLimit <= 0)
                    return rc;
                if (requestArgs.Origins == null || requestArgs.Origins.Length <= 0)
                    return rc;
                if (requestArgs.Destinations == null || requestArgs.Destinations.Length <= 0)
                    return rc;
                if (requestArgs.Modes == null || requestArgs.Modes.Length <= 0)
                    return rc;

                // 3. Извлекаем счетчики и миималное количество запросов
                rc = 3;
                int originCount = requestArgs.Origins.Length;
                int destinationCount = requestArgs.Destinations.Length;
                if (originCount > pairLimit && destinationCount > pairLimit)
                    return rc;
                if (originCount * destinationCount <= pairLimit)
                {
                    GeoPoint[] origins = requestArgs.Origins;
                    GeoPoint[] destinations = requestArgs.Destinations;
                    string[] modes = requestArgs.Modes;
                    int modeCount = modes.Length;
                    Point[,,] geoData = new Point[originCount, destinationCount, modeCount];
                    geoContext = new GeoYandexRequestData[] { new GeoYandexRequestData(origins, 0, originCount, destinations, 0, destinationCount, modes, geoData) };
                    return rc = 0;
                }

                int minReqestCount = (originCount * destinationCount + pairLimit - 1) / pairLimit;

                // 4. Случай (origins1 + ... + originsk) -> destinations,  originsi * destinations ≤ pairLimit, k = minReqestCount
                rc = 4;
                int n2 = pairLimit / destinationCount;
                if (originCount <= n2 * minReqestCount)
                {
                    geoContext = new GeoYandexRequestData[minReqestCount];
                    int k = 0;
                    GeoPoint[] origins = requestArgs.Origins;
                    GeoPoint[] destinations = requestArgs.Destinations;
                    string[] modes = requestArgs.Modes;
                    int modeCount = modes.Length;
                    Point[,,] geoData = new Point[originCount, destinationCount, modeCount];

                    for (int originsStartIndex = 0; originsStartIndex < originCount; originsStartIndex += n2)
                    {
                        int length = originCount - originsStartIndex;
                        if (length > n2) length = n2;
                        geoContext[k++] = new GeoYandexRequestData(origins, originsStartIndex, length, destinations, 0, destinationCount, modes, geoData);
                    }

                    return rc = 0;
                }

                // 5. Случай origins -> (destinations1 + ... + destinationsk),  origins * destinationsi ≤ pairLimit, k = minReqestCount
                rc = 5;
                int n1 = pairLimit / originCount;
                if (destinationCount <= n1 * minReqestCount)
                {
                    geoContext = new GeoYandexRequestData[minReqestCount];
                    int k = 0;
                    GeoPoint[] origins = requestArgs.Origins;
                    GeoPoint[] destinations = requestArgs.Destinations;
                    string[] modes = requestArgs.Modes;
                    int modeCount = modes.Length;
                    Point[,,] geoData = new Point[originCount, destinationCount, modeCount];

                    for (int destinationsStartIndex = 0; destinationsStartIndex < destinationCount; destinationsStartIndex += n1)
                    {
                        int length = destinationCount - destinationsStartIndex;
                        if (length > n1) length = n1;
                        geoContext[k++] = new GeoYandexRequestData(origins, 0, originCount, destinations, destinationsStartIndex, length, modes, geoData);
                    }

                    return rc = 0;
                }

                // 6. Выход
                rc = 6;
                return rc;
            }
            catch
            {
                return rc;
            }
        }

        /// <summary>
        /// Построение контекстов запросов к Yandex
        /// в специальных случаях точного миимального решения
        /// </summary>
        /// <param name="requestArgs">Данные для запросов</param>
        /// <param name="pairLimit">Максимальное количество пар в одном запросе</param>
        /// <param name="geoContext">Результат - контексты запросов</param>
        /// <returns>0 - контексты построены; контексты не построены</returns>
        private static int GetGeoContextSpecialCase(GeoYandexRequest requestArgs,
                                                    int originStartIndex, int originLength,
                                                    int destinationStartIndex, int destinationLength,
                                                    int pairLimit, out GeoYandexRequestData[] geoContext)
        {
            // 1. Инициализация
            int rc = 1;
            geoContext = null;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (pairLimit <= 0)
                    return rc;
                if (requestArgs.Origins == null || requestArgs.Origins.Length <= 0)
                    return rc;
                if (requestArgs.Destinations == null || requestArgs.Destinations.Length <= 0)
                    return rc;
                if (requestArgs.Modes == null || requestArgs.Modes.Length <= 0)
                    return rc;

                // 3. Извлекаем счетчики и миималное количество запросов
                rc = 3;
                int originCount = requestArgs.Origins.Length;
                int destinationCount = requestArgs.Destinations.Length;
                if (originCount > pairLimit && destinationCount > pairLimit)
                    return rc;
                if (originCount * destinationCount <= pairLimit)
                {
                    GeoPoint[] origins = requestArgs.Origins;
                    GeoPoint[] destinations = requestArgs.Destinations;
                    string[] modes = requestArgs.Modes;
                    int modeCount = modes.Length;
                    Point[,,] geoData = new Point[originCount, destinationCount, modeCount];
                    geoContext = new GeoYandexRequestData[] { new GeoYandexRequestData(origins, 0, originCount, destinations, 0, destinationCount, modes, geoData) };
                    return rc = 0;
                }

                int minReqestCount = (originCount * destinationCount + pairLimit - 1) / pairLimit;

                // 4. Случай (origins1 + ... + originsk) -> destinations,  originsi * destinations ≤ pairLimit, k = minReqestCount
                rc = 4;
                int n2 = pairLimit / destinationCount;
                if (originCount <= n2 * minReqestCount)
                {
                    geoContext = new GeoYandexRequestData[minReqestCount];
                    int k = 0;
                    GeoPoint[] origins = requestArgs.Origins;
                    GeoPoint[] destinations = requestArgs.Destinations;
                    string[] modes = requestArgs.Modes;
                    int modeCount = modes.Length;
                    Point[,,] geoData = new Point[originCount, destinationCount, modeCount];

                    for (int originsStartIndex = 0; originsStartIndex < originCount; originsStartIndex += n2)
                    {
                        int length = originCount - originsStartIndex;
                        if (length > n2) length = n2;
                        geoContext[k++] = new GeoYandexRequestData(origins, originsStartIndex, length, destinations, 0, destinationCount, modes, geoData);
                    }

                    return rc = 0;
                }

                // 5. Случай origins -> (destinations1 + ... + destinationsk),  origins * destinationsi ≤ pairLimit, k = minReqestCount
                rc = 5;
                int n1 = pairLimit / originCount;
                if (destinationCount <= n1 * minReqestCount)
                {
                    geoContext = new GeoYandexRequestData[minReqestCount];
                    int k = 0;
                    GeoPoint[] origins = requestArgs.Origins;
                    GeoPoint[] destinations = requestArgs.Destinations;
                    string[] modes = requestArgs.Modes;
                    int modeCount = modes.Length;
                    Point[,,] geoData = new Point[originCount, destinationCount, modeCount];

                    for (int destinationsStartIndex = 0; destinationsStartIndex < destinationCount; destinationsStartIndex += n1)
                    {
                        int length = destinationCount - destinationsStartIndex;
                        if (length > n1) length = n1;
                        geoContext[k++] = new GeoYandexRequestData(origins, 0, originCount, destinations, destinationsStartIndex, length, modes, geoData);
                    }

                    return rc = 0;
                }

                // 6. Выход
                rc = 6;
                return rc;
            }
            catch
            {
                return rc;
            }
        }


        /// <summary>
        /// Для заданного целого n нахождение всех пар n1 и n2,
        /// таких, что
        ///            n1 ≤ n2
        ///            0 ≤ n - n1 * n2 ≤ limit;
        /// </summary>
        /// <param name="number">Заданное число n</param>
        /// <param name="differenceLim">limit</param>
        /// <returns>Найденные пары</returns>
        private static Point[] GetMultiplierPairs(int number, int differenceLim = 1)
        {
            // 1. Инициализация

            try
            {
                // 2. Проверяем исходные данные
                if (number <= 0)
                    return null;
                if (number == 1)
                    return new Point[] { new Point(1, 1) };
                if (differenceLim < 0)
                    differenceLim = 0;

                // 3. Выделяем память под результат
                int divLim = (int)Math.Sqrt(number);
                Point[] result = new Point[divLim * (number + 1) / 2];
                int count = 0;

                for (int i = 1; i <= divLim; i++)
                {
                    for (int j = i; j <= number; j++)
                    {
                        int difference = number - i * j;
                        if (difference >= 0 && difference <= differenceLim)
                        {
                            result[count].X = i;
                            result[count++].Y = j;
                        }
                    }
                }

                if (count < result.Length)
                {
                    Array.Resize(ref result, count);
                }

                // 4. Выход - Ok
                return result;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Выполнение гео-запросов Yandex
        /// </summary>
        /// <param name="status">Объект типа GeoThreadContext с данными для запросов</param>
        private static void GeoThread(object status)
        {
            // 1. Инициализация
            int rc = 1;
            GeoYandexThreadContext context = status as GeoYandexThreadContext;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (context == null)
                    return;
                if (string.IsNullOrEmpty(context.GetUrl))
                    return;
                if (string.IsNullOrEmpty(context.ApiKey))
                    return;
                if (context.RequestData == null || context.RequestData.Length <= 0)
                    return;
                if (context.StartIndex < 0 || context.StartIndex >= context.RequestData.Length)
                    return;
                if (context.Step <= 0)
                    return;

                // 3. Цикл запросов к Yandex
                rc = 3;
                string getUrl = context.GetUrl;
                string apiKey = context.ApiKey;
                StringBuilder sb = new StringBuilder(100 * 50);
                string geoPointPattern = "{0:0.0#######},{1:0.0#######}";

                for (int contextIndex = context.StartIndex; contextIndex < context.RequestData.Length; contextIndex += context.Step)
                {
                    // 3.1 Извлекаем и проверяем данные запроса
                    rc = 31;
                    GeoYandexRequestData requestData = context.RequestData[contextIndex];
                    if (requestData == null)
                        continue;

                    GeoPoint[] origins = requestData.Origins;
                    GeoPoint[] destinations = requestData.Destinations;
                    int originStartIndex = requestData.StartOrginIndex;
                    int originLength = requestData.OriginLength;
                    int destinationStartIndex = requestData.StartDestinationIndex;
                    int destinationLength = requestData.DestinationLength;

                    requestData.ExitCode = 310;
                    if (origins == null || origins.Length <= 0)
                        continue;
                    if (originStartIndex < 0 || originStartIndex >= origins.Length)
                        continue;
                    if (originLength <= 0 || originStartIndex + originLength > origins.Length)
                        continue;

                    requestData.ExitCode = 311;
                    if (destinations == null || destinations.Length <= 0)
                        continue;
                    if (destinationStartIndex < 0 || destinationStartIndex >= destinations.Length)
                        continue;
                    if (destinationLength <= 0 || destinationStartIndex + destinationLength > destinations.Length)
                        continue;

                    requestData.ExitCode = 312;
                    string[] modes = requestData.Modes;
                    if (modes == null || modes.Length <= 0)
                        continue;

                    requestData.ExitCode = 313;
                    Point[,,] geoData = requestData.GeoData;
                    if (geoData == null ||
                        geoData.GetLength(0) != origins.Length ||
                        geoData.GetLength(1) != destinations.Length ||
                        geoData.GetLength(2) < modes.Length)
                        continue;

                    // 3.2 Строим запрос
                    rc = 32;
                    requestData.ExitCode = rc;
                    // https://yandex.ru/routing/doc/distance_matrix/concepts/structure.html
                    //
                    // GET https://api.routing.yandex.net/v2/distancematrix
                    //      ?apikey =<string>
                    //      &origins =<lat1,lon1|lat2,lon2|...>
                    //      &destinations =<lat1,lon1|lat2,lon2|...>
                    //      &[mode=<string>]
                    //      &[departure_time=<integer>]
                    //      &[avoid_tolls=<boolean>]
                    //      &[weight=<float>]
                    //      &[axle_weight=<float>]
                    //      &[max_weight=<float>]
                    //      &[height=<float>]
                    //      &[width=<float>]
                    //      &[length=<float>]
                    //      &[payload=<float>]
                    //      &[eco_class=<integer>]
                    //      &[has_trailer=<boolean>]
                    //-------------------------------------------------------- 
                    //  Шаблон url: https://api.routing.yandex.net/v2/distancematrix?apikey={0}&origins={1}&destinations={2}&mode={3}
                    //              {0} - apikey;
                    //              {1} - origins;
                    //              {2} - destinations;
                    //              {3} - mode.

                    // 3.3 Строим origins-аргумент
                    rc = 33;
                    requestData.ExitCode = rc;
                    string originsArg;
                    sb.Length = 0;

                    sb.AppendFormat(CultureInfo.InvariantCulture, geoPointPattern, origins[originStartIndex].Latitude, origins[originStartIndex].Longitude);
                    int originCount = 1;

                    for (int j = originStartIndex + 1; j < originStartIndex + originLength; j++)
                    {
                        sb.Append('|');
                        sb.AppendFormat(CultureInfo.InvariantCulture, geoPointPattern, origins[j].Latitude, origins[j].Longitude);
                        originCount++;
                    }

                    originsArg = sb.ToString();

                    // 3.4 Строим destinations-аргумент
                    rc = 34;
                    requestData.ExitCode = rc;
                    string destinationsArg;
                    sb.Length = 0;

                    sb.AppendFormat(CultureInfo.InvariantCulture, geoPointPattern, destinations[destinationStartIndex].Latitude, destinations[destinationStartIndex].Longitude);
                    int destinationCount = 1;

                    for (int j = destinationStartIndex + 1; j < destinationStartIndex + destinationLength; j++)
                    {
                        sb.Append('|');
                        sb.AppendFormat(CultureInfo.InvariantCulture, geoPointPattern, destinations[j].Latitude, destinations[j].Longitude);
                        destinationCount++;
                    }

                    destinationsArg = sb.ToString();

                    // 3.5 Цикл получения данных по режиму
                    rc = 35;
                    //JsonSerializer serializer = JsonSerializer.Create();

                    for (int m = 0; m < modes.Length; m++)
                    {
                        string url = string.Format(getUrl, apiKey, originsArg, destinationsArg, modes[m]);
                        Logger.WriteToLog(123, MessageSeverity.Info, string.Format(Messages.MSG_123, modes[m], originCount, destinationCount, url));

                        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                        request.Method = "GET";
                        request.UserAgent = USER_AGENT;
                        request.Accept = "application/json";
                        request.ContentType = "application/json";
                        //request.Timeout = 10000;
                        request.Timeout = context.ResponseTimeout;

                        // 3.5.1 Отправляем запрос
                        rc = 351;

                        using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                        {
                            if (response.StatusCode != HttpStatusCode.OK)
                            {
                                // 3.5.2 Http-статус
                                rc = 352;
                                requestData.HttpStatusCode = -(int)response.StatusCode;
                                requestData.ErrorMessage = response.StatusDescription;
                                Logger.WriteToLog(124, MessageSeverity.Error, string.Format(Messages.MSG_124, response.StatusCode, response.StatusDescription));
                                return;
                            }
                            else
                            {
                                // 3.5.3 Читаем отклик
                                rc = 353;
                                using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                                {
                                    string json = reader.ReadToEnd();

                                    // 3.5.4 Отклик с ошибками
                                    rc = 354;
                                    string errors = GeoYandexResponseParser.GetErrorMessages(json);
                                    if (errors != null)
                                    {
                                        requestData.ExitCode = rc;
                                        requestData.ErrorMessage = errors;
                                        return;
                                    }

                                    // 3.5.5 Извлекаем данные из отклика
                                    rc = 355;
                                    GeoYandexResponseItem[] items;
                                    int rc1 = GeoYandexResponseParser.TryParse(json, out items);
                                    if (rc1 != 0)
                                    {
                                        requestData.ErrorMessage = json;
                                        rc = 1000000 * rc + rc1;
                                        return;
                                    }

                                    // 3.5.6 Проверяем число полученных элементов данных
                                    rc = 356;
                                    if (items.Length != originLength * destinationLength)
                                    {
                                        requestData.ErrorMessage = json;
                                        return;
                                    }

                                    // 3.5.7 Переносим данные в массив результата
                                    rc = 357;
                                    int k = 0;

                                    for (int i = originStartIndex; i < originStartIndex + originLength; i++)
                                    {
                                        for (int j = destinationStartIndex; j < destinationStartIndex + destinationLength; j++)
                                        {
                                            GeoYandexResponseItem item = items[k++];
                                            if ("OK".Equals(item.status, StringComparison.InvariantCultureIgnoreCase))
                                            {
                                                geoData[i, j, m].X = item.distance;
                                                geoData[i, j, m].Y = item.duration;
                                            }
                                            else
                                            {
                                                geoData[i, j, m].X = -1;
                                                geoData[i, j, m].Y = -1;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                // 4. Выход - Ok
                rc = 0;
            }
            catch (Exception ex)
            {
                Logger.WriteToLog(669, MessageSeverity.Error, string.Format(Messages.MSG_669, $"{nameof(GeoYandex)}.{nameof(GeoYandex.GeoThread)}", rc, (ex.InnerException == null ? ex.Message : ex.InnerException.Message)));
            }
            finally
            {
                if (context != null)
                {
                    context.ExitCode = rc;
                    if (context.SyncEvent != null)
                        context.SyncEvent.Set();
                }
            }
        }
    }
}
