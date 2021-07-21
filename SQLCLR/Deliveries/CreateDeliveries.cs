using Microsoft.SqlServer.Server;
using SQLCLR.AverageDeliveryCost;
using SQLCLR.Couriers;
using SQLCLR.Deliveries;
using SQLCLR.Log;
using SQLCLR.MaxOrdersOfRoute;
using SQLCLR.Orders;
using SQLCLR.Shops;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Xml;
using System.Xml.Serialization;

public partial class StoredProcedures
{
    #region SQL instructions

    /// <summary>
    /// Выбор данных магазина
    /// </summary>
    private const string SELECT_SHOPS = "SELECT * FROM lsvShops WHERE shpUpdated = 1";

    /// <summary>
    /// Выбор заказов магазинов
    /// </summary>
    private const string SELECT_ORDERS =
        "SELECT lsvOrders.* " +
        "FROM   lsvOrders INNER JOIN " +
        "          lsvShops ON lsvOrders.ordShopID = lsvShops.shpShopID " +
        " WHERE  (lsvShops.shpUpdated = 1) AND " +
        "        (lsvOrders.ordCompleted = 0) AND " +
        "        (lsvOrders.ordStatusID = 1 OR lsvOrders.ordStatusID = 2);";

    /// <summary>
    /// Выбор способов доставки заказов
    /// </summary>
    private const string SELECT_ORDER_VEHICLE_TYPES =
        "SELECT lsvOrders.ordOrderID, lsvOrderVehicleTypes.ovtVehicleID " +
        "FROM   lsvOrders INNER JOIN " +
        "          lsvOrderVehicleTypes ON lsvOrders.ordOrderID = lsvOrderVehicleTypes.ovtOrderID INNER JOIN " +
        "             lsvShops ON lsvOrders.ordShopID = lsvShops.shpShopID " +
        " WHERE  (lsvShops.shpUpdated = 1) AND " +
        "        (lsvOrders.ordCompleted = 0) AND " +
        "        (lsvOrders.ordStatusID = 1 OR lsvOrders.ordStatusID = 2) " +
        "ORDER BY lsvOrders.ordOrderID;";

    /// <summary>
    /// Выбор параметров способов доставки
    /// </summary>
    private const string SELECT_COURIER_TYPES = "SELECT * FROM lsvCourierTypes WHERE crtVehicleID IN ({0});";

    /// <summary>
    /// Выбор курьеров, доступных для доставки заказов
    /// </summary>
    private const string SELECT_COURIERS =
        "SELECT lsvCouriers.* " +
        "FROM lsvCouriers LEFT JOIN " +
        "        lsvShops ON lsvCouriers.crsShopID = lsvShops.shpShopID " +
        " WHERE  (lsvCouriers.crsStatusID = 1) AND " +
        "        (lsvCouriers.crsShopID = 0 OR lsvShops.shpUpdated = 1);";

    /// <summary>
    /// Выбор порогов для среднего времени доставки одного заказа
    /// </summary>
    private const string SELECT_AVERAGE_COST_THRESHOLDS = "SELECT * FROM lsvAverageDeliveryCost";

    /// <summary>
    /// Выбор максимального числа заказов
    /// для разных длин маршрутов
    /// </summary>
    private const string SELECT_MAX_ORDERS_OF_ROUTE = 
        "SELECT lsvSalesmanProblemLevels.splLevel AS Length, lsvSalesmanProblemLevels.splMaxOrders AS MaxOrders " +
        "FROM   lsvFunctionalParameters INNER JOIN " +
        "          lsvSalesmanProblemLevels ON lsvFunctionalParameters.fnpID = lsvSalesmanProblemLevels.splFnpID " +
        "WHERE  lsvFunctionalParameters.fnpCfgServiceID = {0};";

    /// <summary>
    /// Очистка таблиц с отгрузками
    /// </summary>
    private const string CLEAR_DELIVERIES = "DELETE dbo.lsvDeliveryOrders; DELETE dbo.lsvDeliveryNodeInfo; DELETE dbo.lsvNodeDeliveryTime; DELETE dbo.lsvDeliveries";

    /// <summary>
    /// Имя сервера
    /// </summary>
    private const string SELECT_SERVERNAME = "SELECT @@SERVERNAME";

    #endregion SQL instructions

    /// <summary>
    /// Число возможных отгрузок на поток
    /// </summary>
    private const int DELIVERIES_PER_THREAD = 1000;

    /// <summary>
    /// Максимальное число потоков для построителя отгрузок из ThreadContext
    /// </summary>
    private const int MAX_DELIVERY_THREADS = 8;

    /// <summary>
    /// Построение всех возможных отгрузок для всех
    /// отмеченных магазинов
    /// </summary>
    /// <param name="service_id">ID LogisticsService</param>
    /// <param name="calc_time">Момент времени, на который проводятся расчеты</param>
    /// <returns>0 - отгрузки построены; иначе отгрузки не построены</returns>
    [SqlProcedure]
    public static SqlInt32 CreateDeliveries(SqlInt32 service_id, SqlDateTime calc_time)
    {
        // 1. Инициализация
        int rc = 1;
        int rc1 = 1;

        try
        {
            #if debug
                Logger.WriteToLog(1, $"---> CreateDeliveries. service_id = {service_id.Value}, calc_time = {calc_time.Value: yyyy-MM-dd HH:mm:ss.fff}", 0);
            #endif

            // 2. Проверяем исходные данные
            rc = 2;
            if (!SqlContext.IsAvailable)
            {
                #if debug
                    Logger.WriteToLog(4, $"CreateDeliveries. service_id = {service_id.Value}. rc = {rc}. SQLContext is not available", 1);
                #endif
                return rc;
            }

            // 3. Открываем соединение в контексте текущей сессии
            //    и загружаем все необходимые данные
            rc = 3;
            Shop[] shops = null;
            Order[] orders = null;
            int[] requiredVehicleTypes = null;
            CourierTypeRecord[] courierTypeRecords = null;
            CourierRecord[] courierRecords = null;
            AverageDeliveryCostRecord[] thresholdRecords;
            MaxOrdersOfRouteRecord[] routeLimitationRecords;

            using (SqlConnection connection = new SqlConnection("context connection=true"))
            {
                // 3.1 Открываем соединение
                rc = 31;
                connection.Open();
                //#if debug
                //    Logger.WriteToLog(444, $"CreateDeliveries. service_id = {service_id.Value}. DataSource = {GetServerName(connection)}, Database = {connection.Database}" , 0);
                //#endif

                // 3.2 Выбираем магазины для пересчета отгрузок
                rc = 32;
                rc1 = SelectShops(connection, out shops);
                if (rc1 != 0)
                {
                    #if debug
                        Logger.WriteToLog(4, $"CreateDeliveries. service_id = {service_id.Value}. rc = {rc}. rc1 = {rc1}. Shops are not selected", 1);
                    #endif
                    return rc = 1000000 * rc + rc1;
                }
                if (shops == null || shops.Length <= 0)
                {
                    #if debug
                        Logger.WriteToLog(5, $"CreateDeliveries. service_id = {service_id.Value}. rc = {rc}. rc1 = {rc1}. Shops are not found", 0);
                    #endif
                    return rc = 0;
                }

                #if debug
                    Logger.WriteToLog(6, $"CreateDeliveries. service_id = {service_id.Value}. Selected shops: {shops.Length}", 0);
                #endif

                // 3.3 Выбираем заказы, для которых нужно построить отгрузки
                rc = 33;
                rc1 = SelectOrders(connection, out orders);
                if (rc1 != 0)
                {
                    #if debug
                        Logger.WriteToLog(7, $"CreateDeliveries. service_id = {service_id.Value}. rc = {rc}. rc1 = {rc1}. Orders are not selected", 1);
                    #endif
                    return rc = 1000000 * rc + rc1;
                }

                orders = FilterOrdersOnCalcTime(calc_time.Value, orders);

                if (orders == null || orders.Length <= 0)
                {
                    #if debug
                        Logger.WriteToLog(8, $"CreateDeliveries. service_id = {service_id.Value}. rc = {rc}. rc1 = {rc1}. Orders are not found", 0);
                    #endif
                    return rc = 0;
                }

                #if debug
                    Logger.WriteToLog(9, $"CreateDeliveries. service_id = {service_id.Value}. Selected orders: {orders.Length}", 0);
                #endif


                // 3.4 Выбираем способы доставки заказов,
                //     которые могут быть использованы
                rc = 34;
                requiredVehicleTypes = AllOrders.GetOrderVehicleTypes(orders);
                if (requiredVehicleTypes == null || requiredVehicleTypes.Length <= 0)
                {
                    #if debug
                        Logger.WriteToLog(10, $"CreateDeliveries. service_id = {service_id.Value}. rc = {rc}. Orders vehicle types are not found", 1);
                    #endif
                    return rc;
                }

                #if debug
                    Logger.WriteToLog(11, $"CreateDeliveries. service_id = {service_id.Value}. Order vehicle  types: {requiredVehicleTypes.Length}", 0);
                #endif

                // 3.5 Загружаем параметры способов отгрузки
                rc = 35;
                rc1 = SelectCourierTypes(requiredVehicleTypes, connection, out courierTypeRecords);
                if (rc1 != 0)
                {
                    #if debug
                        Logger.WriteToLog(12, $"CreateDeliveries. service_id = {service_id.Value}. rc = {rc}. rc1 = {rc1}. Courier types are not selected", 1);
                    #endif
                    return rc = 1000000 * rc + rc1;
                }
                if (courierTypeRecords == null || courierTypeRecords.Length <= 0)
                {
                    #if debug
                        Logger.WriteToLog(13, $"CreateDeliveries. service_id = {service_id.Value}. rc = {rc}. Courier types are not found", 1);
                    #endif
                    return rc;
                }

                // 3.6 Загружаем информацию о курьерах
                rc = 36;
                rc1 = SelectCouriers(connection, out courierRecords);
                if (rc1 != 0)
                {
                    #if debug
                        Logger.WriteToLog(14, $"CreateDeliveries. service_id = {service_id.Value}. rc = {rc}. rc1 = {rc1}. Couriers are not selected", 1);
                    #endif
                    return rc = 1000000 * rc + rc1;
                }
                if (courierRecords == null || courierRecords.Length <= 0)
                {
                    #if debug
                        Logger.WriteToLog(15, $"CreateDeliveries. service_id = {service_id.Value}. rc = {rc}. Courier are not found", 1);
                    #endif
                    return rc;
                }

                #if debug
                    Logger.WriteToLog(16, $"CreateDeliveries. service_id = {service_id.Value}. Courier records: {courierRecords.Length}", 0);

                    for (int i = 0; i < courierRecords.Length; i++)
                    {
                        Logger.WriteToLog(161, $"CreateDeliveries. service_id = {service_id.Value}. courierRecords[{i}].Id = {courierRecords[i].CourierId}, courierRecords[{i}].VehicleID = {courierRecords[i].VehicleId}", 0);

                    }
                #endif

                // 3.7 Загружаем пороги для среднего времени доставки заказов
                rc = 37;
                rc1 = SelectThresholds(connection, out thresholdRecords);
                if (rc1 != 0)
                {
                    #if debug
                        Logger.WriteToLog(17, $"CreateDeliveries. service_id = {service_id.Value}. rc = {rc}. rc1 = {rc1}. Thresholds are not selected", 1);
                    #endif
                    return rc = 1000000 * rc + rc1;
                }
                //if (thresholdRecords == null || thresholdRecords.Length <= 0)
                //    return rc;

                // 3.8 Загружаем ограничения на длину маршрутов от числа заказов
                rc = 38;
                rc1 = SelectMaxOrdersOfRoute(service_id.Value, connection, out routeLimitationRecords);
                if (rc1 != 0)
                {
                    #if debug
                        Logger.WriteToLog(18, $"CreateDeliveries. service_id = {service_id.Value}. rc = {rc}. rc1 = {rc1}. Max route length are not selected", 1);
                    #endif
                    return rc = 1000000 * rc + rc1;
                }
                if (routeLimitationRecords == null || routeLimitationRecords.Length <= 0)
                {
                    #if debug
                        Logger.WriteToLog(19, $"CreateDeliveries. service_id = {service_id.Value}. rc = {rc}. Max route length records are not found", 1);
                    #endif
                    return rc;
                }
                //}

                // 4. Создаём объект c курьерами
                rc = 4;
                AllCouriers allCouriers = new AllCouriers();
                rc1 = allCouriers.Create(courierTypeRecords, courierRecords);
                if (rc1 != 0)
                {
                #if debug
                    Logger.WriteToLog(20, $"CreateDeliveries. service_id = {service_id.Value}. rc = {rc}. rc1 = {rc1}. allCouriers is not created", 1);
                #endif
                    return rc = 1000000 * rc + rc1;
                }

                // 5. Создаём объект c заказами
                rc = 5;
                AllOrders allOrders = new AllOrders();
                rc1 = allOrders.Create(orders);
                if (rc1 != 0)
                {
                    #if debug
                        Logger.WriteToLog(21, $"CreateDeliveries. service_id = {service_id.Value}. rc = {rc}. rc1 = {rc1}. allOrders is not created", 1);
                    #endif
                    return rc = 1000000 * rc + rc1;
                }

                // 6. Создаём объект c порогами средней стоимости доставки
                rc = 6;
                AverageCostThresholds thresholds = new AverageCostThresholds();
                rc1 = thresholds.Create(thresholdRecords);
                if (rc1 != 0)
                {
                    #if debug
                        Logger.WriteToLog(22, $"CreateDeliveries. service_id = {service_id.Value}. rc = {rc}. rc1 = {rc1}. thresholds is not created", 1);
                    #endif
                    return rc = 1000000 * rc + rc1;
                }

                // 7. Создаём объект c ограничениями на длину маршрута по числу заказов
                rc = 7;
                RouteLimitations limitations = new RouteLimitations();
                rc1 = limitations.Create(routeLimitationRecords);
                if (rc1 != 0)
                {
                    #if debug
                        Logger.WriteToLog(23, $"CreateDeliveries. service_id = {service_id.Value}. rc = {rc}. rc1 = {rc1}. limitations is not created", 1);
                    #endif
                    return rc = 1000000 * rc + rc1;
                }

                // 8. Определяем число потоков для расчетов
                rc = 8;
                int threadCount = 2 * Environment.ProcessorCount;
                if (threadCount < 8)
                {
                    threadCount = 8;
                }
                else if (threadCount > 16)
                {
                    threadCount = 16;
                }

                // 9. Строим порции расчетов (ThreadContext), выполняемые в одном потоке
                //    (порция - это все заказы для одного способа доставки (курьера) в одном магазине)
                rc = 9;
                ThreadContext[] context = GetThreadContextEx(connection, service_id.Value, calc_time.Value, shops, allOrders, allCouriers, limitations);
                //ThreadContext[] context = GetThreadContext(service_id.Value, calc_time.Value, shops, allOrders, allCouriers, limitations);
                if (context == null || context.Length <= 0)
                {
                    #if debug
                        Logger.WriteToLog(24, $"CreateDeliveries. service_id = {service_id.Value}. rc = {rc}. Thread context is not created", 1);
                    #endif
                }

              //CalcThread(context[0]);
                if (context.Length < threadCount)
                    threadCount = context.Length;

                #if debug
                    Logger.WriteToLog(25, $"CreateDeliveries. service_id = {service_id.Value}. Thread context count: {context.Length}. Thread count: {threadCount}", 0);
                #endif

                // 10. Сортируем контексты по убыванию числа заказов
                rc = 10;
                Array.Sort(context, CompareContextByOrderCount);

                // 11. Создаём объекты синхронизации
                rc = 11;
                ManualResetEvent[] syncEvents = new ManualResetEvent[threadCount];
                int[] contextIndex = new int[threadCount];

                // 12. Запускаем первые threadCount- потоков
                rc = 12;
                int errorCount = 0;
                CourierDeliveryInfo[] allDeliveries = new CourierDeliveryInfo[100000];
                int deliveryCount = 0;

                //CalcThread(context[0]);
                //allDeliveries = context[0].Deliveries;
                //deliveryCount = context[0].DeliveryCount;
                //goto ff;

                #if debug
                    Logger.WriteToLog(26, $"CreateDeliveries. service_id = {service_id.Value}. Thread context count: {context.Length}", 0);
                #endif

                //Thread.BeginThreadAffinity();
                for (int i = 0; i < threadCount; i++)
                {
                    int m = i;
                    contextIndex[m] = i;
                    ThreadContext threadContext = context[m];
                    syncEvents[m] = new ManualResetEvent(false);
                    threadContext.SyncEvent = syncEvents[m];
                    //ThreadPool.QueueUserWorkItem(CalcThread, threadContext);
                    ThreadPool.QueueUserWorkItem(CalcThreadEx, threadContext);
                }

                // 13. Запускаем последующие потоки
                //     после завершения очередного
                rc = 13;

                for (int i = threadCount; i < context.Length; i++)
                {
                    int threadIndex = WaitHandle.WaitAny(syncEvents);

                    ThreadContext executedThreadContext = context[contextIndex[threadIndex]];

                    contextIndex[threadIndex] = i;
                    int m = i;
                    ThreadContext threadContext = context[m];
                    threadContext.SyncEvent = syncEvents[threadIndex];
                    threadContext.SyncEvent.Reset();
                    //ThreadPool.QueueUserWorkItem(CalcThread, threadContext);
                    ThreadPool.QueueUserWorkItem(CalcThreadEx, threadContext);

                    // Обработка завершившегося потока
                    if (executedThreadContext.ExitCode != 0)
                    {
                        errorCount++;
                    }
                    else
                    {
                        int contextDeliveryCount = executedThreadContext.DeliveryCount;
                        if (contextDeliveryCount > 0)
                        {
                            if (deliveryCount + contextDeliveryCount > allDeliveries.Length)
                            {
                                int size = allDeliveries.Length / 2;
                                if (size < contextDeliveryCount)
                                    size = contextDeliveryCount;
                                Array.Resize(ref allDeliveries, allDeliveries.Length + size);
                            }

                            executedThreadContext.Deliveries.CopyTo(allDeliveries, deliveryCount);
                            deliveryCount += contextDeliveryCount;
                        }
                    }
                }

                //WaitHandle.WaitAll(syncEvents);

                for (int i = 0; i < syncEvents.Length; i++)
                {
                    syncEvents[i].WaitOne();
                }

                //Thread.EndThreadAffinity();
                for (int i = 0; i < threadCount; i++)
                {
                    syncEvents[i].Dispose();

                    // Обработка последних завершившихся потоков
                    ThreadContext executedThreadContext = context[contextIndex[i]];
                    int contextDeliveryCount = executedThreadContext.DeliveryCount;
                    if (contextDeliveryCount > 0)
                    {
                        if (deliveryCount + contextDeliveryCount > allDeliveries.Length)
                        {
                            int size = allDeliveries.Length / 2;
                            if (size < contextDeliveryCount)
                                size = contextDeliveryCount;
                            Array.Resize(ref allDeliveries, allDeliveries.Length + size);
                        }

                        executedThreadContext.Deliveries.CopyTo(allDeliveries, deliveryCount);
                        deliveryCount += contextDeliveryCount;
                    }
                }
//ff:
                if (deliveryCount <= 0)
                {
                    #if debug
                        Logger.WriteToLog(261, $"CreateDeliveries. service_id = {service_id.Value}. Exit rc = {rc}", 0);
                    #endif

                    return rc = 0;
                }

                if (deliveryCount < allDeliveries.Length)
                {
                    Array.Resize(ref allDeliveries, deliveryCount);
                }

                // 14. Сохраняем построенные отгрузки
                rc = 14;

                //#if debug
                //    Logger.WriteToLog(27, $"CreateDeliveries. service_id = {service_id.Value}. Saving deliveries...", 0);
                //#endif

                //using (SqlConnection connection = new SqlConnection("context connection=true"))
                //{
                // 14.1 Открываем соединение
                //rc = 141;
                //connection.Open();

                // 14.2 Очищаем таблицу lsvDeliveries и все связааные с ней таблицы
                rc = 142;
                ClearDeliveries(connection);

                // 14.3 Сохраняем построенные отгрузки
                rc = 143;
                //rc1 = SaveDeliveries(allDeliveries, connection);
                #if debug
                    Logger.WriteToLog(290, $"CreateDeliveries. service_id = {service_id.Value}. Deliveries saving...", 0);
                #endif

                rc1 = SaveDeliveriesEx(allDeliveries, GetServerName(connection), connection.Database);
                if (rc1 != 0)
                {
                    #if debug
                        Logger.WriteToLog(28, $"CreateDeliveries. service_id = {service_id.Value}. rc = {rc}. rc1 = {rc1}. deliveries is not saved", 1);
                    #endif
                    return rc = 1000000 * rc + rc1;
                }

                connection.Close();
                //connection.Close();
                //return rc = 777;
            }

            #if debug
                Logger.WriteToLog(291, $"CreateDeliveries. service_id = {service_id.Value}. Deliveries saved", 0);
            #endif

            // 15. Выход - Ok
            rc = 0;
            return rc;
        }
        catch (Exception ex)
        {
            #if debug
                Logger.WriteToLog(3, $"CreateDeliveries. service_id = {service_id.Value}. rc = {rc}. Exception {ex.Message}", 2);
            #endif
            return rc;
        }
        finally
        {
            #if debug
                Logger.WriteToLog(2, $"<--- CreateDeliveries. service_id = {service_id.Value}. calc_time = {calc_time.Value: yyyy-MM-dd HH:mm:ss.fff}. rc = {rc}", 0);
            #endif
        }
    }

    /// <summary>
    /// Фильтрация заказов по времени расчета
    /// </summary>
    /// <param name="calc_time">Время расчета</param>
    /// <param name="orders">Фильтруемые заказы</param>
    /// <returns>Отфильтрованные заказы или null</returns>
    private static Order[] FilterOrdersOnCalcTime(DateTime calc_time, Order[] orders)
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
                if (order.DeliveryTimeTo > calc_time)
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
    /// Фильтрация заказов по максимально возможному весу
    /// </summary>
    /// <param name="maxOrdeeWeight">Максимальный допустимый вес заказа, кг</param>
    /// <param name="orders">Фильтруемые заказы</param>
    /// <returns>Отфильтрованные заказы или null</returns>
    private static Order[] FilterOrdersOnMaxWeight(double maxOrdeeWeight, Order[] orders)
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
                if (order.Weight <= maxOrdeeWeight)
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
    /// Контекст-построитель отгрузок
    /// в отдельном потоке
    /// </summary>
    /// <param name="status">Контекст потока</param>
    private static void CalcThread(object status)
    {
        // 1. Инициализация
        int rc = 1;
        ThreadContext context = status as ThreadContext;

        try
        {
            // 2. Проверяем исходные данные
            rc = 2;
            //Thread.BeginThreadAffinity();
            if (context == null ||
                context.OrderCount <= 0 ||
                context.ShopCourier == null ||
                context.ShopFrom == null ||
                context.Orders == null || context.Orders.Length <= 0 ||
                context.MaxRouteLength <= 0 || context.MaxRouteLength > 8)
                return;

            #if debug
                Logger.WriteToLog(301, $"CalcThread enter. order_count = {context.OrderCount}, shop_id = {context.ShopFrom.Id}, courier_id = {context.ShopCourier.Id}, level = {context.MaxRouteLength}", 0);
            #endif


            //// 3. Выбираем гео-данные для всех точек 
            //rc = 3;
            //Point[,] geoData;
            //int rc1 = GeoData.Select(context.ServiceId, context.ShopCourier.YandexType, context.ShopFrom, context.Orders, out geoData);
            //if (rc1 != 0)
            //{
            //    rc = 10000 * rc + rc1;
            //    return;
            //}

            // 4. Строим отгрузки
            rc = 4;
            //Thread.BeginThreadAffinity();
            int rc1 = RouteBuilder.Build(context);
            //Thread.EndThreadAffinity();
            if (rc1 != 0)
            {
                rc = 100000 * rc + rc1;
                return;
            }

            // 5. Выход - Ok
            rc = 0;
        }
        catch
        {   }
        finally
        {
            if (context != null)
            {
                #if debug
                    Logger.WriteToLog(302, $"CalcThread exit rc = {rc}. order_count = {context.OrderCount}, shop_id = {context.ShopFrom.Id}, courier_id = {context.ShopCourier.Id}, level = {context.MaxRouteLength}", 0);
                #endif
                context.ExitCode = rc;
                if (context.SyncEvent != null)
                    context.SyncEvent.Set();


            }
        }
    }

    /// <summary>
    /// Контекст-построитель отгрузок
    /// в отдельном потоке
    /// </summary>
    /// <param name="status">Контекст потока</param>
    private static void CalcThreadEx(object status)
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
                context.MaxRouteLength <= 0 || context.MaxRouteLength > 8)
                return;

            #if debug
                Logger.WriteToLog(301, $"CalcThreadEx enter. order_count = {context.OrderCount}, shop_id = {context.ShopFrom.Id}, courier_id = {context.ShopCourier.Id}, level = {context.MaxRouteLength}", 0);
            #endif

            // 3. Создаём ключи всех отгрузок
            rc = 3;
            long[] deliveryKeys = CreateDeliverySortedKeys(context.OrderCount, context.MaxRouteLength);
            if (deliveryKeys == null || deliveryKeys.Length <= 0)
                return;

            // 4. Вычисляем число потоков для построения отгрузок из ThreadContext
            rc = 4;
            int threadCount = (deliveryKeys.Length + DELIVERIES_PER_THREAD - 1) / DELIVERIES_PER_THREAD;
            if (threadCount > MAX_DELIVERY_THREADS)
                threadCount = MAX_DELIVERY_THREADS;

            #if debug
                Logger.WriteToLog(304, $"CalcThreadEx enter. order_count = {context.OrderCount}, shop_id = {context.ShopFrom.Id}, courier_id = {context.ShopCourier.Id}, level = {context.MaxRouteLength}, thread_count = {threadCount}", 0);
            #endif

            // 5. Требуется всего один поток
            rc = 5;
            if (threadCount <= 1)
            {
                ThreadContextEx contextEx = new ThreadContextEx(context, null, deliveryKeys, 0, 1);
                allCountextEx = new ThreadContextEx[] { contextEx};
                RouteBuilder.BuildEx(contextEx);
            }
            else
            {
                // 6. Требуется несколько потоков
                rc = 6;
                allCountextEx = new ThreadContextEx[threadCount];
                for (int i = 0; i < threadCount; i++)
                {
                    ManualResetEvent syncEvent = new ManualResetEvent(false);
                    int k = i;
                    ThreadContextEx contextEx = new ThreadContextEx(context, syncEvent, deliveryKeys, k, threadCount);
                    allCountextEx[k] = contextEx;
                    ThreadPool.QueueUserWorkItem(RouteBuilder.BuildEx, contextEx);
                }

                for (int i = 0; i < threadCount; i++)
                {
                    allCountextEx[i].SyncEvent.WaitOne();
                    allCountextEx[i].SyncEvent.Dispose();
                    allCountextEx[i].SyncEvent = null;
                }
            }

            // 7. Строим общий результат
            rc = 7;
            CourierDeliveryInfo[] deliveries = null;
            int[] id = new int[5];
            int rc1 = 0;
            if (threadCount <= 1)
            {
                deliveries = allCountextEx[0].Deliveries;
                rc1 = allCountextEx[0].ExitCode;
            }
            else
            {
                deliveries = new CourierDeliveryInfo[deliveryKeys.Length];
                for (int i = 0; i < threadCount; i++)
                {
                    if (allCountextEx[i].ExitCode != 0)
                    {
                        rc1 = allCountextEx[i].ExitCode;
                    }
                    else if (allCountextEx[i].Deliveries != null)
                    {
                        CourierDeliveryInfo[] threadDeliveries = allCountextEx[i].Deliveries;
                        for (int j = 0; j < threadDeliveries.Length; j++)
                        {
                            CourierDeliveryInfo threadDelivery = threadDeliveries[j];
#if debug
                            //6.31238809.31239251.31239320.31241604.31260002
                            //6.31238809.31239251.31239320.31241604.31260002
                            //6.31238809.31239251.31239320.31241604.31260002
                            if (threadDelivery != null && threadDelivery.DeliveryCourier.VehicleID == 6 && threadDelivery.OrderCount == 5)
                            {
                                for (int mm = 0; mm < threadDelivery.OrderCount; mm++)
                                {
                                    id[mm] = threadDelivery.Orders[mm].Id;
                                }

                                Array.Sort(id);
                                if (id[0] == 31238809 &&
                                    id[1] == 31239251 &&
                                    id[2] == 31239320 &&
                                    id[3] == 31241604 &&
                                    id[4] == 31260002)
                                {

                                    Logger.WriteToLog(308, $"CalcThreadEx. thread = {i}. L = {threadDeliveries.Length}. pkey = 6.31238809.31239251.31239320.31241604.31260002. index = {j}. key = {deliveryKeys[j].ToString("X")}", 2);
                                }
                            }
                        #endif



                            if (threadDelivery != null)
                            {
                                CourierDeliveryInfo delivery = deliveries[j];
                                if (delivery == null)
                                {
                                    deliveries[j] = threadDelivery;
                                }
                                else if (threadDelivery.Cost < delivery.Cost)
                                {
                                    deliveries[j] = threadDelivery;
                                }
                            }
                        }
                    }
                }
            }

            if (deliveries != null && deliveries.Length > 0)
            {
                int count = 0;
                for (int i = 0; i < deliveries.Length; i++)
                {
                    if (deliveries[i] != null)
                    {
//#if debug
//                        if (count == 122644 || count == 123323)
//                        {
//                            StringBuilder sb = new StringBuilder(300);
//                            sb.Append(deliveries[i].Orders[0].Id);

//                                for (int mm = 0; mm < deliveries[i].OrderCount; mm++)
//                                {
//                                    sb.Append('.');
//                                    sb.Append(deliveries[i].Orders[mm].Id);
//                                }



//                            Logger.WriteToLog(388, $"CalcThreadEx. count = {count}. i = {i}. key = 6.31238809.31239251.31239320.31241604.31260002. index = {j}", 2);
//                        }

//#endif 
                        deliveries[count++] = deliveries[i];
                    }
                }

                if (count < deliveries.Length)
                {
                    Array.Resize(ref deliveries, count);
                }
            }

            context.Deliveries = deliveries;
            allCountextEx = null;

            if (rc1 != 0)
            {
                rc = 100000 * rc + rc1;
                return;
            }

            // 8. Выход - Ok
            rc = 0;
        }
        catch (Exception ex)
        {
            #if debug
                Logger.WriteToLog(303, $"CalcThreadEx. service_id = {context.ServiceId}. rc = {rc}. order_count = {context.OrderCount}, shop_id = {context.ShopFrom.Id}, courier_id = {context.ShopCourier.Id}, level = {context.MaxRouteLength} Exception = {ex.Message}", 2);
            #endif
        }
        finally
        {
            if (context != null)
            {
                #if debug
                    Logger.WriteToLog(302, $"CalcThreadEx exit rc = {rc}. order_count = {context.OrderCount}, shop_id = {context.ShopFrom.Id}, courier_id = {context.ShopCourier.Id}, level = {context.MaxRouteLength}", 0);
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
    /// Выборка магазинов, для которых нужно
    /// построить все отгрузки
    /// </summary>
    /// <param name="connection">Открытое соединение</param>
    /// <param name="shops">Выбранные магазины или null</param>
    /// <returns>0 - магазины выбраны; иначе - магазины не выбраны</returns>
    private static int SelectShops(SqlConnection connection, out Shop[] shops)
    {
        // 1. Инициализация
        int rc = 1;
        shops = null;

        try
        {
            // 2. Проверяем исходные данные
            rc = 2;
            if (connection == null || connection.State != ConnectionState.Open)
                return rc;

            // 3. Выбираем магазины
            rc = 3;
            Shop[] allShops = new Shop[1000];
            int shopCount = 0;

            using (SqlCommand cmd = new SqlCommand(SELECT_SHOPS, connection))
            {

                // 3.2 Читаем записи и строим результат
                rc = 32;
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    int iShopId = reader.GetOrdinal("shpShopID");
                    int iLatitude = reader.GetOrdinal("shpLatitude");
                    int iLongitude = reader.GetOrdinal("shpLongitude");
                    int iWorkStart = reader.GetOrdinal("shpWorkStart");
                    int iWorkEnd = reader.GetOrdinal("shpWorkEnd");

                    while (reader.Read())
                    {
                        Shop shop = new Shop(reader.GetInt32(iShopId));
                        shop.Latitude = reader.GetDouble(iLatitude);
                        shop.Longitude = reader.GetDouble(iLongitude);
                        shop.WorkStart = reader.GetTimeSpan(iWorkStart);
                        shop.WorkEnd = reader.GetTimeSpan(iWorkEnd);

                        if (shopCount >= allShops.Length)
                        {
                            Array.Resize(ref allShops, (int)(1.25 * allShops.Length));
                        }

                        allShops[shopCount++] = shop;
                    }
                }
            }

            if (shopCount < allShops.Length)
            {
                Array.Resize(ref allShops, shopCount);
            }

            shops = allShops;

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
    /// Выбор заказов, для которых нужно построить отгрузки
    /// </summary>
    /// <param name="connection">Открытое соединение</param>
    /// <param name="orders">Выбранные заказы или null</param>
    /// <returns>0 - заказы выбраны; иначе - заказы не выбраны</returns>
    private static int SelectOrders(SqlConnection connection, out Order[] orders)
    {
        // 1. Инициализация
        int rc = 1;
        orders = null;

        try
        {
            // 2. Проверяем исходные данные
            rc = 2;
            if (connection == null || connection.State != ConnectionState.Open)
                return rc;

            // 3. Выбираем заказы
            rc = 3;
            Order[] allOrders = new Order[1000];
            int count = 0;
            //int currentOrderId = int.MinValue;
            //int[] vehicleTypes = new int[1024];
            //int vehicleTypeCount = 0;
            //Order order;

            using (SqlCommand cmd = new SqlCommand(SELECT_ORDERS, connection))
            {
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    int iOrderID = reader.GetOrdinal("ordOrderID");
                    int iStatusID = reader.GetOrdinal("ordStatusID");
                    int iShopID = reader.GetOrdinal("ordShopID");
                    int iPriority = reader.GetOrdinal("ordPriority");
                    int iWeight = reader.GetOrdinal("ordWeight");
                    int iLatitude = reader.GetOrdinal("ordLatitude");
                    int iLongitude = reader.GetOrdinal("ordLongitude");
                    int iAssembledDate = reader.GetOrdinal("ordAssembledDate");
                    int iReceiptedDate = reader.GetOrdinal("ordReceiptedDate");
                    int iDeliveryTimeFrom = reader.GetOrdinal("ordDeliveryTimeFrom");
                    int iDeliveryTimeTo = reader.GetOrdinal("ordDeliveryTimeTo");
                    //int iCompleted = reader.GetOrdinal("ordCompleted");
                    int iTimeCheckDisabled = reader.GetOrdinal("ordTimeCheckDisabled");

                    while (reader.Read())
                    {
                        Order order = new Order(reader.GetInt32(iOrderID));
                        order.Status = (OrderStatus)reader.GetInt32(iStatusID);
                        order.ShopId = reader.GetInt32(iShopID);
                        order.Priority = reader.GetInt32(iPriority);
                        order.Weight = reader.GetDouble(iWeight);
                        order.Latitude = reader.GetDouble(iLatitude);
                        order.Longitude = reader.GetDouble(iLongitude);
                        order.AssembledDate = reader.GetDateTime(iAssembledDate);
                        order.ReceiptedDate = reader.GetDateTime(iReceiptedDate);
                        order.DeliveryTimeFrom = reader.GetDateTime(iDeliveryTimeFrom);
                        order.DeliveryTimeTo = reader.GetDateTime(iDeliveryTimeTo);
                        order.TimeCheckDisabled = reader.GetBoolean(iTimeCheckDisabled);

                        if (count >= allOrders.Length)
                        {
                            Array.Resize(ref allOrders, (int)(1.25 * allOrders.Length));
                        }

                        allOrders[count++] = order;
                    }
                }
            }

            if (count < allOrders.Length)
            {
                Array.Resize(ref allOrders, count);
            }

            // 4. Строим индекс заказов
            rc = 4;
            int[] orderId = new int[count];

            for (int i = 0; i < count; i++)
                orderId[i] = allOrders[i].Id;

            Array.Sort(orderId, allOrders);


            // 5. Выбираем способы доставки заказов
            rc = 5;
            int currentOrderId = int.MinValue;
            int[] vehicleTypes = new int[1024];
            int vehicleTypeCount = 0;

            using (SqlCommand cmd = new SqlCommand(SELECT_ORDER_VEHICLE_TYPES, connection))
            {

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    int iOrderID = reader.GetOrdinal("ordOrderID");
                    int iVehicleID = reader.GetOrdinal("ovtVehicleID");

                    while (reader.Read())
                    {
                        int id = reader.GetInt32(iOrderID);
                        if (id != currentOrderId)
                        {
                            if (vehicleTypeCount > 0)
                            {
                                int index = Array.BinarySearch(orderId, currentOrderId);
                                if (index >= 0)
                                {
                                    int[] orderVehicleTypes = new int[vehicleTypeCount];
                                    Array.Copy(vehicleTypes, 0, orderVehicleTypes, 0, vehicleTypeCount);
                                    allOrders[index].VehicleTypes = orderVehicleTypes;
                                }
                            }

                            vehicleTypeCount = 0;
                            currentOrderId = id;
                        }

                        vehicleTypes[vehicleTypeCount++] = reader.GetInt32(iVehicleID);
                    }

                    if (vehicleTypeCount > 0)
                    {
                        int index = Array.BinarySearch(orderId, currentOrderId);
                        if (index >= 0)
                        {
                            int[] orderVehicleTypes = new int[vehicleTypeCount];
                            Array.Copy(vehicleTypes, 0, orderVehicleTypes, 0, vehicleTypeCount);
                            allOrders[index].VehicleTypes = orderVehicleTypes;
                        }
                    }
                }
            }

            orders = allOrders;

            // 6. Выход - Ok
            rc = 0;
            return rc;
        }
        catch
        {
            return rc;
        }
    }

    /// <summary>
    /// Выбор параметров используемых способов доставки
    /// </summary>
    /// <param name="vehicleTypes">ID способов доставки</param>
    /// <param name="connection">Открытое соединение</param>
    /// <param name="courierTypeRecordss">Параметры способов доставки или null</param>
    /// <returns>0 - параметры выбраны; иначе - параметры не выбраны</returns>
    private static int SelectCourierTypes(int[] vehicleTypes, SqlConnection connection, out CourierTypeRecord[] courierTypeRecordss)
    {
        // 1. Инициализация
        int rc = 1;
        courierTypeRecordss = null;

        try
        {
            // 2. Проверяем исходные данные
            rc = 2;
            if (vehicleTypes == null || vehicleTypes.Length <= 0)
                return rc;
            if (connection == null || connection.State != ConnectionState.Open)
                return rc;

            // 3. Строим список загружаемых типов
            rc = 3;
            StringBuilder sb = new StringBuilder(8 * vehicleTypes.Length);
            sb.Append(vehicleTypes[0]);

            for (int i = 1; i < vehicleTypes.Length; i++)
            {
                sb.Append(',');
                sb.Append(vehicleTypes[i]);
            }

            // 4. Настраиваем SQL-запрос
            rc = 4;
            string sqlText = string.Format(SELECT_COURIER_TYPES, sb.ToString());

            // 5. Выбираем данные способов доставки
            rc = 5;
            XmlSerializer serializer = new XmlSerializer(typeof(CourierTypeData));
            CourierTypeRecord[] records = new CourierTypeRecord[vehicleTypes.Length];
            int count = 0;

            using (SqlCommand cmd = new SqlCommand(sqlText, connection))
            {
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    // 5.1 Выбираем индексы колонок в выборке
                    rc = 51;
                    int iVehicleID = reader.GetOrdinal("crtVehicleID");
                    int iIsTaxi = reader.GetOrdinal("crtIsTaxi");
                    int iYandexType = reader.GetOrdinal("crtYandexType");
                    int iDServiceType = reader.GetOrdinal("crtDServiceType");
                    int iCourierType = reader.GetOrdinal("crtCourierType");
                    int iCalcMethod = reader.GetOrdinal("crtCalcMethod");
                    int iMaxOrderWeight = reader.GetOrdinal("crtMaxOrderWeight");
                    int iMaxWeight = reader.GetOrdinal("crtMaxWeight");
                    int iMaxOrderCount = reader.GetOrdinal("crtMaxOrderCount");
                    int iMaxDistance = reader.GetOrdinal("crtMaxDistance");
                    int iGetOrderTime = reader.GetOrdinal("crtGetOrderTime");
                    int iHandInTime = reader.GetOrdinal("crtHandInTime");
                    int iStartDelay = reader.GetOrdinal("crtStartDelay");
                    int iData = reader.GetOrdinal("crtData");

                    while (reader.Read())
                    {
                        // 5.2 Десериализуем xml-данные
                        rc = 52;
                        SqlXml dataXml = reader.GetSqlXml(iData);
                        CourierTypeData ctd = null;
                        using (XmlReader xmlReader = dataXml.CreateReader())
                        {
                            ctd = (CourierTypeData)serializer.Deserialize(xmlReader);
                            ctd.Create();
                        }

                        // 5.3 Сохраняем тип
                        rc = 53;
                        records[count++] =
                            new CourierTypeRecord(reader.GetInt32(iVehicleID),
                                                  reader.GetBoolean(iIsTaxi),
                                                  reader.GetInt32(iYandexType),
                                                  reader.GetInt32(iDServiceType),
                                                  reader.GetInt32(iCourierType),
                                                  reader.GetString(iCalcMethod),
                                                  reader.GetDouble(iMaxOrderWeight),
                                                  reader.GetDouble(iMaxWeight),
                                                  reader.GetInt32(iMaxOrderCount),
                                                  reader.GetDouble(iMaxDistance),
                                                  reader.GetDouble(iGetOrderTime),
                                                  reader.GetDouble(iHandInTime),
                                                  reader.GetDouble(iStartDelay),
                                                  ctd);
                    }
                }
            }

            if (count < records.Length)
            {
                Array.Resize(ref records, count);
            }

            courierTypeRecordss = records;

            // 6. Выход - Ok
            rc = 0;
            return rc;
        }
        catch
        {
            return rc;
        }
    }

    /// <summary>
    /// Выбор курьеров и такси, доступных для построения отгрузок
    /// </summary>
    /// <param name="connection">Открытое соединение</param>
    /// <param name="records">Выбранные курьеры и такси или null</param>
    /// <returns>0 - курьеры выбраны; иначе - курьеры не выбраны</returns>
    private static int SelectCouriers(SqlConnection connection, out CourierRecord[] records)
    {
        // 1. Инициализация
        int rc = 1;
        records = null;

        try
        {
            // 2. Проверяем исходные данные
            rc = 2;
            if (connection == null || connection.State != ConnectionState.Open)
                return rc;

            // 3. Выбираем заказы
            rc = 3;
            CourierRecord[] allRecords = new CourierRecord[3000];
            int count = 0;

            using (SqlCommand cmd = new SqlCommand(SELECT_COURIERS, connection))
            {
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    int iCourierID = reader.GetOrdinal("crsCourierID");
                    int iVehicleID = reader.GetOrdinal("crsVehicleID");
                    int iStatusID = reader.GetOrdinal("crsStatusID");
                    int iWorkStart = reader.GetOrdinal("crsWorkStart");
                    int iWorkEnd = reader.GetOrdinal("crsWorkEnd");
                    int iLunchStart = reader.GetOrdinal("crsLunchTimeStart");
                    int iLunchEnd = reader.GetOrdinal("crsLunchTimeEnd");
                    int iLastDeliveryStart = reader.GetOrdinal("crsLastDeliveryStart");
                    int iLastDeliveryEnd = reader.GetOrdinal("crsLastDeliveryEnd");
                    int iOrderCount = reader.GetOrdinal("crsOrderCount");
                    int iTotalDeliveryTime = reader.GetOrdinal("crsTotalDeliveryTime");
                    int iTotalCost = reader.GetOrdinal("crsTotalCost");
                    int iAverageOrderCost = reader.GetOrdinal("crsAverageOrderCost");
                    int iShopID = reader.GetOrdinal("crsShopID");
                    int iLatitude = reader.GetOrdinal("crsLatitude");
                    int iLongitude = reader.GetOrdinal("crsLongitude");

                    while (reader.Read())
                    {
                        CourierRecord record = new CourierRecord();
                        record.CourierId = reader.GetInt32(iCourierID);
                        record.VehicleId = reader.GetInt32(iVehicleID);
                        record.Status = (CourierStatus)reader.GetInt32(iStatusID);
                        record.WorkStart = reader.GetTimeSpan(iWorkStart);
                        record.WorkEnd = reader.GetTimeSpan(iWorkEnd);
                        record.LunchTimeStart = reader.GetTimeSpan(iLunchStart);
                        record.LunchTimeEnd = reader.GetTimeSpan(iLunchEnd);
                        record.LastDeliveryStart = reader.GetDateTime(iLastDeliveryStart);
                        record.LastDeliveryEnd = reader.GetDateTime(iLastDeliveryEnd);
                        record.OrderCount = reader.GetInt32(iOrderCount);
                        record.TotalDeliveryTime = reader.GetDouble(iTotalDeliveryTime);
                        record.TotalCost = reader.GetDouble(iTotalCost);
                        record.AverageOrderCost = reader.GetDouble(iAverageOrderCost);
                        record.ShopId = reader.GetInt32(iShopID);
                        record.Latitude = reader.GetDouble(iLatitude);
                        record.Longitude = reader.GetDouble(iLongitude);

                        if (count >= allRecords.Length)
                        {
                            Array.Resize(ref allRecords, (int)(1.25 * allRecords.Length));
                        }

                        allRecords[count++] = record;
                    }
                }
            }

            if (count < allRecords.Length)
            {
                Array.Resize(ref allRecords, count);
            }

            records = allRecords;

            // 5. Выход - Ok
            rc = 0;
            return rc;
        }
        catch
        {
            return rc;
        }
    }

    /// <summary>
    /// Выбор порогов для средней стоимости доставки заказа
    /// </summary>
    /// <param name="connection">Открытое соединение</param>
    /// <param name="records">Записи таблицы lsvAverageDeliveryCost</param>
    /// <returns>0 - пороги выбраны; иначе - попроги не выбраны</returns>
    private static int SelectThresholds(SqlConnection connection, out AverageDeliveryCostRecord[] records)
    {
        // 1. Инициализация
        int rc = 1;
        records = null;

        try
        {
            // 2. Проверяем исходные данные
            rc = 2;
            if (connection == null || connection.State != ConnectionState.Open)
                return rc;

            // 3. Выбираем заказы
            rc = 3;
            AverageDeliveryCostRecord[] allRecords = new AverageDeliveryCostRecord[30000];
            int count = 0;

            using (SqlCommand cmd = new SqlCommand(SELECT_AVERAGE_COST_THRESHOLDS, connection))
            {
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    int iShopId = reader.GetOrdinal("adcShopID");
                    int iVehicleId = reader.GetOrdinal("adcVehicleTypeID");
                    int iCost = reader.GetOrdinal("adcCost");

                    while (reader.Read())
                    {
                        AverageDeliveryCostRecord record = new AverageDeliveryCostRecord();
                        record.ShopId = reader.GetInt32(iShopId);
                        record.VehicleId = reader.GetInt32(iVehicleId);
                        record.Cost = reader.GetDouble(iCost);

                        if (count >= allRecords.Length)
                        {
                            Array.Resize(ref allRecords, (int)(1.25 * allRecords.Length));
                        }

                        allRecords[count++] = record;
                    }
                }
            }

            if (count < allRecords.Length)
            {
                Array.Resize(ref allRecords, count);
            }

            records = allRecords;

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
    /// Загрузка ограничений на длину маршрута по числу заказов,
    /// из которых создаются отгрузки
    /// </summary>
    /// <param name="service_id">ID LogisticsService</param>
    /// <param name="connection">Открытое соединение</param>
    /// <param name="records">Записи таблицы lsvAverageDeliveryCost</param>
    /// <returns>0 - пороги выбраны; иначе - попроги не выбраны</returns>
    private static int SelectMaxOrdersOfRoute(int service_id, SqlConnection connection, out MaxOrdersOfRouteRecord[] records)
    {
        // 1. Инициализация
        int rc = 1;
        records = null;

        try
        {
            // 2. Проверяем исходные данные
            rc = 2;
            if (connection == null || connection.State != ConnectionState.Open)
                return rc;

            // 3. Выбираем заказы
            rc = 3;
            MaxOrdersOfRouteRecord[] allRecords = new MaxOrdersOfRouteRecord[16];
            int count = 0;

            using (SqlCommand cmd = new SqlCommand(string.Format(SELECT_MAX_ORDERS_OF_ROUTE, service_id), connection))
            {
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    int iLength = reader.GetOrdinal("Length");
                    int iMaxOrders = reader.GetOrdinal("MaxOrders");

                    while (reader.Read())
                    {
                        MaxOrdersOfRouteRecord record = new MaxOrdersOfRouteRecord(
                            reader.GetInt32(iLength),
                            reader.GetInt32(iMaxOrders));

                        if (count >= allRecords.Length)
                        {
                            Array.Resize(ref allRecords, (int)(1.25 * allRecords.Length));
                        }

                        allRecords[count++] = record;
                    }
                }
            }

            if (count < allRecords.Length)
            {
                Array.Resize(ref allRecords, count);
            }

            records = allRecords;

            // 5. Выход - Ok
            rc = 0;
            return rc;
        }
        catch
        {
            return rc;
        }
    }

    /// <summary>
    /// Построение всех расчетных контекстов
    /// </summary>
    /// <param name="serviceId">ID LogisticsService</param>
    /// <param name="calcTime">Момент времени, на который делаются расчеты</param>
    /// <param name="shops">Магазины</param>
    /// <param name="allOrders">Заказы</param>
    /// <param name="allCouriers">Курьеры</param>
    /// <param name="limitations">Ограничения на длину маршрута по числу заказов</param>
    /// <returns>Контексты или null</returns>
    private static ThreadContext[] GetThreadContext(int serviceId, DateTime calcTime, Shop[] shops, AllOrders allOrders, AllCouriers allCouriers, RouteLimitations limitations)
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
            if (limitations == null || !limitations.IsCreated)
                return null;

            #if debug
                Logger.WriteToLog(201, $"GetThreadContext. service_id = {serviceId}. Enter...", 0);
            #endif

            // 3. Строим контексты всех потоков
            rc = 3;
            int size = shops.Length * allCouriers.BaseKeys.Length;
            ThreadContext[] context = new ThreadContext[size];
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
                int[] courierVehicleTypes = AllCouriers.GetCourierVehicleTypes(shopCouriers);
                if (courierVehicleTypes == null || courierVehicleTypes.Length <= 0)
                    continue;

                int[] orderVehicleTypes = AllOrders.GetOrderVehicleTypes(shopOrders);
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

                        int maxRouteLength = limitations.GetRouteLength(count);

                        //Courier courier = allCouriers.CreateReferenceCourier(orderVehicleTypes[j]);
                        Courier courier = allCouriers.FindFirstShopCourierByType(shop.Id, orderVehicleTypes[j]);
                        if (courier != null)
                        {
                            if (courier.MaxOrderCount < maxRouteLength)
                                maxRouteLength = courier.MaxOrderCount;
                            context[contextCount++] = new ThreadContext(serviceId, calcTime, maxRouteLength, shop, contextOrders, courier, null, null);
                        }
                        else
                        {
                            #if debug
                                Logger.WriteToLog(204, $"GetThreadContext. service_id = {serviceId}. shop_id = {shop.Id}. orderVehicleType = {orderVehicleTypes[j]}", 0);
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
                Logger.WriteToLog(202, $"GetThreadContext. service_id = {serviceId}. Exit. context count = {contextCount}", 0);
            #endif

            // 4. Выход - Ok
            return context;
        }
        catch (Exception ex)
        {
            #if debug
                Logger.WriteToLog(203, $"GetThreadContext. service_id = {serviceId}. rc = {rc}. Exception = {ex.Message}", 0);
            #endif
            return null;
        }
    }

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
    private static ThreadContext[] GetThreadContextEx(SqlConnection connection, int serviceId, DateTime calcTime, Shop[] shops, AllOrders allOrders, AllCouriers allCouriers, RouteLimitations limitations)
    {
        // 1. Инициализация
        int rc = 1;

        try
        {
            // 2. Проверяем исходные данные
            rc = 2;
            if (connection == null || connection.State != ConnectionState.Open)
                return null;
            if (shops == null || shops.Length <= 0)
                return null;
            if (allOrders == null || !allOrders.IsCreated)
                return null;
            if (allCouriers == null || !allCouriers.IsCreated)
                return null;
            if (limitations == null || !limitations.IsCreated)
                return null;

            #if debug
                Logger.WriteToLog(201, $"GetThreadContext. service_id = {serviceId}. Enter...", 0);
            #endif

            // 3. Строим контексты всех потоков
            rc = 3;
            int size = shops.Length * allCouriers.BaseKeys.Length;
            ThreadContext[] context = new ThreadContext[size];
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
                int[] courierVehicleTypes = AllCouriers.GetCourierVehicleTypes(shopCouriers);
                if (courierVehicleTypes == null || courierVehicleTypes.Length <= 0)
                    continue;

                int[] orderVehicleTypes = AllOrders.GetOrderVehicleTypes(shopOrders);
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

                        int maxRouteLength = limitations.GetRouteLength(count);

                        //Courier courier = allCouriers.CreateReferenceCourier(orderVehicleTypes[j]);
                        Courier courier = allCouriers.FindFirstShopCourierByType(shop.Id, orderVehicleTypes[j]);
                        if (courier != null)
                        {
                            contextOrders = FilterOrdersOnMaxWeight(courier.MaxOrderWeight, contextOrders);
                            if (contextOrders != null && contextOrders.Length > 0)
                            {
                                if (courier.MaxOrderCount < maxRouteLength)
                                    maxRouteLength = courier.MaxOrderCount;
                                Point[,] geoData;
                                int rc1 = GeoData.Select(connection, serviceId, courier.YandexType, shop, contextOrders, out geoData);
                                if (rc1 == 0)
                                {
                                    context[contextCount++] = new ThreadContext(serviceId, calcTime, maxRouteLength, shop, contextOrders, courier, geoData, null);
                                    //context[contextCount++] = new ThreadContext(serviceId, calcTime, maxRouteLength, shop, contextOrders, courier, null);
                                }
                            }
                        }
                        else
                        {
                            #if debug
                                Logger.WriteToLog(204, $"GetThreadContext. service_id = {serviceId}. shop_id = {shop.Id}. orderVehicleType = {orderVehicleTypes[j]}", 0);
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
                Logger.WriteToLog(202, $"GetThreadContext. service_id = {serviceId}. Exit. context count = {contextCount}", 0);
            #endif

            // 4. Выход - Ok
            return context;
        }
        catch (Exception ex)
        {
            #if debug
                Logger.WriteToLog(203, $"GetThreadContext. service_id = {serviceId}. rc = {rc}. Exception = {ex.Message}", 0);
            #endif
            return null;
        }
    }

    /// <summary>
    /// Сравнение двух контекстов по количеству заказов
    /// </summary>
    /// <param name="context1">Контекст 1</param>
    /// <param name="context2">Контекст 2</param>
    /// <returns>- 1  - Контекст1 больше Контекст2; 0 - Контекст1 = Контекст2; 1 - Контекст1 меньше Контекст2</returns>
    private static int CompareContextByOrderCount(ThreadContext context1, ThreadContext context2)
    {
        if (context1.OrderCount > context2.OrderCount)
            return -1;
        if (context1.OrderCount < context2.OrderCount)
            return 1;
        return 0;
    }

    /// <summary>
    /// Сохрание отгрузок в БД
    /// (Предполагается, что таблица lsvDeliveries предварительно очищена)
    /// </summary>
    /// <param name="deliveries">Сохраняемые отгрузки</param>
    /// <param name="connection">Открытое соединение</param>
    /// <returns>0 - отгрузки сохранены; иначе - отгрузки не сохранены</returns>
    private static int SaveDeliveries(CourierDeliveryInfo[] deliveries, SqlConnection connection)
    {
        // 1. Инициализация
        int rc = 1;

        try
        {
            // 2. Проверяем исходные данные
            rc = 2;
            if (deliveries == null || deliveries.Length <= 0)
                return rc;
            if (connection == null || connection.State != ConnectionState.Open)
                return rc;

            // 3. Строим таблицу lsvDeliveries, передаваемую на сервер
            rc = 3;
            using (DataTable table = new DataTable("lsvDeliveries"))
            {
                table.Columns.Add("dlvID", typeof(int));
                table.Columns.Add("dlvCourierID", typeof(int));
                table.Columns.Add("dlvShopID", typeof(int));
                table.Columns.Add("dlvCalculationTime", typeof(DateTime));
                table.Columns.Add("dlvOrderCount", typeof(int));
                table.Columns.Add("dlvOrderCost", typeof(double));
                table.Columns.Add("dlvWeight", typeof(double));
                table.Columns.Add("dlvDeliveryTime", typeof(double));
                table.Columns.Add("dlvExecutionTime", typeof(double));
                table.Columns.Add("dlvIsLoop", typeof(bool));
                table.Columns.Add("dlvReserveTime", typeof(double));
                table.Columns.Add("dlvStartDeliveryInterval", typeof(DateTime));
                table.Columns.Add("dlvEndDeliveryInterval", typeof(DateTime));
                table.Columns.Add("dlvCost", typeof(double));
                table.Columns.Add("dlvStartDelivery", typeof(DateTime));
                table.Columns.Add("dlvCompleted", typeof(bool));
                table.Columns.Add("dlvPermutationKey", typeof(string));
                table.Columns.Add("dlvPriority", typeof(int));
                table.Columns.Add("dlvIsReceipted", typeof(bool));
                StringBuilder sb = new StringBuilder(120);
                string[] deliveryPermutationKey = new string[deliveries.Length];

                for (int i = 0; i < deliveries.Length; i++)
                {
                    CourierDeliveryInfo delivery = deliveries[i];
                    Order[] orders = delivery.Orders;
                    if (orders == null || orders.Length <= 0)
                        continue;

                    Array.Sort(orders, CompareByOrderId);

                    sb.Length = 0;
                    int priority = orders[0].Priority;
                    sb.Append(orders[0].Id);
                    bool isReceipted = (orders[0].Status == OrderStatus.Receipted);

                    for (int j = 1; j < orders.Length; j++)
                    {
                        Order order = orders[j];
                        if (order.Priority > priority)
                            priority = order.Priority;
                        sb.Append('.');
                        sb.Append(order.Id);
                        if (order.Status == OrderStatus.Receipted)
                            isReceipted = true;
                    }

                    table.Rows.Add(
                        i + 1,                       // dlvID
                        delivery.DeliveryCourier.Id, // dlvCourierID
                        delivery.FromShop.Id,        // dlvShopID
                        delivery.CalculationTime,    // dlvCalculationTime
                        delivery.OrderCount,         // dlvOrderCount
                        delivery.OrderCost,          // dlvOrderCost
                        delivery.Weight,
                        delivery.DeliveryTime,
                        delivery.ExecutionTime,
                        delivery.IsLoop,
                        delivery.ReserveTime.TotalMinutes,
                        delivery.StartDeliveryInterval,
                        delivery.EndDeliveryInterval,
                        delivery.Cost,
                        delivery.StartDeliveryInterval,
                        false,                       // dlvCompleted
                        sb.ToString(),               // dlvPermutationKey
                        priority,
                        isReceipted                  // dlvReceipted
                        );
                }

                // 4. Заполняем таблицу lsvDeliveries
                rc = 4;
                using (var copy = new SqlBulkCopy(connection, SqlBulkCopyOptions.KeepIdentity, null))
                {
                    copy.DestinationTableName = "dbo.lsvDeliveries";
                    copy.ColumnMappings.Add("dlvID", "dlvID");
                    copy.ColumnMappings.Add("dlvCourierID", "dlvCourierID");
                    copy.ColumnMappings.Add("dlvShopID", "dlvShopID");
                    copy.ColumnMappings.Add("dlvCalculationTime", "dlvCalculationTime");
                    copy.ColumnMappings.Add("dlvOrderCount", "dlvOrderCount");
                    copy.ColumnMappings.Add("dlvOrderCost", "dlvOrderCost");
                    copy.ColumnMappings.Add("dlvWeight", "dlvWeight");
                    copy.ColumnMappings.Add("dlvDeliveryTime", "dlvDeliveryTime");
                    copy.ColumnMappings.Add("dlvExecutionTime", "dlvExecutionTime");
                    copy.ColumnMappings.Add("dlvIsLoop", "dlvIsLoop");
                    copy.ColumnMappings.Add("dlvReserveTime", "dlvReserveTime");
                    copy.ColumnMappings.Add("dlvStartDeliveryInterval", "dlvStartDeliveryInterval");
                    copy.ColumnMappings.Add("dlvEndDeliveryInterval", "dlvEndDeliveryInterval");
                    copy.ColumnMappings.Add("dlvCost", "dlvCost");
                    copy.ColumnMappings.Add("dlvStartDelivery", "dlvStartDelivery");
                    copy.ColumnMappings.Add("dlvCompleted", "dlvCompleted");
                    copy.ColumnMappings.Add("dlvPermutationKey", "dlvPermutationKey");
                    copy.ColumnMappings.Add("dlvPriority", "dlvPriority");
                    copy.ColumnMappings.Add("dlvIsReceipted", "dlvIsReceipted");
                    copy.WriteToServer(table);
                }
            }

            // 5. Строим таблицу lsvDeliveryOrders, передаваемую на сервер
            rc = 5;
            using (DataTable table = new DataTable("lsvDeliveryOrders"))
            {
                table.Columns.Add("dorDlvID", typeof(int));
                table.Columns.Add("dorOrderID", typeof(int));

                for (int i = 0; i < deliveries.Length; i++)
                {
                    CourierDeliveryInfo delivery = deliveries[i];
                    Order[] orders = delivery.Orders;
                    if (orders == null || orders.Length <= 0)
                        continue;
                    int dorDlvID = i + 1;

                    for (int j = 0; j < orders.Length; j++)
                    {
                        table.Rows.Add(dorDlvID, orders[j].Id);
                    }
                }

                // 6. Заполняем таблицу lsvDeliveryOrders
                rc = 6;

                using (var copy = new SqlBulkCopy(connection))
                {
                    copy.DestinationTableName = "dbo.lsvDeliveryOrders";
                    copy.ColumnMappings.Add("dorDlvID", "dorDlvID");
                    copy.ColumnMappings.Add("dorOrderID", "dorOrderID");
                    copy.WriteToServer(table);
                }
            }

            // 7. Строим таблицу lsvDeliveryOrders, передаваемую на сервер
            rc = 7;
            using (DataTable table = new DataTable("lsvDeliveryOrders"))
            {
                table.Columns.Add("dorDlvID", typeof(int));
                table.Columns.Add("dorOrderID", typeof(int));

                for (int i = 0; i < deliveries.Length; i++)
                {
                    CourierDeliveryInfo delivery = deliveries[i];
                    Order[] orders = delivery.Orders;
                    if (orders == null || orders.Length <= 0)
                        return rc;
                    int dorDlvID = i + 1;

                    for (int j = 0; j < orders.Length; j++)
                    {
                        table.Rows.Add(dorDlvID, orders[j].Id);
                    }
                }

                // 8. Заполняем таблицу lsvDeliveryOrders
                rc = 8;
                using (var copy = new SqlBulkCopy(connection))
                {
                    copy.DestinationTableName = "dbo.lsvDeliveryOrders";
                    copy.ColumnMappings.Add("dorDlvID", "dorDlvID");
                    copy.ColumnMappings.Add("dorOrderID", "dorOrderID");
                    copy.WriteToServer(table);
                }
            }

            // 9. Строим таблицу lsvNodeDeliveryTime, передаваемую на сервер
            rc = 9;
            using (DataTable table = new DataTable("lsvNodeDeliveryTime"))
            {
                table.Columns.Add("ndtDlvID", typeof(int));
                table.Columns.Add("ndtTime", typeof(double));

                for (int i = 0; i < deliveries.Length; i++)
                {
                    CourierDeliveryInfo delivery = deliveries[i];
                    double[] nodeDeliveryTime = delivery.NodeDeliveryTime;
                    if (nodeDeliveryTime == null || nodeDeliveryTime.Length <= 0)
                        continue;
                    int ndtDlvID = i + 1;

                    for (int j = 0; j < nodeDeliveryTime.Length; j++)
                    {
                        table.Rows.Add(ndtDlvID, nodeDeliveryTime[j]);
                    }
                }

                // 10. Заполняем таблицу lsvNodeDeliveryTime
                rc = 10;
                using (var copy = new SqlBulkCopy(connection))
                {
                    copy.DestinationTableName = "dbo.lsvNodeDeliveryTime";
                    copy.ColumnMappings.Add("ndtDlvID", "ndtDlvID");
                    copy.ColumnMappings.Add("ndtTime", "ndtTime");
                    copy.WriteToServer(table);
                }
            }

            // 11. Строим таблицу lsvDeliveryNodeInfo, передаваемую на сервер
            rc = 11;
            using (DataTable table = new DataTable("lsvDeliveryNodeInfo"))
            {
                table.Columns.Add("dniDlvID", typeof(int));
                table.Columns.Add("dniDistance", typeof(int));
                table.Columns.Add("dniTime", typeof(int));

                for (int i = 0; i < deliveries.Length; i++)
                {
                    CourierDeliveryInfo delivery = deliveries[i];
                    Point[] nodeInfo = delivery.NodeInfo;
                    if (nodeInfo == null || nodeInfo.Length <= 0)
                        continue;

                    int dniDlvID = i + 1;

                    for (int j = 0; j < nodeInfo.Length; j++)
                    {


                        table.Rows.Add(dniDlvID, nodeInfo[j].X, nodeInfo[j].Y);
                    }
                }

                // 12. Заполняем таблицу lsvDeliveryNodeInfo
                rc = 12;
                using (var copy = new SqlBulkCopy(connection))
                {
                    copy.DestinationTableName = "dbo.lsvDeliveryNodeInfo";
                    copy.ColumnMappings.Add("dniDlvID", "dniDlvID");
                    copy.ColumnMappings.Add("dniDistance", "dniDistance");
                    copy.ColumnMappings.Add("dniTime", "dniTime");
                    copy.WriteToServer(table);
                }
            }

            // 13. Выход - OK
            rc = 0;
            return rc;
        }
        catch (Exception ex)
        {
            #if debug
                Logger.WriteToLog(700, $"SaveDeliveries rc = {rc} Exception = {ex.Message}", 2);
            #endif
            return rc;
        }
    }

    /// <summary>
    /// Очистка таблиц с отгрузками
    /// </summary>
    /// <param name="connection">Открытое соединение</param>
    /// <returns>0 - таблицы очищены; иначе - таблицы не очищены</returns>
    private static int ClearDeliveries(SqlConnection connection)
    {
        int rc = 1;

        try
        {
            // 2. Проверяем исходные данные
            rc = 2;
            if (connection == null || connection.State != ConnectionState.Open)
                return rc;

            // 3. Очищаем таблицы
            rc = 3;
            using (SqlCommand cmd = new SqlCommand(CLEAR_DELIVERIES, connection))
            {
                cmd.ExecuteNonQuery();
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
    /// Срвнение двух заказов по orderID
    /// </summary>
    /// <param name="order1">Заказ 1</param>
    /// <param name="order2">Заказ 2</param>
    /// <returns>-1  - Заказа1 меньше Заказ2; 0  - Заказ1 = Заказ2; 1 - Заказ1 больше Заказ2</returns>
    private static int CompareByOrderId(Order order1, Order order2)
    {
        if (order1.Id < order2.Id)
            return -1;
        if (order1.Id > order2.Id)
            return 1;
        return 0;
    }

    /// <summary>
    /// Сохрание отгрузок в БД
    /// (Предполагается, что таблица lsvDeliveries предварительно очищена)
    /// </summary>
    /// <param name="deliveries">Сохраняемые отгрузки</param>
    /// <param name="serverName">Имя сервера</param>
    /// <param name="dbName">Имя БД</param>
    /// <returns>0 - отгрузки сохранены; иначе - отгрузки не сохранены</returns>
    private static int SaveDeliveriesE0(CourierDeliveryInfo[] deliveries, string serverName, string dbName)
    {
        // 1. Инициализация
        int rc = 1;
        //WindowsImpersonationContext impersonatedIdentity = null;

        try
        {
            // 2. Проверяем исходные данные
            rc = 2;
            if (deliveries == null || deliveries.Length <= 0)
                return rc;
            //if (connection == null || connection.State != ConnectionState.Open)
            //    return rc;

            //WindowsIdentity currentIdentity = SqlContext.WindowsIdentity;
            //impersonatedIdentity = currentIdentity.Impersonate();

            using (SqlConnection connection = new SqlConnection($"Server={serverName};Database={dbName};Integrated Security=true"))
            {
                connection.Open();
                // 3. Строим таблицу lsvDeliveries, передаваемую на сервер
                rc = 3;
                using (DataTable table = new DataTable("lsvDeliveries"))
                {
                    table.Columns.Add("dlvID", typeof(int));
                    table.Columns.Add("dlvCourierID", typeof(int));
                    table.Columns.Add("dlvShopID", typeof(int));
                    table.Columns.Add("dlvCalculationTime", typeof(DateTime));
                    table.Columns.Add("dlvOrderCount", typeof(int));
                    table.Columns.Add("dlvOrderCost", typeof(double));
                    table.Columns.Add("dlvWeight", typeof(double));
                    table.Columns.Add("dlvDeliveryTime", typeof(double));
                    table.Columns.Add("dlvExecutionTime", typeof(double));
                    table.Columns.Add("dlvIsLoop", typeof(bool));
                    table.Columns.Add("dlvReserveTime", typeof(double));
                    table.Columns.Add("dlvStartDeliveryInterval", typeof(DateTime));
                    table.Columns.Add("dlvEndDeliveryInterval", typeof(DateTime));
                    table.Columns.Add("dlvCost", typeof(double));
                    table.Columns.Add("dlvStartDelivery", typeof(DateTime));
                    table.Columns.Add("dlvCompleted", typeof(bool));
                    table.Columns.Add("dlvPermutationKey", typeof(string));
                    table.Columns.Add("dlvPriority", typeof(int));
                    table.Columns.Add("dlvIsReceipted", typeof(bool));
                    StringBuilder sb = new StringBuilder(120);
                    string[] deliveryPermutationKey = new string[deliveries.Length];

                    for (int i = 0; i < deliveries.Length; i++)
                    {
                        CourierDeliveryInfo delivery = deliveries[i];
                        Order[] orders = delivery.Orders;
                        if (orders == null || orders.Length <= 0)
                            continue;

                        Array.Sort(orders, CompareByOrderId);

                        sb.Length = 0;
                        int priority = orders[0].Priority;
                        sb.Append(delivery.DeliveryCourier.VehicleID);
                        sb.Append('.');
                        sb.Append(orders[0].Id);
                        bool isReceipted = (orders[0].Status == OrderStatus.Receipted);

                        for (int j = 1; j < orders.Length; j++)
                        {
                            Order order = orders[j];
                            if (order.Priority > priority)
                                priority = order.Priority;
                            sb.Append('.');
                            sb.Append(order.Id);
                            if (order.Status == OrderStatus.Receipted)
                                isReceipted = true;
                        }

                        table.Rows.Add(
                            i + 1,                       // dlvID
                            delivery.DeliveryCourier.Id, // dlvCourierID
                            delivery.FromShop.Id,        // dlvShopID
                            delivery.CalculationTime,    // dlvCalculationTime
                            delivery.OrderCount,         // dlvOrderCount
                            delivery.OrderCost,          // dlvOrderCost
                            delivery.Weight,
                            delivery.DeliveryTime,
                            delivery.ExecutionTime,
                            delivery.IsLoop,
                            delivery.ReserveTime.TotalMinutes,
                            delivery.StartDeliveryInterval,
                            delivery.EndDeliveryInterval,
                            delivery.Cost,
                            delivery.StartDeliveryInterval,
                            false,                       // dlvCompleted
                            sb.ToString(),               // dlvPermutationKey
                            priority,
                            isReceipted                  // dlvReceipted
                            );
                    }

                    // 4. Заполняем таблицу lsvDeliveries
                    rc = 4;
                    using (var copy = new SqlBulkCopy(connection, SqlBulkCopyOptions.KeepIdentity, null))
                    {
                        copy.DestinationTableName = "dbo.lsvDeliveries";
                        copy.ColumnMappings.Add("dlvID", "dlvID");
                        copy.ColumnMappings.Add("dlvCourierID", "dlvCourierID");
                        copy.ColumnMappings.Add("dlvShopID", "dlvShopID");
                        copy.ColumnMappings.Add("dlvCalculationTime", "dlvCalculationTime");
                        copy.ColumnMappings.Add("dlvOrderCount", "dlvOrderCount");
                        copy.ColumnMappings.Add("dlvOrderCost", "dlvOrderCost");
                        copy.ColumnMappings.Add("dlvWeight", "dlvWeight");
                        copy.ColumnMappings.Add("dlvDeliveryTime", "dlvDeliveryTime");
                        copy.ColumnMappings.Add("dlvExecutionTime", "dlvExecutionTime");
                        copy.ColumnMappings.Add("dlvIsLoop", "dlvIsLoop");
                        copy.ColumnMappings.Add("dlvReserveTime", "dlvReserveTime");
                        copy.ColumnMappings.Add("dlvStartDeliveryInterval", "dlvStartDeliveryInterval");
                        copy.ColumnMappings.Add("dlvEndDeliveryInterval", "dlvEndDeliveryInterval");
                        copy.ColumnMappings.Add("dlvCost", "dlvCost");
                        copy.ColumnMappings.Add("dlvStartDelivery", "dlvStartDelivery");
                        copy.ColumnMappings.Add("dlvCompleted", "dlvCompleted");
                        copy.ColumnMappings.Add("dlvPermutationKey", "dlvPermutationKey");
                        copy.ColumnMappings.Add("dlvPriority", "dlvPriority");
                        copy.ColumnMappings.Add("dlvIsReceipted", "dlvIsReceipted");
                        copy.WriteToServer(table);
                        copy.Close();
                    }
                }

                // 5. Строим таблицу lsvDeliveryOrders, передаваемую на сервер
                rc = 5;
                using (DataTable table = new DataTable("lsvDeliveryOrders"))
                {
                    table.Columns.Add("dorDlvID", typeof(int));
                    table.Columns.Add("dorOrderID", typeof(int));

                    for (int i = 0; i < deliveries.Length; i++)
                    {
                        CourierDeliveryInfo delivery = deliveries[i];
                        Order[] orders = delivery.Orders;
                        if (orders == null || orders.Length <= 0)
                            continue;
                        int dorDlvID = i + 1;

                        for (int j = 0; j < orders.Length; j++)
                        {
                            table.Rows.Add(dorDlvID, orders[j].Id);
                        }
                    }

                    // 6. Заполняем таблицу lsvDeliveryOrders
                    rc = 6;

                    using (var copy = new SqlBulkCopy(connection))
                    {
                        copy.DestinationTableName = "dbo.lsvDeliveryOrders";
                        copy.ColumnMappings.Add("dorDlvID", "dorDlvID");
                        copy.ColumnMappings.Add("dorOrderID", "dorOrderID");
                        copy.WriteToServer(table);
                        copy.Close();
                    }
                }

                // 7. Строим таблицу lsvDeliveryOrders, передаваемую на сервер
                rc = 7;
                using (DataTable table = new DataTable("lsvDeliveryOrders"))
                {
                    table.Columns.Add("dorDlvID", typeof(int));
                    table.Columns.Add("dorOrderID", typeof(int));

                    for (int i = 0; i < deliveries.Length; i++)
                    {
                        CourierDeliveryInfo delivery = deliveries[i];
                        Order[] orders = delivery.Orders;
                        if (orders == null || orders.Length <= 0)
                            return rc;
                        int dorDlvID = i + 1;

                        for (int j = 0; j < orders.Length; j++)
                        {
                            table.Rows.Add(dorDlvID, orders[j].Id);
                        }
                    }

                    // 8. Заполняем таблицу lsvDeliveryOrders
                    rc = 8;
                    using (var copy = new SqlBulkCopy(connection))
                    {
                        copy.DestinationTableName = "dbo.lsvDeliveryOrders";
                        copy.ColumnMappings.Add("dorDlvID", "dorDlvID");
                        copy.ColumnMappings.Add("dorOrderID", "dorOrderID");
                        copy.WriteToServer(table);
                        copy.Close();
                    }
                }

                // 9. Строим таблицу lsvNodeDeliveryTime, передаваемую на сервер
                rc = 9;
                using (DataTable table = new DataTable("lsvNodeDeliveryTime"))
                {
                    table.Columns.Add("ndtDlvID", typeof(int));
                    table.Columns.Add("ndtTime", typeof(double));

                    for (int i = 0; i < deliveries.Length; i++)
                    {
                        CourierDeliveryInfo delivery = deliveries[i];
                        double[] nodeDeliveryTime = delivery.NodeDeliveryTime;
                        if (nodeDeliveryTime == null || nodeDeliveryTime.Length <= 0)
                            continue;
                        int ndtDlvID = i + 1;

                        for (int j = 0; j < nodeDeliveryTime.Length; j++)
                        {
                            table.Rows.Add(ndtDlvID, nodeDeliveryTime[j]);
                        }
                    }

                    // 10. Заполняем таблицу lsvNodeDeliveryTime
                    rc = 10;
                    using (var copy = new SqlBulkCopy(connection))
                    {
                        copy.DestinationTableName = "dbo.lsvNodeDeliveryTime";
                        copy.ColumnMappings.Add("ndtDlvID", "ndtDlvID");
                        copy.ColumnMappings.Add("ndtTime", "ndtTime");
                        copy.WriteToServer(table);
                        copy.Close();
                    }
                }

                // 11. Строим таблицу lsvDeliveryNodeInfo, передаваемую на сервер
                rc = 11;
                using (DataTable table = new DataTable("lsvDeliveryNodeInfo"))
                {
                    table.Columns.Add("dniDlvID", typeof(int));
                    table.Columns.Add("dniDistance", typeof(int));
                    table.Columns.Add("dniTime", typeof(int));

                    for (int i = 0; i < deliveries.Length; i++)
                    {
                        CourierDeliveryInfo delivery = deliveries[i];
                        Point[] nodeInfo = delivery.NodeInfo;
                        if (nodeInfo == null || nodeInfo.Length <= 0)
                            continue;

                        int dniDlvID = i + 1;

                        for (int j = 0; j < nodeInfo.Length; j++)
                        {
                            table.Rows.Add(dniDlvID, nodeInfo[j].X, nodeInfo[j].Y);
                        }
                    }

                    // 12. Заполняем таблицу lsvDeliveryNodeInfo
                    rc = 12;
                    using (var copy = new SqlBulkCopy(connection))
                    {
                        copy.DestinationTableName = "dbo.lsvDeliveryNodeInfo";
                        copy.ColumnMappings.Add("dniDlvID", "dniDlvID");
                        copy.ColumnMappings.Add("dniDistance", "dniDistance");
                        copy.ColumnMappings.Add("dniTime", "dniTime");
                        copy.WriteToServer(table);
                        copy.Close();
                    }
                }

                connection.Close();
            }

            // 13. Выход - OK
            rc = 0;
            return rc;
        }
        catch (Exception ex)
        {
            #if debug
                Logger.WriteToLog(700, $"SaveDeliveries rc = {rc} Exception = {ex.Message}", 2);
            #endif
            return rc;
        }
        //finally
        //{
        //    if (impersonatedIdentity != null)
        //    {
        //        #if debug
        //            Logger.WriteToLog(701, $"SaveDeliveries rc = {rc} Before Dispose()", 0);
        //        #endif

        //        //impersonatedIdentity.Dispose();

        //        #if debug
        //            Logger.WriteToLog(702, $"SaveDeliveries rc = {rc} After Dispose()", 0);
        //        #endif
        //    }
        //}
    }

    /// <summary>
    /// Сохрание отгрузок в БД
    /// (Предполагается, что таблица lsvDeliveries предварительно очищена)
    /// </summary>
    /// <param name="deliveries">Сохраняемые отгрузки</param>
    /// <param name="serverName">Имя сервера</param>
    /// <param name="dbName">Имя БД</param>
    /// <returns>0 - отгрузки сохранены; иначе - отгрузки не сохранены</returns>
    private static int SaveDeliveriesEx(CourierDeliveryInfo[] deliveries, string serverName, string dbName)
    {
        // 1. Инициализация
        int rc = 1;

        try
        {
            // 2. Проверяем исходные данные
            rc = 2;
            if (deliveries == null || deliveries.Length <= 0)
                return rc;

            using (SqlConnection connection = new SqlConnection($"Server={serverName};Database={dbName};Integrated Security=true"))
            {
                connection.Open();
                // 3. Строим таблицу lsvDeliveries, передаваемую на сервер
                rc = 3;
                using (DataTable table = new DataTable("lsvDeliveries"))
                {
                    table.Columns.Add("dlvID", typeof(int));
                    table.Columns.Add("dlvCourierID", typeof(int));
                    table.Columns.Add("dlvShopID", typeof(int));
                    table.Columns.Add("dlvCalculationTime", typeof(DateTime));
                    table.Columns.Add("dlvOrderCount", typeof(int));
                    table.Columns.Add("dlvOrderCost", typeof(double));
                    table.Columns.Add("dlvWeight", typeof(double));
                    table.Columns.Add("dlvDeliveryTime", typeof(double));
                    table.Columns.Add("dlvExecutionTime", typeof(double));
                    table.Columns.Add("dlvIsLoop", typeof(bool));
                    table.Columns.Add("dlvReserveTime", typeof(double));
                    table.Columns.Add("dlvStartDeliveryInterval", typeof(DateTime));
                    table.Columns.Add("dlvEndDeliveryInterval", typeof(DateTime));
                    table.Columns.Add("dlvCost", typeof(double));
                    table.Columns.Add("dlvStartDelivery", typeof(DateTime));
                    table.Columns.Add("dlvCompleted", typeof(bool));
                    table.Columns.Add("dlvPermutationKey", typeof(string));
                    table.Columns.Add("dlvPriority", typeof(int));
                    table.Columns.Add("dlvIsReceipted", typeof(bool));
                    StringBuilder sb = new StringBuilder(120);
                    string[] deliveryPermutationKey = new string[deliveries.Length];
                    int[] sortedOrders = new int[32];

                    for (int i = 0; i < deliveries.Length; i++)
                    {
                        CourierDeliveryInfo delivery = deliveries[i];
                        if (delivery.Orders == null || delivery.Orders.Length <= 0)
                            continue;

                        Order[] orders = delivery.Orders;
                        int orderCount = orders.Length;
                        for (int j = 0; j < orderCount; j++)
                        {
                            sortedOrders[j] = orders[j].Id;
                        }

                        Array.Sort(sortedOrders, 0, orderCount);

                        sb.Length = 0;
                        int priority = orders[0].Priority;
                        sb.Append(delivery.DeliveryCourier.VehicleID);
                        sb.Append('.');
                        sb.Append(sortedOrders[0]);
                        bool isReceipted = (orders[0].Status == OrderStatus.Receipted);

                        for (int j = 1; j < orderCount; j++)
                        {
                            Order order = orders[j];
                            if (order.Priority > priority)
                                priority = order.Priority;
                            sb.Append('.');
                            sb.Append(sortedOrders[j]);
                            if (order.Status == OrderStatus.Receipted)
                                isReceipted = true;
                        }

                        #if debug
                        if (sb.ToString() == "6.31238809.31239251.31239320.31241604.31260002")
                        {
                            Logger.WriteToLog(388, $"CalcThreadEx. thread = {i}. key = 6.31238809.31239251.31239320.31241604.31260002. index = {i}", 0);
                        }
                        #endif

                        table.Rows.Add(
                            i + 1,                       // dlvID
                            delivery.DeliveryCourier.Id, // dlvCourierID
                            delivery.FromShop.Id,        // dlvShopID
                            delivery.CalculationTime,    // dlvCalculationTime
                            delivery.OrderCount,         // dlvOrderCount
                            delivery.OrderCost,          // dlvOrderCost
                            delivery.Weight,
                            delivery.DeliveryTime,
                            delivery.ExecutionTime,
                            delivery.IsLoop,
                            delivery.ReserveTime.TotalMinutes,
                            delivery.StartDeliveryInterval,
                            delivery.EndDeliveryInterval,
                            delivery.Cost,
                            delivery.StartDeliveryInterval,
                            false,                       // dlvCompleted
                            sb.ToString(),               // dlvPermutationKey
                            priority,
                            isReceipted                  // dlvReceipted
                            );
                    }

                    // 4. Заполняем таблицу lsvDeliveries
                    rc = 4;
                    using (var copy = new SqlBulkCopy(connection, SqlBulkCopyOptions.KeepIdentity, null))
                    {
                        copy.DestinationTableName = "dbo.lsvDeliveries";
                        copy.ColumnMappings.Add("dlvID", "dlvID");
                        copy.ColumnMappings.Add("dlvCourierID", "dlvCourierID");
                        copy.ColumnMappings.Add("dlvShopID", "dlvShopID");
                        copy.ColumnMappings.Add("dlvCalculationTime", "dlvCalculationTime");
                        copy.ColumnMappings.Add("dlvOrderCount", "dlvOrderCount");
                        copy.ColumnMappings.Add("dlvOrderCost", "dlvOrderCost");
                        copy.ColumnMappings.Add("dlvWeight", "dlvWeight");
                        copy.ColumnMappings.Add("dlvDeliveryTime", "dlvDeliveryTime");
                        copy.ColumnMappings.Add("dlvExecutionTime", "dlvExecutionTime");
                        copy.ColumnMappings.Add("dlvIsLoop", "dlvIsLoop");
                        copy.ColumnMappings.Add("dlvReserveTime", "dlvReserveTime");
                        copy.ColumnMappings.Add("dlvStartDeliveryInterval", "dlvStartDeliveryInterval");
                        copy.ColumnMappings.Add("dlvEndDeliveryInterval", "dlvEndDeliveryInterval");
                        copy.ColumnMappings.Add("dlvCost", "dlvCost");
                        copy.ColumnMappings.Add("dlvStartDelivery", "dlvStartDelivery");
                        copy.ColumnMappings.Add("dlvCompleted", "dlvCompleted");
                        copy.ColumnMappings.Add("dlvPermutationKey", "dlvPermutationKey");
                        copy.ColumnMappings.Add("dlvPriority", "dlvPriority");
                        copy.ColumnMappings.Add("dlvIsReceipted", "dlvIsReceipted");
                        copy.WriteToServer(table);
                        copy.Close();
                    }
                }

                // 5. Строим таблицу lsvDeliveryOrders, передаваемую на сервер
                rc = 5;
                using (var copy = new SqlBulkCopy(connection))
                {
                    using (DataTable table = new DataTable("lsvDeliveryOrders"))
                    {
                        table.Columns.Add("dorDlvID", typeof(int));
                        table.Columns.Add("dorOrderID", typeof(int));

                        for (int i = 0; i < deliveries.Length; i++)
                        {
                            CourierDeliveryInfo delivery = deliveries[i];
                            Order[] orders = delivery.Orders;
                            if (orders == null || orders.Length <= 0)
                                continue;
                            int dorDlvID = i + 1;

                            for (int j = 0; j < orders.Length; j++)
                            {
                                table.Rows.Add(dorDlvID, orders[j].Id);
                            }
                        }

                        // 6. Заполняем таблицу lsvDeliveryOrders
                        rc = 6;

                        copy.DestinationTableName = "dbo.lsvDeliveryOrders";
                        copy.ColumnMappings.Add("dorDlvID", "dorDlvID");
                        copy.ColumnMappings.Add("dorOrderID", "dorOrderID");
                        copy.WriteToServer(table);
                    }

                    // 7. Строим таблицу lsvNodeDeliveryTime, передаваемую на сервер
                    rc = 7;
                    using (DataTable table = new DataTable("lsvNodeDeliveryTime"))
                    {
                        table.Columns.Add("ndtDlvID", typeof(int));
                        table.Columns.Add("ndtTime", typeof(double));

                        for (int i = 0; i < deliveries.Length; i++)
                        {
                            CourierDeliveryInfo delivery = deliveries[i];
                            double[] nodeDeliveryTime = delivery.NodeDeliveryTime;
                            if (nodeDeliveryTime == null || nodeDeliveryTime.Length <= 0)
                                continue;
                            int ndtDlvID = i + 1;

                            for (int j = 0; j < nodeDeliveryTime.Length; j++)
                            {
                                table.Rows.Add(ndtDlvID, nodeDeliveryTime[j]);
                            }
                        }

                        // 8. Заполняем таблицу lsvNodeDeliveryTime
                        rc = 8;
                        copy.DestinationTableName = "dbo.lsvNodeDeliveryTime";
                        copy.ColumnMappings.Clear();
                        copy.ColumnMappings.Add("ndtDlvID", "ndtDlvID");
                        copy.ColumnMappings.Add("ndtTime", "ndtTime");
                        copy.WriteToServer(table);
                    }

                    // 9. Строим таблицу lsvDeliveryNodeInfo, передаваемую на сервер
                    rc = 9;
                    using (DataTable table = new DataTable("lsvDeliveryNodeInfo"))
                    {
                        table.Columns.Add("dniDlvID", typeof(int));
                        table.Columns.Add("dniDistance", typeof(int));
                        table.Columns.Add("dniTime", typeof(int));

                        for (int i = 0; i < deliveries.Length; i++)
                        {
                            CourierDeliveryInfo delivery = deliveries[i];
                            Point[] nodeInfo = delivery.NodeInfo;
                            if (nodeInfo == null || nodeInfo.Length <= 0)
                                continue;

                            int dniDlvID = i + 1;

                            for (int j = 0; j < nodeInfo.Length; j++)
                            {
                                table.Rows.Add(dniDlvID, nodeInfo[j].X, nodeInfo[j].Y);
                            }
                        }

                        // 10. Заполняем таблицу lsvDeliveryNodeInfo
                        rc = 10;
                        copy.DestinationTableName = "dbo.lsvDeliveryNodeInfo";
                        copy.ColumnMappings.Clear();
                        copy.ColumnMappings.Add("dniDlvID", "dniDlvID");
                        copy.ColumnMappings.Add("dniDistance", "dniDistance");
                        copy.ColumnMappings.Add("dniTime", "dniTime");
                        copy.WriteToServer(table);
                    }
                }

                connection.Close();
            }

            // 13. Выход - OK
            rc = 0;
            return rc;
        }
        catch (Exception ex)
        {
            #if debug
                Logger.WriteToLog(700, $"SaveDeliveries rc = {rc} Exception = {ex.Message}", 2);
            #endif
            return rc;
        }
        //finally
        //{
        //    if (impersonatedIdentity != null)
        //    {
        //        #if debug
        //            Logger.WriteToLog(701, $"SaveDeliveries rc = {rc} Before Dispose()", 0);
        //        #endif

        //        //impersonatedIdentity.Dispose();

        //        #if debug
        //            Logger.WriteToLog(702, $"SaveDeliveries rc = {rc} After Dispose()", 0);
        //        #endif
        //    }
        //}
    }

    /// <summary>
    /// Получить имя сервера
    /// </summary>
    /// <param name="connection">Открытое соединение</param>
    /// <returns>Имя сервера или null</returns>
    private static string GetServerName(SqlConnection connection)
    {
        using (SqlCommand cmd = new SqlCommand(SELECT_SERVERNAME, connection))
        {
            return cmd.ExecuteScalar() as string;
        }
    }

    /// <summary>
    /// Создание отсортированных ключей всех отгрузок
    /// с числом заказов до 256 и глубиной до 8
    /// </summary>
    /// <param name="n">Чило заказов (1 ≤ n ≤ 256)</param>
    /// <param name="level">Максимальное число заказов в отгрузке (1 ≤ level ≤ 8)</param>
    /// <returns>Отсортированные ключи отгрузок или null</returns>
    private static long[] CreateDeliverySortedKeys(int n, int level)
    {
        // 1. Инициализация

        try
        {
            // 2. Проверяем исходные данные
            if (n <= 0 || n > 256)
                return null;
            if (level <= 0 || level > 8)
                return null;
            if (level > n)
                level = n;

            // 3. Подсчет общего числа ключей
            long mf = 1;
            long nmf = 1;
            long size = 0;

            for (int m = 1; m <= level; m++)
            {
                mf *= m;  // mf = m!
                nmf *= (n - m + 1);  // (n - m + 1) ... n
                size += (nmf / mf);
            }

            // 4. Цикл построения ключей
            long[] keys = new long[size];
            int count = 0;

            byte[] vec = new byte[8];
            byte[] vec0 = new byte[8] { 0, 0, 0, 0, 0, 0, 0, 0 };

            for (int i1 = 0; i1 < n; i1++)
            {
                Buffer.BlockCopy(vec0, 0, vec, 1, 7);
                vec[0] = (byte)i1;
                keys[count++] = BitConverter.ToInt64(vec, 0);

                if (level >= 2)
                {
                    for (int i2 = i1 + 1; i2 < n; i2++)
                    {
                        Buffer.BlockCopy(vec0, 0, vec, 2, 6);
                        vec[1] = (byte)i2;
                        keys[count++] = BitConverter.ToInt64(vec, 0);

                        if (level >= 3)
                        {
                            for (int i3 = i2 + 1; i3 < n; i3++)
                            {
                                Buffer.BlockCopy(vec0, 0, vec, 3, 5);
                                vec[2] = (byte)i3;
                                keys[count++] = BitConverter.ToInt64(vec, 0);

                                if (level >= 4)
                                {
                                    for (int i4 = i3 + 1; i4 < n; i4++)
                                    {
                                        Buffer.BlockCopy(vec0, 0, vec, 4, 4);
                                        vec[3] = (byte)i4;
                                        keys[count++] = BitConverter.ToInt64(vec, 0);

                                        if (level >= 5)
                                        {
                                            for (int i5 = i4 + 1; i5 < n; i5++)
                                            {
                                                Buffer.BlockCopy(vec0, 0, vec, 5, 3);
                                                vec[4] = (byte)i5;
                                                keys[count++] = BitConverter.ToInt64(vec, 0);

                                                if (level >= 6)
                                                {
                                                    for (int i6 = i5 + 1; i6 < n; i6++)
                                                    {
                                                        Buffer.BlockCopy(vec0, 0, vec, 6, 2);
                                                        vec[5] = (byte)i6;
                                                        keys[count++] = BitConverter.ToInt64(vec, 0);

                                                        if (level >= 7)
                                                        {
                                                            for (int i7 = i6 + 1; i7 < n; i7++)
                                                            {
                                                                vec[7] = 0;
                                                                vec[6] = (byte)i7;
                                                                keys[count++] = BitConverter.ToInt64(vec, 0);


                                                                if (level >= 8)
                                                                {
                                                                    for (int i8 = i7 + 1; i8 < n; i8++)
                                                                    {
                                                                        vec[7] = (byte)i8;
                                                                        keys[count++] = BitConverter.ToInt64(vec, 0);
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

            // 5. Сортировка ключей
            Array.Sort(keys);

            // 6. Выход - Ok
            return keys;
        }
        catch
        {
            return null;
        }
    }
}
