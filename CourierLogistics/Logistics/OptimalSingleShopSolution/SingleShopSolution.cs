
namespace CourierLogistics.Logistics.OptimalSingleShopSolution
{
    using CourierLogistics.Logistics.OptimalSingleShopSolution.PermutationsRepository;
    using CourierLogistics.Report;
    using CourierLogistics.SourceData.Couriers;
    using CourierLogistics.SourceData.Orders;
    using CourierLogistics.SourceData.Shops;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Оптимальное ежедневное решение для одного магазина
    /// на основе собранных реальных данных
    /// </summary>
    public class SingleShopSolution
    {
        ///// <summary>
        ///// Передельное время доставки, мин
        ///// </summary>
        //public const double DELIVERY_LIMIT = 120;

        /// <summary>
        /// Надбавка для преобразования
        /// сферического расстония между точками на земной поверхности в реальное
        /// </summary>
        public const double DISTANCE_ALLOWANCE = 1.2;

        /// <summary>
        /// Продолжительность работы курьера, час
        /// </summary>
        public const double WORK_DURATION = 12;

        /// <summary>
        /// Минимальная продолжительность рабочего дня курьера, час
        /// </summary>
        public const double MIN_WORK_DURATION = 12;

        /// <summary>
        /// Максимальное число заказов в минуту
        /// </summary>
        public const int ORDERS_PER_MINUTE_LIMIT = 50;

        ///// <summary>
        ///// Минимальное оплачиваемое количество часов
        ///// </summary>
        //public const double MIN_WORK_TIME = 4;

        /// <summary>
        /// Желаемая стоимость доставки одного заказа
        /// </summary>
        public const double ORDER_COST_THRESHOLD = 90;

        /// <summary>
        /// Магазин для анализа
        /// </summary>
        public Shop SingleShop { get; private set; }

        /// <summary>
        /// Заказы поступившие в магазин за несколько дней
        /// </summary>
        public Order[] ShopOrders { get; private set; }

        /// <summary>
        /// Отчет для вывода результатов работы
        /// </summary>
        public ExcelReport Report { get; private set; }

        /// <summary>
        /// Прерывание врзникшее во время последнего вызова Create
        /// </summary>
        public Exception LastException { get; private set; }

        /// <summary>
        /// Флаг: true - решение найдено; false - решение не найдено
        /// </summary>
        public bool IsCreated { get; private set; }

        /// <summary>
        /// История отгрузок
        /// </summary>
        public CourierDeliveryInfo[] deliveryHistory { get; private set; }

        /// <summary>
        /// Количество отгрузок в истории
        /// </summary>
        public int deliveryHistoryCount;

        ///// <summary>
        ///// Параметрический конструктор SingleShopSolution
        ///// </summary>
        ///// <param name="shop">Магазин</param>
        ///// <param name="shopOrders">Заказы поступившие в магазин</param>
        ///// <param name="report">Отчет для размещения результатов</param>
        //public SingleShopSolution(Shop shop, Order[] shopOrders, ExcelReport report)
        //{
        //    SingleShop = shop;
        //    ShopOrders = shopOrders;
        //    Report = report;
        //}

        /// <summary>
        /// Построение квазиоптимальных решений для назначения курьеров
        /// </summary>
        /// <param name="shop">Магазин</param>
        /// <param name="shopOrders">Заказы поступившие в магазин</param>
        /// <param name="report">Отчет для размещения результатов</param>
        public int Create(Shop shop, Order[] shopOrders, ExcelReport report, ref Permutations permutationsRepository)
        {
            // 1. Инициализация
            int rc = 1;
            int rc1 = 1;
            IsCreated = false;
            LastException = null;
            SingleShop = shop;
            ShopOrders = shopOrders;
            Report = report;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (shop == null)
                    return rc;
                if (shopOrders == null || shopOrders.Length <= 0)
                    return rc;
                if (report == null)
                    return rc;
                if (permutationsRepository == null)
                    permutationsRepository = new Permutations();

                // 3. Сортируем заказы по времени поступления
                rc = 3;
                //Array.Sort(shopOrders, CompareByOrderDate);
                Array.Sort(shopOrders, CompareByCollectedDate);

                // 4. Цикл обработки по дням
                rc = 4;
                //DateTime currentDay = shopOrders[0].Date_order.Date;
                DateTime currentDay = shopOrders[0].Date_collected.Date;
                Order order;
                int dayStartIndex = 0;
                Dictionary<string, ShopCourierStatistics> courierSummary = new Dictionary<string, ShopCourierStatistics>(5000);
                Order[] dayOrders;
                CourierDeliveryInfo[] deliveryHistory;
                Order[] notDeliveryOrders;
                int length;
                int shopNo = shop.N;

                for (int i = 0; i < shopOrders.Length; i++)
                {
                    order = shopOrders[i];
                    if (order.Date_collected.Date !=  currentDay)
                    {
                        length = i - dayStartIndex;
                        dayOrders = new Order[length];
                        Array.Copy(shopOrders, dayStartIndex, dayOrders, 0, length);
                                              
                        //rc1 = DailySolution_GettEx(shop, dayOrders, out deliveryHistory, out notDeliveryOrders);
                        //rc1 = FindOptimalDailySolution(shop, dayOrders, ref permutationsRepository, out deliveryHistory, out notDeliveryOrders);
                        //rc1 = FindRealDailySolution(shop, dayOrders, ref permutationsRepository, out deliveryHistory, out notDeliveryOrders);
                        rc1 = FindRealDailySolutionEx(shop, dayOrders, ref permutationsRepository, out deliveryHistory, out notDeliveryOrders);
                        if (rc1 == 0)
                        {
                            PrintDeliveryHistory(report, ExcelReport.HISTORY_PAGE, shopNo, deliveryHistory);
                            AddToCourierSummary(courierSummary, shopNo, deliveryHistory);
                        }
                        else
                        {
                            rc1 = rc1;
                        }

                        currentDay = order.Date_collected.Date;
                        dayStartIndex = i;
                    }
                }

                length = shopOrders.Length - dayStartIndex;
                dayOrders = new Order[length];
                Array.Copy(shopOrders, dayStartIndex, dayOrders, 0, length);
                                              
                //rc1 = FindOptimalDailySolution(shop, dayOrders, ref permutationsRepository, out deliveryHistory, out notDeliveryOrders);
                //rc1 = FindRealDailySolution(shop, dayOrders, ref permutationsRepository, out deliveryHistory, out notDeliveryOrders);
                rc1 = FindRealDailySolutionEx(shop, dayOrders, ref permutationsRepository, out deliveryHistory, out notDeliveryOrders);
                if (rc1 == 0)
                {
                    PrintDeliveryHistory(report, ExcelReport.HISTORY_PAGE, shopNo, deliveryHistory);
                    AddToCourierSummary(courierSummary, shopNo, deliveryHistory);
                }

                // 5. Печатаем сводную статистику
                rc = 5;
                PrintCourierSummary(report,ExcelReport.SUMMARY_PAGE, courierSummary.Values.ToArray());

                // 6. Сохраняем и отображаем отчет
                rc = 6;
                //report.Save();
                //report.Show();
                
                // 7. Выход - Ok
                rc = 0;
                return rc;
            }
            catch (Exception ex)
            {
                LastException = ex;
                return rc;
            }
        }

        ///// <summary>
        ///// Квазиоптимальное решение для одного магазина за один день
        ///// с использованием только Gett-такси
        ///// </summary>
        ///// <param name="shop">Магазин</param>
        ///// <param name="shopOrders">История заказов за один день</param>
        ///// <param name="report">Отчет для размещения результата</param>
        ///// <returns></returns>
        //private int DailySolution_Gett(Shop shop, Order[] shopOrders, ExcelReport report, out CourierDeliveryInfo[] deliveryHistory)
        //{
        //    // 1. Инициализация
        //    int rc = 1;
        //    int rc1 = 1;
        //    deliveryHistory = null;

        //    try
        //    {
        //        // 2. Проверяем исходные данные
        //        rc = 2;
        //        if (shop == null)
        //            return rc;
        //        if (shopOrders == null || shopOrders.Length <= 0)
        //            return rc;
        //        if (report == null)
        //            return rc;

        //        // 3. Создаём ресурс курьеров
        //        rc = 3;
        //        ShopCouriers couriersResource = new ShopCouriers();
        //        Courier gettCourier = new Courier(1, new CourierType_GettTaxi());
        //        gettCourier.Status = CourierStatus.Ready;
        //        gettCourier.WorkStart = new TimeSpan(0);
        //        gettCourier.WorkEnd = new TimeSpan(24,0,0);
        //        deliveryHistory = new CourierDeliveryInfo[shopOrders.Length];
        //        int deliveryHistoryCount = 0;

        //        //Courier yandexCourier = new Courier(2, new CourierType_YandexTaxi());
        //        //Courier onFootCourier = new Courier(0, new CourierType_OnFoot());
        //        //Courier onCarCourier = new Courier(0, new CourierType_OnFoot());
        //        //Courier onBicycleCourier = new Courier(0, new CourierType_OnBicycle());
        //        couriersResource.AddCourier(gettCourier);
        //        //couriersResource.AddCourier(yandexCourier);

        //        // 4. Расчитываем для каждого заказа данные для доставки
        //        rc = 4;
        //        CourierDeliveryInfo[] deliveryInfo = new CourierDeliveryInfo[shopOrders.Length];
        //        double shopLatitude = shop.Latitude;
        //        double shopLongitude = shop.Longitude;
        //        int count = 0;

        //        for (int i = 0; i < shopOrders.Length; i++)
        //        {
        //            Order order = shopOrders[i];
        //            DateTime assemblyEnd = order.Date_collected;
        //            double distance = DISTANCE_ALLOWANCE * Helper.Distance(shopLatitude, shopLongitude, order.Latitude, order.Longitude);
        //            DateTime deliveryTimeLimit = order.Date_order.AddMinutes(DELIVERY_LIMIT);

        //            if (assemblyEnd != DateTime.MinValue)
        //            {
        //                CourierDeliveryInfo courierDeliveryInfo;
        //                rc1 = gettCourier.DeliveryCheck(assemblyEnd, distance, deliveryTimeLimit, order.Weight, out courierDeliveryInfo);
        //                if (rc1 == 0)
        //                {
        //                    courierDeliveryInfo.ShippingOrder = order;
        //                    courierDeliveryInfo.DistanceFromShop = distance;
        //                    deliveryInfo[count++] = courierDeliveryInfo;
        //                }
        //            }
        //        }

        //        if (count <= 0)
        //            return rc;
        //        if (count < deliveryInfo.Length)
        //        {
        //            Array.Resize(ref deliveryInfo, count);
        //        }

        //        // 5. Отмечаем точки интервалов начало доставки на временной оси
        //        rc = 5;
        //        // до 16 заказов в минуту
        //        int ordersPerMinuteLimit = 16;
        //        int[,] intervalHist = new int[1440, ordersPerMinuteLimit + 1];

        //        for(int i = 0; i < deliveryInfo.Length; i++)
        //        {
        //            int startHistIndex = (int) deliveryInfo[i].StartDeliveryInterval.TimeOfDay.TotalMinutes;
        //            int endHistIndex = (int) deliveryInfo[i].EndDeliveryInterval.TimeOfDay.TotalMinutes;

        //            for (int j = startHistIndex; j <= endHistIndex; j++)
        //            {
        //                int intervalCount = intervalHist[j, 0];
        //                if (intervalCount < ordersPerMinuteLimit)
        //                {
        //                    intervalHist[j, 0] = ++intervalCount;
        //                    intervalHist[j, intervalCount] = i;
        //                }
        //            }
        //        }

        //        // 6. Сортируем по возрастанию наиболее позднего времени отгрузки
        //        rc = 6;
        //        Array.Sort(deliveryInfo, CompareByEndDeliveryInterval);

        //        // 6. Цикл построения решения
        //        rc = 6;
        //        Permutations permutationsRepository = new Permutations();
        //        int startIndex = 0;
        //        CourierDeliveryInfo[] isolatedOrders = new CourierDeliveryInfo[deliveryInfo.Length];
        //        int isolatedCount = 0;

        //        while (true)
        //        {
        //            // 6.1 Находим первый заказ в очереди на доставку
        //            rc = 61;
        //            int nextOrderIndex = -1;

        //            for (int i = startIndex; i < count; i++)
        //            {
        //                if (!deliveryInfo[i].Completed)
        //                {
        //                    nextOrderIndex = i;
        //                    break;
        //                }
        //            }

        //            if (nextOrderIndex < 0)
        //                break;

        //            startIndex = nextOrderIndex + 1;
        //            CourierDeliveryInfo nextOrder = deliveryInfo[nextOrderIndex];

        //            // 6.2 Выбираем все заказы - кандидаты на отгрузку по времени
        //            //     (имеющие пересекающиеся интервалы доставки)
        //            rc = 62;
        //            int startHistIndex = (int)nextOrder.StartDeliveryInterval.TimeOfDay.TotalMinutes;
        //            int endHistIndex = (int)nextOrder.EndDeliveryInterval.TimeOfDay.TotalMinutes;
        //            int maxCount = 1;
        //            int maxIndex = -1;
        //            CourierDeliveryInfo[] selectedOrders;

        //            for (int i = startHistIndex; i <= endHistIndex; i++)
        //            {
        //                if (intervalHist[i, 0] > maxCount)
        //                {
        //                    maxCount = intervalHist[i, 0];
        //                    maxIndex = i;
        //                }
        //            }

        //            if (maxIndex == -1)
        //            {
        //                isolatedOrders[isolatedCount++] = nextOrder;
        //                continue;
        //            }

        //            selectedOrders = new CourierDeliveryInfo[maxCount - 1];
        //            count = 0;

        //            for (int i = 1; i <= maxCount; i++)
        //            {
        //                int orderIndex = intervalHist[maxIndex, i];
        //                if (orderIndex != nextOrderIndex)
        //                {
        //                    CourierDeliveryInfo dInfo = deliveryInfo[orderIndex];
        //                    if (!dInfo.Completed)
        //                        selectedOrders[count++] = deliveryInfo[orderIndex];
        //                }
        //            }

        //            if (count <= 0)
        //            {
        //                isolatedOrders[isolatedCount++] = nextOrder;
        //                continue;
        //            }
        //            else if (count < selectedOrders.Length)
        //            {
        //                Array.Resize(ref selectedOrders, count);
        //            }

        //            // 6.3 Расчитываем расстояние от next-заказа до выбранных
        //            rc = 63;
        //            double[] nextDist = new double[selectedOrders.Length];
        //            double nextLatitude = nextOrder.ShippingOrder.Latitude;
        //            double nextLongitude = nextOrder.ShippingOrder.Longitude;

        //            for (int i = 0; i < selectedOrders.Length; i++)
        //            {
        //                Order order = selectedOrders[i].ShippingOrder;
        //                //nextDist[i] = Helper.Distance(nextLatitude, nextLongitude, order.Longitude, order.Longitude);
        //                nextDist[i] = DISTANCE_ALLOWANCE * Helper.Distance(nextLatitude, nextLongitude, order.Longitude, order.Longitude);
        //            }

        //            // 6.4 Фильтруем по расстоянию и возможности доставки
        //            rc = 64;
        //            DateTime[] deliveryTimeLimit = new DateTime[4];
        //            deliveryTimeLimit[0] = DateTime.MaxValue;
        //            deliveryTimeLimit[3] = DateTime.MaxValue;
        //            double[] betweenDistance = new double[4];
        //            count = 0;

        //            for (int i = 0; i < selectedOrders.Length; i++)
        //            {
        //                CourierDeliveryInfo selOrder = selectedOrders[i];
        //                DateTime modelTime = selOrder.ShippingOrder.Date_collected;
        //                if (modelTime < nextOrder.ShippingOrder.Date_collected)
        //                    modelTime = nextOrder.ShippingOrder.Date_collected;
        //                double totalWeight = selOrder.ShippingOrder.Weight + nextOrder.ShippingOrder.Weight;

        //                betweenDistance[1] = nextOrder.DistanceFromShop;
        //                betweenDistance[2] = nextDist[i];
        //                betweenDistance[3] = selOrder.DistanceFromShop;
        //                deliveryTimeLimit[1] = nextOrder.ShippingOrder.Date_order.AddMinutes(DELIVERY_LIMIT);
        //                deliveryTimeLimit[2] = selOrder.ShippingOrder.Date_order.AddMinutes(DELIVERY_LIMIT);

        //                CourierDeliveryInfo twoOrdersDeliveryInfo;
        //                rc1 = gettCourier.DeliveryCheck(modelTime, betweenDistance, deliveryTimeLimit, totalWeight, out twoOrdersDeliveryInfo);
        //                if (rc1 != 0)
        //                {
        //                    double temp = betweenDistance[1];
        //                    betweenDistance[1] = betweenDistance[2];
        //                    betweenDistance[2] = temp;

        //                    DateTime tempTime = deliveryTimeLimit[1];
        //                    deliveryTimeLimit[1] = deliveryTimeLimit[2];
        //                    deliveryTimeLimit[2] = tempTime;
        //                    rc1 = gettCourier.DeliveryCheck(modelTime, betweenDistance, deliveryTimeLimit, totalWeight, out twoOrdersDeliveryInfo);
        //                }

        //                if (rc1 == 0)
        //                {
        //                    selectedOrders[count++] = selOrder;
        //                }
        //            }

        //            if (count <= 0)
        //            {
        //                isolatedOrders[isolatedCount++] = nextOrder;
        //                continue;
        //            }
        //            //else if (count  < selectedOrders.Length)
        //            //{
        //            Array.Resize(ref selectedOrders, count + 1);
        //            selectedOrders[count++] = nextOrder;
        //            //}

        //            // 6.5 Строим попарные расстояния между точками доставки
        //            //     (next-точка будет иметь индекс = count - 1)
        //            rc = 65;
        //            double[,] pointDist = new double[count, count];
        //            double totalOrderWeight = 0;
        //            DateTime startDelivery = selectedOrders[0].ShippingOrder.Date_collected;

        //            for (int i = 0; i < count; i++)
        //            {
        //                Order order = selectedOrders[i].ShippingOrder;
        //                double latitude1 = order.Latitude;
        //                double longitude1 = order.Longitude;

        //                totalOrderWeight += order.Weight;

        //                DateTime packEnd = order.Date_collected;
        //                if (packEnd > startDelivery) startDelivery = packEnd;

        //                for (int j = i + 1; j < count; j++)
        //                {
        //                    order = selectedOrders[j].ShippingOrder;
        //                    double d = DISTANCE_ALLOWANCE * Helper.Distance(latitude1, longitude1, order.Latitude, order.Longitude);
        //                    pointDist[i, j] = d;
        //                    pointDist[j, i] = d;
        //                }
        //            }

        //            // Задача комивояжера - нужно построить путь обхода точек доставки с минимальной стоимостью
        //            // Если n - количество точек доставки, то при простом переборе возникает n! вариантов
        //            //  2! = 2
        //            //  3! = 6
        //            //  4! = 24
        //            //  5! = 120
        //            //  6! = 720
        //            //  7! = 5040
        //            // Будем использовать простой перебор для n ≤ 7
        //            // При n > 7 будем выбирать среди n-путей, каждый из которых построен так:
        //            //       1) выбираем первую точку произвольно (n - вариантов)
        //            //       2) находим ближайшего к ней соседа, который не обслужен
        //            //       3) повторяем пункт 2) пока не обойдем все точки
        //            rc = 65;
        //            if (count <= 6)
        //            {
        //                int[,] permutations = permutationsRepository.GetPermutations(count);
        //                int permutationCount = permutations.GetLength(0);
        //                CourierDeliveryInfo[] deliveryPath = new CourierDeliveryInfo[count];
        //                int[] orderIndex = new int[count];
        //                betweenDistance = new double[count + 2];
        //                deliveryTimeLimit = new DateTime[count + 2];
        //                CourierDeliveryInfo bestPathInfo = null;
        //                CourierDeliveryInfo[] bestDeliveryPath = null;
        //                double bestCost = double.MinValue;

        //                for (int i = 0; i < permutationCount; i++)
        //                {
        //                    // 6.5.1 Строим путь следования
        //                    rc = 651;
        //                    for (int j = 0; j < count; j++)
        //                    {
        //                        int index = permutations[i, j];
        //                        orderIndex[j] = index;
        //                        deliveryPath[j] = selectedOrders[index];
        //                    }

        //                    // 6.5.2 Готовим данные для DeliveryCheck
        //                    rc = 652;
        //                    betweenDistance[0] = 0;
        //                    betweenDistance[1] = deliveryPath[0].DistanceFromShop;
        //                    betweenDistance[count + 1] = deliveryPath[count - 1].DistanceFromShop;
        //                    deliveryTimeLimit[count] = deliveryPath[count - 1].ShippingOrder.Date_order.AddMinutes(DELIVERY_LIMIT);

        //                    for (int j = 2; j <= count; j++)
        //                    {
        //                        betweenDistance[j] = pointDist[orderIndex[j - 2], orderIndex[j - 1]];
        //                        deliveryTimeLimit[j - 1] = deliveryPath[j - 2].ShippingOrder.Date_order.AddMinutes(DELIVERY_LIMIT);
        //                    }

        //                    // 6.5.3 Проверяем путь
        //                    rc = 653;
        //                    CourierDeliveryInfo pathInfo;
        //                    rc1 = gettCourier.DeliveryCheck(startDelivery, betweenDistance, deliveryTimeLimit, totalOrderWeight, out pathInfo);
        //                    if (rc1 == 0)
        //                    {
        //                        if (pathInfo.Cost < bestCost)
        //                        {
        //                            bestCost = pathInfo.Cost;
        //                            bestPathInfo = pathInfo;
        //                            bestDeliveryPath = deliveryPath;
        //                        }
        //                    }
        //                }

        //                if (bestDeliveryPath != null)
        //                {
        //                    // 6.5.4 Найден обход всех точек с доставкой в срок
        //                    rc = 654;

        //                    for (int j = 0; j < bestDeliveryPath.Length; j++)
        //                    {
        //                        bestDeliveryPath[j].Completed = true;
        //                    }

        //                    bestPathInfo.ShippingOrder = nextOrder.ShippingOrder;
        //                    bestPathInfo.DeliveredOrders = bestDeliveryPath;
        //                    deliveryHistory[deliveryHistoryCount++] = bestPathInfo;
        //                    continue;
        //                }
        //            }




        //        }




        //        return rc;

        //    }
        //    catch
        //    {
        //        return rc;
        //    }
        //}

        /// <summary>
        /// Сравнение заказов по времени поступления
        /// </summary>
        /// <param name="order1">Заказ 1</param>
        /// <param name="order2">Заказ 2</param>
        /// <returns>-1 - Заказ1 &lt; Заказ2; 0 - Заказ1 = Заказ2; 1 - Заказ1 &gt; Заказ2</returns>
        private static int CompareByOrderDate(Order order1, Order order2)
        {
            if (order1.Date_order < order2.Date_order)
                return -1;
            if (order1.Date_order > order2.Date_order)
                return 1;
            return 0;
        }

        /// <summary>
        /// Сравнение заказов по времени сборки
        /// </summary>
        /// <param name="order1">Заказ 1</param>
        /// <param name="order2">Заказ 2</param>
        /// <returns>-1 - Заказ1 &lt; Заказ2; 0 - Заказ1 = Заказ2; 1 - Заказ1 &gt; Заказ2</returns>
        private static int CompareByCollectedDate(Order order1, Order order2)
        {
            if (order1.Date_collected < order2.Date_collected)
                return -1;
            if (order1.Date_collected > order2.Date_collected)
                return 1;
            return 0;
        }

        /// <summary>
        /// Сравнение данных отгрузки по предельному времени отгрузки
        /// </summary>
        /// <param name="deliveryInfo1">Данные отгрузки 1</param>
        /// <param name="deliveryInfo2">Данные отгрузки 2</param>
        /// <returns>-1 - Данные1 &lt; Данные2; 0 - Данные1 = Данные2; 1 - Данные1 &gt; данные2</returns>
        private static int CompareByEndDeliveryInterval(CourierDeliveryInfo deliveryInfo1, CourierDeliveryInfo deliveryInfo2)
        {
            if (deliveryInfo1.EndDeliveryInterval < deliveryInfo2.EndDeliveryInterval)
                return -1;
            if (deliveryInfo1.EndDeliveryInterval > deliveryInfo2.EndDeliveryInterval)
                return 1;
            return 0;
        }

        /// <summary>
        /// Сравнение данных отгрузки по времени начала отгрузки
        /// </summary>
        /// <param name="deliveryInfo1">Данные отгрузки 1</param>
        /// <param name="deliveryInfo2">Данные отгрузки 2</param>
        /// <returns>-1 - Данные1 &lt; Данные2; 0 - Данные1 = Данные2; 1 - Данные1 &gt; данные2</returns>
        private static int CompareByStartDelivery(CourierDeliveryInfo deliveryInfo1, CourierDeliveryInfo deliveryInfo2)
        {
            if (deliveryInfo1.StartDelivery < deliveryInfo2.StartDelivery)
                return -1;
            if (deliveryInfo1.StartDelivery > deliveryInfo2.StartDelivery)
                return 1;
            return 0;
        }

        /// <summary>
        /// Сравнение статистик по ключу
        /// </summary>
        /// <param name="courierStatistics1">Данные 1</param>
        /// <param name="courierStatistics2">Данные 2</param>
        /// <returns>-1 - Данные1 &lt; Данные2; 0 - Данные1 = Данные2; 1 - Данные1 &gt; данные2</returns>
        private static int CompareByKey(ShopCourierStatistics courierStatistics1, ShopCourierStatistics courierStatistics2)
        {
            return string.Compare(courierStatistics1.Key, courierStatistics2.Key);
        }

        /// <summary>
        /// Квазиоптимальное решение для одного магазина за один день
        /// с использованием только Gett-такси
        /// </summary>
        /// <param name="shop">Магазин</param>
        /// <param name="shopOrders">История заказов за один день</param>
        /// <param name="notDeliveryOrders">Заказы, которые не удалось доставить</param>
        /// <returns></returns>
        private static int DailySolution_GettEx(Shop shop, Order[] shopOrders, out CourierDeliveryInfo[] deliveryHistory, out Order[] notDeliveryOrders)
        {
            // 1. Инициализация
            int rc = 1;
            int rc1 = 1;
            deliveryHistory = null;
            notDeliveryOrders = null;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (shop == null)
                    return rc;
                if (shopOrders == null || shopOrders.Length <= 0)
                    return rc;

                // 3. Создаём ресурс курьеров
                rc = 3;
                ShopCouriers couriersResource = new ShopCouriers();
                Courier gettCourier = new Courier(1, new CourierType_GettTaxi());
                gettCourier.Status = CourierStatus.Ready;
                gettCourier.WorkStart = new TimeSpan(0);
                gettCourier.WorkEnd = new TimeSpan(24, 0, 0);
                deliveryHistory = new CourierDeliveryInfo[shopOrders.Length];
                int deliveryHistoryCount = 0;

                couriersResource.AddCourier(gettCourier);

                // 4. Расчитываем для каждого заказа данные для доставки
                rc = 4;
                Order[] isolatedOrders = new Order[shopOrders.Length];
                int isolatedCount = 0;
                CourierDeliveryInfo[] deliveryInfo = new CourierDeliveryInfo[shopOrders.Length];
                double shopLatitude = shop.Latitude;
                double shopLongitude = shop.Longitude;
                int count = 0;
                //int mm = 0;

                for (int i = 0; i < shopOrders.Length; i++)
                {
                    Order order = shopOrders[i];
                    DateTime assemblyEnd = order.Date_collected;
                    double distance = DISTANCE_ALLOWANCE * Helper.Distance(shopLatitude, shopLongitude, order.Latitude, order.Longitude);
                    //DateTime deliveryTimeLimit = order.Date_order.AddMinutes(DELIVERY_LIMIT);
                    DateTime deliveryTimeLimit = order.GetDeliveryLimit(Courier.DELIVERY_LIMIT);

                    if (assemblyEnd != DateTime.MinValue)
                    {
                        CourierDeliveryInfo courierDeliveryInfo;
                        rc1 = gettCourier.DeliveryCheck(assemblyEnd, distance, deliveryTimeLimit, order.Weight, out courierDeliveryInfo);
                        if (rc1 == 0)
                        {
                            courierDeliveryInfo.ShippingOrder = order;
                            courierDeliveryInfo.DistanceFromShop = distance;
                            deliveryInfo[count++] = courierDeliveryInfo;
                        }
                        else
                        {
                            isolatedOrders[isolatedCount++] = order;
                            //mm++;
                        }
                    }
                }

                if (count <= 0)
                    goto Isolated;
                if (count < deliveryInfo.Length)
                {
                    Array.Resize(ref deliveryInfo, count);
                }

                // 5. Отмечаем точки интервалов начала доставки на временной оси
                rc = 5;
                // до 16 заказов в минуту
                int ordersPerMinuteLimit = 16;
                int[,] intervalHist = new int[1440, ordersPerMinuteLimit + 1];

                for (int i = 0; i < deliveryInfo.Length; i++)
                {
                    int startHistIndex = (int)deliveryInfo[i].StartDeliveryInterval.TimeOfDay.TotalMinutes;
                    int endHistIndex = (int)deliveryInfo[i].EndDeliveryInterval.TimeOfDay.TotalMinutes;

                    for (int j = startHistIndex; j <= endHistIndex; j++)
                    {
                        int intervalCount = intervalHist[j, 0];
                        if (intervalCount < ordersPerMinuteLimit)
                        {
                            intervalHist[j, 0] = ++intervalCount;
                            intervalHist[j, intervalCount] = i;
                        }
                    }
                }

                // 6. Сортируем по возрастанию наиболее позднего времени отгрузки
                rc = 6;
                Array.Sort(deliveryInfo, CompareByEndDeliveryInterval);

                // 7. Цикл построения решения для групповых отгрузок
                rc = 7;
                Permutations permutationsRepository = new Permutations();
                int startIndex = 0;

                while (true)
                {
                    // 7.1 Находим первый заказ в очереди на доставку
                    rc = 71;
                    int nextOrderIndex = -1;

                    for (int i = startIndex; i < deliveryInfo.Length; i++)
                    {
                        if (!deliveryInfo[i].Completed)
                        {
                            nextOrderIndex = i;
                            break;
                        }
                    }

                    if (nextOrderIndex < 0)
                        break;

                    startIndex = nextOrderIndex + 1;
                    CourierDeliveryInfo nextOrder = deliveryInfo[nextOrderIndex];

                    // 7.2 Выбираем все заказы - кандидаты на отгрузку по времени
                    //     (имеющие пересекающиеся интервалы доставки)
                    rc = 72;
                    int startHistIndex = (int)nextOrder.StartDeliveryInterval.TimeOfDay.TotalMinutes;
                    int endHistIndex = (int)nextOrder.EndDeliveryInterval.TimeOfDay.TotalMinutes;
                    int maxCount = 1;
                    int maxIndex = -1;
                    CourierDeliveryInfo[] selectedOrders;

                    for (int i = startHistIndex; i <= endHistIndex; i++)
                    {
                        if (intervalHist[i, 0] > maxCount)
                        {
                            maxCount = intervalHist[i, 0];
                            maxIndex = i;
                        }
                    }

                    if (maxIndex == -1)
                    {
                        nextOrder.Completed = true;
                        isolatedOrders[isolatedCount++] = nextOrder.ShippingOrder;
                        continue;
                    }

                    selectedOrders = new CourierDeliveryInfo[maxCount - 1];
                    count = 0;

                    for (int i = 1; i <= maxCount; i++)
                    {
                        int orderIndex = intervalHist[maxIndex, i];
                        if (orderIndex != nextOrderIndex)
                        {
                            CourierDeliveryInfo dInfo = deliveryInfo[orderIndex];
                            if (!dInfo.Completed)
                                selectedOrders[count++] = deliveryInfo[orderIndex];
                        }
                    }

                    if (count <= 0)
                    {
                        nextOrder.Completed = true;
                        isolatedOrders[isolatedCount++] = nextOrder.ShippingOrder;
                        continue;
                    }
                    else if (count < selectedOrders.Length)
                    {
                        Array.Resize(ref selectedOrders, count);
                    }

                    // 7.3 Расчитываем расстояние от next-заказа до выбранных
                    rc = 73;
                    double[] nextDist = new double[selectedOrders.Length];
                    double nextLatitude = nextOrder.ShippingOrder.Latitude;
                    double nextLongitude = nextOrder.ShippingOrder.Longitude;

                    for (int i = 0; i < selectedOrders.Length; i++)
                    {
                        Order order = selectedOrders[i].ShippingOrder;
                        //nextDist[i] = Helper.Distance(nextLatitude, nextLongitude, order.Longitude, order.Longitude);
                        nextDist[i] = DISTANCE_ALLOWANCE * Helper.Distance(nextLatitude, nextLongitude, order.Latitude, order.Longitude);
                    }

                    // 7.4 Фильтруем по расстоянию и возможности доставки
                    rc = 74;
                    DateTime[] deliveryTimeLimit = new DateTime[4];
                    deliveryTimeLimit[0] = DateTime.MaxValue;
                    deliveryTimeLimit[3] = DateTime.MaxValue;
                    double[] betweenDistance = new double[4];
                    count = 0;

                    for (int i = 0; i < selectedOrders.Length; i++)
                    {
                        CourierDeliveryInfo selOrder = selectedOrders[i];
                        DateTime modelTime = selOrder.ShippingOrder.Date_collected;
                        if (modelTime < nextOrder.ShippingOrder.Date_collected)
                            modelTime = nextOrder.ShippingOrder.Date_collected;
                        double totalWeight = selOrder.ShippingOrder.Weight + nextOrder.ShippingOrder.Weight;

                        betweenDistance[1] = nextOrder.DistanceFromShop;
                        betweenDistance[2] = nextDist[i];
                        betweenDistance[3] = selOrder.DistanceFromShop;
                        //deliveryTimeLimit[1] = nextOrder.ShippingOrder.Date_order.AddMinutes(DELIVERY_LIMIT);
                        deliveryTimeLimit[1] = nextOrder.ShippingOrder.GetDeliveryLimit(Courier.DELIVERY_LIMIT);
                        //deliveryTimeLimit[2] = selOrder.ShippingOrder.Date_order.AddMinutes(DELIVERY_LIMIT);
                        deliveryTimeLimit[2] = selOrder.ShippingOrder.GetDeliveryLimit(Courier.DELIVERY_LIMIT);
                        double[] nodeDeliveryTime;
                        CourierDeliveryInfo twoOrdersDeliveryInfo;
                        rc1 = gettCourier.DeliveryCheck(modelTime, betweenDistance, deliveryTimeLimit, totalWeight, out twoOrdersDeliveryInfo, out nodeDeliveryTime);
                        if (rc1 != 0)
                        {
                            double temp = betweenDistance[1];
                            betweenDistance[1] = betweenDistance[2];
                            betweenDistance[2] = temp;

                            DateTime tempTime = deliveryTimeLimit[1];
                            deliveryTimeLimit[1] = deliveryTimeLimit[2];
                            deliveryTimeLimit[2] = tempTime;
                            double[] nodeDeliveryTimeX;
                            rc1 = gettCourier.DeliveryCheck(modelTime, betweenDistance, deliveryTimeLimit, totalWeight, out twoOrdersDeliveryInfo, out nodeDeliveryTimeX);
                        }

                        if (rc1 == 0)
                        {
                            selectedOrders[count++] = selOrder;
                        }
                    }

                    if (count <= 0)
                    {
                        nextOrder.Completed = true;
                        isolatedOrders[isolatedCount++] = nextOrder.ShippingOrder;
                        continue;
                    }

                    Array.Resize(ref selectedOrders, count + 1);
                    selectedOrders[count++] = nextOrder;  // Исходный заказ последний

                    // Задача комивояжера - нужно построить путь обхода точек доставки с минимальной стоимостью
                    // Если n - количество точек доставки, то при простом переборе возникает n! вариантов
                    //  2! = 2
                    //  3! = 6
                    //  4! = 24
                    //  5! = 120
                    //  6! = 720
                    //  7! = 5040
                    //  8! = 40320
                    // Будем использовать простой перебор для n ≤ 8
                    // При n > 8 будем выбирать среди n-путей, каждый из которых построен так:
                    //       1) выбираем первую точку произвольно (n - вариантов)
                    //       2) находим ближайшего к ней соседа, который не обслужен
                    //       3) повторяем пункт 2) пока не обойдем все точки
                    //       4) среди n построенных путей находим путь с минимальной стоимостью
                    // 7.5 Перебор всех перестановок
                    rc = 75;
                    CourierDeliveryInfo bestPathInfo = null;

                    if (count <= 8)
                    {
                        int[,] permutations = permutationsRepository.GetPermutations(count);

                        rc1 = FindPathWithMinCost(gettCourier, selectedOrders, permutations, out bestPathInfo);
                        if (rc1 != 0 && count > 2)
                        {
                            // 7.5.1 Отбрасываем одну точку доставки и пытаемся найти путь с минимальной стоимостью
                            rc = 751;
                            CourierDeliveryInfo[] deliveryOrders = new CourierDeliveryInfo[selectedOrders.Length - 1];

                            for (int i = 0; i < selectedOrders.Length - 1; i++)
                            {
                                int deliveryOrderCount = 0;
                                for (int j = 0; j < selectedOrders.Length; j++)
                                {
                                    if (j != i)
                                    {
                                        deliveryOrders[deliveryOrderCount++] = selectedOrders[j];
                                    }
                                }

                                permutations = permutationsRepository.GetPermutations(deliveryOrderCount);
                                CourierDeliveryInfo bestPathInfoX;
                                rc1 = FindPathWithMinCost(gettCourier, deliveryOrders, permutations, out bestPathInfoX);
                                if (rc1 == 0)
                                {
                                    if (bestPathInfo == null)
                                    {
                                        bestPathInfo = bestPathInfoX;
                                    }
                                    else if (bestPathInfoX.Cost < bestPathInfo.Cost)
                                    {
                                        bestPathInfo = bestPathInfoX;
                                    }
                                }
                            }

                            if (bestPathInfo == null && count > 3)
                            {
                                // 7.5.2 Обрасываем две точки доставки и пытаемся найти путь с минимальной стоимостью
                                rc = 752;
                                deliveryOrders = new CourierDeliveryInfo[selectedOrders.Length - 2];

                                for (int i = 0; i < selectedOrders.Length - 1; i++)
                                {
                                    for (int j = i + 1; j < selectedOrders.Length - 1; j++)
                                    {
                                        int deliveryOrderCount = 0;
                                        for (int k = 0; k < selectedOrders.Length; k++)
                                        {
                                            if (k != i && k != j)
                                            {
                                                deliveryOrders[deliveryOrderCount++] = selectedOrders[k];
                                            }
                                        }

                                        permutations = permutationsRepository.GetPermutations(deliveryOrderCount);
                                        CourierDeliveryInfo bestPathInfoX;
                                        rc1 = FindPathWithMinCost(gettCourier, deliveryOrders, permutations, out bestPathInfoX);
                                        if (rc1 == 0)
                                        {
                                            if (bestPathInfo == null)
                                            {
                                                bestPathInfo = bestPathInfoX;
                                            }
                                            else if (bestPathInfoX.Cost < bestPathInfo.Cost)
                                            {
                                                bestPathInfo = bestPathInfoX;
                                            }
                                        }
                                    }
                                }

                                if (bestPathInfo == null && count > 4)
                                {
                                    // 7.5.3 Обрасываем три точки доставки и пытаемся найти путь с минимальной стоимостью
                                    rc = 753;
                                    deliveryOrders = new CourierDeliveryInfo[selectedOrders.Length - 3];

                                    for (int i = 0; i < selectedOrders.Length - 1; i++)
                                    {
                                        for (int j = i + 1; j < selectedOrders.Length - 1; j++)
                                        {
                                            for (int k = j + 1; k < selectedOrders.Length; k++)
                                            {
                                                int deliveryOrderCount = 0;
                                                for (int m = 0; m < selectedOrders.Length; m++)
                                                {
                                                    if (m != i && m != j && m != k)
                                                    {
                                                        deliveryOrders[deliveryOrderCount++] = selectedOrders[m];
                                                    }
                                                }

                                                permutations = permutationsRepository.GetPermutations(deliveryOrderCount);
                                                CourierDeliveryInfo bestPathInfoX;
                                                rc1 = FindPathWithMinCost(gettCourier, selectedOrders, permutations, out bestPathInfoX);
                                                if (rc1 == 0)
                                                {
                                                    if (bestPathInfo == null)
                                                    {
                                                        bestPathInfo = bestPathInfoX;
                                                    }
                                                    else if (bestPathInfoX.Cost < bestPathInfo.Cost)
                                                    {
                                                        bestPathInfo = bestPathInfoX;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        if (bestPathInfo != null)
                        {
                            bestPathInfo.ShippingOrder = nextOrder.ShippingOrder;
                            deliveryHistory[deliveryHistoryCount++] = bestPathInfo;

                            foreach (CourierDeliveryInfo deliveryInfoX in bestPathInfo.DeliveredOrders)
                            {
                                deliveryInfoX.Completed = true;
                            }

                        }
                        else
                        {
                            nextOrder.Completed = true;
                            isolatedOrders[isolatedCount++] = nextOrder.ShippingOrder;
                        }
                    }
                    else
                    {
                        // 7.6 
                        //       1) выбираем первую точку произвольно (n - вариантов)
                        //       2) находим ближайшего к ней соседа, который не обслужен
                        //       3) повторяем пункт 2) пока не обойдем все точки
                        //       4) среди n построенных путей находим путь с минимальной стоимостью
                        rc = 76;
                        rc1 = FindSpecialPathWithMinCost(gettCourier, selectedOrders, out bestPathInfo);
                        if (rc1 != 0)
                        {
                            // 7.6.1 Обрасываем одну точку доставки и пытаемся найти путь с минимальной стоимостью
                            rc = 761;
                            CourierDeliveryInfo[] deliveryOrders = new CourierDeliveryInfo[selectedOrders.Length - 1];

                            for (int i = 0; i < selectedOrders.Length - 1; i++)
                            {
                                int deliveryOrderCount = 0;
                                for (int j = 0; j < selectedOrders.Length; j++)
                                {
                                    if (j != i)
                                    {
                                        deliveryOrders[deliveryOrderCount++] = selectedOrders[j];
                                    }
                                }

                                CourierDeliveryInfo bestPathInfoX;
                                rc1 = FindSpecialPathWithMinCost(gettCourier, selectedOrders, out bestPathInfoX);
                                if (rc1 == 0)
                                {
                                    if (bestPathInfo == null)
                                    {
                                        bestPathInfo = bestPathInfoX;
                                    }
                                    else if (bestPathInfoX.Cost < bestPathInfo.Cost)
                                    {
                                        bestPathInfo = bestPathInfoX;
                                    }
                                }
                            }

                            if (bestPathInfo == null)
                            {
                                // 7.6.2 Обрасываем две точки доставки и пытаемся найти путь с минимальной стоимостью
                                rc = 762;
                                deliveryOrders = new CourierDeliveryInfo[selectedOrders.Length - 2];

                                for (int i = 0; i < selectedOrders.Length - 1; i++)
                                {
                                    for (int j = i + 1; j < selectedOrders.Length - 1; j++)
                                    {
                                        int deliveryOrderCount = 0;
                                        for (int k = 0; k < selectedOrders.Length; k++)
                                        {
                                            if (k != i && k != j)
                                            {
                                                deliveryOrders[deliveryOrderCount++] = selectedOrders[k];
                                            }
                                        }

                                        CourierDeliveryInfo bestPathInfoX;
                                        rc1 = FindSpecialPathWithMinCost(gettCourier, selectedOrders, out bestPathInfoX);
                                        if (rc1 == 0)
                                        {
                                            if (bestPathInfo == null)
                                            {
                                                bestPathInfo = bestPathInfoX;
                                            }
                                            else if (bestPathInfoX.Cost < bestPathInfo.Cost)
                                            {
                                                bestPathInfo = bestPathInfoX;
                                            }
                                        }
                                    }
                                }

                                if (bestPathInfo == null)
                                {
                                    // 7.6.3 Обрасываем три точки доставки и пытаемся найти путь с минимальной стоимостью
                                    rc = 763;
                                    deliveryOrders = new CourierDeliveryInfo[selectedOrders.Length - 3];

                                    for (int i = 0; i < selectedOrders.Length - 1; i++)
                                    {
                                        for (int j = i + 1; j < selectedOrders.Length - 1; j++)
                                        {
                                            for (int k = j + 1; k < selectedOrders.Length; k++)
                                            {
                                                int deliveryOrderCount = 0;
                                                for (int m = 0; m < selectedOrders.Length; m++)
                                                {
                                                    if (m != i && m != j && m != k)
                                                    {
                                                        deliveryOrders[deliveryOrderCount++] = selectedOrders[m];
                                                    }
                                                }

                                                CourierDeliveryInfo bestPathInfoX;
                                                rc1 = FindSpecialPathWithMinCost(gettCourier, selectedOrders, out bestPathInfoX);
                                                if (rc1 == 0)
                                                {
                                                    if (bestPathInfo == null)
                                                    {
                                                        bestPathInfo = bestPathInfoX;
                                                    }
                                                    else if (bestPathInfoX.Cost < bestPathInfo.Cost)
                                                    {
                                                        bestPathInfo = bestPathInfoX;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        if (bestPathInfo != null)
                        {
                            bestPathInfo.ShippingOrder = nextOrder.ShippingOrder;
                            deliveryHistory[deliveryHistoryCount++] = bestPathInfo;

                            foreach (CourierDeliveryInfo deliveryInfoX in bestPathInfo.DeliveredOrders)
                            {
                                deliveryInfoX.Completed = true;
                            }

                        }
                        else
                        {
                            nextOrder.Completed = true;
                            isolatedOrders[isolatedCount++] = nextOrder.ShippingOrder;
                        }
                    }
                }

                // 8. Обработка изолированных точек доставки
                Isolated:
                rc = 8;
                if (isolatedCount > 0)
                {
                    // 8.1 Выбираем изолированные заказы
                    rc = 81;
                    Order[] allIsolatedOrders = new Order[isolatedCount];
                    for (int i = 0; i < isolatedCount; i++)
                    {
                        allIsolatedOrders[i] = isolatedOrders[i];
                    }

                    // 8.2 Распределяем изолированные заказы 
                    rc = 82;
                    int courierId = 1;

                    for (int i = 0; i < isolatedCount; i++)
                    {
                        courierId++;

                        // 8.2.1 Делаем расчет для пешего курьера
                        rc = 821;
                        Courier courier_OnFoot = new Courier(courierId, new CourierType_OnFoot());
                        courier_OnFoot.Status = CourierStatus.Ready;
                        courier_OnFoot.LunchTimeStart = new TimeSpan(0);
                        courier_OnFoot.LunchTimeEnd = new TimeSpan(0);
                        courier_OnFoot.WorkStart = new TimeSpan(0);
                        courier_OnFoot.WorkEnd = new TimeSpan(24,0,0);

                        CourierDeliveryInfo[] selectedOrders_OnFoot;
                        Order[] notSelectedOrders_OnFoot;
                        double orderCost_OnFoot = double.MaxValue;

                        rc1 = SelectIsolatedOrdersForCourier(courier_OnFoot, DateTime.MinValue, shop, allIsolatedOrders, out selectedOrders_OnFoot, out notSelectedOrders_OnFoot);
                        if (rc1 == 0 && selectedOrders_OnFoot != null && selectedOrders_OnFoot.Length > 0)
                        {
                            orderCost_OnFoot = WORK_DURATION * courier_OnFoot.CourierType.HourlyRate / selectedOrders_OnFoot.Length;
                        }

                        // 8.2.2 Делаем расчет для курьера на велосипеде
                        rc = 822;
                        Courier courier_OnBicycle = new Courier(courierId, new CourierType_Bicycle());
                        courier_OnBicycle.Status = CourierStatus.Ready;
                        courier_OnBicycle.LunchTimeStart = new TimeSpan(0);
                        courier_OnBicycle.LunchTimeEnd = new TimeSpan(0);
                        courier_OnBicycle.WorkStart = new TimeSpan(0);
                        courier_OnBicycle.WorkEnd = new TimeSpan(24,0,0);

                        CourierDeliveryInfo[] selectedOrders_OnBicycle;
                        Order[] notSelectedOrders_OnBicycle;
                        double orderCost_OnBicycle = double.MaxValue;

                        rc1 = SelectIsolatedOrdersForCourier(courier_OnBicycle, DateTime.MinValue, shop, allIsolatedOrders, out selectedOrders_OnBicycle, out notSelectedOrders_OnBicycle);
                        if (rc1 == 0 && selectedOrders_OnBicycle != null && selectedOrders_OnBicycle.Length > 0)
                        {
                            orderCost_OnBicycle = WORK_DURATION * courier_OnBicycle.CourierType.HourlyRate / selectedOrders_OnBicycle.Length;
                        }

                        // 8.2.3 Делаем расчет для курьера на авто
                        rc = 823;
                        Courier courier_OnCar = new Courier(courierId, new CourierType_Car());
                        courier_OnCar.Status = CourierStatus.Ready;
                        courier_OnCar.LunchTimeStart = new TimeSpan(0);
                        courier_OnCar.LunchTimeEnd = new TimeSpan(0);
                        courier_OnCar.WorkStart = new TimeSpan(0);
                        courier_OnCar.WorkEnd = new TimeSpan(24,0,0);

                        CourierDeliveryInfo[] selectedOrders_OnCar;
                        Order[] notSelectedOrders_OnCar;
                        double orderCost_OnCar = double.MaxValue;

                        rc1 = SelectIsolatedOrdersForCourier(courier_OnCar, DateTime.MinValue, shop, allIsolatedOrders, out selectedOrders_OnCar, out notSelectedOrders_OnCar);
                        if (rc1 == 0 && selectedOrders_OnCar != null && selectedOrders_OnCar.Length > 0)
                        {
                            orderCost_OnCar = (1 + courier_OnCar.CourierType.Insurance) * WORK_DURATION * courier_OnCar.CourierType.HourlyRate / selectedOrders_OnCar.Length;
                        }

                        // 8.2.4 Выбираем вариант с наименьшей стоимостью заказа
                        rc = 824;
                        Courier courier = courier_OnFoot;
                        double orderCost = orderCost_OnFoot;
                        CourierDeliveryInfo[] selectedOrders = selectedOrders_OnFoot;
                        Order[] notSelectedOrders = notSelectedOrders_OnFoot;

                        if (orderCost_OnBicycle < orderCost)
                        {
                            courier = courier_OnBicycle;
                            orderCost = orderCost_OnBicycle;
                            selectedOrders = selectedOrders_OnBicycle;
                            notSelectedOrders = notSelectedOrders_OnBicycle;
                        }

                        if (orderCost_OnCar < orderCost)
                        {
                            courier = courier_OnCar;
                            orderCost = orderCost_OnCar;
                            selectedOrders = selectedOrders_OnCar;
                            notSelectedOrders = notSelectedOrders_OnCar;
                        }

                        if (orderCost == double.MaxValue)
                            break;

                        couriersResource.AddCourier(courier);

                        for (int j = 0; j < selectedOrders.Length; j++)
                        {
                            deliveryHistory[deliveryHistoryCount++] = selectedOrders[j];
                        }

                        DateTime startWorkInterval;
                        DateTime endWorkInterval;
                        rc1 = GetWorkInterval(selectedOrders, out startWorkInterval, out endWorkInterval);
                        if (rc1 == 0)
                        {
                            courier.WorkStart = startWorkInterval.TimeOfDay;
                            courier.WorkEnd = endWorkInterval.TimeOfDay;
                        }

                        allIsolatedOrders = notSelectedOrders;
                        if (notSelectedOrders == null || notSelectedOrders.Length <= 0)
                            break;
                    }

                    notDeliveryOrders = allIsolatedOrders;
                }

                Array.Resize(ref deliveryHistory, deliveryHistoryCount);


                // 9. Выход - Ok
                rc = 0;
                return rc;

            }
            catch
            {
                return rc;
            }
        }

        /// <summary>
        /// Рачет интервала выполения заказов
        /// </summary>
        /// <param name="orders">Заказы</param>
        /// <param name="startWorkInterval">Начало интервала</param>
        /// <param name="endWorkInterval">Конец интервала</param>
        /// <returns>0 - интервал расчитан; иначе - интервал не расчитан</returns>
        private static int GetWorkInterval(CourierDeliveryInfo[] orders, out DateTime startWorkInterval, out DateTime endWorkInterval)
        {
            // 1. Инициализация
            int rc = 1;
            startWorkInterval = DateTime.MaxValue;
            endWorkInterval = DateTime.MinValue;
            
            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (orders == null || orders.Length <= 0)
                    return 0;

                // 3. Расчитываем рабочий интервал
                rc = 3;
                foreach (CourierDeliveryInfo order in orders)
                {
                    DateTime startDelivery = order.StartDelivery;
                    //DateTime returnTime = startDelivery.AddMinutes(order.DeliveryTime);
                    DateTime returnTime = startDelivery.AddMinutes(order.ExecutionTime);
                    if (startDelivery < startWorkInterval) startWorkInterval = startDelivery;
                    if (returnTime > endWorkInterval) endWorkInterval = returnTime;
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
        /// Длительность работы с заказами, час
        /// </summary>
        /// <param name="orders">Заказы</param>
        /// <returns>Длительность (значение 0 - длительность не может быть рассчитана)</returns>
        private static double GetTotalWorkTime(CourierDeliveryInfo[] orders)
        {
            DateTime startWorkInterval;
            DateTime endWorkInterval;

            if (GetWorkInterval(orders, out startWorkInterval, out endWorkInterval) == 0)
            {
                return (endWorkInterval - startWorkInterval).TotalHours;
            }
            else
            {
                return 0;
            }
        }

        /// <summary>
        /// Поиск пути с минимальной стоимостью среди заданных перестановок
        /// </summary>
        /// <param name="courier">Курьер</param>
        /// <param name="deliveredOrders">Доставляемые заказы</param>
        /// <param name="permutations">Перестановки, среди которых осуществляется поиск</param>
        /// <param name="bestPathInfo">Путь с минимальной стоимостью</param>
        /// <returns>0 - путь найден; иначе - путь не найден</returns>
        private static int FindPathWithMinCost(Courier courier, CourierDeliveryInfo[] deliveredOrders, int[,] permutations, out CourierDeliveryInfo bestPathInfo)
        {
            // 1. Инициализация
            int rc = 1;
            bestPathInfo = null;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (courier == null)
                    return rc;
                if (deliveredOrders == null || deliveredOrders.Length <= 0)
                    return rc;

                int count = deliveredOrders.Length;

                if (permutations == null || permutations.Length <= 0)
                    return rc;
                if (permutations.GetLength(1) != count)
                    return rc;

                // 3. Находим попарные расстояния между точками доставки, 
                //    подсчитытываем общий вес и находим наиболее раннее время на начала отгрузки
                rc = 3;
                double[,] pointDist = new double[count, count];
                double totalOrderWeight = 0;
                DateTime startDelivery = deliveredOrders[0].ShippingOrder.Date_collected;

                for (int i = 0; i < count; i++)
                {
                    Order order = deliveredOrders[i].ShippingOrder;
                    double latitude1 = order.Latitude;
                    double longitude1 = order.Longitude;

                    totalOrderWeight += order.Weight;

                    DateTime packEnd = order.Date_collected;
                    if (packEnd > startDelivery)
                        startDelivery = packEnd;

                    for (int j = i + 1; j < count; j++)
                    {
                        order = deliveredOrders[j].ShippingOrder;
                        double d = DISTANCE_ALLOWANCE * Helper.Distance(latitude1, longitude1, order.Latitude, order.Longitude);
                        pointDist[i, j] = d;
                        pointDist[j, i] = d;
                    }
                }

                // 4. Решаем задачу комивояжера - построение пути обхода c минимальной стоимостью
                rc = 4;
                int permutationCount = permutations.GetLength(0);
                CourierDeliveryInfo[] deliveryPath = new CourierDeliveryInfo[count];
                int[] orderIndex = new int[count];
                double[] betweenDistance = new double[count + 2];
                DateTime[] deliveryTimeLimit = new DateTime[count + 2];
                CourierDeliveryInfo[] bestDeliveryPath = null;
                double bestCost = double.MaxValue;

                for (int i = 0; i < permutationCount; i++)
                {
                    // 4.1 Строим путь следования
                    rc = 41;
                    for (int j = 0; j < count; j++)
                    {
                        int index = permutations[i, j];
                        orderIndex[j] = index;
                        deliveryPath[j] = deliveredOrders[index];
                    }

                    // 4.2 Готовим данные для DeliveryCheck
                    rc = 42;
                    betweenDistance[0] = 0;
                    betweenDistance[1] = deliveryPath[0].DistanceFromShop;
                    betweenDistance[count + 1] = deliveryPath[count - 1].DistanceFromShop;
                    //deliveryTimeLimit[count] = deliveryPath[count - 1].ShippingOrder.Date_order.AddMinutes(DELIVERY_LIMIT);
                    deliveryTimeLimit[count] = deliveryPath[count - 1].ShippingOrder.GetDeliveryLimit(Courier.DELIVERY_LIMIT);

                    for (int j = 2; j <= count; j++)
                    {
                        betweenDistance[j] = pointDist[orderIndex[j - 2], orderIndex[j - 1]];
                        //deliveryTimeLimit[j - 1] = deliveryPath[j - 2].ShippingOrder.Date_order.AddMinutes(DELIVERY_LIMIT);
                        deliveryTimeLimit[j - 1] = deliveryPath[j - 2].ShippingOrder.GetDeliveryLimit(Courier.DELIVERY_LIMIT);
                    }

                    // 4.3 Проверяем путь
                    rc = 43;
                    CourierDeliveryInfo pathInfo;
                    double[] nodeDeliveryTime;
                    int rc1 = courier.DeliveryCheck(startDelivery, betweenDistance, deliveryTimeLimit, totalOrderWeight, out pathInfo, out nodeDeliveryTime);
                    if (rc1 == 0)
                    {
                        if (pathInfo.Cost < bestCost)
                        {
                            bestCost = pathInfo.Cost;
                            bestPathInfo = pathInfo;
                            bestDeliveryPath = deliveryPath;
                        }
                    }
                }

                if (bestDeliveryPath == null)
                    return rc;

                // Сохраняем найденный путь обхода
                bestPathInfo.DeliveredOrders = bestDeliveryPath;

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
        /// Поиск пути с минимальной стоимостью среди специальных путей с минимальной длиной звеньев
        /// </summary>
        /// <param name="courier">Курьер</param>
        /// <param name="deliveredOrders">Доставляемые заказы</param>
        /// <param name="bestPathInfo">Путь с минимальной стоимостью</param>
        /// <returns>0 - путь найден; иначе - путь не найден</returns>
        private static int FindSpecialPathWithMinCost(Courier courier, CourierDeliveryInfo[] deliveredOrders, out CourierDeliveryInfo bestPathInfo)
        {
            // 1. Инициализация
            int rc = 1;
            bestPathInfo = null;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (courier == null)
                    return rc;
                if (deliveredOrders == null || deliveredOrders.Length <= 0)
                    return rc;

                int count = deliveredOrders.Length;

                // 3. Находим попарные расстояния между точками доставки, 
                //    подсчитытываем общий вес и находим наиболее раннее время на начала отгрузки
                rc = 3;
                double[,] pointDist = new double[count, count];
                double totalOrderWeight = 0;
                DateTime startDelivery = deliveredOrders[0].ShippingOrder.Date_collected;

                for (int i = 0; i < count; i++)
                {
                    Order order = deliveredOrders[i].ShippingOrder;
                    double latitude1 = order.Latitude;
                    double longitude1 = order.Longitude;

                    totalOrderWeight += order.Weight;

                    DateTime packEnd = order.Date_collected;
                    if (packEnd > startDelivery)
                        startDelivery = packEnd;

                    for (int j = i + 1; j < count; j++)
                    {
                        order = deliveredOrders[j].ShippingOrder;
                        double d = DISTANCE_ALLOWANCE * Helper.Distance(latitude1, longitude1, order.Latitude, order.Longitude);
                        pointDist[i, j] = d;
                        pointDist[j, i] = d;
                    }
                }

                // 4. Построение пути обхода c минимальной стоимостью среди count специальных путей
                rc = 4;
                CourierDeliveryInfo[] deliveryPath = new CourierDeliveryInfo[count];
                int[] orderIndex = new int[count];
                double[] betweenDistance = new double[count + 2];
                DateTime[] deliveryTimeLimit = new DateTime[count + 2];
                CourierDeliveryInfo[] bestDeliveryPath = null;
                double bestCost = double.MinValue;
                bool[] isSelected = new bool[count];

                for (int i = 0; i < count; i++)
                {
                    // 4.1 Строим специальный путь с заданной стартовой точкой и минимальными растояниями от точки до точки
                    rc = 41;
                    Array.Clear(isSelected, 0, count);
                    isSelected[i] = true;

                    orderIndex[0] = i;
                    deliveryPath[0] = deliveredOrders[i];

                    int currentOrderIndex = i;

                    for (int k = 1; k < count; k++)
                    {
                        double minDist = double.MaxValue;
                        int minOrderIndex = -1;

                        for (int j = 0; j < count; j++)
                        {
                            if (isSelected[j])
                                continue;
                            if (pointDist[currentOrderIndex, j] < minDist)
                            {
                                minDist = pointDist[currentOrderIndex, j];
                                minOrderIndex = j;
                            }
                        }

                        isSelected[minOrderIndex] = true;
                        orderIndex[k] = minOrderIndex;
                        deliveryPath[k] = deliveredOrders[minOrderIndex];
                    }

                    // 4.2 Готовим данные для DeliveryCheck
                    rc = 42;
                    betweenDistance[0] = 0;
                    betweenDistance[1] = deliveryPath[0].DistanceFromShop;
                    betweenDistance[count + 1] = deliveryPath[count - 1].DistanceFromShop;
                    //deliveryTimeLimit[count] = deliveryPath[count - 1].ShippingOrder.Date_order.AddMinutes(DELIVERY_LIMIT);
                    deliveryTimeLimit[count] = deliveryPath[count - 1].ShippingOrder.GetDeliveryLimit(Courier.DELIVERY_LIMIT);

                    for (int j = 2; j <= count; j++)
                    {
                        betweenDistance[j] = pointDist[orderIndex[j - 2], orderIndex[j - 1]];
                        //deliveryTimeLimit[j - 1] = deliveryPath[j - 2].ShippingOrder.Date_order.AddMinutes(DELIVERY_LIMIT);
                        deliveryTimeLimit[j - 1] = deliveryPath[j - 2].ShippingOrder.GetDeliveryLimit(Courier.DELIVERY_LIMIT);
                    }

                    // 4.3 Проверяем путь
                    rc = 43;
                    CourierDeliveryInfo pathInfo;
                    double[] nodeDeliveryTime;
                    int rc1 = courier.DeliveryCheck(startDelivery, betweenDistance, deliveryTimeLimit, totalOrderWeight, out pathInfo, out nodeDeliveryTime);
                    if (rc1 == 0)
                    {
                        if (pathInfo.Cost < bestCost)
                        {
                            bestCost = pathInfo.Cost;
                            bestPathInfo = pathInfo;
                            bestDeliveryPath = deliveryPath;
                        }
                    }
                }

                if (bestDeliveryPath == null)
                    return rc;

                // Сохраняем найденный путь обхода
                bestPathInfo.DeliveredOrders = bestDeliveryPath;

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
        /// Выбор изолированных заказов, которые могут быть доставлены заданным курьером
        /// </summary>
        /// <param name="courier">Курьер</param>
        /// <param name="courierStart">Время, с которого курьер может начать доставку</param>
        /// <param name="shop">Магазин, из которого осуществляется доставка</param>
        /// <param name="orders">Изолированные заказы</param>
        /// <param name="selectedOrders">Заказы, котоые могут быть доставлены курьером</param>
        /// <param name="notSelectedOrders">Заказы, оставшиеся не выбранными</param>
        /// <returns>0 - выбор произведен; иначе - выбор не произведен</returns>
        private static int SelectIsolatedOrdersForCourier(Courier courier, DateTime courierStart, Shop shop, Order[] orders, out CourierDeliveryInfo[] selectedOrders, out Order[] notSelectedOrders)
        {
            // 1. Инициализация
            int rc = 1;
            int rc1 = 1;
            selectedOrders = null;
            notSelectedOrders = orders;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (courier == null || courier.Status != CourierStatus.Ready)
                    return rc;
                if (shop == null)
                    return rc;
                if (orders == null || orders.Length <= 0)
                    return rc;

                switch (courier.CourierType.VechicleType)
                {
                    case CourierVehicleType.OnFoot:
                    case CourierVehicleType.Bicycle:
                    case CourierVehicleType.Car:
                        break;
                    default:
                        return rc;
                }

                // 3. Просчитываем время на доставку каждого товара в отдельности
                rc = 3;
                CourierDeliveryInfo[] deliveryInfo = new CourierDeliveryInfo[orders.Length];
                double shopLatitude = shop.Latitude;
                double shopLongitude = shop.Longitude;
                int count = 0;

                for (int i = 0; i < orders.Length; i++)
                {
                    Order order = orders[i];
                    DateTime assemblyEnd = order.Date_collected;
                    double distance = DISTANCE_ALLOWANCE * Helper.Distance(shopLatitude, shopLongitude, order.Latitude, order.Longitude);
                    //DateTime deliveryTimeLimit = order.Date_order.AddMinutes(DELIVERY_LIMIT);
                    DateTime deliveryTimeLimit = order.GetDeliveryLimit(Courier.DELIVERY_LIMIT);

                    if (assemblyEnd != DateTime.MinValue)
                    {
                        CourierDeliveryInfo courierDeliveryInfo;
                        rc1 = courier.DeliveryCheck(assemblyEnd, distance, deliveryTimeLimit, order.Weight, out courierDeliveryInfo);
                        if (rc1 == 0)
                        {
                            courierDeliveryInfo.ShippingOrder = order;
                            courierDeliveryInfo.DistanceFromShop = distance;
                            courierDeliveryInfo.ShippingOrder = order;
                            deliveryInfo[count++] = courierDeliveryInfo;
                        }
                    }
                }

                if (count <= 0)
                    return rc;
                if (count < deliveryInfo.Length)
                {
                    Array.Resize(ref deliveryInfo, count);
                }

                // 4. Сортируем по правой границе интервала начала доставки
                rc = 4;
                Array.Sort(deliveryInfo, CompareByEndDeliveryInterval);

                // 5. Выбор заказов, которые могут быть доставлены заданным курьером
                rc = 5;
                DateTime returnTime = courierStart;
                int startIndex = 0;
                selectedOrders = new CourierDeliveryInfo[count];
                int selectedCount = 0;
                bool flag = true;

                while (flag)
                {
                    flag = false;

                    for (int i = startIndex; i < count; i++)
                    {
                        // 5.1 Пропускаем обработанные заказы
                        rc = 51;
                        CourierDeliveryInfo courierDeliveryInfo = deliveryInfo[i];
                        if (courierDeliveryInfo.Completed)
                            continue;

                        // 5.2 Если заказ не может быть выполнен по времени
                        rc = 52;
                        if (courierDeliveryInfo.EndDeliveryInterval < returnTime)
                            continue;

                        // 5.3 Выбираем заказ
                        rc = 53;
                        if (courierDeliveryInfo.StartDeliveryInterval >= returnTime)
                        {
                            courierDeliveryInfo.StartDelivery = courierDeliveryInfo.StartDeliveryInterval;
                            returnTime = courierDeliveryInfo.StartDeliveryInterval.AddMinutes(courierDeliveryInfo.ExecutionTime);
                        }
                        else
                        {
                            courierDeliveryInfo.StartDelivery = returnTime;
                            returnTime = returnTime.AddMinutes(courierDeliveryInfo.ExecutionTime);
                        }

                        flag = true;
                        courierDeliveryInfo.Completed = true;
                        selectedOrders[selectedCount++] = courierDeliveryInfo;
                        startIndex = i + 1;
                    }
                }

                if (selectedCount < selectedOrders.Length)
                {
                    Array.Resize(ref selectedOrders, selectedCount);
                }

                // 6. Отбираем не наначенные заказы
                rc = 6;
                if (selectedCount > 0)
                {
                    int[] orderId = selectedOrders.Select(p => p.ShippingOrder.Id_order).ToArray();
                    Array.Sort(orderId);
                    notSelectedOrders = orders.Where(p => Array.BinarySearch(orderId, p.Id_order) < 0).ToArray();
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
        /// Вывод в отчет истории отгрузок
        /// </summary>
        /// <param name="report"></param>
        /// <param name="historyName"></param>
        /// <param name="shopNo"></param>
        /// <param name="deliveryHistory"></param>
        /// <returns></returns>
        private static int PrintDeliveryHistory(ExcelReport report, string historyName, int shopNo, CourierDeliveryInfo[] deliveryHistory)
        {
            // 1. Инициализация
            int rc = 1;
            int rc1 = 1;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (report == null)
                    return rc;
                if (string.IsNullOrEmpty(historyName))
                    return rc;
                if (deliveryHistory == null || deliveryHistory.Length <= 0)
                    return rc;

                // 3. Цикл печати
                rc = 3;
                bool isOk = true;

                Array.Sort(deliveryHistory, CompareByStartDelivery);

                foreach (CourierDeliveryInfo deliveryInfo in deliveryHistory)
                {
                    //int[] deliveredOrderId;
                    //if (deliveryInfo.DeliveredOrders != null)
                    //{
                    //    deliveredOrderId = deliveryInfo.DeliveredOrders.Select(p => p.ShippingOrder.Id_order).ToArray();
                    //}
                    //else
                    //{
                    //    deliveredOrderId = new int[] { deliveryInfo.ShippingOrder.Id_order };
                    //}

                    //if (deliveryInfo.DeliveryCourier.CourierType.VechicleType == CourierVehicleType.GettTaxi ||
                    //    deliveryInfo.DeliveryCourier.CourierType.VechicleType == CourierVehicleType.YandexTaxi ||
                    //    deliveryInfo.DeliveryCourier.CourierType.VechicleType == CourierVehicleType.OnFoot1 ||
                    //    deliveryInfo.DeliveryCourier.CourierType.VechicleType == CourierVehicleType.Bicycle1 ||
                    //    deliveryInfo.DeliveryCourier.CourierType.VechicleType == CourierVehicleType.Car1)
                    //{
                    //    rc1 = report.PrintHistoryRow(historyName,
                    //        shopNo,
                    //        deliveryInfo.StartDelivery,
                    //        deliveryInfo.StartDelivery.AddMinutes(deliveryInfo.DeliveryTime),
                    //        deliveryInfo.DeliveryCourier.Id,
                    //        Enum.GetName(deliveryInfo.DeliveryCourier.CourierType.VechicleType.GetType(), deliveryInfo.DeliveryCourier.CourierType.VechicleType),
                    //        deliveryInfo.OrderCount,
                    //        deliveryInfo.Weight, 
                    //        deliveryInfo.Cost,
                    //        deliveredOrderId);
                    //}
                    //else
                    //{

                    //    rc1 = report.PrintHistoryRow(historyName,
                    //        shopNo,
                    //        deliveryInfo.StartDelivery,
                    //        deliveryInfo.StartDelivery.AddMinutes(deliveryInfo.ExecutionTime),
                    //        deliveryInfo.DeliveryCourier.Id,
                    //        Enum.GetName(deliveryInfo.DeliveryCourier.CourierType.VechicleType.GetType(), deliveryInfo.DeliveryCourier.CourierType.VechicleType),
                    //        deliveryInfo.OrderCount,
                    //        deliveryInfo.Weight, 
                    //        -1,
                    //       deliveredOrderId);
                    //}

                    rc1 = report.PrintHistoryRowEx(historyName, deliveryInfo);

                    if (rc1 != 0)
                        isOk = false;
                }

                if (!isOk)
                    return rc;

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
        /// Пополнение статистики курьров
        /// </summary>
        /// <param name="courierSummary">Статистика курьеров</param>
        /// <param name="shopNo">Номер магазина</param>
        /// <param name="deliveryHistory">История отгрузок</param>
        /// <returns></returns>
        private static int AddToCourierSummary(Dictionary<string, ShopCourierStatistics> courierSummary, int shopNo, CourierDeliveryInfo[] deliveryHistory)
        {
            // 1. Инициализация
            int rc = 1;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (courierSummary == null)
                    return rc;
                if (deliveryHistory == null || deliveryHistory.Length <= 0)
                    return rc;

                // 3. Пополняем статистику
                rc = 3;
                double cost;

                foreach(CourierDeliveryInfo deliveryInfo in deliveryHistory)
                {
                    string key = ShopCourierStatistics.GetKey(shopNo, deliveryInfo.StartDelivery, deliveryInfo.DeliveryCourier.Id);
                    ShopCourierStatistics courierStatistics;
                    if (!courierSummary.TryGetValue(key, out courierStatistics))
                    {
                        switch (deliveryInfo.DeliveryCourier.CourierType.VechicleType)
                        {
                            case CourierVehicleType.OnFoot:
                            case CourierVehicleType.Bicycle:
                            case CourierVehicleType.Car:
                                cost = deliveryInfo.DeliveryCourier.TotalCost;
                                //cost = WORK_DURATION * (1 + deliveryInfo.DeliveryCourier.CourierType.Insurance) * deliveryInfo.DeliveryCourier.CourierType.HourlyRate;
                                break;
                            //case CourierVehicleType.GettTaxi:
                            //case CourierVehicleType.YandexTaxi:
                            //    cost = -1;
                            //    break;
                            default:
                                cost = 0;
                                break;
                        }

                        courierStatistics = new ShopCourierStatistics(shopNo, deliveryInfo.StartDelivery, deliveryInfo.DeliveryCourier, cost);
                        courierSummary.Add(key, courierStatistics);
                    }

                    courierStatistics.Add(deliveryInfo);
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
        /// Вывод в отчет статистики по курьерам
        /// </summary>
        /// <param name="report">Отчет</param>
        /// <param name="summaryName">Название Summary-листа</param>
        /// <param name="courierStatistics">Статистика по курьерам</param>
        /// <returns>0 - отчет построен; иначе - отчет не построен</returns>
        private static int PrintCourierSummary(ExcelReport report, string summaryName, ShopCourierStatistics[] courierStatistics)
        {
            // 1. Инициализация
            int rc = 1;
            int rc1 = 1;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (report == null)
                    return rc;
                if (string.IsNullOrEmpty(summaryName))
                    return rc;
                if (courierStatistics == null || courierStatistics.Length <= 0)
                    return rc;

                // 3. Цикл печати
                rc = 3;
                bool isOk = true;

                Array.Sort(courierStatistics, CompareByKey);

                foreach (ShopCourierStatistics courierStat in courierStatistics)
                {
                    //rc1 = report.PrintSummaryRow(summaryName,
                    //    courierStat.Date,
                    //    courierStat.ShopId,
                    //    courierStat.CourierTypeToString(),
                    //    courierStat.CourierId,
                    //    courierStat.OrderCount,
                    //    courierStat.TotalWeight,
                    //    courierStat.TotalCost,
                    //    courierStat.OrderCost);

                    rc1 = report.PrintSummaryRowEx(summaryName, courierStat);

                    if (rc1 != 0)
                        isOk = false;
                }

                if (!isOk)
                    return rc;

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
        /// Квазиоптимальное решение для одного магазина за один день
        /// для пяти видов курьеров (OnFoot, OnBicycle, OnCar, GettTaxi, YandexTaxi)
        /// </summary>
        /// <param name="shop">Магазин</param>
        /// <param name="shopOrders">История заказов за один день</param>
        /// <param name="notDeliveredOrders">Заказы, которые не удалось доставить</param>
        /// <returns>0 - решение найдено; иначе - решение не найдено</returns>
        private static int FindOptimalDailySolution(Shop shop, Order[] shopOrders, ref Permutations permutationsRepository, out CourierDeliveryInfo[] deliveryHistory, out Order[] notDeliveredOrders)
        {
            // 1. Инициализация
            int rc = 1;
            int rc1 = 1;
            deliveryHistory = null;
            notDeliveredOrders = null;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (shop == null)
                    return rc;
                if (shopOrders == null || shopOrders.Length <= 0)
                    return rc;
                if (permutationsRepository == null)
                    permutationsRepository = new Permutations();

                // 3. Создаём пять видов курьеров
                rc = 3;
                Courier gettCourier = new Courier(1, new CourierType_GettTaxi());
                gettCourier.Status = CourierStatus.Ready;
                gettCourier.WorkStart = new TimeSpan(0);
                gettCourier.WorkEnd = new TimeSpan(24, 0, 0);

                Courier yandexCourier = new Courier(2, new CourierType_YandexTaxi());
                yandexCourier.Status = CourierStatus.Ready;
                yandexCourier.WorkStart = new TimeSpan(0);
                yandexCourier.WorkEnd = new TimeSpan(24, 0, 0);

                Courier onFootCourier = new Courier(3, new CourierType_OnFoot());
                onFootCourier.Status = CourierStatus.Ready;
                onFootCourier.WorkStart = new TimeSpan(0);
                onFootCourier.WorkEnd = new TimeSpan(24, 0, 0);

                Courier onBicycleCourier = new Courier(4, new CourierType_Bicycle());
                onBicycleCourier.Status = CourierStatus.Ready;
                onBicycleCourier.WorkStart = new TimeSpan(0);
                onBicycleCourier.WorkEnd = new TimeSpan(24, 0, 0);

                Courier onCarCourier = new Courier(5, new CourierType_Car());
                onCarCourier.Status = CourierStatus.Ready;
                onCarCourier.WorkStart = new TimeSpan(0);
                onCarCourier.WorkEnd = new TimeSpan(24, 0, 0);

                //Courier[] allTypeCouriers = new Courier[] { gettCourier, yandexCourier, onFootCourier, onBicycleCourier, onCarCourier };
                Courier[] allTypeCouriers = new Courier[] { onFootCourier, onBicycleCourier, onCarCourier };

                // 4. Расчитываем расстояние от магазина до всех точек доставки
                rc = 4;
                double shopLatitude = shop.Latitude;
                double shopLongitude = shop.Longitude;
                double[] distanceFromShop = new double[shopOrders.Length];

                for (int i = 0; i < shopOrders.Length; i++)
                {
                    Order order = shopOrders[i];
                    order.Completed = false;
                    distanceFromShop[i] = DISTANCE_ALLOWANCE * Helper.Distance(shopLatitude, shopLongitude, order.Latitude, order.Longitude);
                }

                // 5. Расчитываем для каждого заказа данные для доставки всеми видами курьеров
                rc = 5;
                CourierDeliveryInfo[][] allTypeCouriersDeliveryInfo = new CourierDeliveryInfo[allTypeCouriers.Length][];
                CourierDeliveryInfo[] deliveryInfo = new CourierDeliveryInfo[shopOrders.Length];
                int count = 0;

                for (int i = 0; i < allTypeCouriers.Length; i++)
                {
                    Courier courier = allTypeCouriers[i];
                    count = 0;

                    for (int j = 0; j < shopOrders.Length; j++)
                    {
                        Order order = shopOrders[j];
                        DateTime assemblyEnd = order.Date_collected;

                        if (assemblyEnd != DateTime.MinValue)
                        {
                            DateTime deliveryTimeLimit = order.GetDeliveryLimit(Courier.DELIVERY_LIMIT);
                            CourierDeliveryInfo courierDeliveryInfo;
                            double distance = distanceFromShop[j];
                            rc1 = courier.DeliveryCheck(assemblyEnd, distance, deliveryTimeLimit, order.Weight, out courierDeliveryInfo);
                            if (rc1 == 0)
                            {
                                courierDeliveryInfo.ShippingOrder = order;
                                courierDeliveryInfo.DistanceFromShop = distance;
                                deliveryInfo[count++] = courierDeliveryInfo;
                            }
                        }
                    }

                    if (count > 0)
                    {
                        CourierDeliveryInfo[] courierInfo = deliveryInfo.Take(count).ToArray();
                        Array.Sort(courierInfo, CompareByEndDeliveryInterval);
                        allTypeCouriersDeliveryInfo[i] = courierInfo;
                    }
                    else
                    {
                        allTypeCouriersDeliveryInfo[i] = new CourierDeliveryInfo[0];
                    }
                }

                // 6. Отмечаем точки интервалов начала доставки (ИНД) на временной оси для каждого типа курьеров
                rc = 6;
                int[][,] allTypeCouriersIntervalHist = new int[allTypeCouriers.Length][,];

                for (int i = 0; i < allTypeCouriers.Length; i++)
                {
                    deliveryInfo = allTypeCouriersDeliveryInfo[i];
                    int[,] intervalHist = new int[1440, ORDERS_PER_MINUTE_LIMIT];

                    for (int j = 0; j < deliveryInfo.Length; j++)
                    {
                        int startHistIndex = (int)deliveryInfo[j].StartDeliveryInterval.TimeOfDay.TotalMinutes;
                        int endHistIndex = (int)deliveryInfo[j].EndDeliveryInterval.TimeOfDay.TotalMinutes;

                        for (int k = startHistIndex; k <= endHistIndex; k++)
                        {
                            int intervalCount = intervalHist[k, 0];
                            if (intervalCount < ORDERS_PER_MINUTE_LIMIT)
                            {
                                intervalHist[k, 0] = ++intervalCount;
                                intervalHist[k, intervalCount] = j;
                            }
                        }
                    }

                    allTypeCouriersIntervalHist[i] = intervalHist;
                }

                // 7. Цикл поиска оптимального решения
                rc = 7;
                ShopCouriers couriersResource = new ShopCouriers();
                deliveryHistory = new CourierDeliveryInfo[shopOrders.Length];
                int deliveryHistoryCount = 0;
                int hourlyCourierId = 3;

                while (true)
                {
                    // 7.1 Для каждого типа курьера считаем отдельно стоимость следующего шага
                    rc = 71;
                    CourierDeliveryInfo bestDeliveryByTaxi = null;
                    CourierDeliveryInfo[] bestDeliveryByHourlyCourier = null;
                    double bestTotalWorkTime = 0;
                    int bestTotalOrders = 0;
                    double bestTotalCost = 0;

                    for (int k = 0; k < allTypeCouriers.Length; k++)
                    {
                        Courier courier = allTypeCouriers[k];
                        CourierDeliveryInfo deliveryByTaxi = null;
                        CourierDeliveryInfo[] deliveryByHourlyCourier = null;
                        double totalWorkTime;
                        int totalOrders;
                        double totalCost;

                        if (courier.CourierType.VechicleType == CourierVehicleType.GettTaxi ||
                            courier.CourierType.VechicleType == CourierVehicleType.YandexTaxi)
                        {
                            // 7.2 Для такси
                            rc = 72;
                            rc1 = BuildTaxiDelivery(courier, allTypeCouriersDeliveryInfo[k], allTypeCouriersIntervalHist[k], ref permutationsRepository, out deliveryByTaxi);
                            if (rc1 == 0 && deliveryByTaxi != null)
                            {
                                if (bestDeliveryByTaxi == null)
                                {
                                    bestDeliveryByTaxi = deliveryByTaxi;
                                }
                                else if (deliveryByTaxi.OrderCost < bestDeliveryByTaxi.OrderCost)
                                {
                                    bestDeliveryByTaxi = deliveryByTaxi;
                                }
                            }
                        }
                        else
                        {
                            // 7.3 Для почасовых курьеров
                            rc = 73;
                            rc1 = BuildHourlyDelivery(courier, allTypeCouriersDeliveryInfo[k], allTypeCouriersIntervalHist[k], ref permutationsRepository, 
                                out deliveryByHourlyCourier,
                                out totalWorkTime,  
                                out totalOrders,
                                out totalCost);

                            if (rc1 == 0 && deliveryByHourlyCourier != null)
                            {
                                if (bestDeliveryByHourlyCourier == null && totalOrders > 0)
                                {
                                    bestDeliveryByHourlyCourier = deliveryByHourlyCourier;
                                    bestTotalWorkTime = totalWorkTime;
                                    bestTotalOrders = totalOrders;
                                    bestTotalCost = totalCost;
                                }
                                else if (totalOrders > 0 && (totalCost / totalOrders) < (bestTotalCost / bestTotalOrders))
                                {
                                    bestDeliveryByHourlyCourier = deliveryByHourlyCourier;
                                    bestTotalWorkTime = totalWorkTime;
                                    bestTotalOrders = totalOrders;
                                    bestTotalCost = totalCost;
                                }
                            }
                        }
                    }

                    // 7.4 Выбираем один результат и делаем отгрузку
                    rc = 74;
                    int caseNo = 0;
                    if (bestDeliveryByTaxi != null) caseNo += 2;
                    if (bestDeliveryByHourlyCourier != null) caseNo++;
                    Courier newCourier;
                    switch (caseNo)  // Taxi Hourly
                    {
                        case 0:      //   -    -   (Невозможно сделать шаг вперед)
                            goto ExitWhile;
                        case 1:      //   -    +   (Только почасовой курьер)
                            newCourier = bestDeliveryByHourlyCourier[0].DeliveryCourier.Clone(hourlyCourierId++);
                            newCourier.TotalDeliveryTime = bestTotalWorkTime;
                            newCourier.TotalCost = bestTotalCost;

                            foreach (CourierDeliveryInfo dlvInfo in bestDeliveryByHourlyCourier)
                            {
                                dlvInfo.DeliveryCourier = newCourier;
                                deliveryHistory[deliveryHistoryCount++] = dlvInfo;
                                dlvInfo.ShippingOrder.Completed = true;

                                if (dlvInfo.DeliveredOrders != null)
                                {
                                    foreach (CourierDeliveryInfo dlvOrder in dlvInfo.DeliveredOrders)
                                    {
                                        dlvOrder.Completed = true;
                                    }
                                }
                            }

                            break;
                        case 2:      //   +    -   (Только такси)
                            deliveryHistory[deliveryHistoryCount++] = bestDeliveryByTaxi;
                            bestDeliveryByTaxi.ShippingOrder.Completed = true;

                            if (bestDeliveryByTaxi.DeliveredOrders != null)
                            {
                                foreach (CourierDeliveryInfo dlvOrder in bestDeliveryByTaxi.DeliveredOrders)
                                {
                                    dlvOrder.Completed = true;
                                }
                            }
                            break;
                        case 3:      //   +    +   (Такси + почасовой курьер)
                            if ((bestTotalCost / bestTotalOrders) <= ORDER_COST_THRESHOLD)
                            {
                                newCourier = bestDeliveryByHourlyCourier[0].DeliveryCourier.Clone(hourlyCourierId++);
                                newCourier.TotalDeliveryTime = bestTotalWorkTime;
                                newCourier.TotalCost = bestTotalCost;

                                foreach (CourierDeliveryInfo dlvInfo in bestDeliveryByHourlyCourier)
                                {
                                    dlvInfo.ShippingOrder.Completed = true;
                                    dlvInfo.DeliveryCourier = newCourier;
                                    deliveryHistory[deliveryHistoryCount++] = dlvInfo;
                                    if (dlvInfo.DeliveredOrders != null)
                                    {
                                        foreach (CourierDeliveryInfo dlvOrder in dlvInfo.DeliveredOrders)
                                        {
                                            dlvOrder.Completed = true;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                deliveryHistory[deliveryHistoryCount++] = bestDeliveryByTaxi;
                                bestDeliveryByTaxi.ShippingOrder.Completed = true;
                                if (bestDeliveryByTaxi.DeliveredOrders != null)
                                {
                                    foreach (CourierDeliveryInfo dlvOrder in bestDeliveryByTaxi.DeliveredOrders)
                                    {
                                        dlvOrder.Completed = true;
                                    }
                                }
                            }
                            break;
                    }
                }

                ExitWhile:

                // 8. Извлекаем неотгруженные заказы (например те, которые не могут быть доставлены в срок)
                rc = 8;
                notDeliveredOrders = shopOrders.Where(p => !p.Completed).ToArray();
                if (notDeliveredOrders != null && notDeliveredOrders.Length > 0)
                {
                    rc = rc;
                }

                // 9. Подрезаем History
                rc = 9;
                Array.Resize(ref deliveryHistory, deliveryHistoryCount);

                // 10. Выход - Ok
                rc = 0;
                return rc;
            }
            catch
            {
                return rc;
            }
        }

        /// <summary>
        /// Выбор оптимального маршрута доставки 
        /// заданных заказов при помощи такси
        /// </summary>
        /// <param name="courier">Курьер-такси</param>
        /// <param name="deliveryInfo">Доставляемые товары</param>
        /// <param name="intervalHist">Точки ИНД доставляемых товаров</param>
        /// <param name="permutationsRepository">Генератор перестановок</param>
        /// <param name="stepDeliveryInfo">Информации о построенном маршруте</param>
        /// <returns>0 - маршрут построен; иначе - маршрут не построен</returns>
        public static int BuildTaxiDelivery(Courier courier, CourierDeliveryInfo[] deliveryInfo, int[,] intervalHist, ref Permutations permutationsRepository, out CourierDeliveryInfo stepDeliveryInfo)
        {
            // 1. Инициализация
            int rc = 1;
            int rc1 = 1;
            stepDeliveryInfo = null;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (courier == null ||
                    !(courier.CourierType.VechicleType == CourierVehicleType.GettTaxi ||
                      courier.CourierType.VechicleType == CourierVehicleType.YandexTaxi ||
                      courier.CourierType.VechicleType == CourierVehicleType.OnFoot1 ||
                      courier.CourierType.VechicleType == CourierVehicleType.Bicycle1 ||
                      courier.CourierType.VechicleType == CourierVehicleType.Car1))
                    return rc;
                if (deliveryInfo == null || deliveryInfo.Length <= 0)
                    return rc;
                if (intervalHist == null || intervalHist.Length <= 0)
                    return rc;
                if (permutationsRepository == null)
                    permutationsRepository = new Permutations();

                // 3. Находим первый неотгруженный товар
                rc = 3;
                CourierDeliveryInfo nextOrder = deliveryInfo.FirstOrDefault(p => !p.Completed);
                if (nextOrder == null)
                    return rc;

                // 4. Выбираем все заказы - кандидаты на отгрузку по времени
                //     (имеющие пересекающиеся интервалы доставки)
                rc = 4;
                int startHistIndex = (int)nextOrder.StartDeliveryInterval.TimeOfDay.TotalMinutes;
                int endHistIndex = (int)nextOrder.EndDeliveryInterval.TimeOfDay.TotalMinutes;
                int maxCount = 1;
                int maxIndex = -1;
                CourierDeliveryInfo[] selectedOrders;

                for (int i = startHistIndex; i <= endHistIndex; i++)
                {
                    if (intervalHist[i, 0] > maxCount)
                    {
                        maxCount = intervalHist[i, 0];
                        maxIndex = i;
                    }
                }

                // Изолированная точка
                if (maxIndex == -1)
                {
                    stepDeliveryInfo = nextOrder;
                    return rc = 0;
                }

                // 5. Отбираем заказы подходящие по времени
                rc = 5;
                selectedOrders = new CourierDeliveryInfo[maxCount - 1];
                int count = 0;

                for (int i = 1; i <= maxCount; i++)
                {
                    int orderIndex = intervalHist[maxIndex, i];
                    CourierDeliveryInfo dInfo = deliveryInfo[orderIndex];
                    if (!dInfo.Completed && dInfo != nextOrder)
                    {
                        selectedOrders[count++] = deliveryInfo[orderIndex];
                    }
                }

                if (count <= 0)
                {
                    stepDeliveryInfo = nextOrder;
                    return rc = 0;
                }

                Array.Resize(ref selectedOrders, count);

                // 6. Расчитываем расстояние от next-заказа до выбранных точек доставки
                rc = 6;
                double[] nextDist = new double[selectedOrders.Length];
                double nextLatitude = nextOrder.ShippingOrder.Latitude;
                double nextLongitude = nextOrder.ShippingOrder.Longitude;

                for (int i = 0; i < selectedOrders.Length; i++)
                {
                    Order order = selectedOrders[i].ShippingOrder;
                    nextDist[i] = DISTANCE_ALLOWANCE * Helper.Distance(nextLatitude, nextLongitude, order.Latitude, order.Longitude);
                }

                // 7. Фильтруем по расстоянию и возможности доставки
                rc = 7;
                DateTime[] deliveryTimeLimit = new DateTime[4];
                deliveryTimeLimit[0] = DateTime.MaxValue;
                deliveryTimeLimit[3] = DateTime.MaxValue;
                double[] betweenDistance = new double[4];
                count = 0;

                for (int i = 0; i < selectedOrders.Length; i++)
                {
                    CourierDeliveryInfo selOrder = selectedOrders[i];
                    DateTime modelTime = selOrder.ShippingOrder.Date_collected;
                    if (modelTime < nextOrder.ShippingOrder.Date_collected)
                        modelTime = nextOrder.ShippingOrder.Date_collected;
                    double totalWeight = selOrder.ShippingOrder.Weight + nextOrder.ShippingOrder.Weight;

                    betweenDistance[1] = nextOrder.DistanceFromShop;
                    betweenDistance[2] = nextDist[i];
                    betweenDistance[3] = selOrder.DistanceFromShop;
                    deliveryTimeLimit[1] = nextOrder.ShippingOrder.GetDeliveryLimit(Courier.DELIVERY_LIMIT);
                    deliveryTimeLimit[2] = selOrder.ShippingOrder.GetDeliveryLimit(Courier.DELIVERY_LIMIT);

                    CourierDeliveryInfo twoOrdersDeliveryInfo;
                    double[] nodeDeliveryTime;
                    rc1 = courier.DeliveryCheck(modelTime, betweenDistance, deliveryTimeLimit, totalWeight, out twoOrdersDeliveryInfo, out nodeDeliveryTime);
                    if (rc1 != 0)
                    {
                        double temp = betweenDistance[1];
                        betweenDistance[1] = betweenDistance[2];
                        betweenDistance[2] = temp;

                        DateTime tempTime = deliveryTimeLimit[1];
                        deliveryTimeLimit[1] = deliveryTimeLimit[2];
                        deliveryTimeLimit[2] = tempTime;
                        double[] nodeDeliveryTimeX;
                        rc1 = courier.DeliveryCheck(modelTime, betweenDistance, deliveryTimeLimit, totalWeight, out twoOrdersDeliveryInfo, out nodeDeliveryTimeX);
                    }

                    if (rc1 == 0)
                    {
                        selectedOrders[count++] = selOrder;
                    }
                }

                if (count <= 0)
                {
                    stepDeliveryInfo = nextOrder;
                    return rc = 0;
                }

                Array.Resize(ref selectedOrders, count + 1);
                selectedOrders[count++] = nextOrder;  // Исходный заказ последний
                Array.Reverse(selectedOrders);  // Исходный заказ первый

                // 8. Решаем задачу комивояжера - нужно построить путь обхода точек доставки с минимальной стоимостью.
                //    Если невозможно вовремя доставить все заказы, то выбираем путь с максимальным числом точек доставки
                //    и минимальной стоимостью. Любой путь должен включать next-точку (точку с индексом 0)
                rc = 8;
                CourierDeliveryInfo bestPathInfo = null;
                if (count <= 8)
                {
                    // 8.1 Решаем перебором всех вариантов
                    rc = 81;
                    int[,] permutations = permutationsRepository.GetPermutations(count);
                    rc1 = FindPathWithMinCostEx(courier, selectedOrders, permutations, DateTime.MinValue, out bestPathInfo);
                    if (rc1 == 0 && bestPathInfo != null)
                    {
                        bestPathInfo.ShippingOrder = nextOrder.ShippingOrder;
                        stepDeliveryInfo = bestPathInfo;
                    }
                }
                else
                {
                    // 8.2. Применим 2-opt алгоритм
                    rc1 = FindPathWithMinCostBy2Opt(courier, selectedOrders, DateTime.MinValue, out bestPathInfo);
                    if (rc1 == 0 && bestPathInfo != null)
                    {
                        bestPathInfo.ShippingOrder = nextOrder.ShippingOrder;
                        stepDeliveryInfo = bestPathInfo;
                    }
                }

                if (stepDeliveryInfo == null)
                    return rc;


                // 9. Выход - Ok
                rc = 0;
                return rc;
            }
            catch
            {
                return rc;
            }
        }

        /// <summary>
        /// Поиск пути с минимальной стоимостью среди заданных перестановок
        /// </summary>
        /// <param name="courier">Курьер</param>
        /// <param name="deliveredOrders">Доставляемые заказы</param>
        /// <param name="permutations">Перестановки, среди которых осуществляется поиск</param>
        /// <param name="startTime">Время, начиная с которого можно использовать курьера</param>
        /// <param name="bestPathInfo">Путь с минимальной стоимостью</param>
        /// <returns>0 - путь найден; иначе - путь не найден</returns>
        public static int FindPathWithMinCostEx(Courier courier, CourierDeliveryInfo[] deliveredOrders, int[,] permutations, DateTime startTime, out CourierDeliveryInfo bestPathInfo)
        {
            // 1. Инициализация
            int rc = 1;
            bestPathInfo = null;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (courier == null)
                    return rc;
                if (deliveredOrders == null || deliveredOrders.Length <= 0)
                    return rc;

                int count = deliveredOrders.Length;

                if (permutations == null || permutations.Length <= 0)
                    return rc;
                if (permutations.GetLength(1) != count)
                    return rc;

                // 3. Находим попарные расстояния между точками доставки, 
                rc = 3;
                double[,] pointDist = new double[count, count];

                for (int i = 0; i < count; i++)
                {
                    Order order = deliveredOrders[i].ShippingOrder;
                    double latitude1 = order.Latitude;
                    double longitude1 = order.Longitude;

                    for (int j = i + 1; j < count; j++)
                    {
                        order = deliveredOrders[j].ShippingOrder;
                        double d = DISTANCE_ALLOWANCE * Helper.Distance(latitude1, longitude1, order.Latitude, order.Longitude);
                        pointDist[i, j] = d;
                        pointDist[j, i] = d;
                    }
                }

                // 4. Решаем задачу комивояжера - построение пути обхода c минимальной стоимостью
                rc = 4;
                int permutationCount = permutations.GetLength(0);
                CourierDeliveryInfo[] deliveryPath = new CourierDeliveryInfo[count];
                int[] orderIndex = new int[count];
                //int[] bestOrderIndex = new int[count];
                double bestCost = double.MaxValue;
                int bestOrderCount = -1;

                for (int i = 0; i < permutationCount; i++)
                {
                    //if (i == 6883)
                    //{
                    //    rc = rc;
                    //}
                    // 4.1 Выбираем индексы перестановки
                    rc = 41;
                    int nextOrederIndex = -1;

                    for (int j = 0; j < count; j++)
                    {
                        int index = permutations[i, j];
                        if (index == 0) nextOrederIndex = j;
                        orderIndex[j] = index;
                    }

                    // 4.2 Проверяем путь соответствующий перестановке
                    rc = 42;
                    CourierDeliveryInfo pathInfo;
                    double[] nodeDeliveryTime;
                    DateTime startDelivery;
                    int rc1 = DeliveryCheck(courier, deliveredOrders, orderIndex, pointDist, startTime, out pathInfo, out startDelivery, out nodeDeliveryTime);
                    if (rc1 == 0)
                    {
                        if (bestOrderCount < pathInfo.OrderCount)
                        {
                            bestOrderCount = pathInfo.OrderCount;
                            bestCost = pathInfo.Cost;
                            bestPathInfo = pathInfo;
                            //orderIndex.CopyTo(bestOrderIndex, 0);
                            //bestOrderIndex = orderIndex;
                        }
                        else if (bestOrderCount == pathInfo.OrderCount && pathInfo.Cost < bestCost)
                        {
                            //Console.WriteLine($"i ={i}");
                            bestCost = pathInfo.Cost;
                            bestPathInfo = pathInfo;
                            //bestOrderIndex = orderIndex;
                            //orderIndex.CopyTo(bestOrderIndex, 0);
                        }
                    }
                    else
                    {
                        // 4.3 Если обязательный для доставки заказ не может быть доставлен в срок
                        rc = 43;
                        if (nextOrederIndex < 0)
                            continue;
                        if (deliveredOrders[nextOrederIndex].ShippingOrder.GetDeliveryLimit(Courier.DELIVERY_LIMIT) < startDelivery.AddMinutes(nodeDeliveryTime[nextOrederIndex + 1]))
                            continue;

                        // 4.3 Отбрасываем заказы, которые не могут быть доставлены в срок по одному
                        rc = 43;
                        int[] testOrderIndex = (int[])orderIndex.Clone();
                        int startIndex = nextOrederIndex + 1;

                        while (startIndex < testOrderIndex.Length)
                        {
                            // 4.4 Находим первый заказ по пути следования, который не может быть доставлен в срок
                            rc = 44;
                            int outOfTimeIndex = -1;

                            for (int j = startIndex; j < testOrderIndex.Length; j++)
                            {
                                int index = testOrderIndex[j];
                                CourierDeliveryInfo deliveryOrder = deliveredOrders[j];
                                if (deliveryOrder.ShippingOrder.GetDeliveryLimit(Courier.DELIVERY_LIMIT) < startDelivery.AddMinutes(nodeDeliveryTime[j + 1]))
                                {
                                    outOfTimeIndex = j;
                                    break;
                                }
                            }

                            if (outOfTimeIndex < 0)
                                break;

                            // 4.5 Строим новый список индексов заказов
                            rc = 45;
                            testOrderIndex[outOfTimeIndex] = -1;
                            testOrderIndex = testOrderIndex.Where(p => p >= 0).ToArray();
                            startIndex = outOfTimeIndex;

                            // 4.6 Проверяем путь
                            rc = 46;
                            rc1 = DeliveryCheck(courier, deliveredOrders, testOrderIndex, pointDist, startTime, out pathInfo, out startDelivery, out nodeDeliveryTime);
                            if (rc1 == 0)
                            {
                                if (bestOrderCount < pathInfo.OrderCount)
                                {
                                    bestOrderCount = pathInfo.OrderCount;
                                    bestCost = pathInfo.Cost;
                                    bestPathInfo = pathInfo;
                                    //bestOrderIndex = testOrderIndex;
                                    //testOrderIndex.CopyTo(bestOrderIndex, 0);

                                }
                                else if (bestOrderCount == pathInfo.OrderCount && pathInfo.Cost < bestCost)
                                {
                                    bestCost = pathInfo.Cost;
                                    bestPathInfo = pathInfo;
                                    //bestOrderIndex = testOrderIndex;
                                    //testOrderIndex.CopyTo(bestOrderIndex, 0);
                                }

                                break;
                            }
                        }
                    }
                }

                if (bestPathInfo == null)
                    return rc;

                // Сохраняем найденный путь обхода
                //bestPathInfo.DeliveredOrders = bestOrderIndex.Select(p => deliveredOrders[p]).ToArray();

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
        /// Поиск пути с минимальной стоимостью c помщью 2-opt алгоритма
        /// (https://en.wikipedia.org/wiki/2-opt)
        /// </summary>
        /// <param name="courier">Курьер</param>
        /// <param name="deliveredOrders">Доставляемые заказы</param>
        /// <param name="startTime">Время, начиная с которого можно использовать курьера</param>
        /// <param name="bestPathInfo">Путь с минимальной стоимостью</param>
        /// <returns>0 - путь найден; иначе - путь не найден</returns>
        public static int FindPathWithMinCostBy2Opt(Courier courier, CourierDeliveryInfo[] deliveredOrders, DateTime startTime, out CourierDeliveryInfo bestPathInfo)
        {
            // 1. Инициализация
            int rc = 1;
            bestPathInfo = null;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (courier == null)
                    return rc;
                if (deliveredOrders == null || deliveredOrders.Length <= 0)
                    return rc;

                int count = deliveredOrders.Length;

                // 3. Находим попарные расстояния между точками доставки, 
                rc = 3;
                double[,] pointDist = new double[count, count];

                for (int i = 0; i < count; i++)
                {
                    Order order = deliveredOrders[i].ShippingOrder;
                    double latitude1 = order.Latitude;
                    double longitude1 = order.Longitude;

                    for (int j = i + 1; j < count; j++)
                    {
                        order = deliveredOrders[j].ShippingOrder;
                        double d = DISTANCE_ALLOWANCE * Helper.Distance(latitude1, longitude1, order.Latitude, order.Longitude);
                        pointDist[i, j] = d;
                        pointDist[j, i] = d;
                    }
                }

                // 4. Решаем задачу комивояжера - построение пути обхода c минимальной стоимостью
                rc = 4;
                CourierDeliveryInfo[] deliveryPath = new CourierDeliveryInfo[count];
                int[] orderIndex = new int[count];
                int[] bestOrderIndex = null;
                double bestCost = double.MaxValue;
                int bestOrderCount = -1;

                // 4.1 Инициализируем индексы
                rc = 41;
                for (int i = 0; i < count; i++)
                {
                    orderIndex[i] = i;
                }

                //for (int iter = 0; iter < 40320; iter++)
                for (int iter = 0; iter < 4096; iter++)
                {
                    for (int i = 0; i < count; i++)
                    {
                        //for (int k = i + 1; k < count; k++)
                        for (int k = i; k < count; k++)
                        {
                            // 4.2 Производим 2-opt преобразование индексов
                            rc = 42;
                            Array.Reverse(orderIndex, i, k - i + 1);

                            CourierDeliveryInfo pathInfo;
                            double[] nodeDeliveryTime;
                            DateTime startDelivery;
                            int rc1 = DeliveryCheck(courier, deliveredOrders, orderIndex, pointDist, startTime, out pathInfo, out startDelivery, out nodeDeliveryTime);
                            if (rc1 == 0)
                            {
                                if (bestOrderCount < pathInfo.OrderCount)
                                {
                                    bestOrderCount = pathInfo.OrderCount;
                                    bestCost = pathInfo.Cost;
                                    bestPathInfo = pathInfo;
                                    bestOrderIndex = orderIndex;
                                    goto NextIter;
                                }
                                else if (bestOrderCount == pathInfo.OrderCount && pathInfo.Cost < bestCost)
                                {
                                    bestCost = pathInfo.Cost;
                                    bestPathInfo = pathInfo;
                                    bestOrderIndex = orderIndex;
                                    goto NextIter;
                                }
                            }
                            else
                            {
                                // 4.3 Если обязательный для доставки заказ не может быть доставлен в срок
                                rc = 43;
                                int nextOrederIndex = -1;
                                int[] testOrderIndex = (int[]) orderIndex.Clone();

                                for (int j = 0; j < testOrderIndex.Length; j++)
                                {
                                    if (testOrderIndex[j] == 0)
                                    {
                                        nextOrederIndex = j;
                                        break;
                                    }
                                }

                                if (nextOrederIndex < 0)
                                    continue;
                                if (deliveredOrders[nextOrederIndex].ShippingOrder.GetDeliveryLimit(Courier.DELIVERY_LIMIT) < startDelivery.AddMinutes(nodeDeliveryTime[nextOrederIndex + 1]))
                                    continue;

                                // 4.4 Отбрасываем заказы, которые не могут быть доставлены в срок по одному
                                rc = 44;
                                int startIndex = nextOrederIndex + 1;

                                while (startIndex < testOrderIndex.Length)
                                {
                                    // 4.5 Находим первый заказ по пути следования, который не может быть доставлен в срок
                                    rc = 45;
                                    int outOfTimeIndex = -1;

                                    for (int j = startIndex; j < testOrderIndex.Length; j++)
                                    {
                                        int index = testOrderIndex[j];
                                        //CourierDeliveryInfo deliveryOrder = deliveredOrders[j];
                                        CourierDeliveryInfo deliveryOrder = deliveredOrders[index];
                                        if (deliveryOrder.ShippingOrder.GetDeliveryLimit(Courier.DELIVERY_LIMIT) < startDelivery.AddMinutes(nodeDeliveryTime[j + 1]))
                                        {
                                            outOfTimeIndex = j;
                                            break;
                                        }
                                    }

                                    if (outOfTimeIndex < 0)
                                        break;

                                    // 4.6 Строим новый список индексов заказов
                                    rc = 45;
                                    testOrderIndex[outOfTimeIndex] = -1;
                                    testOrderIndex = testOrderIndex.Where(p => p >= 0).ToArray();
                                    startIndex = outOfTimeIndex;

                                    // 4.7 Проверяем путь
                                    rc = 47;
                                    rc1 = DeliveryCheck(courier, deliveredOrders, testOrderIndex, pointDist, startTime, out pathInfo, out startDelivery, out nodeDeliveryTime);
                                    if (rc1 == 0)
                                    {
                                        if (bestOrderCount < pathInfo.OrderCount)
                                        {
                                            bestOrderCount = pathInfo.OrderCount;
                                            bestCost = pathInfo.Cost;
                                            bestPathInfo = pathInfo;
                                            bestOrderIndex = testOrderIndex;
                                            goto NextIter;
                                        }
                                        else if (bestOrderCount == pathInfo.OrderCount && pathInfo.Cost < bestCost)
                                        {
                                            bestCost = pathInfo.Cost;
                                            bestPathInfo = pathInfo;
                                            bestOrderIndex = testOrderIndex;
                                            goto NextIter;
                                        }

                                        break;
                                    }
                                }
                            }
                        }
                    }

                    // улучшений не было
                    break;
                    NextIter: ;
                }

                if (bestPathInfo == null)
                    return rc;

                // Сохраняем найденный путь обхода
                bestPathInfo.DeliveredOrders = bestOrderIndex.Select(p => deliveredOrders[p]).ToArray();

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
        /// Проверка пути заданного перестановкой
        /// </summary>
        /// <param name="courier">Курьер</param>
        /// <param name="deliveredOrders">Доставляемые заказы, из которых осуществляется выбор</param>
        /// <param name="orderIndex">Индексы выбираемых заказов в порядке следования</param>
        /// <param name="pointDist">Таблица попарных расстояний между точками доставки заказов</param>
        /// <param name="startTime">Время, начиная с которого можно использовать курьера</param>
        /// <param name="pathInfo">Информация о доставке заданных заказов</param>
        /// <param name="nodeDeliveryTime">Время доставки в минутах до каждой точки доставки от начала доставки</param>
        /// <returns>0 - путь прошел проверку и является допустимым; иначе - путь не прошел проверку</returns>
        private static int DeliveryCheck(Courier courier, CourierDeliveryInfo[] deliveredOrders, int[] orderIndex, double[,] pointDist, DateTime startTime, out CourierDeliveryInfo pathInfo, out DateTime startDelivery, out double[] nodeDeliveryTime)
        {
            // 1. Инициализация
            int rc = 1;
            pathInfo = null;
            nodeDeliveryTime = null;
            startDelivery = DateTime.MinValue;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (courier == null)
                    return rc;
                if (deliveredOrders == null || deliveredOrders.Length <= 0)
                    return rc;
                if (orderIndex == null || orderIndex.Length < 2)
                    return rc;
                if (pointDist == null || pointDist.Length <= 0)
                    return rc;

                int count = deliveredOrders.Length;
                if (pointDist == null ||
                    pointDist.GetLength(0) != count ||
                    pointDist.GetLength(1) != count)
                    return rc;

                // 3. Строим путь следования
                rc = 3;
                int orderCount = orderIndex.Length;
                CourierDeliveryInfo[] deliveryPath = new CourierDeliveryInfo[orderCount];
                double totalOrderWeight = 0;
                //startDelivery = deliveredOrders[orderIndex[0]].ShippingOrder.Date_collected;

                for (int i = 0; i < orderCount; i++)
                {
                    int index = orderIndex[i];
                    CourierDeliveryInfo deliveryOrder = deliveredOrders[index];
                    Order order = deliveryOrder.ShippingOrder;
                    deliveryPath[i] = deliveryOrder;

                    totalOrderWeight += order.Weight;

                    DateTime packEnd = order.Date_collected;
                    if (packEnd > startDelivery)
                        startDelivery = packEnd;
                }

                if (startDelivery < startTime) startDelivery = startTime;

                // 4. Готовим параметры для DeliveryCheck
                rc = 4;
                double[] betweenDistance = new double[orderCount + 2];
                DateTime[] deliveryTimeLimit = new DateTime[orderCount + 2];
                betweenDistance[1] = deliveryPath[0].DistanceFromShop;
                betweenDistance[orderCount + 1] = deliveryPath[orderCount - 1].DistanceFromShop;
                deliveryTimeLimit[orderCount] = deliveryPath[orderCount - 1].ShippingOrder.GetDeliveryLimit(Courier.DELIVERY_LIMIT);

                for (int i = 2; i <= orderCount; i++)
                {
                    betweenDistance[i] = pointDist[orderIndex[i - 2], orderIndex[i - 1]];
                    deliveryTimeLimit[i - 1] = deliveryPath[i - 2].ShippingOrder.GetDeliveryLimit(Courier.DELIVERY_LIMIT);
                }

                // 5. Проверяем путь
                rc = 5;
                int rc1 = courier.DeliveryCheck(startDelivery, betweenDistance, deliveryTimeLimit, totalOrderWeight, out pathInfo, out nodeDeliveryTime);
                if (rc1 != 0)
                    return rc = 1000 * rc + rc1;

                pathInfo.DeliveredOrders = deliveryPath;

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
        /// Построение всех возможных отгрузок для заданного курьера
        /// </summary>
        /// <param name="courier">Почасовой курьер (OnFoot, Bicycle, Car)</param>
        /// <param name="deliveryInfo">Доставляемые товары</param>
        /// <param name="intervalHist">Точки ИНД доставляемых товаров</param>
        /// <param name="permutationsRepository">Генератор перестановок</param>
        /// <param name="stepDeliveryInfo">Информации о построенных отгрузках</param>
        /// <param name="totalWorkTime">Общее оплачиваемое время</param>
        /// <param name="totalOrderCount">Общее число заказов во всех отгрузках</param>
        /// <param name="totalCost">Общее стоимость курьера для всех отгрузок</param>
        /// <returns>0 - отгрузки построены; иначе - отгрузки не построены</returns>
        private static int BuildHourlyDelivery(Courier courier, CourierDeliveryInfo[] deliveryInfo, int[,] intervalHist, ref Permutations permutationsRepository, 
            out CourierDeliveryInfo[] stepDeliveryInfo, 
            out double totalWorkTime,
            out int totalOrderCount,
            out double totalCost)
        {
            // 1. Инициализация
            int rc = 1;
            int rc1 = 1;
            stepDeliveryInfo = null;
            bool[] saveCompleted = null;
            totalWorkTime = 0;
            totalOrderCount = 0;
            totalCost = 0;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (courier == null ||
                    !(courier.CourierType.VechicleType == CourierVehicleType.OnFoot ||
                      courier.CourierType.VechicleType == CourierVehicleType.Bicycle ||
                      courier.CourierType.VechicleType == CourierVehicleType.Car))
                    return rc;
                if (deliveryInfo == null || deliveryInfo.Length <= 0)
                    return rc;
                if (intervalHist == null || intervalHist.Length <= 0)
                    return rc;
                if (permutationsRepository == null)
                    permutationsRepository = new Permutations();

                // 3. Сохраняем значение флага Completed
                rc = 3;
                saveCompleted = deliveryInfo.Select(p => p.Completed).ToArray();

                // 4. Цикл построения всех возможных отгрузок с помощью данного курьера 
                rc = 4;
                DateTime returnTime = DateTime.MinValue;
                CourierDeliveryInfo[] allCourierDeliveries = new CourierDeliveryInfo[deliveryInfo.Length];
                int deliveryCount = 0;

                while (true)
                {
                    // 5. Находим первый заказ доступный для доставки
                    rc = 5;
                    CourierDeliveryInfo nextOrder = deliveryInfo.FirstOrDefault(p => !p.Completed && p.EndDeliveryInterval >= returnTime);
                    if (nextOrder == null)
                        break;

                    // 6. Выбираем все заказы - кандидаты на отгрузку по времени
                    //     (имеющие пересекающиеся интервалы доставки)
                    rc = 6;
                    DateTime startIntervalTime = nextOrder.StartDeliveryInterval;
                    if (startIntervalTime < returnTime) startIntervalTime = returnTime;

                    int startHistIndex = (int)startIntervalTime.TimeOfDay.TotalMinutes;
                    int endHistIndex = (int)nextOrder.EndDeliveryInterval.TimeOfDay.TotalMinutes;

                    int maxCount = 1;
                    int maxIndex = -1;
                    CourierDeliveryInfo[] selectedOrders;

                    for (int i = startHistIndex; i <= endHistIndex; i++)
                    {
                        if (intervalHist[i, 0] > maxCount)
                        {
                            maxCount = intervalHist[i, 0];
                            maxIndex = i;
                        }
                    }

                    // Если их нет, то это изолированная точка
                    if (maxIndex == -1)
                    {
                        allCourierDeliveries[deliveryCount++] = nextOrder;
                        nextOrder.StartDelivery = startIntervalTime;
                        nextOrder.NodeDeliveryTime = new double[] { 5, nextOrder.DeliveryTime };
                        nextOrder.NodeDistance = new double[] { 0, nextOrder.DistanceFromShop };
                        returnTime = startIntervalTime.AddMinutes(nextOrder.ExecutionTime);
                        nextOrder.Completed = true;
                        continue;
                    }

                    // 7. Отбираем заказы подходящие по времени
                    rc = 7;
                    selectedOrders = new CourierDeliveryInfo[maxCount - 1];
                    int count = 0;

                    for (int i = 1; i <= maxCount; i++)
                    {
                        int orderIndex = intervalHist[maxIndex, i];
                        CourierDeliveryInfo dInfo = deliveryInfo[orderIndex];
                        if (!dInfo.Completed && dInfo != nextOrder && dInfo.EndDeliveryInterval > returnTime)
                        {
                            selectedOrders[count++] = deliveryInfo[orderIndex];
                        }
                    }

                    if (count <= 0)
                    {
                        allCourierDeliveries[deliveryCount++] = nextOrder;
                        nextOrder.StartDelivery = startIntervalTime;
                        nextOrder.NodeDeliveryTime = new double[] { 5, nextOrder.DeliveryTime };
                        nextOrder.NodeDistance = new double[] { 0, nextOrder.DistanceFromShop };
                        returnTime = startIntervalTime.AddMinutes(nextOrder.ExecutionTime);
                        nextOrder.Completed = true;
                        continue;
                    }

                    Array.Resize(ref selectedOrders, count);

                    // 8. Расчитываем расстояние от next-заказа до выбранных точек доставки
                    rc = 8;
                    double[] nextDist = new double[selectedOrders.Length];
                    double nextLatitude = nextOrder.ShippingOrder.Latitude;
                    double nextLongitude = nextOrder.ShippingOrder.Longitude;

                    for (int i = 0; i < selectedOrders.Length; i++)
                    {
                        Order order = selectedOrders[i].ShippingOrder;
                        nextDist[i] = DISTANCE_ALLOWANCE * Helper.Distance(nextLatitude, nextLongitude, order.Latitude, order.Longitude);
                    }

                    // 9. Фильтруем по расстоянию и возможности доставки
                    rc = 9;
                    DateTime[] deliveryTimeLimit = new DateTime[4];
                    deliveryTimeLimit[0] = DateTime.MaxValue;
                    deliveryTimeLimit[3] = DateTime.MaxValue;
                    double[] betweenDistance = new double[4];
                    count = 0;

                    for (int i = 0; i < selectedOrders.Length; i++)
                    {
                        CourierDeliveryInfo selOrder = selectedOrders[i];
                        DateTime modelTime = selOrder.ShippingOrder.Date_collected;
                        if (modelTime < nextOrder.ShippingOrder.Date_collected)
                            modelTime = nextOrder.ShippingOrder.Date_collected;
                        if (modelTime < returnTime) modelTime = returnTime;
                        double totalWeight = selOrder.ShippingOrder.Weight + nextOrder.ShippingOrder.Weight;

                        betweenDistance[1] = nextOrder.DistanceFromShop;
                        betweenDistance[2] = nextDist[i];
                        betweenDistance[3] = selOrder.DistanceFromShop;
                        deliveryTimeLimit[1] = nextOrder.ShippingOrder.GetDeliveryLimit(Courier.DELIVERY_LIMIT);
                        deliveryTimeLimit[2] = selOrder.ShippingOrder.GetDeliveryLimit(Courier.DELIVERY_LIMIT);

                        CourierDeliveryInfo twoOrdersDeliveryInfo;
                        double[] nodeDeliveryTime;
                        rc1 = courier.DeliveryCheck(modelTime, betweenDistance, deliveryTimeLimit, totalWeight, out twoOrdersDeliveryInfo, out nodeDeliveryTime);
                        if (rc1 != 0)
                        {
                            double temp = betweenDistance[1];
                            betweenDistance[1] = betweenDistance[2];
                            betweenDistance[2] = temp;

                            DateTime tempTime = deliveryTimeLimit[1];
                            deliveryTimeLimit[1] = deliveryTimeLimit[2];
                            deliveryTimeLimit[2] = tempTime;
                            double[] nodeDeliveryTimeX;
                            rc1 = courier.DeliveryCheck(modelTime, betweenDistance, deliveryTimeLimit, totalWeight, out twoOrdersDeliveryInfo, out nodeDeliveryTimeX);
                        }

                        if (rc1 == 0)
                        {
                            selectedOrders[count++] = selOrder;
                        }
                    }

                    if (count <= 0)
                    {
                        allCourierDeliveries[deliveryCount++] = nextOrder;
                        nextOrder.StartDelivery = startIntervalTime;
                        nextOrder.NodeDeliveryTime = new double[] { 5, nextOrder.DeliveryTime };
                        nextOrder.NodeDistance = new double[] { 0, nextOrder.DistanceFromShop };
                        returnTime = startIntervalTime.AddMinutes(nextOrder.ExecutionTime);
                        nextOrder.Completed = true;
                        continue;
                    }

                    Array.Resize(ref selectedOrders, count + 1);
                    selectedOrders[count++] = nextOrder;  // Исходный заказ последний
                    Array.Reverse(selectedOrders);        // Исходный заказ первый

                    // 10. Решаем задачу комивояжера - нужно построить путь обхода точек доставки с минимальной стоимостью.
                    //    Если невозможно вовремя доставить все заказы, то выбираем путь с максимальным числом точек доставки
                    //    и минимальной стоимостью. Любой путь должен включать next-точку (точку с индексом 0)
                    rc = 10;
                    CourierDeliveryInfo bestPathInfo = null;
                    if (count <= 8)
                    {
                        // 10.1 Решаем перебором всех вариантов
                        rc = 101;
                        int[,] permutations = permutationsRepository.GetPermutations(count);
                        rc1 = FindPathWithMinCostEx(courier, selectedOrders, permutations, returnTime, out bestPathInfo);
                        if (rc1 == 0 && bestPathInfo != null)
                        {
                            bestPathInfo.ShippingOrder = nextOrder.ShippingOrder;
                        }
                        else
                        {
                            nextOrder.Completed = true;
                            //    Order order = nextOrder.ShippingOrder;
                            //    DateTime assemblyEnd = order.Date_collected;
                            //    if (returnTime > assemblyEnd) assemblyEnd = returnTime;
                            //    DateTime timeLimit = order.GetDeliveryLimit(DELIVERY_LIMIT);
                            //    double distance = nextOrder.DistanceFromShop;
                            //    rc1 = courier.DeliveryCheck(assemblyEnd, distance, timeLimit, order.Weight, out bestPathInfo);
                            //    if (rc1 == 0)
                            //    {
                            //        bestPathInfo.ShippingOrder = nextOrder.ShippingOrder;
                            //        bestPathInfo.DeliveredOrders = new CourierDeliveryInfo[] { nextOrder };
                            //    }
                            //    else
                            //    {
                            //        bestPathInfo = new CourierDeliveryInfo(courier, assemblyEnd, 1, order.Weight);
                            //        bestPathInfo.Cost = nextOrder.Cost;
                            //        bestPathInfo.DeliveredOrders = new CourierDeliveryInfo[] { nextOrder };
                            //        bestPathInfo.DeliveryTime = nextOrder.DeliveryTime;
                            //        bestPathInfo.DistanceFromShop = nextOrder.DistanceFromShop;
                            //        bestPathInfo.EndDeliveryInterval = nextOrder.EndDeliveryInterval;
                            //        bestPathInfo.ExecutionTime = nextOrder.ExecutionTime;
                            //        bestPathInfo.ReserveTime = new TimeSpan(0);
                            //        bestPathInfo.ShippingOrder = order;
                            //        bestPathInfo.StartDelivery = assemblyEnd;
                            //        bestPathInfo.StartDeliveryInterval = nextOrder.StartDeliveryInterval;
                            //    }
                        }
                    }
                    else
                    {
                        // 10.2. Применим 2-opt алгоритм
                        rc = 102;
                        rc1 = FindPathWithMinCostBy2Opt(courier, selectedOrders, returnTime, out bestPathInfo);
                        if (rc1 == 0 && bestPathInfo != null)
                        {
                            bestPathInfo.ShippingOrder = nextOrder.ShippingOrder;
                        }
                        else
                        {
                            nextOrder.Completed = true;
                        }
                    }

                    if (bestPathInfo != null)
                    {
                        nextOrder.Completed = true;
                        foreach (CourierDeliveryInfo deiveryInfo in bestPathInfo.DeliveredOrders)
                        {
                            deiveryInfo.Completed = true;
                        }

                        //returnTime = bestPathInfo.CalculationTime.AddMinutes(bestPathInfo.ExecutionTime);
                        returnTime = bestPathInfo.StartDelivery.AddMinutes(bestPathInfo.ExecutionTime);
                        allCourierDeliveries[deliveryCount++] = bestPathInfo;
                    }
                }

                if (deliveryCount <= 0)
                    return rc;

                stepDeliveryInfo = allCourierDeliveries.Take(deliveryCount).ToArray();

                // 11. Считаем время выполнения всех заказов, их число и стоимость курьера
                rc = 11;
                totalOrderCount = stepDeliveryInfo.Sum(p => p.OrderCount);
                DateTime workStart = stepDeliveryInfo[0].StartDelivery;
                CourierDeliveryInfo lastDelivery = stepDeliveryInfo[stepDeliveryInfo.Length - 1];
                //DateTime workEnd = lastDelivery.StartDelivery.AddMinutes(lastDelivery.NodeDeliveryTime[lastDelivery.OrderCount] + courier.CourierType.HandInTime);
                DateTime workEnd = lastDelivery.StartDelivery.AddMinutes(lastDelivery.NodeDeliveryTime[lastDelivery.OrderCount]);
                courier.GetCourierDayCost(workStart, workEnd, totalOrderCount, out totalWorkTime, out totalCost);

                //totalWorkTime = (returnTime -  allCourierDeliveries[0].StartDeliveryInterval).TotalHours;
                //totalWorkTime = (returnTime -  allCourierDeliveries[0].StartDelivery).TotalHours;
                //if (totalWorkTime < Courier.MIN_WORK_TIME) totalWorkTime = Courier.MIN_WORK_TIME;
                //totalWorkTime = Math.Round(totalWorkTime + 0.5, 0);
                //totalCost = totalWorkTime * (1 + courier.CourierType.Insurance) * courier.CourierType.HourlyRate;

                // 12. Выход - Ok
                rc = 0;
                return rc;
            }
            catch
            {
                return rc;
            }
            finally
            {
                // Восстановление значеня флага Completed
                if (saveCompleted != null)
                {
                    for (int i = 0; i < saveCompleted.Length; i++)
                    {
                        deliveryInfo[i].Completed = saveCompleted[i];
                    }
                }
            }
        }

        /// <summary>
        /// Дневное решение для "реального" поведения без знания будущего
        /// для пяти видов курьеров (OnFoot, OnBicycle, OnCar, GettTaxi, YandexTaxi)
        /// </summary>
        /// <param name="shop">Магазин</param>
        /// <param name="shopOrders">История заказов за один день</param>
        /// <param name="notDeliveredOrders">Заказы, которые не удалось доставить</param>
        /// <returns>0 - решение найдено; иначе - решение не найдено</returns>
        private static int FindRealDailySolution(Shop shop, Order[] shopOrders, ref Permutations permutationsRepository, out CourierDeliveryInfo[] deliveryHistory, out Order[] notDeliveredOrders)
        {
            // 1. Инициализация
            int rc = 1;
            int rc1 = 1;
            deliveryHistory = null;
            notDeliveredOrders = null;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (shop == null)
                    return rc;
                if (shopOrders == null || shopOrders.Length <= 0)
                    return rc;
                if (permutationsRepository == null)
                    permutationsRepository = new Permutations();

                // 3. Создаём пять видов курьеров
                rc = 3;
                Courier gettCourier = new Courier(1, new CourierType_GettTaxi());
                gettCourier.Status = CourierStatus.Ready;
                gettCourier.WorkStart = new TimeSpan(0);
                gettCourier.WorkEnd = new TimeSpan(24, 0, 0);

                Courier yandexCourier = new Courier(2, new CourierType_YandexTaxi());
                yandexCourier.Status = CourierStatus.Ready;
                yandexCourier.WorkStart = new TimeSpan(0);
                yandexCourier.WorkEnd = new TimeSpan(24, 0, 0);

                Courier onFootCourier = new Courier(3, new CourierType_OnFoot());
                onFootCourier.Status = CourierStatus.Ready;
                onFootCourier.WorkStart = new TimeSpan(0);
                onFootCourier.WorkEnd = new TimeSpan(24, 0, 0);

                Courier onBicycleCourier = new Courier(4, new CourierType_Bicycle());
                onBicycleCourier.Status = CourierStatus.Ready;
                onBicycleCourier.WorkStart = new TimeSpan(0);
                onBicycleCourier.WorkEnd = new TimeSpan(24, 0, 0);

                Courier onCarCourier = new Courier(5, new CourierType_Car());
                onCarCourier.Status = CourierStatus.Ready;
                onCarCourier.WorkStart = new TimeSpan(0);
                onCarCourier.WorkEnd = new TimeSpan(24, 0, 0);

                Courier onFoot1Courier = new Courier(6, new CourierType_OnFoot1());
                onFoot1Courier.Status = CourierStatus.Ready;
                onFoot1Courier.WorkStart = new TimeSpan(0);
                onFoot1Courier.WorkEnd = new TimeSpan(24, 0, 0);

                Courier onBiCycle1Courier = new Courier(8, new CourierType_Bicycle1());
                onBiCycle1Courier.Status = CourierStatus.Ready;
                onBiCycle1Courier.WorkStart = new TimeSpan(0);
                onBiCycle1Courier.WorkEnd = new TimeSpan(24, 0, 0);

                Courier onCar11Courier = new Courier(8, new CourierType_Car1());
                onCar11Courier.Status = CourierStatus.Ready;
                onCar11Courier.WorkStart = new TimeSpan(0);
                onCar11Courier.WorkEnd = new TimeSpan(24, 0, 0);
                Courier[] allTypeCouriers = null;

                switch (shop.DeliveryTypes)    // C B F
                {
                    case DeliveryType.None:    //  + + + 
                        allTypeCouriers = new Courier[] { onFoot1Courier, onBiCycle1Courier, onCar11Courier };
                        break;
                    case DeliveryType.OnFoot:  //  - - + 
                        allTypeCouriers = new Courier[] { onFoot1Courier };
                        break;
                    case DeliveryType.Bicycle: //  - + - 
                        allTypeCouriers = new Courier[] { onBiCycle1Courier };
                        break;
                    case DeliveryType.Bicycle | DeliveryType.OnFoot: //  - + + 
                        allTypeCouriers = new Courier[] { onBiCycle1Courier, onFoot1Courier };
                        break;
                    case DeliveryType.Car: //  + - - 
                        allTypeCouriers = new Courier[] { onCar11Courier };
                        break;
                    case DeliveryType.Car | DeliveryType.OnFoot: //  + - + 
                        allTypeCouriers = new Courier[] { onCar11Courier, onFoot1Courier };
                        break;
                    case DeliveryType.Car | DeliveryType.Bicycle: //  + + - 
                        allTypeCouriers = new Courier[] { onCar11Courier, onBiCycle1Courier };
                        break;
                    default:
                        allTypeCouriers = new Courier[] { onCar11Courier, onBiCycle1Courier, onFoot1Courier };
                        break;

                }



                //Courier[] allTypeCouriers = new Courier[] { gettCourier, yandexCourier, onFootCourier, onBicycleCourier, onCarCourier };
                //Courier[] allTypeCouriers = new Courier[] { onFootCourier, onBicycleCourier, onCarCourier };

                // 4. Расчитываем расстояние от магазина до всех точек доставки
                rc = 4;
                double shopLatitude = shop.Latitude;
                double shopLongitude = shop.Longitude;
                double[] distanceFromShop = new double[shopOrders.Length];

                for (int i = 0; i < shopOrders.Length; i++)
                {
                    Order order = shopOrders[i];
                    order.Completed = false;
                    distanceFromShop[i] = DISTANCE_ALLOWANCE * Helper.Distance(shopLatitude, shopLongitude, order.Latitude, order.Longitude);
                }

                // 5. Расчитываем для каждого заказа данные для доставки всеми видами курьеров
                rc = 5;
                CourierDeliveryInfo[][] allTypeCouriersDeliveryInfo = new CourierDeliveryInfo[allTypeCouriers.Length][];
                CourierDeliveryInfo[] deliveryInfo = new CourierDeliveryInfo[shopOrders.Length];
                int count = 0;

                for (int i = 0; i < allTypeCouriers.Length; i++)
                {
                    Courier courier = allTypeCouriers[i];
                    count = 0;

                    for (int j = 0; j < shopOrders.Length; j++)
                    {
                        Order order = shopOrders[j];
                        DateTime assemblyEnd = order.Date_collected;

                        if (assemblyEnd != DateTime.MinValue)
                        {
                            DateTime deliveryTimeLimit = order.GetDeliveryLimit(Courier.DELIVERY_LIMIT);
                            CourierDeliveryInfo courierDeliveryInfo;
                            double distance = distanceFromShop[j];
                            rc1 = courier.DeliveryCheck(assemblyEnd, distance, deliveryTimeLimit, order.Weight, out courierDeliveryInfo);
                            if (rc1 == 0)
                            {
                                courierDeliveryInfo.ShippingOrder = order;
                                courierDeliveryInfo.DistanceFromShop = distance;
                                deliveryInfo[count++] = courierDeliveryInfo;
                            }
                            else
                            {
                                rc = rc;
                            }
                        }
                    }

                    if (count > 0)
                    {
                        CourierDeliveryInfo[] courierInfo = deliveryInfo.Take(count).ToArray();
                        Array.Sort(courierInfo, CompareByEndDeliveryInterval);
                        allTypeCouriersDeliveryInfo[i] = courierInfo;
                    }
                    else
                    {
                        allTypeCouriersDeliveryInfo[i] = new CourierDeliveryInfo[0];
                    }
                }

                // 6. Отмечаем точки интервалов начала доставки (ИНД) на временной оси для каждого типа курьеров
                rc = 6;
                int[][,] allTypeCouriersIntervalHist = new int[allTypeCouriers.Length][,];

                for (int i = 0; i < allTypeCouriers.Length; i++)
                {
                    deliveryInfo = allTypeCouriersDeliveryInfo[i];
                    int[,] intervalHist = new int[1440, ORDERS_PER_MINUTE_LIMIT];

                    for (int j = 0; j < deliveryInfo.Length; j++)
                    {
                        int startHistIndex = (int)deliveryInfo[j].StartDeliveryInterval.TimeOfDay.TotalMinutes;
                        int endHistIndex = (int)deliveryInfo[j].EndDeliveryInterval.TimeOfDay.TotalMinutes;

                        for (int k = startHistIndex; k <= endHistIndex; k++)
                        {
                            int intervalCount = intervalHist[k, 0];
                            if (intervalCount < ORDERS_PER_MINUTE_LIMIT)
                            {
                                intervalHist[k, 0] = ++intervalCount;
                                intervalHist[k, intervalCount] = j;
                            }
                        }
                    }

                    allTypeCouriersIntervalHist[i] = intervalHist;
                }

                // 7. Цикл поиска "реального" решения
                rc = 7;
                ShopCouriers couriersResource = new ShopCouriers();
                deliveryHistory = new CourierDeliveryInfo[shopOrders.Length];
                int deliveryHistoryCount = 0;
                int hourlyCourierId = 3;

                while (true)
                {
                    // 7.1 Для каждого типа курьера считаем отдельно стоимость следующего шага
                    rc = 71;
                    CourierDeliveryInfo bestDeliveryByTaxi = null;
                    CourierDeliveryInfo[] bestDeliveryByHourlyCourier = null;
                    double bestTotalWorkTime = 0;
                    int bestTotalOrders = 0;
                    double bestTotalCost = 0;

                    for (int k = 0; k < allTypeCouriers.Length; k++)
                    {
                        Courier courier = allTypeCouriers[k];
                        CourierDeliveryInfo deliveryByTaxi = null;
                        CourierDeliveryInfo[] deliveryByHourlyCourier = null;
                        double totalWorkTime;
                        int totalOrders;
                        double totalCost;

                        if (courier.CourierType.VechicleType == CourierVehicleType.GettTaxi ||
                            courier.CourierType.VechicleType == CourierVehicleType.YandexTaxi ||
                            courier.CourierType.VechicleType == CourierVehicleType.OnFoot1 ||
                            courier.CourierType.VechicleType == CourierVehicleType.Bicycle1 ||
                            courier.CourierType.VechicleType == CourierVehicleType.Car1)
                        {
                            // 7.2 Для такси
                            rc = 72;
                            rc1 = BuildTaxiDelivery(courier, allTypeCouriersDeliveryInfo[k], allTypeCouriersIntervalHist[k], ref permutationsRepository, out deliveryByTaxi);
                            if (rc1 == 0 && deliveryByTaxi != null)
                            {
                                if (bestDeliveryByTaxi == null)
                                {
                                    bestDeliveryByTaxi = deliveryByTaxi;
                                }
                                else if (deliveryByTaxi.OrderCost < bestDeliveryByTaxi.OrderCost)
                                {
                                    bestDeliveryByTaxi = deliveryByTaxi;
                                }
                            }
                            else
                            {
                                if (rc1 != 3)
                                {
                                    rc = rc;
                                }
                            }
                        }
                        else
                        {
                            // 7.3 Для почасовых курьеров
                            rc = 73;
                            rc1 = BuildHourlyDelivery(courier, allTypeCouriersDeliveryInfo[k], allTypeCouriersIntervalHist[k], ref permutationsRepository,
                                out deliveryByHourlyCourier,
                                out totalWorkTime,
                                out totalOrders,
                                out totalCost);

                            if (rc1 == 0 && deliveryByHourlyCourier != null)
                            {
                                if (bestDeliveryByHourlyCourier == null && totalOrders > 0)
                                {
                                    bestDeliveryByHourlyCourier = deliveryByHourlyCourier;
                                    bestTotalWorkTime = totalWorkTime;
                                    bestTotalOrders = totalOrders;
                                    bestTotalCost = totalCost;
                                }
                                else if (totalOrders > 0 && (totalCost / totalOrders) < (bestTotalCost / bestTotalOrders))
                                {
                                    bestDeliveryByHourlyCourier = deliveryByHourlyCourier;
                                    bestTotalWorkTime = totalWorkTime;
                                    bestTotalOrders = totalOrders;
                                    bestTotalCost = totalCost;
                                }
                            }
                        }
                    }

                    // 7.4 Выбираем один результат и делаем отгрузку
                    rc = 74;
                    int caseNo = 0;
                    if (bestDeliveryByTaxi != null)
                        caseNo += 2;
                    if (bestDeliveryByHourlyCourier != null)
                        caseNo++;
                    Courier newCourier;
                    switch (caseNo)  // Taxi Hourly
                    {
                        case 0:      //   -    -   (Невозможно сделать шаг вперед)
                            goto ExitWhile;
                        case 1:      //   -    +   (Только почасовой курьер)
                            newCourier = bestDeliveryByHourlyCourier[0].DeliveryCourier.Clone(hourlyCourierId++);
                            newCourier.TotalDeliveryTime = bestTotalWorkTime;
                            newCourier.TotalCost = bestTotalCost;

                            foreach (CourierDeliveryInfo dlvInfo in bestDeliveryByHourlyCourier)
                            {
                                dlvInfo.DeliveryCourier = newCourier;
                                deliveryHistory[deliveryHistoryCount++] = dlvInfo;
                                dlvInfo.ShippingOrder.Completed = true;

                                if (dlvInfo.DeliveredOrders != null)
                                {
                                    foreach (CourierDeliveryInfo dlvOrder in dlvInfo.DeliveredOrders)
                                    {
                                        dlvOrder.Completed = true;
                                    }
                                }
                            }

                            break;
                        case 2:      //   +    -   (Только такси)
                            deliveryHistory[deliveryHistoryCount++] = bestDeliveryByTaxi;
                            bestDeliveryByTaxi.ShippingOrder.Completed = true;

                            if (bestDeliveryByTaxi.DeliveredOrders != null)
                            {
                                foreach (CourierDeliveryInfo dlvOrder in bestDeliveryByTaxi.DeliveredOrders)
                                {
                                    dlvOrder.Completed = true;
                                }
                            }
                            break;
                        case 3:      //   +    +   (Такси + почасовой курьер)
                            if ((bestTotalCost / bestTotalOrders) <= ORDER_COST_THRESHOLD)
                            {
                                newCourier = bestDeliveryByHourlyCourier[0].DeliveryCourier.Clone(hourlyCourierId++);
                                newCourier.TotalDeliveryTime = bestTotalWorkTime;
                                newCourier.TotalCost = bestTotalCost;

                                foreach (CourierDeliveryInfo dlvInfo in bestDeliveryByHourlyCourier)
                                {
                                    dlvInfo.ShippingOrder.Completed = true;
                                    dlvInfo.DeliveryCourier = newCourier;
                                    deliveryHistory[deliveryHistoryCount++] = dlvInfo;
                                    if (dlvInfo.DeliveredOrders != null)
                                    {
                                        foreach (CourierDeliveryInfo dlvOrder in dlvInfo.DeliveredOrders)
                                        {
                                            dlvOrder.Completed = true;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                deliveryHistory[deliveryHistoryCount++] = bestDeliveryByTaxi;
                                bestDeliveryByTaxi.ShippingOrder.Completed = true;
                                if (bestDeliveryByTaxi.DeliveredOrders != null)
                                {
                                    foreach (CourierDeliveryInfo dlvOrder in bestDeliveryByTaxi.DeliveredOrders)
                                    {
                                        dlvOrder.Completed = true;
                                    }
                                }
                            }
                            break;
                    }
                }

                ExitWhile:

                // 8. Извлекаем неотгруженные заказы (например те, которые не могут быть доставлены в срок)
                rc = 8;
                notDeliveredOrders = shopOrders.Where(p => !p.Completed).ToArray();
                if (notDeliveredOrders != null && notDeliveredOrders.Length > 0)
                {
                    rc = rc;
                }

                // 9. Подрезаем History
                rc = 9;
                Array.Resize(ref deliveryHistory, deliveryHistoryCount);

                // 10. Выход - Ok
                rc = 0;
                return rc;
            }
            catch
            {
                return rc;
            }
        }

        /// <summary>
        /// Дневное решение для "реального" поведения со знанием будущего
        /// для четырех видов курьеров (OnBicycle, OnCar, GettTaxi, YandexTaxi)
        /// </summary>
        /// <param name="shop">Магазин</param>
        /// <param name="shopOrders">История заказов за один день</param>
        /// <param name="notDeliveredOrders">Заказы, которые не удалось доставить</param>
        /// <returns>0 - решение найдено; иначе - решение не найдено</returns>
        private static int FindRealDailySolutionEx(Shop shop, Order[] shopOrders, ref Permutations permutationsRepository, out CourierDeliveryInfo[] deliveryHistory, out Order[] notDeliveredOrders)
        {
            // 1. Инициализация
            int rc = 1;
            int rc1 = 1;
            deliveryHistory = null;
            notDeliveredOrders = null;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (shop == null)
                    return rc;
                if (shopOrders == null || shopOrders.Length <= 0)
                    return rc;
                if (permutationsRepository == null)
                    permutationsRepository = new Permutations();

                // 3. Создаём пять видов курьеров
                rc = 3;
                Courier gettCourier = new Courier(1, new CourierType_GettTaxi());
                gettCourier.Status = CourierStatus.Ready;
                gettCourier.WorkStart = new TimeSpan(0);
                gettCourier.WorkEnd = new TimeSpan(24, 0, 0);

                Courier yandexCourier = new Courier(2, new CourierType_YandexTaxi());
                yandexCourier.Status = CourierStatus.Ready;
                yandexCourier.WorkStart = new TimeSpan(0);
                yandexCourier.WorkEnd = new TimeSpan(24, 0, 0);

                Courier onCarCourier = new Courier(3, new CourierType_Car());
                onCarCourier.Status = CourierStatus.Ready;
                onCarCourier.WorkStart = new TimeSpan(0);
                onCarCourier.WorkEnd = new TimeSpan(24, 0, 0);

                Courier onBicycleCourier = new Courier(4, new CourierType_Bicycle());
                onBicycleCourier.Status = CourierStatus.Ready;
                onBicycleCourier.WorkStart = new TimeSpan(0);
                onBicycleCourier.WorkEnd = new TimeSpan(24, 0, 0);

                List<Courier> enabledCouriers = new List<Courier>(4);
                enabledCouriers.Add(gettCourier);
                enabledCouriers.Add(yandexCourier);

                if (shop.DeliveryTypes == DeliveryType.None)
                {
                    enabledCouriers.Add(onBicycleCourier);
                    enabledCouriers.Add(onCarCourier);
                }
                else
                {
                    if ((shop.DeliveryTypes & DeliveryType.Car) != 0)
                        enabledCouriers.Add(onCarCourier);
                    if ((shop.DeliveryTypes & DeliveryType.OnFoot) != 0 || (shop.DeliveryTypes & DeliveryType.Bicycle) != 0)
                        enabledCouriers.Add(onBicycleCourier);

                }

                Courier[] allTypeCouriers = enabledCouriers.ToArray();

                // 4. Расчитываем расстояние от магазина до всех точек доставки
                rc = 4;
                double shopLatitude = shop.Latitude;
                double shopLongitude = shop.Longitude;
                double[] distanceFromShop = new double[shopOrders.Length];

                for (int i = 0; i < shopOrders.Length; i++)
                {
                    Order order = shopOrders[i];
                    order.Completed = false;
                    distanceFromShop[i] = DISTANCE_ALLOWANCE * Helper.Distance(shopLatitude, shopLongitude, order.Latitude, order.Longitude);
                }

                // 5. Расчитываем для каждого заказа данные для доставки всеми видами курьеров
                rc = 5;
                CourierDeliveryInfo[][] allTypeCouriersDeliveryInfo = new CourierDeliveryInfo[allTypeCouriers.Length][];
                CourierDeliveryInfo[] deliveryInfo = new CourierDeliveryInfo[shopOrders.Length];
                int count = 0;

                for (int i = 0; i < allTypeCouriers.Length; i++)
                {
                    Courier courier = allTypeCouriers[i];
                    count = 0;

                    for (int j = 0; j < shopOrders.Length; j++)
                    {
                        Order order = shopOrders[j];
                        DateTime assemblyEnd = order.Date_collected;

                        if (assemblyEnd != DateTime.MinValue)
                        {
                            DateTime deliveryTimeLimit = order.GetDeliveryLimit(Courier.DELIVERY_LIMIT);
                            CourierDeliveryInfo courierDeliveryInfo;
                            double distance = distanceFromShop[j];
                            rc1 = courier.DeliveryCheck(assemblyEnd, distance, deliveryTimeLimit, order.Weight, out courierDeliveryInfo);
                            if (rc1 == 0)
                            {
                                courierDeliveryInfo.ShippingOrder = order;
                                courierDeliveryInfo.DistanceFromShop = distance;
                                deliveryInfo[count++] = courierDeliveryInfo;
                            }
                            else
                            {
                                rc = rc;
                            }
                        }
                    }

                    if (count > 0)
                    {
                        CourierDeliveryInfo[] courierInfo = deliveryInfo.Take(count).ToArray();
                        Array.Sort(courierInfo, CompareByEndDeliveryInterval);
                        allTypeCouriersDeliveryInfo[i] = courierInfo;
                    }
                    else
                    {
                        allTypeCouriersDeliveryInfo[i] = new CourierDeliveryInfo[0];
                    }
                }

                // 6. Отмечаем точки интервалов начала доставки (ИНД) на временной оси для каждого типа курьеров
                rc = 6;
                int[][,] allTypeCouriersIntervalHist = new int[allTypeCouriers.Length][,];

                for (int i = 0; i < allTypeCouriers.Length; i++)
                {
                    deliveryInfo = allTypeCouriersDeliveryInfo[i];
                    int[,] intervalHist = new int[1440, ORDERS_PER_MINUTE_LIMIT];

                    for (int j = 0; j < deliveryInfo.Length; j++)
                    {
                        int startHistIndex = (int)deliveryInfo[j].StartDeliveryInterval.TimeOfDay.TotalMinutes;
                        int endHistIndex = (int)deliveryInfo[j].EndDeliveryInterval.TimeOfDay.TotalMinutes;

                        for (int k = startHistIndex; k <= endHistIndex; k++)
                        {
                            int intervalCount = intervalHist[k, 0];
                            if (intervalCount < ORDERS_PER_MINUTE_LIMIT)
                            {
                                intervalHist[k, 0] = ++intervalCount;
                                intervalHist[k, intervalCount] = j;
                            }
                        }
                    }

                    allTypeCouriersIntervalHist[i] = intervalHist;
                }

                // 7. Цикл поиска "реального" решения
                rc = 7;
                ShopCouriers couriersResource = new ShopCouriers();
                deliveryHistory = new CourierDeliveryInfo[shopOrders.Length];
                int deliveryHistoryCount = 0;
                int hourlyCourierId = 3;
                Order[] notDelivered = new Order[shopOrders.Length];
                int notDeliveredCount = 0;

                while (true)
                {
                    // 7.1 Для каждого типа курьера считаем отдельно стоимость следующего шага
                    rc = 71;
                    CourierDeliveryInfo bestDeliveryByTaxi = null;
                    CourierDeliveryInfo[] bestDeliveryByHourlyCourier = null;
                    double bestTotalWorkTime = 0;
                    int bestTotalOrders = 0;
                    double bestTotalCost = 0;

                    for (int k = 0; k < allTypeCouriers.Length; k++)
                    {
                        Courier courier = allTypeCouriers[k];
                        CourierDeliveryInfo deliveryByTaxi = null;
                        CourierDeliveryInfo[] deliveryByHourlyCourier = null;
                        double totalWorkTime;
                        int totalOrders;
                        double totalCost;

                        if (courier.CourierType.VechicleType == CourierVehicleType.Car ||
                            courier.CourierType.VechicleType == CourierVehicleType.Bicycle ||
                            courier.CourierType.VechicleType == CourierVehicleType.OnFoot)
                        {
                            // 7.2 Для почасовых курьеров
                            rc = 72;
                            rc1 = BuildHourlyDelivery(courier, allTypeCouriersDeliveryInfo[k], allTypeCouriersIntervalHist[k], ref permutationsRepository,
                                out deliveryByHourlyCourier,
                                out totalWorkTime,
                                out totalOrders,
                                out totalCost);

                            if (rc1 == 0 && deliveryByHourlyCourier != null)
                            {
                                CourierDeliveryInfo[] checkedDeliveries;
                                DateTime checkedWorkStart;
                                DateTime checkedWorkEnd;
                                double checkedWorkTime;
                                double checkedDayCost;
                                int checkedDayOrderCount;

                                rc1 = CheckDayCourierDeliveries(courier, deliveryByHourlyCourier, 
                                    out checkedDeliveries, out checkedWorkStart, out checkedWorkEnd, 
                                    out checkedWorkTime, out checkedDayCost, out checkedDayOrderCount);

                                if (rc1 == 0 && checkedDeliveries != null)
                                {
                                    if (bestDeliveryByHourlyCourier == null && checkedDayOrderCount > 0)
                                    {
                                        bestDeliveryByHourlyCourier = checkedDeliveries;
                                        bestTotalWorkTime = checkedWorkTime;
                                        bestTotalOrders = checkedDayOrderCount;
                                        bestTotalCost = checkedDayCost;
                                    }
                                    else if (checkedDayOrderCount > 0 && (checkedDayCost / checkedDayOrderCount) < (bestTotalCost / bestTotalOrders))
                                    {
                                        bestDeliveryByHourlyCourier = checkedDeliveries;
                                        bestTotalWorkTime = checkedWorkTime;
                                        bestTotalOrders = checkedDayOrderCount;
                                        bestTotalCost = checkedDayCost;
                                    }
                                }
                            }
                        }
                        else
                        {
                            // 7.3 Для такси
                            rc = 73;
                            rc1 = BuildTaxiDelivery(courier, allTypeCouriersDeliveryInfo[k], allTypeCouriersIntervalHist[k], ref permutationsRepository, out deliveryByTaxi);
                            if (rc1 == 0 && deliveryByTaxi != null)
                            {
                                if (bestDeliveryByTaxi == null)
                                {
                                    bestDeliveryByTaxi = deliveryByTaxi;
                                }
                                else if (deliveryByTaxi.OrderCost < bestDeliveryByTaxi.OrderCost)
                                {
                                    bestDeliveryByTaxi = deliveryByTaxi;
                                }
                            }
                            else
                            {
                                if (rc1 != 3)
                                {
                                    rc = rc;
                                }
                            }
                        }
                    }

                    // 7.4 Выбираем один результат и делаем отгрузку
                    rc = 74;
                    int caseNo = 0;
                    if (bestDeliveryByTaxi != null) caseNo += 2;
                    if (bestDeliveryByHourlyCourier != null) caseNo++;
                    Courier newCourier;

                    switch (caseNo)  // Taxi Hourly
                    {
                        case 0:      //   -    -   (Невозможно сделать шаг вперед)
                            List<Order> list = new List<Order>(allTypeCouriers.Length);

                            for (int m = 0; m < allTypeCouriers.Length; m++)
                            {
                                CourierDeliveryInfo dInfo = allTypeCouriersDeliveryInfo[m].FirstOrDefault(p => !p.Completed);
                                if (dInfo != null)
                                {
                                    if (list.IndexOf(dInfo.ShippingOrder) < 0)
                                    {
                                        list.Add(dInfo.ShippingOrder);
                                        notDelivered[notDeliveredCount++] = dInfo.ShippingOrder;
                                        dInfo.Completed = true;
                                    }
                                }
                            }
                            if (list.Count <= 0)
                                goto ExitWhile;
                            break;
                        case 1:      //   -    +   (Только почасовой курьер)
                            newCourier = bestDeliveryByHourlyCourier[0].DeliveryCourier.Clone(hourlyCourierId++);
                            newCourier.TotalDeliveryTime = bestTotalWorkTime;
                            newCourier.TotalCost = bestTotalCost;

                            foreach (CourierDeliveryInfo dlvInfo in bestDeliveryByHourlyCourier)
                            {
                                dlvInfo.DeliveryCourier = newCourier;
                                deliveryHistory[deliveryHistoryCount++] = dlvInfo;
                                dlvInfo.ShippingOrder.Completed = true;

                                if (dlvInfo.DeliveredOrders != null)
                                {
                                    foreach (CourierDeliveryInfo dlvOrder in dlvInfo.DeliveredOrders)
                                    {
                                        dlvOrder.Completed = true;
                                    }
                                }
                            }

                            break;
                        case 2:      //   +    -   (Только такси)
                            deliveryHistory[deliveryHistoryCount++] = bestDeliveryByTaxi;
                            bestDeliveryByTaxi.ShippingOrder.Completed = true;

                            if (bestDeliveryByTaxi.DeliveredOrders != null)
                            {
                                foreach (CourierDeliveryInfo dlvOrder in bestDeliveryByTaxi.DeliveredOrders)
                                {
                                    dlvOrder.Completed = true;
                                }
                            }
                            break;
                        case 3:      //   +    +   (Такси + почасовой курьер)
                            double hourlyOrderCost = bestTotalCost / bestTotalOrders;
                            if (hourlyOrderCost <= ORDER_COST_THRESHOLD || hourlyOrderCost <= bestDeliveryByTaxi.OrderCost)
                            {
                                newCourier = bestDeliveryByHourlyCourier[0].DeliveryCourier.Clone(hourlyCourierId++);
                                newCourier.TotalDeliveryTime = bestTotalWorkTime;
                                newCourier.TotalCost = bestTotalCost;

                                foreach (CourierDeliveryInfo dlvInfo in bestDeliveryByHourlyCourier)
                                {
                                    dlvInfo.ShippingOrder.Completed = true;
                                    dlvInfo.DeliveryCourier = newCourier;
                                    deliveryHistory[deliveryHistoryCount++] = dlvInfo;
                                    if (dlvInfo.DeliveredOrders != null)
                                    {
                                        foreach (CourierDeliveryInfo dlvOrder in dlvInfo.DeliveredOrders)
                                        {
                                            dlvOrder.Completed = true;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                deliveryHistory[deliveryHistoryCount++] = bestDeliveryByTaxi;
                                bestDeliveryByTaxi.ShippingOrder.Completed = true;
                                if (bestDeliveryByTaxi.DeliveredOrders != null)
                                {
                                    foreach (CourierDeliveryInfo dlvOrder in bestDeliveryByTaxi.DeliveredOrders)
                                    {
                                        dlvOrder.Completed = true;
                                    }
                                }
                            }
                            break;
                    }
                }

                ExitWhile:

                // 8. Извлекаем неотгруженные заказы (например те, которые не могут быть доставлены в срок)
                rc = 8;
                notDeliveredOrders = shopOrders.Where(p => !p.Completed).ToArray();
                if (notDeliveredCount > 0)
                  notDeliveredOrders = notDeliveredOrders.Concat(notDelivered.Take(notDeliveredCount)).ToArray();

                // 9. Обрабатываем неотгруженные заказы
                rc = 9;
                for (int i = 0; i < notDeliveredOrders.Length; i++)
                {
                    Order order = notDeliveredOrders[i];

                    // Пропускаем заказы без даты заказа и даты сборки
                    if (order.Date_collected == DateTime.MinValue ||
                        order.Date_order == DateTime.MinValue)
                        continue;

                    double distance = Helper.Distance(shopLatitude, shopLongitude, order.Latitude, order.Longitude);

                    CourierDeliveryInfo cdi1;
                    CourierDeliveryInfo cdi2;
                    int r1 = gettCourier.DeliveryCheck(order.Date_collected, distance, order.Date_collected.AddHours(1), order.Weight, out cdi1);
                    int r2 = yandexCourier.DeliveryCheck(order.Date_collected, distance, order.Date_collected.AddHours(1), order.Weight, out cdi2);

                    if (r1 == 0 && r2 == 0)
                    {
                        if (cdi1.Cost <= cdi2.Cost)
                        {
                            cdi1.ShippingOrder = order;
                            cdi1.DistanceFromShop = distance;
                            cdi1.DeliveredOrders = new CourierDeliveryInfo[] { cdi1};
                            deliveryHistory[deliveryHistoryCount++] = cdi1;
                        }
                        else
                        {
                            cdi2.ShippingOrder = order;
                            cdi2.DistanceFromShop = distance;
                            cdi2.DeliveredOrders = new CourierDeliveryInfo[] { cdi2};
                            deliveryHistory[deliveryHistoryCount++] = cdi2;
                        }
                        order.Completed = true;
                    }
                    else if (r1 == 0 && r2 != 0)
                    {
                        cdi1.ShippingOrder = order;
                        cdi1.DistanceFromShop = distance;
                        cdi1.DeliveredOrders = new CourierDeliveryInfo[] { cdi1};
                        deliveryHistory[deliveryHistoryCount++] = cdi1;
                        order.Completed = true;
                    }
                    else if (r1 != 0 && r2 == 0)
                    {
                        cdi2.ShippingOrder = order;
                        cdi2.DistanceFromShop = distance;
                        cdi1.DeliveredOrders = new CourierDeliveryInfo[] { cdi2};
                        deliveryHistory[deliveryHistoryCount++] = cdi2;
                        order.Completed = true;
                    }
                }

                // 10. Подрезаем History
                rc = 10;
                Array.Resize(ref deliveryHistory, deliveryHistoryCount);

                // 11. Выход - Ok
                rc = 0;
                return rc;
            }
            catch
            {
                return rc;
            }
        }
        //rc1 = BuildHourlyDelivery(courier, allTypeCouriersDeliveryInfo[k], allTypeCouriersIntervalHist[k], ref permutationsRepository,

        /// <summary>
        /// Выбор из последовательности отгрузок наиболее
        /// подходящей части, начинающейся с первой отгрузки
        /// </summary>
        /// <param name="courier">Курьер</param>
        /// <param name="courierDeliveries">Упорядоченная во времени последовательность отгрузок</param>
        /// <param name="checkedDeliveries">Отобранная часть</param>
        /// <param name="checkedWorkStart">Время начала работы для отобранной части</param>
        /// <param name="checkedWorkEnd">Время завершения работы для отобранной части</param>
        /// <param name="checkedWorkTime">Число оплачиваемых часов</param>
        /// <param name="checkedDayCost">Общая стоимость отгрузки отобранной части</param>
        /// <param name="checkDayOrderCount">Общие число заказов в отобранной части</param>
        /// <returns>0 - проверка и отбор выполнен; иначе - проверка не выполнена</returns>
        private static int CheckDayCourierDeliveries(Courier courier, CourierDeliveryInfo[] courierDeliveries,
            out CourierDeliveryInfo[] checkedDeliveries, 
            out DateTime checkedWorkStart,
            out DateTime checkedWorkEnd,
            out double checkedWorkTime, 
            out double checkedDayCost, 
            out int checkDayOrderCount)
        {
            // 1. Инициализация
            int rc = 1;
            checkedDeliveries = null;
            checkedWorkStart = default(DateTime);
            checkedWorkEnd = default(DateTime);
            checkedWorkTime = 0;
            checkedDayCost = 0;
            checkDayOrderCount = 0;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (courier == null)
                    return rc;
                if (courierDeliveries == null || courierDeliveries.Length <= 0)
                    return rc;

                // 3. Отбираем все отгрузки за первые четыре часа и далее минимум стоимости
                rc = 3;
                DateTime workStart = courierDeliveries[0].StartDelivery;
                DateTime requiredIntervalEnd = workStart.AddHours(Courier.MIN_WORK_TIME);
                int count = 0;
                int totalOrderCount = 0;
                CourierDeliveryInfo lastDelivery = null;

                for (int i = 0; i < courierDeliveries.Length; i++)
                {
                    CourierDeliveryInfo deliveryInfo = courierDeliveries[i];
                    if (deliveryInfo.StartDelivery > requiredIntervalEnd)
                        break;
                    count++;
                    lastDelivery = deliveryInfo;
                    totalOrderCount += deliveryInfo.OrderCount;
                }

                // 4. Инициализируем цикл поиска минимума по цене отгрузки
                rc = 4;
                double handInTime = courier.CourierType.HandInTime;
                DateTime bestWorkStart = workStart;
                
                DateTime bestWorkEnd = lastDelivery.StartDelivery.AddMinutes(lastDelivery.NodeDeliveryTime[lastDelivery.OrderCount] + handInTime);
                int bestDeliveryCount = count;
                double bestWorkTime;
                double bestTotalCost;
                int bestOrderCount = totalOrderCount;

                courier.GetCourierDayCost(bestWorkStart, bestWorkEnd, bestOrderCount, out bestWorkTime, out bestTotalCost);
                double bestOrderCost = bestTotalCost / bestOrderCount;

                // 5. Цикл отбора отгрузок
                rc = 5;

                for (int i = count; i < courierDeliveries.Length; i++)
                {
                    CourierDeliveryInfo deliveryInfo = courierDeliveries[i];
                    int orderCount = deliveryInfo.OrderCount;
                    totalOrderCount += orderCount;
                    DateTime workEnd = deliveryInfo.StartDelivery.AddMinutes(deliveryInfo.NodeDeliveryTime[orderCount] + handInTime);
                    double workTime;
                    double totalCost;
                    if (courier.GetCourierDayCost(workStart, workEnd, orderCount, out workTime, out totalCost))
                    {
                        double orderCost = totalCost / totalOrderCount;
                        if (orderCost <= ORDER_COST_THRESHOLD ||
                            orderCost <= bestOrderCost)
                        {
                            bestWorkEnd = workEnd;
                            bestDeliveryCount = i + 1;
                            bestWorkTime = workTime;
                            bestTotalCost = totalCost;
                            bestOrderCount = totalOrderCount;
                            bestOrderCost = orderCost;
                        }
                    }
                }

                // 6. Формируем результат
                rc = 6;
                if (bestDeliveryCount == courierDeliveries.Length)
                {
                    checkedDeliveries = courierDeliveries;
                }
                else
                {
                    checkedDeliveries = courierDeliveries.Take(bestDeliveryCount).ToArray();
                }

                checkedWorkStart = bestWorkStart;
                checkedWorkEnd = bestWorkEnd;
                checkedWorkTime = bestWorkTime;
                checkedDayCost = bestTotalCost;
                checkDayOrderCount = bestOrderCount;

                // 7. Выход - Ok
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
