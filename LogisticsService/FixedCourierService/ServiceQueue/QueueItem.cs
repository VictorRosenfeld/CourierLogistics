
namespace LogisticsService.FixedCourierService.ServiceQueue
{
    using LogisticsService.Couriers;
    using System;

    /// <summary>
    /// Элемент очереди событий
    /// </summary>
    public class QueueItem
    {
        /// <summary>
        /// Время наступления события
        /// </summary>
        public DateTime EventTime { get; private set; }

        /// <summary>
        /// Тип элемента (события)
        /// </summary>
        public QueueItemType ItemType { get; set; }

        /// <summary>
        /// Отгрузка
        /// </summary>
        public CourierDeliveryInfo Delivery { get; private set; }

        /// <summary>
        /// Параметрический конструктор класса QueueItem
        /// </summary>
        /// <param name="eventTime">Время наступления события</param>
        /// <param name="itemType">Тип элемента очереди</param>
        /// <param name="delivery">Отгрузка</param>
        public QueueItem(DateTime eventTime, QueueItemType itemType, CourierDeliveryInfo delivery)
        {
            EventTime = eventTime;
            ItemType = itemType;
            Delivery = delivery;
        }
    }
}
