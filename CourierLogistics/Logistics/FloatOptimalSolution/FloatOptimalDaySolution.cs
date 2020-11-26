
namespace CourierLogistics.Logistics.FloatOptimalSolution
{
    using CourierLogistics.Logistics.FloatSolution;
    using CourierLogistics.Logistics.FloatSolution.CourierInLive;
    using CourierLogistics.Logistics.FloatSolution.ShopInLive;
    using CourierLogistics.Logistics.RealSingleShopSolution;
    using CourierLogistics.SourceData.Couriers;
    using CourierLogistics.SourceData.Orders;
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Оптимальное решение для свободной модели
    /// на основе реальных данных
    /// </summary>
    public class FloatOptimalDaySolution
    {
        /// <summary>
        /// Флаг: true - решение построено; false - решение не построено
        /// </summary>
        public bool IsCreated { get; private set; }

        /// <summary>
        /// Прерывание возникшее во время построения решения
        /// </summary>
        public Exception LastException { get; private set; }

        /// <summary>
        /// Расчет за один день
        /// </summary>
        /// <param name="shops">Магазины </param>
        /// <param name="couriers">Курьры</param>
        /// <param name="oneDayOrders">Заказы за день</param>
        /// <param name="statistics">Статистика заказов</param>
        /// <returns>0 - день начат; иначе - день не начат</returns>
        public int Create(ShopEx[] shops, Order[] oneDayOrders, ShopStatistics statistics, out CourierDeliveryInfo[] oneDayDeliveries)
        {
            // 1. Инициализация
            int rc = 1;
            int rc1 = 1;
            oneDayDeliveries = null;
            IsCreated = false;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (oneDayOrders == null || oneDayOrders.Length <= 0)
                    return rc;
                if (shops == null || shops.Length <= 0)
                    return rc;

                // 3. Сортируем заказы по магазину
                rc = 3;
                Array.Sort(oneDayOrders, CompareByShopAndCollectedDate);

                // 4. Сортируем магазины по Id
                rc = 4;
                int[] shopId = shops.Select(p => p.N).ToArray();
                Array.Sort(shopId, shops);

                // 5. Инициализируем магазины
                rc = 5;
                int currentShopId = oneDayOrders[0].ShopNo;
                int startIndex = 0;
                int shopIndex;
                int orderCount;
                Order[] shopOrders;

                for (int i = 1; i < oneDayOrders.Length; i++)
                {
                    if (oneDayOrders[i].ShopNo != currentShopId)
                    {
                        shopIndex = Array.BinarySearch(shopId, currentShopId);
                        if (shopIndex >= 0)
                        {
                            orderCount = i - startIndex;
                            shopOrders = new Order[orderCount];
                            Array.Copy(oneDayOrders, startIndex, shopOrders, 0, orderCount);
                            rc1 = shops[shopIndex].StartOptimalDay(shopOrders, statistics);
                        }

                        startIndex = i;
                        currentShopId = oneDayOrders[i].ShopNo;
                    }
                }


                shopIndex = Array.BinarySearch(shopId, currentShopId);
                if (shopIndex >= 0)
                {
                    orderCount = oneDayOrders.Length - startIndex;
                    shopOrders = new Order[orderCount];
                    Array.Copy(oneDayOrders, startIndex, shopOrders, 0, orderCount);
                    rc1 = shops[shopIndex].StartOptimalDay(shopOrders, statistics);
                }

                // 6. Многопоточное построение покрытий для всех магазинов
                rc = 6;
                int threadCount = 4;
                Task<int>[] coverThread = new Task<int>[threadCount];

                for (int i = 2; i <= 8; i++)
                {
                    ShopEx.PermutationsRepository.GetPermutations(i);
                }

                Array.Sort(shops, CompareShopsByOrderCount);

                coverThread[0] = Task.Run(() => CreateCover(shops, 0, threadCount));
                coverThread[1] = Task.Run(() => CreateCover(shops, 1, threadCount));
                coverThread[2] = Task.Run(() => CreateCover(shops, 2, threadCount));
                coverThread[3] = Task.Run(() => CreateCover(shops, 3, threadCount));

                Task.WaitAll(coverThread);

                // 6. Цикл отгрузки заказов с помощью курьеров на авто
                rc = 6;
                DateTime currentDate = oneDayOrders[0].Date_collected.Date;
                CourierEx[] dayCouriers = new CourierEx[oneDayOrders.Length];
                int courierCount = 0;
                oneDayDeliveries = new CourierDeliveryInfo[oneDayOrders.Length];
                int oneDayDeliveryCount = 0;

                int courierId = 2;

                for (int i = 0; i < oneDayOrders.Length; i++)
                {
                    // 6.1 Создаём нового курьера на авто
                    rc = 61;
                    CourierEx carCourier = new CourierEx(++courierId, new CourierType_Car());
                    carCourier.Status = CourierStatus.Ready;
                    carCourier.WorkStart = new TimeSpan(0);
                    carCourier.WorkEnd = new TimeSpan(24, 0, 0);

                    // 6.2 Строим для него решения
                    rc = 62;
                    int saveDeliveryCount = oneDayDeliveryCount;
                    rc1 = DayCourierSolution(carCourier, shops, ref oneDayDeliveries, ref oneDayDeliveryCount);
                    if (rc1 == 0 && oneDayDeliveryCount > saveDeliveryCount)
                    {
                        TimeSpan wt = oneDayDeliveries[oneDayDeliveryCount - 1].StartDelivery.AddMinutes(oneDayDeliveries[oneDayDeliveryCount - 1].DeliveryTime) - oneDayDeliveries[saveDeliveryCount].StartDelivery;
                        int hh = wt.Hours;
                        if (wt.Minutes > 0 || wt.Seconds > 0)
                            hh++;
                        if (hh < 4)
                        {
                            for (int j = saveDeliveryCount; j < oneDayDeliveryCount; j++)
                            {
                                CourierDeliveryInfo saveDilivery = oneDayDeliveries[j];
                                if (saveDilivery.DeliveredOrders != null)
                                {
                                    foreach (var di in saveDilivery.DeliveredOrders)
                                    {
                                        di.Completed = false;
                                    }
                                }
                                else
                                {
                                    saveDilivery.ShippingOrder.Completed = false;
                                }
                            }

                            oneDayDeliveryCount = saveDeliveryCount;
                            courierId--;
                            break;
                        }

                        dayCouriers[courierCount++] = carCourier;

                        int sum = 0;
                        for (int ii = saveDeliveryCount; ii < oneDayDeliveryCount; ii++)
                        {
                            sum += oneDayDeliveries[ii].OrderCount;
                        }

                        double cost = 250.0 * hh / sum;
                        int cnt = oneDayDeliveries.Take(oneDayDeliveryCount).Sum(p => p.OrderCount);
                        Console.WriteLine($"{DateTime.Now} > couriers = {courierCount}, deliveries = {cnt}, car oreder cost = {cost: 0.00}");
                    }
                    else
                    {
                        courierId--;
                        break;
                    }
                }

                // 7. Цикл отгрузки заказов с помощью курьеров на велосипеде
                rc = 7;
                for (int i = 0; i < oneDayOrders.Length; i++)
                {
                    // 7.1 Создаём нового курьера на велосипеде
                    rc = 61;
                    CourierEx bicycleCourier = new CourierEx(++courierId, new CourierType_Bicycle());
                    bicycleCourier.Status = CourierStatus.Ready;
                    bicycleCourier.WorkStart = new TimeSpan(0);
                    bicycleCourier.WorkEnd = new TimeSpan(24, 0, 0);

                    // 6.2 Строим для него решения
                    rc = 62;
                    int saveDeliveryCount = oneDayDeliveryCount;
                    rc1 = DayCourierSolution(bicycleCourier, shops, ref oneDayDeliveries, ref oneDayDeliveryCount);
                    if (rc1 == 0 && oneDayDeliveryCount > saveDeliveryCount)
                    {
                        dayCouriers[courierCount++] = bicycleCourier;

                        int sum = 0;
                        for (int ii = saveDeliveryCount; ii < oneDayDeliveryCount; ii++)
                        {
                            sum += oneDayDeliveries[ii].OrderCount;
                        }
                        TimeSpan wt = oneDayDeliveries[oneDayDeliveryCount - 1].StartDelivery.AddMinutes(oneDayDeliveries[oneDayDeliveryCount - 1].DeliveryTime) - oneDayDeliveries[saveDeliveryCount].StartDelivery;
                        int hh = wt.Hours;
                        if (wt.Minutes > 0 || wt.Seconds > 0)
                            hh++;
                        double cost = 200.0 * hh / sum;
                        int cnt = oneDayDeliveries.Take(oneDayDeliveryCount).Sum(p => p.OrderCount);
                        Console.WriteLine($"{DateTime.Now} > couriers = {courierCount}, deliveries = {cnt}, bicycle oreder cost = {cost: 0.00}");
                    }
                    else
                    {
                        courierId--;
                        break;
                    }
                }

                // 8. Создаём два типа такси
                rc = 8;
                CourierEx gettTaxi = new CourierEx(2, new CourierType_GettTaxi());
                gettTaxi.Status = CourierStatus.Ready;
                gettTaxi.WorkStart = new TimeSpan(0);
                gettTaxi.WorkEnd = new TimeSpan(24, 0, 0);
                dayCouriers[courierCount++] = gettTaxi;

                CourierEx yandexTaxi = new CourierEx(1, new CourierType_YandexTaxi());
                yandexTaxi.Status = CourierStatus.Ready;
                yandexTaxi.WorkStart = new TimeSpan(0);
                yandexTaxi.WorkEnd = new TimeSpan(24, 0, 0);
                dayCouriers[courierCount++] = yandexTaxi;


                CourierEx[] taxi = new CourierEx[] { gettTaxi, yandexTaxi };

                // 9. Находим оптимальнoе решения для доставки с помощью такси
                //    для оставшихся не отгруженными заказов
                rc = 9;
                DayTaxiSolution(taxi, shops, ref oneDayDeliveries, ref oneDayDeliveryCount);

                // 10. Обрезаем пустой хвост для хранилища отгрузок
                rc = 10;
                if (oneDayDeliveryCount < oneDayDeliveries.Length)
                {
                    Array.Resize(ref oneDayDeliveries, oneDayDeliveryCount);
                }

                // 11. Выход - Ok
                rc = 0;
                IsCreated = true;
                return rc;
            }
            catch (Exception ex)
            {
                LastException = ex;
                return rc;
            }
        }

        /// <summary>
        /// Расчет за один день
        /// </summary>
        /// <param name="shops">Магазины </param>
        /// <param name="couriers">Курьры</param>
        /// <param name="oneDayOrders">Заказы за день</param>
        /// <param name="statistics">Статистика заказов</param>
        /// <returns>0 - день начат; иначе - день не начат</returns>
        public int CreateEx(ShopEx[] shops, Order[] oneDayOrders, ShopStatistics statistics, out CourierDeliveryInfo[] oneDayDeliveries)
        {
            // 1. Инициализация
            int rc = 1;
            int rc1 = 1;
            oneDayDeliveries = null;
            IsCreated = false;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (oneDayOrders == null || oneDayOrders.Length <= 0)
                    return rc;
                if (shops == null || shops.Length <= 0)
                    return rc;

                // 3. Сортируем заказы по магазину
                rc = 3;
                Array.Sort(oneDayOrders, CompareByShopAndCollectedDate);

                // 4. Сортируем магазины по Id
                rc = 4;
                int[] shopId = shops.Select(p => p.N).ToArray();
                Array.Sort(shopId, shops);

                // 5. Инициализируем магазины
                rc = 5;
                int currentShopId = oneDayOrders[0].ShopNo;
                int startIndex = 0;
                int shopIndex;
                int orderCount;
                Order[] shopOrders;

                for (int i = 1; i < oneDayOrders.Length; i++)
                {
                    if (oneDayOrders[i].ShopNo != currentShopId)
                    {
                        shopIndex = Array.BinarySearch(shopId, currentShopId);
                        if (shopIndex >= 0)
                        {
                            orderCount = i - startIndex;
                            shopOrders = new Order[orderCount];
                            Array.Copy(oneDayOrders, startIndex, shopOrders, 0, orderCount);
                            rc1 = shops[shopIndex].StartOptimalDay(shopOrders, statistics);
                        }

                        startIndex = i;
                        currentShopId = oneDayOrders[i].ShopNo;
                    }
                }


                shopIndex = Array.BinarySearch(shopId, currentShopId);
                if (shopIndex >= 0)
                {
                    orderCount = oneDayOrders.Length - startIndex;
                    shopOrders = new Order[orderCount];
                    Array.Copy(oneDayOrders, startIndex, shopOrders, 0, orderCount);
                    rc1 = shops[shopIndex].StartOptimalDay(shopOrders, statistics);
                }

                // 6. Многопоточное построение покрытий для всех магазинов
                rc = 6;
                int threadCount = 4;
                Task<int>[] coverThread = new Task<int>[threadCount];

                for (int i = 2; i <= 8; i++)
                {
                    ShopEx.PermutationsRepository.GetPermutations(i);
                }

                Array.Sort(shops, CompareShopsByOrderCount);

                coverThread[0] = Task.Run(() => CreateCover(shops, 0, threadCount));
                coverThread[1] = Task.Run(() => CreateCover(shops, 1, threadCount));
                coverThread[2] = Task.Run(() => CreateCover(shops, 2, threadCount));
                coverThread[3] = Task.Run(() => CreateCover(shops, 3, threadCount));

                Task.WaitAll(coverThread);

                // 6. Цикл отгрузки заказов с помощью курьеров на авто
                rc = 6;
                DateTime currentDate = oneDayOrders[0].Date_collected.Date;
                CourierEx[] dayCouriers = new CourierEx[oneDayOrders.Length];
                int courierCount = 0;
                oneDayDeliveries = new CourierDeliveryInfo[oneDayOrders.Length];
                int oneDayDeliveryCount = 0;

                int courierId = 2;

                for (int i = 0; i < oneDayOrders.Length; i++)
                {
                    // 6.1 Создаём нового курьера на авто
                    rc = 61;
                    CourierEx carCourier = new CourierEx(++courierId, new CourierType_Car());
                    carCourier.Status = CourierStatus.Ready;
                    carCourier.WorkStart = new TimeSpan(0);
                    carCourier.WorkEnd = new TimeSpan(24, 0, 0);

                    // 6.2 Строим для него решения
                    rc = 62;
                    int saveDeliveryCount = oneDayDeliveryCount;
                    rc1 = DayCourierSolutionEx(carCourier, shops, ref oneDayDeliveries, ref oneDayDeliveryCount);
                    if (rc1 == 0 && oneDayDeliveryCount > saveDeliveryCount)
                    {
                        TimeSpan wt = oneDayDeliveries[oneDayDeliveryCount - 1].StartDelivery.AddMinutes(oneDayDeliveries[oneDayDeliveryCount - 1].DeliveryTime) - oneDayDeliveries[saveDeliveryCount].StartDeliveryInterval;
                        int hh = wt.Hours;
                        if (wt.Minutes > 0 || wt.Seconds > 0)
                            hh++;
                        //if (hh < 4)
                        //{
                        //    for (int j = saveDeliveryCount; j < oneDayDeliveryCount; j++)
                        //    {
                        //        CourierDeliveryInfo saveDilivery = oneDayDeliveries[j];
                        //        if (saveDilivery.DeliveredOrders != null)
                        //        {
                        //            foreach (var di in saveDilivery.DeliveredOrders)
                        //            {
                        //                di.Completed = false;
                        //            }
                        //        }
                        //        else
                        //        {
                        //            saveDilivery.ShippingOrder.Completed = false;
                        //        }
                        //    }

                        //    oneDayDeliveryCount = saveDeliveryCount;
                        //    courierId--;
                        //    break;
                        //}

                        dayCouriers[courierCount++] = carCourier;

                        int sum = 0;
                        for (int ii = saveDeliveryCount; ii < oneDayDeliveryCount; ii++)
                        {
                            sum += oneDayDeliveries[ii].OrderCount;
                        }

                        double cost = 250.0 * hh / sum;
                        //int cnt = oneDayDeliveries.Take(oneDayDeliveryCount).Sum(p => p.OrderCount);
                        int complCount = oneDayOrders.Count(P => P.Completed);
                        Console.WriteLine($"{DateTime.Now} > couriers = {courierCount}, deliveries = {complCount}, car oreder cost = {cost: 0.00} work time = {hh}");
                    }
                    else
                    {
                        courierId--;
                        break;
                    }
                }

                // 7. Цикл отгрузки заказов с помощью курьеров на велосипеде
                rc = 7;
                for (int i = 0; i < oneDayOrders.Length; i++)
                {
                    // 7.1 Создаём нового курьера на велосипеде
                    rc = 61;
                    CourierEx bicycleCourier = new CourierEx(++courierId, new CourierType_Bicycle());
                    bicycleCourier.Status = CourierStatus.Ready;
                    bicycleCourier.WorkStart = new TimeSpan(0);
                    bicycleCourier.WorkEnd = new TimeSpan(24, 0, 0);

                    // 6.2 Строим для него решения
                    rc = 62;
                    int saveDeliveryCount = oneDayDeliveryCount;
                    rc1 = DayCourierSolution(bicycleCourier, shops, ref oneDayDeliveries, ref oneDayDeliveryCount);
                    if (rc1 == 0 && oneDayDeliveryCount > saveDeliveryCount)
                    {
                        dayCouriers[courierCount++] = bicycleCourier;

                        int sum = 0;
                        for (int ii = saveDeliveryCount; ii < oneDayDeliveryCount; ii++)
                        {
                            sum += oneDayDeliveries[ii].OrderCount;
                        }
                        TimeSpan wt = oneDayDeliveries[oneDayDeliveryCount - 1].StartDelivery.AddMinutes(oneDayDeliveries[oneDayDeliveryCount - 1].DeliveryTime) - oneDayDeliveries[saveDeliveryCount].StartDelivery;
                        int hh = wt.Hours;
                        if (wt.Minutes > 0 || wt.Seconds > 0)
                            hh++;
                        double cost = 200.0 * hh / sum;
                        int cnt = oneDayDeliveries.Take(oneDayDeliveryCount).Sum(p => p.OrderCount);
                        Console.WriteLine($"{DateTime.Now} > couriers = {courierCount}, deliveries = {cnt}, bicycle oreder cost = {cost: 0.00}");
                    }
                    else
                    {
                        courierId--;
                        break;
                    }
                }

                // 8. Создаём два типа такси
                rc = 8;
                CourierEx gettTaxi = new CourierEx(2, new CourierType_GettTaxi());
                gettTaxi.Status = CourierStatus.Ready;
                gettTaxi.WorkStart = new TimeSpan(0);
                gettTaxi.WorkEnd = new TimeSpan(24, 0, 0);
                dayCouriers[courierCount++] = gettTaxi;

                CourierEx yandexTaxi = new CourierEx(1, new CourierType_YandexTaxi());
                yandexTaxi.Status = CourierStatus.Ready;
                yandexTaxi.WorkStart = new TimeSpan(0);
                yandexTaxi.WorkEnd = new TimeSpan(24, 0, 0);
                dayCouriers[courierCount++] = yandexTaxi;


                CourierEx[] taxi = new CourierEx[] { gettTaxi, yandexTaxi };

                // 9. Находим оптимальнoе решения для доставки с помощью такси
                //    для оставшихся не отгруженными заказов
                rc = 9;
                DayTaxiSolution(taxi, shops, ref oneDayDeliveries, ref oneDayDeliveryCount);

                // 10. Обрезаем пустой хвост для хранилища отгрузок
                rc = 10;
                if (oneDayDeliveryCount < oneDayDeliveries.Length)
                {
                    Array.Resize(ref oneDayDeliveries, oneDayDeliveryCount);
                }

                // 11. Выход - Ok
                rc = 0;
                IsCreated = true;
                return rc;
            }
            catch (Exception ex)
            {
                LastException = ex;
                return rc;
            }
        }

        private static int CreateCover(ShopEx[] shops, int startIndex, int step)
        {
            // 1. Инициализация
            int rc = 1;
            Console.WriteLine($"{DateTime.Now} > {Thread.CurrentThread.ManagedThreadId}. StartIndex = {startIndex}. Step = {step}");

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (shops == null || shops.Length <= 0)
                    return rc;
                if (startIndex < 0 || startIndex >= shops.Length)
                    return rc;
                if (step <= 0)
                    return rc;

                // 3. Цикл построения покрытий магазинов
                rc = 3;
                bool isOk = true;


                for (int i = startIndex; i < shops.Length; i += step)
                {
                    int rc1 = shops[i].CreateCover(2);
                    if (rc1 == 0)
                    {
                        Console.WriteLine($"{DateTime.Now} > {i}. Shop = {shops[i].N}. Order Count = {shops[i].GetDeliveryInfo(2).Length}. Cover Count = {shops[i].GetCourierCover(2).Length}");
                    }
                    //else
                    //{
                    //    isOk = false;
                    //    Console.WriteLine($"{i}. Shop = {shops[i].N}. Order Count = {shops[i].GetDeliveryInfo(2).Length}. rc = {rc1}");
                    //}
                }

                if (!isOk)
                    return rc;

                // 4. Выход 
                rc = 0;
                return rc;
            }
            catch
            {
                return rc;
            }
        }

        /// <summary>
        /// Построение оптимальных отгрузок для заданного курьера
        /// за один рабочий день
        /// </summary>
        /// <param name="courier">Курьер</param>
        /// <param name="shops">Магазины с заказами одного дня</param>
        /// <returns>0 - отгрузки построены; иначе - отгрузки не построены</returns>
        private static int DayCourierSolution(CourierEx courier, ShopEx[] shops, ref CourierDeliveryInfo[] allDayDeliveries, ref int deliveryCount)
        {
            // 1. Инициализация
            int rc = 1;
            int rc1 = 1;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (courier == null)
                    return rc;
                if (shops == null || shops.Length <= 0)
                    return rc;

                if (allDayDeliveries == null)
                    return rc;
                if (deliveryCount < 0 || deliveryCount > allDayDeliveries.Length)
                    return rc;

                // 3. Выбираем лучшую отгрузку среди всех магазинов
                rc = 3;
                DateTime currentDate = shops[0].GetFirstOrderAssembledDate();
                //DateTime arrivalTime = currentDate.AddHours(10);
                //double arrivalCost = 0;
                TimeSpan waitInterval = TimeSpan.FromHours(3);
                double avgMinCost = double.MaxValue;
                CourierDeliveryInfo bestDelivery = null;

                for (int i = 0; i < shops.Length; i++)
                {
                    ShopEx shop = shops[i];
                    CourierDeliveryInfo bestShopDelivery;
                    rc1 = shop.FindDeliveryWithMinAverageCost(courier, DateTime.MaxValue, 0, waitInterval, FloatSolutionParameters.MAX_ORDERS_FOR_OPTIMAL_SOLUTION, out bestShopDelivery);
                    if (rc1 == 0)
                    {
                        if (bestShopDelivery.OrderCost < avgMinCost)
                        {
                            avgMinCost = bestShopDelivery.OrderCost;
                            bestDelivery = bestShopDelivery;
                        }
                    }
                }

                // 4. Если первая лучшая отгрузка не найдена
                rc = 4;
                if (bestDelivery == null)
                    return rc;

                if (deliveryCount >= allDayDeliveries.Length)
                {
                    Array.Resize(ref allDayDeliveries, allDayDeliveries.Length + 1000);
                }

                allDayDeliveries[deliveryCount++] = bestDelivery;

                // 5. Помечаем заказы, как отгруженные и устанавливаем координаты местоположения курьера
                rc = 5;
                courier.WorkStart = bestDelivery.StartDelivery.TimeOfDay;

                if (bestDelivery.DeliveredOrders != null && bestDelivery.DeliveredOrders.Length > 0)
                {
                    foreach (CourierDeliveryInfo di in bestDelivery.DeliveredOrders)
                        di.Completed = true;
                    Order lastDeliveryOrder = bestDelivery.DeliveredOrders[bestDelivery.OrderCount - 1].ShippingOrder;
                    courier.Latitude = lastDeliveryOrder.Latitude;
                    courier.Longitude = lastDeliveryOrder.Longitude;
                }
                else
                {
                    Order singleOrder = bestDelivery.ShippingOrder;
                    singleOrder.Completed = true;
                    courier.Latitude = singleOrder.Latitude;
                    courier.Longitude = singleOrder.Longitude;
                }

                // 6. Цикл работы одного курьера до конца рабочего времени
                rc = 6;
                double[] distFromShop = new double[shops.Length];
                ShopEx[] reachableShops = new ShopEx[shops.Length];
                waitInterval = TimeSpan.FromHours(2);

                while (true)
                {
                    // 6.1 Определяем время, когда курьер освободится
                    rc = 61;
                    DateTime deliveryEnd = bestDelivery.StartDelivery.AddMinutes(bestDelivery.DeliveryTime + courier.CourierType.HandInTime);
                    if ((deliveryEnd.TimeOfDay - courier.WorkStart).TotalHours > FloatSolutionParameters.COURIER_WORK_TIME_LIMIT)
                        break;

                    // 6.2 Определяем достижимые курьром магазины из текущего местоположения
                    rc = 62;
                    double carLatitude = courier.Latitude;
                    double carLongitude = courier.Longitude;
                    int reachableCount = 0;

                    for (int i = 0; i < shops.Length; i++)
                    {
                        ShopEx shop = shops[i];
                        double dist = FloatSolutionParameters.DISTANCE_ALLOWANCE * Helper.Distance(carLatitude, carLongitude, shop.Latitude, shop.Longitude);
                        distFromShop[i] = dist;
                        if (dist <= FloatSolutionParameters.MAX_DISTANCE_TO_AVAILABLE_SHOP)
                        {
                            reachableShops[reachableCount++] = shop;
                        }
                    }

                    ShopEx[] availableShops;
                    if (reachableCount >= FloatSolutionParameters.MIN_AVAILABLE_SHOP_COUNT)
                    {
                        availableShops = new ShopEx[reachableCount];
                        Array.Copy(reachableShops, 0, availableShops, 0, reachableCount);
                    }
                    else
                    {
                        Array.Sort(distFromShop, shops);
                        availableShops = new ShopEx[FloatSolutionParameters.MIN_AVAILABLE_SHOP_COUNT];
                        Array.Copy(shops, 0, availableShops, 0, FloatSolutionParameters.MIN_AVAILABLE_SHOP_COUNT);
                    }

                    // 6.3 Выбираем лучшую отгрузку
                    rc = 63;
                    avgMinCost = double.MaxValue;
                    bestDelivery = null;

                    for (int i = 0; i < availableShops.Length; i++)
                    {
                        ShopEx shop = availableShops[i];
                        CourierDeliveryInfo bestShopDelivery;
                        double dt = courier.GetArrivalTime(shop.Latitude, shop.Longitude);
                        double arrivalCost = dt * (1 + courier.CourierType.Insurance) * courier.CourierType.HourlyRate / 60;
                        DateTime arrivalTime = deliveryEnd.AddMinutes(courier.GetArrivalTime(shop.Latitude, shop.Longitude));
                        rc1 = shop.FindDeliveryWithMinAverageCost(courier, arrivalTime, arrivalCost, waitInterval, FloatSolutionParameters.MAX_ORDERS_FOR_OPTIMAL_SOLUTION, out bestShopDelivery);
                        if (rc1 == 0)
                        {
                            if (bestShopDelivery.OrderCost < avgMinCost)
                            {
                                avgMinCost = bestShopDelivery.OrderCost;
                                bestDelivery = bestShopDelivery;
                            }
                        }
                    }

                    // 6.4 Если отгрузка не найдена
                    if (bestDelivery == null)
                        break;

                    if (deliveryCount >= allDayDeliveries.Length)
                    {
                        Array.Resize(ref allDayDeliveries, allDayDeliveries.Length + 1000);
                    }

                    allDayDeliveries[deliveryCount++] = bestDelivery;

                    // 6.5 Помечаем заказы, как отгруженные
                    rc = 65;
                    if (bestDelivery.DeliveredOrders != null && bestDelivery.DeliveredOrders.Length > 0)
                    {
                        foreach (CourierDeliveryInfo di in bestDelivery.DeliveredOrders)
                            di.Completed = true;
                        Order lastDeliveryOrder = bestDelivery.DeliveredOrders[bestDelivery.OrderCount - 1].ShippingOrder;
                        courier.Latitude = lastDeliveryOrder.Latitude;
                        courier.Longitude = lastDeliveryOrder.Longitude;
                    }
                    else
                    {
                        Order singleOrder = bestDelivery.ShippingOrder;
                        singleOrder.Completed = true;
                        courier.Latitude = singleOrder.Latitude;
                        courier.Longitude = singleOrder.Longitude;
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

        /// <summary>
        /// Построение оптимальных отгрузок для заданного курьера
        /// за один рабочий день
        /// </summary>
        /// <param name="taxi">Таси</param>
        /// <param name="shops">Магазины с заказами одного дня</param>
        /// <returns>0 - отгрузки построены; иначе - отгрузки не построены</returns>
        private static int DayTaxiSolution(CourierEx[] taxi, ShopEx[] shops, ref CourierDeliveryInfo[] allDayDeliveries, ref int deliveryCount)
        {
            // 1. Инициализация
            int rc = 1;
            int rc1 = 1;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (taxi == null || taxi.Length <= 0)
                    return rc;

                for (int i = 0; i < taxi.Length; i++)
                {
                    if (!taxi[i].IsTaxi)
                        return rc;
                }

                if (shops == null || shops.Length <= 0)
                    return rc;

                if (allDayDeliveries == null)
                    return rc;
                if (deliveryCount < 0 || deliveryCount > allDayDeliveries.Length)
                    return rc;

                // 3. Цикл построения для каждого магазина отдельно
                rc = 3;
                DateTime currentDate = shops[0].GetFirstOrderAssembledDate();

                for (int i = 0; i < shops.Length; i++)
                {
                    ShopEx shop = shops[i];

                    // 3.1 Находим отгрузки, пока они есть
                    rc = 31;
                    while (true)
                    {
                        double bestDeliveryOrderCost = double.MaxValue;
                        CourierDeliveryInfo bestTaxiDelivery = null;

                        // 3.2 Находим лучшую отгрузку среди всех такси в текущем сотоянии
                        rc = 32;
                        for (int j = 0; j < taxi.Length; j++)
                        {
                            CourierDeliveryInfo taxiDelivery;
                            rc1 = shop.FindOptimalTaxiDelivery(taxi[j], FloatSolutionParameters.MAX_ORDERS_FOR_OPTIMAL_SOLUTION, out taxiDelivery);
                            if (rc1 == 0 && taxiDelivery != null)
                            {
                                if (taxiDelivery.OrderCost < bestDeliveryOrderCost)
                                {
                                    bestDeliveryOrderCost = taxiDelivery.OrderCost;
                                    bestTaxiDelivery = taxiDelivery;
                                }
                            }
                        }

                        // 3.3 Если отгрузок в магазине больше нет
                        rc = 33;
                        if (bestTaxiDelivery == null)
                            break;

                        // 3.4 Помечаем заказы, как отгруженные
                        rc = 34;
                        if (bestTaxiDelivery.DeliveredOrders != null && bestTaxiDelivery.DeliveredOrders.Length > 0)
                        {
                            foreach (CourierDeliveryInfo di in bestTaxiDelivery.DeliveredOrders)
                                di.Completed = true;
                        }
                        else
                        {
                            bestTaxiDelivery.ShippingOrder.Completed = true;
                        }



                        // 3.5 Пополняем хранилище дневных отгрузок
                        rc = 35;
                        if (deliveryCount >= allDayDeliveries.Length)
                        {
                            Array.Resize(ref allDayDeliveries, allDayDeliveries.Length + 1000);
                        }

                        allDayDeliveries[deliveryCount++] = bestTaxiDelivery;
                    }
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
        /// Сравнение заказов по магазину и времени сборки
        /// </summary>
        /// <param name="order1">Заказ 1</param>
        /// <param name="order2">Заказ 2</param>
        /// <returns>-1 - Заказ1 &lt; Заказ2; 0 - Заказ1 = Заказ2; 1 - Заказ1 &gt; Заказ2</returns>
        private static int CompareByShopAndCollectedDate(Order order1, Order order2)
        {
            if (order1.ShopNo < order2.ShopNo)
                return -1;
            if (order1.ShopNo > order2.ShopNo)
                return 1;
            if (order1.Date_collected < order2.Date_collected)
                return -1;
            if (order1.Date_collected > order2.Date_collected)
                return 1;
            return 0;
        }

        /// <summary>
        /// Сравнение магазинов по количеству заказов
        /// </summary>
        /// <param name="shop1">Магазин 1</param>
        /// <param name="shop2">Магазин 2</param>
        /// <returns>-1, 0, или 1</returns>
        private static int CompareShopsByOrderCount(ShopEx shop1, ShopEx shop2)
        {
            if (shop1.OrderCount < shop2.OrderCount)
                return 1;
            if (shop1.OrderCount > shop2.OrderCount)
                return -1;
            return 0;
        }


        /// <summary>
        /// Построение оптимальных отгрузок для заданного курьера
        /// за один рабочий день
        /// </summary>
        /// <param name="courier">Курьер</param>
        /// <param name="shops">Магазины с заказами одного дня</param>
        /// <param name="allDayDeliveries">Хранилище найденных отгрузок</param>
        /// <param name="deliveryCount">Чило отгрузок в хранилище</param>
        /// <returns>0 - отгрузки построены; иначе - отгрузки не построены</returns>
        private static int DayCourierSolutionEx(CourierEx courier, ShopEx[] shops, ref CourierDeliveryInfo[] allDayDeliveries, ref int deliveryCount)
        {
            // 1. Инициализация
            int rc = 1;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (courier == null)
                    return rc;
                if (shops == null || shops.Length <= 0)
                    return rc;

                if (allDayDeliveries == null)
                    return rc;
                if (deliveryCount < 0 || deliveryCount > allDayDeliveries.Length)
                    return rc;

                // 3. Перебираем все магазины, как начальную точку отгрузки
                //    и пытаемся найти последовательность отгрузок с минимальной средней стоимостью
                rc = 3;
                CourierDeliveryInfo[] courierBestOneDayDeliveries = null;
                double bestOneDayOrderCost = double.MaxValue;
                CourierDeliveryInfo[] oneDayDeliveries = new CourierDeliveryInfo[100];
                int oneDayDeliveryCount = 0;
                double hourCost = (1 + courier.CourierType.Insurance) * courier.CourierType.HourlyRate;

                for (int i = 0; i < shops.Length; i++)
                {
                    try
                    {
                        // 3.0 Извлекаем магазин
                        rc = 30;
                        oneDayDeliveryCount = 0;
                        ShopEx startShop = shops[i];
                        if (startShop.OrderCount <= 0)
                            continue;

                        // 3.1 Находим индекс курьера
                        rc = 31;
                        int courierTypeIndex = startShop.GetCouriersDeliveryInfoIndex(courier);
                        if (courierTypeIndex < 0)
                            continue;

                        // 3.2 Извлекаем покрытие для курьера
                        rc = 32;
                        CourierDeliveryInfo[] courierCover = startShop.GetCourierCover(courierTypeIndex);
                        if (courierCover == null || courierCover.Length <= 0)
                            continue;

                        // 3.3 Отыскиваем первое свободную отгрузку из покрытия
                        rc = 33;
                        CourierDeliveryInfo freeCoverDelivery = null;

                        for (int j = 0; j < courierCover.Length; j++)
                        {
                            CourierDeliveryInfo coverDelivery = courierCover[j];
                            if (coverDelivery.ShippingOrder != null)
                            {
                                if (!coverDelivery.ShippingOrder.Completed)
                                {
                                    freeCoverDelivery = coverDelivery;
                                    break;
                                }
                            }
                            else if (coverDelivery.DeliveredOrders != null && coverDelivery.DeliveredOrders.Length > 0)
                            {
                                if (!coverDelivery.DeliveredOrders[0].Completed)
                                {
                                    freeCoverDelivery = coverDelivery;
                                    break;
                                }
                            }
                        }

                        if (freeCoverDelivery == null)
                            continue;

                        freeCoverDelivery.DeliveryCourier = courier;
                        oneDayDeliveries[oneDayDeliveryCount++] = freeCoverDelivery;
                        freeCoverDelivery.StartDelivery = freeCoverDelivery.StartDeliveryInterval;

                        // 3.4 Определяем координаты курьера в момент завершения отгрузки
                        rc = 34;
                        courier.WorkStart = freeCoverDelivery.StartDelivery.TimeOfDay;

                        if (freeCoverDelivery.DeliveredOrders != null && freeCoverDelivery.DeliveredOrders.Length > 0)
                        {
                            foreach (CourierDeliveryInfo di in freeCoverDelivery.DeliveredOrders)
                                di.Completed = true;
                            Order lastDeliveryOrder = freeCoverDelivery.DeliveredOrders[freeCoverDelivery.OrderCount - 1].ShippingOrder;
                            courier.Latitude = lastDeliveryOrder.Latitude;
                            courier.Longitude = lastDeliveryOrder.Longitude;
                        }
                        else
                        {
                            Order singleOrder = freeCoverDelivery.ShippingOrder;
                            singleOrder.Completed = true;
                            courier.Latitude = singleOrder.Latitude;
                            courier.Longitude = singleOrder.Longitude;
                        }

                        // 3.5 Цикл работы одного курьера до конца рабочего времени
                        rc = 35;
                        double[] distFromShop = new double[shops.Length];
                        ShopEx[] reachableShops = new ShopEx[shops.Length];

                        while (true)
                        {
                            // 3.6 Определяем время, когда курьер освободится
                            rc = 36;
                            DateTime deliveryEnd = freeCoverDelivery.StartDelivery.AddMinutes(freeCoverDelivery.DeliveryTime + courier.CourierType.HandInTime);
                            if ((deliveryEnd.TimeOfDay - courier.WorkStart).TotalHours > FloatSolutionParameters.COURIER_WORK_TIME_LIMIT)
                                break;

                            // 3.7 Определяем достижимые курьром магазины из текущего местоположения
                            rc = 37;
                            double carLatitude = courier.Latitude;
                            double carLongitude = courier.Longitude;
                            int reachableCount = 0;

                            for (int j = 0; j < shops.Length; j++)
                            {
                                ShopEx shop = shops[j];
                                double dist = FloatSolutionParameters.DISTANCE_ALLOWANCE * Helper.Distance(carLatitude, carLongitude, shop.Latitude, shop.Longitude);
                                distFromShop[j] = dist;
                                if (dist <= FloatSolutionParameters.MAX_DISTANCE_TO_AVAILABLE_SHOP)
                                {
                                    reachableShops[reachableCount++] = shop;
                                }
                            }

                            ShopEx[] availableShops;
                            if (reachableCount >= FloatSolutionParameters.MIN_AVAILABLE_SHOP_COUNT)
                            {
                                availableShops = new ShopEx[reachableCount];
                                Array.Copy(reachableShops, 0, availableShops, 0, reachableCount);
                            }
                            else
                            {
                                Array.Sort(distFromShop, shops);
                                availableShops = new ShopEx[FloatSolutionParameters.MIN_AVAILABLE_SHOP_COUNT];
                                Array.Copy(shops, 0, availableShops, 0, FloatSolutionParameters.MIN_AVAILABLE_SHOP_COUNT);
                            }

                            // 3.8 Выбираем первую свободную отгрузку из покрытия
                            rc = 38;
                            double freeCoverDeliverOrderCost = double.MaxValue;
                            freeCoverDelivery = null;

                            for (int j = 0; j < availableShops.Length; j++)
                            {
                                // 3.9 Извлекаем магазин и индекс курьера
                                rc = 39;
                                ShopEx shop = availableShops[j];
                                int courierIndex = shop.GetCouriersDeliveryInfoIndex(courier);

                                // 3.10 Расчитываем время прибытия в магазин
                                rc = 310;
                                double dt = courier.GetArrivalTime(shop.Latitude, shop.Longitude);
                                double arrivalCost = hourCost * dt / 60;
                                DateTime arrivalTime = deliveryEnd.AddMinutes(courier.GetArrivalTime(shop.Latitude, shop.Longitude));

                                // 3.11 Находим свободную отгрузку из покрытия c минимальной средней стоимостью доставки заказа
                                rc = 311;
                                CourierDeliveryInfo bestCoverDelivery = shop.GetFirstFreeCoverDelivery(courierIndex, arrivalTime);
                                if (bestCoverDelivery == null)
                                    continue;

                                // 3.12 Расчитываем среднюю стоимость доставки заказа
                                rc = 312;
                                double orderCost;
                                if (bestCoverDelivery.StartDeliveryInterval <= arrivalTime)
                                {
                                    orderCost = (bestCoverDelivery.Cost + arrivalCost) / bestCoverDelivery.OrderCount;
                                    bestCoverDelivery.StartDelivery = arrivalTime;
                                }
                                else
                                {
                                    orderCost = (bestCoverDelivery.Cost + arrivalCost + hourCost * (bestCoverDelivery.StartDeliveryInterval - arrivalTime).TotalHours) / bestCoverDelivery.OrderCount;
                                    bestCoverDelivery.StartDelivery = bestCoverDelivery.StartDeliveryInterval;
                                }

                                // 3.13 Отбираем наилучшую отгрузку среди свободных отгрузок достижимых магазинов
                                rc = 313;
                                if (orderCost < freeCoverDeliverOrderCost)
                                {
                                    freeCoverDeliverOrderCost = orderCost;
                                    freeCoverDelivery = bestCoverDelivery;
                                }
                            }

                            // 3.14 Если отгрузка не найдена - рабочий день закончен
                            rc = 314;
                            if (freeCoverDelivery == null)
                                break;

                            freeCoverDelivery.DeliveryCourier = courier;
                            oneDayDeliveries[oneDayDeliveryCount++] = freeCoverDelivery;

                            // 3.15 Устанавливаем координаты курьера в конечной точке отгрузки
                            rc = 315;
                            double lastOrderLatitude;
                            double lastOrderLongitude;
                            if (!freeCoverDelivery.GetLastOrderLatLong(out lastOrderLatitude, out lastOrderLongitude))
                                break;
                            courier.Latitude = lastOrderLatitude;
                            courier.Longitude = lastOrderLongitude;

                            // 3.16 Помечаем заказы, как отгруженные
                            rc = 316;

                            if (freeCoverDelivery.DeliveredOrders != null && freeCoverDelivery.DeliveredOrders.Length > 0)
                            {
                                foreach (CourierDeliveryInfo di in freeCoverDelivery.DeliveredOrders)
                                    di.Completed = true;
                            }
                            else if (freeCoverDelivery.ShippingOrder != null)
                            {
                                freeCoverDelivery.ShippingOrder.Completed = true;
                            }
                        }

                        // 4. Расчитываем среднюю стоимость доставки заказа курьером за день обновляем лучший дневной маршрут
                        rc = 4;
                        if (oneDayDeliveryCount > 0)
                        {
                            DateTime startWork = oneDayDeliveries[0].StartDeliveryInterval;
                            oneDayDeliveries[0].StartDelivery = startWork;
                            CourierDeliveryInfo lastDelivery = oneDayDeliveries[oneDayDeliveryCount - 1];
                            DateTime endWork = lastDelivery.StartDelivery.AddMinutes(lastDelivery.DeliveryTime);
                            TimeSpan workInterval = (endWork - startWork);
                            if (workInterval.TotalHours >= FloatSolutionParameters.COURIER_MIN_WORK_TIME)
                            {
                                int orderCount = 0;
                                for (int j = 0; j < oneDayDeliveryCount; j++)
                                    orderCount += oneDayDeliveries[j].OrderCount;
                                int hourCount = workInterval.Hours;
                                if (workInterval.Minutes > 0 || workInterval.Seconds > 0)
                                    hourCount++;
                                double orderCost = hourCount * hourCost / orderCount;
                                if (orderCost < bestOneDayOrderCost)
                                {
                                    bestOneDayOrderCost = orderCost;
                                    courierBestOneDayDeliveries = oneDayDeliveries.Take(oneDayDeliveryCount).ToArray();
                                }
                            }

                        }
                    }
                    finally
                    {
                        for (int j = 0; j < oneDayDeliveryCount; j++)
                        {
                            CourierDeliveryInfo bestDelivery = oneDayDeliveries[j];
                            if (bestDelivery.DeliveredOrders != null && bestDelivery.DeliveredOrders.Length > 0)
                            {
                                foreach (CourierDeliveryInfo di in bestDelivery.DeliveredOrders)
                                    di.Completed = false;
                            }
                            else if (bestDelivery.ShippingOrder != null)
                            {
                                bestDelivery.ShippingOrder.Completed = false;
                            }
                        }
                    }
                }

                // 5. Если дневной маршрут не найден
                rc = 5;
                if (courierBestOneDayDeliveries == null)
                    return rc;

                // 6. Сохраняем найденный маршрут
                rc = 6;
                oneDayDeliveryCount = courierBestOneDayDeliveries.Length;

                if (deliveryCount + oneDayDeliveryCount > allDayDeliveries.Length)
                {
                    Array.Resize(ref allDayDeliveries, allDayDeliveries.Length + oneDayDeliveryCount + 1000);
                }

                Array.Copy(courierBestOneDayDeliveries, 0, allDayDeliveries, deliveryCount, oneDayDeliveryCount);
                deliveryCount += oneDayDeliveryCount;

                //for (int kk = 0; kk < shops.Length; kk++)
                //{
                //    if (shops[kk].N == 3378)
                //    {
                //        kk = kk;
                //    }
                //}

                // 7. Помечаем заказы, как отгруженные
                rc = 7;
                foreach (CourierDeliveryInfo bestDelivery in courierBestOneDayDeliveries)
                {
                    if (bestDelivery.DeliveredOrders != null && bestDelivery.DeliveredOrders.Length > 0)
                    {
                        foreach (CourierDeliveryInfo di in bestDelivery.DeliveredOrders)
                            di.Completed = true;
                    }
                    else if (bestDelivery.ShippingOrder != null)
                    {
                        bestDelivery.ShippingOrder.Completed = true;
                    }
                }

                // 8. Выход - Ok
                rc = 0;
                return rc;
            }
            catch
            {
                return rc;
            }
        }



    }
}
