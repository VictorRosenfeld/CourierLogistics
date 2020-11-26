
namespace CourierLogistics.Logistics.FloatSolution.CourierService.ServiceQueue
{
    using CourierLogistics.Logistics.FloatSolution.ShopInLive;
    using CourierLogistics.SourceData.Couriers;

    /// <summary>
    /// Аргументы элемента очереди событий типа DeliveryAlert
    /// </summary>
    public class QueueItemDeliveryAlertArgs
    {
        /// <summary>
        /// Магазин, из которого осуществляется отгрузка
        /// </summary>
        public ShopEx Shop { get; private set; }

        /// <summary>
        /// Возможная отгрузка
        /// </summary>
        public CourierDeliveryInfo[] Delivery { get; private set; }

        /// <summary>
        /// Параметрический конструктор класса QueueItemDeliveryAlertArgs
        /// </summary>
        /// <param name="shop"> Магазин, из которого осуществляется отгрузка</param>
        /// <param name="delivery">Отгрузка</param>
        public QueueItemDeliveryAlertArgs(ShopEx shop, CourierDeliveryInfo delivery)
        {
            Shop = shop;
            Delivery = new CourierDeliveryInfo[] { delivery };
        }

        /// <summary>
        /// Параметрический конструктор класса QueueItemDeliveryAlertArgs
        /// </summary>
        /// <param name="shop"> Магазин, из которого осуществляется отгрузка</param>
        /// <param name="delivery">Отгрузки</param>
        public QueueItemDeliveryAlertArgs(ShopEx shop, CourierDeliveryInfo[] delivery)
        {
            Shop = shop;
            Delivery = delivery;
        }
    }
}
