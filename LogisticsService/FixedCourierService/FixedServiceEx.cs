
//namespace LogisticsService.FixedCourierService
//{
//    using LogisticsService.API;
//    using LogisticsService.Couriers;
//    using LogisticsService.FixedCourierService.ServiceQueue;
//    using LogisticsService.Geo;
//    using LogisticsService.Orders;
//    using LogisticsService.SalesmanTravelingProblem;
//    using LogisticsService.ServiceParameters;
//    using LogisticsService.Shops;
//    using Newtonsoft.Json;
//    using System;
//    using System.Collections.Generic;
//    using System.IO;
//    //using System.Drawing;
//    using System.Threading;
//    using System.Threading.Tasks;
//    using System.Timers;
//    using static LogisticsService.API.GetOrderEvents;
//    using static LogisticsService.API.GetShopEvents;

//    /// <summary>
//    /// Сервис с фиксированными курьерами
//    /// </summary>
//    public class FixedServiceEx : IDisposable
//    {
//        /// <summary>
//        /// Параметры конфигурации
//        /// </summary>
//        private ServiceConfig config;

//        /// <summary>
//        /// Менеджер расстояний и времени движения
//        /// между точками разными способами
//        /// </summary>
//        private GeoCache geoCache;

//        /// <summary>
//        /// Все курьеры
//        /// </summary>
//        private AllCouriers allCouriers;

//        /// <summary>
//        /// Все заказы
//        /// </summary>
//        private AllOrdersEx allOrders;

//        /// <summary>
//        /// Все магазины
//        /// </summary>
//        private AllShopsEx allShops;

//        /// <summary>
//        /// Флаг: true - экземпляр создан; false - экземпляр не создан
//        /// </summary>
//        public bool IsCreated { get; private set; }

//        /// <summary>
//        /// Мьютекс для синхронизации получения
//        /// новых данных и их обработки
//        /// </summary>
//        private Mutex syncMutex = new Mutex();

//        /// <summary>
//        /// Таймер для запроса данных с сервера
//        /// </summary>
//        private System.Timers.Timer requestTimer;

//        /// <summary>
//        /// Таймер для отработки событий в очереди
//        /// </summary>
//        private System.Timers.Timer queueTimer;

//        /// <summary>
//        /// Таймер для информирования сервера о состоянии очереди
//        /// </summary>
//        private System.Timers.Timer queueInfoTimer;

//        /// <summary>
//        /// Очередь событий на отгрузку
//        /// </summary>
//        private EventQueue queue;

//        /// <summary>
//        /// Решение задачи комивояжера
//        /// </summary>
//        private SalesmanSolutionEx salesmanSolution;

//        /// <summary>
//        /// Создание сервиса
//        /// </summary>
//        /// <param name="jsonFilename"></param>
//        /// <returns></returns>
//        public int Create(string jsonFilename)
//        {
//            // 1. Инициализация
//            int rc = 1;
//            IsCreated = false;
//            config = null;
//            geoCache = null;
//            allCouriers = null;
//            allOrders = null;
//            allShops = null;
//            DisposeTimer(requestTimer);
//            DisposeTimer(queueTimer);
//            DisposeTimer(queueInfoTimer);
//            requestTimer = null;
//            queueTimer = null;
//            queueInfoTimer = null;
//            queue = null;
//            salesmanSolution = null;

//            try
//            {
//                // 2. Проверяем исходные данные
//                rc = 2;
//                if (string.IsNullOrWhiteSpace(jsonFilename))
//                    return rc;

//                // 3. Загружаем Config
//                rc = 3;
//                config = ServiceConfig.Deserialize(jsonFilename);
//                if (config == null)
//                    return rc;

//                RequestParameters.API_ROOT = config.functional_parameters.api_root;
//                RequestParameters.SERVICE_ID = config.functional_parameters.service_id;

//                // 4. Создаём Geo cache
//                rc = 4;
//                int capacity = config.functional_parameters.geo_cache_capacity;
//                if (capacity <= 10000)
//                    capacity = 10000;
//                geoCache = new GeoCache(config, capacity);

//                #region For Debug Only

//                //double[] latitude = new double[]  { 53.196871, 53.18774, 53.224951598824, 53.189827799216, 53.205591699608 };
//                //double[] longitude = new double[] { 45.005578, 44.97662, 44.9953245,      44.9854697,      44.9902701 };

//                //int rcGeo = geoCache.PutLocationInfo(latitude, longitude, CourierVehicleType.YandexTaxi);

//                //Point[,] dataTable;
//                //rcGeo = geoCache.GetPointsDataTable(latitude, longitude, CourierVehicleType.YandexTaxi, out dataTable);

//                #endregion For Debug Only

//                // 5. Создаём All couriers
//                rc = 5;
//                allCouriers = new AllCouriers();
//                int rc1 = allCouriers.Create(config);
//                if (rc1 != 0)
//                    return rc = 100 * rc + rc1;

//                // 6. Создаём All orders
//                rc = 6;
//                allOrders = new AllOrdersEx();
//                rc1 = allOrders.Create();
//                if (rc1 != 0)
//                    return rc = 100 * rc + rc1;

//                // 7. Создаём All shops
//                rc = 7;
//                allShops = new AllShopsEx();
//                rc1 = allShops.Create();
//                if (rc1 != 0)
//                    return rc = 100 * rc + rc1;

//                salesmanSolution = new SalesmanSolutionEx(geoCache, config.functional_parameters.salesman_problem_levels);

//                //salesmanSolution.CheckPath(allCouriers.Couriers[4]);

//                // 8. Создаём таймер для запроса новых данных
//                rc = 8;
//                requestTimer = new System.Timers.Timer();
//                requestTimer.Elapsed += RequestTimer_Elapsed;
//                requestTimer.AutoReset = true;
//                requestTimer.Interval = config.functional_parameters.data_request_interval;
//                requestTimer.Enabled = false;

//                // 9. Создаём таймер для обработки очереди
//                rc = 9;
//                queueTimer = new System.Timers.Timer();
//                queueTimer.Elapsed += QueueTimer_Elapsed;
//                queueTimer.AutoReset = false;
//                queueTimer.Interval = 10000;
//                queueTimer.Enabled = false;


//                // 10. Создаём таймер для информирования сервера о состоянии очереди
//                rc = 10;
//                queueInfoTimer = new System.Timers.Timer();
//                queueInfoTimer.Elapsed += QueueInfoTimer_Elapsed;
//                queueInfoTimer.AutoReset = true;
//                queueInfoTimer.Interval = config.functional_parameters.queue_Info_interval;
//                queueInfoTimer.Enabled = false;

//                // 11. Выход - Ok
//                rc = 0;
//                IsCreated = true;
//                return rc;
//            }
//            catch
//            {
//                return rc;
//            }
//        }

//        /// <summary>
//        /// Событие таймера информирования сервера о состоянии очереди
//        /// </summary>
//        /// <param name="sender">Таймер</param>
//        /// <param name="e">Аргументы события</param>
//        private void QueueInfoTimer_Elapsed(object sender, ElapsedEventArgs e)
//        {
//            // 1. Инициализация
//            bool isCatched = false;
//            if (!IsCreated)
//                return;
//            EventQueue currentQueue = queue;
//            if (currentQueue == null)
//                return;

//            try
//            {
//                isCatched = syncMutex.WaitOne(400000);
//                Helper.WriteInfoToLog($"QueueInfoTimer_Elapsed ItemCount = {currentQueue.Count}");
//                QueueInfoToServer(currentQueue);
//            }
//            catch
//            { }
//            finally
//            {
//                if (isCatched)
//                    syncMutex.ReleaseMutex();
//            }
//        }

//        /// <summary>
//        /// Запуск сервиса
//        /// </summary>
//        /// <returns></returns>
//        public int Start()
//        {
//            // 1. Инициализация
//            int rc = 1;

//            try
//            {
//                // 2. Проверяем исходные данные
//                rc = 2;
//                if (!IsCreated)
//                    return rc;

//                // 3. Обновляем данные
//                rc = 3;
//                queueInfoTimer.Start();
//                Task.Run(() => Refresh(1));

//                requestTimer.Start();

//                // 4.Выход - Ok
//                rc = 0;
//                return rc;
//            }
//            catch
//            {
//                return rc;
//            }
//        }

//        /// <summary>
//        /// Остановить выполнение
//        /// </summary>
//        public void Stop()
//        {
//            try
//            {
//                // 2. Проверяем исходные данные
//                if (!IsCreated)
//                    return;

//                // 3. Удаляем очередь
//                queue = null;

//                // 4. Останавливаем таймеры
//                try
//                { requestTimer.Stop(); }
//                catch { }
//                try
//                { queueTimer.Stop(); }
//                catch { }
//                try
//                { queueInfoTimer.Stop(); }
//                catch { }
//            }
//            catch
//            { }
//        }

//        /// <summary>
//        /// Событие таймера запроса данных
//        /// </summary>
//        /// <param name="sender">Таймер</param>
//        /// <param name="e">Аргументы события</param>
//        private void RequestTimer_Elapsed(object sender, ElapsedEventArgs e)
//        {
//            // 1. Инициализация
//            bool isCatched = false;
//            if (!IsCreated)
//                return;

//            try
//            {
//                isCatched = syncMutex.WaitOne(config.functional_parameters.data_request_interval);
//                Refresh(0);
//                Helper.WriteInfoToLog($"GeoCache capacity {geoCache.HashCapacity}. ItemCount = {geoCache.CacheItemCount}");
//            }
//            catch
//            { }
//            finally
//            {
//                if (isCatched)
//                    syncMutex.ReleaseMutex();
//            }
//        }

//        /// <summary>
//        /// Событие таймера - обработка очереди на отгрузку
//        /// </summary>
//        /// <param name="sender">Таймер</param>
//        /// <param name="e">Аргументы события</param>
//        private void QueueTimer_Elapsed(object sender, ElapsedEventArgs e)
//        {
//            // 1. Инициализация
//            bool isCatched = false;
//            if (!IsCreated)
//                return;
//            EventQueue currentQueue = queue;
//            if (currentQueue == null)
//                return;

//            try
//            {
//                queueTimer.Enabled = false;
//                isCatched = syncMutex.WaitOne(70000);
//                Helper.WriteInfoToLog($"QueueTimer_Elapsed ItemCount = {currentQueue.Count}");
//                QueueHandler(currentQueue);


//                QueueItem item = currentQueue.GetCurrentItem();
//                if (item != null)
//                {
//                    DateTime nextTime = item.EventTime;
//                    DateTime currentTime = DateTime.Now;
//                    double interval;
//                    if (nextTime <= currentTime)
//                    {
//                        interval = 500;
//                    }
//                    else
//                    {
//                        interval = (nextTime - currentTime).TotalMilliseconds;
//                        if (interval < 500)
//                            interval = 500;
//                    }

//                    queueTimer.Interval = interval;
//                    queueTimer.Start();
//                }
//            }
//            catch
//            { }
//            finally
//            {
//                if (isCatched)
//                    syncMutex.ReleaseMutex();
//            }
//        }

//        /// <summary>
//        /// Ликвидация таймера
//        /// </summary>
//        /// <param name="timer">Таймер</param>
//        private static void DisposeTimer(System.Timers.Timer timer)
//        {
//            try
//            {
//                if (timer != null)
//                {
//                    timer.Dispose();
//                }
//            }
//            catch
//            { }
//        }

//        /// <summary>
//        /// Ликвидация Mutex
//        /// </summary>
//        /// <param name="mutex">Mutex</param>
//        private static void DisposeMutex(Mutex mutex)
//        {
//            try
//            {
//                if (mutex != null)
//                {
//                    mutex.Dispose();
//                }
//            }
//            catch
//            { }

//        }

//        #region Refresh

//        /// <summary>
//        /// Запрос данных с сервера
//        /// </summary>
//        /// <param name="requestType"></param>
//        /// <returns></returns>
//        private int Refresh(int requestType)
//        {
//            // 1. Инициализация
//            int rc = 1;
//            bool isCatched = false;
//            //requestTimer.Enabled = false;
//            try
//            {
//                // 2. Проверяем исходные данные
//                rc = 2;
//                if (!IsCreated)
//                    return rc;

//                // 3. Захватываем Mutex
//                rc = 3;
//                isCatched = syncMutex.WaitOne(70000);
//                if (!isCatched)
//                    return rc;

//                // 4. Данные о курьрах
//                rc = 4;
//                CourierEvent[] courierEvents;
//                int rc1 = GetCourierEvents.GetEvents(requestType, out courierEvents);
//                if (rc1 == 0)
//                    rc1 = allCouriers.Refresh(courierEvents);

//                // 5. Данные о магазинах
//                rc = 5;
//                ShopEvent[] shopEvents;
//                rc1 = GetShopEvents.GetEvents(requestType, out shopEvents);
//                if (rc1 == 0)
//                    rc1 = allShops.Refresh(shopEvents);

//                // 6. Данные о заказах
//                rc = 6;
//                OrderEvent[] orderEvents;
//                rc1 = GetOrderEvents.GetEvents(requestType, out orderEvents);
//                if (rc1 == 0)
//                    rc1 = allOrders.Refresh(orderEvents);

//                // 7. Если новых событий для пересчета нет
//                rc = 7;
//                if ((courierEvents == null || courierEvents.Length <= 0) &&
//                    (orderEvents == null || orderEvents.Length <= 0))
//                    return rc = 0;

//                // 8. Строим список магазинов требующих пересчета
//                rc = 8;
//                Dictionary<int, Shop> recalcShops = new Dictionary<int, Shop>(allShops.Count);

//                if (courierEvents != null && courierEvents.Length > 0)
//                {
//                    foreach(CourierEvent courierEvent in courierEvents)
//                    {
//                        if (!recalcShops.ContainsKey(courierEvent.shop_id))
//                        {
//                            Shop shop;
//                            if (allShops.Shops.TryGetValue(courierEvent.shop_id, out shop))
//                                recalcShops.Add(shop.Id, shop);
//                        }
//                    }
//                }

//                if (orderEvents != null && orderEvents.Length > 0)
//                {
//                    foreach(OrderEvent orderEvent in orderEvents)
//                    {
//                        if (!recalcShops.ContainsKey(orderEvent.shop_id))
//                        {
//                            Shop shop;
//                            if (allShops.Shops.TryGetValue(orderEvent.shop_id, out shop))
//                                recalcShops.Add(shop.Id, shop);
//                        }
//                    }
//                }

//                if (recalcShops.Count <= 0)
//                    return rc = 0;

//                // 9. Пересчет отгрузок магазинов
//                rc = 9;
//                Shop[] shops = new Shop[recalcShops.Count];
//                recalcShops.Values.CopyTo(shops, 0);
//                recalcShops = null;
//                rc1 = RecalcDelivery(shops);
//                if (rc1 != 0)
//                    return rc = 1000 * rc + rc1;


//                //// 7. Пересчитываем отгрузки, если есть изменения
//                //rc = 7;
//                //if ((courierEvents != null && courierEvents.Length > 0) ||
//                //    (shopEvents != null && shopEvents.Length > 0) ||
//                //    (orderEvents != null && orderEvents.Length > 0))


//                //CourierDeliveryInfo[] aOrders;
//                //CourierDeliveryInfo[] rOrders;
//                //Order[] unOrders;

//                //int rcv = salesmanSolution.CreateShopDeliveries(allShops.Shops[2583],
//                //    new Order[] { allOrders.Orders[8681024], allOrders.Orders[8681217], allOrders.Orders[8684011] },
//                //    new Courier[] { allCouriers.Couriers[4] }, false, out aOrders, out rOrders, out unOrders);


//                // 10. Выход - Ok
//                rc = 0;
//                return rc;
//            }
//            catch
//            {
//                return rc;
//            }
//            finally
//            {
//                if (isCatched)
//                    syncMutex.ReleaseMutex();
//            }
//        }

//        /// <summary>
//        /// Пересчет отгрузок во всех магазинах
//        /// </summary>
//        /// <returns></returns>
//        private int RecalcDelivery()
//        {
//            // 1. Инициализация
//            int rc = 1;
//            EventQueue deliveryQueue = null;

//            try
//            {
//                // 2. Проверяем исходные данные
//                rc = 2;
//                if (!IsCreated)
//                    return rc;

//                queueTimer.Enabled = false;

//                // 3. Производим обработку для каждого магазина отдельно
//                rc = 3;
//                QueueItem[] allQueueItems = new QueueItem[3000];
//                int itemCount = 0;

//                foreach (Shop shop in allShops.Shops.Values)
//                {
//                    // 3.1 Выбираем заказы магазина
//                    rc = 31;
//                    Order[] shopOrders = allOrders.GetShopOrders(shop.Id);
//                    if (shopOrders == null || shopOrders.Length <= 0)
//                        continue;

//                    // 3.2. Выбираем курьров магазина
//                    rc = 32;
//                    Courier[] shopCouriers = allCouriers.GetShopCouriers(shop.Id);

//                    // 3.3 Строим отгрузки для магазина
//                    rc = 33;
//                    CourierDeliveryInfo[] assembledOrders;
//                    CourierDeliveryInfo[] receiptedOrders;
//                    Order[] undeliveredOrders;
//                    int rc1 = CreateShopDeliveries(shop, shopOrders, shopCouriers, out assembledOrders, out receiptedOrders, out undeliveredOrders);
//                    if (rc1 != 0)
//                        continue;

//                    // 3.4 Отправляем отгрузки с не собранными товарами
//                    rc = 34;
//                    if (receiptedOrders != null && receiptedOrders.Length > 0)
//                    {
//                        rc1 = SendReceiptedOrders(receiptedOrders);
//                    }

//                    // 3.5 Обрабатываем отгрузки, состоящие целиком из собранных заказов
//                    rc = 35;
//                    if (assembledOrders != null && assembledOrders.Length > 0)
//                    {
//                        QueueItem[] deliveryQueueItems;
//                        rc1 = SendAssembledOrders(assembledOrders, out deliveryQueueItems);
//                        if (deliveryQueueItems != null && deliveryQueueItems.Length > 0)
//                        {
//                            if (itemCount + deliveryQueueItems.Length >= allQueueItems.Length)
//                            {
//                                Array.Resize(ref allQueueItems, allQueueItems.Length + 100 * deliveryQueueItems.Length);
//                            }

//                            deliveryQueueItems.CopyTo(allQueueItems, itemCount);
//                            itemCount += deliveryQueueItems.Length;
//                        }
//                    }

//                    // 3.6 Отправляем информацию о заказах, которые не могут быть доставлены в срок
//                    rc = 36;
//                    if (undeliveredOrders != null && undeliveredOrders.Length > 0)
//                    {
//                        for (int k = 0; k < undeliveredOrders.Length; k++)
//                        {
//                            if (undeliveredOrders[k].DeliveryTimeTo > DateTime.Now)
//                            {
//                                if (undeliveredOrders[k].Id != 8656859)
//                                    k = k;
//                            }
//                        }

//                        rc1 = SendUndeliveryOrders(undeliveredOrders);
//                    }
//                }

//                // 4. Создаём очередь на отгррузки и запускаем таймер очереди
//                rc = 4;
//                if (itemCount > 0)
//                {
//                    Array.Resize(ref allQueueItems, itemCount);
//                    deliveryQueue = new EventQueue();
//                    Helper.WriteInfoToLog($"Создание очереди отгрузок ({itemCount})");
//                    int rc1 = deliveryQueue.Create(allQueueItems);
//                    if (rc1 == 0)
//                    {
//                        QueueItem queueItem = deliveryQueue.GetCurrentItem();
//                        if (queueItem != null)
//                        {
//                            DateTime currentTime = DateTime.Now;
//                            double interval = 500;
//                            if (currentTime < queueItem.EventTime)
//                            {
//                                interval = (queueItem.EventTime - currentTime).TotalMilliseconds;
//                                if (interval < 500)
//                                    interval = 500;
//                            }

//                            queueTimer.Interval = interval;
//                            queueTimer.Start();
//                        }
//                    }
//                }

//                // 5. Выход - Ok
//                rc = 0;
//                return rc;
//            }
//            catch
//            {
//                return rc;
//            }
//            finally
//            {
//                queue = deliveryQueue;
//            }
//        }

//        /// <summary>
//        ///  Пересчет отгрузок в заданных магазинах
//        /// </summary>
//        /// <param name="shops">Магазины, в которых производится пересчет</param>
//        /// <returns></returns>
//        private int RecalcDelivery(Shop[] shops)
//        {
//            // 1. Инициализация
//            int rc = 1;
//            EventQueue deliveryQueue = null;

//            try
//            {
//                // 2. Проверяем исходные данные
//                rc = 2;
//                if (!IsCreated)
//                    return rc;
//                if (shops == null || shops.Length <= 0)
//                    return rc;

//                queueTimer.Enabled = false;

//                // 3. Производим обработку для каждого магазина отдельно
//                rc = 3;
//                QueueItem[] allQueueItems = new QueueItem[3000];
//                int itemCount = 0;

//                foreach (Shop shop in shops)
//                {
//                    // 3.1 Выбираем заказы магазина
//                    rc = 31;
//                    Order[] shopOrders = allOrders.GetShopOrders(shop.Id);
//                    if (shopOrders == null || shopOrders.Length <= 0)
//                        continue;

//                    // 3.2. Выбираем курьров магазина
//                    rc = 32;
//                    Courier[] shopCouriers = allCouriers.GetShopCouriers(shop.Id);

//                    // 3.3 Строим отгрузки для магазина
//                    rc = 33;
//                    CourierDeliveryInfo[] assembledOrders;
//                    CourierDeliveryInfo[] receiptedOrders;
//                    Order[] undeliveredOrders;
//                    int rc1 = CreateShopDeliveries(shop, shopOrders, shopCouriers, out assembledOrders, out receiptedOrders, out undeliveredOrders);
//                    if (rc1 != 0)
//                        continue;

//                    // 3.4 Отправляем отгрузки с не собранными товарами
//                    rc = 34;
//                    if (receiptedOrders != null && receiptedOrders.Length > 0)
//                    {
//                        rc1 = SendReceiptedOrders(receiptedOrders);
//                    }

//                    // 3.5 Обрабатываем отгрузки, состоящие целиком из собранных заказов
//                    rc = 35;
//                    if (assembledOrders != null && assembledOrders.Length > 0)
//                    {
//                        QueueItem[] deliveryQueueItems;
//                        rc1 = SendAssembledOrders(assembledOrders, out deliveryQueueItems);
//                        if (deliveryQueueItems != null && deliveryQueueItems.Length > 0)
//                        {
//                            if (itemCount + deliveryQueueItems.Length >= allQueueItems.Length)
//                            {
//                                Array.Resize(ref allQueueItems, allQueueItems.Length + 100 * deliveryQueueItems.Length);
//                            }

//                            deliveryQueueItems.CopyTo(allQueueItems, itemCount);
//                            itemCount += deliveryQueueItems.Length;
//                        }
//                    }

//                    // 3.6 Отправляем информацию о заказах, которые не могут быть доставлены в срок
//                    rc = 36;
//                    if (undeliveredOrders != null && undeliveredOrders.Length > 0)
//                    {
//                        //for (int k = 0; k < undeliveredOrders.Length; k++)
//                        //{
//                        //    if (undeliveredOrders[k].DeliveryTimeTo > DateTime.Now)
//                        //    {
//                        //        if (undeliveredOrders[k].Id != 8656859)
//                        //            k = k;
//                        //    }
//                        //}

//                        rc1 = SendUndeliveryOrders(undeliveredOrders);
//                    }
//                }

//                // 4. Сливаем построенные элементы с существующими элементами для других магазинов
//                rc = 4;
//                if (queue != null && queue.Count > 0 && queue.IsCreated)
//                {
//                    // 4.1 Выбираем ID-магазинов
//                    rc = 41;
//                    int[] shopId = new int[shops.Length];
//                    for (int i = 0; i < shops.Length; i++)
//                    {
//                        shopId[i] = shops[i].Id;
//                    }

//                    // 4.2 Сортируем ID магазинов
//                    rc = 42;
//                    Array.Sort(shopId);

//                    // 4.3. Добавляем существующие элементы других магазинов
//                    rc = 43;
//                    for (int i = queue.ItemIndex; i < queue.Count; i++)
//                    {
//                        QueueItem queueItem = queue.Items[i];
//                        if (queueItem.Delivery != null && queueItem.Delivery.FromShop != null)
//                        {
//                            if (Array.BinarySearch(shopId, queueItem.Delivery.FromShop.Id) < 0)
//                            {
//                                if (itemCount >= allQueueItems.Length)
//                                {
//                                    Array.Resize(ref allQueueItems, allQueueItems.Length + queue.Count - queue.ItemIndex);
//                                }

//                                allQueueItems[itemCount++] = queueItem;
//                            }
//                        }
//                    }
//                }

//                // 5. Создаём очередь на отгррузки и запускаем таймер очереди
//                rc = 5;
//                if (itemCount > 0)
//                {
//                    Array.Resize(ref allQueueItems, itemCount);
//                    deliveryQueue = new EventQueue();
//                    Helper.WriteInfoToLog($"Создание очереди отгрузок ({itemCount})");
//                    int rc1 = deliveryQueue.Create(allQueueItems);
//                    if (rc1 == 0)
//                    {
//                        QueueItem queueItem = deliveryQueue.GetCurrentItem();
//                        if (queueItem != null)
//                        {
//                            DateTime currentTime = DateTime.Now;
//                            double interval = 500;
//                            if (currentTime < queueItem.EventTime)
//                            {
//                                interval = (queueItem.EventTime - currentTime).TotalMilliseconds;
//                                if (interval < 500)
//                                    interval = 500;
//                            }

//                            queueTimer.Interval = interval;
//                            queueTimer.Start();
//                        }
//                    }

//                    PrintQueue(deliveryQueue);
//                }

//                // 5. Выход - Ok
//                rc = 0;
//                return rc;
//            }
//            catch
//            {
//                return rc;
//            }
//            finally
//            {
//                queue = deliveryQueue;
//            }
//        }

//        /// <summary>
//        /// Создание отгрузок заказов магазина
//        /// </summary>
//        /// <param name="shop">Магазин</param>
//        /// <param name="shopOrders">Отгружаемые заказы</param>
//        /// <param name="shopCouriers">Доступные курьеры и такси для доставки заказов</param>
//        /// <param name="assembledOrders">Отгрузки покрывающие все собранные заказы</param>
//        /// <param name="receiptedOrders">Отгрузки, в которые могут попасть поступившие, но не собранные заказы</param>
//        /// <param name="undeliveredOrders">Заказы, которые не могут быть доставлены в срок</param>
//        /// <returns></returns>
//        private int CreateShopDeliveries(Shop shop, Order[] shopOrders, Courier[] shopCouriers, out CourierDeliveryInfo[] assembledOrders, out CourierDeliveryInfo[] receiptedOrders, out Order[] undeliveredOrders)
//        {
//            // 1. Инициализация
//            int rc = 1;
//            assembledOrders = null;
//            receiptedOrders = null;
//            undeliveredOrders = null;

//            try
//            {
//                return salesmanSolution.CreateShopDeliveries(shop, shopOrders, shopCouriers, out assembledOrders, out receiptedOrders, out undeliveredOrders);
//            }
//            catch
//            {
//                return rc;
//            }
//        }

//        /// <summary>
//        /// Передачу серверу возможных отгрузок с
//        /// поступившими, но не собранными заказами
//        /// </summary>
//        /// <param name="receiptedOrders">Отгрузки с поступившими, но не собранными заказами</param>
//        /// <returns>0 - отгрузки переданы серверу; иначе - отгрузки серверу не переданы</returns>
//        private int SendReceiptedOrders(CourierDeliveryInfo[] receiptedOrders)
//        {
//            // 1. Инициализация
//            int rc = 1;

//            try
//            {
//                // 2. Проверяем исходные данные
//                rc = 2;
//                if (receiptedOrders == null || receiptedOrders.Length <= 0)
//                    return rc;

//                // 3. Строим данные для отправки на сервер
//                rc = 3;
//                BeginShipment.Shipment[] shipments = new BeginShipment.Shipment[receiptedOrders.Length];

//                for (int i = 0; i < receiptedOrders.Length; i++)
//                {
//                    // 3.1 Извлекаем отгрузку
//                    rc = 31;
//                    CourierDeliveryInfo delivery = receiptedOrders[i];

//                    // 3.2 Добавляем отгрузку в данные ззапроса на сервер
//                    rc = 32;
//                    BeginShipment.Shipment shipment = new BeginShipment.Shipment();
//                    shipment.id = Guid.NewGuid().ToString();
//                    shipment.status = 0;
//                    shipment.shop_id = delivery.FromShop.Id;
//                    shipment.courier_id = delivery.DeliveryCourier.Id;
//                    shipment.date_target_end = delivery.EndDeliveryInterval;
//                    shipment.info = CreateDeliveryInfo(delivery);

//                    // date_target
//                    if (delivery.OrderCost <= delivery.DeliveryCourier.AverageOrderCost)
//                    {
//                        shipment.date_target = delivery.StartDelivery;
//                    }
//                    else
//                    {
//                        DateTime targetTime;
//                        if (delivery.DeliveryCourier.IsTaxi)
//                        {
//                            targetTime = delivery.EndDeliveryInterval.AddMinutes(-config.functional_parameters.taxi_alert_interval);
//                        }
//                        else
//                        {
//                            targetTime = delivery.EndDeliveryInterval.AddMinutes(-config.functional_parameters.courier_alert_interval);
//                        }

//                        if (targetTime < delivery.StartDeliveryInterval)
//                            targetTime = delivery.StartDeliveryInterval;
//                        shipment.date_target = targetTime;
//                    }

//                    // delivery_service_id
//                    switch (delivery.DeliveryCourier.CourierType.VechicleType)
//                    {
//                        case CourierVehicleType.YandexTaxi:
//                            shipment.delivery_service_id = 14;
//                            break;
//                        case CourierVehicleType.GettTaxi:
//                            shipment.delivery_service_id = 12;
//                            break;
//                        default:
//                            shipment.delivery_service_id = 4;
//                            break;
//                    }

//                    // orders
//                    int[] orderId = new int[delivery.OrderCount];
//                    for (int j = 0; j < orderId.Length; j++)
//                    {
//                        orderId[j] = delivery.Orders[j].Id;
//                    }

//                    shipment.orders = orderId;

//                    shipments[i] = shipment;
//                }

//                // 4. Отправляем запрос на сервер
//                rc = 4;
//                int rc1 = BeginShipment.Begin(shipments);
//                if (rc1 != 0)
//                    return rc = 1000 * rc + rc1;

//                // 5. Выход - Ok
//                rc = 0;
//                return rc;
//            }
//            catch
//            {
//                return rc;
//            }
//        }

//        /// <summary>
//        /// Передача на сервер отгрузок с собранными товарами
//        /// и построение элементов очереди для отложенных отгрузок
//        /// </summary>
//        /// <param name="assembledOrders">Отгрузки с целиком собранными заказами</param>
//        /// <param name="deliveryQueueItems">Отгрузки с целиком собранными заказами</param>
//        /// <returns>0 - отгрузки переданы серверу; иначе - отгрузки серверу не переданы</returns>
//        private int SendAssembledOrders(CourierDeliveryInfo[] assembledOrders, out QueueItem[] deliveryQueueItems)
//        {
//            // 1. Инициализация
//            int rc = 1;
//            deliveryQueueItems = null;

//            try
//            {
//                // 2. Проверяем исходные данные
//                rc = 2;
//                if (assembledOrders == null || assembledOrders.Length <= 0)
//                    return rc;

//                // 3. Строим данные для отправки на сервер
//                rc = 3;
//                BeginShipment.Shipment[] shipments = new BeginShipment.Shipment[assembledOrders.Length];
//                int shipmentCount = 0;

//                CourierDeliveryInfo[] sentOrders = new CourierDeliveryInfo[assembledOrders.Length];
//                int sentCount = 0;

//                QueueItem[] queueItems = new QueueItem[assembledOrders.Length];
//                int itemCount = 0;

//                for (int i = 0; i < assembledOrders.Length; i++)
//                {
//                    // 3.1 Извлекаем отгрузку
//                    rc = 31;
//                    CourierDeliveryInfo delivery = assembledOrders[i];
//                    if (delivery.IsCompleted())
//                        continue;

//                    // 3.2 Проверяем, что отгрузить нужно прямо сейчас
//                    rc = 32;
//                    DateTime eventTime;
//                    if (delivery.DeliveryCourier.IsTaxi)
//                    {
//                        eventTime = delivery.EndDeliveryInterval.AddMinutes(-config.functional_parameters.taxi_alert_interval);
//                    }
//                    else
//                    {
//                        eventTime = delivery.EndDeliveryInterval.AddMinutes(-config.functional_parameters.courier_alert_interval);
//                    }

//                    if (eventTime < delivery.StartDeliveryInterval)
//                        eventTime = delivery.StartDeliveryInterval;
//                    DateTime currentTime = DateTime.Now;

//                    if ((eventTime <= currentTime) ||
//                        (!delivery.DeliveryCourier.IsTaxi && delivery.OrderCost <= delivery.DeliveryCourier.AverageOrderCost && delivery.StartDeliveryInterval >= currentTime))
//                    //(delivery.OrderCost <= delivery.DeliveryCourier.AverageOrderCost && delivery.StartDeliveryInterval >= currentTime))
//                    {
//                        // 3.3 Построение данных для отгрузки прямо сейчас
//                        rc = 33;
//                        sentOrders[sentCount++] = delivery;
//                        BeginShipment.Shipment shipment = new BeginShipment.Shipment();
//                        shipment.id = Guid.NewGuid().ToString();
//                        shipment.status = 1;
//                        shipment.shop_id = delivery.FromShop.Id;
//                        shipment.courier_id = delivery.DeliveryCourier.Id;
//                        shipment.date_target = delivery.StartDeliveryInterval;
//                        shipment.date_target_end = delivery.EndDeliveryInterval;
//                        shipment.info = CreateDeliveryInfo(delivery);
                        
//                        switch (delivery.DeliveryCourier.CourierType.VechicleType)
//                        {
//                            case CourierVehicleType.YandexTaxi:
//                                shipment.delivery_service_id = 14;
//                                break;
//                            case CourierVehicleType.GettTaxi:
//                                shipment.delivery_service_id = 12;
//                                break;
//                            default:
//                                shipment.delivery_service_id = 4;
//                                break;
//                        }

//                        // orders
//                        int[] orderId = new int[delivery.OrderCount];
//                        for (int j = 0; j < orderId.Length; j++)
//                        {
//                            orderId[j] = delivery.Orders[j].Id;
//                        }

//                        shipment.orders = orderId;
//                        shipments[shipmentCount++] = shipment;
//                    }
//                    else
//                    {
//                        queueItems[itemCount++] = new QueueItem(eventTime, QueueItemType.CourierDeliveryAlert, delivery);
//                    }
//                }

//                Array.Resize(ref queueItems, itemCount);
//                deliveryQueueItems = queueItems;

//                // 4. Отправляем запрос на сервер
//                rc = 4;
//                if (shipmentCount > 0)
//                {
//                    Array.Resize(ref shipments, shipmentCount);
//                    int rc1 = BeginShipment.Begin(shipments);
//                    if (rc1 != 0)
//                        return rc = 1000 * rc + rc1;

//                    // 5. Помечаем заказы, как отгруженные в отгрузках отправленных на сервер
//                    rc = 5;
//                    for (int i = 0; i < sentCount; i++)
//                    {
//                        sentOrders[i].SetCompleted(true);
//                    }
//                }

//                // 6. Выход - Ok
//                rc = 0;
//                return rc;
//            }
//            catch
//            {
//                return rc;
//            }
//        }

//        /// <summary>
//        /// Передачу серверу заказов которые не могут быть отгружены в срок
//        /// </summary>
//        /// <param name="undeliveredOrders">Заказы, которые не могут быть отгружены в срок</param>
//        /// <returns>0 - отгрузки переданы серверу; иначе - отгрузки серверу не переданы</returns>
//        private int SendUndeliveryOrders(Order[] undeliveredOrders)
//        {
//            // 1. Инициализация
//            int rc = 1;

//            try
//            {
//                // 2. Проверяем исходные данные
//                rc = 2;
//                if (undeliveredOrders == null || undeliveredOrders.Length <= 0)
//                    return rc;

//                int shopId = undeliveredOrders[0].ShopId;

//                // 3. Строим данные для отправки на сервер
//                rc = 3;
//                BeginShipment.Shipment shipment = new BeginShipment.Shipment();
//                shipment.id = Guid.NewGuid().ToString();
//                shipment.status = 2;
//                shipment.shop_id = shopId;
//                shipment.courier_id = 0;
//                shipment.date_target = DateTime.Now;
//                shipment.date_target_end = shipment.date_target;
//                shipment.delivery_service_id = 0;
//                //shipment.info = new BeginShipment.DeliveryInfo();

//                // orders
//                int[] orderId = new int[undeliveredOrders.Length];
//                for (int j = 0; j < orderId.Length; j++)
//                {
//                    orderId[j] = undeliveredOrders[j].Id;
//                }

//                shipment.orders = orderId;

//                // 4. Отправляем запрос на сервер
//                rc = 4;
//                int rc1 = BeginShipment.Begin(shipment);
//                if (rc1 != 0)
//                    return rc = 1000 * rc + rc1;

//                // 5. Выход - Ok
//                rc = 0;
//                return rc;
//            }
//            catch
//            {
//                return rc;
//            }
//        }

//        #endregion Refresh

//        #region QueueHandler

//        /// <summary>
//        /// Обработка очереди событий
//        /// </summary>
//        /// <param name="queue">Очередь</param>
//        /// <returns>0 - очередь обработана; иначе - очередь не обработана</returns>
//        private int QueueHandler(EventQueue queue)
//        {
//            // 1. Иницализация
//            int rc = 1;

//            try
//            {
//                // 2. Проверяем исходные данные
//                rc = 2;
//                if (!IsCreated)
//                    return rc;
//                if (queue == null)
//                    return rc;

//                // 3. Извлекаем текущий элемент очереди
//                rc = 3;
//                QueueItem item = queue.GetCurrentItem();
//                if (item == null)
//                    return rc;

//                DateTime toTime = DateTime.Now.AddMilliseconds(config.functional_parameters.event_time_interval);
//                if (toTime < item.EventTime)
//                    toTime = item.EventTime.AddMilliseconds(config.functional_parameters.event_time_interval);

//                // 4. Извлекаем все события, очередь которых наступила
//                rc = 4;
//                QueueItem[] activeEvents = queue.MoveToTime(toTime);
//                if (activeEvents == null || activeEvents.Length <= 0)
//                    return rc;

//                // 5. Обрабатываем выбранные события
//                rc = 5;
//                BeginShipment.Shipment[] shipments = new BeginShipment.Shipment[activeEvents.Length];
//                int count = 0;

//                for (int i = 0; i < activeEvents.Length; i++)
//                {
//                    // 5.1 Извлекаем отгрузку
//                    rc = 51;
//                    QueueItem activeEvent = activeEvents[i];
//                    if (activeEvent == null || activeEvent.Delivery == null)
//                        continue;

//                    CourierDeliveryInfo delivery = activeEvent.Delivery;
//                    if (delivery.IsCompleted())
//                        continue;

//                    // 5.2 Строим данные для запроса
//                    rc = 52;
//                    BeginShipment.Shipment shipment = new BeginShipment.Shipment();
//                    shipment.id = Guid.NewGuid().ToString();
//                    shipment.status = 1;
//                    shipment.shop_id = delivery.FromShop.Id;
//                    shipment.courier_id = delivery.DeliveryCourier.Id;
//                    shipment.date_target = delivery.StartDeliveryInterval;
//                    shipment.date_target_end = delivery.EndDeliveryInterval;
//                    shipment.info = CreateDeliveryInfo(delivery);

//                    // delivery_service_id
//                    switch (delivery.DeliveryCourier.CourierType.VechicleType)
//                    {
//                        case CourierVehicleType.YandexTaxi:
//                            shipment.delivery_service_id = 14;
//                            break;
//                        case CourierVehicleType.GettTaxi:
//                            shipment.delivery_service_id = 12;
//                            break;
//                        default:
//                            shipment.delivery_service_id = 4;
//                            break;
//                    }

//                    // orders
//                    int[] orderId = new int[delivery.OrderCount];
//                    for (int j = 0; j < orderId.Length; j++)
//                    {
//                        orderId[j] = delivery.Orders[j].Id;
//                    }

//                    shipment.orders = orderId;
//                    shipments[count++] = shipment;
//                }

//                Array.Resize(ref shipments, count);

//                // 6. Отправляем запрос на сервер
//                rc = 6;
//                if (count > 0)
//                {
//                    int rc1 = BeginShipment.Begin(shipments);
//                    if (rc1 != 0)
//                        return rc = 1000 * rc + rc1;

//                    // 7. Помечаем заказы, как отгруженные
//                    rc = 7;

//                    for (int i = 0; i < activeEvents.Length; i++)
//                    {
//                        QueueItem activeEvent = activeEvents[i];
//                        if (activeEvent != null && activeEvent.Delivery != null)
//                        {
//                            activeEvent.Delivery.SetCompleted(true);
//                        }
//                    }
//                }

//                // 7. Выход - Ok
//                rc = 0;
//                return rc;
//            }
//            catch
//            {
//                return rc;
//            }
//        }

//        #endregion QueueHandler

//        /// <summary>
//        /// Выборка информации об отгрузке для отправки на сервер
//        /// </summary>
//        /// <param name="delivery">Отгрузка</param>
//        /// <returns>Данные об отгрузке для сервера</returns>
//        private static BeginShipment.DeliveryInfo CreateDeliveryInfo(CourierDeliveryInfo delivery)
//        {
//            // 1. Инициализация

//            try
//            {
//                // 2. Проверяем исходные данные
//                if (delivery == null)
//                    return null;

//                // 3. Инициализируем DeliveryInfo
//                BeginShipment.DeliveryInfo deliveryInfo = new BeginShipment.DeliveryInfo();
//                deliveryInfo.calculationTime = delivery.CalculationTime;
//                deliveryInfo.delivery_time = delivery.DeliveryTime;
//                deliveryInfo.end_delivery_interval = delivery.EndDeliveryInterval;
//                deliveryInfo.execution_time = delivery.ExecutionTime;
//                deliveryInfo.is_loop = delivery.IsLoop;

//                if (delivery.NodeInfo != null && delivery.NodeInfo.Length > 0)
//                {
//                    BeginShipment.NodeInfo[] nodeInfo = new BeginShipment.NodeInfo[delivery.NodeInfo.Length];
//                    for (int i = 0; i < delivery.NodeInfo.Length; i++)
//                    {
//                        BeginShipment.NodeInfo ni = new BeginShipment.NodeInfo();
//                        ni.distance = delivery.NodeInfo[i].X;
//                        ni.duration = delivery.NodeInfo[i].Y;
//                        nodeInfo[i] = ni;
//                    }

//                    deliveryInfo.nodeInfo = nodeInfo;
//                }


//                deliveryInfo.node_delivery_time = delivery.NodeDeliveryTime;
//                deliveryInfo.reserve_time = delivery.ReserveTime;
//                deliveryInfo.start_delivery_interval = delivery.StartDeliveryInterval;
//                deliveryInfo.sum_cost = delivery.Cost;
//                deliveryInfo.weight = delivery.Weight;

//                // 4. Возвращаем результат
//                return deliveryInfo;
//            }
//            catch
//            {
//                return null;
//            }
//        }

//        #region Переодическое информирование сервера об очереди отрузок

//        /// <summary>
//        /// Передача на сервер информации находящейся в очереди
//        /// </summary>
//        /// <param name="queue">Очередь</param>
//        /// <returns>0 - очередь обработана; иначе - очередь не обработана</returns>
//        private int QueueInfoToServer(EventQueue queue)
//        {
//            // 1. Иницализация
//            int rc = 1;

//            try
//            {
//                // 2. Проверяем исходные данные
//                rc = 2;
//                if (!IsCreated)
//                    return rc;
//                if (queue == null || !queue.IsCreated)
//                    return rc;

//                if (queue.Count <= 0)
//                    return rc;

//                // 5. Обрабатываем выбранные события
//                rc = 5;
//                BeginShipment.Shipment[] shipments = new BeginShipment.Shipment[queue.Count];
//                int count = 0;

//                for (int i = 0; i < queue.Count; i++)
//                {
//                    // 5.1 Извлекаем отгрузку
//                    rc = 51;
//                    QueueItem queueItem = queue.Items[i];
//                    if (queueItem == null || queueItem.Delivery == null)
//                        continue;

//                    CourierDeliveryInfo delivery = queueItem.Delivery;
//                    if (delivery.IsCompleted())
//                        continue;

//                    // 5.2 Строим данные для запроса
//                    rc = 52;
//                    BeginShipment.Shipment shipment = new BeginShipment.Shipment();
//                    shipment.id = Guid.NewGuid().ToString();
//                    shipment.status = 0;
//                    shipment.shop_id = delivery.FromShop.Id;
//                    shipment.courier_id = delivery.DeliveryCourier.Id;
//                    shipment.date_target = queueItem.EventTime;
//                    shipment.date_target_end = delivery.EndDeliveryInterval;
//                    shipment.info = CreateDeliveryInfo(delivery);

//                    // delivery_service_id
//                    switch (delivery.DeliveryCourier.CourierType.VechicleType)
//                    {
//                        case CourierVehicleType.YandexTaxi:
//                            shipment.delivery_service_id = 14;
//                            break;
//                        case CourierVehicleType.GettTaxi:
//                            shipment.delivery_service_id = 12;
//                            break;
//                        default:
//                            shipment.delivery_service_id = 4;
//                            break;
//                    }

//                    // orders
//                    int[] orderId = new int[delivery.OrderCount];
//                    for (int j = 0; j < orderId.Length; j++)
//                    {
//                        orderId[j] = delivery.Orders[j].Id;
//                    }

//                    shipment.orders = orderId;
//                    shipments[count++] = shipment;
//                }

//                if (count < shipments.Length)
//                {
//                    Array.Resize(ref shipments, count);
//                }

//                // 6. Отправляем запрос на сервер
//                rc = 6;
//                if (count > 0)
//                {
//                    int rc1 = BeginShipment.Begin(shipments);
//                    if (rc1 != 0)
//                        return rc = 1000 * rc + rc1;
//                }

//                // 7. Выход - Ok
//                rc = 0;
//                return rc;
//            }
//            catch
//            {
//                return rc;
//            }
//        }

//        #endregion Переодическое информирование сервера об очереди отрузок 

//        #region IDisposable Support

//        private bool disposedValue = false; // To detect redundant calls

//        protected virtual void Dispose(bool disposing)
//        {
//            if (!disposedValue)
//            {
//                if (disposing)
//                {
//                    if (requestTimer != null)
//                    {
//                        requestTimer.Elapsed -= RequestTimer_Elapsed;
//                        DisposeTimer(requestTimer);
//                        requestTimer = null;
//                    }

//                    if (queueTimer != null)
//                    {
//                        queueTimer.Elapsed -= QueueTimer_Elapsed;
//                        DisposeTimer(queueTimer);
//                        queueTimer = null;
//                    }

//                    if (queueInfoTimer != null)
//                    {
//                        queueInfoTimer.Elapsed -= QueueInfoTimer_Elapsed;
//                        DisposeTimer(queueInfoTimer);
//                        queueInfoTimer = null;
//                    }

//                    if (syncMutex != null)
//                    {
//                        DisposeMutex(syncMutex);
//                        syncMutex = null;
//                    }

//                    config = null;
//                    geoCache = null;
//                    allCouriers = null;
//                    allOrders = null;
//                    allShops = null;
//                    queue = null;
//                    salesmanSolution = null;

//                    // TODO: dispose managed state (managed objects).
//                }

//                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
//                // TODO: set large fields to null.

//                IsCreated = false;
//                disposedValue = true;
//            }
//        }

//        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
//        // ~FixedService() {
//        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
//        //   Dispose(false);
//        // }

//        // This code added to correctly implement the disposable pattern.
//        public void Dispose()
//        {
//            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
//            Dispose(true);
//            // TODO: uncomment the following line if the finalizer is overridden above.
//            // GC.SuppressFinalize(this);
//        }

//        #endregion IDisposable Support

//        /// <summary>
//        /// Печать активных элементов очереди
//        /// </summary>
//        /// <param name="queue"></param>
//        private static void PrintQueue(EventQueue queue)
//        {
//            try
//            {
//                // 2. Проверяем исходные данные
//                if (queue == null || queue.Count <= 0)
//                    return;

//                // 3. Печатаем заголовок
//                Helper.WriteInfoToLog("Состояние очереди");

//                // 4. Печатаем все активные элементы
//                BeginShipment.Shipment shipment = new BeginShipment.Shipment();
//                JsonSerializer serializer = JsonSerializer.Create();

//                for (int i = queue.ItemIndex; i < queue.Count; i++)
//                {
//                    // 4.1 Извлекаем элемент очереди
//                    QueueItem queueItem = queue.Items[i];
//                    CourierDeliveryInfo delivery = queueItem.Delivery;

//                    if (delivery == null)
//                        continue;

//                    // 4.2 Заполняем информацию об отгрузке
//                    shipment.id = Guid.NewGuid().ToString();
//                    shipment.status = 0;
//                    shipment.shop_id = delivery.FromShop.Id;
//                    shipment.courier_id = delivery.DeliveryCourier.Id;
//                    shipment.date_target_end = delivery.EndDeliveryInterval;
//                    shipment.info = CreateDeliveryInfo(delivery);
//                    shipment.date_target = queueItem.EventTime;

//                    // delivery_service_id
//                    switch (delivery.DeliveryCourier.CourierType.VechicleType)
//                    {
//                        case CourierVehicleType.YandexTaxi:
//                            shipment.delivery_service_id = 14;
//                            break;
//                        case CourierVehicleType.GettTaxi:
//                            shipment.delivery_service_id = 12;
//                            break;
//                        default:
//                            shipment.delivery_service_id = 4;
//                            break;
//                    }

//                    // orders
//                    int[] orderId = new int[delivery.OrderCount];
//                    for (int j = 0; j < orderId.Length; j++)
//                    {
//                        orderId[j] = delivery.Orders[j].Id;
//                    }

//                    shipment.orders = orderId;

//                    // 4.3 Переводим в json-представление
//                    string json;
                    
//                    using (StringWriter sw = new StringWriter())
//                    {
//                        serializer.Serialize(sw, shipment);
//                        sw.Close();
//                        json = sw.ToString();
//                    }

//                    // 4.4 Печатаем в лог
//                    Helper.WriteInfoToLog($"   Элемент {i}: {json}");
//                }
//            }
//            catch
//            {   }
//        }
//    }
//}
