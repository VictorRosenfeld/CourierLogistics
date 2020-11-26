
namespace LogisticsService.API
{
    using Newtonsoft.Json;
    using System.IO;
    using System.Net;
    using System.Text;

    /// <summary>
    /// Информация о времени перемещения
    /// и растоянии между заданными точками
    /// </summary>
    public class GetShippingInfo
    {
        /// <summary>
        /// URL Post запроса
        /// </summary>
        private const string URL = "{0}get-routes";

        /// <summary>
        /// Запрос данных 
        /// </summary>
        /// <param name="requestData">Post request data</param>
        /// <param name="responseData">Post response data</param>
        /// <returns>0 - запрос успешно выполнен; иначе - выполнить запрос не удалось</returns>
        public static int GetInfo(ShippingInfoRequest requestData, out ShippingInfoResponse responseData)
        {
            // 1. Инициализация
            int rc = 1;
            responseData = null;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (requestData == null)
                    return rc;

                // 3. Строим Post-запрос
                rc = 3;
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(string.Format(URL, RequestParameters.API_ROOT));
                request.Method = "POST";
                request.UserAgent = RequestParameters.USER_AGENT;
                request.Accept = RequestParameters.HEADER_ACCEPT;
                request.ContentType = RequestParameters.HEADER_CONTENT_TYPE;
                request.Headers.Add(RequestParameters.HEADER_AUTHORIZATION);
                request.Timeout = RequestParameters.TIMEOUT;
                request.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;

                string postData;
                JsonSerializerSettings settings = new JsonSerializerSettings();
                settings.NullValueHandling = NullValueHandling.Ignore;
                JsonSerializer serializer = JsonSerializer.Create(settings);
                using (StringWriter sw = new StringWriter())
                {
                    serializer.Serialize(sw, requestData);
                    sw.Close();
                    postData = sw.ToString();
                }

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
                        throw new HttpListenerException((int)response.StatusCode, response.StatusDescription);

                    using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                    {
                        responseData = (ShippingInfoResponse)serializer.Deserialize(reader, typeof(ShippingInfoResponse));
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
        /// Запрос данных 
        /// </summary>
        /// <param name="requestData">Post request data</param>
        /// <param name="responseData">Post response data</param>
        /// <returns>0 - запрос успешно выполнен; иначе - выполнить запрос не удалось</returns>
        public static int GetInfo(ShippingInfoRequestEx requestData, out ShippingInfoResponse responseData)
        {
            // 1. Инициализация
            int rc = 1;
            responseData = null;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (requestData == null)
                    return rc;

                // 3. Строим Post-запрос
                rc = 3;
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(string.Format(URL, RequestParameters.API_ROOT));
                request.Method = "POST";
                request.UserAgent = RequestParameters.USER_AGENT;
                request.Accept = RequestParameters.HEADER_ACCEPT;
                request.ContentType = RequestParameters.HEADER_CONTENT_TYPE;
                request.Headers.Add(RequestParameters.HEADER_AUTHORIZATION);
                request.Timeout = RequestParameters.TIMEOUT;
                request.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
                Helper.WriteToLog(request.Address.OriginalString);

                string postData;
                JsonSerializerSettings settings = new JsonSerializerSettings();
                settings.NullValueHandling = NullValueHandling.Ignore;
                JsonSerializer serializer = JsonSerializer.Create(settings);
                using (StringWriter sw = new StringWriter())
                {
                    serializer.Serialize(sw, requestData);
                    sw.Close();
                    postData = sw.ToString();
                }

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
                        throw new HttpListenerException((int)response.StatusCode, response.StatusDescription);

                    using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                    {
                        //responseData = (ShippingInfoResponse)serializer.Deserialize(reader, typeof(ShippingInfoResponse));
                        string json = reader.ReadToEnd();
                        Helper.WriteToLog(json);
                        if (string.IsNullOrEmpty(json))
                            return rc;

                        using (StringReader sr = new StringReader(json))
                        {
                            responseData = (ShippingInfoResponse)serializer.Deserialize(sr, typeof(ShippingInfoResponse));
                        }
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
        /// Post-данные для
        /// запроса времени движения и расстояний
        /// между заданными точками
        /// </summary>
        public class ShippingInfoRequest
        {
            /// <summary>
            /// Типы способов доставки
            /// walking - пеший курьер;
            /// cycling - курьер на велосипеде;
            /// driving - курьер на авто
            /// </summary>
            public string[] modes { get; set; }

            /// <summary>
            /// Точки заданные широтой и долготой:
            /// points[i] = {latitude_i, longitude_i}
            /// </summary>
            public double[][] points { get; set; }

            /// <summary>
            /// Количество точек
            /// </summary>
            [JsonIgnore]
            public int Count => points == null ? 0 : points.Length;
        }

        /// <summary>
        /// Post-данные отклика для
        /// запроса времени движения и расстояний
        /// между заданными точками
        /// </summary>
        public class ShippingInfoResponse
        {
            /// <summary>
            /// Расстояния и время для типа walking
            /// </summary>
            public PointsInfo[][] walking { get; set; }

            /// <summary>
            /// Расстояния и время для типа cycling
            /// </summary>
            public PointsInfo[][] cycling { get; set; }

            /// <summary>
            /// Расстояния и время для типа driving
            /// </summary>
            public PointsInfo[][] driving { get; set; }
        }

        /// <summary>
        /// Инфофрмация о времени движения и
        /// расстоянии между точками
        /// </summary>
        public class PointsInfo
        {
            /// <summary>
            /// Расстояние между точками, м
            /// </summary>
            public int distance { get; set; }

            /// <summary>
            /// Время движения, сек
            /// </summary>
            public int duration { get; set; }
        }


        /// <summary>
        /// Post-данные для
        /// запроса времени движения и расстояний
        /// между заданными точками
        /// </summary>
        public class ShippingInfoRequestEx
        {
            /// <summary>
            /// Типы способов доставки
            /// walking - пеший курьер;
            /// cycling - курьер на велосипеде;
            /// driving - курьер на авто
            /// </summary>
            public string[] modes { get; set; }

            /// <summary>
            /// Исходные точки заданные широтой и долготой:
            /// points[i] = {latitude_i, longitude_i}
            /// </summary>
            public double[][] origins { get; set; }

            /// <summary>
            /// Точки назначения заданные широтой и долготой:
            /// points[i] = {latitude_i, longitude_i}
            /// </summary>
            public double[][] destinations { get; set; }

            /// <summary>
            /// Количество исходных точек
            /// </summary>
            [JsonIgnore]
            public int OriginCount => origins == null ? 0 : origins.Length;

            /// <summary>
            /// Количество точек назначения
            /// </summary>
            [JsonIgnore]
            public int DestinationCount => destinations == null ? 0 : destinations.Length;
        }

    }
}
