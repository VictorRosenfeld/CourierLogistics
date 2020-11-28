
namespace LogisticsService.API
{
    using LogisticsService.Log;
    using Newtonsoft.Json;
    using System.IO;
    using System.Net;
    using System.Text;

    /// <summary>
    /// Подтверждение получения события
    /// </summary>
    public class SendAck
    {
        /// <summary>
        /// URL Post запроса
        /// </summary>
        private const string URL = "https://telegram.it-stuff.ru/POService/hs/test/events/{0}/received";
        private const string URLx = "https://telegram.it-stuff.ru/POService/hs/test/events/received";

        /// <summary>
        /// Подтверждение получения события
        /// </summary>
        /// <param name="eventId"Id подтверждаемого события</param>
        /// <returns>0 - запрос успешно выполнен; иначе - выполнить запрос не удалось</returns>
        public static int Send(int eventId)
        {
            // 1. Инициализация
            int rc = 1;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;

                // 3. Строим Post-запрос
                rc = 3;
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(string.Format(URL, eventId));
                request.Method = "POST";
                request.UserAgent = RequestParameters.USER_AGENT;
                request.Accept = RequestParameters.HEADER_ACCEPT;
                request.ContentType = RequestParameters.HEADER_CONTENT_TYPE;
                request.Headers.Add(RequestParameters.HEADER_AUTHORIZATION);
                request.Timeout = RequestParameters.TIMEOUT;
                request.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
                Helper.WriteToLog(string.Format(MessagePatterns.SEND_ACK_REQUEST, request.Address.OriginalString));

                request.ContentLength = 0;

                // 4. Посылаем Post-запрос и обрабатываем отклик
                rc = 4;
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        Helper.WriteToLog(string.Format(MessagePatterns.SEND_ACK_ERROR_RESPONSE, response.StatusCode, response.StatusDescription));
                        throw new HttpListenerException((int)response.StatusCode, response.StatusDescription);
                    }

                    using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                    {
                        string json = reader.ReadToEnd();
                        Helper.WriteToLog(string.Format(MessagePatterns.SEND_ACK_RESPONSE, json));

                        using (StringReader sr = new StringReader(json))
                        {
                            JsonSerializer serializer = JsonSerializer.Create();
                            PostResponse rsp = (PostResponse)serializer.Deserialize(sr, typeof(PostResponse));
                            if (rsp == null)
                                return rc;
                            if (rsp.result != 0)
                                return rc = 10000 * rc + rsp.result;
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
        /// Post-отклик
        /// </summary>
        private class PostResponse
        {
            /// <summary>
            /// Код отклика
            /// </summary>
            public int result { get; set; }
        }

        /// <summary>
        /// Подтверждение получения события
        /// </summary>
        /// <param name="eventId"Id подтверждаемых события</param>
        /// <returns>0 - запрос успешно выполнен; иначе - выполнить запрос не удалось</returns>
        public static int Send(int[] eventId)
        {
            // 1. Инициализация
            int rc = 1;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (eventId == null || eventId.Length <= 0)
                    return rc;

                // 3. Строим Post-запрос
                rc = 3;
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(URLx);
                request.Method = "POST";
                request.UserAgent = RequestParameters.USER_AGENT;
                request.Accept = RequestParameters.HEADER_ACCEPT;
                request.ContentType = RequestParameters.HEADER_CONTENT_TYPE;
                request.Headers.Add(RequestParameters.HEADER_AUTHORIZATION);
                request.Timeout = RequestParameters.TIMEOUT;
                request.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
                Helper.WriteToLog(string.Format(MessagePatterns.SEND_ACK_REQUEST, request.Address.OriginalString));

                string postData;
                JsonSerializer serializer = JsonSerializer.Create();
                using (StringWriter sw = new StringWriter())
                {
                    serializer.Serialize(sw, eventId);
                    sw.Close();
                    postData = sw.ToString();
                }

                //Helper.WriteToLog(postData);
                Helper.WriteToLog(string.Format(MessagePatterns.SEND_ACK_POST_DATA, postData));

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
                        Helper.WriteToLog(string.Format(MessagePatterns.SEND_ACK_ERROR_RESPONSE, response.StatusCode, response.StatusDescription));
                        throw new HttpListenerException((int)response.StatusCode, response.StatusDescription);
                    }

                    using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                    {
                        //PostResponse rsp = (PostResponse)serializer.Deserialize(reader, typeof(PostResponse));
                        string json = reader.ReadToEnd();
                        //Helper.WriteToLog(json);
                        Helper.WriteToLog(string.Format(MessagePatterns.SEND_ACK_RESPONSE, json));

                        using (StringReader sr = new StringReader(json))
                        {
                            PostResponse rsp = (PostResponse)serializer.Deserialize(sr, typeof(PostResponse));
                            if (rsp == null)
                                return rc;
                            if (rsp.result != 0)
                                return rc = 10000 * rc + rsp.result;
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
    }
}
