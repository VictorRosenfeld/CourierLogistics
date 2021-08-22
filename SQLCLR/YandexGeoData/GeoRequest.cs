using SQLCLR.Deliveries;
using SQLCLR.YandexGeoData;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
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

    private static int GetGeoContext(GeoRequestArgs requestArgs, int pairLimit, out GeoContext[] geoContext)
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
                yandexModes[allModeCount++] = MODE_DRIVING;
            }

            if (hasWalking)
            {
                yandexModes[yandexModeCount++] = MODE_WALKING;
                yandexModes[allModeCount++] = MODE_WALKING;
            }

            if (hasTransit)
            {
                yandexModes[yandexModeCount++] = MODE_TRANSIT;
                yandexModes[allModeCount++] = MODE_TRANSIT;
            }

            if (hasTruck)
            {
                yandexModes[yandexModeCount++] = MODE_TRUCK;
                yandexModes[allModeCount++] = MODE_TRUCK;
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

            // 6. Два частных случая - один или два запроса
            rc = 6;
            int pairLimit2 = pairLimit + pairLimit;

            if (originCount * destinationCount <= pairLimit)
            {
                geoContext = new GeoContext[] { new GeoContext(origins, 0, originCount, destinations, 0, destinationCount, yandexModes, geoData, null) };
                return rc = 0;
            }
            if (originCount * destinationCount <= pairLimit2)
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

            // 7. Выделяем память под результат
            rc = 7;
            int size = (originCount * destinationCount + pairLimit - 1) / pairLimit + 2;
            GeoContext[] result = new GeoContext[size];
            int count = 0;

            // 8. Делим destination-точки на группы по pairLimit точек + остаток (от 0 до pairLimit - 1);
            rc = 8;
            int destinationGroupCount = destinationCount / pairLimit;
            int destinationReminder = destinationCount % pairLimit;
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

            if (destinationReminder == 0)
                goto Fin;

            //                          [origins]
            //                              ↓
            //                    [destinationRemainder]     (0 < destinationRemainder < pairLimit)
            //                     ↑
            //           (destinationStartIndex)

            // 9. Делим origin-точки на группы по pairLimit точек + остаток (от 0 до pairLimit - 1);
            rc = 9;
            int originGroupCount = originCount / pairLimit;
            int originReminder = originCount % pairLimit;
            int originStartIndex = pairLimit * originGroupCount;

            if (originCount > 0)
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

            if (originReminder == 0)
                goto Fin;

            //                   (originStartIndex)
            //                           ↓
            //                          [originReminder]        (0 < originRemainder < pairLimit)
            //                                 ↓
            //                       [destinationRemainder]     (0 < destinationRemainder < pairLimit)
            //                        ↑
            //              (destinationStartIndex)


            // 10. Цикл дальнейшего построения
            rc = 10;

            while (originReminder > 0 && destinationReminder > 0)
            {
                // 10.1 Два частных случая - один или два запроса
                rc = 101;
                size = originReminder * destinationReminder;

                if (size <= pairLimit)
                {
                    result[count++] = new GeoContext(origins, originStartIndex, originReminder, destinations, destinationStartIndex, destinationReminder, yandexModes, geoData, null);
                    break;
                }
                else if (size <= pairLimit2)
                {
                    if ((originReminder % 2) == 0)
                    {
                        int length = originReminder / 2;
                        result[count++] = new GeoContext(origins, originStartIndex, length, destinations, destinationStartIndex, destinationReminder, yandexModes, geoData, null);
                        result[count++] = new GeoContext(origins, originStartIndex + length, originReminder - length, destinations, destinationStartIndex, destinationReminder, yandexModes, geoData, null);
                    }
                    else if ((destinationCount % 2) == 0)
                    {
                        int length = destinationReminder / 2;
                        result[count++] = new GeoContext(origins, originStartIndex, originReminder, destinations, destinationStartIndex, length, yandexModes, geoData, null);
                        result[count++] = new GeoContext(origins, originStartIndex, originReminder, destinations, destinationStartIndex + length, destinationReminder - length, yandexModes, geoData, null);
                    }
                    else
                    {
                        int length = originReminder / 2;
                        result[count++] = new GeoContext(origins, originStartIndex, length, destinations, destinationStartIndex, destinationReminder, yandexModes, geoData, null);
                        result[count++] = new GeoContext(origins, originStartIndex + length, originReminder - length, destinations, destinationStartIndex, destinationReminder, yandexModes, geoData, null);
                    }

                    break;
                }

                // 10.2 Случай size = originReminder * destinationReminder > 2 * pairLimit
                rc = 102;


            }



        Fin:

            return rc;
        }
        catch
        {
            return rc;
        }
    }

    private static int[] GetDivisors(int number)
    {
        // 1. Инициализация
        
        try
        {
            if ()
        }
        catch
        {

        }
    }


}


