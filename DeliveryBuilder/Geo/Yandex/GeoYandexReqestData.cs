
namespace DeliveryBuilder.Geo.Yandex
{
    /// <summary>
    /// Контекст одного запроса гео-данных Yandex
    /// </summary>
    public class YandexRequestData
    {
        #region Координаты исходных точек запроса

        /// <summary>
        /// Координаты исходных точек
        /// </summary>
        public GeoPoint[] Origins { get; private set; }

        /// <summary>
        /// Индекс первой точки исходного интервала
        /// </summary>
        public int StartOrginIndex { get; private set; }

        /// <summary>
        /// Длина исходного интервала
        /// </summary>
        public int OriginLength { get; private set; }

        #endregion Координаты исходных точек запроса

        #region Координаты точек назначения запроса

        /// <summary>
        /// Координаты точек назначения
        /// </summary>
        public GeoPoint[] Destinations { get; private set; }

        /// <summary>
        /// Индекс первой точки интервала назначения
        /// </summary>
        public int StartDestinationIndex { get; private set; }

        /// <summary>
        /// Длина интервала назначения
        /// </summary>
        public int DestinationLength { get; private set; }

        #endregion Координаты точек назначения запроса

        /// <summary>
        /// Способы передвижения
        /// </summary>
        public string[] Modes { get; set; }

        /// <summary>
        /// Попарные гео-данные результата
        /// (размерность Origins.Length x Destinations.Length x Modes.Length)
        /// </summary>
        public Point[,,] GeoData { get; private set; }

        /// <summary>
        /// Код возврата
        /// (0 - данные получены; иначе - данные не получены)
        /// </summary>
        public int ExitCode { get; set; }

        /// <summary>
        /// Status Code Http-запроса
        /// </summary>
        public int HttpStatusCode { get; set; }

        /// <summary>
        /// Status Description Http-запроса или Erros-сообщение Yandex
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Параметрический конструктор класса GeoContext
        /// </summary>
        /// <param name="origins">Координаты исходных точек</param>
        /// <param name="startOriginIndex">Индекс первой точки исходного интервала</param>
        /// <param name="originLength">Длина исходного интервала</param>
        /// <param name="destinations">Координаты точек назначения</param>
        /// <param name="startDestinatioIndex">Индекс первой точки интервала назначения</param>
        /// <param name="destinationLength">Длина интервала назначения</param>
        /// <param name="modes">Способы передвижения</param>
        /// <param name="geoData">Попарные гео-данные результата (размерность Origins.Length x Destinations.Length x Modes.Length)</param>
        public YandexRequestData(GeoPoint[] origins, int startOriginIndex, int originLength,
                          GeoPoint[] destinations, int startDestinatioIndex, int destinationLength,
                          string[] modes, Point[,,] geoData)
        {
            Origins = origins;
            StartOrginIndex = startOriginIndex;
            OriginLength = originLength;
            Destinations = destinations;
            StartDestinationIndex = startDestinatioIndex;
            DestinationLength = destinationLength;
            Modes = modes;
            GeoData = geoData;
        }
    }
}
