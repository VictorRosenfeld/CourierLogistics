
namespace CourierLogistics.Logistics.FloatSolution.CourierService.ServiceQueue
{
    using CourierLogistics.Logistics.FloatSolution.CourierInLive;

    /// <summary>
    /// Аргументы элемента очереди событий типа MovedToPoint
    /// </summary>
    public class QueueItemMovedToPointArgs
    {
        /// <summary>
        /// Курьер, который осуществил доставку
        /// </summary>
        public CourierEx Courier { get; private set; }

        /// <summary>
        /// Широта целевой точки
        /// </summary>
        public double Latitude { get; private set; }

        /// <summary>
        /// Долгота целевой точки
        /// </summary>
        public double Longitude { get; private set; }

        /// <summary>
        /// Параметрический конструктор класса QueueItemMovedToPointArgs
        /// </summary>
        /// <param name="courier">Курьер</param>
        /// <param name="latitude">Широта целевой точки</param>
        /// <param name="longitude">Долгота целевой точки</param>
        public QueueItemMovedToPointArgs(CourierEx courier, double latitude, double longitude)
        {
            Courier = courier;
            Latitude = latitude;
            Longitude = longitude;
        }
    }
}
