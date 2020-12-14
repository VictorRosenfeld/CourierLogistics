
namespace LogisticsService.API
{
    using LogisticsService.Log;
    using Newtonsoft.Json;
    using System;
    using System.IO;
    using System.Linq;
    using System.Net;

    /// <summary>
    /// Запрос событий курьеров
    /// </summary>
    public class GetCourierEvents
    {
        /// <summary>
        /// URL Get запроса с параметром RequestType ≠ 1
        /// </summary>
        private const string URL0 = "{0}events/courier?service_id={1}";

        /// <summary>
        /// URL Get запроса с параметром RequestType = 1
        /// </summary>
        private const string URL1 = "{0}events/courier?service_id={1}&all=1";

        /// <summary>
        /// Запрос всех событий курьеров
        /// </summary>
        /// <param name="requestType">Тип запроса: 0 - только новые; 1 - все события</param>
        /// <param name="events">Полученные события</param>
        /// <returns>0 - запрос успешно выполнен; иначе - выполнить запрос не удалось</returns>
        public static int GetEvents(int requestType, out CourierEvent[] events)
        {
            // 1. Инициализация
            int rc = 1;
            events = null;

            try
            {
                // 2. Строим Get-запрос
                rc = 2;
                HttpWebRequest request = null;
                if (requestType == 1)
                {
                    request = (HttpWebRequest)WebRequest.Create(string.Format(URL1, RequestParameters.API_ROOT, RequestParameters.SERVICE_ID));
                }
                else
                {
                    request = (HttpWebRequest)WebRequest.Create(string.Format(URL0, RequestParameters.API_ROOT, RequestParameters.SERVICE_ID));
                }

                request.Method = "GET";
                request.UserAgent = RequestParameters.USER_AGENT;
                request.Accept = RequestParameters.HEADER_ACCEPT;
                request.ContentType = RequestParameters.HEADER_CONTENT_TYPE;
                request.Headers.Add(RequestParameters.HEADER_AUTHORIZATION);
                request.Timeout = RequestParameters.TIMEOUT;
                request.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
                //Logger.WriteToLog(request.Address.OriginalString);
                Logger.WriteToLog(string.Format(MessagePatterns.COURIER_EVENTS_REQUEST, request.Address.OriginalString));

                // 3. Посылаем Get-запрос и обрабатываем отклик
                rc = 3;
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        Logger.WriteToLog(string.Format(MessagePatterns.COURIER_EVENTS_ERROR_RESPONSE, response.StatusCode, response.StatusDescription));
                        throw new HttpListenerException((int)response.StatusCode, response.StatusDescription);
                    }

                    using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                    {
                        JsonSerializerSettings settings = new JsonSerializerSettings();
                        settings.NullValueHandling = NullValueHandling.Ignore;
                        JsonSerializer serializer = JsonSerializer.Create(settings);
                        //events = (CourierEvent[])serializer.Deserialize(reader, typeof(CourierEvent[]));

                        string json = reader.ReadToEnd();
                        //Logger.WriteToLog(json);
                        Logger.WriteToLog(string.Format(MessagePatterns.COURIER_EVENTS_RESPONSE, json));

                        using (StringReader sr = new StringReader(json))
                        {
                            events = (CourierEvent[])serializer.Deserialize(sr, typeof(CourierEvent[]));
                        }
                    }
                }

                if (events != null && events.Length > 0)
                    SendAck.Send(events.Select(ev => ev.id).ToArray());

                // 4. Выход - Ok
                rc = 0;
                return rc;
            }
            catch
            {
                return rc;
            }
        }
    }

    /// <summary>
    /// Отклик с событиями курьеров
    /// </summary>
    public class CourierEvents
    {
        /// <summary>
        /// События курьеров, полученные из запроса
        /// </summary>
        public CourierEvent[] Events { get; set; }

        /// <summary>
        /// Количество событий
        /// </summary>
        public int Count => Events == null ? 0 : Events.Length;
    }

    /// <summary>
    /// Событие курьера
    /// </summary>
    public class CourierEvent
    {
        /// <summary>
        /// Id события
        /// </summary>
        public int id { get; set; }

        /// <summary>
        /// Id курьера
        /// </summary>
        public int courier_id { get; set; }

        /// <summary>
        /// Тип курьера:
        /// "walking" - пеший;
        /// "cycling" - вело;
        /// "driving" - авто;
        /// </summary>
        public string courier_type { get; set; }

        /// <summary>
        /// Тип события:
        /// 0 - курьер доступен;
        /// 1 - прибытие в магазин
        /// 2 - подтверждение начала доставки (не обрабатывается)
        /// 3 - выполняет доставку
        /// 4 - не используется
        /// 5 - завершение работы
        /// 6 - не доступен (не может использоваться для доставки)
        /// </summary>
        public int type { get; set; }

        /// <summary>
        /// Время начала работы по графику
        /// </summary>
        public DateTime work_start { get; set; }

        /// <summary>
        /// Время завершения работы по графику
        /// </summary>
        public DateTime work_end { get; set; }

        /// <summary>
        /// Id магазина
        /// (имеет смысл для типов 0, 1, 2)
        /// </summary>
        public int shop_id { get; set; }

        /// <summary>
        /// Широта местоположения курьера на момент запроса
        /// </summary>
        public double geo_lat { get; set; }

        /// <summary>
        /// Долгота местоположения курьера на момент запроса
        /// </summary>
        public double geo_lon { get; set; }

        /// <summary>
        /// Время, когда произошло событие
        /// тип 0   - время прибытия в магазин
        /// тип 1   - время прибытия в магазин
        /// тип 2   - время подтверждения доставки
        /// тип 3   - время запроса состояния
        /// тип 4   - не определено
        /// тип 5   - время завершения работы
        /// тип 6   - время перехода в состояние 'не доступен'
        /// </summary>
        /// 
        public DateTime date_event { get; set; }
    }
}
