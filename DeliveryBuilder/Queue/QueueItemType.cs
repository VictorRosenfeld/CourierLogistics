
namespace DeliveryBuilder.Queue
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
        /// Истечение времени ожидания отгрузки курьером или такси
        /// </summary>
        CourierDeliveryAlert = 1,

        /// <summary>
        /// Истечение предельного времени отгрузки с помощью такси
        /// </summary>
        TaxiDeliveryAlert = 2,

        /// <summary>
        /// Элемент проверочной очереди
        /// </summary>
        CheckingAlert = 3,
    }
}
