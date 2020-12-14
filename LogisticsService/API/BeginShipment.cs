
namespace LogisticsService.API
{
    using LogisticsService.Log;
    using Newtonsoft.Json;
    using System;
    using System.IO;
    using System.Net;
    using System.Text;

    /// <summary>
    /// Команда/рекомендация на отгрузку
    /// </summary>
    public class BeginShipment
    {
        /// <summary>
        /// URL Post запроса
        /// </summary>
        private const string URL = "{0}{1}/delivery-job";

        /// <summary>
        /// Передача отгрузки
        /// </summary>
        /// <param name="shipment">Отгрузка</param>
        /// <returns>0 - запрос успешно выполнен; иначе - выполнить запрос не удалось</returns>
        public static int Begin(Shipment shipment)
        {
            // 1. Инициализация
            int rc = 1;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (shipment == null)
                    return rc;

                // 3. Строим Post-запрос
                rc = 3;
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(string.Format(URL, RequestParameters.API_ROOT, RequestParameters.SERVICE_ID));
                request.Method = "POST";
                request.UserAgent = RequestParameters.USER_AGENT;
                request.Accept = RequestParameters.HEADER_ACCEPT;
                request.ContentType = RequestParameters.HEADER_CONTENT_TYPE;
                request.Headers.Add(RequestParameters.HEADER_AUTHORIZATION);
                request.Timeout = RequestParameters.TIMEOUT;
                request.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
                //Logger.WriteToLog(request.Address.OriginalString);
                Logger.WriteToLog(string.Format(MessagePatterns.BEGIN_SHIPMENT_REQUEST, request.Address.OriginalString));

                string postData;
                JsonSerializer serializer = JsonSerializer.Create();
                using (StringWriter sw = new StringWriter())
                {
                    serializer.Serialize(sw, new Shipment[] { shipment });
                    sw.Close();
                    postData = sw.ToString();
                }

                //Logger.WriteToLog(postData);
                Logger.WriteToLog(string.Format(MessagePatterns.BEGIN_SHIPMENT_POST_DATA, postData));

                byte[] byteArray = Encoding.UTF8.GetBytes(postData);
                request.ContentLength = byteArray.Length;

                using (Stream dataStream = request.GetRequestStream())
                {
                    dataStream.Write(byteArray, 0, byteArray.Length);
                    dataStream.Close();
                }

                // 4. Посылаем Post-запрос и обрабатываем отклик
                rc = 4;
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    if (response.StatusCode != HttpStatusCode.OK)
                    { 
                        Logger.WriteToLog(string.Format(MessagePatterns.BEGIN_SHIPMENT_ERROR_RESPONSE, response.StatusCode, response.StatusDescription));
                        throw new HttpListenerException((int)response.StatusCode, response.StatusDescription);
                    }

                    using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                    {

                        //PostResponse rsp = (PostResponse)serializer.Deserialize(reader, typeof(PostResponse));
                        PostResponse rsp;
                        string json = reader.ReadToEnd();
                        //Logger.WriteToLog(json);
                        Logger.WriteToLog(string.Format(MessagePatterns.BEGIN_SHIPMENT_RESPONSE, json));
                        
                        using (StringReader sr = new StringReader(json))
                        {
                            rsp = (PostResponse)serializer.Deserialize(reader, typeof(PostResponse));
                        }

                        if (rsp == null)
                            return rc;
                        if (rsp.result != 0)
                            return rc = 10000 * rc + rsp.result;
                    }
                }

                // 5. Выход - Ok
                rc = 0;
                return rc;
            }
            catch
            {
                return rc;
            }
        }

        /// <summary>
        /// Передача отгрузки
        /// </summary>
        /// <param name="shipment">Отгрузка</param>
        /// <returns>0 - запрос успешно выполнен; иначе - выполнить запрос не удалось</returns>
        public static int Begin(Shipment[] shipment)
        {
            // 1. Инициализация
            int rc = 1;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (shipment == null || shipment.Length <= 0)
                    return rc;

                // 3. Строим Post-запрос
                rc = 3;
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(string.Format(URL, RequestParameters.API_ROOT, RequestParameters.SERVICE_ID));
                request.Method = "POST";
                request.UserAgent = RequestParameters.USER_AGENT;
                request.Accept = RequestParameters.HEADER_ACCEPT;
                request.ContentType = RequestParameters.HEADER_CONTENT_TYPE;
                request.Headers.Add(RequestParameters.HEADER_AUTHORIZATION);
                request.Timeout = RequestParameters.TIMEOUT;
                request.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
                //Logger.WriteToLog(request.Address.OriginalString);
                Logger.WriteToLog(string.Format(MessagePatterns.BEGIN_SHIPMENT_REQUEST, request.Address.OriginalString));

                string postData;
                JsonSerializer serializer = JsonSerializer.Create();
                using (StringWriter sw = new StringWriter())
                {
                    serializer.Serialize(sw, shipment);
                    sw.Close();
                    postData = sw.ToString();
                }

                //Logger.WriteToLog(postData);
                Logger.WriteToLog(string.Format(MessagePatterns.BEGIN_SHIPMENT_POST_DATA, postData));

                byte[] byteArray = Encoding.UTF8.GetBytes(postData);
                request.ContentLength = byteArray.Length;

                using (Stream dataStream = request.GetRequestStream())
                {
                    dataStream.Write(byteArray, 0, byteArray.Length);
                    dataStream.Close();
                }

                // 4. Посылаем Post-запрос и обрабатываем отклик
                rc = 4;
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        Logger.WriteToLog(string.Format(MessagePatterns.BEGIN_SHIPMENT_ERROR_RESPONSE, response.StatusCode, response.StatusDescription));
                        throw new HttpListenerException((int)response.StatusCode, response.StatusDescription);
                    }

                    using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                    {
                        //string json = reader.ReadToEnd();
                        PostResponse rsp = (PostResponse)serializer.Deserialize(reader, typeof(PostResponse));
                        if (rsp == null)
                            return rc;
                        if (rsp.result != 0)
                            return rc = 10000 * rc + rsp.result;
                    }
                }

                // 5. Выход - Ok
                rc = 0;
                return rc;
            }
            catch
            {
                return rc;
            }
        }

        /// <summary>
        /// Отказ в отгрузке заказа
        /// </summary>
        /// <param name="rejectedOrders">Отвергнутые заказы</param>
        /// <returns>0 - запрос успешно выполнен; иначе - выполнить запрос не удалось</returns>
        public static int Reject(RejectedOrder[] rejectedOrders)
        {
            // 1. Инициализация
            int rc = 1;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (rejectedOrders == null || rejectedOrders.Length <= 0)
                    return rc;

                // 3. Строим Post-запрос
                rc = 3;
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(string.Format(URL, RequestParameters.API_ROOT, RequestParameters.SERVICE_ID));
                request.Method = "POST";
                request.UserAgent = RequestParameters.USER_AGENT;
                request.Accept = RequestParameters.HEADER_ACCEPT;
                request.ContentType = RequestParameters.HEADER_CONTENT_TYPE;
                request.Headers.Add(RequestParameters.HEADER_AUTHORIZATION);
                request.Timeout = RequestParameters.TIMEOUT;
                request.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
                //Logger.WriteToLog(request.Address.OriginalString);
                Logger.WriteToLog(string.Format(MessagePatterns.REJECT_ORDER_REQUEST, request.Address.OriginalString));

                string postData;
                JsonSerializer serializer = JsonSerializer.Create();
                using (StringWriter sw = new StringWriter())
                {
                    serializer.Serialize(sw, rejectedOrders);
                    sw.Close();
                    postData = sw.ToString();
                }

                //Logger.WriteToLog(postData);
                Logger.WriteToLog(string.Format(MessagePatterns.REJECT_ORDER_POST_DATA, postData));

                byte[] byteArray = Encoding.UTF8.GetBytes(postData);
                request.ContentLength = byteArray.Length;

                using (Stream dataStream = request.GetRequestStream())
                {
                    dataStream.Write(byteArray, 0, byteArray.Length);
                    dataStream.Close();
                }

                // 4. Посылаем Post-запрос и обрабатываем отклик
                rc = 4;
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        Logger.WriteToLog(string.Format(MessagePatterns.REJECT_ORDER_ERROR_RESPONSE, response.StatusCode, response.StatusDescription));
                        throw new HttpListenerException((int)response.StatusCode, response.StatusDescription);
                    }

                    using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                    {
                        //string json = reader.ReadToEnd();
                        PostResponse rsp = (PostResponse)serializer.Deserialize(reader, typeof(PostResponse));
                        if (rsp == null)
                            return rc;
                        if (rsp.result != 0)
                            return rc = 10000 * rc + rsp.result;
                    }
                }

                // 5. Выход - Ok
                rc = 0;
                return rc;
            }
            catch
            {
                return rc;
            }
        }

        /// <summary>
        /// Отгрузка
        /// </summary>
        public class Shipment
        {
            /// <summary>
            /// ID сервиса
            /// </summary>
            //public int service_id { get; set; }

            /// <summary>
            /// Id отгрузки (Guid)
            /// </summary>
            public string id { get; set; }

            /// <summary>
            /// Тип отгрузки
            /// 0 - команда на отгрузку
            /// 1 - рекомендуемая отгрузка
            /// </summary>
            public int status { get; set; }

            /// <summary>
            /// Id магазина, их которого осуществляется отгрузка
            /// </summary>
            public int shop_id { get; set; }

            /// <summary>
            /// Тип курьера
            /// 4  - пеший, вело или авто
            /// 12 - Gett-такси
            /// 14 - Yandex-такси
            /// </summary>
            public int delivery_service_id { get; set; }

            /// <summary>
            /// Id курьера
            /// (имеет смысл для 12 и 14; для 12 - 0; для 14 - 1)
            /// </summary>
            public int courier_id { get; set; }

            /// <summary>
            /// Назначенное время отгрузки
            /// </summary>
            public DateTime date_target { get; set; }

            /// <summary>
            /// Предельное время отгрузки для доставки в срок
            /// </summary>
            public DateTime date_target_end { get; set; }

            /// <summary>
            /// Id заказов, образующих отгрузку
            /// </summary>
            public int[] orders { get; set; }

            /// <summary>
            /// Информация об отгрузке
            /// </summary>
            public DeliveryInfo info { get; set; }

            /// <summary>
            /// Количество заказов
            /// </summary>
            [JsonIgnore]
            public int Count => orders == null ? 0 : orders.Length;
        }

        /// <summary>
        /// Отказ в отгрузке заказа
        /// </summary>
        public class RejectedOrder
        {
            /// <summary>
            /// Id отгрузки (Guid)
            /// </summary>
            public string id { get; set; }

            /// <summary>
            /// Тип отгрузки
            /// 0 - команда на отгрузку
            /// 1 - рекомендуемая отгрузка
            /// 2 - собранный заказ не может быть доставлен в срок
            /// 3 - не собранный заказ не может быть доставлен в срок
            /// </summary>
            public int status { get; set; }

            /// <summary>
            /// Id магазина, их которого осуществляется отгрузка
            /// </summary>
            public int shop_id { get; set; }

            /// <summary>
            /// Тип курьера
            /// 4  - пеший, вело или авто
            /// 12 - Gett-такси
            /// 14 - Yandex-такси
            /// </summary>
            public int delivery_service_id { get; set; }

            /// <summary>
            /// Id курьера
            /// (имеет смысл для 12 и 14; для 12 - 0; для 14 - 1)
            /// </summary>
            public int courier_id { get; set; }

            /// <summary>
            /// Назначенное время отгрузки
            /// </summary>
            public DateTime date_target { get; set; }

            /// <summary>
            /// Предельное время отгрузки для доставки в срок
            /// </summary>
            public DateTime date_target_end { get; set; }

            /// <summary>
            /// Id заказов, образующих отгрузку
            /// </summary>
            public int[] orders { get; set; }

            /// <summary>
            /// Информация об отклоненном заказе
            /// </summary>
            public RejectedInfo info { get; set; }

            /// <summary>
            /// Количество заказов
            /// </summary>
            [JsonIgnore]
            public int Count => orders == null ? 0 : orders.Length;
        }

        /// <summary>
        /// Информация об отгрузке
        /// </summary>
        public class DeliveryInfo
        {
            /// <summary>
            /// Время расчета отгрузки
            /// </summary>
            public DateTime calculationTime { get; set; }

            /// <summary>
            /// Стоимость всей отгрузки
            /// </summary>
            public double sum_cost { get; set; }

            /// <summary>
            /// Общий вес отгруженных заказов
            /// </summary>
            public double weight { get; set; }

            /// <summary>
            /// Флаг возврата в магазин
            /// </summary>
            public bool is_loop { get; set; }

            /// <summary>
            /// Расстояния и время движения между точками маршрута
            /// </summary>
            public NodeInfo[] nodeInfo { get; set; }

            /// <summary>
            /// Самое раннее время отгрузки
            /// </summary>
            public DateTime start_delivery_interval { get; set; }

            /// <summary>
            /// Самое позднее время отгрузки
            /// </summary>
            public DateTime end_delivery_interval { get; set; }

            /// <summary>
            /// Резерв времени на отгрузку
            /// </summary>
            public TimeSpan reserve_time { get; set; }

            /// <summary>
            /// Для каждой точки пути - время
            /// от начала отгрузки до вручения, мин
            /// </summary>
            public double[] node_delivery_time { get; set; }

            /// <summary>
            /// Время от момента начала отгрузки до вручения последнего заказа, мин
            /// </summary>
            public double delivery_time { get; set; }

            /// <summary>
            /// Время выполнения всей отгрузки, мин
            /// (Если isLoop = false, то execution_time = delivery_time;
            /// (Если isLoop = true, то execution_time = delivery_time + время возврата в магазин)
            /// </summary>
            public double execution_time { get; set; }
        }

        /// <summary>
        /// Информация об отклоненном заказе
        /// </summary>
        public class RejectedInfo
        {
            /// <summary>
            /// Время расчета отгрузки
            /// </summary>
            public DateTime calculationTime { get; set; }

            /// <summary>
            /// Общий вес отгруженных заказов
            /// </summary>
            public double weight { get; set; }

            /// <summary>
            /// Время доставки для всех
            /// доступных спсобов доставки
            /// </summary>
            public RejectedInfoItem[] delivery_method;
        }

        /// <summary>
        /// Данные о времени доставки для данного способа доставки
        /// </summary>
        public class RejectedInfoItem
        {
            /// <summary>
            /// Код способа доставки
            /// </summary>
            public int delivery_type;

            /// <summary>
            /// Причина отказа
            /// 0 - неопределенная
            /// 1 - поздняя сборка
            /// 2 - нет подходящего курьера
            /// 3 - превышен общий вес
            /// 4 - превышена дальность
            /// 5 - затянувшаяся отгрузка
            /// </summary>
            public int rejection_reason;

            /// <summary>
            /// Время от начала отгрузки до вручения заказа, сек
            /// </summary>
            public int delivery_time;
        }

        /// <summary>
        /// Расстояние и время движения между точками
        /// </summary>
        public class NodeInfo
        {
            /// <summary>
            /// Расстояние между точками, метров
            /// </summary>
            public int distance { get; set; }

            /// <summary>
            /// Время движения между точками
            /// </summary>
            public int duration { get; set; }
        }

        /// <summary>
        /// Post-отклик
        /// </summary>
        private class PostResponse
        {
            /// <summary>
            /// Код отклика
            /// </summary>
            public int result { get; set; }
        }
    }
}
