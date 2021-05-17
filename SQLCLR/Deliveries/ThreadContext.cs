
namespace SQLCLR.Deliveries
{
    using SQLCLR.Couriers;
    using SQLCLR.Orders;
    using SQLCLR.Shops;
    using System.Threading;

    /// <summary>
    /// Контекст построителя отгрузок
    /// в отдельном потоке
    /// </summary>
    public class ThreadContext
    {
        /// <summary>
        /// Объект синхронизации
        /// </summary>
        public ManualResetEvent SyncEvent { get; private set; }

        #region Аргументы расчета

        /// <summary>
        /// Магазин
        /// </summary>
        public Shop ShopFrom { get; private set; }

        /// <summary>
        /// Заказы, для которых создаются отгрузки
        /// </summary>
        public Order[] Orders { get; private set; }

        /// <summary>
        /// Курьер, c помощью которого выполняются отгрузки
        /// </summary>
        public Courier ShopCourier { get; private set; }

        #endregion Аргументы расчета

        #region Результат работы построителя отгрузок

        /// <summary>
        /// Построенные отгрузки
        /// </summary>
        public CourierDeliveryInfo[] Deliveries { get; set; }

        /// <summary>
        /// Код возврата
        /// </summary>
        public int ExitCode { get; set; }

        #endregion Результат работы построителя отгрузок

        /// <summary>
        /// Параметрический конструктор класса ThreadContext
        /// </summary>
        /// <param name="shop">Магазин</param>
        /// <param name="orders">Заказы магазина</param>
        /// <param name="courier">Курьер</param>
        /// <param name="syncEvent">Объект синхронизации</param>
        public ThreadContext(Shop shop, Order[] orders, Courier courier, ManualResetEvent syncEvent)
        {
            ShopFrom = shop;
            Orders = orders;
            ShopCourier = courier;
            ExitCode = -1;
        }
    }
}
