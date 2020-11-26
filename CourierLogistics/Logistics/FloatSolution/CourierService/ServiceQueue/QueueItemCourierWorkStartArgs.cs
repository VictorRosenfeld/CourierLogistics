
namespace CourierLogistics.Logistics.FloatSolution.CourierService.ServiceQueue
{
    using CourierLogistics.Logistics.FloatSolution.CourierInLive;

    /// <summary>
    /// Аргументы элемента очереди событий типа CourierWorkStart
    /// </summary>
    public class QueueItemCourierWorkStartArgs
    {
        /// <summary>
        /// Курьер, который осуществил доставку
        /// </summary>
        public CourierEx Courier { get; private set; }

        /// <summary>
        /// Широта точки начала работы
        /// </summary>
        public double Latitude { get; private set; }

        /// <summary>
        /// Долгота точки начала работы
        /// </summary>
        public double Longitude { get; private set; }

        /// <summary>
        /// Параметрический конструктор класса QueueItemCourierWorkStartArgs
        /// </summary>
        /// <param name="courier">Курьер</param>
        /// <param name="latitude">Широта точки начала работы</param>
        /// <param name="longitude">Долгота точки начала работы</param>
        public QueueItemCourierWorkStartArgs(CourierEx courier, double latitude, double longitude)
        {
            Courier = courier;
            Latitude = latitude;
            Longitude = longitude;
        }

    }
}
