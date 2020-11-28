

namespace LogisticsService.API
{
    using LogisticsService.Log;
    using Newtonsoft.Json;
    using System;
    using System.IO;
    using System.Linq;
    using System.Net;

    public class GetOrderEvents
    {
        /// <summary>
        /// URL Get запроса с параметром RequestType ≠ 1
        /// </summary>
        private const string URL0 = "{0}events/order?service_id={1}";

        /// <summary>
        /// URL Get запроса с параметром RequestType = 1
        /// </summary>
        private const string URL1 = "{0}events/order?service_id={1}&all=1";

        /// <summary>1
        /// Запрос всех событий связанных с заказами
        /// </summary>
        /// <param name="requestType">Тип запроса: 0 - только новые; 1 - все события</param>
        /// <param name="events">Полученные события</param>
        /// <returns>0 - запрос успешно выполнен; иначе - выполнить запрос не удалось</returns>
        public static int GetEvents(int requestType, out OrderEvent[] events)
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
                //Helper.WriteToLog(request.Address.OriginalString);
                Helper.WriteToLog(string.Format(MessagePatterns.ORDER_EVENTS_REQUEST, request.Address.OriginalString));

                // 3. Посылаем Get-запрос и обрабатываем отклик
                rc = 3;
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        Helper.WriteToLog(string.Format(MessagePatterns.ORDER_EVENTS_ERROR_RESPONSE, response.StatusCode, response.StatusDescription));
                        throw new HttpListenerException((int)response.StatusCode, response.StatusDescription);
                    }

                    using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                    {
                        JsonSerializerSettings settings = new JsonSerializerSettings();
                        settings.NullValueHandling = NullValueHandling.Ignore;
                        JsonSerializer serializer = JsonSerializer.Create(settings);
                        //events = (OrderEvent[])serializer.Deserialize(reader, typeof(OrderEvent[]));

                        string json = reader.ReadToEnd();
                        //Helper.WriteToLog(json);
                        Helper.WriteToLog(string.Format(MessagePatterns.ORDER_EVENTS_RESPONSE, json));

                        using (StringReader sr = new StringReader(json))
                        {
                            events = (OrderEvent[])serializer.Deserialize(sr, typeof(OrderEvent[]));
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
        /// Событие связанное с заказом
        /// </summary>
        public class OrderEvent
        {
            /// <summary>
            /// Id события
            /// </summary>
            public int id { get; set; }

            /// <summary>
            /// Id заказа
            /// </summary>
            public int order_id { get; set; }

            /// <summary>
            /// Тип события:
            /// 0 - поступление заказа в магазин
            /// 1 - завершение сборки заказа
            /// 3 - заказ отменен
            /// </summary>
            public int type { get; set; }

            /// <summary>
            /// Id-магазина - владельца заказа
            /// </summary>
            public int shop_id { get; set; }

            /// <summary>
            /// Широта магазина
            /// </summary>
            public double shop_geo_lat { get; set; }

            /// <summary>
            /// Долгота магазина
            /// </summary>
            public double shop_geo_lon { get; set; }

            /// <summary>
            /// Широта точки вручения заказа
            /// </summary>
            public double geo_lat { get; set; }

            /// <summary>
            /// Долгота точки вручения заказа
            /// </summary>
            public double geo_lon { get; set; }

            /// <summary>
            /// Вес заказа, кг
            /// </summary>
            public double weight { get; set; }

            /// <summary>
            /// Требуемое время вручения от
            /// </summary>
            public DateTime delivery_frame_from { get; set; }

            /// <summary>
            /// Требуемое время вручения до
            /// </summary>
            public DateTime delivery_frame_to { get; set; }

            /// <summary>
            /// Время события:
            /// тип = 0  - время назначения заказа магазину
            /// тип = 1  - время сборки заказа в магазине
            /// тип = 3  - время отмены заказа
            /// </summary>
            public DateTime date_event { get; set; }

            /// <summary>
            /// Доступные для исполнения
            /// заказа сервисы и магазины
            /// </summary>
            public ShopService[] service_available { get; set; }
        }

        /// <summary>
        /// Доступный сервис
        /// </summary>
        public class ShopService
        {
            /// <summary>
            /// Id магазина
            /// </summary>
            public int shop_id { get; set; }

            /// <summary>
            /// Id сервиса
            /// </summary>
            public int dservice_id { get; set; }
        }

        //public class Rootobject
        //{
        //    public Class1[] Property1 { get; set; }
        //}

        //public class Class1
        //{
        //    public int id { get; set; }
        //    public int order_id { get; set; }
        //    public int type { get; set; }
        //    public int shop_id { get; set; }
        //    public float shop_geo_lat { get; set; }
        //    public float shop_geo_lon { get; set; }
        //    public float geo_lat { get; set; }
        //    public float geo_lon { get; set; }
        //    public float weight { get; set; }
        //    public DateTime delivery_frame_from { get; set; }
        //    public DateTime delivery_frame_to { get; set; }
        //    public DateTime date_event { get; set; }
        //    public Service_Available[] service_available { get; set; }
        //}

        //public class Service_Available
        //{
        //    public int shop_id { get; set; }
        //    public int dservice_id { get; set; }
        //}
    }
}
