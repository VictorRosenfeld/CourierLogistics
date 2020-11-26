
namespace CourierLogistics.Logistics.FloatSolution.CourierService.ServiceQueue
{
    using CourierLogistics.Logistics.FloatSolution.CourierInLive;
    using CourierLogistics.SourceData.Couriers;

    /// <summary>
    /// Аргументы элемента очереди событий типа OrderDelivered
    /// </summary>
    public class QueueItemOrderDeliveredArgs
    {
        /// <summary>
        /// Курьер, который осуществил доставку
        /// </summary>
        public CourierEx Courier { get; private set; }

        /// <summary>
        /// Доставленные заказы
        /// </summary>
        public CourierDeliveryInfo Delivery { get; private set; }

        /// <summary>
        /// Параметрический конструктор класса QueueItemDeliveryAlertArgs
        /// </summary>
        /// <param name="courier">Курьер</param>
        /// <param name="delivery">Отгрузка</param>
        public QueueItemOrderDeliveredArgs(CourierEx courier, CourierDeliveryInfo delivery)
        {
            Courier = courier;
            Delivery = delivery;
        }
    }
}
