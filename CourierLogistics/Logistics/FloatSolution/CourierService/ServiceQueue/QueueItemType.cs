
namespace CourierLogistics.Logistics.FloatSolution.CourierService.ServiceQueue
{
    /// <summary>
    /// Тип элемента очереди
    /// </summary>
    public enum QueueItemType
    {
        /// <summary>
        /// Не определен
        /// </summary>
        None = 0,

        /// <summary>
        /// Начало работы курьера
        /// </summary>
        CourierWorkStart = 1,

        /// <summary>
        /// Конец работы курьера
        /// </summary>
        CourierWorkEnd = 2,

        /// <summary>
        /// Перемещение курьера в новую точку
        /// </summary>
        MovedToPoint = 3,

        /// <summary>
        /// Завершение доставки курьером
        /// </summary>
        OrderDelivered = 4,

        /// <summary>
        /// Завершение сборки заказа
        /// </summary>
        OrderAssembled = 5,

        /// <summary>
        /// Истечение срока отгрузки курьером
        /// </summary>
        CourierDeliveryAlert = 6,

        /// <summary>
        /// Истечение срока отгрузки такси
        /// </summary>
        TaxiDeliveryAlert = 7,

        /// <summary>
        /// Все заказы магазина отгружены
        /// </summary>
        ShopAllDelivered = 8,
    }
}
