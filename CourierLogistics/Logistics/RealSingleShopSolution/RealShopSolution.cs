
namespace CourierLogistics.Logistics.RealSingleShopSolution
{
    using CourierLogistics.Logistics.OptimalSingleShopSolution;
    using CourierLogistics.Logistics.OptimalSingleShopSolution.PermutationsRepository;
    using CourierLogistics.Logistics.RealSingleShopSolution.PartitionOfASet;
    using CourierLogistics.Report;
    using CourierLogistics.SourceData.Couriers;
    using CourierLogistics.SourceData.Orders;
    using CourierLogistics.SourceData.Shops;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Реальное решение для курьров, привязанных к магазину
    /// </summary>
    public class RealShopSolution
    {
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
        /// (внутренний параметр)
        /// </summary>
        public const int ORDERS_PER_MINUTE_LIMIT = 50;

        /// <summary>
        /// Желаемая стоимость доставки одного заказа
        /// </summary>
        public const double ORDER_COST_THRESHOLD = 95;

        /// <summary>
        /// Время для принятия решения о числе курьеров
        /// и времени их прибытия
        /// </summary>
        public const double MORNING_TIME_DECISION = 10;

        /// <summary>
        /// Максимальное значение переменной цикла
        /// при построении разбиений множества для разного
        /// количества элемеентов (в диапазоне 0 - 8)
        /// </summary>
        private static int[] PARTITION_LOOP_MAX_VALUE = new int[] { 0x0, 0x1, 0x3, 0x7, 0xF, 0x1F, 0x3F, 0x7F, 0xFF };

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
        /// Статистика прошедшего периода
        /// </summary>
        public ShopStatistics Statistics { get; private set; }

        /// <summary>
        /// Генератор перестановок
        /// </summary>
        private Permutations permutationsRepository;

        /// <summary>
        /// Параметрический конструктор класса RealShopSolution
        /// </summary>
        /// <param name="statistics">Статистика</param>
        /// <param name="report">Excel-отчет</param>
        public RealShopSolution(ShopStatistics statistics, ExcelReport report)
        {
            Statistics = statistics;
            if (statistics == null || !statistics.IsLoaded)
            {
                throw new ArgumentException("Статистика не загружена", "statistics");
            }

            Report = report;
            if (report == null)
            {
                throw new ArgumentNullException("Отчет не определен", "report");
            }

            permutationsRepository = new Permutations();


        }

        public int Create(Shop shop, Order[] shopOrders)
        {
            // 1. Инициализация
            int rc = 1;
            int rc1 = 1;
            IsCreated = false;
            LastException = null;
            SingleShop = shop;
            ShopOrders = shopOrders;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (shop == null)
                    return rc;
                if (shopOrders == null || shopOrders.Length <= 0)
                    return rc;

                // 3. Сортируем заказы по времени поступления
                rc = 3;
                Array.Sort(shopOrders, CompareByCollectedDate);

                // 4. Цикл обработки по дням
                rc = 4;
                DateTime currentDay = shopOrders[0].Date_collected.Date;
                Order order;
                int dayStartIndex = 0;
                Dictionary<string, ShopCourierStatistics> courierSummary = new Dictionary<string, ShopCourierStatistics>(5000);

                Order[] dayOrders;
                CourierDeliveryInfo[] deliveryHistory;
                Order[] undeliveredOrders;
                int length;
                int shopNo = shop.N;
                int undeliveredCount = 0;

                for (int i = 0; i < shopOrders.Length; i++)
                {
                    order = shopOrders[i];
                    order.Completed = false;
                    if (order.Date_collected.Date != currentDay)
                    {
                        length = i - dayStartIndex;
                        dayOrders = new Order[length];
                        Array.Copy(shopOrders, dayStartIndex, dayOrders, 0, length);
                        //if (currentDay.Day == 1)
                        //{
                        //    rc1 = rc1;
                        //}
                        rc1 = FindOneDaySolution(shop, dayOrders, Statistics, permutationsRepository, out deliveryHistory, out undeliveredOrders);
                        if (rc1 == 0)
                        {
                            PrintDeliveryHistory(Report, ExcelReport.HISTORY_PAGE, shopNo, deliveryHistory);
                            AddToCourierSummary(courierSummary, shopNo, deliveryHistory);
                            if (undeliveredOrders != null)
                                undeliveredCount += undeliveredOrders.Length;
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

                rc1 = FindOneDaySolution(shop, dayOrders, Statistics, permutationsRepository, out deliveryHistory, out undeliveredOrders);
                if (rc1 == 0)
                {
                    PrintDeliveryHistory(Report, ExcelReport.HISTORY_PAGE, shopNo, deliveryHistory);
                    AddToCourierSummary(courierSummary, shopNo, deliveryHistory);
                    if (undeliveredOrders != null)
                        undeliveredCount += undeliveredOrders.Length;
                }

                // 5. Печатаем сводную статистику
                rc = 5;
                PrintCourierSummary(Report, ExcelReport.SUMMARY_PAGE, courierSummary.Values.ToArray());
                int nn = shopOrders.Count(p => p.Completed);
                Console.WriteLine($"Shop {shopNo}. Orders = {shopOrders.Length}, undelivered = {undeliveredCount}, delivered = {nn - undeliveredCount} rejected ={shopOrders.Length - nn + undeliveredCount}");
                //Order[] orders = shopOrders.Where(p => !p.Completed).ToArray();

                // 6. Выход - Ok
                rc = 0;
                return rc;
            }
            catch (Exception ex)
            {
                LastException = ex;
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

                foreach (CourierDeliveryInfo deliveryInfo in deliveryHistory)
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

                    int rc1 = report.PrintHistoryRowEx(historyName, deliveryInfo);

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
                    int rc1 = report.PrintSummaryRowEx(summaryName, courierStat);

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
        /// Модель реального поведения при доставке
        /// из одного магазина за один день с
        /// привязкой курьров к магазину
        /// </summary>
        /// <param name="shop">Магазин</param>
        /// <param name="shopOrders">Все дневные заказы</param>
        /// <param name="statistics">Собранная статистика</param>
        /// <param name="permutationsRepository">Перестановки</param>
        /// <param name="deliveryHistory">История построенных отгрузок</param>
        /// <param name="undeliveredOrders">Заказы, которые не удалось доставить</param>
        /// <returns>0 - модель отработала успешно; иначе - модель не отработала</returns>
        private static int FindOneDaySolution(Shop shop, Order[] shopOrders, ShopStatistics statistics, Permutations permutationsRepository, out CourierDeliveryInfo[] deliveryHistory, 
            out Order[] undeliveredOrders)
        {
            // 1. Инициализация
            int rc = 1;
            int rc1 = 1;
            deliveryHistory = null;
            undeliveredOrders = null;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (shop == null)
                    return rc;
                if (shopOrders == null || shopOrders.Length <= 0)
                    return rc;
                if (permutationsRepository == null)
                    return rc;

                // 3. Запрашиваем курьров на текущий день
                rc = 3;
                DateTime shopDay = shopOrders[0].Date_collected.Date;
                ShopCouriers couriersRepository;
                rc1 = GetDayCouriersOfShop(shop, shopDay, statistics, out couriersRepository);
                if (rc1 != 0)
                    return rc = 100 * rc + rc1;

                // 4. Добавляем такси, если их нет
                rc = 4;
                if (couriersRepository.Couriers.Values.FirstOrDefault(courier => courier.CourierType.VechicleType == CourierVehicleType.GettTaxi) == null)
                {
                    Courier gettCourier = new Courier(1, new CourierType_GettTaxi());
                    gettCourier.Status = CourierStatus.Ready;
                    gettCourier.WorkStart = new TimeSpan(0);
                    gettCourier.WorkEnd = new TimeSpan(24, 0, 0);
                    couriersRepository.AddCourier(gettCourier);
                }

                if (couriersRepository.Couriers.Values.FirstOrDefault(courier => courier.CourierType.VechicleType == CourierVehicleType.YandexTaxi) == null)
                {
                    Courier yandexCourier = new Courier(2, new CourierType_YandexTaxi());
                    yandexCourier.Status = CourierStatus.Ready;
                    yandexCourier.WorkStart = new TimeSpan(0);
                    yandexCourier.WorkEnd = new TimeSpan(24, 0, 0);
                    couriersRepository.AddCourier(yandexCourier);
                }

                // 5. Создаём общий перечень курьеров и маску для такси + маску для прочих курьеров, привязанных к магазину
                rc = 5;
                Courier[] allTypeCouriers = couriersRepository.Couriers.Values.ToArray();
                List<Courier> taxiList = new List<Courier>(allTypeCouriers.Length);
                List<Courier> courierList = new List<Courier>(allTypeCouriers.Length);

                for (int i = 0; i < allTypeCouriers.Length; i++)
                {
                    Courier courier = allTypeCouriers[i];
                    courier.Index = i;

                    if (courier.CourierType.VechicleType == CourierVehicleType.GettTaxi ||
                        courier.CourierType.VechicleType == CourierVehicleType.YandexTaxi)
                    {
                        taxiList.Add(courier);
                    }
                    else
                    {
                        courierList.Add(courier);
                    }
                }

                //if (courierList.Count <= 0)
                //    return rc;

                Courier[] allTaxi = taxiList.ToArray();
                Courier[] allCouriers = courierList.ToArray();
                taxiList = null;
                courierList = null;

                // 6. Расчитываем расстояние от магазина до всех точек доставки
                rc = 6;
                double shopLatitude = shop.Latitude;
                double shopLongitude = shop.Longitude;
                double[] distanceFromShop = new double[shopOrders.Length];

                for (int i = 0; i < shopOrders.Length; i++)
                {
                    Order order = shopOrders[i];
                    //if (order.Id_order == 2977845)
                    //{
                    //    rc = rc;
                    //}
                    order.Completed = false;
                    distanceFromShop[i] = DISTANCE_ALLOWANCE * Helper.Distance(shopLatitude, shopLongitude, order.Latitude, order.Longitude);
                }

                // 7. Расчитываем для каждого заказа данные для доставки всеми видами курьеров
                rc = 7;
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
                        //if (order.Id_order == 2977845)
                        //{
                        //    rc = rc;
                        //}

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

                // 8. Цикл поиска "реального" решения
                rc = 8;
                deliveryHistory = new CourierDeliveryInfo[shopOrders.Length];
                int deliveryHistoryCount = 0;
                CourierDeliveryInfo[][] taxiDeliveryInfo = new CourierDeliveryInfo[allTaxi.Length][];
                Order[] undelivered = new Order[shopOrders.Length];
                int undeliveredCount = 0;
                DateTime timeStamp = shopOrders[0].Date_collected.Date.AddMinutes(1439.7);
                DateTime te = shopOrders.Max(p => p.DeliveryTimeLimit);
                DateTime timeEnd = te;

                if (allCouriers.Length > 0)
                {
                    timeStamp = allCouriers.Min(p => p.LastDeliveryEnd);
                    timeEnd = timeStamp.Date.Add(allCouriers.Max(p => p.WorkEnd));
                    if (te < timeEnd) timeEnd = te;
                }

                Courier[] solutionCouriers;
                CourierDeliveryInfo[][] solutionDeliveryInfo;

                //while (timeStamp <= timeEnd && (shopOrders.FirstOrDefault(p => !p.Completed) != null))
                //while (timeStamp <= timeEnd && !AreAllOrdersShipped(allTypeCouriersDeliveryInfo))
                while (shopOrders.FirstOrDefault(p => !p.Completed) != null)
                {
                    // ????????????????????????????????????????
                    // 8.1 Отгружаем все заказы, которые могут быть выполнены только такси
                    rc = 81;
                    // случай без курьеров выпадает
                    //int[] expiredDeliveryTimeOrderId = GetOrdersWithExpiredDeliveryTime(allTypeCouriers, allTypeCouriersDeliveryInfo);
                    int[] expiredDeliveryTimeOrderId = GetShopCourierUndeliveredOrders(allTypeCouriers, allTypeCouriersDeliveryInfo);
                    if (expiredDeliveryTimeOrderId != null && expiredDeliveryTimeOrderId.Length > 0)
                    {
                        // 8.1.1 Отбираем заказы, которые могут быть отгружены такси
                        rc = 811;
                        Array.Sort(expiredDeliveryTimeOrderId);

                        for (int i = 0; i < allTaxi.Length; i++)
                        {
                            Courier taxi = allTaxi[i];
                            int taxiIndex = taxi.Index;
                            taxiDeliveryInfo[i] = SelectDeliveryInfoByOrderId(allTypeCouriersDeliveryInfo[taxi.Index], expiredDeliveryTimeOrderId);
                        }

                        // 8.1.2 Цикл отгрузки отобранных заказов
                        rc = 812;
                        rc1 = 0;

                        while (rc1 == 0)
                        {
                            CourierDeliveryInfo taxiDelivery;
                            rc1 = FindLocalSolutionAtTime(DateTime.MinValue, allTaxi, taxiDeliveryInfo, permutationsRepository, out taxiDelivery);
                            if (rc1 == 0 && taxiDelivery != null)
                            {
                                deliveryHistory[deliveryHistoryCount++] = taxiDelivery;
                                taxiDelivery.ShippingOrder.Completed = true;

                                if (taxiDelivery.DeliveredOrders != null)
                                {
                                    foreach (CourierDeliveryInfo dlvOrder in taxiDelivery.DeliveredOrders)
                                    {
                                        dlvOrder.Completed = true;
                                    }
                                }
                            }
                        }

                        // 8.1.3 Сохраняем заказы, которые не могут быть отгружены (доставлены в срок)
                        rc = 813;
                        for (int i = 0; i < allTaxi.Length; i++)
                        {
                            if (taxiDeliveryInfo[i] != null)
                            {
                                foreach (CourierDeliveryInfo orderInfo in taxiDeliveryInfo[i])
                                {
                                    if (!orderInfo.Completed)
                                    {
                                        undelivered[undeliveredCount++] = orderInfo.ShippingOrder;
                                        orderInfo.Completed = true;
                                    }
                                }
                            }
                        }
                    }

                    if (timeStamp > timeEnd)
                        break;

                    // события, определяющие следующее включение:
                    //  1. сборка нового заказа
                    //  2. истечение ИНД
                    //  3. прибытие курьера
                    //  4. убытие курьера
                    //  5. предела ожидания курьера
                    //  6. резервом времени отгрузки
                    //  7. завершение работы курьера
                    //
                    //  решение об отгрузке курьером:
                    //  1. наличия отгрузки
                    //  2. частоты поступления заказов на данном интервале
                    //  3. средняя стоимость доставки товара в отгрузке
                    //  4. резерв времени

                    // 8.2 Выбираем всех курьров, находящихся в магазине в данный момент времени
                    rc = 82;
                    solutionCouriers = allCouriers.Where(c => c.LastDeliveryEnd <= timeStamp).ToArray();
                    if (solutionCouriers.Length <= 0)
                    {
                        timeStamp = allCouriers.Min(p => p.LastDeliveryEnd);
                        continue;
                    }


                    Array.Sort(solutionCouriers, CompareByLastDeliveryEnd);

                    // 8.3 Выбираем все заказы, которые собраны и не отгружены
                    // на данный момент
                    rc = 83;
                    solutionDeliveryInfo = new CourierDeliveryInfo[solutionCouriers.Length][];

                    for (int i = 0; i < solutionCouriers.Length; i++)
                    {
                        Courier courier = solutionCouriers[i];

                        solutionDeliveryInfo[i] = SelectAvailableOrders(timeStamp, allTypeCouriersDeliveryInfo[courier.Index]);
                    }


                    //CourierDeliveryInfo di = solutionDeliveryInfo[0].FirstOrDefault(p => p.ShippingOrder.Id_order == 2556601);
                    //if (di != null)
                    //{
                    //    rc = rc;
                    //}

                    //if (solutionDeliveryInfo.Length == 1 && solutionDeliveryInfo[0].Length == 1 && 
                    //    solutionDeliveryInfo[0][0].ShippingOrder.Id_order == 2721547)
                    //{
                    //    rc = rc;
                    //}

                    // +++++++++++++++++
                    // когда нет заказов - особый случай !
                    // +++++++++++++++++

                    // 8.4 Находим хорошую отгрузку
                    rc = 84;
                    bool isShipping = false;
                    CourierDeliveryInfo courierDelivery;
                    //rc1 = FindLocalSolutionAtTime(DateTime.MinValue, solutionCouriers, solutionDeliveryInfo, permutationsRepository, out courierDelivery);
                    rc1 = FindLocalSolutionAtTime(timeStamp, solutionCouriers, solutionDeliveryInfo, permutationsRepository, out courierDelivery);
                    if (rc1 == 0 && courierDelivery != null)
                    {
                        if (courierDelivery.StartDelivery < timeStamp)
                        {
                            rc = rc;
                        }

                        // 8.5 Условие выполнения отгрузки
                        rc = 85;
                        DateTime limitTime = courierDelivery.StartDelivery.Add(courierDelivery.ReserveTime);
                        DateTime t = timeStamp.Date.Add(courierDelivery.DeliveryCourier.WorkEnd).AddMinutes(-courierDelivery.DeliveryTime);
                        double dt = (t - timeStamp).TotalMinutes;
                        bool isReady = (limitTime <= timeStamp || (limitTime - timeStamp).TotalMinutes <= 5);
                        if (dt <= 5) isReady = true;

                        if (isReady ||
                            courierDelivery.OrderCost <= courierDelivery.DeliveryCourier.AverageOrderCost)
                        {
                            isShipping = true;
                        }
                    }

                    if (isShipping)
                    {
                        // 8.6 Отгрузка...
                        rc = 86;
                        deliveryHistory[deliveryHistoryCount++] = courierDelivery;
                        courierDelivery.ShippingOrder.Completed = true;

                        if (courierDelivery.DeliveredOrders != null)
                        {
                            foreach (CourierDeliveryInfo dlvOrder in courierDelivery.DeliveredOrders)
                            {
                                dlvOrder.Completed = true;
                            }
                        }

                        courierDelivery.DeliveryCourier.LastDeliveryStart = courierDelivery.StartDelivery;
                        courierDelivery.DeliveryCourier.LastDeliveryEnd = courierDelivery.StartDelivery.AddMinutes(courierDelivery.ExecutionTime);
                        DateTime newTimeStamp = allCouriers.Min(p => p.LastDeliveryEnd);
                        if (newTimeStamp > timeStamp)
                        {
                            timeStamp = newTimeStamp;
                        }
                        else
                        {
                            timeStamp = timeStamp.AddSeconds(5);
                            rc = rc;
                        }
                    }
                    else
                    {
                        // 8.7 Время следующей сборки
                        rc = 87;
                        DateTime nextCollectedTime = DateTime.MaxValue;
                        for (int i = 0; i < shopOrders.Length; i++)
                        {
                            DateTime collectedTime = shopOrders[i].Date_collected;
                            if (collectedTime > timeStamp && collectedTime < nextCollectedTime)
                            {
                                nextCollectedTime = collectedTime;
                            }
                        }

                        // 8.8 Истечение ИНД
                        rc = 88;
                        DateTime endDeliveryInterval = DateTime.MaxValue;

                        for (int i = 0; i < allTypeCouriersDeliveryInfo.Length; i++)
                        {
                            CourierDeliveryInfo[] deliverInfo = allTypeCouriersDeliveryInfo[i];
                            if (deliverInfo != null && deliverInfo.Length > 0)
                            {
                                foreach(CourierDeliveryInfo orderInfo in deliverInfo)
                                {
                                    if (!orderInfo.Completed)
                                    {
                                        if (orderInfo.EndDeliveryInterval > timeStamp && orderInfo.EndDeliveryInterval < endDeliveryInterval)
                                        {
                                            endDeliveryInterval = orderInfo.EndDeliveryInterval;
                                        }
                                    }
                                }
                            }
                        }

                        // 8.9 Прибытие нового курьера
                        rc = 89;
                        DateTime courierReturnTime = DateTime.MaxValue;
                        for (int i = 0; i < allCouriers.Length; i++)
                        {
                            DateTime lastDeliveryEnd = allCouriers[i].LastDeliveryEnd;
                            if (lastDeliveryEnd > timeStamp && lastDeliveryEnd < courierReturnTime)
                            {
                                courierReturnTime = lastDeliveryEnd;
                            }
                        }

                        // 8.10 Педедел ожидания
                        rc = 810;
                        DateTime waitLimit = DateTime.MaxValue;

                        // 8.11 Время резерва для отгрузки
                        rc = 811;
                        DateTime reservedTime = DateTime.MaxValue;
                        DateTime endWork  = DateTime.MaxValue;
                        if (rc1 == 0 && courierDelivery != null)
                        {
                            reservedTime = courierDelivery.StartDelivery.Add(courierDelivery.ReserveTime);
                            endWork = timeStamp.Date.Add(courierDelivery.DeliveryCourier.WorkEnd).AddMinutes(-courierDelivery.DeliveryTime);
                        }

                        // 8.12 Устанавливаем следующее время анализа
                        rc = 812;
                        DateTime newTimeStamp = nextCollectedTime;
                        if (endDeliveryInterval < newTimeStamp) newTimeStamp = endDeliveryInterval;
                        if (courierReturnTime < newTimeStamp) newTimeStamp = courierReturnTime;
                        if (reservedTime < newTimeStamp) newTimeStamp = reservedTime;
                        if (endWork < newTimeStamp) newTimeStamp = endWork;
                        if (newTimeStamp != DateTime.MaxValue)
                        {
                            timeStamp = newTimeStamp;
                        }
                        else
                        {
                            timeStamp = timeStamp.AddMinutes(5);
                        }
                    }
                }

                // 9. Подрезаем неиспользованные хвосты массивов
                rc = 9;
                if (undeliveredCount < undelivered.Length)
                {
                    Array.Resize(ref undelivered, undeliveredCount);
                }

                undeliveredOrders = undelivered;

                if (deliveryHistoryCount < deliveryHistory.Length)
                {
                    Array.Resize(ref deliveryHistory, deliveryHistoryCount);
                }

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
        ///// Поиск оптимального решения по заданным товарам
        ///// и курьерам в заданный момент времени
        ///// </summary>
        ///// <param name = "timeStamp" >Временная отметка</param>
        ///// <param name = "solutionCouriers" > Доступные курьеры для отгрузки заказов</param>
        ///// <param name = "solutionDeliveryInfo" > Доступные для отгрузки заказы</param>
        ///// <param name = "solutioIntervalHist" > Отметки ИНД на временной оси для solutionDeliveryInfo</param>
        ///// <param name = "permutationsRepository" > Перестановки </ param >
        ///// < param name= "deliveryHistory" > Построенные отгрузки</param>
        ///// <param name = "undeliveredOrders" > Заказы, которые не могут быть доставлены в срок</param>
        ///// <returns>0 - решение построено; иначе - решение не построено</returns>
        //private static int BuildLocalSolution(DateTime timeStamp,
        //              Courier[] solutionCouriers, CourierDeliveryInfo[][] solutionDeliveryInfo, int[][,] solutioIntervalHist,
        //              Permutations permutationsRepository, Partitions partitionsRepository,
        //              out CourierDeliveryInfo delivery)
        //{
        //    // 1. Инициализация
        //    int rc = 1;
        //    int rc1 = 1;
        //    delivery = null;

        //    try
        //    {
        //        // 2. Проверяем исходные данные
        //        rc = 2;
        //        if (solutionCouriers == null || solutionCouriers.Length <= 0)
        //            return rc;
        //        if (solutionDeliveryInfo == null || solutionDeliveryInfo.Length != solutionCouriers.Length)
        //            return rc;
        //        if (solutioIntervalHist == null || solutioIntervalHist.Length != solutionCouriers.Length)
        //            return rc;
        //        if (permutationsRepository == null)
        //            return rc;
        //        if (partitionsRepository == null)
        //            return rc;


        //        // 3. Выбираем все заказы, которые могут быть выполнены всеми курьрами
        //        rc = 3;
        //        int size = solutionDeliveryInfo.Sum(p => p.Length);
        //        Dictionary<int, DateTime> allOrders = new Dictionary<int, DateTime>(size);

        //        for (int i = 0; i < solutionDeliveryInfo.Length; i++)
        //        {
        //            CourierDeliveryInfo[] courierDeliveries = solutionDeliveryInfo[i];
        //            foreach (CourierDeliveryInfo delInfo in courierDeliveries)
        //            {

        //            }
        //        }



        //        // 3. Цикл построения решения
        //        rc = 3;
        //        //int size = solutionDeliveryInfo.Max(p => p.Length);
        //        //deliveryHistory = new CourierDeliveryInfo[size];
        //        //int deliveryHistoryCount = 0;

        //        while (true)
        //        {
        //            // 3.1 Для каждого типа курьера считаем отдельно стоимость следующего шага
        //            rc = 31;
        //            CourierDeliveryInfo bestDeliveryByTaxi = null;
        //            CourierDeliveryInfo bestDeliveryByHourlyCourier = null;
        //            double bestTotalWorkTime = 0;
        //            int bestTotalOrders = 0;
        //            double bestTotalCost = 0;

        //            for (int k = 0; k < solutionCouriers.Length; k++)
        //            {
        //                Courier courier = solutionCouriers[k];
        //                CourierDeliveryInfo deliveryByTaxi = null;
        //                CourierDeliveryInfo deliveryByHourlyCourier = null;
        //                double totalWorkTime;
        //                int totalOrders;
        //                double totalCost;

        //                if (courier.CourierType.VechicleType == CourierVehicleType.GettTaxi ||
        //                    courier.CourierType.VechicleType == CourierVehicleType.YandexTaxi)
        //                {
        //                    // 3.2 Для такси
        //                    rc = 32;

        //                    rc1 = SingleShopSolution.BuildTaxiDelivery(courier, solutionDeliveryInfo[k], solutioIntervalHist[k], ref permutationsRepository, out deliveryByTaxi);
        //                    if (rc1 == 0 && deliveryByTaxi != null)
        //                    {
        //                        if (bestDeliveryByTaxi == null)
        //                        {
        //                            bestDeliveryByTaxi = deliveryByTaxi;
        //                        }
        //                        else if (deliveryByTaxi.OrderCost < bestDeliveryByTaxi.OrderCost)
        //                        {
        //                            bestDeliveryByTaxi = deliveryByTaxi;
        //                        }
        //                    }
        //                }
        //                else
        //                {
        //                    // 3.3 Для курьера магазина
        //                    rc = 33;

        //                    rc1 = BuildCourierDelivery(courier, timeStamp, solutionDeliveryInfo[k], solutioIntervalHist[k], permutationsRepository, out deliveryByHourlyCourier);
        //                    if (rc1 == 0 && deliveryByHourlyCourier != null)
        //                    {
        //                        if (bestDeliveryByHourlyCourier == null)
        //                        {
        //                            bestDeliveryByHourlyCourier = deliveryByHourlyCourier;
        //                        }
        //                        else if (deliveryByHourlyCourier.OrderCost < bestDeliveryByHourlyCourier.OrderCost)
        //                        {
        //                            bestDeliveryByHourlyCourier = deliveryByHourlyCourier;
        //                        }
        //                    }
        //                }
        //            }

        //            // 3.4 Выбираем и проверяем наилучшую отгрузку
        //            rc = 34;
        //            int caseNo = 0;
        //            if (bestDeliveryByTaxi != null)
        //                caseNo += 2;
        //            if (bestDeliveryByHourlyCourier != null)
        //                caseNo++;
        //            switch (caseNo)  // Taxi Hourly
        //            {
        //                case 0:      //   -    -   (Невозможно отгрузить ни одтн из заказов)
        //                    return rc;
        //                case 1:      //   -    +   (Только почасовой курьер)





        //                    foreach (CourierDeliveryInfo dlvInfo in bestDeliveryByHourlyCourier)
        //                    {
        //                        dlvInfo.DeliveryCourier = newCourier;
        //                        deliveryHistory[deliveryHistoryCount++] = dlvInfo;
        //                        dlvInfo.ShippingOrder.Completed = true;

        //                        if (dlvInfo.DeliveredOrders != null)
        //                        {
        //                            foreach (CourierDeliveryInfo dlvOrder in dlvInfo.DeliveredOrders)
        //                            {
        //                                dlvOrder.Completed = true;
        //                            }
        //                        }
        //                    }

        //                    break;
        //                case 2:      //   +    -   (Только такси)
        //                    deliveryHistory[deliveryHistoryCount++] = bestDeliveryByTaxi;
        //                    bestDeliveryByTaxi.ShippingOrder.Completed = true;

        //                    if (bestDeliveryByTaxi.DeliveredOrders != null)
        //                    {
        //                        foreach (CourierDeliveryInfo dlvOrder in bestDeliveryByTaxi.DeliveredOrders)
        //                        {
        //                            dlvOrder.Completed = true;
        //                        }
        //                    }
        //                    break;
        //                case 3:      //   +    +   (Такси + почасовой курьер)
        //                    double hourlyOrderCost = bestTotalCost / bestTotalOrders;
        //                    if (hourlyOrderCost <= ORDER_COST_THRESHOLD || hourlyOrderCost <= bestDeliveryByTaxi.OrderCost)
        //                    {
        //                        newCourier = bestDeliveryByHourlyCourier[0].DeliveryCourier.Clone(hourlyCourierId++);
        //                        newCourier.TotalDeliveryTime = bestTotalWorkTime;
        //                        newCourier.TotalCost = bestTotalCost;

        //                        foreach (CourierDeliveryInfo dlvInfo in bestDeliveryByHourlyCourier)
        //                        {
        //                            dlvInfo.ShippingOrder.Completed = true;
        //                            dlvInfo.DeliveryCourier = newCourier;
        //                            deliveryHistory[deliveryHistoryCount++] = dlvInfo;
        //                            if (dlvInfo.DeliveredOrders != null)
        //                            {
        //                                foreach (CourierDeliveryInfo dlvOrder in dlvInfo.DeliveredOrders)
        //                                {
        //                                    dlvOrder.Completed = true;
        //                                }
        //                            }
        //                        }
        //                    }
        //                    else
        //                    {
        //                        deliveryHistory[deliveryHistoryCount++] = bestDeliveryByTaxi;
        //                        bestDeliveryByTaxi.ShippingOrder.Completed = true;
        //                        if (bestDeliveryByTaxi.DeliveredOrders != null)
        //                        {
        //                            foreach (CourierDeliveryInfo dlvOrder in bestDeliveryByTaxi.DeliveredOrders)
        //                            {
        //                                dlvOrder.Completed = true;
        //                            }
        //                        }
        //                    }
        //                    break;
        //            }


        //        }


        //    }
        //    catch
        //    {
        //        return rc;
        //    }
        //}


        /// <summary>
        /// Поиск оптимального отгрузки в заданный момент времени
        /// </summary>
        /// <param name = "timeStamp" >Временная отметка</param>
        /// <param name = "solutionCouriers">Доступные курьеры для отгрузки заказов</param>
        /// <param name = "solutionDeliveryInfo">Доступные для отгрузки заказы</param>
        /// <param name = "permutationsRepository">Перестановки</param >
        /// <returns>0 - решение построено; иначе - решение не построено</returns>
        private static int FindLocalSolutionAtTime(DateTime timeStamp,
                      Courier[] solutionCouriers, CourierDeliveryInfo[][] solutionDeliveryInfo,
                      Permutations permutationsRepository, 
                      out CourierDeliveryInfo delivery)
        {
            // 1. Инициализация
            int rc = 1;
            int rc1 = 1;
            delivery = null;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (solutionCouriers == null || solutionCouriers.Length <= 0)
                    return rc;
                if (solutionDeliveryInfo == null || solutionDeliveryInfo.Length != solutionCouriers.Length)
                    return rc;
                if (permutationsRepository == null)
                    return rc;

                // 3.  Выбор наилучшей отгрузки отдельно для такси и курьеров магазина
                rc = 3;
                CourierDeliveryInfo bestTaxiDelivery = null;
                CourierDeliveryInfo bestCourierDelivery = null;
                CourierDeliveryInfo[] selectedDeliveryInfo = new CourierDeliveryInfo[8];

                for (int i = 0; i < solutionCouriers.Length; i++)
                {
                    // 3.1 Извлекаем заказы, которые могут быть отгружены курьером
                    rc = 31;
                    Courier courier = solutionCouriers[i];
                    bool isTaxi = (courier.CourierType.VechicleType == CourierVehicleType.GettTaxi || courier.CourierType.VechicleType == CourierVehicleType.YandexTaxi);
                    CourierDeliveryInfo[] courierOrders = solutionDeliveryInfo[i].Where(p => !p.Completed).ToArray();

                    if (courierOrders != null && courierOrders.Length > 0)
                    {
                        // 3.2 Строим все комбинации из заказов, проверяем возможность их доставки в одной отгрузке и отбираем наилучшую отгрузку
                        // отдельно для такси и курьров магазина
                        rc = 32;
                        if (courierOrders.Length > 8) courierOrders = courierOrders.Take(8).ToArray();
                        int orderCount = courierOrders.Length;
                        
                        int maxValue = PARTITION_LOOP_MAX_VALUE[orderCount];
                        CourierDeliveryInfo salesmanSolution;
                        int selectedCount = 0;

                        for (int j = 1; j <= maxValue; j++)
                        {
                            // 3.3 Генерация очередного подмножества из заказов
                            rc = 33;
                            selectedCount = 0;

                            if ((j & 0x1) != 0) selectedDeliveryInfo[selectedCount++] = courierOrders[0];
                            if (orderCount > 1)
                            {
                                if ((j & 0x2) != 0) selectedDeliveryInfo[selectedCount++] = courierOrders[1];
                                if (orderCount > 2)
                                {
                                    if ((j & 0x4) != 0) selectedDeliveryInfo[selectedCount++] = courierOrders[2];
                                    if (orderCount > 3)
                                    {
                                        if ((j & 0x8) != 0) selectedDeliveryInfo[selectedCount++] = courierOrders[3];
                                        if (orderCount > 4)
                                        {
                                            if ((j & 0x10) != 0) selectedDeliveryInfo[selectedCount++] = courierOrders[4];
                                            if (orderCount > 5)
                                            {
                                                if ((j & 0x20) != 0) selectedDeliveryInfo[selectedCount++] = courierOrders[5];
                                                if (orderCount > 6)
                                                {
                                                    if ((j & 0x40) != 0) selectedDeliveryInfo[selectedCount++] = courierOrders[6];
                                                    if (orderCount > 7)
                                                    {
                                                        if ((j & 0x80) != 0) selectedDeliveryInfo[selectedCount++] = courierOrders[7];
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }

                            // 3.4 Проверка возможности совместной отгрузки подмножества заказов
                            rc = 34;
                            CourierDeliveryInfo[] testOrders = selectedDeliveryInfo.Take(selectedCount).ToArray();
                            int[,] permutations = permutationsRepository.GetPermutations(selectedCount);
                            if (timeStamp != DateTime.MinValue)
                            {
                                rc1 = FindSalesmanSolution(courier, testOrders, permutations, timeStamp, out salesmanSolution);
                            }
                            else
                            {
                                DateTime startTime = testOrders.Max(p => p.ShippingOrder.Date_collected);
                                rc1 = FindSalesmanSolution(courier, testOrders, permutations, startTime, out salesmanSolution);
                            }

                            // 3.5. Отбор наилучших отрузок отдельно для такси и курьеров магазина
                            rc = 35;
                            if (rc1 == 0 && salesmanSolution != null)
                            {
                                salesmanSolution.ShippingOrder = testOrders[0].ShippingOrder;

                                if (isTaxi)
                                {
                                    if (bestTaxiDelivery == null)
                                    {
                                        bestTaxiDelivery = salesmanSolution;
                                    }
                                    else if (salesmanSolution.OrderCost < bestTaxiDelivery.OrderCost)
                                    {
                                        bestTaxiDelivery = salesmanSolution;
                                    }
                                }
                                else
                                {
                                    if (bestCourierDelivery == null)
                                    {
                                        bestCourierDelivery = salesmanSolution;
                                    }
                                    else if (salesmanSolution.OrderCount > bestCourierDelivery.OrderCount)
                                    {
                                        bestCourierDelivery = salesmanSolution;
                                    }
                                    else if (salesmanSolution.OrderCount == bestCourierDelivery.OrderCount &&
                                        salesmanSolution.Cost < bestCourierDelivery.Cost)
                                    {
                                        bestCourierDelivery = salesmanSolution;
                                    }
                                }
                            }
                        }
                    }
                }

                // 4. Устанавливаем результат поиска
                rc = 4;
                delivery = bestCourierDelivery;
                if (delivery == null) delivery = bestTaxiDelivery;
                if (delivery == null) return rc;

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
        /// Выборка заказов, которые не могут быть доставлены в срок
        /// при помощи курьров, приписанных к магазину
        /// </summary>
        /// <param name="allTypesCouriers">Курьры магазина</param>
        /// <param name="allTypeCouriersDeliveryInfo">Расчетная информация о доставке</param>
        /// <returns>Id заказов или null</returns>
        private static int[] GetOrdersWithExpiredDeliveryTime(Courier[] allTypesCouriers, CourierDeliveryInfo[][] allTypeCouriersDeliveryInfo)
        {
            // 1. Инициализация

            try
            {
                // 2. Проверяем исходные данные
                if (allTypesCouriers == null || allTypesCouriers.Length <= 0)
                    return null;
                if (allTypeCouriersDeliveryInfo == null ||
                    allTypeCouriersDeliveryInfo.Length != allTypesCouriers.Length)
                    return null;

                // 3. Отбираем те заказы, которые не могут быть выполнены
                //    ни одним из курьров магазина (авто и/или вело) в срок

                // (orderId, count)
                Dictionary<int, int> expiredTimeOrders = new Dictionary<int, int>(32);
                int courierCount = 0;

                for (int i = 0; i < allTypesCouriers.Length; i++)
                {
                    Courier courier = allTypesCouriers[i];
                    if (courier.CourierType.VechicleType != CourierVehicleType.GettTaxi && 
                        courier.CourierType.VechicleType != CourierVehicleType.YandexTaxi)
                    {
                        CourierDeliveryInfo[] deliveryInfo = allTypeCouriersDeliveryInfo[i];
                        DateTime returnTime = courier.LastDeliveryEnd;
                        courierCount++;

                        for (int j = 0; j < deliveryInfo.Length; j++)
                        {
                            // 3.1 Если заказ может быть отгружен курьером
                            CourierDeliveryInfo deliveryOrder = deliveryInfo[j];
                            if (deliveryOrder.EndDeliveryInterval >= returnTime)
                                break;

                            // 3.2 Если заказ уже отгружен
                            if (deliveryOrder.Completed)
                                continue;

                            // 3.3 Увеличиваем счетчик у заказа, который не может быть отгружен в срок
                            int orderId = deliveryOrder.ShippingOrder.Id_order;
                            int count;

                            if (expiredTimeOrders.TryGetValue(orderId, out count))
                            {
                                expiredTimeOrders[orderId] = ++count;
                            }
                            else
                            {
                                expiredTimeOrders.Add(orderId, 1);
                            }
                        }
                    }
                }

                // 4. Строим результат
                if (expiredTimeOrders.Count <= 0)
                    return new int[0];
                int[] resultOrderId = new int[expiredTimeOrders.Count];
                int resultCount = 0;

                foreach (KeyValuePair<int, int> kvp in expiredTimeOrders)
                {
                    if (kvp.Value == courierCount)
                    {
                        resultOrderId[resultCount++] = kvp.Key;
                    }
                }

                if (resultCount < resultOrderId.Length)
                {
                    Array.Resize(ref resultOrderId, resultCount);
                }

                // 5. Выход
                return resultOrderId;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Выборка заказов, которые не могут быть доставлены в срок
        /// при помощи курьров, приписанных к магазину
        /// </summary>
        /// <param name="allTypesCouriers">Курьры магазина</param>
        /// <param name="allTypeCouriersDeliveryInfo">Расчетная информация о доставке</param>
        /// <returns>Id заказов или null</returns>
        private static int[] GetShopCourierUndeliveredOrders(Courier[] allCouriers, CourierDeliveryInfo[][] allTypeCouriersDeliveryInfo)
        {
            // 1. Инициализация

            try
            {
                // 2. Проверяем исходные данные
                if (allCouriers == null || allCouriers.Length <= 0)
                    return null;
                if (allTypeCouriersDeliveryInfo == null || allTypeCouriersDeliveryInfo.Length <= 0)
                    return null;

                // 3. Сливаем Id всех заказов
                Dictionary<int, bool> allOrders = new Dictionary<int, bool>(200);

                for (int i = 0; i < allCouriers.Length; i++)
                {
                    Courier courier = allCouriers[i];
                    //CourierDeliveryInfo[] deliveryInfo = allTypeCouriersDeliveryInfo[courier.Index];
                    CourierDeliveryInfo[] deliveryInfo = allTypeCouriersDeliveryInfo[i];

                    for (int j = 0; j < deliveryInfo.Length; j++)
                    {
                        int orderId = deliveryInfo[j].ShippingOrder.Id_order;
                        //if (orderId == 2977845)
                        //{
                        //    i = i;
                        //}
                            allOrders[orderId] = false;
                    }
                }

                if (allOrders.Count <= 0)
                    return new int[0];

                // 4. Помечаем те заказы, которые выполнены или могут быть выполнены
                for (int i = 0; i < allCouriers.Length; i++)
                {
                    Courier courier = allCouriers[i];
                    if (courier.CourierType.VechicleType == CourierVehicleType.GettTaxi ||
                        courier.CourierType.VechicleType == CourierVehicleType.YandexTaxi)
                        continue;
                    //CourierDeliveryInfo[] deliveryInfo = allTypeCouriersDeliveryInfo[courier.Index];
                    CourierDeliveryInfo[] deliveryInfo = allTypeCouriersDeliveryInfo[i];
                    DateTime returnTime = courier.LastDeliveryEnd;

                    for (int j = 0; j < deliveryInfo.Length; j++)
                    {
                        CourierDeliveryInfo deliveryOrder = deliveryInfo[j];
                        if (deliveryOrder.Completed || 
                            deliveryOrder.EndDeliveryInterval >= returnTime)
                        {
                            allOrders[deliveryOrder.ShippingOrder.Id_order] = true;
                        }
                    }
                }

                // 5. Строим результат (не помеченные заказы)
                int[] resultOrderId = new int[allOrders.Count];
                int resultCount = 0;

                foreach (KeyValuePair<int, bool> kvp in allOrders)
                {
                    if (kvp.Value == false)
                    {
                        resultOrderId[resultCount++] = kvp.Key;
                    }
                }

                if (resultCount < resultOrderId.Length)
                {
                    Array.Resize(ref resultOrderId, resultCount);
                }

                // 6. Выход
                return resultOrderId;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Выборка курьеров магазина на заданный день
        /// c установкой интервала рабочего времени и 
        /// средней стоимости заказа для способа доставки
        /// </summary>
        /// <param name="shop">Магазин</param>
        /// <param name="day">День</param>
        /// <param name="statistics">Накопленная статистика</param>
        /// <param name="couriers"></param>
        /// <returns>0 - Курьеры созданы; иначе - курьеры не созданы</returns>
        private static int GetDayCouriersOfShop(Shop shop, DateTime day, ShopStatistics statistics, out ShopCouriers couriers)
        {
            // 1. Инициализация
            int rc = 1;
            couriers = null;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (shop == null)
                    return rc;
                if (statistics == null || 
                    !statistics.IsLoaded)
                    return rc;

                // 3. Выбираем CS-статистику за день
                rc = 3;
                ShopCourierStatistics[] shopDayStatistics = statistics.CSStatistics.SelectShopDayStatistics(shop.N, day);
                if (shopDayStatistics == null || shopDayStatistics.Length <= 0)
                    return rc;

                // 4. Строим курьеров для магазина и подсчитываем среднюю стоимость заказа для каждого типа курьера
                rc = 4;
                couriers = new ShopCouriers();
                int maxValue = Enum.GetValues(typeof(CourierVehicleType)).Cast<int>().Max();
                double[] totalTypeCost = new double[maxValue + 1];
                int[] typeOrders = new int[maxValue + 1];

                for (int i = 0; i < shopDayStatistics.Length; i++)
                {
                    ShopCourierStatistics courierStatistics = shopDayStatistics[i];

                    Courier courier = courierStatistics.ShopCourier;
                    Courier shopCourier = new Courier(courier.Id, courier.CourierType);

                    if (courier.CourierType.VechicleType == CourierVehicleType.GettTaxi ||
                        courier.CourierType.VechicleType == CourierVehicleType.YandexTaxi)
                    {
                        shopCourier.WorkStart = new TimeSpan(0);
                        shopCourier.WorkEnd = new TimeSpan(24, 0, 0);
                    }
                    else
                    {
                        shopCourier.WorkStart = courierStatistics.WorkStart.TimeOfDay;
                        shopCourier.WorkEnd = courierStatistics.WorkEnd.TimeOfDay;

                        TimeSpan dt = shopCourier.WorkEnd - shopCourier.WorkStart;
                        if (dt.TotalHours < Courier.MIN_WORK_TIME)
                        {
                            shopCourier.WorkEnd = shopCourier.WorkStart.Add(TimeSpan.FromHours(Courier.MIN_WORK_TIME));
                        }
                        else
                        {
                            if (dt.Minutes > 0 || dt.Seconds > 0)
                            {
                                 shopCourier.WorkEnd = shopCourier.WorkStart.Add(TimeSpan.FromHours(dt.Hours + 1));
                            }
                        }

                        shopCourier.LastDeliveryStart = day.Date;
                        shopCourier.LastDeliveryEnd = courierStatistics.WorkStart;
                    }

                    shopCourier.Status = CourierStatus.Ready;
                    couriers.AddCourier(shopCourier);

                    //courier.WorkStart = courierStatistics.WorkStart.TimeOfDay;
                    //courier.WorkEnd = courierStatistics.WorkEnd.TimeOfDay;
                    //courier.OrderCount = courierStatistics.OrderCount;
                    //courier.TotalCost = courierStatistics.TotalCost;
                    //courier.LastDeliveryStart = day.Date;
                    //courier.LastDeliveryEnd = courierStatistics.WorkStart;
                    //courier.TotalDeliveryTime = courierStatistics.WorkTime;

                    int vehicleTypeValue = (int)courier.CourierType.VechicleType;
                    totalTypeCost[vehicleTypeValue] += courierStatistics.TotalCost;
                    typeOrders[vehicleTypeValue] += courierStatistics.OrderCount;
                }

                foreach (Courier courier in couriers.Couriers.Values)
                {
                    int vehicleTypeValue = (int)courier.CourierType.VechicleType;
                    if (typeOrders[vehicleTypeValue] > 0)
                    {
                        courier.AverageOrderCost = totalTypeCost[vehicleTypeValue] / typeOrders[vehicleTypeValue];
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
        /// Построение курьеров для
        /// заданного типа доставки
        /// </summary>
        /// <param name="deliveryType"></param>
        /// <returns>Доступные курьеры</returns>
        private static Courier[] GetAvailableCouriers(DeliveryType deliveryType)
        {
            // 1. Инициализация

            try
            {
                // 2. Добавляем такси
                Courier[] courierList = new Courier[4];
                int listCount = 0;

                Courier gettCourier = new Courier(1, new CourierType_GettTaxi());
                gettCourier.Status = CourierStatus.Ready;
                gettCourier.WorkStart = new TimeSpan(0);
                gettCourier.WorkEnd = new TimeSpan(24, 0, 0);
                courierList[listCount++] = gettCourier;

                Courier yandexCourier = new Courier(2, new CourierType_YandexTaxi());
                yandexCourier.Status = CourierStatus.Ready;
                yandexCourier.WorkStart = new TimeSpan(0);
                yandexCourier.WorkEnd = new TimeSpan(24, 0, 0);
                courierList[listCount++] = yandexCourier;

                // 3. Добавляем Car-курьера
                if (deliveryType == DeliveryType.None ||
                    (deliveryType & DeliveryType.Car) != 0)
                {
                    Courier carCourier = new Courier(3, new CourierType_Car());
                    carCourier.Status = CourierStatus.Ready;
                    carCourier.WorkStart = new TimeSpan(0);
                    carCourier.WorkEnd = new TimeSpan(24, 0, 0);
                    courierList[listCount++] = carCourier;
                }

                // 4. Добавляем Bicycle-курьера
                if (deliveryType == DeliveryType.None ||
                    (deliveryType & DeliveryType.Bicycle) != 0)
                {
                    Courier bicycleCourier = new Courier(3, new CourierType_Bicycle());
                    bicycleCourier.Status = CourierStatus.Ready;
                    bicycleCourier.WorkStart = new TimeSpan(0);
                    bicycleCourier.WorkEnd = new TimeSpan(24, 0, 0);
                    courierList[listCount++] = bicycleCourier;
                }

                if (listCount < courierList.Length)
                {
                    Array.Resize(ref courierList, listCount);
                }

                // 5. Выход - Ok
                return courierList;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Выбор данных об отгрузке заданных неотгруженных заказов
        /// и отметка точек ИНД на временной оси
        /// </summary>
        /// <param name="orderId">Заказы</param>
        /// <param name="courierEnabled">Маска доступных курьеров</param>
        /// <param name="allTypeCouriers">Все курьеры</param>
        /// <param name="allTypeCouriersDeliveryInfo">Информация об отгрузке заказов для всех курьеров</param>
        /// <param name="couriers">Отобранные курьеры</param>
        /// <param name="courierDeliveryInfo">Информация об заданных заказах для отобранных курьеров</param>
        /// <param name="courierIntervalHist">Точки ИНД на временной оси для заданных заказов</param>
        /// <returns>0 - выбор произведен; иначе - выбор не произведен</returns>
        private static int GetDeliveryInfoAndHistIntervalForOrders(
            int[] orderId, bool[] courierEnabled, Courier[] allTypeCouriers, CourierDeliveryInfo[][] allTypeCouriersDeliveryInfo, 
            out Courier[] couriers, out CourierDeliveryInfo[][] courierDeliveryInfo, out int[][,] courierIntervalHist)
        {
            // 1. Инициализация
            int rc = 1;
            couriers = null;
            courierDeliveryInfo = null;
            courierIntervalHist = null;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (orderId == null || orderId.Length <= 0)
                    return rc;
                if (courierEnabled == null || courierEnabled.Length <= 0)
                    return rc;
                if (allTypeCouriers == null || allTypeCouriers.Length != courierEnabled.Length)
                    return rc;
                if (allTypeCouriersDeliveryInfo == null || allTypeCouriersDeliveryInfo.Length != courierEnabled.Length)
                    return rc;

                // 3. Подсчитываем число доступных курьров
                rc = 3;
                int courierCount = courierEnabled.Count(p => p);
                if (courierCount <= 0)
                    return rc;

                // 4. Сортируем Id заказов
                rc = 4;
                Array.Sort(orderId);

                // 5. Выделяем память под результат
                rc = 5;
                couriers = new Courier[courierCount];
                courierDeliveryInfo = new CourierDeliveryInfo[courierCount][];
                courierIntervalHist = new int[courierCount][,];

                // 6. Цикл отбора заказов для каждого заданного курьера
                rc = 6;
                CourierDeliveryInfo[] selectedDeliveryInfo = new CourierDeliveryInfo[orderId.Length];
                int selectedDeliveryInfoCount;
                courierCount = 0;
                bool isExists = false;

                for (int i = 0; i < courierEnabled.Length; i++)
                {
                    // 6.1 Пропускаем недоступных курьеров
                    rc = 61;
                    if (!courierEnabled[i])
                        continue;

                    // 6.2  Выбираем данные курьера
                    rc = 62;
                    couriers[courierCount] = allTypeCouriers[i];
                    CourierDeliveryInfo[] deliveryInfo = allTypeCouriersDeliveryInfo[i];
                    selectedDeliveryInfoCount = 0;
                    int[,] intervalHist = new int[1440, ORDERS_PER_MINUTE_LIMIT];

                    // 6.3 Отбираем данные, относящиеся к заданным заказам и отмечаем точки ИНД на временной оси
                    rc = 63;

                    for (int j = 0; j < deliveryInfo.Length; j++)
                    {
                        CourierDeliveryInfo orderInfo = deliveryInfo[j];
                        if (orderInfo.Completed)
                            continue;
                        if (Array.BinarySearch(orderId, orderInfo.ShippingOrder.Id_order) >= 0)
                        {
                            selectedDeliveryInfo[selectedDeliveryInfoCount++] = orderInfo;

                            int startHistIndex = (int)orderInfo.StartDeliveryInterval.TimeOfDay.TotalMinutes;
                            int endHistIndex = (int)orderInfo.EndDeliveryInterval.TimeOfDay.TotalMinutes;

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
                    }

                    if (selectedDeliveryInfoCount <= 0)
                    {
                        courierDeliveryInfo[courierCount] = new CourierDeliveryInfo[0];
                    }
                    else
                    {
                        isExists = true;
                        CourierDeliveryInfo[] courierInfo = selectedDeliveryInfo.Take(selectedDeliveryInfoCount).ToArray();
                        Array.Sort(courierInfo, CompareByEndDeliveryInterval);
                        courierDeliveryInfo[courierCount] = courierInfo;
                    }

                    courierIntervalHist[courierCount++] = intervalHist;
                }

                if (!isExists)
                    return rc;

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
        /// Построение наилучшей отгрузки в заданный момент времени
        /// </summary>
        /// <param name="courier">Почасовой курьер (OnFoot, Bicycle, Car)</param>
        /// <param name="timeStamp">Временная точка расчета</param>
        /// <param name="deliveryInfo">Доставляемые товары</param>
        /// <param name="intervalHist">Точки ИНД доставляемых товаров</param>
        /// <param name="permutationsRepository">Генератор перестановок</param>
        /// <param name="delivery">Построенная отгрузка</param>
        /// <returns>0 - отгрузка построена; иначе - отгрузка не построена</returns>
        private static int BuildCourierDelivery(Courier courier, DateTime timeStamp, CourierDeliveryInfo[] deliveryInfo, int[,] intervalHist, Permutations permutationsRepository, out CourierDeliveryInfo delivery)
        {
            // 1. Инициализация
            int rc = 1;
            int rc1 = 1;
            delivery = null;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (courier == null ||
                    courier.CourierType.VechicleType == CourierVehicleType.GettTaxi ||
                    courier.CourierType.VechicleType == CourierVehicleType.YandexTaxi)
                    return rc;
                if (courier.LastDeliveryEnd > timeStamp)
                    return rc;
                if (deliveryInfo == null || deliveryInfo.Length <= 0)
                    return rc;
                if (intervalHist == null || intervalHist.Length <= 0)
                    return rc;
                if (permutationsRepository == null)
                    return rc;

                // 3. Находим первый заказ доступный для доставки
                rc = 3;
                CourierDeliveryInfo nextOrder = deliveryInfo.FirstOrDefault(p => !p.Completed && p.EndDeliveryInterval >= timeStamp && p.StartDeliveryInterval <= timeStamp);
                if (nextOrder == null)
                    return rc;

                // 4. Выбираем все заказы - кандидаты на отгрузку по времени
                //     (имеющие пересекающиеся интервалы доставки)
                rc = 4;
                DateTime startIntervalTime = nextOrder.StartDeliveryInterval;
                if (startIntervalTime < timeStamp)
                    startIntervalTime = timeStamp;
                int histIndex = (int)timeStamp.TimeOfDay.TotalMinutes;
                int histCount = intervalHist[histIndex, 0];

                // Изолированная точка
                if (histCount <= 1)
                {
                    delivery = nextOrder;
                    delivery.StartDelivery = timeStamp;
                    delivery.NodeDeliveryTime = new double[] { courier.CourierType.GetOrderTime, nextOrder.DeliveryTime };
                    delivery.NodeDistance = new double[] { 0, nextOrder.DistanceFromShop };
                    delivery.Cost = courier.AverageOrderCost;
                    return rc = 0;
                }

                // 5. Отбираем заказы подходящие по времени
                rc = 5;
                CourierDeliveryInfo[] selectedOrders = new CourierDeliveryInfo[histCount - 1];
                int count = 0;

                for (int i = 1; i <= histCount; i++)
                {
                    int orderIndex = intervalHist[histIndex, i];
                    CourierDeliveryInfo dInfo = deliveryInfo[orderIndex];
                    if (!dInfo.Completed && dInfo != nextOrder && dInfo.EndDeliveryInterval > timeStamp)
                    {
                        selectedOrders[count++] = deliveryInfo[orderIndex];
                    }
                }

                if (count <= 0)
                {
                    delivery = nextOrder;
                    delivery.StartDelivery = timeStamp;
                    delivery.NodeDeliveryTime = new double[] { courier.CourierType.GetOrderTime, nextOrder.DeliveryTime };
                    delivery.NodeDistance = new double[] { 0, nextOrder.DistanceFromShop };
                    delivery.Cost = courier.AverageOrderCost;
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
                    double totalWeight = selOrder.ShippingOrder.Weight + nextOrder.ShippingOrder.Weight;

                    betweenDistance[1] = nextOrder.DistanceFromShop;
                    betweenDistance[2] = nextDist[i];
                    betweenDistance[3] = selOrder.DistanceFromShop;
                    deliveryTimeLimit[1] = nextOrder.ShippingOrder.GetDeliveryLimit(Courier.DELIVERY_LIMIT);
                    deliveryTimeLimit[2] = selOrder.ShippingOrder.GetDeliveryLimit(Courier.DELIVERY_LIMIT);

                    CourierDeliveryInfo twoOrdersDeliveryInfo;
                    double[] nodeDeliveryTime;
                    rc1 = courier.DeliveryCheck(timeStamp, betweenDistance, deliveryTimeLimit, totalWeight, out twoOrdersDeliveryInfo, out nodeDeliveryTime);
                    if (rc1 != 0)
                    {
                        double temp = betweenDistance[1];
                        betweenDistance[1] = betweenDistance[2];
                        betweenDistance[2] = temp;

                        DateTime tempTime = deliveryTimeLimit[1];
                        deliveryTimeLimit[1] = deliveryTimeLimit[2];
                        deliveryTimeLimit[2] = tempTime;
                        double[] nodeDeliveryTimeX;
                        rc1 = courier.DeliveryCheck(timeStamp, betweenDistance, deliveryTimeLimit, totalWeight, out twoOrdersDeliveryInfo, out nodeDeliveryTimeX);
                    }

                    if (rc1 == 0)
                    {
                        selectedOrders[count++] = selOrder;
                    }
                }

                if (count <= 0)
                {
                    delivery = nextOrder;
                    delivery.StartDelivery = timeStamp;
                    delivery.NodeDeliveryTime = new double[] { courier.CourierType.GetOrderTime, nextOrder.DeliveryTime };
                    delivery.NodeDistance = new double[] { 0, nextOrder.DistanceFromShop };
                    delivery.Cost = courier.AverageOrderCost;
                    return rc = 0;
                }

                Array.Resize(ref selectedOrders, count + 1);
                selectedOrders[count++] = nextOrder;  // Исходный заказ последний
                Array.Reverse(selectedOrders);        // Исходный заказ первый

                // 8. Решаем задачу комивояжера - нужно построить путь обхода точек доставки с минимальной стоимостью.
                //    Если невозможно вовремя доставить все заказы, то выбираем путь с максимальным числом точек доставки
                //    и минимальной стоимостью. Любой путь должен включать next-точку (точку с индексом 0)
                rc = 8;
                if (count <= 8)
                {
                    // 8.1 Решаем перебором всех вариантов
                    rc = 81;
                    int[,] permutations = permutationsRepository.GetPermutations(count);
                    rc1 = SingleShopSolution.FindPathWithMinCostEx(courier, selectedOrders, permutations, timeStamp, out delivery);
                    if (rc1 == 0 && delivery != null)
                    {
                        delivery.ShippingOrder = nextOrder.ShippingOrder;
                    }
                }
                else
                {
                    // 10.2. Применим 2-opt алгоритм
                    rc = 102;
                    rc1 = SingleShopSolution.FindPathWithMinCostBy2Opt(courier, selectedOrders, timeStamp, out delivery);
                    if (rc1 == 0 && delivery != null)
                    {
                        delivery.ShippingOrder = nextOrder.ShippingOrder;
                    }
                }

                if (delivery == null)
                    return rc;

                // 9. Подставляем стоимость отгрузки, исходя из
                //     средней стоимости доставки заказа
                rc = 9;
                delivery.Cost = courier.AverageOrderCost * delivery.OrderCount;

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
        /// Построение отгрузки с максимальным числом заказов на заданный момент времени
        /// (тактика "жадного" курьера - хватаю всё, что могу доставить в срок)
        /// </summary>
        /// <param name="courier">Почасовой курьер (OnFoot, Bicycle, Car)</param>
        /// <param name="timeStamp">Временная точка расчета</param>
        /// <param name="deliveryInfo">Доставляемые товары</param>
        /// <param name="intervalHist">Точки ИНД доставляемых товаров</param>
        /// <param name="permutationsRepository">Хранилище перестановок</param>
        /// <param name="delivery">Построенная отгрузка</param>
        /// <returns>0 - отгрузка построена; иначе - отгрузка не построена</returns>
        private static int BuildCourierDeliveryEx(Courier courier, DateTime timeStamp, CourierDeliveryInfo[] deliveryInfo, int[,] intervalHist, Permutations permutationsRepository, out CourierDeliveryInfo delivery)
        {
            // 1. Инициализация
            int rc = 1;
            int rc1 = 1;
            delivery = null;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (courier == null ||
                    courier.CourierType.VechicleType == CourierVehicleType.GettTaxi ||
                    courier.CourierType.VechicleType == CourierVehicleType.YandexTaxi)
                    return rc;
                if (courier.LastDeliveryEnd > timeStamp)
                    return rc;
                if (deliveryInfo == null || deliveryInfo.Length <= 0)
                    return rc;
                if (intervalHist == null || intervalHist.Length <= 0)
                    return rc;
                if (permutationsRepository == null)
                    return rc;

                // 3. Находим первый заказ доступный для доставки
                rc = 3;
                CourierDeliveryInfo nextOrder = deliveryInfo.FirstOrDefault(p => !p.Completed && p.EndDeliveryInterval >= timeStamp && p.StartDeliveryInterval <= timeStamp);
                if (nextOrder == null)
                    return rc;

                // 4. Выбираем все заказы - кандидаты на отгрузку по времени
                //     (имеющие пересекающиеся интервалы доставки)
                rc = 4;
                DateTime startIntervalTime = nextOrder.StartDeliveryInterval;
                if (startIntervalTime < timeStamp)
                    startIntervalTime = timeStamp;
                int histIndex = (int)timeStamp.TimeOfDay.TotalMinutes;
                int histCount = intervalHist[histIndex, 0];

                // Изолированная точка
                if (histCount <= 1)
                {
                    delivery = nextOrder;
                    delivery.StartDelivery = timeStamp;
                    delivery.NodeDeliveryTime = new double[] { courier.CourierType.GetOrderTime, nextOrder.DeliveryTime };
                    delivery.NodeDistance = new double[] { 0, nextOrder.DistanceFromShop };
                    delivery.Cost = courier.AverageOrderCost;
                    return rc = 0;
                }

                // 5. Отбираем заказы подходящие по времени
                rc = 5;
                CourierDeliveryInfo[] selectedOrders = new CourierDeliveryInfo[histCount - 1];
                int count = 0;

                for (int i = 1; i <= histCount; i++)
                {
                    int orderIndex = intervalHist[histIndex, i];
                    CourierDeliveryInfo dInfo = deliveryInfo[orderIndex];
                    if (!dInfo.Completed && dInfo != nextOrder && dInfo.EndDeliveryInterval > timeStamp)
                    {
                        selectedOrders[count++] = deliveryInfo[orderIndex];
                    }
                }

                if (count <= 0)
                {
                    delivery = nextOrder;
                    delivery.StartDelivery = timeStamp;
                    delivery.NodeDeliveryTime = new double[] { courier.CourierType.GetOrderTime, nextOrder.DeliveryTime };
                    delivery.NodeDistance = new double[] { 0, nextOrder.DistanceFromShop };
                    //delivery.Cost = courier.AverageOrderCost;
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
                    double totalWeight = selOrder.ShippingOrder.Weight + nextOrder.ShippingOrder.Weight;

                    betweenDistance[1] = nextOrder.DistanceFromShop;
                    betweenDistance[2] = nextDist[i];
                    betweenDistance[3] = selOrder.DistanceFromShop;
                    deliveryTimeLimit[1] = nextOrder.ShippingOrder.GetDeliveryLimit(Courier.DELIVERY_LIMIT);
                    deliveryTimeLimit[2] = selOrder.ShippingOrder.GetDeliveryLimit(Courier.DELIVERY_LIMIT);

                    CourierDeliveryInfo twoOrdersDeliveryInfo;
                    double[] nodeDeliveryTime;
                    rc1 = courier.DeliveryCheck(timeStamp, betweenDistance, deliveryTimeLimit, totalWeight, out twoOrdersDeliveryInfo, out nodeDeliveryTime);
                    if (rc1 != 0)
                    {
                        double temp = betweenDistance[1];
                        betweenDistance[1] = betweenDistance[2];
                        betweenDistance[2] = temp;

                        DateTime tempTime = deliveryTimeLimit[1];
                        deliveryTimeLimit[1] = deliveryTimeLimit[2];
                        deliveryTimeLimit[2] = tempTime;
                        double[] nodeDeliveryTimeX;
                        rc1 = courier.DeliveryCheck(timeStamp, betweenDistance, deliveryTimeLimit, totalWeight, out twoOrdersDeliveryInfo, out nodeDeliveryTimeX);
                    }

                    if (rc1 == 0)
                    {
                        selectedOrders[count++] = selOrder;
                    }
                }

                if (count <= 0)
                {
                    delivery = nextOrder;
                    delivery.StartDelivery = timeStamp;
                    delivery.NodeDeliveryTime = new double[] { courier.CourierType.GetOrderTime, nextOrder.DeliveryTime };
                    delivery.NodeDistance = new double[] { 0, nextOrder.DistanceFromShop };
                    delivery.Cost = courier.AverageOrderCost;
                    return rc = 0;
                }

                Array.Resize(ref selectedOrders, count + 1);
                selectedOrders[count++] = nextOrder;  // Исходный заказ последний
                Array.Reverse(selectedOrders);        // Исходный заказ первый

                // 8. Решаем задачу комивояжера - нужно построить путь обхода точек доставки с минимальной стоимостью.
                //    Если невозможно вовремя доставить все заказы, то выбираем путь с максимальным числом точек доставки
                //    и минимальной стоимостью. Любой путь должен включать next-точку (точку с индексом 0)
                rc = 8;
                CourierDeliveryInfo iterDelivery;

                if (count <= 8)
                {
                    // 8.1 Перебор всех перестановок (n ≤ 8)
                    rc = 81;
                    int[,] permutations = permutationsRepository.GetPermutations(count);

                    for (int k = 0; k < count; k++)
                    {
                        nextOrder = selectedOrders[k];
                        selectedOrders[k] = selectedOrders[0];
                        selectedOrders[0] = nextOrder;

                        rc1 = SingleShopSolution.FindPathWithMinCostEx(courier, selectedOrders, permutations, timeStamp, out iterDelivery);
                        if (rc1 == 0 && iterDelivery != null)
                        {
                            if (delivery == null)
                            {
                                delivery = iterDelivery;
                                delivery.ShippingOrder = nextOrder.ShippingOrder;
                                if (delivery.OrderCount >= count)
                                    break;
                            }
                            else if (delivery.OrderCount < iterDelivery.OrderCount)
                            {
                                delivery = iterDelivery;
                                delivery.ShippingOrder = nextOrder.ShippingOrder;
                                if (delivery.OrderCount >= count)
                                    break;
                            }
                            else if (delivery.OrderCount == iterDelivery.OrderCount &&
                                     delivery.Cost > iterDelivery.Cost)
                            {
                                delivery = iterDelivery;
                                delivery.ShippingOrder = nextOrder.ShippingOrder;
                            }
                        }
                    }
                }
                else
                {
                    // 8.2 2-opt алгоритм (n > 8)
                    rc = 82;
                    for (int k = 0; k < count; k++)
                    {
                        nextOrder = selectedOrders[k];
                        selectedOrders[k] = selectedOrders[0];
                        selectedOrders[0] = nextOrder;

                        rc1 = SingleShopSolution.FindPathWithMinCostBy2Opt(courier, selectedOrders, timeStamp, out iterDelivery);
                        if (rc1 == 0 && iterDelivery != null)
                        {
                            if (delivery == null)
                            {
                                delivery = iterDelivery;
                                delivery.ShippingOrder = nextOrder.ShippingOrder;
                                if (delivery.OrderCount >= count)
                                    break;
                            }
                            else if (delivery.OrderCount < iterDelivery.OrderCount)
                            {
                                delivery = iterDelivery;
                                delivery.ShippingOrder = nextOrder.ShippingOrder;
                                if (delivery.OrderCount >= count)
                                    break;
                            }
                        }
                    }
                }

                if (delivery == null)
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
        /// Сравнение двух курьеров:
        /// </summary>
        /// <param name="courier1"></param>
        /// <param name="courier2"></param>
        /// <returns>-1 - courier1 предшествует courier2; 0 - courier1 совпадает с courier 2; 1 - courier1 следует за courier2</returns>
        private static int CompareByCourierType(Courier courier1, Courier courier2)
        {
            int caseNo = 0;
            if (courier1.CourierType.VechicleType == CourierVehicleType.GettTaxi || courier1.CourierType.VechicleType == CourierVehicleType.YandexTaxi) caseNo = 2;
            if (courier2.CourierType.VechicleType == CourierVehicleType.GettTaxi || courier2.CourierType.VechicleType == CourierVehicleType.YandexTaxi) caseNo++;

            switch (caseNo) // 1 2  (c - courier, t - taxi)
            {
                case 0:     // c c
                    if (courier1.LastDeliveryEnd < courier2.LastDeliveryEnd)
                        return -1;
                    if (courier1.LastDeliveryEnd > courier2.LastDeliveryEnd)
                        return 1;
                    return 0;
                case 1:     // c t
                    return -1;
                case 2:     // t c
                    return 1;
                case 3:     // t t
                    if (courier1.Id < courier2.Id)
                        return -1;
                    if (courier1.Id > courier2.Id)
                        return 1;
                    return 0;
                default:
                    return 0;
            }
        }

        /// <summary>
        /// Сравнение двух курьеров по времени возврата в магазин
        /// </summary>
        /// <param name="courier1"></param>
        /// <param name="courier2"></param>
        /// <returns>-1 - courier1 предшествует courier2; 0 - courier1 совпадает с courier 2; 1 - courier1 следует за courier2</returns>
        private static int CompareByLastDeliveryEnd(Courier courier1, Courier courier2)
        {
            if (courier1.LastDeliveryEnd < courier2.LastDeliveryEnd)
                return -1;
            if (courier1.LastDeliveryEnd > courier2.LastDeliveryEnd)
                return 1;
            return 0;
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
        public static int FindSalesmanSolution(Courier courier, CourierDeliveryInfo[] deliveredOrders, int[,] permutations, DateTime startTime, out CourierDeliveryInfo bestPathInfo)
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
                    // 4.1 Выбираем индексы перестановки
                    rc = 41;

                    for (int j = 0; j < count; j++)
                    {
                        orderIndex[j] = permutations[i, j];
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
                        }
                        else if (bestOrderCount == pathInfo.OrderCount && pathInfo.Cost < bestCost)
                        {
                            bestCost = pathInfo.Cost;
                            bestPathInfo = pathInfo;
                        }
                    }
                }

                if (bestPathInfo == null)
                    return rc;

                // 5. Выход - Ok
                rc = 0;
                return rc;
            }
            catch
            {
                return rc;
            }
        }

        private static int DeliveryCheck_cur(Courier courier, CourierDeliveryInfo[] deliveredOrders, int[] orderIndex, double[,] pointDist, DateTime startTime, out CourierDeliveryInfo pathInfo, out DateTime startDelivery, out double[] nodeDeliveryTime)
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

                if (startDelivery < startTime)
                    startDelivery = startTime;

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
                //if (orderIndex == null || orderIndex.Length < 2)
                //    return rc;
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

                if (startDelivery < startTime)
                    startDelivery = startTime;

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
        /// Отбор неотгруженных заказов по Id из списка 
        /// </summary>
        /// <param name="deliveryInfo">Заказы, из которых осуществляется выбор</param>
        /// <param name="sortedOrderId">Отсортированный список Id заказов</param>
        /// <returns>Отобранные заказы или null</returns>
        private static CourierDeliveryInfo[] SelectDeliveryInfoByOrderId(CourierDeliveryInfo[] deliveryInfo, int[] sortedOrderId)
        {
            // 1. Инициализация

            try
            {
                // 2. Проверяем исходные данные
                if (deliveryInfo == null || deliveryInfo.Length <= 0)
                    return new CourierDeliveryInfo[0];
                if (sortedOrderId == null || sortedOrderId.Length <= 0)
                    return new CourierDeliveryInfo[0];

                // 3. Отбираем заказы из заданного списка
                CourierDeliveryInfo[] selectedDeliveryInfo = new CourierDeliveryInfo[deliveryInfo.Length];
                int count = 0;

                for (int i = 0; i < deliveryInfo.Length; i++)
                {
                    CourierDeliveryInfo orderInfo = deliveryInfo[i];
                    if (!orderInfo.Completed)
                    {
                        if (Array.BinarySearch(sortedOrderId, orderInfo.ShippingOrder.Id_order) >= 0)
                        {
                            selectedDeliveryInfo[count++] = orderInfo;
                        }
                    }
                }

                if (count < selectedDeliveryInfo.Length)
                {
                    Array.Resize(ref selectedDeliveryInfo, count);
                }

                // 4. Выход - Ok
                return selectedDeliveryInfo;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Отбор заказов, который могут быть отгружены
        /// в данный момент времени
        /// </summary>
        /// <param name="timeStamp">Момент времени</param>
        /// <param name="deliveryInfo">Отгрузки, из которых осущесвляется выбор</param>
        /// <returns>Отоборанный заказы или null</returns>
        private static CourierDeliveryInfo[] SelectAvailableOrders(DateTime timeStamp, CourierDeliveryInfo[] deliveryInfo)
        {
            // 1. Инициализация

            try
            {
                // 2. Проверяем исходные данные
                if (deliveryInfo == null || deliveryInfo.Length <= 0)
                    return new CourierDeliveryInfo[0];

                // 3. Цикл отбора заказов
                CourierDeliveryInfo[] selectedDeliveryInfo = new CourierDeliveryInfo[deliveryInfo.Length];
                int count = 0;

                for (int i = 0; i < deliveryInfo.Length; i++)
                {
                    //if (deliveryInfo[i].ShippingOrder.Id_order == 2445984)
                    //{
                    //    i = i;
                    //}
                    CourierDeliveryInfo orderInfo = deliveryInfo[i];
                    if (!orderInfo.Completed &&
                        //orderInfo.ShippingOrder.Date_collected <= timeStamp &&
                        orderInfo.StartDeliveryInterval <= timeStamp &&
                        orderInfo.EndDeliveryInterval >= timeStamp)
                    {
                        selectedDeliveryInfo[count++] = orderInfo;
                    }
                }

                if (count < selectedDeliveryInfo.Length)
                {
                    Array.Resize(ref selectedDeliveryInfo, count);
                }

                // 4. Выход - Ok
                return selectedDeliveryInfo;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Проверка, что все заказы отгружены
        /// </summary>
        /// <param name="allDeliveryInfo">Прверяемые заказы</param>
        /// <returns>true - все заказы отгружены; false - отгружены не все заказы</returns>
        private static bool AreAllOrdersShipped(CourierDeliveryInfo[][] allDeliveryInfo)
        {
            // 1. Инициализация

            try
            {
                // 2. Проверяем исходные данные
                if (allDeliveryInfo == null || allDeliveryInfo.Length <= 0)
                    return true;

                // 3. Цикл проверки, что все заказы отгружены
                for (int i = 0; i < allDeliveryInfo.Length; i++)
                {
                    CourierDeliveryInfo[] courierDeliveryInfo = allDeliveryInfo[i];
                    if (courierDeliveryInfo != null || courierDeliveryInfo.Length > 0)
                    {
                        for (int j = courierDeliveryInfo.Length - 1; j >= 0; j--)
                        {
                            if (!courierDeliveryInfo[j].Completed)
                                return false;
                        }
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
