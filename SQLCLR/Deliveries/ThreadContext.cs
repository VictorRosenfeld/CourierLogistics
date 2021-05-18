
namespace SQLCLR.Deliveries
{
    using SQLCLR.Couriers;
    using SQLCLR.Orders;
    using SQLCLR.Shops;
    using System;
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
        public ManualResetEvent SyncEvent { get; set; }

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
        /// Количество заказов
        /// </summary>
        public int OrderCount => (Orders == null ? 0 : Orders.Length);

        /// <summary>
        /// Курьер, c помощью которого выполняются отгрузки
        /// </summary>
        public Courier ShopCourier { get; private set; }

        /// <summary>
        /// Максимальная длина маршрута
        /// </summary>
        public int MaxRouteLength { get; set; }

        /// <summary>
        /// Время, на которое создаются отгрузки
        /// </summary>
        public DateTime CalcTime { get; set; }

        #endregion Аргументы расчета

        #region Результат работы построителя отгрузок

        /// <summary>
        /// Построенные отгрузки
        /// </summary>
        public CourierDeliveryInfo[] Deliveries { get; set; }

        /// <summary>
        /// Число построенных отгрузок
        /// </summary>
        public int DeliveryCount => (Deliveries == null ? 0 : Deliveries.Length);

        /// <summary>
        /// Код возврата
        /// </summary>
        public int ExitCode { get; set; }

        #endregion Результат работы построителя отгрузок

        /// <summary>
        /// Параметрический конструктор класса ThreadContext
        /// </summary>
        /// <param name="calcTime">Время расчета</param>
        /// <param name="maxRouteLength">Максимальная длина создаваемых маршрутов</param>
        /// <param name="shop">Магазин</param>
        /// <param name="orders">Заказы магазина</param>
        /// <param name="courier">Курьер</param>
        /// <param name="syncEvent">Объект синхронизации</param>
        public ThreadContext(DateTime calcTime, int maxRouteLength, Shop shop, Order[] orders, Courier courier, ManualResetEvent syncEvent)
        {
            CalcTime = calcTime;
            MaxRouteLength = maxRouteLength;
            ShopFrom = shop;
            Orders = orders;
            ShopCourier = courier;
            SyncEvent = syncEvent;
            ExitCode = -1;
        }
    }
}
