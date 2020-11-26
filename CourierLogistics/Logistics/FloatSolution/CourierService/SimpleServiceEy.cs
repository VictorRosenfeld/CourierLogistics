
namespace CourierLogistics.Logistics.FloatSolution.CourierService
{
    using CourierLogistics.Logistics.FloatSolution.CourierInLive;
    using CourierLogistics.Logistics.FloatSolution.CourierService.ServiceQueue;
    using CourierLogistics.Logistics.FloatSolution.ShopInLive;
    using CourierLogistics.Logistics.RealSingleShopSolution;
    using CourierLogistics.SourceData.Couriers;
    using CourierLogistics.SourceData.Orders;
    using System;
    using System.Linq;

    /// <summary>
    /// Сервис управления курьерами:
    /// 1) После отгрузки курьеры остаются в точке доставки
    /// 2) В одной отгрузке заказы только из одного магазина
    /// 3) Для каждого дня делается независимый расчет
    /// 4) Из прошлого берем число и тип курьеров магазина, время их работы
    /// 5) Из прошлого берем среднюю стоимость доставки заказа в данном магазине данным типом курьера
    /// 6) Начальная позиция курьеров в магазине - их приписка в оптимальной модели
    /// </summary>
    public class SimpleServiceEy
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
        public SimpleServiceEy()
        {
            queue = new EventQueue(1200000);
        }


        // рабочие массивы
        // [i, j]
        // couriers[i] - курьер
        // shops[courierDeliveryMap[i,j]] - магазин
        private int[,] courierDeliveryMap;
        private int[] courierDeliveryCount;

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
            //taxiOnly = new CourierEx[0];
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
                courierDeliveryMap = new int[couriers.Length, 256];

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

                //int nd = oneDayOrders.Count(p => p.Source == -1);

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

                    // 7.2 Инициализация начала нового дня курьера
                    rc = 72;
                    courier.StartDay(currentDay);
                    if (courier.WorkStart < startTime)
                        startTime = courier.WorkStart;
                }

                // 8. Устанавливаем время начала просмотра очереди событий
                rc = 8;
                if (startTime == TimeSpan.MaxValue)
                    startTime = TimeSpan.Zero;
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
                            rc1 = QueueEvent_CourierDeliveryAlert(queueItem, shops, couriers);
                            break;
                        case QueueItemType.TaxiDeliveryAlert:
                            rc1 = QueueEvent_TaxiDeliveryAlert(queueItem, shops, couriers);
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
        /// Обработка элемента очереди событий типа CourierDeliveryAlert
        /// </summary>
        /// <param name="item">Элемент очереди событий</param>
        /// <param name="allShops">Все магазины</param>
        /// <param name="allCouriers">Все курьеры</param>
        /// <returns>0 - элемент очереди успешно обработан; иначе - элемент очереди не обработан</returns>
        private int QueueEvent_CourierDeliveryAlert(QueueItem item, ShopEx[] allShops, CourierEx[] allCouriers)
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

                // 5. Обновляем возможные отгрузки
                rc = 5;
                bool[] isShopDeliveryUpdated = allShops.Select(p => p.IsPossibleCourier(courierId)).ToArray();
                int rc1 = PossibleDeliveryOptimizer(item.EventTime, allShops, allCouriers, queue, isShopDeliveryUpdated, courierDeliveryCount, courierDeliveryMap);
                if (rc1 != 0)
                {
                    rc1 = rc1;
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
        /// Обработка элемента очереди событий типа TaxiDeliveryAlert
        /// </summary>
        /// <param name="item">Элемент очереди событий</param>
        /// <param name="allShops">Все магазины</param>
        /// <param name="allCouriers">Все курьеры</param>
        /// <returns>0 - элемент очереди успешно обработан; иначе - элемент очереди не обработан</returns>
        private int QueueEvent_TaxiDeliveryAlert(QueueItem item, ShopEx[] allShops, CourierEx[] allCouriers)
        {
            // 1. Инициализация
            int rc = 1;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (item == null ||
                    item.ItemType != QueueItemType.TaxiDeliveryAlert ||
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
                if (args.Shop == null || args.Delivery == null || args.Delivery.Length != 1)
                    return rc;

                item.Status = QueueItemStatus.Executed;

                // 4. Если заказ уже отгружен
                rc = 4;
                CourierDeliveryInfo taxiDelivery = args.Delivery[0];
                if (taxiDelivery == null)
                    return rc;
                if (taxiDelivery.ShippingOrder == null)
                    return rc;
                if (taxiDelivery.ShippingOrder.Completed)
                    return rc = 0;

                // 5. Если заказ входит в возможные отгрузки магазина
                rc = 5;
                int shopId = taxiDelivery.ShippingOrder.ShopNo;
                ShopEx shop = null;
                int shopIndex = -1;

                for (int i = 0; i < allShops.Length; i++)
                {
                    if (allShops[i].N == shopId)
                    {
                        shop = allShops[i];
                        shopIndex = i;
                        break;
                    }
                }

                if (shop == null)
                    return rc;

                CourierDeliveryInfo deliveryWithOrder = shop.GetPossibleDeliveryWithOrder(taxiDelivery.ShippingOrder.Id_order);
                if (deliveryWithOrder == null || deliveryWithOrder.DeliveryCourier == null || deliveryWithOrder.DeliveryCourier.IsTaxi)
                {
                    if (deliveryWithOrder != null)
                        queue.DisableItems(new int[] { deliveryWithOrder.QueueItemIndex });

                    // 6. Выбираем такси заданного типа из доступных
                    rc = 6;
                    if (taxiDelivery.DeliveryCourier == null)
                        return rc;
                    CourierVehicleType taxiType = taxiDelivery.DeliveryCourier.CourierType.VechicleType;
                    CourierEx taxi = allCouriers.FirstOrDefault(c => c.CourierType.VechicleType == taxiType);
                    if (taxi == null || !taxi.IsTaxi)
                        return rc;

                    // 7. Находим для такси наилучшую отгрузку
                    rc = 7;
                    //CourierDeliveryInfo bestTaxiDelivery = shop.GetBestCourierDelivery(item.EventTime, taxi);
                    //if (bestTaxiDelivery == null)
                    //    return rc;

                    // 8. Делаем отгрузку такси
                    rc = 8;
                    //taxi.BeginDelivery(item.EventTime, bestTaxiDelivery);
                    taxi.BeginDelivery(item.EventTime, args.Delivery[0]);

                    // 9. Пересчитываем отгрузки
                    rc = 9;
                    bool[] isShopDeliveryUpdated = new bool[allShops.Length];
                    isShopDeliveryUpdated[shopIndex] = true;
                    int rc1 = PossibleDeliveryOptimizer(item.EventTime, allShops, allCouriers, queue, isShopDeliveryUpdated, courierDeliveryCount, courierDeliveryMap);
                    if (rc1 != 0)
                    {
                        rc1 = rc1;
                    }
                }
                //else if (deliveryWithOrder.DeliveryCourier.IsTaxi)
                //{

                //}
                else if (!deliveryWithOrder.DeliveryCourier.IsTaxi) // С заданным товаром есть отгрузка штатным курьером
                {
                    queue.DisableItems(new int[] { deliveryWithOrder.QueueItemIndex });
                    // 10. Делаем отгрузку курьером
                    rc = 10;
                    bool[] isShopDeliveryUpdated;  
                    CourierEx courier = deliveryWithOrder.DeliveryCourier as CourierEx;
                    if (courier != null)
                    {
                        courier.BeginDelivery(item.EventTime, deliveryWithOrder);
                        isShopDeliveryUpdated = allShops.Select(p => p.IsPossibleCourier(courier.Id)).ToArray();
                    }
                    else
                    {
                        isShopDeliveryUpdated = new bool[allShops.Length];
                    }

                    // 11. Пересчитываем отгрузки
                    rc = 11;
                    int rc1 = PossibleDeliveryOptimizer(item.EventTime, allShops, allCouriers, queue, isShopDeliveryUpdated, courierDeliveryCount, courierDeliveryMap);
                    if (rc1 != 0)
                    {
                        rc1 = rc1;
                    }
                }

                // 12. Выход - Ok
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
                //if (courier.Id == 329)
                //    rc = rc;

                if (!courier.IsTaxi)
                {
                    // 5. Обновляем возможные отгрузки
                    rc = 5;
                    double[] distFromShop = allShops.Select(p => p.GetDistance(courier)).ToArray();
                    Array.Sort(distFromShop, allShops);
                    bool[] isShopDeliveryUpdated = new bool[allShops.Length];
                    int count = 0;

                    for (int i = 0; i < distFromShop.Length; i++)
                    {
                        if (distFromShop[i] <= FloatSolutionParameters.COURIER_DISTANCE_TO_SHOP_LIMIT)
                        {
                            isShopDeliveryUpdated[i] = true;
                            count++;
                        }
                    }

                    //int nn = 4;

                    if (count < FloatSolutionParameters.MIN_AVAILABLE_SHOP_COUNT)
                    {
                        count = FloatSolutionParameters.MIN_AVAILABLE_SHOP_COUNT;
                        if (distFromShop.Length < count)
                            count = distFromShop.Length;

                        for (int i = 0; i < count; i++)
                        {
                            if (distFromShop[i] > FloatSolutionParameters.MAX_DISTANCE_TO_AVAILABLE_SHOP)
                                break;
                            isShopDeliveryUpdated[i] = true;
                        }
                    }

                    if (isShopDeliveryUpdated[0] == false)
                        isShopDeliveryUpdated[0] = true;


                    //bool[] isShopDeliveryUpdated = allShops.Select(p => p.IsCourierEnabled(courier)).ToArray();
                    //if (isShopDeliveryUpdated.Count(p => p) <= 0)
                    //{
                    //    Console.WriteLine($"Курьер {courier.Id} завис !");
                    //}

                    int rc1 = PossibleDeliveryOptimizer(item.EventTime, allShops, allCouriers, queue, isShopDeliveryUpdated, courierDeliveryCount, courierDeliveryMap);
                    if (rc1 != 0)
                    {
                        rc1 = rc1;
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
        /// Построние отгрузок, в которых кадый курьер
        /// используется только в одной отгрузке
        /// </summary>
        /// <param name="startTime">Момент времени для расчетов</param>
        /// <param name="shops">Магазины, для которых проводится построение</param>
        /// <param name="allCouriers">Все курьеры без такси</param>
        /// <param name="courierDeliveryCount">Рабочий массив для счетчиков магазинов, из которых производится отгрузка</param>
        /// <param name="courierDeliveryMap">Рабочий массив для списков магазинов, из которых курьер производит отгрузку</param>
        /// <returns>0 - построение выполнено; иначе - построение не выполнено</returns>
        private static int PossibleDeliveryOptimizer(
            DateTime startTime,
            ShopEx[] shops,
            CourierEx[] allCouriers,
            EventQueue queue,
            bool[] isShopDeliveryUpdated,
            int[] courierDeliveryCount, int[,] courierDeliveryMap)
        {
            // 1. Инициализация
            int rc = 1;
            int rc1 = 1;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (shops == null || shops.Length <= 0)
                    return rc;
                if (allCouriers == null || allCouriers.Length <= 0)
                    return rc;
                if (queue == null)
                    return rc;
                if (courierDeliveryCount == null ||
                    courierDeliveryCount.Length < allCouriers.Length)
                    return rc;
                if (courierDeliveryMap == null || courierDeliveryMap.GetLength(0) != courierDeliveryCount.Length)
                    return rc;
                if (isShopDeliveryUpdated == null ||
                    isShopDeliveryUpdated.Length != shops.Length)
                    return rc;

                CourierEx[] freeCouriers = allCouriers.Where(c => c.Status == CourierStatus.Ready).ToArray();
                if (freeCouriers == null || freeCouriers.Length <= 0)
                    return rc;

                for (int i = 0; i < allCouriers.Length; i++)
                    allCouriers[i].ShopIndex = -1;

                bool[] isMultiShop = new bool[shops.Length];
                //bool[] isShopDeliveryUpdated = new bool[shops.Length];
                CourierDeliveryInfo[][] iterPossibleDeliveries = new CourierDeliveryInfo[shops.Length][];
                int n = 0;

                for (int i = 0; i < isShopDeliveryUpdated.Length; i++)
                {
                    //if (i == 272)
                    //{
                    //    i = i;
                    //}
                    if (isShopDeliveryUpdated[i])
                    {
                        CourierDeliveryInfo[] updatedPossibleDelivery;
                        rc1 = shops[i].CreatePossibleDeliveriesEx(startTime, freeCouriers, out updatedPossibleDelivery);
                        if (rc1 == 0)
                        {
                            if (updatedPossibleDelivery != null)
                            {
                                rc = rc;
                                n++;
                            }
                            iterPossibleDeliveries[i] = updatedPossibleDelivery;
                            //n++;
                        }
                        else
                        {
                            isShopDeliveryUpdated[i] = false;
                        }
                    }
                    else
                    {
                        iterPossibleDeliveries[i] = shops[i].PossibleShipment;
                    }
                }

                // 3. Строим карту использвания курьеров в возможных отгрузках
                rc = 3;
                rc1 = CreateCourierDeliveryMap(iterPossibleDeliveries, courierDeliveryCount, courierDeliveryMap);
                if (rc1 != 0)
                    //return rc = 100 * rc + rc1;
                    goto UpdateDelivery;
                //int xx = CheckCourierMap(courierDeliveryCount, courierDeliveryMap);
                //if (xx >= 0)
                //{
                //    xx = xx;
                //}

                // 4. Выбираем индексы всех курьеров осуществляющих возможные отгрузки из нескольких магазинов
                rc = 4;
                int[] multiDeliveryCourier;
                int[] multiDeliveryCount;

                while (true)
                {
                    rc1 = SelectMultiDeliveryCouriers(courierDeliveryCount, out multiDeliveryCourier, out multiDeliveryCount);
                    if (rc1 != 0 || multiDeliveryCourier == null || multiDeliveryCourier.Length <= 0)
                        break;

                    Array.Clear(isMultiShop, 0, isMultiShop.Length);

                    // 5. Сортируем мульти-курьеров по количеству отгрузок
                    rc = 5;
                    Array.Sort(multiDeliveryCount, multiDeliveryCourier);

                    // 6. Итерация - выбор одного магазина для всех мульти-курьеров
                    rc = 6;

                    for (int i = multiDeliveryCount.Length - 1; i >= 0; i--)
                    {
                        // 6.1 Выбираем мульти-курьера и связанные с ним магазины
                        rc = 61;
                        int multiCourierIndex = multiDeliveryCourier[i];
                        CourierEx multiCourier = allCouriers[multiCourierIndex];
                        if (multiCourier.IsTaxi)
                            continue;
                        int shopCount = multiDeliveryCount[i];

                        // 6.2 Для каждого мульти-магазина определяем стоимость доставки с мульти курьером и без него
                        rc = 62;
                        double[] costWith = new double[shopCount];
                        int[] orderCountWith = new int[shopCount];
                        double[] costWithout = new double[shopCount];
                        int[] orderCountWithout = new int[shopCount];
                        int multiCourierId = multiCourier.Id;

                        for (int j = 0; j < shopCount; j++)
                        {
                            // 6.2.1 Сохраняем текущие данные с мульти-курьером
                            rc = 621;
                            int shopIndex = courierDeliveryMap[multiCourierIndex, j];
                            isMultiShop[shopIndex] = true;
                            ShopEx multiShop = shops[shopIndex];

                            CourierDeliveryInfo[] possibleDeliveryWith = iterPossibleDeliveries[shopIndex];
                            if (possibleDeliveryWith != null && possibleDeliveryWith.Length > 0)
                            {
                                double cost = 0;
                                int orderCount = 0;

                                for (int k = 0; k < possibleDeliveryWith.Length; k++)
                                {
                                    CourierDeliveryInfo deliveryInfo = possibleDeliveryWith[k];
                                    cost += deliveryInfo.Cost;
                                    orderCount += deliveryInfo.OrderCount;
                                }

                                costWith[j] = cost;
                                orderCountWith[j] = orderCount;
                            }

                            // 6.2.2 Отбираем свободных курьров для данного магазина без мульти-курьера (включая такси)
                            rc = 622;
                            CourierEx[] freeShopCouriers = new CourierEx[freeCouriers.Length];
                            int freeShopCourierCount = 0;

                            for (int k = 0; k < freeCouriers.Length; k++)
                            {
                                CourierEx freeCourier = freeCouriers[k];
                                if (freeCourier.IsTaxi)
                                {
                                    freeShopCouriers[freeShopCourierCount++] = freeCourier;
                                }
                                else if (freeCourier.Id != multiCourierId)
                                {
                                    if (freeCourier.ShopIndex == -1)
                                    {
                                        if (multiShop.IsCourierEnabled(freeCourier))
                                        {
                                            freeShopCouriers[freeShopCourierCount++] = freeCourier;
                                        }
                                    }
                                    else if (freeCourier.ShopIndex == shopIndex)
                                    {
                                        freeShopCouriers[freeShopCourierCount++] = freeCourier;
                                    }
                                }
                            }

                            if (freeShopCourierCount <= 0)
                            {
                                rc = rc;
                            }

                            if (freeShopCourierCount < freeShopCouriers.Length)
                            {
                                Array.Resize(ref freeShopCouriers, freeShopCourierCount);
                            }

                            // 6.2.3 Строим лучший набор возможных отгрузок
                            rc = 623;
                            CourierDeliveryInfo[] possibleDeliveryWithout;
                            rc1 = multiShop.CreatePossibleDeliveriesEx(startTime, freeShopCouriers, out possibleDeliveryWithout);
                            if (rc1 == 0 && possibleDeliveryWithout.Length > 0)
                            {
                                double cost = 0;
                                int orderCount = 0;

                                for (int k = 0; k < possibleDeliveryWithout.Length; k++)
                                {
                                    CourierDeliveryInfo deliveryInfo = possibleDeliveryWithout[k];
                                    cost += deliveryInfo.Cost;
                                    orderCount += deliveryInfo.OrderCount;
                                }

                                costWithout[j] = cost;
                                orderCountWithout[j] = orderCount;
                            }
                        }

                        // 6.3 Приписываем мульти-курьера одному из связанных магазинов
                        rc = 63;
                        // ??????????????????????????????????? ++++++++++++++++++++++++++++
                        int selectedShopIndex = SelectMultiShop(costWith, orderCountWith, costWithout, orderCountWithout);
                        if (selectedShopIndex >= 0)
                        {
                            //multiCourier.ShopIndex = selecteadShopIndex;
                            multiCourier.ShopIndex = courierDeliveryMap[multiCourierIndex, selectedShopIndex];
                        }
                        else
                        {
                            rc = rc;
                        }
                    }

                    // 7. Пересчитываем для магазинов с мультикурьерами все возможные отгрузки заново
                    rc = 7;
                    for (int i = 0; i < shops.Length; i++)
                    {
                        // 7.1 Фильтруем мульти-магазины
                        rc = 71;
                        if (!isMultiShop[i])
                            continue;

                        // 7.2 Отбираем свободных курьеров для магазина
                        rc = 72;
                        ShopEx multiShop = shops[i];
                        CourierEx[] freeShopCouriers = new CourierEx[freeCouriers.Length];
                        int freeShopCourierCount = 0;

                        for (int j = 0; j < freeCouriers.Length; j++)
                        {
                            CourierEx freeCourier = freeCouriers[j];
                            if (freeCourier.IsTaxi)
                            {
                                freeShopCouriers[freeShopCourierCount++] = freeCourier;
                            }
                            else if (freeCourier.ShopIndex == -1)
                            {
                                if (multiShop.IsCourierEnabled(freeCourier))
                                {
                                    freeShopCouriers[freeShopCourierCount++] = freeCourier;
                                }
                            }
                            else if (freeCourier.ShopIndex == i)
                            {
                                freeShopCouriers[freeShopCourierCount++] = freeCourier;
                            }
                        }

                        if (freeShopCourierCount <= 0)
                        {
                            rc = rc;
                        }

                        if (freeShopCourierCount < freeShopCouriers.Length)
                        {
                            Array.Resize(ref freeShopCouriers, freeShopCourierCount);
                        }

                        // 7.3 Пересчитываем возможные отгрузки
                        rc = 73;
                        CourierDeliveryInfo[] shopPossibleDelivery;
                        rc1 = multiShop.CreatePossibleDeliveriesEx(startTime, freeShopCouriers, out shopPossibleDelivery);
                        if (rc1 == 0)
                        {
                            iterPossibleDeliveries[i] = shopPossibleDelivery;
                            isShopDeliveryUpdated[i] = true;
                        }
                    }

                    // 8. Строим карту использования курьеров
                    rc = 8;
                    rc1 = CreateCourierDeliveryMap(iterPossibleDeliveries, courierDeliveryCount, courierDeliveryMap);
                    if (rc1 != 0)
                        break;
                    //int yy = CheckCourierMap(courierDeliveryCount, courierDeliveryMap);
                    //if (yy >= 0)
                    //{
                    //    yy = yy;
                    //}
                }

                // 9. Обновляем возможные отгрузки магазинов
                UpdateDelivery:
                rc = 9;
                for (int i = 0; i < shops.Length; i++)
                {
                    if (isShopDeliveryUpdated[i])
                    {
                        ShopEx shop = shops[i];
                        queue.DisableItems(shop.GetPossibleDeliveryQueueIndexes());
                        rc1 = shop.UpdatePossibleDelivery(startTime, iterPossibleDeliveries[i]);
                    }
                }

                // 10. Выход - Ok
                rc = 0;
                return rc;
            }
            catch
            {
                return rc;
            }
        }

        private static int CheckCourierMap(int[] courierDeliveryCount, int[,] courierDeliveryMap)
        {
            for (int i = 0; i < courierDeliveryCount.Length; i++)
            {
                int count = courierDeliveryCount[i];
                if (count > 1)
                {
                    int[] shopId = new int[count];
                    for (int j = 0; j < count; j++)
                    {
                        shopId[j] = courierDeliveryMap[i, j];
                    }

                    Array.Sort(shopId);
                    int id = shopId[0];

                    for (int j = 1; j < count; j++)
                    {
                        if (shopId[j] == id)
                            return i;
                        id = shopId[j];
                    }
                }
            }

            return -1;
        }

        /// <summary>
        /// К какому магазину приписать мульти-курьера
        /// </summary>
        /// <param name="costWith">Стоимость доставки с мульти-курьером</param>
        /// <param name="orderCountWith">Число закзов с мульти-курьером</param>
        /// <param name="costWithout">Стоимость доставки без мульти-курьера</param>
        /// <param name="orderCountWithout">Число закзов без мульти-курьера</param>
        /// <returns>Индекс магазина (неотрицательное значение)</returns>
        private static int SelectMultiShop(double[] costWith, int[] orderCountWith, double[] costWithout, int[] orderCountWithout)
        {
            // 1. Инициализация
            int rc = -1;

            try
            {
                // 2. Проверяем исходные данные
                rc = -2;
                if (costWith == null || costWith.Length <= 0)
                    return rc;

                int shopCount = costWith.Length;

                if (orderCountWith == null || orderCountWith.Length != shopCount)
                    return rc;
                if (costWithout == null || costWithout.Length != shopCount)
                    return rc;
                if (orderCountWithout == null || orderCountWithout.Length != shopCount)
                    return rc;

                // 3. Считаем итоги для without
                rc = -3;
                double sumCostWithout = 0;
                int sumOrderCountWithout = 0;
                //int ind = -1;
                //int cnt = 0;

                for (int i = 0; i < shopCount; i++)
                {
                    // !!!!!!!!!!! +++++++++++++ +!?
                    if (orderCountWith[i] != orderCountWithout[i])
                    {
                        //if (ind == -1)
                        //    ind = i;
                        //cnt++;
                        //if (orderCountWithout[i] > orderCountWith[i])
                        //    Console.WriteLine($"with count = {orderCountWith[i]}, without count = {orderCountWithout[i]}");
                        return i;
                    }

                    //if (cnt > 1)
                    //    Console.WriteLine($"cnt = {cnt}");
                    //if (ind != -1)
                    //    return ind;

                    sumCostWithout += costWithout[i];
                    sumOrderCountWithout += orderCountWithout[i];
                }

                // 4. Выбираем конфигурацию с наименьшей средней стоимостью доставки
                rc = -4;
                double minAvgCost = double.MaxValue;
                int minShopIndex = -1;

                for (int i = 0; i < shopCount; i++)
                {
                    double allDeliveryCost = sumCostWithout - costWithout[i] + costWith[i];
                    int allDeliveryOrderCount = sumOrderCountWithout - orderCountWithout[i] + orderCountWith[i];
                    if (allDeliveryOrderCount > 0)
                    {
                        double avgCost = allDeliveryCost / allDeliveryOrderCount;
                        if (avgCost < minAvgCost)
                        {
                            minAvgCost = avgCost;
                            minShopIndex = i;
                        }
                    }
                }

                // 5. Выход
                return minShopIndex;
            }
            catch
            {
                return rc;
            }
        }

        /// <summary>
        /// Построение для каждого курьера списка магазинов,
        /// из которых он осуществляет отгрузку.
        /// (Для такси списки не строятся)
        /// </summary>
        /// <param name="shops">Магазины</param>
        /// <param name="courierDeliveryCount">Количество магазинов в списке курьера</param>
        /// <param name="courierDeliveryMap">Списки магазинов для всех курьеров</param>
        /// <returns></returns>
        private static int CreateCourierDeliveryMap(CourierDeliveryInfo[][] shopDeliveryInfo, int[] courierDeliveryCount, int[,] courierDeliveryMap)
        {
            // 1. Инициализация
            int rc = 1;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (shopDeliveryInfo == null || shopDeliveryInfo.Length <= 0)
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

                for (int i = 0; i < shopDeliveryInfo.Length; i++)
                {
                    // 4.1 Извлекаем возможные отгрузки магазина
                    rc = 41;
                    CourierDeliveryInfo[] possibleDelivery = shopDeliveryInfo[i];
                    if (possibleDelivery == null || possibleDelivery.Length <= 0)
                        continue;

                    // 4.2. Заносим магазин в список курьера, участвующего в отгрузке
                    rc = 42;

                    for (int j = 0; j < possibleDelivery.Length; j++)
                    {
                        Courier courier = possibleDelivery[j].DeliveryCourier;
                        if (courier.Status == CourierStatus.Ready && !courier.IsTaxi)
                        {
                            int courierIndex = courier.Id - 1;
                            if (courierIndex == 17)
                            {
                                rc = rc;
                            }
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

        /// <summary>
        /// Выбор курьеров c отгрузками
        /// из нескольких магазинов
        /// </summary>
        /// <param name="courierDeliveryCount">Количество магазинов, в отгрузке из которых участвует курьер</param>
        /// <param name="courierIndex">Индексы выбранных курьеров</param>
        /// <param name="deliveryCount">Счётчики отгрузок выбранных курьеров</param>
        /// <returns>0 - курьеры выбраны; иначе - курьеры не выбраны</returns>
        private static int SelectMultiDeliveryCouriers(int[] courierDeliveryCount, out int[] courierIndex, out int[] deliveryCount)
        {
            // 1. Инициализация
            int rc = 1;
            courierIndex = null;
            deliveryCount = null;


            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (courierDeliveryCount == null || courierDeliveryCount.Length <= 0)
                    return rc;

                // 3. Выбираем индексы курьров, участвующих в доставке из нескольких магазинов
                rc = 3;
                int courierCount = courierDeliveryCount.Length;
                courierIndex = new int[courierCount];
                deliveryCount = new int[courierCount];
                int count = 0;

                for (int i = 0; i < courierCount; i++)
                {
                    if (courierDeliveryCount[i] > 1)
                    {
                        courierIndex[count] = i;
                        deliveryCount[count++] = courierDeliveryCount[i];
                    }
                }

                // 4. Отрезаем пустой хвост
                rc = 4;
                if (count < courierIndex.Length)
                {
                    Array.Resize(ref courierIndex, count);
                    Array.Resize(ref deliveryCount, count);
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

                // 4. Находим индекс магазина в массиве всех магазинов
                rc = 4;
                int shopId = args.Shop.N;
                int shopIndex = -1;

                for (int i = 0; i < allShops.Length; i++)
                {
                    if (allShops[i].N == shopId)
                    {
                        shopIndex = i;
                        break;
                    }
                }

                if (shopIndex < 0)
                {
                    rc = rc;
                    return rc;
                }

                // 5. Обновляем возможные отгрузки
                rc = 5;
                bool[] isShopDeliveryUpdated = new bool[allShops.Length];
                isShopDeliveryUpdated[shopIndex] = true;
                int rc1 = PossibleDeliveryOptimizer(item.EventTime, allShops, allCouriers, queue, isShopDeliveryUpdated, courierDeliveryCount, courierDeliveryMap);
                if (rc1 != 0)
                {
                    rc1 = rc1;
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
                //if (args.Courier.Id == 225)
                //{
                //    rc = rc;
                //}

                item.Status = QueueItemStatus.Executed;

                // 4. Начинаем рабочий день
                rc = 4;
                CourierEx courier = args.Courier;
                if (courier.IsTaxi)
                    return rc = 0;

                courier.BeginWork(item.EventTime, args.Latitude, args.Longitude);

                // 5. Обновляем возможные отгрузки
                rc = 5;
                bool[] isShopDeliveryUpdated = allShops.Select(p => p.IsCourierEnabled(courier)).ToArray();
                //int cnt = isShopDeliveryUpdated.Count(p => p);

                int rc1 = PossibleDeliveryOptimizer(item.EventTime, allShops, allCouriers, queue, isShopDeliveryUpdated, courierDeliveryCount, courierDeliveryMap);
                if (rc1 != 0)
                {
                    rc1 = rc1;
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
        private int QueueEvent_CourierWorkEnd_old(QueueItem item, ShopEx[] allShops, CourierEx[] allCouriers)
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

                // 4. Пересчитываем отгрузки
                rc = 4;
                CourierEx courier = args.Courier;
                courier.EndWork(item.EventTime, args.Latitude, args.Longitude);

                bool[] isShopDeliveryUpdated = allShops.Select(p => p.IsPossibleCourier(courier.Id)).ToArray();
                int rc1 = PossibleDeliveryOptimizer(item.EventTime, allShops, allCouriers, queue, isShopDeliveryUpdated, courierDeliveryCount, courierDeliveryMap);
                if (rc1 != 0)
                {
                    rc1 = rc1;
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

                foreach (CourierDeliveryInfo possibleDelivery in args.Delivery)
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
                    else if (!possibleDelivery.DeliveryCourier.IsTaxi && possibleDelivery.OrderCost <= 0.9 * sender.GetOrderAverageCost(possibleDelivery.DeliveryCourier))
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
            { }
        }

        /// <summary>
        /// Событие 'Возможные отгрузки такси'
        /// </summary>
        /// <param name="sender">Магазин</param>
        /// <param name="args">Аргументы события</param>
        private void Shop_TaxiDeliveryAlert(ShopEx sender, ShopTaxiDeliveryAlertEventArgs args)
        {
            // 1. Инициализация
            try
            {
                // 2. Проверяем исходные данные
                if (sender == null)
                    return;
                if (args.Taxi == null)
                    return;
                if (args == null || args.TaxiDelivery == null || args.TaxiDelivery.Length <= 0)
                    return;

                // 3. Добавляем события в очередь
                foreach (CourierDeliveryInfo possibleDelivery in args.TaxiDelivery)
                {
                    QueueItemDeliveryAlertArgs alertArgs = new QueueItemDeliveryAlertArgs(sender, possibleDelivery);
                    DateTime alertTime = possibleDelivery.EndDeliveryInterval.AddSeconds(-1);
                    queue.AddEvent(alertTime, QueueItemType.TaxiDeliveryAlert, alertArgs);
                }
            }
            catch
            { }
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
            { }
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
                    shop.TaxiDeliveryAlert += Shop_TaxiDeliveryAlert;
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
                    shop.TaxiDeliveryAlert -= Shop_TaxiDeliveryAlert;
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
