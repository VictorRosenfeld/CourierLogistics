
namespace SQLCLR.Deliveries
{
    using SQLCLR.Couriers;
    using SQLCLR.Log;
    using SQLCLR.Orders;
    using SQLCLR.Shops;
    using System;
    using System.Collections.Generic;
    using System.Threading;

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
        /// <returns>0 - отгрузки построены; иначе - отгрузки не построены</returns>
        public static int Build(ThreadContext context)
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
                Point[,] geoData = context.GeoData;
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

                    //#if debug
                    //    Logger.WriteToLog(501, $"RouteBuilder.Build. rcFind = {rcFind}, i1 = {i1}, order_id = {orders[0].Id}", 0);
                    //#endif
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

        /// <summary>
        /// Построение всех возможных отгрузок для
        /// заданного контекста и гео-данных
        /// </summary>
        /// <param name="status">Расширенный контекст</param>
        /// <returns>0 - отгрузки построены; иначе - отгрузки не построены</returns>
        public unsafe static void BuildEx(object status)
        {
            // 1. Инициализация
            int rc = 1;
            ThreadContextEx context = status as ThreadContextEx;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (context == null)
                    return;
                context.Deliveries = null;
                Point[,] geoData = context.GeoData;
                if (geoData == null)
                    return;

                // 3. Извлекаем и проверяем данные из контекста
                rc = 3;
                int level = context.MaxRouteLength;
                DateTime calcTime = context.CalcTime;
                Order[] contextOrders = context.Orders;
                if (contextOrders == null || contextOrders.Length <= 0)
                    return;
                int orderCount = contextOrders.Length;
                Shop contextShop = context.ShopFrom;
                if (contextShop == null)
                    return;
                Courier contextCourier = context.ShopCourier;
                if (contextCourier == null)
                    return;
                long[] deliveryKeys = context.DeliveryKeys;
                if (deliveryKeys == null || deliveryKeys.Length <= 0)
                    return;
                int startIndex = context.StartOrderIndex;
                if (startIndex < 0 || startIndex >= orderCount)
                    return;
                int step = context.OrderIndexStep;
                if (step <= 0)
                    return;
                CourierDeliveryInfo[] deliveries = new CourierDeliveryInfo[deliveryKeys.Length];

                // 4. Цикл обработки
                rc = 4;
                CourierDeliveryInfo dictDelivery;
                CourierDeliveryInfo delivery;

                byte[] key1 = new byte[8];
                byte[] key2 = new byte[8];
                byte[] key3 = new byte[8];
                byte[] key4 = new byte[8];
                byte[] key5 = new byte[8];
                byte[] key6 = new byte[8];
                byte[] key7 = new byte[8];
                byte[] key8 = new byte[8];
                long key;
                //
                byte b1, b2, b3, b4, b5, b6, b7, b8;
                bool[] selectedOrders = new bool[orderCount];

                int index;

                int rcFind = 1;
                int shopIndex = orderCount;
                Order[] orders = new Order[8];
                int[] orderGeoIndex = new int[9];
                bool isLoop = !contextCourier.IsTaxi;

                // level 1
                for (int i1 = startIndex; i1 < orderCount; i1+=step)
                {
                    orderGeoIndex[0] = i1;
                    orderGeoIndex[1] = shopIndex;
                    orders[0] = contextOrders[i1];
                    rcFind = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 1, isLoop, geoData, out delivery);

                    //#if debug
                    //    Logger.WriteToLog(501, $"RouteBuilder.Build. rcFind = {rcFind}, i1 = {i1}, order_id = {orders[0].Id}", 0);
                    //#endif
                    if (rcFind != 0)
                        continue;

                    deliveries[i1] = delivery;
                    b1 = (byte)i1;
                    key1[0] = b1;
                    selectedOrders[i1] = true;

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

                            b2 = (byte)i2;
                            selectedOrders[i2] = true;

                            if (i1 < i2)
                            {
                                key2[0] = b1;
                                key2[1] = b2;
                            }
                            else
                            {
                                key2[0] = b2;
                                key2[1] = b1;
                            }

                            fixed (byte* pbyte = &key2[0])
                            { key = *((long*)pbyte); }
                            index = Array.BinarySearch(deliveryKeys, key);
                            dictDelivery = deliveries[index];
                            if (dictDelivery == null)
                            {
                                deliveries[index] = delivery;
                            }
                            else if (delivery.Cost < dictDelivery.Cost)
                            {
                                deliveries[index] = delivery;
                            }

                            // level 3
                            if (level >= 3)
                            {
                                for (int i3 = 0; i3 < orderCount; i3++)
                                {
                                    if (selectedOrders[i3])
                                        continue;
                                    orderGeoIndex[2] = i3;
                                    orderGeoIndex[3] = shopIndex;
                                    orders[2] = contextOrders[i3];
                                    rcFind = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 3, isLoop, geoData, out delivery);
                                    if (rcFind != 0)
                                        continue;

                                    b3 = (byte)i3;
                                    selectedOrders[i3] = true;

                                    // строим ключ
                                    if (b3 < key2[0])
                                    {
                                        key3[0] = b3;
                                        Buffer.BlockCopy(key2, 0, key3, 1, 2);
                                    }
                                    else if (i3 < key2[1])
                                    {
                                        key3[0] = key2[0];
                                        key3[1] = b3;
                                        key3[2] = key2[1];
                                    }
                                    else
                                    {
                                        Buffer.BlockCopy(key2, 0, key3, 0, 2);
                                        key3[2] = b3;
                                    }

                                    fixed (byte* pbyte = &key3[0])
                                    { key = *((long*)pbyte); }

                                    // обновляем отгрузку
                                    index = Array.BinarySearch(deliveryKeys, key);
                                    dictDelivery = deliveries[index];
                                    if (dictDelivery == null)
                                    {
                                        deliveries[index] = delivery;
                                    }
                                    else if (delivery.Cost < dictDelivery.Cost)
                                    {
                                        deliveries[index] = delivery;
                                    }

                                    // level 4
                                    if (level >= 4)
                                    {
                                        for (int i4 = 0; i4 < orderCount; i4++)
                                        {
                                            if (selectedOrders[i4])
                                                continue;
                                            orderGeoIndex[3] = i4;
                                            orderGeoIndex[4] = shopIndex;
                                            orders[3] = contextOrders[i4];
                                            rcFind = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 4, isLoop, geoData, out delivery);
                                            if (rcFind != 0)
                                                continue;

                                            b4 = (byte)i4;
                                            selectedOrders[i4] = true;

                                            // строим ключ
                                            if (b4 < key3[1])
                                            {
                                                if (b4 <= key3[0])
                                                {
                                                    key4[0] = b4;
                                                    Buffer.BlockCopy(key3, 0, key4, 1, 3);
                                                }
                                                else
                                                {
                                                    key4[0] = key3[0];
                                                    key4[1] = b4;
                                                    Buffer.BlockCopy(key3, 1, key4, 2, 2);
                                                }
                                            }
                                            else
                                            {
                                                if (i4 < key3[2])
                                                {
                                                    key4[0] = key3[0];
                                                    key4[1] = key3[1];
                                                    Buffer.BlockCopy(key3, 0, key4, 0, 2);
                                                    key4[2] = b4;
                                                    key4[3] = key3[2];
                                                }
                                                else
                                                {
                                                    Buffer.BlockCopy(key3, 0, key4, 0, 3);
                                                    key4[3] = b4;
                                                }
                                            }

                                            fixed (byte* pbyte = &key4[0])
                                            { key = *((long*)pbyte); }

                                            // обновляем отгрузку
                                            index = Array.BinarySearch(deliveryKeys, key);
                                            dictDelivery = deliveries[index];
                                            if (dictDelivery == null)
                                            {
                                                deliveries[index] = delivery;
                                            }
                                            else if (delivery.Cost < dictDelivery.Cost)
                                            {
                                                deliveries[index] = delivery;
                                            }

                                            // level 5
                                            if (level >= 5)
                                            {
                                                for (int i5 = 0; i5 < orderCount; i5++)
                                                {
                                                    if (selectedOrders[i5])
                                                        continue;

                                                    orderGeoIndex[4] = i5;
                                                    orderGeoIndex[5] = shopIndex;
                                                    orders[4] = contextOrders[i5];
                                                    rcFind = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 5, isLoop, geoData, out delivery);
                                                    if (rcFind != 0)
                                                        continue;

                                                    b5 = (byte) i5;
                                                    selectedOrders[i5] = true;

                                                    // строим ключ
                                                    // b0 b1 b2 b3
                                                    if (b5 < key4[2])
                                                    {
                                                        if (b5 <= key4[0])
                                                        {
                                                            key5[0] = b5;
                                                            Buffer.BlockCopy(key4, 0, key5, 1, 4);
                                                        }
                                                        else if (b5 <= key4[1])
                                                        {
                                                            key5[0] = key4[0];
                                                            key5[1] = b5;
                                                            Buffer.BlockCopy(key4, 1, key5, 2, 3);
                                                        }
                                                    }
                                                    else
                                                    {
                                                        if (b5 < key4[3])
                                                        {
                                                            Buffer.BlockCopy(key4, 0, key5, 0, 3);
                                                            key5[3] = b5;
                                                            key5[4] = key4[3];
                                                        }
                                                        else
                                                        {
                                                            Buffer.BlockCopy(key4, 0, key5, 0, 4);
                                                            key5[4] = b5;
                                                        }
                                                    }

                                                    fixed (byte* pbyte = &key5[0])
                                                    { key = *((long*)pbyte); }

                                                    // обновляем отгрузку
                                                    index = Array.BinarySearch(deliveryKeys, key);
                                                    dictDelivery = deliveries[index];
                                                    if (dictDelivery == null)
                                                    {
                                                        deliveries[index] = delivery;
                                                    }
                                                    else if (delivery.Cost < dictDelivery.Cost)
                                                    {
                                                        deliveries[index] = delivery;
                                                    }

                                                    // level 6
                                                    if (level >= 6)
                                                    {
                                                        for (int i6 = 0; i6 < orderCount; i6++)
                                                        {
                                                            if (selectedOrders[i6])
                                                                continue;

                                                            orderGeoIndex[5] = i6;
                                                            orderGeoIndex[6] = shopIndex;
                                                            orders[5] = contextOrders[i6];
                                                            rcFind = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 6, isLoop, geoData, out delivery);
                                                            if (rcFind != 0)
                                                                continue;

                                                            b6 = (byte) i6;
                                                            selectedOrders[i6] = true;

                                                            // строим ключ
                                                            // b0 b1 b2 b3 b4
                                                            if (b6 < key5[2])
                                                            {
                                                                if (b6 <= key5[0])
                                                                {
                                                                    key6[0] = b6;
                                                                    Buffer.BlockCopy(key5, 0, key6, 1, 5);
                                                                }
                                                                else if (b6 < key5[1])
                                                                {
                                                                    key6[0] = key5[0];
                                                                    key6[1] = b6;
                                                                    Buffer.BlockCopy(key5, 1, key6, 2, 4);
                                                                }
                                                                else
                                                                {
                                                                    Buffer.BlockCopy(key5, 0, key6, 0, 2);
                                                                    key6[2] = b6;
                                                                    Buffer.BlockCopy(key5, 2, key6, 3, 3);
                                                                }
                                                            }
                                                            else
                                                            {
                                                                if (b6 <= key5[3])
                                                                {
                                                                    Buffer.BlockCopy(key5, 0, key6, 0, 3);
                                                                    key6[3] = b6;
                                                                    Buffer.BlockCopy(key5, 3, key6, 4, 2);
                                                                }
                                                                else if (b6 < key5[4])
                                                                {
                                                                    Buffer.BlockCopy(key5, 0, key6, 0, 4);
                                                                    key6[4] = b6;
                                                                    key6[5] = key5[4];
                                                                }
                                                                else
                                                                {
                                                                    Buffer.BlockCopy(key5, 0, key6, 0, 5);
                                                                    key6[5] = b6;
                                                                }
                                                            }

                                                            fixed (byte* pbyte = &key6[0])
                                                            { key = *((long*)pbyte); }

                                                            // обновляем отгрузку
                                                            index = Array.BinarySearch(deliveryKeys, key);
                                                            dictDelivery = deliveries[index];
                                                            if (dictDelivery == null)
                                                            {
                                                                deliveries[index] = delivery;
                                                            }
                                                            else if (delivery.Cost < dictDelivery.Cost)
                                                            {
                                                                deliveries[index] = delivery;
                                                            }

                                                            // level 7
                                                            if (level >= 7)
                                                            {
                                                                for (int i7 = 0; i7 < orderCount; i7++)
                                                                {
                                                                    if (selectedOrders[i7])
                                                                        continue;

                                                                    orderGeoIndex[6] = i7;
                                                                    orderGeoIndex[7] = shopIndex;
                                                                    orders[6] = contextOrders[i7];
                                                                    rcFind = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 7, isLoop, geoData, out delivery);
                                                                    if (rcFind != 0)
                                                                        continue;

                                                                    b7 = (byte) i7;
                                                                    selectedOrders[i7] = true;

                                                                    // строим ключ
                                                                    // b0 b1 b2 b3 b4 b5

                                                                    if (b7 < key6[2])
                                                                    {
                                                                        if (b7 <= key6[0])
                                                                        {
                                                                            key7[0] = b7;
                                                                            Buffer.BlockCopy(key6, 0, key7, 1, 6);
                                                                        }
                                                                        else if (b7 < key6[1])
                                                                        {
                                                                            key7[0] = key6[0];
                                                                            key7[1] = b7;
                                                                            Buffer.BlockCopy(key6, 1, key7, 2, 5);
                                                                        }
                                                                        else
                                                                        {
                                                                            Buffer.BlockCopy(key6, 0, key7, 0, 2);
                                                                            key7[2] = b7;
                                                                            Buffer.BlockCopy(key6, 2, key7, 3, 4);
                                                                        }
                                                                    }
                                                                    else
                                                                    {
                                                                        if (b7 < key6[4])
                                                                        {
                                                                            if (b7 <= key6[3])
                                                                            {
                                                                                Buffer.BlockCopy(key6, 0, key7, 0, 3);
                                                                                key7[3] = b7;
                                                                                Buffer.BlockCopy(key6, 3, key7, 4, 3);
                                                                            }
                                                                            else
                                                                            {
                                                                                Buffer.BlockCopy(key6, 0, key7, 0, 4);
                                                                                key7[4] = b7;
                                                                                Buffer.BlockCopy(key6, 4, key7, 5, 2);
                                                                            }
                                                                        }
                                                                        else
                                                                        {
                                                                            if (b7 < key6[5])
                                                                            {
                                                                                Buffer.BlockCopy(key6, 0, key7, 0, 5);
                                                                                key7[5] = b7;
                                                                                key7[6] = key6[5];
                                                                            }
                                                                            else
                                                                            {
                                                                                Buffer.BlockCopy(key6, 0, key7, 0, 6);
                                                                                key7[6] = b7;
                                                                            }
                                                                        }
                                                                    }

                                                                    fixed (byte* pbyte = &key7[0])
                                                                    { key = *((long*)pbyte); }

                                                                    // обновляем отгрузку
                                                                    index = Array.BinarySearch(deliveryKeys, key);
                                                                    dictDelivery = deliveries[index];
                                                                    if (dictDelivery == null)
                                                                    {
                                                                        deliveries[index] = delivery;
                                                                    }
                                                                    else if (delivery.Cost < dictDelivery.Cost)
                                                                    {
                                                                        deliveries[index] = delivery;
                                                                    }

                                                                    // level 8
                                                                    if (level >= 8)
                                                                    {
                                                                        for (int i8 = 0; i8 < orderCount; i8++)
                                                                        {
                                                                            if (selectedOrders[i8])
                                                                                continue;

                                                                            orderGeoIndex[7] = i8;
                                                                            orderGeoIndex[8] = shopIndex;
                                                                            orders[7] = contextOrders[i8];
                                                                            rcFind = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 8, isLoop, geoData, out delivery);
                                                                            if (rcFind != 0)
                                                                                continue;

                                                                            b8 = (byte) i8;
                                                                            selectedOrders[i8] = true;

                                                                            // строим ключ
                                                                            if (b8 < key7[3])
                                                                            {
                                                                                if (b8 < key7[1])
                                                                                {
                                                                                    if (b8 <= key7[0])  // b b0 b1 b2 b3 b4 b5 b6
                                                                                    {
                                                                                        key8[0] = b8;
                                                                                        Buffer.BlockCopy(key7, 0, key8, 1, 7);
                                                                                    }
                                                                                    else              // b0 b b1 b2 b3 b4 b5 b6
                                                                                    {
                                                                                        key8[0] = key7[0];
                                                                                        key8[1] = b8;
                                                                                        Buffer.BlockCopy(key7, 1, key8, 2, 6);
                                                                                    }
                                                                                }
                                                                                else
                                                                                {
                                                                                    if (b8 <= key7[2])  // b0 b1 b b2 b3 b4 b5 b6
                                                                                    {
                                                                                        Buffer.BlockCopy(key7, 0, key8, 0, 2);
                                                                                        key8[2] = b8;
                                                                                        Buffer.BlockCopy(key7, 2, key8, 3, 5);
                                                                                    }
                                                                                    else              // b0 b1 b2 b b3 b4 b5 b6
                                                                                    {
                                                                                        Buffer.BlockCopy(key7, 0, key8, 0, 3);
                                                                                        key8[3] = b8;
                                                                                        Buffer.BlockCopy(key7, 3, key8, 4, 4);
                                                                                    }
                                                                                }
                                                                            }
                                                                            else
                                                                            {
                                                                                if (b8 < key7[5])
                                                                                {
                                                                                    if (b8 <= key7[4])  // b0 b1 b2 b3 b b4 b5 b6
                                                                                    {
                                                                                        Buffer.BlockCopy(key7, 0, key8, 0, 4);
                                                                                        key8[4] = b8;
                                                                                        Buffer.BlockCopy(key7, 4, key8, 5, 3);
                                                                                    }
                                                                                    else             // b0 b1 b2 b3 b4 b b5 b6
                                                                                    {
                                                                                        Buffer.BlockCopy(key7, 0, key8, 0, 5);
                                                                                        key8[5] = b8;
                                                                                        Buffer.BlockCopy(key7, 5, key8, 6, 2);
                                                                                    }
                                                                                }
                                                                                else
                                                                                {
                                                                                    if (b8 < key7[6])  // b0 b1 b2 b3 b4 b5 b b6
                                                                                    {
                                                                                        Buffer.BlockCopy(key7, 0, key8, 0, 6);
                                                                                        key8[6] = b8;
                                                                                        key8[7] = key7[6];
                                                                                    }
                                                                                    else             // b0 b1 b2 b3 b4 b5 b6 b
                                                                                    {
                                                                                        Buffer.BlockCopy(key7, 0, key8, 0, 7);
                                                                                        key8[7] = b8;
                                                                                    }
                                                                                }
                                                                            }


                                                                            fixed (byte* pbyte = &key8[0])
                                                                            { key = *((long*)pbyte); }

                                                                            // обновляем отгрузку
                                                                            index = Array.BinarySearch(deliveryKeys, key);
                                                                            dictDelivery = deliveries[index];
                                                                            if (dictDelivery == null)
                                                                            {
                                                                                deliveries[index] = delivery;
                                                                            }
                                                                            else if (delivery.Cost < dictDelivery.Cost)
                                                                            {
                                                                                deliveries[index] = delivery;
                                                                            }
                                                                            selectedOrders[i8] = false;
                                                                        }
                                                                    }
                                                                    selectedOrders[i7] = false;
                                                                }
                                                            }
                                                            selectedOrders[i6] = false;
                                                        }
                                                    }
                                                    selectedOrders[i5] = false;
                                                }
                                            }
                                            selectedOrders[i4] = false;
                                        }
                                    }
                                    selectedOrders[i3] = false;
                                }
                            }
                            selectedOrders[i2] = false;
                        }
                    }
                    selectedOrders[i1] = false;
                }

                // 5. Формируем результат
                rc = 5;
                //index = 0;
                //for (int i = 0; i < deliveries.Length; i++)
                //{
                //    if (deliveries[i] != null)
                //    {
                //        deliveries[index++] = deliveries[i];
                //    }
                //}

                //if (index < 0)
                //{
                //    Array.Resize(ref deliveries, index);
                //}

                context.Deliveries = deliveries;

                //deliveryKeys = null;

                // 6. Выход - Ok
                rc = 0;
                return;
            }
            catch
            {
                return;
            }
            finally
            {
                if (context != null) 
                {
                    context.ExitCode = rc;
                    ManualResetEvent syncEvent = context.SyncEvent;
                    if (syncEvent != null)
                    {
                        syncEvent.Set();
                    }
                }
            }
        }

        // Предельные значения числа заказов для разных длин маршрутов
        //    level orders   capacity
        //      8    30       8656936 23
        //      7    35       8731847 25
        //      6    44       8295045 30
        //      5    64       8303632 35
        //      4    119      8221710 120
        //      3    365      8104825 365
        //      2    4096     8390656 4096
        //      1    8000000  8000000 8000000

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
