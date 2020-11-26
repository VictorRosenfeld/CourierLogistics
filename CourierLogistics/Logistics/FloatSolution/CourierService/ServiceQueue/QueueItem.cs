
namespace CourierLogistics.Logistics.FloatSolution.CourierService.ServiceQueue
{
    using System;

    /// <summary>
    /// Элемент очереди событий
    /// </summary>
    public class QueueItem
    {
        /// <summary>
        /// Индекс следующего элемента.
        /// (Если индекс не положительный,
        /// то это ссылка на секунду)
        /// </summary>
        public int NextItem { get; set; }

        /// <summary>
        /// Время наступления события
        /// </summary>
        public DateTime EventTime { get; private set; }

        /// <summary>
        /// Состояние элемента очереди
        /// </summary>
        public QueueItemStatus Status { get; set; }

        /// <summary>
        /// Тип элемента (события)
        /// </summary>
        public QueueItemType ItemType { get; private set; }

        /// <summary>
        /// Аргумент события
        /// </summary>
        public object Args { get; private set; }

        /// <summary>
        /// Параметрический конструктор класса QueueItem
        /// </summary>
        /// <param name="nextItem">Индекс следующего элемента. (Если индекс не положительный, то это ссылка на секунду)</param>
        /// <param name="eventTime">Время наступления события</param>
        /// <param name="itemType">Тип элемента (события)</param>
        /// <param name="args">Аргумент события</param>
        public QueueItem(int nextItem, DateTime eventTime, QueueItemType itemType, object args)
        {
            NextItem = nextItem;
            EventTime = eventTime;
            ItemType = itemType;
            Args = args;
            Status = QueueItemStatus.Active;
        }
    }
}
