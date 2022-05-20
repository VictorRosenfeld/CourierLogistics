
namespace DeliveryBuilder.Recalc
{
    using DeliveryBuilder.Couriers;
    //using DeliveryBuilder.Deliveries;
    using DeliveryBuilder.ExtraOrders;
    using DeliveryBuilder.Geo;
    using DeliveryBuilder.Orders;
    using DeliveryBuilder.SalesmanProblemLevels;
    using DeliveryBuilder.Shops;
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading;

    public class Calcs
    {

        /// <summary>
        /// Число проверяемых отгрузок на поток
        /// </summary>
        private const int DELIVERIES_PER_THREAD = 100000;

        /// <summary>
        /// Максимальное число потоков для построителя отгрузок из ThreadContext
        /// </summary>
        private const int MAX_DELIVERY_THREADS = 8;



        /// <summary>
        /// Построение всех расчетных контекстов
        /// </summary>
        /// <param name="connection">Открытое соединение</param>
        /// <param name="serviceId">ID LogisticsService</param>
        /// <param name="calcTime">Момент времени, на который делаются расчеты</param>
        /// <param name="shops">Магазины</param>
        /// <param name="allOrders">Заказы</param>
        /// <param name="allCouriers">Курьеры</param>
        /// <param name="limitations">Ограничения на длину маршрута по числу заказов</param>
        /// <returns>Контексты или null</returns>
        internal static CalcThreadContext[] GetCalcThreadContext(int serviceId, DateTime calcTime, Shop[] shops, AllOrdersEx allOrders, AllCouriersEx allCouriers, GeoData geoMng, SalesmanLevels limitations)
        {
            // 1. Инициализация
            int rc = 1;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (shops == null || shops.Length <= 0)
                    return null;
                if (allOrders == null || !allOrders.IsCreated)
                    return null;
                if (allCouriers == null || !allCouriers.IsCreated)
                    return null;
                if (geoMng == null || !geoMng.IsCreated)
                    return null;
                if (limitations == null || !limitations.IsCreated)
                    return null;

#if debug
            Logger.WriteToLog(201, $"GetCalcThreadContext. service_id = {serviceId}. Enter...", 0);
#endif

                // 3. Строим контексты всех потоков
                rc = 3;
                int size = shops.Length * allCouriers.BaseKeys.Length;
                Recalc.CalcThreadContext[] context = new Recalc.CalcThreadContext[size];
                int contextCount = 0;

                for (int i = 0; i < shops.Length; i++)
                {
                    // 3.1 Извлекаем магазин
                    rc = 31;
                    Shop shop = shops[i];

                    // 3.2 Извлекаем заказы магазина
                    rc = 32;
                    //Order[] shopOrders = allOrders.GetShopOrders(shop.Id);
                    Order[] shopOrders = allOrders.GetShopOrders(shop.Id, calcTime);
                    if (shopOrders == null || shopOrders.Length <= 0)
                        continue;

                    // 3.3 Извлекаем курьеров магазина
                    rc = 33;
                    Courier[] shopCouriers = allCouriers.GetShopCouriers(shop.Id, true);
                    if (shopCouriers == null || shopCouriers.Length <= 0)
                        continue;

                    // 3.4 Выбираем возможные способы доставки
                    rc = 34;
                    int[] courierVehicleTypes = AllCouriersEx.GetCourierVehicleTypes(shopCouriers);
                    if (courierVehicleTypes == null || courierVehicleTypes.Length <= 0)
                        continue;

                    int[] orderVehicleTypes = AllOrdersEx.GetOrderVehicleTypes(shopOrders);
                    if (orderVehicleTypes == null || orderVehicleTypes.Length <= 0)
                        continue;

                    Array.Sort(courierVehicleTypes);
                    int vehicleTypeCount = 0;

                    for (int j = 0; j < orderVehicleTypes.Length; j++)
                    {
                        if (Array.BinarySearch(courierVehicleTypes, orderVehicleTypes[j]) >= 0)
                            orderVehicleTypes[vehicleTypeCount++] = orderVehicleTypes[j];
                    }

                    if (vehicleTypeCount <= 0)
                        continue;

                    // 3.5 Раскладываем заказы по способам доставки
                    //     отбрасывая заказы, для которых нет доступного курьера
                    rc = 35;
                    Array.Sort(orderVehicleTypes, 0, vehicleTypeCount);
                    Order[,] vehicleTypeOrders = new Order[vehicleTypeCount, shopOrders.Length];
                    int[] vehicleTypeOrderCount = new int[vehicleTypeCount];

                    for (int j = 0; j < shopOrders.Length; j++)
                    {
                        Order order = shopOrders[j];
                        int[] vehicleTypes = order.VehicleTypes;
                        if (vehicleTypes != null)
                        {
                            for (int k = 0; k < vehicleTypes.Length; k++)
                            {
                                int index = Array.BinarySearch(orderVehicleTypes, 0, vehicleTypeCount, vehicleTypes[k]);
                                if (index >= 0)
                                {
                                    vehicleTypeOrders[index, vehicleTypeOrderCount[index]++] = order;
                                }
                            }
                        }
                    }

                    // 3.6 Добавляем контексты потоков
                    rc = 36;
                    for (int j = 0; j < vehicleTypeCount; j++)
                    {
                        int count = vehicleTypeOrderCount[j];
                        if (count > 0)
                        {
                            Order[] contextOrders = new Order[count];
                            for (int k = 0; k < count; k++)
                            {
                                contextOrders[k] = vehicleTypeOrders[j, k];
                            }

                            //int maxRouteLength = limitations.GetRouteLength(count);

                            //Courier courier = allCouriers.CreateReferenceCourier(orderVehicleTypes[j]);
                            Courier courier = allCouriers.FindFirstShopCourierByType(shop.Id, orderVehicleTypes[j]);
                            if (courier != null)
                            {
                                contextOrders = FilterOrdersOnMaxWeight(courier.MaxOrderWeight, contextOrders);
                                if (contextOrders != null && contextOrders.Length > 0)
                                {
                                    //if (courier.MaxOrderCount < maxRouteLength)
                                    //    maxRouteLength = courier.MaxOrderCount;
                                    Point[,] geoData;
                                    int rc1 = geoMng.Select(courier.YandexType, shop, contextOrders, out geoData);
                                    if (rc1 == 0)
                                    {
                                        context[contextCount++] = new CalcThreadContext(serviceId, calcTime, shop, contextOrders, courier, geoMng, limitations, null);
                                    }
                                }
                            }
                            else
                            {
#if debug
                            Logger.WriteToLog(204, $"GetCalcThreadContext. service_id = {serviceId}. shop_id = {shop.Id}. orderVehicleType = {orderVehicleTypes[j]}", 0);
#endif
                            }
                        }
                    }
                }

                if (contextCount < context.Length)
                {
                    Array.Resize(ref context, contextCount);
                }
                rc = 0;

#if debug
            Logger.WriteToLog(202, $"GetCalcThreadContext. service_id = {serviceId}. Exit. context count = {contextCount}", 0);
#endif

                // 4. Выход - Ok
                return context;
            }
            catch (Exception ex)
            {
#if debug
            Logger.WriteToLog(203, $"GetCalcThreadContext. service_id = {serviceId}. rc = {rc}. Exception = {ex.Message}", 0);
#endif
                return null;
            }
        }

        /// <summary>
        /// Контекст-построитель отгрузок
        /// в отдельном потоке
        /// </summary>
        /// <param name="status">Контекст потока</param>
        public static void CalcThreadEs(object status)
        {
            // 1. Инициализация
            int rc = 1;
            int rc1 = 1;
            CalcThreadContext calcContext = status as CalcThreadContext;
            //Thread.BeginThreadAffinity();

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (calcContext == null ||
                    calcContext.OrderCount <= 0 ||
                    calcContext.ShopCourier == null ||
                    calcContext.ShopCourier.MaxOrderCount <= 0 ||
                    calcContext.ShopFrom == null ||
                    calcContext.Limitations == null)
                    return;

#if debug
                Logger.WriteToLog(301, $"CalcThreadEs enter. order_count = {calcContext.OrderCount}, shop_id = {calcContext.ShopFrom.Id}, courier_id = {calcContext.ShopCourier.Id}", 0);
#endif

                // 3. Анализируем состояние
                rc = 3;
                int orderCount = calcContext.OrderCount;
                SalesmanLevels limitations = calcContext.Limitations;
                int courierMaxOrderCount = calcContext.ShopCourier.MaxOrderCount;
                int yandexTypeId = calcContext.ShopCourier.YandexType;

                // Можно сделать полный перебор
                int level = calcContext.Limitations.GetRouteLength(orderCount);
                Point[,] geoData;

                if (level >= courierMaxOrderCount)
                {
                    rc = 31;
                    rc1 = calcContext.GeoMng.Select(yandexTypeId, calcContext.ShopFrom, calcContext.Orders, out geoData);
                    if (rc1 != 0)
                    {
                        rc = 100 * rc + rc1;
                        return;
                    }

                    ThreadContext iterContext = new ThreadContext(calcContext.ServiceId, calcContext.CalcTime, courierMaxOrderCount, calcContext.ShopFrom, calcContext.Orders, calcContext.ShopCourier, geoData, null);
                    CalcThreadFullEx(iterContext);
                    calcContext.Deliveries = iterContext.Deliveries;
                    rc = (iterContext.ExitCode == 0 ? 0 : 100000 * rc + calcContext.ExitCode);
                    return;
                }

                // Прочие случаи
                int maxOrderCount = calcContext.ShopCourier.MaxOrderCount;
                int startLevel = (maxOrderCount <= 4 ? maxOrderCount : 4);
                int startOrderCount;
                if (maxOrderCount <= 4)
                {
                    startLevel = maxOrderCount;
                    startOrderCount = limitations.GetRouteMaxOrders(startLevel);
                }
                else
                {
                    startLevel = 4;
                    if (maxOrderCount == 5)
                    { startOrderCount = calcContext.Config.Cloud.Size5; }    // 55 - 65
                    else if (maxOrderCount == 6)
                    { startOrderCount = calcContext.Config.Cloud.Size6; }    // 45 - 56
                    else if (maxOrderCount == 7)
                    { startOrderCount = calcContext.Config.Cloud.Size7; }    // 35 - 51
                    else
                    { startOrderCount = calcContext.Config.Cloud.Size8; }    // 30 - 48
                }

                // 4. Готовим заказы для дальнейшего использования
                rc = 4;
                //int maxOrders = calcContext.Limitations.GetRouteMaxOrders(maxOrderCount);
                Order[] iterationOrders = (Order[])calcContext.Orders.Clone();
                int[] iterationOrderId = new int[calcContext.OrderCount];

                for (int i = 0; i < iterationOrders.Length; i++)
                { iterationOrderId[i] = iterationOrders[i].Id; }

                Array.Sort(iterationOrderId, iterationOrders);

                // 5. Цикл построения маршрутов
                rc = 5;
                CourierDeliveryInfo[] allDeliveries = new CourierDeliveryInfo[500000];
                int deliveryCount = 0;
                int iterationOrderCount = iterationOrders.Length;
                Order[] threadContextOrders;

                while (iterationOrderCount > 0)
                {
#if debug
                    Logger.WriteToLog(305, $"CalcThreadEs while 5.1. iterationOrderCount = {iterationOrderCount}", 0);
#endif
                    // 5. Выбор заказов и параметров для очередного расчета
                    rc = 5;
                    level = calcContext.Limitations.GetRouteLength(iterationOrderCount);

                    if (level >= courierMaxOrderCount)
                    {
                        // 5.0 Для оставшихся точек можно сделать полный перебор
                        rc = 50;
                        level = courierMaxOrderCount;
                        threadContextOrders = iterationOrders;
                    }
                    else if (iterationOrderCount > startOrderCount)
                    {
                        // 5.1 Выбираем сгусток, его глубину и заказы для очередного расчета
                        rc = 51;
                        level = startLevel;
                        double[,] geoDist = GeoDistance.CalcDistance(iterationOrders);
                        //rc1 = OrdersCloud.FindCloud(iterationOrders, startOrderCount, 1300, 0.5, geoDist, out threadContextOrders);
                        rc1 = OrdersCloud.FindCloud(iterationOrders, startOrderCount, calcContext.Config.Cloud.Radius, calcContext.Config.Cloud.Delta, geoDist, out threadContextOrders);
#if debug
                        Logger.WriteToLog(305, $"CalcThreadEs while 5.1. iterationOrderCount = {iterationOrderCount}, FindCloud.rc = {rc1} Cloud.Orders = {(threadContextOrders == null ? 0 : threadContextOrders.Length)}", 0);
#endif
                        if (rc1 != 0 || threadContextOrders == null || threadContextOrders.Length <= 0)
                        {
                            threadContextOrders = new Order[startOrderCount];
                            Array.Copy(iterationOrders, threadContextOrders, startOrderCount);
#if debug
                            Logger.WriteToLog(3051, $"CalcThreadEs while 5.1 FindCloud failed. iterationOrderCount = {iterationOrderCount}, FindCloud.rc = {rc1} Cloud.Orders = {(threadContextOrders == null ? 0 : threadContextOrders.Length)}", 2);
#endif
                        }
                        else
                        {
                            int maxLevel = calcContext.Limitations.GetRouteLength(threadContextOrders.Length);
                            if (maxLevel > level)
                            { level = maxLevel; }
                            if (level > maxOrderCount)
                            { level = maxOrderCount; }
                        }
                    }
                    else if (level > startLevel)
                    {
                        if (level > maxOrderCount)
                        { level = maxOrderCount; }
                        threadContextOrders = iterationOrders;
                    }
                    else // if (iterationOrderCount == startOrderCount)
                    {
                        level = startLevel;
                        threadContextOrders = iterationOrders;
                    }

#if debug
                    Logger.WriteToLog(306, $"CalcThreadEs while 5.2. iterationOrderCount = {iterationOrderCount}, level = {level}, threadContextOrders = {(threadContextOrders == null ? 0 : threadContextOrders.Length)}", 0);
#endif

                    // 5.2 Запрашиваем гео-данные
                    rc = 52;
                    int[] threadContextOrderId = new int[threadContextOrders.Length];
                    for (int i = 0; i < threadContextOrders.Length; i++)
                        threadContextOrderId[i] = threadContextOrders[i].Id;
                    //Array.Sort(threadContextOrders, CompareByOrderId);
                    Array.Sort(threadContextOrderId, threadContextOrders);

                    rc1 = calcContext.GeoMng.Select(yandexTypeId, calcContext.ShopFrom, threadContextOrders, out geoData);
                    if (rc1 != 0)
                    {
                        rc = 100 * rc + rc1;
                        return;
                    }

                    // 5.3 Строим контекст
                    rc = 53;
                    ThreadContext iterContext =
                        new ThreadContext(
                                            calcContext.ServiceId,
                                            calcContext.CalcTime,
                                            level,
                                            calcContext.ShopFrom,
                                            threadContextOrders,
                                            calcContext.ShopCourier,
                                            geoData,
                                            null
                                         );

                    // 5.4 Строим маршруты полным перебором
                    rc = 54;
                    //CalcThreadEx(iterContext);
                    //CalcThreadFull(iterContext);
                    CalcThreadFullEx(iterContext);
                    if (iterContext.ExitCode != 0)
                    {
                        rc = (iterContext.ExitCode == 0 ? 0 : 100000 * rc + calcContext.ExitCode);
                        return;
                    }

                    // 5.5 Расширяем отгрузки, если требуется
                    rc = 55;
                    CourierDeliveryInfo[] iterDeliveries = iterContext.Deliveries;
                    if (level < courierMaxOrderCount)
                    {
#if debug
                        Logger.WriteToLog(306, $"CalcThreadEs while 5.5 before. iterDeliveries = {iterDeliveries.Length}, level = {level}, courierMaxOrderCount = {courierMaxOrderCount}", 0);
#endif
                        //rc1 = DilateRoutes(ref iterDeliveries, level, courierMaxOrderCount, threadContextOrders, geoData);
                        rc1 = DilateRoutesMultuthread(ref iterDeliveries, level, courierMaxOrderCount, threadContextOrders, geoData);
#if debug
                        Logger.WriteToLog(306, $"CalcThreadEs while 5.5 after. iterDeliveries = {iterDeliveries.Length}, rc1 = {rc1}", 0);
#endif
                    }

                    // 5.6 Пополняем множество построенных отгрузок
                    rc = 56;
                    if (deliveryCount + iterDeliveries.Length > allDeliveries.Length)
                    {
                        int extraSize = 2 * iterDeliveries.Length;
                        Array.Resize(ref allDeliveries, allDeliveries.Length + extraSize);
                    }

                    iterDeliveries.CopyTo(allDeliveries, deliveryCount);
                    deliveryCount += iterDeliveries.Length;

                    // 5.7 Выбираем заказы, присутствующие в построенных отгрузках
                    rc = 57;
                    bool[] iterationOrderFlag = new bool[iterationOrders.Length];

                    for (int i = 0; i < iterDeliveries.Length; i++)
                    {
                        Order[] deiveryOrders = iterDeliveries[i].Orders;

                        for (int j = 0; j < deiveryOrders.Length; j++)
                        {
                            int index = Array.BinarySearch(iterationOrderId, deiveryOrders[j].Id);
                            if (index >= 0)
                                iterationOrderFlag[index] = true;
                        }
                    }

                    iterationOrderCount = 0;
                    for (int i = 0; i < iterationOrders.Length; i++)
                    {
                        if (iterationOrderFlag[i])
                            continue;
                        if (Array.BinarySearch(threadContextOrderId, iterationOrders[i].Id) >= 0)
                            continue;
                        iterationOrders[iterationOrderCount] = iterationOrders[i];
                        iterationOrderId[iterationOrderCount++] = iterationOrders[i].Id;
                    }

                    if (iterationOrderCount <= 0)
                        break;

                    if (iterationOrderCount < iterationOrders.Length)
                    {
                        Array.Resize(ref iterationOrders, iterationOrderCount);
                        Array.Resize(ref iterationOrderId, iterationOrderCount);
                    }

#if debug
                    Logger.WriteToLog(306, $"CalcThreadEs while end. iterationOrderCount = {iterationOrderCount}", 0);
#endif
                }

                if (deliveryCount < allDeliveries.Length)
                {
                    Array.Resize(ref allDeliveries, deliveryCount);
                }

                RemoveDuplicateDeliveries(ref allDeliveries, courierMaxOrderCount);

                calcContext.Deliveries = allDeliveries;

                // 8. Выход - Ok
                rc = 0;
            }
            catch (Exception ex)
            {
#if debug
                Logger.WriteToLog(303, $"CalcThreadEs. service_id = {calcContext.ServiceId}. rc = {rc}. order_count = {calcContext.OrderCount}, shop_id = {calcContext.ShopFrom.Id}, courier_id = {calcContext.ShopCourier.Id} Exception = {ex.Message}", 2);
#endif
            }
            finally
            {
                //Thread.EndThreadAffinity();

                if (calcContext != null)
                {
#if debug
                    Logger.WriteToLog(302, $"CalcThreadEs exit rc = {rc}. order_count = {calcContext.OrderCount}, shop_id = {calcContext.ShopFrom.Id}, courier_id = {calcContext.ShopCourier.Id}", 0);
#endif
                    calcContext.ExitCode = rc;

                    if (calcContext.SyncEvent != null)
                        calcContext.SyncEvent.Set();
                }
            }
        }


        /// <summary>
        /// Фильтрация заказов по максимально возможному весу
        /// </summary>
        /// <param name="maxOrderWeight">Максимальный допустимый вес заказа, кг</param>
        /// <param name="orders">Фильтруемые заказы</param>
        /// <returns>Отфильтрованные заказы или null</returns>
        private static Order[] FilterOrdersOnMaxWeight(double maxOrderWeight, Order[] orders)
        {
            try
            {
                // 2. Проверяем исходные данные
                if (orders == null || orders.Length <= 0)
                    return orders;

                // 3. Цикл фильтрации
                Order[] filteredOrders = new Order[orders.Length];
                int count = 0;

                for (int i = 0; i < orders.Length; i++)
                {
                    Order order = orders[i];
                    if (order.Weight <= maxOrderWeight)
                        filteredOrders[count++] = order;
                }

                if (count < filteredOrders.Length)
                {
                    Array.Resize(ref filteredOrders, count);
                }

                // 4. Выход
                return filteredOrders;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Контекст-построитель отгрузок полным перебором
        /// в отдельном потоке
        /// </summary>
        /// <param name="status">Контекст потока</param>
        private static void CalcThreadFullEx(object status)
        {
            // 1. Инициализация
            int rc = 1;
            ThreadContext context = status as ThreadContext;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (context == null ||
                    context.OrderCount <= 0 ||
                    context.ShopCourier == null ||
                    context.ShopFrom == null ||
                    context.Orders == null || context.Orders.Length <= 0 ||
                    context.MaxRouteLength <= 0 || context.MaxRouteLength > 8)
                    return;

                // 3. Вызываем частный метод
                rc = 3;
                switch (context.MaxRouteLength)
                {
                    case 1:
                    case 2:
                        CalcThread(RouteBuilder.BuildEx2, status);
                        break;
                    case 3:
                        CalcThread(RouteBuilder.BuildEx3, status);
                        break;
                    case 4:
                        CalcThread(RouteBuilder.BuildEx4, status);
                        break;
                    case 5:
                        CalcThread(RouteBuilder.BuildEx5, status);
                        break;
                    case 6:
                        CalcThread(RouteBuilder.BuildEx6, status);
                        break;
                    case 7:
                        CalcThread(RouteBuilder.BuildEx7, status);
                        break;
                    default:
                        CalcThread(RouteBuilder.BuildEx8, status);
                        break;
                }

                // 8. Выход - Ok
                rc = 0;
            }
            catch (Exception ex)
            {
#if debug
                Logger.WriteToLog(303, $"CalcThreadFullEx exception. service_id = {context.ServiceId}. rc = {rc}. order_count = {context.OrderCount}, shop_id = {context.ShopFrom.Id}, courier_id = {context.ShopCourier.Id}, level = {context.MaxRouteLength} Exception = {ex.Message}", 2);
#endif
            }
            finally
            {
                if (context != null)
                {
                    if (context.SyncEvent != null)
                    {
                        try
                        { context.SyncEvent.Set(); }
                        catch { }
                    }
                }
            }
        }

        /// <summary>
        /// Контекст-построитель отгрузок
        /// полным перебором в отдельном потоке
        /// </summary>
        /// <param name="callback">Метод построения отгрузок</param>
        /// <param name="status">Контекст потока</param>
        private static void CalcThread(WaitCallback callback, object status)
        {
            // 1. Инициализация
            int rc = 1;
            ThreadContext context = status as ThreadContext;
            ThreadContextEx[] allCountextEx = null;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (context == null ||
                    context.OrderCount <= 0 ||
                    context.ShopCourier == null ||
                    context.ShopFrom == null ||
                    context.Orders == null || context.Orders.Length <= 0 ||
                    context.MaxRouteLength < 1 || context.MaxRouteLength > 8)
                    return;

#if debug
                Logger.WriteToLog(301, $"CalcThread enter. service_id = {context.ServiceId}. order_count = {context.OrderCount}, shop_id = {context.ShopFrom.Id}, courier_id = {context.ShopCourier.Id}, level = {context.MaxRouteLength}", 0);
#endif

                // 3. Вычисляем число потоков для построения отгрузок из ThreadContext
                rc = 3;
                int level = context.MaxRouteLength;
                int orderCount = context.Orders.Length;
                long size = orderCount;
                if (orderCount >= 2 && level >= 2)
                { size += (orderCount - 1) * orderCount; }
                if (orderCount >= 3 && level >= 3)
                { size += (orderCount - 2) * (orderCount - 1) * orderCount; }
                if (orderCount >= 4 && level >= 4)
                { size += (orderCount - 3) * (orderCount - 2) * (orderCount - 1) * orderCount; }
                if (orderCount >= 5 && level >= 5)
                { size += (orderCount - 4) * (orderCount - 3) * (orderCount - 2) * (orderCount - 1) * orderCount; }
                if (orderCount >= 6 && level >= 6)
                { size += (orderCount - 5) * (orderCount - 4) * (orderCount - 3) * (orderCount - 2) * (orderCount - 1) * orderCount; }
                if (orderCount >= 7 && level >= 7)
                { size += (orderCount - 6) * (orderCount - 5) * (orderCount - 4) * (orderCount - 3) * (orderCount - 2) * (orderCount - 1) * orderCount; }
                if (orderCount >= 8 && level >= 8)
                { size += (orderCount - 7) * (orderCount - 6) * (orderCount - 5) * (orderCount - 4) * (orderCount - 3) * (orderCount - 2) * (orderCount - 1) * orderCount; }

                int threadCount = (int)(size + DELIVERIES_PER_THREAD - 1) / DELIVERIES_PER_THREAD;
                if (threadCount > MAX_DELIVERY_THREADS)
                    threadCount = MAX_DELIVERY_THREADS;

                // 4. Требуется всего один поток
                rc = 4;
                if (threadCount <= 1)
                {
                    ThreadContextEx contextEx = new ThreadContextEx(context, null, null, 0, 1);
                    allCountextEx = new ThreadContextEx[] { contextEx };
                    callback(contextEx);
                }
                else
                {
                    // 5. Требуется несколько потоков
                    rc = 5;
                    allCountextEx = new ThreadContextEx[threadCount];
                    for (int i = 0; i < threadCount; i++)
                    {
                        ManualResetEvent syncEvent = new ManualResetEvent(false);
                        int k = i;
                        ThreadContextEx contextEx = new ThreadContextEx(context, syncEvent, null, k, threadCount);
                        allCountextEx[k] = contextEx;
                        //ThreadPool.QueueUserWorkItem(callback, contextEx);
                        Thread th;
                        switch (level)
                        {
                            case 1:
                            case 2:
                                th = new Thread(RouteBuilder.BuildEx2);
                                break;
                            case 3:
                                th = new Thread(RouteBuilder.BuildEx3);
                                break;
                            case 4:
                                th = new Thread(RouteBuilder.BuildEx4);
                                break;
                            case 5:
                                th = new Thread(RouteBuilder.BuildEx5);
                                break;
                            case 6:
                                th = new Thread(RouteBuilder.BuildEx6);
                                break;
                            case 7:
                                th = new Thread(RouteBuilder.BuildEx7);
                                break;
                            //case 8:
                            default:
                                th = new Thread(RouteBuilder.BuildEx8);
                                break;
                        }
                        th.Start(contextEx);
                    }

                    for (int i = 0; i < threadCount; i++)
                    {
                        allCountextEx[i].SyncEvent.WaitOne();
                        allCountextEx[i].SyncEvent.Dispose();
                        allCountextEx[i].SyncEvent = null;
                    }
                }

                // 6. Строим общий результат
                rc = 6;
                CourierDeliveryInfo[] deliveries = null;
                int rc1 = 0;

                if (threadCount <= 1)
                {
                    deliveries = allCountextEx[0].Deliveries;
                    rc1 = allCountextEx[0].ExitCode;
                }
                else
                {
                    // 6.1 Подсчитываем число отгрузок
                    rc = 61;
                    size = 0;
                    for (int i = 0; i < threadCount; i++)
                    {
                        var contextEx = allCountextEx[i];
                        if (contextEx.ExitCode != 0)
                        {
                            rc1 = contextEx.ExitCode;
                        }
                        else
                        {
                            size += contextEx.DeliveryCount;
                        }
                    }

                    // 6.2 Объединяем все отгрузки
                    rc = 62;

                    if (size > 0)
                    {
                        deliveries = new CourierDeliveryInfo[size];
                        size = 0;

                        for (int i = 0; i < threadCount; i++)
                        {
                            var contextEx = allCountextEx[i];
                            if (contextEx.ExitCode == 0 && contextEx.DeliveryCount > 0)
                            {
                                contextEx.Deliveries.CopyTo(deliveries, size);
                                size += contextEx.DeliveryCount;
                            }
                        }
                    }
                }

                context.Deliveries = deliveries;
                allCountextEx = null;

                if (rc1 != 0)
                {
                    rc = 100000 * rc + rc1;
                    return;
                }

                // 7. Выход - Ok
                rc = 0;
            }
            catch (Exception ex)
            {
#if debug
                Logger.WriteToLog(303, $"CalcThread exception. service_id = {context.ServiceId}. rc = {rc}. order_count = {context.OrderCount}, shop_id = {context.ShopFrom.Id}, courier_id = {context.ShopCourier.Id}, level = {context.MaxRouteLength} Exception = {ex.Message}", 2);
#endif
            }
            finally
            {
                if (context != null)
                {
#if debug
                    Logger.WriteToLog(302, $"CalcThread exit rc = {rc}. service_id = {context.ServiceId}. order_count = {context.OrderCount}, shop_id = {context.ShopFrom.Id}, courier_id = {context.ShopCourier.Id}, level = {context.MaxRouteLength}", 0);
#endif
                    context.ExitCode = rc;

                    if (allCountextEx != null && allCountextEx.Length > 0)
                    {
                        for (int i = 0; i < allCountextEx.Length; i++)
                        {
                            ThreadContextEx contextEx = allCountextEx[i];
                            ManualResetEvent syncEvent = contextEx.SyncEvent;
                            if (syncEvent != null)
                            {
                                syncEvent.Dispose();
                                contextEx.SyncEvent = null;
                            }
                        }
                    }

                    if (context.SyncEvent != null)
                        context.SyncEvent.Set();
                }
            }
        }

        /// <summary>
        /// Убираем дубли отгрузок
        /// </summary>
        /// <param name="deliveries">Отгрузки</param>
        /// <param name="maxOrderCount">Максимальное число заказов в отгрузке</param>
        /// <returns>0 - дубли удалены; иначе - дубли не удалены</returns>
        private static int RemoveDuplicateDeliveries(ref CourierDeliveryInfo[] deliveries, int maxOrderCount)
        {
            // 1. Инициализация
            int rc = 1;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (deliveries == null || deliveries.Length <= 0)
                    return rc;
                if (maxOrderCount <= 0)
                    return rc;

                // 3. Отбираем отгрузки с точностью до перестановки
                rc = 3;
                int[] deliveryOrderId = new int[maxOrderCount];
                StringBuilder keyBuffer = new StringBuilder(16 * maxOrderCount);

                Dictionary<string, CourierDeliveryInfo> uniqueDeliveries = new Dictionary<string, CourierDeliveryInfo>(deliveries.Length);
                CourierDeliveryInfo tryGetDelivery;

                for (int i = 0; i < deliveries.Length; i++)
                {
                    CourierDeliveryInfo delivery = deliveries[i];
                    int vehicleId = delivery.DeliveryCourier.VehicleID;
                    Order[] deliveryOrders = delivery.Orders;
                    int deliveryOrderCount = deliveryOrders.Length;

                    for (int j = 0; j < deliveryOrderCount; j++)
                    {
                        deliveryOrderId[j] = deliveryOrders[j].Id;
                    }

                    Array.Sort(deliveryOrderId, 0, deliveryOrderCount);

                    keyBuffer.Length = 0;
                    keyBuffer.Append(vehicleId);
                    keyBuffer.Append('.');
                    keyBuffer.Append(deliveryOrderId[0]);

                    for (int j = 1; j < deliveryOrderCount; j++)
                    {
                        keyBuffer.Append('.');
                        keyBuffer.Append(deliveryOrderId[j]);
                    }

                    string key = keyBuffer.ToString();
                    if (uniqueDeliveries.TryGetValue(key, out tryGetDelivery))
                    {
                        if (delivery.Cost < tryGetDelivery.Cost)
                            uniqueDeliveries[key] = delivery;
                    }
                    else
                    {
                        uniqueDeliveries.Add(key, delivery);
                    }
                }

                if (uniqueDeliveries.Count < deliveries.Length)
                {
                    uniqueDeliveries.Values.CopyTo(deliveries, 0);
                    Array.Resize(ref deliveries, uniqueDeliveries.Count);
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
        /// Расширение исходных отгрузок до заданной длины
        /// с сохранением исходного порядка доставки заказов
        /// </summary>
        /// <param name="deliveries">Исходные отгрузки</param>
        /// <param name="fromLevel">Длина расширяемых отгрузок</param>
        /// <param name="toLevel">Длина, до которой расширяются отгрузки</param>
        /// <param name="orders">Отсортированные по Id заказы</param>
        /// <param name="geoData">Гео-данные заказов</param>
        /// <returns>0 - исходные отгрузки расширены; исходные отгрузки не расширены</returns>
        private static int DilateRoutesMultuthread(ref CourierDeliveryInfo[] deliveries, int fromLevel, int toLevel, Order[] orders, Point[,] geoData)
        {
            // 1. Инициализация
            int rc = 1;
            int rc1 = 1;
            ManualResetEvent[] syncEvents = null;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (deliveries == null || deliveries.Length <= 0)
                    return rc;
                if (fromLevel <= 0 || fromLevel > toLevel)
                    return rc;
                if (fromLevel == toLevel)
                    return rc = 0;
                if (orders == null || orders.Length <= 0)
                    return rc;
                int orderCount = orders.Length;
                if (geoData == null || geoData.GetLength(0) != orderCount + 1 || geoData.GetLength(1) != orderCount + 1)
                    return rc;
#if debug
                Logger.WriteToLog(804, $"DilateRoutesMultuthread enter. = {rc}. fromLevel = {fromLevel}, toLevel = {toLevel}, orders = {orders.Length}", 0);
#endif

                // 3. Отбираем маршруты с исходным уровнем
                rc = 3;
                CourierDeliveryInfo[] iterDelivery = new CourierDeliveryInfo[deliveries.Length];
                int count = 0;

                for (int i = 0; i < deliveries.Length; i++)
                {
                    if (deliveries[i].OrderCount == fromLevel)
                    {
                        iterDelivery[count++] = deliveries[i];
                    }
                }

                if (count <= 0)
                    return rc = 0;

                if (count < iterDelivery.Length)
                {
                    Array.Resize(ref iterDelivery, count);
                }

                // 4. Опренделяем число потоков для расширения маршрутов
                rc = 4;
                count = count * (fromLevel + 1) * (orderCount - fromLevel);
                int threadCount = (count + 99999) / 100000;
                if (threadCount > 10)
                    threadCount = 10;

                //if (threadCount >= 8)
                //    threadCount = 7;

                // 5. Строим расширения маршрутов
                rc = 5;
                rc1 = 0;

                if (threadCount <= 1)
                {
                    DilateRoutesContext context = new DilateRoutesContext
                        (
                            iterDelivery,
                            0,
                            1,
                            fromLevel,
                            toLevel,
                            orders,
                            geoData,
                            null
                        );
                    DilateRoutesThread(context);
                    rc1 = context.ExitCode;
                    if (rc1 == 0)
                    {
                        count = context.ExtendedCount;
                        if (count > 0)
                        {
                            int index = deliveries.Length;
                            Array.Resize(ref deliveries, index + count);
                            context.ExtendedDeliveries.CopyTo(deliveries, index);
                        }
                    }
                }
                else
                {
                    syncEvents = new ManualResetEvent[threadCount];
                    DilateRoutesContext[] threadContext = new DilateRoutesContext[threadCount];
                    //ThreadPool.QueueUserWorkItem(NullThread);
                    //Thread.BeginThreadAffinity();

                    for (int i = 0; i < threadCount; i++)
                    {
                        int m = i;
                        ManualResetEvent sevent = new ManualResetEvent(false);
                        syncEvents[m] = sevent;
                        DilateRoutesContext context = new DilateRoutesContext
                            (
                                iterDelivery,
                                m,
                                threadCount,
                                fromLevel,
                                toLevel,
                                orders,
                                geoData,
                                syncEvents[m]
                            );
                        threadContext[m] = context;
                        ThreadPool.QueueUserWorkItem(DilateRoutesThread, threadContext[m]);
                        //Thread.Sleep(5);
                    }


                    count = 0;
                    for (int i = 0; i < threadCount; i++)
                    {
                        syncEvents[i].WaitOne();
                        syncEvents[i].Dispose();
                        syncEvents[i] = null;
                        if (threadContext[i].ExitCode == 0)
                        {
                            count += threadContext[i].ExtendedCount;
                        }
                        else
                        {
                            rc1 = threadContext[i].ExitCode;
                        }
                    }
                    //Thread.EndThreadAffinity();

                    syncEvents = null;

                    if (count > 0)
                    {
                        int index = deliveries.Length;
                        Array.Resize(ref deliveries, index + count);

                        for (int i = 0; i < threadCount; i++)
                        {
                            if (threadContext[i].ExitCode == 0 && threadContext[i].ExtendedCount > 0)
                            {
                                threadContext[i].ExtendedDeliveries.CopyTo(deliveries, index);
                                index += threadContext[i].ExtendedCount;
                            }
                        }
                    }

                    threadContext = null;
                }

                iterDelivery = null;
                rc = (rc1 == 0 ? 0 : 10000 * rc + rc1);

                // 6. Выход...
                return rc;
            }
            catch (Exception ex)
            {
#if debug
                Logger.WriteToLog(806, $"DilateRoutesMultuthread.rc = {rc}. fromLevel = {fromLevel}, toLevel = {toLevel}, orders = {orders.Length}, Exception = {ex.ToString()}", 2);
#endif

                return rc;
            }
            finally
            {
                if (syncEvents != null && syncEvents.Length > 0)
                {
                    for (int i = 0; i < syncEvents.Length; i++)
                    {
                        if (syncEvents[i] != null)
                        {
                            syncEvents[i].Dispose();
                            syncEvents[i] = null;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Расширение исходных отгрузок до заданной длины
        /// с сохранением исходного порядка доставки заказов
        /// </summary>
        /// <param name="deliveries">Исходные отгрузки</param>
        /// <param name="fromLevel">Длина расширяемых отгрузок</param>
        /// <param name="toLevel">Длина, до которой расширяются отгрузки</param>
        /// <param name="orders">Отсортированные по Id заказы</param>
        /// <param name="geoData">Гео-данные заказов</param>
        /// <returns>0 - исходные отгрузки расширены; исходные отгрузки не расширены</returns>
        private static void DilateRoutesThread(object status)
        {
            // 1. Инициализация
            int rc = 1;
            int rc1 = 1;
            DilateRoutesContext context = status as DilateRoutesContext;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (context == null)
                    return;

                CourierDeliveryInfo[] deliveries = context.SourceDeliveries;
                int fromLevel = context.FromLevel;
                int toLevel = context.ToLevel;
                Order[] orders = context.Orders;
                Point[,] geoData = context.GeoData;
                int startIndex = context.StartIndex;
                int step = context.Step;

                if (deliveries == null || deliveries.Length <= 0)
                    return;
                if (fromLevel <= 0 || fromLevel > toLevel)
                    return;
                if (fromLevel == toLevel)
                {
                    rc = 0;
                    return;
                }
                if (orders == null || orders.Length <= 0)
                    return;
                int orderCount = orders.Length;
                if (geoData == null || geoData.GetLength(0) != orderCount + 1 || geoData.GetLength(1) != orderCount + 1)
                    return;
                if (startIndex < 0 || startIndex >= deliveries.Length)
                    return;
                if (step <= 0)
                    return;

                //#if debug
                //            Logger.WriteToLog(704, $"DilateRoutesThread enter. fromLevel = {fromLevel}, toLevel = {toLevel}, orders = {orders.Length}, startIndex = {startIndex}, step = {step}", 0);
                //#endif

                // 3. Цикл расширения маршрутов
                rc = 3;
                int shopIndex = orderCount;
                bool[] flags = new bool[orderCount];
                int[] orderId = new int[orderCount];
                Courier courier = deliveries[0].DeliveryCourier;
                double handInTime = courier.HandInTime;
                double maxOrderWeight = courier.MaxOrderWeight;
                double maxWeight = courier.MaxWeight;
                int maxDistance = (int)(1000.0 * courier.MaxDistance + 0.5);
                CourierDeliveryInfo[] iterDelivery = new CourierDeliveryInfo[(deliveries.Length - startIndex - 1) / step + 1];
                int count = 0;

                for (int i = 0; i < orderCount; i++)
                    orderId[i] = orders[i].Id;

                for (int i = startIndex; i < deliveries.Length; i += step)
                {
                    iterDelivery[count++] = deliveries[i];
                }

                //if (count < iterDelivery.Length)
                //{
                //    Array.Resize(ref iterDelivery, count);
                //}

                // 3.1 Цикл по длине маршрута
                rc = 31;
                CourierDeliveryInfo[] extendedDeliveries = new CourierDeliveryInfo[(toLevel - fromLevel) * iterDelivery.Length];
                int extendedCount = 0;
                Order[] extendedOrders = new Order[toLevel];
                int[] orderGeoIndex = new int[toLevel + 1];
                int n = 0;

                for (int i = fromLevel; i < toLevel; i++)
                {
                    n = count;
                    count = 0;
                    int[] orderIndex = new int[i];
                    double[] nodeReserve = new double[i];

                    // 3.2 Цикл по расширяемой отгрузке
                    rc = 32;

                    for (int j = 0; j < n; j++)
                    {
                        // 3.3 Извлекаем отгрузку
                        rc = 33;
                        CourierDeliveryInfo delivery = iterDelivery[j];
                        double[] nodeDeliveryTime = delivery.NodeDeliveryTime;
                        Point[] nodeInfo = delivery.NodeInfo;
                        Order[] deliveryOrders = delivery.Orders;
                        int deliveryOrderCount = delivery.Orders.Length;
                        DateTime startDeliveryInterval = delivery.StartDeliveryInterval;
                        DateTime endDeliveryInterval = delivery.EndDeliveryInterval;
                        DateTime calcTime = delivery.CalculationTime;
                        Shop fromShop = delivery.FromShop;
                        bool isLoop = delivery.IsLoop;
                        int toShopDistance = nodeInfo[deliveryOrderCount + 1].X;
                        double lastDeliveryTime = nodeDeliveryTime[deliveryOrderCount];

                        // 3.4 Находим индексы заказов в отгрузке, устанавливаем флаги заказов из отгрузки
                        //     подсчитываем длину маршрута
                        //     и вчисляем резервы веремени от каждой отгрузки до последней
                        rc = 34;
                        Array.Clear(flags, 0, flags.Length);
                        double prevMinReserve = double.MaxValue;
                        int deliveryDistance = 0;

                        for (int k = 0; k < deliveryOrderCount; k++)
                        {
                            // 3.4.1 Находим индекс и метим заказы из отгрузки
                            rc = 341;
                            int index = Array.BinarySearch(orderId, deliveryOrders[k].Id);
                            orderIndex[k] = index;
                            flags[index] = true;
                            deliveryDistance += nodeInfo[k + 1].X;

                            // 3.4.2 Подсчитываем резерв времени для сегментов пути
                            rc = 342;
                            Order order = deliveryOrders[deliveryOrderCount - k - 1];
                            double ndt = nodeDeliveryTime[deliveryOrderCount];
                            DateTime nodeMinTime = startDeliveryInterval.AddMinutes(ndt);
                            DateTime nodeMaxTime = endDeliveryInterval.AddMinutes(ndt);
                            if (order.DeliveryTimeFrom > nodeMinTime)
                                nodeMinTime = order.DeliveryTimeFrom;
                            if (order.DeliveryTimeTo < nodeMaxTime)
                                nodeMaxTime = order.DeliveryTimeTo;
                            if (nodeMaxTime < nodeMinTime)
                            {
                                nodeReserve[deliveryOrderCount - k - 1] = 0;
                            }
                            else
                            {
                                double reserve = (nodeMaxTime - nodeMinTime).TotalMinutes;
                                if (reserve < prevMinReserve)
                                    prevMinReserve = reserve;
                                nodeReserve[deliveryOrderCount - k - 1] = prevMinReserve;
                            }
                        }

                        if (isLoop)
                            deliveryDistance += toShopDistance;

                        CourierDeliveryInfo minCostDelivery = null;

                        // 3.5 Цикл по добавляемому к отгрузке заказу
                        rc = 35;
                        for (int k = 0; k < orderCount; k++)
                        {
                            // 3.5.1 Заказ входит в отгрузку ?
                            rc = 351;
                            if (flags[k])
                                continue;

                            // 3.5.2 Проверяем ограничения на вес
                            rc = 352;
                            Order order = orders[k];
                            if (order.Weight > maxOrderWeight)
                                continue;
                            if (order.Weight + delivery.Weight > maxWeight)
                                continue;

                            // 3.5.3 Добавляемая позиция - последний
                            rc = 353;
                            Point geoData1 = geoData[orderIndex[deliveryOrderCount - 1], k];
                            Point geoData2 = geoData[k, shopIndex];

                            // 3.5.3.1 Проверяем возможность доставки вовремя
                            rc = 3531;
                            double dt = lastDeliveryTime + geoData1.Y / 60 + handInTime;
                            DateTime t1 = order.DeliveryTimeFrom.AddMinutes(-dt);
                            DateTime t2 = order.DeliveryTimeTo.AddMinutes(-dt);
                            if (startDeliveryInterval > t1)
                                t1 = startDeliveryInterval;
                            if (endDeliveryInterval < t2)
                                t2 = endDeliveryInterval;
                            if (t1 <= t2)
                            {
                                // 3.5.3.2 Проверяем длину всего маршрута
                                rc = 3532;
                                double routeDistance = (isLoop ? deliveryDistance - toShopDistance + geoData1.X + geoData2.X : deliveryDistance + geoData1.X);
                                if (routeDistance <= maxDistance)
                                {
                                    // 3.5.3.3 Подсчитываем стоимость маршрута
                                    rc = 3533;
                                    CourierDeliveryInfo di;
                                    rc1 = courier.DeliveryCheckEx(delivery, order, geoData1, geoData2, t1, t2, out di);
                                    if (rc1 == 0 && di != null)
                                    {
                                        if (minCostDelivery == null)
                                        {
                                            minCostDelivery = di;
                                        }
                                        else if (di.Cost < minCostDelivery.Cost)
                                        {
                                            minCostDelivery = di;
                                        }
                                    }
                                }
                            }

                            // 3.5.4 Проверка добавления перед первым заказом, перед вторым, ... , перед последним
                            rc = 354;
                            int beforeOrderIndex = shopIndex;

                            for (int m = 0; m < deliveryOrderCount; m++)
                            {
                                // 3.5.4.1 Извлекаем гео-данные
                                rc = 3541;
                                int afterOrderIndex = orderIndex[m];
                                geoData1 = geoData[beforeOrderIndex, k];
                                geoData1 = geoData[k, afterOrderIndex];

                                // 3.5.4.2 Проверяем доставку в срок
                                rc = 3542;
                                dt = (geoData1.Y + geoData2.Y) / 60.0 + handInTime;
                                if (nodeReserve[m] < dt)
                                    continue;

                                // 3.5.4.3 Проверяем ограничение на длину маршрута
                                rc = 3543;
                                double routeDistance = deliveryDistance + 0.001 * (geoData1.X + geoData2.X - nodeInfo[m].X);
                                if (routeDistance > maxDistance)
                                    continue;

                                // 3.5.4.4 Подсчитываем стоимость отгрузки
                                rc = 3544;
                                CourierDeliveryInfo di;
                                if (m == 0)
                                {
                                    extendedOrders[0] = order;
                                    deliveryOrders.CopyTo(extendedOrders, 1);

                                    orderGeoIndex[0] = k;
                                    orderIndex.CopyTo(orderGeoIndex, 1);
                                }
                                else
                                {
                                    Array.Copy(deliveryOrders, extendedOrders, m);
                                    extendedOrders[m] = order;
                                    Array.Copy(deliveryOrders, m, extendedOrders, m + 1, deliveryOrderCount - m);

                                    Array.Copy(orderIndex, orderGeoIndex, m);
                                    orderGeoIndex[m] = k;
                                    Array.Copy(orderIndex, m, orderGeoIndex, m + 1, deliveryOrderCount - m);
                                }

                                orderGeoIndex[deliveryOrderCount + 1] = shopIndex;

                                rc1 = courier.DeliveryCheck(calcTime, fromShop, extendedOrders, orderGeoIndex, deliveryOrderCount + 1, isLoop, geoData, out di);
                                if (rc1 == 0 && di != null)
                                {
                                    if (minCostDelivery == null)
                                    {
                                        minCostDelivery = di;
                                    }
                                    else if (di.Cost < minCostDelivery.Cost)
                                    {
                                        minCostDelivery = di;
                                    }
                                }

                                // 3.5.4.5 Продвигаем before-индекс
                                beforeOrderIndex = afterOrderIndex;
                            }
                        }

                        if (minCostDelivery != null)
                            iterDelivery[count++] = minCostDelivery;
                    }

                    // 3.6 Если расширять больше нечего
                    rc = 36;
                    if (count <= 0)
                        break;

                    // 3.7 Пополняем исходное множество отгрузок
                    rc = 37;
                    Array.Copy(iterDelivery, 0, extendedDeliveries, extendedCount, count);
                    extendedCount += count;
                }

                if (extendedCount <= 0)
                {
                    context.ExtendedDeliveries = new CourierDeliveryInfo[0];
                }
                else
                {
                    if (extendedCount < extendedDeliveries.Length)
                    {
                        Array.Resize(ref extendedDeliveries, extendedCount);
                    }
                    context.ExtendedDeliveries = extendedDeliveries;
                }

                iterDelivery = null;
                extendedDeliveries = null;

                // 4. Выход - Ok
                rc = 0;
                //#if debug
                //            Logger.WriteToLog(705, $"DilateRoutesThread exit. = {rc}. fromLevel = {fromLevel}, toLevel = {toLevel}, orders = {orders.Length}, startIndex = {startIndex}, step = {step}", 0);
                //#endif

                //count = 0;
                //for (int k = 0; k <  context.ExtendedCount; k++)
                //{
                //    if (context.ExtendedDeliveries[k] == null)
                //        count++;
                //}

                //if (count > 0)
                //{
                //    #if debug
                //        Logger.WriteToLog(706, $"DilateRoutesThread exit. rc = {rc}. null_count = {count}, fromLevel = {fromLevel}, toLevel = {toLevel}, orders = {orders.Length}, startIndex = {startIndex}, step = {step}", 0);
                //    #endif

                //}

            }
            catch
            {

            }
            finally
            {
                if (context != null)
                {
                    context.ExitCode = rc;

                    if (context.SyncEvent != null)
                    {
                        context.SyncEvent.Set();
                    }
                }
            }
        }


    }
}
