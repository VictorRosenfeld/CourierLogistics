﻿
namespace DeliveryBuilder.Recalc
{
    using DeliveryBuilder.Couriers;
    using DeliveryBuilder.Geo;
    using DeliveryBuilder.Orders;
    using DeliveryBuilder.Shops;
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
        /// Максимальная длина маршрута для полного перебора
        /// </summary>
        public int MaxRouteLength { get; set; }

        /// <summary>
        /// Время, на которое создаются отгрузки
        /// </summary>
        public DateTime CalcTime { get; set; }

        /// <summary>
        /// Geo-данные между парами точек:
        ///     GeoData[i,j].X  - distance
        ///     GeoData[i,j].Y  - distance
        /// где
        ///     i, j меньше или равно OrderCount
        ///     i соответствует Order[i];
        ///     i = OrderCount соответствует ShopFrom
        /// </summary>
        public Point[,] GeoData { get; set; }

        #endregion Аргументы расчета

        #region Результат работы построителя отгрузок

        /// <summary>
        /// Построенные отгрузки
        /// </summary>
        public CourierDeliveryInfo[] Deliveries { get; set; }

        /// <summary>
        /// Построенные отгрузки
        /// (формат элемента: [f0][f1][f2][f3][n][b1]...[bk])
        /// </summary>
        public byte[] Items { get; set; }

        /// <summary>
        /// Количество построенных отгрузок
        /// (формат элемента: [f0][f1][f2][f3][n][b1]...[bk])
        /// </summary>
        public int ItemCount { get; set; }

        /// <summary>
        /// Число построенных отгрузок
        /// </summary>
        public int DeliveryCount => (Deliveries == null ? 0 : Deliveries.Length);

        /// <summary>
        /// Код возврата
        /// </summary>
        public int ExitCode { get; set; }

        /// <summary>
        /// Флаг завершения работы потока: false - поток выполняется; true - поток завершен
        /// </summary>
        public bool Completed { get; set; }

        #endregion Результат работы построителя отгрузок

        /// <summary>
        /// Параметрический конструктор класса ThreadContext
        /// </summary>
        /// <param name="serviceId">ID logisticsService</param>
        /// <param name="calcTime">Время расчета</param>
        /// <param name="limitations">Длины маршрутов в зависимости от числа заказов при полном переборе</param>
        /// <param name="maxRouteLength">Максимальная длина создаваемых маршрутов</param>
        /// <param name="shop">Магазин</param>
        /// <param name="orders">Заказы магазина</param>
        /// <param name="courier">Курьер</param>
        /// <param name="geoData">Гео-данные</param>
        /// <param name="syncEvent">Объект синхронизации</param>
        public ThreadContext(int serviceId, DateTime calcTime, int maxRouteLength, Shop shop, Order[] orders, Courier courier, Point[,] geoData, ManualResetEvent syncEvent)
        {
            ServiceId = serviceId;
            CalcTime = calcTime;
            MaxRouteLength = maxRouteLength;
            ShopFrom = shop;
            Orders = orders;
            ShopCourier = courier;
            GeoData = geoData;
            SyncEvent = syncEvent;
            ExitCode = -1;
            Completed = false;
        }
    }
}
