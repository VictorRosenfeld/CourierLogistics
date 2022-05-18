
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
        /// Активное событие
        /// </summary>
        Active = 1,

        /// <summary>
        /// Отработанное событие,
        /// которое следует удалить из очереди
        /// </summary>
        Completed = 2,
    }
}
