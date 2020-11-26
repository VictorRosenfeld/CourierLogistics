
namespace CourierLogistics.Logistics.FloatSolution.CourierService.ServiceQueue
{
    /// <summary>
    /// Состояние элемента очереди событий
    /// </summary>
    public enum QueueItemStatus
    {
        /// <summary>
        /// Активное событие
        /// </summary>
        Active = 0,

        /// <summary>
        /// Событие было выполнено
        /// </summary>
        Executed = 1,

        /// <summary>
        /// Событие было отменено
        /// </summary>
        Disabled = 2,
    }
}
