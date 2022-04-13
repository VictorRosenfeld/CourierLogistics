
namespace SQLCLRex.Deliveries
{
    using SQLCLRex.Couriers;
    using SQLCLRex.Log;
    using SQLCLRex.Orders;
    using SQLCLRex.Shops;
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
        public unsafe static void BuildEx_old(object status)
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
//#if (debug)
//                Logger.WriteToLog(309, $"BuildEx. vehicleID = {context.ShopCourier.VehicleID}. startIndex = {context.StartOrderIndex}. step = {context.OrderIndexStep}", 0);
//#endif
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
                //CourierDeliveryInfo delivery;
                CourierDeliveryInfo delivery1;
                CourierDeliveryInfo delivery2;
                CourierDeliveryInfo delivery3;
                CourierDeliveryInfo delivery4;
                CourierDeliveryInfo delivery5;
                CourierDeliveryInfo delivery6;
                CourierDeliveryInfo delivery7;
                CourierDeliveryInfo delivery8;

                //byte[] key1 = new byte[8];
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
                double handInTime = contextCourier.HandInTime;
                DateTime t1;
                DateTime t2;
                double dtx;
                Order order;

                // level 1
                for (int i1 = startIndex; i1 < orderCount; i1+=step)
                {
                    orderGeoIndex[0] = i1;
                    orderGeoIndex[1] = shopIndex;
                    orders[0] = contextOrders[i1];
                    rcFind = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 1, isLoop, geoData, out delivery1);

                    //#if debug
                    //    Logger.WriteToLog(501, $"RouteBuilder.Build. rcFind = {rcFind}, i1 = {i1}, order_id = {orders[0].Id}", 0);
                    //#endif
                    if (rcFind != 0)
                        continue;

                    deliveries[i1] = delivery1;
                    b1 = (byte)i1;
                    //key1[0] = b1;
                    selectedOrders[i1] = true;

                    // level 2
                    if (level >= 2)
                    {
                        for (int i2 = 0; i2 < orderCount; i2++)
                        {
                            if (i2 == i1)
                                continue;

                            order = contextOrders[i2];

                            dtx = delivery1.NodeDeliveryTime[1] + geoData[i1, i2].Y / 60.0 + handInTime;
                            t1 = delivery1.StartDeliveryInterval.AddMinutes(dtx);
                            t2 = delivery1.EndDeliveryInterval.AddMinutes(dtx);
                            if (order.DeliveryTimeFrom > t1)
                                t1 = order.DeliveryTimeFrom;
                            if (order.DeliveryTimeTo < t2)
                                t2 = order.DeliveryTimeTo;
                            if (t1 > t2)
                                continue;

                            orderGeoIndex[1] = i2;
                            orderGeoIndex[2] = shopIndex;
                            //orders[1] = contextOrders[i2];
                            orders[1] = order;
                            rcFind = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 2, isLoop, geoData, out delivery2);
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
                                deliveries[index] = delivery2;
                            }
                            else if (delivery2.Cost < dictDelivery.Cost)
                            {
                                deliveries[index] = delivery2;
                            }

                            // level 3
                            if (level >= 3)
                            {
                                for (int i3 = 0; i3 < orderCount; i3++)
                                {
                                    if (selectedOrders[i3])
                                        continue;

                                    order = contextOrders[i3];

                                    dtx = delivery2.NodeDeliveryTime[2] + geoData[i2, i3].Y / 60.0 + handInTime;
                                    t1 = delivery2.StartDeliveryInterval.AddMinutes(dtx);
                                    t2 = delivery2.EndDeliveryInterval.AddMinutes(dtx);
                                    if (order.DeliveryTimeFrom > t1)
                                        t1 = order.DeliveryTimeFrom;
                                    if (order.DeliveryTimeTo < t2)
                                        t2 = order.DeliveryTimeTo;
                                    if (t1 > t2)
                                        continue;

                                    orderGeoIndex[2] = i3;
                                    orderGeoIndex[3] = shopIndex;
                                    //orders[2] = contextOrders[i3];
                                    orders[2] = order;
                                    rcFind = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 3, isLoop, geoData, out delivery3);
                                    if (rcFind != 0)
                                        continue;
                                    //rcFind = contextCourier.DeliveryCheckEx(delivery2, order, geoData[i2, i3], geoData[i3, shopIndex], t1, t2, out delivery3);
                                    //if (rcFind != 0)
                                    //    continue;

                                    b3 = (byte)i3;
                                    selectedOrders[i3] = true;

                                    // строим ключ
                                    if (b3 < key2[0])
                                    {
                                        key3[0] = b3;
                                        Buffer.BlockCopy(key2, 0, key3, 1, 2);
                                    }
                                    else if (b3 < key2[1])
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
                                        deliveries[index] = delivery3;
                                    }
                                    else if (delivery3.Cost < dictDelivery.Cost)
                                    {
                                        deliveries[index] = delivery3;
                                    }

                                    // level 4
                                    if (level >= 4)
                                    {
                                        for (int i4 = 0; i4 < orderCount; i4++)
                                        {
                                            if (selectedOrders[i4])
                                                continue;

                                            order = contextOrders[i4];

                                            dtx = delivery3.NodeDeliveryTime[3] + geoData[i3, i4].Y / 60.0 + handInTime;
                                            t1 = delivery3.StartDeliveryInterval.AddMinutes(dtx);
                                            t2 = delivery3.EndDeliveryInterval.AddMinutes(dtx);
                                            if (order.DeliveryTimeFrom > t1)
                                                t1 = order.DeliveryTimeFrom;
                                            if (order.DeliveryTimeTo < t2)
                                                t2 = order.DeliveryTimeTo;
                                            if (t1 > t2)
                                                continue;

                                            orderGeoIndex[3] = i4;
                                            orderGeoIndex[4] = shopIndex;
                                            //orders[3] = contextOrders[i4];
                                            orders[3] = order;
                                            rcFind = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 4, isLoop, geoData, out delivery4);
                                            if (rcFind != 0)
                                                continue;
                                            //rcFind = contextCourier.DeliveryCheckEx(delivery3, order, geoData[i3, i4], geoData[i4, shopIndex], t1, t2, out delivery4);
                                            //if (rcFind != 0)
                                            //    continue;

                                            b4 = (byte)i4;
                                            selectedOrders[i4] = true;

                                            // строим ключ
                                            // b0 b1 b2
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
                                                if (b4 < key3[2])
                                                {
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
                                                deliveries[index] = delivery4;
                                            }
                                            else if (delivery4.Cost < dictDelivery.Cost)
                                            {
                                                deliveries[index] = delivery4;
                                            }

                                            // level 5
                                            if (level >= 5)
                                            {
                                                for (int i5 = 0; i5 < orderCount; i5++)
                                                {
                                                    if (selectedOrders[i5])
                                                        continue;

                                                    order =  contextOrders[i5];

                                                    dtx = delivery4.NodeDeliveryTime[4] + geoData[i4, i5].Y / 60.0 + handInTime;
                                                    t1 = delivery4.StartDeliveryInterval.AddMinutes(dtx);
                                                    t2 = delivery4.EndDeliveryInterval.AddMinutes(dtx);
                                                    if (order.DeliveryTimeFrom > t1)
                                                        t1 = order.DeliveryTimeFrom;
                                                    if (order.DeliveryTimeTo < t2)
                                                        t2 = order.DeliveryTimeTo;
                                                    if (t1 > t2)
                                                        continue;

                                                    orderGeoIndex[4] = i5;
                                                    orderGeoIndex[5] = shopIndex;
                                                    //orders[4] = contextOrders[i5];
                                                    orders[4] = order;
                                                    rcFind = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 5, isLoop, geoData, out delivery5);
                                                    if (rcFind != 0)
                                                        continue;
                                                    //rcFind = contextCourier.DeliveryCheckEx(delivery4, order, geoData[i4, i5], geoData[i5, shopIndex], t1, t2, out delivery5);
                                                    //if (rcFind != 0)
                                                    //    continue;

                                                    b5 = (byte) i5;
                                                    selectedOrders[i5] = true;

                                                    // строим ключ
                                                    // b0 b1 b2 b3
                                                    if (b5 < key4[2])
                                                    {
                                                        if (b5 <= key4[0])      // b5 b0 b1 b2 b3
                                                        {
                                                            key5[0] = b5;
                                                            Buffer.BlockCopy(key4, 0, key5, 1, 4);
                                                        }
                                                        else if (b5 <= key4[1]) // b0 b5 b1 b2 b3
                                                        {
                                                            key5[0] = key4[0];
                                                            key5[1] = b5;
                                                            Buffer.BlockCopy(key4, 1, key5, 2, 3);
                                                        }
                                                        else                    // b0 b1 b5 b2 b3
                                                        {
                                                            Buffer.BlockCopy(key4, 0, key5, 0, 2);
                                                            key5[2] = b5;
                                                            Buffer.BlockCopy(key4, 2, key5, 3, 2);
                                                        }
                                                    }
                                                    else
                                                    {
                                                        if (b5 < key4[3])      // b0 b1 b2 b5 b3
                                                        {
                                                            Buffer.BlockCopy(key4, 0, key5, 0, 3);
                                                            key5[3] = b5;
                                                            key5[4] = key4[3];
                                                        }
                                                        else                   // b0 b1 b2 b3 b5
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
                                                        deliveries[index] = delivery5;
                                                    }
                                                    else if (delivery5.Cost < dictDelivery.Cost)
                                                    {
                                                        deliveries[index] = delivery5;
                                                    }

                                                    // level 6
                                                    if (level >= 6)
                                                    {
                                                        for (int i6 = 0; i6 < orderCount; i6++)
                                                        {
                                                            if (selectedOrders[i6])
                                                                continue;

                                                            order =  contextOrders[i6];

                                                            dtx = delivery5.NodeDeliveryTime[5] + geoData[i5, i6].Y / 60.0 + handInTime;
                                                            t1 = delivery5.StartDeliveryInterval.AddMinutes(dtx);
                                                            t2 = delivery5.EndDeliveryInterval.AddMinutes(dtx);
                                                            if (order.DeliveryTimeFrom > t1)
                                                                t1 = order.DeliveryTimeFrom;
                                                            if (order.DeliveryTimeTo < t2)
                                                                t2 = order.DeliveryTimeTo;
                                                            if (t1 > t2)
                                                                continue;

                                                            orderGeoIndex[5] = i6;
                                                            orderGeoIndex[6] = shopIndex;
                                                            //orders[5] = contextOrders[i6];
                                                            orders[5] = order;
                                                            rcFind = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 6, isLoop, geoData, out delivery6);
                                                            if (rcFind != 0)
                                                                continue;

                                                            b6 = (byte) i6;
                                                            selectedOrders[i6] = true;

                                                            // строим ключ
                                                            // b0 b1 b2 b3 b4
                                                            if (b6 < key5[2])
                                                            {
                                                                if (b6 <= key5[0])      // b6 b0 b1 b2 b3 b4
                                                                {
                                                                    key6[0] = b6;
                                                                    Buffer.BlockCopy(key5, 0, key6, 1, 5);
                                                                }
                                                                else if (b6 < key5[1])  // b0 b6 b1 b2 b3 b4
                                                                {
                                                                    key6[0] = key5[0];
                                                                    key6[1] = b6;
                                                                    Buffer.BlockCopy(key5, 1, key6, 2, 4);
                                                                }
                                                                else                    // b0 b1 b6 b2 b3 b4
                                                                {
                                                                    Buffer.BlockCopy(key5, 0, key6, 0, 2);
                                                                    key6[2] = b6;
                                                                    Buffer.BlockCopy(key5, 2, key6, 3, 3);
                                                                }
                                                            }
                                                            else
                                                            {
                                                                if (b6 <= key5[3])      // b0 b1 b2 b6 b3 b4
                                                                {
                                                                    Buffer.BlockCopy(key5, 0, key6, 0, 3);
                                                                    key6[3] = b6;
                                                                    Buffer.BlockCopy(key5, 3, key6, 4, 2);
                                                                }
                                                                else if (b6 < key5[4])  // b0 b1 b2 b3 b6 b4
                                                                {
                                                                    Buffer.BlockCopy(key5, 0, key6, 0, 4);
                                                                    key6[4] = b6;
                                                                    key6[5] = key5[4];
                                                                }
                                                                else                    // b0 b1 b2 b3 b4 b6
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
                                                                deliveries[index] = delivery6;
                                                            }
                                                            else if (delivery6.Cost < dictDelivery.Cost)
                                                            {
                                                                deliveries[index] = delivery6;
                                                            }

                                                            // level 7
                                                            if (level >= 7)
                                                            {
                                                                for (int i7 = 0; i7 < orderCount; i7++)
                                                                {
                                                                    if (selectedOrders[i7])
                                                                        continue;

                                                                    order = contextOrders[i7];

                                                                    dtx = delivery6.NodeDeliveryTime[6] + geoData[i6, i7].Y / 60.0 + handInTime;
                                                                    t1 = delivery6.StartDeliveryInterval.AddMinutes(dtx);
                                                                    t2 = delivery6.EndDeliveryInterval.AddMinutes(dtx);
                                                                    if (order.DeliveryTimeFrom > t1)
                                                                        t1 = order.DeliveryTimeFrom;
                                                                    if (order.DeliveryTimeTo < t2)
                                                                        t2 = order.DeliveryTimeTo;
                                                                    if (t1 > t2)
                                                                        continue;

                                                                    orderGeoIndex[6] = i7;
                                                                    orderGeoIndex[7] = shopIndex;
                                                                    //orders[6] = contextOrders[i7];
                                                                    orders[6] = order;
                                                                    rcFind = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 7, isLoop, geoData, out delivery7);
                                                                    if (rcFind != 0)
                                                                        continue;

                                                                    b7 = (byte) i7;
                                                                    selectedOrders[i7] = true;

                                                                    // строим ключ
                                                                    // b0 b1 b2 b3 b4 b5

                                                                    if (b7 < key6[2])
                                                                    {
                                                                        if (b7 <= key6[0])      // b7 b0 b1 b2 b3 b4 b5
                                                                        {
                                                                            key7[0] = b7;
                                                                            Buffer.BlockCopy(key6, 0, key7, 1, 6);
                                                                        }
                                                                        else if (b7 < key6[1])  // b0 b7 b1 b2 b3 b4 b5
                                                                        {
                                                                            key7[0] = key6[0];
                                                                            key7[1] = b7;
                                                                            Buffer.BlockCopy(key6, 1, key7, 2, 5);
                                                                        }
                                                                        else                    // b0 b1 b7 b2 b3 b4 b5
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
                                                                            if (b7 <= key6[3])  // b0 b1 b2 b7 b3 b4 b5
                                                                            {
                                                                                Buffer.BlockCopy(key6, 0, key7, 0, 3);
                                                                                key7[3] = b7;
                                                                                Buffer.BlockCopy(key6, 3, key7, 4, 3);
                                                                            }
                                                                            else                // b0 b1 b2 b3 b7 b4 b5
                                                                            {
                                                                                Buffer.BlockCopy(key6, 0, key7, 0, 4);
                                                                                key7[4] = b7;
                                                                                Buffer.BlockCopy(key6, 4, key7, 5, 2);
                                                                            }
                                                                        }
                                                                        else
                                                                        {
                                                                            if (b7 < key6[5])   // b0 b1 b2 b3 b4 b7 b5
                                                                            {
                                                                                Buffer.BlockCopy(key6, 0, key7, 0, 5);
                                                                                key7[5] = b7;
                                                                                key7[6] = key6[5];
                                                                            }
                                                                            else                // b0 b1 b2 b3 b4 b5 b7
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
                                                                        deliveries[index] = delivery7;
                                                                    }
                                                                    else if (delivery7.Cost < dictDelivery.Cost)
                                                                    {
                                                                        deliveries[index] = delivery7;
                                                                    }

                                                                    // level 8
                                                                    if (level >= 8)
                                                                    {
                                                                        for (int i8 = 0; i8 < orderCount; i8++)
                                                                        {
                                                                            if (selectedOrders[i8])
                                                                                continue;

                                                                            order = contextOrders[i8];

                                                                            dtx = delivery7.NodeDeliveryTime[7] + geoData[i7, i8].Y / 60.0 + handInTime;
                                                                            t1 = delivery7.StartDeliveryInterval.AddMinutes(dtx);
                                                                            t2 = delivery7.EndDeliveryInterval.AddMinutes(dtx);
                                                                            if (order.DeliveryTimeFrom > t1)
                                                                                t1 = order.DeliveryTimeFrom;
                                                                            if (order.DeliveryTimeTo < t2)
                                                                                t2 = order.DeliveryTimeTo;
                                                                            if (t1 > t2)
                                                                                continue;

                                                                            orderGeoIndex[7] = i8;
                                                                            orderGeoIndex[8] = shopIndex;
                                                                            //orders[7] = contextOrders[i8];
                                                                            orders[7] = order;
                                                                            rcFind = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 8, isLoop, geoData, out delivery8);
                                                                            if (rcFind != 0)
                                                                                continue;

                                                                            b8 = (byte) i8;
                                                                            selectedOrders[i8] = true;

                                                                            // строим ключ
                                                                            // b0 b1 b2 b3 b4 b5 b6
                                                                            if (b8 < key7[3])
                                                                            {
                                                                                if (b8 < key7[1])
                                                                                {
                                                                                    if (b8 <= key7[0])  // b8 b0 b1 b2 b3 b4 b5 b6
                                                                                    {
                                                                                        key8[0] = b8;
                                                                                        Buffer.BlockCopy(key7, 0, key8, 1, 7);
                                                                                    }
                                                                                    else                // b0 b8 b1 b2 b3 b4 b5 b6
                                                                                    {
                                                                                        key8[0] = key7[0];
                                                                                        key8[1] = b8;
                                                                                        Buffer.BlockCopy(key7, 1, key8, 2, 6);
                                                                                    }
                                                                                }
                                                                                else
                                                                                {
                                                                                    if (b8 <= key7[2])  // b0 b1 b8 b2 b3 b4 b5 b6
                                                                                    {
                                                                                        Buffer.BlockCopy(key7, 0, key8, 0, 2);
                                                                                        key8[2] = b8;
                                                                                        Buffer.BlockCopy(key7, 2, key8, 3, 5);
                                                                                    }
                                                                                    else                // b0 b1 b2 b8 b3 b4 b5 b6
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
                                                                                    if (b8 <= key7[4])  // b0 b1 b2 b3 b8 b4 b5 b6
                                                                                    {
                                                                                        Buffer.BlockCopy(key7, 0, key8, 0, 4);
                                                                                        key8[4] = b8;
                                                                                        Buffer.BlockCopy(key7, 4, key8, 5, 3);
                                                                                    }
                                                                                    else                // b0 b1 b2 b3 b4 b8 b5 b6
                                                                                    {
                                                                                        Buffer.BlockCopy(key7, 0, key8, 0, 5);
                                                                                        key8[5] = b8;
                                                                                        Buffer.BlockCopy(key7, 5, key8, 6, 2);
                                                                                    }
                                                                                }
                                                                                else
                                                                                {
                                                                                    if (b8 < key7[6])   // b0 b1 b2 b3 b4 b5 b8 b6
                                                                                    {
                                                                                        Buffer.BlockCopy(key7, 0, key8, 0, 6);
                                                                                        key8[6] = b8;
                                                                                        key8[7] = key7[6];
                                                                                    }
                                                                                    else                // b0 b1 b2 b3 b4 b5 b6 b
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
                                                                                deliveries[index] = delivery8;
                                                                            }
                                                                            else if (delivery8.Cost < dictDelivery.Cost)
                                                                            {
                                                                                deliveries[index] = delivery8;
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
            catch (Exception ex)
            {
            #if debug
                Logger.WriteToLog(373, $"RouteBuilder. startIndex = {context.StartOrderIndex}. rc = {rc}. order_count = {context.OrderCount}, shop_id = {context.ShopFrom.Id}, courier_id = {context.ShopCourier.Id}, level = {context.MaxRouteLength} Exception = {ex.Message}", 2);
            #endif
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

        /// <summary>
        /// Построение всех возможных отгрузок для
        /// заданного контекста и гео-данных
        /// </summary>
        /// <param name="status">Расширенный контекст</param>
        /// <returns>0 - отгрузки построены; иначе - отгрузки не построены</returns>
        public unsafe static void BuildEx(object status)
        {
            // 1. Инициализация
#if (debug)
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            DateTime startTime = DateTime.Now;
#endif
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
#if (debug)
                //Thread.Sleep(context.ShopCourier.VehicleID * 100 + 10 * context.StartOrderIndex);
                Logger.WriteToLog(309, $"BuildEx. vehicleID = {context.ShopCourier.VehicleID}. order_count = {context.OrderCount}. startIndex = {context.StartOrderIndex}. step = {context.OrderIndexStep}", 0);
#endif
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
                //CourierDeliveryInfo delivery;
                CourierDeliveryInfo delivery1;
                CourierDeliveryInfo delivery2;
                CourierDeliveryInfo delivery3;
                CourierDeliveryInfo delivery4;
                CourierDeliveryInfo delivery5;
                CourierDeliveryInfo delivery6;
                CourierDeliveryInfo delivery7;
                CourierDeliveryInfo delivery8;

                //byte[] key1 = new byte[8];
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
                double handInTime = contextCourier.HandInTime;
                DateTime t1;
                DateTime t2;
                double dtx;
                Order order;

                // level 1
                for (int i1 = startIndex; i1 < orderCount; i1+=step)
                {
                    orderGeoIndex[0] = i1;
                    orderGeoIndex[1] = shopIndex;
                    orders[0] = contextOrders[i1];
                    rcFind = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 1, isLoop, geoData, out delivery1);

                    //#if debug
                    //    Logger.WriteToLog(501, $"RouteBuilder.Build. rcFind = {rcFind}, i1 = {i1}, order_id = {orders[0].Id}", 0);
                    //#endif
                    if (rcFind != 0)
                        continue;

                    deliveries[i1] = delivery1;
                    b1 = (byte)i1;
                    //key1[0] = b1;
                    selectedOrders[i1] = true;

                    // level 2
                    if (level >= 2)
                    {
                        for (int i2 = 0; i2 < orderCount; i2++)
                        {
                            if (i2 == i1)
                                continue;

                            order = contextOrders[i2];

                            dtx = delivery1.NodeDeliveryTime[1] + geoData[i1, i2].Y / 60.0 + handInTime;
                            t1 = order.DeliveryTimeFrom.AddMinutes(-dtx);
                            t2 = order.DeliveryTimeTo.AddMinutes(-dtx);
                            if (delivery1.StartDeliveryInterval > t1)
                                t1 = delivery1.StartDeliveryInterval;
                            if (delivery1.EndDeliveryInterval < t2)
                                t2 = delivery1.EndDeliveryInterval;
                            if (t1 > t2)
                                continue;

                            orderGeoIndex[1] = i2;
                            orderGeoIndex[2] = shopIndex;
                            //orders[1] = contextOrders[i2];
                            orders[1] = order;
                            //rcFind = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 2, isLoop, geoData, out delivery2);
                            //if (rcFind != 0)
                            //    continue;

                            rcFind = contextCourier.DeliveryCheckEx(delivery1, order, geoData[i1, i2], geoData[i2, shopIndex], t1, t2, out delivery2);
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
                                deliveries[index] = delivery2;
                            }
                            else if (delivery2.Cost < dictDelivery.Cost)
                            {
                                deliveries[index] = delivery2;
                            }

                            // level 3
                            if (level >= 3)
                            {
                                for (int i3 = 0; i3 < orderCount; i3++)
                                {
                                    if (selectedOrders[i3])
                                        continue;

                                    order = contextOrders[i3];

                                    dtx = delivery2.NodeDeliveryTime[2] + geoData[i2, i3].Y / 60.0 + handInTime;
                                    t1 = order.DeliveryTimeFrom.AddMinutes(-dtx);
                                    t2 = order.DeliveryTimeTo.AddMinutes(-dtx);
                                    if (delivery2.StartDeliveryInterval > t1)
                                        t1 = delivery2.StartDeliveryInterval;
                                    if (delivery2.EndDeliveryInterval < t2)
                                        t2 = delivery2.EndDeliveryInterval;
                                    if (t1 > t2)
                                        continue;

                                    orderGeoIndex[2] = i3;
                                    orderGeoIndex[3] = shopIndex;
                                    //orders[2] = contextOrders[i3];
                                    orders[2] = order;
                                    //rcFind = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 3, isLoop, geoData, out delivery3);
                                    //if (rcFind != 0)
                                    //    continue;
                                    rcFind = contextCourier.DeliveryCheckEx(delivery2, order, geoData[i2, i3], geoData[i3, shopIndex], t1, t2, out delivery3);
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
                                    else if (b3 < key2[1])
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
                                        deliveries[index] = delivery3;
                                    }
                                    else if (delivery3.Cost < dictDelivery.Cost)
                                    {
                                        deliveries[index] = delivery3;
                                    }

                                    // level 4
                                    if (level >= 4)
                                    {
                                        for (int i4 = 0; i4 < orderCount; i4++)
                                        {
                                            if (selectedOrders[i4])
                                                continue;

                                            order = contextOrders[i4];

                                            dtx = delivery3.NodeDeliveryTime[3] + geoData[i3, i4].Y / 60.0 + handInTime;
                                            t1 = order.DeliveryTimeFrom.AddMinutes(-dtx);
                                            t2 = order.DeliveryTimeTo.AddMinutes(-dtx);
                                            if (delivery3.StartDeliveryInterval > t1)
                                                t1 = delivery3.StartDeliveryInterval;
                                            if (delivery3.EndDeliveryInterval < t2)
                                                t2 = delivery3.EndDeliveryInterval;
                                            if (t1 > t2)
                                                continue;

                                            orderGeoIndex[3] = i4;
                                            orderGeoIndex[4] = shopIndex;
                                            //orders[3] = contextOrders[i4];
                                            orders[3] = order;
                                            //rcFind = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 4, isLoop, geoData, out delivery4);
                                            //if (rcFind != 0)
                                            //    continue;
                                            rcFind = contextCourier.DeliveryCheckEx(delivery3, order, geoData[i3, i4], geoData[i4, shopIndex], t1, t2, out delivery4);
                                            if (rcFind != 0)
                                                continue;

                                            b4 = (byte)i4;
                                            selectedOrders[i4] = true;

                                            // строим ключ
                                            // b0 b1 b2
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
                                                if (b4 < key3[2])
                                                {
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
                                                deliveries[index] = delivery4;
                                            }
                                            else if (delivery4.Cost < dictDelivery.Cost)
                                            {
                                                deliveries[index] = delivery4;
                                            }

                                            // level 5
                                            if (level >= 5)
                                            {
                                                for (int i5 = 0; i5 < orderCount; i5++)
                                                {
                                                    if (selectedOrders[i5])
                                                        continue;

                                                    order =  contextOrders[i5];

                                                    dtx = delivery4.NodeDeliveryTime[4] + geoData[i4, i5].Y / 60.0 + handInTime;
                                                    t1 = order.DeliveryTimeFrom.AddMinutes(-dtx);
                                                    t2 = order.DeliveryTimeTo.AddMinutes(-dtx);
                                                    if (delivery4.StartDeliveryInterval > t1)
                                                        t1 = delivery4.StartDeliveryInterval;
                                                    if (delivery4.EndDeliveryInterval < t2)
                                                        t2 = delivery4.EndDeliveryInterval;
                                                    if (t1 > t2)
                                                        continue;

                                                    orderGeoIndex[4] = i5;
                                                    orderGeoIndex[5] = shopIndex;
                                                    //orders[4] = contextOrders[i5];
                                                    orders[4] = order;
                                                    //rcFind = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 5, isLoop, geoData, out delivery5);
                                                    //if (rcFind != 0)
                                                    //    continue;
                                                    rcFind = contextCourier.DeliveryCheckEx(delivery4, order, geoData[i4, i5], geoData[i5, shopIndex], t1, t2, out delivery5);
                                                    if (rcFind != 0)
                                                        continue;

                                                    b5 = (byte) i5;
                                                    selectedOrders[i5] = true;

                                                    // строим ключ
                                                    // b0 b1 b2 b3
                                                    if (b5 < key4[2])
                                                    {
                                                        if (b5 <= key4[0])      // b5 b0 b1 b2 b3
                                                        {
                                                            key5[0] = b5;
                                                            Buffer.BlockCopy(key4, 0, key5, 1, 4);
                                                        }
                                                        else if (b5 <= key4[1]) // b0 b5 b1 b2 b3
                                                        {
                                                            key5[0] = key4[0];
                                                            key5[1] = b5;
                                                            Buffer.BlockCopy(key4, 1, key5, 2, 3);
                                                        }
                                                        else                    // b0 b1 b5 b2 b3
                                                        {
                                                            Buffer.BlockCopy(key4, 0, key5, 0, 2);
                                                            key5[2] = b5;
                                                            Buffer.BlockCopy(key4, 2, key5, 3, 2);
                                                        }
                                                    }
                                                    else
                                                    {
                                                        if (b5 < key4[3])      // b0 b1 b2 b5 b3
                                                        {
                                                            Buffer.BlockCopy(key4, 0, key5, 0, 3);
                                                            key5[3] = b5;
                                                            key5[4] = key4[3];
                                                        }
                                                        else                   // b0 b1 b2 b3 b5
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
                                                        deliveries[index] = delivery5;
                                                    }
                                                    else if (delivery5.Cost < dictDelivery.Cost)
                                                    {
                                                        deliveries[index] = delivery5;
                                                    }

                                                    // level 6
                                                    if (level >= 6)
                                                    {
                                                        for (int i6 = 0; i6 < orderCount; i6++)
                                                        {
                                                            if (selectedOrders[i6])
                                                                continue;

                                                            order =  contextOrders[i6];

                                                            dtx = delivery4.NodeDeliveryTime[5] + geoData[i5, i6].Y / 60.0 + handInTime;
                                                            t1 = order.DeliveryTimeFrom.AddMinutes(-dtx);
                                                            t2 = order.DeliveryTimeTo.AddMinutes(-dtx);
                                                            if (delivery5.StartDeliveryInterval > t1)
                                                                t1 = delivery5.StartDeliveryInterval;
                                                            if (delivery5.EndDeliveryInterval < t2)
                                                                t2 = delivery5.EndDeliveryInterval;
                                                            if (t1 > t2)
                                                                continue;


                                                            orderGeoIndex[5] = i6;
                                                            orderGeoIndex[6] = shopIndex;
                                                            //orders[5] = contextOrders[i6];
                                                            orders[5] = order;
                                                            //rcFind = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 6, isLoop, geoData, out delivery6);
                                                            //if (rcFind != 0)
                                                            //    continue;

                                                            rcFind = contextCourier.DeliveryCheckEx(delivery5, order, geoData[i5, i6], geoData[i6, shopIndex], t1, t2, out delivery6);
                                                            if (rcFind != 0)
                                                                continue;

                                                            b6 = (byte) i6;
                                                            selectedOrders[i6] = true;

                                                            // строим ключ
                                                            // b0 b1 b2 b3 b4
                                                            if (b6 < key5[2])
                                                            {
                                                                if (b6 <= key5[0])      // b6 b0 b1 b2 b3 b4
                                                                {
                                                                    key6[0] = b6;
                                                                    Buffer.BlockCopy(key5, 0, key6, 1, 5);
                                                                }
                                                                else if (b6 < key5[1])  // b0 b6 b1 b2 b3 b4
                                                                {
                                                                    key6[0] = key5[0];
                                                                    key6[1] = b6;
                                                                    Buffer.BlockCopy(key5, 1, key6, 2, 4);
                                                                }
                                                                else                    // b0 b1 b6 b2 b3 b4
                                                                {
                                                                    Buffer.BlockCopy(key5, 0, key6, 0, 2);
                                                                    key6[2] = b6;
                                                                    Buffer.BlockCopy(key5, 2, key6, 3, 3);
                                                                }
                                                            }
                                                            else
                                                            {
                                                                if (b6 <= key5[3])      // b0 b1 b2 b6 b3 b4
                                                                {
                                                                    Buffer.BlockCopy(key5, 0, key6, 0, 3);
                                                                    key6[3] = b6;
                                                                    Buffer.BlockCopy(key5, 3, key6, 4, 2);
                                                                }
                                                                else if (b6 < key5[4])  // b0 b1 b2 b3 b6 b4
                                                                {
                                                                    Buffer.BlockCopy(key5, 0, key6, 0, 4);
                                                                    key6[4] = b6;
                                                                    key6[5] = key5[4];
                                                                }
                                                                else                    // b0 b1 b2 b3 b4 b6
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
                                                                deliveries[index] = delivery6;
                                                            }
                                                            else if (delivery6.Cost < dictDelivery.Cost)
                                                            {
                                                                deliveries[index] = delivery6;
                                                            }

                                                            // level 7
                                                            if (level >= 7)
                                                            {
                                                                for (int i7 = 0; i7 < orderCount; i7++)
                                                                {
                                                                    if (selectedOrders[i7])
                                                                        continue;

                                                                    order = contextOrders[i7];

                                                                    dtx = delivery6.NodeDeliveryTime[6] + geoData[i6, i7].Y / 60.0 + handInTime;
                                                                    t1 = order.DeliveryTimeFrom.AddMinutes(-dtx);
                                                                    t2 = order.DeliveryTimeTo.AddMinutes(-dtx);
                                                                    if (delivery6.StartDeliveryInterval > t1)
                                                                        t1 = delivery6.StartDeliveryInterval;
                                                                    if (delivery6.EndDeliveryInterval < t2)
                                                                        t2 = delivery6.EndDeliveryInterval;
                                                                    if (t1 > t2)
                                                                        continue;

                                                                    orderGeoIndex[6] = i7;
                                                                    orderGeoIndex[7] = shopIndex;
                                                                    //orders[6] = contextOrders[i7];
                                                                    orders[6] = order;
                                                                    //rcFind = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 7, isLoop, geoData, out delivery7);
                                                                    //if (rcFind != 0)
                                                                    //    continue;
                                                                    rcFind = contextCourier.DeliveryCheckEx(delivery6, order, geoData[i6, i7], geoData[i7, shopIndex], t1, t2, out delivery7);
                                                                    if (rcFind != 0)
                                                                        continue;

                                                                    b7 = (byte) i7;
                                                                    selectedOrders[i7] = true;

                                                                    // строим ключ
                                                                    // b0 b1 b2 b3 b4 b5

                                                                    if (b7 < key6[2])
                                                                    {
                                                                        if (b7 <= key6[0])      // b7 b0 b1 b2 b3 b4 b5
                                                                        {
                                                                            key7[0] = b7;
                                                                            Buffer.BlockCopy(key6, 0, key7, 1, 6);
                                                                        }
                                                                        else if (b7 < key6[1])  // b0 b7 b1 b2 b3 b4 b5
                                                                        {
                                                                            key7[0] = key6[0];
                                                                            key7[1] = b7;
                                                                            Buffer.BlockCopy(key6, 1, key7, 2, 5);
                                                                        }
                                                                        else                    // b0 b1 b7 b2 b3 b4 b5
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
                                                                            if (b7 <= key6[3])  // b0 b1 b2 b7 b3 b4 b5
                                                                            {
                                                                                Buffer.BlockCopy(key6, 0, key7, 0, 3);
                                                                                key7[3] = b7;
                                                                                Buffer.BlockCopy(key6, 3, key7, 4, 3);
                                                                            }
                                                                            else                // b0 b1 b2 b3 b7 b4 b5
                                                                            {
                                                                                Buffer.BlockCopy(key6, 0, key7, 0, 4);
                                                                                key7[4] = b7;
                                                                                Buffer.BlockCopy(key6, 4, key7, 5, 2);
                                                                            }
                                                                        }
                                                                        else
                                                                        {
                                                                            if (b7 < key6[5])   // b0 b1 b2 b3 b4 b7 b5
                                                                            {
                                                                                Buffer.BlockCopy(key6, 0, key7, 0, 5);
                                                                                key7[5] = b7;
                                                                                key7[6] = key6[5];
                                                                            }
                                                                            else                // b0 b1 b2 b3 b4 b5 b7
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
                                                                        deliveries[index] = delivery7;
                                                                    }
                                                                    else if (delivery7.Cost < dictDelivery.Cost)
                                                                    {
                                                                        deliveries[index] = delivery7;
                                                                    }

                                                                    // level 8
                                                                    if (level >= 8)
                                                                    {
                                                                        for (int i8 = 0; i8 < orderCount; i8++)
                                                                        {
                                                                            if (selectedOrders[i8])
                                                                                continue;

                                                                            order = contextOrders[i8];

                                                                            dtx = delivery7.NodeDeliveryTime[7] + geoData[i7, i8].Y / 60.0 + handInTime;
                                                                            t1 = order.DeliveryTimeFrom.AddMinutes(-dtx);
                                                                            t2 = order.DeliveryTimeTo.AddMinutes(-dtx);
                                                                            if (delivery7.StartDeliveryInterval > t1)
                                                                                t1 = delivery7.StartDeliveryInterval;
                                                                            if (delivery7.EndDeliveryInterval < t2)
                                                                                t2 = delivery7.EndDeliveryInterval;
                                                                            if (t1 > t2)
                                                                                continue;

                                                                            orderGeoIndex[7] = i8;
                                                                            orderGeoIndex[8] = shopIndex;
                                                                            //orders[7] = contextOrders[i8];
                                                                            orders[7] = order;
                                                                            //rcFind = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 8, isLoop, geoData, out delivery8);
                                                                            //if (rcFind != 0)
                                                                            //    continue;
                                                                            rcFind = contextCourier.DeliveryCheckEx(delivery7, order, geoData[i7, i8], geoData[i8, shopIndex], t1, t2, out delivery8);
                                                                            if (rcFind != 0)
                                                                                continue;

                                                                            b8 = (byte) i8;
                                                                            selectedOrders[i8] = true;

                                                                            // строим ключ
                                                                            // b0 b1 b2 b3 b4 b5 b6
                                                                            if (b8 < key7[3])
                                                                            {
                                                                                if (b8 < key7[1])
                                                                                {
                                                                                    if (b8 <= key7[0])  // b8 b0 b1 b2 b3 b4 b5 b6
                                                                                    {
                                                                                        key8[0] = b8;
                                                                                        Buffer.BlockCopy(key7, 0, key8, 1, 7);
                                                                                    }
                                                                                    else                // b0 b8 b1 b2 b3 b4 b5 b6
                                                                                    {
                                                                                        key8[0] = key7[0];
                                                                                        key8[1] = b8;
                                                                                        Buffer.BlockCopy(key7, 1, key8, 2, 6);
                                                                                    }
                                                                                }
                                                                                else
                                                                                {
                                                                                    if (b8 <= key7[2])  // b0 b1 b8 b2 b3 b4 b5 b6
                                                                                    {
                                                                                        Buffer.BlockCopy(key7, 0, key8, 0, 2);
                                                                                        key8[2] = b8;
                                                                                        Buffer.BlockCopy(key7, 2, key8, 3, 5);
                                                                                    }
                                                                                    else                // b0 b1 b2 b8 b3 b4 b5 b6
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
                                                                                    if (b8 <= key7[4])  // b0 b1 b2 b3 b8 b4 b5 b6
                                                                                    {
                                                                                        Buffer.BlockCopy(key7, 0, key8, 0, 4);
                                                                                        key8[4] = b8;
                                                                                        Buffer.BlockCopy(key7, 4, key8, 5, 3);
                                                                                    }
                                                                                    else                // b0 b1 b2 b3 b4 b8 b5 b6
                                                                                    {
                                                                                        Buffer.BlockCopy(key7, 0, key8, 0, 5);
                                                                                        key8[5] = b8;
                                                                                        Buffer.BlockCopy(key7, 5, key8, 6, 2);
                                                                                    }
                                                                                }
                                                                                else
                                                                                {
                                                                                    if (b8 < key7[6])   // b0 b1 b2 b3 b4 b5 b8 b6
                                                                                    {
                                                                                        Buffer.BlockCopy(key7, 0, key8, 0, 6);
                                                                                        key8[6] = b8;
                                                                                        key8[7] = key7[6];
                                                                                    }
                                                                                    else                // b0 b1 b2 b3 b4 b5 b6 b
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
                                                                                deliveries[index] = delivery8;
                                                                            }
                                                                            else if (delivery8.Cost < dictDelivery.Cost)
                                                                            {
                                                                                deliveries[index] = delivery8;
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
            catch (Exception ex)
            {
            #if debug
                Logger.WriteToLog(373, $"RouteBuilder. startIndex = {context.StartOrderIndex}. rc = {rc}. order_count = {context.OrderCount}, shop_id = {context.ShopFrom.Id}, courier_id = {context.ShopCourier.Id}, level = {context.MaxRouteLength} Exception = {ex.Message}", 2);
            #endif
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
            #if debug
                    sw.Stop();
                    //Logger.WriteToLog(3090, $"BuildEx exit. vehicleID = {context.ShopCourier.VehicleID}. startIndex = {context.StartOrderIndex}. step = {context.OrderIndexStep}", 0);
                    Logger.WriteToLog(3090, $"BuildEx exit ({startTime.ToString("yyyy-MM-dd HH:mm:ss.fff")} ET = {sw.ElapsedMilliseconds}). vehicleID = {context.ShopCourier.VehicleID}. startIndex = {context.StartOrderIndex}. step = {context.OrderIndexStep}", 0);
            #endif
                }
            }
        }

        // Предельные значения числа заказов для разных длин маршрутов
        //    level orders   capacity
        //      8    30       8656936 20
        //      7    35       8731847 25
        //      6    44       8295045 30
        //      5    64       8303632 35
        //      4    119      8221710 120
        //      3    365      8104825 365
        //      2    4096     8390656 4096
        //      1    8000000  8000000 8000000

        // Предельные значения числа заказов для разных длин маршрутов
        //    level orders        keys         clouds       keys
        //      8    14          12 911          48         214 038 000
        //      7    17          41 226          51         206 417 400    
        //      6    24          190 051         56         207 886 140
        //      5    40          760 099         65         206 497 200
        //      4    96          3 469 497       96
        //      3    432         13 437 289      432
        //      2    9 000       40 504 501      9000
        //      1    80 000 000  80 000 000      80000000


        /// <summary>
        /// Построение всех возможных отгрузок длины 2 для
        /// заданного контекста и гео-данных
        /// </summary>
        /// <param name="status">Расширенный контекст</param>
        /// <returns>0 - отгрузки построены; иначе - отгрузки не построены</returns>
        public static void BuildEx2(object status)
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
#if (debug)
                //Thread.Sleep(context.ShopCourier.VehicleID * 100 + 10 * context.StartOrderIndex);
                Logger.WriteToLog(309, $"BuildEx2 enter. vehicleID = {context.ShopCourier.VehicleID}. order_count = {context.OrderCount}, level = {context.MaxRouteLength}, startIndex = {context.StartOrderIndex}, step = {context.OrderIndexStep}", 0);
#endif
                // 3. Извлекаем и проверяем данные из контекста
                rc = 3;
                int level = context.MaxRouteLength;
                if (level < 1 || level > 2)
                    return;
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
                int startIndex = context.StartOrderIndex;
                if (startIndex < 0 || startIndex >= orderCount)
                    return;
                int step = context.OrderIndexStep;
                if (step <= 0)
                    return;
                CourierDeliveryInfo[] deliveries;
                if (orderCount >= 2)
                {
                    deliveries = new CourierDeliveryInfo[
                        orderCount +
                        (orderCount - 1) * orderCount / 2];
                }
                else
                {
                    deliveries = new CourierDeliveryInfo[1];
                }
                int count = 0;

                // 4. Цикл выбора допустимых маршрутов
                rc = 4;
                Order[] orders = new Order[2];
                int[] orderGeoIndex = new int[3];
                bool isLoop = !contextCourier.IsTaxi;
                int shopIndex = orderCount;
                CourierDeliveryInfo delivery;
                CourierDeliveryInfo delivery1;
                CourierDeliveryInfo delivery2;

                for (int i1 = startIndex; i1 < orderCount; i1 += step)
                {
                    orderGeoIndex[0] = i1;
                    orderGeoIndex[1] = shopIndex;
                    orders[0] = contextOrders[i1];
                    int rcFind = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 1, isLoop, geoData, out delivery);

                    if (rcFind == 0)
                    {
                        deliveries[count++] = delivery;
                        orderGeoIndex[2] = shopIndex;

                        for (int i2 = i1 + 1; i2 < orderCount; i2++)
                        {
                            orderGeoIndex[0] = i1;
                            orderGeoIndex[1] = i2;
                            orders[0] = contextOrders[i1];
                            orders[1] = contextOrders[i2];

                            int rcFind1 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 2, isLoop, geoData, out delivery1);

                            orderGeoIndex[0] = i2;
                            orderGeoIndex[1] = i1;
                            orders[0] = contextOrders[i2];
                            orders[1] = contextOrders[i1];
                            int rcFind2 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 2, isLoop, geoData, out delivery2);

                            if (rcFind1 == 0)
                            {
                                if (rcFind2 == 0)
                                { deliveries[count++] = (delivery1.Cost <= delivery2.Cost ? delivery1 : delivery2); }
                                else
                                { deliveries[count++] = delivery1; }
                            }
                            else if (rcFind2 == 0)
                            { deliveries[count++] = delivery2; }
                        }
                    }
                    else
                    {
                        orderGeoIndex[1] = i1;
                        orderGeoIndex[2] = shopIndex;
                        orders[1] = contextOrders[i1];

                        for (int i2 = i1 + 1; i2 < orderCount; i2++)
                        {
                            orderGeoIndex[0] = i2;
                            orders[0] = contextOrders[i2];

                            int rcFind1 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 2, isLoop, geoData, out delivery1);
                            if (rcFind1 == 0)
                            { deliveries[count++] = delivery1; }
                        }
                    }
                }

                if (count < deliveries.Length)
                { Array.Resize(ref deliveries, count); }

                context.Deliveries = deliveries;

                // 6. Выход - Ok
                rc = 0;
                return;
            }
            catch (Exception ex)
            {
#if debug
                Logger.WriteToLog(373, $"BuildEx2. startIndex = {context.StartOrderIndex}. rc = {rc}. order_count = {context.OrderCount}, shop_id = {context.ShopFrom.Id}, courier_id = {context.ShopCourier.Id}, level = {context.MaxRouteLength} Exception = {ex.Message}", 2);
#endif
                return;
            }
            finally
            {
                if (context != null)
                {
#if debug
                    Logger.WriteToLog(3090, $"BuildEx2 exit. vehicleID = {context.ShopCourier.VehicleID}. order_count = {context.OrderCount}, level = {context.MaxRouteLength}, startIndex = {context.StartOrderIndex}, step = {context.OrderIndexStep}", 0);
#endif
                    context.ExitCode = rc;
                    ManualResetEvent syncEvent = context.SyncEvent;
                    if (syncEvent != null)
                    {
                        syncEvent.Set();
                    }
                }
            }
        }

        /// <summary>
        /// Построение всех возможных отгрузок длины 3 для
        /// заданного контекста и гео-данных
        /// </summary>
        /// <param name="status">Расширенный контекст</param>
        /// <returns>0 - отгрузки построены; иначе - отгрузки не построены</returns>
        public static void BuildEx3(object status)
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
#if (debug)
                //Thread.Sleep(context.ShopCourier.VehicleID * 100 + 10 * context.StartOrderIndex);
                Logger.WriteToLog(309, $"BuildEx3 enter. vehicleID = {context.ShopCourier.VehicleID}. order_count = {context.OrderCount}, level = {context.MaxRouteLength}, startIndex = {context.StartOrderIndex}, step = {context.OrderIndexStep}", 0);
#endif
                // 3. Извлекаем и проверяем данные из контекста
                rc = 3;
                int level = context.MaxRouteLength;
                if (level != 3)
                    return;
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
                //long[] deliveryKeys = context.DeliveryKeys;
                //if (deliveryKeys == null || deliveryKeys.Length <= 0)
                //    return;
                int startIndex = context.StartOrderIndex;
                if (startIndex < 0 || startIndex >= orderCount)
                    return;
                int step = context.OrderIndexStep;
                if (step <= 0)
                    return;
                CourierDeliveryInfo[] deliveries;
                if (orderCount >= 3)
                {
                    deliveries = new CourierDeliveryInfo[
                        orderCount +
                        (orderCount - 1) * orderCount / 2 +
                        (orderCount - 2) * (orderCount - 1) * orderCount / 6];
                }
                else if (orderCount >= 2)
                {
                    deliveries = new CourierDeliveryInfo[
                        orderCount +
                        (orderCount - 1) * orderCount / 2];
                }
                else
                {
                    deliveries = new CourierDeliveryInfo[1];
                }
                int count = 0;

                // 4. Цикл выбора допустимых маршрутов
                rc = 4;
                Order[] orders = new Order[3];
                int[] orderGeoIndex = new int[4];
                bool isLoop = !contextCourier.IsTaxi;
                int shopIndex = orderCount;
                CourierDeliveryInfo delivery;
                CourierDeliveryInfo delivery1;
                CourierDeliveryInfo delivery2;
                CourierDeliveryInfo delivery3;

                for (int i1 = startIndex; i1 < orderCount; i1 += step)
                {
                    orderGeoIndex[0] = i1;
                    orderGeoIndex[1] = shopIndex;
                    orders[0] = contextOrders[i1];
                    int rcFind = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 1, isLoop, geoData, out delivery);

                    if (rcFind == 0)
                    {
                        deliveries[count++] = delivery;

                        for (int i2 = i1 + 1; i2 < orderCount; i2++)
                        {
                            orderGeoIndex[0] = i1;
                            orderGeoIndex[1] = i2;
                            orderGeoIndex[2] = shopIndex;
                            orders[0] = contextOrders[i1];
                            orders[1] = contextOrders[i2];

                            int rcFind1 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 2, isLoop, geoData, out delivery1);

                            orderGeoIndex[0] = i2;
                            orderGeoIndex[1] = i1;
                            orders[0] = contextOrders[i2];
                            orders[1] = contextOrders[i1];
                            int rcFind2 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 2, isLoop, geoData, out delivery2);

                            if (rcFind1 == 0)
                            {
                                if (rcFind2 == 0)
                                { deliveries[count++] = (delivery1.Cost <= delivery2.Cost ? delivery1 : delivery2); }
                                else
                                { deliveries[count++] = delivery1; }
                            }
                            else if (rcFind2 == 0)
                            { deliveries[count++] = delivery2; }


                            orderGeoIndex[3] = shopIndex;
                            for (int i3 = i2 + 1; i3 < orderCount; i3++)
                            {
                                delivery = null;

                                // 1 2 3
                                orderGeoIndex[0] = i1;
                                orderGeoIndex[1] = i2;
                                orderGeoIndex[2] = i3;
                                orders[0] = contextOrders[i1];
                                orders[1] = contextOrders[i2];
                                orders[2] = contextOrders[i3];

                                int rcFind3 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 3, isLoop, geoData, out delivery3);
                                if (rcFind3 == 0)
                                { delivery = delivery3; }

                                // 1 3 2
                                orderGeoIndex[1] = i3;
                                orderGeoIndex[2] = i2;
                                orders[0] = contextOrders[i1];
                                orders[1] = contextOrders[i3];
                                orders[2] = contextOrders[i2];
                                rcFind3 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 3, isLoop, geoData, out delivery3);
                                if (rcFind3 == 0 && (delivery == null || delivery3.Cost < delivery.Cost))
                                { delivery = delivery3; }

                                // 2 1 3
                                orderGeoIndex[0] = i2;
                                orderGeoIndex[1] = i1;
                                orderGeoIndex[2] = i3;
                                orders[0] = contextOrders[i2];
                                orders[1] = contextOrders[i1];
                                orders[2] = contextOrders[i3];
                                rcFind3 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 3, isLoop, geoData, out delivery3);
                                if (rcFind3 == 0 && (delivery == null || delivery3.Cost < delivery.Cost))
                                { delivery = delivery3; }

                                // 2 3 1
                                orderGeoIndex[1] = i3;
                                orderGeoIndex[2] = i1;
                                orders[0] = contextOrders[i2];
                                orders[1] = contextOrders[i3];
                                orders[2] = contextOrders[i1];
                                rcFind3 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 3, isLoop, geoData, out delivery3);
                                if (rcFind3 == 0 && (delivery == null || delivery3.Cost < delivery.Cost))
                                { delivery = delivery3; }

                                // 3 1 2
                                orderGeoIndex[0] = i3;
                                orderGeoIndex[1] = i1;
                                orderGeoIndex[2] = i2;
                                orders[0] = contextOrders[i3];
                                orders[1] = contextOrders[i1];
                                orders[2] = contextOrders[i2];
                                rcFind3 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 3, isLoop, geoData, out delivery3);
                                if (rcFind3 == 0 && (delivery == null || delivery3.Cost < delivery.Cost))
                                { delivery = delivery3; }

                                // 3 2 1
                                orderGeoIndex[1] = i2;
                                orderGeoIndex[2] = i1;
                                orders[0] = contextOrders[i3];
                                orders[1] = contextOrders[i2];
                                orders[2] = contextOrders[i1];
                                rcFind3 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 3, isLoop, geoData, out delivery3);
                                if (rcFind3 == 0 && (delivery == null || delivery3.Cost < delivery.Cost))
                                { delivery = delivery3; }

                                if (delivery != null)
                                { deliveries[count++] = delivery; }
                            }
                        }
                    }
                    else
                    {
                        for (int i2 = i1 + 1; i2 < orderCount; i2++)
                        {
                            orderGeoIndex[0] = i2;
                            orderGeoIndex[1] = i1;
                            orderGeoIndex[2] = shopIndex;

                            int rcFind1 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 2, isLoop, geoData, out delivery1);
                            if (rcFind1 == 0)
                            { deliveries[count++] = delivery1; }

                            orderGeoIndex[3] = shopIndex;
                            for (int i3 = i2 + 1; i3 < orderCount; i3++)
                            {
                                delivery = null;

                                // 2 1 3
                                orderGeoIndex[0] = i2;
                                orderGeoIndex[1] = i1;
                                orderGeoIndex[2] = i3;
                                orders[0] = contextOrders[i2];
                                orders[1] = contextOrders[i1];
                                orders[2] = contextOrders[i3];
                                int rcFind3 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 3, isLoop, geoData, out delivery3);
                                if (rcFind3 == 0 && (delivery == null || delivery3.Cost < delivery.Cost))
                                { delivery = delivery3; }

                                // 2 3 1
                                orderGeoIndex[1] = i3;
                                orderGeoIndex[2] = i1;
                                orders[0] = contextOrders[i2];
                                orders[1] = contextOrders[i3];
                                orders[2] = contextOrders[i1];
                                rcFind3 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 3, isLoop, geoData, out delivery3);
                                if (rcFind3 == 0 && (delivery == null || delivery3.Cost < delivery.Cost))
                                { delivery = delivery3; }

                                // 3 1 2
                                orderGeoIndex[0] = i3;
                                orderGeoIndex[1] = i1;
                                orderGeoIndex[2] = i2;
                                orders[0] = contextOrders[i3];
                                orders[1] = contextOrders[i1];
                                orders[2] = contextOrders[i2];
                                rcFind3 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 3, isLoop, geoData, out delivery3);
                                if (rcFind3 == 0 && (delivery == null || delivery3.Cost < delivery.Cost))
                                { delivery = delivery3; }

                                // 3 2 1
                                orderGeoIndex[1] = i2;
                                orderGeoIndex[2] = i1;
                                orders[0] = contextOrders[i3];
                                orders[1] = contextOrders[i2];
                                orders[2] = contextOrders[i1];
                                rcFind3 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 3, isLoop, geoData, out delivery3);
                                if (rcFind3 == 0 && (delivery == null || delivery3.Cost < delivery.Cost))
                                { delivery = delivery3; }

                                if (delivery != null)
                                { deliveries[count++] = delivery; }
                            }
                        }
                    }
                }

                if (count < deliveries.Length)
                { Array.Resize(ref deliveries, count); }

                context.Deliveries = deliveries;

                // 6. Выход - Ok
                rc = 0;
                return;
            }
            catch (Exception ex)
            {
#if debug
                Logger.WriteToLog(373, $"BuildEx3. startIndex = {context.StartOrderIndex}. rc = {rc}. order_count = {context.OrderCount}, shop_id = {context.ShopFrom.Id}, courier_id = {context.ShopCourier.Id}, level = {context.MaxRouteLength} Exception = {ex.Message}", 2);
#endif
                return;
            }
            finally
            {
                if (context != null)
                {
#if debug
                    Logger.WriteToLog(3090, $"BuildEx3 exit. vehicleID = {context.ShopCourier.VehicleID}. order_count = {context.OrderCount}, level = {context.MaxRouteLength}, startIndex = {context.StartOrderIndex}, step = {context.OrderIndexStep}", 0);
#endif
                    context.ExitCode = rc;
                    ManualResetEvent syncEvent = context.SyncEvent;
                    if (syncEvent != null)
                    {
                        syncEvent.Set();
                    }
                }
            }
        }

        /// <summary>
        /// Построение всех возможных отгрузок длины 4 для
        /// заданного контекста и гео-данных
        /// </summary>
        /// <param name="status">Расширенный контекст</param>
        /// <returns>0 - отгрузки построены; иначе - отгрузки не построены</returns>
        public static void BuildEx4(object status)
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
#if (debug)
                Logger.WriteToLog(309, $"BuildEx4 enter. vehicleID = {context.ShopCourier.VehicleID}. order_count = {context.OrderCount}, level = {context.MaxRouteLength}, startIndex = {context.StartOrderIndex}, step = {context.OrderIndexStep}", 0);
#endif
                // 3. Извлекаем и проверяем данные из контекста
                rc = 3;
                int level = context.MaxRouteLength;
                if (level != 4)
                    return;
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
                //long[] deliveryKeys = context.DeliveryKeys;
                //if (deliveryKeys == null || deliveryKeys.Length <= 0)
                //    return;
                int startIndex = context.StartOrderIndex;
                if (startIndex < 0 || startIndex >= orderCount)
                    return;
                int step = context.OrderIndexStep;
                if (step <= 0)
                    return;
                CourierDeliveryInfo[] deliveries;
                if (orderCount >= 4)
                {
                    deliveries = new CourierDeliveryInfo[
                        orderCount + 
                        (orderCount - 1) * orderCount / 2 + 
                        (orderCount - 2) * (orderCount - 1) * orderCount / 6 +
                        (orderCount - 3) * (orderCount - 2) * (orderCount - 1) * orderCount / 24];
                }
                else if (orderCount >= 3)
                {
                    deliveries = new CourierDeliveryInfo[
                        orderCount + 
                        (orderCount - 1) * orderCount / 2 + 
                        (orderCount - 2) * (orderCount - 1) * orderCount / 6];
                }
                else if (orderCount >= 2)
                {
                    deliveries = new CourierDeliveryInfo[
                        orderCount + 
                        (orderCount - 1) * orderCount / 2];
                }
                else
                {
                    deliveries = new CourierDeliveryInfo[1];
                }
           
                int count = 0;

                // 4. Цикл выбора допустимых маршрутов
                rc = 4;
                Order[] orders = new Order[4];
                int[] orderGeoIndex = new int[5];
                bool isLoop = !contextCourier.IsTaxi;
                int shopIndex = orderCount;
                CourierDeliveryInfo delivery;
                CourierDeliveryInfo delivery1;
                CourierDeliveryInfo delivery2;
                CourierDeliveryInfo delivery3;
                CourierDeliveryInfo delivery4;

                for (int i1 = startIndex; i1 < orderCount; i1 += step)
                {
                    orderGeoIndex[0] = i1;
                    orderGeoIndex[1] = shopIndex;
                    orders[0] = contextOrders[i1];
                    int rcFind = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 1, isLoop, geoData, out delivery);

                    if (rcFind == 0)
                    {
                        deliveries[count++] = delivery;

                        for (int i2 = i1 + 1; i2 < orderCount; i2++)
                        {
                            orderGeoIndex[0] = i1;
                            orderGeoIndex[1] = i2;
                            orderGeoIndex[2] = shopIndex;
                            orders[0] = contextOrders[i1];
                            orders[1] = contextOrders[i2];
                            int rcFind1 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 2, isLoop, geoData, out delivery1);

                            orderGeoIndex[0] = i2;
                            orderGeoIndex[1] = i1;
                            orders[0] = contextOrders[i2];
                            orders[1] = contextOrders[i1];
                            int rcFind2 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 2, isLoop, geoData, out delivery2);

                            if (rcFind1 == 0)
                            {
                                if (rcFind2 == 0)
                                { deliveries[count++] = (delivery1.Cost <= delivery2.Cost ? delivery1 : delivery2); }
                                else
                                { deliveries[count++] = delivery1; }
                            }
                            else if (rcFind2 == 0)
                            { deliveries[count++] = delivery2; }


                            for (int i3 = i2 + 1; i3 < orderCount; i3++)
                            {
                                delivery = null;

                                // 1 2 3
                                orderGeoIndex[0] = i1;
                                orderGeoIndex[1] = i2;
                                orderGeoIndex[2] = i3;
                                orderGeoIndex[3] = shopIndex;
                                orders[0] = contextOrders[i1];
                                orders[1] = contextOrders[i2];
                                orders[2] = contextOrders[i3];
                                int rcFind3 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 3, isLoop, geoData, out delivery3);
                                if (rcFind3 == 0)
                                { delivery = delivery3; }

                                // 1 3 2
                                orderGeoIndex[1] = i3;
                                orderGeoIndex[2] = i2;
                                orders[0] = contextOrders[i1];
                                orders[1] = contextOrders[i3];
                                orders[2] = contextOrders[i2];
                                rcFind3 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 3, isLoop, geoData, out delivery3);
                                if (rcFind3 == 0 && (delivery == null || delivery3.Cost < delivery.Cost))
                                { delivery = delivery3; }

                                // 2 1 3
                                orderGeoIndex[0] = i2;
                                orderGeoIndex[1] = i1;
                                orderGeoIndex[2] = i3;
                                orders[0] = contextOrders[i2];
                                orders[1] = contextOrders[i1];
                                orders[2] = contextOrders[i3];
                                rcFind3 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 3, isLoop, geoData, out delivery3);
                                if (rcFind3 == 0 && (delivery == null || delivery3.Cost < delivery.Cost))
                                { delivery = delivery3; }

                                // 2 3 1
                                orderGeoIndex[1] = i3;
                                orderGeoIndex[2] = i1;
                                orders[0] = contextOrders[i2];
                                orders[1] = contextOrders[i3];
                                orders[2] = contextOrders[i1];
                                rcFind3 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 3, isLoop, geoData, out delivery3);
                                if (rcFind3 == 0 && (delivery == null || delivery3.Cost < delivery.Cost))
                                { delivery = delivery3; }

                                // 3 1 2
                                orderGeoIndex[0] = i3;
                                orderGeoIndex[1] = i1;
                                orderGeoIndex[2] = i2;
                                orders[0] = contextOrders[i3];
                                orders[1] = contextOrders[i1];
                                orders[2] = contextOrders[i2];
                                rcFind3 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 3, isLoop, geoData, out delivery3);
                                if (rcFind3 == 0 && (delivery == null || delivery3.Cost < delivery.Cost))
                                { delivery = delivery3; }

                                // 3 2 1
                                orderGeoIndex[1] = i2;
                                orderGeoIndex[2] = i1;
                                orders[0] = contextOrders[i3];
                                orders[1] = contextOrders[i2];
                                orders[2] = contextOrders[i1];
                                rcFind3 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 3, isLoop, geoData, out delivery3);
                                if (rcFind3 == 0 && (delivery == null || delivery3.Cost < delivery.Cost))
                                { delivery = delivery3; }

                                if (delivery != null)
                                { deliveries[count++] = delivery; }

                                orderGeoIndex[4] = shopIndex;

                                for (int i4 = i3 + 1; i4 < orderCount; i4++)
                                {
                                    delivery = null;

                                    // 1 2 3 4
                                    orderGeoIndex[0] = i1;
                                    orderGeoIndex[1] = i2;
                                    orderGeoIndex[2] = i3;
                                    orderGeoIndex[3] = i4;
                                    orders[0] = contextOrders[i1];
                                    orders[1] = contextOrders[i2];
                                    orders[2] = contextOrders[i3];
                                    orders[3] = contextOrders[i4];
                                    int rcFind4 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 4, isLoop, geoData, out delivery4);
                                    if (rcFind4 == 0)
                                    { delivery = delivery4; }

                                    // 1 2 4 3
                                    orderGeoIndex[0] = i1;
                                    orderGeoIndex[1] = i2;
                                    orderGeoIndex[2] = i4;
                                    orderGeoIndex[3] = i3;
                                    orders[0] = contextOrders[i1];
                                    orders[1] = contextOrders[i2];
                                    orders[2] = contextOrders[i4];
                                    orders[3] = contextOrders[i3];
                                    rcFind4 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 4, isLoop, geoData, out delivery4);
                                    if (rcFind4 == 0 && (delivery == null || delivery4.Cost < delivery.Cost))
                                    { delivery = delivery4; }
                                    
                                    // 1 3 2 4
                                    orderGeoIndex[0] = i1;
                                    orderGeoIndex[1] = i3;
                                    orderGeoIndex[2] = i2;
                                    orderGeoIndex[3] = i4;
                                    orders[0] = contextOrders[i1];
                                    orders[1] = contextOrders[i3];
                                    orders[2] = contextOrders[i2];
                                    orders[3] = contextOrders[i4];
                                    rcFind4 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 4, isLoop, geoData, out delivery4);
                                    if (rcFind4 == 0 && (delivery == null || delivery4.Cost < delivery.Cost))
                                    { delivery = delivery4; }
                                    
                                    // 1 3 4 2
                                    orderGeoIndex[0] = i1;
                                    orderGeoIndex[1] = i3;
                                    orderGeoIndex[2] = i4;
                                    orderGeoIndex[3] = i2;
                                    orders[0] = contextOrders[i1];
                                    orders[1] = contextOrders[i3];
                                    orders[2] = contextOrders[i4];
                                    orders[3] = contextOrders[i2];
                                    rcFind4 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 4, isLoop, geoData, out delivery4);
                                    if (rcFind4 == 0 && (delivery == null || delivery4.Cost < delivery.Cost))
                                    { delivery = delivery4; }
                                    
                                    // 1 4 2 3
                                    orderGeoIndex[0] = i1;
                                    orderGeoIndex[1] = i4;
                                    orderGeoIndex[2] = i2;
                                    orderGeoIndex[3] = i3;
                                    orders[0] = contextOrders[i1];
                                    orders[1] = contextOrders[i4];
                                    orders[2] = contextOrders[i2];
                                    orders[3] = contextOrders[i3];
                                    rcFind4 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 4, isLoop, geoData, out delivery4);
                                    if (rcFind4 == 0 && (delivery == null || delivery4.Cost < delivery.Cost))
                                    { delivery = delivery4; }

                                    // 1 4 3 2
                                    orderGeoIndex[0] = i1;
                                    orderGeoIndex[1] = i4;
                                    orderGeoIndex[2] = i3;
                                    orderGeoIndex[3] = i2;
                                    orders[0] = contextOrders[i1];
                                    orders[1] = contextOrders[i4];
                                    orders[2] = contextOrders[i3];
                                    orders[3] = contextOrders[i2];
                                    rcFind4 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 4, isLoop, geoData, out delivery4);
                                    if (rcFind4 == 0 && (delivery == null || delivery4.Cost < delivery.Cost))
                                    { delivery = delivery4; }
                                    
                                    // 2 1 3 4
                                    orderGeoIndex[0] = i2;
                                    orderGeoIndex[1] = i1;
                                    orderGeoIndex[2] = i3;
                                    orderGeoIndex[3] = i4;
                                    orders[0] = contextOrders[i2];
                                    orders[1] = contextOrders[i1];
                                    orders[2] = contextOrders[i3];
                                    orders[3] = contextOrders[i4];
                                    rcFind4 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 4, isLoop, geoData, out delivery4);
                                    if (rcFind4 == 0 && (delivery == null || delivery4.Cost < delivery.Cost))
                                    { delivery = delivery4; }
                                    
                                    // 2 1 4 3
                                    orderGeoIndex[0] = i2;
                                    orderGeoIndex[1] = i1;
                                    orderGeoIndex[2] = i4;
                                    orderGeoIndex[3] = i3;
                                    orders[0] = contextOrders[i2];
                                    orders[1] = contextOrders[i1];
                                    orders[2] = contextOrders[i4];
                                    orders[3] = contextOrders[i3];
                                    rcFind4 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 4, isLoop, geoData, out delivery4);
                                    if (rcFind4 == 0 && (delivery == null || delivery4.Cost < delivery.Cost))
                                    { delivery = delivery4; }
                                    
                                    // 2 3 1 4
                                    orderGeoIndex[0] = i2;
                                    orderGeoIndex[1] = i3;
                                    orderGeoIndex[2] = i1;
                                    orderGeoIndex[3] = i4;
                                    orders[0] = contextOrders[i2];
                                    orders[1] = contextOrders[i3];
                                    orders[2] = contextOrders[i1];
                                    orders[3] = contextOrders[i4];
                                    rcFind4 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 4, isLoop, geoData, out delivery4);
                                    if (rcFind4 == 0 && (delivery == null || delivery4.Cost < delivery.Cost))
                                    { delivery = delivery4; }
                                    
                                    // 2 3 4 1
                                    orderGeoIndex[0] = i2;
                                    orderGeoIndex[1] = i3;
                                    orderGeoIndex[2] = i4;
                                    orderGeoIndex[3] = i1;
                                    orders[0] = contextOrders[i2];
                                    orders[1] = contextOrders[i3];
                                    orders[2] = contextOrders[i4];
                                    orders[3] = contextOrders[i1];
                                    rcFind4 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 4, isLoop, geoData, out delivery4);
                                    if (rcFind4 == 0 && (delivery == null || delivery4.Cost < delivery.Cost))
                                    { delivery = delivery4; }
                                    
                                    // 2 4 1 3
                                    orderGeoIndex[0] = i2;
                                    orderGeoIndex[1] = i4;
                                    orderGeoIndex[2] = i1;
                                    orderGeoIndex[3] = i3;
                                    orders[0] = contextOrders[i2];
                                    orders[1] = contextOrders[i4];
                                    orders[2] = contextOrders[i1];
                                    orders[3] = contextOrders[i3];
                                    rcFind4 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 4, isLoop, geoData, out delivery4);
                                    if (rcFind4 == 0 && (delivery == null || delivery4.Cost < delivery.Cost))
                                    { delivery = delivery4; }
                                    
                                    // 2 4 3 1
                                    orderGeoIndex[0] = i2;
                                    orderGeoIndex[1] = i4;
                                    orderGeoIndex[2] = i3;
                                    orderGeoIndex[3] = i1;
                                    orders[0] = contextOrders[i2];
                                    orders[1] = contextOrders[i4];
                                    orders[2] = contextOrders[i3];
                                    orders[3] = contextOrders[i1];
                                    rcFind4 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 4, isLoop, geoData, out delivery4);
                                    if (rcFind4 == 0 && (delivery == null || delivery4.Cost < delivery.Cost))
                                    { delivery = delivery4; }
                                    
                                    // 3 1 2 4
                                    orderGeoIndex[0] = i3;
                                    orderGeoIndex[1] = i1;
                                    orderGeoIndex[2] = i2;
                                    orderGeoIndex[3] = i4;
                                    orders[0] = contextOrders[i3];
                                    orders[1] = contextOrders[i1];
                                    orders[2] = contextOrders[i2];
                                    orders[3] = contextOrders[i4];
                                    rcFind4 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 4, isLoop, geoData, out delivery4);
                                    if (rcFind4 == 0 && (delivery == null || delivery4.Cost < delivery.Cost))
                                    { delivery = delivery4; }
                                    
                                    // 3 1 4 2
                                    orderGeoIndex[0] = i3;
                                    orderGeoIndex[1] = i1;
                                    orderGeoIndex[2] = i4;
                                    orderGeoIndex[3] = i2;
                                    orders[0] = contextOrders[i3];
                                    orders[1] = contextOrders[i1];
                                    orders[2] = contextOrders[i4];
                                    orders[3] = contextOrders[i2];
                                    rcFind4 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 4, isLoop, geoData, out delivery4);
                                    if (rcFind4 == 0 && (delivery == null || delivery4.Cost < delivery.Cost))
                                    { delivery = delivery4; }
                                    
                                    // 3 2 1 4
                                    orderGeoIndex[0] = i3;
                                    orderGeoIndex[1] = i2;
                                    orderGeoIndex[2] = i1;
                                    orderGeoIndex[3] = i4;
                                    orders[0] = contextOrders[i3];
                                    orders[1] = contextOrders[i2];
                                    orders[2] = contextOrders[i1];
                                    orders[3] = contextOrders[i4];
                                    rcFind4 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 4, isLoop, geoData, out delivery4);
                                    if (rcFind4 == 0 && (delivery == null || delivery4.Cost < delivery.Cost))
                                    { delivery = delivery4; }
                                    
                                    // 3 2 4 1
                                    orderGeoIndex[0] = i3;
                                    orderGeoIndex[1] = i2;
                                    orderGeoIndex[2] = i4;
                                    orderGeoIndex[3] = i1;
                                    orders[0] = contextOrders[i3];
                                    orders[1] = contextOrders[i2];
                                    orders[2] = contextOrders[i4];
                                    orders[3] = contextOrders[i1];
                                    rcFind4 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 4, isLoop, geoData, out delivery4);
                                    if (rcFind4 == 0 && (delivery == null || delivery4.Cost < delivery.Cost))
                                    { delivery = delivery4; }
                                    
                                    // 3 4 1 2
                                    orderGeoIndex[0] = i3;
                                    orderGeoIndex[1] = i4;
                                    orderGeoIndex[2] = i1;
                                    orderGeoIndex[3] = i2;
                                    orders[0] = contextOrders[i3];
                                    orders[1] = contextOrders[i4];
                                    orders[2] = contextOrders[i1];
                                    orders[3] = contextOrders[i2];
                                    rcFind4 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 4, isLoop, geoData, out delivery4);
                                    if (rcFind4 == 0 && (delivery == null || delivery4.Cost < delivery.Cost))
                                    { delivery = delivery4; }
                                    
                                    // 3 4 2 1
                                    orderGeoIndex[0] = i3;
                                    orderGeoIndex[1] = i4;
                                    orderGeoIndex[2] = i2;
                                    orderGeoIndex[3] = i1;
                                    orders[0] = contextOrders[i3];
                                    orders[1] = contextOrders[i4];
                                    orders[2] = contextOrders[i2];
                                    orders[3] = contextOrders[i1];
                                    rcFind4 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 4, isLoop, geoData, out delivery4);
                                    if (rcFind4 == 0 && (delivery == null || delivery4.Cost < delivery.Cost))
                                    { delivery = delivery4; }
                                    
                                    // 4 1 2 3
                                    orderGeoIndex[0] = i4;
                                    orderGeoIndex[1] = i1;
                                    orderGeoIndex[2] = i2;
                                    orderGeoIndex[3] = i3;
                                    orders[0] = contextOrders[i4];
                                    orders[1] = contextOrders[i1];
                                    orders[2] = contextOrders[i2];
                                    orders[3] = contextOrders[i3];
                                    rcFind4 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 4, isLoop, geoData, out delivery4);
                                    if (rcFind4 == 0 && (delivery == null || delivery4.Cost < delivery.Cost))
                                    { delivery = delivery4; }
                                    
                                    // 4 1 3 2
                                    orderGeoIndex[0] = i4;
                                    orderGeoIndex[1] = i1;
                                    orderGeoIndex[2] = i3;
                                    orderGeoIndex[3] = i2;
                                    orders[0] = contextOrders[i4];
                                    orders[1] = contextOrders[i1];
                                    orders[2] = contextOrders[i3];
                                    orders[3] = contextOrders[i2];
                                    rcFind4 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 4, isLoop, geoData, out delivery4);
                                    if (rcFind4 == 0 && (delivery == null || delivery4.Cost < delivery.Cost))
                                    { delivery = delivery4; }
                                    
                                    // 4 2 1 3
                                    orderGeoIndex[0] = i4;
                                    orderGeoIndex[1] = i2;
                                    orderGeoIndex[2] = i1;
                                    orderGeoIndex[3] = i3;
                                    orders[0] = contextOrders[i4];
                                    orders[1] = contextOrders[i2];
                                    orders[2] = contextOrders[i1];
                                    orders[3] = contextOrders[i3];
                                    rcFind4 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 4, isLoop, geoData, out delivery4);
                                    if (rcFind4 == 0 && (delivery == null || delivery4.Cost < delivery.Cost))
                                    { delivery = delivery4; }
                                    
                                    // 4 2 3 1
                                    orderGeoIndex[0] = i4;
                                    orderGeoIndex[1] = i2;
                                    orderGeoIndex[2] = i3;
                                    orderGeoIndex[3] = i1;
                                    orders[0] = contextOrders[i4];
                                    orders[1] = contextOrders[i2];
                                    orders[2] = contextOrders[i3];
                                    orders[3] = contextOrders[i1];
                                    rcFind4 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 4, isLoop, geoData, out delivery4);
                                    if (rcFind4 == 0 && (delivery == null || delivery4.Cost < delivery.Cost))
                                    { delivery = delivery4; }
                                    
                                    // 4 3 1 2
                                    orderGeoIndex[0] = i4;
                                    orderGeoIndex[1] = i3;
                                    orderGeoIndex[2] = i1;
                                    orderGeoIndex[3] = i2;
                                    orders[0] = contextOrders[i4];
                                    orders[1] = contextOrders[i3];
                                    orders[2] = contextOrders[i1];
                                    orders[3] = contextOrders[i2];
                                    rcFind4 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 4, isLoop, geoData, out delivery4);
                                    if (rcFind4 == 0 && (delivery == null || delivery4.Cost < delivery.Cost))
                                    { delivery = delivery4; }
                                    
                                    // 4 3 2 1
                                    orderGeoIndex[0] = i4;
                                    orderGeoIndex[1] = i3;
                                    orderGeoIndex[2] = i2;
                                    orderGeoIndex[3] = i1;
                                    orders[0] = contextOrders[i4];
                                    orders[1] = contextOrders[i3];
                                    orders[2] = contextOrders[i2];
                                    orders[3] = contextOrders[i1];
                                    rcFind4 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 4, isLoop, geoData, out delivery4);
                                    if (rcFind4 == 0 && (delivery == null || delivery4.Cost < delivery.Cost))
                                    { delivery = delivery4; }

                                    if (delivery != null)
                                    { deliveries[count++] = delivery; }
                                }
                            }
                        }
                    }
                    else
                    {
                        for (int i2 = i1 + 1; i2 < orderCount; i2++)
                        {
                            orderGeoIndex[0] = i2;
                            orderGeoIndex[1] = i1;
                            orderGeoIndex[2] = shopIndex;
                            orders[0] = contextOrders[i2];
                            orders[1] = contextOrders[i1];

                            int rcFind1 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 2, isLoop, geoData, out delivery1);
                            if (rcFind1 == 0)
                            { deliveries[count++] = delivery1; }

                            for (int i3 = i2 + 1; i3 < orderCount; i3++)
                            {
                                delivery = null;

                                // 2 1 3
                                orderGeoIndex[0] = i2;
                                orderGeoIndex[1] = i1;
                                orderGeoIndex[2] = i3;
                                orderGeoIndex[3] = shopIndex;
                                orders[0] = contextOrders[i2];
                                orders[1] = contextOrders[i1];
                                orders[2] = contextOrders[i3];
                                int rcFind3 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 3, isLoop, geoData, out delivery3);
                                if (rcFind3 == 0 && (delivery == null || delivery3.Cost < delivery.Cost))
                                { delivery = delivery3; }

                                // 2 3 1
                                orderGeoIndex[1] = i3;
                                orderGeoIndex[2] = i1;
                                orders[0] = contextOrders[i2];
                                orders[1] = contextOrders[i3];
                                orders[2] = contextOrders[i1];
                                rcFind3 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 3, isLoop, geoData, out delivery3);
                                if (rcFind3 == 0 && (delivery == null || delivery3.Cost < delivery.Cost))
                                { delivery = delivery3; }

                                // 3 1 2
                                orderGeoIndex[0] = i3;
                                orderGeoIndex[1] = i1;
                                orderGeoIndex[2] = i2;
                                orders[0] = contextOrders[i3];
                                orders[1] = contextOrders[i1];
                                orders[2] = contextOrders[i2];
                                rcFind3 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 3, isLoop, geoData, out delivery3);
                                if (rcFind3 == 0 && (delivery == null || delivery3.Cost < delivery.Cost))
                                { delivery = delivery3; }

                                // 3 2 1
                                orderGeoIndex[1] = i2;
                                orderGeoIndex[2] = i1;
                                orders[0] = contextOrders[i3];
                                orders[1] = contextOrders[i2];
                                orders[2] = contextOrders[i1];
                                rcFind3 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 3, isLoop, geoData, out delivery3);
                                if (rcFind3 == 0 && (delivery == null || delivery3.Cost < delivery.Cost))
                                { delivery = delivery3; }

                                if (delivery != null)
                                { deliveries[count++] = delivery; }

                                orderGeoIndex[4] = shopIndex;

                                for (int i4 = i3 + 1; i4 < orderCount; i4++)
                                {
                                    delivery = null;

                                    // 2 1 3 4
                                    orderGeoIndex[0] = i2;
                                    orderGeoIndex[1] = i1;
                                    orderGeoIndex[2] = i3;
                                    orderGeoIndex[3] = i4;
                                    orders[0] = contextOrders[i2];
                                    orders[1] = contextOrders[i1];
                                    orders[2] = contextOrders[i3];
                                    orders[3] = contextOrders[i4];
                                    int rcFind4 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 4, isLoop, geoData, out delivery4);
                                    if (rcFind4 == 0 && (delivery == null || delivery4.Cost < delivery.Cost))
                                    { delivery = delivery4; }

                                    // 2 1 4 3
                                    orderGeoIndex[0] = i2;
                                    orderGeoIndex[1] = i1;
                                    orderGeoIndex[2] = i4;
                                    orderGeoIndex[3] = i3;
                                    orders[0] = contextOrders[i2];
                                    orders[1] = contextOrders[i1];
                                    orders[2] = contextOrders[i4];
                                    orders[3] = contextOrders[i3];
                                    rcFind4 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 4, isLoop, geoData, out delivery4);
                                    if (rcFind4 == 0 && (delivery == null || delivery4.Cost < delivery.Cost))
                                    { delivery = delivery4; }

                                    // 2 3 1 4
                                    orderGeoIndex[0] = i2;
                                    orderGeoIndex[1] = i3;
                                    orderGeoIndex[2] = i1;
                                    orderGeoIndex[3] = i4;
                                    orders[0] = contextOrders[i2];
                                    orders[1] = contextOrders[i3];
                                    orders[2] = contextOrders[i1];
                                    orders[3] = contextOrders[i4];
                                    rcFind4 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 4, isLoop, geoData, out delivery4);
                                    if (rcFind4 == 0 && (delivery == null || delivery4.Cost < delivery.Cost))
                                    { delivery = delivery4; }

                                    // 2 3 4 1
                                    orderGeoIndex[0] = i2;
                                    orderGeoIndex[1] = i3;
                                    orderGeoIndex[2] = i4;
                                    orderGeoIndex[3] = i1;
                                    orders[0] = contextOrders[i2];
                                    orders[1] = contextOrders[i3];
                                    orders[2] = contextOrders[i4];
                                    orders[3] = contextOrders[i1];
                                    rcFind4 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 4, isLoop, geoData, out delivery4);
                                    if (rcFind4 == 0 && (delivery == null || delivery4.Cost < delivery.Cost))
                                    { delivery = delivery4; }

                                    // 2 4 1 3
                                    orderGeoIndex[0] = i2;
                                    orderGeoIndex[1] = i4;
                                    orderGeoIndex[2] = i1;
                                    orderGeoIndex[3] = i3;
                                    orders[0] = contextOrders[i2];
                                    orders[1] = contextOrders[i4];
                                    orders[2] = contextOrders[i1];
                                    orders[3] = contextOrders[i3];
                                    rcFind4 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 4, isLoop, geoData, out delivery4);
                                    if (rcFind4 == 0 && (delivery == null || delivery4.Cost < delivery.Cost))
                                    { delivery = delivery4; }

                                    // 2 4 3 1
                                    orderGeoIndex[0] = i2;
                                    orderGeoIndex[1] = i4;
                                    orderGeoIndex[2] = i3;
                                    orderGeoIndex[3] = i1;
                                    orders[0] = contextOrders[i2];
                                    orders[1] = contextOrders[i4];
                                    orders[2] = contextOrders[i3];
                                    orders[3] = contextOrders[i1];
                                    rcFind4 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 4, isLoop, geoData, out delivery4);
                                    if (rcFind4 == 0 && (delivery == null || delivery4.Cost < delivery.Cost))
                                    { delivery = delivery4; }

                                    // 3 1 2 4
                                    orderGeoIndex[0] = i3;
                                    orderGeoIndex[1] = i1;
                                    orderGeoIndex[2] = i2;
                                    orderGeoIndex[3] = i4;
                                    orders[0] = contextOrders[i3];
                                    orders[1] = contextOrders[i1];
                                    orders[2] = contextOrders[i2];
                                    orders[3] = contextOrders[i4];
                                    rcFind4 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 4, isLoop, geoData, out delivery4);
                                    if (rcFind4 == 0 && (delivery == null || delivery4.Cost < delivery.Cost))
                                    { delivery = delivery4; }

                                    // 3 1 4 2
                                    orderGeoIndex[0] = i3;
                                    orderGeoIndex[1] = i1;
                                    orderGeoIndex[2] = i4;
                                    orderGeoIndex[3] = i2;
                                    orders[0] = contextOrders[i3];
                                    orders[1] = contextOrders[i1];
                                    orders[2] = contextOrders[i4];
                                    orders[3] = contextOrders[i2];
                                    rcFind4 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 4, isLoop, geoData, out delivery4);
                                    if (rcFind4 == 0 && (delivery == null || delivery4.Cost < delivery.Cost))
                                    { delivery = delivery4; }

                                    // 3 2 1 4
                                    orderGeoIndex[0] = i3;
                                    orderGeoIndex[1] = i2;
                                    orderGeoIndex[2] = i1;
                                    orderGeoIndex[3] = i4;
                                    orders[0] = contextOrders[i3];
                                    orders[1] = contextOrders[i2];
                                    orders[2] = contextOrders[i1];
                                    orders[3] = contextOrders[i4];
                                    rcFind4 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 4, isLoop, geoData, out delivery4);
                                    if (rcFind4 == 0 && (delivery == null || delivery4.Cost < delivery.Cost))
                                    { delivery = delivery4; }

                                    // 3 2 4 1
                                    orderGeoIndex[0] = i3;
                                    orderGeoIndex[1] = i2;
                                    orderGeoIndex[2] = i4;
                                    orderGeoIndex[3] = i1;
                                    orders[0] = contextOrders[i3];
                                    orders[1] = contextOrders[i2];
                                    orders[2] = contextOrders[i4];
                                    orders[3] = contextOrders[i1];
                                    rcFind4 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 4, isLoop, geoData, out delivery4);
                                    if (rcFind4 == 0 && (delivery == null || delivery4.Cost < delivery.Cost))
                                    { delivery = delivery4; }

                                    // 3 4 1 2
                                    orderGeoIndex[0] = i3;
                                    orderGeoIndex[1] = i4;
                                    orderGeoIndex[2] = i1;
                                    orderGeoIndex[3] = i2;
                                    orders[0] = contextOrders[i3];
                                    orders[1] = contextOrders[i4];
                                    orders[2] = contextOrders[i1];
                                    orders[3] = contextOrders[i2];
                                    rcFind4 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 4, isLoop, geoData, out delivery4);
                                    if (rcFind4 == 0 && (delivery == null || delivery4.Cost < delivery.Cost))
                                    { delivery = delivery4; }

                                    // 3 4 2 1
                                    orderGeoIndex[0] = i3;
                                    orderGeoIndex[1] = i4;
                                    orderGeoIndex[2] = i2;
                                    orderGeoIndex[3] = i1;
                                    orders[0] = contextOrders[i3];
                                    orders[1] = contextOrders[i4];
                                    orders[2] = contextOrders[i2];
                                    orders[3] = contextOrders[i1];
                                    rcFind4 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 4, isLoop, geoData, out delivery4);
                                    if (rcFind4 == 0 && (delivery == null || delivery4.Cost < delivery.Cost))
                                    { delivery = delivery4; }

                                    // 4 1 2 3
                                    orderGeoIndex[0] = i4;
                                    orderGeoIndex[1] = i1;
                                    orderGeoIndex[2] = i2;
                                    orderGeoIndex[3] = i3;
                                    orders[0] = contextOrders[i4];
                                    orders[1] = contextOrders[i1];
                                    orders[2] = contextOrders[i2];
                                    orders[3] = contextOrders[i3];
                                    rcFind4 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 4, isLoop, geoData, out delivery4);
                                    if (rcFind4 == 0 && (delivery == null || delivery4.Cost < delivery.Cost))
                                    { delivery = delivery4; }

                                    // 4 1 3 2
                                    orderGeoIndex[0] = i4;
                                    orderGeoIndex[1] = i1;
                                    orderGeoIndex[2] = i3;
                                    orderGeoIndex[3] = i2;
                                    orders[0] = contextOrders[i4];
                                    orders[1] = contextOrders[i1];
                                    orders[2] = contextOrders[i3];
                                    orders[3] = contextOrders[i2];
                                    rcFind4 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 4, isLoop, geoData, out delivery4);
                                    if (rcFind4 == 0 && (delivery == null || delivery4.Cost < delivery.Cost))
                                    { delivery = delivery4; }

                                    // 4 2 1 3
                                    orderGeoIndex[0] = i4;
                                    orderGeoIndex[1] = i2;
                                    orderGeoIndex[2] = i1;
                                    orderGeoIndex[3] = i3;
                                    orders[0] = contextOrders[i4];
                                    orders[1] = contextOrders[i2];
                                    orders[2] = contextOrders[i1];
                                    orders[3] = contextOrders[i3];
                                    rcFind4 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 4, isLoop, geoData, out delivery4);
                                    if (rcFind4 == 0 && (delivery == null || delivery4.Cost < delivery.Cost))
                                    { delivery = delivery4; }

                                    // 4 2 3 1
                                    orderGeoIndex[0] = i4;
                                    orderGeoIndex[1] = i2;
                                    orderGeoIndex[2] = i3;
                                    orderGeoIndex[3] = i1;
                                    orders[0] = contextOrders[i4];
                                    orders[1] = contextOrders[i2];
                                    orders[2] = contextOrders[i3];
                                    orders[3] = contextOrders[i1];
                                    rcFind4 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 4, isLoop, geoData, out delivery4);
                                    if (rcFind4 == 0 && (delivery == null || delivery4.Cost < delivery.Cost))
                                    { delivery = delivery4; }

                                    // 4 3 1 2
                                    orderGeoIndex[0] = i4;
                                    orderGeoIndex[1] = i3;
                                    orderGeoIndex[2] = i1;
                                    orderGeoIndex[3] = i2;
                                    orders[0] = contextOrders[i4];
                                    orders[1] = contextOrders[i3];
                                    orders[2] = contextOrders[i1];
                                    orders[3] = contextOrders[i2];
                                    rcFind4 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 4, isLoop, geoData, out delivery4);
                                    if (rcFind4 == 0 && (delivery == null || delivery4.Cost < delivery.Cost))
                                    { delivery = delivery4; }

                                    // 4 3 2 1
                                    orderGeoIndex[0] = i4;
                                    orderGeoIndex[1] = i3;
                                    orderGeoIndex[2] = i2;
                                    orderGeoIndex[3] = i1;
                                    orders[0] = contextOrders[i4];
                                    orders[1] = contextOrders[i3];
                                    orders[2] = contextOrders[i2];
                                    orders[3] = contextOrders[i1];
                                    rcFind4 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 4, isLoop, geoData, out delivery4);
                                    if (rcFind4 == 0 && (delivery == null || delivery4.Cost < delivery.Cost))
                                    { delivery = delivery4; }

                                    if (delivery != null)
                                    { deliveries[count++] = delivery; }
                                }
                            }
                        }
                    }
                }

                if (count < deliveries.Length)
                { Array.Resize(ref deliveries, count); }

                context.Deliveries = deliveries;

                // 5. Выход - Ok
                rc = 0;
                return;
            }
            catch (Exception ex)
            {
#if debug
                Logger.WriteToLog(373, $"BuildEx4. startIndex = {context.StartOrderIndex}. rc = {rc}. order_count = {context.OrderCount}, shop_id = {context.ShopFrom.Id}, courier_id = {context.ShopCourier.Id}, level = {context.MaxRouteLength} Exception = {ex.Message}", 2);
#endif
                return;
            }
            finally
            {
                if (context != null)
                {
#if debug
                    Logger.WriteToLog(3090, $"BuildEx4 exit. rc = {rc}. vehicleID = {context.ShopCourier.VehicleID}. order_count = {context.OrderCount}, level = {context.MaxRouteLength}, startIndex = {context.StartOrderIndex}, step = {context.OrderIndexStep}", 0);
#endif
                    context.ExitCode = rc;
                    ManualResetEvent syncEvent = context.SyncEvent;
                    if (syncEvent != null)
                    {
                        syncEvent.Set();
                    }
                }
            }
        }

        /// <summary>
        /// Построение всех возможных отгрузок длины 5 для
        /// заданного контекста и гео-данных
        /// </summary>
        /// <param name="status">Расширенный контекст</param>
        /// <returns>0 - отгрузки построены; иначе - отгрузки не построены</returns>
        public static void BuildEx5(object status)
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
#if (debug)
                Logger.WriteToLog(309, $"BuildEx5 enter. vehicleID = {context.ShopCourier.VehicleID}. order_count = {context.OrderCount}, level = {context.MaxRouteLength}, startIndex = {context.StartOrderIndex}, step = {context.OrderIndexStep}", 0);
#endif
                // 3. Извлекаем и проверяем данные из контекста
                rc = 3;
                int level = context.MaxRouteLength;
                if (level != 4)
                    return;
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
                //long[] deliveryKeys = context.DeliveryKeys;
                //if (deliveryKeys == null || deliveryKeys.Length <= 0)
                //    return;
                int startIndex = context.StartOrderIndex;
                if (startIndex < 0 || startIndex >= orderCount)
                    return;
                int step = context.OrderIndexStep;
                if (step <= 0)
                    return;
                CourierDeliveryInfo[] deliveries;
                if (orderCount >= 5)
                {
                    deliveries = new CourierDeliveryInfo[
                        orderCount + 
                        (orderCount - 1) * orderCount / 2 + 
                        (orderCount - 2) * (orderCount - 1) * orderCount / 6 +
                        (orderCount - 3) * (orderCount - 2) * (orderCount - 1) * orderCount / 24 +
                        (orderCount - 4) * (orderCount - 3) * (orderCount - 2) * (orderCount - 1) * orderCount / 120];
                }
                else if (orderCount >= 4)
                {
                    deliveries = new CourierDeliveryInfo[
                        orderCount + 
                        (orderCount - 1) * orderCount / 2 + 
                        (orderCount - 2) * (orderCount - 1) * orderCount / 6 +
                        (orderCount - 3) * (orderCount - 2) * (orderCount - 1) * orderCount / 24];
                }
                else if (orderCount >= 3)
                {
                    deliveries = new CourierDeliveryInfo[
                        orderCount + 
                        (orderCount - 1) * orderCount / 2 + 
                        (orderCount - 2) * (orderCount - 1) * orderCount / 6];
                }
                else if (orderCount >= 2)
                {
                    deliveries = new CourierDeliveryInfo[
                        orderCount + 
                        (orderCount - 1) * orderCount / 2];
                }
                else
                {
                    deliveries = new CourierDeliveryInfo[1];
                }
           
                int count = 0;

                // 4. Цикл выбора допустимых маршрутов
                rc = 4;
                int[] orderIndex = new int[5];
                Order[] orders = new Order[5];
                int[] orderGeoIndex = new int[6];
                bool isLoop = !contextCourier.IsTaxi;
                int shopIndex = orderCount;
                CourierDeliveryInfo delivery;
                CourierDeliveryInfo delivery1;
                CourierDeliveryInfo delivery2;
                CourierDeliveryInfo delivery3;
                CourierDeliveryInfo delivery4;
                CourierDeliveryInfo delivery5;
                byte[] permutations5 = RouteCheck.Permutations.Generate(5);

                for (int i1 = startIndex; i1 < orderCount; i1 += step)
                {
                    orderGeoIndex[0] = i1;
                    orderGeoIndex[1] = shopIndex;
                    orders[0] = contextOrders[i1];
                    int rcFind = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 1, isLoop, geoData, out delivery);

                    if (rcFind == 0)
                    {
                        deliveries[count++] = delivery;

                        for (int i2 = i1 + 1; i2 < orderCount; i2++)
                        {
                            orderGeoIndex[0] = i1;
                            orderGeoIndex[1] = i2;
                            orderGeoIndex[2] = shopIndex;
                            orders[0] = contextOrders[i1];
                            orders[1] = contextOrders[i2];
                            int rcFind1 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 2, isLoop, geoData, out delivery1);

                            orderGeoIndex[0] = i2;
                            orderGeoIndex[1] = i1;
                            orders[0] = contextOrders[i2];
                            orders[1] = contextOrders[i1];
                            int rcFind2 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 2, isLoop, geoData, out delivery2);

                            if (rcFind1 == 0)
                            {
                                if (rcFind2 == 0)
                                { deliveries[count++] = (delivery1.Cost <= delivery2.Cost ? delivery1 : delivery2); }
                                else
                                { deliveries[count++] = delivery1; }
                            }
                            else if (rcFind2 == 0)
                            { deliveries[count++] = delivery2; }


                            for (int i3 = i2 + 1; i3 < orderCount; i3++)
                            {
                                delivery = null;

                                // 1 2 3
                                orderGeoIndex[0] = i1;
                                orderGeoIndex[1] = i2;
                                orderGeoIndex[2] = i3;
                                orderGeoIndex[3] = shopIndex;
                                orders[0] = contextOrders[i1];
                                orders[1] = contextOrders[i2];
                                orders[2] = contextOrders[i3];
                                int rcFind3 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 3, isLoop, geoData, out delivery3);
                                if (rcFind3 == 0)
                                { delivery = delivery3; }

                                // 1 3 2
                                orderGeoIndex[1] = i3;
                                orderGeoIndex[2] = i2;
                                orders[0] = contextOrders[i1];
                                orders[1] = contextOrders[i3];
                                orders[2] = contextOrders[i2];
                                rcFind3 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 3, isLoop, geoData, out delivery3);
                                if (rcFind3 == 0 && (delivery == null || delivery3.Cost < delivery.Cost))
                                { delivery = delivery3; }

                                // 2 1 3
                                orderGeoIndex[0] = i2;
                                orderGeoIndex[1] = i1;
                                orderGeoIndex[2] = i3;
                                orders[0] = contextOrders[i2];
                                orders[1] = contextOrders[i1];
                                orders[2] = contextOrders[i3];
                                rcFind3 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 3, isLoop, geoData, out delivery3);
                                if (rcFind3 == 0 && (delivery == null || delivery3.Cost < delivery.Cost))
                                { delivery = delivery3; }

                                // 2 3 1
                                orderGeoIndex[1] = i3;
                                orderGeoIndex[2] = i1;
                                orders[0] = contextOrders[i2];
                                orders[1] = contextOrders[i3];
                                orders[2] = contextOrders[i1];
                                rcFind3 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 3, isLoop, geoData, out delivery3);
                                if (rcFind3 == 0 && (delivery == null || delivery3.Cost < delivery.Cost))
                                { delivery = delivery3; }

                                // 3 1 2
                                orderGeoIndex[0] = i3;
                                orderGeoIndex[1] = i1;
                                orderGeoIndex[2] = i2;
                                orders[0] = contextOrders[i3];
                                orders[1] = contextOrders[i1];
                                orders[2] = contextOrders[i2];
                                rcFind3 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 3, isLoop, geoData, out delivery3);
                                if (rcFind3 == 0 && (delivery == null || delivery3.Cost < delivery.Cost))
                                { delivery = delivery3; }

                                // 3 2 1
                                orderGeoIndex[1] = i2;
                                orderGeoIndex[2] = i1;
                                orders[0] = contextOrders[i3];
                                orders[1] = contextOrders[i2];
                                orders[2] = contextOrders[i1];
                                rcFind3 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 3, isLoop, geoData, out delivery3);
                                if (rcFind3 == 0 && (delivery == null || delivery3.Cost < delivery.Cost))
                                { delivery = delivery3; }

                                if (delivery != null)
                                { deliveries[count++] = delivery; }

                                //orderGeoIndex[4] = shopIndex;

                                for (int i4 = i3 + 1; i4 < orderCount; i4++)
                                {
                                    delivery = null;

                                    // 1 2 3 4
                                    orderGeoIndex[0] = i1;
                                    orderGeoIndex[1] = i2;
                                    orderGeoIndex[2] = i3;
                                    orderGeoIndex[3] = i4;
                                    orderGeoIndex[4] = shopIndex;
                                    orders[0] = contextOrders[i1];
                                    orders[1] = contextOrders[i2];
                                    orders[2] = contextOrders[i3];
                                    orders[3] = contextOrders[i4];
                                    int rcFind4 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 4, isLoop, geoData, out delivery4);
                                    if (rcFind4 == 0)
                                    { delivery = delivery4; }

                                    // 1 2 4 3
                                    orderGeoIndex[0] = i1;
                                    orderGeoIndex[1] = i2;
                                    orderGeoIndex[2] = i4;
                                    orderGeoIndex[3] = i3;
                                    orders[0] = contextOrders[i1];
                                    orders[1] = contextOrders[i2];
                                    orders[2] = contextOrders[i4];
                                    orders[3] = contextOrders[i3];
                                    rcFind4 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 4, isLoop, geoData, out delivery4);
                                    if (rcFind4 == 0 && (delivery == null || delivery4.Cost < delivery.Cost))
                                    { delivery = delivery4; }
                                    
                                    // 1 3 2 4
                                    orderGeoIndex[0] = i1;
                                    orderGeoIndex[1] = i3;
                                    orderGeoIndex[2] = i2;
                                    orderGeoIndex[3] = i4;
                                    orders[0] = contextOrders[i1];
                                    orders[1] = contextOrders[i3];
                                    orders[2] = contextOrders[i2];
                                    orders[3] = contextOrders[i4];
                                    rcFind4 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 4, isLoop, geoData, out delivery4);
                                    if (rcFind4 == 0 && (delivery == null || delivery4.Cost < delivery.Cost))
                                    { delivery = delivery4; }
                                    
                                    // 1 3 4 2
                                    orderGeoIndex[0] = i1;
                                    orderGeoIndex[1] = i3;
                                    orderGeoIndex[2] = i4;
                                    orderGeoIndex[3] = i2;
                                    orders[0] = contextOrders[i1];
                                    orders[1] = contextOrders[i3];
                                    orders[2] = contextOrders[i4];
                                    orders[3] = contextOrders[i2];
                                    rcFind4 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 4, isLoop, geoData, out delivery4);
                                    if (rcFind4 == 0 && (delivery == null || delivery4.Cost < delivery.Cost))
                                    { delivery = delivery4; }
                                    
                                    // 1 4 2 3
                                    orderGeoIndex[0] = i1;
                                    orderGeoIndex[1] = i4;
                                    orderGeoIndex[2] = i2;
                                    orderGeoIndex[3] = i3;
                                    orders[0] = contextOrders[i1];
                                    orders[1] = contextOrders[i4];
                                    orders[2] = contextOrders[i2];
                                    orders[3] = contextOrders[i3];
                                    rcFind4 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 4, isLoop, geoData, out delivery4);
                                    if (rcFind4 == 0 && (delivery == null || delivery4.Cost < delivery.Cost))
                                    { delivery = delivery4; }

                                    // 1 4 3 2
                                    orderGeoIndex[0] = i1;
                                    orderGeoIndex[1] = i4;
                                    orderGeoIndex[2] = i3;
                                    orderGeoIndex[3] = i2;
                                    orders[0] = contextOrders[i1];
                                    orders[1] = contextOrders[i4];
                                    orders[2] = contextOrders[i3];
                                    orders[3] = contextOrders[i2];
                                    rcFind4 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 4, isLoop, geoData, out delivery4);
                                    if (rcFind4 == 0 && (delivery == null || delivery4.Cost < delivery.Cost))
                                    { delivery = delivery4; }
                                    
                                    // 2 1 3 4
                                    orderGeoIndex[0] = i2;
                                    orderGeoIndex[1] = i1;
                                    orderGeoIndex[2] = i3;
                                    orderGeoIndex[3] = i4;
                                    orders[0] = contextOrders[i2];
                                    orders[1] = contextOrders[i1];
                                    orders[2] = contextOrders[i3];
                                    orders[3] = contextOrders[i4];
                                    rcFind4 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 4, isLoop, geoData, out delivery4);
                                    if (rcFind4 == 0 && (delivery == null || delivery4.Cost < delivery.Cost))
                                    { delivery = delivery4; }
                                    
                                    // 2 1 4 3
                                    orderGeoIndex[0] = i2;
                                    orderGeoIndex[1] = i1;
                                    orderGeoIndex[2] = i4;
                                    orderGeoIndex[3] = i3;
                                    orders[0] = contextOrders[i2];
                                    orders[1] = contextOrders[i1];
                                    orders[2] = contextOrders[i4];
                                    orders[3] = contextOrders[i3];
                                    rcFind4 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 4, isLoop, geoData, out delivery4);
                                    if (rcFind4 == 0 && (delivery == null || delivery4.Cost < delivery.Cost))
                                    { delivery = delivery4; }
                                    
                                    // 2 3 1 4
                                    orderGeoIndex[0] = i2;
                                    orderGeoIndex[1] = i3;
                                    orderGeoIndex[2] = i1;
                                    orderGeoIndex[3] = i4;
                                    orders[0] = contextOrders[i2];
                                    orders[1] = contextOrders[i3];
                                    orders[2] = contextOrders[i1];
                                    orders[3] = contextOrders[i4];
                                    rcFind4 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 4, isLoop, geoData, out delivery4);
                                    if (rcFind4 == 0 && (delivery == null || delivery4.Cost < delivery.Cost))
                                    { delivery = delivery4; }
                                    
                                    // 2 3 4 1
                                    orderGeoIndex[0] = i2;
                                    orderGeoIndex[1] = i3;
                                    orderGeoIndex[2] = i4;
                                    orderGeoIndex[3] = i1;
                                    orders[0] = contextOrders[i2];
                                    orders[1] = contextOrders[i3];
                                    orders[2] = contextOrders[i4];
                                    orders[3] = contextOrders[i1];
                                    rcFind4 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 4, isLoop, geoData, out delivery4);
                                    if (rcFind4 == 0 && (delivery == null || delivery4.Cost < delivery.Cost))
                                    { delivery = delivery4; }
                                    
                                    // 2 4 1 3
                                    orderGeoIndex[0] = i2;
                                    orderGeoIndex[1] = i4;
                                    orderGeoIndex[2] = i1;
                                    orderGeoIndex[3] = i3;
                                    orders[0] = contextOrders[i2];
                                    orders[1] = contextOrders[i4];
                                    orders[2] = contextOrders[i1];
                                    orders[3] = contextOrders[i3];
                                    rcFind4 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 4, isLoop, geoData, out delivery4);
                                    if (rcFind4 == 0 && (delivery == null || delivery4.Cost < delivery.Cost))
                                    { delivery = delivery4; }
                                    
                                    // 2 4 3 1
                                    orderGeoIndex[0] = i2;
                                    orderGeoIndex[1] = i4;
                                    orderGeoIndex[2] = i3;
                                    orderGeoIndex[3] = i1;
                                    orders[0] = contextOrders[i2];
                                    orders[1] = contextOrders[i4];
                                    orders[2] = contextOrders[i3];
                                    orders[3] = contextOrders[i1];
                                    rcFind4 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 4, isLoop, geoData, out delivery4);
                                    if (rcFind4 == 0 && (delivery == null || delivery4.Cost < delivery.Cost))
                                    { delivery = delivery4; }
                                    
                                    // 3 1 2 4
                                    orderGeoIndex[0] = i3;
                                    orderGeoIndex[1] = i1;
                                    orderGeoIndex[2] = i2;
                                    orderGeoIndex[3] = i4;
                                    orders[0] = contextOrders[i3];
                                    orders[1] = contextOrders[i1];
                                    orders[2] = contextOrders[i2];
                                    orders[3] = contextOrders[i4];
                                    rcFind4 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 4, isLoop, geoData, out delivery4);
                                    if (rcFind4 == 0 && (delivery == null || delivery4.Cost < delivery.Cost))
                                    { delivery = delivery4; }
                                    
                                    // 3 1 4 2
                                    orderGeoIndex[0] = i3;
                                    orderGeoIndex[1] = i1;
                                    orderGeoIndex[2] = i4;
                                    orderGeoIndex[3] = i2;
                                    orders[0] = contextOrders[i3];
                                    orders[1] = contextOrders[i1];
                                    orders[2] = contextOrders[i4];
                                    orders[3] = contextOrders[i2];
                                    rcFind4 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 4, isLoop, geoData, out delivery4);
                                    if (rcFind4 == 0 && (delivery == null || delivery4.Cost < delivery.Cost))
                                    { delivery = delivery4; }
                                    
                                    // 3 2 1 4
                                    orderGeoIndex[0] = i3;
                                    orderGeoIndex[1] = i2;
                                    orderGeoIndex[2] = i1;
                                    orderGeoIndex[3] = i4;
                                    orders[0] = contextOrders[i3];
                                    orders[1] = contextOrders[i2];
                                    orders[2] = contextOrders[i1];
                                    orders[3] = contextOrders[i4];
                                    rcFind4 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 4, isLoop, geoData, out delivery4);
                                    if (rcFind4 == 0 && (delivery == null || delivery4.Cost < delivery.Cost))
                                    { delivery = delivery4; }
                                    
                                    // 3 2 4 1
                                    orderGeoIndex[0] = i3;
                                    orderGeoIndex[1] = i2;
                                    orderGeoIndex[2] = i4;
                                    orderGeoIndex[3] = i1;
                                    orders[0] = contextOrders[i3];
                                    orders[1] = contextOrders[i2];
                                    orders[2] = contextOrders[i4];
                                    orders[3] = contextOrders[i1];
                                    rcFind4 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 4, isLoop, geoData, out delivery4);
                                    if (rcFind4 == 0 && (delivery == null || delivery4.Cost < delivery.Cost))
                                    { delivery = delivery4; }
                                    
                                    // 3 4 1 2
                                    orderGeoIndex[0] = i3;
                                    orderGeoIndex[1] = i4;
                                    orderGeoIndex[2] = i1;
                                    orderGeoIndex[3] = i2;
                                    orders[0] = contextOrders[i3];
                                    orders[1] = contextOrders[i4];
                                    orders[2] = contextOrders[i1];
                                    orders[3] = contextOrders[i2];
                                    rcFind4 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 4, isLoop, geoData, out delivery4);
                                    if (rcFind4 == 0 && (delivery == null || delivery4.Cost < delivery.Cost))
                                    { delivery = delivery4; }
                                    
                                    // 3 4 2 1
                                    orderGeoIndex[0] = i3;
                                    orderGeoIndex[1] = i4;
                                    orderGeoIndex[2] = i2;
                                    orderGeoIndex[3] = i1;
                                    orders[0] = contextOrders[i3];
                                    orders[1] = contextOrders[i4];
                                    orders[2] = contextOrders[i2];
                                    orders[3] = contextOrders[i1];
                                    rcFind4 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 4, isLoop, geoData, out delivery4);
                                    if (rcFind4 == 0 && (delivery == null || delivery4.Cost < delivery.Cost))
                                    { delivery = delivery4; }
                                    
                                    // 4 1 2 3
                                    orderGeoIndex[0] = i4;
                                    orderGeoIndex[1] = i1;
                                    orderGeoIndex[2] = i2;
                                    orderGeoIndex[3] = i3;
                                    orders[0] = contextOrders[i4];
                                    orders[1] = contextOrders[i1];
                                    orders[2] = contextOrders[i2];
                                    orders[3] = contextOrders[i3];
                                    rcFind4 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 4, isLoop, geoData, out delivery4);
                                    if (rcFind4 == 0 && (delivery == null || delivery4.Cost < delivery.Cost))
                                    { delivery = delivery4; }
                                    
                                    // 4 1 3 2
                                    orderGeoIndex[0] = i4;
                                    orderGeoIndex[1] = i1;
                                    orderGeoIndex[2] = i3;
                                    orderGeoIndex[3] = i2;
                                    orders[0] = contextOrders[i4];
                                    orders[1] = contextOrders[i1];
                                    orders[2] = contextOrders[i3];
                                    orders[3] = contextOrders[i2];
                                    rcFind4 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 4, isLoop, geoData, out delivery4);
                                    if (rcFind4 == 0 && (delivery == null || delivery4.Cost < delivery.Cost))
                                    { delivery = delivery4; }
                                    
                                    // 4 2 1 3
                                    orderGeoIndex[0] = i4;
                                    orderGeoIndex[1] = i2;
                                    orderGeoIndex[2] = i1;
                                    orderGeoIndex[3] = i3;
                                    orders[0] = contextOrders[i4];
                                    orders[1] = contextOrders[i2];
                                    orders[2] = contextOrders[i1];
                                    orders[3] = contextOrders[i3];
                                    rcFind4 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 4, isLoop, geoData, out delivery4);
                                    if (rcFind4 == 0 && (delivery == null || delivery4.Cost < delivery.Cost))
                                    { delivery = delivery4; }
                                    
                                    // 4 2 3 1
                                    orderGeoIndex[0] = i4;
                                    orderGeoIndex[1] = i2;
                                    orderGeoIndex[2] = i3;
                                    orderGeoIndex[3] = i1;
                                    orders[0] = contextOrders[i4];
                                    orders[1] = contextOrders[i2];
                                    orders[2] = contextOrders[i3];
                                    orders[3] = contextOrders[i1];
                                    rcFind4 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 4, isLoop, geoData, out delivery4);
                                    if (rcFind4 == 0 && (delivery == null || delivery4.Cost < delivery.Cost))
                                    { delivery = delivery4; }
                                    
                                    // 4 3 1 2
                                    orderGeoIndex[0] = i4;
                                    orderGeoIndex[1] = i3;
                                    orderGeoIndex[2] = i1;
                                    orderGeoIndex[3] = i2;
                                    orders[0] = contextOrders[i4];
                                    orders[1] = contextOrders[i3];
                                    orders[2] = contextOrders[i1];
                                    orders[3] = contextOrders[i2];
                                    rcFind4 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 4, isLoop, geoData, out delivery4);
                                    if (rcFind4 == 0 && (delivery == null || delivery4.Cost < delivery.Cost))
                                    { delivery = delivery4; }
                                    
                                    // 4 3 2 1
                                    orderGeoIndex[0] = i4;
                                    orderGeoIndex[1] = i3;
                                    orderGeoIndex[2] = i2;
                                    orderGeoIndex[3] = i1;
                                    orders[0] = contextOrders[i4];
                                    orders[1] = contextOrders[i3];
                                    orders[2] = contextOrders[i2];
                                    orders[3] = contextOrders[i1];
                                    rcFind4 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 4, isLoop, geoData, out delivery4);
                                    if (rcFind4 == 0 && (delivery == null || delivery4.Cost < delivery.Cost))
                                    { delivery = delivery4; }

                                    if (delivery != null)
                                    { deliveries[count++] = delivery; }

                                    orderGeoIndex[5] = shopIndex;
                                    orderIndex[0] = i1;
                                    orderIndex[1] = i2;
                                    orderIndex[2] = i3;
                                    orderIndex[3] = i4;

                                    for (int i5 = i4 + 1; i5 < orderCount; i5++)
                                    {
                                        orderIndex[4] = i5;
                                        delivery = null;

                                        for (int i = 0; i < permutations5.Length; i+=5)
                                        {
                                            int p1 = orderIndex[permutations5[i]];
                                            int p2 = orderIndex[permutations5[i + 1]];
                                            int p3 = orderIndex[permutations5[i + 2]];
                                            int p4 = orderIndex[permutations5[i + 3]];
                                            int p5 = orderIndex[permutations5[i + 4]];

                                            orderGeoIndex[0] = p1;
                                            orderGeoIndex[1] = p2;
                                            orderGeoIndex[2] = p3;
                                            orderGeoIndex[3] = p4;
                                            orderGeoIndex[3] = p5;

                                            orders[0] = contextOrders[p1];
                                            orders[1] = contextOrders[p2];
                                            orders[2] = contextOrders[p3];
                                            orders[3] = contextOrders[p4];
                                            orders[4] = contextOrders[p5];

                                            int rcFind5 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 5, isLoop, geoData, out delivery5);
                                            if (rcFind5 == 0 && (delivery == null || delivery5.Cost < delivery.Cost))
                                            { delivery = delivery5; }
                                        }

                                        if (delivery != null)
                                        { deliveries[count++] = delivery; }
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        for (int i2 = i1 + 1; i2 < orderCount; i2++)
                        {
                            orderGeoIndex[0] = i2;
                            orderGeoIndex[1] = i1;
                            orderGeoIndex[2] = shopIndex;
                            orders[0] = contextOrders[i2];
                            orders[1] = contextOrders[i1];

                            int rcFind1 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 2, isLoop, geoData, out delivery1);
                            if (rcFind1 == 0)
                            { deliveries[count++] = delivery1; }

                            for (int i3 = i2 + 1; i3 < orderCount; i3++)
                            {
                                delivery = null;

                                // 2 1 3
                                orderGeoIndex[0] = i2;
                                orderGeoIndex[1] = i1;
                                orderGeoIndex[2] = i3;
                                orderGeoIndex[3] = shopIndex;
                                orders[0] = contextOrders[i2];
                                orders[1] = contextOrders[i1];
                                orders[2] = contextOrders[i3];
                                int rcFind3 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 3, isLoop, geoData, out delivery3);
                                if (rcFind3 == 0 && (delivery == null || delivery3.Cost < delivery.Cost))
                                { delivery = delivery3; }

                                // 2 3 1
                                orderGeoIndex[1] = i3;
                                orderGeoIndex[2] = i1;
                                orders[0] = contextOrders[i2];
                                orders[1] = contextOrders[i3];
                                orders[2] = contextOrders[i1];
                                rcFind3 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 3, isLoop, geoData, out delivery3);
                                if (rcFind3 == 0 && (delivery == null || delivery3.Cost < delivery.Cost))
                                { delivery = delivery3; }

                                // 3 1 2
                                orderGeoIndex[0] = i3;
                                orderGeoIndex[1] = i1;
                                orderGeoIndex[2] = i2;
                                orders[0] = contextOrders[i3];
                                orders[1] = contextOrders[i1];
                                orders[2] = contextOrders[i2];
                                rcFind3 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 3, isLoop, geoData, out delivery3);
                                if (rcFind3 == 0 && (delivery == null || delivery3.Cost < delivery.Cost))
                                { delivery = delivery3; }

                                // 3 2 1
                                orderGeoIndex[1] = i2;
                                orderGeoIndex[2] = i1;
                                orders[0] = contextOrders[i3];
                                orders[1] = contextOrders[i2];
                                orders[2] = contextOrders[i1];
                                rcFind3 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 3, isLoop, geoData, out delivery3);
                                if (rcFind3 == 0 && (delivery == null || delivery3.Cost < delivery.Cost))
                                { delivery = delivery3; }

                                if (delivery != null)
                                { deliveries[count++] = delivery; }


                                for (int i4 = i3 + 1; i4 < orderCount; i4++)
                                {
                                    delivery = null;

                                    // 2 1 3 4
                                    orderGeoIndex[0] = i2;
                                    orderGeoIndex[1] = i1;
                                    orderGeoIndex[2] = i3;
                                    orderGeoIndex[3] = i4;
                                    orderGeoIndex[4] = shopIndex;

                                    orders[0] = contextOrders[i2];
                                    orders[1] = contextOrders[i1];
                                    orders[2] = contextOrders[i3];
                                    orders[3] = contextOrders[i4];
                                    int rcFind4 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 4, isLoop, geoData, out delivery4);
                                    if (rcFind4 == 0 && (delivery == null || delivery4.Cost < delivery.Cost))
                                    { delivery = delivery4; }

                                    // 2 1 4 3
                                    orderGeoIndex[0] = i2;
                                    orderGeoIndex[1] = i1;
                                    orderGeoIndex[2] = i4;
                                    orderGeoIndex[3] = i3;
                                    orders[0] = contextOrders[i2];
                                    orders[1] = contextOrders[i1];
                                    orders[2] = contextOrders[i4];
                                    orders[3] = contextOrders[i3];
                                    rcFind4 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 4, isLoop, geoData, out delivery4);
                                    if (rcFind4 == 0 && (delivery == null || delivery4.Cost < delivery.Cost))
                                    { delivery = delivery4; }

                                    // 2 3 1 4
                                    orderGeoIndex[0] = i2;
                                    orderGeoIndex[1] = i3;
                                    orderGeoIndex[2] = i1;
                                    orderGeoIndex[3] = i4;
                                    orders[0] = contextOrders[i2];
                                    orders[1] = contextOrders[i3];
                                    orders[2] = contextOrders[i1];
                                    orders[3] = contextOrders[i4];
                                    rcFind4 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 4, isLoop, geoData, out delivery4);
                                    if (rcFind4 == 0 && (delivery == null || delivery4.Cost < delivery.Cost))
                                    { delivery = delivery4; }

                                    // 2 3 4 1
                                    orderGeoIndex[0] = i2;
                                    orderGeoIndex[1] = i3;
                                    orderGeoIndex[2] = i4;
                                    orderGeoIndex[3] = i1;
                                    orders[0] = contextOrders[i2];
                                    orders[1] = contextOrders[i3];
                                    orders[2] = contextOrders[i4];
                                    orders[3] = contextOrders[i1];
                                    rcFind4 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 4, isLoop, geoData, out delivery4);
                                    if (rcFind4 == 0 && (delivery == null || delivery4.Cost < delivery.Cost))
                                    { delivery = delivery4; }

                                    // 2 4 1 3
                                    orderGeoIndex[0] = i2;
                                    orderGeoIndex[1] = i4;
                                    orderGeoIndex[2] = i1;
                                    orderGeoIndex[3] = i3;
                                    orders[0] = contextOrders[i2];
                                    orders[1] = contextOrders[i4];
                                    orders[2] = contextOrders[i1];
                                    orders[3] = contextOrders[i3];
                                    rcFind4 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 4, isLoop, geoData, out delivery4);
                                    if (rcFind4 == 0 && (delivery == null || delivery4.Cost < delivery.Cost))
                                    { delivery = delivery4; }

                                    // 2 4 3 1
                                    orderGeoIndex[0] = i2;
                                    orderGeoIndex[1] = i4;
                                    orderGeoIndex[2] = i3;
                                    orderGeoIndex[3] = i1;
                                    orders[0] = contextOrders[i2];
                                    orders[1] = contextOrders[i4];
                                    orders[2] = contextOrders[i3];
                                    orders[3] = contextOrders[i1];
                                    rcFind4 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 4, isLoop, geoData, out delivery4);
                                    if (rcFind4 == 0 && (delivery == null || delivery4.Cost < delivery.Cost))
                                    { delivery = delivery4; }

                                    // 3 1 2 4
                                    orderGeoIndex[0] = i3;
                                    orderGeoIndex[1] = i1;
                                    orderGeoIndex[2] = i2;
                                    orderGeoIndex[3] = i4;
                                    orders[0] = contextOrders[i3];
                                    orders[1] = contextOrders[i1];
                                    orders[2] = contextOrders[i2];
                                    orders[3] = contextOrders[i4];
                                    rcFind4 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 4, isLoop, geoData, out delivery4);
                                    if (rcFind4 == 0 && (delivery == null || delivery4.Cost < delivery.Cost))
                                    { delivery = delivery4; }

                                    // 3 1 4 2
                                    orderGeoIndex[0] = i3;
                                    orderGeoIndex[1] = i1;
                                    orderGeoIndex[2] = i4;
                                    orderGeoIndex[3] = i2;
                                    orders[0] = contextOrders[i3];
                                    orders[1] = contextOrders[i1];
                                    orders[2] = contextOrders[i4];
                                    orders[3] = contextOrders[i2];
                                    rcFind4 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 4, isLoop, geoData, out delivery4);
                                    if (rcFind4 == 0 && (delivery == null || delivery4.Cost < delivery.Cost))
                                    { delivery = delivery4; }

                                    // 3 2 1 4
                                    orderGeoIndex[0] = i3;
                                    orderGeoIndex[1] = i2;
                                    orderGeoIndex[2] = i1;
                                    orderGeoIndex[3] = i4;
                                    orders[0] = contextOrders[i3];
                                    orders[1] = contextOrders[i2];
                                    orders[2] = contextOrders[i1];
                                    orders[3] = contextOrders[i4];
                                    rcFind4 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 4, isLoop, geoData, out delivery4);
                                    if (rcFind4 == 0 && (delivery == null || delivery4.Cost < delivery.Cost))
                                    { delivery = delivery4; }

                                    // 3 2 4 1
                                    orderGeoIndex[0] = i3;
                                    orderGeoIndex[1] = i2;
                                    orderGeoIndex[2] = i4;
                                    orderGeoIndex[3] = i1;
                                    orders[0] = contextOrders[i3];
                                    orders[1] = contextOrders[i2];
                                    orders[2] = contextOrders[i4];
                                    orders[3] = contextOrders[i1];
                                    rcFind4 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 4, isLoop, geoData, out delivery4);
                                    if (rcFind4 == 0 && (delivery == null || delivery4.Cost < delivery.Cost))
                                    { delivery = delivery4; }

                                    // 3 4 1 2
                                    orderGeoIndex[0] = i3;
                                    orderGeoIndex[1] = i4;
                                    orderGeoIndex[2] = i1;
                                    orderGeoIndex[3] = i2;
                                    orders[0] = contextOrders[i3];
                                    orders[1] = contextOrders[i4];
                                    orders[2] = contextOrders[i1];
                                    orders[3] = contextOrders[i2];
                                    rcFind4 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 4, isLoop, geoData, out delivery4);
                                    if (rcFind4 == 0 && (delivery == null || delivery4.Cost < delivery.Cost))
                                    { delivery = delivery4; }

                                    // 3 4 2 1
                                    orderGeoIndex[0] = i3;
                                    orderGeoIndex[1] = i4;
                                    orderGeoIndex[2] = i2;
                                    orderGeoIndex[3] = i1;
                                    orders[0] = contextOrders[i3];
                                    orders[1] = contextOrders[i4];
                                    orders[2] = contextOrders[i2];
                                    orders[3] = contextOrders[i1];
                                    rcFind4 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 4, isLoop, geoData, out delivery4);
                                    if (rcFind4 == 0 && (delivery == null || delivery4.Cost < delivery.Cost))
                                    { delivery = delivery4; }

                                    // 4 1 2 3
                                    orderGeoIndex[0] = i4;
                                    orderGeoIndex[1] = i1;
                                    orderGeoIndex[2] = i2;
                                    orderGeoIndex[3] = i3;
                                    orders[0] = contextOrders[i4];
                                    orders[1] = contextOrders[i1];
                                    orders[2] = contextOrders[i2];
                                    orders[3] = contextOrders[i3];
                                    rcFind4 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 4, isLoop, geoData, out delivery4);
                                    if (rcFind4 == 0 && (delivery == null || delivery4.Cost < delivery.Cost))
                                    { delivery = delivery4; }

                                    // 4 1 3 2
                                    orderGeoIndex[0] = i4;
                                    orderGeoIndex[1] = i1;
                                    orderGeoIndex[2] = i3;
                                    orderGeoIndex[3] = i2;
                                    orders[0] = contextOrders[i4];
                                    orders[1] = contextOrders[i1];
                                    orders[2] = contextOrders[i3];
                                    orders[3] = contextOrders[i2];
                                    rcFind4 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 4, isLoop, geoData, out delivery4);
                                    if (rcFind4 == 0 && (delivery == null || delivery4.Cost < delivery.Cost))
                                    { delivery = delivery4; }

                                    // 4 2 1 3
                                    orderGeoIndex[0] = i4;
                                    orderGeoIndex[1] = i2;
                                    orderGeoIndex[2] = i1;
                                    orderGeoIndex[3] = i3;
                                    orders[0] = contextOrders[i4];
                                    orders[1] = contextOrders[i2];
                                    orders[2] = contextOrders[i1];
                                    orders[3] = contextOrders[i3];
                                    rcFind4 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 4, isLoop, geoData, out delivery4);
                                    if (rcFind4 == 0 && (delivery == null || delivery4.Cost < delivery.Cost))
                                    { delivery = delivery4; }

                                    // 4 2 3 1
                                    orderGeoIndex[0] = i4;
                                    orderGeoIndex[1] = i2;
                                    orderGeoIndex[2] = i3;
                                    orderGeoIndex[3] = i1;
                                    orders[0] = contextOrders[i4];
                                    orders[1] = contextOrders[i2];
                                    orders[2] = contextOrders[i3];
                                    orders[3] = contextOrders[i1];
                                    rcFind4 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 4, isLoop, geoData, out delivery4);
                                    if (rcFind4 == 0 && (delivery == null || delivery4.Cost < delivery.Cost))
                                    { delivery = delivery4; }

                                    // 4 3 1 2
                                    orderGeoIndex[0] = i4;
                                    orderGeoIndex[1] = i3;
                                    orderGeoIndex[2] = i1;
                                    orderGeoIndex[3] = i2;
                                    orders[0] = contextOrders[i4];
                                    orders[1] = contextOrders[i3];
                                    orders[2] = contextOrders[i1];
                                    orders[3] = contextOrders[i2];
                                    rcFind4 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 4, isLoop, geoData, out delivery4);
                                    if (rcFind4 == 0 && (delivery == null || delivery4.Cost < delivery.Cost))
                                    { delivery = delivery4; }

                                    // 4 3 2 1
                                    orderGeoIndex[0] = i4;
                                    orderGeoIndex[1] = i3;
                                    orderGeoIndex[2] = i2;
                                    orderGeoIndex[3] = i1;
                                    orders[0] = contextOrders[i4];
                                    orders[1] = contextOrders[i3];
                                    orders[2] = contextOrders[i2];
                                    orders[3] = contextOrders[i1];
                                    rcFind4 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 4, isLoop, geoData, out delivery4);
                                    if (rcFind4 == 0 && (delivery == null || delivery4.Cost < delivery.Cost))
                                    { delivery = delivery4; }

                                    if (delivery != null)
                                    { deliveries[count++] = delivery; }

                                    orderGeoIndex[5] = shopIndex;
                                    orderIndex[0] = i1;
                                    orderIndex[1] = i2;
                                    orderIndex[2] = i3;
                                    orderIndex[3] = i4;

                                    for (int i5 = i4 + 1; i5 < orderCount; i5++)
                                    {
                                        orderIndex[4] = i5;
                                        delivery = null;

                                        for (int i = 0; i < permutations5.Length; i += 5)
                                        {
                                            int p1 = orderIndex[permutations5[i]];
                                            int p2 = orderIndex[permutations5[i + 1]];
                                            int p3 = orderIndex[permutations5[i + 2]];
                                            int p4 = orderIndex[permutations5[i + 3]];
                                            int p5 = orderIndex[permutations5[i + 4]];

                                            orderGeoIndex[0] = p1;
                                            orderGeoIndex[1] = p2;
                                            orderGeoIndex[2] = p3;
                                            orderGeoIndex[3] = p4;
                                            orderGeoIndex[3] = p5;

                                            orders[0] = contextOrders[p1];
                                            orders[1] = contextOrders[p2];
                                            orders[2] = contextOrders[p3];
                                            orders[3] = contextOrders[p4];
                                            orders[4] = contextOrders[p5];

                                            int rcFind5 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 5, isLoop, geoData, out delivery5);
                                            if (rcFind5 == 0 && (delivery == null || delivery5.Cost < delivery.Cost))
                                            { delivery = delivery5; }
                                        }

                                        if (delivery != null)
                                        { deliveries[count++] = delivery; }
                                    }
                                }
                            }
                        }
                    }
                }

                if (count < deliveries.Length)
                { Array.Resize(ref deliveries, count); }

                context.Deliveries = deliveries;

                // 5. Выход - Ok
                rc = 0;
                return;
            }
            catch (Exception ex)
            {
#if debug
                Logger.WriteToLog(373, $"BuildEx5. startIndex = {context.StartOrderIndex}. rc = {rc}. order_count = {context.OrderCount}, shop_id = {context.ShopFrom.Id}, courier_id = {context.ShopCourier.Id}, level = {context.MaxRouteLength} Exception = {ex.Message}", 2);
#endif
                return;
            }
            finally
            {
                if (context != null)
                {
#if debug
                    Logger.WriteToLog(3090, $"BuildEx4 exit. rc = {rc}. vehicleID = {context.ShopCourier.VehicleID}. order_count = {context.OrderCount}, level = {context.MaxRouteLength}, startIndex = {context.StartOrderIndex}, step = {context.OrderIndexStep}", 0);
#endif
                    context.ExitCode = rc;
                    ManualResetEvent syncEvent = context.SyncEvent;
                    if (syncEvent != null)
                    {
                        syncEvent.Set();
                    }
                }
            }
        }

//        /// <summary>
//        /// Построение всех возможных отгрузок длины 4 для
//        /// заданного контекста и гео-данных
//        /// </summary>
//        /// <param name="status">Расширенный контекст</param>
//        /// <returns>0 - отгрузки построены; иначе - отгрузки не построены</returns>
//        public static void BuildEx8(object status)
//        {
//            // 1. Инициализация
//            int rc = 1;
//            ThreadContextEx context = status as ThreadContextEx;

//            try
//            {
//                // 2. Проверяем исходные данные
//                rc = 2;
//                if (context == null)
//                    return;
//                context.Deliveries = null;
//                Point[,] geoData = context.GeoData;
//                if (geoData == null)
//                    return;
//#if (debug)
//                Logger.WriteToLog(309, $"BuildEx8 enter. vehicleID = {context.ShopCourier.VehicleID}. order_count = {context.OrderCount}, level = {context.MaxRouteLength}, startIndex = {context.StartOrderIndex}, step = {context.OrderIndexStep}", 0);
//#endif
//                // 3. Извлекаем и проверяем данные из контекста
//                rc = 3;
//                int level = context.MaxRouteLength;
//                if (level < 1 || level > 8)
//                    return;
//                DateTime calcTime = context.CalcTime;
//                Order[] contextOrders = context.Orders;
//                if (contextOrders == null || contextOrders.Length <= 0)
//                    return;
//                int orderCount = contextOrders.Length;
//                Shop contextShop = context.ShopFrom;
//                if (contextShop == null)
//                    return;
//                Courier contextCourier = context.ShopCourier;
//                if (contextCourier == null)
//                    return;
//                int startIndex = context.StartOrderIndex;
//                if (startIndex < 0 || startIndex >= orderCount)
//                    return;
//                int step = context.OrderIndexStep;
//                if (step <= 0)
//                    return;

//                // 4. Выделяем память под результат
//                rc = 4;
//                long size = CalcCapacityEx(level, orderCount);
//                size = (size + step - 1) / step;
//                int sizeOfFloat = sizeof(float);
//                int itemSize = 1 + level + sizeOfFloat;
//                size *= itemSize;
//                if (size >= 0x7FFFFFFF)
//                    return;
//                byte[] result = new byte[size];
//                int ptr = 0;

//                // 5. Цикл перебора вариантов
//                rc = 5;
//                Order[] orders = new Order[8];
//                int[] orderGeoIndex = new int[9];
//                bool isLoop = !contextCourier.IsTaxi;
//                int shopIndex = orderCount;

//                // 4. Цикл выбора допустимых маршрутов
//                rc = 4;
//                CourierDeliveryInfo delivery;
//                byte b1;
//                byte b2;
//                byte b3;
//                byte b4;
//                byte b5;
//                byte b6;
//                byte b7;
//                byte b8;
//                bool[] alreadySelected = new bool[orderCount];
//                int offset;

//                for (int i1 = startIndex; i1 < orderCount; i1 += step)
//                {
//                    orderGeoIndex[0] = i1;
//                    orderGeoIndex[1] = shopIndex;
//                    orders[0] = contextOrders[i1];
//                    int rcFind = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 1, isLoop, geoData, out delivery);

//                    if (rcFind == 0)
//                    {
//                        b1 = (byte)i1;
//                        alreadySelected[i1] = true;

//                        // [f0][f1][f2][f3][1][b1]
//                        BitConverter.GetBytes((float)delivery.Cost).CopyTo(result, ptr);
//                        result[ptr + sizeOfFloat] = 1;
//                        result[ptr + sizeOfFloat + 1] = b1;
//                        ptr += itemSize;

//                        if (level >= 2)
//                        {
//                            for (int i2 = 0; i2 < orderCount; i2++)
//                            {
//                                if (alreadySelected[i2])
//                                    continue;

//                                orderGeoIndex[1] = i2;
//                                orderGeoIndex[2] = shopIndex;
//                                orders[1] = contextOrders[i2];
//                                rcFind = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 2, isLoop, geoData, out delivery);

//                                if (rcFind == 0)
//                                {
//                                    b2 = (byte)i2;
//                                    alreadySelected[i2] = true;

//                                    // [f0][f1][f2][f3][1][b1][b2]
//                                    BitConverter.GetBytes((float)delivery.Cost).CopyTo(result, ptr);
//                                    offset = ptr + sizeOfFloat;
//                                    result[offset] = 2;
//                                    result[offset + 1] = b1;
//                                    result[offset + 2] = b2;
//                                    ptr += itemSize;

//                                    if (level >= 3)
//                                    {
//                                        for (int i3 = 0; i3 < orderCount; i3++)
//                                        {
//                                            if (alreadySelected[i3])
//                                                continue;

//                                            orderGeoIndex[2] = i3;
//                                            orderGeoIndex[3] = shopIndex;
//                                            orders[2] = contextOrders[i3];
//                                            rcFind = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 3, isLoop, geoData, out delivery);

//                                            if (rcFind == 0)
//                                            {
//                                                b3 = (byte)i3;
//                                                alreadySelected[i3] = true;

//                                                // [f0][f1][f2][f3][1][b1][b2][b3]
//                                                BitConverter.GetBytes((float)delivery.Cost).CopyTo(result, ptr);
//                                                offset = ptr + sizeOfFloat;
//                                                result[offset] = 3;
//                                                result[offset + 1] = b1;
//                                                result[offset + 2] = b2;
//                                                result[offset + 3] = b3;
//                                                ptr += itemSize;

//                                                if (level >= 4)
//                                                {
//                                                    for (int i4 = 0; i4 < orderCount; i4++)
//                                                    {
//                                                        if (alreadySelected[i4])
//                                                            continue;

//                                                        orderGeoIndex[3] = i4;
//                                                        orderGeoIndex[4] = shopIndex;
//                                                        orders[3] = contextOrders[i4];
//                                                        rcFind = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 4, isLoop, geoData, out delivery);

//                                                        if (rcFind == 0)
//                                                        {
//                                                            b4 = (byte)i4;
//                                                            alreadySelected[i4] = true;

//                                                            // [f0][f1][f2][f3][1][b1][b2][b3][b4]
//                                                            BitConverter.GetBytes((float)delivery.Cost).CopyTo(result, ptr);
//                                                            offset = ptr + sizeOfFloat;
//                                                            result[offset] = 4;
//                                                            result[offset + 1] = b1;
//                                                            result[offset + 2] = b2;
//                                                            result[offset + 3] = b3;
//                                                            result[offset + 4] = b4;
//                                                            ptr += itemSize;

//                                                            if (level >= 5)
//                                                            {
//                                                                for (int i5 = 0; i5 < orderCount; i5++)
//                                                                {
//                                                                    if (alreadySelected[i5])
//                                                                        continue;

//                                                                    orderGeoIndex[4] = i5;
//                                                                    orderGeoIndex[5] = shopIndex;
//                                                                    orders[4] = contextOrders[i5];
//                                                                    rcFind = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 5, isLoop, geoData, out delivery);

//                                                                    if (rcFind == 0)
//                                                                    {
//                                                                        b5 = (byte)i5;
//                                                                        alreadySelected[i5] = true;

//                                                                        // [f0][f1][f2][f3][1][b1][b2][b3][b4][b5]
//                                                                        BitConverter.GetBytes((float)delivery.Cost).CopyTo(result, ptr);
//                                                                        offset = ptr + sizeOfFloat;
//                                                                        result[offset] = 5;
//                                                                        result[offset + 1] = b1;
//                                                                        result[offset + 2] = b2;
//                                                                        result[offset + 3] = b3;
//                                                                        result[offset + 4] = b4;
//                                                                        result[offset + 5] = b5;
//                                                                        ptr += itemSize;

//                                                                        if (level >= 6)
//                                                                        {
//                                                                            for (int i6 = 0; i6 < orderCount; i6++)
//                                                                            {
//                                                                                if (alreadySelected[i6])
//                                                                                    continue;

//                                                                                orderGeoIndex[5] = i6;
//                                                                                orderGeoIndex[6] = shopIndex;
//                                                                                orders[5] = contextOrders[i6];
//                                                                                rcFind = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 6, isLoop, geoData, out delivery);

//                                                                                if (rcFind == 0)
//                                                                                {
//                                                                                    b6 = (byte)i6;
//                                                                                    alreadySelected[i6] = true;

//                                                                                    // [f0][f1][f2][f3][1][b1][b2][b3][b4][b5][b6]
//                                                                                    BitConverter.GetBytes((float)delivery.Cost).CopyTo(result, ptr);
//                                                                                    offset = ptr + sizeOfFloat;
//                                                                                    result[offset] = 6;
//                                                                                    result[offset + 1] = b1;
//                                                                                    result[offset + 2] = b2;
//                                                                                    result[offset + 3] = b3;
//                                                                                    result[offset + 4] = b4;
//                                                                                    result[offset + 5] = b5;
//                                                                                    result[offset + 6] = b6;
//                                                                                    ptr += itemSize;

//                                                                                    if (level >= 7)
//                                                                                    {
//                                                                                        for (int i7 = 0; i7 < orderCount; i7++)
//                                                                                        {
//                                                                                            if (alreadySelected[i7])
//                                                                                                continue;

//                                                                                            orderGeoIndex[6] = i7;
//                                                                                            orderGeoIndex[7] = shopIndex;
//                                                                                            orders[6] = contextOrders[i7];
//                                                                                            rcFind = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 7, isLoop, geoData, out delivery);

//                                                                                            if (rcFind == 0)
//                                                                                            {
//                                                                                                b7 = (byte)i7;
//                                                                                                alreadySelected[i7] = true;

//                                                                                                // [f0][f1][f2][f3][1][b1][b2][b3][b4][b5][b6][b7]
//                                                                                                BitConverter.GetBytes((float)delivery.Cost).CopyTo(result, ptr);
//                                                                                                offset = ptr + sizeOfFloat;
//                                                                                                result[offset] = 7;
//                                                                                                result[offset + 1] = b1;
//                                                                                                result[offset + 2] = b2;
//                                                                                                result[offset + 3] = b3;
//                                                                                                result[offset + 4] = b4;
//                                                                                                result[offset + 5] = b5;
//                                                                                                result[offset + 6] = b6;
//                                                                                                result[offset + 7] = b7;
//                                                                                                ptr += itemSize;

//                                                                                                if (level >= 8)
//                                                                                                {
//                                                                                                    for (int i8 = 0; i8 < orderCount; i8++)
//                                                                                                    {
//                                                                                                        if (alreadySelected[i8])
//                                                                                                            continue;

//                                                                                                        orderGeoIndex[7] = i8;
//                                                                                                        orderGeoIndex[8] = shopIndex;
//                                                                                                        orders[7] = contextOrders[i8];
//                                                                                                        rcFind = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 8, isLoop, geoData, out delivery);

//                                                                                                        if (rcFind == 0)
//                                                                                                        {
//                                                                                                            b8 = (byte)i8;
//                                                                                                            //alreadySelected[i8] = true;

//                                                                                                            // [f0][f1][f2][f3][1][b1][b2][b3][b4][b5][b6][b7][b8]
//                                                                                                            BitConverter.GetBytes((float)delivery.Cost).CopyTo(result, ptr);
//                                                                                                            offset = ptr + sizeOfFloat;
//                                                                                                            result[offset] = 8;
//                                                                                                            result[offset + 1] = b1;
//                                                                                                            result[offset + 2] = b2;
//                                                                                                            result[offset + 3] = b3;
//                                                                                                            result[offset + 4] = b4;
//                                                                                                            result[offset + 5] = b5;
//                                                                                                            result[offset + 6] = b6;
//                                                                                                            result[offset + 7] = b7;
//                                                                                                            result[offset + 8] = b8;
//                                                                                                            ptr += itemSize;

//                                                                                                            //alreadySelected[i8] = false;
//                                                                                                        }
//                                                                                                    }
//                                                                                                }

//                                                                                                alreadySelected[i7] = false;
//                                                                                            }
//                                                                                        }
//                                                                                    }

//                                                                                    alreadySelected[i6] = false;
//                                                                                }
//                                                                            }
//                                                                        }

//                                                                        alreadySelected[i5] = false;
//                                                                    }
//                                                                }
//                                                            }

//                                                            alreadySelected[i4] = false;
//                                                        }
//                                                    }
//                                                }

//                                                alreadySelected[i3] = false;
//                                            }
//                                        }
//                                    }

//                                    alreadySelected[i2] = false;
//                                }
//                            }
//                        }

//                        alreadySelected[i1] = false;
//                    }
//                }

//                if (ptr < result.Length)
//                { Array.Resize(ref result, ptr); }

//                context.Items = result;
//                context.ItemCount = ptr / itemSize;

//                // 5. Выход - Ok
//                rc = 0;
//                return;
//            }
//            catch (Exception ex)
//            {
//#if debug
//                Logger.WriteToLog(373, $"BuildEx8. startIndex = {context.StartOrderIndex}. rc = {rc}. order_count = {context.OrderCount}, shop_id = {context.ShopFrom.Id}, courier_id = {context.ShopCourier.Id}, level = {context.MaxRouteLength} Exception = {ex.Message}", 2);
//#endif
//                return;
//            }
//            finally
//            {
//                if (context != null)
//                {
//#if debug
//                    Logger.WriteToLog(3090, $"BuildEx8 exit. rc = {rc}. vehicleID = {context.ShopCourier.VehicleID}. order_count = {context.OrderCount}, level = {context.MaxRouteLength}, startIndex = {context.StartOrderIndex}, step = {context.OrderIndexStep}, delivery_count = {context.ItemCount}", 0);
//#endif
//                    context.ExitCode = rc;
//                    ManualResetEvent syncEvent = context.SyncEvent;
//                    if (syncEvent != null)
//                    {
//                        syncEvent.Set();
//                    }
//                }
//            }
//        }

        /// <summary>
        /// Построение всех возможных отгрузок длины 8 для
        /// заданного контекста и гео-данных
        /// </summary>
        /// <param name="status">Расширенный контекст</param>
        /// <returns>0 - отгрузки построены; иначе - отгрузки не построены</returns>
        public static void BuildEx8(object status)
        {
            // 1. Инициализация
            int rc = 1;
            ThreadContextEx context = status as ThreadContextEx;
            #if (debug)
                        System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
                        sw.Start();
                        DateTime startTime = DateTime.Now;
            #endif

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
#if (debug)
                Logger.WriteToLog(309, $"BuildEx8 enter. vehicleID = {context.ShopCourier.VehicleID}. order_count = {context.OrderCount}, level = {context.MaxRouteLength}, startIndex = {context.StartOrderIndex}, step = {context.OrderIndexStep}", 0);
#endif
                // 3. Извлекаем и проверяем данные из контекста
                rc = 3;
                int level = context.MaxRouteLength;
                if (level != 8)
                    return;
                DateTime calcTime = context.CalcTime;
                Order[] contextOrders = context.Orders;
                if (contextOrders == null || contextOrders.Length <= 0)
                    return;
                int orderCount = contextOrders.Length;
                if (orderCount > 24)
                    return;

                Shop contextShop = context.ShopFrom;
                if (contextShop == null)
                    return;
                Courier contextCourier = context.ShopCourier;
                if (contextCourier == null)
                    return;
                int startIndex = context.StartOrderIndex;
                if (startIndex < 0 || startIndex >= orderCount)
                    return;
                int step = context.OrderIndexStep;
                if (step <= 0)
                    return;

                // 4. Выделяем память под результат
                rc = 4;
                long size = 1 << orderCount;
                CourierDeliveryInfo[] result = new CourierDeliveryInfo[size];

                // 5. Цикл перебора вариантов
                rc = 5;
                Order[] orders = new Order[8];
                int[] orderGeoIndex = new int[9];
                bool isLoop = !contextCourier.IsTaxi;
                int shopIndex = orderCount;

                // 6. Цикл выбора допустимых маршрутов
                rc = 6;
                CourierDeliveryInfo delivery;
                int key = 0;
                bool[] alreadySelected = new bool[orderCount];
                int k1;
                int k2;
                int k3;
                int k4;
                int k5;
                int k6;
                int k7;
                int k8;

                for (int i1 = startIndex; i1 < orderCount; i1 += step)
                {
                    orderGeoIndex[0] = i1;
                    orderGeoIndex[1] = shopIndex;
                    orders[0] = contextOrders[i1];
                    int rcFind = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 1, isLoop, geoData, out delivery);

                    if (rcFind == 0)
                    {
                        alreadySelected[i1] = true;
                        k1 = 1 << i1;
                        key |= k1;

                        result[key] = delivery;

                        for (int i2 = 0; i2 < orderCount; i2++)
                        {
                            if (alreadySelected[i2])
                                continue;

                            orderGeoIndex[1] = i2;
                            orderGeoIndex[2] = shopIndex;
                            orders[1] = contextOrders[i2];
                            rcFind = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 2, isLoop, geoData, out delivery);

                            if (rcFind == 0)
                            {
                                alreadySelected[i2] = true;
                                k2 = 1 << i2;
                                key |= k2;

                                if (result[key] == null || delivery.Cost < result[key].Cost)
                                { result[key] = delivery; }

                                for (int i3 = 0; i3 < orderCount; i3++)
                                {
                                    if (alreadySelected[i3])
                                        continue;

                                    orderGeoIndex[2] = i3;
                                    orderGeoIndex[3] = shopIndex;
                                    orders[2] = contextOrders[i3];
                                    rcFind = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 3, isLoop, geoData, out delivery);

                                    if (rcFind == 0)
                                    {
                                        alreadySelected[i3] = true;
                                        k3 = 1 << i3;
                                        key |= k3;

                                        if (result[key] == null || delivery.Cost < result[key].Cost)
                                        { result[key] = delivery; }

                                        for (int i4 = 0; i4 < orderCount; i4++)
                                        {
                                            if (alreadySelected[i4])
                                                continue;

                                            orderGeoIndex[3] = i4;
                                            orderGeoIndex[4] = shopIndex;
                                            orders[3] = contextOrders[i4];
                                            rcFind = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 4, isLoop, geoData, out delivery);

                                            if (rcFind == 0)
                                            {
                                                alreadySelected[i4] = true;
                                                k4 = 1 << i4;
                                                key |= k4;
                                                if (result[key] == null || delivery.Cost < result[key].Cost)
                                                { result[key] = delivery; }

                                                for (int i5 = 0; i5 < orderCount; i5++)
                                                {
                                                    if (alreadySelected[i5])
                                                        continue;

                                                    orderGeoIndex[4] = i5;
                                                    orderGeoIndex[5] = shopIndex;
                                                    orders[4] = contextOrders[i5];
                                                    rcFind = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 5, isLoop, geoData, out delivery);

                                                    if (rcFind == 0)
                                                    {
                                                        alreadySelected[i5] = true;
                                                        k5 = 1 << i5;
                                                        key |= k5;
                                                        if (result[key] == null || delivery.Cost < result[key].Cost)
                                                        { result[key] = delivery; }

                                                        for (int i6 = 0; i6 < orderCount; i6++)
                                                        {
                                                            if (alreadySelected[i6])
                                                                continue;

                                                            orderGeoIndex[5] = i6;
                                                            orderGeoIndex[6] = shopIndex;
                                                            orders[5] = contextOrders[i6];
                                                            rcFind = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 6, isLoop, geoData, out delivery);

                                                            if (rcFind == 0)
                                                            {
                                                                alreadySelected[i6] = true;
                                                                k6 = 1 << i6;
                                                                key |= k6;
                                                                if (result[key] == null || delivery.Cost < result[key].Cost)
                                                                { result[key] = delivery; }

                                                                for (int i7 = 0; i7 < orderCount; i7++)
                                                                {
                                                                    if (alreadySelected[i7])
                                                                        continue;

                                                                    orderGeoIndex[6] = i7;
                                                                    orderGeoIndex[7] = shopIndex;
                                                                    orders[6] = contextOrders[i7];
                                                                    rcFind = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 7, isLoop, geoData, out delivery);

                                                                    if (rcFind == 0)
                                                                    {
                                                                        alreadySelected[i7] = true;
                                                                        k7 = 1 << i7;
                                                                        key |= k7;
                                                                        if (result[key] == null || delivery.Cost < result[key].Cost)
                                                                        { result[key] = delivery; }

                                                                        for (int i8 = 0; i8 < orderCount; i8++)
                                                                        {
                                                                            if (alreadySelected[i8])
                                                                                continue;

                                                                            orderGeoIndex[7] = i8;
                                                                            orderGeoIndex[8] = shopIndex;
                                                                            orders[7] = contextOrders[i8];
                                                                            rcFind = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 8, isLoop, geoData, out delivery);

                                                                            if (rcFind == 0)
                                                                            {
                                                                                k8 = key | (1 << i8);
                                                                                if (result[k8] == null || delivery.Cost < result[k8].Cost)
                                                                                { result[k8] = delivery; }
                                                                            }
                                                                        }

                                                                        alreadySelected[i7] = false;
                                                                        key ^= k7;
                                                                    }
                                                                }

                                                                alreadySelected[i6] = false;
                                                                key ^= k6;
                                                            }
                                                        }

                                                        alreadySelected[i5] = false;
                                                        key ^= k5;
                                                    }
                                                }

                                                alreadySelected[i4] = false;
                                                key ^= k4;
                                            }
                                        }

                                        alreadySelected[i3] = false;
                                        key ^= k3;
                                    }
                                }

                                alreadySelected[i2] = false;
                                key ^= k2;
                            }
                        }

                        alreadySelected[i1] = false;
                        key ^= k1;
                    }
                }

                // 7. Сжатие
                rc = 7;
                key = 0;
                for (int i = 0; i < result.Length; i++)
                {
                    delivery = result[i];
                    if (delivery != null)
                    { result[key++] = delivery; }
                }

                if (key < result.Length)
                {
                    Array.Resize(ref result, key);
                }

                context.Deliveries = result;

                // 8. Выход - Ok
                rc = 0;
                return;
            }
            catch (Exception ex)
            {
#if debug
                Logger.WriteToLog(373, $"BuildEx8. startIndex = {context.StartOrderIndex}. rc = {rc}. order_count = {context.OrderCount}, shop_id = {context.ShopFrom.Id}, courier_id = {context.ShopCourier.Id}, level = {context.MaxRouteLength} Exception = {ex.Message}", 2);
#endif
                return;
            }
            finally
            {
                if (context != null)
                {
#if debug
                    //Logger.WriteToLog(3090, $"BuildEx8 exit. rc = {rc}. vehicleID = {context.ShopCourier.VehicleID}. order_count = {context.OrderCount}, level = {context.MaxRouteLength}, startIndex = {context.StartOrderIndex}, step = {context.OrderIndexStep}, delivery_count = {context.ItemCount}", 0);
                    Logger.WriteToLog(3090, $"BuildEx8 exit ({startTime.ToString("yyyy-MM-dd HH:mm:ss.fff")} ET = {sw.ElapsedMilliseconds}). vehicleID = {context.ShopCourier.VehicleID}. order_count = {context.OrderCount}. startIndex = {context.StartOrderIndex}. step = {context.OrderIndexStep}, delivery_count = {context.DeliveryCount}", 0);
#endif
                    context.ExitCode = rc;
                    ManualResetEvent syncEvent = context.SyncEvent;
                    if (syncEvent != null)
                    {
                        syncEvent.Set();
                    }
                }
            }
        }

        /// <summary>
        /// Построение всех возможных отгрузок длины 7 для
        /// заданного контекста и гео-данных
        /// </summary>
        /// <param name="status">Расширенный контекст</param>
        /// <returns>0 - отгрузки построены; иначе - отгрузки не построены</returns>
        public static void BuildEx7(object status)
        {
            // 1. Инициализация
            int rc = 1;
            ThreadContextEx context = status as ThreadContextEx;
            #if (debug)
                        System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
                        sw.Start();
                        DateTime startTime = DateTime.Now;
            #endif

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
#if (debug)
                Logger.WriteToLog(309, $"BuildEx7 enter. vehicleID = {context.ShopCourier.VehicleID}. order_count = {context.OrderCount}, level = {context.MaxRouteLength}, startIndex = {context.StartOrderIndex}, step = {context.OrderIndexStep}", 0);
#endif
                // 3. Извлекаем и проверяем данные из контекста
                rc = 3;
                int level = context.MaxRouteLength;
                if (level != 7)
                    return;
                DateTime calcTime = context.CalcTime;
                Order[] contextOrders = context.Orders;
                if (contextOrders == null || contextOrders.Length <= 0)
                    return;
                int orderCount = contextOrders.Length;
                if (orderCount > 24)
                    return;

                Shop contextShop = context.ShopFrom;
                if (contextShop == null)
                    return;
                Courier contextCourier = context.ShopCourier;
                if (contextCourier == null)
                    return;
                int startIndex = context.StartOrderIndex;
                if (startIndex < 0 || startIndex >= orderCount)
                    return;
                int step = context.OrderIndexStep;
                if (step <= 0)
                    return;

                // 4. Выделяем память под результат
                rc = 4;
                long size = 1 << orderCount;
                CourierDeliveryInfo[] result = new CourierDeliveryInfo[size];

                // 5. Цикл перебора вариантов
                rc = 5;
                Order[] orders = new Order[8];
                int[] orderGeoIndex = new int[9];
                bool isLoop = !contextCourier.IsTaxi;
                int shopIndex = orderCount;

                // 6. Цикл выбора допустимых маршрутов
                rc = 6;
                CourierDeliveryInfo delivery;
                int key = 0;
                bool[] alreadySelected = new bool[orderCount];
                int k1;
                int k2;
                int k3;
                int k4;
                int k5;
                int k6;
                int k7;

                for (int i1 = startIndex; i1 < orderCount; i1 += step)
                {
                    orderGeoIndex[0] = i1;
                    orderGeoIndex[1] = shopIndex;
                    orders[0] = contextOrders[i1];
                    int rcFind = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 1, isLoop, geoData, out delivery);

                    if (rcFind == 0)
                    {
                        alreadySelected[i1] = true;
                        k1 = 1 << i1;
                        key |= k1;

                        result[key] = delivery;

                        for (int i2 = 0; i2 < orderCount; i2++)
                        {
                            if (alreadySelected[i2])
                                continue;

                            orderGeoIndex[1] = i2;
                            orderGeoIndex[2] = shopIndex;
                            orders[1] = contextOrders[i2];
                            rcFind = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 2, isLoop, geoData, out delivery);

                            if (rcFind == 0)
                            {
                                alreadySelected[i2] = true;
                                k2 = 1 << i2;
                                key |= k2;

                                if (result[key] == null || delivery.Cost < result[key].Cost)
                                { result[key] = delivery; }

                                for (int i3 = 0; i3 < orderCount; i3++)
                                {
                                    if (alreadySelected[i3])
                                        continue;

                                    orderGeoIndex[2] = i3;
                                    orderGeoIndex[3] = shopIndex;
                                    orders[2] = contextOrders[i3];
                                    rcFind = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 3, isLoop, geoData, out delivery);

                                    if (rcFind == 0)
                                    {
                                        alreadySelected[i3] = true;
                                        k3 = 1 << i3;
                                        key |= k3;

                                        if (result[key] == null || delivery.Cost < result[key].Cost)
                                        { result[key] = delivery; }

                                        for (int i4 = 0; i4 < orderCount; i4++)
                                        {
                                            if (alreadySelected[i4])
                                                continue;

                                            orderGeoIndex[3] = i4;
                                            orderGeoIndex[4] = shopIndex;
                                            orders[3] = contextOrders[i4];
                                            rcFind = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 4, isLoop, geoData, out delivery);

                                            if (rcFind == 0)
                                            {
                                                alreadySelected[i4] = true;
                                                k4 = 1 << i4;
                                                key |= k4;
                                                if (result[key] == null || delivery.Cost < result[key].Cost)
                                                { result[key] = delivery; }

                                                for (int i5 = 0; i5 < orderCount; i5++)
                                                {
                                                    if (alreadySelected[i5])
                                                        continue;

                                                    orderGeoIndex[4] = i5;
                                                    orderGeoIndex[5] = shopIndex;
                                                    orders[4] = contextOrders[i5];
                                                    rcFind = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 5, isLoop, geoData, out delivery);

                                                    if (rcFind == 0)
                                                    {
                                                        alreadySelected[i5] = true;
                                                        k5 = 1 << i5;
                                                        key |= k5;
                                                        if (result[key] == null || delivery.Cost < result[key].Cost)
                                                        { result[key] = delivery; }

                                                        for (int i6 = 0; i6 < orderCount; i6++)
                                                        {
                                                            if (alreadySelected[i6])
                                                                continue;

                                                            orderGeoIndex[5] = i6;
                                                            orderGeoIndex[6] = shopIndex;
                                                            orders[5] = contextOrders[i6];
                                                            rcFind = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 6, isLoop, geoData, out delivery);

                                                            if (rcFind == 0)
                                                            {
                                                                alreadySelected[i6] = true;
                                                                k6 = 1 << i6;
                                                                key |= k6;
                                                                if (result[key] == null || delivery.Cost < result[key].Cost)
                                                                { result[key] = delivery; }

                                                                for (int i7 = 0; i7 < orderCount; i7++)
                                                                {
                                                                    if (alreadySelected[i7])
                                                                        continue;

                                                                    orderGeoIndex[6] = i7;
                                                                    orderGeoIndex[7] = shopIndex;
                                                                    orders[6] = contextOrders[i7];
                                                                    rcFind = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 7, isLoop, geoData, out delivery);

                                                                    if (rcFind == 0)
                                                                    {
                                                                        k7 = key | (1 << i7);
                                                                        if (result[k7] == null || delivery.Cost < result[k7].Cost)
                                                                        { result[k7] = delivery; }
                                                                    }
                                                                }

                                                                alreadySelected[i6] = false;
                                                                key ^= k6;
                                                            }
                                                        }

                                                        alreadySelected[i5] = false;
                                                        key ^= k5;
                                                    }
                                                }

                                                alreadySelected[i4] = false;
                                                key ^= k4;
                                            }
                                        }

                                        alreadySelected[i3] = false;
                                        key ^= k3;
                                    }
                                }

                                alreadySelected[i2] = false;
                                key ^= k2;
                            }
                        }

                        alreadySelected[i1] = false;
                        key ^= k1;
                    }
                }

                // 7. Сжатие
                rc = 7;
                key = 0;
                for (int i = 0; i < result.Length; i++)
                {
                    delivery = result[i];
                    if (delivery != null)
                    { result[key++] = delivery; }
                }

                if (key < result.Length)
                {
                    Array.Resize(ref result, key);
                }

                context.Deliveries = result;

                // 8. Выход - Ok
                rc = 0;
                return;
            }
            catch (Exception ex)
            {
#if debug
                Logger.WriteToLog(373, $"BuildEx7. startIndex = {context.StartOrderIndex}. rc = {rc}. order_count = {context.OrderCount}, shop_id = {context.ShopFrom.Id}, courier_id = {context.ShopCourier.Id}, level = {context.MaxRouteLength} Exception = {ex.Message}", 2);
#endif
                return;
            }
            finally
            {
                if (context != null)
                {
#if debug
                    //Logger.WriteToLog(3090, $"BuildEx8 exit. rc = {rc}. vehicleID = {context.ShopCourier.VehicleID}. order_count = {context.OrderCount}, level = {context.MaxRouteLength}, startIndex = {context.StartOrderIndex}, step = {context.OrderIndexStep}, delivery_count = {context.ItemCount}", 0);
                    Logger.WriteToLog(3090, $"BuildEx7 exit ({startTime.ToString("yyyy-MM-dd HH:mm:ss.fff")} ET = {sw.ElapsedMilliseconds}). vehicleID = {context.ShopCourier.VehicleID}. order_count = {context.OrderCount}. startIndex = {context.StartOrderIndex}. step = {context.OrderIndexStep}, delivery_count = {context.DeliveryCount}", 0);
#endif
                    context.ExitCode = rc;
                    ManualResetEvent syncEvent = context.SyncEvent;
                    if (syncEvent != null)
                    {
                        syncEvent.Set();
                    }
                }
            }
        }

        /// <summary>
        /// Построение всех возможных отгрузок длины 6 для
        /// заданного контекста и гео-данных
        /// </summary>
        /// <param name="status">Расширенный контекст</param>
        /// <returns>0 - отгрузки построены; иначе - отгрузки не построены</returns>
        public static void BuildEx6(object status)
        {
            // 1. Инициализация
            int rc = 1;
            ThreadContextEx context = status as ThreadContextEx;
            #if (debug)
                        System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
                        sw.Start();
                        DateTime startTime = DateTime.Now;
            #endif

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
#if (debug)
                Logger.WriteToLog(309, $"BuildEx6 enter. vehicleID = {context.ShopCourier.VehicleID}. order_count = {context.OrderCount}, level = {context.MaxRouteLength}, startIndex = {context.StartOrderIndex}, step = {context.OrderIndexStep}", 0);
#endif
                // 3. Извлекаем и проверяем данные из контекста
                rc = 3;
                int level = context.MaxRouteLength;
                if (level != 6)
                    return;
                DateTime calcTime = context.CalcTime;
                Order[] contextOrders = context.Orders;
                if (contextOrders == null || contextOrders.Length <= 0)
                    return;
                int orderCount = contextOrders.Length;
                if (orderCount > 24)
                    return;

                Shop contextShop = context.ShopFrom;
                if (contextShop == null)
                    return;
                Courier contextCourier = context.ShopCourier;
                if (contextCourier == null)
                    return;
                int startIndex = context.StartOrderIndex;
                if (startIndex < 0 || startIndex >= orderCount)
                    return;
                int step = context.OrderIndexStep;
                if (step <= 0)
                    return;

                // 4. Выделяем память под результат
                rc = 4;
                long size = 1 << orderCount;
                CourierDeliveryInfo[] result = new CourierDeliveryInfo[size];

                // 5. Цикл перебора вариантов
                rc = 5;
                Order[] orders = new Order[8];
                int[] orderGeoIndex = new int[9];
                bool isLoop = !contextCourier.IsTaxi;
                int shopIndex = orderCount;

                // 6. Цикл выбора допустимых маршрутов
                rc = 6;
                CourierDeliveryInfo delivery;
                int key = 0;
                bool[] alreadySelected = new bool[orderCount];
                int k1;
                int k2;
                int k3;
                int k4;
                int k5;
                int k6;

                for (int i1 = startIndex; i1 < orderCount; i1 += step)
                {
                    orderGeoIndex[0] = i1;
                    orderGeoIndex[1] = shopIndex;
                    orders[0] = contextOrders[i1];
                    int rcFind = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 1, isLoop, geoData, out delivery);

                    if (rcFind == 0)
                    {
                        alreadySelected[i1] = true;
                        k1 = 1 << i1;
                        key |= k1;

                        result[key] = delivery;

                        for (int i2 = 0; i2 < orderCount; i2++)
                        {
                            if (alreadySelected[i2])
                                continue;

                            orderGeoIndex[1] = i2;
                            orderGeoIndex[2] = shopIndex;
                            orders[1] = contextOrders[i2];
                            rcFind = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 2, isLoop, geoData, out delivery);

                            if (rcFind == 0)
                            {
                                alreadySelected[i2] = true;
                                k2 = 1 << i2;
                                key |= k2;

                                if (result[key] == null || delivery.Cost < result[key].Cost)
                                { result[key] = delivery; }

                                for (int i3 = 0; i3 < orderCount; i3++)
                                {
                                    if (alreadySelected[i3])
                                        continue;

                                    orderGeoIndex[2] = i3;
                                    orderGeoIndex[3] = shopIndex;
                                    orders[2] = contextOrders[i3];
                                    rcFind = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 3, isLoop, geoData, out delivery);

                                    if (rcFind == 0)
                                    {
                                        alreadySelected[i3] = true;
                                        k3 = 1 << i3;
                                        key |= k3;

                                        if (result[key] == null || delivery.Cost < result[key].Cost)
                                        { result[key] = delivery; }

                                        for (int i4 = 0; i4 < orderCount; i4++)
                                        {
                                            if (alreadySelected[i4])
                                                continue;

                                            orderGeoIndex[3] = i4;
                                            orderGeoIndex[4] = shopIndex;
                                            orders[3] = contextOrders[i4];
                                            rcFind = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 4, isLoop, geoData, out delivery);

                                            if (rcFind == 0)
                                            {
                                                alreadySelected[i4] = true;
                                                k4 = 1 << i4;
                                                key |= k4;
                                                if (result[key] == null || delivery.Cost < result[key].Cost)
                                                { result[key] = delivery; }

                                                for (int i5 = 0; i5 < orderCount; i5++)
                                                {
                                                    if (alreadySelected[i5])
                                                        continue;

                                                    orderGeoIndex[4] = i5;
                                                    orderGeoIndex[5] = shopIndex;
                                                    orders[4] = contextOrders[i5];
                                                    rcFind = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 5, isLoop, geoData, out delivery);

                                                    if (rcFind == 0)
                                                    {
                                                        alreadySelected[i5] = true;
                                                        k5 = 1 << i5;
                                                        key |= k5;
                                                        if (result[key] == null || delivery.Cost < result[key].Cost)
                                                        { result[key] = delivery; }

                                                        for (int i6 = 0; i6 < orderCount; i6++)
                                                        {
                                                            if (alreadySelected[i6])
                                                                continue;

                                                            orderGeoIndex[5] = i6;
                                                            orderGeoIndex[6] = shopIndex;
                                                            orders[5] = contextOrders[i6];
                                                            rcFind = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 6, isLoop, geoData, out delivery);

                                                            if (rcFind == 0)
                                                            {
                                                                k6 = key | (1 << i6);
                                                                if (result[k6] == null || delivery.Cost < result[k6].Cost)
                                                                { result[k6] = delivery; }
                                                            }
                                                        }

                                                        alreadySelected[i5] = false;
                                                        key ^= k5;
                                                    }
                                                }

                                                alreadySelected[i4] = false;
                                                key ^= k4;
                                            }
                                        }

                                        alreadySelected[i3] = false;
                                        key ^= k3;
                                    }
                                }

                                alreadySelected[i2] = false;
                                key ^= k2;
                            }
                        }

                        alreadySelected[i1] = false;
                        key ^= k1;
                    }
                }

                // 7. Сжатие
                rc = 7;
                key = 0;
                for (int i = 0; i < result.Length; i++)
                {
                    delivery = result[i];
                    if (delivery != null)
                    { result[key++] = delivery; }
                }

                if (key < result.Length)
                {
                    Array.Resize(ref result, key);
                }

                context.Deliveries = result;

                // 8. Выход - Ok
                rc = 0;
                return;
            }
            catch (Exception ex)
            {
#if debug
                Logger.WriteToLog(373, $"BuildEx6. startIndex = {context.StartOrderIndex}. rc = {rc}. order_count = {context.OrderCount}, shop_id = {context.ShopFrom.Id}, courier_id = {context.ShopCourier.Id}, level = {context.MaxRouteLength} Exception = {ex.Message}", 2);
#endif
                return;
            }
            finally
            {
                if (context != null)
                {
#if debug
                    //Logger.WriteToLog(3090, $"BuildEx8 exit. rc = {rc}. vehicleID = {context.ShopCourier.VehicleID}. order_count = {context.OrderCount}, level = {context.MaxRouteLength}, startIndex = {context.StartOrderIndex}, step = {context.OrderIndexStep}, delivery_count = {context.ItemCount}", 0);
                    Logger.WriteToLog(3090, $"BuildEx6 exit ({startTime.ToString("yyyy-MM-dd HH:mm:ss.fff")} ET = {sw.ElapsedMilliseconds}). vehicleID = {context.ShopCourier.VehicleID}. order_count = {context.OrderCount}. startIndex = {context.StartOrderIndex}. step = {context.OrderIndexStep}, delivery_count = {context.DeliveryCount}", 0);
#endif
                    context.ExitCode = rc;
                    ManualResetEvent syncEvent = context.SyncEvent;
                    if (syncEvent != null)
                    {
                        syncEvent.Set();
                    }
                }
            }
        }

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
        /// Вычисление приближенного числа
        /// ожидаемых отгрузок
        /// </summary>
        /// <param name="level">Максимальная длина маршрута</param>
        /// <param name="orderCount">Количество заказов</param>
        /// <returns>Количество отгрузок</returns>
        private static long CalcCapacityEx(int level, int orderCount)
        {
            if (level < 1)
            { level = 1; }
            else if (level > 8)
            { level = 8; }

            long sum = 0;
            long mult = 1;

            for (int i = orderCount; i >= 0 && i > orderCount - level; i--)
            {
                mult *= i;
                sum += mult;
            }

            return sum;
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
