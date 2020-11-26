

namespace CourierLogistics.Logistics.FloatSolution.CourierService.ServiceQueue
{
    using CourierLogistics.Logistics.FloatSolution.ShopInLive;
    using CourierLogistics.SourceData.Orders;

    /// <summary>
    /// Аргументы элемента очереди событий типа OrderAssembled
    /// </summary>
    public class QueueItemOrderAssembledArgs
    {
        /// <summary>
        /// Магазин, в котором собран заказ
        /// </summary>
        public ShopEx Shop { get; private set; }

        /// <summary>
        /// Собранный заказ
        /// </summary>
        public Order AssembledOrder { get; private set; }

        /// <summary>
        /// Параметрический конструктор класса QueueItemOrderAssembledArgs
        /// </summary>
        /// <param name="shop">Магазин</param>
        /// <param name="order">Заказ</param>
        public QueueItemOrderAssembledArgs(ShopEx shop, Order order)
        {
            Shop = shop;
            AssembledOrder = order;
        }
    }
}
