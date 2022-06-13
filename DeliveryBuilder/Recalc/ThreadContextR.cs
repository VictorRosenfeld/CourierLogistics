
namespace DeliveryBuilder.Recalc
{
    using DeliveryBuilder.Couriers;
    using DeliveryBuilder.Geo;
    using DeliveryBuilder.Orders;
    using DeliveryBuilder.SalesmanProblemLevels;
    using DeliveryBuilder.Shops;
    using System;
    using System.Threading;

    /// <summary>
    /// Контекст построителя отгрузок
    /// в отдельном потоке
    /// </summary>
    public class ThreadContextR : ThreadContext
    {
        /// <summary>
        /// Начальное значение индексов заказов
        /// </summary>
        public int[] InitOrderIndexes { get; private set; }

        /// <summary>
        /// Количество наборов индексов (подмножеств заказов)
        /// </summary>
        public int SubsetCount { get; private set; }

        /// <summary>
        /// Параметрический конструктор класса ThreadContextR
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
        /// <param name="initOrderIndexes">Начальное значение индексов заказов</param>
        /// <param name="subsetCount">Количество наборов индексов (подмножеств заказов)</param>
        public ThreadContextR( int serviceId,
                               DateTime calcTime,
                               SalesmanLevels limitations,
                               int maxRouteLength,
                               Shop shop, Order[] orders,
                               Courier courier,
                               Point[,] geoData,
                               ManualResetEvent syncEvent,
                               int[] initOrderIndexes,
                               int subsetCount) : base(serviceId, calcTime, maxRouteLength, shop, orders, courier, geoData, syncEvent)
        {
            InitOrderIndexes = initOrderIndexes;
            SubsetCount = subsetCount;
        }

        /// <summary>
        /// Параметрический конструктор класса ThreadContextR
        /// </summary>
        /// <param name="context">Контекст построителя отгрузок</param>
        /// <param name="syncEvent">Объект синхронизации</param>
        /// <param name="initOrderIndexes">Начальное значение индексов заказов</param>
        /// <param name="subsetCount">Количество наборов индексов (подмножеств заказов)</param>
        public ThreadContextR(ThreadContext context,
                               ManualResetEvent syncEvent,
                               int[] initOrderIndexes,
                               int subsetCount) : base(context.ServiceId, context.CalcTime, context.MaxRouteLength, context.ShopFrom, context.Orders, context.ShopCourier, context.GeoData, syncEvent)
        {
            InitOrderIndexes = initOrderIndexes;
            SubsetCount = subsetCount;
        }
    }
}
