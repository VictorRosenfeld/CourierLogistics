
namespace CourierLogistics.Logistics.FloatSolution.ShopInLive
{
    using CourierLogistics.SourceData.Orders;
    using System;

    /// <summary>
    /// Аргументы события OrderAssembled
    /// </summary>
    public class ShopOrderAssembledEventArgs
    {
        /// <summary>
        /// Время наступления события
        /// </summary>
        public DateTime EventTime { get; set; }

        /// <summary>
        /// Собранный заказ
        /// </summary>
        public Order ShopOrder { get; set; }

        /// <summary>
        /// Параметрический конструктор класса ShopOrderAssembledEventArgs
        /// </summary>
        /// <param name="eventTime">Время наступления события</param>
        /// <param name="delivery">Собранный заказ</param>
        public ShopOrderAssembledEventArgs(DateTime eventTime, Order order)
        {
            EventTime = eventTime;
            ShopOrder = order;
        }
    }
}
