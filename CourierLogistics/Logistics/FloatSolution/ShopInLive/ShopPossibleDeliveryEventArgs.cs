
namespace CourierLogistics.Logistics.FloatSolution.ShopInLive
{
    using CourierLogistics.SourceData.Couriers;
    using System;

    /// <summary>
    /// Аргументы события ShopPossibleDelivery в магазине
    /// </summary>
    public class ShopPossibleDeliveryEventArgs
    {
        /// <summary>
        /// Время наступления события
        /// </summary>
        public DateTime EventTime { get; private set; }

        /// <summary>
        /// Отгрузка или собранный заказ
        /// </summary>
        public CourierDeliveryInfo[] Delivery { get; private set; }

        /// <summary>
        /// Параметрический конструктор класса ShopEventArgs
        /// </summary>
        /// <param name="eventTime">Время наступления события</param>
        /// <param name="delivery">Возможные отгрузки</param>
        public ShopPossibleDeliveryEventArgs(DateTime eventTime, CourierDeliveryInfo[] delivery)
        {
            EventTime = eventTime;
            Delivery = delivery;
        }
    }
}
