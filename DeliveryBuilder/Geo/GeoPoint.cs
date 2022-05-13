
namespace DeliveryBuilder.Geo
{
    /// <summary>
    /// Гео-точка
    /// </summary>
    public struct GeoPoint
    {
        /// <summary>
        /// Широта
        /// </summary>
        public double Latitude { get; private set; }

        /// <summary>
        /// Долгота
        /// </summary>
        public double Longitude { get; private set; }

        /// <summary>
        /// Параметрический конструктор структуры GeoPoint
        /// </summary>
        /// <param name="latitude">Широта</param>
        /// <param name="longitude">Долгота</param>
        public GeoPoint(double latitude, double longitude)
        {
            Latitude = latitude;
            Longitude = longitude;
        }
    }
}
