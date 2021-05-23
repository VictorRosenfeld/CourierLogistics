
namespace SQLCLR.Deliveries
{
    using SQLCLR.Couriers;
    using SQLCLR.Orders;
    using SQLCLR.Shops;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Построитель отгрузок
    /// </summary>
    public class RouteBuilder
    {
        /// <summary>
        /// Построение всех возможных отгрузок для
        /// заданного контекста и гео-данных
        /// </summary>
        /// <param name="context">Контекст</param>
        /// <param name="geoData">Гео-данные</param>
        /// <returns>0 - отгрузки построены; иначе - отгрузки не построены</returns>
        public static int Build(ThreadContext context, Point[,] geoData)
        {
            // 1. Инициализация
            int rc = 1;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (context == null)
                    return rc;
                context.Deliveries = null;

                if (geoData == null)
                    return rc;

                // 3. Извлекаем данные из контекста
                rc = 3;
                int level = context.MaxRouteLength;
                DateTime calcTime = context.CalcTime;
                Order[] contextOrders = context.Orders;
                Shop contextShop = context.ShopFrom;
                Courier contextCourier = context.ShopCourier;

                // 4. Вычисляем примерное число отгрузок
                rc = 4;
                int orderCount = contextOrders.Length;
                int capacity = CalcCapacity(level, orderCount);

                // 5. Готовим цикл обработки
                rc = 5;
                Dictionary<int[], CourierDeliveryInfo> dictDeliveries = new Dictionary<int[], CourierDeliveryInfo>(capacity, new DeliveryKeyComparerEx(orderCount));
                CourierDeliveryInfo dictDelivery;
                CourierDeliveryInfo delivery;

                int[] key1 = new int[1];
                int[] key2 = new int[2];
                int[] key3 = new int[3];
                int[] key4 = new int[4];
                int[] key5 = new int[5];
                int[] key6 = new int[6];
                int[] key7 = new int[7];
                int[] key8 = new int[8];

                int index;
                int intsize = sizeof(int);
                int size;

                int rcFind = 1;
                int shopIndex = orderCount;
                Order[] orders = new Order[8];
                int[] orderGeoIndex = new int[9];
                bool isLoop = !contextCourier.IsTaxi;
                // level 1
                for (int i1 = 0; i1 < orderCount; i1++)
                {
                    orderGeoIndex[0] = i1;
                    orderGeoIndex[1] = shopIndex;
                    orders[0] = contextOrders[i1];
                    rcFind = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 1, isLoop, geoData, out delivery);
                    if (rcFind != 0)
                        continue;

                    key1[0] = i1;
                    dictDeliveries.Add(key1, delivery);
                    // level 2
                    if (level >= 2)
                    {
                        for (int i2 = 0; i2 < orderCount; i2++)
                        {
                            if (i2 == i1)
                                continue;
                            orderGeoIndex[1] = i2;
                            orderGeoIndex[2] = shopIndex;
                            orders[1] = contextOrders[i2];
                            rcFind = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 2, isLoop, geoData, out delivery);
                            if (rcFind != 0)
                                continue;

                            if (i1 < i2)
                            {
                                key2[0] = i1;
                                key2[1] = i2;
                            }
                            else
                            {
                                key2[0] = i2;
                                key2[1] = i1;
                            }

                            if (dictDeliveries.TryGetValue(key2, out dictDelivery))
                            {
                                if (delivery.Cost < dictDelivery.Cost)
                                    delivery.CopyTo(dictDelivery);
                            }
                            else
                            {
                                dictDeliveries.Add(key2, delivery);
                            }
                            // level 3
                            if (level >= 3)
                            {
                                for (int i3 = 0; i3 < orderCount; i3++)
                                {
                                    if (i3 == i1 || i3 == i2)
                                        continue;
                                    orderGeoIndex[2] = i3;
                                    orderGeoIndex[3] = shopIndex;
                                    orders[2] = contextOrders[i3];
                                    rcFind = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 3, isLoop, geoData, out delivery);
                                    if (rcFind != 0)
                                        continue;

                                    if (i3 < key2[0])
                                    {
                                        key3[0] = i3;
                                        key3[1] = key2[0];
                                        key3[2] = key2[1];
                                    }
                                    else if (i3 < key2[1])
                                    {
                                        key3[0] = key2[0];
                                        key3[1] = i3;
                                        key3[2] = key2[1];
                                    }
                                    else
                                    {
                                        key3[0] = key2[0];
                                        key3[1] = key2[1];
                                        key3[2] = i3;
                                    }

                                    if (dictDeliveries.TryGetValue(key3, out dictDelivery))
                                    {
                                        if (delivery.Cost < dictDelivery.Cost)
                                            delivery.CopyTo(dictDelivery);
                                    }
                                    else
                                    {
                                        dictDeliveries.Add(key3, delivery);
                                    }
                                    // level 4
                                    if (level >= 4)
                                    {
                                        for (int i4 = 0; i4 < orderCount; i4++)
                                        {
                                            if (i4 == i1 || i4 == i2 || i4 == i3)
                                                continue;
                                            orderGeoIndex[3] = i4;
                                            orderGeoIndex[4] = shopIndex;
                                            orders[3] = contextOrders[i4];
                                            rcFind = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 4, isLoop, geoData, out delivery);
                                            if (rcFind != 0)
                                                continue;

                                            if (i4 < key3[1])
                                            {
                                                if (i4 < key3[0])
                                                {
                                                    key4[0] = i4;
                                                    key3.CopyTo(key4, 1);
                                                }
                                                else
                                                {
                                                    key4[0] = key3[0];
                                                    key4[1] = i4;
                                                    key4[2] = key3[1];
                                                    key4[3] = key3[2];
                                                }
                                            }
                                            else
                                            {
                                                if (i4 < key3[2])
                                                {
                                                    key4[0] = key3[0];
                                                    key4[1] = key3[1];
                                                    key4[2] = i4;
                                                    key4[3] = key3[2];

                                                }
                                                else
                                                {
                                                    key3.CopyTo(key4, 0);
                                                    key4[3] = i4;
                                                }
                                            }

                                            if (dictDeliveries.TryGetValue(key4, out dictDelivery))
                                            {
                                                if (delivery.Cost < dictDelivery.Cost)
                                                    delivery.CopyTo(dictDelivery);
                                            }
                                            else
                                            {
                                                dictDeliveries.Add(key4, delivery);
                                            }
                                            // level 5
                                            if (level >= 5)
                                            {
                                                for (int i5 = 0; i5 < orderCount; i5++)
                                                {
                                                    index = Array.BinarySearch(key4, i5);
                                                    if (index >= 0)
                                                        continue;

                                                    orderGeoIndex[4] = i5;
                                                    orderGeoIndex[5] = shopIndex;
                                                    orders[4] = contextOrders[i5];
                                                    rcFind = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 5, isLoop, geoData, out delivery);
                                                    if (rcFind != 0)
                                                        continue;

                                                    index = ~index;
                                                    size = intsize * index;
                                                    Buffer.BlockCopy(key4, 0, key5, 0, size);
                                                    key5[index] = i5;
                                                    Buffer.BlockCopy(key4, size, key5, size + intsize, intsize * (4 - index));
                                                    if (dictDeliveries.TryGetValue(key5, out dictDelivery))
                                                    {
                                                        if (delivery.Cost < dictDelivery.Cost)
                                                            delivery.CopyTo(dictDelivery);
                                                    }
                                                    else
                                                    {
                                                        dictDeliveries.Add(key5, delivery);
                                                    }
                                                    // level 6
                                                    if (level >= 6)
                                                    {
                                                        for (int i6 = 0; i6 < orderCount; i6++)
                                                        {
                                                            index = Array.BinarySearch(key5, i6);
                                                            if (index >= 0)
                                                                continue;

                                                            orderGeoIndex[5] = i6;
                                                            orderGeoIndex[6] = shopIndex;
                                                            orders[5] = contextOrders[i6];
                                                            rcFind = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 6, isLoop, geoData, out delivery);
                                                            if (rcFind != 0)
                                                                continue;

                                                            index = ~index;
                                                            size = intsize * index;
                                                            Buffer.BlockCopy(key5, 0, key6, 0, size);
                                                            key6[index] = i6;
                                                            Buffer.BlockCopy(key5, size, key6, size + intsize, intsize * (5 - index));
                                                            if (dictDeliveries.TryGetValue(key6, out dictDelivery))
                                                            {
                                                                if (delivery.Cost < dictDelivery.Cost)
                                                                    delivery.CopyTo(dictDelivery);
                                                            }
                                                            else
                                                            {
                                                                dictDeliveries.Add(key6, delivery);
                                                            }
                                                            // level 7
                                                            if (level >= 7)
                                                            {
                                                                for (int i7 = 0; i7 < orderCount; i7++)
                                                                {
                                                                    index = Array.BinarySearch(key6, i7);
                                                                    if (index >= 0)
                                                                        continue;

                                                                    orderGeoIndex[6] = i7;
                                                                    orderGeoIndex[7] = shopIndex;
                                                                    orders[6] = contextOrders[i7];
                                                                    rcFind = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 7, isLoop, geoData, out delivery);
                                                                    if (rcFind != 0)
                                                                        continue;

                                                                    index = ~index;
                                                                    size = intsize * index;
                                                                    Buffer.BlockCopy(key6, 0, key7, 0, size);
                                                                    key7[index] = i7;
                                                                    Buffer.BlockCopy(key6, size, key7, size + intsize, intsize * (6 - index));
                                                                    if (dictDeliveries.TryGetValue(key7, out dictDelivery))
                                                                    {
                                                                        if (delivery.Cost < dictDelivery.Cost)
                                                                            delivery.CopyTo(dictDelivery);
                                                                    }
                                                                    else
                                                                    {
                                                                        dictDeliveries.Add(key7, delivery);
                                                                    }
                                                                    // level 8
                                                                    if (level >= 8)
                                                                    {
                                                                        for (int i8 = 0; i8 < orderCount; i8++)
                                                                        {
                                                                            index = Array.BinarySearch(key7, i8);
                                                                            if (index >= 0)
                                                                                continue;

                                                                            orderGeoIndex[7] = i8;
                                                                            orderGeoIndex[8] = shopIndex;
                                                                            orders[7] = contextOrders[i8];
                                                                            rcFind = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 8, isLoop, geoData, out delivery);
                                                                            if (rcFind != 0)
                                                                                continue;

                                                                            index = ~index;
                                                                            size = intsize * index;
                                                                            Buffer.BlockCopy(key7, 0, key8, 0, size);
                                                                            key8[index] = i8;
                                                                            Buffer.BlockCopy(key7, size, key8, size + intsize, intsize * (7 - index));
                                                                            if (dictDeliveries.TryGetValue(key8, out dictDelivery))
                                                                            {
                                                                                if (delivery.Cost < dictDelivery.Cost)
                                                                                    delivery.CopyTo(dictDelivery);
                                                                            }
                                                                            else
                                                                            {
                                                                                dictDeliveries.Add(key8, delivery);
                                                                            }
                                                                        }
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                // 6. Формируем результат
                rc = 6;
                CourierDeliveryInfo[] deliveries = new CourierDeliveryInfo[dictDeliveries.Count];
                dictDeliveries.Values.CopyTo(deliveries, 0);
                context.Deliveries = deliveries;
                dictDeliveries = null;

                // 7. Выход - Ok
                rc = 0;
                return rc;
            }
            catch
            {
                return rc;
            }
        }

        // Предельные значения числа заказов для разных длин маршрутов
        //    level orders   capacity
        //      8    30       8656936
        //      7    35       8731847
        //      6    44       8295045
        //      5    64       8303632
        //      4    119      8221710
        //      3    365      8104825
        //      2    4096     8390656
        //      1    8000000  8000000

        /// <summary>
        /// Вычисление приближенного числа
        /// ожидаемых отгрузок
        /// </summary>
        /// <param name="level">Максимальная длина маршрута</param>
        /// <param name="orderCount">Количество заказов</param>
        /// <returns>Количество отгрузок</returns>
        private static int CalcCapacity(int level, int orderCount)
        {
            if (level > 8)
                level = 8;

            switch (level)
            {
                case 8:
                    if (orderCount >= 30)
                        return 4000000;
                    break;
                case 7:
                    if (orderCount >= 35)
                        return 4000000;
                    break;
                case 6:
                    if (orderCount >= 44)
                        return 4000000;
                    break;
                case 5:
                    if (orderCount >= 64)
                        return 4000000;
                    break;
                case 4:
                    if (orderCount >= 119)
                        return 4000000;
                    break;
                case 3:
                    if (orderCount >= 365)
                        return 4000000;
                    break;
                case 2:
                    if (orderCount >= 4096)
                        return 4000000;
                    break;
                case 1:
                    if (orderCount >= 8000000)
                        return 4000000;
                    return (orderCount + 1) / 2;
            }

            int capacity = 0;
            for (int i = 1; i <= level; i++)
            {
                capacity += C(orderCount, i);
            }

            capacity /= 2;
            if (capacity < 16)
                capacity = 16;

            return capacity;
        }

        /// <summary>
        /// Значение биномиального коэффициента
        /// </summary>
        /// <param name="n">Общее число элементов</param>
        /// <param name="m">Число выбираемых элементов</param>
        /// <returns>Значение коэффициента</returns>
        private static int C(int n, int m)
        {
            long X = 1;
            long Y = 1;

            for (int i = 1; i <= m; i++)
            {
                X = X * (n - i + 1);
                Y = Y * i;
            }

            return (int)(X / Y);
        }
    }
}
