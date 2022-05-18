

namespace DeliveryBuilder.Queue
{
    using System;

    /// <summary>
    /// Очередь отгрузок
    /// </summary>
    public class DeliveryQueue
    {
        /// <summary>
        /// Элементы очереди событий
        /// </summary>
        private QueueItem[] items;

        /// <summary>
        /// Число элементов в очереди
        /// </summary>
        private int itemCount;

        /// <summary>
        /// Элементы очереди событий
        /// </summary>
        public QueueItem[] Items
        {
            get
            {
                if (itemCount <= 0)
                { return new QueueItem[0]; }
                else
                {
                    QueueItem[] result = new QueueItem[itemCount];
                    Array.Copy(items, result, itemCount);
                    return result;
                }
            }
        }

        /// <summary>
        /// Число элементов в очереди
        /// </summary>
        public int Count => itemCount;

        /// <summary>
        /// Конструктор класса DeliveryQueue
        /// </summary>
        public DeliveryQueue()
        {
            items = new QueueItem[300];
            itemCount = 0;
        }

        /// <summary>
        /// Получить элементы, для которых наступило время старта
        /// </summary>
        /// <param name="toDate"></param>
        /// <returns></returns>
        public QueueItem[]  GetToStart(DateTime toDate)
        {
            // 1. Инициализация
            
            try
            {
                // 2. Проверяем исходные данные
                if (itemCount <= 0)
                    return null;

                // 3. Выбираем элементы очереди
                QueueItem[] result = new QueueItem[itemCount];
                int count = 0;

                for (int i = 0; i < itemCount; i++)
                {
                    QueueItem item = items[i];
                    if (item.ItemType == QueueItemType.Active &&
                        item.EventTime <= toDate)
                    { result[count++] = item; }
                }

                if (count < result.Length)
                { Array.Resize(ref result, count); }

                // 4. Выход - Ok
                return result;

            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Обновление элеметов очереди заданных магазинов
        /// </summary>
        /// <param name="shopId">ID обновляемых магазинов</param>
        /// <param name="shopItems">Элементы очереди заданных магазинов</param>
        /// <returns>0 - Обновление очереди выполнено; иначе - обвление очереди не выполнено</returns>
        public int Update(int[] shopId, QueueItem[] shopItems)
        {
            // 1. Иициализация
            int rc = 1;

            try
            {
                // 2. Удаляем все элементы заданых маазинов
                rc = 2;
                if (itemCount > 0 && shopId != null && shopId.Length > 0)
                {
                    Array.Sort(shopId);
                    int count = 0;

                    for (int i = 0; i < itemCount; i++)
                    {
                        if (Array.BinarySearch(shopId, items[i].Delivery.FromShop.Id) < 0)
                        { items[count++] = items[i]; }
                    }

                    itemCount = count;
                }

                // 3. Добавляем новые элементы
                rc = 3;
                if (shopItems != null && shopItems.Length > 0)
                {
                    int count = itemCount + shopItems.Length;
                    if (count > items.Length)
                    { Array.Resize(ref items, count); }

                    for (int i = 0; i < shopItems.Length; i++)
                    { items[itemCount++] = shopItems[i]; }
                }

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
        /// Удаление отработанных отгрузок
        /// </summary>
        public void Clear()
        {
            if (itemCount > 0)
            {
                int count = 0;
                for (int i = 0; i < itemCount; i++)
                {
                    if (items[i].ItemType == QueueItemType.Active)
                    { items[count++] = items[i]; }
                }

                itemCount = count;
            }
        }
    }
}
