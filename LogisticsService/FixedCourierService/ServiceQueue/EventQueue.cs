
namespace LogisticsService.FixedCourierService.ServiceQueue
{
    using System;

    /// <summary>
    /// Очередь событий подлежащих обработке
    /// </summary>
    public class EventQueue
    {
        /// <summary>
        /// Элементы очереди событий
        /// </summary>
        public QueueItem[] Items { get; private set; }

        /// <summary>
        /// Количество элементов очереди
        /// </summary>
        public int Count => Items == null ? 0 : Items.Length;

        /// <summary>
        /// Индекс элемента подлежащего обработке
        /// </summary>
        public int ItemIndex { get; private set; }

        /// <summary>
        /// Флаг: true - очередь создана; false - очередь не создана
        /// </summary>
        public bool IsCreated { get; private set; }

        public int ActiveCount => (Items == null ? 0 : Items.Length - ItemIndex);

        /// <summary>
        /// Создание очереди
        /// </summary>
        /// <param name="items">Элементы очереди</param>
        /// <returns>0 - очередь создана; очередь не создана</returns>
        public int Create(QueueItem[] items)
        {
            // 1. Инициализация
            int rc = 1;
            IsCreated = false;
            Items = null;
            ItemIndex = 0;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (items == null || items.Length <= 0)
                    return rc;

                // 3. Сортируем события по времени их наступления
                rc = 3;
                QueueItem[] allItems = (QueueItem[])items.Clone();
                Array.Sort(allItems, CompareByEventTime);

                Items = allItems;

                // 4. Выход - Ok
                rc = 0;
                IsCreated = true;
                return rc;
            }
            catch
            {
                return rc;
            }
        }

        /// <summary>
        /// Получение текущего элемента очереди
        /// </summary>
        /// <returns>Текущий элемент очереди или null</returns>
        public QueueItem GetCurrentItem()
        {
            try
            {
                if (!IsCreated)
                    return null;
                return Items[ItemIndex];
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Переход к следующему элементу очереди
        /// </summary>
        /// <returns>Следующий элемент или null</returns>
        public QueueItem MoveNext()
        {
            try
            {
                // 2. Проверяем исходные данные
                if (!IsCreated)
                    return null;

                // 3. Переходим к следующему элементу
                if (ItemIndex >= Items.Length - 1)
                    return null;
                return Items[++ItemIndex];
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Выборка всех событий от текущего до заданного времени
        /// </summary>
        /// <param name="toTime">Время 'до'</param>
        /// <returns>Выбранные события</returns>
        public QueueItem[] MoveToTime(DateTime toTime)
        {
            try
            {
                // 2. Проверяем исходные данные
                if (!IsCreated)
                    return null;

                // 3. Выбираем все элементы очереди до заданного времени и продвигаем указатель
                int size = Items.Length - ItemIndex;
                if (size > 300) size = 300;
                QueueItem[] selectedItems = new QueueItem[size];
                int count = 0;

                for (int i = ItemIndex; i < Items.Length; i++)
                {
                    if (Items[i].EventTime > toTime)
                        break;
                    if (count >= selectedItems.Length)
                    {
                        Array.Resize(ref selectedItems, selectedItems.Length + 300);
                    }
                    selectedItems[count++] = Items[i];
                }

                if (count <= 0)
                    return new QueueItem[0];

                if (count < selectedItems.Length)
                {
                    Array.Resize(ref selectedItems, count);
                }

                ItemIndex += count;

                // 4. Выход - Ok
                return selectedItems;
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
        private static int  CompareByEventTime(QueueItem item1, QueueItem item2)
        {
            if (item1.EventTime < item2.EventTime)
                return -1;
            if (item1.EventTime > item2.EventTime)
                return 1;
            return 0;
        }

        /// <summary>
        /// Поиск активного элемента очереди,
        /// содержащего отгрузкус заданным заказом
        /// </summary>
        /// <param name="orderId">Id заказа</param>
        /// <param name="toTime">Ограничение просмотра элементов по времени</param>
        /// <returns></returns>
        public QueueItem FindItemByOrderId(int orderId, DateTime toTime)
        {
            // 1. Инициализация
            
            try
            {
                // 2. Проверяем исходные данные
                if (!IsCreated)
                    return null;

                // 3. Ищем отгрузку с заданным заказом
                for (int i = ItemIndex; i < Items.Length; i++)
                {
                    // 3.1 Извлекаем очередной элемент
                    QueueItem item = Items[i];
                    if (item.EventTime > toTime)
                        break;
                    if (item.ItemType == QueueItemType.None)
                        continue;

                    // 3.2 Проверяем наличие заказа в отгрузке
                    if (item.Delivery != null)
                    {
                        if (item.Delivery.ContainsOrder(orderId))
                            return item;
                    }
                }

                // 4. Отгрузка не найдена
                return null;
            }
            catch
            {
                return null;
            }

        }
    }
}
