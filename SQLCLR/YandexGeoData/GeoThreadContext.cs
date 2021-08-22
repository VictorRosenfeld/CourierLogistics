
namespace SQLCLR.YandexGeoData
{
    using System.Threading;

    /// <summary>
    /// �������� ������ ��������� ���-������ Yandex
    /// </summary>
    public class GeoThreadContext
    {
        /// <summary>
        /// Url ������� Yandex
        /// </summary>
        public string GetUrl { get; private set; }

        /// <summary>
        /// API Key
        /// </summary>
        public string ApiKey { get; private set; }

        /// <summary>
        /// ������ ��������
        /// </summary>
        public YandexRequestData[] RequestData { get; private set; }

        /// <summary>
        /// ������ ������� �������������� �������
        /// </summary>
        public int StartIndex { get; private set; }

        /// <summary>
        /// ��� ��������� ��������
        /// </summary>
        public int Step { get; private set; }

        /// <summary>
        /// ������ �������������
        /// </summary>
        public ManualResetEvent SyncEvent { get; set; }

        /// <summary>
        /// ��� ��������
        /// </summary>
        public int ExitCode { get; set; } 

        /// <summary>
        /// ��������������� ����������� ������ GeoThreadContext
        /// </summary>
        /// <param name="getUrl">Url ������� Yandex</param>
        /// <param name="apiKey">API Key</param>
        /// <param name="requestData">������ ��������</param>
        /// <param name="startIndex">������ ������� �������������� �������</param>
        /// <param name="step"> ��� ��������� ��������</param>
        /// <param name="syncEvent">������ �������������</param>
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
