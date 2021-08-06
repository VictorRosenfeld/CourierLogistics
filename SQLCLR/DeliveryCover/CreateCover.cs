
namespace SQLCLR.DeliveryCover
{
    using SQLCLR.Couriers;
    using SQLCLR.Deliveries;
    using SQLCLR.Orders;
    using System;
    using System.Collections;

    public class CreateCover
    {
        public static int Create(CourierDeliveryInfo[] deliveries, AllOrders allOrders, AllCouriers allCouriers)
        {
            // 1. Инициализация
            int rc = 1;
            int rc1 = 1;

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

                // 3. Сортируем отгрузки по магазину
                rc = 3;
                Array.Sort(deliveries, CompareDeliveriesByShopId);

                // 4. Обрабатываем каждый магазин в отдельности
                rc = 4;
                int startIndex = 0;
                int endIndex = 0;
                int currentShopId = deliveries[0].FromShop.Id;
                CourierDeliveryInfo[] recomendations;
                CourierDeliveryInfo[] cover;
                Order[] rejectedOrders;

                for (int i = 1; i < deliveries.Length; i++)
                {
                    if (deliveries[i].FromShop.Id == currentShopId)
                    {
                        endIndex = i;
                    }
                    else
                    {
                        rc1 = CreateShopCover(deliveries, allOrders.GetShopOrders(currentShopId), allCouriers.GetShopCouriers(currentShopId),
                            out recomendations, out cover, out rejectedOrders);
                    }
                }







                // 3. Сортируем все отгрузки по магазину, убыванию приоритета и возрастанию цены
                rc = 3;




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
        public static int CreateShopCover(CourierDeliveryInfo[] shopDeliveries, Order[] shopOrders, Courier[] shopCouriers,
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
                if (shopCouriers == null || shopCouriers.Length <= 0)
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
                    // 7.1 Создаём репозиторий доступных курьеров и такси
                    rc = 71;
                    CourierRepository courierRepository = new CourierRepository();
                    rc1 = courierRepository.Create(shopCouriers);
                    if (rc1 != 0)
                        return rc = 1000 * rc + rc1;

                    // 7.2 Цикл построения покрытия
                    rc = 72;
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
    }
}
