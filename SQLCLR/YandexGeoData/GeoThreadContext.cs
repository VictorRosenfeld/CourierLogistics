
namespace SQLCLR.YandexGeoData
{
    using System.Threading;

    /// <summary>
    /// Контекст потока получения гео-данных Yandex
    /// </summary>
    public class GeoThreadContext
    {
        /// <summary>
        /// Url запроса Yandex
        /// </summary>
        public string GetUrl { get; private set; }

        /// <summary>
        /// API Key
        /// </summary>
        public string ApiKey { get; private set; }

        /// <summary>
        /// Данные запросов
        /// </summary>
        public YandexRequestData[] RequestData { get; private set; }

        /// <summary>
        /// Индекс первого обрбатываемого запроса
        /// </summary>
        public int StartIndex { get; private set; }

        /// <summary>
        /// Шаг обработки запросов
        /// </summary>
        public int Step { get; private set; }

        /// <summary>
        /// Объект синхронизации
        /// </summary>
        public ManualResetEvent SyncEvent { get; set; }

        /// <summary>
        /// Код возврата
        /// </summary>
        public int ExitCode { get; set; } 

        /// <summary>
        /// Параметрический конструктор класса GeoThreadContext
        /// </summary>
        /// <param name="getUrl">Url запроса Yandex</param>
        /// <param name="apiKey">API Key</param>
        /// <param name="requestData">Данные запросов</param>
        /// <param name="startIndex">Индекс первого обрбатываемого запроса</param>
        /// <param name="step"> Шаг обработки запросов</param>
        /// <param name="syncEvent">Объект синхронизации</param>
        public GeoThreadContext(string getUrl, string apiKey, YandexRequestData[] requestData, int startIndex, int step, ManualResetEvent syncEvent)
        {
            GetUrl = getUrl;
            ApiKey = apiKey;
            RequestData = requestData;
            StartIndex = startIndex;
            Step = step;
            SyncEvent = syncEvent;
            ExitCode = -1;
        }
    }
}
