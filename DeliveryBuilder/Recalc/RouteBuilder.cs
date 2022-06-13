
namespace DeliveryBuilder.Recalc
{
    using DeliveryBuilder.Couriers;
    using DeliveryBuilder.Log;
    using DeliveryBuilder.Orders;
    using DeliveryBuilder.Geo;
    using DeliveryBuilder.Shops;
    using System;
    using System.Threading;
    using System.Diagnostics;
    using System.Text;

    /// <summary>
    /// Построитель отгрузок
    /// </summary>
    public class RouteBuilder
    {
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
            Stopwatch sw = new Stopwatch();
            sw.Start();
            int rc = 1;
            ThreadContextEx context = status as ThreadContextEx;
            int count = 0;

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
                Logger.WriteToLog(114, MessageSeverity.Info, string.Format(Messages.MSG_114, context.ShopCourier.VehicleID, context.OrderCount, context.MaxRouteLength, context.StartOrderIndex, context.OrderIndexStep));

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
                Logger.WriteToLog(669, MessageSeverity.Error, string.Format(Messages.MSG_669, $"{nameof(RouteBuilder)}.{nameof(RouteBuilder.BuildEx2)}", rc, (ex.InnerException == null ? ex.Message : ex.InnerException.Message)));
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
                    Logger.WriteToLog(111, MessageSeverity.Info, string.Format(Messages.MSG_111, rc, context.ShopCourier.VehicleID, context.OrderCount, context.MaxRouteLength, context.StartOrderIndex, context.OrderIndexStep, sw.ElapsedMilliseconds, count));
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
            Stopwatch sw = new Stopwatch();
            sw.Start();
            int rc = 1;
            ThreadContextEx context = status as ThreadContextEx;
            int count = 0;

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
                Logger.WriteToLog(108, MessageSeverity.Info, string.Format(Messages.MSG_108, context.ShopCourier.VehicleID, context.OrderCount, context.MaxRouteLength, context.StartOrderIndex, context.OrderIndexStep));

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
                Logger.WriteToLog(669, MessageSeverity.Error, string.Format(Messages.MSG_669, $"{nameof(RouteBuilder)}.{nameof(RouteBuilder.BuildEx3)}", rc, (ex.InnerException == null ? ex.Message : ex.InnerException.Message)));
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

                    Logger.WriteToLog(109, MessageSeverity.Info, string.Format(Messages.MSG_105, rc, context.ShopCourier.VehicleID, context.OrderCount, context.MaxRouteLength, context.StartOrderIndex, context.OrderIndexStep, sw.ElapsedMilliseconds, count));
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
            Stopwatch sw = new Stopwatch();
            sw.Start();
            int rc = 1;
            ThreadContextEx context = status as ThreadContextEx;
            int count = 0;

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
                Logger.WriteToLog(106, MessageSeverity.Info, string.Format(Messages.MSG_106, context.ShopCourier.VehicleID, context.OrderCount, context.MaxRouteLength, context.StartOrderIndex, context.OrderIndexStep));

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
                Logger.WriteToLog(669, MessageSeverity.Error, string.Format(Messages.MSG_669, $"{nameof(RouteBuilder)}.{nameof(RouteBuilder.BuildEx4)}", rc, (ex.InnerException == null ? ex.Message : ex.InnerException.Message)));
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

                    Logger.WriteToLog(107, MessageSeverity.Info, string.Format(Messages.MSG_107, rc, context.ShopCourier.VehicleID, context.OrderCount, context.MaxRouteLength, context.StartOrderIndex, context.OrderIndexStep, sw.ElapsedMilliseconds, count));
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
            Stopwatch sw = new Stopwatch();
            sw.Start();
            int rc = 1;
            ThreadContextEx context = status as ThreadContextEx;
            int count = 0;

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
                Logger.WriteToLog(104, MessageSeverity.Info, string.Format(Messages.MSG_104, context.ShopCourier.VehicleID, context.OrderCount, context.MaxRouteLength, context.StartOrderIndex, context.OrderIndexStep));

                // 3. Извлекаем и проверяем данные из контекста
                rc = 3;
                int level = context.MaxRouteLength;
                if (level != 5)
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
                byte[] permutations5 = Permutations.Generate(5);

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
                                            orderGeoIndex[4] = p5;

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
                                            orderGeoIndex[4] = p5;

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
                Logger.WriteToLog(669, MessageSeverity.Error, string.Format(Messages.MSG_669, $"{nameof(RouteBuilder)}.{nameof(RouteBuilder.BuildEx5)}", rc, (ex.InnerException == null ? ex.Message : ex.InnerException.Message)));
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

                    Logger.WriteToLog(105, MessageSeverity.Info, string.Format(Messages.MSG_105, rc, context.ShopCourier.VehicleID, context.OrderCount, context.MaxRouteLength, context.StartOrderIndex, context.OrderIndexStep, sw.ElapsedMilliseconds, count));
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
            Stopwatch sw = new Stopwatch();
            sw.Start();
            int rc = 1;
            ThreadContextEx context = status as ThreadContextEx;
            int key = 0;

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
                Logger.WriteToLog(112, MessageSeverity.Info, string.Format(Messages.MSG_112, context.ShopCourier.VehicleID, context.OrderCount, context.MaxRouteLength, context.StartOrderIndex, context.OrderIndexStep));

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
                Order[] orders = new Order[6];
                int[] orderGeoIndex = new int[7];
                bool isLoop = !contextCourier.IsTaxi;
                int shopIndex = orderCount;

                // 6. Цикл выбора допустимых маршрутов
                rc = 6;
                CourierDeliveryInfo delivery;
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
                Logger.WriteToLog(669, MessageSeverity.Error, string.Format(Messages.MSG_669, $"{nameof(RouteBuilder)}.{nameof(RouteBuilder.BuildEx6)}", rc, (ex.InnerException == null ? ex.Message : ex.InnerException.Message)));
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
                    Logger.WriteToLog(113, MessageSeverity.Info, string.Format(Messages.MSG_113, rc, context.ShopCourier.VehicleID, context.OrderCount, context.MaxRouteLength, context.StartOrderIndex, context.OrderIndexStep, sw.ElapsedMilliseconds, key));
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
            Stopwatch sw = new Stopwatch();
            sw.Start();
            int rc = 1;
            ThreadContextEx context = status as ThreadContextEx;
            int key = 0;

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
                Logger.WriteToLog(110, MessageSeverity.Info, string.Format(Messages.MSG_110, context.ShopCourier.VehicleID, context.OrderCount, context.MaxRouteLength, context.StartOrderIndex, context.OrderIndexStep));
                
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
                Order[] orders = new Order[7];
                int[] orderGeoIndex = new int[8];
                bool isLoop = !contextCourier.IsTaxi;
                int shopIndex = orderCount;

                // 6. Цикл выбора допустимых маршрутов
                rc = 6;
                CourierDeliveryInfo delivery;
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
                Logger.WriteToLog(669, MessageSeverity.Error, string.Format(Messages.MSG_669, $"{nameof(RouteBuilder)}.{nameof(RouteBuilder.BuildEx7)}", rc, (ex.InnerException == null ? ex.Message : ex.InnerException.Message)));
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
                    Logger.WriteToLog(115, MessageSeverity.Info, string.Format(Messages.MSG_115, rc, context.ShopCourier.VehicleID, context.OrderCount, context.MaxRouteLength, context.StartOrderIndex, context.OrderIndexStep, sw.ElapsedMilliseconds, key));
                }
            }
        }

        /// <summary>
        /// Построение всех возможных отгрузок длины 8 для
        /// заданного контекста и гео-данных
        /// </summary>
        /// <param name="status">Расширенный контекст</param>
        /// <returns>0 - отгрузки построены; иначе - отгрузки не построены</returns>
        public static void BuildEx8(object status)
        {
            // 1. Инициализация
            Stopwatch sw = new Stopwatch();
            sw.Start();
            int rc = 1;
            ThreadContextEx context = status as ThreadContextEx;
            int key = 0;

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
                Logger.WriteToLog(116, MessageSeverity.Info, string.Format(Messages.MSG_116, context.ShopCourier.VehicleID, context.OrderCount, context.MaxRouteLength, context.StartOrderIndex, context.OrderIndexStep));
                
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
                Logger.WriteToLog(669, MessageSeverity.Error, string.Format(Messages.MSG_669, $"{nameof(RouteBuilder)}.{nameof(RouteBuilder.BuildEx8)}", rc, (ex.InnerException == null ? ex.Message : ex.InnerException.Message)));
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
                    Logger.WriteToLog(117, MessageSeverity.Info, string.Format(Messages.MSG_117, rc, context.ShopCourier.VehicleID, context.OrderCount, context.MaxRouteLength, context.StartOrderIndex, context.OrderIndexStep, sw.ElapsedMilliseconds, key));
                }
            }
        }

        /// <summary>
        /// Построение всех возможных отгрузок длины 2 для
        /// заданного контекста и гео-данных
        /// </summary>
        /// <param name="status">Расширенный контекст</param>
        /// <returns>0 - отгрузки построены; иначе - отгрузки не построены</returns>
        public static void Build2(object status)
        {
            // 1. Инициализация
            Stopwatch sw = new Stopwatch();
            sw.Start();
            int rc = 1;
            ThreadContextEx context = status as ThreadContextEx;
            int count = 0;

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
                Logger.WriteToLog(114, MessageSeverity.Info, string.Format(Messages.MSG_114, context.ShopCourier.VehicleID, context.OrderCount, context.MaxRouteLength, context.StartOrderIndex, context.OrderIndexStep));

                // 3. Извлекаем и проверяем данные из контекста
                rc = 3;
                int level = context.MaxRouteLength;
                if (level < 1 || level > 2)
                    return;
                DateTime calcTime = context.CalcTime;
                Order[] contextOrders = context.Orders;
                if (contextOrders == null || contextOrders.Length <= 0)
                    return;
                short[] subsets = context.OrderSubsets;
                if (subsets == null || subsets.Length <= 0 ||
                    (subsets.Length % level) != 0)
                    return;
                int subsetCount = subsets.Length / level;

                int orderCount = contextOrders.Length;
                Shop contextShop = context.ShopFrom;
                if (contextShop == null)
                    return;
                Courier contextCourier = context.ShopCourier;
                if (contextCourier == null)
                    return;

                int step = context.OrderIndexStep;
                if (step <= 0)
                    return;
                int startIndex = context.StartOrderIndex;
                if (startIndex < 0 || startIndex >= step)
                    return;

                // 4. Цикл выбора допустимых маршрутов
                rc = 4;
                CourierDeliveryInfo[] deliveries = new CourierDeliveryInfo[subsetCount / step + 1];
                Order[] orders = new Order[2];
                int[] orderGeoIndex = new int[3];
                bool isLoop = !contextCourier.IsTaxi;
                int shopIndex = orderCount;
                CourierDeliveryInfo delivery;
                CourierDeliveryInfo delivery1;
                CourierDeliveryInfo delivery2;

                int subsetStep = step * level;
                int start = startIndex * level;
                int i1 = 0;
                int i2 = 0;

                for (int i = start; i < subsets.Length; i += subsetStep)
                {
                    // 4.1 Выбираем индексы заказов
                    rc = 41;
                    int subsetOrderCount = 1;
                    i1 = subsets[i];

                    if ((i2 = subsets[i + 1]) != 0)
                        subsetOrderCount++;

                    // 4.2 Обрабатываем по количеству заказов
                    rc = 42;
                    switch (subsetOrderCount)
                    {
                        case 1:
                            // 1
                            orderGeoIndex[0] = i1;
                            orderGeoIndex[1] = shopIndex;
                            orders[0] = contextOrders[i1];
                            int rcFind = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 1, isLoop, geoData, out delivery);
                            if (rcFind == 0)
                                deliveries[count++] = delivery;
                            break;
                        case 2:
                            // 1 2
                            orderGeoIndex[0] = i1;
                            orderGeoIndex[1] = i2;
                            orderGeoIndex[2] = shopIndex;
                            orders[0] = contextOrders[i1];
                            orders[1] = contextOrders[i2];
                            int rcFind1 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 2, isLoop, geoData, out delivery1);

                            // 2 1
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
                            break;
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
                Logger.WriteToLog(669, MessageSeverity.Error, string.Format(Messages.MSG_669, $"{nameof(RouteBuilder)}.{nameof(RouteBuilder.Build2)}", rc, (ex.InnerException == null ? ex.Message : ex.InnerException.Message)));
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
                    Logger.WriteToLog(111, MessageSeverity.Info, string.Format(Messages.MSG_111, rc, context.ShopCourier.VehicleID, context.OrderCount, context.MaxRouteLength, context.StartOrderIndex, context.OrderIndexStep, sw.ElapsedMilliseconds, count));
                }
            }
        }

        /// <summary>
        /// Построение всех возможных отгрузок длины 3 для
        /// заданного контекста и гео-данных
        /// </summary>
        /// <param name="status">Расширенный контекст</param>
        /// <returns>0 - отгрузки построены; иначе - отгрузки не построены</returns>
        public static void Build3(object status)
        {
            // 1. Инициализация
            Stopwatch sw = new Stopwatch();
            sw.Start();
            int rc = 1;
            ThreadContextEx context = status as ThreadContextEx;
            int count = 0;

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
                Logger.WriteToLog(108, MessageSeverity.Info, string.Format(Messages.MSG_108, context.ShopCourier.VehicleID, context.OrderCount, context.MaxRouteLength, context.StartOrderIndex, context.OrderIndexStep));

                // 3. Извлекаем и проверяем данные из контекста
                rc = 3;
                int level = context.MaxRouteLength;
                if (level != 3)
                    return;
                DateTime calcTime = context.CalcTime;
                Order[] contextOrders = context.Orders;
                if (contextOrders == null || contextOrders.Length <= 0)
                    return;
                short[] subsets = context.OrderSubsets;
                if (subsets == null || subsets.Length <= 0 ||
                    (subsets.Length % level) != 0)
                    return;
                int subsetCount = subsets.Length / level;

                int orderCount = contextOrders.Length;
                Shop contextShop = context.ShopFrom;
                if (contextShop == null)
                    return;
                Courier contextCourier = context.ShopCourier;
                if (contextCourier == null)
                    return;
                int step = context.OrderIndexStep;
                if (step <= 0)
                    return;

                int startIndex = context.StartOrderIndex;
                if (startIndex < 0 || startIndex >= step)
                    return;

                // 4. Цикл выбора допустимых маршрутов
                rc = 4;
                CourierDeliveryInfo[] deliveries = new CourierDeliveryInfo[subsetCount / step + 1];
                Order[] orders = new Order[3];
                int[] orderGeoIndex = new int[4];
                bool isLoop = !contextCourier.IsTaxi;
                int shopIndex = orderCount;
                CourierDeliveryInfo delivery;
                CourierDeliveryInfo delivery1;
                CourierDeliveryInfo delivery2;
                CourierDeliveryInfo delivery3;

                int subsetStep = step * level;
                int start = startIndex * level;
                int i1 = 0;
                int i2 = 0;
                int i3 = 0;

                for (int i = start; i < subsets.Length; i += subsetStep)
                {
                    // 4.1 Выбираем индексы заказов
                    rc = 41;
                    int subsetOrderCount = 1;
                    i1 = subsets[i];

                    if ((i2 = subsets[i + 1]) != 0)
                    {
                        subsetOrderCount++;
                        if ((i3 = subsets[i + 2]) != 0)
                            subsetOrderCount++;
                    }

                    // 4.2 Обрабатываем по количеству заказов
                    rc = 42;
                    switch (subsetOrderCount)
                    {
                        case 1:
                            // 1
                            orderGeoIndex[0] = i1;
                            orderGeoIndex[1] = shopIndex;
                            orders[0] = contextOrders[i1];
                            int rcFind = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 1, isLoop, geoData, out delivery);
                            if (rcFind == 0)
                                deliveries[count++] = delivery;
                            break;
                        case 2:
                            // 1 2
                            orderGeoIndex[0] = i1;
                            orderGeoIndex[1] = i2;
                            orderGeoIndex[2] = shopIndex;
                            orders[0] = contextOrders[i1];
                            orders[1] = contextOrders[i2];
                            int rcFind1 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 2, isLoop, geoData, out delivery1);

                            // 2 1
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
                            break;
                        case 3:
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
                            break;
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
                Logger.WriteToLog(669, MessageSeverity.Error, string.Format(Messages.MSG_669, $"{nameof(RouteBuilder)}.{nameof(RouteBuilder.Build3)}", rc, (ex.InnerException == null ? ex.Message : ex.InnerException.Message)));
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

                    Logger.WriteToLog(109, MessageSeverity.Info, string.Format(Messages.MSG_105, rc, context.ShopCourier.VehicleID, context.OrderCount, context.MaxRouteLength, context.StartOrderIndex, context.OrderIndexStep, sw.ElapsedMilliseconds, count));
                }
            }
        }

        /// <summary>
        /// Построение всех возможных отгрузок длины 4 для
        /// заданного контекста и гео-данных
        /// </summary>
        /// <param name="status">Расширенный контекст</param>
        /// <returns>0 - отгрузки построены; иначе - отгрузки не построены</returns>
        public static void Build4(object status)
        {
            // 1. Инициализация
            Stopwatch sw = new Stopwatch();
            sw.Start();
            int rc = 1;
            ThreadContextEx context = status as ThreadContextEx;
            int count = 0;

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
                Logger.WriteToLog(106, MessageSeverity.Info, string.Format(Messages.MSG_106, context.ShopCourier.VehicleID, context.OrderCount, context.MaxRouteLength, context.StartOrderIndex, context.OrderIndexStep));

                // 3. Извлекаем и проверяем данные из контекста
                rc = 3;
                int level = context.MaxRouteLength;
                if (level != 4)
                    return;
                DateTime calcTime = context.CalcTime;
                Order[] contextOrders = context.Orders;
                if (contextOrders == null || contextOrders.Length <= 0)
                    return;
                short[] subsets = context.OrderSubsets;
                if (subsets == null || subsets.Length <= 0 ||
                    (subsets.Length % level) != 0)
                    return;
                int subsetCount = subsets.Length / level;

                int orderCount = contextOrders.Length;
                Shop contextShop = context.ShopFrom;
                if (contextShop == null)
                    return;
                Courier contextCourier = context.ShopCourier;
                if (contextCourier == null)
                    return;
                int step = context.OrderIndexStep;
                if (step <= 0)
                    return;

                int startIndex = context.StartOrderIndex;
                if (startIndex < 0 || startIndex >= step)
                    return;

                // 4. Цикл выбора допустимых маршрутов
                rc = 4;
                CourierDeliveryInfo[] deliveries = new CourierDeliveryInfo[subsetCount / step + 1];
                Order[] orders = new Order[4];
                int[] orderGeoIndex = new int[5];
                bool isLoop = !contextCourier.IsTaxi;
                int shopIndex = orderCount;
                CourierDeliveryInfo delivery;
                CourierDeliveryInfo delivery1;
                CourierDeliveryInfo delivery2;
                CourierDeliveryInfo delivery3;
                CourierDeliveryInfo delivery4;

                int subsetStep = step * level;
                int start = startIndex * level;
                int i1 = 0;
                int i2 = 0;
                int i3 = 0;
                int i4 = 0;

                for (int i = start; i < subsets.Length; i += subsetStep)
                {
                    // 4.1 Выбираем индексы заказов
                    rc = 41;
                    int subsetOrderCount = 1;
                    i1 = subsets[i];

                    if ((i2 = subsets[i + 1]) != 0)
                    {
                        subsetOrderCount++;
                        if ((i3 = subsets[i + 2]) != 0)
                        {
                            subsetOrderCount++;
                            if ((i4 = subsets[i + 3]) != 0)
                                subsetOrderCount++;
                        }
                    }

                    // 4.2 Обрабатываем по количеству заказов
                    rc = 42;
                    switch (subsetOrderCount)
                    {
                        case 1:
                            // 1
                            orderGeoIndex[0] = i1;
                            orderGeoIndex[1] = shopIndex;
                            orders[0] = contextOrders[i1];
                            int rcFind = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 1, isLoop, geoData, out delivery);
                            if (rcFind == 0)
                                deliveries[count++] = delivery;
                            break;
                        case 2:
                            // 1 2
                            orderGeoIndex[0] = i1;
                            orderGeoIndex[1] = i2;
                            orderGeoIndex[2] = shopIndex;
                            orders[0] = contextOrders[i1];
                            orders[1] = contextOrders[i2];
                            int rcFind1 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 2, isLoop, geoData, out delivery1);

                            // 2 1
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
                            break;
                        case 3:
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
                            break;
                        case 4:
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

                            break;
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
                Logger.WriteToLog(669, MessageSeverity.Error, string.Format(Messages.MSG_669, $"{nameof(RouteBuilder)}.{nameof(RouteBuilder.Build4)}", rc, (ex.InnerException == null ? ex.Message : ex.InnerException.Message)));
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

                    Logger.WriteToLog(107, MessageSeverity.Info, string.Format(Messages.MSG_107, rc, context.ShopCourier.VehicleID, context.OrderCount, context.MaxRouteLength, context.StartOrderIndex, context.OrderIndexStep, sw.ElapsedMilliseconds, count));
                }
            }
        }

        /// <summary>
        /// Построение всех возможных отгрузок длины 5 для
        /// заданного контекста и гео-данных
        /// </summary>
        /// <param name="status">Расширенный контекст</param>
        /// <returns>0 - отгрузки построены; иначе - отгрузки не построены</returns>
        public static void Build5(object status)
        {
            // 1. Инициализация
            Stopwatch sw = new Stopwatch();
            sw.Start();
            int rc = 1;
            ThreadContextEx context = status as ThreadContextEx;
            int count = 0;

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
                Logger.WriteToLog(104, MessageSeverity.Info, string.Format(Messages.MSG_104, context.ShopCourier.VehicleID, context.OrderCount, context.MaxRouteLength, context.StartOrderIndex, context.OrderIndexStep));

                // 3. Извлекаем и проверяем данные из контекста
                rc = 3;
                int level = context.MaxRouteLength;
                if (level != 5)
                    return;
                DateTime calcTime = context.CalcTime;
                Order[] contextOrders = context.Orders;
                if (contextOrders == null || contextOrders.Length <= 0)
                    return;
                short[] subsets = context.OrderSubsets;
                if (subsets == null || subsets.Length <= 0 ||
                    (subsets.Length % level) != 0)
                    return;
                int subsetCount = subsets.Length / level;

                int orderCount = contextOrders.Length;
                Shop contextShop = context.ShopFrom;
                if (contextShop == null)
                    return;
                Courier contextCourier = context.ShopCourier;
                if (contextCourier == null)
                    return;
                int step = context.OrderIndexStep;
                if (step <= 0)
                    return;
                int startIndex = context.StartOrderIndex;
                if (startIndex < 0 || startIndex >= step)
                    return;

                // 4. Цикл выбора допустимых маршрутов
                rc = 4;
                CourierDeliveryInfo[] deliveries = new CourierDeliveryInfo[subsetCount / step + 1];
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
                byte[] permutations5 = Permutations.Generate(5);

                int subsetStep = step * level;
                int start = startIndex * level;
                int i1 = 0;
                int i2 = 0;
                int i3 = 0;
                int i4 = 0;
                int i5 = 0;

                for (int i = start; i < subsets.Length; i+= subsetStep)
                {
                    // 4.1 Выбираем индексы заказов
                    rc = 41;
                    int subsetOrderCount = 1;
                    i1 = subsets[i];

                    if ((i2 = subsets[i + 1]) != 0)
                    {
                        subsetOrderCount++;
                        if ((i3 = subsets[i + 2]) != 0)
                        {
                            subsetOrderCount++;
                            if ((i4 = subsets[i + 3]) != 0)
                            {
                                subsetOrderCount++;
                                if ((i5 = subsets[i + 4]) != 0)
                                    subsetOrderCount++;
                            }
                        }
                    }

                    // 4.2 Обрабатываем по количеству заказов
                    rc = 42;

                    switch (subsetOrderCount)
                    {
                        case 1:
                            // 1
                            orderGeoIndex[0] = i1;
                            orderGeoIndex[1] = shopIndex;
                            orders[0] = contextOrders[i1];
                            int rcFind = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 1, isLoop, geoData, out delivery);
                            if (rcFind == 0)
                                deliveries[count++] = delivery;
                            break;
                        case 2:
                            // 1 2
                            orderGeoIndex[0] = i1;
                            orderGeoIndex[1] = i2;
                            orderGeoIndex[2] = shopIndex;
                            orders[0] = contextOrders[i1];
                            orders[1] = contextOrders[i2];
                            int rcFind1 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 2, isLoop, geoData, out delivery1);

                            // 2 1
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
                            break;
                        case 3:
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
                            break;
                        case 4:
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

                            break;
                        case 5:
                            delivery = null;
                            orderGeoIndex[5] = shopIndex;

                            orderIndex[0] = i1;
                            orderIndex[1] = i2;
                            orderIndex[2] = i3;
                            orderIndex[3] = i4;
                            orderIndex[4] = i5;

                            for (int j = 0; j < permutations5.Length; j += 5)
                            {
                                int p1 = orderIndex[permutations5[j]];
                                int p2 = orderIndex[permutations5[j + 1]];
                                int p3 = orderIndex[permutations5[j + 2]];
                                int p4 = orderIndex[permutations5[j + 3]];
                                int p5 = orderIndex[permutations5[j + 4]];

                                orderGeoIndex[0] = p1;
                                orderGeoIndex[1] = p2;
                                orderGeoIndex[2] = p3;
                                orderGeoIndex[3] = p4;
                                orderGeoIndex[4] = p5;

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
                            break;
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
                Logger.WriteToLog(669, MessageSeverity.Error, string.Format(Messages.MSG_669, $"{nameof(RouteBuilder)}.{nameof(RouteBuilder.Build5)}", rc, (ex.InnerException == null ? ex.Message : ex.InnerException.Message)));
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

                    Logger.WriteToLog(105, MessageSeverity.Info, string.Format(Messages.MSG_105, rc, context.ShopCourier.VehicleID, context.OrderCount, context.MaxRouteLength, context.StartOrderIndex, context.OrderIndexStep, sw.ElapsedMilliseconds, count));
                }
            }
        }

        /// <summary>
        /// Построение всех возможных отгрузок длины 6 для
        /// заданного контекста и гео-данных
        /// </summary>
        /// <param name="status">Расширенный контекст</param>
        /// <returns>0 - отгрузки построены; иначе - отгрузки не построены</returns>
        public static void Build6(object status)
        {
            // 1. Инициализация
            Stopwatch sw = new Stopwatch();
            sw.Start();
            int rc = 1;
            ThreadContextEx context = status as ThreadContextEx;
            int count = 0;

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
                Logger.WriteToLog(112, MessageSeverity.Info, string.Format(Messages.MSG_112, context.ShopCourier.VehicleID, context.OrderCount, context.MaxRouteLength, context.StartOrderIndex, context.OrderIndexStep));

                // 3. Извлекаем и проверяем данные из контекста
                rc = 3;
                int level = context.MaxRouteLength;
                if (level != 6)
                    return;
                DateTime calcTime = context.CalcTime;
                Order[] contextOrders = context.Orders;
                if (contextOrders == null || contextOrders.Length <= 0)
                    return;
                short[] subsets = context.OrderSubsets;
                if (subsets == null || subsets.Length <= 0 ||
                    (subsets.Length % level) != 0)
                    return;
                int subsetCount = subsets.Length / level;

                int orderCount = contextOrders.Length;
                Shop contextShop = context.ShopFrom;
                if (contextShop == null)
                    return;
                Courier contextCourier = context.ShopCourier;
                if (contextCourier == null)
                    return;
                int step = context.OrderIndexStep;
                if (step <= 0)
                    return;
                int startIndex = context.StartOrderIndex;
                if (startIndex < 0 || startIndex >= step)
                    return;

                // 4. Цикл выбора допустимых маршрутов
                rc = 4;
                CourierDeliveryInfo[] deliveries = new CourierDeliveryInfo[subsetCount / step + 1];
                int[] orderIndex = new int[6];
                Order[] orders = new Order[6];
                int[] orderGeoIndex = new int[7];
                bool isLoop = !contextCourier.IsTaxi;
                int shopIndex = orderCount;
                CourierDeliveryInfo delivery;
                CourierDeliveryInfo delivery1;
                CourierDeliveryInfo delivery2;
                CourierDeliveryInfo delivery3;
                CourierDeliveryInfo delivery4;
                CourierDeliveryInfo delivery5;
                CourierDeliveryInfo delivery6;
                byte[] permutations5 = Permutations.Generate(5);
                byte[] permutations6 = Permutations.Generate(6);

                int subsetStep = step * level;
                int start = startIndex * level;
                int i1 = 0;
                int i2 = 0;
                int i3 = 0;
                int i4 = 0;
                int i5 = 0;
                int i6 = 0;

                for (int i = start; i < subsets.Length; i+= subsetStep)
                {
                    // 4.1 Выбираем индексы заказов
                    rc = 41;
                    int subsetOrderCount = 1;
                    i1 = subsets[i];

                    if ((i2 = subsets[i + 1]) != 0)
                    {
                        subsetOrderCount++;
                        if ((i3 = subsets[i + 2]) != 0)
                        {
                            subsetOrderCount++;
                            if ((i4 = subsets[i + 3]) != 0)
                            {
                                subsetOrderCount++;
                                if ((i5 = subsets[i + 4]) != 0)
                                {
                                    subsetOrderCount++;
                                    if ((i6 = subsets[i + 5]) != 0)
                                        subsetOrderCount++;
                                }
                            }
                        }
                    }

                    // 4.2 Обрабатываем по количеству заказов
                    rc = 42;

                    switch (subsetOrderCount)
                    {
                        case 1:
                            // 1
                            orderGeoIndex[0] = i1;
                            orderGeoIndex[1] = shopIndex;
                            orders[0] = contextOrders[i1];
                            int rcFind = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 1, isLoop, geoData, out delivery);
                            if (rcFind == 0)
                                deliveries[count++] = delivery;
                            break;
                        case 2:
                            // 1 2
                            orderGeoIndex[0] = i1;
                            orderGeoIndex[1] = i2;
                            orderGeoIndex[2] = shopIndex;
                            orders[0] = contextOrders[i1];
                            orders[1] = contextOrders[i2];
                            int rcFind1 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 2, isLoop, geoData, out delivery1);

                            // 2 1
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
                            break;
                        case 3:
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
                            break;
                        case 4:
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

                            break;
                        case 5:
                            delivery = null;
                            orderGeoIndex[5] = shopIndex;

                            orderIndex[0] = i1;
                            orderIndex[1] = i2;
                            orderIndex[2] = i3;
                            orderIndex[3] = i4;
                            orderIndex[4] = i5;

                            for (int j = 0; j < permutations5.Length; j += 5)
                            {
                                int p1 = orderIndex[permutations5[j]];
                                int p2 = orderIndex[permutations5[j + 1]];
                                int p3 = orderIndex[permutations5[j + 2]];
                                int p4 = orderIndex[permutations5[j + 3]];
                                int p5 = orderIndex[permutations5[j + 4]];

                                orderGeoIndex[0] = p1;
                                orderGeoIndex[1] = p2;
                                orderGeoIndex[2] = p3;
                                orderGeoIndex[3] = p4;
                                orderGeoIndex[4] = p5;

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
                            break;
                        case 6:
                            delivery = null;
                            orderGeoIndex[6] = shopIndex;

                            orderIndex[0] = i1;
                            orderIndex[1] = i2;
                            orderIndex[2] = i3;
                            orderIndex[3] = i4;
                            orderIndex[4] = i5;
                            orderIndex[5] = i6;

                            for (int j = 0; j < permutations6.Length; j += 6)
                            {
                                int p1 = orderIndex[permutations6[j]];
                                int p2 = orderIndex[permutations6[j + 1]];
                                int p3 = orderIndex[permutations6[j + 2]];
                                int p4 = orderIndex[permutations6[j + 3]];
                                int p5 = orderIndex[permutations6[j + 4]];
                                int p6 = orderIndex[permutations6[j + 5]];

                                orderGeoIndex[0] = p1;
                                orderGeoIndex[1] = p2;
                                orderGeoIndex[2] = p3;
                                orderGeoIndex[3] = p4;
                                orderGeoIndex[4] = p5;
                                orderGeoIndex[5] = p6;

                                orders[0] = contextOrders[p1];
                                orders[1] = contextOrders[p2];
                                orders[2] = contextOrders[p3];
                                orders[3] = contextOrders[p4];
                                orders[4] = contextOrders[p5];
                                orders[5] = contextOrders[p6];

                                int rcFind6 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 6, isLoop, geoData, out delivery6);
                                if (rcFind6 == 0 && (delivery == null || delivery6.Cost < delivery.Cost))
                                { delivery = delivery6; }
                            }

                            if (delivery != null)
                            { deliveries[count++] = delivery; }
                            break;
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
                Logger.WriteToLog(669, MessageSeverity.Error, string.Format(Messages.MSG_669, $"{nameof(RouteBuilder)}.{nameof(RouteBuilder.Build6)}", rc, (ex.InnerException == null ? ex.Message : ex.InnerException.Message)));
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

                    Logger.WriteToLog(113, MessageSeverity.Info, string.Format(Messages.MSG_113, rc, context.ShopCourier.VehicleID, context.OrderCount, context.MaxRouteLength, context.StartOrderIndex, context.OrderIndexStep, sw.ElapsedMilliseconds, count));
                }
            }
        }

        /// <summary>
        /// Построение всех возможных отгрузок длины 7 для
        /// заданного контекста и гео-данных
        /// </summary>
        /// <param name="status">Расширенный контекст</param>
        /// <returns>0 - отгрузки построены; иначе - отгрузки не построены</returns>
        public static void Build7(object status)
        {
            // 1. Инициализация
            Stopwatch sw = new Stopwatch();
            sw.Start();
            int rc = 1;
            ThreadContextEx context = status as ThreadContextEx;
            int count = 0;

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
                Logger.WriteToLog(114, MessageSeverity.Info, string.Format(Messages.MSG_114, context.ShopCourier.VehicleID, context.OrderCount, context.MaxRouteLength, context.StartOrderIndex, context.OrderIndexStep));

                // 3. Извлекаем и проверяем данные из контекста
                rc = 3;
                int level = context.MaxRouteLength;
                if (level != 7)
                    return;
                DateTime calcTime = context.CalcTime;
                Order[] contextOrders = context.Orders;
                if (contextOrders == null || contextOrders.Length <= 0)
                    return;
                short[] subsets = context.OrderSubsets;
                if (subsets == null || subsets.Length <= 0 ||
                    (subsets.Length % level) != 0)
                    return;
                int subsetCount = subsets.Length / level;

                int orderCount = contextOrders.Length;
                Shop contextShop = context.ShopFrom;
                if (contextShop == null)
                    return;
                Courier contextCourier = context.ShopCourier;
                if (contextCourier == null)
                    return;
                int step = context.OrderIndexStep;
                if (step <= 0)
                    return;
                int startIndex = context.StartOrderIndex;
                if (startIndex < 0 || startIndex >= step)
                    return;

                // 4. Цикл выбора допустимых маршрутов
                rc = 4;
                CourierDeliveryInfo[] deliveries = new CourierDeliveryInfo[subsetCount / step + 1];
                int[] orderIndex = new int[7];
                Order[] orders = new Order[7];
                int[] orderGeoIndex = new int[8];
                bool isLoop = !contextCourier.IsTaxi;
                int shopIndex = orderCount;
                CourierDeliveryInfo delivery;
                CourierDeliveryInfo delivery1;
                CourierDeliveryInfo delivery2;
                CourierDeliveryInfo delivery3;
                CourierDeliveryInfo delivery4;
                CourierDeliveryInfo delivery5;
                CourierDeliveryInfo delivery6;
                CourierDeliveryInfo delivery7;
                byte[] permutations5 = Permutations.Generate(5);
                byte[] permutations6 = Permutations.Generate(6);
                byte[] permutations7 = Permutations.Generate(7);

                int subsetStep = step * level;
                int start = startIndex * level;
                int i1 = 0;
                int i2 = 0;
                int i3 = 0;
                int i4 = 0;
                int i5 = 0;
                int i6 = 0;
                int i7 = 0;

                for (int i = start; i < subsets.Length; i+= subsetStep)
                {
                    // 4.1 Выбираем индексы заказов
                    rc = 41;
                    int subsetOrderCount = 1;
                    i1 = subsets[i];

                    if ((i2 = subsets[i + 1]) != 0)
                    {
                        subsetOrderCount++;
                        if ((i3 = subsets[i + 2]) != 0)
                        {
                            subsetOrderCount++;
                            if ((i4 = subsets[i + 3]) != 0)
                            {
                                subsetOrderCount++;
                                if ((i5 = subsets[i + 4]) != 0)
                                {
                                    subsetOrderCount++;
                                    if ((i6 = subsets[i + 5]) != 0)
                                    {
                                        subsetOrderCount++;
                                        if ((i7 = subsets[i + 6]) != 0)
                                            subsetOrderCount++;
                                    }
                                }
                            }
                        }
                    }

                    // 4.2 Обрабатываем по количеству заказов
                    rc = 42;

                    switch (subsetOrderCount)
                    {
                        case 1:
                            // 1
                            orderGeoIndex[0] = i1;
                            orderGeoIndex[1] = shopIndex;
                            orders[0] = contextOrders[i1];
                            int rcFind = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 1, isLoop, geoData, out delivery);
                            if (rcFind == 0)
                                deliveries[count++] = delivery;
                            break;
                        case 2:
                            // 1 2
                            orderGeoIndex[0] = i1;
                            orderGeoIndex[1] = i2;
                            orderGeoIndex[2] = shopIndex;
                            orders[0] = contextOrders[i1];
                            orders[1] = contextOrders[i2];
                            int rcFind1 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 2, isLoop, geoData, out delivery1);

                            // 2 1
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
                            break;
                        case 3:
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
                            break;
                        case 4:
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

                            break;
                        case 5:
                            delivery = null;
                            orderGeoIndex[5] = shopIndex;

                            orderIndex[0] = i1;
                            orderIndex[1] = i2;
                            orderIndex[2] = i3;
                            orderIndex[3] = i4;
                            orderIndex[4] = i5;

                            for (int j = 0; j < permutations5.Length; j += 5)
                            {
                                int p1 = orderIndex[permutations5[j]];
                                int p2 = orderIndex[permutations5[j + 1]];
                                int p3 = orderIndex[permutations5[j + 2]];
                                int p4 = orderIndex[permutations5[j + 3]];
                                int p5 = orderIndex[permutations5[j + 4]];

                                orderGeoIndex[0] = p1;
                                orderGeoIndex[1] = p2;
                                orderGeoIndex[2] = p3;
                                orderGeoIndex[3] = p4;
                                orderGeoIndex[4] = p5;

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
                            break;
                        case 6:
                            delivery = null;
                            orderGeoIndex[6] = shopIndex;

                            orderIndex[0] = i1;
                            orderIndex[1] = i2;
                            orderIndex[2] = i3;
                            orderIndex[3] = i4;
                            orderIndex[4] = i5;
                            orderIndex[5] = i6;

                            for (int j = 0; j < permutations6.Length; j += 6)
                            {
                                int p1 = orderIndex[permutations6[j]];
                                int p2 = orderIndex[permutations6[j + 1]];
                                int p3 = orderIndex[permutations6[j + 2]];
                                int p4 = orderIndex[permutations6[j + 3]];
                                int p5 = orderIndex[permutations6[j + 4]];
                                int p6 = orderIndex[permutations6[j + 5]];

                                orderGeoIndex[0] = p1;
                                orderGeoIndex[1] = p2;
                                orderGeoIndex[2] = p3;
                                orderGeoIndex[3] = p4;
                                orderGeoIndex[4] = p5;
                                orderGeoIndex[5] = p6;

                                orders[0] = contextOrders[p1];
                                orders[1] = contextOrders[p2];
                                orders[2] = contextOrders[p3];
                                orders[3] = contextOrders[p4];
                                orders[4] = contextOrders[p5];
                                orders[5] = contextOrders[p6];

                                int rcFind6 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 6, isLoop, geoData, out delivery6);
                                if (rcFind6 == 0 && (delivery == null || delivery6.Cost < delivery.Cost))
                                { delivery = delivery6; }
                            }

                            if (delivery != null)
                            { deliveries[count++] = delivery; }
                            break;
                        case 7:
                            delivery = null;
                            orderGeoIndex[7] = shopIndex;

                            orderIndex[0] = i1;
                            orderIndex[1] = i2;
                            orderIndex[2] = i3;
                            orderIndex[3] = i4;
                            orderIndex[4] = i5;
                            orderIndex[5] = i6;
                            orderIndex[6] = i7;

                            for (int j = 0; j < permutations7.Length; j += 7)
                            {
                                int p1 = orderIndex[permutations7[j]];
                                int p2 = orderIndex[permutations7[j + 1]];
                                int p3 = orderIndex[permutations7[j + 2]];
                                int p4 = orderIndex[permutations7[j + 3]];
                                int p5 = orderIndex[permutations7[j + 4]];
                                int p6 = orderIndex[permutations7[j + 5]];
                                int p7 = orderIndex[permutations7[j + 6]];

                                orderGeoIndex[0] = p1;
                                orderGeoIndex[1] = p2;
                                orderGeoIndex[2] = p3;
                                orderGeoIndex[3] = p4;
                                orderGeoIndex[4] = p5;
                                orderGeoIndex[5] = p6;
                                orderGeoIndex[6] = p7;

                                orders[0] = contextOrders[p1];
                                orders[1] = contextOrders[p2];
                                orders[2] = contextOrders[p3];
                                orders[3] = contextOrders[p4];
                                orders[4] = contextOrders[p5];
                                orders[5] = contextOrders[p6];
                                orders[6] = contextOrders[p7];

                                int rcFind7 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 7, isLoop, geoData, out delivery7);
                                if (rcFind7 == 0 && (delivery == null || delivery7.Cost < delivery.Cost))
                                { delivery = delivery7; }
                            }

                            if (delivery != null)
                            { deliveries[count++] = delivery; }
                            break;
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
                Logger.WriteToLog(669, MessageSeverity.Error, string.Format(Messages.MSG_669, $"{nameof(RouteBuilder)}.{nameof(RouteBuilder.Build7)}", rc, (ex.InnerException == null ? ex.Message : ex.InnerException.Message)));
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

                    Logger.WriteToLog(115, MessageSeverity.Info, string.Format(Messages.MSG_115, rc, context.ShopCourier.VehicleID, context.OrderCount, context.MaxRouteLength, context.StartOrderIndex, context.OrderIndexStep, sw.ElapsedMilliseconds, count));
                }
            }
        }

        /// <summary>
        /// Построение всех возможных отгрузок длины 8 для
        /// заданного контекста и гео-данных
        /// </summary>
        /// <param name="status">Расширенный контекст</param>
        /// <returns>0 - отгрузки построены; иначе - отгрузки не построены</returns>
        public static void Build8(object status)
        {
            // 1. Инициализация
            Stopwatch sw = new Stopwatch();
            sw.Start();
            int rc = 1;
            ThreadContextEx context = status as ThreadContextEx;
            int count = 0;

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
                Logger.WriteToLog(116, MessageSeverity.Info, string.Format(Messages.MSG_116, context.ShopCourier.VehicleID, context.OrderCount, context.MaxRouteLength, context.StartOrderIndex, context.OrderIndexStep));

                // 3. Извлекаем и проверяем данные из контекста
                rc = 3;
                int level = context.MaxRouteLength;
                if (level != 8)
                    return;
                DateTime calcTime = context.CalcTime;
                Order[] contextOrders = context.Orders;
                if (contextOrders == null || contextOrders.Length <= 0)
                    return;
                short[] subsets = context.OrderSubsets;
                if (subsets == null || subsets.Length <= 0 ||
                    (subsets.Length % level) != 0)
                    return;
                int subsetCount = subsets.Length / level;

                int orderCount = contextOrders.Length;
                Shop contextShop = context.ShopFrom;
                if (contextShop == null)
                    return;
                Courier contextCourier = context.ShopCourier;
                if (contextCourier == null)
                    return;
                int step = context.OrderIndexStep;
                if (step <= 0)
                    return;
                int startIndex = context.StartOrderIndex;
                if (startIndex < 0 || startIndex >= step)
                    return;

                // 4. Цикл выбора допустимых маршрутов
                rc = 4;
                CourierDeliveryInfo[] deliveries = new CourierDeliveryInfo[subsetCount / step + 1];
                int[] orderIndex = new int[7];
                Order[] orders = new Order[7];
                int[] orderGeoIndex = new int[8];
                bool isLoop = !contextCourier.IsTaxi;
                int shopIndex = orderCount;
                CourierDeliveryInfo delivery;
                CourierDeliveryInfo delivery1;
                CourierDeliveryInfo delivery2;
                CourierDeliveryInfo delivery3;
                CourierDeliveryInfo delivery4;
                CourierDeliveryInfo delivery5;
                CourierDeliveryInfo delivery6;
                CourierDeliveryInfo delivery7;
                CourierDeliveryInfo delivery8;
                byte[] permutations5 = Permutations.Generate(5);
                byte[] permutations6 = Permutations.Generate(6);
                byte[] permutations7 = Permutations.Generate(7);
                byte[] permutations8 = Permutations.Generate(8);

                int subsetStep = step * level;
                int start = startIndex * level;
                int i1 = 0;
                int i2 = 0;
                int i3 = 0;
                int i4 = 0;
                int i5 = 0;
                int i6 = 0;
                int i7 = 0;
                int i8 = 0;

                for (int i = start; i < subsets.Length; i+= subsetStep)
                {
                    // 4.1 Выбираем индексы заказов
                    rc = 41;
                    int subsetOrderCount = 1;
                    i1 = subsets[i];

                    if ((i2 = subsets[i + 1]) != 0)
                    {
                        subsetOrderCount++;
                        if ((i3 = subsets[i + 2]) != 0)
                        {
                            subsetOrderCount++;
                            if ((i4 = subsets[i + 3]) != 0)
                            {
                                subsetOrderCount++;
                                if ((i5 = subsets[i + 4]) != 0)
                                {
                                    subsetOrderCount++;
                                    if ((i6 = subsets[i + 5]) != 0)
                                    {
                                        subsetOrderCount++;
                                        if ((i7 = subsets[i + 6]) != 0)
                                        {
                                            subsetOrderCount++;
                                            if ((i8 = subsets[i + 7]) != 0)
                                                subsetOrderCount++;
                                        }
                                    }
                                }
                            }
                        }
                    }

                    // 4.2 Обрабатываем по количеству заказов
                    rc = 42;

                    switch (subsetOrderCount)
                    {
                        case 1:
                            // 1
                            orderGeoIndex[0] = i1;
                            orderGeoIndex[1] = shopIndex;
                            orders[0] = contextOrders[i1];
                            int rcFind = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 1, isLoop, geoData, out delivery);
                            if (rcFind == 0)
                                deliveries[count++] = delivery;
                            break;
                        case 2:
                            // 1 2
                            orderGeoIndex[0] = i1;
                            orderGeoIndex[1] = i2;
                            orderGeoIndex[2] = shopIndex;
                            orders[0] = contextOrders[i1];
                            orders[1] = contextOrders[i2];
                            int rcFind1 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 2, isLoop, geoData, out delivery1);

                            // 2 1
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
                            break;
                        case 3:
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
                            break;
                        case 4:
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

                            break;
                        case 5:
                            delivery = null;
                            orderGeoIndex[5] = shopIndex;

                            orderIndex[0] = i1;
                            orderIndex[1] = i2;
                            orderIndex[2] = i3;
                            orderIndex[3] = i4;
                            orderIndex[4] = i5;

                            for (int j = 0; j < permutations5.Length; j += 5)
                            {
                                int p1 = orderIndex[permutations5[j]];
                                int p2 = orderIndex[permutations5[j + 1]];
                                int p3 = orderIndex[permutations5[j + 2]];
                                int p4 = orderIndex[permutations5[j + 3]];
                                int p5 = orderIndex[permutations5[j + 4]];

                                orderGeoIndex[0] = p1;
                                orderGeoIndex[1] = p2;
                                orderGeoIndex[2] = p3;
                                orderGeoIndex[3] = p4;
                                orderGeoIndex[4] = p5;

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
                            break;
                        case 6:
                            delivery = null;
                            orderGeoIndex[6] = shopIndex;

                            orderIndex[0] = i1;
                            orderIndex[1] = i2;
                            orderIndex[2] = i3;
                            orderIndex[3] = i4;
                            orderIndex[4] = i5;
                            orderIndex[5] = i6;

                            for (int j = 0; j < permutations6.Length; j += 6)
                            {
                                int p1 = orderIndex[permutations6[j]];
                                int p2 = orderIndex[permutations6[j + 1]];
                                int p3 = orderIndex[permutations6[j + 2]];
                                int p4 = orderIndex[permutations6[j + 3]];
                                int p5 = orderIndex[permutations6[j + 4]];
                                int p6 = orderIndex[permutations6[j + 5]];

                                orderGeoIndex[0] = p1;
                                orderGeoIndex[1] = p2;
                                orderGeoIndex[2] = p3;
                                orderGeoIndex[3] = p4;
                                orderGeoIndex[4] = p5;
                                orderGeoIndex[5] = p6;

                                orders[0] = contextOrders[p1];
                                orders[1] = contextOrders[p2];
                                orders[2] = contextOrders[p3];
                                orders[3] = contextOrders[p4];
                                orders[4] = contextOrders[p5];
                                orders[5] = contextOrders[p6];

                                int rcFind6 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 6, isLoop, geoData, out delivery6);
                                if (rcFind6 == 0 && (delivery == null || delivery6.Cost < delivery.Cost))
                                { delivery = delivery6; }
                            }

                            if (delivery != null)
                            { deliveries[count++] = delivery; }
                            break;
                        case 7:
                            delivery = null;
                            orderGeoIndex[7] = shopIndex;

                            orderIndex[0] = i1;
                            orderIndex[1] = i2;
                            orderIndex[2] = i3;
                            orderIndex[3] = i4;
                            orderIndex[4] = i5;
                            orderIndex[5] = i6;
                            orderIndex[6] = i7;

                            for (int j = 0; j < permutations7.Length; j += 7)
                            {
                                int p1 = orderIndex[permutations7[j]];
                                int p2 = orderIndex[permutations7[j + 1]];
                                int p3 = orderIndex[permutations7[j + 2]];
                                int p4 = orderIndex[permutations7[j + 3]];
                                int p5 = orderIndex[permutations7[j + 4]];
                                int p6 = orderIndex[permutations7[j + 5]];
                                int p7 = orderIndex[permutations7[j + 6]];

                                orderGeoIndex[0] = p1;
                                orderGeoIndex[1] = p2;
                                orderGeoIndex[2] = p3;
                                orderGeoIndex[3] = p4;
                                orderGeoIndex[4] = p5;
                                orderGeoIndex[5] = p6;
                                orderGeoIndex[6] = p7;

                                orders[0] = contextOrders[p1];
                                orders[1] = contextOrders[p2];
                                orders[2] = contextOrders[p3];
                                orders[3] = contextOrders[p4];
                                orders[4] = contextOrders[p5];
                                orders[5] = contextOrders[p6];
                                orders[6] = contextOrders[p7];

                                int rcFind7 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 7, isLoop, geoData, out delivery7);
                                if (rcFind7 == 0 && (delivery == null || delivery7.Cost < delivery.Cost))
                                { delivery = delivery7; }
                            }

                            if (delivery != null)
                            { deliveries[count++] = delivery; }
                            break;
                        case 8:
                            delivery = null;
                            orderGeoIndex[8] = shopIndex;

                            orderIndex[0] = i1;
                            orderIndex[1] = i2;
                            orderIndex[2] = i3;
                            orderIndex[3] = i4;
                            orderIndex[4] = i5;
                            orderIndex[5] = i6;
                            orderIndex[6] = i7;
                            orderIndex[7] = i8;

                            for (int j = 0; j < permutations8.Length; j += 8)
                            {
                                int p1 = orderIndex[permutations8[j]];
                                int p2 = orderIndex[permutations8[j + 1]];
                                int p3 = orderIndex[permutations8[j + 2]];
                                int p4 = orderIndex[permutations8[j + 3]];
                                int p5 = orderIndex[permutations8[j + 4]];
                                int p6 = orderIndex[permutations8[j + 5]];
                                int p7 = orderIndex[permutations8[j + 6]];
                                int p8 = orderIndex[permutations8[j + 7]];

                                orderGeoIndex[0] = p1;
                                orderGeoIndex[1] = p2;
                                orderGeoIndex[2] = p3;
                                orderGeoIndex[3] = p4;
                                orderGeoIndex[4] = p5;
                                orderGeoIndex[5] = p6;
                                orderGeoIndex[6] = p7;
                                orderGeoIndex[7] = p8;

                                orders[0] = contextOrders[p1];
                                orders[1] = contextOrders[p2];
                                orders[2] = contextOrders[p3];
                                orders[3] = contextOrders[p4];
                                orders[4] = contextOrders[p5];
                                orders[5] = contextOrders[p6];
                                orders[6] = contextOrders[p7];
                                orders[7] = contextOrders[p8];

                                int rcFind8 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 8, isLoop, geoData, out delivery8);
                                if (rcFind8 == 0 && (delivery == null || delivery8.Cost < delivery.Cost))
                                { delivery = delivery8; }
                            }

                            if (delivery != null)
                            { deliveries[count++] = delivery; }
                            break;
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
                Logger.WriteToLog(669, MessageSeverity.Error, string.Format(Messages.MSG_669, $"{nameof(RouteBuilder)}.{nameof(RouteBuilder.Build8)}", rc, (ex.InnerException == null ? ex.Message : ex.InnerException.Message)));
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

                    Logger.WriteToLog(117, MessageSeverity.Info, string.Format(Messages.MSG_117, rc, context.ShopCourier.VehicleID, context.OrderCount, context.MaxRouteLength, context.StartOrderIndex, context.OrderIndexStep, sw.ElapsedMilliseconds, count));
                }
            }
        }

        /// <summary>
        /// Построение всех возможных отгрузок длины 5 для
        /// заданного контекста и гео-данных
        /// </summary>
        /// <param name="status">Расширенный контекст</param>
        /// <returns>0 - отгрузки построены; иначе - отгрузки не построены</returns>
        public static void Build(object status)
        {
            // 1. Инициализация
            Stopwatch sw = new Stopwatch();
            sw.Start();
            int rc = 1;
            ThreadContextR context = status as ThreadContextR;
            int count = 0;

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
                Logger.WriteToLog(126, MessageSeverity.Info, string.Format(Messages.MSG_126, context.ShopCourier.VehicleID, context.OrderCount, context.MaxRouteLength, context.SubsetCount, ArrayToString(context.InitOrderIndexes)));

                // 3. Извлекаем и проверяем данные из контекста
                rc = 3;
                int level = context.MaxRouteLength;
                if (level < 1 || level > 8)
                    return;
                DateTime calcTime = context.CalcTime;
                Order[] contextOrders = context.Orders;
                if (contextOrders == null || contextOrders.Length <= 0)
                    return;
                int[] orderIndex = context.InitOrderIndexes;
                if (orderIndex == null || orderIndex.Length != level)
                    return;
                int subsetCount = context.SubsetCount;
                if (subsetCount <= 0)
                    return;

                int orderCount = contextOrders.Length;
                Shop contextShop = context.ShopFrom;
                if (contextShop == null)
                    return;
                Courier contextCourier = context.ShopCourier;
                if (contextCourier == null)
                    return;

                int pointer = 0;
                for (int i = 1; i < level && orderIndex[i] != 0; i++)
                { pointer = i; }

                // 4. Цикл выбора допустимых маршрутов
                rc = 4;
                int cnt = orderCount - 1;
                int lev = level - 1;
                CourierDeliveryInfo[] deliveries = new CourierDeliveryInfo[subsetCount];
                Order[] orders = new Order[level];
                int[] orderGeoIndex = new int[level + 1];
                bool isLoop = !contextCourier.IsTaxi;
                int shopIndex = orderCount;
                CourierDeliveryInfo delivery;
                CourierDeliveryInfo delivery1;
                CourierDeliveryInfo delivery2;
                CourierDeliveryInfo delivery3;
                CourierDeliveryInfo delivery4;
                CourierDeliveryInfo delivery5;
                CourierDeliveryInfo delivery6;
                CourierDeliveryInfo delivery7;
                CourierDeliveryInfo delivery8;

                byte[] permutations5 = null;
                byte[] permutations6 = null;
                byte[] permutations7 = null;
                byte[] permutations8 = null;
                if (level >= 5)
                {
                    permutations5 = Permutations.Generate(5);
                    if (level >= 6)
                    {
                        permutations6 = Permutations.Generate(6);
                        if (level >= 7)
                        {
                            permutations7 = Permutations.Generate(7);
                            if (level >= 8)
                            { permutations8 = Permutations.Generate(8); }
                        }
                    }
                }

                int i1 = 0;
                int i2 = 0;
                int i3 = 0;
                int i4 = 0;
                int i5 = 0;
                int i6 = 0;
                int i7 = 0;
                int i8 = 0;

                for (int i = 0; i < subsetCount; i++)
                {
                    // 4.1 Обрабатываем по количеству заказов
                    rc = 41;
                    switch (pointer)
                    {
                        case 0:
                            // 1
                            i1 = orderIndex[0];
                            orderGeoIndex[0] = i1;
                            orderGeoIndex[1] = shopIndex;
                            orders[0] = contextOrders[i1];
                            int rcFind = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 1, isLoop, geoData, out delivery);
                            if (rcFind == 0)
                                deliveries[count++] = delivery;
                            break;
                        case 1:
                            // 1 2
                            i1 = orderIndex[0];
                            i2 = orderIndex[1];
                            orderGeoIndex[0] = i1;
                            orderGeoIndex[1] = i2;
                            orderGeoIndex[2] = shopIndex;
                            orders[0] = contextOrders[i1];
                            orders[1] = contextOrders[i2];
                            int rcFind1 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 2, isLoop, geoData, out delivery1);

                            // 2 1
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
                            break;
                        case 2:
                            i1 = orderIndex[0];
                            i2 = orderIndex[1];
                            i3 = orderIndex[2];
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
                            break;
                        case 3:
                            i1 = orderIndex[0];
                            i2 = orderIndex[1];
                            i3 = orderIndex[2];
                            i4 = orderIndex[3];
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

                            break;
                        case 4:
                            i1 = orderIndex[0];
                            i2 = orderIndex[1];
                            i3 = orderIndex[2];
                            i4 = orderIndex[3];
                            i5 = orderIndex[4];
                            delivery = null;
                            orderGeoIndex[5] = shopIndex;

                            orderIndex[0] = i1;
                            orderIndex[1] = i2;
                            orderIndex[2] = i3;
                            orderIndex[3] = i4;
                            orderIndex[4] = i5;

                            for (int j = 0; j < permutations5.Length; j += 5)
                            {
                                int p1 = orderIndex[permutations5[j]];
                                int p2 = orderIndex[permutations5[j + 1]];
                                int p3 = orderIndex[permutations5[j + 2]];
                                int p4 = orderIndex[permutations5[j + 3]];
                                int p5 = orderIndex[permutations5[j + 4]];

                                orderGeoIndex[0] = p1;
                                orderGeoIndex[1] = p2;
                                orderGeoIndex[2] = p3;
                                orderGeoIndex[3] = p4;
                                orderGeoIndex[4] = p5;

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
                            break;
                        case 5:
                            i1 = orderIndex[0];
                            i2 = orderIndex[1];
                            i3 = orderIndex[2];
                            i4 = orderIndex[3];
                            i5 = orderIndex[4];
                            i6 = orderIndex[5];
                            delivery = null;
                            orderGeoIndex[6] = shopIndex;

                            orderIndex[0] = i1;
                            orderIndex[1] = i2;
                            orderIndex[2] = i3;
                            orderIndex[3] = i4;
                            orderIndex[4] = i5;
                            orderIndex[5] = i6;

                            for (int j = 0; j < permutations6.Length; j += 6)
                            {
                                int p1 = orderIndex[permutations6[j]];
                                int p2 = orderIndex[permutations6[j + 1]];
                                int p3 = orderIndex[permutations6[j + 2]];
                                int p4 = orderIndex[permutations6[j + 3]];
                                int p5 = orderIndex[permutations6[j + 4]];
                                int p6 = orderIndex[permutations6[j + 5]];

                                orderGeoIndex[0] = p1;
                                orderGeoIndex[1] = p2;
                                orderGeoIndex[2] = p3;
                                orderGeoIndex[3] = p4;
                                orderGeoIndex[4] = p5;
                                orderGeoIndex[5] = p6;

                                orders[0] = contextOrders[p1];
                                orders[1] = contextOrders[p2];
                                orders[2] = contextOrders[p3];
                                orders[3] = contextOrders[p4];
                                orders[4] = contextOrders[p5];
                                orders[5] = contextOrders[p6];

                                int rcFind6 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 6, isLoop, geoData, out delivery6);
                                if (rcFind6 == 0 && (delivery == null || delivery6.Cost < delivery.Cost))
                                { delivery = delivery6; }
                            }

                            if (delivery != null)
                            { deliveries[count++] = delivery; }
                            break;
                        case 6:
                            i1 = orderIndex[0];
                            i2 = orderIndex[1];
                            i3 = orderIndex[2];
                            i4 = orderIndex[3];
                            i5 = orderIndex[4];
                            i6 = orderIndex[5];
                            i7 = orderIndex[6];
                            delivery = null;
                            orderGeoIndex[7] = shopIndex;

                            orderIndex[0] = i1;
                            orderIndex[1] = i2;
                            orderIndex[2] = i3;
                            orderIndex[3] = i4;
                            orderIndex[4] = i5;
                            orderIndex[5] = i6;
                            orderIndex[6] = i7;

                            for (int j = 0; j < permutations7.Length; j += 7)
                            {
                                int p1 = orderIndex[permutations7[j]];
                                int p2 = orderIndex[permutations7[j + 1]];
                                int p3 = orderIndex[permutations7[j + 2]];
                                int p4 = orderIndex[permutations7[j + 3]];
                                int p5 = orderIndex[permutations7[j + 4]];
                                int p6 = orderIndex[permutations7[j + 5]];
                                int p7 = orderIndex[permutations7[j + 6]];

                                orderGeoIndex[0] = p1;
                                orderGeoIndex[1] = p2;
                                orderGeoIndex[2] = p3;
                                orderGeoIndex[3] = p4;
                                orderGeoIndex[4] = p5;
                                orderGeoIndex[5] = p6;
                                orderGeoIndex[6] = p7;

                                orders[0] = contextOrders[p1];
                                orders[1] = contextOrders[p2];
                                orders[2] = contextOrders[p3];
                                orders[3] = contextOrders[p4];
                                orders[4] = contextOrders[p5];
                                orders[5] = contextOrders[p6];
                                orders[6] = contextOrders[p7];

                                int rcFind7 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 7, isLoop, geoData, out delivery7);
                                if (rcFind7 == 0 && (delivery == null || delivery7.Cost < delivery.Cost))
                                { delivery = delivery7; }
                            }

                            if (delivery != null)
                            { deliveries[count++] = delivery; }
                            break;
                        case 7:
                            i1 = orderIndex[0];
                            i2 = orderIndex[1];
                            i3 = orderIndex[2];
                            i4 = orderIndex[3];
                            i5 = orderIndex[4];
                            i6 = orderIndex[5];
                            i7 = orderIndex[6];
                            i8 = orderIndex[7];
                            delivery = null;
                            orderGeoIndex[8] = shopIndex;

                            orderIndex[0] = i1;
                            orderIndex[1] = i2;
                            orderIndex[2] = i3;
                            orderIndex[3] = i4;
                            orderIndex[4] = i5;
                            orderIndex[5] = i6;
                            orderIndex[6] = i7;
                            orderIndex[7] = i8;

                            for (int j = 0; j < permutations8.Length; j += 8)
                            {
                                int p1 = orderIndex[permutations8[j]];
                                int p2 = orderIndex[permutations8[j + 1]];
                                int p3 = orderIndex[permutations8[j + 2]];
                                int p4 = orderIndex[permutations8[j + 3]];
                                int p5 = orderIndex[permutations8[j + 4]];
                                int p6 = orderIndex[permutations8[j + 5]];
                                int p7 = orderIndex[permutations8[j + 6]];
                                int p8 = orderIndex[permutations8[j + 7]];

                                orderGeoIndex[0] = p1;
                                orderGeoIndex[1] = p2;
                                orderGeoIndex[2] = p3;
                                orderGeoIndex[3] = p4;
                                orderGeoIndex[4] = p5;
                                orderGeoIndex[5] = p6;
                                orderGeoIndex[6] = p7;
                                orderGeoIndex[7] = p8;

                                orders[0] = contextOrders[p1];
                                orders[1] = contextOrders[p2];
                                orders[2] = contextOrders[p3];
                                orders[3] = contextOrders[p4];
                                orders[4] = contextOrders[p5];
                                orders[5] = contextOrders[p6];
                                orders[6] = contextOrders[p7];
                                orders[7] = contextOrders[p8];

                                int rcFind8 = contextCourier.DeliveryCheck(calcTime, contextShop, orders, orderGeoIndex, 8, isLoop, geoData, out delivery8);
                                if (rcFind8 == 0 && (delivery == null || delivery8.Cost < delivery.Cost))
                                { delivery = delivery8; }
                            }

                            if (delivery != null)
                            { deliveries[count++] = delivery; }
                            break;
                    }

                    // 4.0 Продвиаемся к следующему набору индексов
                    rc = 40;
                    int index = orderIndex[pointer];

                    if (index < cnt)
                    {
                        if (pointer >= lev)
                        { orderIndex[pointer] = index + 1; }
                        else
                        { orderIndex[++pointer] = index + 1; }
                    }
                    else
                    {
                        orderIndex[pointer] = 0;
                        bool pointerChanged = false;

                        for (int j = pointer - 1; j >= 0; j--)
                        {
                            if (orderIndex[j] < cnt)
                            {
                                orderIndex[j]++;
                                pointer = j;
                                pointerChanged = true;
                                break;
                            }
                            else
                            { orderIndex[j] = 0; }
                        }

                        if (!pointerChanged)
                            break;
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
                Logger.WriteToLog(669, MessageSeverity.Error, string.Format(Messages.MSG_669, $"{nameof(RouteBuilder)}.{nameof(RouteBuilder.Build)}", rc, (ex.InnerException == null ? ex.Message : ex.InnerException.Message)));
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
                    Logger.WriteToLog(127, rc == 0 ? MessageSeverity.Info : MessageSeverity.Warn, string.Format(Messages.MSG_127, rc, context.ShopCourier.VehicleID, context.OrderCount, context.MaxRouteLength, context.SubsetCount, ArrayToString(context.InitOrderIndexes), sw.ElapsedMilliseconds, count));
                }
            }
        }

        /// <summary>
        /// Преобразование массива в строку
        /// </summary>
        /// <param name="id">Массив зачений</param>
        /// <param name="separator">Разделитель зачений</param>
        /// <returns>Строковое представление массива</returns>
        public static string ArrayToString<T>(T[] id, string separator = ", ")
        {
            if (id == null || id.Length <= 0)
                return "[]";
            StringBuilder sb = new StringBuilder(10 * id.Length);
            sb.Append("[");
            sb.Append(id[0]);
            for (int i = 1; i < id.Length; i++)
            {
                sb.Append(separator);
                sb.Append(id[i]);
            }
            sb.Append("]");
            return sb.ToString();
        }
    }
}
