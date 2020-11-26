
namespace LogisticsService.FixedCourierService.ServiceQueue
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Очередь проверочных событий
    /// при борьбы с утечками
    /// </summary>
    public class CheckingQueue
    {
        /// <summary>
        /// Текущие элементы для проверки утечки
        /// </summary>
        private Dictionary<int, QueueItem> checkingItems;

        /// <summary>
        /// Словарь всех элементов
        /// </summary>
        public Dictionary<int, QueueItem> CheckingItems => checkingItems;

        /// <summary>
        /// Общее число элементов
        /// </summary>
        public int Count => (checkingItems == null ? 0 : checkingItems.Count);

        /// <summary>
        /// Очередь событий
        /// </summary>
        private QueueItem[] queue;

        /// <summary>
        /// Очередь событий
        /// </summary>
        public QueueItem[] Queue => queue;

        /// <summary>
        /// Добавление нового элемента
        /// </summary>
        /// <param name="item">Добавляемый элемент</param>
        public void AddItem(QueueItem item)
        {
            checkingItems[item.Delivery.Orders[0].Id] = item;
            queue = null;
        }

        /// <summary>
        /// Удаление элемента из очереди
        /// </summary>
        /// <param name="item">Добавляемый элемент</param>
        public void DeleteItem(int orderId)
        {
            try
            { checkingItems.Remove(orderId); queue = null; }
            catch { }
        }

        /// <summary>
        /// Параметрический конструктор класса CheckingQueue
        /// </summary>
        /// <param name="capacity">Начальная ёмкость очереди</param>
        public CheckingQueue(int capacity)
        {
            if (capacity <= 0)
                capacity = 1000;
            checkingItems = new Dictionary<int, QueueItem>(capacity);
            queue = null;
        }

        /// <summary>
        /// Создание отсортированной очереди
        /// </summary>
        /// <returns></returns>
        private int Create()
        {
            // 1. Инициализация
            int rc = 1;
            queue = null;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (checkingItems == null || checkingItems.Count <= 0)
                    return rc;

                // 3. Извлекаем активные события
                rc = 3;
                QueueItem[] items = checkingItems.Values.Where(item => item.ItemType == QueueItemType.CheckingAlert).ToArray();
                if (items == null || items.Length <= 0)
                {
                    queue = new QueueItem[0];
                    return rc = 0;
                }

                // 4. Сортируем элементы по возратанию времени наступления события
                rc = 4;
                Array.Sort(items, CompareByEventTime);

                queue = items;

                // 5. Выход - Ok
                rc = 0;
                return rc;
            }
            catch
            {
                return rc;
            }
        }

        /// <summary>
        /// Получить текущую очередь
        /// </summary>
        /// <returns></returns>
        public QueueItem[] GetQueue()
        {
            if (queue == null)
                Create();
            return queue;
        }

        /// <summary>
        /// Получить время наиболее раннего события
        /// </summary>
        /// <returns></returns>
        public DateTime GetFirstEventTime()
        {
            if (queue == null)
                Create();
            if (queue != null && queue.Length > 0)
                return queue[0].EventTime;
            return DateTime.MaxValue;
        }

        /// <summary>
        /// Выбор всех активных событий, наступающих
        /// до заданного времени
        /// </summary>
        /// <param name="toTime"></param>
        /// <returns></returns>
        public QueueItem[] MoveToTime(DateTime toTime)
        {
            // 1. Инициализация

            try
            {
                // 2. Перестраиваем очередь, если требуется
                if (queue == null)
                    Create();

                if (queue == null || queue.Length <= 0)
                    return new QueueItem[0];

                // 3. Отбираем трубуемые события
                QueueItem[] items = new QueueItem[queue.Length];
                int count = 0;

                for (int i = 0; i < queue.Length; i++)
                {
                    QueueItem item = queue[i];
                    if (item.EventTime > toTime)
                        break;
                    items[count++] = item;
                }

                if (count < items.Length)
                {
                    Array.Resize(ref items, count);
                }

                // 4. Выход - Ok
                return items;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Сравнение двух элементов очереди по времени наступления события
        /// </summary>
        /// <param name="item1">Элемент 1</param>
        /// <param name="item2">Элемент 2</param>
        /// <returns>-1 - item1 меньше item2; 0 - item1 = item2; item1 больше item2</returns>
        private static int CompareByEventTime(QueueItem item1, QueueItem item2)
        {
            if (item1.EventTime < item2.EventTime)
                return -1;
            if (item1.EventTime > item2.EventTime)
                return 1;
            return 0;
        }

        /// <summary>
        /// Удаление всех не активных элементов из словаря
        /// </summary>
        public void DeleteNotActive()
        {
            try
            {
                // 2. Проверяем исходные данные
                if (checkingItems == null || checkingItems.Count <= 0)
                    return;

                // 3. Выбираем id удаляемых элементов
                int[] keys = new int[checkingItems.Count];
                int count = 0;
                
                foreach(KeyValuePair<int, QueueItem> kvp in checkingItems)
                {
                    if (kvp.Value.ItemType != QueueItemType.CheckingAlert)
                    {
                        keys[count++] = kvp.Key;
                    }
                }

                if (count <= 0)
                    return;

                queue = null;

                // 4. Удаляем элементы с отобранными ключами
                for (int i = 0; i < count; i++)
                {
                    checkingItems.Remove(keys[i]);
                }

                // 5. Выход - Ok
            }
            catch
            {  }
        }
    }
}
