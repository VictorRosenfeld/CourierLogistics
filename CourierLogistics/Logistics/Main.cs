
namespace CourierLogistics.Logistics
{
    using CourierLogistics.Logistics.CourierStatisticsCalculator;
    using CourierLogistics.Logistics.FloatOptimalSolution;
    using CourierLogistics.Logistics.FloatSolution.CourierInLive;
    using CourierLogistics.Logistics.FloatSolution.CourierService;
    using CourierLogistics.Logistics.FloatSolution.CourierService.ServiceQueue;
    using CourierLogistics.Logistics.FloatSolution.FloatCourierStatistics;
    using CourierLogistics.Logistics.OptimalSingleShopSolution;
    using CourierLogistics.Logistics.OptimalSingleShopSolution.PermutationsRepository;
    using CourierLogistics.Logistics.RealSingleShopSolution;
    using CourierLogistics.Report;
    using CourierLogistics.SourceData.Couriers;
    using CourierLogistics.SourceData.Orders;
    using CourierLogistics.SourceData.Shops;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Windows.Forms;

    /// <summary>
    /// Отработка логистики курьеров
    /// </summary>
    public class Main
    {
        /// <summary>
        /// Файл с магазинами
        /// </summary>
        public string ShopsFile { get; private set; }

        /// <summary>
        /// Файл с заказами
        /// </summary>
        public string OrdersFile { get; private set; }

        /// <summary>
        /// Магазины
        /// </summary>
        public AllShops ShopsCollection { get; private set; } 

        /// <summary>
        /// Заказы
        /// </summary>
        public AllOrders OrdersCollection { get; private set; }

        /// <summary>
        /// Статистика использемая в реальной модели
        /// </summary>
        public ShopStatistics Statistics { get; private set; }

        /// <summary>
        /// Флаг: true - логистика создана; false - логистика не создана
        /// </summary>
        public bool IsCreated { get; private set; }

        /// <summary>
        /// Применение логистической модели
        /// </summary>
        /// <param name="shopsFile">Ффайл с магазнами</param>
        /// <param name="shopDeliveryTypesFile">Файл с типами магазинов</param>
        /// <param name="ordersFile">Файл с заказами</param>
        /// <returns></returns>
        public int Create(string shopsFile, string shopDeliveryTypesFile, string ordersFile)
        {
            // 1. Инициализация
            int rc = 1;
            IsCreated = false;
            ShopsCollection = null;
            OrdersCollection = null;
            ShopsFile = shopsFile;
            OrdersFile = ordersFile;

            try
            {
                // 2. Проверяем иисходные данные
                rc = 2;
                if (!File.Exists(shopsFile))
                {
                    MessageBox.Show($"Файл с магазинами\n{shopsFile}\nне найден", "Построение логистики", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return rc;
                }
                if (!File.Exists(ordersFile))
                {
                    MessageBox.Show($"Файл с заказами\n{ordersFile}\nне найден", "Построение логистики", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return rc;
                }

                // 3. Загружаем магазины
                rc = 3;
                AllShops shops;
                int rc1 = ShopsLoader.Load(shopsFile, out shops);
                if (rc1 != 0)
                {
                    MessageBox.Show($"Не удалось загрузить файл с магазинами\n{ordersFile}", "Построение логистики", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return rc = 100 * rc + rc1;
                }

                ShopsCollection = shops;

                rc1 = ShopsLoader.LoadDeliveryTypes(shopDeliveryTypesFile, shops);

                // 4. Загружаем заказы
                rc = 4;
                AllOrders orders;
                rc1 = OrdersLoader.Load(ordersFile, out orders);
                if (rc1 != 0)
                {
                    MessageBox.Show($"Не удалось загрузить файл с заказами\n{ordersFile}", "Построение логистики", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return rc = 100 * rc + rc1;
                }

                OrdersCollection = orders;

                // 5. Собираем статистику курьеров
                rc = 5;
                CourierStatistics courierStatistics = new CourierStatistics();
                rc1 = courierStatistics.Create(orders);
                if (rc1 != 0)
                {
                    MessageBox.Show($"Не удалось собрать ежедневную статистику курьеров", "Построение логистики", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return rc = 100 * rc + rc1;
                }

                // 6. Создаём отчет
                rc = 6;
                ExcelReport report = new ExcelReport("LogisticsReport.xlsx");

                // 7. Печатаем Courier Statistics
                rc = 7;
                CourierStatisticsDayData[] courierDayData = courierStatistics.StatisticsData.SelelectAllDailyData();
                if (courierDayData != null && courierDayData.Length > 0)
                {
                    Array.Sort(courierDayData, CompareCourierDayDataByDay);

                    foreach (CourierStatisticsDayData dayData in courierDayData)
                    {
                        report.PrintCourierStatisticsRow(dayData);
                    }
                }

                // 8. Извлекаем список всех магазинов
                rc = 8;
                int[] shopId = orders.GetShops();
                if (shopId == null || shopId.Length <= 0)
                    return rc;

                SingleShopSolution solution = new SingleShopSolution();
                Permutations permutationsRepository = new Permutations();

                for (int i = 0; i < shopId.Length; i++)
                {
                    //Console.WriteLine(i);
                    int id = shopId[i];
                    Order[] shopOrders = orders.GetShopOrders(id);
                    //CorrectOrderSelectedTime(shopOrders);
                    rc1 = solution.Create(shops.GetShop(id), shopOrders, report, ref permutationsRepository);
                    Console.WriteLine($"{i}. {DateTime.Now} > ShopNo = {id}. Day OrderCount = {shopOrders.Length}, rc1 = {rc1}");

                }


                report.Save();
                report.Show();
                return rc;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Построение логистики", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return rc;
            }
        }

        private static int CorrectOrderSelectedTime(Order[] orders)
        {
            // 1. Инициализация
            int rc = 1;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (orders == null || orders.Length <= 0)
                    return rc;

                // 3. Цикл коррктировки времени сборки заказа
                rc = 3;
                foreach(Order order in orders)
                {
                    DateTime timeLimit = order.GetDeliveryLimit(Courier.DELIVERY_LIMIT);
                    if (timeLimit != DateTime.MinValue)
                    {
                        if (order.Date_collected == DateTime.MinValue || order.Date_collected >= timeLimit)
                        {
                            order.Date_collected = timeLimit.AddHours(-1);
                        }
                        else if (order.Date_collected >= timeLimit.AddHours(-1))
                        {
                            order.Date_collected = timeLimit.AddHours(-1);
                        }
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
        /// Сравнение дневной статистики по дню и ID курьера
        /// </summary>
        /// <param name="dayData1">День 1</param>
        /// <param name="dayData2">День 2</param>
        /// <returns>-1 День_1 предшетвует День_2; 0 - День_1 = День_2; 1 - День_1 следует за День_2</returns>
        private static int CompareCourierDayDataByDay(CourierStatisticsDayData dayData1, CourierStatisticsDayData dayData2)
        {
            if (dayData1.Day < dayData2.Day)
                return -1;
            if (dayData1.Day > dayData2.Day)
                return 1;
            if (dayData1.Id < dayData2.Id)
                return -1;
            if (dayData1.Id > dayData2.Id)
                return 1;

            return 0;
        }

        /// <summary>
        /// Применение логистической модели
        /// </summary>
        /// <param name="shopsFile">Ффайл с магазнами</param>
        /// <param name="shopDeliveryTypesFile">Файл с типами магазинов</param>
        /// <param name="ordersFile">Файл с заказами</param>
        /// <returns></returns>
        public int CreateReal(string shopsFile, string shopDeliveryTypesFile, string ordersFile, string statisticsFile)
        {
            // 1. Инициализация
            int rc = 1;
            IsCreated = false;
            ShopsCollection = null;
            OrdersCollection = null;
            ShopsFile = shopsFile;
            OrdersFile = ordersFile;

            try
            {
                // 2. Проверяем иисходные данные
                rc = 2;
                if (!File.Exists(shopsFile))
                {
                    MessageBox.Show($"Файл с магазинами\n{shopsFile}\nне найден", "Построение логистики", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return rc;
                }
                if (!File.Exists(ordersFile))
                {
                    MessageBox.Show($"Файл с заказами\n{ordersFile}\nне найден", "Построение логистики", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return rc;
                }
                if (!File.Exists(statisticsFile))
                {
                    MessageBox.Show($"Excel-Файл со статистикой\n{statisticsFile}\nне найден", "Построение логистики", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return rc;
                }

                // 3. Загружаем магазины
                rc = 3;
                AllShops shops;
                int rc1 = ShopsLoader.Load(shopsFile, out shops);
                if (rc1 != 0)
                {
                    MessageBox.Show($"Не удалось загрузить файл с магазинами\n{ordersFile}", "Построение логистики", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return rc = 100 * rc + rc1;
                }

                ShopsCollection = shops;

                rc1 = ShopsLoader.LoadDeliveryTypes(shopDeliveryTypesFile, shops);

                // 4. Загружаем заказы
                rc = 4;
                AllOrders orders;
                rc1 = OrdersLoader.Load(ordersFile, out orders);
                if (rc1 != 0)
                {
                    MessageBox.Show($"Не удалось загрузить файл с заказами\n{ordersFile}", "Построение логистики", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return rc = 100 * rc + rc1;
                }

                OrdersCollection = orders;

                // 5. Загружаем статистику
                rc = 5;
                ShopStatistics stat = new ShopStatistics();
                rc1 = stat.Load(statisticsFile);
                if (rc1 != 0)
                {
                    MessageBox.Show($"Не удалось загрузить файл со статистикой\n{statisticsFile}", "Построение логистики", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return rc = 100 * rc + rc1;
                }

                Statistics = stat;

                // 6. Собираем статистику курьеров
                rc = 6;
                CourierStatistics courierStatistics = new CourierStatistics();
                rc1 = courierStatistics.Create(orders);
                if (rc1 != 0)
                {
                    MessageBox.Show($"Не удалось собрать ежедневную статистику курьеров", "Построение логистики", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return rc = 100 * rc + rc1;
                }

                // 7. Создаём отчет
                rc = 7;
                ExcelReport report = new ExcelReport("LogisticsReportR.xlsx");

                // 8. Печатаем Courier Statistics
                rc = 8;
                CourierStatisticsDayData[] courierDayData = courierStatistics.StatisticsData.SelelectAllDailyData();
                if (courierDayData != null && courierDayData.Length > 0)
                {
                    Array.Sort(courierDayData, CompareCourierDayDataByDay);

                    foreach (CourierStatisticsDayData dayData in courierDayData)
                    {
                        report.PrintCourierStatisticsRow(dayData);
                    }
                }

                // 9. Извлекаем список всех магазинов
                rc = 9;
                int[] shopId = orders.GetShops();
                if (shopId == null || shopId.Length <= 0)
                    return rc;

                // 10. Цикл построения реальной модели для каждого магазина
                rc = 10;
                RealShopSolution solution = new RealShopSolution(stat, report);

                for (int i = 0; i < shopId.Length; i++)
                {
                    //Console.WriteLine(i);
                    int id = shopId[i];
                    Order[] shopOrders = orders.GetShopOrders(id);
                    CorrectOrderSelectedTime(shopOrders);
                    rc1 = solution.Create(shops.GetShop(id), shopOrders);
                    Console.WriteLine($"{i}. {DateTime.Now} > ShopNo = {id}. Day OrderCount = {shopOrders.Length}, rc1 = {rc1}");

                }

                // 11. Сохраняем и открываем отчет
                rc = 11;
                report.Save();
                report.Show();

                // 12. Выход - Ok
                rc = 0;
                return rc;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Построение логистики", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return rc;
            }
        }

        /// <summary>
        /// Применение логистической модели
        /// </summary>
        /// <param name="shopsFile">Ффайл с магазнами</param>
        /// <param name="shopDeliveryTypesFile">Файл с типами магазинов</param>
        /// <param name="ordersFile">Файл с заказами</param>
        /// <param name="statisticsFile">Статистика оптимальной модели</param>
        /// <returns></returns>
        public int CreateFloat(string shopsFile, string shopDeliveryTypesFile, string ordersFile, string statisticsFile)
        {
            // 1. Инициализация
            int rc = 1;
            IsCreated = false;
            ShopsCollection = null;
            OrdersCollection = null;
            ShopsFile = shopsFile;
            OrdersFile = ordersFile;

            try
            {
                // 2. Проверяем иисходные данные
                rc = 2;
                if (!File.Exists(shopsFile))
                {
                    MessageBox.Show($"Файл с магазинами\n{shopsFile}\nне найден", "Построение логистики", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return rc;
                }
                if (!File.Exists(ordersFile))
                {
                    MessageBox.Show($"Файл с заказами\n{ordersFile}\nне найден", "Построение логистики", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return rc;
                }
                if (!File.Exists(statisticsFile))
                {
                    MessageBox.Show($"Excel-Файл со статистикой\n{statisticsFile}\nне найден", "Построение логистики", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return rc;
                }

                // 3. Загружаем магазины
                rc = 3;
                AllShops shops;
                int rc1 = ShopsLoader.Load(shopsFile, out shops);
                if (rc1 != 0)
                {
                    MessageBox.Show($"Не удалось загрузить файл с магазинами\n{ordersFile}", "Построение логистики", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return rc = 100 * rc + rc1;
                }

                ShopsCollection = shops;

                rc1 = ShopsLoader.LoadDeliveryTypes(shopDeliveryTypesFile, shops);

                // 4. Загружаем заказы
                rc = 4;
                AllOrders orders;
                rc1 = OrdersLoader.Load(ordersFile, out orders);
                if (rc1 != 0)
                {
                    MessageBox.Show($"Не удалось загрузить файл с заказами\n{ordersFile}", "Построение логистики", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return rc = 100 * rc + rc1;
                }

                OrdersCollection = orders;

                // 5. Загружаем статистику
                rc = 5;
                ShopStatistics stat = new ShopStatistics();
                rc1 = stat.Load(statisticsFile);
                if (rc1 != 0)
                {
                    MessageBox.Show($"Не удалось загрузить файл со статистикой\n{statisticsFile}", "Построение логистики", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return rc = 100 * rc + rc1;
                }

                Statistics = stat;

                // 6. Собираем статистику курьеров
                rc = 6;
                CourierStatistics courierStatistics = new CourierStatistics();
                rc1 = courierStatistics.Create(orders);
                if (rc1 != 0)
                {
                    MessageBox.Show($"Не удалось собрать ежедневную статистику курьеров", "Построение логистики", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return rc = 100 * rc + rc1;
                }

                // 7. Создаём отчет
                rc = 7;
                ExcelReport report = new ExcelReport("LogisticsReportF.xlsx", true);

                // 8. Печатаем Courier Statistics
                rc = 8;
                CourierStatisticsDayData[] courierDayData = courierStatistics.StatisticsData.SelelectAllDailyData();
                if (courierDayData != null && courierDayData.Length > 0)
                {
                    Array.Sort(courierDayData, CompareCourierDayDataByDay);

                    foreach (CourierStatisticsDayData dayData in courierDayData)
                    {
                        report.PrintCourierStatisticsRow(dayData);
                    }
                }

                // 9. Выбираем диапазон времени сборки заказов
                rc = 9;
                DateTime minTime;
                DateTime maxTime;
                rc1 = orders.GetOrdersTimeRange(out minTime, out maxTime);
                if (rc1 != 0 || minTime > maxTime)
                    return rc = 100 * rc + rc1;

                // 10. Цикл построения реальной модели для каждого дня
                rc = 10;
                DateTime fromDay = minTime.Date;
                DateTime toDay = minTime.Date;
                CourierEx[] dayCouriers;
                Dictionary<string, FloatCourierDayStatistics> dayStatistics = new Dictionary<string, FloatCourierDayStatistics>(5000);

                //SimpleService floatModel = new SimpleService();
                //SimpleServiceEx floatModel = new SimpleServiceEx();
                SimpleServiceEy floatModel = new SimpleServiceEy();

                for (DateTime day = fromDay; day <= toDay; day = day.AddDays(1))
                {
                    rc1 = CreateDayCouriers(day, stat.CSStatistics, shops, out dayCouriers);
                    if (rc1 != 0)
                    {
                        Console.WriteLine($"{DateTime.Now} > Day {day: dd:MM:yy}. Курьеры не созданы rc1 = {rc1}");
                    }
                    else
                    {
                        Order[] dayOders = orders.GetDayOrders(day);
                        rc1 = floatModel.StartDay(shops.Shops.Values.ToArray(), dayCouriers, dayOders, stat);
                        Console.WriteLine($"{DateTime.Now} > Day {day: dd:MM:yy}. Код возврата - {rc1}. Элементов в очереди событий - {floatModel.QueueItemCount}({floatModel.QueueCapacity})");
                        int nn = 0;
                        if (rc1 == 0)
                        {
                            int GeoCount = Helper.GeoCount;
                            Console.WriteLine($"GeoCount = {GeoCount}");

                            Order[] undeliveredOrders = dayOders.Where(p => !p.Completed).ToArray();
                            int nd = dayOders.Count(p => p.Source == -1);

                            for (int i = 0; i < undeliveredOrders.Length; i++)
                            {
                                Order order = undeliveredOrders[i];
                                if (order.Source != -1)
                                {
                                    Shop shop = shops.GetShop(order.ShopNo);
                                    if (shop == null)
                                    {
                                        //Console.WriteLine($"{order.Date_order}({order.Date_collected} ~ {order.DeliveryTimeLimit}) > Undelivered Order. ShopID = -{order.ShopNo}. OrederID = {order.Id_order}. Weight = {order.Weight}, Lat = {order.Latitude}, Long = {order.Longitude}");
                                    }
                                    else
                                    {
                                        nn++;
                                        double dist = 1.2 * Helper.Distance(shop.Latitude, shop.Longitude, order.Latitude, order.Longitude);
                                        double carArrivalTime = 60 * dist / 20;
                                        Console.WriteLine($"{order.Date_order}({order.Date_collected} ~ {order.DeliveryTimeLimit}) > Undelivered Order. ShopID = {order.ShopNo}. OrederID = {order.Id_order}. Weight = {order.Weight}, Lat = {order.Latitude}, Long = {order.Longitude}, dist ={dist:0.00} km, arrivalTime = {carArrivalTime: 0.00} min");
                                    }
                                }
                            }

                            // 10.1 Обрабатываем статистику
                            rc = 101;
                            QueueItem[] queueItem = floatModel.SelectQueueItemOfDeliveredOrders();
                            Array.Sort(queueItem, CompareQueueItemByEventTime);
                            dayStatistics.Clear();
                            int n = 0;

                            for (int i = 0; i < queueItem.Length; i++)
                            {
                                QueueItem item = queueItem[i];
                                QueueItemOrderDeliveredArgs args = item.Args as QueueItemOrderDeliveredArgs;
                                if (args == null || args.Delivery == null || args.Courier == null)
                                    continue;
                                n += args.Delivery.OrderCount;
                                report.PrintHistoryRowEx(ExcelReport.HISTORY_PAGE, args.Delivery);

                                FloatCourierDayStatistics courierExStatistics;
                                string key = FloatCourierDayStatistics.GetKey(day, args.Courier.Id);

                                if (!dayStatistics.TryGetValue(key, out courierExStatistics))
                                {
                                    double cost = (!args.Delivery.DeliveryCourier.IsTaxi ? args.Delivery.DeliveryCourier.TotalCost : 0);
                                    courierExStatistics = new FloatCourierDayStatistics(day, args.Courier, cost);
                                    dayStatistics.Add(key, courierExStatistics);
                                }



                                //if (!dayStatistics.TryGetValue(key, out courierExStatistics))
                                //{
                                //    courierExStatistics = new FloatCourierDayStatistics(day, args.Courier,);
                                //    dayStatistics.Add(key, courierExStatistics);
                                //}

                                courierExStatistics.Add(args.Delivery);
                            }

                            FloatCourierDayStatistics[] dayStat = dayStatistics.Values.ToArray();
                            Array.Sort(dayStat, CompareFloatCourierStatisticsByCourierId);

                            for (int i = 0; i < dayStat.Length; i++)
                            {

                                report.PrintFloatSummaryRowEx(ExcelReport.FLOAT_SUMMARY_PAGE, dayStat[i]);
                            }
                        }
                    }
                }



                // 9. Извлекаем список всех магазинов
                //rc = 9;
                //int[] shopId = orders.GetShops();
                //if (shopId == null || shopId.Length <= 0)
                //    return rc;






                //RealShopSolution solution = new RealShopSolution(stat, report);


                //for (int i = 0; i < shopId.Length; i++)
                //{
                //    //Console.WriteLine(i);
                //    int id = shopId[i];
                //    Order[] shopOrders = orders.GetShopOrders(id);
                //    CorrectOrderSelectedTime(shopOrders);
                //    //rc1 = solution.Create(shops.GetShop(id), shopOrders);
                //    Console.WriteLine($"{i}. {DateTime.Now} > ShopNo = {id}. Day OrderCount = {shopOrders.Length}, rc1 = {rc1}");

                //}

                // 11. Сохраняем и открываем отчет
                rc = 11;
                report.Save();
                report.Show();

                // 12. Выход - Ok
                rc = 0;
                return rc;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Построение логистики", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return rc;
            }
        }

        private int CompareQueueItemByEventTime(QueueItem item1, QueueItem item2)
        {
            if (item1.EventTime < item2.EventTime)
                return -1;
            if (item1.EventTime > item2.EventTime)
                return 1;
            return 0;
        }

        private int CompareFloatCourierStatisticsByCourierId(FloatCourierDayStatistics item1, FloatCourierDayStatistics item2)
        {
            if (item1.CourierId < item2.CourierId)
                return -1;
            if (item1.CourierId > item2.CourierId)
                return 1;
            return 0;
        }

        private int CompareCourierDeliveryInfoByStartDelivery(CourierDeliveryInfo devInfo1, CourierDeliveryInfo devInfo2)
        {
            if (devInfo1.StartDelivery < devInfo2.StartDelivery)
                return -1;
            if (devInfo1.StartDelivery > devInfo2.StartDelivery)
                return 1;
            return 0;
        }

        /// <summary>
        /// Создание курьров для заданного дня
        /// </summary>
        /// <param name="day">День</param>
        /// <param name="summaryStatistics">CS-статистика</param>
        /// <param name="shops">Магазины</param>
        /// <param name="dayCouriers">Курьеры</param>
        /// <returns>0 - курьеры созданы; иначе - курьеры не созданы</returns>
        private static int CreateDayCouriers(DateTime day, SummaryStatistics summaryStatistics, AllShops shops, out CourierEx[] dayCouriers)
        {
            // 1. Инициализация
            int rc = 1;
            dayCouriers = null;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (summaryStatistics == null ||
                    summaryStatistics.Statistics == null ||
                    summaryStatistics.Statistics.Length <= 0)
                    return rc;
                if (shops == null ||
                    shops.Shops == null ||
                    shops.Shops.Count <= 0)
                    return rc;

                // 3. Цикл построения всех курьеров, участвовавших в отгрузке за заданный день
                rc = 3;
                DateTime d = day.Date;
                int size = 5 * shops.Shops.Count;
                CourierEx[] couriers = new CourierEx[size];
                int courierCount = 0;

                // 4. Сначала добавляем такси
                rc = 4;
                CourierEx gettTaxi = new CourierEx(1, new CourierType_GettTaxi());
                gettTaxi.Status = CourierStatus.Ready;
                gettTaxi.WorkStart = new TimeSpan(0);
                gettTaxi.WorkEnd = new TimeSpan(24, 0, 0);
                couriers[courierCount++] = gettTaxi;

                CourierEx yandexTaxi = new CourierEx(2, new CourierType_YandexTaxi());
                yandexTaxi.Status = CourierStatus.Ready;
                yandexTaxi.WorkStart = new TimeSpan(0);
                yandexTaxi.WorkEnd = new TimeSpan(24, 0, 0);
                couriers[courierCount++] = yandexTaxi;

                // 5. Добавляем курьров магазинов для заданного дня
                rc = 5;

                foreach(ShopCourierStatistics shopDayStatistics in summaryStatistics.Statistics)
                {
                    if (shopDayStatistics.Date.Date == d)
                    {
                        Courier statCourier = shopDayStatistics.ShopCourier;
                        if (!statCourier.IsTaxi)
                        {
                            //if (shopDayStatistics.WorkTime > 4 && shopDayStatistics.OrderCost < 100)
                            //if (shopDayStatistics.WorkTime >= 8 && shopDayStatistics.OrderCost < 120)
                            if (shopDayStatistics.WorkTime >= 8 && shopDayStatistics.OrderCost < 95)
                            {
                                Shop shop = shops.GetShop(shopDayStatistics.ShopId);
                                if (shop != null)
                                {
                                    CourierEx courier = new CourierEx(courierCount + 1, statCourier.CourierType);
                                    courier.WorkStart = (shopDayStatistics.WorkStart - d);
                                    courier.WorkEnd = (shopDayStatistics.WorkStart.AddHours(shopDayStatistics.WorkTime) - d);
                                    courier.Latitude = shop.Latitude;
                                    courier.Longitude = shop.Longitude;
                                    courier.Status = CourierStatus.Unknown;
                                    if (courierCount >= couriers.Length)
                                    {
                                        Array.Resize(ref couriers, couriers.Length + 500);
                                    }

                                    couriers[courierCount++] = courier;
                                }
                            }
                        }
                    }
                }

                if (courierCount < couriers.Length)
                {
                    Array.Resize(ref couriers, courierCount);
                }

                dayCouriers = couriers;

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
        /// Применение логистической модели
        /// </summary>
        /// <param name="shopsFile">Ффайл с магазнами</param>
        /// <param name="ordersFile">Файл с заказами</param>
        /// <param name="statisticsFile">Статистика оптимальной модели</param>
        /// <returns></returns>
        public int CreateFloatOptimal(string shopsFile, string ordersFile, string statisticsFile)
        {
            // 1. Инициализация
            int rc = 1;
            IsCreated = false;
            ShopsCollection = null;
            OrdersCollection = null;
            ShopsFile = shopsFile;
            OrdersFile = ordersFile;

            try
            {
                // 2. Проверяем иисходные данные
                rc = 2;
                if (!File.Exists(shopsFile))
                {
                    MessageBox.Show($"Файл с магазинами\n{shopsFile}\nне найден", "Построение логистики", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return rc;
                }
                if (!File.Exists(ordersFile))
                {
                    MessageBox.Show($"Файл с заказами\n{ordersFile}\nне найден", "Построение логистики", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return rc;
                }
                if (!File.Exists(statisticsFile))
                {
                    MessageBox.Show($"Excel-Файл со статистикой\n{statisticsFile}\nне найден", "Построение логистики", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return rc;
                }

                // 3. Загружаем магазины
                rc = 3;
                AllShops shops;
                int rc1 = ShopsLoader.Load(shopsFile, out shops);
                if (rc1 != 0)
                {
                    MessageBox.Show($"Не удалось загрузить файл с магазинами\n{ordersFile}", "Построение логистики", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return rc = 100 * rc + rc1;
                }

                ShopsCollection = shops;

                // 4. Загружаем заказы
                rc = 4;
                AllOrders orders;
                rc1 = OrdersLoader.Load(ordersFile, out orders);
                if (rc1 != 0)
                {
                    MessageBox.Show($"Не удалось загрузить файл с заказами\n{ordersFile}", "Построение логистики", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return rc = 100 * rc + rc1;
                }

                OrdersCollection = orders;

                // 5. Загружаем статистику
                rc = 5;
                ShopStatistics stat = new ShopStatistics();
                rc1 = stat.Load(statisticsFile);
                if (rc1 != 0)
                {
                    MessageBox.Show($"Не удалось загрузить файл со статистикой\n{statisticsFile}", "Построение логистики", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return rc = 100 * rc + rc1;
                }

                Statistics = stat;

                // 6. Собираем статистику курьеров
                rc = 6;
                CourierStatistics courierStatistics = new CourierStatistics();
                rc1 = courierStatistics.Create(orders);
                if (rc1 != 0)
                {
                    MessageBox.Show($"Не удалось собрать ежедневную статистику курьеров", "Построение логистики", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return rc = 100 * rc + rc1;
                }

                // 7. Создаём отчет
                rc = 7;
                ExcelReport report = new ExcelReport("LogisticsReportFO.xlsx", true);

                // 8. Печатаем Courier Statistics
                rc = 8;
                CourierStatisticsDayData[] courierDayData = courierStatistics.StatisticsData.SelelectAllDailyData();
                if (courierDayData != null && courierDayData.Length > 0)
                {
                    Array.Sort(courierDayData, CompareCourierDayDataByDay);

                    foreach (CourierStatisticsDayData dayData in courierDayData)
                    {
                        report.PrintCourierStatisticsRow(dayData);
                    }
                }

                // 9. Выбираем диапазон времени сборки заказов
                rc = 9;
                DateTime minTime;
                DateTime maxTime;
                rc1 = orders.GetOrdersTimeRange(out minTime, out maxTime);
                if (rc1 != 0 || minTime > maxTime)
                    return rc = 100 * rc + rc1;

                // 10. Цикл построения реальной модели для каждого дня
                rc = 10;
                DateTime fromDay = minTime.Date;
                DateTime toDay = minTime.Date;
                CourierEx[] dayCouriers;
                Dictionary<string, FloatCourierDayStatistics> dayStatistics = new Dictionary<string, FloatCourierDayStatistics>(7000);

                FloatOptimalDaySolution floatOptimalModel = new FloatOptimalDaySolution();

                for (DateTime day = fromDay; day <= toDay; day = day.AddDays(1))
                {
                    Order[] dayOders = orders.GetDayOrders(day);
                    CourierDeliveryInfo[] oneDayDeliveries;
                    //rc1 = floatOptimalModel.Create(shops.Shops.Values.ToArray(), dayOders, stat, out oneDayDeliveries);
                    rc1 = floatOptimalModel.CreateEx(shops.Shops.Values.ToArray(), dayOders, stat, out oneDayDeliveries);
                    Console.WriteLine($"{DateTime.Now} > Day {day: dd:MM:yy}. Код возврата - {rc1}.");
                    int nn = 0;

                    if (rc1 == 0)
                    {
                        Order[] undeliveredOrders = dayOders.Where(p => !p.Completed).ToArray();
                        int nd = dayOders.Count(p => p.Source == -1);

                        for (int i = 0; i < undeliveredOrders.Length; i++)
                        {
                            Order order = undeliveredOrders[i];
                            if (order.Source != -1)
                            {
                                Shop shop = shops.GetShop(order.ShopNo);
                                if (shop == null)
                                {
                                    //Console.WriteLine($"{order.Date_order}({order.Date_collected} ~ {order.DeliveryTimeLimit}) > Undelivered Order. ShopID = -{order.ShopNo}. OrederID = {order.Id_order}. Weight = {order.Weight}, Lat = {order.Latitude}, Long = {order.Longitude}");
                                }
                                else
                                {
                                    nn++;
                                    double dist = 1.2 * Helper.Distance(shop.Latitude, shop.Longitude, order.Latitude, order.Longitude);
                                    double carArrivalTime = 60 * dist / 20;
                                    Console.WriteLine($"{order.Date_order}({order.Date_collected} ~ {order.DeliveryTimeLimit}) > Undelivered Order. ShopID = {order.ShopNo}. OrederID = {order.Id_order}. Weight = {order.Weight}, Lat = {order.Latitude}, Long = {order.Longitude}, dist ={dist:0.00} km, arrivalTime = {carArrivalTime: 0.00} min");
                                }
                            }
                        }

                        // 10.1 Обрабатываем статистику
                        rc = 101;
                        dayStatistics.Clear();
                        Array.Sort(oneDayDeliveries, CompareCourierDeliveryInfoByStartDelivery);

                        int n = 0;

                        foreach (CourierDeliveryInfo dayDelivery in oneDayDeliveries)
                        {
                            n += dayDelivery.OrderCount;
                            report.PrintHistoryRowEx(ExcelReport.HISTORY_PAGE, dayDelivery);
                            FloatCourierDayStatistics courierExStatistics;
                            string key = FloatCourierDayStatistics.GetKey(day, dayDelivery.DeliveryCourier.Id);

                            if (!dayStatistics.TryGetValue(key, out courierExStatistics))
                            {
                                double cost = (!dayDelivery.DeliveryCourier.IsTaxi ? dayDelivery.DeliveryCourier.TotalCost : 0);
                                courierExStatistics = new FloatCourierDayStatistics(day, (CourierEx) dayDelivery.DeliveryCourier, cost);
                                dayStatistics.Add(key, courierExStatistics);
                            }

                            courierExStatistics.Add(dayDelivery);
                        }

                        FloatCourierDayStatistics[] dayStat = dayStatistics.Values.ToArray();
                        Array.Sort(dayStat, CompareFloatCourierStatisticsByCourierId);

                        for (int i = 0; i < dayStat.Length; i++)
                        {
                            report.PrintFloatSummaryRowEx(ExcelReport.FLOAT_SUMMARY_PAGE, dayStat[i]);
                        }

                        goto printX;
                    }
                }

                // 11. Сохраняем и открываем отчет
                printX:
                rc = 11;
                report.Save();
                report.Show();

                // 12. Выход - Ok
                rc = 0;
                return rc;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Построение логистики", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return rc;
            }
        }

    }
}
