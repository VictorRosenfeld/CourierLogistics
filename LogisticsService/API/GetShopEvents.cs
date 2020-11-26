
namespace LogisticsService.API
{
    using Newtonsoft.Json;
    using System;
    using System.IO;
    using System.Linq;
    using System.Net;

    /// <summary>
    /// Запрос доступных магазинов
    /// </summary>
    public class GetShopEvents
    {
        /// <summary>
        /// URL Get запроса с параметром RequestType ≠ 1
        /// </summary>
        private const string URL0 = "{0}events/shop?service_id={1}";

        /// <summary>
        /// URL Get запроса с параметром RequestType = 1
        /// </summary>
        private const string URL1 = "{0}events/shop?service_id={1}&all=1";

        /// <summary>
        /// Запрос информации о магазинах
        /// </summary>
        /// <param name="requestType">Тип запроса: 0 - только новые; 1 - все доступные магазины</param>
        /// <param name="events">Полученные события</param>
        /// <returns>0 - запрос успешно выполнен; иначе - выполнить запрос не удалось</returns>
        public static int GetEvents(int requestType, out ShopEvent[] events)
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
                Helper.WriteToLog(request.Address.OriginalString);

                // 3. Посылаем Get-запрос и обрабатываем отклик
                rc = 3;
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    if (response.StatusCode != HttpStatusCode.OK)
                        throw new HttpListenerException((int)response.StatusCode, response.StatusDescription);

                    using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                    {
                        JsonSerializerSettings settings = new JsonSerializerSettings();
                        settings.NullValueHandling = NullValueHandling.Ignore;
                        JsonSerializer serializer = JsonSerializer.Create(settings);
                        //events = (ShopEvent[])serializer.Deserialize(reader, typeof(ShopEvent[]));

                        string json = reader.ReadToEnd();
                        Helper.WriteToLog(json);

                        using (StringReader sr = new StringReader(json))
                        {
                            events = (ShopEvent[])serializer.Deserialize(sr, typeof(ShopEvent[]));
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

        /// <summary>
        /// Событие магазина
        /// </summary>
        public class ShopEvent
        {
            /// <summary>
            /// Id события
            /// </summary>
            public int id { get; set; }

            /// <summary>
            /// Id магазина
            /// </summary>
            public int shop_id { get; set; }

            /// <summary>
            /// Время начала работы магазина
            /// </summary>
            public DateTime work_start { get; set; }

            /// <summary>
            /// Время завершения работы магазина
            /// </summary>
            public DateTime work_end { get; set; }

            /// <summary>
            /// Широта магазина
            /// </summary>
            public double geo_lat { get; set; }

            /// <summary>
            /// Долгота магазина
            /// </summary>
            public double geo_lon { get; set; }

            /// <summary>
            /// Время запроса
            /// </summary>
            public DateTime date_event { get; set; }
        }
    }
}
