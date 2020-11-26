
namespace CourierLogistics.Logistics.FloatSolution.ShopInLive
{
    /// <summary>
    /// Событие в магазине
    /// </summary>
    public enum ShopEventType
    {
        /// <summary>
        /// Не определено
        /// </summary>
        None = 0,

        /// <summary>
        /// Собран заказ
        /// </summary>
        OrderAssembled = 1,

        /// <summary>
        /// Пересчет возможных отгрузок
        /// </summary>
        Recalc = 2,

        /// <summary>
        /// Наступает время отгрузки
        /// </summary>
        DeliveryAlert = 3,
    }
}
