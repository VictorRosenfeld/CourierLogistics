
namespace CourierLogistics.Logistics.FloatSolution.CourierService.ServiceQueue
{
    using System;

    /// <summary>
    /// Очередь событий
    /// </summary>
    public class EventQueue
    {
        /// <summary>
        /// Индекс секунды относительно начала дня
        /// (daySecond[i] ---> items[daySecond[i]])
        /// </summary>
        private int[] daySecond;

        /// <summary>
        /// Элементы очереди событий
        /// </summary>
        private QueueItem[] items;

        /// <summary>
        /// Счётчик элементов
        /// </summary>
        private int itemCount;

        /// <summary>
        /// Текущий элемент очереди
        /// </summary>
        private int currentItemIndex;

        /// <summary>
        /// Количество элементов в очереди
        /// </summary>
        public int ItemCount => itemCount;

        /// <summary>
        /// Ёмкость очереди
        /// </summary>
        public int Capcity => items.Length;

        /// <summary>
        /// Параметрический конструктор класса EventQueue 
        /// </summary>
        /// <param name="capcity">Ёмкость очереди</param>
        public EventQueue(int capcity = 1200000)
        {
            daySecond = new int[24 * 60 * 60];
            items = new QueueItem[capcity];
            itemCount = 1;
            currentItemIndex = -1;
        }

        /// <summary>
        /// Добавление события в упорядоченную
        /// по времени очередь событий
        /// </summary>
        /// <param name="eventTime">Время наступления события</param>
        /// <param name="itemType">Тип элемента</param>
        /// <param name="args">Аргументы события</param>
        /// <returns></returns>
        public int AddEvent(DateTime eventTime, QueueItemType itemType, object args)
        {
            // 1. Инициализация
            int rc = -1;

            try
            {
                // 3. Извлекаем секунду
                rc = -3;
                int secondIndex = (int)eventTime.TimeOfDay.TotalSeconds;

                // 4. В данную секунду ещё не было событий или это первое событие для данной секунды
                rc = -4;
                int firstItemIndex = daySecond[secondIndex];
                if (firstItemIndex == 0)
                {
                    items[itemCount] = new QueueItem(-secondIndex, eventTime, itemType, args);
                    daySecond[secondIndex] = itemCount;
                    return itemCount++;
                }
                else if (items[firstItemIndex].EventTime > eventTime)
                {
                    items[itemCount] = new QueueItem(firstItemIndex, eventTime, itemType, args);
                    daySecond[secondIndex] = itemCount;
                    return itemCount++;
                }

                // 5. Добавление нового элемента в список
                rc = -5;
                int currentItemIndex = firstItemIndex;

                while (true)
                {
                    int nextItemIndex = items[currentItemIndex].NextItem;
                    if (nextItemIndex <= 0 ||
                        items[nextItemIndex].EventTime > eventTime)
                    {
                        items[itemCount] = new QueueItem(nextItemIndex, eventTime, itemType, args);
                        items[currentItemIndex].NextItem = itemCount;
                        return itemCount++;
                    }

                    currentItemIndex = nextItemIndex;
                }
            }
            catch
            {
                return rc;
            }
        }

        /// <summary>
        /// Выбрать следующее событие
        /// </summary>
        /// <returns></returns>
        public QueueItem GetNext()
        {
            //try
            //{
                // 1. Проход по списку секунды до первого активного события или конца списка
                while (currentItemIndex > 0)
                {
                    currentItemIndex = items[currentItemIndex].NextItem;
                    if (currentItemIndex > 0 && items[currentItemIndex].Status == QueueItemStatus.Active)
                    {
                        //if (currentItemIndex == 77)
                        //{
                        //    currentItemIndex = currentItemIndex;
                        //}
                        return items[currentItemIndex];
                    }
                }

                // 2. Продвижение до секунды имеющей не пустой список событий
                int startSecond = -currentItemIndex + 1;

                for (int i = startSecond; i < daySecond.Length; i++)
                {
                    if (daySecond[i] > 0)
                    {
                        currentItemIndex = daySecond[i];
                        //if (currentItemIndex == 77)
                        //{
                        //    currentItemIndex = currentItemIndex;
                        //}

                        return items[currentItemIndex];
                    }
                }

                // 3. Все секунды просмотрены, событий больше нет
                return null;
            //}
            //finally
            //{
            //    if (currentItemIndex == 239)
            //    {
            //        currentItemIndex = currentItemIndex;
            //    }
            //}
        }
        

        /// <summary>
        /// Очистка очереди событий
        /// </summary>
        public void Clear()
        {
            itemCount = 1;
            currentItemIndex = -1;
        }

        /// <summary>
        /// Выборка всех событий очереди,
        /// упорядоченных во времени
        /// </summary>
        /// <returns></returns>
        public QueueItem[]  SelectQueueItemsOrderByEventTime()
        {
            // 1. Инициализация
            
            try
            {
                // 2. Проверяем исходные данные
                if (itemCount <= 1)
                    return new QueueItem[0];

                // 3. Цикл выбора событий, упорядоченных во времени
                QueueItem[] result = new QueueItem[itemCount];
                int count = 0;

                for (int i = 0; i < daySecond.Length; i++)
                {
                    int itemIndex = daySecond[i];

                    while (itemIndex > 0)
                    {
                        QueueItem item = items[itemIndex];
                        if (item.Status != QueueItemStatus.Disabled)
                            result[count++] = item;
                        itemIndex = item.NextItem;
                    }
                }

                if (count < result.Length)
                {
                    Array.Resize(ref result, count);
                }

                // 4. Выход - Ok
                return result;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Выборка всех выполненных событий отгрузки
        /// </summary>
        /// <returns>Выбранные события</returns>
        public QueueItem[] SelectQueueItemOfDeliveredOrders()
        {
            // 1. Инициализация

            try
            {
                // 2. Проверяем исходные данные
                if (itemCount <= 1)
                    return new QueueItem[0];

                // 3. Цикл выбора событий, упорядоченных во времени
                QueueItem[] result = new QueueItem[itemCount];
                int count = 0;

                for (int i = 1; i < itemCount; i++)
                {
                    QueueItem item = items[i];
                    if (item.ItemType == QueueItemType.OrderDelivered &&
                        item.Status == QueueItemStatus.Executed)
                        result[count++] = item;
                }

                if (count < result.Length)
                {
                    Array.Resize(ref result, count);
                }

                // 4. Выход - Ok
                return result;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Установка указателя очереди
        /// </summary>
        /// <param name="startTime">Время, с которого следует начать просмотр событий</param>
        /// <returns>0 - указатель установлен; иначе - указатель не установлен</returns>
        public int SetQueueCurrentItem(TimeSpan startTime)
        {
            // 1. Инициализация
            int rc = 1;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                int second = (int)startTime.TotalSeconds;
                if (second < 0 || second > daySecond.Length)
                    return rc;

                int secondItem = daySecond[second];

                // 3. Создаём фиктивный элемент
                rc = 3;
                if (secondItem > 0)
                {
                    daySecond[second] = itemCount;
                    items[itemCount] = new QueueItem(secondItem, DateTime.Now, QueueItemType.None, null);
                }
                else
                {
                    items[itemCount] = new QueueItem(-second, DateTime.Now, QueueItemType.None, null);
                }

                // 4. Устанвливаем указатель очереди
                rc = 4;
                currentItemIndex = itemCount++;

                // 4. Выход - Ok
                rc = 0;
                return rc;
            }
            catch
            {
                return rc;
            }
        }

        /// <summary>
        /// Отключение заданных активных событий
        /// </summary>
        /// <param name="itemIndex">Индексы элементов очереди событий</param>
        public void DisableItems(int[] itemIndex)
        {
            // 1. Инициализация

            try
            {
                // 2. Проверяем исходные данные
                if (itemIndex == null || itemIndex.Length <= 0)
                    return;

                // 3. Цикл обработки элементов
                for (int i = 0; i < itemIndex.Length; i++)
                {
                    int index = itemIndex[i];
                    if (index >= 1 && index < itemCount && items[index].Status == QueueItemStatus.Active)
                        items[index].Status = QueueItemStatus.Disabled;
                }
            }
            catch
            {  }
        }
    }
}
