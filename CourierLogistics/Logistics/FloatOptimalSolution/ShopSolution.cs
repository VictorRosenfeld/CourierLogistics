
namespace CourierLogistics.Logistics.FloatOptimalSolution
{
    using CourierLogistics.Logistics.FloatSolution;
    using CourierLogistics.Logistics.FloatSolution.CourierInLive;
    using CourierLogistics.Logistics.FloatSolution.OrdersDeliverySolution;
    using CourierLogistics.Logistics.FloatSolution.ShopInLive;
    using CourierLogistics.SourceData.Couriers;
    using CourierLogistics.SourceData.Orders;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class ShopSolution
    {
        /// <summary>
        /// Создание отгрузок, которые не могут быть улучшены
        /// (не являются частью других возможных отгрузок)
        /// </summary>
        /// <param name="courier">Курьер, с помощью которого осуществляется доставка</param>
        /// <param name="deliveryInfo">Информация об отгрузках, для которых осуществляется построение</param>
        /// <param name="orderPairMap"></param>
        /// <returns></returns>
        public static int Create(CourierEx courier, CourierDeliveryInfo[] deliveryInfo, out CourierDeliveryInfo[] bestDelivery)
        {
            // 1. Инициализация
            int rc = 1;
            bestDelivery = null;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (courier == null)
                    return rc;
                if (deliveryInfo == null || deliveryInfo.Length <= 1)
                    return rc;

                // 3. Расчитываем попарные расстояния между точками
                rc = 3;
                int orderCount = deliveryInfo.Length;
                double[,] orderPairDist = new double[orderCount, orderCount];

                for (int i = 0; i < orderCount; i++)
                {
                    Order order1 = deliveryInfo[i].ShippingOrder;
                    order1.Number = i;
                    double latitude1 = order1.Latitude;
                    double longitide1 = order1.Longitude;

                    for (int j = i + 1; j < orderCount; j++)
                    {
                        Order order2 = deliveryInfo[j].ShippingOrder;
                        double dist = FloatSolutionParameters.DISTANCE_ALLOWANCE * Helper.Distance(latitude1, longitide1, order2.Latitude, order2.Longitude);
                        orderPairDist[i, j] = dist;
                        orderPairDist[j, i] = dist;
                    }
                }

                // 4. Выделяем память под результат
                rc = 4;
                bool[,] orderPairMap = new bool[orderCount, orderCount];

                // 5. Цикл проверки пар на совместимость
                rc = 5;
                DateTime[] deliveryTimeLimit = new DateTime[4];
                deliveryTimeLimit[0] = DateTime.MaxValue;
                deliveryTimeLimit[3] = DateTime.MaxValue;
                double[] betweenDistance = new double[4];

                for (int i = 0; i < orderCount; i++)
                {
                    // 5.1 Выбираем первый заказ пары
                    rc = 51;
                    CourierDeliveryInfo delivInfo1 = deliveryInfo[i];
                    Order order1 = delivInfo1.ShippingOrder;
                    DateTime assembledTime1 = order1.Date_collected;

                    for (int j = 0; j < orderCount; j++)
                    {
                        // 5.2 Выбираем второй заказ пары
                        rc = 52;
                        CourierDeliveryInfo delivInfo2 = deliveryInfo[j];
                        Order order2 = delivInfo2.ShippingOrder;
                        DateTime assembledTime2 = order2.Date_collected;

                        // 5.3 Устанавливаем время отгрузки (время расчетов)
                        rc = 53;
                        DateTime modelTime;

                        if (assembledTime1 <= assembledTime2)
                        {
                            if (assembledTime2 > delivInfo1.EndDeliveryInterval)
                                continue;
                            modelTime = assembledTime2;
                        }
                        else
                        {
                            if (assembledTime1 > delivInfo2.EndDeliveryInterval)
                                continue;
                            modelTime = assembledTime1;
                        }

                        // 5.4 Проверяем путь магазин -> заказ1 -> заказ2
                        rc = 54;
                        double totalWeight = order1.Weight + order2.Weight;
                        betweenDistance[1] = delivInfo1.DistanceFromShop;
                        betweenDistance[2] = orderPairDist[i,j];
                        betweenDistance[3] = delivInfo2.DistanceFromShop;
                        deliveryTimeLimit[1] = order1.DeliveryTimeLimit;
                        deliveryTimeLimit[2] = order2.DeliveryTimeLimit;

                        CourierDeliveryInfo pairDeliveryInfo;
                        double[] nodeDeliveryTime;
                        int rc1 = courier.DeliveryCheck(modelTime, betweenDistance, deliveryTimeLimit, totalWeight, out pairDeliveryInfo, out nodeDeliveryTime);
                        if (rc1 == 0)
                            //orderPairMap[i, j] = pairDeliveryInfo;
                            orderPairMap[i, j] = true;

                        // 5.5 Проверяем путь магазин -> заказ2 -> заказ1
                        rc = 55;
                        betweenDistance[1] = delivInfo2.DistanceFromShop;
                        betweenDistance[2] = orderPairDist[j,i];
                        betweenDistance[3] = delivInfo1.DistanceFromShop;
                        deliveryTimeLimit[1] = order2.DeliveryTimeLimit;
                        deliveryTimeLimit[2] = order1.DeliveryTimeLimit;
                        rc1 = courier.DeliveryCheck(modelTime, betweenDistance, deliveryTimeLimit, totalWeight, out pairDeliveryInfo, out nodeDeliveryTime);
                        if (rc1 == 0)
                            //orderPairMap[j, i] = pairDeliveryInfo;
                            orderPairMap[j, i] = true;
                    }
                }

                // 6.Рекурсивное построения всех путей
                rc = 6;
                CourierDeliveryInfo[] result = new CourierDeliveryInfo[1500];
                int resultCount = 0;

                for (int i = 0; i < orderCount; i++)
                {
                    deliveryInfo[i].DeliveredOrders = new CourierDeliveryInfo[] { deliveryInfo[i] };
                    ContinuePath(courier, deliveryInfo[i], i + 1, deliveryInfo, orderPairMap, orderPairDist, ref result, ref resultCount);
                    deliveryInfo[i].DeliveredOrders = null;
                }

                // 7. Фильтрруем отгрузки
                //    (Отбрасываем отгрузки, которые являются частью других отгрузок)
                rc = 7;
                int rc2 = 77;
                if (deliveryInfo.Length <= 64)
                {
                    rc2 = FilterPath64(result, resultCount, out bestDelivery);
                }
                else if (deliveryInfo.Length <= 128)
                {
                    rc2 = FilterPath128(result, resultCount, out bestDelivery);
                }
                else
                {
                    rc2 = rc2;
                }

                if (rc2 != 0)
                    return rc = 100 * rc + rc2;


                // 8. Выход - Ok;
                rc = 0;
                return rc;
            }
            catch
            {
                return rc;
            }
        }

        /// <summary>
        /// Рекурсивное построение всех путей,
        /// которые можно нарастить, начиная
        /// от исходного пути
        /// </summary>
        /// <param name="courier">Курьер</param>
        /// <param name="parentDelivery">Исходный путь</param>
        /// <param name="startAddedIndex">Индекс заказа, начиная с которого происходит наращивание</param>
        /// <param name="deliveryInfo">Информация обо всех заказах</param>
        /// <param name="orderPairs">Все совместимые пары заказов</param>
        /// <param name="dist">Попарные расстояния между заказами</param>
        /// <param name="result">Результирующие пути</param>
        /// <param name="pathCount">Количество результирующик путей</param>
        /// <returns>0 - пути построены; иначе - пути не построены</returns>
        private static int ContinuePath(CourierEx courier, CourierDeliveryInfo parentDelivery, int startAddedIndex, CourierDeliveryInfo[] deliveryInfo, bool[,] orderPairs, double[,] dist, ref CourierDeliveryInfo[] result, ref int pathCount)
        {
            // 1. Инициализация
            int rc = 1;
            
            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (courier == null)
                    return rc;
                if (parentDelivery == null || 
                    parentDelivery.DeliveredOrders == null || 
                    parentDelivery.DeliveredOrders.Length <= 0)
                    return rc;

                int parentPathLength = parentDelivery.DeliveredOrders.Length;

                if (deliveryInfo == null || deliveryInfo.Length <= 0 || 
                    parentPathLength > deliveryInfo.Length)
                    return rc;
                if (startAddedIndex < 0)
                    return rc;
                if (result == null)
                    return rc;
                if (pathCount < 0)
                    return rc;

                // 3. Если путь не может быть длинее
                rc = 3;

                if (parentPathLength >= 8 || startAddedIndex >= deliveryInfo.Length)
                {
                    if (pathCount >= result.Length)
                    {
                        Array.Resize(ref result, result.Length + 1000);
                    }

                    result[pathCount++] = parentDelivery;
                    return rc = 0;
                }

                // 5. Наращиваем путь на один заказ и проверяет новый путь
                rc = 5;
                int continueCount = 0;
                DateTime parentModelTime = parentDelivery.StartDelivery;
                Order[] parentDeliveryOrder = parentDelivery.DeliveredOrders.Select(p => p.ShippingOrder).ToArray();
                CourierDeliveryInfo[] newPath = new CourierDeliveryInfo[parentPathLength + 1];
                parentDelivery.DeliveredOrders.CopyTo(newPath, 0);
                int[,] permutations = ShopEx.PermutationsRepository.GetPermutations(newPath.Length);

                for (int i = startAddedIndex; i < deliveryInfo.Length; i++)
                {
                    // 5.1 Извлекаем заказ, добавляемый к пути
                    rc = 51;
                    CourierDeliveryInfo addedDelivInfo = deliveryInfo[i];
                    Order addedOrder = addedDelivInfo.ShippingOrder;
                    DateTime orderModelTime = addedOrder.Date_collected;

                    // 5.2 Допустимые интервалы доставки должны пересекаться
                    rc = 52;
                    if (addedDelivInfo.StartDeliveryInterval > parentDelivery.EndDeliveryInterval)
                        continue;
                    if (addedDelivInfo.EndDeliveryInterval < parentDelivery.StartDeliveryInterval)
                        continue;

                    // 5.3 Все пары заказов с добавляемым элементом должны быть совместимы
                    rc = 53;
                    bool isOk = true;

                    for (int j = 0; j < parentPathLength; j++)
                    {
                        int parentOrderIndex = parentDeliveryOrder[j].Number;
                        //if (orderPairs[i, parentOrderIndex] == null && orderPairs[parentOrderIndex, i] == null)
                        if (!orderPairs[i, parentOrderIndex] && !orderPairs[parentOrderIndex, i])
                        {
                            isOk = false;
                            break;
                        }
                    }

                    if (!isOk)
                        continue;

                    // 5.4 Проверяем наращенный путь
                    rc = 54;
                    newPath[parentPathLength] = addedDelivInfo;
                    DateTime modelTime = parentDelivery.StartDelivery;
                    if (addedDelivInfo.StartDelivery > modelTime) modelTime = addedDelivInfo.StartDelivery;

                    CourierDeliveryInfo bestPath;
                    int rc1 = DeliverySolution.FindSalesmanProblemSolutionEx(courier, newPath, dist, permutations, modelTime, out bestPath);
                    if (rc1 == 0 && bestPath != null)
                    {
                        continueCount++;

                        // 5.5. Пытаемся нарастить следующие звенья
                        rc = 55;
                        ContinuePath(courier, bestPath, i + 1, deliveryInfo, orderPairs, dist, ref result, ref pathCount);
                    }
                }

                // 6. Если новые звенья нарастить не удалось
                rc = 6;
                if (continueCount <= 0)
                {
                    if (pathCount >= result.Length)
                    {
                        Array.Resize(ref result, result.Length + 1000);
                    }

                    result[pathCount++] = parentDelivery;
                }

                // 7. Выход - OK
                rc = 0;
                return rc;
            }
            catch
            {
                return rc;
            }
        }

        /// <summary>
        /// Отбрасывание отгрузок, которые являются
        /// частью других отгрузок
        /// (Ограничение - число заказов не более 64)
        /// </summary>
        /// <param name="result">Фильтрруемые отгрузки</param>
        /// <param name="resultCount">Число фильтруемых отгрузок</param>
        /// <param name="filteredResult">Результат фильтрации</param>
        /// <returns>0 - отгрузки отфильтрованы; иначе - отгрузки не отфильтрованы</returns>
        private static int FilterPath64(CourierDeliveryInfo[] result, int resultCount, out CourierDeliveryInfo[] filteredResult)
        {
            // 1. Инициализация
            int rc = 1;
            filteredResult = null;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (result == null || result.Length <= 0)
                    return rc;
                if (resultCount <= 0 || resultCount > result.Length)
                    return rc;

                // 3. Строим маску для степеней двойки
                rc = 3;
                ulong[] bitMask = new ulong[64];
                ulong bit = 1;

                for (int i = 0; i < bitMask.Length; i++, bit <<= 1)
                {
                    bitMask[i] = bit;
                }

                // 4. Строим маски путей
                rc = 4;
                ulong[] pathMask = new ulong[resultCount];

                for (int i = 0; i < resultCount; i++)
                {
                    CourierDeliveryInfo[] path = result[i].DeliveredOrders;

                    if (path != null && path.Length > 0)
                    {

                        ulong mask = 0;

                        for (int j = 0; j < path.Length; j++)
                        {
                            mask |= bitMask[path[j].ShippingOrder.Number];
                        }

                        pathMask[i] = mask;
                    }
                    else
                    {
                        pathMask[i] = bitMask[result[i].ShippingOrder.Number];
                    }
                }

                // 5. Помечаем все пути, которые являются частью дркгих путей
                rc = 5;
                bool[] isSubPath = new bool[resultCount];

                for (int i = 0; i < resultCount; i++)
                {
                    ulong mask1 = pathMask[i];

                    for (int j = i + 1; j < resultCount; j++)
                    {
                        ulong mask2 =  pathMask[j];
                        ulong conj = mask1 & mask2;
                        if (mask1 == conj)
                        {
                            isSubPath[i] = true;
                        }
                        else if (mask2 == conj)
                        {
                            isSubPath[j] = true;
                        }
                    }
                }

                // 6. Отбрасываем подпути
                rc = 5;
                filteredResult = new CourierDeliveryInfo[resultCount];
                int count = 0;

                for (int i = 0; i < resultCount; i++)
                {
                    if (!isSubPath[i])
                    {
                        filteredResult[count++] = result[i];
                    }
                }

                if (count < filteredResult.Length)
                {
                    Array.Resize(ref filteredResult, count);
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
        /// Отбрасывание отгрузок, которые являются
        /// частью других отгрузок
        /// (Ограничение - число заказов не более 128)
        /// </summary>
        /// <param name="result">Фильтрруемые отгрузки</param>
        /// <param name="resultCount">Число фильтруемых отгрузок</param>
        /// <param name="filteredResult">Результат фильтрации</param>
        /// <returns>0 - отгрузки отфильтрованы; иначе - отгрузки не отфильтрованы</returns>
        private static int FilterPath128(CourierDeliveryInfo[] result, int resultCount, out CourierDeliveryInfo[] filteredResult)
        {
            // 1. Инициализация
            int rc = 1;
            filteredResult = null;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (result == null || result.Length <= 0)
                    return rc;
                if (resultCount <= 0 || resultCount > result.Length)
                    return rc;

                // 3. Строим маску для степеней двойки
                rc = 3;
                ulong[] bitMask = new ulong[64];
                ulong bit = 1;

                for (int i = 0; i < bitMask.Length; i++, bit <<= 1)
                {
                    bitMask[i] = bit;
                }

                // 4. Строим маску пути длиной 128 бит (два ulong)
                rc = 4;
                ulong[] pathMask1 = new ulong[resultCount];  // старшие биты
                ulong[] pathMask2 = new ulong[resultCount];  // младшие биты

                for (int i = 0; i < resultCount; i++)
                {
                    CourierDeliveryInfo[] path = result[i].DeliveredOrders;

                    if (path != null && path.Length > 0)
                    {

                        ulong mask1 = 0;
                        ulong mask2 = 0;

                        for (int j = 0; j < path.Length; j++)
                        {
                            int n = path[j].ShippingOrder.Number;
                            if (n > 63)
                            {
                                mask1 |= bitMask[n - 64];
                            }
                            else
                            {
                                mask2 |= bitMask[n];
                            }

                        }

                        pathMask1[i] = mask1;
                        pathMask2[i] = mask2;
                    }
                    else
                    {
                        int n = result[i].ShippingOrder.Number;

                        if (n > 63)
                        {
                            pathMask1[i] = bitMask[n - 64];
                        }
                        else
                        {
                            pathMask2[i] = bitMask[n];
                        }
                    }
                }

                // 5. Помечаем все пути, которые являются частью дркгих путей
                rc = 5;
                bool[] isSubPath = new bool[resultCount];

                for (int i = 0; i < resultCount; i++)
                {
                    ulong mask1 = pathMask1[i];
                    ulong mask2 = pathMask2[i];

                    for (int j = i + 1; j < resultCount; j++)
                    {
                        ulong m1 =  pathMask1[j];
                        ulong m2 =  pathMask2[j];
                        ulong conj1 = mask1 & m1;
                        ulong conj2 = mask2 & m2;

                        if (mask1 == conj1 && mask2 == conj2)
                        {
                            isSubPath[i] = true;
                        }
                        else if (m1 == conj1 && m2 == conj2)
                        {
                            isSubPath[j] = true;
                        }
                    }
                }

                // 6. Отбрасываем подпути
                rc = 5;
                filteredResult = new CourierDeliveryInfo[resultCount];
                int count = 0;

                for (int i = 0; i < resultCount; i++)
                {
                    if (!isSubPath[i])
                    {
                        filteredResult[count++] = result[i];
                    }
                }

                if (count < filteredResult.Length)
                {
                    Array.Resize(ref filteredResult, count);
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
        /// Создание отгрузок, которые не могут быть улучшены
        /// (не являются частью других возможных отгрузок)
        /// </summary>
        /// <param name="courier">Курьер, с помощью которого осуществляется доставка</param>
        /// <param name="deliveryInfo">Информация об отгрузках, для которых осуществляется построение</param>
        /// <param name="orderPairMap"></param>
        /// <returns></returns>
        public static int CreateEx(CourierEx courier, CourierDeliveryInfo[] deliveryInfo, out CourierDeliveryInfo[] bestDelivery)
        {
            // 1. Инициализация
            int rc = 1;
            bestDelivery = null;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (courier == null)
                    return rc;
                if (deliveryInfo == null || deliveryInfo.Length <= 1)
                    return rc;

                // 3. Расчитываем попарные расстояния между точками
                rc = 3;
                int orderCount = deliveryInfo.Length;
                double[,] orderPairDist = new double[orderCount, orderCount];

                for (int i = 0; i < orderCount; i++)
                {
                    Order order1 = deliveryInfo[i].ShippingOrder;
                    order1.Number = i;
                    double latitude1 = order1.Latitude;
                    double longitide1 = order1.Longitude;

                    for (int j = i + 1; j < orderCount; j++)
                    {
                        Order order2 = deliveryInfo[j].ShippingOrder;
                        double dist = FloatSolutionParameters.DISTANCE_ALLOWANCE * Helper.Distance(latitude1, longitide1, order2.Latitude, order2.Longitude);
                        orderPairDist[i, j] = dist;
                        orderPairDist[j, i] = dist;
                    }
                }

                // 4. Выделяем под карту совместимых заказов (которые могут быть в одной отгрузке)
                rc = 4;
                CourierDeliveryInfo[,] orderPairMap = new CourierDeliveryInfo[orderCount, orderCount];

                // 5. Цикл построения совместимых пар
                rc = 5;
                DateTime[] deliveryTimeLimit = new DateTime[4];
                deliveryTimeLimit[0] = DateTime.MaxValue;
                deliveryTimeLimit[3] = DateTime.MaxValue;
                double[] betweenDistance = new double[4];
                Dictionary<ulong, CourierDeliveryInfo> deliveries = new Dictionary<ulong, CourierDeliveryInfo>(30 * orderCount);
                Dictionary<ulong, CourierDeliveryInfo> newDeliveries = new Dictionary<ulong, CourierDeliveryInfo>(orderCount * (orderCount - 1) / 2);

                for (int i = 0; i < orderCount; i++)
                {
                    // 5.1 Выбираем первый заказ пары
                    rc = 51;
                    CourierDeliveryInfo delivInfo1 = deliveryInfo[i];
                    Order order1 = delivInfo1.ShippingOrder;
                    DateTime assembledTime1 = order1.Date_collected;
                    bool isPair = false;

                    for (int j = i + 1; j < orderCount; j++)
                    {
                        // 5.2 Выбираем второй заказ пары
                        rc = 52;
                        CourierDeliveryInfo delivInfo2 = deliveryInfo[j];
                        Order order2 = delivInfo2.ShippingOrder;
                        DateTime assembledTime2 = order2.Date_collected;

                        // 5.3 Устанавливаем время отгрузки (время расчетов)
                        rc = 53;
                        DateTime modelTime;

                        if (assembledTime1 <= assembledTime2)
                        {
                            if (assembledTime2 > delivInfo1.EndDeliveryInterval)
                                continue;
                            modelTime = assembledTime2;
                        }
                        else
                        {
                            if (assembledTime1 > delivInfo2.EndDeliveryInterval)
                                continue;
                            modelTime = assembledTime1;
                        }

                        // 5.4 Проверяем путь магазин -> заказ1 -> заказ2
                        rc = 54;
                        double totalWeight = order1.Weight + order2.Weight;
                        betweenDistance[1] = delivInfo1.DistanceFromShop;
                        betweenDistance[2] = orderPairDist[i,j];
                        betweenDistance[3] = delivInfo2.DistanceFromShop;
                        deliveryTimeLimit[1] = order1.DeliveryTimeLimit;
                        deliveryTimeLimit[2] = order2.DeliveryTimeLimit;

                        CourierDeliveryInfo pairDeliveryInfo1;
                        CourierDeliveryInfo pairDeliveryInfo2;
                        double[] nodeDeliveryTime;
                        int rc1 = courier.DeliveryCheck(modelTime, betweenDistance, deliveryTimeLimit, totalWeight, out pairDeliveryInfo1, out nodeDeliveryTime);

                        betweenDistance[1] = delivInfo2.DistanceFromShop;
                        betweenDistance[2] = orderPairDist[j,i];
                        betweenDistance[3] = delivInfo1.DistanceFromShop;
                        deliveryTimeLimit[1] = order2.DeliveryTimeLimit;
                        deliveryTimeLimit[2] = order1.DeliveryTimeLimit;
                        int rc2 = courier.DeliveryCheck(modelTime, betweenDistance, deliveryTimeLimit, totalWeight, out pairDeliveryInfo2, out nodeDeliveryTime);

                        int caseNo = 0;
                        if (rc1 == 0 && pairDeliveryInfo1 != null) caseNo = 2;
                        if (rc2 == 0 && pairDeliveryInfo2 != null) caseNo++;

                        switch (caseNo)  // 1 2
                        {
                            //case 0:      // - -
                            //    break;
                            case 1:      // - +
                                isPair = true;
                                pairDeliveryInfo2.DeliveredOrders = new CourierDeliveryInfo[] { delivInfo2, delivInfo1 };
                                newDeliveries.Add(Helper.GetDeliveryKey(i, j), pairDeliveryInfo2);
                                break;
                            case 2:      // + -
                                isPair = true;
                                pairDeliveryInfo1.DeliveredOrders = new CourierDeliveryInfo[] { delivInfo1, delivInfo2 };
                                newDeliveries.Add(Helper.GetDeliveryKey(i, j), pairDeliveryInfo1);
                                break;
                            case 3:      // + +
                                isPair = true;
                                if (pairDeliveryInfo1.Cost <= pairDeliveryInfo1.Cost)
                                {
                                    pairDeliveryInfo1.DeliveredOrders = new CourierDeliveryInfo[] { delivInfo1, delivInfo2 };
                                    newDeliveries.Add(Helper.GetDeliveryKey(i, j), pairDeliveryInfo1);
                                }
                                else
                                {
                                    pairDeliveryInfo2.DeliveredOrders = new CourierDeliveryInfo[] { delivInfo2, delivInfo1 };
                                    newDeliveries.Add(Helper.GetDeliveryKey(i, j), pairDeliveryInfo2);
                                }
                                break;
                        }
                    }

                    if (!isPair)
                    {
                        deliveries.Add(Helper.GetDeliveryKey(i), delivInfo1);
                    }
                }

                // 6. Цикл наращивания отгрузок
                rc = 6;
                bool[] disabledOrder = new bool[orderCount];
                Dictionary<ulong, CourierDeliveryInfo> collection = new Dictionary<ulong, CourierDeliveryInfo>(10 * newDeliveries.Count);
                int nn = 0;
                int mm = 0;

                for (int iter = 3; iter <= 8; iter++)
                {
                    collection.Clear();
                    int[,] permutations = ShopEx.PermutationsRepository.GetPermutations(iter);
                    CourierDeliveryInfo[] newPath = new CourierDeliveryInfo[iter];
                    int[] newPathOrderIndex = new int[iter];
                    int[] orderIndex = new int[iter - 1];

                    foreach (KeyValuePair<ulong, CourierDeliveryInfo> kvp in newDeliveries)
                    {
                        // 6.1 Извлекаем очередной путь для наращивания
                        rc = 61;
                        CourierDeliveryInfo parentPath = kvp.Value;
                        parentPath.DeliveredOrders.CopyTo(newPath, 0);

                        // 6.1 Помечаем заказы входящие в наращиваемый путь
                        rc = 61;
                        Array.Clear(disabledOrder, 0, disabledOrder.Length);
                        int[] parentOrderIndex = parentPath.DeliveredOrders.Select(p => p.ShippingOrder.Number).ToArray();
                        //int maxIndex = -1;

                        for (int i = 0; i < orderIndex.Length; i++)
                        {
                            //if (orderIndex[i] > maxIndex)
                            //    maxIndex = orderIndex[i];
                            disabledOrder[parentOrderIndex[i]] = true;
                        }

                        // 6.2 Пытаемся нарастить отгрузку на один элемент
                        rc = 62;
                        bool isAugmented = false;

                        //for (int i = maxIndex + 1; i < orderCount; i++)
                        for (int i = 0; i < orderCount; i++)
                        {
                            // 6.3. Если заказ уже входит в отгрузку
                            rc = 63;
                            if (disabledOrder[i])
                                continue;

                            // 6.4 Замещаем по одному заказ из отгрузки на новый заказ
                            //     и проверяем что такой набор заказов представлен
                            rc = 64;
                            CourierDeliveryInfo addedDelivInfo = deliveryInfo[i];
                            int addedOrderIndex = addedDelivInfo.ShippingOrder.Number;
                            bool isPossible = true;

                            for (int j = 0; j < orderIndex.Length; j++)
                            {
                                parentOrderIndex.CopyTo(orderIndex, 0);
                                //int saveIndex = orderIndex[j];
                                orderIndex[j] = addedOrderIndex;
                                bool isContains = newDeliveries.ContainsKey(Helper.GetDeliveryKey(orderIndex));
                                //orderIndex[j] = saveIndex;

                                if (!isContains)
                                {
                                    isPossible = false;
                                    break;
                                }
                            }

                            if (isPossible)
                            {
                                // 6.5 Проверяем совместимый путь
                                rc = 65;
                                parentOrderIndex.CopyTo(newPathOrderIndex, 0);
                                newPathOrderIndex[newPathOrderIndex.Length - 1] = addedOrderIndex;
                                ulong key = Helper.GetDeliveryKey(newPathOrderIndex);
                                if (!collection.ContainsKey(key))
                                {
                                    DateTime modelTime = parentPath.StartDelivery;
                                    if (addedDelivInfo.StartDelivery > modelTime) modelTime = addedDelivInfo.StartDelivery;
                                    CourierDeliveryInfo bestPath;
                                    newPath[newPath.Length - 1] = addedDelivInfo;
                                    int rc1 = DeliverySolution.FindSalesmanProblemSolutionEx(courier, newPath, orderPairDist, permutations, modelTime, out bestPath);
                                    nn++;
                                    if (rc1 == 0 && bestPath != null)
                                    {
                                        // 6.6 Путь удалось нарастить
                                        rc = 66;
                                        isAugmented = true;
                                        collection.Add(key, bestPath);
                                    }
                                }
                                else
                                {
                                    // Путь уже был наращен
                                    isAugmented = true;
                                }
                            }
                        }

                        // 6.7. Если родительский путь не удалось нарастить
                        rc = 67;
                        if (!isAugmented)
                        {
                            deliveries.Add(kvp.Key, parentPath);
                        }
                    }

                    // 6.8 Переход к новой итерации
                    rc = 68;
                    if (collection.Count <= 0)
                        break;

                    newDeliveries.Clear();
                    foreach (KeyValuePair<ulong, CourierDeliveryInfo> kvp in collection)
                        newDeliveries.Add(kvp.Key, kvp.Value);
                }

                // 7. Создаём результат
                rc = 7;
                int count1 = deliveries.Count;
                int count2 = collection.Count;
                bestDelivery = new CourierDeliveryInfo[count1 + count2];
                if (count1 > 0)
                    deliveries.Values.CopyTo(bestDelivery, 0);
                if (count2 > 0)
                    collection.Values.CopyTo(bestDelivery, count1);

                // 8. Выход - Ok;
                rc = 0;
                return rc;
            }
            catch
            {
                return rc;
            }
        }

        /// <summary>
        /// Создание отгрузок, которые не могут быть улучшены
        /// (не являются частью других возможных отгрузок)
        /// </summary>
        /// <param name="courier">Курьер, с помощью которого осуществляется доставка</param>
        /// <param name="deliveryInfo">Информация об отгрузках, для которых осуществляется построение</param>
        /// <param name="orderPairMap"></param>
        /// <returns></returns>
        public static int CreateEz(CourierEx courier, CourierDeliveryInfo[] deliveryInfo, out CourierDeliveryInfo[] bestDelivery)
        {
            // 1. Инициализация
            int rc = 1;
            bestDelivery = null;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (courier == null)
                    return rc;
                if (deliveryInfo == null || deliveryInfo.Length <= 1)
                    return rc;

                // 3. Расчитываем попарные расстояния между точками
                rc = 3;
                int orderCount = deliveryInfo.Length;
                double[,] orderPairDist = new double[orderCount, orderCount];

                for (int i = 0; i < orderCount; i++)
                {
                    Order order1 = deliveryInfo[i].ShippingOrder;
                    order1.Number = i;
                    double latitude1 = order1.Latitude;
                    double longitide1 = order1.Longitude;

                    for (int j = i + 1; j < orderCount; j++)
                    {
                        Order order2 = deliveryInfo[j].ShippingOrder;
                        double dist = FloatSolutionParameters.DISTANCE_ALLOWANCE * Helper.Distance(latitude1, longitide1, order2.Latitude, order2.Longitude);
                        orderPairDist[i, j] = dist;
                        orderPairDist[j, i] = dist;
                    }
                }

                // 4. Выделяем под карту совместимых заказов (которые могут быть в одной отгрузке)
                rc = 4;
                CourierDeliveryInfo[,] orderPairMap = new CourierDeliveryInfo[orderCount, orderCount];

                // 5. Цикл построения совместимых пар
                rc = 5;
                DateTime[] deliveryTimeLimit = new DateTime[4];
                deliveryTimeLimit[0] = DateTime.MaxValue;
                deliveryTimeLimit[3] = DateTime.MaxValue;
                double[] betweenDistance = new double[4];
                Dictionary<ulong, CourierDeliveryInfo> deliveries = new Dictionary<ulong, CourierDeliveryInfo>(30 * orderCount);
                Dictionary<ulong, CourierDeliveryInfo> newDeliveries = new Dictionary<ulong, CourierDeliveryInfo>(orderCount * (orderCount - 1) / 2);

                for (int i = 0; i < orderCount; i++)
                {
                    // 5.1 Выбираем первый заказ пары
                    rc = 51;
                    CourierDeliveryInfo delivInfo1 = deliveryInfo[i];
                    Order order1 = delivInfo1.ShippingOrder;
                    DateTime assembledTime1 = order1.Date_collected;
                    bool isPair = false;

                    for (int j = i + 1; j < orderCount; j++)
                    {
                        // 5.2 Выбираем второй заказ пары
                        rc = 52;
                        CourierDeliveryInfo delivInfo2 = deliveryInfo[j];
                        Order order2 = delivInfo2.ShippingOrder;
                        DateTime assembledTime2 = order2.Date_collected;

                        // 5.3 Устанавливаем время отгрузки (время расчетов)
                        rc = 53;
                        DateTime modelTime;

                        if (assembledTime1 <= assembledTime2)
                        {
                            if (assembledTime2 > delivInfo1.EndDeliveryInterval)
                                continue;
                            modelTime = assembledTime2;
                        }
                        else
                        {
                            if (assembledTime1 > delivInfo2.EndDeliveryInterval)
                                continue;
                            modelTime = assembledTime1;
                        }

                        // 5.4 Проверяем путь магазин -> заказ1 -> заказ2
                        rc = 54;
                        double totalWeight = order1.Weight + order2.Weight;
                        betweenDistance[1] = delivInfo1.DistanceFromShop;
                        betweenDistance[2] = orderPairDist[i,j];
                        betweenDistance[3] = delivInfo2.DistanceFromShop;
                        deliveryTimeLimit[1] = order1.DeliveryTimeLimit;
                        deliveryTimeLimit[2] = order2.DeliveryTimeLimit;

                        CourierDeliveryInfo pairDeliveryInfo1;
                        CourierDeliveryInfo pairDeliveryInfo2;
                        double[] nodeDeliveryTime;
                        int rc1 = courier.DeliveryCheck(modelTime, betweenDistance, deliveryTimeLimit, totalWeight, out pairDeliveryInfo1, out nodeDeliveryTime);

                        betweenDistance[1] = delivInfo2.DistanceFromShop;
                        betweenDistance[2] = orderPairDist[j,i];
                        betweenDistance[3] = delivInfo1.DistanceFromShop;
                        deliveryTimeLimit[1] = order2.DeliveryTimeLimit;
                        deliveryTimeLimit[2] = order1.DeliveryTimeLimit;
                        int rc2 = courier.DeliveryCheck(modelTime, betweenDistance, deliveryTimeLimit, totalWeight, out pairDeliveryInfo2, out nodeDeliveryTime);

                        int caseNo = 0;
                        if (rc1 == 0 && pairDeliveryInfo1 != null) caseNo = 2;
                        if (rc2 == 0 && pairDeliveryInfo2 != null) caseNo++;

                        switch (caseNo)  // 1 2
                        {
                            //case 0:      // - -
                            //    break;
                            case 1:      // - +
                                isPair = true;
                                pairDeliveryInfo2.DeliveredOrders = new CourierDeliveryInfo[] { delivInfo2, delivInfo1 };
                                newDeliveries.Add(Helper.GetDeliveryKey(i, j), pairDeliveryInfo2);
                                break;
                            case 2:      // + -
                                isPair = true;
                                pairDeliveryInfo1.DeliveredOrders = new CourierDeliveryInfo[] { delivInfo1, delivInfo2 };
                                newDeliveries.Add(Helper.GetDeliveryKey(i, j), pairDeliveryInfo1);
                                break;
                            case 3:      // + +
                                isPair = true;
                                if (pairDeliveryInfo1.Cost <= pairDeliveryInfo1.Cost)
                                {
                                    pairDeliveryInfo1.DeliveredOrders = new CourierDeliveryInfo[] { delivInfo1, delivInfo2 };
                                    newDeliveries.Add(Helper.GetDeliveryKey(i, j), pairDeliveryInfo1);
                                }
                                else
                                {
                                    pairDeliveryInfo2.DeliveredOrders = new CourierDeliveryInfo[] { delivInfo2, delivInfo1 };
                                    newDeliveries.Add(Helper.GetDeliveryKey(i, j), pairDeliveryInfo2);
                                }
                                break;
                        }
                    }

                    if (!isPair)
                    {
                        deliveries.Add(Helper.GetDeliveryKey(i), delivInfo1);
                    }
                }

                // 6. Цикл наращивания отгрузок
                rc = 6;
                bool[] disabledOrder = new bool[orderCount];
                Dictionary<ulong, CourierDeliveryInfo> collection = new Dictionary<ulong, CourierDeliveryInfo>(10 * newDeliveries.Count);
                Dictionary<ulong, bool> countZ = new Dictionary<ulong, bool>(100000);
                int nn = 0;
                int mm = 0;
                int kk = 0;
                int jj = 0;
                int pp = 0;

                for (int iter = 3; iter <= 8; iter++)
                {
                    collection.Clear();
                    int[,] permutations = ShopEx.PermutationsRepository.GetPermutations(iter);
                    CourierDeliveryInfo[] newPath = new CourierDeliveryInfo[iter];
                    int[] newPathOrderIndex = new int[iter];
                    int[] orderIndex = new int[iter - 1];
                    countZ.Clear();

                    foreach (KeyValuePair<ulong, CourierDeliveryInfo> kvp in newDeliveries)
                    {
                        // 6.1 Извлекаем очередной путь для наращивания
                        rc = 61;
                        CourierDeliveryInfo parentPath = kvp.Value;
                        parentPath.DeliveredOrders.CopyTo(newPath, 0);

                        // 6.1 Помечаем заказы входящие в наращиваемый путь
                        rc = 61;
                        Array.Clear(disabledOrder, 0, disabledOrder.Length);
                        int[] parentOrderIndex = parentPath.DeliveredOrders.Select(p => p.ShippingOrder.Number).ToArray();
                        //int maxIndex = -1;

                        for (int i = 0; i < orderIndex.Length; i++)
                        {
                            //if (orderIndex[i] > maxIndex)
                            //    maxIndex = orderIndex[i];
                            disabledOrder[parentOrderIndex[i]] = true;
                        }

                        // 6.2 Пытаемся нарастить отгрузку на один элемент
                        rc = 62;
                        bool isAugmented = false;

                        //for (int i = maxIndex + 1; i < orderCount; i++)
                        for (int i = 0; i < orderCount; i++)
                        {
                            // 6.3. Если заказ уже входит в отгрузку
                            rc = 63;
                            if (disabledOrder[i])
                                continue;

                            kk++;

                            // 6.4 Замещаем по одному заказ из отгрузки на новый заказ
                            //     и проверяем что такой набор заказов представлен
                            rc = 64;
                            CourierDeliveryInfo addedDelivInfo = deliveryInfo[i];
                            int addedOrderIndex = addedDelivInfo.ShippingOrder.Number;
                            bool isPossible = true;

                            parentOrderIndex.CopyTo(newPathOrderIndex, 0);
                            newPathOrderIndex[newPathOrderIndex.Length - 1] = addedOrderIndex;
                            ulong key = Helper.GetDeliveryKey(newPathOrderIndex);
                            bool flag;
                            if (countZ.TryGetValue(key, out flag))
                            {
                                mm++;
                                if (flag)
                                    isAugmented = true;
                                continue;
                            }

                            for (int j = 0; j < orderIndex.Length; j++)
                            {
                                parentOrderIndex.CopyTo(orderIndex, 0);
                                //int saveIndex = orderIndex[j];
                                orderIndex[j] = addedOrderIndex;
                                bool isContains = newDeliveries.ContainsKey(Helper.GetDeliveryKey(orderIndex));
                                //orderIndex[j] = saveIndex;

                                if (!isContains)
                                {
                                    isPossible = false;
                                    break;
                                }
                            }

                            if (isPossible)
                            {
                                // 6.5 Проверяем совместимый путь
                                rc = 65;
                                if (!collection.ContainsKey(key))
                                {
                                    nn++;
                                    CourierDeliveryInfo specialPath;
                                    int rc5 = FindSpecialPath(courier, parentPath, addedDelivInfo, orderPairDist, out specialPath);
                                    if (rc5 == 0)
                                    {
                                        specialPath.ShippingOrder = addedDelivInfo.ShippingOrder;
                                        pp++;
                                        isAugmented = true;
                                        collection.Add(key, specialPath);
                                        countZ.Add(key, true);
                                    }
                                    else
                                    {

                                        DateTime modelTime = parentPath.StartDelivery;
                                        if (addedDelivInfo.StartDelivery > modelTime)
                                            modelTime = addedDelivInfo.StartDelivery;
                                        CourierDeliveryInfo bestPath;
                                        newPath[newPath.Length - 1] = addedDelivInfo;
                                        int rc1 = DeliverySolution.FindSalesmanProblemSolutionFast(courier, newPath, orderPairDist, permutations, modelTime, out bestPath);
                                        if (rc1 == 0 && bestPath != null)
                                        {
                                            bestPath.ShippingOrder = addedDelivInfo.ShippingOrder;
                                            jj++;
                                            // 6.6 Путь удалось нарастить
                                            rc = 66;
                                            isAugmented = true;
                                            collection.Add(key, bestPath);
                                            countZ.Add(key, true);
                                        }
                                    }
                                }
                                else
                                {
                                    // Путь уже был наращен
                                    isAugmented = true;
                                }
                            }
                            else
                            {
                                countZ.Add(key, false);
                            }
                        }

                        // 6.7. Если родительский путь не удалось нарастить
                        rc = 67;
                        if (!isAugmented)
                        {
                            deliveries.Add(kvp.Key, parentPath);
                        }
                    }

                    // 6.8 Переход к новой итерации
                    rc = 68;
                    if (collection.Count <= 0)
                        break;

                    newDeliveries.Clear();
                    foreach (KeyValuePair<ulong, CourierDeliveryInfo> kvp in collection)
                        newDeliveries.Add(kvp.Key, kvp.Value);
                }

                // 7. Создаём результат
                rc = 7;
                int count1 = deliveries.Count;
                int count2 = collection.Count;
                bestDelivery = new CourierDeliveryInfo[count1 + count2];
                if (count1 > 0)
                    deliveries.Values.CopyTo(bestDelivery, 0);
                if (count2 > 0)
                    collection.Values.CopyTo(bestDelivery, count1);

                // 8. Выход - Ok;
                rc = 0;
                return rc;
            }
            catch
            {
                return rc;
            }
        }

        private static int FindSpecialPath(CourierEx courier, CourierDeliveryInfo possiblePath, CourierDeliveryInfo addedOrder, double[,] orderDist, out CourierDeliveryInfo specialPath)
        {
            // 1. Инициализация
            int rc = 1;
            specialPath = null;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (courier == null)
                    return rc;
                if (possiblePath == null || possiblePath.OrderCount <= 0)
                    return rc;
                if (addedOrder == null)
                    return rc;
                if (orderDist == null || orderDist.Length <= 0)
                    return rc;

                // 3. Подставляем последовательно новый заказ между всеми заказами допустимой отгрузки и проверяем путь
                rc = 3;
                int possibleOrderCount = possiblePath.OrderCount;
                int size = possibleOrderCount + 3;
                DateTime[] deliveryTimeLimit = new DateTime[size];
                deliveryTimeLimit[0] = DateTime.MaxValue;
                deliveryTimeLimit[size - 1] = DateTime.MaxValue;
                double[] betweenDistance = new double[size];
                double totalWeight = possiblePath.Weight + addedOrder.Weight;
                DateTime modelTime = possiblePath.StartDelivery;
                if (addedOrder.StartDelivery > modelTime)
                    modelTime = addedOrder.StartDelivery;
                DateTime[] possibleDeliveryLimit = possiblePath.DeliveredOrders.Select(p => p.ShippingOrder.DeliveryTimeLimit).ToArray();
                int addedOrderIndex = addedOrder.ShippingOrder.Number;

                int size1 = size - 1;
                int size2 = size - 2;

                for (int i = 1; i < size - 1; i++)
                {
                    if (i == 1)
                    {
                        // 3.1 Добавляемый заказ - первый в отгрузке
                        rc = 31;
                        deliveryTimeLimit[1] = addedOrder.ShippingOrder.DeliveryTimeLimit;
                        possibleDeliveryLimit.CopyTo(deliveryTimeLimit, 2);
                        betweenDistance[1] = addedOrder.DistanceFromShop;
                        betweenDistance[2] = orderDist[addedOrderIndex, possiblePath.DeliveredOrders[0].ShippingOrder.Number];
                        Array.Copy(possiblePath.NodeDistance, 1, betweenDistance, 3, possibleOrderCount);
                    }
                    else if (i == size2)
                    {
                        // 3.2 Добавляемый заказ - последний в отгрузке
                        rc = 32;
                        deliveryTimeLimit[size - 2] = addedOrder.ShippingOrder.DeliveryTimeLimit;
                        possibleDeliveryLimit.CopyTo(deliveryTimeLimit, 1);
                        possiblePath.NodeDistance.CopyTo(betweenDistance, 0);
                        betweenDistance[size - 2] = orderDist[addedOrderIndex, possiblePath.DeliveredOrders[possibleOrderCount - 1].ShippingOrder.Number];
                        betweenDistance[size - 1] = addedOrder.DistanceFromShop;
                    }
                    else
                    {
                        // 3.3 Добавляемый заказ - внутри исходного пути
                        rc = 33;
                        Array.Copy(possibleDeliveryLimit, 0, deliveryTimeLimit, 1, i - 1);
                        deliveryTimeLimit[i] = addedOrder.ShippingOrder.DeliveryTimeLimit;
                        Array.Copy(possibleDeliveryLimit, i - 1, deliveryTimeLimit, i + 1, possibleOrderCount - i + 1);

                        Array.Copy(possiblePath.NodeDistance, 1, betweenDistance, 1, i - 1);
                        betweenDistance[i] = orderDist[addedOrderIndex, possiblePath.DeliveredOrders[i - 2].ShippingOrder.Number];
                        betweenDistance[i + 1] = orderDist[addedOrderIndex, possiblePath.DeliveredOrders[i - 1].ShippingOrder.Number];
                        Array.Copy(possiblePath.NodeDistance, i + 1, betweenDistance, i + 2, possibleOrderCount - i + 1);
                    }

                    // 3.4 Проверяем путь
                    rc = 34;
                    double[] nodeDeliveryTime;
                    CourierDeliveryInfo extendedPath;

                    int rc1 = courier.DeliveryCheck(modelTime, betweenDistance, deliveryTimeLimit, totalWeight, out extendedPath, out nodeDeliveryTime);
                    if (rc1 == 0 && extendedPath != null)
                    {
                        CourierDeliveryInfo[] newpath = new CourierDeliveryInfo[possibleOrderCount + 1];
                        if (i == 1)
                        {
                            newpath[0] = addedOrder;
                            possiblePath.DeliveredOrders.CopyTo(newpath, 1);
                        }
                        else if (i == size2)
                        {
                            newpath[newpath.Length - 1] = addedOrder;
                            possiblePath.DeliveredOrders.CopyTo(newpath, 0);
                        }
                        else
                        {
                            Array.Copy(possiblePath.DeliveredOrders, 0, newpath, 0, i - 1);
                            newpath[i - 1] = addedOrder;
                            Array.Copy(possiblePath.DeliveredOrders, i - 1, newpath, i, possibleOrderCount - i + 1);
                        }

                        extendedPath.DeliveredOrders = newpath;
                        specialPath = extendedPath;
                        return rc = 0;
                    }
                }

                // 4. Выход - специальный путь не найден
                rc = 4;
                return rc;
            }
            catch
            {
                return rc;
            }
        }

        private static int FindSpecialPath(CourierEx courier, Dictionary<ulong, CourierDeliveryInfo> savedDeliveries, CourierDeliveryInfo possibleDelivery, CourierDeliveryInfo addedOrder, double[,] orderDist, out CourierDeliveryInfo specialPath)
        {
            // 1. Инициализация
            int rc = 1;
            specialPath = null;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (courier == null)
                    return rc;
                if (savedDeliveries == null)
                    return rc;
                if (possibleDelivery == null || possibleDelivery.OrderCount <= 0)
                    return rc;
                if (addedOrder == null)
                    return rc;
                if (orderDist == null || orderDist.Length <= 0)
                    return rc;

                // 3. Замещаем по одному заказ из возможной отгрузки на новый заказ
                //    и проверяем что такой набор заказов представлен
                rc = 3;
                int addedOrderIndex = addedOrder.ShippingOrder.Number;
                CourierDeliveryInfo[] possiblePath = possibleDelivery.DeliveredOrders;
                int[] possiblePathOrderIndex = possiblePath.Select(p => p.ShippingOrder.Number).ToArray();
                int[] checkedPathOrderIndex = new int[possiblePathOrderIndex.Length];
                CourierDeliveryInfo[] parentPath = new CourierDeliveryInfo[possiblePath.Length + 1];
                CourierDeliveryInfo[] orderAddedToParentPath = new CourierDeliveryInfo[possiblePath.Length + 1];
                parentPath[0] = possibleDelivery;
                orderAddedToParentPath[0] = addedOrder;

                for (int i = 0; i < possiblePathOrderIndex.Length; i++)
                {
                    // 3.1 Строим ключ отгрузки, в которой замещен один заказ
                    rc = 31;
                    possiblePathOrderIndex.CopyTo(checkedPathOrderIndex, 0);
                    checkedPathOrderIndex[i] = addedOrderIndex;
                    ulong deliveryKey = Helper.GetDeliveryKey(checkedPathOrderIndex);
                    CourierDeliveryInfo existingDelivery;

                    // 3.2 Если отгрузки с построенным ключом не существует, то расширенная отгрузка не является допустимой
                    rc = 32;
                    if (!savedDeliveries.TryGetValue(deliveryKey, out existingDelivery))
                        return rc;

                    // 3.3 Сохраняем пару (замещенный заказ, существующая отгрузка)
                    rc = 33;
                    orderAddedToParentPath[i + 1] = possiblePath[i];
                    parentPath[i + 1] = existingDelivery;
                }

                // 4. Попытка найти допустимый путь среди специальных путей
                rc = 4;
                int possibleOrderCount = possibleDelivery.OrderCount;
                int size = possibleOrderCount + 3;
                int size1 = size - 1;
                int size2 = size - 2;
                DateTime[] deliveryTimeLimit = new DateTime[size];
                deliveryTimeLimit[0] = DateTime.MaxValue;
                deliveryTimeLimit[size - 1] = DateTime.MaxValue;
                double[] betweenDistance = new double[size];
                double totalWeight = possibleDelivery.Weight + addedOrder.Weight;
                DateTime modelTime = possibleDelivery.StartDelivery;
                if (addedOrder.StartDelivery > modelTime)
                    modelTime = addedOrder.StartDelivery;

                for (int k = 0; k < possibleOrderCount; k++)
                {
                    // 4.1 Извлекаем пару (замещенный заказ, существующая отгрузка)
                    rc = 41;
                    CourierDeliveryInfo parentDelivery = parentPath[k];
                    CourierDeliveryInfo addedDelivery = orderAddedToParentPath[k];

                    // 4.2 Подставляем последовательно добавляемый заказ между всеми заказами допустимой отгрузки и проверяем путь
                    rc = 42;
                    DateTime[] parentDeliveryLimit = parentDelivery.DeliveredOrders.Select(p => p.ShippingOrder.DeliveryTimeLimit).ToArray();
                    addedOrderIndex = addedDelivery.ShippingOrder.Number;

                    for (int i = 1; i < size - 1; i++)
                    {
                        if (i == 1)
                        {
                            // 4.3 Добавляемый заказ - первый в отгрузке
                            rc = 43;
                            deliveryTimeLimit[1] = addedDelivery.ShippingOrder.DeliveryTimeLimit;
                            parentDeliveryLimit.CopyTo(deliveryTimeLimit, 2);
                            betweenDistance[1] = addedDelivery.DistanceFromShop;
                            betweenDistance[2] = orderDist[addedOrderIndex, parentDelivery.DeliveredOrders[0].ShippingOrder.Number];
                            Array.Copy(parentDelivery.NodeDistance, 1, betweenDistance, 3, possibleOrderCount);
                        }
                        else if (i == size2)
                        {
                            // 4.4 Добавляемый заказ - последний в отгрузке
                            rc = 44;
                            deliveryTimeLimit[size - 2] = addedDelivery.ShippingOrder.DeliveryTimeLimit;
                            parentDeliveryLimit.CopyTo(deliveryTimeLimit, 1);
                            parentDelivery.NodeDistance.CopyTo(betweenDistance, 0);
                            betweenDistance[size - 2] = orderDist[addedOrderIndex, parentDelivery.DeliveredOrders[possibleOrderCount - 1].ShippingOrder.Number];
                            betweenDistance[size - 1] = addedDelivery.DistanceFromShop;
                        }
                        else
                        {
                            // 4.5 Добавляемый заказ - внутри исходного пути
                            rc = 45;
                            Array.Copy(parentDeliveryLimit, 0, deliveryTimeLimit, 1, i - 1);
                            deliveryTimeLimit[i] = addedDelivery.ShippingOrder.DeliveryTimeLimit;
                            Array.Copy(parentDeliveryLimit, i - 1, deliveryTimeLimit, i + 1, possibleOrderCount - i + 1);

                            Array.Copy(parentDelivery.NodeDistance, 1, betweenDistance, 1, i - 1);
                            betweenDistance[i] = orderDist[addedOrderIndex, parentDelivery.DeliveredOrders[i - 2].ShippingOrder.Number];
                            betweenDistance[i + 1] = orderDist[addedOrderIndex, parentDelivery.DeliveredOrders[i - 1].ShippingOrder.Number];
                            Array.Copy(parentDelivery.NodeDistance, i + 1, betweenDistance, i + 2, possibleOrderCount - i + 1);
                        }

                        // 4.6 Проверяем путь
                        rc = 46;
                        double[] nodeDeliveryTime;
                        CourierDeliveryInfo extendedPath;

                        int rc1 = courier.DeliveryCheck(modelTime, betweenDistance, deliveryTimeLimit, totalWeight, out extendedPath, out nodeDeliveryTime);
                        if (rc1 == 0 && extendedPath != null)
                        {
                            CourierDeliveryInfo[] newpath = new CourierDeliveryInfo[possibleOrderCount + 1];
                            if (i == 1)
                            {
                                newpath[0] = addedDelivery;
                                parentDelivery.DeliveredOrders.CopyTo(newpath, 1);
                            }
                            else if (i == size2)
                            {
                                newpath[newpath.Length - 1] = addedDelivery;
                                parentDelivery.DeliveredOrders.CopyTo(newpath, 0);
                            }
                            else
                            {
                                Array.Copy(parentDelivery.DeliveredOrders, 0, newpath, 0, i - 1);
                                newpath[i - 1] = addedDelivery;
                                Array.Copy(parentDelivery.DeliveredOrders, i - 1, newpath, i, possibleOrderCount - i + 1);
                            }

                            extendedPath.DeliveredOrders = newpath;
                            extendedPath.ShippingOrder = addedOrder.ShippingOrder;
                            specialPath = extendedPath;
                            return rc = 0;
                        }
                    }
                }

                // 5. Выход - специальный путь не найден
                rc = 5;
                return rc;
            }
            catch
            {
                return rc;
            }
        }

        /// <summary>
        /// Создание отгрузок, которые не могут быть улучшены
        /// (не являются частью других возможных отгрузок)
        /// </summary>
        /// <param name="courier">Курьер, с помощью которого осуществляется доставка</param>
        /// <param name="deliveryInfo">Информация об отгрузках, для которых осуществляется построение</param>
        /// <param name="orderPairMap"></param>
        /// <returns></returns>
        public static int CreateEv(CourierEx courier, CourierDeliveryInfo[] deliveryInfo, out CourierDeliveryInfo[] bestDelivery)
        {
            // 1. Инициализация
            int rc = 1;
            bestDelivery = null;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (courier == null)
                    return rc;
                if (deliveryInfo == null || deliveryInfo.Length <= 1)
                    return rc;

                // 3. Расчитываем попарные расстояния между точками
                rc = 3;
                int orderCount = deliveryInfo.Length;
                double[,] orderPairDist = new double[orderCount, orderCount];

                for (int i = 0; i < orderCount; i++)
                {
                    Order order1 = deliveryInfo[i].ShippingOrder;
                    order1.Number = i;
                    double latitude1 = order1.Latitude;
                    double longitide1 = order1.Longitude;

                    for (int j = i + 1; j < orderCount; j++)
                    {
                        Order order2 = deliveryInfo[j].ShippingOrder;
                        double dist = FloatSolutionParameters.DISTANCE_ALLOWANCE * Helper.Distance(latitude1, longitide1, order2.Latitude, order2.Longitude);
                        orderPairDist[i, j] = dist;
                        orderPairDist[j, i] = dist;
                    }
                }

                //// 4. Выделяем под карту совместимых заказов (которые могут быть в одной отгрузке)
                //rc = 4;
                //CourierDeliveryInfo[,] orderPairMap = new CourierDeliveryInfo[orderCount, orderCount];

                // 5. Цикл построения совместимых пар
                rc = 5;
                DateTime[] deliveryTimeLimit = new DateTime[4];
                deliveryTimeLimit[0] = DateTime.MaxValue;
                deliveryTimeLimit[3] = DateTime.MaxValue;
                double[] betweenDistance = new double[4];
                Dictionary<ulong, CourierDeliveryInfo> deliveries = new Dictionary<ulong, CourierDeliveryInfo>(30 * orderCount);
                Dictionary<ulong, CourierDeliveryInfo> newDeliveries = new Dictionary<ulong, CourierDeliveryInfo>(orderCount * (orderCount - 1) / 2);
                bool[] pairOrder = new bool[orderCount];

                for (int i = 0; i < orderCount; i++)
                {
                    // 5.1 Выбираем первый заказ пары
                    rc = 51;
                    CourierDeliveryInfo delivInfo1 = deliveryInfo[i];
                    Order order1 = delivInfo1.ShippingOrder;
                    DateTime assembledTime1 = order1.Date_collected;
                    bool isPair = false;

                    for (int j = i + 1; j < orderCount; j++)
                    {
                        // 5.2 Выбираем второй заказ пары
                        rc = 52;
                        CourierDeliveryInfo delivInfo2 = deliveryInfo[j];
                        Order order2 = delivInfo2.ShippingOrder;
                        DateTime assembledTime2 = order2.Date_collected;

                        // 5.3 Устанавливаем время отгрузки (время расчетов)
                        rc = 53;
                        DateTime modelTime;

                        if (assembledTime1 <= assembledTime2)
                        {
                            if (assembledTime2 > delivInfo1.EndDeliveryInterval)
                                continue;
                            modelTime = assembledTime2;
                        }
                        else
                        {
                            if (assembledTime1 > delivInfo2.EndDeliveryInterval)
                                continue;
                            modelTime = assembledTime1;
                        }

                        // 5.4 Проверяем путь магазин -> заказ1 -> заказ2
                        rc = 54;
                        double totalWeight = order1.Weight + order2.Weight;
                        betweenDistance[1] = delivInfo1.DistanceFromShop;
                        betweenDistance[2] = orderPairDist[i,j];
                        betweenDistance[3] = delivInfo2.DistanceFromShop;
                        deliveryTimeLimit[1] = order1.DeliveryTimeLimit;
                        deliveryTimeLimit[2] = order2.DeliveryTimeLimit;

                        CourierDeliveryInfo pairDeliveryInfo1;
                        CourierDeliveryInfo pairDeliveryInfo2;
                        double[] nodeDeliveryTime;
                        int rc1 = courier.DeliveryCheck(modelTime, betweenDistance, deliveryTimeLimit, totalWeight, out pairDeliveryInfo1, out nodeDeliveryTime);

                        betweenDistance[1] = delivInfo2.DistanceFromShop;
                        betweenDistance[2] = orderPairDist[j,i];
                        betweenDistance[3] = delivInfo1.DistanceFromShop;
                        deliveryTimeLimit[1] = order2.DeliveryTimeLimit;
                        deliveryTimeLimit[2] = order1.DeliveryTimeLimit;
                        int rc2 = courier.DeliveryCheck(modelTime, betweenDistance, deliveryTimeLimit, totalWeight, out pairDeliveryInfo2, out nodeDeliveryTime);

                        int caseNo = 0;
                        if (rc1 == 0 && pairDeliveryInfo1 != null) caseNo = 2;
                        if (rc2 == 0 && pairDeliveryInfo2 != null) caseNo++;
                        isPair = false;

                        switch (caseNo)  // 1 2
                        {
                            //case 0:      // - -
                            //    break;
                            case 1:      // - +
                                isPair = true;
                                pairDeliveryInfo2.DeliveredOrders = new CourierDeliveryInfo[] { delivInfo2, delivInfo1 };
                                newDeliveries.Add(Helper.GetDeliveryKey(i, j), pairDeliveryInfo2);
                                break;
                            case 2:      // + -
                                isPair = true;
                                pairDeliveryInfo1.DeliveredOrders = new CourierDeliveryInfo[] { delivInfo1, delivInfo2 };
                                newDeliveries.Add(Helper.GetDeliveryKey(i, j), pairDeliveryInfo1);
                                break;
                            case 3:      // + +
                                isPair = true;
                                if (pairDeliveryInfo1.Cost <= pairDeliveryInfo2.Cost)
                                {
                                    pairDeliveryInfo1.DeliveredOrders = new CourierDeliveryInfo[] { delivInfo1, delivInfo2 };
                                    newDeliveries.Add(Helper.GetDeliveryKey(i, j), pairDeliveryInfo1);
                                }
                                else
                                {
                                    pairDeliveryInfo2.DeliveredOrders = new CourierDeliveryInfo[] { delivInfo2, delivInfo1 };
                                    newDeliveries.Add(Helper.GetDeliveryKey(i, j), pairDeliveryInfo2);
                                }
                                break;
                        }

                        if (isPair)
                        {
                            pairOrder[i] = true;
                            pairOrder[j] = true;
                        }
                    }


                        //pairOrder
                        //deliveries.Add(Helper.GetDeliveryKey(i), delivInfo1);
                    //}
                }

                for (int i = 0; i < orderCount; i++)
                {
                    if (!pairOrder[i])
                    {
                        CourierDeliveryInfo delivInfo = deliveryInfo[i];
                        deliveries.Add(Helper.GetDeliveryKey(i), delivInfo);
                    }
                }

                // 6. Цикл наращивания отгрузок
                rc = 6;
                bool[] disabledOrder = new bool[orderCount];
                Dictionary<ulong, CourierDeliveryInfo> extendedDeliveries = new Dictionary<ulong, CourierDeliveryInfo>(10 * newDeliveries.Count);
                Dictionary<ulong, bool> countZ = new Dictionary<ulong, bool>(100000);
                int nn = 0;
                int mm = 0;
                int kk = 0;
                int jj = 0;
                int pp = 0;
                int ss = 0;
                int tt = 0;

                for (int iter = 3; iter <= 8; iter++)
                {
                    extendedDeliveries.Clear();
                    int[,] permutations = ShopEx.PermutationsRepository.GetPermutations(iter);
                    CourierDeliveryInfo[] newPath = new CourierDeliveryInfo[iter];
                    int[] newPathOrderIndex = new int[iter];
                    int[] orderIndex = new int[iter - 1];
                    countZ.Clear();
                    int vv = 0;

                    foreach (KeyValuePair<ulong, CourierDeliveryInfo> kvp in newDeliveries)
                    {
                        vv++;
                        // 6.1 Извлекаем очередной путь для наращивания
                        rc = 61;
                        CourierDeliveryInfo parentPath = kvp.Value;
                        parentPath.DeliveredOrders.CopyTo(newPath, 0);

                        // 6.2 Помечаем заказы входящие в наращиваемый путь
                        rc = 62;
                        Array.Clear(disabledOrder, 0, disabledOrder.Length);
                        int[] parentOrderIndex = parentPath.DeliveredOrders.Select(p => p.ShippingOrder.Number).ToArray();

                        for (int i = 0; i < orderIndex.Length; i++)
                        {
                            disabledOrder[parentOrderIndex[i]] = true;
                        }

                        // 6.3 Пытаемся нарастить отгрузку на один элемент
                        rc = 63;
                        bool isExtended = false;
                        DateTime parentStartTime = parentPath.StartDelivery;


                        for (int i = 0; i < orderCount; i++)
                        {
                            // 6.4. Если заказ уже входит в отгрузку
                            rc = 64;
                            if (disabledOrder[i])
                                continue;

                            mm++;

                            // 6.5. Если расширенная отгрузка уже обработана
                            rc = 65;
                            CourierDeliveryInfo addedDelivInfo = deliveryInfo[i];
                            int addedOrderIndex = addedDelivInfo.ShippingOrder.Number;
                            parentOrderIndex.CopyTo(newPathOrderIndex, 0);

                            newPathOrderIndex[newPathOrderIndex.Length - 1] = addedOrderIndex;
                            ulong key = Helper.GetDeliveryKey(newPathOrderIndex);
                            bool flag;

                            if (countZ.TryGetValue(key, out flag))
                            {
                                nn++;
                                if (flag)
                                    isExtended = true;
                                continue;
                            }

                            // 6.6 Пытаемся найти специальный путь
                            rc = 66;
                            kk++;
                            CourierDeliveryInfo specialPath;
                            int rcs = FindSpecialPath(courier, newDeliveries, parentPath, addedDelivInfo, orderPairDist, out specialPath);
                            if (rcs == 32)
                            {
                                jj++;
                                countZ.Add(key, false);
                            }
                            else if (rcs == 0 && specialPath != null)
                            {
                                pp++;
                                isExtended = true;
                                extendedDeliveries.Add(key, specialPath);
                                countZ.Add(key, true);
                            }
                            else
                            {
                                // 6.7. Пытаемся найти общий путь
                                rc = 67;
                                DateTime modelTime = parentStartTime;
                                if (addedDelivInfo.StartDelivery > modelTime)
                                    modelTime = addedDelivInfo.StartDelivery;
                                CourierDeliveryInfo bestPath;
                                newPath[newPath.Length - 1] = addedDelivInfo;
                                ss++;
                                int rc1 = DeliverySolution.FindSalesmanProblemSolutionFast(courier, newPath, orderPairDist, permutations, modelTime, out bestPath);
                                if (rc1 == 0 && bestPath != null)
                                {
                                    bestPath.ShippingOrder = addedDelivInfo.ShippingOrder;
                                    tt++;
                                    isExtended = true;
                                    extendedDeliveries.Add(key, bestPath);
                                    countZ.Add(key, true);
                                }
                                else
                                {
                                    countZ.Add(key, false);
                                }
                            }
                        }

                        // 6.8. Если родительский путь не удалось нарастить
                        rc = 68;
                        if (!isExtended)
                        {
                            deliveries.Add(kvp.Key, parentPath);
                        }
                    }

                    // 6.8 Переход к новой итерации
                    rc = 68;
                    if (extendedDeliveries.Count <= 0)
                        break;

                    newDeliveries.Clear();
                    foreach (KeyValuePair<ulong, CourierDeliveryInfo> kvp in extendedDeliveries)
                        newDeliveries.Add(kvp.Key, kvp.Value);
                }

                // 7. Создаём результат
                rc = 7;
                int count1 = deliveries.Count;
                int count2 = extendedDeliveries.Count;
                bestDelivery = new CourierDeliveryInfo[count1 + count2];
                if (count1 > 0)
                    deliveries.Values.CopyTo(bestDelivery, 0);
                if (count2 > 0)
                    extendedDeliveries.Values.CopyTo(bestDelivery, count1);

                // 8. Выход - Ok;
                rc = 0;
                return rc;
            }
            catch
            {
                return rc;
            }
        }

        /// <summary>
        /// Создание отгрузок, покрывающих заданное множество заказов
        /// </summary>
        /// <param name="courier">Курьер, с помощью которого осуществляется доставка</param>
        /// <param name="deliveryInfo">Информация об заказах, для которых осуществляется построение</param>
        /// <param name="orderCover">Найденное покрытие</param>
        /// <returns></returns>
        public static int CreateOrderCover(CourierEx courier, CourierDeliveryInfo[] deliveryInfo, out CourierDeliveryInfo[] orderCover)
        {
            // 1. Инициализация
            int rc = 1;
            orderCover = null;
            bool[] sourceOrderCompleted = null;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (courier == null)
                    return rc;
                if (deliveryInfo == null || deliveryInfo.Length <= 1)
                    return rc;

                // 3. Сортируем отгрузки по времени начала допустимого интервала отгрузки
                rc = 3;
                Array.Sort(deliveryInfo, CompareByStartInterval);

                // 4. Расчитываем попарные расстояния между точками
                rc = 4;
                int orderCount = deliveryInfo.Length;
                double[,] orderPairDist = new double[orderCount, orderCount];

                for (int i = 0; i < orderCount; i++)
                {
                    Order order1 = deliveryInfo[i].ShippingOrder;
                    order1.Number = i;
                    double latitude1 = order1.Latitude;
                    double longitide1 = order1.Longitude;

                    for (int j = i + 1; j < orderCount; j++)
                    {
                        Order order2 = deliveryInfo[j].ShippingOrder;
                        double dist = FloatSolutionParameters.DISTANCE_ALLOWANCE * Helper.Distance(latitude1, longitide1, order2.Latitude, order2.Longitude);
                        orderPairDist[i, j] = dist;
                        orderPairDist[j, i] = dist;
                    }
                }

                // 5. Поиск хорошего покрытия
                rc = 5;
                sourceOrderCompleted = deliveryInfo.Select(p => p.Completed).ToArray();
                CourierDeliveryInfo[] selectedOrders = new CourierDeliveryInfo[deliveryInfo.Length];
                CourierDeliveryInfo[] cover = new CourierDeliveryInfo[deliveryInfo.Length];
                int coverCount = 0;

                
                while (true)
                {
                    CourierDeliveryInfo bestDelivery = null;
                    double bestOrderCost = double.MaxValue;
                    
                    for (int i = deliveryInfo.Length - 1; i >= 0; i--)
                    {
                        // 5.1 Выбираем неотгруженный базовый заказ
                        rc = 51;
                        CourierDeliveryInfo baseDelivery = deliveryInfo[i];
                        if (baseDelivery.Completed)
                            continue;

                        DateTime baseStartInterval = baseDelivery.StartDeliveryInterval;
                        int count = 0;

                        // 5.2 Отбираем заказы, которые могут быть отгружены совместно
                        rc = 52;

                        for (int j = i - 1; j >= 0; j--)
                        {
                            CourierDeliveryInfo delivInfo = deliveryInfo[i];
                            if (!delivInfo.Completed && deliveryInfo[i].EndDeliveryInterval >= baseStartInterval)
                            {
                                selectedOrders[count] = deliveryInfo[i];
                                if (++count >= FloatSolutionParameters.MAX_ORDERS_FOR_OPTIMAL_SOLUTION)
                                    break;
                            }
                        }

                        if (count <= 0)
                        {
                            baseDelivery.DeliveryCourier = courier;
                            baseDelivery.Completed = true;
                            cover[coverCount++] = baseDelivery;
                            if (baseDelivery.OrderCost < bestOrderCost)
                            {
                                bestOrderCost = baseDelivery.OrderCost;

                            }
                        }
                        else
                        {
                            CourierDeliveryInfo[] availableDelivery = selectedOrders.Take(count).ToArray();
                            CourierDeliveryInfo[] baseBestDelivery;
                            int rc1 = CreateEv(courier, availableDelivery, out baseBestDelivery);
                            if (rc1 == 0 && baseBestDelivery != null)
                            {

                            }
                        }



                    }



                }





                //// 4. Выделяем под карту совместимых заказов (которые могут быть в одной отгрузке)
                //rc = 4;
                //CourierDeliveryInfo[,] orderPairMap = new CourierDeliveryInfo[orderCount, orderCount];

                // 5. Цикл построения совместимых пар
                rc = 5;
                DateTime[] deliveryTimeLimit = new DateTime[4];
                deliveryTimeLimit[0] = DateTime.MaxValue;
                deliveryTimeLimit[3] = DateTime.MaxValue;
                double[] betweenDistance = new double[4];
                Dictionary<ulong, CourierDeliveryInfo> deliveries = new Dictionary<ulong, CourierDeliveryInfo>(30 * orderCount);
                Dictionary<ulong, CourierDeliveryInfo> newDeliveries = new Dictionary<ulong, CourierDeliveryInfo>(orderCount * (orderCount - 1) / 2);
                bool[] pairOrder = new bool[orderCount];

                for (int i = 0; i < orderCount; i++)
                {
                    // 5.1 Выбираем первый заказ пары
                    rc = 51;
                    CourierDeliveryInfo delivInfo1 = deliveryInfo[i];
                    Order order1 = delivInfo1.ShippingOrder;
                    DateTime assembledTime1 = order1.Date_collected;
                    bool isPair = false;

                    for (int j = i + 1; j < orderCount; j++)
                    {
                        // 5.2 Выбираем второй заказ пары
                        rc = 52;
                        CourierDeliveryInfo delivInfo2 = deliveryInfo[j];
                        Order order2 = delivInfo2.ShippingOrder;
                        DateTime assembledTime2 = order2.Date_collected;

                        // 5.3 Устанавливаем время отгрузки (время расчетов)
                        rc = 53;
                        DateTime modelTime;

                        if (assembledTime1 <= assembledTime2)
                        {
                            if (assembledTime2 > delivInfo1.EndDeliveryInterval)
                                continue;
                            modelTime = assembledTime2;
                        }
                        else
                        {
                            if (assembledTime1 > delivInfo2.EndDeliveryInterval)
                                continue;
                            modelTime = assembledTime1;
                        }

                        // 5.4 Проверяем путь магазин -> заказ1 -> заказ2
                        rc = 54;
                        double totalWeight = order1.Weight + order2.Weight;
                        betweenDistance[1] = delivInfo1.DistanceFromShop;
                        betweenDistance[2] = orderPairDist[i,j];
                        betweenDistance[3] = delivInfo2.DistanceFromShop;
                        deliveryTimeLimit[1] = order1.DeliveryTimeLimit;
                        deliveryTimeLimit[2] = order2.DeliveryTimeLimit;

                        CourierDeliveryInfo pairDeliveryInfo1;
                        CourierDeliveryInfo pairDeliveryInfo2;
                        double[] nodeDeliveryTime;
                        int rc1 = courier.DeliveryCheck(modelTime, betweenDistance, deliveryTimeLimit, totalWeight, out pairDeliveryInfo1, out nodeDeliveryTime);

                        betweenDistance[1] = delivInfo2.DistanceFromShop;
                        betweenDistance[2] = orderPairDist[j,i];
                        betweenDistance[3] = delivInfo1.DistanceFromShop;
                        deliveryTimeLimit[1] = order2.DeliveryTimeLimit;
                        deliveryTimeLimit[2] = order1.DeliveryTimeLimit;
                        int rc2 = courier.DeliveryCheck(modelTime, betweenDistance, deliveryTimeLimit, totalWeight, out pairDeliveryInfo2, out nodeDeliveryTime);

                        int caseNo = 0;
                        if (rc1 == 0 && pairDeliveryInfo1 != null) caseNo = 2;
                        if (rc2 == 0 && pairDeliveryInfo2 != null) caseNo++;
                        isPair = false;

                        switch (caseNo)  // 1 2
                        {
                            //case 0:      // - -
                            //    break;
                            case 1:      // - +
                                isPair = true;
                                pairDeliveryInfo2.DeliveredOrders = new CourierDeliveryInfo[] { delivInfo2, delivInfo1 };
                                newDeliveries.Add(Helper.GetDeliveryKey(i, j), pairDeliveryInfo2);
                                break;
                            case 2:      // + -
                                isPair = true;
                                pairDeliveryInfo1.DeliveredOrders = new CourierDeliveryInfo[] { delivInfo1, delivInfo2 };
                                newDeliveries.Add(Helper.GetDeliveryKey(i, j), pairDeliveryInfo1);
                                break;
                            case 3:      // + +
                                isPair = true;
                                if (pairDeliveryInfo1.Cost <= pairDeliveryInfo2.Cost)
                                {
                                    pairDeliveryInfo1.DeliveredOrders = new CourierDeliveryInfo[] { delivInfo1, delivInfo2 };
                                    newDeliveries.Add(Helper.GetDeliveryKey(i, j), pairDeliveryInfo1);
                                }
                                else
                                {
                                    pairDeliveryInfo2.DeliveredOrders = new CourierDeliveryInfo[] { delivInfo2, delivInfo1 };
                                    newDeliveries.Add(Helper.GetDeliveryKey(i, j), pairDeliveryInfo2);
                                }
                                break;
                        }

                        if (isPair)
                        {
                            pairOrder[i] = true;
                            pairOrder[j] = true;
                        }
                    }


                        //pairOrder
                        //deliveries.Add(Helper.GetDeliveryKey(i), delivInfo1);
                    //}
                }

                for (int i = 0; i < orderCount; i++)
                {
                    if (!pairOrder[i])
                    {
                        CourierDeliveryInfo delivInfo = deliveryInfo[i];
                        deliveries.Add(Helper.GetDeliveryKey(i), delivInfo);
                    }
                }

                // 6. Цикл наращивания отгрузок
                rc = 6;
                bool[] disabledOrder = new bool[orderCount];
                Dictionary<ulong, CourierDeliveryInfo> extendedDeliveries = new Dictionary<ulong, CourierDeliveryInfo>(10 * newDeliveries.Count);
                Dictionary<ulong, bool> countZ = new Dictionary<ulong, bool>(100000);
                int nn = 0;
                int mm = 0;
                int kk = 0;
                int jj = 0;
                int pp = 0;
                int ss = 0;
                int tt = 0;

                for (int iter = 3; iter <= 8; iter++)
                {
                    extendedDeliveries.Clear();
                    int[,] permutations = ShopEx.PermutationsRepository.GetPermutations(iter);
                    CourierDeliveryInfo[] newPath = new CourierDeliveryInfo[iter];
                    int[] newPathOrderIndex = new int[iter];
                    int[] orderIndex = new int[iter - 1];
                    countZ.Clear();
                    int vv = 0;

                    foreach (KeyValuePair<ulong, CourierDeliveryInfo> kvp in newDeliveries)
                    {
                        vv++;
                        // 6.1 Извлекаем очередной путь для наращивания
                        rc = 61;
                        CourierDeliveryInfo parentPath = kvp.Value;
                        parentPath.DeliveredOrders.CopyTo(newPath, 0);

                        // 6.2 Помечаем заказы входящие в наращиваемый путь
                        rc = 62;
                        Array.Clear(disabledOrder, 0, disabledOrder.Length);
                        int[] parentOrderIndex = parentPath.DeliveredOrders.Select(p => p.ShippingOrder.Number).ToArray();

                        for (int i = 0; i < orderIndex.Length; i++)
                        {
                            disabledOrder[parentOrderIndex[i]] = true;
                        }

                        // 6.3 Пытаемся нарастить отгрузку на один элемент
                        rc = 63;
                        bool isExtended = false;
                        DateTime parentStartTime = parentPath.StartDelivery;


                        for (int i = 0; i < orderCount; i++)
                        {
                            // 6.4. Если заказ уже входит в отгрузку
                            rc = 64;
                            if (disabledOrder[i])
                                continue;

                            mm++;

                            // 6.5. Если расширенная отгрузка уже обработана
                            rc = 65;
                            CourierDeliveryInfo addedDelivInfo = deliveryInfo[i];
                            int addedOrderIndex = addedDelivInfo.ShippingOrder.Number;
                            parentOrderIndex.CopyTo(newPathOrderIndex, 0);

                            newPathOrderIndex[newPathOrderIndex.Length - 1] = addedOrderIndex;
                            ulong key = Helper.GetDeliveryKey(newPathOrderIndex);
                            bool flag;

                            if (countZ.TryGetValue(key, out flag))
                            {
                                nn++;
                                if (flag)
                                    isExtended = true;
                                continue;
                            }

                            // 6.6 Пытаемся найти специальный путь
                            rc = 66;
                            kk++;
                            CourierDeliveryInfo specialPath;
                            int rcs = FindSpecialPath(courier, newDeliveries, parentPath, addedDelivInfo, orderPairDist, out specialPath);
                            if (rcs == 32)
                            {
                                jj++;
                                countZ.Add(key, false);
                            }
                            else if (rcs == 0 && specialPath != null)
                            {
                                pp++;
                                isExtended = true;
                                extendedDeliveries.Add(key, specialPath);
                                countZ.Add(key, true);
                            }
                            else
                            {
                                // 6.7. Пытаемся найти общий путь
                                rc = 67;
                                DateTime modelTime = parentStartTime;
                                if (addedDelivInfo.StartDelivery > modelTime)
                                    modelTime = addedDelivInfo.StartDelivery;
                                CourierDeliveryInfo bestPath;
                                newPath[newPath.Length - 1] = addedDelivInfo;
                                ss++;
                                int rc1 = DeliverySolution.FindSalesmanProblemSolutionFast(courier, newPath, orderPairDist, permutations, modelTime, out bestPath);
                                if (rc1 == 0 && bestPath != null)
                                {
                                    bestPath.ShippingOrder = addedDelivInfo.ShippingOrder;
                                    tt++;
                                    isExtended = true;
                                    extendedDeliveries.Add(key, bestPath);
                                    countZ.Add(key, true);
                                }
                                else
                                {
                                    countZ.Add(key, false);
                                }
                            }
                        }

                        // 6.8. Если родительский путь не удалось нарастить
                        rc = 68;
                        if (!isExtended)
                        {
                            deliveries.Add(kvp.Key, parentPath);
                        }
                    }

                    // 6.8 Переход к новой итерации
                    rc = 68;
                    if (extendedDeliveries.Count <= 0)
                        break;

                    newDeliveries.Clear();
                    foreach (KeyValuePair<ulong, CourierDeliveryInfo> kvp in extendedDeliveries)
                        newDeliveries.Add(kvp.Key, kvp.Value);
                }

                // 7. Создаём результат
                rc = 7;
                int count1 = deliveries.Count;
                int count2 = extendedDeliveries.Count;
                orderCover = new CourierDeliveryInfo[count1 + count2];
                if (count1 > 0)
                    deliveries.Values.CopyTo(orderCover, 0);
                if (count2 > 0)
                    extendedDeliveries.Values.CopyTo(orderCover, count1);

                // 8. Выход - Ok;
                rc = 0;
                return rc;
            }
            catch
            {
                return rc;
            }
            finally
            {
                if (sourceOrderCompleted != null)
                {
                    for (int i = 0; i < deliveryInfo.Length; i++)
                    {
                        deliveryInfo[i].Completed = sourceOrderCompleted[i];
                    }
                }
            }
        }

        /// <summary>
        /// Сравнение двух отгрузок по времени начала допустимого интервала отгрузки
        /// </summary>
        /// <param name="delivInfo1">Отгрузка 1</param>
        /// <param name="delivInfo2">Отгрузка 2</param>
        /// <returns>-1, 0, или 1</returns>
        private static int CompareByStartInterval(CourierDeliveryInfo delivInfo1, CourierDeliveryInfo delivInfo2)
        {
            if (delivInfo1.StartDeliveryInterval < delivInfo2.StartDeliveryInterval)
                return -1;
            if (delivInfo1.StartDeliveryInterval > delivInfo2.StartDeliveryInterval)
                return 1;
            return 0;
        }

        /// <summary>
        /// Создание отгрузок, покрывающих заданное множество заказов
        /// </summary>
        /// <param name="courier">Курьер, с помощью которого осуществляется доставка</param>
        /// <param name="deliveryInfo">Информация об заказах, для которых осуществляется построение</param>
        /// <param name="orderCover">Найденное покрытие</param>
        /// <returns>0 - покрытие найдено (отгрузки отсортированы по времени отгрузки); иначе - покрвтие не найдено</returns>
        public static int CreateOrderCoverEx(CourierEx courier, CourierDeliveryInfo[] deliveryInfo, out CourierDeliveryInfo[] orderCover)
        {
            // 1. Инициализация
            int rc = 1;
            orderCover = null;
            bool[] sourceOrderCompleted = null;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (courier == null)
                    return rc;
                if (deliveryInfo == null || deliveryInfo.Length <= 1)
                    return rc;

                // 3. Сортируем отгрузки по времени начала допустимого интервала отгрузки
                rc = 3;
                Array.Sort(deliveryInfo, CompareByStartInterval);

                // 4. Расчитываем попарные расстояния между точками
                rc = 4;
                int orderCount = deliveryInfo.Length;
                double[,] orderPairDist = new double[orderCount, orderCount];

                for (int i = 0; i < orderCount; i++)
                {
                    Order order1 = deliveryInfo[i].ShippingOrder;
                    order1.Number = i;
                    double latitude1 = order1.Latitude;
                    double longitide1 = order1.Longitude;

                    for (int j = i + 1; j < orderCount; j++)
                    {
                        Order order2 = deliveryInfo[j].ShippingOrder;
                        double dist = FloatSolutionParameters.DISTANCE_ALLOWANCE * Helper.Distance(latitude1, longitide1, order2.Latitude, order2.Longitude);
                        orderPairDist[i, j] = dist;
                        orderPairDist[j, i] = dist;
                    }
                }

                // 5. Инициализируем коллекцию допустимых отгрузок
                rc = 5;
                Dictionary<ulong, CourierDeliveryInfo> allDeliveries = new Dictionary<ulong, CourierDeliveryInfo>(500 * orderCount);

                for (int i = 0; i < orderCount; i++)
                {
                    CourierDeliveryInfo delivInfo = deliveryInfo[i];
                    delivInfo.DeliveryCourier = courier;
                    ulong key = Helper.GetDeliveryKey(delivInfo.ShippingOrder.Number);
                    allDeliveries.Add(key, delivInfo);
                }

                #region 6. Выделяем возможные отгрузки из двух заказов

                // 6. Выделяем возможные отгрузки из двух заказов
                rc = 6;
                DateTime[] deliveryTimeLimit = new DateTime[4];
                deliveryTimeLimit[0] = DateTime.MaxValue;
                deliveryTimeLimit[3] = DateTime.MaxValue;
                double[] betweenDistance = new double[4];

                for (int i = 0; i < orderCount; i++)
                {
                    // 6.1 Выбираем первый заказ пары
                    rc = 61;
                    CourierDeliveryInfo delivInfo1 = deliveryInfo[i];
                    Order order1 = delivInfo1.ShippingOrder;
                    DateTime assembledTime1 = order1.Date_collected;

                    for (int j = i + 1; j < orderCount; j++)
                    {
                        // 6.2 Выбираем второй заказ пары
                        rc = 62;
                        CourierDeliveryInfo delivInfo2 = deliveryInfo[j];
                        Order order2 = delivInfo2.ShippingOrder;
                        DateTime assembledTime2 = order2.Date_collected;

                        // 6.3 Устанавливаем время отгрузки (время расчетов)
                        rc = 63;
                        DateTime modelTime;

                        if (assembledTime1 <= assembledTime2)
                        {
                            if (assembledTime2 > delivInfo1.EndDeliveryInterval)
                                continue;
                            modelTime = assembledTime2;
                        }
                        else
                        {
                            if (assembledTime1 > delivInfo2.EndDeliveryInterval)
                                continue;
                            modelTime = assembledTime1;
                        }

                        // 6.4 Проверяем путь магазин -> заказ1 -> заказ2
                        rc = 64;
                        double totalWeight = order1.Weight + order2.Weight;
                        betweenDistance[1] = delivInfo1.DistanceFromShop;
                        betweenDistance[2] = orderPairDist[i, j];
                        betweenDistance[3] = delivInfo2.DistanceFromShop;
                        deliveryTimeLimit[1] = order1.DeliveryTimeLimit;
                        deliveryTimeLimit[2] = order2.DeliveryTimeLimit;

                        CourierDeliveryInfo pairDeliveryInfo1;
                        CourierDeliveryInfo pairDeliveryInfo2;
                        double[] nodeDeliveryTime;
                        int rc1 = courier.DeliveryCheck(modelTime, betweenDistance, deliveryTimeLimit, totalWeight, out pairDeliveryInfo1, out nodeDeliveryTime);

                        betweenDistance[1] = delivInfo2.DistanceFromShop;
                        betweenDistance[2] = orderPairDist[j, i];
                        betweenDistance[3] = delivInfo1.DistanceFromShop;
                        deliveryTimeLimit[1] = order2.DeliveryTimeLimit;
                        deliveryTimeLimit[2] = order1.DeliveryTimeLimit;
                        int rc2 = courier.DeliveryCheck(modelTime, betweenDistance, deliveryTimeLimit, totalWeight, out pairDeliveryInfo2, out nodeDeliveryTime);

                        int caseNo = 0;
                        if (rc1 == 0 && pairDeliveryInfo1 != null)
                            caseNo = 2;
                        if (rc2 == 0 && pairDeliveryInfo2 != null)
                            caseNo++;

                        switch (caseNo)  // 1 2
                        {
                            case 1:      // - +
                                pairDeliveryInfo2.DeliveredOrders = new CourierDeliveryInfo[] { delivInfo2, delivInfo1 };
                                allDeliveries.Add(Helper.GetDeliveryKey(i, j), pairDeliveryInfo2);
                                break;
                            case 2:      // + -
                                pairDeliveryInfo1.DeliveredOrders = new CourierDeliveryInfo[] { delivInfo1, delivInfo2 };
                                allDeliveries.Add(Helper.GetDeliveryKey(i, j), pairDeliveryInfo1);
                                break;
                            case 3:      // + +
                                if (pairDeliveryInfo1.Cost <= pairDeliveryInfo2.Cost)
                                {
                                    pairDeliveryInfo1.DeliveredOrders = new CourierDeliveryInfo[] { delivInfo1, delivInfo2 };
                                    allDeliveries.Add(Helper.GetDeliveryKey(i, j), pairDeliveryInfo1);
                                }
                                else
                                {
                                    pairDeliveryInfo2.DeliveredOrders = new CourierDeliveryInfo[] { delivInfo2, delivInfo1 };
                                    allDeliveries.Add(Helper.GetDeliveryKey(i, j), pairDeliveryInfo2);
                                }
                                break;
                        }
                    }
                }

                #endregion 6. Выделяем возможные отгрузки из двух заказов

                // 7. Построение покрытия
                rc = 7;
                sourceOrderCompleted = deliveryInfo.Select(p => p.Completed).ToArray();
                CourierDeliveryInfo[] selectedOrders = new CourierDeliveryInfo[deliveryInfo.Length];
                CourierDeliveryInfo[] cover = new CourierDeliveryInfo[deliveryInfo.Length];
                int coverCount = 0;

                while (true)
                {
                    // 7.1. Инициализируем итерацию (поиск очередной наилучшей отгрузки среди неотгруженных заказов)
                    rc = 71;
                    CourierDeliveryInfo iterBestDelivery = null;
                    double iterBestOrderCost = double.MaxValue;

                    for (int i = deliveryInfo.Length - 1; i >= 0; i--)
                    {
                        // 7.1 Выбираем неотгруженный базовый заказ, определяющий время отгрузки
                        rc = 71;
                        CourierDeliveryInfo baseDelivery = deliveryInfo[i];
                        if (baseDelivery.Completed)
                            continue;

                        DateTime baseStartInterval = baseDelivery.StartDeliveryInterval;
                        int count = 0;

                        // 7.2 Отбираем заказы, которые могут быть отгружены совместно с базовым заказом
                        rc = 72;
                        selectedOrders[count++] = baseDelivery;

                        for (int j = i - 1; j >= 0; j--)
                        {
                            CourierDeliveryInfo delivInfo = deliveryInfo[j];
                            if (!delivInfo.Completed && delivInfo.EndDeliveryInterval >= baseStartInterval)
                            {
                                if (allDeliveries.ContainsKey(Helper.GetDeliveryKey(j, i)))
                                {
                                    selectedOrders[count] = delivInfo;
                                    if (++count >= FloatSolutionParameters.MAX_ORDERS_FOR_COVER_SOLUTION)
                                        break;
                                }
                            }
                        }

                        // 7.3 Одиночная отгрузка ?
                        rc = 73;
                        if (count <= 1)
                        {
                            if (baseDelivery.OrderCost < iterBestOrderCost)
                            {
                                iterBestDelivery = baseDelivery;
                                iterBestOrderCost = baseDelivery.OrderCost;
                            }

                            continue;
                        }

                        // 7.4 Находим наилучшую отгрузку, связанную с базовой
                        rc = 74;
                        CourierDeliveryInfo[] solutionOrders = selectedOrders.Take(count).ToArray();
                        CourierDeliveryInfo solution;

                        int rc1 = DeliverySolution.FindBestDelivery(courier, solutionOrders, orderPairDist, allDeliveries, ShopEx.PermutationsRepository, out solution);
                        if (rc1 == 0 && solution != null)
                        {
                            if (solution.OrderCost < iterBestOrderCost)
                            {
                                iterBestDelivery = solution;
                                iterBestOrderCost = solution.OrderCost;
                            }
                        }
                    }

                    // 8. Если все заказы отгружены
                    rc = 8;
                    if (iterBestDelivery == null)
                        break;

                    // 9. Помечаем отгруженные закаказы
                    rc = 9;
                    cover[coverCount++] = iterBestDelivery;
                    iterBestDelivery.Completed = true;

                    if (iterBestDelivery.DeliveredOrders != null)
                    {
                        foreach (CourierDeliveryInfo delivInfo in iterBestDelivery.DeliveredOrders)
                        {
                            delivInfo.Completed = true;
                        }
                    }
                }

                // 10. Отрезаем свободный хвост и сортируем по времени отгрузки
                rc = 10;

                if (coverCount < cover.Length)
                {
                    Array.Resize(ref cover, coverCount);
                }

                Array.Sort(cover, CompareByStartInterval);

                orderCover = cover;

                // 11. Выход - Ok;
                rc = 0;
                return rc;
            }
            catch
            {
                return rc;
            }
            finally
            {
                if (sourceOrderCompleted != null)
                {
                    for (int i = 0; i < deliveryInfo.Length; i++)
                    {
                        //deliveryInfo[i].Completed = sourceOrderCompleted[i];
                        deliveryInfo[i].Completed = false;
                    }
                }
            }
        }


    }
}
