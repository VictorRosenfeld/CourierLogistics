using Microsoft.SqlServer.Server;
using YandexGeo.Request;
using YandexGeo.Response;
using System;
using System.Data.SqlTypes;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Xml.Serialization;

public class StoredProcedures
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
    /// Запрос гео-данных Yandex
    /// </summary>
    /// <param name="getUrl">Url GET-запроса</param>
    /// <param name="apiKey"></param>
    /// <param name="pair_limit">Огранчение на количество возвращаемых элементов за один запрос</param>
    /// <param name="cycling_duration_ratio">Коэффициент пересчета cycling-времени из walking-времени</param>
    /// <param name="request">Xml-данные запроса</param>
    /// <param name="response">Xml-отклик</param>
    /// <returns>0 - запрос успешно выполнен; иначе - запрос не выполнен</returns>
    ///-------------------------------------------------------------------------------------------------------
    /// Формат request
    ///   <yandex>
    ///      <modes>
    ///         <mode>driving</mode>
    ///         <mode>walking</mode>
    ///         <mode>cycling</mode>*)
    ///         <mode>transit</mode>
    ///         <mode>truck</mode>
    ///      </modes>
    ///      <origins>
    ///         <point lat = "..." lon="..."/>
    ///                    . . .
    ///         <point lat = "..." lon="..."/>
    ///      </origins>
    ///      <destinations>
    ///         <point lat = "..." lon="..."/>
    ///                    . . .
    ///         <point lat = "..." lon="..."/>
    ///      </destinations>
    ///   </yandex>
    ///- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - 
    /// *) Yandex не поддерживает режим cycling. 
    ///    Режим cycling рассчитывается на основе режима walking:
    ///          cycling.distance = walking.distance
    ///          cycling.duration = cycling_duration_ratio * walking.duration
    ///-------------------------------------------------------------------------------------------------------
    /// Формат response *)
    ///   <yandex>
    ///      <mode type = "driving">
    ///         <row>
    ///           <data distance="..." duration="..."/>
    ///                          . . .
    ///   	      <data distance="..." duration="..."/>
    ///   	    </row>
    ///   	                 .  .  .
    ///         <row>
    ///           <data distance="..." duration="..."/>
    ///                          . . .
    ///   	      <data distance="..." duration="..."/>
    ///   	    </row>
    ///      </mode>
    ///      <mode type = "walking" >
    ///         <row>
    ///           <data distance="..." duration="..."/>
    ///                          . . .
    ///   	      <data distance="..." duration="..."/>
    ///   	    </row>
    ///   	                 .  .  .
    ///         <row>
    ///           <data distance="..." duration="..."/>
    ///                          . . .
    ///   	      <data distance="..." duration="..."/>
    ///   	    </row>
    ///     </mode>
    ///   </yandex>
    ///- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - 
    ///  *) Если рассстояние и время не получены,
    ///    то в тэге data устанавливаются следующие значения:
    ///
    ///    <data distance = "-1" duration="-1"/>
    ///-------------------------------------------------------------------------------------------------------
    [SqlProcedure]
    public static int YandexGeoRequest(SqlString getUrl, SqlString apiKey, SqlInt32 pair_limit, SqlDouble cycling_duration_ratio, SqlXml request, out SqlXml response)
    {
        // 1. Инициализация
        int rc = 1;
        int rc1 = 1;
        response = null;
        GeoThreadContext[] contexts = null;

        try
        {
            // 2. Проверяем исходные данные
            rc = 2;
            if (getUrl.IsNull)
                return rc;
            if (apiKey.IsNull)
                return rc;
            if (pair_limit.IsNull || pair_limit.Value <= 0)
                return rc;
            if (cycling_duration_ratio.Value <= 0 || cycling_duration_ratio.Value > 1)
                return rc;
            if (request == null || request.IsNull)
                return rc;

            // 3. Извлекаем параметры запроса
            rc = 3;
            GeoRequestArgs requestArgs;
            XmlSerializer serializer = new XmlSerializer(typeof(GeoRequestArgs));
            using (StringReader sr = new StringReader(request.Value))
            { requestArgs = (GeoRequestArgs)serializer.Deserialize(sr); }

            // 4. Строим котексты запросов
            rc = 4;
            YandexRequestData[] requestData;
            rc1 = GetGeoContext(requestArgs, pair_limit.Value, out requestData);
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
                GeoThreadContext context = new GeoThreadContext(getUrl.Value, apiKey.Value, requestData, 0, 1, null);
                GeoThread(context);
                rc1 = context.ExitCode;
            }
            else
            {
                contexts = new GeoThreadContext[threadCount];

                for (int i = 0; i < threadCount; i++)
                {
                    int m = i;
                    contexts[m] = new GeoThreadContext(getUrl.Value, apiKey.Value, requestData, i, threadCount, new ManualResetEvent(false));
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

            // 8. Заполняем cycling-способ передвижения
            rc = 8;
            int originCount = requestArgs.origins.Length;
            int destinationCount = requestArgs.destinations.Length;

            if (requestArgs.HasCycling)
            {
                int walkingIndex = requestArgs.WalkingIndex;
                int cyclingIndex = requestArgs.CyclingIndex;
                double c = cycling_duration_ratio.Value;

                if (cyclingIndex == walkingIndex)
                {
                    for (int i = 0; i < originCount; i++)
                    {
                        for (int j = 0; j < originCount; j++)
                        {
                            geoData[i, j, cyclingIndex].Y = (int)(c * geoData[i, j, cyclingIndex].Y);
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < originCount; i++)
                    {
                        for (int j = 0; j < originCount; j++)
                        {
                            geoData[i, j, cyclingIndex].X = geoData[i, j, walkingIndex].X;
                            geoData[i, j, cyclingIndex].Y = (int)(c * geoData[i, j, walkingIndex].Y);
                        }
                    }
                }
            }

            // 9. Создаём отклик с результатом
            rc = 9;
            StringBuilder sbXml = new StringBuilder(15000);
            string[] mode = new string[] { MODE_DRIVING, MODE_CYCLING, MODE_WALKING, MODE_TRANSIT, MODE_TRUCK };
            int[] modeIndex = new int[] { requestArgs.DrivingIndex, requestArgs.CyclingIndex, requestArgs.WalkingIndex, requestArgs.TransitIndex, requestArgs.TransitIndex };

            sbXml.Append("<yandex>");

            for (int m = 0; m < mode.Length; m++)
            {
                // 9.1 Пропускаем не заданные режимы
                rc = 91;
                int index = modeIndex[m];
                if (index < 0)
                    continue;

                // 9.2 Добавляем открывающий тэг <mode ... >
                rc = 92;
                sbXml.Append($@"<mode type=""{mode[m]}"">");

                // 9.3 Добавляем данные для текущего способа передвижения
                rc = 93;
                for (int i = 0; i < originCount; i++)
                {
                    for (int j = 0; j < destinationCount; j++)
                    {
                        Point pt = geoData[i, j, index];
                        sbXml.Append($@"<data distance=""{pt.X}"" duration=""{pt.Y}""/>");
                    }
                }

                // 9.4 Добавляем закрывающий тэг </mode>
                rc = 94;
                sbXml.Append(@"</mode>");
            }

            // 9.5 Добавляем закрывающий тэг </yandex>
            rc = 95;
            sbXml.Append("</yandex>");

            // 10. Присваиваем результат
            rc = 10;

            MemoryStream ms = new MemoryStream(Encoding.Unicode.GetBytes(sbXml.ToString()));
            response = new SqlXml(ms);
            ms = null;

            // 11. Выход - Ok
            rc = 0;
            return rc;
        }
        catch
        {
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
        GeoThreadContext context = status as GeoThreadContext;

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
                YandexRequestData requestData = context.RequestData[contextIndex];
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

                sb.AppendFormat(CultureInfo.InvariantCulture, geoPointPattern, origins[originStartIndex].lat, origins[originStartIndex].lon);

                for (int j = originStartIndex + 1; j < originStartIndex + originLength; j++)
                {
                    sb.Append('|');
                    sb.AppendFormat(CultureInfo.InvariantCulture, geoPointPattern, origins[j].lat, origins[j].lon);
                }

                originsArg = sb.ToString();

                // 3.4 Строим destinations-аргумент
                rc = 34;
                requestData.ExitCode = rc;
                string destinationsArg;
                sb.Length = 0;

                sb.AppendFormat(CultureInfo.InvariantCulture, geoPointPattern, destinations[destinationStartIndex].lat, destinations[destinationStartIndex].lon);

                for (int j = destinationStartIndex + 1; j < destinationStartIndex + destinationLength; j++)
                {
                    sb.Append('|');
                    sb.AppendFormat(CultureInfo.InvariantCulture, geoPointPattern, destinations[j].lat, destinations[j].lon);
                }

                destinationsArg = sb.ToString();

                // 3.5 Цикл получения данных по режиму
                rc = 35;
                //JsonSerializer serializer = JsonSerializer.Create();

                for (int m = 0; m < modes.Length; m++)
                {
                    string url = string.Format(getUrl, apiKey, originsArg, destinationsArg, modes[m]);
                    //#if debug
                    //                    Logger.WriteToLog(1008, $"YandexGeoRequest -> GeoThread url = {url}", 0);
                    //#endif

                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                    request.Method = "GET";
                    request.UserAgent = USER_AGENT;
                    request.Accept = "application/json";
                    request.ContentType = "application/json";
                    request.Timeout = 10000;

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
                                string errors = YandexResponseParser.GetErrorMessages(json);
                                if (errors != null)
                                {
                                    requestData.ExitCode = rc;
                                    requestData.ErrorMessage = errors;
                                    return;
                                }

                                // 3.5.5 Извлекаем данные из отклика
                                rc = 355;
                                YandexResponseItem[] items;
                                int rc1 = YandexResponseParser.TryParse(json, out items);
                                if (rc1 != 0)
                                {
                                    requestData.ErrorMessage = json;
                                    rc = 1000000 * rc + rc1;
                                    return;
                                }

                                // 3.5.6 Проверяем число полученных жлементов данных
                                rc = 356;
                                if (items.Length != originLength * destinationLength)
                                {
                                    requestData.ErrorMessage = json;
                                    return;
                                }

                                // 3.5.7 Переносим данные в массив результата
                                rc = 356;
                                int k = 0;

                                for (int i = originStartIndex; i < originStartIndex + originLength; i++)
                                {
                                    for (int j = destinationStartIndex; j < destinationStartIndex + destinationLength; j++)
                                    {
                                        YandexResponseItem item = items[k++];
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
        catch
        {

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

    /// <summary>
    /// Построение контекстов запросов к Yandex
    /// </summary>
    /// <param name="requestArgs">Данные для запросов</param>
    /// <param name="pairLimit">Максимальное количество пар в одном запросе</param>
    /// <param name="geoContext">Результат - контексты запросов</param>
    /// <returns>0 - контексты построены; контексты не построены</returns>
    public static int GetGeoContext(GeoRequestArgs requestArgs, int pairLimit, out YandexRequestData[] geoContext)
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
            if (requestArgs == null)
                return rc;
            if (requestArgs.origins == null || requestArgs.origins.Length <= 0)
                return rc;
            if (requestArgs.destinations == null || requestArgs.destinations.Length <= 0)
                return rc;
            if (requestArgs.modes == null || requestArgs.modes.Length <= 0)
                return rc;

            // 3. Устанавливаем флаги заданных способов передвижения
            rc = 3;
            bool hasDriving = false;
            bool hasWalking = false;
            bool hasCycling = false;
            bool hasTransit = false;
            bool hasTruck = false;

            for (int i = 0; i < requestArgs.modes.Length; i++)
            {
                string mode = requestArgs.modes[i];

                if (mode == MODE_DRIVING)
                { hasDriving = true; }
                else if (mode == MODE_CYCLING)
                { hasCycling = true; }
                else if (mode == MODE_WALKING)
                { hasWalking = true; }
                else if (mode == MODE_TRANSIT)
                { hasTransit = true; }
                else if (mode == MODE_TRUCK)
                { hasTruck = true; }
            }

            if (!hasDriving && !hasWalking && !hasCycling && !hasTransit)
                return rc;

            requestArgs.HasDriving = hasDriving;
            requestArgs.HasCycling = hasCycling;
            requestArgs.HasWalking = hasWalking;
            requestArgs.HasTransit = hasTransit;
            requestArgs.HasTruck = hasTruck;

            // 4. Строим необходимые способы передвижения Yandex и все способы передвижения 
            rc = 4;
            string[] yandexModes = new string[4];
            string[] allModes = new string[5];
            int yandexModeCount = 0;
            int allModeCount = 0;
            //
            int drivingIndex = -1;
            int walkingIndex = -1;
            int cyclingIndex = -1;
            int truckIndex = -1;
            int transitIndex = -1;

            if (hasDriving)
            {
                drivingIndex = yandexModeCount;
                yandexModes[yandexModeCount++] = MODE_DRIVING;
                allModes[allModeCount++] = MODE_DRIVING;
            }

            if (hasWalking)
            {
                walkingIndex = yandexModeCount;
                yandexModes[yandexModeCount++] = MODE_WALKING;
                allModes[allModeCount++] = MODE_WALKING;
            }

            if (hasTransit)
            {
                transitIndex = yandexModeCount;
                yandexModes[yandexModeCount++] = MODE_TRANSIT;
                allModes[allModeCount++] = MODE_TRANSIT;
            }

            if (hasTruck)
            {
                truckIndex = yandexModeCount;
                yandexModes[yandexModeCount++] = MODE_TRUCK;
                allModes[allModeCount++] = MODE_TRUCK;
            }

            if (hasCycling)
            {
                cyclingIndex = yandexModeCount;
                allModes[allModeCount++] = MODE_CYCLING;

                if (!hasWalking)
                {
                    walkingIndex = yandexModeCount;
                    yandexModes[yandexModeCount++] = MODE_WALKING;
                }
            }

            if (allModeCount < allModes.Length)
            {
                Array.Resize(ref allModes, allModeCount);
            }

            if (yandexModeCount < yandexModes.Length)
            {
                Array.Resize(ref yandexModes, yandexModeCount);
            }

            requestArgs.DrivingIndex = drivingIndex;
            requestArgs.CyclingIndex = cyclingIndex;
            requestArgs.WalkingIndex = walkingIndex;
            requestArgs.TransitIndex = transitIndex;
            requestArgs.TruckIndex = truckIndex;

            // 5. Выделяем память под результат
            rc = 5;
            GeoPoint[] origins = requestArgs.origins;
            GeoPoint[] destinations = requestArgs.destinations;
            int originCount = origins.Length;
            int destinationCount = destinations.Length;
            Point[,,] geoData = new Point[originCount, destinationCount, allModeCount];

            // 6. Два частных случая - один или два запроса и два частных случая pairLimit = 1, 2
            rc = 6;
            int pairLimit2 = pairLimit + pairLimit;

            if (originCount * destinationCount <= pairLimit)
            {
                geoContext = new YandexRequestData[] { new YandexRequestData(origins, 0, originCount, destinations, 0, destinationCount, yandexModes, geoData) };
                return rc = 0;
            }
            else if (originCount * destinationCount <= pairLimit2)
            {
                if ((originCount % 2) == 0)
                {
                    int length = originCount / 2;
                    geoContext = new YandexRequestData[] {
                        new YandexRequestData(origins, 0, length, destinations, 0, destinationCount, yandexModes, geoData),
                        new YandexRequestData(origins, length, originCount - length, destinations, 0, destinationCount, yandexModes, geoData)
                    };
                }
                else if ((destinationCount % 2) == 0)
                {
                    int length = destinationCount / 2;
                    geoContext = new YandexRequestData[] {
                        new YandexRequestData(origins, 0, originCount, destinations, 0, length, yandexModes, geoData),
                        new YandexRequestData(origins, 0, originCount, destinations, length, destinationCount - length, yandexModes, geoData)
                    };
                }
                else
                {
                    int length = originCount / 2;
                    geoContext = new YandexRequestData[] {
                        new YandexRequestData(origins, 0, length, destinations, 0, destinationCount, yandexModes, geoData),
                        new YandexRequestData(origins, length, originCount - length, destinations, 0, destinationCount, yandexModes, geoData)
                    };
                }

                return rc = 0;
            }
            else if (pairLimit == 1)
            {
                geoContext = new YandexRequestData[originCount * destinationCount];
                int cnt = 0;

                for (int i = 0; i < originCount; i++)
                {
                    for (int j = 0; j < destinationCount; j++)
                    {
                        geoContext[cnt++] = new YandexRequestData(origins, i, 1, destinations, j, 1, yandexModes, geoData);
                    }
                }

                return rc = 0;
            }
            else if (pairLimit == 2)
            {
                geoContext = new YandexRequestData[(originCount * destinationCount + 1) / 2];
                int cnt = 0;

                for (int i = 0; i < originCount; i++)
                {
                    for (int j = 0; j < destinationCount - 1; j += 2)
                    {
                        geoContext[cnt++] = new YandexRequestData(origins, i, 1, destinations, j, 2, yandexModes, geoData);
                    }
                }

                if ((destinationCount % 2) != 0)
                {
                    for (int i = 0; i < originCount - 1; i += 2)
                    {
                        geoContext[cnt++] = new YandexRequestData(origins, i, 2, destinations, destinationCount - 1, 1, yandexModes, geoData);
                    }

                    if ((originCount % 2) != 0)
                    {
                        geoContext[cnt++] = new YandexRequestData(origins, originCount - 1, 1, destinations, destinationCount - 1, 1, yandexModes, geoData);
                    }
                }

                return rc = 0;
            }

            // 7. Выделяем память под результат
            rc = 7;
            int size = (originCount * destinationCount + pairLimit - 1) / pairLimit + 2;
            YandexRequestData[] result = new YandexRequestData[size];
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
                        result[count++] = new YandexRequestData(origins, i, 1, destinations, j, pairLimit, yandexModes, geoData);
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
                        result[count++] = new YandexRequestData(origins, j, pairLimit, destinations, i, 1, yandexModes, geoData);
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
            GeoIterationData[] iterationData = new GeoIterationData[originRemainder * destinationRemainder];
            iterationData[0] = new GeoIterationData(originStartIndex, originRemainder, destinationStartIndex, destinationRemainder);
            int iterDataCount = 1;

            while (iterDataCount > 0)
            {
                // 11.0 Извлекаем параметры для очередной итерации
                rc = 110;
                GeoIterationData data = iterationData[--iterDataCount];
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
                    result[count++] = new YandexRequestData(origins, originStartIndex, originRemainder, destinations, destinationStartIndex, destinationRemainder, yandexModes, geoData);
                    continue;
                }
                else if (size <= pairLimit2)
                {
                    if ((originRemainder % 2) == 0)
                    {
                        int length = originRemainder / 2;
                        result[count++] = new YandexRequestData(origins, originStartIndex, length, destinations, destinationStartIndex, destinationRemainder, yandexModes, geoData);
                        result[count++] = new YandexRequestData(origins, originStartIndex + length, originRemainder - length, destinations, destinationStartIndex, destinationRemainder, yandexModes, geoData);
                    }
                    else if ((destinationCount % 2) == 0)
                    {
                        int length = destinationRemainder / 2;
                        result[count++] = new YandexRequestData(origins, originStartIndex, originRemainder, destinations, destinationStartIndex, length, yandexModes, geoData);
                        result[count++] = new YandexRequestData(origins, originStartIndex, originRemainder, destinations, destinationStartIndex + length, destinationRemainder - length, yandexModes, geoData);
                    }
                    else
                    {
                        int length = originRemainder / 2;
                        result[count++] = new YandexRequestData(origins, originStartIndex, length, destinations, destinationStartIndex, destinationRemainder, yandexModes, geoData);
                        result[count++] = new YandexRequestData(origins, originStartIndex + length, originRemainder - length, destinations, destinationStartIndex, destinationRemainder, yandexModes, geoData);
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
                                    result[count++] = new YandexRequestData(origins, j, x, destinations, k, y, yandexModes, geoData);
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
                                    result[count++] = new YandexRequestData(origins, j, y, destinations, k, x, yandexModes, geoData);
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
                            result[count++] = new YandexRequestData(origins, i, minX, destinations, j, minY, yandexModes, geoData);
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
                            iterationData[iterDataCount++] = new GeoIterationData(originStartIndex, originRemainder, destinationStartIndex1, destinationRemainder1);
                        }

                        if (r1 > 0)
                        {
                            // 11.5.1.2 r1 --> n2 * y
                            int originStartIndex1 = originStartIndex + n1 * minX;
                            int originRemainder1 = r1;
                            int destinationRemainder2 = n2 * minY;
                            iterationData[iterDataCount++] = new GeoIterationData(originStartIndex1, originRemainder1, destinationStartIndex, destinationRemainder2);
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
                            iterationData[iterDataCount++] = new GeoIterationData(originStartIndex1, originRemainder1, destinationStartIndex1, destinationRemainder1);
                        }

                        if (r1 > 0)
                        {
                            // 11.5.2.2 r1 --> destinationReminder = n2 * y + r2
                            int originStartIndex2 = originStartIndex + n1 * minX;
                            int originRemainder2 = r1;
                            iterationData[iterDataCount++] = new GeoIterationData(originStartIndex2, originRemainder2, destinationStartIndex, destinationRemainder);
                        }
                    }
                }

                WhileEnd:
                ;
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
}
