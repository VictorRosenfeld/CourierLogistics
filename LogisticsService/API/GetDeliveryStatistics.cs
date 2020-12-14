
namespace LogisticsService.API
{
    using LogisticsService.Log;
    using Newtonsoft.Json;
    using System;
    using System.IO;
    using System.Net;
    using System.Text;

    /// <summary>
    /// Среднее время доставки заказа
    /// </summary>
    public class GetDeliveryStatistics
    {
        /// <summary>
        /// URL Post запроса
        /// </summary>
        private const string URL = "{0}statistics";

        /// <summary>
        /// Запрос средней стоимости доставки заказа
        /// </summary>
        /// <param name="requestData">Post request data</param>
        /// <param name="responseData">Post response data</param>
        /// <returns>0 - запрос успешно выполнен; иначе - выполнить запрос не удалось</returns>
        public static int GetOrderDeliveryCost(StatisticsRequest requestData, out StatisticsResponse responseData)
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
                Logger.WriteToLog(request.Address.OriginalString);

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

                Logger.WriteToLog(postData);

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
                        //string json = reader.ReadToEnd();
                        responseData = (StatisticsResponse)serializer.Deserialize(reader, typeof(StatisticsResponse));
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

    /// <summary>
    /// Данные Post-запроса
    /// </summary>
    public class StatisticsRequest
    {
        /// <summary>
        /// Способы доставки
        /// </summary>
        public int[] types { get; set; }

        /// <summary>
        /// Id магазина
        /// </summary>
        public int shop_id { get; set; }

        /// <summary>
        /// Расчетный день
        /// </summary>
        public DateTime date_target { get; set; }
    }

    /// <summary>
    /// Данные отклика Post-запроса
    /// </summary>
    public class StatisticsResponse
    {
        /// <summary>
        /// Id магазина
        /// </summary>
        public int shop_id { get; set; }

        /// <summary>
        /// Расчетный день
        /// </summary>
        public DateTime date_target { get; set; }

        /// <summary>
        /// Средняя стоимость по типам
        /// </summary>
        public OrderDeliveryCost[] type_cost { get; set; }
    }

    /// <summary>
    /// Средняя стоимость для типа
    /// </summary>
    public class OrderDeliveryCost
    {
        /// <summary>
        /// Тип доставки
        /// </summary>
        public int type { get; set; }

        /// <summary>
        /// Средняя стоимость доставки
        /// </summary>
        public float cost { get; set; }
    }
}
