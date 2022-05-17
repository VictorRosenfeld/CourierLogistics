

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
        public QueueItem[] Items { get; private set; }

        /// <summary>
        /// Количество элементов очереди
        /// </summary>
        public int Count => Items == null ? 0 : Items.Length;

        /// <summary>
        /// Флаг: true - очередь создана; false - очередь не создана
        /// </summary>
        public bool IsCreated { get; private set; }

        /// <summary>
        /// Очистка очереди
        /// </summary>
        public void Clear()
        {
            Items = new QueueItem[0];
        }

        /// <summary>
        /// Удалить отгузки из заданного магазина
        /// </summary>
        public void Clear(int shopId)
        {
            
        }



        /// <summary>
        /// Получить элементы, для которых наступило время 
        /// </summary>
        /// <param name="toDate"></param>
        /// <returns></returns>
        public QueueItem[]  GetToStart(DateTime toDate)
        {
            // 1. Инициализация
            
            try
            {
                // 2. Проверяем исходные данные
                if (Items == null || Items.Length <= 0)
                    return null;

                // 3. Выбираем элементы очереди
                QueueItem[] result = new QueueItem[Items.Length];
                int count = 0;

                for (int i = 0; i < Items.Length; i++)
                {
                    QueueItem item = Items[i];
                    if (item.EventTime <= toDate)
                    {
                        result[count++] = item;
                    }
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

    }
}
