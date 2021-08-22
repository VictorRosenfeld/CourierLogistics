//using SQLCLR.Deliveries;
using SQLCLR.YandexGeoData;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Drawing;
using System.IO;
using System.Text;
using System.Xml.Serialization;

public partial class GeoRequest
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
    ///      <mode type = "driving" >
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

    public static int Request(SqlString getUrl, SqlString apiKey, SqlInt32 pair_limit, SqlDouble cycling_duration_ratio, SqlXml request, out SqlXml response)
    {
        // 1. Инициализация
        int rc = 1;
        response = new SqlXml();

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

            // 4. Проверяем параметры запроса
            rc = 4;
            if (requestArgs.origins == null || requestArgs.origins.Length <= 0)
                return rc;
            if (requestArgs.destinations == null || requestArgs.destinations.Length <= 0)
                return rc;
            if (requestArgs.modes == null || requestArgs.modes.Length <= 0)
                return rc;

            bool hasDriving = false;
            bool hasWalking = false;
            bool hasCycling = false;
            bool hasTransit = false;

            for (int i = 0; i < requestArgs.modes.Length; i++)
            {
                string mode = requestArgs.modes[i];

                if (mode == "driving")
                { hasDriving = true; }
                else if (mode == "walking")
                { hasWalking = true; }
                else if (mode == "cycling")
                { hasCycling = true; }
                else if (mode == "transit")
                { hasTransit = true; }
            }

            if (!hasDriving && !hasWalking && !hasCycling && !hasTransit)
                return rc;

            // 5. Обрабатываем режимы
            rc = 5;
            string[] yandexModes = new string[3];
            string[] modes = new string[4];
            int yandexModeCount = 0;
            int modeCount = 0;

            if (hasDriving)
            {
                yandexModes[yandexModeCount++] = "driving";
                yandexModes[modeCount++] = "driving";
            }

            if (hasWalking)
            {
                yandexModes[yandexModeCount++] = "walking";
                yandexModes[modeCount++] = "walking";
            }

            if (hasTransit)
            {
                yandexModes[yandexModeCount++] = "transit";
                yandexModes[modeCount++] = "transit";
                modeCount++;
            }

            if (hasCycling)
            {
                modeCount++;
                yandexModes[modeCount++] = "cycling";
                if (!hasWalking)
                    yandexModes[yandexModeCount++] = "walking";
            }

            if (modeCount < modes.Length)
            {
                Array.Resize(ref modes, modeCount);
            }

            if (yandexModeCount < yandexModes.Length)
            {
                Array.Resize(ref yandexModes, yandexModeCount);
            }

            // 6. Строим контексты для всех запросов к Yandex
            
            int originCount = (requestArgs.origins == null ? 0 : requestArgs.origins.Length);
            int destinationCount = (requestArgs.destinations == null ? 0 : requestArgs.destinations.Length);



            // 4. Строим последовательность запросов для получения всех данных
            rc = 4;
            if (originCount <= 0 || destinationCount <= 0)
                return rc;






            return rc;
        }
        catch
        {
            return rc;
        }
    }

    public static int GetGeoContext(GeoRequestArgs requestArgs, int pairLimit, out GeoContext[] geoContext)
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

            if (hasDriving)
            {
                yandexModes[yandexModeCount++] = MODE_DRIVING;
                allModes[allModeCount++] = MODE_DRIVING;
            }

            if (hasWalking)
            {
                yandexModes[yandexModeCount++] = MODE_WALKING;
                allModes[allModeCount++] = MODE_WALKING;
            }

            if (hasTransit)
            {
                yandexModes[yandexModeCount++] = MODE_TRANSIT;
                allModes[allModeCount++] = MODE_TRANSIT;
            }

            if (hasTruck)
            {
                yandexModes[yandexModeCount++] = MODE_TRUCK;
                allModes[allModeCount++] = MODE_TRUCK;
            }

            if (hasCycling)
            {
                allModes[allModeCount++] = MODE_CYCLING;

                if (!hasWalking)
                    yandexModes[yandexModeCount++] = MODE_WALKING;
            }

            if (allModeCount < allModes.Length)
            {
                Array.Resize(ref allModes, allModeCount);
            }

            if (yandexModeCount < yandexModes.Length)
            {
                Array.Resize(ref yandexModes, yandexModeCount);
            }

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
                geoContext = new GeoContext[] { new GeoContext(origins, 0, originCount, destinations, 0, destinationCount, yandexModes, geoData, null) };
                return rc = 0;
            }
            else if (originCount * destinationCount <= pairLimit2)
            {
                if ((originCount % 2) == 0)
                {
                    int length = originCount / 2;
                    geoContext = new GeoContext[] {
                        new GeoContext(origins, 0, length, destinations, 0, destinationCount, yandexModes, geoData, null),
                        new GeoContext(origins, length, originCount - length, destinations, 0, destinationCount, yandexModes, geoData, null)
                    };
                }
                else if ((destinationCount % 2) == 0)
                {
                    int length = destinationCount / 2;
                    geoContext = new GeoContext[] {
                        new GeoContext(origins, 0, originCount, destinations, 0, length, yandexModes, geoData, null),
                        new GeoContext(origins, 0, originCount, destinations, length, destinationCount - length, yandexModes, geoData, null)
                    };
                }
                else
                {
                    int length = originCount / 2;
                    geoContext = new GeoContext[] {
                        new GeoContext(origins, 0, length, destinations, 0, destinationCount, yandexModes, geoData, null),
                        new GeoContext(origins, length, originCount - length, destinations, 0, destinationCount, yandexModes, geoData, null)
                    };
                }

                return rc = 0;
            }
            else if (pairLimit == 1)
            {
                geoContext = new GeoContext[originCount * destinationCount];
                int cnt = 0;

                for (int i = 0; i < originCount; i++)
                {
                    for (int j = 0; j < destinationCount; j++)
                    {
                        geoContext[cnt++] = new GeoContext(origins, i, 1, destinations, j, 1, yandexModes, geoData, null);
                    }
                }

                return rc = 0;
            }
            else if (pairLimit == 2)
            {
                geoContext = new GeoContext[(originCount * destinationCount + 1) / 2];
                int cnt = 0;

                for (int i = 0; i < originCount; i++)
                {
                    for (int j = 0; j < destinationCount - 1; j +=2)
                    {
                        geoContext[cnt++] = new GeoContext(origins, i, 1, destinations, j, 2, yandexModes, geoData, null);
                    }
                }

                if ((destinationCount % 2) != 0)
                {
                    for (int i = 0; i < originCount - 1; i += 2)
                    {
                        geoContext[cnt++] = new GeoContext(origins, i, 2, destinations, destinationCount - 1, 1, yandexModes, geoData, null);
                    }

                    if ((originCount % 2) != 0)
                    {
                        geoContext[cnt++] = new GeoContext(origins, originCount - 1, 1, destinations, destinationCount - 1, 1, yandexModes, geoData, null);
                    }
                }

                return rc = 0;
            }

            // 7. Выделяем память под результат
            rc = 7;
            int size = (originCount * destinationCount + pairLimit - 1) / pairLimit + 2;
            GeoContext[] result = new GeoContext[size];
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
                        result[count++] = new GeoContext(origins, i, 1, destinations, j, pairLimit, yandexModes, geoData, null);
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
                        result[count++] = new GeoContext(origins, j, pairLimit, destinations, i, 1, yandexModes, geoData, null);
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
                    result[count++] = new GeoContext(origins, originStartIndex, originRemainder, destinations, destinationStartIndex, destinationRemainder, yandexModes, geoData, null);
                    continue;
                }
                else if (size <= pairLimit2)
                {
                    if ((originRemainder % 2) == 0)
                    {
                        int length = originRemainder / 2;
                        result[count++] = new GeoContext(origins, originStartIndex, length, destinations, destinationStartIndex, destinationRemainder, yandexModes, geoData, null);
                        result[count++] = new GeoContext(origins, originStartIndex + length, originRemainder - length, destinations, destinationStartIndex, destinationRemainder, yandexModes, geoData, null);
                    }
                    else if ((destinationCount % 2) == 0)
                    {
                        int length = destinationRemainder / 2;
                        result[count++] = new GeoContext(origins, originStartIndex, originRemainder, destinations, destinationStartIndex, length, yandexModes, geoData, null);
                        result[count++] = new GeoContext(origins, originStartIndex, originRemainder, destinations, destinationStartIndex + length, destinationRemainder - length, yandexModes, geoData, null);
                    }
                    else
                    {
                        int length = originRemainder / 2;
                        result[count++] = new GeoContext(origins, originStartIndex, length, destinations, destinationStartIndex, destinationRemainder, yandexModes, geoData, null);
                        result[count++] = new GeoContext(origins, originStartIndex + length, originRemainder - length, destinations, destinationStartIndex, destinationRemainder, yandexModes, geoData, null);
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
                                    result[count++] = new GeoContext(origins, j, x, destinations, k, y, yandexModes, geoData, null);
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
                                    result[count++] = new GeoContext(origins, j, y, destinations, k, x, yandexModes, geoData, null);
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
                            result[count++] = new GeoContext(origins, i, minX, destinations, j, minY, yandexModes, geoData, null);
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

    ///// <summary>
    ///// Выбор всех делителей заданного числа
    ///// не превосходящих порог
    ///// </summary>
    ///// <param name="number">Заданное число</param>
    ///// <param name="divLim">Верхняя граница для делителей</param>
    ///// <returns>Делители или null</returns>
    //private static int[] GetDivisors(int number, int divLim)
    //{
    //    // 1. Инициализация
        
    //    try
    //    {
    //        // 2. Проверяем исходные данные
    //        if (number <= 0)
    //            return null;
    //        if (divLim < 1)
    //            return null;
    //        if (divLim > number)
    //            divLim = number;

    //        // 3. Цикл построения всех делителей
    //        int[] divisors = new int[divLim / 2 + 2];
    //        divisors[0] = 1;
    //        int count = 1;

    //        for (int i = 2; i <= divLim; i++)
    //        {
    //            if ((number % i) == 0)
    //                divisors[count++] = i;
    //        }

    //        if (count < divisors.Length)
    //        {
    //            Array.Resize(ref divisors, count);
    //        }

    //        // 4. Выход - Ok
    //        return divisors;
    //    }
    //    catch
    //    {
    //        return null;
    //    }
    //}

    /// <summary>
    /// Для заданного числа number выбор пар чисел x, y
    /// таких, что 
    ///            (1)    0 ≤ number - x * y ≤ differenceLim (y ≥ x > 0)
    /// </summary>
    /// <param name="number">Заданное число</param>
    /// <param name="differenceLim">Порог для разницы в неравенстве (1)</param>
    /// <returns>Найденные множители или null</returns>
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


