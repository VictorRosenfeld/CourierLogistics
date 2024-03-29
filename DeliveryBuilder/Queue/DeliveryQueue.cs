﻿

namespace DeliveryBuilder.Queue
{
    using DeliveryBuilder.Recalc;
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
                    if (item != null)
                    {
                        if (item.ItemType == QueueItemType.Active && item.EventTime <= toDate)
                        { result[count++] = item; }
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

        ///// <summary>
        ///// Обновление элеметов очереди заданных магазинов
        ///// </summary>
        ///// <param name="shopId">ID обновляемых магазинов</param>
        ///// <param name="shopItems">Элементы очереди заданных магазинов</param>
        ///// <returns>0 - Обновление очереди выполнено; иначе - обвление очереди не выполнено</returns>
        //public int Update(int[] shopId, QueueItem[] shopItems)
        //{
        //    // 1. Иициализация
        //    int rc = 1;

        //    try
        //    {
        //        // 2. Удаляем все элементы заданых маазинов
        //        rc = 2;
        //        if (itemCount > 0 && shopId != null && shopId.Length > 0)
        //        {
        //            Array.Sort(shopId);
        //            int count = 0;

        //            for (int i = 0; i < itemCount; i++)
        //            {
        //                if (Array.BinarySearch(shopId, items[i].Delivery.FromShop.Id) < 0)
        //                { items[count++] = items[i]; }
        //            }

        //            itemCount = count;
        //        }

        //        // 3. Добавляем новые элементы
        //        rc = 3;
        //        if (shopItems != null && shopItems.Length > 0)
        //        {
        //            int count = itemCount + shopItems.Length;
        //            if (count > items.Length)
        //            { Array.Resize(ref items, count); }

        //            for (int i = 0; i < shopItems.Length; i++)
        //            { items[itemCount++] = shopItems[i]; }
        //        }

        //        // 4. Выход - Ok
        //        rc = 0;
        //        return rc;
        //    }
        //    catch
        //    {
        //        return rc;
        //    }
        //}

        /// <summary>
        /// Обновление элеметов очереди заданных магазинов
        /// </summary>
        /// <param name="shopId">ID обновляемых магазинов</param>
        /// <param name="deliveries">Элементы очереди заданных магазинов</param>
        /// <returns>0 - Обновление очереди выполнено; иначе - обвление очереди не выполнено</returns>
        public int Update(int[] shopId, CourierDeliveryInfo[] deliveries)
        {
            // 1. Иициализация
            int rc = 1;

            try
            {
                // 2. Удаляем все элементы заданых магазинов
                rc = 2;
                if (itemCount > 0 && shopId != null && shopId.Length > 0)
                {
                    Array.Sort(shopId);
                    int count = 0;

                    for (int i = 0; i < itemCount; i++)
                    {
                        if (Array.BinarySearch(shopId, items[i].Delivery.FromShop.Id) < 0)
                        { items[count++] = items[i]; }
                        else
                        { items[i] = null; }
                    }

                    itemCount = count;
                }

                // 3. Добавляем новые элементы
                rc = 3;
                if (deliveries != null && deliveries.Length > 0)
                {
                    int count = itemCount + deliveries.Length;
                    if (count > items.Length)
                    { Array.Resize(ref items, count); }

                    for (int i = 0; i < deliveries.Length; i++)
                    { items[itemCount++] = new QueueItem(deliveries[i].EventTime, QueueItemType.Active, deliveries[i]); }
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
                    if (items[i] != null && items[i].ItemType == QueueItemType.Active)
                    { items[count++] = items[i]; }
                    else
                    { items[i] = null; }
                }

                itemCount = count;
            }
        }

        /// <summary>
        /// Выборка ID всех заказов, требующих отгрузки
        /// </summary>
        /// <returns></returns>
        public int[] GetOrderIds()
        {
            // 1. Инициализация
            
            try
            {
                // 2. Проверяем исходные данные
                if (itemCount <= 0)
                { return new int[0]; }

                // 3. Выбираем ID всех заказов, требующих отгрузки
                int[] orderIds = new int[8 * itemCount];
                int count = 0;

                for (int i = 0; i < itemCount; i++)
                {
                    QueueItem item = items[i];
                    if (item != null && item.ItemType == QueueItemType.Active && item.Delivery != null)
                    {
                        foreach (var order in item.Delivery.Orders)
                        {
                            if (count >= orderIds.Length)
                            { Array.Resize(ref orderIds, orderIds.Length + Math.Max(100, item.Delivery.Orders.Length)); }
                            orderIds[count++] = order.Id;
                        }
                    }
                }

                if (count < orderIds.Length)
                { Array.Resize(ref orderIds, count); }

                // 4. Выход - Ok;
                return orderIds;
            }
            catch
            {
                return null;
            }
        }
    }
}
