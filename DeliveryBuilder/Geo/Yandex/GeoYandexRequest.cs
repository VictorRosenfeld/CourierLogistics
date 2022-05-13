
namespace DeliveryBuilder.Geo.Yandex
{
    /// <summary>
    /// Исходные данные и результат запроса гео-данных Yandex
    /// </summary>
    public struct GeoYandexRequest
    {
        /// <summary>
        /// Способы передвижения
        /// </summary>
        public string[] Modes { get; private set; }

        /// <summary>
        /// Координаты исходных точек
        /// </summary>
        public GeoPoint[] Origins { get; set; }

        /// <summary>
        /// Координаты точек назначения
        /// </summary>
        public GeoPoint[] Destinations { get; set; }

        /// <summary>
        /// Параметрический конструктор структуры GeoYandexRequest
        /// </summary>
        /// <param name="modes">Способы передвижения</param>
        /// <param name="origins">Координаты исходных точек</param>
        /// <param name="destinations">Координаты точек назначения</param>
        public GeoYandexRequest(string[] modes, GeoPoint[] origins, GeoPoint[] destinations)
        {
            Modes = modes;
            Origins = origins;
            Destinations = destinations;
            Result = null;
        }

        /// <summary>
        /// Результат запроса данных
        /// Данные для режима m, 
        /// исходной точки i и
        /// точки назначения j
        /// будут находиться в элементе:
        /// Result[i, j, m].X - расстояние, метров
        /// Result[i, j, m].Y - длительность, сек.
        /// Если данные не получены, то
        /// Result[i, j, m].X = -1
        /// Result[i, j, m].Y = -1
        /// </summary>
        public Point[,,] Result { get; set; }
    }



}
