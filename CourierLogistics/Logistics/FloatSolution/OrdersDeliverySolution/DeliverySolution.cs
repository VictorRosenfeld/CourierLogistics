
namespace CourierLogistics.Logistics.FloatSolution.OrdersDeliverySolution
{
    using CourierLogistics.Logistics.FloatSolution.CourierInLive;
    using CourierLogistics.Logistics.OptimalSingleShopSolution.PermutationsRepository;
    using CourierLogistics.SourceData.Couriers;
    using CourierLogistics.SourceData.Orders;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Проверка и построение отгрузок
    /// </summary>
    public class DeliverySolution
    {

        /// <summary>
        /// Максимальное значение переменной цикла
        /// при построении разбиений множества для разного
        /// количества элемеентов (в диапазоне 0 - 8)
        /// </summary>
        private static int[] PARTITION_LOOP_MAX_VALUE = new int[] { 0x0, 0x1, 0x3, 0x7, 0xF, 0x1F, 0x3F, 0x7F, 0xFF, 0x1FF, 0x3FF };

        /// <summary>
        /// Поиск оптимального отгрузки в заданный момент времени
        /// </summary>
        /// <param name = "timeStamp" >Время начала доставки</param>
        /// <param name = "solutionCouriers">Доступные курьеры для отгрузки заказов</param>
        /// <param name = "solutionDeliveryInfo">Доступные для отгрузки заказы</param>
        /// <param name = "permutationsRepository">Перестановки</param >
        /// <returns>0 - решение построено; иначе - решение не построено</returns>
        public static int FindLocalSolutionAtTime(DateTime timeStamp,
                      CourierEx[] solutionCouriers, CourierDeliveryInfo[][] solutionDeliveryInfo,
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
                    CourierEx courier = solutionCouriers[i];
                    bool isTaxi = courier.IsTaxi;
                    CourierDeliveryInfo[] courierOrders = solutionDeliveryInfo[i].Where(p => !p.Completed).ToArray();

                    if (courierOrders != null && courierOrders.Length > 0)
                    {
                        // 3.2 Строим все комбинации из заказов, проверяем возможность их доставки в одной отгрузке и отбираем наилучшую отгрузку
                        // отдельно для такси и курьров магазина
                        rc = 32;
                        if (courierOrders.Length > 8)
                            courierOrders = courierOrders.Take(8).ToArray();
                        int orderCount = courierOrders.Length;

                        int maxValue = PARTITION_LOOP_MAX_VALUE[orderCount];
                        CourierDeliveryInfo salesmanSolution;
                        int selectedCount = 0;

                        for (int j = 1; j <= maxValue; j++)
                        {
                            // 3.3 Генерация очередного подмножества из заказов
                            rc = 33;
                            selectedCount = 0;

                            if ((j & 0x1) != 0)
                                selectedDeliveryInfo[selectedCount++] = courierOrders[0];
                            if (orderCount > 1)
                            {
                                if ((j & 0x2) != 0)
                                    selectedDeliveryInfo[selectedCount++] = courierOrders[1];
                                if (orderCount > 2)
                                {
                                    if ((j & 0x4) != 0)
                                        selectedDeliveryInfo[selectedCount++] = courierOrders[2];
                                    if (orderCount > 3)
                                    {
                                        if ((j & 0x8) != 0)
                                            selectedDeliveryInfo[selectedCount++] = courierOrders[3];
                                        if (orderCount > 4)
                                        {
                                            if ((j & 0x10) != 0)
                                                selectedDeliveryInfo[selectedCount++] = courierOrders[4];
                                            if (orderCount > 5)
                                            {
                                                if ((j & 0x20) != 0)
                                                    selectedDeliveryInfo[selectedCount++] = courierOrders[5];
                                                if (orderCount > 6)
                                                {
                                                    if ((j & 0x40) != 0)
                                                        selectedDeliveryInfo[selectedCount++] = courierOrders[6];
                                                    if (orderCount > 7)
                                                    {
                                                        if ((j & 0x80) != 0)
                                                            selectedDeliveryInfo[selectedCount++] = courierOrders[7];
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
                            if (isTaxi)
                            {
                                rc1 = FindSalesmanProblemSolution(courier, testOrders, permutations, timeStamp, out salesmanSolution);
                            }
                            else
                            {
                                rc1 = FindSalesmanProblemSolution(courier, testOrders, permutations, courier.ArrivalTime, out salesmanSolution);
                                if (rc1 == 0 && salesmanSolution != null)
                                {
                                    TimeSpan arrivalTime = (courier.ArrivalTime - timeStamp);
                                    salesmanSolution.CourierArrivalTime = arrivalTime;
                                    double plusCost = courier.CourierType.HourlyRate * arrivalTime.TotalHours;
                                    salesmanSolution.Cost += plusCost;
                                }
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
                if (delivery == null)
                    delivery = bestTaxiDelivery;
                if (delivery == null)
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

        /// <summary>
        /// Поиск пути с минимальной стоимостью среди заданных перестановок
        /// </summary>
        /// <param name="courier">Курьер</param>
        /// <param name="deliveredOrders">Доставляемые заказы</param>
        /// <param name="permutations">Перестановки, среди которых осуществляется поиск</param>
        /// <param name="startTime">Время, начиная с которого можно использовать курьера</param>
        /// <param name="bestPathInfo">Путь с минимальной стоимостью</param>
        /// <returns>0 - путь найден; иначе - путь не найден</returns>
        public static int FindSalesmanProblemSolution(CourierEx courier, CourierDeliveryInfo[] deliveredOrders, int[,] permutations, DateTime startTime, out CourierDeliveryInfo bestPathInfo)
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
                        double d = FloatSolutionParameters.DISTANCE_ALLOWANCE * Helper.Distance(latitude1, longitude1, order.Latitude, order.Longitude);
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

        /// <summary>
        /// Проверка реализуемости доставки данных заказов курьром
        /// </summary>
        /// <param name="courier">Курьер</param>
        /// <param name="deliveredOrders">Проверяемые заказы</param>
        /// <param name="orderIndex">Перестановка</param>
        /// <param name="pointDist">Попарные расстояния между точками</param>
        /// <param name="startTime">Время, начиная с которого производится отгрузка</param>
        /// <param name="pathInfo">Путь следования</param>
        /// <param name="startDelivery">Время начала отгрузки</param>
        /// <param name="nodeDeliveryTime">Время вручения заказа относительно начала отгрузки, мин</param>
        /// <returns>0 - отгрузка реализуема; иначе - отгрузка не реализуема</returns>
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
        /// Поиск пути с минимальной стоимостью среди заданных перестановок
        /// </summary>
        /// <param name="courier">Курьер</param>
        /// <param name="deliveredOrders">Доставляемые заказы</param>
        /// <param name="pairDist">Попарные расстояния между точками вручения заказов</param>
        /// <param name="permutations">Перестановки, среди которых осуществляется поиск</param>
        /// <param name="startTime">Время, начиная с которого можно использовать курьера</param>
        /// <param name="bestPathInfo">Путь с минимальной стоимостью</param>
        /// <returns>0 - путь найден; иначе - путь не найден</returns>
        public static int FindSalesmanProblemSolutionEx(CourierEx courier, CourierDeliveryInfo[] deliveredOrders, double[,] pairDist, int[,] permutations, DateTime startTime, out CourierDeliveryInfo bestPathInfo)
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
                    int index1 = deliveredOrders[i].ShippingOrder.Number;

                    for (int j = i + 1; j < count; j++)
                    {
                        int index2 = deliveredOrders[j].ShippingOrder.Number;
                        double d = pairDist[index1, index2];
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

        /// <summary>
        /// Поиск первого допустимого пути среди заданных перестановок
        /// </summary>
        /// <param name="courier">Курьер</param>
        /// <param name="deliveredOrders">Доставляемые заказы</param>
        /// <param name="pairDist">Попарные расстояния между точками вручения заказов</param>
        /// <param name="permutations">Перестановки, среди которых осуществляется поиск</param>
        /// <param name="startTime">Время, начиная с которого можно использовать курьера</param>
        /// <param name="bestPathInfo">Путь с минимальной стоимостью</param>
        /// <returns>0 - путь найден; иначе - путь не найден</returns>
        public static int FindSalesmanProblemSolutionFast(CourierEx courier, CourierDeliveryInfo[] deliveredOrders, double[,] pairDist, int[,] permutations, DateTime startTime, out CourierDeliveryInfo bestPathInfo)
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
                    int index1 = deliveredOrders[i].ShippingOrder.Number;

                    for (int j = i + 1; j < count; j++)
                    {
                        int index2 = deliveredOrders[j].ShippingOrder.Number;
                        double d = pairDist[index1, index2];
                        pointDist[i, j] = d;
                        pointDist[j, i] = d;
                    }
                }

                // 4. Решаем задачу комивояжера - построение пути обхода c минимальной стоимостью
                rc = 4;
                int permutationCount = permutations.GetLength(0);
                CourierDeliveryInfo[] deliveryPath = new CourierDeliveryInfo[count];
                int[] orderIndex = new int[count];

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
                        bestPathInfo = pathInfo;
                        break;
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

        /// <summary>
        /// Поиск отгрузки с минимальной средней стоимостью доставки заказа для заданных заказов
        /// </summary>
        /// <param name = "courier">Курьер осуществляющий доставку</param>
        /// <param name = "solutionDeliveryInfo">Доступные для отгрузки заказы</param>
        /// <param name = "pairDist">Попарные расстояния между точками доставки заказов</param>
        /// <param name = "allDeliveries">Все известные отгрузки</param >
        /// <param name = "permutationsRepository">Перестановки</param >
        /// <param name = "delivery">Найденная отгрузка</param >
        /// <returns>0 - решение построено; иначе - решение не построено</returns>
        public static int FindBestDelivery(CourierEx courier, 
                      CourierDeliveryInfo[] solutionDeliveryInfo,
                      double[,] pairDist,
                      Dictionary<ulong, CourierDeliveryInfo> allDeliveries,
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
                if (courier == null)
                    return rc;
                if (solutionDeliveryInfo == null || solutionDeliveryInfo.Length <= 0)
                    return rc;
                if (solutionDeliveryInfo.Length > FloatSolutionParameters.MAX_ORDERS_FOR_COVER_SOLUTION)
                    return rc;
                if (pairDist == null || pairDist.Length <= 0)
                    return rc;
                if (allDeliveries == null || allDeliveries.Count <= 0)
                    return rc;
                if (permutationsRepository == null)
                    return rc;

                // 3.  Выбор наилучшей отгрузки отдельно для такси и курьеров магазина
                rc = 3;
                int orderCount = solutionDeliveryInfo.Length;
                CourierDeliveryInfo[] selectedDeliveryInfo = new CourierDeliveryInfo[orderCount];
                int[] solutionDeliveryInfoIndex = solutionDeliveryInfo.Select(p => p.ShippingOrder.Number).ToArray();
                int[] orderIndex = new int[orderCount];
                int[] subOrderIndex = new int[orderCount];

                // 4. Строим все комбинации из заказов, проверяем возможность их доставки в одной отгрузке и отбираем наилучшую отгрузку
                rc = 4;
                int maxValue = PARTITION_LOOP_MAX_VALUE[orderCount];
                CourierDeliveryInfo salesmanSolution;
                int deliveryOrderCount = 0;

                for (int i = 1; i <= maxValue; i++)
                {
                    // 4.1 Генерация очередного подмножества из заказов
                    rc = 41;
                    deliveryOrderCount = 0;

                    if ((i & 0x1) != 0)
                    {
                        orderIndex[deliveryOrderCount] = solutionDeliveryInfoIndex[0];
                        selectedDeliveryInfo[deliveryOrderCount++] = solutionDeliveryInfo[0];
                    }
                    if (orderCount > 1)
                    {
                        if ((i & 0x2) != 0)
                        {
                            orderIndex[deliveryOrderCount] = solutionDeliveryInfoIndex[1];
                            selectedDeliveryInfo[deliveryOrderCount++] = solutionDeliveryInfo[1];
                        }
                        if (orderCount > 2)
                        {
                            if ((i & 0x4) != 0)
                            {
                                orderIndex[deliveryOrderCount] = solutionDeliveryInfoIndex[2];
                                selectedDeliveryInfo[deliveryOrderCount++] = solutionDeliveryInfo[2];
                            }
                            if (orderCount > 3)
                            {
                                if ((i & 0x8) != 0)
                                {
                                    orderIndex[deliveryOrderCount] = solutionDeliveryInfoIndex[3];
                                    selectedDeliveryInfo[deliveryOrderCount++] = solutionDeliveryInfo[3];
                                }
                                if (orderCount > 4)
                                {
                                    if ((i & 0x10) != 0)
                                    {
                                        orderIndex[deliveryOrderCount] = solutionDeliveryInfoIndex[4];
                                        selectedDeliveryInfo[deliveryOrderCount++] = solutionDeliveryInfo[4];
                                    }
                                    if (orderCount > 5)
                                    {
                                        if ((i & 0x20) != 0)
                                        {
                                            orderIndex[deliveryOrderCount] = solutionDeliveryInfoIndex[5];
                                            selectedDeliveryInfo[deliveryOrderCount++] = solutionDeliveryInfo[5];
                                        }
                                        if (orderCount > 6)
                                        {
                                            if ((i & 0x40) != 0)
                                            {
                                                orderIndex[deliveryOrderCount] = solutionDeliveryInfoIndex[6];
                                                selectedDeliveryInfo[deliveryOrderCount++] = solutionDeliveryInfo[6];
                                            }
                                            if (orderCount > 7)
                                            {
                                                if ((i & 0x80) != 0)
                                                {
                                                    orderIndex[deliveryOrderCount] = solutionDeliveryInfoIndex[7];
                                                    selectedDeliveryInfo[deliveryOrderCount++] = solutionDeliveryInfo[7];
                                                }
                                                if (orderCount > 8)
                                                {
                                                    if ((i & 0x100) != 0)
                                                    {
                                                        orderIndex[deliveryOrderCount] = solutionDeliveryInfoIndex[8];
                                                        selectedDeliveryInfo[deliveryOrderCount++] = solutionDeliveryInfo[8];
                                                    }
                                                    if (orderCount > 9)
                                                    {
                                                        if ((i & 0x200) != 0)
                                                        {
                                                            orderIndex[deliveryOrderCount] = solutionDeliveryInfoIndex[9];
                                                            selectedDeliveryInfo[deliveryOrderCount++] = solutionDeliveryInfo[9];
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

                    // 4.2 Если в отгрузке больше 8 заказов
                    rc = 42;
                    if (deliveryOrderCount > 8)
                        continue;

                    // 4.3 Если отгрузка уже встречалась
                    rc = 43;
                    ulong key = Helper.GetDeliveryKey(orderIndex, deliveryOrderCount);
                    CourierDeliveryInfo savedDelivery;
                    if (allDeliveries.TryGetValue(key, out savedDelivery))
                    {
                        if (savedDelivery != null)
                        {
                            if (delivery == null || savedDelivery.OrderCost < delivery.OrderCost)
                                delivery = savedDelivery;
                        }
                        continue;
                    }

                    // 4.4 Проверяем, что отгрузки за вычетом одного заказа являются допустимыми
                    rc = 44;
                    int n = deliveryOrderCount - 1;

                    // первые n - 1
                    ulong keyX = Helper.GetDeliveryKey(orderIndex, n);
                    if (!allDeliveries.ContainsKey(keyX))
                        continue;

                    // последние n - 1
                    Array.Copy(orderIndex, 1, subOrderIndex, 0, n);
                    keyX = Helper.GetDeliveryKey(subOrderIndex, n);
                    if (!allDeliveries.ContainsKey(keyX))
                        continue;

                    // отброшен заказ из промежутка индексов [1, n - 1]

                    for (int j = 1; j < n; j++)
                    {
                        Array.Copy(orderIndex, 0, subOrderIndex, 0, j);
                        Array.Copy(orderIndex, j + 1, subOrderIndex, j, n - j);
                        keyX = Helper.GetDeliveryKey(subOrderIndex, n);
                        if (!allDeliveries.ContainsKey(keyX))
                            goto NextSubset;
                    }

                    // 4.5 Проверяем новую, ранее не встречавшуюся отгрузку
                    rc = 45;
                    //if (deliveryOrderCount == 8)
                    //{
                    //    rc = rc;
                    //}

                    int[,] permutations = permutationsRepository.GetPermutations(deliveryOrderCount);
                    CourierDeliveryInfo[] testOrders = new CourierDeliveryInfo[deliveryOrderCount];
                    Array.Copy(selectedDeliveryInfo, testOrders, deliveryOrderCount);
                    DateTime startDelevery = testOrders.Max(p => p.StartDeliveryInterval);
                    rc1 = FindSalesmanProblemSolutionEx(courier, testOrders, pairDist, permutations, startDelevery, out salesmanSolution);
                    if (rc1 == 0 && salesmanSolution != null)
                    {
                        allDeliveries.Add(key, salesmanSolution);
                        if (delivery == null || salesmanSolution.OrderCost < delivery.OrderCost)
                            salesmanSolution = savedDelivery;
                    }
                    else
                    {
                        allDeliveries.Add(key, null);
                    }

                    NextSubset:;
                }

                if (delivery == null)
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
    }
}
