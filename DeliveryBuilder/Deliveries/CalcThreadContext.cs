
namespace DeliveryBuilder.Deliveries
{
    using DeliveryBuilder.Couriers;
    //using DeliveryBuilder.MaxOrdersOfRoute;
    using DeliveryBuilder.Orders;
    using DeliveryBuilder.SalesmanProblemLevels;
    using DeliveryBuilder.Shops;
    using System;
    using System.Threading;

    /// <summary>
    /// Контекст построителя отгрузок
    /// в отдельном потоке
    /// </summary>
    public class CalcThreadContext
    {
        public CalcConfig Config { get; set; }

        /// <summary>
        /// Объект синхронизации
        /// </summary>
        public ManualResetEvent SyncEvent { get; set; }

        #region Аргументы расчета

        /// <summary>
        /// Имя сервера
        /// </summary>
        public string ServerName { get; private set; }

        /// <summary>
        /// Имя БД
        /// </summary>
        public string DbName { get; private set; }

        /// <summary>
        /// ID LogisticsService
        /// </summary>
        public int ServiceId { get; private set; }

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
        /// Ограничения на длину маршрута от числа заказов
        /// (при построении полным перебором)
        /// </summary>
        public SalesmanLevels Limitations { get; set; }

        /// <summary>
        /// Время, на которое создаются отгрузки
        /// </summary>
        public DateTime CalcTime { get; set; }

        ///// <summary>
        ///// Geo-данные между парами точек:
        /////     GeoData[i,j].X  - distance
        /////     GeoData[i,j].Y  - distance
        ///// где
        /////     i, j меньше или равно OrderCount
        /////     i соответствует Order[i];
        /////     i = OrderCount соответствует ShopFrom
        ///// </summary>
        //public Point[,] GeoData { get; set; }

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
        /// Параметрический конструктор класса CalcThreadContext
        /// </summary>
        /// <param name="serverName">Имя сервера</param>
        /// <param name="dbName">Имя БД</param>
        /// <param name="serviceId">ID logisticsService</param>
        /// <param name="calcTime">Время расчета</param>
        /// <param name="maxRouteLength">Максимальная длина создаваемых маршрутов</param>
        /// <param name="shop">Магазин</param>
        /// <param name="orders">Заказы магазина</param>
        /// <param name="courier">Курьер</param>
        /// <param name="limitations">Длины маршрутов в зависимости от числа заказов при полном переборе</param>
        /// <param name="syncEvent">Объект синхронизации</param>
        public CalcThreadContext(string serverName, string dbName, int serviceId, DateTime calcTime, Shop shop, Order[] orders, Courier courier, SalesmanLevels limitations, ManualResetEvent syncEvent)
        {
            ServerName = serverName;
            DbName = dbName;
            ServiceId = serviceId;
            CalcTime = calcTime;
            ShopFrom = shop;
            Orders = orders;
            ShopCourier = courier;
            Limitations = limitations;
            SyncEvent = syncEvent;
            ExitCode = -1;
        }
    }
}
