
namespace SQLCLR.DeliveryCover
{
    using SQLCLR.Couriers;
    using SQLCLR.Deliveries;
    using SQLCLR.Orders;
    using SQLCLR.Shops;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.SqlClient;

    /// <summary>
    /// Построение покрытия
    /// </summary>
    public class CreateCover
    {
        /// <summary>
        /// Построение покрытия для каждого магазина из
        /// ранее построенных допустимых отгрузок
        /// </summary>
        /// <param name="deliveries">Отгрузки, из которых строятся покрытия</param>
        /// <param name="allOrders">Активные заказы</param>
        /// <param name="allCouriers">Доступные курьеры и такси</param>
        /// <param name="connection">Открытое соединение с БД для полцчения гео-данных</param>
        /// <param name="recomendations">Построенные рекомендации</param>
        /// <param name="covers">Построенные покрытия</param>
        /// <param name="rejectedOrders">Причины отказов в доставке заказов</param>
        /// <returns>0 - покрытия построены; иначе - покрытия не построены</returns>
        public static int Create(CourierDeliveryInfo[] deliveries, AllOrders allOrders, AllCouriers allCouriers, SqlConnection connection,
                        out CourierDeliveryInfo[] recomendations, out CourierDeliveryInfo[] covers, out OrderRejectionCause[] rejectedOrders)
        {
            // 1. Инициализация
            int rc = 1;
            int rc1 = 1;
            recomendations = null;
            covers = null;
            rejectedOrders = null;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (deliveries == null || deliveries.Length <= 0)
                    return rc;
                if (allCouriers == null || !allCouriers.IsCreated)
                    return rc;
                if (allOrders == null || !allOrders.IsCreated)
                    return rc;
                if (connection == null || connection.State != ConnectionState.Open)
                    return rc;

                // 3. Сортируем отгрузки по магазину
                rc = 3;
                Array.Sort(deliveries, CompareDeliveriesByShopId);

                // 4. Обрабатываем каждый магазин в отдельности
                rc = 4;
                int startIndex = 0;
                int endIndex = 0;
                int currentShopId = deliveries[0].FromShop.Id;
                CourierDeliveryInfo[] shopRecomendations;
                CourierDeliveryInfo[] shopCover;
                Order[] rejectedShopOrders;
                DateTime calcTime = deliveries[0].CalculationTime;

                covers = new CourierDeliveryInfo[allOrders.Count];
                recomendations = new CourierDeliveryInfo[allOrders.Count];
                rejectedOrders = new OrderRejectionCause[allOrders.Count * allCouriers.BaseKeys.Length];

                int coverCount = 0;
                int recomendationCount = 0;
                int rejectedCount = 0;

                for (int i = 1; i < deliveries.Length; i++)
                {
                    if (deliveries[i].FromShop.Id == currentShopId)
                    {
                        endIndex = i;
                    }
                    else
                    {
                        // 4.1 Создаём репозиторий курьеров магазина
                        rc = 41;
                        Courier[] shopCouriers = allCouriers.GetShopCouriers(currentShopId);
                        CourierRepository courierRepository = new CourierRepository();
                        rc1 = courierRepository.Create(shopCouriers);
                        //if (rc1 != 0)
                        //    return rc = 1000 * rc + rc1;

                        // 4.2 Извлекаем отгрузки магазина
                        rc = 42;
                        CourierDeliveryInfo[] shopDeliveries = new CourierDeliveryInfo[endIndex - startIndex + 1];
                        Array.Copy(deliveries, startIndex, shopDeliveries, 0, shopDeliveries.Length);

                        // 4.3 Извлекаем заказы магазина
                        rc = 43;
                        Order[] shopOrders = allOrders.GetShopOrders(currentShopId);

                        // 4.4 Строим покрытие
                        rc = 44;
                        rc1 = CreateShopCover(shopDeliveries, shopOrders, courierRepository, out shopRecomendations, out shopCover, out rejectedShopOrders);
                        if (rc1 == 0)
                        {
                            // 4.5 Пополняем список рекомендаций
                            rc = 45;
                            if (shopRecomendations != null && shopRecomendations.Length > 0)
                            {
                                shopRecomendations.CopyTo(recomendations, recomendationCount);
                                recomendationCount += shopRecomendations.Length;
                            }

                            // 4.6 Пополняем покрытия
                            rc = 46;
                            if (shopCover != null && shopCover.Length > 0)
                            {
                                shopCover.CopyTo(covers, coverCount);
                                coverCount += shopCover.Length;
                            }

                            // 4.7 Пополняем заказы, которые не могут быть доставлены
                            rc = 47;
                            if (rejectedShopOrders != null && rejectedShopOrders.Length > 0)
                            {
                                OrderRejectionCause[] shopRejectionOrders;
                                rc1 = ShopOrderRejectionAnalyzer(calcTime, shopDeliveries[0].FromShop, shopOrders, courierRepository, connection, out shopRejectionOrders);
                                if (rc1 == 0)
                                {
                                    if (shopRejectionOrders != null && shopRejectionOrders.Length > 0)
                                    {
                                        shopRejectionOrders.CopyTo(rejectedOrders, rejectedCount);
                                        rejectedCount += shopRejectionOrders.Length;
                                    }
                                }
                            }
                        }

                        // 4.8 Переходим к следующему магазину
                        startIndex = i;
                        endIndex = i;
                        currentShopId = deliveries[i].FromShop.Id;
                    }
                }

                // Обрабатываем последний магазин

                // 4.9 Создаём репозиторий курьеров магазина
                rc = 49;
                Courier[] lastShopCouriers = allCouriers.GetShopCouriers(currentShopId);
                CourierRepository lastCourierRepository = new CourierRepository();
                rc1 = lastCourierRepository.Create(lastShopCouriers);

                // 4.10 Извлекаем отгрузки магазина
                rc = 410;
                CourierDeliveryInfo[] lastShopDeliveries = new CourierDeliveryInfo[endIndex - startIndex + 1];
                Array.Copy(deliveries, startIndex, lastShopDeliveries, 0, lastShopDeliveries.Length);

                // 4.11 Извлекаем заказы магазина
                rc = 411;
                Order[] lastShopOrders = allOrders.GetShopOrders(currentShopId);

                // 4.12 Строим покрытие
                rc = 412;
                rc1 = CreateShopCover(lastShopDeliveries, lastShopOrders, lastCourierRepository, out shopRecomendations, out shopCover, out rejectedShopOrders);
                if (rc1 == 0)
                {
                    // 4.12 Пополняем список рекомендаций
                    rc = 412;
                    if (shopRecomendations != null && shopRecomendations.Length > 0)
                    {
                        shopRecomendations.CopyTo(recomendations, recomendationCount);
                        recomendationCount += shopRecomendations.Length;
                    }

                    // 4.13 Пополняем покрытия
                    rc = 413;
                    if (shopCover != null && shopCover.Length > 0)
                    {
                        shopCover.CopyTo(covers, coverCount);
                        coverCount += shopCover.Length;
                    }

                    // 4.14 Пополняем заказы, которые не могут быть доставлены
                    rc = 414;
                    if (rejectedShopOrders != null && rejectedShopOrders.Length > 0)
                    {
                        OrderRejectionCause[] shopRejectionOrders;
                        rc1 = ShopOrderRejectionAnalyzer(calcTime, lastShopDeliveries[0].FromShop, lastShopOrders, lastCourierRepository, connection, out shopRejectionOrders);
                        if (rc1 == 0)
                        {
                            if (shopRejectionOrders != null && shopRejectionOrders.Length > 0)
                            {
                                shopRejectionOrders.CopyTo(rejectedOrders, rejectedCount);
                                rejectedCount += shopRejectionOrders.Length;
                            }
                        }
                    }
                }

                // 4.15 Усекаем неиспользованные части массивов
                rc = 15;

                if (recomendationCount < recomendations.Length)
                {
                    Array.Resize(ref recomendations, recomendationCount);
                }

                if (coverCount < covers.Length)
                {
                    Array.Resize(ref covers, coverCount);
                }

                if (rejectedCount < rejectedOrders.Length)
                {
                    Array.Resize(ref rejectedOrders, rejectedCount);
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
        /// Построение покрытия для магазина
        /// </summary>
        /// <param name="shopDeliveries">Допустимые отгрузки магазина, из которых строится покрытие</param>
        /// <param name="shopOrders">Все активные заказы магазина</param>
        /// <param name="shopCouriers">Все доступные курьеры и такси</param>
        /// <param name="recomendations">Отгрузки-рекомендации</param>
        /// <param name="cover">Отгрузки из собранных заказов, образующих покрытие</param>
        /// <param name="rejectedOrders">Заказы, которые не могут быть отгружены</param>
        /// <returns>0 - покрытие построено; иначе - покрытие не построено</returns>
        public static int CreateShopCover(CourierDeliveryInfo[] shopDeliveries, Order[] shopOrders, CourierRepository courierRepository,
                            out CourierDeliveryInfo[] recomendations, out CourierDeliveryInfo[] cover, out Order[] rejectedOrders)
        {
            // 1. Инициализация
            int rc = 1;
            int rc1 = 1;
            recomendations = null;
            cover = null;
            rejectedOrders = null;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (shopDeliveries == null || shopDeliveries.Length <= 0)
                {
                    rejectedOrders = shopOrders;
                    return rc = 0;
                }
                if (shopOrders == null || shopOrders.Length <= 0)
                    return rc;
                if (courierRepository == null || !courierRepository.IsCreated)
                    return rc;

                // 4. Строим индекс для всех заказов магазина
                rc = 4;
                int[] shopOrderId = new int[shopOrders.Length];

                for (int i = 0; i < shopOrders.Length; i++)
                { shopOrderId[i] = shopOrders[i].Id; }

                Array.Sort(shopOrderId, shopOrders);

                // 5. Установка приритета и флага наличия не собранных заказов для каждой отгрузки
                rc = 5;
                bool isReceipted = false;
                bool[] flags1 = new bool[shopOrders.Length];
                bool[] flags2 = new bool[shopOrders.Length];
                int allAssembledOrderCount = 0;
                int allActiveOrderCount = 0;

                for (int i = 0; i < shopDeliveries.Length; i++)
                {
                    CourierDeliveryInfo delivery = shopDeliveries[i];
                    Order[] deliveryOrders = delivery.Orders;
                    bool isDeliveryReceipted = false;
                    int deliveryPriority = int.MinValue;

                    for (int j = 0; j < deliveryOrders.Length; j++)
                    {
                        Order order = deliveryOrders[j];
                        int index = Array.BinarySearch(shopOrderId, order.Id);

                        if (order.Status == OrderStatus.Receipted)
                        {
                            isDeliveryReceipted = true;
                            if (!flags1[index])
                            {
                                flags1[index] = true;
                                allActiveOrderCount++;
                            }
                        }
                        else
                        {
                            if (!flags1[index])
                            {
                                flags1[index] = true;
                                allActiveOrderCount++;
                            }
                            if (!flags2[index])
                            {
                                flags2[index] = true;
                                allAssembledOrderCount++;
                            }
                        }

                        if (order.Priority > deliveryPriority)
                            deliveryPriority = order.Priority;
                    }

                    delivery.Priority = deliveryPriority;
                    delivery.IsReceipted = isDeliveryReceipted;
                    if (isDeliveryReceipted)
                        isReceipted = true;
                }

                // 5. Сортировка всех отгрузок магазина в порядке убывания приоритета и возрастания стоимости
                rc = 5;
                Array.Sort(shopDeliveries, CompareDeliveriesByPriorityAndOrderCost);

                // 6. Создаём рекомендации (покрытие 1)
                rc = 6;
                if (isReceipted)
                {
                    recomendations = new CourierDeliveryInfo[shopOrders.Length];
                    int recomendationCount = 0;
                    int coverOrderCount = 0;
                    int[] deliveryOrderIndex = new int[shopOrders.Length];

                    Array.Clear(flags1, 0, flags1.Length);

                    for (int i = 0; i < shopDeliveries.Length; i++)
                    {
                        CourierDeliveryInfo delivery = shopDeliveries[i];
                        Order[] deliveryOrders = delivery.Orders;
                        
                        for (int j = 0; j < deliveryOrders.Length; j++)
                        {
                            int index = Array.BinarySearch(shopOrderId, deliveryOrders[j].Id);
                            if (flags1[index])
                                goto NextDelivery1;
                            deliveryOrderIndex[j] = index;
                        }

                        if (delivery.IsReceipted)
                            recomendations[recomendationCount++] = delivery;

                        coverOrderCount += deliveryOrders.Length;
                        if (coverOrderCount >= allActiveOrderCount)
                            break;

                        for (int j = 0; j < deliveryOrders.Length; j++)
                        {
                            flags1[deliveryOrderIndex[j]] = true;
                        }

                        NextDelivery1:;
                    }

                    if (recomendationCount < recomendations.Length)
                    {
                        Array.Resize(ref recomendations, recomendationCount);
                    }
                }

                // 7. Строим покрытие только из собранных заказов (покрытие 2)
                rc = 7;

                if (allAssembledOrderCount > 0)
                {
                    //// 7.1 Создаём репозиторий доступных курьеров и такси
                    //rc = 71;
                    //CourierRepository courierRepository = new CourierRepository();
                    //rc1 = courierRepository.Create(shopCouriers);
                    //if (rc1 != 0)
                    //    return rc = 1000 * rc + rc1;

                    // 7.1 Цикл построения покрытия
                    rc = 71;
                    cover = new CourierDeliveryInfo[shopOrders.Length];
                    int coverCount = 0;
                    int coverOrderCount = 0;
                    int[] deliveryOrderIndex = new int[shopOrders.Length];

                    Array.Clear(flags2, 0, flags2.Length);

                    for (int i = 0; i < shopDeliveries.Length; i++)
                    {
                        CourierDeliveryInfo delivery = shopDeliveries[i];
                        if (delivery.IsReceipted)
                            continue;

                        Order[] deliveryOrders = delivery.Orders;
                        
                        for (int j = 0; j < deliveryOrders.Length; j++)
                        {
                            int index = Array.BinarySearch(shopOrderId, deliveryOrders[j].Id);
                            if (flags2[index])
                                goto NextDelivery2;
                            deliveryOrderIndex[j] = index;
                        }

                        Courier deliveryCourier = courierRepository.GetNextCourier(delivery.DeliveryCourier.VehicleID);
                        if (deliveryCourier != null)
                        {
                            delivery.DeliveryCourier = deliveryCourier;
                            cover[coverCount++] = delivery;

                            coverOrderCount += deliveryOrders.Length;
                            if (coverOrderCount >= allAssembledOrderCount)
                                break;
                        }

                        for (int j = 0; j < deliveryOrders.Length; j++)
                        {
                            flags2[deliveryOrderIndex[j]] = true;
                        }

                        NextDelivery2:;
                    }

                    if (coverCount < cover.Length)
                    {
                        Array.Resize(ref cover, coverCount);
                    }
                }

                // 8. Заказы, которые не могут быть доставлены
                rc = 8;
                int rejectedOrderCount = 0;
                rejectedOrders = new Order[shopOrders.Length];

                for (int i = 0; i < shopOrders.Length; i++)
                {
                    if (!flags2[i])
                    {
                        if (shopOrders[i].Status == OrderStatus.Assembled)
                        {
                            rejectedOrders[rejectedOrderCount++] = shopOrders[i];
                        }
                        else if (!flags1[i])
                        {
                            rejectedOrders[rejectedOrderCount++] = shopOrders[i];
                        }
                    }
                }

                if (rejectedOrderCount < rejectedOrders.Length)
                {
                    Array.Resize(ref rejectedOrders, rejectedOrderCount);
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

        /// <summary>
        /// Сравнение отгрузок по ShopId
        /// </summary>
        /// <param name="d1">Отгрузка 1</param>
        /// <param name="d2">Отгрузка 2</param>
        /// <returns>-1 - d1 меньше 2; 0 - d1 равно d2; 1 - d1 больше d2</returns>
        private static int CompareDeliveriesByShopId(CourierDeliveryInfo d1, CourierDeliveryInfo d2)
        {
            if (d1.FromShop.Id > d2.FromShop.Id)
                return -1;
            if (d1.FromShop.Id < d2.FromShop.Id)
                return 1;
            return 0;
        }

        /// <summary>
        /// Сравнение отгрузок по убыванию приритета и возрастанию средней стоимости одного заказа
        /// </summary>
        /// <param name="d1">Отгрузка 1</param>
        /// <param name="d2">Отгрузка 2</param>
        /// <returns></returns>
        private static int CompareDeliveriesByPriorityAndOrderCost(CourierDeliveryInfo d1, CourierDeliveryInfo d2)
        {
            if (d1.Priority > d2.Priority)
                return -1;
            if (d1.Priority < d2.Priority)
                return 1;
            if (d1.OrderCount < d2.OrderCount)
                return -1;
            if (d1.OrderCount > d2.OrderCount)
                return 1;
            return 0;
        }

        /// <summary>
        /// Выяснение причин отказов
        /// </summary>
        /// <param name="calcTime">Время расчета</param>
        /// <param name="shop">Магазин</param>
        /// <param name="orders">Заказы не вошедшие в отгрузки</param>
        /// <param name="courierRepository">Доступные курьеры</param>
        /// <param name="connection">Открытое соединение для получения гео-данных</param>
        /// <param name="causeList">Установленные причины отказов</param>
        /// <returns>0 - причины отказов установлены; иначе - причины отказов не установлены</returns>
        private static int ShopOrderRejectionAnalyzer(DateTime calcTime, Shop shop, Order[] orders, 
            CourierRepository courierRepository, SqlConnection connection, out OrderRejectionCause[] causeList)
        {
            // 1. Инициализация
            int rc = 1;
            int rc1 = 1;
            causeList = null;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (shop == null)
                    return rc;
                if (orders == null || orders.Length <= 0)
                    return rc;
                if (courierRepository == null || !courierRepository.IsCreated)
                    return rc;
                if (connection == null || connection.State != ConnectionState.Open)
                    return rc;

                // 3. Цикл выяснения причин отказа
                rc = 3;
                DateTime DateTimeNullValue = new DateTime(1900, 1, 1, 0, 0, 0);
                OrderRejectionCause[] orderCause = new OrderRejectionCause[8 * orders.Length];
                int causeCount = 0;
                double shopLat = shop.Latitude;
                double shopLon = shop.Longitude;
                Dictionary<int, Point[,]> orderGeoData = new Dictionary<int, Point[,]>(32);

                for (int i = 0; i < orders.Length; i++)
                {
                    // 3.1 Извлекаем заказ
                    rc = 31;
                    Order order = orders[i];

                    // 3.2 Проверяем параметры заказа
                    rc = 32;
                    if (order.DeliveryTimeFrom == DateTimeNullValue ||
                        order.DeliveryTimeTo == DateTimeNullValue || 
                        order.ShopId == 0 ||
                        order.Latitude == 0 ||
                        order.Longitude == 0)
                    {
                        if (causeCount >= orderCause.Length)
                            Array.Resize(ref orderCause, orderCause.Length + 4);
                        orderCause[causeCount++] = new OrderRejectionCause(order.Id, -1, OrderRejectionReason.InvalidArgs);
                        continue;
                    }

                    if (order.DeliveryTimeTo <= calcTime)
                    {
                        if (causeCount >= orderCause.Length)
                            Array.Resize(ref orderCause, orderCause.Length + 4);
                        orderCause[causeCount++] = new OrderRejectionCause(order.Id, -1, OrderRejectionReason.TimeOver);
                        continue;
                    }

                    // 3.3 Выясняем причину для каждого способа доставки 
                    rc = 33;
                    int[] orderVehicleId = order.VehicleTypes;
                    double orderLat = order.Latitude;
                    double orderLon = order.Longitude;

                    if (causeCount + orderVehicleId.Length > orderCause.Length)
                            Array.Resize(ref orderCause, orderCause.Length + orderVehicleId.Length);

                    orderGeoData.Clear();

                    for (int j = 0; j < orderVehicleId.Length; j++)
                    {
                        int vehicleId = orderVehicleId[j];

                        // 3.3.1 Наличие курьера для доставки
                        rc = 331;
                        Courier courier = courierRepository.GetFirstCourier(vehicleId);
                        if (courier == null)
                        {
                            orderCause[causeCount++] = new OrderRejectionCause(order.Id, vehicleId, OrderRejectionReason.CourierNA);
                            continue;
                        }

                        // 3.3.2 Вес заказа
                        rc = 332;
                        if (order.Weight > courier.MaxOrderWeight)
                        {
                            orderCause[causeCount++] = new OrderRejectionCause(order.Id, vehicleId, OrderRejectionReason.WeightOver);
                            continue;
                        }

                        // 3.3.3 Извлекаем гео-данные
                        rc = 333;
                        int distance1;
                        int duration1;
                        int distance2;
                        int duration2;
                        Point[,] geoData;

                        if (orderGeoData.TryGetValue(courier.YandexType, out geoData))
                        {
                            distance1 = geoData[1, 0].X;
                            duration1 = geoData[1, 0].Y;
                            distance2 = geoData[0, 1].X;
                            duration2 = geoData[0, 1].Y;
                        }
                        else
                        {
                            rc1 = GeoData.Select(connection, courier.YandexType, shopLat, shopLon, orderLat, orderLon, out distance1, out duration1, out distance2, out duration2);
                            if (rc1 != 0)
                            {
                                orderCause[causeCount++] = new OrderRejectionCause(order.Id, vehicleId, OrderRejectionReason.GeoDataNA);
                                continue;
                            }

                            geoData = new Point[2, 2];
                            geoData[1, 0].X = distance1;
                            geoData[1, 0].Y = duration1;
                            geoData[0, 1].X = distance2;
                            geoData[0, 1].Y = duration2;

                            orderGeoData.Add(courier.YandexType, geoData);
                        }

                        // 3.3.4 Длина маршрута
                        rc = 334;
                        double d = (courier.IsTaxi ? 0.001 * distance1 : 0.001 * (distance1 + distance2));
                        if (d >  courier.MaxDistance)
                        {
                            orderCause[causeCount++] = new OrderRejectionCause(order.Id, vehicleId, OrderRejectionReason.DistanceOver);
                            continue;
                        }

                        // 3.3.5 Доставка вовремя
                        rc = 335;
                        double deliveryTime = courier.StartDelay + courier.GetOrderTime + duration1 / 60.0 + courier.HandInTime;
                        DateTime t = calcTime.AddMinutes(deliveryTime);
                        if (t > order.DeliveryTimeTo)
                        {
                            orderCause[causeCount++] = new OrderRejectionCause(order.Id, vehicleId, OrderRejectionReason.TimeOver);
                            continue;
                        }

                        // 3.3.6 Курьер недоступен
                        rc = 336;
                        if (order.Status == OrderStatus.Assembled && !courier.IsTaxi)
                        {
                            if (!courierRepository.IstCourierEnabled(vehicleId))
                            {
                                orderCause[causeCount++] = new OrderRejectionCause(order.Id, vehicleId, OrderRejectionReason.CourierNA);
                                continue;
                            }
                        }

                        // 3.3.7 Запускаем окончательную проверку
                        rc = 337;
                        CourierDeliveryInfo delivery;
                        rc1 = courier.DeliveryCheck(calcTime, shop, new Order[] { order }, new int[] { 0, 1 }, 1, !courier.IsTaxi, geoData, out delivery);

                        switch (rc1)
                        {
                            case 0:  // Ok
                                orderCause[causeCount++] = new OrderRejectionCause(order.Id, vehicleId, OrderRejectionReason.None, rc1);
                                break;
                            case 2:  // неверные аргументы
                                orderCause[causeCount++] = new OrderRejectionCause(order.Id, vehicleId, OrderRejectionReason.InvalidArgs, rc1);
                                break;
                            case 3:  // общий вес
                                orderCause[causeCount++] = new OrderRejectionCause(order.Id, vehicleId, OrderRejectionReason.WeightOver, rc1);
                                break;
                            case 5:  // доставка вовремя
                                orderCause[causeCount++] = new OrderRejectionCause(order.Id, vehicleId, OrderRejectionReason.TimeOver, rc1);
                                break;
                            case 399:  // неизвестный calcMethod
                                orderCause[causeCount++] = new OrderRejectionCause(order.Id, vehicleId, OrderRejectionReason.UnknownCalcMethod, rc1);
                                break;
                            default:  // время и стоимость отгрузки
                                orderCause[causeCount++] = new OrderRejectionCause(order.Id, vehicleId, OrderRejectionReason.Unrecognized, rc1);
                                break;
                        }
                    }
                }

                if (causeCount < orderCause.Length)
                {
                    Array.Resize(ref orderCause, causeCount);
                }

                causeList = orderCause;

                // 4. Выход - Ok
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
