
//namespace LogisticsService.SalesmanTravelingProblem
//{
//    using LogisticsService.Couriers;
//    using LogisticsService.Locations;
//    using LogisticsService.Orders;
//    using LogisticsService.SalesmanTravelingProblem.PermutationsSupport;
//    using LogisticsService.Shops;
//    using System;
//    using System.Collections.Generic;
//    using System.Drawing;
//    using System.Linq;
//    using System.Threading.Tasks;

//    /// <summary>
//    /// Решение задачи доставки заказов магазина
//    /// с помощью имеющихся курьеров и такси
//    /// </summary>
//    public class SalesmanSolution
//    {
//        /// <summary>
//        /// Менеджер расстояний и времени движения между точками
//        /// </summary>
//        private LocationManager locationManager;

//        /// <summary>
//        /// Хранилище перестановок
//        /// </summary>
//        private Permutations permutations;

//        /// <summary>
//        /// Параметрический конструктор класса SalesmanSolution
//        /// </summary>
//        /// <param name="locManager">Менеджер расстояний и времени движения между точками</param>
//        public SalesmanSolution(LocationManager locManager)
//        {
//            locationManager = locManager;
//            permutations = new Permutations();
//            for (int i = 1; i <= 8; i++)
//                permutations.GetPermutations(i);
//        }

//        ///// <summary>
//        ///// Создание отгрузок для заказов магазина
//        ///// </summary>
//        ///// <param name="shop">Магазин</param>
//        ///// <param name="shopOrders">Отгружаемые заказы</param>
//        ///// <param name="shopCouriers">Доступные курьеры и такси для доставки заказов</param>
//        ///// <param name="isLoop">Флаг возврата в магазин: true - требуется возврат в магазин; false - отгрузка завершается после вручения последнего заказа</param>
//        ///// <param name="assembledOrders">Отгрузки покрывающие все собранные заказы</param>
//        ///// <param name="receiptedOrders">Отгрузки, в которые могут попасть поступившие, но не собранные заказы</param>
//        ///// <param name="undeliveredOrders">Заказы, которые не могут быть доставлены в срок</param>
//        ///// <returns>0 - отгрузки созданы; иначе - отгрузки не созданы</returns>
//        //public int CreateShopDeliveriesOld(Shop shop, Order[] shopOrders, Courier[] shopCouriers, bool isLoop, out CourierDeliveryInfo[] assembledOrders, out CourierDeliveryInfo[] receiptedOrders, out Order[] undeliveredOrders)
//        //{
//        //    // 1. Инициализация
//        //    int rc = 1;
//        //    int rc1 = 1;
//        //    assembledOrders = null;
//        //    receiptedOrders = null;
//        //    undeliveredOrders = null;

//        //    try
//        //    {
//        //        // 2. Проверяем исходные 
//        //        rc = 2;
//        //        if (locationManager == null)
//        //            return rc;
//        //        if (shop == null)
//        //            return rc;
//        //        if (shopCouriers == null || shopCouriers.Length <= 0)
//        //            return rc;
//        //        if (shopOrders == null || shopOrders.Length <= 0)
//        //            return rc;                

//        //        // 3. Выбираем по одному курьру каждого типа среди заданных
//        //        rc = 3;
//        //        Dictionary<CourierVehicleType, Courier> allTypeCouriers = new Dictionary<CourierVehicleType, Courier>(8);
//        //        for (int i = 0; i < shopCouriers.Length; i++)
//        //        {
//        //            Courier courier = shopCouriers[i];
//        //            if (courier.Status != CourierStatus.Ready)
//        //                continue;

//        //            if (!allTypeCouriers.ContainsKey(courier.CourierType.VechicleType))
//        //            {
//        //                if (courier.IsTaxi)
//        //                {
//        //                    allTypeCouriers.Add(courier.CourierType.VechicleType, courier);
//        //                }
//        //                else
//        //                {
//        //                    Courier courierClone = courier.Clone();
//        //                    courierClone.WorkStart = TimeSpan.Zero;
//        //                    courierClone.WorkStart = TimeSpan.FromHours(24);
//        //                    courierClone.LunchTimeStart = TimeSpan.Zero;
//        //                    courierClone.LunchTimeEnd = TimeSpan.Zero;
//        //                    allTypeCouriers.Add(courierClone.CourierType.VechicleType, courierClone);
//        //                }
//        //            }
//        //        }

//        //        if (allTypeCouriers.Count <= 0)
//        //            return rc;

//        //        // 4. Обеспечиваем наличие всех необходимых расстояний и времени движения между парами точек в двух направлениях
//        //        rc = 4;
//        //        Courier[] allCouriers = new Courier[allTypeCouriers.Count];
//        //        allTypeCouriers.Values.CopyTo(allCouriers, 0);

//        //        int[] locationIndex = new int[shopOrders.Length + 1];
//        //        locationIndex[0] = shop.LocationIndex;
//        //        for (int i = 0; i < shopOrders.Length; i++)
//        //        {
//        //            locationIndex[i + 1] = shopOrders[i].LocationIndex;
//        //        }

//        //        for (int i = 0; i < allTypeCouriers.Count; i++)
//        //        {
//        //            rc1 = locationManager.PutLocationInfo(locationIndex, allCouriers[i].CourierType.VechicleType);
//        //            if (rc1 != 0)
//        //                return rc = 100 * rc + rc1;
//        //        }

//        //        // 5. Запускаем построение всех возможных путей всеми возможными способами
//        //        //    Каждый способ доставки обрабатывается в отдельном потоке
//        //        rc = 5;
//        //        DateTime calcTime = DateTime.Now;
//        //        //DateTime calcTime = new DateTime(2020, 11, 4, 18, 50, 0);
//        //        Task<int>[] tasks = new Task<int>[allTypeCouriers.Count];
//        //        CourierDeliveryInfo[][] taskDeliveries = new CourierDeliveryInfo[allTypeCouriers.Count][];

//        //        int rcz = CreateShopDeliveriesEx(shop, shopOrders, allCouriers[2], isLoop, calcTime, out taskDeliveries[2]);

//        //        for (int i = 0; i < tasks.Length; i++)
//        //        {
//        //            tasks[i] = Task.Run(() => CreateShopDeliveriesEx(shop, shopOrders, allCouriers[i], isLoop, calcTime, out taskDeliveries[i]));
//        //        }

//        //        Task.WaitAll(tasks);

//        //        // 6. Объединяем все построенные отгрузки
//        //        rc = 6;
//        //        int deliveryCount = 0;
//        //        CourierDeliveryInfo[] allDeliveries;

//        //        for (int i = 0; i < tasks.Length; i++)
//        //        {
//        //            if (tasks[i].Result == 0)
//        //            {
//        //                if (taskDeliveries[i] != null)
//        //                    deliveryCount += taskDeliveries[i].Length;
//        //            }
//        //        }

//        //        if (deliveryCount <= 0)
//        //        {
//        //            undeliveredOrders = shopOrders;
//        //            return rc = 0;
//        //        }

//        //        allDeliveries = new CourierDeliveryInfo[deliveryCount];
//        //        deliveryCount = 0;

//        //        for (int i = 0; i < tasks.Length; i++)
//        //        {
//        //            if (tasks[i].Result == 0)
//        //            {
//        //                if (taskDeliveries[i] != null)
//        //                {
//        //                    taskDeliveries[i].CopyTo(allDeliveries, deliveryCount);
//        //                    deliveryCount += taskDeliveries[i].Length;
//        //                }
//        //            }
//        //        }

//        //        // 7. Сортируем по средней стоимости доставки одного заказа
//        //        rc = 7;
//        //        Array.Sort(allDeliveries, CmpareByOrderCost);

//        //        // 8. Присваиваем заказам индексы
//        //        rc = 8;
//        //        for (int i = 0; i < shopOrders.Length; i++)
//        //        {
//        //            shopOrders[i].Index = i;
//        //        }

//        //        // 9. Строим покрытие из всех построенных отгрузок
//        //        rc = 9;
//        //        CourierDeliveryInfo[] deiveryCover;
//        //        bool[] orderCoverMap;
//        //        //rc1 = BuildDeliveryCover(allDeliveries, shopOrders.Length, false, out deiveryCover, out orderCoverMap);
//        //        rc1 = BuildDeliveryCoverEx(shopCouriers, allDeliveries, shopOrders.Length, false, locationManager, out deiveryCover, out orderCoverMap);
//        //        if (rc1 != 0)
//        //            return rc = 100 * rc + rc1;

//        //        // 10. Если ли в построенном покрытии не собранные заказы
//        //        rc = 10;
//        //        int receiptedCount = 0;
//        //        int undeliveredCount = 0;

//        //        for (int i = 0; i < orderCoverMap.Length; i++)
//        //        {
//        //            if (!orderCoverMap[i])
//        //            {
//        //                undeliveredCount++;
//        //            }
//        //            else if (shopOrders[i].Status == OrderStatus.Receipted)
//        //            {
//        //                receiptedCount++;
//        //            }
//        //        }

//        //        // 11. Если в покрытии есть не собранные заказы
//        //        rc = 11;
//        //        if (receiptedCount > 0)
//        //        {
//        //            receiptedOrders = new CourierDeliveryInfo[receiptedCount];
//        //            receiptedCount = 0;

//        //            for (int i = 0; i < deiveryCover.Length; i++)
//        //            {
//        //                if (!deiveryCover[i].HasAssembledOnly)
//        //                {
//        //                    receiptedOrders[receiptedCount++] = deiveryCover[i];
//        //                }
//        //            }

//        //            if (receiptedCount < receiptedOrders.Length)
//        //            {
//        //                Array.Resize(ref receiptedOrders, receiptedCount);
//        //            }

//        //            rc1 = BuildDeliveryCoverEx(shopCouriers, allDeliveries, shopOrders.Length, true, locationManager, out deiveryCover, out orderCoverMap);
//        //        }

//        //        assembledOrders = deiveryCover;

//        //        // 12. Формируем список заказов которые не могут быть доставлены в срок
//        //        rc = 12;
//        //        undeliveredCount = 0;

//        //        for (int i = 0; i < shopOrders.Length; i++)
//        //        {
//        //            if (!orderCoverMap[i] && shopOrders[i].Status == OrderStatus.Assembled)
//        //                undeliveredCount++;
//        //        }

//        //        undeliveredOrders = new Order[undeliveredCount];
//        //        if (undeliveredCount > 0)
//        //        {
//        //            for (int i = 0; i < shopOrders.Length; i++)
//        //            {
//        //                undeliveredCount = 0;
//        //                if (!orderCoverMap[i] && shopOrders[i].Status == OrderStatus.Assembled)
//        //                {
//        //                    undeliveredOrders[undeliveredCount++] = shopOrders[i];
//        //                }
//        //            }
//        //        }

//        //        // 13. Выход - Ok
//        //        rc = 0;
//        //        return rc;
//        //    }
//        //    catch
//        //    {
//        //        return rc;
//        //    }
//        //}

//        /// <summary>
//        /// Создание отгрузок для заказов магазина
//        /// </summary>
//        /// <param name="shop">Магазин</param>
//        /// <param name="allOrdersOfShop">Заказы магазина, для которых создаются отгрузки</param>
//        /// <param name="shopCouriers">Доступные для доставки заказов курьеры и такси</param>
//        /// <param name="isLoop">Флаг возврата в магазин: true - требуется возврат в магазин; false - отгрузка завершается после вручения последнего заказа</param>
//        /// <param name="assembledOrders">Отгрузки покрывающие все собранные заказы</param>
//        /// <param name="receiptedOrders">Отгрузки, в которые могут попасть поступившие, но не собранные заказы</param>
//        /// <param name="undeliveredOrders">Заказы, которые не могут быть доставлены в срок</param>
//        /// <returns>0 - отгрузки созданы; иначе - отгрузки не созданы</returns>
//        public int CreateShopDeliveries(Shop shop, Order[] allOrdersOfShop, Courier[] shopCouriers, bool isLoop, out CourierDeliveryInfo[] assembledOrders, out CourierDeliveryInfo[] receiptedOrders, out Order[] undeliveredOrders)
//        {
//            // 1. Инициализация
//            int rc = 1;
//            int rc1 = 1;
//            assembledOrders = null;
//            receiptedOrders = null;
//            undeliveredOrders = null;

//            try
//            {
//                // 2. Проверяем исходные 
//                rc = 2;
//                if (locationManager == null)
//                    return rc;
//                if (shop == null)
//                    return rc;
//                if (shopCouriers == null || shopCouriers.Length <= 0)
//                    return rc;
//                if (allOrdersOfShop == null || allOrdersOfShop.Length <= 0)
//                    return rc;

//                // 3. Отбираем заказы, которые не могут быть доставлены в срок на данный момент
//                rc = 3;
//                DateTime calcTime = DateTime.Now;
//                //calcTime = new DateTime(2020, 11, 8, 10, 19, 0);
//                Order[] undelivOrders = new Order[allOrdersOfShop.Length];
//                Order[] shopOrders = new Order[allOrdersOfShop.Length];
//                int undeliveredCount = 0;
//                int orderCount = 0;

//                for (int i = 0; i <  allOrdersOfShop.Length; i++)
//                {
//                    Order order = allOrdersOfShop[i];
//                    if (calcTime > order.DeliveryTimeTo)
//                    {
//                        undelivOrders[undeliveredCount++] = order;
//                        Helper.WriteWarningToLog($"Undelivered By Time. CreateShopDeliveries. Shop {shop.Id}. Order {order.Id}, order.DeliveryTimeTo {order.DeliveryTimeTo}, calcTime {calcTime}");
//                    }
//                    else
//                    {
//                        shopOrders[orderCount++] = order;
//                    }
//                }

//                if (orderCount <= 0)
//                {
//                    if (undeliveredCount < undelivOrders.Length)
//                    {
//                        Array.Resize(ref undelivOrders, undeliveredCount);
//                    }
//                    undeliveredOrders = undelivOrders;
//                    return rc = 0;
//                }

//                if (orderCount < shopOrders.Length)
//                {
//                    Array.Resize(ref shopOrders, orderCount);
//                }

//                // 4. Выбираем по одному курьру каждого типа среди заданных
//                rc = 4;
//                Dictionary<CourierVehicleType, Courier> allTypeCouriers = new Dictionary<CourierVehicleType, Courier>(8);
//                for (int i = 0; i < shopCouriers.Length; i++)
//                {
//                    Courier courier = shopCouriers[i];
//                    if (courier.Status != CourierStatus.Ready)
//                        continue;

//                    if (!allTypeCouriers.ContainsKey(courier.CourierType.VechicleType))
//                    {
//                        if (courier.IsTaxi)
//                        {
//                            allTypeCouriers.Add(courier.CourierType.VechicleType, courier);
//                        }
//                        else
//                        {
//                            Courier courierClone = courier.Clone();
//                            courierClone.WorkStart = TimeSpan.Zero;
//                            courierClone.WorkEnd = new TimeSpan(23, 59, 59);

//                            courierClone.WorkEnd = TimeSpan.FromHours(23.99);
//                            courierClone.LunchTimeStart = TimeSpan.Zero;
//                            courierClone.LunchTimeEnd = TimeSpan.Zero;
//                            allTypeCouriers.Add(courierClone.CourierType.VechicleType, courierClone);
//                        }
//                    }
//                }

//                if (allTypeCouriers.Count <= 0)
//                    return rc;

//                // 5. Обеспечиваем наличие всех необходимых расстояний и времени движения между парами точек в двух направлениях
//                rc = 5;
//                Courier[] allCouriers = new Courier[allTypeCouriers.Count];
//                allTypeCouriers.Values.CopyTo(allCouriers, 0);

//                int[] locationIndex = new int[shopOrders.Length + 1];
//                locationIndex[0] = shop.LocationIndex;
//                for (int i = 0; i < shopOrders.Length; i++)
//                {
//                    locationIndex[i + 1] = shopOrders[i].LocationIndex;
//                }

//                for (int i = 0; i < allTypeCouriers.Count; i++)
//                {
//                    rc1 = locationManager.PutLocationInfo(locationIndex, allCouriers[i].CourierType.VechicleType);
//                    if (rc1 != 0)
//                        return rc = 100 * rc + rc1;
//                }

//                // 6. Запускаем построение всех возможных путей всеми возможными способами
//                //    Каждый способ доставки обрабатывается в отдельном потоке
//                rc = 6;
//                //DateTime calcTime = new DateTime(2020, 11, 4, 18, 50, 0);
//                Task<int>[] tasks = new Task<int>[allTypeCouriers.Count];
//                CourierDeliveryInfo[][] taskDeliveries = new CourierDeliveryInfo[allTypeCouriers.Count][];

//                rc1 = CreateShopDeliveriesEx(shop, shopOrders, allCouriers[0], isLoop, calcTime, out taskDeliveries[0]);

//                switch (allCouriers.Length)
//                {
//                    case 1:
//                        tasks[0] = Task.Run(() => CreateShopDeliveriesEx(shop, shopOrders, allCouriers[0], isLoop, calcTime, out taskDeliveries[0]));
//                        break;
//                    case 2:
//                        tasks[0] = Task.Run(() => CreateShopDeliveriesEx(shop, shopOrders, allCouriers[0], isLoop, calcTime, out taskDeliveries[0]));
//                        tasks[1] = Task.Run(() => CreateShopDeliveriesEx(shop, shopOrders, allCouriers[1], isLoop, calcTime, out taskDeliveries[1]));
//                        break;
//                    case 3:
//                        tasks[0] = Task.Run(() => CreateShopDeliveriesEx(shop, shopOrders, allCouriers[0], isLoop, calcTime, out taskDeliveries[0]));
//                        tasks[1] = Task.Run(() => CreateShopDeliveriesEx(shop, shopOrders, allCouriers[1], isLoop, calcTime, out taskDeliveries[1]));
//                        tasks[2] = Task.Run(() => CreateShopDeliveriesEx(shop, shopOrders, allCouriers[2], isLoop, calcTime, out taskDeliveries[2]));
//                        break;
//                    case 4:
//                        tasks[0] = Task.Run(() => CreateShopDeliveriesEx(shop, shopOrders, allCouriers[0], isLoop, calcTime, out taskDeliveries[0]));
//                        tasks[1] = Task.Run(() => CreateShopDeliveriesEx(shop, shopOrders, allCouriers[1], isLoop, calcTime, out taskDeliveries[1]));
//                        tasks[2] = Task.Run(() => CreateShopDeliveriesEx(shop, shopOrders, allCouriers[2], isLoop, calcTime, out taskDeliveries[2]));
//                        tasks[3] = Task.Run(() => CreateShopDeliveriesEx(shop, shopOrders, allCouriers[3], isLoop, calcTime, out taskDeliveries[3]));
//                        break;
//                    case 5:
//                        tasks[0] = Task.Run(() => CreateShopDeliveriesEx(shop, shopOrders, allCouriers[0], isLoop, calcTime, out taskDeliveries[0]));
//                        tasks[1] = Task.Run(() => CreateShopDeliveriesEx(shop, shopOrders, allCouriers[1], isLoop, calcTime, out taskDeliveries[1]));
//                        tasks[2] = Task.Run(() => CreateShopDeliveriesEx(shop, shopOrders, allCouriers[2], isLoop, calcTime, out taskDeliveries[2]));
//                        tasks[3] = Task.Run(() => CreateShopDeliveriesEx(shop, shopOrders, allCouriers[3], isLoop, calcTime, out taskDeliveries[3]));
//                        tasks[4] = Task.Run(() => CreateShopDeliveriesEx(shop, shopOrders, allCouriers[4], isLoop, calcTime, out taskDeliveries[4]));
//                        break;
//                    case 6:
//                        tasks[0] = Task.Run(() => CreateShopDeliveriesEx(shop, shopOrders, allCouriers[0], isLoop, calcTime, out taskDeliveries[0]));
//                        tasks[1] = Task.Run(() => CreateShopDeliveriesEx(shop, shopOrders, allCouriers[1], isLoop, calcTime, out taskDeliveries[1]));
//                        tasks[2] = Task.Run(() => CreateShopDeliveriesEx(shop, shopOrders, allCouriers[2], isLoop, calcTime, out taskDeliveries[2]));
//                        tasks[3] = Task.Run(() => CreateShopDeliveriesEx(shop, shopOrders, allCouriers[3], isLoop, calcTime, out taskDeliveries[3]));
//                        tasks[4] = Task.Run(() => CreateShopDeliveriesEx(shop, shopOrders, allCouriers[4], isLoop, calcTime, out taskDeliveries[4]));
//                        tasks[5] = Task.Run(() => CreateShopDeliveriesEx(shop, shopOrders, allCouriers[5], isLoop, calcTime, out taskDeliveries[5]));
//                        break;
//                    case 7:
//                        tasks[0] = Task.Run(() => CreateShopDeliveriesEx(shop, shopOrders, allCouriers[0], isLoop, calcTime, out taskDeliveries[0]));
//                        tasks[1] = Task.Run(() => CreateShopDeliveriesEx(shop, shopOrders, allCouriers[1], isLoop, calcTime, out taskDeliveries[1]));
//                        tasks[2] = Task.Run(() => CreateShopDeliveriesEx(shop, shopOrders, allCouriers[2], isLoop, calcTime, out taskDeliveries[2]));
//                        tasks[3] = Task.Run(() => CreateShopDeliveriesEx(shop, shopOrders, allCouriers[3], isLoop, calcTime, out taskDeliveries[3]));
//                        tasks[4] = Task.Run(() => CreateShopDeliveriesEx(shop, shopOrders, allCouriers[4], isLoop, calcTime, out taskDeliveries[4]));
//                        tasks[5] = Task.Run(() => CreateShopDeliveriesEx(shop, shopOrders, allCouriers[5], isLoop, calcTime, out taskDeliveries[5]));
//                        tasks[6] = Task.Run(() => CreateShopDeliveriesEx(shop, shopOrders, allCouriers[6], isLoop, calcTime, out taskDeliveries[6]));
//                        break;
//                    case 8:
//                        tasks[0] = Task.Run(() => CreateShopDeliveriesEx(shop, shopOrders, allCouriers[0], isLoop, calcTime, out taskDeliveries[0]));
//                        tasks[1] = Task.Run(() => CreateShopDeliveriesEx(shop, shopOrders, allCouriers[1], isLoop, calcTime, out taskDeliveries[1]));
//                        tasks[2] = Task.Run(() => CreateShopDeliveriesEx(shop, shopOrders, allCouriers[2], isLoop, calcTime, out taskDeliveries[2]));
//                        tasks[3] = Task.Run(() => CreateShopDeliveriesEx(shop, shopOrders, allCouriers[3], isLoop, calcTime, out taskDeliveries[3]));
//                        tasks[4] = Task.Run(() => CreateShopDeliveriesEx(shop, shopOrders, allCouriers[4], isLoop, calcTime, out taskDeliveries[4]));
//                        tasks[5] = Task.Run(() => CreateShopDeliveriesEx(shop, shopOrders, allCouriers[5], isLoop, calcTime, out taskDeliveries[5]));
//                        tasks[6] = Task.Run(() => CreateShopDeliveriesEx(shop, shopOrders, allCouriers[6], isLoop, calcTime, out taskDeliveries[6]));
//                        tasks[7] = Task.Run(() => CreateShopDeliveriesEx(shop, shopOrders, allCouriers[7], isLoop, calcTime, out taskDeliveries[7]));
//                        break;
//                }



//                //int rcz = CreateShopDeliveriesEx(shop, shopOrders, allCouriers[1], isLoop, calcTime, out taskDeliveries[1]);

//                //for (int i = 0; i < tasks.Length; i++)
//                //{
//                //    tasks[i] = Task.Run(() => CreateShopDeliveriesEx(shop, shopOrders, allCouriers[i], isLoop, calcTime, out taskDeliveries[i]));
//                //}

//                Task.WaitAll(tasks);

//                // 7. Объединяем все построенные отгрузки
//                rc = 7;
//                int deliveryCount = 0;
//                CourierDeliveryInfo[] allDeliveries;

//                for (int i = 0; i < tasks.Length; i++)
//                {
//                    int rcx = tasks[i].Result;

//                    //if (tasks[i].Result == 0)
//                    if (rcx == 0)
//                    {
//                        if (taskDeliveries[i] != null)
//                            deliveryCount += taskDeliveries[i].Length;
//                    }
//                    else
//                    {
//                        rc = rc;
//                    }
//                }

//                if (deliveryCount <= 0)
//                {
//                    undeliveredOrders = shopOrders;
//                    return rc = 0;
//                }

//                allDeliveries = new CourierDeliveryInfo[deliveryCount];
//                deliveryCount = 0;

//                for (int i = 0; i < tasks.Length; i++)
//                {
//                    if (tasks[i].Result == 0)
//                    {
//                        if (taskDeliveries[i] != null)
//                        {
//                            taskDeliveries[i].CopyTo(allDeliveries, deliveryCount);
//                            deliveryCount += taskDeliveries[i].Length;
//                        }
//                    }
//                }

//                // 8. Сортируем по средней стоимости доставки одного заказа
//                rc = 8;
//                Array.Sort(allDeliveries, CmpareByOrderCost);

//                // 9. Присваиваем заказам индексы
//                rc = 9;
//                for (int i = 0; i < shopOrders.Length; i++)
//                {
//                    shopOrders[i].Index = i;
//                }

//                // 10. Строим покрытие из всех построенных отгрузок
//                rc = 10;
//                CourierDeliveryInfo[] deiveryCover;
//                bool[] orderCoverMap;
//                //rc1 = BuildDeliveryCover(allDeliveries, shopOrders.Length, false, out deiveryCover, out orderCoverMap);
//                rc1 = BuildDeliveryCoverEx(shopCouriers, allDeliveries, shopOrders.Length, false, locationManager, out deiveryCover, out orderCoverMap);
//                if (rc1 != 0)
//                    return rc = 100 * rc + rc1;

//                // 11. Если ли в построенном покрытии не собранные заказы
//                rc = 11;
//                int receiptedCount = 0;

//                for (int i = 0; i < orderCoverMap.Length; i++)
//                {
//                    if (!orderCoverMap[i])
//                    {
//                        //undeliveredCount++;
//                    }
//                    else if (shopOrders[i].Status == OrderStatus.Receipted)
//                    {
//                        receiptedCount++;
//                    }
//                }

//                // 12. Если в покрытии есть не собранные заказы
//                rc = 12;
//                if (receiptedCount > 0)
//                {
//                    receiptedOrders = new CourierDeliveryInfo[receiptedCount];
//                    receiptedCount = 0;

//                    for (int i = 0; i < deiveryCover.Length; i++)
//                    {
//                        if (!deiveryCover[i].HasAssembledOnly)
//                        {
//                            receiptedOrders[receiptedCount++] = deiveryCover[i];
//                        }
//                    }

//                    if (receiptedCount < receiptedOrders.Length)
//                    {
//                        Array.Resize(ref receiptedOrders, receiptedCount);
//                    }

//                    rc1 = BuildDeliveryCoverEx(shopCouriers, allDeliveries, shopOrders.Length, true, locationManager, out deiveryCover, out orderCoverMap);
//                }

//                assembledOrders = deiveryCover;

//                // 13. Формируем список заказов которые не могут быть доставлены в срок
//                rc = 12;
//                for (int i = 0; i < shopOrders.Length; i++)
//                {
//                    if (!orderCoverMap[i] && shopOrders[i].Status == OrderStatus.Assembled)
//                    {
//                        Helper.WriteWarningToLog($"Undelivered By Courier. CreateShopDeliveries. Shop {shop.Id}. Order {shopOrders[i].Id}, calcTime {calcTime}");
//                        undelivOrders[undeliveredCount++] = shopOrders[i];
//                    }
//                }

//                if (undeliveredCount < undelivOrders.Length)
//                {
//                    Array.Resize(ref undelivOrders, undeliveredCount);
//                }

//                undeliveredOrders = undelivOrders;

//                // 14. Выход - Ok
//                rc = 0;
//                return rc;
//            }
//            catch (Exception ex)
//            {
//                Helper.WriteErrorToLog($"CreateShopDeliveries(Shop {shop.Id}, Orders {Helper.ArrayToString(allOrdersOfShop.Select(order => order.Id).ToArray())}, couriers { Helper.ArrayToString(shopCouriers.Select(courier => courier.Id).ToArray())}, isloop {isLoop})");
//                Helper.WriteErrorToLog($"(rc = {rc})");
//                Logger.WriteToLog(ex.ToString());

//                return rc;
//            }
//        }

//        ///// <summary>
//        ///// Создание отгрузок заказов магазина
//        ///// </summary>
//        ///// <param name="shop">Магазин</param>
//        ///// <param name="shopOrders">Отгружаемые заказы</param>
//        ///// <param name="shopCourier">Курьер</param>
//        ///// <param name="isLoop">Флаг возврата в магазин: true - требуется возврат в магазин; false - отгрузка завершается после вручения последнего заказа</param>
//        ///// <param name="calcTime">Момент расчетов</param>
//        ///// <param name="allPossibleDeliveries">Все возможные отгрузки состоящие не более, чем из 8 заказов</param>
//        ///// <returns>0 - отгрузки построены; иначе - отгрузки не построены</returns>
//        //private int CreateShopDeliveriesExOld(Shop shop, Order[] shopOrders, Courier shopCourier, bool isLoop, DateTime calcTime, out CourierDeliveryInfo[] allPossibleDeliveries)
//        //{
//        //    // 1. Инициализация
//        //    int rc = 1;
//        //    allPossibleDeliveries = null;

//        //    try
//        //    {
//        //        // 2. Проверяем исходные 
//        //        rc = 2;
//        //        if (shop == null)
//        //            return rc;
//        //        if (shopCourier == null)
//        //            return rc;
//        //        if (shopOrders == null || shopOrders.Length <= 0)
//        //            return rc;

//        //        // 3. Выбираем расстояния и времена движения между точками для заданного способа передвижения
//        //        rc = 3;
//        //        Point[,] locInfo = locationManager.GetLocationInfoForType(shopCourier.CourierType.VechicleType);
//        //        if (locInfo == null || locInfo.Length <= 0)
//        //            return rc;

//        //        // 4. Выделяем память под все возможные отгрузки
//        //        rc = 4;
//        //        int orderCount = shopOrders.Length;
//        //        int size;
//        //        if (orderCount <= 8)
//        //        {
//        //            size = (int) (Math.Pow(2, orderCount) + 0.5);
//        //        }
//        //        else
//        //        {
//        //            long n1 = 1;
//        //            long n2 = 1;
//        //            long k = 1;
                    
//        //            for (int i = orderCount; i <= orderCount - 8; i--)
//        //            {
//        //                n1 *= i;
//        //                n2 *= k++;
//        //            }

//        //            size = (int) (n1 / n2);
//        //        }

//        //        // 5. Извлекаем перестановки
//        //        rc = 5;
//        //        int[,] permutations1 = permutations.GetPermutations(1);
//        //        int[,] permutations2 = (orderCount >= 2 ? permutations.GetPermutations(2) : null);
//        //        int[,] permutations3 = (orderCount >= 3 ? permutations.GetPermutations(3) : null);
//        //        int[,] permutations4 = (orderCount >= 4 ? permutations.GetPermutations(4) : null);
//        //        int[,] permutations5 = (orderCount >= 5 ? permutations.GetPermutations(5) : null);
//        //        int[,] permutations6 = (orderCount >= 6 ? permutations.GetPermutations(6) : null);
//        //        int[,] permutations7 = (orderCount >= 7 ? permutations.GetPermutations(7) : null);
//        //        int[,] permutations8 = (orderCount >= 8 ? permutations.GetPermutations(8) : null);

//        //        // 6. Рабочий массив для проверки путей
//        //        rc = 6;
//        //        Order[] orders = new Order[8];

//        //        // 7. Цикл построения всех возможных отгрузок
//        //        rc = 7;
//        //        CourierDeliveryInfo delivery;
//        //        CourierDeliveryInfo[] allDeliveries = new CourierDeliveryInfo[size];
//        //        int count = 0;
//        //        int rcFind = 1;

//        //        for (int i1 = 0; i1 < orderCount; i1++)
//        //        {
//        //            orders[0] = shopOrders[i1];
//        //            rcFind = FindSalesmanProblemSolution(shop, orders, 1, shopCourier, isLoop, permutations1, locInfo, calcTime, out delivery);
//        //            if (rcFind != 0 || delivery == null) continue;
//        //            allDeliveries[count++] = delivery;

//        //            for (int i2 = i1 + 1; i2 < orderCount; i2++)
//        //            {
//        //                orders[1] = shopOrders[i2];
//        //                rcFind = FindSalesmanProblemSolution(shop, orders, 2, shopCourier, isLoop, permutations2, locInfo, calcTime, out delivery);
//        //                if (rcFind != 0 || delivery == null) continue;
//        //                allDeliveries[count++] = delivery;

//        //                for (int i3 = i2 + 1; i3 < orderCount; i3++)
//        //                {
//        //                    orders[2] = shopOrders[i3];
//        //                    rcFind = FindSalesmanProblemSolution(shop, orders, 3, shopCourier, isLoop, permutations3, locInfo, calcTime, out delivery);
//        //                    if (rcFind != 0 || delivery == null) continue;
//        //                    allDeliveries[count++] = delivery;

//        //                    for (int i4 = i3 + 1; i4 < orderCount; i4++)
//        //                    {
//        //                        orders[3] = shopOrders[i4];
//        //                        rcFind = FindSalesmanProblemSolution(shop, orders, 4, shopCourier, isLoop, permutations4, locInfo, calcTime, out delivery);
//        //                        if (rcFind != 0 || delivery == null) continue;
//        //                        allDeliveries[count++] = delivery;

//        //                        for (int i5 = i4 + 1; i5 < orderCount; i5++)
//        //                        {
//        //                            orders[4] = shopOrders[i5];
//        //                            rcFind = FindSalesmanProblemSolution(shop, orders, 5, shopCourier, isLoop, permutations5, locInfo, calcTime, out delivery);
//        //                            if (rcFind != 0 || delivery == null) continue;
//        //                            allDeliveries[count++] = delivery;

//        //                            for (int i6 = i5 + 1; i6 < orderCount; i6++)
//        //                            {
//        //                                orders[5] = shopOrders[i6];
//        //                                rcFind = FindSalesmanProblemSolution(shop, orders, 6, shopCourier, isLoop, permutations6, locInfo, calcTime, out delivery);
//        //                                if (rcFind != 0 || delivery == null) continue;
//        //                                allDeliveries[count++] = delivery;

//        //                                for (int i7 = i6 + 1; i7 < orderCount; i7++)
//        //                                {
//        //                                    orders[6] = shopOrders[i7];
//        //                                    rcFind = FindSalesmanProblemSolution(shop, orders, 7, shopCourier, isLoop, permutations7, locInfo, calcTime, out delivery);
//        //                                    if (rcFind != 0 || delivery == null) continue;
//        //                                    allDeliveries[count++] = delivery;

//        //                                    for (int i8 = i7 + 1; i8 < orderCount; i8++)
//        //                                    {
//        //                                        orders[7] = shopOrders[i8];
//        //                                        rcFind = FindSalesmanProblemSolution(shop, orders, 8, shopCourier, isLoop, permutations8, locInfo, calcTime, out delivery);
//        //                                        if (rcFind != 0 || delivery == null) continue;
//        //                                        allDeliveries[count++] = delivery;
//        //                                    }
//        //                                }
//        //                            }
//        //                        }
//        //                    }
//        //                }
//        //            }
//        //        }

//        //        if (count < allDeliveries.Length)
//        //        {
//        //            Array.Resize(ref allDeliveries, count);
//        //        }

//        //        allPossibleDeliveries = allDeliveries;

//        //        // 8. Выход - Ok
//        //        rc = 0;
//        //        return rc;
//        //    }
//        //    catch
//        //    {
//        //        return rc;
//        //    }
//        //}

//        ///// <summary>
//        ///// Создание отгрузок заказов магазина
//        ///// </summary>
//        ///// <param name="shop">Магазин</param>
//        ///// <param name="shopOrdersX">Отгружаемые заказы</param>
//        ///// <param name="shopCourier">Курьер</param>
//        ///// <param name="isLoop">Флаг возврата в магазин: true - требуется возврат в магазин; false - отгрузка завершается после вручения последнего заказа</param>
//        ///// <param name="calcTime">Момент расчетов</param>
//        ///// <param name="allPossibleDeliveries">Все возможные отгрузки состоящие не более, чем из 8 заказов</param>
//        ///// <returns>0 - отгрузки построены; иначе - отгрузки не построены</returns>
//        //private int CreateShopDeliveriesEx(Shop shop, Order[] shopOrdersX, Courier shopCourier, bool isLoop, DateTime calcTime, out CourierDeliveryInfo[] allPossibleDeliveries)
//        //{
//        //    // 1. Инициализация
//        //    int rc = 1;
//        //    allPossibleDeliveries = null;

//        //    try
//        //    {
//        //        // 2. Проверяем исходные 
//        //        rc = 2;
//        //        if (shop == null)
//        //            return rc;
//        //        if (shopCourier == null)
//        //            return rc;
//        //        if (shopOrdersX == null || shopOrdersX.Length <= 0)
//        //            return rc;

//        //        // 3. Отбираем заказы, которые могут быть доставлены курьером
//        //        rc = 3;
//        //        Order[] shopOrders = new Order[shopOrdersX.Length];
//        //        int orderCount = 0;
//        //        EnabledCourierType serviceFlags = shopCourier.ServiceFlags;

//        //        for (int i = 0; i < shopOrdersX.Length; i++)
//        //        {
//        //            Order order = shopOrdersX[i];
//        //            if ((order.EnabledTypes & serviceFlags) != 0)
//        //            {
//        //                shopOrders[orderCount++] = order;
//        //            }
//        //        }

//        //        if (orderCount <= 0)
//        //            return rc = 0;

//        //        if (orderCount < shopOrders.Length)
//        //        {
//        //            Array.Resize(ref shopOrders, orderCount);
//        //        }

//        //        // 4. Выбираем расстояния и времена движения между точками для заданного способа передвижения
//        //        rc = 4;
//        //        Point[,] locInfo = locationManager.GetLocationInfoForType(shopCourier.CourierType.VechicleType);
//        //        if (locInfo == null || locInfo.Length <= 0)
//        //            return rc;

//        //        // 5. Выделяем память под все возможные отгрузки
//        //        rc = 5;
//        //        int size;
//        //        if (orderCount <= 8)
//        //        {
//        //            size = (int) (Math.Pow(2, orderCount) + 0.5);
//        //        }
//        //        else
//        //        {
//        //            long n1 = 1;
//        //            long n2 = 1;
//        //            long k = 1;
                    
//        //            for (int i = orderCount; i >= orderCount - 8; i--)
//        //            {
//        //                n1 *= i;
//        //                n2 *= k++;
//        //            }

//        //            size = (int) (n1 / n2);
//        //        }

//        //        // 6. Извлекаем перестановки
//        //        rc = 6;
//        //        int[,] permutations1 = permutations.GetPermutations(1);
//        //        int[,] permutations2 = (orderCount >= 2 ? permutations.GetPermutations(2) : null);
//        //        int[,] permutations3 = (orderCount >= 3 ? permutations.GetPermutations(3) : null);
//        //        int[,] permutations4 = (orderCount >= 4 ? permutations.GetPermutations(4) : null);
//        //        int[,] permutations5 = (orderCount >= 5 ? permutations.GetPermutations(5) : null);
//        //        int[,] permutations6 = (orderCount >= 6 ? permutations.GetPermutations(6) : null);
//        //        int[,] permutations7 = (orderCount >= 7 ? permutations.GetPermutations(7) : null);
//        //        int[,] permutations8 = (orderCount >= 8 ? permutations.GetPermutations(8) : null);

//        //        // 7. Цикл построения всех возможных отгрузок
//        //        rc = 7;
//        //        Order[] orders = new Order[8];
//        //        CourierDeliveryInfo delivery;
//        //        CourierDeliveryInfo[] allDeliveries = new CourierDeliveryInfo[size];
//        //        int count = 0;
//        //        int rcFind = 1;

//        //        for (int i1 = 0; i1 < orderCount; i1++)
//        //        {
//        //            orders[0] = shopOrders[i1];
//        //            rcFind = FindSalesmanProblemSolution(shop, orders, 1, shopCourier, isLoop, permutations1, locInfo, calcTime, out delivery);
//        //            if (rcFind != 0 || delivery == null) continue;
//        //            allDeliveries[count++] = delivery;

//        //            for (int i2 = i1 + 1; i2 < orderCount; i2++)
//        //            {
//        //                orders[1] = shopOrders[i2];
//        //                rcFind = FindSalesmanProblemSolution(shop, orders, 2, shopCourier, isLoop, permutations2, locInfo, calcTime, out delivery);
//        //                if (rcFind != 0 || delivery == null) continue;
//        //                allDeliveries[count++] = delivery;

//        //                for (int i3 = i2 + 1; i3 < orderCount; i3++)
//        //                {
//        //                    orders[2] = shopOrders[i3];
//        //                    rcFind = FindSalesmanProblemSolution(shop, orders, 3, shopCourier, isLoop, permutations3, locInfo, calcTime, out delivery);
//        //                    if (rcFind != 0 || delivery == null) continue;
//        //                    allDeliveries[count++] = delivery;

//        //                    for (int i4 = i3 + 1; i4 < orderCount; i4++)
//        //                    {
//        //                        orders[3] = shopOrders[i4];
//        //                        rcFind = FindSalesmanProblemSolution(shop, orders, 4, shopCourier, isLoop, permutations4, locInfo, calcTime, out delivery);
//        //                        if (rcFind != 0 || delivery == null) continue;
//        //                        allDeliveries[count++] = delivery;

//        //                        for (int i5 = i4 + 1; i5 < orderCount; i5++)
//        //                        {
//        //                            orders[4] = shopOrders[i5];
//        //                            rcFind = FindSalesmanProblemSolution(shop, orders, 5, shopCourier, isLoop, permutations5, locInfo, calcTime, out delivery);
//        //                            if (rcFind != 0 || delivery == null) continue;
//        //                            allDeliveries[count++] = delivery;

//        //                            for (int i6 = i5 + 1; i6 < orderCount; i6++)
//        //                            {
//        //                                orders[5] = shopOrders[i6];
//        //                                rcFind = FindSalesmanProblemSolution(shop, orders, 6, shopCourier, isLoop, permutations6, locInfo, calcTime, out delivery);
//        //                                if (rcFind != 0 || delivery == null) continue;
//        //                                allDeliveries[count++] = delivery;

//        //                                for (int i7 = i6 + 1; i7 < orderCount; i7++)
//        //                                {
//        //                                    orders[6] = shopOrders[i7];
//        //                                    rcFind = FindSalesmanProblemSolution(shop, orders, 7, shopCourier, isLoop, permutations7, locInfo, calcTime, out delivery);
//        //                                    if (rcFind != 0 || delivery == null) continue;
//        //                                    allDeliveries[count++] = delivery;

//        //                                    for (int i8 = i7 + 1; i8 < orderCount; i8++)
//        //                                    {
//        //                                        orders[7] = shopOrders[i8];
//        //                                        rcFind = FindSalesmanProblemSolution(shop, orders, 8, shopCourier, isLoop, permutations8, locInfo, calcTime, out delivery);
//        //                                        if (rcFind != 0 || delivery == null) continue;
//        //                                        allDeliveries[count++] = delivery;
//        //                                    }
//        //                                }
//        //                            }
//        //                        }
//        //                    }
//        //                }
//        //            }
//        //        }

//        //        if (count < allDeliveries.Length)
//        //        {
//        //            Array.Resize(ref allDeliveries, count);
//        //        }

//        //        allPossibleDeliveries = allDeliveries;

//        //        // 8. Выход - Ok
//        //        rc = 0;
//        //        return rc;
//        //    }
//        //    catch (Exception ex)
//        //    {
//        //        Helper.WriteErrorToLog($"CreateShopDeliveriesEx(Shop {shop.Id}, Orders {Helper.ArrayToString(shopOrdersX.Select(order => order.Id).ToArray())}, courier {shopCourier.Id}, isloop {isLoop}, calcTime {calcTime})");
//        //        Helper.WriteErrorToLog($"(rc = {rc})");
//        //        Logger.WriteToLog(ex.ToString());

//        //        return rc;
//        //    }
//        //}

//        ///// <summary>
//        ///// Создание отгрузок заказов магазина
//        ///// </summary>
//        ///// <param name="shop">Магазин</param>
//        ///// <param name="shopOrders">Отгружаемые заказы</param>
//        ///// <param name="shopCourier">Курьер</param>
//        ///// <param name="isLoop">Флаг возврата в магазин: true - требуется возврат в магазин; false - отгрузка завершается после вручения последнего заказа</param>
//        ///// <param name="calcTime">Момент расчетов</param>
//        ///// <param name="allPossibleDeliveries">Все возможные отгрузки состоящие не более, чем из 8 заказов</param>
//        ///// <returns>0 - отгрузки построены; иначе - отгрузки не построены</returns>
//        //private int CreateShopDeliveriesEx1(Shop shop, Order[] shopOrders, Courier shopCourier, bool isLoop, DateTime calcTime, out CourierDeliveryInfo[] allPossibleDeliveries)
//        //{
//        //    // 1. Инициализация
//        //    int rc = 1;
//        //    allPossibleDeliveries = null;

//        //    try
//        //    {
//        //        // 2. Проверяем исходные 
//        //        rc = 2;
//        //        if (shop == null)
//        //            return rc;
//        //        if (shopCourier == null)
//        //            return rc;
//        //        if (shopOrders == null || shopOrders.Length <= 0)
//        //            return rc;

//        //        // 3. Отбираем заказы, которые могут быть доставлены курьером
//        //        rc = 3;
//        //        Order[] courierOrders = new Order[shopOrders.Length];
//        //        int orderCount = 0;
//        //        EnabledCourierType serviceFlags = shopCourier.ServiceFlags;

//        //        for (int i = 0; i < shopOrders.Length; i++)
//        //        {
//        //            Order order = shopOrders[i];
//        //            if ((order.EnabledTypes & serviceFlags) != 0)
//        //            {
//        //                courierOrders[orderCount++] = order;
//        //            }
//        //        }

//        //        if (orderCount <= 0)
//        //            return rc = 0;

//        //        if (orderCount < courierOrders.Length)
//        //        {
//        //            Array.Resize(ref courierOrders, orderCount);
//        //        }

//        //        // 4. Выбираем расстояния и времена движения между точками для заданного способа передвижения
//        //        rc = 4;
//        //        Point[,] locInfo = locationManager.GetLocationInfoForType(shopCourier.CourierType.VechicleType);
//        //        if (locInfo == null || locInfo.Length <= 0)
//        //            return rc;

//        //        // 5. Выделяем память под все возможные отгрузки
//        //        rc = 5;
//        //        int size;
//        //        if (orderCount <= 12)
//        //        {
//        //            size = (int) (Math.Pow(2, orderCount) + 0.5);
//        //        }
//        //        else
//        //        {
//        //            size = 4096;
//        //        }

//        //        // 6. Извлекаем перестановки
//        //        rc = 6;
//        //        int[,] permutations1 = permutations.GetPermutations(1);
//        //        int[,] permutations2 = (orderCount >= 2 ? permutations.GetPermutations(2) : null);
//        //        int[,] permutations3 = (orderCount >= 3 ? permutations.GetPermutations(3) : null);
//        //        int[,] permutations4 = (orderCount >= 4 ? permutations.GetPermutations(4) : null);
//        //        int[,] permutations5 = (orderCount >= 5 ? permutations.GetPermutations(5) : null);
//        //        int[,] permutations6 = (orderCount >= 6 ? permutations.GetPermutations(6) : null);
//        //        int[,] permutations7 = (orderCount >= 7 ? permutations.GetPermutations(7) : null);
//        //        int[,] permutations8 = (orderCount >= 8 ? permutations.GetPermutations(8) : null);

//        //        // 7. Цикл построения всех возможных отгрузок
//        //        rc = 7;
//        //        Order[] orders = new Order[8];
//        //        CourierDeliveryInfo delivery;
//        //        CourierDeliveryInfo[] allDeliveries = new CourierDeliveryInfo[size];
//        //        int count = 0;
//        //        int rcFind = 1;

//        //        for (int i1 = 0; i1 < orderCount; i1++)
//        //        {
//        //            orders[0] = courierOrders[i1];
//        //            rcFind = FindSalesmanProblemSolution(shop, orders, 1, shopCourier, isLoop, permutations1, locInfo, calcTime, out delivery);
//        //            if (rcFind != 0 || delivery == null) continue;
//        //            if (count >= allDeliveries.Length) Array.Resize(ref allDeliveries, allDeliveries.Length + 2048);
//        //            allDeliveries[count++] = delivery;

//        //            for (int i2 = i1 + 1; i2 < orderCount; i2++)
//        //            {
//        //                orders[1] = courierOrders[i2];
//        //                rcFind = FindSalesmanProblemSolution(shop, orders, 2, shopCourier, isLoop, permutations2, locInfo, calcTime, out delivery);
//        //                if (rcFind != 0 || delivery == null) continue;
//        //                if (count >= allDeliveries.Length) Array.Resize(ref allDeliveries, allDeliveries.Length + 2048);
//        //                allDeliveries[count++] = delivery;

//        //                for (int i3 = i2 + 1; i3 < orderCount; i3++)
//        //                {
//        //                    orders[2] = courierOrders[i3];
//        //                    rcFind = FindSalesmanProblemSolution(shop, orders, 3, shopCourier, isLoop, permutations3, locInfo, calcTime, out delivery);
//        //                    if (rcFind != 0 || delivery == null) continue;
//        //                   if (count >= allDeliveries.Length) Array.Resize(ref allDeliveries, allDeliveries.Length + 2048);
//        //                    allDeliveries[count++] = delivery;

//        //                    for (int i4 = i3 + 1; i4 < orderCount; i4++)
//        //                    {
//        //                        orders[3] = courierOrders[i4];
//        //                        rcFind = FindSalesmanProblemSolution(shop, orders, 4, shopCourier, isLoop, permutations4, locInfo, calcTime, out delivery);
//        //                        if (rcFind != 0 || delivery == null) continue;
//        //                        if (count >= allDeliveries.Length) Array.Resize(ref allDeliveries, allDeliveries.Length + 2048);
//        //                        allDeliveries[count++] = delivery;

//        //                        for (int i5 = i4 + 1; i5 < orderCount; i5++)
//        //                        {
//        //                            orders[4] = courierOrders[i5];
//        //                            rcFind = FindSalesmanProblemSolution(shop, orders, 5, shopCourier, isLoop, permutations5, locInfo, calcTime, out delivery);
//        //                            if (rcFind != 0 || delivery == null) continue;
//        //                            if (count >= allDeliveries.Length) Array.Resize(ref allDeliveries, allDeliveries.Length + 2048);
//        //                            allDeliveries[count++] = delivery;

//        //                            for (int i6 = i5 + 1; i6 < orderCount; i6++)
//        //                            {
//        //                                orders[5] = courierOrders[i6];
//        //                                rcFind = FindSalesmanProblemSolution(shop, orders, 6, shopCourier, isLoop, permutations6, locInfo, calcTime, out delivery);
//        //                                if (rcFind != 0 || delivery == null) continue;
//        //                                if (count >= allDeliveries.Length) Array.Resize(ref allDeliveries, allDeliveries.Length + 2048);
//        //                                allDeliveries[count++] = delivery;

//        //                                for (int i7 = i6 + 1; i7 < orderCount; i7++)
//        //                                {
//        //                                    orders[6] = courierOrders[i7];
//        //                                    rcFind = FindSalesmanProblemSolution(shop, orders, 7, shopCourier, isLoop, permutations7, locInfo, calcTime, out delivery);
//        //                                    if (rcFind != 0 || delivery == null) continue;
//        //                                    if (count >= allDeliveries.Length) Array.Resize(ref allDeliveries, allDeliveries.Length + 2048);
//        //                                    allDeliveries[count++] = delivery;

//        //                                    for (int i8 = i7 + 1; i8 < orderCount; i8++)
//        //                                    {
//        //                                        orders[7] = courierOrders[i8];
//        //                                        rcFind = FindSalesmanProblemSolution(shop, orders, 8, shopCourier, isLoop, permutations8, locInfo, calcTime, out delivery);
//        //                                        if (rcFind != 0 || delivery == null) continue;
//        //                                        if (count >= allDeliveries.Length) Array.Resize(ref allDeliveries, allDeliveries.Length + 2048);
//        //                                        allDeliveries[count++] = delivery;
//        //                                    }
//        //                                }
//        //                            }
//        //                        }
//        //                    }
//        //                }
//        //            }
//        //        }

//        //        if (count < allDeliveries.Length)
//        //        {
//        //            Array.Resize(ref allDeliveries, count);
//        //        }

//        //        allPossibleDeliveries = allDeliveries;

//        //        // 8. Выход - Ok
//        //        rc = 0;
//        //        return rc;
//        //    }
//        //    catch (Exception ex)
//        //    {
//        //        Helper.WriteErrorToLog($"CreateShopDeliveriesEx(Shop {shop.Id}, Orders {Helper.ArrayToString(shopOrders.Select(order => order.Id).ToArray())}, courier {shopCourier.Id}, isloop {isLoop}, calcTime {calcTime})");
//        //        Helper.WriteErrorToLog($"(rc = {rc})");
//        //        Logger.WriteToLog(ex.ToString());

//        //        return rc;
//        //    }
//        //}
        
//        /// <summary>
//        /// Создание всех возможных отгрузок заказов магазина для заданного курьера
//        /// </summary>
//        /// <param name="shop">Магазин</param>
//        /// <param name="shopOrders">Заданные заказы</param>
//        /// <param name="shopCourier">Курьер</param>
//        /// <param name="isLoop">Флаг возврата в магазин: true - требуется возврат в магазин; false - отгрузка завершается после вручения последнего заказа</param>
//        /// <param name="calcTime">Момент расчетов</param>
//        /// <param name="allPossibleDeliveries">Все возможные отгрузки состоящие не более, чем из 8 заказов</param>
//        /// <returns>0 - отгрузки построены; иначе - отгрузки не построены</returns>
//        private int CreateShopDeliveriesEx(Shop shop, Order[] shopOrders, Courier shopCourier, bool isLoop, DateTime calcTime, out CourierDeliveryInfo[] allPossibleDeliveries)
//        {
//            // 1. Инициализация
//            int rc = 1;
//            allPossibleDeliveries = null;

//            try
//            {
//                // 2. Проверяем исходные 
//                rc = 2;
//                if (shop == null)
//                    return rc;
//                if (shopCourier == null)
//                    return rc;
//                if (shopOrders == null || shopOrders.Length <= 0)
//                    return rc;

//                // 3. Отбираем заказы, которые могут быть доставлены курьером
//                rc = 3;
//                Order[] courierOrders = new Order[shopOrders.Length];
//                int orderCount = 0;
//                EnabledCourierType serviceFlags = shopCourier.ServiceFlags;

//                for (int i = 0; i < shopOrders.Length; i++)
//                {
//                    Order order = shopOrders[i];
//                    if ((order.EnabledTypes & serviceFlags) != 0)
//                    {
//                        courierOrders[orderCount++] = order;
//                    }
//                }

//                if (orderCount <= 0)
//                    return rc = 0;

//                if (orderCount < courierOrders.Length)
//                {
//                    Array.Resize(ref courierOrders, orderCount);
//                }

//                // 4. Строим отгрузки в зависимомти от числа заказов
//                rc = 4;
//                if (orderCount <= 14)
//                {
//                    return CreateShopDeliveries8(shop, courierOrders, shopCourier, isLoop, calcTime, out allPossibleDeliveries);
//                }
//                else if (orderCount <= 24)
//                {
//                    return CreateShopDeliveries7(shop, courierOrders, shopCourier, isLoop, calcTime, out allPossibleDeliveries);
//                }
//                else if (orderCount <= 32)
//                {
//                    return CreateShopDeliveries6(shop, courierOrders, shopCourier, isLoop, calcTime, out allPossibleDeliveries);
//                }
//                else
//                {
//                    return CreateShopDeliveries5(shop, courierOrders, shopCourier, isLoop, calcTime, out allPossibleDeliveries);
//                }
//            }
//            catch (Exception ex)
//            {
//                Helper.WriteErrorToLog($"CreateShopDeliveriesForCourier(Shop {shop.Id}, Orders {Helper.ArrayToString(shopOrders.Select(order => order.Id).ToArray())}, courier {shopCourier.Id}, isloop {isLoop}, calcTime {calcTime})");
//                Helper.WriteErrorToLog($"(rc = {rc})");
//                Logger.WriteToLog(ex.ToString());

//                return rc;
//            }
//        }

//        /// <summary>
//        /// Создание отгрузок заказов магазина
//        /// </summary>
//        /// <param name="shop">Магазин</param>
//        /// <param name="courierOrders">Отгружаемые заказы</param>
//        /// <param name="shopCourier">Курьер</param>
//        /// <param name="isLoop">Флаг возврата в магазин: true - требуется возврат в магазин; false - отгрузка завершается после вручения последнего заказа</param>
//        /// <param name="calcTime">Момент расчетов</param>
//        /// <param name="allPossibleDeliveries">Все возможные отгрузки состоящие не более, чем из 8 заказов</param>
//        /// <returns>0 - отгрузки построены; иначе - отгрузки не построены</returns>
//        private int CreateShopDeliveries8(Shop shop, Order[] courierOrders, Courier shopCourier, bool isLoop, DateTime calcTime, out CourierDeliveryInfo[] allPossibleDeliveries)
//        {
//            // 1. Инициализация
//            int rc = 1;
//            allPossibleDeliveries = null;

//            try
//            {
//                // 2. Проверяем исходные 
//                rc = 2;
//                if (shop == null)
//                    return rc;
//                if (shopCourier == null)
//                    return rc;
//                if (courierOrders == null || courierOrders.Length <= 0)
//                    return rc;

//                // 3. Выбираем расстояния и времена движения между точками для заданного способа передвижения
//                rc = 3;
//                int orderCount = courierOrders.Length;
//                Point[,] locInfo = locationManager.GetLocationInfoForType(shopCourier.CourierType.VechicleType);
//                if (locInfo == null || locInfo.Length <= 0)
//                    return rc;

//                // 5. Выделяем память под все возможные отгрузки
//                rc = 5;
//                int size;
//                if (orderCount <= 12)
//                {
//                    size = (int) (Math.Pow(2, orderCount) + 0.5);
//                }
//                else
//                {
//                    size = 4096;
//                }

//                // 6. Извлекаем перестановки
//                rc = 6;
//                int[,] permutations1 = permutations.GetPermutations(1);
//                int[,] permutations2 = (orderCount >= 2 ? permutations.GetPermutations(2) : null);
//                int[,] permutations3 = (orderCount >= 3 ? permutations.GetPermutations(3) : null);
//                int[,] permutations4 = (orderCount >= 4 ? permutations.GetPermutations(4) : null);
//                int[,] permutations5 = (orderCount >= 5 ? permutations.GetPermutations(5) : null);
//                int[,] permutations6 = (orderCount >= 6 ? permutations.GetPermutations(6) : null);
//                int[,] permutations7 = (orderCount >= 7 ? permutations.GetPermutations(7) : null);
//                int[,] permutations8 = (orderCount >= 8 ? permutations.GetPermutations(8) : null);

//                // 7. Цикл построения всех возможных отгрузок
//                rc = 7;
//                Order[] orders = new Order[8];
//                CourierDeliveryInfo delivery;
//                CourierDeliveryInfo[] allDeliveries = new CourierDeliveryInfo[size];
//                int count = 0;
//                int rcFind = 1;

//                for (int i1 = 0; i1 < orderCount; i1++)
//                {
//                    orders[0] = courierOrders[i1];
//                    rcFind = FindSalesmanProblemSolution(shop, orders, 1, shopCourier, isLoop, permutations1, locInfo, calcTime, out delivery);
//                    if (rcFind != 0 || delivery == null) continue;
//                    if (count >= allDeliveries.Length) Array.Resize(ref allDeliveries, allDeliveries.Length + 2048);
//                    allDeliveries[count++] = delivery;

//                    for (int i2 = i1 + 1; i2 < orderCount; i2++)
//                    {
//                        orders[1] = courierOrders[i2];
//                        rcFind = FindSalesmanProblemSolution(shop, orders, 2, shopCourier, isLoop, permutations2, locInfo, calcTime, out delivery);
//                        if (rcFind != 0 || delivery == null) continue;
//                        if (count >= allDeliveries.Length) Array.Resize(ref allDeliveries, allDeliveries.Length + 2048);
//                        allDeliveries[count++] = delivery;

//                        for (int i3 = i2 + 1; i3 < orderCount; i3++)
//                        {
//                            orders[2] = courierOrders[i3];
//                            rcFind = FindSalesmanProblemSolution(shop, orders, 3, shopCourier, isLoop, permutations3, locInfo, calcTime, out delivery);
//                            if (rcFind != 0 || delivery == null) continue;
//                           if (count >= allDeliveries.Length) Array.Resize(ref allDeliveries, allDeliveries.Length + 2048);
//                            allDeliveries[count++] = delivery;

//                            for (int i4 = i3 + 1; i4 < orderCount; i4++)
//                            {
//                                orders[3] = courierOrders[i4];
//                                rcFind = FindSalesmanProblemSolution(shop, orders, 4, shopCourier, isLoop, permutations4, locInfo, calcTime, out delivery);
//                                if (rcFind != 0 || delivery == null) continue;
//                                if (count >= allDeliveries.Length) Array.Resize(ref allDeliveries, allDeliveries.Length + 2048);
//                                allDeliveries[count++] = delivery;

//                                for (int i5 = i4 + 1; i5 < orderCount; i5++)
//                                {
//                                    orders[4] = courierOrders[i5];
//                                    rcFind = FindSalesmanProblemSolution(shop, orders, 5, shopCourier, isLoop, permutations5, locInfo, calcTime, out delivery);
//                                    if (rcFind != 0 || delivery == null) continue;
//                                    if (count >= allDeliveries.Length) Array.Resize(ref allDeliveries, allDeliveries.Length + 2048);
//                                    allDeliveries[count++] = delivery;

//                                    for (int i6 = i5 + 1; i6 < orderCount; i6++)
//                                    {
//                                        orders[5] = courierOrders[i6];
//                                        rcFind = FindSalesmanProblemSolution(shop, orders, 6, shopCourier, isLoop, permutations6, locInfo, calcTime, out delivery);
//                                        if (rcFind != 0 || delivery == null) continue;
//                                        if (count >= allDeliveries.Length) Array.Resize(ref allDeliveries, allDeliveries.Length + 2048);
//                                        allDeliveries[count++] = delivery;

//                                        for (int i7 = i6 + 1; i7 < orderCount; i7++)
//                                        {
//                                            orders[6] = courierOrders[i7];
//                                            rcFind = FindSalesmanProblemSolution(shop, orders, 7, shopCourier, isLoop, permutations7, locInfo, calcTime, out delivery);
//                                            if (rcFind != 0 || delivery == null) continue;
//                                            if (count >= allDeliveries.Length) Array.Resize(ref allDeliveries, allDeliveries.Length + 2048);
//                                            allDeliveries[count++] = delivery;

//                                            for (int i8 = i7 + 1; i8 < orderCount; i8++)
//                                            {
//                                                orders[7] = courierOrders[i8];
//                                                rcFind = FindSalesmanProblemSolution(shop, orders, 8, shopCourier, isLoop, permutations8, locInfo, calcTime, out delivery);
//                                                if (rcFind != 0 || delivery == null) continue;
//                                                if (count >= allDeliveries.Length) Array.Resize(ref allDeliveries, allDeliveries.Length + 2048);
//                                                allDeliveries[count++] = delivery;
//                                            }
//                                        }
//                                    }
//                                }
//                            }
//                        }
//                    }
//                }

//                if (count < allDeliveries.Length)
//                {
//                    Array.Resize(ref allDeliveries, count);
//                }

//                allPossibleDeliveries = allDeliveries;

//                // 8. Выход - Ok
//                rc = 0;
//                return rc;
//            }
//            catch (Exception ex)
//            {
//                Helper.WriteErrorToLog($"CreateShopDeliveries7(Shop {shop.Id}, Orders {Helper.ArrayToString(courierOrders.Select(order => order.Id).ToArray())}, courier {shopCourier.Id}, isloop {isLoop}, calcTime {calcTime})");
//                Helper.WriteErrorToLog($"(rc = {rc})");
//                Logger.WriteToLog(ex.ToString());

//                return rc;
//            }
//        }

//        /// <summary>
//        /// Создание отгрузок заказов магазина
//        /// </summary>
//        /// <param name="shop">Магазин</param>
//        /// <param name="courierOrders">Отгружаемые заказы</param>
//        /// <param name="shopCourier">Курьер</param>
//        /// <param name="isLoop">Флаг возврата в магазин: true - требуется возврат в магазин; false - отгрузка завершается после вручения последнего заказа</param>
//        /// <param name="calcTime">Момент расчетов</param>
//        /// <param name="allPossibleDeliveries">Все возможные отгрузки состоящие не более, чем из 7 заказов</param>
//        /// <returns>0 - отгрузки построены; иначе - отгрузки не построены</returns>
//        private int CreateShopDeliveries7(Shop shop, Order[] courierOrders, Courier shopCourier, bool isLoop, DateTime calcTime, out CourierDeliveryInfo[] allPossibleDeliveries)
//        {
//            // 1. Инициализация
//            int rc = 1;
//            allPossibleDeliveries = null;

//            try
//            {
//                // 2. Проверяем исходные 
//                rc = 2;
//                if (shop == null)
//                    return rc;
//                if (shopCourier == null)
//                    return rc;
//                if (courierOrders == null || courierOrders.Length <= 0)
//                    return rc;

//                // 3. Выбираем расстояния и времена движения между точками для заданного способа передвижения
//                rc = 3;
//                int orderCount = courierOrders.Length;
//                Point[,] locInfo = locationManager.GetLocationInfoForType(shopCourier.CourierType.VechicleType);
//                if (locInfo == null || locInfo.Length <= 0)
//                    return rc;

//                // 5. Выделяем память под все возможные отгрузки
//                rc = 5;
//                int size;
//                if (orderCount <= 12)
//                {
//                    size = (int) (Math.Pow(2, orderCount) + 0.5);
//                }
//                else
//                {
//                    size = 4096;
//                }

//                // 6. Извлекаем перестановки
//                rc = 6;
//                int[,] permutations1 = permutations.GetPermutations(1);
//                int[,] permutations2 = (orderCount >= 2 ? permutations.GetPermutations(2) : null);
//                int[,] permutations3 = (orderCount >= 3 ? permutations.GetPermutations(3) : null);
//                int[,] permutations4 = (orderCount >= 4 ? permutations.GetPermutations(4) : null);
//                int[,] permutations5 = (orderCount >= 5 ? permutations.GetPermutations(5) : null);
//                int[,] permutations6 = (orderCount >= 6 ? permutations.GetPermutations(6) : null);
//                int[,] permutations7 = (orderCount >= 7 ? permutations.GetPermutations(7) : null);

//                // 7. Цикл построения всех возможных отгрузок
//                rc = 7;
//                Order[] orders = new Order[8];
//                CourierDeliveryInfo delivery;
//                CourierDeliveryInfo[] allDeliveries = new CourierDeliveryInfo[size];
//                int count = 0;
//                int rcFind = 1;

//                for (int i1 = 0; i1 < orderCount; i1++)
//                {
//                    orders[0] = courierOrders[i1];
//                    rcFind = FindSalesmanProblemSolution(shop, orders, 1, shopCourier, isLoop, permutations1, locInfo, calcTime, out delivery);
//                    if (rcFind != 0 || delivery == null) continue;
//                    if (count >= allDeliveries.Length) Array.Resize(ref allDeliveries, allDeliveries.Length + 2048);
//                    allDeliveries[count++] = delivery;

//                    for (int i2 = i1 + 1; i2 < orderCount; i2++)
//                    {
//                        orders[1] = courierOrders[i2];
//                        rcFind = FindSalesmanProblemSolution(shop, orders, 2, shopCourier, isLoop, permutations2, locInfo, calcTime, out delivery);
//                        if (rcFind != 0 || delivery == null) continue;
//                        if (count >= allDeliveries.Length) Array.Resize(ref allDeliveries, allDeliveries.Length + 2048);
//                        allDeliveries[count++] = delivery;

//                        for (int i3 = i2 + 1; i3 < orderCount; i3++)
//                        {
//                            orders[2] = courierOrders[i3];
//                            rcFind = FindSalesmanProblemSolution(shop, orders, 3, shopCourier, isLoop, permutations3, locInfo, calcTime, out delivery);
//                            if (rcFind != 0 || delivery == null) continue;
//                           if (count >= allDeliveries.Length) Array.Resize(ref allDeliveries, allDeliveries.Length + 2048);
//                            allDeliveries[count++] = delivery;

//                            for (int i4 = i3 + 1; i4 < orderCount; i4++)
//                            {
//                                orders[3] = courierOrders[i4];
//                                rcFind = FindSalesmanProblemSolution(shop, orders, 4, shopCourier, isLoop, permutations4, locInfo, calcTime, out delivery);
//                                if (rcFind != 0 || delivery == null) continue;
//                                if (count >= allDeliveries.Length) Array.Resize(ref allDeliveries, allDeliveries.Length + 2048);
//                                allDeliveries[count++] = delivery;

//                                for (int i5 = i4 + 1; i5 < orderCount; i5++)
//                                {
//                                    orders[4] = courierOrders[i5];
//                                    rcFind = FindSalesmanProblemSolution(shop, orders, 5, shopCourier, isLoop, permutations5, locInfo, calcTime, out delivery);
//                                    if (rcFind != 0 || delivery == null) continue;
//                                    if (count >= allDeliveries.Length) Array.Resize(ref allDeliveries, allDeliveries.Length + 2048);
//                                    allDeliveries[count++] = delivery;

//                                    for (int i6 = i5 + 1; i6 < orderCount; i6++)
//                                    {
//                                        orders[5] = courierOrders[i6];
//                                        rcFind = FindSalesmanProblemSolution(shop, orders, 6, shopCourier, isLoop, permutations6, locInfo, calcTime, out delivery);
//                                        if (rcFind != 0 || delivery == null) continue;
//                                        if (count >= allDeliveries.Length) Array.Resize(ref allDeliveries, allDeliveries.Length + 2048);
//                                        allDeliveries[count++] = delivery;

//                                        for (int i7 = i6 + 1; i7 < orderCount; i7++)
//                                        {
//                                            orders[6] = courierOrders[i7];
//                                            rcFind = FindSalesmanProblemSolution(shop, orders, 7, shopCourier, isLoop, permutations7, locInfo, calcTime, out delivery);
//                                            if (rcFind != 0 || delivery == null) continue;
//                                            if (count >= allDeliveries.Length) Array.Resize(ref allDeliveries, allDeliveries.Length + 2048);
//                                            allDeliveries[count++] = delivery;
//                                        }
//                                    }
//                                }
//                            }
//                        }
//                    }
//                }

//                if (count < allDeliveries.Length)
//                {
//                    Array.Resize(ref allDeliveries, count);
//                }

//                allPossibleDeliveries = allDeliveries;

//                // 8. Выход - Ok
//                rc = 0;
//                return rc;
//            }
//            catch (Exception ex)
//            {
//                Helper.WriteErrorToLog($"CreateShopDeliveries7(Shop {shop.Id}, Orders {Helper.ArrayToString(courierOrders.Select(order => order.Id).ToArray())}, courier {shopCourier.Id}, isloop {isLoop}, calcTime {calcTime})");
//                Helper.WriteErrorToLog($"(rc = {rc})");
//                Logger.WriteToLog(ex.ToString());

//                return rc;
//            }
//        }

//        /// <summary>
//        /// Создание отгрузок заказов магазина
//        /// </summary>
//        /// <param name="shop">Магазин</param>
//        /// <param name="courierOrders">Отгружаемые заказы</param>
//        /// <param name="shopCourier">Курьер</param>
//        /// <param name="isLoop">Флаг возврата в магазин: true - требуется возврат в магазин; false - отгрузка завершается после вручения последнего заказа</param>
//        /// <param name="calcTime">Момент расчетов</param>
//        /// <param name="allPossibleDeliveries">Все возможные отгрузки состоящие не более, чем из 6 заказов</param>
//        /// <returns>0 - отгрузки построены; иначе - отгрузки не построены</returns>
//        private int CreateShopDeliveries6(Shop shop, Order[] courierOrders, Courier shopCourier, bool isLoop, DateTime calcTime, out CourierDeliveryInfo[] allPossibleDeliveries)
//        {
//            // 1. Инициализация
//            int rc = 1;
//            allPossibleDeliveries = null;

//            try
//            {
//                // 2. Проверяем исходные 
//                rc = 2;
//                if (shop == null)
//                    return rc;
//                if (shopCourier == null)
//                    return rc;
//                if (courierOrders == null || courierOrders.Length <= 0)
//                    return rc;

//                // 3. Выбираем расстояния и времена движения между точками для заданного способа передвижения
//                rc = 3;
//                int orderCount = courierOrders.Length;
//                Point[,] locInfo = locationManager.GetLocationInfoForType(shopCourier.CourierType.VechicleType);
//                if (locInfo == null || locInfo.Length <= 0)
//                    return rc;

//                // 5. Выделяем память под все возможные отгрузки
//                rc = 5;
//                int size;
//                if (orderCount <= 12)
//                {
//                    size = (int) (Math.Pow(2, orderCount) + 0.5);
//                }
//                else
//                {
//                    size = 4096;
//                }

//                // 6. Извлекаем перестановки
//                rc = 6;
//                int[,] permutations1 = permutations.GetPermutations(1);
//                int[,] permutations2 = (orderCount >= 2 ? permutations.GetPermutations(2) : null);
//                int[,] permutations3 = (orderCount >= 3 ? permutations.GetPermutations(3) : null);
//                int[,] permutations4 = (orderCount >= 4 ? permutations.GetPermutations(4) : null);
//                int[,] permutations5 = (orderCount >= 5 ? permutations.GetPermutations(5) : null);
//                int[,] permutations6 = (orderCount >= 6 ? permutations.GetPermutations(6) : null);

//                // 7. Цикл построения всех возможных отгрузок
//                rc = 7;
//                Order[] orders = new Order[8];
//                CourierDeliveryInfo delivery;
//                CourierDeliveryInfo[] allDeliveries = new CourierDeliveryInfo[size];
//                int count = 0;
//                int rcFind = 1;

//                for (int i1 = 0; i1 < orderCount; i1++)
//                {
//                    orders[0] = courierOrders[i1];
//                    rcFind = FindSalesmanProblemSolution(shop, orders, 1, shopCourier, isLoop, permutations1, locInfo, calcTime, out delivery);
//                    if (rcFind != 0 || delivery == null) continue;
//                    if (count >= allDeliveries.Length) Array.Resize(ref allDeliveries, allDeliveries.Length + 2048);
//                    allDeliveries[count++] = delivery;

//                    for (int i2 = i1 + 1; i2 < orderCount; i2++)
//                    {
//                        orders[1] = courierOrders[i2];
//                        rcFind = FindSalesmanProblemSolution(shop, orders, 2, shopCourier, isLoop, permutations2, locInfo, calcTime, out delivery);
//                        if (rcFind != 0 || delivery == null) continue;
//                        if (count >= allDeliveries.Length) Array.Resize(ref allDeliveries, allDeliveries.Length + 2048);
//                        allDeliveries[count++] = delivery;

//                        for (int i3 = i2 + 1; i3 < orderCount; i3++)
//                        {
//                            orders[2] = courierOrders[i3];
//                            rcFind = FindSalesmanProblemSolution(shop, orders, 3, shopCourier, isLoop, permutations3, locInfo, calcTime, out delivery);
//                            if (rcFind != 0 || delivery == null) continue;
//                           if (count >= allDeliveries.Length) Array.Resize(ref allDeliveries, allDeliveries.Length + 2048);
//                            allDeliveries[count++] = delivery;

//                            for (int i4 = i3 + 1; i4 < orderCount; i4++)
//                            {
//                                orders[3] = courierOrders[i4];
//                                rcFind = FindSalesmanProblemSolution(shop, orders, 4, shopCourier, isLoop, permutations4, locInfo, calcTime, out delivery);
//                                if (rcFind != 0 || delivery == null) continue;
//                                if (count >= allDeliveries.Length) Array.Resize(ref allDeliveries, allDeliveries.Length + 2048);
//                                allDeliveries[count++] = delivery;

//                                for (int i5 = i4 + 1; i5 < orderCount; i5++)
//                                {
//                                    orders[4] = courierOrders[i5];
//                                    rcFind = FindSalesmanProblemSolution(shop, orders, 5, shopCourier, isLoop, permutations5, locInfo, calcTime, out delivery);
//                                    if (rcFind != 0 || delivery == null) continue;
//                                    if (count >= allDeliveries.Length) Array.Resize(ref allDeliveries, allDeliveries.Length + 2048);
//                                    allDeliveries[count++] = delivery;

//                                    for (int i6 = i5 + 1; i6 < orderCount; i6++)
//                                    {
//                                        orders[5] = courierOrders[i6];
//                                        rcFind = FindSalesmanProblemSolution(shop, orders, 6, shopCourier, isLoop, permutations6, locInfo, calcTime, out delivery);
//                                        if (rcFind != 0 || delivery == null) continue;
//                                        if (count >= allDeliveries.Length) Array.Resize(ref allDeliveries, allDeliveries.Length + 2048);
//                                        allDeliveries[count++] = delivery;
//                                    }
//                                }
//                            }
//                        }
//                    }
//                }

//                if (count < allDeliveries.Length)
//                {
//                    Array.Resize(ref allDeliveries, count);
//                }

//                allPossibleDeliveries = allDeliveries;

//                // 8. Выход - Ok
//                rc = 0;
//                return rc;
//            }
//            catch (Exception ex)
//            {
//                Helper.WriteErrorToLog($"CreateShopDeliveries7(Shop {shop.Id}, Orders {Helper.ArrayToString(courierOrders.Select(order => order.Id).ToArray())}, courier {shopCourier.Id}, isloop {isLoop}, calcTime {calcTime})");
//                Helper.WriteErrorToLog($"(rc = {rc})");
//                Logger.WriteToLog(ex.ToString());

//                return rc;
//            }
//        }

//        /// <summary>
//        /// Создание отгрузок заказов магазина
//        /// </summary>
//        /// <param name="shop">Магазин</param>
//        /// <param name="courierOrders">Отгружаемые заказы</param>
//        /// <param name="shopCourier">Курьер</param>
//        /// <param name="isLoop">Флаг возврата в магазин: true - требуется возврат в магазин; false - отгрузка завершается после вручения последнего заказа</param>
//        /// <param name="calcTime">Момент расчетов</param>
//        /// <param name="allPossibleDeliveries">Все возможные отгрузки состоящие не более, чем из 5 заказов</param>
//        /// <returns>0 - отгрузки построены; иначе - отгрузки не построены</returns>
//        private int CreateShopDeliveries5(Shop shop, Order[] courierOrders, Courier shopCourier, bool isLoop, DateTime calcTime, out CourierDeliveryInfo[] allPossibleDeliveries)
//        {
//            // 1. Инициализация
//            int rc = 1;
//            allPossibleDeliveries = null;

//            try
//            {
//                // 2. Проверяем исходные 
//                rc = 2;
//                if (shop == null)
//                    return rc;
//                if (shopCourier == null)
//                    return rc;
//                if (courierOrders == null || courierOrders.Length <= 0)
//                    return rc;

//                // 3. Выбираем расстояния и времена движения между точками для заданного способа передвижения
//                rc = 3;
//                int orderCount = courierOrders.Length;
//                Point[,] locInfo = locationManager.GetLocationInfoForType(shopCourier.CourierType.VechicleType);
//                if (locInfo == null || locInfo.Length <= 0)
//                    return rc;

//                // 5. Выделяем память под все возможные отгрузки
//                rc = 5;
//                int size;
//                if (orderCount <= 12)
//                {
//                    size = (int) (Math.Pow(2, orderCount) + 0.5);
//                }
//                else
//                {
//                    size = 4096;
//                }

//                // 6. Извлекаем перестановки
//                rc = 6;
//                int[,] permutations1 = permutations.GetPermutations(1);
//                int[,] permutations2 = (orderCount >= 2 ? permutations.GetPermutations(2) : null);
//                int[,] permutations3 = (orderCount >= 3 ? permutations.GetPermutations(3) : null);
//                int[,] permutations4 = (orderCount >= 4 ? permutations.GetPermutations(4) : null);
//                int[,] permutations5 = (orderCount >= 5 ? permutations.GetPermutations(5) : null);

//                // 7. Цикл построения всех возможных отгрузок
//                rc = 7;
//                Order[] orders = new Order[8];
//                CourierDeliveryInfo delivery;
//                CourierDeliveryInfo[] allDeliveries = new CourierDeliveryInfo[size];
//                int count = 0;
//                int rcFind = 1;

//                for (int i1 = 0; i1 < orderCount; i1++)
//                {
//                    orders[0] = courierOrders[i1];
//                    rcFind = FindSalesmanProblemSolution(shop, orders, 1, shopCourier, isLoop, permutations1, locInfo, calcTime, out delivery);
//                    if (rcFind != 0 || delivery == null) continue;
//                    if (count >= allDeliveries.Length) Array.Resize(ref allDeliveries, allDeliveries.Length + 2048);
//                    allDeliveries[count++] = delivery;

//                    for (int i2 = i1 + 1; i2 < orderCount; i2++)
//                    {
//                        orders[1] = courierOrders[i2];
//                        rcFind = FindSalesmanProblemSolution(shop, orders, 2, shopCourier, isLoop, permutations2, locInfo, calcTime, out delivery);
//                        if (rcFind != 0 || delivery == null) continue;
//                        if (count >= allDeliveries.Length) Array.Resize(ref allDeliveries, allDeliveries.Length + 2048);
//                        allDeliveries[count++] = delivery;

//                        for (int i3 = i2 + 1; i3 < orderCount; i3++)
//                        {
//                            orders[2] = courierOrders[i3];
//                            rcFind = FindSalesmanProblemSolution(shop, orders, 3, shopCourier, isLoop, permutations3, locInfo, calcTime, out delivery);
//                            if (rcFind != 0 || delivery == null) continue;
//                           if (count >= allDeliveries.Length) Array.Resize(ref allDeliveries, allDeliveries.Length + 2048);
//                            allDeliveries[count++] = delivery;

//                            for (int i4 = i3 + 1; i4 < orderCount; i4++)
//                            {
//                                orders[3] = courierOrders[i4];
//                                rcFind = FindSalesmanProblemSolution(shop, orders, 4, shopCourier, isLoop, permutations4, locInfo, calcTime, out delivery);
//                                if (rcFind != 0 || delivery == null) continue;
//                                if (count >= allDeliveries.Length) Array.Resize(ref allDeliveries, allDeliveries.Length + 2048);
//                                allDeliveries[count++] = delivery;

//                                for (int i5 = i4 + 1; i5 < orderCount; i5++)
//                                {
//                                    orders[4] = courierOrders[i5];
//                                    rcFind = FindSalesmanProblemSolution(shop, orders, 5, shopCourier, isLoop, permutations5, locInfo, calcTime, out delivery);
//                                    if (rcFind != 0 || delivery == null) continue;
//                                    if (count >= allDeliveries.Length) Array.Resize(ref allDeliveries, allDeliveries.Length + 2048);
//                                    allDeliveries[count++] = delivery;
//                                }
//                            }
//                        }
//                    }
//                }

//                if (count < allDeliveries.Length)
//                {
//                    Array.Resize(ref allDeliveries, count);
//                }

//                allPossibleDeliveries = allDeliveries;

//                // 8. Выход - Ok
//                rc = 0;
//                return rc;
//            }
//            catch (Exception ex)
//            {
//                Helper.WriteErrorToLog($"CreateShopDeliveries7(Shop {shop.Id}, Orders {Helper.ArrayToString(courierOrders.Select(order => order.Id).ToArray())}, courier {shopCourier.Id}, isloop {isLoop}, calcTime {calcTime})");
//                Helper.WriteErrorToLog($"(rc = {rc})");
//                Logger.WriteToLog(ex.ToString());

//                return rc;
//            }
//        }

//        /// <summary>
//        /// Создание отгрузок заказов магазина
//        /// </summary>
//        /// <param name="shop">Магазин</param>
//        /// <param name="courierOrders">Отгружаемые заказы</param>
//        /// <param name="shopCourier">Курьер</param>
//        /// <param name="isLoop">Флаг возврата в магазин: true - требуется возврат в магазин; false - отгрузка завершается после вручения последнего заказа</param>
//        /// <param name="calcTime">Момент расчетов</param>
//        /// <param name="allPossibleDeliveries">Все возможные отгрузки состоящие не более, чем из 4 заказов</param>
//        /// <returns>0 - отгрузки построены; иначе - отгрузки не построены</returns>
//        private int CreateShopDeliveries4(Shop shop, Order[] courierOrders, Courier shopCourier, bool isLoop, DateTime calcTime, out CourierDeliveryInfo[] allPossibleDeliveries)
//        {
//            // 1. Инициализация
//            int rc = 1;
//            allPossibleDeliveries = null;

//            try
//            {
//                // 2. Проверяем исходные 
//                rc = 2;
//                if (shop == null)
//                    return rc;
//                if (shopCourier == null)
//                    return rc;
//                if (courierOrders == null || courierOrders.Length <= 0)
//                    return rc;

//                // 3. Выбираем расстояния и времена движения между точками для заданного способа передвижения
//                rc = 3;
//                int orderCount = courierOrders.Length;
//                Point[,] locInfo = locationManager.GetLocationInfoForType(shopCourier.CourierType.VechicleType);
//                if (locInfo == null || locInfo.Length <= 0)
//                    return rc;

//                // 5. Выделяем память под все возможные отгрузки
//                rc = 5;
//                int size;
//                if (orderCount <= 12)
//                {
//                    size = (int) (Math.Pow(2, orderCount) + 0.5);
//                }
//                else
//                {
//                    size = 4096;
//                }

//                // 6. Извлекаем перестановки
//                rc = 6;
//                int[,] permutations1 = permutations.GetPermutations(1);
//                int[,] permutations2 = (orderCount >= 2 ? permutations.GetPermutations(2) : null);
//                int[,] permutations3 = (orderCount >= 3 ? permutations.GetPermutations(3) : null);
//                int[,] permutations4 = (orderCount >= 4 ? permutations.GetPermutations(4) : null);

//                // 7. Цикл построения всех возможных отгрузок
//                rc = 7;
//                Order[] orders = new Order[8];
//                CourierDeliveryInfo delivery;
//                CourierDeliveryInfo[] allDeliveries = new CourierDeliveryInfo[size];
//                int count = 0;
//                int rcFind = 1;

//                for (int i1 = 0; i1 < orderCount; i1++)
//                {
//                    orders[0] = courierOrders[i1];
//                    rcFind = FindSalesmanProblemSolution(shop, orders, 1, shopCourier, isLoop, permutations1, locInfo, calcTime, out delivery);
//                    if (rcFind != 0 || delivery == null) continue;
//                    if (count >= allDeliveries.Length) Array.Resize(ref allDeliveries, allDeliveries.Length + 2048);
//                    allDeliveries[count++] = delivery;

//                    for (int i2 = i1 + 1; i2 < orderCount; i2++)
//                    {
//                        orders[1] = courierOrders[i2];
//                        rcFind = FindSalesmanProblemSolution(shop, orders, 2, shopCourier, isLoop, permutations2, locInfo, calcTime, out delivery);
//                        if (rcFind != 0 || delivery == null) continue;
//                        if (count >= allDeliveries.Length) Array.Resize(ref allDeliveries, allDeliveries.Length + 2048);
//                        allDeliveries[count++] = delivery;

//                        for (int i3 = i2 + 1; i3 < orderCount; i3++)
//                        {
//                            orders[2] = courierOrders[i3];
//                            rcFind = FindSalesmanProblemSolution(shop, orders, 3, shopCourier, isLoop, permutations3, locInfo, calcTime, out delivery);
//                            if (rcFind != 0 || delivery == null) continue;
//                           if (count >= allDeliveries.Length) Array.Resize(ref allDeliveries, allDeliveries.Length + 2048);
//                            allDeliveries[count++] = delivery;

//                            for (int i4 = i3 + 1; i4 < orderCount; i4++)
//                            {
//                                orders[3] = courierOrders[i4];
//                                rcFind = FindSalesmanProblemSolution(shop, orders, 4, shopCourier, isLoop, permutations4, locInfo, calcTime, out delivery);
//                                if (rcFind != 0 || delivery == null) continue;
//                                if (count >= allDeliveries.Length) Array.Resize(ref allDeliveries, allDeliveries.Length + 2048);
//                                allDeliveries[count++] = delivery;
//                            }
//                        }
//                    }
//                }

//                if (count < allDeliveries.Length)
//                {
//                    Array.Resize(ref allDeliveries, count);
//                }

//                allPossibleDeliveries = allDeliveries;

//                // 8. Выход - Ok
//                rc = 0;
//                return rc;
//            }
//            catch (Exception ex)
//            {
//                Helper.WriteErrorToLog($"CreateShopDeliveries7(Shop {shop.Id}, Orders {Helper.ArrayToString(courierOrders.Select(order => order.Id).ToArray())}, courier {shopCourier.Id}, isloop {isLoop}, calcTime {calcTime})");
//                Helper.WriteErrorToLog($"(rc = {rc})");
//                Logger.WriteToLog(ex.ToString());

//                return rc;
//            }
//        }

//        /// <summary>
//        /// Поиск пути с минимальной стоимостью среди заданных перестановок
//        /// </summary>
//        /// <param name="shop">Магазин, из которого осуществляется отгрузка</param>
//        /// <param name="shopOrders">Отгружаемые заказы</param>
//        /// <param name="orderCount">Количество заказов</param>
//        /// <param name="courier">Курьер</param>
//        /// <param name="isLoop">Флаг возврата в магазин: true - требуется возврат в магазин; false - отгрузка завершается после вручения последнего заказа</param>
//        /// <param name="permutations">Перестановки, среди которых осуществляется поиск</param>
//        /// <param name="courierLocationInfo">Расстояния и время движения между точками</param>
//        /// <param name="calcTime">Время, начиная с которого можно использовать курьера</param>
//        /// <param name="bestDelivery">Путь с минимальной стоимостью</param>
//        /// <returns>0 - путь найден; иначе - путь не найден</returns>
//        private static int FindSalesmanProblemSolution(Shop shop, Order[] shopOrders, int orderCount, Courier courier, bool isLoop, int[,] permutations, Point[,] courierLocationInfo, DateTime calcTime, out CourierDeliveryInfo bestDelivery)
//        {
//            // 1. Инициализация
//            int rc = 1;
//            bestDelivery = null;

//            try
//            {
//                // 2. Проверяем исходные данные
//                rc = 2;
//                if (shop == null)
//                    return rc;
//                if (courier == null)
//                    return rc;
//                if (shopOrders == null || shopOrders.Length <= 0)
//                    return rc;
//                if (orderCount < 0 || orderCount >= shopOrders.Length)
//                    return rc;

//                if (courierLocationInfo == null || courierLocationInfo.Length <= 0)
//                    return rc;

//                if (permutations == null || permutations.Length <= 0)
//                    return rc;
//                if (permutations.GetLength(1) != orderCount)
//                    return rc;

//                // 3. Решаем задачу комивояжера - построение пути обхода c минимальной стоимостью
//                rc = 3;
//                int permutationCount = permutations.GetLength(0);
//                Order[] permutOrders = new Order[orderCount];
//                double bestCost = double.MaxValue;
//                int bestOrderCount = -1;

//                for (int i = 0; i < permutationCount; i++)
//                {
//                    // 4.1 Строим перестановку заказов
//                    rc = 41;
//                    for (int j = 0; j < orderCount; j++)
//                    {
//                        permutOrders[j] = shopOrders[permutations[i, j]];
//                    }

//                    // 4.2 Проверяем построенный путь и отбираем наилучший среди всех перестановок
//                    rc = 42;
//                    CourierDeliveryInfo pathInfo;
//                    int rc1 = courier.DeliveryCheck(calcTime, shop, (Order[]) permutOrders.Clone(), isLoop, courierLocationInfo, out pathInfo);
//                    if (rc1 == 0)
//                    {
//                        if (bestOrderCount < pathInfo.OrderCount)
//                        {
//                            bestOrderCount = pathInfo.OrderCount;
//                            bestCost = pathInfo.Cost;
//                            bestDelivery = pathInfo;
//                        }
//                        else if (bestOrderCount == pathInfo.OrderCount && pathInfo.Cost < bestCost)
//                        {
//                            bestCost = pathInfo.Cost;
//                            bestDelivery = pathInfo;
//                        }
//                    }
//                }

//                if (bestDelivery == null)
//                    return rc;

//                // 5. Выход - Ok
//                rc = 0;
//                return rc;
//            }
//            catch (Exception ex)
//            {
//                Helper.WriteErrorToLog($"FindSalesmanProblemSolution(Shop {shop.Id}, Orders {Helper.ArrayToString(shopOrders.Take(orderCount).Select(order => order.Id).ToArray())}, courier {courier.Id}, isloop {isLoop}, calcTime {calcTime})");
//                Helper.WriteErrorToLog($"(rc = {rc})");
//                Logger.WriteToLog(ex.ToString());

//                return rc;
//            }
//        }

//        /// <summary>
//        /// Сравнение двух отгрузок по средней стоимости доставки одного заказа
//        /// </summary>
//        /// <param name="delivery1">Отгрузка 1</param>
//        /// <param name="delivery2">Отгрузка 2</param>
//        /// <returns>-1 - delivery1 меньше delivery2; 0 - delivery1 = delivery2; delivery1 больше delivery2</returns>
//        private static int CmpareByOrderCost(CourierDeliveryInfo delivery1, CourierDeliveryInfo delivery2)
//        {
//            if (delivery1.OrderCost < delivery2.OrderCost)
//                return -1;
//            if (delivery1.OrderCost > delivery2.OrderCost)
//                return 1;
//            return 0;
//        }

//        /// <summary>
//        /// Построение покрытия из заданных отгрузок
//        /// </summary>
//        /// <param name="allDeliveries">
//        /// Все возможные отгрузки, отсортированные по  
//        /// возрастанию средней стоимости доставки одного заказа
//        /// </param>
//        /// <param name="orderCount">Общее количество заказов, для которых строились отгрузки</param>
//        /// <param name="orderCount">Общее количество заказов, для которых строились отгрузки</param>
//        /// <param name="isAssembledOnly">Флаг:
//        /// true - использовать для построения покрытия отгрузки только с собранными заказами; 
//        /// false - использовать для построения покрытия все переданные отгрузки
//        /// </param>
//        /// <param name="orderCoverMap">Фаги заказов, вошедших в построенное покрытие</param>
//        /// <returns>0 - покрытие построено; иначе - покрытие не построено</returns>
//        private static int BuildDeliveryCover(CourierDeliveryInfo[] allDeliveries, int orderCount, bool isAssembledOnly, out CourierDeliveryInfo[] deiveryCover, out bool[] orderCoverMap)
//        {
//            // 1. Инициализация
//            int rc = 1;
//            deiveryCover = null;
//            orderCoverMap = null;

//            try
//            {
//                // 2. Проверяем исходные данные
//                rc = 2;
//                if (allDeliveries == null || allDeliveries.Length <= 0)
//                    return rc;
//                if (orderCount <= 0)
//                    return rc;

//                // 3. Цикл построения покрытия
//                rc = 3;
//                deiveryCover = new CourierDeliveryInfo[orderCount];
//                orderCoverMap = new bool[orderCount];
//                int deliveryCount = 0;
//                int coverCount = 0;

//                for (int i = 0; i < allDeliveries.Length; i++)
//                {
//                    // 3.1 Извлекаем заказы отгрузки
//                    rc = 31;
//                    CourierDeliveryInfo delivery = allDeliveries[i];
//                    Order[] deliveryOrders = delivery.Orders;
//                    if (deliveryOrders == null || deliveryOrders.Length <= 0)
//                        continue;

//                    // 3.2 Фильтруем отгрузки состоящие не только из собранных заказов
//                    rc = 32;
//                    if (isAssembledOnly)
//                    {
//                        for (int j = 0; j < deliveryOrders.Length; j++)
//                        {
//                            if (deliveryOrders[j].Status != OrderStatus.Assembled)
//                                goto NextDelivery;
//                        }
//                    }

//                    // 3.3 Проверяем, что все заказы отгрузки не входят в уже отобранные отгрузки
//                    rc = 33;
//                    for (int j = 0; j < deliveryOrders.Length; j++)
//                    {
//                        if (orderCoverMap[deliveryOrders[j].Index])
//                            goto NextDelivery;
//                    }

//                    // 3.4 Добавляем отгрузку в покрытие
//                    rc = 34;
//                    deiveryCover[deliveryCount++] = delivery;

//                    // 3.5 Помечаем заказы, как попавшие в покрытие
//                    rc = 35;
//                    for (int j = 0; j < deliveryOrders.Length; j++)
//                    {
//                        orderCoverMap[deliveryOrders[j].Index] = true;
//                    }

//                    // 3.6 Если все заказы уже покрыты
//                    rc = 36;
//                    coverCount += deliveryOrders.Length;
//                    if (coverCount >= orderCount)
//                        break;

//                    NextDelivery: ;
//                }

//                if (deliveryCount < deiveryCover.Length)
//                {
//                    Array.Resize(ref deiveryCover, deliveryCount);
//                }

//                // 4. Выход - Ok
//                rc = 0;
//                return rc;
//            }
//            catch
//            {
//                return rc;
//            }
//        }

//        /// <summary>
//        /// Построение покрытия из заданных отгрузок
//        /// </summary>
//        /// <param name="allDeliveries">
//        /// Все возможные отгрузки, отсортированные по  
//        /// возрастанию средней стоимости доставки одного заказа
//        /// </param>
//        /// <param name="orderCount">Общее количество заказов, для которых строились отгрузки</param>
//        /// <param name="isAssembledOnly">Флаг:
//        /// true - использовать для построения покрытия отгрузки только с собранными заказами; 
//        /// false - использовать для построения покрытия все переданные отгрузки
//        /// </param>
//        /// <param name="locationManager">Менеджер расстояний и времени движения между точками</param>
//        /// <param name="orderCoverMap">Фаги заказов, вошедших в построенное покрытие</param>
//        /// <returns>0 - покрытие построено; иначе - покрытие не построено</returns>
//        private static int BuildDeliveryCoverEx(Courier[] shopCouriers, CourierDeliveryInfo[] allDeliveries, int orderCount, bool isAssembledOnly, LocationManager locationManager, out CourierDeliveryInfo[] deiveryCover, out bool[] orderCoverMap)
//        {
//            // 1. Инициализация
//            int rc = 1;
//            deiveryCover = null;
//            orderCoverMap = null;

//            try
//            {
//                // 2. Проверяем исходные данные
//                rc = 2;
//                if (allDeliveries == null || allDeliveries.Length <= 0)
//                    return rc;
//                if (orderCount <= 0)
//                    return rc;

//                // 3. Цикл построения покрытия
//                rc = 3;
//                deiveryCover = new CourierDeliveryInfo[orderCount];
//                orderCoverMap = new bool[orderCount];
//                int deliveryCount = 0;
//                int coverCount = 0;
//                Courier[] availableCourier = new Courier[shopCouriers.Length];
//                shopCouriers.CopyTo(availableCourier, 0);

//                for (int i = 0; i < allDeliveries.Length; i++)
//                {
//                    // 3.1 Извлекаем заказы отгрузки
//                    rc = 31;
//                    CourierDeliveryInfo delivery = allDeliveries[i];
//                    Order[] deliveryOrders = delivery.Orders;
//                    if (deliveryOrders == null || deliveryOrders.Length <= 0)
//                        continue;

//                    // 3.2 Фильтруем отгрузки состоящие не только из собранных заказов
//                    rc = 32;
//                    if (isAssembledOnly)
//                    {
//                        for (int j = 0; j < deliveryOrders.Length; j++)
//                        {
//                            if (deliveryOrders[j].Status != OrderStatus.Assembled)
//                                goto NextDelivery;
//                        }
//                    }

//                    // 3.3 Проверяем, что все заказы отгрузки не входят в уже отобпвнные отгрузки
//                    rc = 33;
//                    for (int j = 0; j < deliveryOrders.Length; j++)
//                    {
//                        if (orderCoverMap[deliveryOrders[j].Index])
//                            goto NextDelivery;
//                    }

//                    // 3.4 Подбираем подходящего доступного курьера 
//                    rc = 34;
//                    CourierDeliveryInfo dstDelivery;
//                    int rc1 = SelectCourier(delivery, ref availableCourier, locationManager, out dstDelivery);
//                    if (rc1 != 0 || dstDelivery == null)
//                        goto NextDelivery;

//                    // 3.5 Добавляем отгрузку в покрытие
//                    rc = 35;
//                    deiveryCover[deliveryCount++] = dstDelivery;

//                    // 3.6 Помечаем заказы, как попавшие в покрытие
//                    rc = 36;
//                    for (int j = 0; j < deliveryOrders.Length; j++)
//                    {
//                        orderCoverMap[deliveryOrders[j].Index] = true;
//                    }

//                    // 3.7 Если все заказы уже покрыты
//                    rc = 37;
//                    coverCount += deliveryOrders.Length;
//                    if (coverCount >= orderCount)
//                        break;

//                    NextDelivery: ;
//                }

//                if (deliveryCount < deiveryCover.Length)
//                {
//                    Array.Resize(ref deiveryCover, deliveryCount);
//                }

//                // 4. Выход - Ok
//                rc = 0;
//                return rc;
//            }
//            catch (Exception ex)
//            {
                
//                Helper.WriteErrorToLog($"BuildDeliveryCoverEx(...)");
//                Helper.WriteErrorToLog($"(rc = {rc})");
//                Logger.WriteToLog(ex.ToString());
//                return rc;
//            }
//        }

//        /// <summary>
//        /// Выбор подходящего курьера или такси для отгрузки
//        /// </summary>
//        /// <param name="srcDelivery">Отгрузка</param>
//        /// <param name="shopCouriers">Доступные курьеры и такси</param>
//        /// <param name="locationManager">Менеджер расстояний и времени движения</param>
//        /// <param name="dstDelivery">Отгрузка для выбранного курьера</param>
//        /// <returns></returns>
//        private static int SelectCourier(CourierDeliveryInfo srcDelivery, ref Courier[] shopCouriers, LocationManager locationManager, out CourierDeliveryInfo dstDelivery)
//        {
//            // 1. Инициализация
//            int rc = 1;
//            dstDelivery = null;

//            try
//            {
//                // 2. Проверяем исходные данные
//                rc = 2;
//                if (srcDelivery == null)
//                    return rc;
//                if (shopCouriers == null || shopCouriers.Length <= 0)
//                    return rc;
//                if (locationManager == null)
//                    return rc;

//                // 3. Ищем подходящего курьера
//                rc = 3;
//                CourierVehicleType srcVehicleType = srcDelivery.DeliveryCourier.CourierType.VechicleType;

//                for (int i = 0; i < shopCouriers.Length; i++)
//                {
//                    Courier courier = shopCouriers[i];
//                    if (courier != null)
//                    {
//                        if (courier.CourierType.VechicleType == srcVehicleType)
//                        {
//                            if (courier.IsTaxi)
//                            {
//                                srcDelivery.DeliveryCourier = courier;
//                                dstDelivery = srcDelivery;
//                                return rc = 0;
//                            }
//                            else
//                            {
//                                Point[,] locInfo = locationManager.GetLocationInfoForType(srcVehicleType);
//                                int rc1 = courier.DeliveryCheck(srcDelivery.CalculationTime, srcDelivery.FromShop, srcDelivery.Orders, srcDelivery.IsLoop, locInfo, out dstDelivery);
//                                if (rc1 == 0 && dstDelivery != null)
//                                {
//                                    srcDelivery.DeliveryCourier = courier;
//                                    shopCouriers[i] = null;
//                                    return rc = 0;
//                                }
//                            }
//                        }
//                    }
//                }

//                dstDelivery = null;

//                // 4. Не удалось подобрать курьера
//                rc = 4;
//                return rc;
//            }
//            catch
//            {
//                return rc;
//            }
//        }
//    }
//}
