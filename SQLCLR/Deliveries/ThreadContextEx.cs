
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
    public class ThreadContextEx : ThreadContext
    {
        /// <summary>
        /// Отсортированные ключи всех возможных отгрузок
        /// </summary>
        public long[] DeliveryKeys { get; private set; }

        /// <summary>
        /// Индекс первого обрабатываемого заказа
        /// </summary>
        public int StartOrderIndex  { get; private set; }

        /// <summary>
        /// Шаг изменения индекса обрабатываемых заказов
        /// </summary>
        public int OrderIndexStep  { get; private set; }

        /// <summary>
        /// Параметрический конструктор класса ThreadContextEx
        /// </summary>
        /// <param name="serviceId">ID logisticsService</param>
        /// <param name="calcTime">Время расчета</param>
        /// <param name="maxRouteLength">Максимальная длина создаваемых маршрутов</param>
        /// <param name="shop">Магазин</param>
        /// <param name="orders">Заказы магазина</param>
        /// <param name="courier">Курьер</param>
        /// <param name="geoData">Гео-данные</param>
        /// <param name="syncEvent">Объект синхронизации</param>
        /// <param name="deliveryKeys">Отсортированные ключи всех возможных отгрузок</param>
        /// <param name="startOrderIndex">Индекс первого обрабатываемого заказа в массиве orders</param>
        /// <param name="orderIndexStep">Шаг изменения индекса обрабатываемых заказов в массиве orders</param>
        public ThreadContextEx(int serviceId, 
                               DateTime calcTime, 
                               int maxRouteLength, 
                               Shop shop, Order[] orders, 
                               Courier courier, 
                               Point[,] geoData, 
                               ManualResetEvent syncEvent,
                               long[] deliveryKeys,
                               int startOrderIndex,
                               int orderIndexStep) : base(serviceId, calcTime, maxRouteLength, shop, orders, courier, geoData, syncEvent)
        {
            DeliveryKeys = deliveryKeys;
            StartOrderIndex = startOrderIndex;
            OrderIndexStep = orderIndexStep;
        }

        /// <summary>
        /// Параметрический конструктор класса ThreadContextEx
        /// </summary>
        /// <param name="context">Контекст построителя отгрузок</param>
        /// <param name="syncEvent">Объект синхронизации</param>
        /// <param name="deliveryKeys">Отсортированные ключи всех возможных отгрузок</param>
        /// <param name="startOrderIndex">Индекс первого обрабатываемого заказа в массиве orders</param>
        /// <param name="orderIndexStep">Шаг изменения индекса обрабатываемых заказов в массиве orders</param>
        public ThreadContextEx(ThreadContext context,
                               ManualResetEvent syncEvent,
                               long[] deliveryKeys,
                               int startOrderIndex,
                               int orderIndexStep) : base(context.ServiceId, context.CalcTime, context.MaxRouteLength, context.ShopFrom, context.Orders, context.ShopCourier, context.GeoData, syncEvent)
        {
            DeliveryKeys = deliveryKeys;
            StartOrderIndex = startOrderIndex;
            OrderIndexStep = orderIndexStep;
        }
    }
}
