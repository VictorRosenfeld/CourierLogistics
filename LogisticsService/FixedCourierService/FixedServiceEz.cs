
namespace LogisticsService.FixedCourierService
{
    using log4net.Appender;
    using LogisticsService.API;
    using LogisticsService.Couriers;
    using LogisticsService.FixedCourierService.ServiceQueue;
    using LogisticsService.Geo;
    using LogisticsService.Log;
    using LogisticsService.Orders;
    using LogisticsService.SalesmanTravelingProblem;
    using LogisticsService.ServiceParameters;
    using LogisticsService.Shops;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Drawing;
    using System.IO;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Timers;
    using static LogisticsService.API.GetOrderEvents;
    using static LogisticsService.API.GetShopEvents;

    /// <summary>
    /// Сервис с фиксированными курьерами
    /// </summary>
    public class FixedServiceEz : IDisposable
    {
        /// <summary>
        /// Параметры конфигурации
        /// </summary>
        private ServiceConfig config;

        /// <summary>
        /// Параметры конфигурации
        /// </summary>
        public ServiceConfig Config => config;

        /// <summary>
        /// Путь к файлу лога
        /// </summary>
        //public string LogFileName => Path.ChangeExtension(Process.GetCurrentProcess().MainModule.FileName, "log");
        public string LogFileName => Logger.File;

        /// <summary>
        /// Менеджер расстояний и времени движения
        /// между точками разными способами
        /// </summary>
        private GeoCache geoCache;

        /// <summary>
        /// Все курьеры
        /// </summary>
        private AllCouriers allCouriers;

        /// <summary>
        /// Все заказы
        /// </summary>
        private AllOrdersEx allOrders;

        /// <summary>
        /// Все магазины
        /// </summary>
        private AllShopsEx allShops;

        /// <summary>
        /// Флаг: true - экземпляр создан; false - экземпляр не создан
        /// </summary>
        public bool IsCreated { get; private set; }

        /// <summary>
        /// Мьютекс для синхронизации получения
        /// новых данных и их обработки
        /// </summary>
        private Mutex syncMutex = new Mutex();

        /// <summary>
        /// Таймер для запроса данных с сервера
        /// </summary>
        private System.Timers.Timer requestTimer;

        /// <summary>
        /// Таймер для отработки событий в очереди
        /// </summary>
        private System.Timers.Timer queueTimer;

        /// <summary>
        /// Таймер для информирования сервера о состоянии очереди
        /// </summary>
        private System.Timers.Timer queueInfoTimer;

        /// <summary>
        /// Таймер очереди для предотвращения утечек
        /// </summary>
        private System.Timers.Timer checkingQueueTimer; 

        /// <summary>
        /// Очередь событий на отгрузку
        /// </summary>
        private EventQueue queue;

        /// <summary>
        /// Очередь для предотвращения утечек
        /// </summary>
        private CheckingQueue checkingQueue;

        /// <summary>
        /// Решение задачи комивояжера
        /// </summary>
        //private SalesmanSolutionEx salesmanSolution;
        private SalesmanSolutionEy salesmanSolution;

        /// <summary>
        /// Создание сервиса
        /// </summary>
        /// <param name="jsonFilename"></param>
        /// <returns></returns>
        public int Create(string jsonFilename)
        {
            // 1. Инициализация
            int rc = 1;
            IsCreated = false;
            config = null;
            geoCache = null;
            allCouriers = null;
            allOrders = null;
            allShops = null;
            DisposeTimer(requestTimer);
            DisposeTimer(queueTimer);
            DisposeTimer(queueInfoTimer);
            DisposeTimer(checkingQueueTimer);
            requestTimer = null;
            queueTimer = null;
            queueInfoTimer = null;
            checkingQueueTimer = null;
            queue = null;
            checkingQueue = null;
            salesmanSolution = null;
            FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);
            //Helper.WriteInfoToLog($"---> FixedServiceEy. Ver {fileVersionInfo.ProductVersion}");


            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (string.IsNullOrWhiteSpace(jsonFilename))
                    return rc;

                // 3. Загружаем Config
                rc = 3;
                config = ServiceConfig.Deserialize(jsonFilename);
                if (config == null)
                    return rc;

                Logger.Create(config.logger.LogFile, config.logger.SavedDays);
                Logger.WriteToLog(string.Format(MessagePatterns.START_SERVICE, "FixedServiceEz", fileVersionInfo.ProductVersion));

                RequestParameters.API_ROOT = config.functional_parameters.api_root;
                RequestParameters.SERVICE_ID = config.functional_parameters.service_id;

                // 4. Создаём Geo cache
                rc = 4;
                int capacity = config.functional_parameters.geo_cache_capacity;
                if (capacity <= 10000)
                    capacity = 10000;
                geoCache = new GeoCache(config, capacity);

                #region For Debug Only

                //double[] latitude = new double[]  { 53.196871, 53.18774, 53.224951598824, 53.189827799216, 53.205591699608 };
                //double[] longitude = new double[] { 45.005578, 44.97662, 44.9953245,      44.9854697,      44.9902701 };

                //int rcGeo = geoCache.PutLocationInfo(latitude, longitude, CourierVehicleType.YandexTaxi);

                //Point[,] dataTable;
                //rcGeo = geoCache.GetPointsDataTable(latitude, longitude, CourierVehicleType.YandexTaxi, out dataTable);

                #endregion For Debug Only

                // 5. Создаём All couriers
                rc = 5;
                allCouriers = new AllCouriers();
                int rc1 = allCouriers.Create(config);
                if (rc1 != 0)
                    return rc = 100 * rc + rc1;

                // 6. Создаём All orders
                rc = 6;
                allOrders = new AllOrdersEx();
                //rc1 = allOrders.Create();
                rc1 = allOrders.CreateEx(config);
                if (rc1 != 0)
                    return rc = 100 * rc + rc1;

                // 7. Создаём All shops
                rc = 7;
                allShops = new AllShopsEx();
                rc1 = allShops.Create();
                if (rc1 != 0)
                    return rc = 100 * rc + rc1;

                //salesmanSolution = new SalesmanSolutionEx(geoCache, config.functional_parameters.salesman_problem_levels);
                salesmanSolution = new SalesmanSolutionEy(geoCache, config.functional_parameters.salesman_problem_levels);

                //salesmanSolution.CheckPath(allCouriers.Couriers[4]);

                // 8. Создаём таймер для запроса новых данных
                rc = 8;
                requestTimer = new System.Timers.Timer();
                requestTimer.Elapsed += RequestTimer_Elapsed;
                requestTimer.AutoReset = true;
                requestTimer.Interval = config.functional_parameters.data_request_interval;
                requestTimer.Enabled = false;

                // 9. Создаём таймер для обработки очереди
                rc = 9;
                queueTimer = new System.Timers.Timer();
                queueTimer.Elapsed += QueueTimer_Elapsed;
                queueTimer.AutoReset = false;
                queueTimer.Interval = 10000;
                queueTimer.Enabled = false;


                // 10. Создаём таймер для информирования сервера о состоянии очереди
                rc = 10;
                queueInfoTimer = new System.Timers.Timer();
                queueInfoTimer.Elapsed += QueueInfoTimer_Elapsed;
                queueInfoTimer.AutoReset = true;
                queueInfoTimer.Interval = config.functional_parameters.queue_Info_interval;
                queueInfoTimer.Enabled = false;

                // 11. Создаём таймер очереди для предотвращения утечек
                rc = 11;
                checkingQueueTimer = new System.Timers.Timer();
                checkingQueueTimer.Elapsed += CheckingQueueTimer_Elapsed;
                checkingQueueTimer.AutoReset = false;
                checkingQueueTimer.Interval = 10000;
                checkingQueueTimer.Enabled = false;

                checkingQueue = new CheckingQueue(50000);

                // 12. Выход - Ok
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
        /// Событие таймера очереди предотвращения отгрузок
        /// </summary>
        /// <param name="sender">Таймер</param>
        /// <param name="e">Аргументы события</param>
        private void CheckingQueueTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            // 1. Инициализация
            bool isCatched = false;
            if (!IsCreated)
                return;
            if (checkingQueue == null)
                return;

            try
            {
                // 2. Захватываем mutex
                checkingQueueTimer.Enabled = false;
                isCatched = syncMutex.WaitOne(300000);
                //Helper.WriteInfoToLog($"CheckingQueueTimer_Elapsed CheckingItemCount = {checkingQueue.Count}");
                //Logger.WriteToLog(string.Format(MessagePatterns.CHECKING_QUEUE_INFO_TIMER_ELAPSED, checkingQueue.Count));

                // 3. Запускаем обработчик очереди
                CheckingQueueHandler(checkingQueue, queue);

                // 4. Переустанавливаем таймер
                DateTime timerOn = checkingQueue.GetFirstEventTime();
                if (timerOn == DateTime.MaxValue)
                    return;
                DateTime now = DateTime.Now;
                double interval = 500;

                if (timerOn > now)
                {
                    interval = (timerOn - now).TotalMilliseconds;
                    if (interval < 500)
                        interval = 500;
                }

                checkingQueueTimer.Interval = interval;
                checkingQueueTimer.Start();
            }
            catch
            { }
            finally
            {
                // 5. Освобождаем mutex
                if (isCatched)
                    syncMutex.ReleaseMutex();
            }
        }

        /// <summary>
        /// Событие таймера информирования сервера о состоянии очереди
        /// </summary>
        /// <param name="sender">Таймер</param>
        /// <param name="e">Аргументы события</param>
        private void QueueInfoTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            // 1. Инициализация
            bool isCatched = false;
            if (!IsCreated)
                return;
            EventQueue currentQueue = queue;
            if (currentQueue == null)
                return;

            try
            {
                isCatched = syncMutex.WaitOne(400000);
                //Helper.WriteInfoToLog($"QueueInfoTimer_Elapsed ItemCount = {currentQueue.Count}");
                Logger.WriteToLog(string.Format(MessagePatterns.QUEUE_INFO_TIMER_ELAPSED, currentQueue.Count));
                
                QueueInfoToServer(currentQueue);
            }
            catch
            { }
            finally
            {
                if (isCatched)
                    syncMutex.ReleaseMutex();
            }
        }

        /// <summary>
        /// Запуск сервиса
        /// </summary>
        /// <returns></returns>
        public int Start()
        {
            // 1. Инициализация
            int rc = 1;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (!IsCreated)
                    return rc;

                // 3. Обновляем данные
                rc = 3;
                queueInfoTimer.Start();
                Task.Run(() => Refresh(1));

                requestTimer.Start();

                // 4.Выход - Ok
                rc = 0;
                return rc;
            }
            catch
            {
                return rc;
            }
        }

        /// <summary>
        /// Остановить выполнение
        /// </summary>
        public void Stop()
        {
            try
            {
                // 2. Проверяем исходные данные
                if (!IsCreated)
                    return;

                // 3. Удаляем очередь
                queue = null;

                // 4. Останавливаем таймеры
                try
                { requestTimer.Stop(); }
                catch { }
                try
                { queueTimer.Stop(); }
                catch { }
                try
                { queueInfoTimer.Stop(); }
                catch { }
                try
                { checkingQueueTimer.Stop(); }
                catch { }
            }
            catch
            { }
        }

        /// <summary>
        /// Событие таймера запроса данных
        /// </summary>
        /// <param name="sender">Таймер</param>
        /// <param name="e">Аргументы события</param>
        private void RequestTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            // 1. Инициализация
            bool isCatched = false;
            if (!IsCreated)
                return;

            try
            {
                isCatched = syncMutex.WaitOne(config.functional_parameters.data_request_interval);
                Refresh(0);
                //Helper.WriteInfoToLog($"GeoCache capacity {geoCache.HashCapacity}. ItemCount = {geoCache.CacheItemCount}");
                Logger.WriteToLog(string.Format(MessagePatterns.GEO_CACHE_INFO, geoCache.HashCapacity, geoCache.CacheItemCount));
            }
            catch
            { }
            finally
            {
                if (isCatched)
                    syncMutex.ReleaseMutex();
            }
        }

        /// <summary>
        /// Событие таймера - обработка очереди на отгрузку
        /// </summary>
        /// <param name="sender">Таймер</param>
        /// <param name="e">Аргументы события</param>
        private void QueueTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            // 1. Инициализация
            bool isCatched = false;
            if (!IsCreated)
                return;
            EventQueue currentQueue = queue;
            if (currentQueue == null)
                return;

            try
            {
                queueTimer.Enabled = false;
                isCatched = syncMutex.WaitOne(70000);
                currentQueue = queue;

                //Helper.WriteInfoToLog($"QueueTimer_Elapsed ItemCount = {currentQueue.Count}");
                Logger.WriteToLog(string.Format(MessagePatterns.QUEUE_TIMER_ELAPSED, currentQueue.Count));
                QueueHandler(currentQueue);
                QueueItem item = currentQueue.GetCurrentItem();
                if (item != null)
                {
                    DateTime nextTime = item.EventTime;
                    DateTime currentTime = DateTime.Now;
                    double interval;
                    if (nextTime <= currentTime)
                    {
                        interval = 500;
                    }
                    else
                    {
                        interval = (nextTime - currentTime).TotalMilliseconds;
                        if (interval < 500)
                            interval = 500;
                    }

                    queueTimer.Interval = interval;
                    queueTimer.Start();
                }
            }
            catch
            { }
            finally
            {
                if (isCatched)
                    syncMutex.ReleaseMutex();
            }
        }

        /// <summary>
        /// Ликвидация таймера
        /// </summary>
        /// <param name="timer">Таймер</param>
        private static void DisposeTimer(System.Timers.Timer timer)
        {
            try
            {
                if (timer != null)
                {
                    timer.Dispose();
                }
            }
            catch
            { }
        }

        /// <summary>
        /// Ликвидация Mutex
        /// </summary>
        /// <param name="mutex">Mutex</param>
        private static void DisposeMutex(Mutex mutex)
        {
            try
            {
                if (mutex != null)
                {
                    mutex.Dispose();
                }
            }
            catch
            { }

        }

        #region Refresh

        /// <summary>
        /// Запрос данных с сервера
        /// </summary>
        /// <param name="requestType"></param>
        /// <returns></returns>
        private int Refresh(int requestType)
        {
            // 1. Инициализация
            int rc = 1;
            bool isCatched = false;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (!IsCreated)
                    return rc;

                // 3. Захватываем Mutex
                rc = 3;
                isCatched = syncMutex.WaitOne(config.functional_parameters.data_request_interval);
                if (!isCatched)
                    return rc;

                // 4. Первым делом отрабатываем очередь отгрузок
                rc = 4;
                EventQueue currentQueue = queue;
                Task<int> queueHandlerTask = Task.Run(() => QueueHandler(currentQueue));

                // 5. Считываем данные о курьрах
                rc = 4;
                CourierEvent[] courierEvents;
                int rcCourierEvents = GetCourierEvents.GetEvents(requestType, out courierEvents);

                // 6. Считываем данные о магазинах
                rc = 6;
                ShopEvent[] shopEvents;
                int rcShopEvents = GetShopEvents.GetEvents(requestType, out shopEvents);

                // 7. Считываем данные о заказах
                rc = 7;
                OrderEvent[] orderEvents;
                int rcOrderEvents = GetOrderEvents.GetEvents(requestType, out orderEvents);

                // 8. Дожидаемся завершения обработки очереди отгрузок
                rc = 8;
                queueHandlerTask.Wait(30000);

                // 9. Отрабатываем считанные данные
                rc = 9;
                if (rcCourierEvents == 0)
                    rcCourierEvents = allCouriers.Refresh(courierEvents);

                if (rcShopEvents == 0)
                    rcShopEvents = allShops.Refresh(shopEvents);

                if (rcOrderEvents == 0)
                    rcOrderEvents = allOrders.Refresh(orderEvents);

                // 9.1 Добавляем элементы в очередь предотвращения отгрузок
                rc = 91;
                if (orderEvents != null && orderEvents.Length > 0)
                {
                    int rcx = AddCheckingInfo(orderEvents);
                }

                // 10. Если новых событий для пересчета нет
                rc = 10;
                if ((courierEvents == null || courierEvents.Length <= 0) &&
                    (orderEvents == null || orderEvents.Length <= 0))
                    return rc = 0;

                // 11. Строим список магазинов требующих пересчета
                rc = 11;
                Dictionary<int, Shop> recalcShops = new Dictionary<int, Shop>(allShops.Count);

                if (courierEvents != null && courierEvents.Length > 0)
                {
                    foreach (CourierEvent courierEvent in courierEvents)
                    {
                        if (!recalcShops.ContainsKey(courierEvent.shop_id))
                        {
                            Shop shop;
                            if (allShops.Shops.TryGetValue(courierEvent.shop_id, out shop))
                                recalcShops.Add(shop.Id, shop);
                        }
                    }
                }

                if (orderEvents != null && orderEvents.Length > 0)
                {
                    foreach (OrderEvent orderEvent in orderEvents)
                    {
                        if (!recalcShops.ContainsKey(orderEvent.shop_id))
                        {
                            Shop shop;
                            if (allShops.Shops.TryGetValue(orderEvent.shop_id, out shop))
                                recalcShops.Add(shop.Id, shop);
                        }
                    }
                }

                if (recalcShops.Count <= 0)
                    return rc = 0;

                // 12. Пересчет отгрузок магазинов
                rc = 12;
                Shop[] shops = new Shop[recalcShops.Count];
                recalcShops.Values.CopyTo(shops, 0);
                recalcShops = null;
                int rc1 = RecalcDelivery(shops);
                if (rc1 != 0)
                    return rc = 1000 * rc + rc1;

                // 13. Выход - Ok
                rc = 0;
                return rc;
            }
            catch
            {
                return rc;
            }
            finally
            {
                if (isCatched)
                    syncMutex.ReleaseMutex();
            }
        }

        ///// <summary>
        ///// Пересчет отгрузок во всех магазинах
        ///// </summary>
        ///// <returns></returns>
        //private int RecalcDelivery()
        //{
        //    // 1. Инициализация
        //    int rc = 1;
        //    EventQueue deliveryQueue = null;

        //    try
        //    {
        //        // 2. Проверяем исходные данные
        //        rc = 2;
        //        if (!IsCreated)
        //            return rc;

        //        queueTimer.Enabled = false;

        //        // 3. Производим обработку для каждого магазина отдельно
        //        rc = 3;
        //        QueueItem[] allQueueItems = new QueueItem[3000];
        //        int itemCount = 0;

        //        foreach (Shop shop in allShops.Shops.Values)
        //        {
        //            // 3.1 Выбираем заказы магазина
        //            rc = 31;
        //            Order[] shopOrders = allOrders.GetShopOrders(shop.Id);
        //            if (shopOrders == null || shopOrders.Length <= 0)
        //                continue;

        //            // 3.2. Выбираем курьров магазина
        //            rc = 32;
        //            Courier[] shopCouriers = allCouriers.GetShopCouriers(shop.Id);

        //            // 3.3 Строим отгрузки для магазина
        //            rc = 33;
        //            CourierDeliveryInfo[] assembledOrders;
        //            CourierDeliveryInfo[] receiptedOrders;
        //            Order[] undeliveredOrders;
        //            int rc1 = CreateShopDeliveries(shop, shopOrders, shopCouriers, out assembledOrders, out receiptedOrders, out undeliveredOrders);
        //            if (rc1 != 0)
        //                continue;

        //            // 3.4 Отправляем отгрузки с не собранными товарами
        //            rc = 34;
        //            if (receiptedOrders != null && receiptedOrders.Length > 0)
        //            {
        //                rc1 = SendReceiptedOrders(receiptedOrders);
        //            }

        //            // 3.5 Обрабатываем отгрузки, состоящие целиком из собранных заказов
        //            rc = 35;
        //            if (assembledOrders != null && assembledOrders.Length > 0)
        //            {
        //                QueueItem[] deliveryQueueItems;
        //                rc1 = SendAssembledOrders(assembledOrders, out deliveryQueueItems);
        //                if (deliveryQueueItems != null && deliveryQueueItems.Length > 0)
        //                {
        //                    if (itemCount + deliveryQueueItems.Length >= allQueueItems.Length)
        //                    {
        //                        Array.Resize(ref allQueueItems, allQueueItems.Length + 100 * deliveryQueueItems.Length);
        //                    }

        //                    deliveryQueueItems.CopyTo(allQueueItems, itemCount);
        //                    itemCount += deliveryQueueItems.Length;
        //                }
        //            }

        //            // 3.6 Отправляем информацию о заказах, которые не могут быть доставлены в срок
        //            rc = 36;
        //            if (undeliveredOrders != null && undeliveredOrders.Length > 0)
        //            {
        //                //for (int k = 0; k < undeliveredOrders.Length; k++)
        //                //{
        //                //    if (undeliveredOrders[k].DeliveryTimeTo > DateTime.Now)
        //                //    {
        //                //        if (undeliveredOrders[k].Id != 8656859)
        //                //            k = k;
        //                //    }
        //                //}

        //                //rc1 = SendUndeliveryOrders(undeliveredOrders);
        //                rc1 = SendUndeliveryOrdersEx(undeliveredOrders);
        //            }
        //        }

        //        // 4. Создаём очередь на отгррузки и запускаем таймер очереди
        //        rc = 4;
        //        if (itemCount > 0)
        //        {
        //            Array.Resize(ref allQueueItems, itemCount);
        //            deliveryQueue = new EventQueue();
        //            Helper.WriteInfoToLog($"Создание очереди отгрузок ({itemCount})");
        //            int rc1 = deliveryQueue.Create(allQueueItems);
        //            if (rc1 == 0)
        //            {
        //                QueueItem queueItem = deliveryQueue.GetCurrentItem();
        //                if (queueItem != null)
        //                {
        //                    DateTime currentTime = DateTime.Now;
        //                    double interval = 500;
        //                    if (currentTime < queueItem.EventTime)
        //                    {
        //                        interval = (queueItem.EventTime - currentTime).TotalMilliseconds;
        //                        if (interval < 500)
        //                            interval = 500;
        //                    }

        //                    queueTimer.Interval = interval;
        //                    queueTimer.Start();
        //                }
        //            }
        //        }

        //        // 5. Выход - Ok
        //        rc = 0;
        //        return rc;
        //    }
        //    catch
        //    {
        //        return rc;
        //    }
        //    finally
        //    {
        //        queue = deliveryQueue;
        //    }
        //}

        /// <summary>
        ///  Пересчет отгрузок в заданных магазинах
        /// </summary>
        /// <param name="shops">Магазины, в которых производится пересчет</param>
        /// <returns></returns>
        private int RecalcDelivery(Shop[] shops)
        {
            // 1. Инициализация
            int rc = 1;
            EventQueue deliveryQueue = null;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (!IsCreated)
                    return rc;
                if (shops == null || shops.Length <= 0)
                    return rc;

                queueTimer.Enabled = false;

                // 3. Производим обработку для каждого магазина отдельно
                rc = 3;
                QueueItem[] allQueueItems = new QueueItem[3000];
                int itemCount = 0;

                foreach (Shop shop in shops)
                {
                    // 3.1 Выбираем заказы магазина
                    rc = 31;
                    Order[] shopOrders = allOrders.GetShopOrders(shop.Id);
                    if (shopOrders == null || shopOrders.Length <= 0)
                        continue;

                    // 3.2. Выбираем курьров магазина
                    rc = 32;
                    //Courier[] shopCouriers = allCouriers.GetShopCouriers(shop.Id);
                    Courier[] shopCouriers = allCouriers.GetShopCouriers(shop.Id, false);

                    // 3.3 Строим отгрузки для магазина
                    rc = 33;
                    CourierDeliveryInfo[] assembledOrders;
                    CourierDeliveryInfo[] receiptedOrders;
                    Order[] undeliveredOrders;
                    Order[] tabuOrders;

                    int rc1 = CreateShopDeliveries(shop, shopOrders, shopCouriers, out assembledOrders, out receiptedOrders, out undeliveredOrders, out tabuOrders);
                    if (rc1 != 0)
                        continue;

                    // 3.4 Отправляем отгрузки с не собранными товарами
                    rc = 34;
                    if (receiptedOrders != null && receiptedOrders.Length > 0)
                    {
                        rc1 = SendReceiptedOrders(receiptedOrders);
                    }

                    // 3.5 Обрабатываем отгрузки, состоящие целиком из собранных заказов
                    rc = 35;
                    if (assembledOrders != null && assembledOrders.Length > 0)
                    {
                        QueueItem[] deliveryQueueItems;
                        rc1 = SendAssembledOrders(assembledOrders, out deliveryQueueItems);
                        if (deliveryQueueItems != null && deliveryQueueItems.Length > 0)
                        {
                            if (itemCount + deliveryQueueItems.Length >= allQueueItems.Length)
                            {
                                Array.Resize(ref allQueueItems, allQueueItems.Length + 100 * deliveryQueueItems.Length);
                            }

                            deliveryQueueItems.CopyTo(allQueueItems, itemCount);
                            itemCount += deliveryQueueItems.Length;
                        }
                    }

                    // 3.6 Отправляем информацию о заказах, которые не могут быть доставлены в срок
                    rc = 36;
                    if (undeliveredOrders != null && undeliveredOrders.Length > 0)
                    {
                        //for (int k = 0; k < undeliveredOrders.Length; k++)
                        //{
                        //    if (undeliveredOrders[k].DeliveryTimeTo > DateTime.Now)
                        //    {
                        //        if (undeliveredOrders[k].Id != 8656859)
                        //            k = k;
                        //    }
                        //}

                        //rc1 = SendUndeliveryOrders(undeliveredOrders);
                        //rc1 = SendUndeliveryOrdersEx(undeliveredOrders);
                        rc1 = SendUndeliveryOrdersEy(undeliveredOrders);
                    }
                }

                // 4. Сливаем построенные элементы с существующими элементами для других магазинов
                rc = 4;
                if (queue != null && queue.Count > 0 && queue.IsCreated)
                {
                    // 4.1 Выбираем ID-магазинов
                    rc = 41;
                    int[] shopId = new int[shops.Length];
                    for (int i = 0; i < shops.Length; i++)
                    {
                        shopId[i] = shops[i].Id;
                    }

                    // 4.2 Сортируем ID магазинов
                    rc = 42;
                    Array.Sort(shopId);

                    // 4.3. Добавляем существующие элементы других магазинов
                    rc = 43;
                    for (int i = queue.ItemIndex; i < queue.Count; i++)
                    {
                        QueueItem queueItem = queue.Items[i];
                        if (queueItem.Delivery != null && queueItem.Delivery.FromShop != null)
                        {
                            if (Array.BinarySearch(shopId, queueItem.Delivery.FromShop.Id) < 0)
                            {
                                if (itemCount >= allQueueItems.Length)
                                {
                                    Array.Resize(ref allQueueItems, allQueueItems.Length + queue.Count - queue.ItemIndex);
                                }

                                allQueueItems[itemCount++] = queueItem;
                            }
                        }
                    }
                }

                // 5. Создаём очередь на отгррузки и запускаем таймер очереди
                rc = 5;
                if (itemCount > 0)
                {
                    Array.Resize(ref allQueueItems, itemCount);
                    deliveryQueue = new EventQueue();
                    //Helper.WriteInfoToLog($"Создание очереди отгрузок ({itemCount})");
                    Logger.WriteToLog(string.Format(MessagePatterns.CREATE_SHIPMENT_QUEUE, itemCount));

                    int rc1 = deliveryQueue.Create(allQueueItems);
                    if (rc1 == 0)
                    {
                        QueueItem queueItem = deliveryQueue.GetCurrentItem();
                        if (queueItem != null)
                        {
                            DateTime currentTime = DateTime.Now;
                            double interval = 500;
                            if (currentTime < queueItem.EventTime)
                            {
                                interval = (queueItem.EventTime - currentTime).TotalMilliseconds;
                                if (interval < 500)
                                    interval = 500;
                            }

                            queueTimer.Interval = interval;
                            queueTimer.Start();
                        }
                    }

                    PrintQueue(deliveryQueue);
                }

                // 5. Выход - Ok
                rc = 0;
                return rc;
            }
            catch
            {
                return rc;
            }
            finally
            {
                queue = deliveryQueue;
            }
        }

        /// <summary>
        /// Создание отгрузок заказов магазина
        /// </summary>
        /// <param name="shop">Магазин</param>
        /// <param name="shopOrders">Отгружаемые заказы</param>
        /// <param name="shopCouriers">Доступные курьеры и такси для доставки заказов</param>
        /// <param name="assembledOrders">Отгрузки покрывающие все собранные заказы</param>
        /// <param name="receiptedOrders">Отгрузки, в которые могут попасть поступившие, но не собранные заказы</param>
        /// <param name="undeliveredOrders">Заказы, которые не могут быть доставлены в срок</param>
        /// <returns></returns>
        private int CreateShopDeliveries(Shop shop, Order[] shopOrders, Courier[] shopCouriers, out CourierDeliveryInfo[] assembledOrders, out CourierDeliveryInfo[] receiptedOrders, out Order[] undeliveredOrders, out Order[] tabuOrder)
        {
            // 1. Инициализация
            int rc = 1;
            assembledOrders = null;
            receiptedOrders = null;
            undeliveredOrders = null;
            tabuOrder = null;

            try
            {
                //return salesmanSolution.CreateShopDeliveries(shop, shopOrders, shopCouriers, out assembledOrders, out receiptedOrders, out undeliveredOrders);
                //return salesmanSolution.CreateShopDeliveriesEx(shop, shopOrders, shopCouriers, out assembledOrders, out receiptedOrders, out undeliveredOrders);
                return salesmanSolution.CreateShopDeliveries(shop, shopOrders, shopCouriers, out assembledOrders, out receiptedOrders, out undeliveredOrders, out tabuOrder);
            }
            catch
            {
                return rc;
            }
        }

        /// <summary>
        /// Передачу серверу возможных отгрузок с
        /// поступившими, но не собранными заказами
        /// </summary>
        /// <param name="receiptedOrders">Отгрузки с поступившими, но не собранными заказами</param>
        /// <returns>0 - отгрузки переданы серверу; иначе - отгрузки серверу не переданы</returns>
        private int SendReceiptedOrders(CourierDeliveryInfo[] receiptedOrders)
        {
            // 1. Инициализация
            int rc = 1;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (receiptedOrders == null || receiptedOrders.Length <= 0)
                    return rc;

                // 3. Строим данные для отправки на сервер
                rc = 3;
                BeginShipment.Shipment[] shipments = new BeginShipment.Shipment[receiptedOrders.Length];

                for (int i = 0; i < receiptedOrders.Length; i++)
                {
                    // 3.1 Извлекаем отгрузку
                    rc = 31;
                    CourierDeliveryInfo delivery = receiptedOrders[i];

                    // 3.2 Добавляем отгрузку в данные ззапроса на сервер
                    rc = 32;
                    BeginShipment.Shipment shipment = new BeginShipment.Shipment();
                    shipment.id = Guid.NewGuid().ToString();
                    shipment.status = 0;
                    shipment.shop_id = delivery.FromShop.Id;
                    shipment.courier_id = delivery.DeliveryCourier.Id;
                    shipment.date_target_end = delivery.EndDeliveryInterval;
                    shipment.info = CreateDeliveryInfo(delivery);

                    // date_target
                    if (!delivery.DeliveryCourier.IsTaxi && delivery.OrderCost <= delivery.DeliveryCourier.AverageOrderCost)
                    {
                        shipment.date_target = delivery.StartDelivery;
                    }
                    else if (config.functional_parameters.shipment_trigger && 
                        delivery.OrderCount >= delivery.DeliveryCourier.CourierType.MaxOrderCount)
                    {
                        shipment.date_target = delivery.StartDeliveryInterval;
                    }
                    else
                    {
                        DateTime targetTime;
                        if (delivery.DeliveryCourier.IsTaxi)
                        {
                            targetTime = delivery.EndDeliveryInterval.AddMinutes(-config.functional_parameters.taxi_alert_interval);
                        }
                        else
                        {
                            targetTime = delivery.EndDeliveryInterval.AddMinutes(-config.functional_parameters.courier_alert_interval);
                        }

                        if (targetTime < delivery.StartDeliveryInterval)
                            targetTime = delivery.StartDeliveryInterval;
                        shipment.date_target = targetTime;
                    }

                    // delivery_service_id
                    shipment.delivery_service_id = delivery.DeliveryCourier.CourierType.DServiceId;

                    //switch (delivery.DeliveryCourier.CourierType.VechicleType)
                    //{
                    //    case CourierVehicleType.YandexTaxi:
                    //        shipment.delivery_service_id = 14;
                    //        break;
                    //    case CourierVehicleType.GettTaxi:
                    //        shipment.delivery_service_id = 12;
                    //        break;
                    //    default:
                    //        shipment.delivery_service_id = 4;
                    //        break;
                    //}

                    // orders
                    int[] orderId = new int[delivery.OrderCount];
                    for (int j = 0; j < orderId.Length; j++)
                    {
                        orderId[j] = delivery.Orders[j].Id;
                    }

                    shipment.orders = orderId;

                    shipments[i] = shipment;
                }

                // 4. Отправляем запрос на сервер
                rc = 4;
                int rc1 = BeginShipment.Begin(shipments);
                if (rc1 != 0)
                    return rc = 1000 * rc + rc1;

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
        /// Передача на сервер отгрузок с собранными товарами
        /// и построение элементов очереди для отложенных отгрузок
        /// </summary>
        /// <param name="assembledOrders">Отгрузки с целиком собранными заказами</param>
        /// <param name="deliveryQueueItems">Отгрузки с целиком собранными заказами</param>
        /// <returns>0 - отгрузки переданы серверу; иначе - отгрузки серверу не переданы</returns>
        private int SendAssembledOrders(CourierDeliveryInfo[] assembledOrders, out QueueItem[] deliveryQueueItems)
        {
            // 1. Инициализация
            int rc = 1;
            deliveryQueueItems = null;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (assembledOrders == null || assembledOrders.Length <= 0)
                    return rc;

                // 3. Строим данные для отправки на сервер
                rc = 3;
                BeginShipment.Shipment[] shipments = new BeginShipment.Shipment[assembledOrders.Length];
                int shipmentCount = 0;

                CourierDeliveryInfo[] sentOrders = new CourierDeliveryInfo[assembledOrders.Length];
                int sentCount = 0;

                QueueItem[] queueItems = new QueueItem[assembledOrders.Length];
                int itemCount = 0;

                for (int i = 0; i < assembledOrders.Length; i++)
                {
                    // 3.1 Извлекаем отгрузку
                    rc = 31;
                    CourierDeliveryInfo delivery = assembledOrders[i];
                    if (delivery.IsCompleted())
                        continue;

                    // 3.2 Проверяем, что отгрузить нужно прямо сейчас
                    rc = 32;
                    DateTime eventTime;
                    if (config.functional_parameters.shipment_trigger && 
                        delivery.OrderCount >= delivery.DeliveryCourier.CourierType.MaxOrderCount)
                    {
                        eventTime = delivery.StartDeliveryInterval;
                    }
                    else if (!delivery.DeliveryCourier.IsTaxi && delivery.OrderCost <= delivery.DeliveryCourier.AverageOrderCost)
                    {
                        eventTime = delivery.StartDeliveryInterval;
                    }
                    else if (delivery.DeliveryCourier.IsTaxi)
                    {
                        eventTime = delivery.EndDeliveryInterval.AddMinutes(-config.functional_parameters.taxi_alert_interval);
                    }
                    else
                    {
                        eventTime = delivery.EndDeliveryInterval.AddMinutes(-config.functional_parameters.courier_alert_interval);
                    }

                    if (eventTime < delivery.StartDeliveryInterval)
                        eventTime = delivery.StartDeliveryInterval;
                    DateTime currentTime = DateTime.Now;

                    //if ((eventTime <= currentTime) ||
                    //    (!delivery.DeliveryCourier.IsTaxi && delivery.OrderCost <= delivery.DeliveryCourier.AverageOrderCost && delivery.StartDeliveryInterval <= currentTime))
                    ////(delivery.OrderCost <= delivery.DeliveryCourier.AverageOrderCost && delivery.StartDeliveryInterval >= currentTime))
                    if (eventTime <= currentTime)
                    {
                        // 3.3 Построение данных для отгрузки прямо сейчас
                        rc = 33;
                        sentOrders[sentCount++] = delivery;
                        BeginShipment.Shipment shipment = new BeginShipment.Shipment();
                        shipment.id = Guid.NewGuid().ToString();
                        shipment.status = 1;
                        shipment.shop_id = delivery.FromShop.Id;
                        shipment.courier_id = delivery.DeliveryCourier.Id;
                        shipment.date_target = delivery.StartDeliveryInterval;
                        shipment.date_target_end = delivery.EndDeliveryInterval;
                        shipment.info = CreateDeliveryInfo(delivery);
                        shipment.delivery_service_id = delivery.DeliveryCourier.CourierType.DServiceId;

                        //switch (delivery.DeliveryCourier.CourierType.VechicleType)
                        //{
                        //    case CourierVehicleType.YandexTaxi:
                        //        shipment.delivery_service_id = 14;
                        //        break;
                        //    case CourierVehicleType.GettTaxi:
                        //        shipment.delivery_service_id = 12;
                        //        break;
                        //    default:
                        //        shipment.delivery_service_id = 4;
                        //        break;
                        //}

                        // orders
                        int[] orderId = new int[delivery.OrderCount];
                        for (int j = 0; j < orderId.Length; j++)
                        {
                            orderId[j] = delivery.Orders[j].Id;
                        }

                        shipment.orders = orderId;
                        shipments[shipmentCount++] = shipment;
                    }
                    else
                    {
                        queueItems[itemCount++] = new QueueItem(eventTime, QueueItemType.CourierDeliveryAlert, delivery);
                    }
                }

                Array.Resize(ref queueItems, itemCount);
                deliveryQueueItems = queueItems;

                // 4. Отправляем запрос на сервер
                rc = 4;
                if (shipmentCount > 0)
                {
                    Array.Resize(ref shipments, shipmentCount);
                    int rc1 = BeginShipment.Begin(shipments);
                    if (rc1 != 0)
                        return rc = 1000 * rc + rc1;

                    // 5. Помечаем заказы, как отгруженные в отгрузках отправленных на сервер
                    rc = 5;
                    for (int i = 0; i < sentCount; i++)
                    {
                        sentOrders[i].SetCompleted(true);
                    }
                }

                // 6. Выход - Ok
                rc = 0;
                return rc;
            }
            catch
            {
                return rc;
            }
        }

        ///// <summary>
        ///// Передачу серверу заказов которые не могут быть отгружены в срок
        ///// </summary>
        ///// <param name="undeliveredOrders">Заказы, которые не могут быть отгружены в срок</param>
        ///// <returns>0 - отгрузки переданы серверу; иначе - отгрузки серверу не переданы</returns>
        //private int SendUndeliveryOrders(Order[] undeliveredOrders)
        //{
        //    // 1. Инициализация
        //    int rc = 1;

        //    try
        //    {
        //        // 2. Проверяем исходные данные
        //        rc = 2;
        //        if (undeliveredOrders == null || undeliveredOrders.Length <= 0)
        //            return rc;

        //        int shopId = undeliveredOrders[0].ShopId;

        //        // 3. Строим данные для отправки на сервер
        //        rc = 3;
        //        BeginShipment.Shipment shipment = new BeginShipment.Shipment();
        //        shipment.id = Guid.NewGuid().ToString();
        //        shipment.status = 2;
        //        shipment.shop_id = shopId;
        //        shipment.courier_id = 0;
        //        shipment.date_target = DateTime.Now;
        //        shipment.date_target_end = shipment.date_target;
        //        shipment.delivery_service_id = 0;
        //        //shipment.info = new BeginShipment.DeliveryInfo();

        //        // orders
        //        int[] orderId = new int[undeliveredOrders.Length];
        //        for (int j = 0; j < orderId.Length; j++)
        //        {
        //            orderId[j] = undeliveredOrders[j].Id;
        //        }

        //        shipment.orders = orderId;

        //        // 4. Отправляем запрос на сервер
        //        rc = 4;
        //        int rc1 = BeginShipment.Begin(shipment);
        //        if (rc1 != 0)
        //            return rc = 1000 * rc + rc1;

        //        // 5. Выход - Ok
        //        rc = 0;
        //        return rc;
        //    }
        //    catch
        //    {
        //        return rc;
        //    }
        //}

        ///// <summary>
        ///// Передачу серверу заказов которые не могут быть отгружены в срок
        ///// с расширенной информацией
        ///// </summary>
        ///// <param name="undeliveredOrders">Заказы, которые не могут быть отгружены в срок</param>
        ///// <returns>0 - отгрузки переданы серверу; иначе - отгрузки серверу не переданы</returns>
        //private int SendUndeliveryOrdersEx(Order[] undeliveredOrders)
        //{
        //    // 1. Инициализация
        //    int rc = 1;

        //    try
        //    {
        //        // 2. Проверяем исходные данные
        //        rc = 2;
        //        if (undeliveredOrders == null || undeliveredOrders.Length <= 0)
        //            return rc;

        //        int shopId = undeliveredOrders[0].ShopId;

        //        // 3. Строим данные для отправки на сервер
        //        rc = 3;
        //        BeginShipment.RejectedOrder[] rejectedOrders = new BeginShipment.RejectedOrder[undeliveredOrders.Length];

        //        for (int i = 0; i < undeliveredOrders.Length; i++)
        //        {
        //            // 3.0 Извлекаем заказ
        //            Order order = undeliveredOrders[i];

        //            // 3.1 Общая информация о заказе
        //            BeginShipment.RejectedOrder rejectedOrder = new BeginShipment.RejectedOrder();
        //            rejectedOrder.id = Guid.NewGuid().ToString();
        //            rejectedOrder.status = 2;
        //            rejectedOrder.shop_id = shopId;
        //            rejectedOrder.courier_id = 0;
        //            rejectedOrder.date_target = DateTime.Now;
        //            rejectedOrder.date_target_end = rejectedOrder.date_target;
        //            rejectedOrder.delivery_service_id = 0;
        //            rejectedOrder.orders = new int[] { order.Id };

        //            // 3.2 Информация о времени доставки всеми возможными способами
        //            Point[] delivTime = GetDeliveryTimeForOrder(order);
        //            if (delivTime != null && delivTime.Length > 0)
        //            {
        //                BeginShipment.RejectedInfo info = new BeginShipment.RejectedInfo();
        //                info.calculationTime = rejectedOrder.date_target;
        //                info.weight = order.Weight;
        //                BeginShipment.RejectedInfoItem[] items = new BeginShipment.RejectedInfoItem[delivTime.Length];
        //                for (int j = 0; j < delivTime.Length; j++)
        //                {
        //                    BeginShipment.RejectedInfoItem item = new BeginShipment.RejectedInfoItem();
        //                    item.delivery_type = delivTime[j].X;
        //                    item.delivery_time = delivTime[j].Y;
        //                    items[j] = item;
        //                }

        //                info.delivery_method = items;
        //                rejectedOrder.info = info;
        //            }

        //            rejectedOrders[i] = rejectedOrder;
        //        }

        //        // 4. Отправляем запрос на сервер
        //        rc = 4;
        //        int rc1 = BeginShipment.Reject(rejectedOrders);
        //        if (rc1 != 0)
        //            return rc = 1000 * rc + rc1;

        //        // 5. Выход - Ok
        //        rc = 0;
        //        return rc;
        //    }
        //    catch
        //    {
        //        return rc;
        //    }
        //}

        /// <summary>
        /// Передачу серверу заказов которые не могут быть отгружены в срок
        /// с расширенной информацией
        /// </summary>
        /// <param name="undeliveredOrders">Заказы, которые не могут быть отгружены в срок</param>
        /// <returns>0 - отгрузки переданы серверу; иначе - отгрузки серверу не переданы</returns>
        private int SendUndeliveryOrdersEy(Order[] undeliveredOrders)
        {
            // 1. Инициализация
            int rc = 1;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (undeliveredOrders == null || undeliveredOrders.Length <= 0)
                    return rc;

                int shopId = undeliveredOrders[0].ShopId;

                // 3. Строим данные для отправки на сервер
                rc = 3;
                BeginShipment.RejectedOrder[] rejectedOrders = new BeginShipment.RejectedOrder[undeliveredOrders.Length];

                for (int i = 0; i < undeliveredOrders.Length; i++)
                {
                    // 3.0 Извлекаем заказ
                    Order order = undeliveredOrders[i];

                    // 3.1 Общая информация о заказе
                    BeginShipment.RejectedOrder rejectedOrder = new BeginShipment.RejectedOrder();
                    rejectedOrder.id = Guid.NewGuid().ToString();
                    rejectedOrder.status = (order.Status == OrderStatus.Assembled ? 2 : 3);
                    rejectedOrder.shop_id = shopId;
                    rejectedOrder.courier_id = 0;
                    rejectedOrder.date_target = DateTime.Now;
                    rejectedOrder.date_target_end = rejectedOrder.date_target;
                    rejectedOrder.delivery_service_id = 0;
                    rejectedOrder.orders = new int[] { order.Id };

                    // 3.2 Информация о времени доставки всеми возможными способами
                    BeginShipment.RejectedInfoItem[] items = GetUndeliveredInfoForOrder(order);
                    if (items != null && items.Length > 0)
                    {
                        BeginShipment.RejectedInfo info = new BeginShipment.RejectedInfo();
                        info.calculationTime = rejectedOrder.date_target;
                        info.weight = order.Weight;

                        info.delivery_method = items;
                        rejectedOrder.info = info;
                    }

                    rejectedOrders[i] = rejectedOrder;
                }

                // 4. Отправляем запрос на сервер
                rc = 4;
                int rc1 = BeginShipment.Reject(rejectedOrders);
                if (rc1 != 0)
                    return rc = 1000 * rc + rc1;

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
        /// Подсчитать время на доставку заказа
        /// в единичной отгрузке для разных способов отгрузки
        /// </summary>
        /// <param name="order">Заказ</param>
        /// <returns>Массив пар = (тип, время) для всех доступных способов отгрузки или null</returns>
        private Point[] GetDeliveryTimeForOrder(Order order)
        {
            // 1. Инициализация

            try
            {
                // 2. Проверяем исходные данные
                if (geoCache == null)
                    if (order == null)
                        return null;
                //if (order.EnabledTypes == EnabledCourierType.Unknown)
                //    return null;

                // 3. Извлекаем магазин
                Shop shop = allShops.GetShop(order.ShopId);
                if (shop == null)
                    return null;

                // 4. Выбираем доступные способы отгрузки
                //CourierVehicleType[] courierTypes = new CourierVehicleType[8];
                //int typeCount = 0;
                //if ((order.EnabledTypes & EnabledCourierType.Car) != 0)
                //    courierTypes[typeCount++] = CourierVehicleType.Car;
                //if ((order.EnabledTypes & EnabledCourierType.Bicycle) != 0)
                //    courierTypes[typeCount++] = CourierVehicleType.Bicycle;
                //if ((order.EnabledTypes & EnabledCourierType.OnFoot) != 0)
                //    courierTypes[typeCount++] = CourierVehicleType.OnFoot;
                //if ((order.EnabledTypes & EnabledCourierType.YandexTaxi) != 0)
                //    courierTypes[typeCount++] = CourierVehicleType.YandexTaxi;
                //if ((order.EnabledTypes & EnabledCourierType.GettTaxi) != 0)
                //    courierTypes[typeCount++] = CourierVehicleType.GettTaxi;

                //if (typeCount <= 0)
                //    return null;

                CourierVehicleType[] courierTypes = order.EnabledTypesEx;
                int typeCount = (courierTypes == null ? 0 : courierTypes.Length);

                if (typeCount <= 0)
                    return null;

                // 5. Подсчет времени для разных типов курьеров
                Point[] result = new Point[typeCount];
                int count = 0;
                double[] latitude = new double[2];
                double[] longitude = new double[2];
                latitude[0] = shop.Latitude;
                longitude[0] = shop.Longitude;
                latitude[1] = order.Latitude;
                longitude[1] = order.Longitude;
                Point[,] dataTable;

                for (int i = 0; i < typeCount; i++)
                {
                    // 5.1 Извлекаем курьера нужного типа
                    Courier courier = allCouriers.FindFirstByType(courierTypes[i]);

                    if (courier != null)
                    {
                        // 5.2. Вычисляем время доставки
                        int rc1 = geoCache.PutLocationInfo(latitude, longitude, courierTypes[i]);
                        if (rc1 == 0)
                        {
                            rc1 = geoCache.GetPointsDataTable(latitude, longitude, courierTypes[i], out dataTable);
                            if (rc1 == 0)
                            {
                                double deliveryTime;
                                double executionTime;
                                double cost;

                                rc1 = courier.CourierType.GetTimeAndCost(dataTable[0, 1], order.Weight, out deliveryTime, out executionTime, out cost);
                                if (rc1 == 0)
                                {
                                    result[count].X = (int)courierTypes[i];
                                    result[count++].Y = (int)(60.0 * deliveryTime + 0.5);
                                }
                            }
                        }
                    }
                }

                if (count < result.Length)
                {
                    Array.Resize(ref result, count);
                }

                // 6. Выход
                return result;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Подсчитать время на доставку заказа
        /// в единичной отгрузке для разных способов отгрузки
        /// </summary>
        /// <param name="order">Заказ</param>
        /// <returns>Массив пар = (тип, время) для всех доступных способов отгрузки или null</returns>
        private BeginShipment.RejectedInfoItem[] GetUndeliveredInfoForOrder(Order order)
        {
            // 1. Инициализация

            try
            {
                // 2. Проверяем исходные данные
                if (geoCache == null)
                    return null;
                if (order == null)
                    return null;
                //if (order.EnabledTypes == EnabledCourierType.Unknown)
                //    return null;

                // 3. Извлекаем магазин
                Shop shop = allShops.GetShop(order.ShopId);
                if (shop == null)
                    return null;

                // 4. Выбираем доступные способы отгрузки
                //CourierVehicleType[] courierTypes = new CourierVehicleType[8];
                //int typeCount = 0;
                //if ((order.EnabledTypes & EnabledCourierType.Car) != 0)
                //    courierTypes[typeCount++] = CourierVehicleType.Car;
                //if ((order.EnabledTypes & EnabledCourierType.Bicycle) != 0)
                //    courierTypes[typeCount++] = CourierVehicleType.Bicycle;
                //if ((order.EnabledTypes & EnabledCourierType.OnFoot) != 0)
                //    courierTypes[typeCount++] = CourierVehicleType.OnFoot;
                //if ((order.EnabledTypes & EnabledCourierType.YandexTaxi) != 0)
                //    courierTypes[typeCount++] = CourierVehicleType.YandexTaxi;
                //if ((order.EnabledTypes & EnabledCourierType.GettTaxi) != 0)
                //    courierTypes[typeCount++] = CourierVehicleType.GettTaxi;

                //if (typeCount <= 0)
                //    return null;

                CourierVehicleType[] courierTypes = order.EnabledTypesEx;
                int typeCount = (courierTypes == null ? 0 : courierTypes.Length);

                if (typeCount <= 0)
                    return null;

                // 5. Подсчет времени для разных типов курьеров
                BeginShipment.RejectedInfoItem[] result = new BeginShipment.RejectedInfoItem[typeCount];
                int count = 0;
                double[] latitude = new double[2];
                double[] longitude = new double[2];
                latitude[0] = shop.Latitude;
                longitude[0] = shop.Longitude;
                latitude[1] = order.Latitude;
                longitude[1] = order.Longitude;
                Point[,] dataTable;

                for (int i = 0; i < typeCount; i++)
                {
                    // 5.1 Извлекаем курьера нужного типа
                    Courier courier = allCouriers.FindFirstByType(courierTypes[i]);

                    if (courier != null)
                    {
                        // 5.2. Вычисляем время доставки
                        int rc1 = geoCache.PutLocationInfo(latitude, longitude, courierTypes[i]);
                        if (rc1 == 0)
                        {
                            rc1 = geoCache.GetPointsDataTable(latitude, longitude, courierTypes[i], out dataTable);
                            if (rc1 == 0)
                            {
                                double deliveryTime;
                                double executionTime;
                                double cost;

                                rc1 = courier.CourierType.GetTimeAndCost(dataTable[0, 1], order.Weight, out deliveryTime, out executionTime, out cost);
                                if (rc1 == 0)
                                {
                                    BeginShipment.RejectedInfoItem infoItem = new BeginShipment.RejectedInfoItem();
                                    infoItem.delivery_type = (int)courierTypes[i];
                                    infoItem.delivery_time = (int)(60.0 * deliveryTime + 0.5);
                                    if (order.RejectionReason != OrderRejectionReason.None)
                                    {
                                        infoItem.rejection_reason = (int)order.RejectionReason;
                                    }
                                    else
                                    {
                                        if (order.Weight > courier.CourierType.MaxWeight)
                                        {
                                            infoItem.rejection_reason = (int)OrderRejectionReason.Overweight;
                                        }
                                        else if (dataTable[0, 1].X > 1000 * courier.CourierType.MaxDistance)
                                        {
                                            infoItem.rejection_reason = (int)OrderRejectionReason.Overdistance;
                                        }
                                        else if (order.Status == OrderStatus.Assembled)
                                        {
                                            if (order.AssembledDate.AddMinutes(deliveryTime) > order.DeliveryTimeTo)
                                            {
                                                infoItem.rejection_reason = (int)OrderRejectionReason.LateAssembled;
                                            }
                                            else if (DateTime.Now.AddMinutes(deliveryTime) > order.DeliveryTimeTo)
                                            {
                                                infoItem.rejection_reason = (int)OrderRejectionReason.LateStart;
                                            }
                                        }
                                        else if (order.Status == OrderStatus.Receipted)
                                        {
                                            if (order.ReceiptedDate.AddMinutes(deliveryTime) > order.DeliveryTimeTo)
                                            {
                                                infoItem.rejection_reason = (int)OrderRejectionReason.ToTimeIsSmall;
                                            }
                                            else if (DateTime.Now.AddMinutes(deliveryTime) > order.DeliveryTimeTo)
                                            {
                                                infoItem.rejection_reason = (int)OrderRejectionReason.LateStart;
                                            }
                                        }
                                    }

                                    result[count++] = infoItem;
                                }
                            }
                        }
                    }
                }

                if (count < result.Length)
                {
                    Array.Resize(ref result, count);
                }

                // 6. Выход
                return result;
            }
            catch
            {
                return null;
            }
        }

        #endregion Refresh

        #region QueueHandler

        /// <summary>
        /// Обработка очереди событий
        /// </summary>
        /// <param name="queue">Очередь</param>
        /// <returns>0 - очередь обработана; иначе - очередь не обработана</returns>
        private int QueueHandler(EventQueue queue)
        {
            // 1. Иницализация
            int rc = 1;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (!IsCreated)
                    return rc;
                if (queue == null)
                    return rc;

                // 3. Извлекаем текущий элемент очереди
                rc = 3;
                QueueItem item = queue.GetCurrentItem();
                if (item == null)
                    return rc;

                DateTime toTime = DateTime.Now.AddMilliseconds(config.functional_parameters.event_time_interval);
                //if (toTime < item.EventTime)
                //    toTime = item.EventTime.AddMilliseconds(config.functional_parameters.event_time_interval);

                // 4. Извлекаем все события, очередь которых наступила
                rc = 4;
                QueueItem[] activeEvents = queue.MoveToTime(toTime);
                if (activeEvents == null || activeEvents.Length <= 0)
                    return rc = 0;

                // 5. Обрабатываем выбранные события
                rc = 5;
                BeginShipment.Shipment[] shipments = new BeginShipment.Shipment[activeEvents.Length];
                int count = 0;

                for (int i = 0; i < activeEvents.Length; i++)
                {
                    // 5.1 Извлекаем отгрузку
                    rc = 51;
                    QueueItem activeEvent = activeEvents[i];
                    if (activeEvent == null || activeEvent.Delivery == null)
                        continue;

                    CourierDeliveryInfo delivery = activeEvent.Delivery;
                    if (delivery.IsCompleted())
                        continue;

                    // 5.2 Строим данные для запроса
                    rc = 52;
                    BeginShipment.Shipment shipment = new BeginShipment.Shipment();
                    shipment.id = Guid.NewGuid().ToString();
                    shipment.status = 1;
                    shipment.shop_id = delivery.FromShop.Id;
                    shipment.courier_id = delivery.DeliveryCourier.Id;
                    //shipment.date_target = delivery.StartDeliveryInterval;
                    shipment.date_target = activeEvent.EventTime;
                    shipment.date_target_end = delivery.EndDeliveryInterval;
                    shipment.info = CreateDeliveryInfo(delivery);

                    // delivery_service_id
                    shipment.delivery_service_id = delivery.DeliveryCourier.CourierType.DServiceId;
                    //switch (delivery.DeliveryCourier.CourierType.VechicleType)
                    //{
                    //    case CourierVehicleType.YandexTaxi:
                    //        shipment.delivery_service_id = 14;
                    //        break;
                    //    case CourierVehicleType.GettTaxi:
                    //        shipment.delivery_service_id = 12;
                    //        break;
                    //    default:
                    //        shipment.delivery_service_id = 4;
                    //        break;
                    //}

                    // orders
                    int[] orderId = new int[delivery.OrderCount];
                    for (int j = 0; j < orderId.Length; j++)
                    {
                        orderId[j] = delivery.Orders[j].Id;
                    }

                    shipment.orders = orderId;
                    shipments[count++] = shipment;
                }

                Array.Resize(ref shipments, count);

                // 6. Отправляем запрос на сервер
                rc = 6;
                if (count > 0)
                {
                    int rc1 = BeginShipment.Begin(shipments);
                    if (rc1 != 0)
                        return rc = 1000 * rc + rc1;

                    // 7. Помечаем заказы, как отгруженные
                    rc = 7;

                    for (int i = 0; i < activeEvents.Length; i++)
                    {
                        QueueItem activeEvent = activeEvents[i];
                        if (activeEvent != null && activeEvent.Delivery != null)
                        {
                            activeEvent.Delivery.SetCompleted(true);
                        }
                    }
                }

                // 7. Выход - Ok
                rc = 0;
                return rc;
            }
            catch
            {
                return rc;
            }
        }

        #endregion QueueHandler

        #region CheckingQueueHandler

        /// <summary>
        /// Обработчик очереди предотвращения утечек
        /// </summary>
        /// <param name="checkingQueue">Очередь предотвращения утечек</param>
        /// <param name="eventQueue">Очередь отгрузок</param>
        /// <returns>0 - обрабочик отраработал успешно; иначе - обработчик не отработал успешно</returns>
        private static int CheckingQueueHandler(CheckingQueue checkingQueue, EventQueue eventQueue)
        {
            // 1. Инициализация
            int rc = 1;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (checkingQueue == null || checkingQueue.Count <= 0)
                    return rc;

                // 3. Запрашиваем все события требующие обработки
                rc = 3;
                DateTime toTime = DateTime.Now.AddSeconds(30);
                QueueItem[] items = checkingQueue.MoveToTime(toTime);
                if (items == null || items.Length <= 0)
                    return rc;

                // 4. Обрабатываем все выбранные события
                rc = 4;
                CourierDeliveryInfo checkingDelivery;
                CourierDeliveryInfo[] sendDelivery = new CourierDeliveryInfo[items.Length];
                int sendDeliveryCount = 0;
                CourierDeliveryInfo[] rejectDelivery = new CourierDeliveryInfo[items.Length];
                int rejectDeliveryCount = 0;

                for (int i = 0; i < items.Length; i++)
                {
                    // 4.1 Извлекаем активный элемент
                    rc = 41;
                    QueueItem checkingItem = items[i];
                    if (checkingItem == null || checkingItem.ItemType != QueueItemType.CheckingAlert)
                        continue;

                    // 4.2 Помечаем его как отработанный
                    rc = 42;
                    checkingItem.ItemType = QueueItemType.None;

                    // 4.3 Проверяем наличие отгрузки
                    rc = 43;
                    if ((checkingDelivery = checkingItem.Delivery) == null)
                        continue;

                    // 4.4 Проверяем состояние заказа
                    rc = 44;
                    if (checkingDelivery.OrderCount <= 0)
                        continue;

                    Order order = checkingDelivery.Orders[0];
                    if (order == null)
                        continue;
                    if (order.Completed)
                        continue;

                    if (order.Status != OrderStatus.Assembled)
                        continue;

                    // 4.5 Находим элемент очереди на отгрузку, содержащий данный заказ
                    rc = 45;
                    QueueItem deliveryItem = null;
                    if (eventQueue != null)
                        deliveryItem = eventQueue.FindItemByOrderId(order.Id, checkingDelivery.EndDeliveryInterval);

                    // 4.6 Производим обработку
                    rc = 46;
                    Courier courier = checkingDelivery.DeliveryCourier;

                    if (courier.IsTaxi)
                    {
                        if (deliveryItem == null)
                        {
                            // checking-taxi, delivery-null
                            sendDelivery[sendDeliveryCount++] = checkingDelivery;
                        }
                        else if (deliveryItem.Delivery.DeliveryCourier.IsTaxi)
                        {  }
                        else
                        {
                            // checking-taxi, delivery-courier  (если не грузим такси, то курьера нужно приберечь)
                            if (deliveryItem.Delivery.DeliveryCourier.Status != CourierStatus.Ready)
                            {
                                sendDelivery[sendDeliveryCount++] = checkingDelivery;
                            }
                            else if ((deliveryItem.Delivery.EndDeliveryInterval - DateTime.Now).TotalMinutes <= 3)
                            {
                                sendDelivery[sendDeliveryCount++] = deliveryItem.Delivery;
                            }
                        }                          
                    }
                    else
                    {
                        if (deliveryItem == null)
                        {
                            // checking-courier, delivery-null (нет подходящего курьера для отгрузки)
                            rejectDelivery[rejectDeliveryCount++] = checkingDelivery;

                        }
                        else if (deliveryItem.Delivery.DeliveryCourier.IsTaxi)
                        {  }
                        else
                        {
                            // checking-courier, delivery-courier  (курьера нужно приберечь)
                            if (deliveryItem.Delivery.DeliveryCourier.Status != CourierStatus.Ready)
                            {
                                sendDelivery[sendDeliveryCount++] = checkingDelivery;
                            }
                            else if ((deliveryItem.Delivery.EndDeliveryInterval - DateTime.Now).TotalMinutes <= 3)
                            {
                                sendDelivery[sendDeliveryCount++] = deliveryItem.Delivery;
                            }
                        }
                    }
                }

                // 5. Отправляем команды об отгрузке
                rc = 5;
                if (sendDeliveryCount > 0)
                {
                    if (sendDeliveryCount < sendDelivery.Length)
                    {
                        Array.Resize(ref sendDelivery, sendDeliveryCount);
                    }

                    //Helper.WriteInfoToLog("(((( Send delivery command from checking queue");
                    Logger.WriteToLog(MessagePatterns.SHIPMENT_FROM_CHECKING_QUEUE1);
                    SendDeliveryCommand(sendDelivery);
                    //Helper.WriteInfoToLog(")))) Send delivery command from checking queue");
                    Logger.WriteToLog(MessagePatterns.SHIPMENT_FROM_CHECKING_QUEUE2);
                }

                // 6. Отправляем команды об отмене
                rc = 6;
                if (rejectDeliveryCount > 0)
                {
                    if (rejectDeliveryCount < rejectDelivery.Length)
                    {
                        Array.Resize(ref rejectDelivery, rejectDeliveryCount);
                    }

                    //Helper.WriteInfoToLog("(((( Send reject command from checking queue");
                    Logger.WriteToLog(MessagePatterns.REJECT_ORDER_FROM_CHECKING_QUEUE1);
                    SendRejectCommand(rejectDelivery);
                    //Helper.WriteInfoToLog(")))) Send reject command from checking queue");
                    Logger.WriteToLog(MessagePatterns.REJECT_ORDER_FROM_CHECKING_QUEUE2);
                }

                //// 6. Удаляем отработанные элементы
                //rc = 6;
                //checkingQueue.DeleteNotActive();

                // 7. Выход - Ok
                rc = 0;
                return rc;
            }
            catch
            {
                return rc;
            }
            finally
            {
                if (rc > 3) checkingQueue.DeleteNotActive();
            }
        }

        /// <summary>
        /// Передача на сервер отгрузок с собранными товарами
        /// </summary>
        /// <param name="checkingOrders">Отгрузки с целиком собранными заказами</param>
        /// <returns>0 - отгрузки переданы серверу; иначе - отгрузки серверу не переданы</returns>
        private static int SendDeliveryCommand(CourierDeliveryInfo[] checkingOrders)
        {
            // 1. Инициализация
            int rc = 1;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (checkingOrders == null || checkingOrders.Length <= 0)
                    return rc;

                // 3. Строим данные для отправки на сервер
                rc = 3;
                BeginShipment.Shipment[] shipments = new BeginShipment.Shipment[checkingOrders.Length];
                int shipmentCount = 0;

                CourierDeliveryInfo[] sentOrders = new CourierDeliveryInfo[checkingOrders.Length];
                int sentCount = 0;

                for (int i = 0; i < checkingOrders.Length; i++)
                {
                    // 3.1 Извлекаем отгрузку
                    rc = 31;
                    CourierDeliveryInfo delivery = checkingOrders[i];
                    if (delivery.IsCompleted())
                        continue;

                    // 3.3 Построение данных для отгрузки прямо сейчас
                    rc = 33;
                    sentOrders[sentCount++] = delivery;
                    BeginShipment.Shipment shipment = new BeginShipment.Shipment();
                    shipment.id = Guid.NewGuid().ToString();
                    shipment.status = 1;
                    shipment.shop_id = delivery.FromShop.Id;
                    shipment.courier_id = delivery.DeliveryCourier.Id;
                    shipment.date_target = delivery.StartDeliveryInterval;
                    shipment.date_target_end = delivery.EndDeliveryInterval;
                    shipment.info = CreateDeliveryInfo(delivery);

                    shipment.delivery_service_id = delivery.DeliveryCourier.CourierType.DServiceId;
                    //switch (.VechicleType)
                    //{
                    //    case CourierVehicleType.YandexTaxi:
                    //        shipment.delivery_service_id = 14;
                    //        break;
                    //    case CourierVehicleType.GettTaxi:
                    //        shipment.delivery_service_id = 12;
                    //        break;
                    //    default:
                    //        shipment.delivery_service_id = 4;
                    //        break;
                    //}

                    // orders
                    int[] orderId = new int[delivery.OrderCount];
                    for (int j = 0; j < orderId.Length; j++)
                    {
                        orderId[j] = delivery.Orders[j].Id;
                    }

                    shipment.orders = orderId;
                    shipments[shipmentCount++] = shipment;
                }

                // 4. Отправляем запрос на сервер
                rc = 4;
                if (shipmentCount > 0)
                {
                    Array.Resize(ref shipments, shipmentCount);
                    int rc1 = BeginShipment.Begin(shipments);
                    if (rc1 != 0)
                        return rc = 1000 * rc + rc1;

                    // 5. Помечаем заказы, как отгруженные в отгрузках отправленных на сервер
                    rc = 5;
                    for (int i = 0; i < sentCount; i++)
                    {
                        sentOrders[i].SetCompleted(true);
                    }
                }

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
        /// Передача на сервер отгрузок с собранными товарами
        /// </summary>
        /// <param name="rejectedOrders">Отвергаемые заказы</param>
        /// <returns>0 - отгрузки переданы серверу; иначе - отгрузки серверу не переданы</returns>
        private static int SendRejectCommand(CourierDeliveryInfo[] rejectedOrders)
        {
            // 1. Инициализация
            int rc = 1;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (rejectedOrders == null || rejectedOrders.Length <= 0)
                    return rc;

                // 3. Строим данные для отправки на сервер
                rc = 3;
                BeginShipment.RejectedOrder[] commands = new BeginShipment.RejectedOrder[rejectedOrders.Length];

                for (int i = 0; i < rejectedOrders.Length; i++)
                {
                    // 3.1 Извлекаем отгрузку
                    rc = 31;
                    CourierDeliveryInfo delivery = rejectedOrders[i];
                    if (delivery.IsCompleted())
                        continue;
                    Order order = delivery.Orders[0];
                    if (!(order.Status == OrderStatus.Assembled || order.Status == OrderStatus.Receipted))
                        continue;

                    // 3.3 Построение данных для отгрузки прямо сейчас
                    rc = 33;
                    BeginShipment.RejectedOrder rejectedOrder = new BeginShipment.RejectedOrder();

                    rejectedOrder.id = Guid.NewGuid().ToString();
                    rejectedOrder.status = (order.Status == OrderStatus.Assembled ? 2 : 3);
                    rejectedOrder.shop_id = delivery.FromShop.Id;
                    rejectedOrder.courier_id = delivery.DeliveryCourier.Id;
                    rejectedOrder.date_target = delivery.StartDeliveryInterval;
                    rejectedOrder.date_target_end = delivery.EndDeliveryInterval;
                    rejectedOrder.orders = new int[] { order.Id };

                    rejectedOrder.delivery_service_id = delivery.DeliveryCourier.CourierType.DServiceId;
                    //switch (delivery.DeliveryCourier.CourierType.VechicleType)
                    //{
                    //    case CourierVehicleType.YandexTaxi:
                    //        rejectedOrder.delivery_service_id = 14;

                    //        break;
                    //    case CourierVehicleType.GettTaxi:
                    //        rejectedOrder.delivery_service_id = 12;
                    //        break;
                    //    default:
                    //        rejectedOrder.delivery_service_id = 4;
                    //        break;
                    //}

                    BeginShipment.RejectedInfo info = new BeginShipment.RejectedInfo();
                    info.calculationTime = rejectedOrder.date_target;
                    info.weight = order.Weight;

                    BeginShipment.RejectedInfoItem infoItem = new BeginShipment.RejectedInfoItem();
                    infoItem.rejection_reason = (int)OrderRejectionReason.CourierNa;
                    infoItem.delivery_time = (int)(60 * delivery.DeliveryTime + 0.5);
                    infoItem.delivery_type = (int)delivery.DeliveryCourier.CourierType.VechicleType;
                    info.delivery_method = new BeginShipment.RejectedInfoItem[] { infoItem };
                    rejectedOrder.info = info;

                    commands[i] = rejectedOrder;
                }

                // 4. Отправляем запрос на сервер
                rc = 4;
                int rc1 = BeginShipment.Reject(commands);
                if (rc1 != 0)
                    return rc = 1000 * rc + rc1;

                // 5. Выход - Ok
                rc = 0;
                return rc;
            }
            catch
            {
                return rc;
            }
        }

        #endregion CheckingQueueHandler

        /// <summary>
        /// Выборка информации об отгрузке для отправки на сервер
        /// </summary>
        /// <param name="delivery">Отгрузка</param>
        /// <returns>Данные об отгрузке для сервера</returns>
        private static BeginShipment.DeliveryInfo CreateDeliveryInfo(CourierDeliveryInfo delivery)
        {
            // 1. Инициализация

            try
            {
                // 2. Проверяем исходные данные
                if (delivery == null)
                    return null;

                // 3. Инициализируем DeliveryInfo
                BeginShipment.DeliveryInfo deliveryInfo = new BeginShipment.DeliveryInfo();
                deliveryInfo.calculationTime = delivery.CalculationTime;
                deliveryInfo.delivery_time = delivery.DeliveryTime;
                deliveryInfo.end_delivery_interval = delivery.EndDeliveryInterval;
                deliveryInfo.execution_time = delivery.ExecutionTime;
                deliveryInfo.is_loop = delivery.IsLoop;

                if (delivery.NodeInfo != null && delivery.NodeInfo.Length > 0)
                {
                    BeginShipment.NodeInfo[] nodeInfo = new BeginShipment.NodeInfo[delivery.NodeInfo.Length];
                    for (int i = 0; i < delivery.NodeInfo.Length; i++)
                    {
                        BeginShipment.NodeInfo ni = new BeginShipment.NodeInfo();
                        ni.distance = delivery.NodeInfo[i].X;
                        ni.duration = delivery.NodeInfo[i].Y;
                        nodeInfo[i] = ni;
                    }

                    deliveryInfo.nodeInfo = nodeInfo;
                }


                deliveryInfo.node_delivery_time = delivery.NodeDeliveryTime;
                deliveryInfo.reserve_time = delivery.ReserveTime;
                deliveryInfo.start_delivery_interval = delivery.StartDeliveryInterval;
                deliveryInfo.sum_cost = delivery.Cost;
                deliveryInfo.weight = delivery.Weight;

                // 4. Возвращаем результат
                return deliveryInfo;
            }
            catch
            {
                return null;
            }
        }

        #region Переодическое информирование сервера об очереди отрузок

        /// <summary>
        /// Передача на сервер информации находящейся в очереди
        /// </summary>
        /// <param name="queue">Очередь</param>
        /// <returns>0 - очередь обработана; иначе - очередь не обработана</returns>
        private int QueueInfoToServer(EventQueue queue)
        {
            // 1. Иницализация
            int rc = 1;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (!IsCreated)
                    return rc;
                if (queue == null || !queue.IsCreated)
                    return rc;

                if (queue.Count <= 0)
                    return rc;

                // 5. Обрабатываем выбранные события
                rc = 5;
                BeginShipment.Shipment[] shipments = new BeginShipment.Shipment[queue.Count];
                int count = 0;

                for (int i = 0; i < queue.Count; i++)
                {
                    // 5.1 Извлекаем отгрузку
                    rc = 51;
                    QueueItem queueItem = queue.Items[i];
                    if (queueItem == null || queueItem.Delivery == null)
                        continue;

                    CourierDeliveryInfo delivery = queueItem.Delivery;
                    if (delivery.IsCompleted())
                        continue;

                    // 5.2 Строим данные для запроса
                    rc = 52;
                    BeginShipment.Shipment shipment = new BeginShipment.Shipment();
                    shipment.id = Guid.NewGuid().ToString();
                    shipment.status = 0;
                    shipment.shop_id = delivery.FromShop.Id;
                    shipment.courier_id = delivery.DeliveryCourier.Id;
                    shipment.date_target = queueItem.EventTime;
                    shipment.date_target_end = delivery.EndDeliveryInterval;
                    shipment.info = CreateDeliveryInfo(delivery);

                    // delivery_service_id
                    shipment.delivery_service_id = delivery.DeliveryCourier.CourierType.DServiceId;

                    //switch (delivery.DeliveryCourier.CourierType.VechicleType)
                    //{
                    //    case CourierVehicleType.YandexTaxi:
                    //        shipment.delivery_service_id = 14;
                    //        break;
                    //    case CourierVehicleType.GettTaxi:
                    //        shipment.delivery_service_id = 12;
                    //        break;
                    //    default:
                    //        shipment.delivery_service_id = 4;
                    //        break;
                    //}

                    // orders
                    int[] orderId = new int[delivery.OrderCount];
                    for (int j = 0; j < orderId.Length; j++)
                    {
                        orderId[j] = delivery.Orders[j].Id;
                    }

                    shipment.orders = orderId;
                    shipments[count++] = shipment;
                }

                if (count < shipments.Length)
                {
                    Array.Resize(ref shipments, count);
                }

                // 6. Отправляем запрос на сервер
                rc = 6;
                if (count > 0)
                {
                    int rc1 = BeginShipment.Begin(shipments);
                    if (rc1 != 0)
                        return rc = 1000 * rc + rc1;
                }

                // 7. Выход - Ok
                rc = 0;
                return rc;
            }
            catch
            {
                return rc;
            }
        }

        #endregion Переодическое информирование сервера об очереди отрузок 

        #region IDisposable Support

        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);
                    Logger.WriteToLog(string.Format(MessagePatterns.STOP_SERVICE, "FixedServiceEz", fileVersionInfo.ProductVersion));

                    if (requestTimer != null)
                    {
                        requestTimer.Elapsed -= RequestTimer_Elapsed;
                        DisposeTimer(requestTimer);
                        requestTimer = null;
                    }

                    if (queueTimer != null)
                    {
                        queueTimer.Elapsed -= QueueTimer_Elapsed;
                        DisposeTimer(queueTimer);
                        queueTimer = null;
                    }

                    if (queueInfoTimer != null)
                    {
                        queueInfoTimer.Elapsed -= QueueInfoTimer_Elapsed;
                        DisposeTimer(queueInfoTimer);
                        queueInfoTimer = null;
                    }

                    if (checkingQueueTimer != null)
                    {
                        checkingQueueTimer.Elapsed -= CheckingQueueTimer_Elapsed;
                        DisposeTimer(checkingQueueTimer);
                        checkingQueueTimer = null;
                    }

                    if (syncMutex != null)
                    {
                        DisposeMutex(syncMutex);
                        syncMutex = null;
                    }

                    config = null;
                    geoCache = null;
                    allCouriers = null;
                    allOrders = null;
                    allShops = null;
                    queue = null;
                    checkingQueue = null;
                    salesmanSolution = null;

                    // TODO: dispose managed state (managed objects).
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                IsCreated = false;
                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~FixedService() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }

        #endregion IDisposable Support

        /// <summary>
        /// Печать активных элементов очереди
        /// </summary>
        /// <param name="queue"></param>
        private static void PrintQueue(EventQueue queue)
        {
            try
            {
                // 2. Проверяем исходные данные
                if (queue == null || queue.Count <= 0)
                    return;

                // 3. Печатаем заголовок
                //Helper.WriteInfoToLog("Состояние очереди");
                Logger.WriteToLog(MessagePatterns.QUEUE_STATE);
                
                // 4. Печатаем все активные элементы
                BeginShipment.Shipment shipment = new BeginShipment.Shipment();
                JsonSerializer serializer = JsonSerializer.Create();

                for (int i = queue.ItemIndex; i < queue.Count; i++)
                {
                    // 4.1 Извлекаем элемент очереди
                    QueueItem queueItem = queue.Items[i];
                    CourierDeliveryInfo delivery = queueItem.Delivery;

                    if (delivery == null)
                        continue;

                    // 4.2 Заполняем информацию об отгрузке
                    shipment.id = Guid.NewGuid().ToString();
                    shipment.status = 0;
                    shipment.shop_id = delivery.FromShop.Id;
                    shipment.courier_id = delivery.DeliveryCourier.Id;
                    shipment.date_target_end = delivery.EndDeliveryInterval;
                    shipment.info = CreateDeliveryInfo(delivery);
                    shipment.date_target = queueItem.EventTime;

                    // delivery_service_id
                    shipment.delivery_service_id = delivery.DeliveryCourier.CourierType.DServiceId;
                    //switch (delivery.DeliveryCourier.CourierType.VechicleType)
                    //{
                    //    case CourierVehicleType.YandexTaxi:
                    //        shipment.delivery_service_id = 14;
                    //        break;
                    //    case CourierVehicleType.GettTaxi:
                    //        shipment.delivery_service_id = 12;
                    //        break;
                    //    default:
                    //        shipment.delivery_service_id = 4;
                    //        break;
                    //}

                    // orders
                    int[] orderId = new int[delivery.OrderCount];
                    for (int j = 0; j < orderId.Length; j++)
                    {
                        orderId[j] = delivery.Orders[j].Id;
                    }

                    shipment.orders = orderId;

                    // 4.3 Переводим в json-представление
                    string json;

                    using (StringWriter sw = new StringWriter())
                    {
                        serializer.Serialize(sw, shipment);
                        sw.Close();
                        json = sw.ToString();
                    }

                    // 4.4 Печатаем в лог
                    //Helper.WriteInfoToLog($"   Элемент {i}: {json}");
                    Logger.WriteToLog(string.Format(MessagePatterns.QUEUE_ITEM, i, json));
                }
            }
            catch
            { }
        }

        /// <summary>
        /// Добавление контрольных элементов в
        /// очередь предотвращения отгрузок для
        /// заданных заказов
        /// </summary>
        /// <param name="orders">Заказы</param>
        /// <returns>0 - элементы добавлены; иначе - элементы не добавлены</returns>
        private int AddCheckingInfo(OrderEvent[] orders)
        {
            // !!!!!!!!!!!!!!!!!!!!!! (отключение очереди контроля утечек)
            return 0;
            // 1. Инициализация
            int rc = 1;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (orders == null || orders.Length <= 0)
                    return rc;

                // 3. Отбираем заказы требующие доставки
                rc = 3;
                OrderEvent[] filteredOrders = new OrderEvent[orders.Length];
                int count = 0;

                for (int i = 0; i < orders.Length; i++)
                {
                    OrderEvent order = orders[i];
                    if (order.type == 0 || order.type == 1)
                    {
                        filteredOrders[count++] = order;
                    }
                }

                if (count <= 0)
                    return rc = 0;

                Array.Resize(ref filteredOrders, count);

                // 4. Получаем гео-данные
                rc = 4;
                UpdateGeoCacheEx(geoCache, allOrders, filteredOrders);

                // 5. Добавляем (обновляем) для кадого заказа элемент в очередь предотвращения утечки
                rc = 5;
                for (int i = 0; i < count; i++)
                {
                    AddCheckingEvent(filteredOrders[i]);
                }

                // 6. Переустанавливаем таймер очереди
                rc = 6;
                DateTime timeOn = checkingQueue.GetFirstEventTime();
                if (timeOn == DateTime.MaxValue)
                    return rc;
                DateTime now = DateTime.Now;
                double interval = 500;

                if (timeOn > now)
                {
                    interval = (timeOn - now).TotalMilliseconds;
                    if (interval < 500)
                        interval = 500;
                }

                checkingQueueTimer.Enabled = false;
                checkingQueueTimer.Interval = interval;
                checkingQueueTimer.Start();

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
        /// Добавление элемента в очередь
        /// предотвращения утечек для заданного заказа
        /// </summary>
        /// <param name="order">Заказ</param>
        /// <returns>0 - элемент добавлен; иначе - элемент не добавлен</returns>
        private int AddCheckingEvent(OrderEvent order)
        {
            // 1. Инициализация
            int rc = 1;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (geoCache == null)
                    return rc;
                if (order == null || !(order.type == 0 || order.type == 1))
                    return rc;
                if (order.service_available == null || order.service_available.Length <= 0)
                    return rc;
                if (order.delivery_frame_from == default(DateTime))
                    return rc;
                if (order.delivery_frame_to == default(DateTime))
                    return rc;
                if (order.delivery_frame_from > order.delivery_frame_to)
                    return rc;

                // 3. Извлекаем магазин
                rc = 3;
                Shop shop = allShops.GetShop(order.shop_id);
                if (shop == null)
                    return rc;

                // 4. Извлекаем заказ
                rc = 4;
                Order shopOrder = allOrders.GetOrder(order.order_id);
                if (shopOrder == null)
                    return rc;

                // 5. Выбираем доступные способы отгрузки
                rc = 5;
                //Dictionary<CourierVehicleType, bool> availableTypes = new Dictionary<CourierVehicleType, bool>(8);

                //for (int i = 0; i < order.service_available.Length; i++)
                //{
                //    ShopService shopService = order.service_available[i];

                //    if (shopService.shop_id == order.shop_id)
                //    {
                //        switch (shopService.dservice_id)
                //        {
                //            case 4: // курьеры
                //                availableTypes[CourierVehicleType.Car] = true;
                //                availableTypes[CourierVehicleType.Bicycle] = true;
                //                availableTypes[CourierVehicleType.OnFoot] = true;
                //                break;
                //            case 12: // Gett-такси
                //                availableTypes[CourierVehicleType.GettTaxi] = true;
                //                break;
                //            case 14: // Yandex-такси
                //                availableTypes[CourierVehicleType.YandexTaxi] = true;
                //                break;
                //        }
                //    }
                //}

                //if (availableTypes.Count <= 0)
                //    return rc;

                //CourierVehicleType[] allTypes = new CourierVehicleType[availableTypes.Count];
                //availableTypes.Keys.CopyTo(allTypes, 0);
                //availableTypes = null;

                CourierVehicleType[] allTypes = shopOrder.EnabledTypesEx;
                if (allTypes == null || allTypes.Length <= 0)
                    return rc;

                // 6. Обрабатываем каждый способ отгрузки и выбираем отгрузку, которую следет поестить в очередь
                rc = 6;
                DateTime calcTime = DateTime.Now;
                CourierDeliveryInfo queueItemDelivery = null;
                double[] latitude = new double[] { shop.Latitude, order.geo_lat};
                double[] longitude = new double[] { shop.Longitude, order.geo_lon};
                latitude[0] = shop.Latitude;
                longitude[0] = shop.Longitude;
                latitude[1] = shop.Latitude;
                longitude[1] = shop.Longitude;

                for (int i = 0; i < allTypes.Length; i++)
                {
                    // 6.1 Координаты должны быть доступны
                    rc = 61;
                    geoCache.PutLocationInfo(latitude, longitude, allTypes[i]);

                    // 6.2 Запрашиваем эталонного курьера
                    rc = 62;
                    Courier courier = allCouriers.GetReferenceCourier(allTypes[i]);
                    if (courier == null)
                        continue;

                    // 6.3 Создаём одиночную отгрузку
                    rc = 63;
                    CourierDeliveryInfo singleDelivery;
                    int rc1 = SalesmanSolutionEy.CreateSingleDelivery(shop, shopOrder, courier, !courier.IsTaxi, calcTime, geoCache, out singleDelivery);
                    if (rc1 == 0 && singleDelivery != null)
                    {
                        if (queueItemDelivery == null)
                        {
                            queueItemDelivery = singleDelivery;
                        }
                        else if (queueItemDelivery.DeliveryCourier.IsTaxi && courier.IsTaxi)
                        {
                            if (singleDelivery.EndDeliveryInterval > queueItemDelivery.EndDeliveryInterval)
                                queueItemDelivery = singleDelivery;
                        }
                        else if (!queueItemDelivery.DeliveryCourier.IsTaxi && courier.IsTaxi)
                        {
                            queueItemDelivery = singleDelivery;
                        }
                        else if (!queueItemDelivery.DeliveryCourier.IsTaxi && !courier.IsTaxi)
                        {
                            if (singleDelivery.EndDeliveryInterval > queueItemDelivery.EndDeliveryInterval)
                                queueItemDelivery = singleDelivery;
                        }
                    }
                }

                if (queueItemDelivery == null)
                    return rc;

                // 7. Устанавливаем время события
                rc = 7;
                DateTime eventTime = queueItemDelivery.EndDeliveryInterval.AddMinutes(-config.functional_parameters.checking_alert_interval);
                if (eventTime < queueItemDelivery.StartDeliveryInterval)
                    eventTime = queueItemDelivery.StartDeliveryInterval;

                // 8. Помещаем отгрузку в контрольную очередь
                rc = 8;
                QueueItem item = new QueueItem(eventTime, QueueItemType.CheckingAlert, queueItemDelivery);
                checkingQueue.AddItem(item);

                // 9. Выход - Ok
                rc = 0;
                return rc;
            }
            catch
            {
                return rc;    
            }

        }

        ///// <summary>
        ///// Запрос геоданных для заданных заказов
        ///// </summary>
        ///// <param name="geoCache">Гео-кэш</param>
        ///// <param name="orderEvents">Заказы</param>
        ///// <returns>0 - данные получены; иначе - данные не получены</returns>
        //private static int UpdateGeoCache(GeoCache geoCache, OrderEvent[] orderEvents)
        //{
        //    // 1. Инициализация
        //    int rc = 1;

        //    try
        //    {
        //        // 2. Проверяем исходные данные
        //        rc = 2;
        //        if (geoCache == null)
        //            return rc;
        //        if (orderEvents == null || orderEvents.Length <= 0)
        //            return rc;

        //        // 3. Сортируем заказы по магазину
        //        rc = 3;
        //        Array.Sort(orderEvents, CompareOrderEventsByShopId);

        //        // 4. Цикл получения данных по всем требуемым парам для всех доступных способов доставки
        //        rc = 4;
        //        int currentShopId = orderEvents[0].shop_id;
        //        int startIndex = 0;
        //        int endIndex = 0;
        //        double[] latitude;
        //        double[] longitude;
        //        int count;
        //        Dictionary<CourierVehicleType, bool> vehicleTypes = new Dictionary<CourierVehicleType, bool>(8);

        //        for (int i = 1; i < orderEvents.Length; i++)
        //        {
        //            if (orderEvents[i].shop_id == currentShopId)
        //            {
        //                endIndex = i;
        //            }
        //            else
        //            {
        //                latitude = new double[endIndex - startIndex + 2];
        //                longitude = new double[latitude.Length];
        //                count = 0;
        //                vehicleTypes.Clear();

        //                for (int j = startIndex; j <= endIndex; j++)
        //                {
        //                    OrderEvent order = orderEvents[j];
        //                    latitude[count] = order.geo_lat;
        //                    longitude[count++] = order.geo_lon;

        //                    if (order.service_available != null && order.service_available.Length > 0)
        //                    {
        //                        for (int k = 0; k < order.service_available.Length; k++)
        //                        {
        //                            ShopService shopService = order.service_available[k];

        //                            if (shopService.shop_id == order.shop_id)
        //                            {
        //                                switch (shopService.dservice_id)
        //                                {
        //                                    case 4: // курьеры
        //                                        vehicleTypes[CourierVehicleType.Car] = true;
        //                                        vehicleTypes[CourierVehicleType.Bicycle] = true;
        //                                        vehicleTypes[CourierVehicleType.OnFoot] = true;
        //                                        break;
        //                                    case 12: // Gett-такси
        //                                        vehicleTypes[CourierVehicleType.GettTaxi] = true;
        //                                        break;
        //                                    case 14: // Yandex-такси
        //                                        vehicleTypes[CourierVehicleType.YandexTaxi] = true;
        //                                        break;
        //                                }
        //                            }
        //                        }
        //                    }
        //                }

        //                if (vehicleTypes.Count > 0)
        //                {
        //                    latitude[count] = orderEvents[startIndex].shop_geo_lat;
        //                    longitude[count] = orderEvents[startIndex].shop_geo_lon;

        //                    foreach(CourierVehicleType vt in vehicleTypes.Keys)
        //                    {
        //                        geoCache.PutLocationInfo(latitude, longitude, vt);
        //                    }
        //                }

        //                currentShopId = orderEvents[i].shop_id;
        //                startIndex = i;
        //                endIndex = i;
        //            }
        //        }

        //        // заказы последнего магазина
        //        latitude = new double[endIndex - startIndex + 2];
        //        longitude = new double[latitude.Length];
        //        count = 0;
        //        vehicleTypes.Clear();

        //        for (int j = startIndex; j <= endIndex; j++)
        //        {
        //            OrderEvent order = orderEvents[j];
        //            latitude[count] = order.geo_lat;
        //            longitude[count++] = order.geo_lon;

        //            if (order.service_available != null && order.service_available.Length > 0)
        //            {
        //                for (int k = 0; k < order.service_available.Length; k++)
        //                {
        //                    ShopService shopService = order.service_available[k];

        //                    if (shopService.shop_id == order.shop_id)
        //                    {
        //                        switch (shopService.dservice_id)
        //                        {
        //                            case 4: // курьеры
        //                                vehicleTypes[CourierVehicleType.Car] = true;
        //                                vehicleTypes[CourierVehicleType.Bicycle] = true;
        //                                vehicleTypes[CourierVehicleType.OnFoot] = true;
        //                                break;
        //                            case 12: // Gett-такси
        //                                vehicleTypes[CourierVehicleType.GettTaxi] = true;
        //                                break;
        //                            case 14: // Yandex-такси
        //                                vehicleTypes[CourierVehicleType.YandexTaxi] = true;
        //                                break;
        //                        }
        //                    }
        //                }
        //            }
        //        }

        //        if (vehicleTypes.Count > 0)
        //        {
        //            latitude[count] = orderEvents[startIndex].shop_geo_lat;
        //            longitude[count] = orderEvents[startIndex].shop_geo_lon;

        //            foreach(CourierVehicleType vt in vehicleTypes.Keys)
        //            {
        //                geoCache.PutLocationInfo(latitude, longitude, vt);
        //            }
        //        }

        //        // 5. Выход - Ok
        //        rc = 0;
        //        return rc;
        //    }
        //    catch
        //    {
        //        return rc;
        //    }
        //}

        /// <summary>
        /// Запрос геоданных для заданных заказов
        /// </summary>
        /// <param name="geoCache">Гео-кэш</param>
        /// <param name="orderEvents">Заказы</param>
        /// <returns>0 - данные получены; иначе - данные не получены</returns>
        private static int UpdateGeoCacheEx(GeoCache geoCache, AllOrdersEx allOrders, OrderEvent[] orderEvents)
        {
            // 1. Инициализация
            int rc = 1;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (geoCache == null)
                    return rc;
                if (allOrders == null)
                    return rc;
                if (orderEvents == null || orderEvents.Length <= 0)
                    return rc;

                // 3. Сортируем заказы по магазину
                rc = 3;
                Array.Sort(orderEvents, CompareOrderEventsByShopId);

                // 4. Цикл получения данных по всем требуемым парам для всех доступных способов доставки
                rc = 4;
                int currentShopId = orderEvents[0].shop_id;
                int startIndex = 0;
                int endIndex = 0;
                double[] latitude;
                double[] longitude;
                int count;
                //List<CourierVehicleType> vehicleTypes = new List<CourierVehicleType>(64);
                Dictionary<CourierVehicleType, bool> vehicleTypes = new Dictionary<CourierVehicleType, bool>(64);

                for (int i = 1; i < orderEvents.Length; i++)
                {
                    if (orderEvents[i].shop_id == currentShopId)
                    {
                        endIndex = i;
                    }
                    else
                    {
                        latitude = new double[endIndex - startIndex + 2];
                        longitude = new double[latitude.Length];
                        count = 0;
                        vehicleTypes.Clear();

                        for (int j = startIndex; j <= endIndex; j++)
                        {
                            OrderEvent order = orderEvents[j];
                            latitude[count] = order.geo_lat;
                            longitude[count++] = order.geo_lon;

                            if (order.service_available != null && order.service_available.Length > 0)
                            {
                                CourierVehicleType[] orderVehicleTypes = allOrders.GetDeliveryMaskEx(currentShopId, order.service_available);
                                if (orderVehicleTypes != null && orderVehicleTypes.Length > 0)
                                {
                                    foreach (CourierVehicleType vt in orderVehicleTypes)
                                    {
                                        vehicleTypes[vt] = true;
                                    }
                                }
                            }
                        }

                        if (vehicleTypes.Count > 0)
                        {
                            latitude[count] = orderEvents[startIndex].shop_geo_lat;
                            longitude[count] = orderEvents[startIndex].shop_geo_lon;

                            foreach(CourierVehicleType vt in vehicleTypes.Keys)
                            {
                                geoCache.PutLocationInfo(latitude, longitude, vt);
                            }
                        }

                        currentShopId = orderEvents[i].shop_id;
                        startIndex = i;
                        endIndex = i;
                    }
                }

                // заказы последнего магазина
                latitude = new double[endIndex - startIndex + 2];
                longitude = new double[latitude.Length];
                count = 0;
                vehicleTypes.Clear();

                for (int j = startIndex; j <= endIndex; j++)
                {
                    OrderEvent order = orderEvents[j];
                    latitude[count] = order.geo_lat;
                    longitude[count++] = order.geo_lon;

                    if (order.service_available != null && order.service_available.Length > 0)
                    {
                        CourierVehicleType[] orderVehicleTypes = allOrders.GetDeliveryMaskEx(currentShopId, order.service_available);
                        if (orderVehicleTypes != null && orderVehicleTypes.Length > 0)
                        {
                            foreach (CourierVehicleType vt in orderVehicleTypes)
                            {
                                vehicleTypes[vt] = true;
                            }
                        }
                    }
                }

                if (vehicleTypes.Count > 0)
                {
                    latitude[count] = orderEvents[startIndex].shop_geo_lat;
                    longitude[count] = orderEvents[startIndex].shop_geo_lon;

                    foreach(CourierVehicleType vt in vehicleTypes.Keys)
                    {
                        geoCache.PutLocationInfo(latitude, longitude, vt);
                    }
                }

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
        /// Сравнение двух событий о заказах по ShopId
        /// </summary>
        /// <param name="event1">Событие 1</param>
        /// <param name="event2">Событие 2</param>
        /// <returns>-1 - Событие_1 меньше Событие_2; 0 - Событие_1 = Событие_2; 1 - Событие_1 больше Событие_2</returns>
        private static int CompareOrderEventsByShopId(OrderEvent event1, OrderEvent event2)
        {
            if (event1.shop_id < event2.shop_id)
                return -1;
            if (event1.shop_id > event2.shop_id)
                return 1;
            return 0;
        }
    }
}
