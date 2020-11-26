
namespace CourierLogistics.Logistics.FloatSolution.CourierService
{
    using CourierLogistics.Logistics.FloatSolution.CourierInLive;
    using CourierLogistics.Logistics.FloatSolution.CourierService.ServiceQueue;
    using CourierLogistics.Logistics.FloatSolution.ShopInLive;
    using CourierLogistics.Logistics.RealSingleShopSolution;
    using CourierLogistics.SourceData.Couriers;
    using CourierLogistics.SourceData.Orders;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Сервис управления курьерами:
    /// 1) После отгрузки курьеры остаются в точке доставки
    /// 2) В одной отгрузке заказы только из одного магазина
    /// 3) Для каждого дня делается независимый расчет
    /// 4) Из прошлого берем число и тип курьеров магазина, время их работы
    /// 5) Из прошлого берем среднюю стоимость доставки заказа в данном магазине данным типом курьера
    /// 6) Начальная позиция курьеров в магазине их приписки в оптимальной модели
    /// </summary>
    public class SimpleService
    {
        /// <summary>
        /// Очередь событий
        /// </summary>
        private EventQueue queue;

        /// <summary>
        ///  Очередь событий
        /// </summary>
        public EventQueue Queue => queue;

        /// <summary>
        /// Число элементов в очереди событий
        /// </summary>
        public int QueueItemCount => queue.ItemCount;

        /// <summary>
        /// Ёмкость очереди событий
        /// </summary>
        public int QueueCapacity => queue.Capcity;

        /// <summary>
        /// Флаг: true - модель успешно отработала; false - модель не отработала
        /// </summary>
        public bool IsCreated { get; private set; }

        /// <summary>
        /// Прерывание возникшее во время работы модели
        /// </summary>
        public Exception LastException { get; private set; }

        /// <summary>
        /// Конструктор класса SimpleService
        /// </summary>
        public SimpleService()
        {
            queue = new EventQueue(1200000);
        }


        // рабочие массивы
        // [i, j]
        // couriers[i] - курьер
        // shops[courierDeliveryMap[i,j]] - магазин
        private int[,] courierDeliveryMap;
        private int[] courierDeliveryCount;

        CourierEx[] taxiOnly;

        /// <summary>
        /// Расчет за один день
        /// </summary>
        /// <param name="shops">Магазины </param>
        /// <param name="couriers">Курьры</param>
        /// <param name="oneDayOrders">Заказы за день</param>
        /// <param name="statistics">Статистика заказов</param>
        /// <returns>0 - день начат; иначе - день не начат</returns>
        public int StartDay(ShopEx[] shops, CourierEx[] couriers, Order[] oneDayOrders, ShopStatistics statistics)
        {
            // 1. Инициализация
            int rc = 1;
            int rc1 = 1;
            IsCreated = false;
            bool areShopsSubscribed = false;
            bool areCouriersSubscribed = false;
            taxiOnly = new CourierEx[0];
            queue.Clear();

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (oneDayOrders == null || oneDayOrders.Length <= 0)
                    return rc;
                if (shops == null || shops.Length <= 0)
                    return rc;
                if (couriers == null || couriers.Length <= 0)
                    return rc;

                courierDeliveryCount = new int[couriers.Length];
                courierDeliveryMap = new int[couriers.Length, 32];
                taxiOnly = couriers.Where(c => c.IsTaxi).ToArray();

                // 3. Подписка на события магазинов и курьеров
                rc = 3;
                areShopsSubscribed = true;
                SubscribeShopEvents(shops);
                areCouriersSubscribed = true;
                SubscribeCourierEvents(couriers);

                // 4. Сортируем заказы по магазину
                rc = 4;
                Array.Sort(oneDayOrders, CompareByShopAndCollectedDate);

                // 5. Сортируем магазины по Id
                int[] shopId = shops.Select(p => p.N).ToArray();
                Array.Sort(shopId, shops);

                // 6. Инициализируем магазины
                rc = 6;
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
                            rc1 = shops[shopIndex].StartDay(shopOrders, statistics);
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
                    rc1 = shops[shopIndex].StartDay(shopOrders, statistics);
                }

                // 7. Находим время прибытия первого курьера в начальную точку
                rc = 7;
                TimeSpan startTime = TimeSpan.MaxValue;
                DateTime currentDay = oneDayOrders[0].Date_collected.Date;

                for (int i = 0; i < couriers.Length; i++)
                {
                    // 7.1 Пропускаем такси
                    rc = 71;
                    CourierEx courier = couriers[i];
                    if (courier.IsTaxi)
                        continue;
                    //if (courier.CourierType.VechicleType == CourierVehicleType.GettTaxi ||
                    //    courier.CourierType.VechicleType == CourierVehicleType.YandexTaxi)
                    //    continue;

                    //courier.BeginWork(currentDay.Add(courier.WorkStart), courier.Latitude, courier.Longitude);
                    courier.StartDay(currentDay);
                    if (courier.WorkStart < startTime) startTime = courier.WorkStart;
                }

                // 8. Устанавливаем время начала просмотра очереди событий
                rc = 8;
                if (startTime == TimeSpan.MaxValue) startTime = TimeSpan.Zero;
                queue.SetQueueCurrentItem(startTime);

                // 9. Отрабатываем очередь событий
                rc = 9;
                QueueItem queueItem;

                while ((queueItem = queue.GetNext()) != null)
                {
                    // 9.1 Пропускаем не активные события
                    rc = 81;
                    if (queueItem.Status != QueueItemStatus.Active)
                        continue;

                    // 9.2 Обрабатываем события по типу
                    rc = 92;

                    switch (queueItem.ItemType)
                    {
                        case QueueItemType.CourierDeliveryAlert:
                            rc1 = QueueEvent_DeliveryAlert(queueItem, shops, couriers);
                            break;
                        case QueueItemType.OrderDelivered:
                            rc1 = QueueEvent_OrderDelivered(queueItem, shops, couriers);
                            break;
                        case QueueItemType.OrderAssembled:
                            rc1 = QueueEvent_OrderAssembled(queueItem, shops, couriers);
                            break;
                        case QueueItemType.CourierWorkStart:
                            rc1 = QueueEvent_CourierWorkStart(queueItem, shops, couriers);
                            break;
                        case QueueItemType.CourierWorkEnd:
                            rc1 = QueueEvent_CourierWorkEnd(queueItem, shops, couriers);
                            break;
                        case QueueItemType.ShopAllDelivered:
                            rc1 = QueueEvent_ShopAllDelivered(queueItem, shops, couriers);
                            break;
                        case QueueItemType.MovedToPoint:
                            //rc1 = QueueEvent_MovedToPoint(queueItem, shops, couriers);
                            break;
                        case QueueItemType.None:
                            break;
                    }
                }

                // 10. Выход - Ok
                rc = 0;
                IsCreated = true;
                return rc;
            }
            catch (Exception ex)
            {
                LastException = ex;
                return rc;
            }
            finally
            {
                if (areShopsSubscribed)
                    UnsubscribeShopEvents(shops);
                if (areCouriersSubscribed)
                    UnsubscribeCourierEvents(couriers);
            }
        }

        #region Обработчики элементов очереди событий

        /// <summary>
        /// Обработка элемента очереди событий типа DeliveryAlert
        /// </summary>
        /// <param name="item">Элемент очереди событий</param>
        /// <param name="allShops">Все магазины</param>
        /// <param name="allCouriers">Все курьеры</param>
        /// <returns>0 - элемент очереди успешно обработан; иначе - элемент очереди не обработан</returns>
        private int QueueEvent_DeliveryAlert(QueueItem item, ShopEx[] allShops, CourierEx[] allCouriers)
        {
            // 1. Инициализация
            int rc = 1;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (item == null || 
                    item.ItemType != QueueItemType.CourierDeliveryAlert ||
                    item.Status != QueueItemStatus.Active)
                    return rc;
                if (allShops == null || allShops.Length <= 0)
                    return rc;
                if (allCouriers == null || allCouriers.Length <= 0)
                    return rc;

                // 3. Извлекаем аргументы события
                rc = 3;
                QueueItemDeliveryAlertArgs args = item.Args as QueueItemDeliveryAlertArgs;
                if (args == null)
                    return rc;
                if (args.Shop == null || args.Delivery == null)
                    return rc;

                // 4. Делаем отгрузки
                rc = 4;
                int[] courierId = new int[args.Delivery.Length];
                int k = 0;

                foreach (CourierDeliveryInfo di in args.Delivery)
                {
                    CourierEx courier = di.DeliveryCourier as CourierEx;
                    if (courier != null)
                    {
                        courierId[k++] = courier.Id;
                        courier.BeginDelivery(item.EventTime, di);
                    }
                }

                item.Status = QueueItemStatus.Executed;

                if (k < courierId.Length)
                {
                    Array.Resize(ref courierId, k);
                }
                Array.Sort(courierId);

                // 5. Отбираем свободных курьров
                rc = 5;
                CourierEx[] freeCouriers = allCouriers.Where(c => c.Status == CourierStatus.Ready).ToArray();

                // 6. Для всех магазинов с данными курьерами делаем пересчет
                //    наиболее оптимальных отгрузок
                rc = 6;
                //int courierId = courier.Id;

                for (int i = 0; i < allShops.Length; i++)
                {
                    // 6.1 Извлекаем магазин
                    rc = 61;
                    ShopEx shop = allShops[i];

                    // 6.2 Если курьер входит в возможные отгрузки магазина
                    rc = 62;
                    if (shop.IsPossibleCourier(courierId))
                    {
                        // 6.3 Отключаем все события, связанные с возможными отгрузками
                        rc = 63;
                        queue.DisableItems(shop.GetPossibleDeliveryQueueIndexes());

                        // 6.4 Строим для магазина возможные отгрузки заново
                        rc = 64;
                        shop.CreatePossibleDeliveries(item.EventTime, freeCouriers);
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
        /// Обработка элемента очереди событий типа OrderDelivered
        /// </summary>
        /// <param name="item">Элемент очереди событий</param>
        /// <param name="allShops">Все магазины</param>
        /// <param name="allCouriers">Все курьеры</param>
        /// <returns>0 - элемент очереди успешно обработан; иначе - элемент очереди не обработан</returns>
        private int QueueEvent_OrderDelivered(QueueItem item, ShopEx[] allShops, CourierEx[] allCouriers)
        {
            // 1. Инициализация
            int rc = 1;
            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (item == null ||
                    item.ItemType != QueueItemType.OrderDelivered ||
                    item.Status != QueueItemStatus.Active)
                    return rc;
                if (allShops == null || allShops.Length <= 0)
                    return rc;
                if (allCouriers == null || allCouriers.Length <= 0)
                    return rc;

                // 3. Извлекаем аргументы события
                rc = 3;
                QueueItemOrderDeliveredArgs args = item.Args as QueueItemOrderDeliveredArgs;
                if (args == null)
                    return rc;
                if (args.Courier == null || args.Delivery == null)
                    return rc;

                // 4. Завершаем отгрузку
                rc = 4;
                CourierEx courier = args.Delivery.DeliveryCourier as CourierEx;
                if (courier == null)
                    return rc;
                courier.EndDelivery(item.EventTime, args.Delivery);
                item.Status = QueueItemStatus.Executed;

                if (!courier.IsTaxi)
                {
                    // 5. Отбираем свободных курьров
                    rc = 5;
                    CourierEx[] freeCouriers = allCouriers.Where(c => c.Status == CourierStatus.Ready).ToArray();

                    // 6. Для всех магазинов с данным курьером делаем пересчет
                    //    наиболее оптимальных отгрузок
                    rc = 6;
                    int courierId = courier.Id;

                    for (int i = 0; i < allShops.Length; i++)
                    {
                        // 6.1 Извлекаем магазин
                        rc = 61;
                        ShopEx shop = allShops[i];

                        // 6.2 Если курьер может быть использован для доставки заказов из данного магазина
                        rc = 62;
                        if (shop.IsCourierEnabled(courier))
                        {
                            // 6.3 Отключаем все события, связанные с возможными отгрузками
                            rc = 63;
                            queue.DisableItems(shop.GetPossibleDeliveryQueueIndexes());

                            // 6.4 Строим для магазина возможные отгрузки заново
                            rc = 64;
                            shop.CreatePossibleDeliveries(item.EventTime, freeCouriers);
                        }
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
        /// Обработка элемента очереди событий типа OrderDelivered
        /// </summary>
        /// <param name="item">Элемент очереди событий</param>
        /// <param name="allShops">Все магазины</param>
        /// <param name="allCouriers">Все курьеры</param>
        /// <returns>0 - элемент очереди успешно обработан; иначе - элемент очереди не обработан</returns>
        private int QueueEvent_OrderDeliveredEx(QueueItem item, ShopEx[] allShops, CourierEx[] allCouriers)
        {
            // 1. Инициализация
            int rc = 1;
            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (item == null ||
                    item.ItemType != QueueItemType.OrderDelivered ||
                    item.Status != QueueItemStatus.Active)
                    return rc;
                if (allShops == null || allShops.Length <= 0)
                    return rc;
                if (allCouriers == null || allCouriers.Length <= 0)
                    return rc;

                // 3. Извлекаем аргументы события
                rc = 3;
                QueueItemOrderDeliveredArgs args = item.Args as QueueItemOrderDeliveredArgs;
                if (args == null)
                    return rc;
                if (args.Courier == null || args.Delivery == null)
                    return rc;

                // 4. Завершаем отгрузку
                rc = 4;
                CourierEx courier = args.Delivery.DeliveryCourier as CourierEx;
                if (courier == null)
                    return rc;
                courier.EndDelivery(item.EventTime, args.Delivery);
                item.Status = QueueItemStatus.Executed;

                // 5. Если это такси
                if (courier.IsTaxi)
                    return rc = 0;

                // 6. Отбираем магазины, из которых курьер может осуществлять отгрузку
                rc = 6;
                ShopEx[] reachableShops = allShops.Where(s => s.IsCourierEnabled(courier)).ToArray();
                if (reachableShops == null || reachableShops.Length <= 0)
                    return rc = 0;

                // 7. Только один магазин достижим
                rc = 7;
                if (reachableShops.Length == 1)
                {
                    // 7.1 Строим список свободных курьеров
                    rc = 71;
                    ShopEx shop = reachableShops[0];
                    courier.Status = CourierStatus.Unknown;
                    CourierEx[] freeCouriers = shop.GetPossibleFreeCouriers();
                    courier.Status = CourierStatus.Ready;
                    int count = freeCouriers.Length;
                    Array.Resize(ref freeCouriers, count + 1 + taxiOnly.Length);
                    freeCouriers[count] = courier;
                    taxiOnly.CopyTo(freeCouriers, count + 1);

                    // 7.2 Строим возможные отгрузки заново
                    rc = 72;
                    queue.DisableItems(shop.GetPossibleDeliveryQueueIndexes());
                    shop.CreatePossibleDeliveries(item.EventTime, freeCouriers);
                }
                else
                {
                    // 8. Достижимы несколько магазинов
                    rc = 8;

                    // 8.1 Расчитываем стоимость отгрузки с новым свобдным курьером


                }



                if (!courier.IsTaxi)
                {
                    // 5. Отбираем свободных курьров
                    rc = 5;
                    CourierEx[] freeCouriers = allCouriers.Where(c => c.Status == CourierStatus.Ready).ToArray();

                    // 6. Для всех магазинов с данным курьером делаем пересчет
                    //    наиболее оптимальных отгрузок
                    rc = 6;
                    int courierId = courier.Id;

                    for (int i = 0; i < allShops.Length; i++)
                    {
                        // 6.1 Извлекаем магазин
                        rc = 61;
                        ShopEx shop = allShops[i];

                        // 6.2 Если курьер может быть использован для доставки заказов из данного магазина
                        rc = 62;
                        if (shop.IsCourierEnabled(courier))
                        {
                            // 6.3 Отключаем все события, связанные с возможными отгрузками
                            rc = 63;
                            queue.DisableItems(shop.GetPossibleDeliveryQueueIndexes());

                            // 6.4 Строим для магазина возможные отгрузки заново
                            rc = 64;
                            shop.CreatePossibleDeliveries(item.EventTime, freeCouriers);
                        }
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




        //private int PossibleDeliveryOptimizer(ShopEx[] shops, CourierEx[] allCouriers)
        //{
        //    // 1. Инициализация
        //    int rc = 1;
        //    int rc1 = 1;

        //    try
        //    {
        //        // 2. Проверяем исходные данные
        //        rc = 2;
        //        if (shops == null || shops.Length <= 0)
        //            return rc;
        //        if (allCouriers == null || allCouriers.Length <= 0)
        //            return rc;

        //        // 3. Строим карту использвания курьеров
        //        rc = 3;
        //        rc1 = CreateCourierDeliveryMap(shops, courierDeliveryCount, courierDeliveryMap);
        //        if (rc1 != 0)
        //            return rc = 100 * rc + rc1;

        //        // 4. Выбираем индексы всех курьеров осуществляющих возможные отгрузки из нескольких магазинов
        //        rc = 4;
        //        int[] multiDeliveryCouriers = SelectMultiShopCouriers(courierDeliveryCount);
        //        if (multiDeliveryCouriers == null || multiDeliveryCouriers.Length <= 0)
        //            return rc = 0;




        //    }
        //    catch
        //    {
        //        return rc;
        //    }
        //}

        /// <summary>
        /// Построение для каждого курьера списка магазинов,
        /// из которых он осуществляет отгрузку.
        /// (Для такси списки не строятся)
        /// </summary>
        /// <param name="shops">Магазины</param>
        /// <param name="courierDeliveryCount">Количество магазинов в списке курьера</param>
        /// <param name="courierDeliveryMap">Списки магазинов для всех курьеров</param>
        /// <returns></returns>
        private static int CreateCourierDeliveryMap(ShopEx[] shops, int[] courierDeliveryCount, int[,] courierDeliveryMap)
        {
            // 1. Инициализация
            int rc = 1;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (shops == null || shops.Length <= 0)
                    return rc;
                if (courierDeliveryCount == null || courierDeliveryCount.Length <= 0)
                    return rc;
                if (courierDeliveryMap == null || courierDeliveryMap.GetLength(0) != courierDeliveryCount.Length)
                    return rc;

                // 3. Обнуляем счетчики списков
                rc = 3;
                Array.Clear(courierDeliveryCount, 0, courierDeliveryCount.Length);

                // 4. Для каждого курьера строим список индексов магазинов, из которых он осуществляет отгрузку
                rc = 4;

                for (int i = 0; i < shops.Length; i++)
                {
                    // 4.1 Извлекаем возможные отгрузки магазина
                    rc = 41;
                    CourierDeliveryInfo[] possibleDelivery = shops[i].PossibleShipment;
                    if (possibleDelivery == null || possibleDelivery.Length <= 0)
                        continue;

                    // 4.2. Заносим магазин в список курьера, участвующего в отгрузке
                    rc = 42;

                    for (int j = 0; j < possibleDelivery.Length; j++)
                    {
                        Courier courier = possibleDelivery[j].DeliveryCourier;
                        if (!courier.IsTaxi)
                        {
                            int courierIndex = courier.Id - 1;
                            courierDeliveryMap[courierIndex, courierDeliveryCount[courierIndex]++] = i;
                        }
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

        //private static int[][] SelectMultiShopCouriers(int[] courierDeliveryCount, int[,] courierDeliveryMap)
        //{
        //    // 1. Инициализация

        //    try
        //    {
        //        // 2. Проверяем исходные данные
        //        if (courierDeliveryCount == null || courierDeliveryCount.Length <= 0)
        //            return null;
        //        if (courierDeliveryMap == null || courierDeliveryMap.GetLength(0) != courierDeliveryCount.Length)
        //            return null;

        //        // 3. Выбираем курьров, участвующих в доставке из нескольких магазинов
        //        int courierCount = courierDeliveryCount.Length;
        //        List<int[]> courierList = new List<int[]>(32);
        //        int[] shopList;

        //        for (int i = 0; i < courierCount; i++)
        //        {
        //            if (courierDeliveryCount[i] > 1)
        //            {
        //                int listCount = courierDeliveryCount[i];
        //                shopList = new int[listCount];

        //                for (int j = 0; j < listCount; j++)
        //                {
        //                    shopList[j] = courierDeliveryMap[i, j];
        //                }

        //                courierList.Add(shopList);
        //            }
        //        }

        //        // 4. Возвращаем результат
        //        return courierList.ToArray();
        //    }
        //    catch
        //    {
        //        return null;
        //    }
        //}

        /// <summary>
        /// Выбор индексов курьеров участвующих 
        /// в отгрузке из нескольких магазинов
        /// </summary>
        /// <param name="courierDeliveryCount">Количество магазинов, в отгрузке из которых участвует курьер</param>
        /// <returns>Индексы курьеров участвующих в отгрузке из нескольких магазинов</returns>
        private static int[] SelectMultiShopCouriers(int[] courierDeliveryCount)
        {
            // 1. Инициализация

            try
            {
                // 2. Проверяем исходные данные
                if (courierDeliveryCount == null || courierDeliveryCount.Length <= 0)
                    return null;

                // 3. Выбираем индексы курьров, участвующих в доставке из нескольких магазинов
                int courierCount = courierDeliveryCount.Length;
                int[] result = new int[courierCount];
                int count = 0;

                for (int i = 0; i < courierCount; i++)
                {
                    if (courierDeliveryCount[i] > 1)
                        result[count++] = i;
                }

                // 4. Отрезаем пустой хвост
                if (count < result.Length)
                {
                    Array.Resize(ref result, count);
                }

                // 5. Выход - Ok
                return result;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Выбор списка магазинов, в отгрузке
        /// из которых участвует заданный курьер
        /// </summary>
        /// <param name="courierIndex">Индекс заданного курьера</param>
        /// <param name="courierDeliveryCount">Счетчики отгрузок для всех курьеров</param>
        /// <param name="courierDeliveryMap">Списки магазтнов для всех курьеров</param>
        /// <returns>Список магазинов курьера или null</returns>
        private static int[] GetShopList(int courierIndex, int[] courierDeliveryCount, int[,] courierDeliveryMap)
        {
            // 1. Инициализация

            try
            {
                // 2. Проверяем исходные данные
                if (courierDeliveryCount == null || courierDeliveryCount.Length <= 0)
                    return null;
                if (courierDeliveryMap == null || courierDeliveryMap.GetLength(0) != courierDeliveryCount.Length)
                    return null;
                if (courierIndex < 0 || courierIndex >= courierDeliveryCount.Length)
                    return null;

                // 3. Выбираем список индексов магазинов, в отгрузке из которых участвует курьер
                int shopCount = courierDeliveryCount[courierIndex];
                int[] shopList;

                if (shopCount <= 0)
                {
                    shopList = new int[0];
                }
                else
                { 
                    shopList = new int[shopCount];

                    for (int j = 0; j < shopCount; j++)
                    {
                        shopList[j] = courierDeliveryMap[courierIndex, j];
                    }
                }

                // 4. Выход - Ok
                return shopList;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Обработка элемента очереди событий типа OrderAssembled
        /// </summary>
        /// <param name="item">Элемент очереди событий</param>
        /// <param name="allShops">Все магазины</param>
        /// <param name="allCouriers">Все курьеры</param>
        /// <returns>0 - элемент очереди успешно обработан; иначе - элемент очереди не обработан</returns>
        private int QueueEvent_OrderAssembled(QueueItem item, ShopEx[] allShops, CourierEx[] allCouriers)
        {
            // 1. Инициализация
            int rc = 1;
            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (item == null ||
                    item.ItemType != QueueItemType.OrderAssembled ||
                    item.Status != QueueItemStatus.Active)
                    return rc;
                if (allShops == null || allShops.Length <= 0)
                    return rc;
                if (allCouriers == null || allCouriers.Length <= 0)
                    return rc;

                // 3. Извлекаем аргументы события
                rc = 3;
                QueueItemOrderAssembledArgs args = item.Args as QueueItemOrderAssembledArgs;
                if (args == null)
                    return rc;
                if (args.Shop == null || args.AssembledOrder == null)
                    return rc;

                item.Status = QueueItemStatus.Executed;

                // 4. Отбираем свободных курьров
                rc = 4;
                CourierEx[] freeCouriers = allCouriers.Where(c => c.Status == CourierStatus.Ready).ToArray();

                // 5. Для данного магазина пересчитываем оптимальные отгрузки
                rc = 5;
                ShopEx shop = args.Shop;
                queue.DisableItems(shop.GetPossibleDeliveryQueueIndexes());
                shop.ShopOrderAssembled(item.EventTime, freeCouriers);

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
        /// Обработка элемента очереди событий типа CourierWorkStart
        /// </summary>
        /// <param name="item">Элемент очереди событий</param>
        /// <param name="allShops">Все магазины</param>
        /// <param name="allCouriers">Все курьеры</param>
        /// <returns>0 - элемент очереди успешно обработан; иначе - элемент очереди не обработан</returns>
        private int QueueEvent_CourierWorkStart(QueueItem item, ShopEx[] allShops, CourierEx[] allCouriers)
        {
            // 1. Инициализация
            int rc = 1;
            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (item == null ||
                    item.ItemType != QueueItemType.CourierWorkStart ||
                    item.Status != QueueItemStatus.Active)
                    return rc;
                if (allShops == null || allShops.Length <= 0)
                    return rc;
                if (allCouriers == null || allCouriers.Length <= 0)
                    return rc;

                // 3. Извлекаем аргументы события
                rc = 3;
                QueueItemCourierWorkStartArgs args = item.Args as QueueItemCourierWorkStartArgs;
                if (args == null)
                    return rc;
                if (args.Courier == null)
                    return rc;

                item.Status = QueueItemStatus.Executed;

                // 4. Отбираем свободных курьров
                rc = 4;
                CourierEx courier = args.Courier;
                courier.BeginWork(item.EventTime, args.Latitude, args.Longitude);
                CourierEx[] freeCouriers = allCouriers.Where(c => c.Status == CourierStatus.Ready).ToArray();

                // 5. Для всех магазинов с данным курьером делаем пересчет
                //    наиболее оптимальных отгрузок
                rc = 5;

                for (int i = 0; i < allShops.Length; i++)
                {
                    // 5.1 Извлекаем магазин
                    rc = 51;
                    ShopEx shop = allShops[i];

                    // 5.2 Если курьер может быть использован для доставки заказов из данного магазина
                    rc = 52;
                    if (shop.IsCourierEnabled(courier))
                    {
                        // 5.3 Отключаем все события, связанные с возможными отгрузками
                        rc = 53;
                        queue.DisableItems(shop.GetPossibleDeliveryQueueIndexes());

                        // 5.4 Строим для магазина возможные отгрузки заново
                        rc = 54;
                        shop.CourierReleased(item.EventTime, courier, freeCouriers);
                    }
                }

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
        /// Обработка элемента очереди событий типа CourierWorkEnd
        /// </summary>
        /// <param name="item">Элемент очереди событий</param>
        /// <param name="allShops">Все магазины</param>
        /// <param name="allCouriers">Все курьеры</param>
        /// <returns>0 - элемент очереди успешно обработан; иначе - элемент очереди не обработан</returns>
        private int QueueEvent_CourierWorkEnd(QueueItem item, ShopEx[] allShops, CourierEx[] allCouriers)
        {
            // 1. Инициализация
            int rc = 1;
            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (item == null ||
                    item.ItemType != QueueItemType.CourierWorkEnd ||
                    item.Status != QueueItemStatus.Active)
                    return rc;
                if (allShops == null || allShops.Length <= 0)
                    return rc;
                if (allCouriers == null || allCouriers.Length <= 0)
                    return rc;

                // 3. Извлекаем аргументы события
                rc = 3;
                QueueItemCourierWorkEndArgs args = item.Args as QueueItemCourierWorkEndArgs;
                if (args == null)
                    return rc;
                if (args.Courier == null)
                    return rc;

                item.Status = QueueItemStatus.Executed;

                // 4. Отбираем свободных курьров
                rc = 4;
                CourierEx courier = args.Courier;
                courier.EndWork(item.EventTime, args.Latitude, args.Longitude);
                CourierEx[] freeCouriers = allCouriers.Where(c => c.Status == CourierStatus.Ready).ToArray();

                // 5. Для всех магазинов с данным курьером делаем пересчет
                //    наиболее оптимальных отгрузок
                rc = 5;

                for (int i = 0; i < allShops.Length; i++)
                {
                    // 5.1 Извлекаем магазин
                    rc = 51;
                    ShopEx shop = allShops[i];

                    // 5.2 Если курьер может быть использован для доставки заказов из данного магазина
                    rc = 52;
                    int courierId = courier.Id;

                    if (shop.IsPossibleCourier(courierId))
                    {
                        // 5.3 Отключаем все события, связанные с возможными отгрузками
                        rc = 53;
                        queue.DisableItems(shop.GetPossibleDeliveryQueueIndexes());

                        // 5.4 Строим для магазина возможные отгрузки заново
                        rc = 54;
                        shop.CourierReleased(item.EventTime, courier, freeCouriers);
                    }
                }

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
        /// Обработка элемента очереди событий типа CourierWorkEnd
        /// </summary>
        /// <param name="item">Элемент очереди событий</param>
        /// <param name="allShops">Все магазины</param>
        /// <param name="allCouriers">Все курьеры</param>
        /// <returns>0 - элемент очереди успешно обработан; иначе - элемент очереди не обработан</returns>
        private int QueueEvent_ShopAllDelivered(QueueItem item, ShopEx[] allShops, CourierEx[] allCouriers)
        {
            // 1. Инициализация
            int rc = 1;
            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (item == null ||
                    item.ItemType != QueueItemType.ShopAllDelivered ||
                    item.Status != QueueItemStatus.Active)
                    return rc;
                if (allShops == null || allShops.Length <= 0)
                    return rc;
                if (allCouriers == null || allCouriers.Length <= 0)
                    return rc;

                // 3. Извлекаем аргументы события
                rc = 3;
                QueueItemShopAllDeliveredArgs args = item.Args as QueueItemShopAllDeliveredArgs;
                if (args == null)
                    return rc;
                if (args.Shop == null)
                    return rc;

                item.Status = QueueItemStatus.Executed;

                // 4. Обработка события в магазине
                rc = 4;
                args.Shop.AllDelivered();

                // 5. Выход - Ok
                rc = 0;
                return rc;
            }
            catch
            {
                return rc;
            }
        }

        #endregion Обработчики элементов очереди событий

        #region События магазинов

        /// <summary>
        /// Событие 'Собран новый заказ'
        /// </summary>
        /// <param name="sender">Магазин</param>
        /// <param name="args">Аргументы события</param>
        private void Shop_OrderAssembled(ShopEx sender, ShopOrderAssembledEventArgs args)
        {
            // 1. Инициализация

            try
            {
                // 2. Проверяем исходные данные
                if (sender == null)
                    return;
                if (args == null || args.ShopOrder == null)
                    return;

                // 3. Добавляем событие в очередь событий
                QueueItemOrderAssembledArgs orderAssembledArgs = new QueueItemOrderAssembledArgs(sender, args.ShopOrder);
                queue.AddEvent(args.EventTime, QueueItemType.OrderAssembled, orderAssembledArgs);
            }
            catch { }
        }

        /// <summary>
        /// Событие 'Пересчет возможных отгрузок'
        /// </summary>
        /// <param name="sender">Магазин</param>
        /// <param name="args">Аргументы события</param>
        private void Shop_PossibleDelivery(ShopEx sender, ShopPossibleDeliveryEventArgs args)
        {
            // 1. Инициализация
            
            try
            {
                // 2. Проверяем исходные данные
                if (sender == null)
                    return;
                if (args == null || args.Delivery == null || args.Delivery.Length <= 0)
                    return;

                // 3. Отбираем отгрузки, которые могут быть начаты прямо сейчас
                CourierDeliveryInfo[] deliveryNow = new CourierDeliveryInfo[args.Delivery.Length];
                int countNow = 0;

                foreach(CourierDeliveryInfo possibleDelivery in args.Delivery)
                {
                    // 3.1 Вычисляем время предупреждения
                    //DateTime alertTime = possibleDelivery.StartDelivery.Add(possibleDelivery.ReserveTime).AddMinutes(-FloatSolutionParameters.DELIVERY_ALERT_INTERVAL);
                    //if (alertTime <= args.EventTime)
                    double reserveTime = possibleDelivery.ReserveTime.TotalMinutes;
                    if (reserveTime <= FloatSolutionParameters.DELIVERY_ALERT_INTERVAL)
                    {
                        // Ждать больше нельзя
                        deliveryNow[countNow++] = possibleDelivery;
                    }
                    //else if (!possibleDelivery.DeliveryCourier.IsTaxi && possibleDelivery.OrderCost <= 85)
                    else if (!possibleDelivery.DeliveryCourier.IsTaxi && possibleDelivery.OrderCost <= sender.GetOrderAverageCost(possibleDelivery.DeliveryCourier))
                    {
                        // Средняя стоимость доставки приемлемая
                        deliveryNow[countNow++] = possibleDelivery;
                    }
                    else
                    {
                        // Не приемлемая и не срочная отгрузка - добавляем в очередь событий
                        QueueItemDeliveryAlertArgs alertArgs = new QueueItemDeliveryAlertArgs(sender, possibleDelivery);
                        DateTime alertTime = args.EventTime.AddMinutes(reserveTime - FloatSolutionParameters.DELIVERY_ALERT_INTERVAL);
                        possibleDelivery.QueueItemIndex = queue.AddEvent(alertTime, QueueItemType.CourierDeliveryAlert, args);
                    }
                }

                // 4. Инициируем отгрузку для отобранных доставок
                if (countNow > 0)
                {
                    for (int i = 0; i < countNow; i++)
                    {
                        CourierDeliveryInfo possibleDelivery = deliveryNow[i];
                        CourierEx courier = possibleDelivery.DeliveryCourier as CourierEx;
                        if (courier != null)
                        {
                            // (((( Изменено 18.08.2020 (vrr)
                            //int rc1 = courier.BeginDelivery(possibleDelivery.StartDelivery, possibleDelivery);
                            int rc1 = courier.BeginDelivery(args.EventTime, possibleDelivery);
                            // )))) Изменено 18.08.2020 (vrr)
                        }
                        else
                        {
                            i = i;
                        }
                    }
                }
            }
            catch
            {   }
        }

        #endregion События магазинов

        #region События курьеров

        /// <summary>
        /// Событие определяющее начало работы курьера
        /// </summary>
        /// <param name="sender">Курьер</param>
        /// <param name="args">Аргументы события</param>
        private void SimpleService_WorkStarted(CourierEx sender, CourierEventArgs args)
        {
            // 1. Инициализация

            try
            {
                // 2. Проверяем исходные данные
                if (sender == null || sender.IsTaxi)
                    return;
                if (args == null)
                    return;

                // 3. Добавляем событие в очередь событий
                QueueItemCourierWorkStartArgs itemArgs = new QueueItemCourierWorkStartArgs(sender, args.Latitude, args.Longitude);
                queue.AddEvent(args.EventTime, QueueItemType.CourierWorkStart, itemArgs);
            }
            catch
            {   }
        }

        /// <summary>
        /// Событие определяющее завершение работы курьера
        /// </summary>
        /// <param name="sender">Курьер</param>
        /// <param name="args">Аргументы события</param>
        private void SimpleService_WorkEnded(CourierEx sender, CourierEventArgs args)
        {
            // 1. Инициализация

            try
            {
                // 2. Проверяем исходные данные
                if (sender == null || sender.IsTaxi)
                    return;
                if (args == null)
                    return;

                // 3. Добавляем событие в очередь событий
                QueueItemCourierWorkEndArgs itemArgs = new QueueItemCourierWorkEndArgs(sender, args.Latitude, args.Longitude);
                queue.AddEvent(args.EventTime, QueueItemType.CourierWorkEnd, itemArgs);
            }
            catch
            { }
        }

        /// <summary>
        /// Событие определяющее завершение отгрузки
        /// </summary>
        /// <param name="sender">Курьер</param>
        /// <param name="args">Аргументы события</param>
        private void SimpleService_OrderDelivered(CourierEx sender, CourierEventArgs args)
        {
            // 1. Инициализация

            try
            {
                // 2. Проверяем исходные данные
                //if (sender == null || sender.IsTaxi)
                if (sender == null)
                    return;
                if (args == null)
                    return;

                // 3. Добавляем событие в очередь событий
                QueueItemOrderDeliveredArgs itemArgs = new QueueItemOrderDeliveredArgs(sender, args.Delivery);
                queue.AddEvent(args.EventTime, QueueItemType.OrderDelivered, itemArgs);
            }
            catch
            { }
        }

        /// <summary>
        /// Событие определяющее завершение перемещения в заданную точку
        /// </summary>
        /// <param name="sender">Курьер</param>
        /// <param name="args">Аргументы события</param>
        private void SimpleService_MovedToPoint(CourierEx sender, CourierEventArgs args)
        {
            // 1. Инициализация

            try
            {
                // 2. Проверяем исходные данные
                if (sender == null || sender.IsTaxi)
                    return;
                if (args == null)
                    return;

                // 3. Добавляем событие в очередь событий
                QueueItemMovedToPointArgs itemArgs = new QueueItemMovedToPointArgs(sender, args.Latitude, args.Longitude);
                queue.AddEvent(args.EventTime, QueueItemType.MovedToPoint, itemArgs);
            }
            catch
            { }
        }

        #endregion События курьеров

        #region Подписка/отписка на события магазинов и курьеров

        /// <summary>
        /// Подписка на события курьеров
        /// </summary>
        /// <param name="couriers"></param>
        /// <returns>0 - подписка произведена; иначе - подписка не произведена</returns>
        private int SubscribeCourierEvents(CourierEx[] couriers)
        {
            // 1. Иницализация
            int rc = 1;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (couriers == null || couriers.Length <= 0)
                    return rc;

                // 3. Подписываемся на события курьеров
                rc = 3;

                for (int i = 0; i < couriers.Length; i++)
                {
                    CourierEx courier = couriers[i];
                    courier.MovedToPoint += SimpleService_MovedToPoint;
                    courier.OrderDelivered += SimpleService_OrderDelivered;
                    courier.WorkEnded += SimpleService_WorkEnded;
                    courier.WorkStarted += SimpleService_WorkStarted;
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
        /// Отписка от событий курьеров
        /// </summary>
        /// <param name="couriers"></param>
        /// <returns>0 - отписка произведена; иначе - отписка не произведена</returns>
        private int UnsubscribeCourierEvents(CourierEx[] couriers)
        {
            // 1. Иницализация
            int rc = 1;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (couriers == null || couriers.Length <= 0)
                    return rc;

                // 3. Отписываемся от событий курьеров
                rc = 3;

                for (int i = 0; i < couriers.Length; i++)
                {
                    CourierEx courier = couriers[i];
                    courier.MovedToPoint -= SimpleService_MovedToPoint;
                    courier.OrderDelivered -= SimpleService_OrderDelivered;
                    courier.WorkEnded -= SimpleService_WorkEnded;
                    courier.WorkStarted -= SimpleService_WorkStarted;
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
        /// Подписка на события магазинов
        /// </summary>
        /// <param name="shops"></param>
        /// <returns>0 - подписка произведена; иначе - подписка не произведена</returns>
        private int SubscribeShopEvents(ShopEx[] shops)
        {
            // 1. Иницализация
            int rc = 1;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (shops == null || shops.Length <= 0)
                    return rc;

                // 3. Подписываемся на события магазинов
                rc = 3;

                for (int i = 0; i < shops.Length; i++)
                {
                    ShopEx shop = shops[i];
                    shop.OrderAssembled += Shop_OrderAssembled;
                    shop.PossibleDelivery += Shop_PossibleDelivery;
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
        /// Отписка от событий магазинов
        /// </summary>
        /// <param name="shops"></param>
        /// <returns>0 - отписка произведена; иначе - отписка не произведена</returns>
        private int UnsubscribeShopEvents(ShopEx[] shops)
        {
            // 1. Иницализация
            int rc = 1;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (shops == null || shops.Length <= 0)
                    return rc;

                // 3. Подписываемся на события магазинов
                rc = 3;

                for (int i = 0; i < shops.Length; i++)
                {
                    ShopEx shop = shops[i];
                    shop.OrderAssembled -= Shop_OrderAssembled;
                    shop.PossibleDelivery -= Shop_PossibleDelivery;
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

        #endregion Подписка/отписка на события магазинов и курьеров

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
        /// Сравнение магазинов по Id
        /// </summary>
        /// <param name="shop1">Магазин 1</param>
        /// <param name="shop2">Магазин 2</param>
        /// <returns>-1 - Магазин1 &lt; Магазин2; 0 - Магазин1 = Магазин2; 1 - Магазин1 &gt; Магазин2</returns>
        private static int CompareByShopId(ShopEx shop1, ShopEx shop2)
        {
            if (shop1.N < shop2.N)
                return -1;
            if (shop1.N > shop2.N)
                return 1;
            return 0;
        }

        /// <summary>
        /// Выборка всех событий очереди с выполненными отгрузками
        /// </summary>
        /// <returns></returns>
        public QueueItem[] SelectQueueItemOfDeliveredOrders()
        {
            return queue.SelectQueueItemOfDeliveredOrders();
        }
    }
}
