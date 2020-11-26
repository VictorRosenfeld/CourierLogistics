
namespace CourierLogistics.Logistics.FloatSolution.CourierService.ServiceQueue
{
    using CourierLogistics.Logistics.FloatSolution.ShopInLive;

    /// <summary>
    /// Аргументы элемента очереди событий типа ShopAllDelivered
    /// </summary>
    public class QueueItemShopAllDeliveredArgs
    {
        /// <summary>
        /// Магазин
        /// </summary>
        public ShopEx Shop { get; private set; }

        /// <summary>
        /// Параметрический конструктор класса QueueItemShopAllDeliveredArgs
        /// </summary>
        /// <param name="shop">Курьер</param>
        public QueueItemShopAllDeliveredArgs(ShopEx shop)
        {
            Shop = shop;
        }
    }
}
