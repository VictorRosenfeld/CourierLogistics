
namespace LogisticsService.API
{
    /// <summary>
    /// Общие параметры запроса к серверу
    /// </summary>
    internal class RequestParameters
    {
        /// <summary>
        ///  User agent
        /// </summary>
        internal const string USER_AGENT = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/74.0.3729.169 Safari/537.36";

        /// <summary>
        /// Параметр авторизации
        /// </summary>
        internal const string HEADER_AUTHORIZATION = "Authorization:Basic UE9TZXJ2aWNlOmRmcjE1Z3l0WVQ=";

        /// <summary>
        /// Параметр Accept заголовка
        /// </summary>
        internal const string HEADER_ACCEPT = "application/json";

        /// <summary>
        /// Параметр ContentType заголовка
        /// </summary>
        internal const string HEADER_CONTENT_TYPE = "application/json";

        /// <summary>
        /// Время ожидания отклика
        /// </summary>
        internal const int TIMEOUT = 30000;

        internal static string API_ROOT { get; set; }

        /// <summary>
        /// ID клиента
        /// </summary>
        internal static int SERVICE_ID { get; set; }
    }
}
