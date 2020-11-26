
namespace CourierLogistics.Logistics.FloatSolution.ShopInLive
{
    using CourierLogistics.Logistics.FloatOptimalSolution;
    using CourierLogistics.Logistics.FloatSolution.CourierInLive;
    using CourierLogistics.Logistics.FloatSolution.OrdersDeliverySolution;
    using CourierLogistics.Logistics.OptimalSingleShopSolution;
    using CourierLogistics.Logistics.OptimalSingleShopSolution.PermutationsRepository;
    using CourierLogistics.Logistics.RealSingleShopSolution;
    using CourierLogistics.SourceData.Couriers;
    using CourierLogistics.SourceData.Orders;
    using CourierLogistics.SourceData.Shops;
    using System;
    using System.Linq;

    public delegate void ShopOrderAssembledEvent(ShopEx sender, ShopOrderAssembledEventArgs args);
    public delegate void ShopPossibleDeliveryEvent(ShopEx sender, ShopPossibleDeliveryEventArgs args);
    public delegate void ShopTaxiDeliveryAlertEvent(ShopEx sender, ShopTaxiDeliveryAlertEventArgs args);

    /// <summary>
    /// Расширенный магазин
    /// </summary>
    public class ShopEx : Shop
    {
        /// <summary>
        /// Событие сборки очередного закза
        /// </summary>
        public event ShopOrderAssembledEvent OrderAssembled;

        /// <summary>
        /// Событие построения возможных отгрузок
        /// </summary>
        public event ShopPossibleDeliveryEvent PossibleDelivery;

        /// <summary>
        /// Событие - возможные отгрузки с помощью такси
        /// </summary>
        public event ShopTaxiDeliveryAlertEvent TaxiDeliveryAlert;

        /// <summary>
        /// Репозиторий с перестановками до 8 элементов
        /// (8! = 40320)
        /// </summary>
        public static Permutations PermutationsRepository = new Permutations();

        /// <summary>
        /// Время последнего события
        /// </summary>
        public DateTime LastEventTime { get; private set; }

        /// <summary>
        /// Последнее событие в магазине
        /// </summary>
        public ShopEventType LastEvent { get; private set; }

        /// <summary>
        /// Время последней отгрузки
        /// </summary>
        public DateTime LastDeliveryTime { get; private set; }

        /// <summary>
        /// Последняя отгрузка
        /// </summary>
        public CourierDeliveryInfo LastDelivery { get; private set; }

        /// <summary>
        /// Возможные варианты отгрузок разными курьерами и такси
        /// </summary>
        public CourierDeliveryInfo[] PossibleShipment { get; private set; }

        /// <summary>
        /// Общая стоимость всех возможных отгрузок
        /// </summary>
        public double PossibleShipmentCost => (PossibleShipment == null ? 0 : PossibleShipment.Sum(p => p.Cost));

        /// <summary>
        /// Общее количество заказов
        /// </summary>
        public int PossibleShipmentOrderCount => (PossibleShipment == null ? 0 : PossibleShipment.Sum(p => p.OrderCount));

        /// <summary>
        /// Средняя стоимость доставки заказа по всем отгрузкам
        /// </summary>
        public double PossibleShipmentAverageOrderCost
        {
            get
            {
                // 2. Проверяем исходные данные
                if (PossibleShipment == null || PossibleShipment.Length <= 0)
                    return 0;

                // 3. Подсчитываем общее число заказов и общую сумму
                double cost = 0;
                int orderCount = 0;

                for (int i = 0; i < PossibleShipment.Length; i++)
                {
                    CourierDeliveryInfo deliveryInfo = PossibleShipment[i];
                    cost += deliveryInfo.Cost;
                    orderCount += deliveryInfo.OrderCount;
                }

                if (orderCount <= 0)
                    return 0;

                return cost / orderCount;
            }
        }

        /// <summary>
        /// Выбор возможных отгрузок
        /// </summary>
        /// <param name="includeTaxi"></param>
        /// <returns></returns>
        public ShopPossibleDelivery[] SelectPossibleDeliveries(bool includingTaxi = false)
        {
            if (PossibleShipment == null || PossibleShipment.Length <= 0)
                return new ShopPossibleDelivery[0];

            if (includingTaxi)
            {
                return PossibleShipment.Select(p => new ShopPossibleDelivery(this, p)).ToArray();
            }
            else
            {
                return PossibleShipment.Where(p => !p.DeliveryCourier.IsTaxi).Select(p => new ShopPossibleDelivery(this, p)).ToArray();
            }
        }

        #region Данные о доставке разными типами курьеров

        /// <summary>
        /// Типы курьров для которых просчитана
        /// доставка всех заказаов магазина
        /// </summary>
        private Courier[] allTypeCouriers;

        /// <summary>
        /// CourierDeliveryInfo[i] - данные по доставке всех товаров курьером allTypeCouriers[i]
        /// </summary>
        private CourierDeliveryInfo[][] allTypeCourierDeliveryInfo;

        /// <summary>
        /// allTypeCourierCover[i] - отгрузки образующие покрытие всех заказов для курьера allTypeCouriers[i]
        /// </summary>
        private CourierDeliveryInfo[][] allTypeCourierCover;

        /// <summary>
        /// Покрытие для заданного типа курьера
        /// </summary>
        /// <param name="courierTypeIndex">Индекс курьера в allTypeCouriers</param>
        /// <returns></returns>
        public CourierDeliveryInfo[] GetCourierCover(int courierTypeIndex) => allTypeCourierCover[courierTypeIndex];

        /// <summary>
        /// Покрытие для заданного типа курьера
        /// </summary>
        /// <param name="courierTypeIndex">Индекс курьера в allTypeCouriers</param>
        /// <returns></returns>
        public CourierDeliveryInfo[] GetDeliveryInfo(int courierTypeIndex) => allTypeCourierDeliveryInfo[courierTypeIndex];

        /// <summary>
        /// Заказы за один день
        /// </summary>
        Order[] shopOrders;

        public Order[] ShopOrders => shopOrders;

        public int OrderCount => (shopOrders == null ? 0 : shopOrders.Length);

        #endregion Данные о доставке разными типами курьеров

        /// <summary>
        /// Конструктор класса ShopEx
        /// </summary>
        public ShopEx() : base()
        { }

        /// <summary>
        /// Заказ собран
        /// </summary>
        /// <param name="assembledTime">Время сборки</param>
        /// <param name="couriers">Курьеры</param>
        /// <returns>0 - новые возможные отгрузки построены; иначе - новые возможные отгрузки не построены</returns>
        public int ShopOrderAssembled(DateTime assembledTime, CourierEx[] couriers)
        {
            int rc = CreatePossibleDeliveries(assembledTime, couriers);
            LastEventTime = assembledTime;
            LastEvent = ShopEventType.OrderAssembled;
            return rc;
        }

        //public int ShipmentStarted(DateTime startedTime, CourierDeliveryInfo delivery, CourierEx[] couriers)
        //{
        //    return CreatePossibleDeliveries(startedTime, couriers);
        //}

        /// <summary>
        /// Освободился курьер
        /// </summary>
        /// <param name="startedTime">Время начала отгрузки (время события)</param>
        /// <param name="courier">Освободившийся курьер</param>
        /// <param name="couriers">Свободные курьеры</param>
        /// <returns></returns>
        public int CourierReleased(DateTime startedTime, CourierEx courier, CourierEx[] couriers)
        {
            return CreatePossibleDeliveries(startedTime, couriers);
        }

        ///// <summary>
        ///// Начать новый день
        ///// </summary>
        ///// <param name="oneDayOrders">Заказы магазина за один день</param>
        ///// <returns>0</returns>
        //public int StartDay_old(Order[] oneDayOrders)
        //{
        //    // 1. Инициализация
        //    int rc = 1;
        //    allTypeCouriers = null;
        //    allTypeCouriersDeliveryInfo = null;
        //    shopOrders = oneDayOrders;

        //    try
        //    {
        //        // 2. Проверяем исходные данные
        //        rc = 2;
        //        if (oneDayOrders == null || oneDayOrders.Length <= 0)
        //            return rc;

        //        // 3. Расчитываем расстояние от магазина до всех точек доставки
        //        rc = 3;
        //        double shopLatitude = Latitude;
        //        double shopLongitude = Longitude;
        //        double[] distanceFromShop = new double[oneDayOrders.Length];
        //        ShopOrderAssembledEventArgs assembledEventArgs = new ShopOrderAssembledEventArgs(DateTime.Now, null);

        //        for (int i = 0; i < oneDayOrders.Length; i++)
        //        {
        //            Order order = oneDayOrders[i];

        //            assembledEventArgs.EventTime = order.Date_collected;
        //            assembledEventArgs.ShopOrder = order;
        //            OrderAssembled?.Invoke(this, assembledEventArgs);

        //            order.Completed = false;
        //            distanceFromShop[i] = FloatSolutionParameters.DISTANCE_ALLOWANCE * Helper.Distance(shopLatitude, shopLongitude, order.Latitude, order.Longitude);
        //        }

        //        // 4. Создаём разные типы курьеров и такси
        //        rc = 4;
        //        Courier gettCourier = new Courier(1, new CourierType_GettTaxi());
        //        gettCourier.Status = CourierStatus.Ready;
        //        gettCourier.WorkStart = new TimeSpan(0);
        //        gettCourier.WorkEnd = new TimeSpan(24, 0, 0);

        //        Courier yandexCourier = new Courier(2, new CourierType_YandexTaxi());
        //        yandexCourier.Status = CourierStatus.Ready;
        //        yandexCourier.WorkStart = new TimeSpan(0);
        //        yandexCourier.WorkEnd = new TimeSpan(24, 0, 0);

        //        Courier carCourier = new Courier(3, new CourierType_Car());
        //        carCourier.Status = CourierStatus.Ready;
        //        carCourier.WorkStart = new TimeSpan(0);
        //        carCourier.WorkEnd = new TimeSpan(24, 0, 0);

        //        Courier bicycleCourier = new Courier(4, new CourierType_Bicycle());
        //        bicycleCourier.Status = CourierStatus.Ready;
        //        bicycleCourier.WorkStart = new TimeSpan(0);
        //        bicycleCourier.WorkEnd = new TimeSpan(24, 0, 0);

        //        allTypeCouriers = new Courier[] { carCourier, bicycleCourier, gettCourier, yandexCourier };

        //        // 5. Расчитываем для каждого заказа данные для доставки всеми видами курьеров
        //        rc = 5;
        //        allTypeCouriersDeliveryInfo = new CourierDeliveryInfo[allTypeCouriers.Length][];
        //        CourierDeliveryInfo[] deliveryInfo = new CourierDeliveryInfo[oneDayOrders.Length];

        //        int count = 0;

        //        for (int i = 0; i < allTypeCouriers.Length; i++)
        //        {
        //            Courier courier = allTypeCouriers[i];
        //            count = 0;

        //            for (int j = 0; j < oneDayOrders.Length; j++)
        //            {
        //                Order order = oneDayOrders[j];
        //                DateTime assembledTime = order.Date_collected;

        //                if (assembledTime != DateTime.MinValue)
        //                {
        //                    DateTime deliveryTimeLimit = order.GetDeliveryLimit(FloatSolutionParameters.DELIVERY_LIMIT);
        //                    CourierDeliveryInfo courierDeliveryInfo;
        //                    double distance = distanceFromShop[j];
        //                    int rc1 = courier.DeliveryCheck(assembledTime, distance, deliveryTimeLimit, order.Weight, out courierDeliveryInfo);
        //                    if (rc1 == 0)
        //                    {
        //                        courierDeliveryInfo.ShippingOrder = order;
        //                        courierDeliveryInfo.DistanceFromShop = distance;
        //                        deliveryInfo[count++] = courierDeliveryInfo;
        //                    }
        //                    else
        //                    {
        //                        rc = rc;
        //                    }
        //                }
        //            }

        //            if (count > 0)
        //            {
        //                CourierDeliveryInfo[] courierInfo = deliveryInfo.Take(count).ToArray();
        //                Array.Sort(courierInfo, CompareByEndDeliveryInterval);
        //                allTypeCouriersDeliveryInfo[i] = courierInfo;
        //            }
        //            else
        //            {
        //                allTypeCouriersDeliveryInfo[i] = new CourierDeliveryInfo[0];
        //            }
        //        }

        //        // 6. Выход - Ok
        //        rc = 0;
        //        return rc;
        //    }
        //    catch
        //    {
        //        return rc;
        //    }
        //}

        /// <summary>
        /// Начать новый день
        /// </summary>
        /// <param name="oneDayOrders">Заказы магазина за один день</param>
        /// <param name="statistics">Статистика за прошлый день</param>
        /// <returns>0</returns>
        public int StartDay(Order[] oneDayOrders, ShopStatistics statistics)
        {
            // 1. Инициализация
            int rc = 1;
            allTypeCouriers = null;
            allTypeCourierDeliveryInfo = null;
            shopOrders = oneDayOrders;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (oneDayOrders == null || oneDayOrders.Length <= 0)
                    return rc;
                if (statistics == null)
                    return rc;

                // 3. Расчитываем расстояние от магазина до всех точек доставки
                rc = 3;
                double shopLatitude = Latitude;
                double shopLongitude = Longitude;
                double[] distanceFromShop = new double[oneDayOrders.Length];
                ShopOrderAssembledEventArgs assembledEventArgs = new ShopOrderAssembledEventArgs(DateTime.Now, null);

                for (int i = 0; i < oneDayOrders.Length; i++)
                {
                    Order order = oneDayOrders[i];
                    //if (order.Id_order == 2451189)
                    //{
                    //    i = i;
                    //}

                    assembledEventArgs.EventTime = order.Date_collected;
                    assembledEventArgs.ShopOrder = order;
                    OrderAssembled?.Invoke(this, assembledEventArgs);

                    order.Completed = false;
                    distanceFromShop[i] = FloatSolutionParameters.DISTANCE_ALLOWANCE * Helper.Distance(shopLatitude, shopLongitude, order.Latitude, order.Longitude);
                }

                // 4. Запрашиваем среднюю стоимость доставки заказа по типам курьеров
                rc = 4;
                DateTime shopDay = shopOrders[0].Date_collected.Date;
                double[] orderAverageCost;
                int rc1 = GetAverageOrderDeliveryCost(this, shopDay, statistics, out orderAverageCost);
                if (rc1 != 0)
                    return rc = 100 * rc + rc1;

                // 5. Добавляем отсутствующие типы курьеров
                rc = 5;
                Courier gettCourier = new Courier(1, new CourierType_GettTaxi());
                gettCourier.Status = CourierStatus.Ready;
                gettCourier.WorkStart = new TimeSpan(0);
                gettCourier.WorkEnd = new TimeSpan(24, 0, 0);
                gettCourier.AverageOrderCost = orderAverageCost[(int)gettCourier.CourierType.VechicleType];

                Courier yandexCourier = new Courier(2, new CourierType_YandexTaxi());
                yandexCourier.Status = CourierStatus.Ready;
                yandexCourier.WorkStart = new TimeSpan(0);
                yandexCourier.WorkEnd = new TimeSpan(24, 0, 0);
                yandexCourier.AverageOrderCost = orderAverageCost[(int)yandexCourier.CourierType.VechicleType];

                CourierEx carCourier = new CourierEx(3, new CourierType_Car());
                carCourier.Status = CourierStatus.Ready;
                carCourier.WorkStart = new TimeSpan(0);
                carCourier.WorkEnd = new TimeSpan(24, 0, 0);
                carCourier.AverageOrderCost = orderAverageCost[(int)carCourier.CourierType.VechicleType];

                Courier bicycleCourier = new Courier(4, new CourierType_Bicycle());
                bicycleCourier.Status = CourierStatus.Ready;
                bicycleCourier.WorkStart = new TimeSpan(0);
                bicycleCourier.WorkEnd = new TimeSpan(24, 0, 0);
                bicycleCourier.AverageOrderCost = orderAverageCost[(int)bicycleCourier.CourierType.VechicleType];

                Courier onFootCourier = new Courier(5, new CourierType_Bicycle());
                onFootCourier.Status = CourierStatus.Ready;
                onFootCourier.WorkStart = new TimeSpan(0);
                onFootCourier.WorkEnd = new TimeSpan(24, 0, 0);
                onFootCourier.AverageOrderCost = orderAverageCost[(int)onFootCourier.CourierType.VechicleType];

                allTypeCouriers = new Courier[] {gettCourier, yandexCourier, carCourier, bicycleCourier, onFootCourier };

                // 6. Расчитываем для каждого заказа данные для доставки всеми видами курьеров
                rc = 6;
                allTypeCourierDeliveryInfo = new CourierDeliveryInfo[allTypeCouriers.Length][];
                CourierDeliveryInfo[] deliveryInfo = new CourierDeliveryInfo[oneDayOrders.Length];

                int count = 0;

                for (int i = 0; i < allTypeCouriers.Length; i++)
                {
                    Courier courier = allTypeCouriers[i];
                    count = 0;

                    for (int j = 0; j < oneDayOrders.Length; j++)
                    {
                        Order order = oneDayOrders[j];
                        DateTime assembledTime = order.Date_collected;

                        if (assembledTime != DateTime.MinValue)
                        {
                            DateTime deliveryTimeLimit = order.GetDeliveryLimit(FloatSolutionParameters.DELIVERY_LIMIT);
                            CourierDeliveryInfo courierDeliveryInfo;
                            double distance = distanceFromShop[j];
                            rc1 = courier.DeliveryCheck(assembledTime, distance, deliveryTimeLimit, order.Weight, out courierDeliveryInfo);
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
                        allTypeCourierDeliveryInfo[i] = courierInfo;
                    }
                    else
                    {
                        allTypeCourierDeliveryInfo[i] = new CourierDeliveryInfo[0];
                    }
                }

                // 7. Сообщаем о возможных отгрузках Yandex-такси
                rc = 7;
                TaxiDeliveryAlert?.Invoke(this, new ShopTaxiDeliveryAlertEventArgs(allTypeCouriers[1], allTypeCourierDeliveryInfo[1]));


                //// Отметим заказы, которые не могут быть доставлены
                //int[] orderId = allTypeCouriersDeliveryInfo[0].Select(p => p.ShippingOrder.Id_order).ToArray();
                //Array.Sort(orderId);
                //foreach (Order order in oneDayOrders)
                //{
                //    if (Array.BinarySearch(orderId, order.Id_order) < 0)
                //        order.Source = -1;
                //}

                //CourierDeliveryInfo[] bestDelivery;
                //int rcx = ShopSolution.Create((CourierEx)allTypeCouriers[2], allTypeCourierDeliveryInfo[2], out bestDelivery);
                //int rcx = ShopSolution.CreateEx((CourierEx)allTypeCouriers[2], allTypeCourierDeliveryInfo[2], out bestDelivery);
                //int rcz = ShopSolution.CreateEz((CourierEx)allTypeCouriers[2], allTypeCourierDeliveryInfo[2], out bestDelivery);
                //int rcv = ShopSolution.CreateEv((CourierEx)allTypeCouriers[2], allTypeCourierDeliveryInfo[2], out bestDelivery);
                //CourierDeliveryInfo[] deliveryCover;
                //int rcu = ShopSolution.CreateOrderCoverEx((CourierEx)allTypeCouriers[2], allTypeCourierDeliveryInfo[2], out deliveryCover);

                // 8. Выход - Ok
                rc = 0;
                return rc;
            }
            catch
            {
                return rc;
            }
        }

        /// <summary>
        /// Начать новый день
        /// </summary>
        /// <param name="oneDayOrders">Заказы магазина за один день</param>
        /// <param name="statistics">Статистика за прошлый день</param>
        /// <returns>0</returns>
        public int StartOptimalDay(Order[] oneDayOrders, ShopStatistics statistics)
        {
            // 1. Инициализация
            int rc = 1;
            allTypeCouriers = null;
            allTypeCourierDeliveryInfo = null;
            shopOrders = oneDayOrders;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (oneDayOrders == null || oneDayOrders.Length <= 0)
                    return rc;

                // 3. Расчитываем расстояние от магазина до всех точек доставки
                rc = 3;
                double shopLatitude = Latitude;
                double shopLongitude = Longitude;
                double[] distanceFromShop = new double[oneDayOrders.Length];
                ShopOrderAssembledEventArgs assembledEventArgs = new ShopOrderAssembledEventArgs(DateTime.Now, null);

                for (int i = 0; i < oneDayOrders.Length; i++)
                {
                    Order order = oneDayOrders[i];
                    //if (order.Id_order == 2451189)
                    //{
                    //    i = i;
                    //}

                    assembledEventArgs.EventTime = order.Date_collected;
                    assembledEventArgs.ShopOrder = order;
                    OrderAssembled?.Invoke(this, assembledEventArgs);

                    order.Completed = false;
                    distanceFromShop[i] = FloatSolutionParameters.DISTANCE_ALLOWANCE * Helper.Distance(shopLatitude, shopLongitude, order.Latitude, order.Longitude);
                }

                // 4. Запрашиваем среднюю стоимость доставки заказа по типам курьеров
                rc = 4;
                DateTime shopDay = shopOrders[0].Date_collected.Date;
                double[] orderAverageCost;
                int rc1 = GetAverageOrderDeliveryCost(this, shopDay, statistics, out orderAverageCost);
                if (rc1 != 0)
                    return rc = 100 * rc + rc1;

                // 5. Создаём все типы курьеров
                rc = 5;
                CourierEx gettCourier = new CourierEx(1, new CourierType_GettTaxi());
                gettCourier.Status = CourierStatus.Ready;
                gettCourier.WorkStart = new TimeSpan(0);
                gettCourier.WorkEnd = new TimeSpan(24, 0, 0);
                gettCourier.AverageOrderCost = orderAverageCost[(int)gettCourier.CourierType.VechicleType];

                CourierEx yandexCourier = new CourierEx(2, new CourierType_YandexTaxi());
                yandexCourier.Status = CourierStatus.Ready;
                yandexCourier.WorkStart = new TimeSpan(0);
                yandexCourier.WorkEnd = new TimeSpan(24, 0, 0);
                yandexCourier.AverageOrderCost = orderAverageCost[(int)yandexCourier.CourierType.VechicleType];

                CourierEx carCourier = new CourierEx(3, new CourierType_Car());
                carCourier.Status = CourierStatus.Ready;
                carCourier.WorkStart = new TimeSpan(0);
                carCourier.WorkEnd = new TimeSpan(24, 0, 0);
                carCourier.AverageOrderCost = orderAverageCost[(int)carCourier.CourierType.VechicleType];

                Courier bicycleCourier = new Courier(4, new CourierType_Bicycle());
                bicycleCourier.Status = CourierStatus.Ready;
                bicycleCourier.WorkStart = new TimeSpan(0);
                bicycleCourier.WorkEnd = new TimeSpan(24, 0, 0);
                bicycleCourier.AverageOrderCost = orderAverageCost[(int)bicycleCourier.CourierType.VechicleType];

                Courier onFootCourier = new Courier(5, new CourierType_Bicycle());
                onFootCourier.Status = CourierStatus.Ready;
                onFootCourier.WorkStart = new TimeSpan(0);
                onFootCourier.WorkEnd = new TimeSpan(24, 0, 0);
                onFootCourier.AverageOrderCost = orderAverageCost[(int)onFootCourier.CourierType.VechicleType];

                allTypeCouriers = new Courier[] {gettCourier, yandexCourier, carCourier, bicycleCourier, onFootCourier };

                // 6. Расчитываем для каждого заказа данные для доставки всеми видами курьеров
                rc = 6;
                allTypeCourierDeliveryInfo = new CourierDeliveryInfo[allTypeCouriers.Length][];
                allTypeCourierCover = new CourierDeliveryInfo[allTypeCouriers.Length][];
                CourierDeliveryInfo[] deliveryInfo = new CourierDeliveryInfo[oneDayOrders.Length];

                int count = 0;

                for (int i = 0; i < allTypeCouriers.Length; i++)
                {
                    Courier courier = allTypeCouriers[i];
                    count = 0;

                    for (int j = 0; j < oneDayOrders.Length; j++)
                    {
                        Order order = oneDayOrders[j];
                        DateTime assembledTime = order.Date_collected;

                        if (assembledTime != DateTime.MinValue)
                        {
                            //DateTime deliveryTimeLimit = order.GetDeliveryLimit(FloatSolutionParameters.DELIVERY_LIMIT);
                            DateTime deliveryTimeLimit = order.DeliveryTimeLimit;
                            CourierDeliveryInfo courierDeliveryInfo;
                            double distance = distanceFromShop[j];
                            rc1 = courier.DeliveryCheck(assembledTime, distance, deliveryTimeLimit, order.Weight, out courierDeliveryInfo);
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
                        allTypeCourierDeliveryInfo[i] = courierInfo;
                    }
                    else
                    {
                        allTypeCourierDeliveryInfo[i] = new CourierDeliveryInfo[0];
                    }
                }

                //CourierDeliveryInfo[] deliveryCover;
                //int rcu = ShopSolution.CreateOrderCoverEx((CourierEx)allTypeCouriers[2], allTypeCourierDeliveryInfo[2], out deliveryCover);

                //if (rcu == 0 && deliveryCover != null)
                //{
                //    Console.WriteLine($"{DateTime.Now} > shop {this.N}. Order Count = {allTypeCourierDeliveryInfo[2].Length}. Cover count = {deliveryCover.Length}");
                //}
                //else
                //{
                //    Console.WriteLine($"{DateTime.Now} > shop {this.N}. Order Count = {allTypeCourierDeliveryInfo[2].Length}. rcu = {rcu}");
                //}


                // 8. Выход - Ok
                rc = 0;
                return rc;
            }
            catch
            {
                return rc;
            }
        }

        /// <summary>
        /// Построения покрытия для заказов магазина
        /// с заданным типом курьера
        /// </summary>
        /// <param name="courierTypeIndex">Индекс курьера в allTypeCouriers</param>
        /// <returns>0 - покрытие построено; иначе - покрытие не построено</returns>
        public int CreateCover(int courierTypeIndex)
        {
            // 1. Инициализация
            int rc = 1;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (allTypeCouriers == null || allTypeCouriers.Length <= 0)
                    return rc;
                if (courierTypeIndex < 0 || courierTypeIndex >= allTypeCouriers.Length)
                    return rc;

                // 3. Создаём покрытие
                rc = 3;
                return ShopSolution.CreateOrderCoverEx((CourierEx) allTypeCouriers[courierTypeIndex], allTypeCourierDeliveryInfo[courierTypeIndex], out allTypeCourierCover[courierTypeIndex]);
            }
            catch
            {
                return rc;
            }
        }

        /// <summary>
        /// Выбор первой свободной отгрузки из покрытия с минимальной средней стоимостью доставки заказа
        /// </summary>
        /// <param name="courierTypeIndex">Индекс типа курьера</param>
        /// <param name="arrivalTime">Вермя прибытия курьера в магазин</param>
        /// <param name="arrivalCost">Стоимость прибытия в магазин</param>
        /// <param name="timeCost">Стоимость простоя (руб/час)</param>
        /// <returns>Найденная отгрузка или null</returns>
        public CourierDeliveryInfo GetFreeCoverDeliveryWithMinOrderCost(int courierTypeIndex, DateTime arrivalTime, double arrivalCost, double timeCost)
        {
            // 1. Инициализация

            try
            {
                // 2. Проверяем исходные данные
                if (allTypeCourierCover == null || allTypeCourierCover.Length <= 0)
                    return null;
                if (courierTypeIndex < 0 || courierTypeIndex >= allTypeCourierCover.Length)
                    return null;
                CourierDeliveryInfo[] courierCover = allTypeCourierCover[courierTypeIndex];
                if (courierCover == null || courierCover.Length <= 0)
                    return null;

                // 3. Выбираем первую свободную отгрузку после заданного времени
                CourierDeliveryInfo freeDeliveryWithMonOrderCost = null;
                double minOrderCost = double.MaxValue;
                double orderCost;

                for (int i = 0; i < courierCover.Length; i++)
                {
                    // 3.1 Извлекаем отгрузку из покрытия
                    CourierDeliveryInfo courierCoverDelivery = courierCover[i];

                    // 3.2 Пропукаем отгрузку с доставленными заказами
                    if (courierCoverDelivery.ShippingOrder != null)
                    {
                        if (courierCoverDelivery.ShippingOrder.Completed)
                            continue;
                    }
                    else if (courierCoverDelivery.DeliveredOrders == null && courierCoverDelivery.DeliveredOrders.Length <= 0)
                    {
                        continue;
                    }
                    else if (courierCoverDelivery.DeliveredOrders[0].Completed)
                    {
                        continue;
                    }
                    
                    // 3.3 Выбираем отгрузку с минимальной средней стоимостью доставки заказа
                    if (arrivalTime >= courierCoverDelivery.StartDeliveryInterval && arrivalTime <= courierCoverDelivery.EndDeliveryInterval)
                    {
                        orderCost = (courierCoverDelivery.Cost + arrivalCost) / courierCoverDelivery.OrderCount;
                        if (orderCost < minOrderCost)
                        {
                            minOrderCost = orderCost;
                            freeDeliveryWithMonOrderCost = courierCoverDelivery;
                        }
                    }
                    else if (courierCoverDelivery.StartDeliveryInterval > arrivalTime)
                    {
                        orderCost = (courierCoverDelivery.Cost + arrivalCost + timeCost * (courierCoverDelivery.StartDeliveryInterval - arrivalTime).TotalHours) / courierCoverDelivery.OrderCount;
                        if (orderCost < minOrderCost)
                        {
                            minOrderCost = orderCost;
                            freeDeliveryWithMonOrderCost = courierCoverDelivery;
                        }
                    }
                }

                // 4. Возвращаем найденную отгрузку или null
                return freeDeliveryWithMonOrderCost;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Выбор первой свободной отгрузки из покрытия с минимальной средней стоимостью доставки заказа
        /// </summary>
        /// <param name="courierTypeIndex">Индекс типа курьера</param>
        /// <param name="arrivalTime">Вермя прибытия курьера в магазин</param>
        /// <param name="arrivalCost">Стоимость прибытия в магазин</param>
        /// <param name="timeCost">Стоимость простоя (руб/час)</param>
        /// <returns>Найденная отгрузка или null</returns>
        public CourierDeliveryInfo GetFirstFreeCoverDelivery(int courierTypeIndex, DateTime arrivalTime)
        {
            // 1. Инициализация

            try
            {
                // 2. Проверяем исходные данные
                if (allTypeCourierCover == null || allTypeCourierCover.Length <= 0)
                    return null;
                if (courierTypeIndex < 0 || courierTypeIndex >= allTypeCourierCover.Length)
                    return null;
                CourierDeliveryInfo[] courierCover = allTypeCourierCover[courierTypeIndex];
                if (courierCover == null || courierCover.Length <= 0)
                    return null;

                // 3. Выбираем первую свободную отгрузку после заданного времени
                for (int i = 0; i < courierCover.Length; i++)
                {
                    // 3.1 Извлекаем отгрузку из покрытия
                    CourierDeliveryInfo courierCoverDelivery = courierCover[i];

                    // 3.2 Пропукаем отгрузку с доставленными заказами
                    if (courierCoverDelivery.ShippingOrder != null)
                    {
                        if (courierCoverDelivery.ShippingOrder.Completed)
                            continue;
                    }
                    else if (courierCoverDelivery.DeliveredOrders == null && courierCoverDelivery.DeliveredOrders.Length <= 0)
                    {
                        continue;
                    }
                    else if (courierCoverDelivery.DeliveredOrders[0].Completed)
                    {
                        continue;
                    }

                    // 3.3 Выбираем первую подходящую отгрузку
                    //if (arrivalTime >= courierCoverDelivery.StartDeliveryInterval && arrivalTime <= courierCoverDelivery.EndDeliveryInterval)
                    //    return courierCoverDelivery;
                    //if (courierCoverDelivery.StartDeliveryInterval > arrivalTime)
                    //    return courierCoverDelivery;
                    if (arrivalTime <= courierCoverDelivery.EndDeliveryInterval)
                        return courierCoverDelivery;
                }

                // 4. Нет доступных отгрузок
                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Поиск наилучшей отгрузки для заданного курьера
        /// </summary>
        /// <param name="courier">Курьер</param>
        /// <param name="arrivalTime">Время прибытия в магазин или DateTime.MinValue для курьера начинающего рабочий день</param>
        /// <param name="maxWaitInterval">Максимальный интервал ожидания</param>
        /// <param name="orderLimit">Максимальное число заказов среди которых производится поиск наилучшей отгрузки</param>
        /// <param name="bestDelivery">Найденная отгрузка или null</param>
        /// <returns>0 - отгрузка найдена; отгрузка не найдена</returns>
        public int FindDeliveryWithMinAverageCost(CourierEx courier, DateTime arrivalTime, double arrivalCost, TimeSpan maxWaitInterval, int orderLimit, out CourierDeliveryInfo bestDelivery)
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
                if (orderLimit <= 0 || orderLimit > 100)
                    return rc;
                if (shopOrders == null || shopOrders.Length <= 0)
                    return rc;


                // 3. Отбираем исходные не отгруженные заказы
                rc = 3;
                CourierDeliveryInfo[] sourceOrders = new CourierDeliveryInfo[shopOrders.Length];
                int count = 0;
                int courierTypeIndex = GetCouriersDeliveryInfoIndex(courier);
                CourierDeliveryInfo[] courierDeliveryInfo = allTypeCourierDeliveryInfo[courierTypeIndex];

                DateTime timeStart = arrivalTime;

                if (arrivalTime == DateTime.MaxValue)
                {
                    DateTime firstStartDelivery = DateTime.MaxValue;

                    for (int i = 0; i < courierDeliveryInfo.Length; i++)
                    {
                        CourierDeliveryInfo delivInfo = courierDeliveryInfo[i];
                        if (!delivInfo.Completed)
                        {
                            if (delivInfo.StartDeliveryInterval < firstStartDelivery)
                            {
                                firstStartDelivery = delivInfo.StartDeliveryInterval;
                            }
                        }
                    }

                    if (firstStartDelivery == DateTime.MaxValue)
                        return rc;

                    timeStart = firstStartDelivery;
                }

                DateTime timeEnd = timeStart.Add(maxWaitInterval);

                for (int i = 0; i < courierDeliveryInfo.Length; i++)
                {
                    CourierDeliveryInfo delivInfo = courierDeliveryInfo[i];
                    if (!delivInfo.Completed && 
                        !(delivInfo.StartDeliveryInterval > timeEnd || delivInfo.EndDeliveryInterval < timeStart))
                    {
                        sourceOrders[count] = delivInfo;
                        if (++count >= orderLimit)
                            break;
                    }
                }

                if (count <= 0)
                    return rc;

                Array.Resize(ref sourceOrders, count);

                // 4. Находим оптимальные отгрузки
                rc = 4;
                CourierDeliveryInfo[] optimalDeliveries;
                int rc1 = ShopSolution.CreateEv(courier, sourceOrders, out optimalDeliveries);
                if (rc1 != 0)
                    return rc = 1000 * rc + rc1;
                if (optimalDeliveries == null || optimalDeliveries.Length <= 0)
                    return rc;

                // 5. Отбираем наилучшую отгрузку
                double hourCost = 0;
                if (!courier.IsTaxi)
                   hourCost = (courier.CourierType.Insurance + 1.0) * courier.CourierType.HourlyRate;
                double minAvgCost = double.MaxValue;

                for (int i = 0; i < optimalDeliveries.Length; i++)
                {
                    CourierDeliveryInfo delivInfo = optimalDeliveries[i];
                    DateTime startDelivery = delivInfo.StartDelivery;

                    if (arrivalTime != DateTime.MaxValue)
                    {
                        if (startDelivery >= arrivalTime)
                        {
                            delivInfo.Cost += (arrivalCost + hourCost * (startDelivery - arrivalTime).TotalHours);
                        }
                        else if (delivInfo.StartDelivery.Add(delivInfo.ReserveTime) >= arrivalTime)
                        {
                            delivInfo.Cost += arrivalCost;
                            delivInfo.StartDelivery = arrivalTime;
                        }
                        else
                        {
                            continue;
                        }
                    }

                    double avgCost = delivInfo.OrderCost;

                    if (avgCost < minAvgCost)
                    {
                        minAvgCost = avgCost;
                        bestDelivery = delivInfo;
                    }
                }

                if (!courier.IsTaxi)
                {
                    if (GetTaxiAverageOrderCost() < minAvgCost)
                        bestDelivery = null;
                }

                if (bestDelivery == null)
                    return rc;

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
        /// Поиск наилучшей отгрузки для заданного такси
        /// </summary>
        /// <param name="taxi">Такси</param>
        /// <param name="orderLimit">Максимальное число заказов среди которых производится поиск наилучшей отгрузки</param>
        /// <param name="bestDelivery">Найденная отгрузка или null</param>
        /// <returns>0 - отгрузка найдена; отгрузка не найдена</returns>
        public int FindOptimalTaxiDelivery(CourierEx taxi, int orderLimit, out CourierDeliveryInfo bestDelivery)
        {
            // 1. Инициализация
            int rc = 1;
            bestDelivery = null;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (taxi == null || !taxi.IsTaxi)
                    return rc;
                if (orderLimit <= 0 || orderLimit > 100)
                    return rc;
                if (shopOrders == null || shopOrders.Length <= 0)
                    return rc;

                // 3. Отбираем исходные не отгруженные заказы
                rc = 3;
                CourierDeliveryInfo[] sourceOrders = new CourierDeliveryInfo[orderLimit];
                int count = 0;
                int courierTypeIndex = GetCouriersDeliveryInfoIndex(taxi);
                CourierDeliveryInfo[] courierDeliveryInfo = allTypeCourierDeliveryInfo[courierTypeIndex];

                for (int i = 0; i < courierDeliveryInfo.Length && count < orderLimit; i++)
                {
                    CourierDeliveryInfo delivInfo = courierDeliveryInfo[i];
                    if (!delivInfo.Completed)
                        sourceOrders[count++] = delivInfo;
                }

                if (count <= 0)
                    return rc;

                Array.Resize(ref sourceOrders, count);

                // 4. Находим оптимальные отгрузки
                rc = 4;
                CourierDeliveryInfo[] optimalDeliveries;
                int rc1 = ShopSolution.CreateEv(taxi, sourceOrders, out optimalDeliveries);
                if (rc1 != 0)
                    return rc = 1000 * rc + rc1;
                if (optimalDeliveries == null || optimalDeliveries.Length <= 0)
                    return rc;

                // 5. Отбираем отгрузку c минимальной средней стоимостью доставки заказа
                rc = 5;
                double minOrderCost = double.MaxValue;

                for (int i = 0; i < optimalDeliveries.Length; i++)
                {
                    CourierDeliveryInfo delivInfo = optimalDeliveries[i];
                    if (delivInfo.OrderCost < minOrderCost)
                    {
                        minOrderCost = delivInfo.OrderCost;
                        bestDelivery = delivInfo;
                    }
                }

                if (bestDelivery != null)
                {
                    CourierEx delivCourier = bestDelivery.DeliveryCourier as CourierEx;
                    if (delivCourier == null)
                        bestDelivery.DeliveryCourier = taxi;
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
        /// Cредняя стоимость доставки такси 
        /// </summary>
        /// <returns>Средняя стоимость или double.MaxValue</returns>
        public double GetTaxiAverageOrderCost()
        {
            double cost = double.MaxValue;
            if (allTypeCouriers == null || allTypeCouriers.Length <= 0)
                return cost;
            foreach(Courier courier in allTypeCouriers)
            {
                if (courier.IsTaxi)
                {
                    double taxiOrderCost = courier.AverageOrderCost;

                    if (taxiOrderCost > 0 && taxiOrderCost < cost)
                        cost = courier.AverageOrderCost;
                }
            }

            return cost;
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
        /// Получить индекс информации о доставке
        /// для заданного курьера
        /// </summary>
        /// <param name="courier">Курьер</param>
        /// <returns>Индекс allTypeCouriersDeliveryInfo или -1</returns>
        public int GetCouriersDeliveryInfoIndex(Courier courier)
        {
            if (courier == null)
                return -1;
            if (allTypeCouriers == null)
                return -2;

            CourierVehicleType vehicleType = courier.CourierType.VechicleType;

            for (int i = 0; i < allTypeCouriers.Length; i++)
            {
                if (allTypeCouriers[i].CourierType.VechicleType == vehicleType)
                    return i;
            }

            return -3;
        }

        /// <summary>
        /// Проверка, что заказ с заданным Id
        /// входит в возможные отгрузки
        /// </summary>
        /// <param name="orderId">ID проверяемого заказа</param>
        /// <returns>true - заказ входит в возможные отгрузки; false - заказ не входит в возможные отгрузки</returns>
        public bool IsPossibleOrder(int orderId)
        {
            // 1. Инициализация

            try
            {
                // 2. Проверяем исходные данные
                if (PossibleShipment == null || PossibleShipment.Length <= 0)
                    return false;

                // 3. Цикл проверки, что курьер с заданным Id входит в возможные отгрузки
                for (int i = 0; i < PossibleShipment.Length; i++)
                {
                    CourierDeliveryInfo delivery = PossibleShipment[i];
                    if (delivery.ShippingOrder != null && delivery.ShippingOrder.Id_order == orderId)
                        return true;

                    if (delivery.DeliveredOrders != null && delivery.DeliveredOrders.Length > 0)
                    {
                        foreach(CourierDeliveryInfo di in delivery.DeliveredOrders)
                        {
                            if (di != null && di.ShippingOrder != null && di.ShippingOrder.Id_order == orderId)
                                return true;
                        }
                    }
                }

                // 4. Заказ не входит в возможные отгрузки
                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Выбор возможной отгрузки с данным заказом
        /// </summary>
        /// <param name="orderId">ID заказа</param>
        /// <returns>Отгрузка или null</returns>
        public CourierDeliveryInfo GetPossibleDeliveryWithOrder(int orderId)
        {
            // 1. Инициализация

            try
            {
                // 2. Проверяем исходные данные
                if (PossibleShipment == null || PossibleShipment.Length <= 0)
                    return null;

                // 3. Цикл проверки, что курьер с заданным Id входит в возможные отгрузки
                for (int i = 0; i < PossibleShipment.Length; i++)
                {
                    CourierDeliveryInfo delivery = PossibleShipment[i];
                    if (delivery.ShippingOrder != null && delivery.ShippingOrder.Id_order == orderId)
                        return delivery;

                    if (delivery.DeliveredOrders != null && delivery.DeliveredOrders.Length > 0)
                    {
                        foreach(CourierDeliveryInfo di in delivery.DeliveredOrders)
                        {
                            if (di != null && di.ShippingOrder != null && di.ShippingOrder.Id_order == orderId)
                                return delivery;
                        }
                    }
                }

                // 4. Заказ не входит в возможные отгрузки
                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Проверка, что курьер с заданным Id
        /// входит в возможные отгрузки
        /// </summary>
        /// <param name="courierId">ID проверяемого курьера</param>
        /// <returns>true - курьер входит в возможные отгрузки; false - курьер не входит в возможные отгрузки</returns>
        public bool IsPossibleCourier(int courierId)
        {
            // 1. Инициализация

            try
            {
                // 2. Проверяем исходные данные
                if (PossibleShipment == null || PossibleShipment.Length <= 0)
                    return false;

                // 3. Цикл проверки, что курьер с заданным Id входит в возможные отгрузки
                for (int i = 0; i < PossibleShipment.Length; i++)
                {
                    CourierDeliveryInfo delivery = PossibleShipment[i];
                    if (delivery.DeliveryCourier != null &&
                        delivery.DeliveryCourier.Id == courierId)
                        return true;
                }

                // 4. Курьер не входит в возможные отгрузки
                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Проверка, что курьеры с заданными Id
        /// входят в возможные отгрузки
        /// </summary>
        /// <param name="courierId">ID проверяемых курьеров</param>
        /// <returns>true - курьер входит в возможные отгрузки; false - курьер не входит в возможные отгрузки</returns>
        public bool IsPossibleCourier(int[] courierId)
        {
            // 1. Инициализация

            try
            {
                // 2. Проверяем исходные данные
                if (PossibleShipment == null || PossibleShipment.Length <= 0)
                    return false;
                if (courierId == null || courierId.Length <= 0)
                    return false;

                // 3. Цикл проверки, что курьер с заданным Id входит в возможные отгрузки
                for (int i = 0; i < PossibleShipment.Length; i++)
                {
                    CourierDeliveryInfo delivery = PossibleShipment[i];
                    if (delivery.DeliveryCourier != null)
                    {
                        if (Array.BinarySearch(courierId, delivery.DeliveryCourier.Id) >= 0)
                            return true;
                    }
                }

                // 4. Курьеры не входят в возможные отгрузки
                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Проверка курьера на возможность доставки
        /// заказов из данного магазина
        /// </summary>
        /// <param name="courier">Курьер</param>
        /// <returns>true - курьер может быть использован для доставки; false - курьер не может использоваться для доставки</returns>
        public bool IsCourierEnabled(CourierEx courier)
        {
            // 1. Инициализация
            
            try
            {
                // 2. Проверяем исходные данные
                if (courier == null)
                    return false;
                if (courier.IsTaxi)
                    return true;
                if (courier.Status != CourierStatus.Ready)
                    return false;

                // 3. Фильтруем по расстоянию
                double distance = FloatSolutionParameters.DISTANCE_ALLOWANCE * Helper.Distance(Latitude, Longitude, courier.Latitude, courier.Longitude);
                //if (distance == 0)
                //    distance = distance;
                return (distance <= FloatSolutionParameters.COURIER_DISTANCE_TO_SHOP_LIMIT);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Расстояние оот курьера до магазина
        /// </summary>
        /// <param name="courier">Курьер</param>
        /// <returns>Расстояние в км или double.MaxValue</returns>
        public double GetDistance(CourierEx courier)
        {
            // 1. Инициализация
            
            try
            {
                // 2. Проверяем исходные данные
                if (courier == null)
                    return double.MaxValue;
                if (courier.IsTaxi)
                    return double.MaxValue;
                if (courier.Status != CourierStatus.Ready)
                    return double.MaxValue;

                // 3. Фильтруем по расстоянию
                return FloatSolutionParameters.DISTANCE_ALLOWANCE * Helper.Distance(Latitude, Longitude, courier.Latitude, courier.Longitude);
            }
            catch
            {
                return double.MaxValue;
            }

        }

        /// <summary>
        /// Выбрать индексы элементов очереди событий
        /// связанных с возможными отгрузками
        /// </summary>
        /// <returns>Индексы элементов очереди, связанные с возможными отгрузками</returns>
        public int[] GetPossibleDeliveryQueueIndexes()
        {
            if (PossibleShipment == null || PossibleShipment.Length <= 0)
                return new int[0];
            return PossibleShipment.Select(p => p.QueueItemIndex).ToArray();
        }

        /// <summary>
        /// Выбрать свободных курьров участвующих
        /// в возможных отгрузках (без такси)
        /// </summary>
        /// <returns>Выбранные курьеры</returns>
        public CourierEx[] GetPossibleFreeCouriers()
        {
            if (PossibleShipment == null || PossibleShipment.Length <= 0)
                return new CourierEx[0];
            CourierEx[] couriers = new CourierEx[PossibleShipment.Length];
            int count = 0;

            for (int i = 0; i < PossibleShipment.Length; i++)
            {
                CourierEx courier = (CourierEx) PossibleShipment[i].DeliveryCourier;
                if (courier.Status == CourierStatus.Ready && !courier.IsTaxi)
                {
                    couriers[count++] = courier;
                }
            }

            if (count < couriers.Length)
            {
                Array.Resize(ref couriers, count);
            }

            return couriers;
        }

        public int AllDelivered()
        {
            //throw new NotImplementedException("AllDelivered is not implemented");
            return 0;
        }

        /// <summary>
        /// Создание оптимальных отгрузок всех
        /// заказов требующих отгрузки на заданный момент
        /// </summary>
        /// <param name="startTime">Момент времени</param>
        /// <param name="freeCouriers">Курьеры доступные для отгрузки</param>
        /// <param name="possibleDelivery">Построенные отгрузки</param>
        /// <returns>0 - отгрузки построены; отгрузки не построены</returns>
        public int CreatePossibleDeliveries(DateTime startTime, CourierEx[] freeCouriers)
        {
            // 1. Инициализация
            int rc = 1;
            PossibleShipment = null;
            Order[] availableOrders = null;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (freeCouriers == null || freeCouriers.Length <= 0)
                    return rc;
                freeCouriers = freeCouriers.Where(c => c.Status == CourierStatus.Ready).ToArray();
                if (freeCouriers.Length <= 0)
                    return rc;

                if (shopOrders == null || shopOrders.Length <= 0)
                    return rc;

                LastEventTime = startTime;
                LastEvent = ShopEventType.Recalc;

                // 3. Отбираем заказы требующие отгрузки на данный момент
                rc = 3;
                availableOrders = shopOrders.Where(p => !p.Completed && p.Date_collected <= startTime).ToArray();
                if (availableOrders == null || availableOrders.Length <= 0)
                {
                    PossibleShipment = new CourierDeliveryInfo[0];
                    return rc = 0;
                }

                // 4. Определяем время прибытия курьеров в магазин и индекс типа курьера среди allTypeCouriers
                rc = 4;
                for (int i = 0; i < freeCouriers.Length; i++)
                {
                    CourierEx courier = freeCouriers[i];
                    courier.CourierTypeIndex = GetCouriersDeliveryInfoIndex(courier);

                    if (!courier.IsTaxi)
                    { 
                        courier.ArrivalTime = startTime.AddMinutes(courier.GetArrivalTime(Latitude, Longitude));
                    }
                }

                // 5. Создаём объект для управления курьерами
                rc = 5;
                CourierStorage courierStorage = new CourierStorage();
                int rc1 = courierStorage.Create(freeCouriers);
                if (rc1 != 0)
                    return rc = 100 * rc + rc1;

                // 6. Строим возможные отгрузки курьерами
                rc = 6;
                CourierEx[] solutionCouriers;
                CourierDeliveryInfo[] possibleDelivery = new CourierDeliveryInfo[availableOrders.Length];
                int deliveryCount = 0;
                //double totalCost = 0;
                //int orderCount = 0;

                //while ((solutionCouriers = courierStorage.GetNextCouriers()) != null)
                while (true)
                {
                    // 6.0 
                    solutionCouriers = courierStorage.GetNextCouriers();
                    if (solutionCouriers == null || solutionCouriers.Length <= 0)
                        break;

                    // 6.1 Отбираем для каждого курьера заказы, доступные для отгрузки по его прбытии в магазин
                    rc = 61;
                    CourierDeliveryInfo[][] solutionDeliveryInfo = new CourierDeliveryInfo[solutionCouriers.Length][];

                    for (int i = 0; i < solutionCouriers.Length; i++)
                    {
                        CourierEx courier = solutionCouriers[i];
                        //solutionDeliveryInfo[i] = SelectAvailableOrders(courier.ArrivalTime, allTypeCouriersDeliveryInfo[courier.CourierTypeIndex]);
                        solutionDeliveryInfo[i] = SelectAvailableOrders(startTime, allTypeCourierDeliveryInfo[courier.CourierTypeIndex]);
                    }

                    // 6.2 Находим наилучшую отгрузку среди курьеров итерации
                    rc = 62;
                    CourierDeliveryInfo solutionDelivery;

                    rc1 = DeliverySolution.FindLocalSolutionAtTime(startTime, solutionCouriers, solutionDeliveryInfo, PermutationsRepository, out solutionDelivery);
                    if (rc1 != 0)
                        break;

                    possibleDelivery[deliveryCount++] = solutionDelivery;
                    courierStorage.IncrementPointer(((CourierEx)solutionDelivery.DeliveryCourier).CourierTypeIndex);

                    // 6.3 Помечаем заказы, как отгруженные
                    solutionDelivery.ShippingOrder.Completed = true;
                    if (solutionDelivery.DeliveredOrders != null && solutionDelivery.DeliveredOrders.Length > 0)
                    {
                        foreach(CourierDeliveryInfo delivery in solutionDelivery.DeliveredOrders)
                        {
                            delivery.Completed = true;
                        }
                    }
                }

                // 7. Строим возможные отгрузки такси
                rc = 7;
                CourierEx[] solutionTaxi = courierStorage.GetNextTaxi();

                if (solutionTaxi != null && solutionTaxi.Length > 0)
                {
                    CourierDeliveryInfo[][] solutionDeliveryInfo = new CourierDeliveryInfo[solutionTaxi.Length][];

                    for (int iter = 0; iter < availableOrders.Length; iter++)
                    {
                        // 7.1 Отбираем для каждого такси заказы, доступные для отгрузки
                        rc = 71;
                        for (int j = 0; j < solutionTaxi.Length; j++)
                        {
                            CourierEx courier = solutionTaxi[j];
                            solutionDeliveryInfo[j] = SelectAvailableOrders(startTime, allTypeCourierDeliveryInfo[courier.CourierTypeIndex]);
                        }

                        // 7.2 Находим наилучшую отгрузку среди такси
                        rc = 72;
                        CourierDeliveryInfo solutionDelivery;

                        rc1 = DeliverySolution.FindLocalSolutionAtTime(startTime, solutionTaxi, solutionDeliveryInfo, PermutationsRepository, out solutionDelivery);
                        if (rc1 != 0)
                            break;

                        possibleDelivery[deliveryCount++] = solutionDelivery;

                        // 7.3 Помечаем заказы, как отгруженные
                        solutionDelivery.ShippingOrder.Completed = true;
                        if (solutionDelivery.DeliveredOrders != null && solutionDelivery.DeliveredOrders.Length > 0)
                        {
                            foreach(CourierDeliveryInfo delivery in solutionDelivery.DeliveredOrders)
                            {
                                delivery.Completed = true;
                            }
                        }
                    }
                }

                if (deliveryCount < possibleDelivery.Length)
                {
                    Array.Resize(ref possibleDelivery, deliveryCount);
                }

                PossibleShipment = possibleDelivery;

                // 8. Обработка заказов, которые не могут быть доставлены в срок
                //    !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                rc = 8;

                // 9. Сбрасываем Completed
                rc = 9;
                for (int i = 0; i < availableOrders.Length; i++)
                    availableOrders[i].Completed = false;

                // 10. Генерируем событие
                rc = 10;
                if (deliveryCount > 0)
                {
                    PossibleDelivery?.Invoke(this, new ShopPossibleDeliveryEventArgs(startTime, possibleDelivery));
                }

                // 11. Выход - Ok
                rc = 0;
                return rc;
            }
            catch
            {
                if (availableOrders != null && availableOrders.Length > 0)
                {
                    for (int i = 0; i < availableOrders.Length; i++)
                        availableOrders[i].Completed = false;
                }
                return rc;
            }
            //finally
            //{
            //}
        }

        //rc1 = shops[i].UpdatePossibleDelivery(startTime, iterPossibleDeliveries[i]);
        public int UpdatePossibleDelivery(DateTime startTime, CourierDeliveryInfo[] possibleDelivery)
        {
            // 1. Инициализация
            int rc = 1;

            try
            {
                // 2. Замещаем возможные доставки
                rc = 2;
                PossibleShipment = possibleDelivery;

                // 3. Генерируем событие
                rc = 3;
                if (possibleDelivery != null && possibleDelivery.Length > 0)
                {
                    PossibleDelivery?.Invoke(this, new ShopPossibleDeliveryEventArgs(startTime, possibleDelivery));
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
        /// Создание оптимальных отгрузок всех
        /// заказов требующих отгрузки на заданный момент
        /// </summary>
        /// <param name="startTime">Момент времени</param>
        /// <param name="freeCouriers">Курьеры доступные для отгрузки</param>
        /// <param name="possibleDelivery">Построенные отгрузки</param>
        /// <returns>0 - отгрузки построены; отгрузки не построены</returns>
        public int CreatePossibleDeliveriesEx(DateTime startTime, CourierEx[] freeCouriers, out CourierDeliveryInfo[] possibleDelivery)
        {
            // 1. Инициализация
            int rc = 1;
            possibleDelivery = new CourierDeliveryInfo[0];
            Order[] availableOrders = null;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (freeCouriers == null || freeCouriers.Length <= 0)
                    return rc;

                if (shopOrders == null || shopOrders.Length <= 0)
                    return rc;

                // 3. Отбираем заказы требующие отгрузки на данный момент
                rc = 3;
                availableOrders = shopOrders.Where(p => !p.Completed && p.Date_collected <= startTime).ToArray();
                if (availableOrders == null || availableOrders.Length <= 0)
                {
                    return rc = 0;
                }

                // 4. Определяем время прибытия курьеров в магазин и индекс типа курьера среди allTypeCouriers
                rc = 4;
                for (int i = 0; i < freeCouriers.Length; i++)
                {
                    CourierEx courier = freeCouriers[i];
                    courier.CourierTypeIndex = GetCouriersDeliveryInfoIndex(courier);

                    if (!courier.IsTaxi)
                    { 
                        courier.ArrivalTime = startTime.AddMinutes(courier.GetArrivalTime(Latitude, Longitude));
                    }
                }

                // 5. Создаём объект для управления курьерами
                rc = 5;
                CourierStorage courierStorage = new CourierStorage();
                int rc1 = courierStorage.Create(freeCouriers);
                if (rc1 != 0)
                    return rc = 100 * rc + rc1;

                // 6. Строим возможные отгрузки курьерами
                rc = 6;
                CourierEx[] solutionCouriers;
                possibleDelivery = new CourierDeliveryInfo[availableOrders.Length];
                int deliveryCount = 0;

                while (true)
                {
                    // 6.0 
                    solutionCouriers = courierStorage.GetNextCouriers();
                    if (solutionCouriers == null || solutionCouriers.Length <= 0)
                        break;

                    // 6.1 Отбираем для каждого курьера заказы, доступные для отгрузки на момент его старта из текущей точки
                    rc = 61;
                    CourierDeliveryInfo[][] solutionDeliveryInfo = new CourierDeliveryInfo[solutionCouriers.Length][];

                    for (int i = 0; i < solutionCouriers.Length; i++)
                    {
                        CourierEx courier = solutionCouriers[i];
                        //solutionDeliveryInfo[i] = SelectAvailableOrders(courier.ArrivalTime, allTypeCouriersDeliveryInfo[courier.CourierTypeIndex]);
                        solutionDeliveryInfo[i] = SelectAvailableOrders(startTime, allTypeCourierDeliveryInfo[courier.CourierTypeIndex]);
                    }

                    // 6.2 Находим наилучшую отгрузку среди курьеров итерации
                    rc = 62;
                    CourierDeliveryInfo solutionDelivery;

                    rc1 = DeliverySolution.FindLocalSolutionAtTime(startTime, solutionCouriers, solutionDeliveryInfo, PermutationsRepository, out solutionDelivery);
                    if (rc1 != 0)
                        break;

                    possibleDelivery[deliveryCount++] = solutionDelivery;
                    courierStorage.IncrementPointer(((CourierEx)solutionDelivery.DeliveryCourier).CourierTypeIndex);

                    // 6.3 Помечаем заказы, как отгруженные
                    solutionDelivery.ShippingOrder.Completed = true;
                    if (solutionDelivery.DeliveredOrders != null && solutionDelivery.DeliveredOrders.Length > 0)
                    {
                        foreach(CourierDeliveryInfo delivery in solutionDelivery.DeliveredOrders)
                        {
                            delivery.Completed = true;
                        }
                    }
                }

                // 7. Строим возможные отгрузки такси
                rc = 7;
                CourierEx[] solutionTaxi = courierStorage.GetNextTaxi();

                if (solutionTaxi != null && solutionTaxi.Length > 0)
                {
                    CourierDeliveryInfo[][] solutionDeliveryInfo = new CourierDeliveryInfo[solutionTaxi.Length][];

                    for (int iter = 0; iter < availableOrders.Length; iter++)
                    {
                        // 7.1 Отбираем для каждого такси заказы, доступные для отгрузки
                        rc = 71;
                        for (int j = 0; j < solutionTaxi.Length; j++)
                        {
                            CourierEx courier = solutionTaxi[j];
                            solutionDeliveryInfo[j] = SelectAvailableOrders(startTime, allTypeCourierDeliveryInfo[courier.CourierTypeIndex]);
                        }

                        // 7.2 Находим наилучшую отгрузку среди такси
                        rc = 72;
                        CourierDeliveryInfo solutionDelivery;

                        rc1 = DeliverySolution.FindLocalSolutionAtTime(startTime, solutionTaxi, solutionDeliveryInfo, PermutationsRepository, out solutionDelivery);
                        if (rc1 != 0)
                            break;

                        possibleDelivery[deliveryCount++] = solutionDelivery;

                        // 7.3 Помечаем заказы, как отгруженные
                        solutionDelivery.ShippingOrder.Completed = true;
                        if (solutionDelivery.DeliveredOrders != null && solutionDelivery.DeliveredOrders.Length > 0)
                        {
                            foreach(CourierDeliveryInfo delivery in solutionDelivery.DeliveredOrders)
                            {
                                delivery.Completed = true;
                            }
                        }
                    }
                }

                if (deliveryCount < possibleDelivery.Length)
                {
                    Array.Resize(ref possibleDelivery, deliveryCount);
                }


                // 8. Обработка заказов, которые не могут быть доставлены в срок
                //    !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                rc = 8;

                // 9. Сбрасываем Completed
                rc = 9;
                for (int i = 0; i < availableOrders.Length; i++)
                    availableOrders[i].Completed = false;

                // 10. Выход - Ok
                rc = 0;
                return rc;
            }
            catch
            {
                if (availableOrders != null && availableOrders.Length > 0)
                {
                    for (int i = 0; i < availableOrders.Length; i++)
                        availableOrders[i].Completed = false;
                }
                return rc;
            }
            //finally
            //{
            //}
        }

        /// <summary>
        /// Построение лучшей отгрузки для курьера
        /// находящегося в магазине или такси
        /// </summary>
        /// <param name="startTime">Момент расчета</param>
        /// <param name="courier">Курьер или такси</param>
        /// <returns>Построенная отгрузка или null</returns>
        public CourierDeliveryInfo GetBestCourierDelivery(DateTime startTime, CourierEx courier)
        {
            // 1. Инициализация

            try
            {
                // 2. Проверяем исходные данные
                if (courier == null)
                    return null;

                // 3. Отбираем заказы доступные для отгрузки
                CourierDeliveryInfo[][] solutionDeliveryInfo = new CourierDeliveryInfo[1][];
                solutionDeliveryInfo[0] = SelectAvailableOrders(startTime, allTypeCourierDeliveryInfo[courier.CourierTypeIndex]);

                // 4. Находим наилучшую отгрузку
                CourierDeliveryInfo solutionDelivery;
                int rc1 = DeliverySolution.FindLocalSolutionAtTime(startTime, new CourierEx[] { courier}, solutionDeliveryInfo, PermutationsRepository, out solutionDelivery);
                if (rc1 != 0)
                    return null;

                // 5. Выход - Ok
                return solutionDelivery;
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
        /// Подсчет средней стоимость доставки заказа
        /// в выбранный день по типам курьеров
        /// </summary>
        /// <param name="shop">Магазин</param>
        /// <param name="day">День</param>
        /// <param name="statistics">Накопленная статистика</param>
        /// <param name="averageOrderCost">Средняя стоимость доставки заказа для каждого типа курьера</param>
        /// <returns>0 - средняя стоимость подсчитана; иначе - средняя стоимость не подсчитана</returns>
        private static int GetAverageOrderDeliveryCost(Shop shop, DateTime day, ShopStatistics statistics, out double[] averageOrderCost)
        {
            // 1. Инициализация
            int rc = 1;
            averageOrderCost = null;

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

                // 4. Подсчитываем общую стоимость доставки и общее число заказов
                //    для каждого типа курьера
                rc = 4;
                int maxValue = Enum.GetValues(typeof(CourierVehicleType)).Cast<int>().Max();
                double[] totalTypeCost = new double[maxValue + 1];
                int[] typeOrders = new int[maxValue + 1];

                for (int i = 0; i < shopDayStatistics.Length; i++)
                {
                    ShopCourierStatistics courierStatistics = shopDayStatistics[i];
                    Courier courier = courierStatistics.ShopCourier;

                    int vehicleTypeValue = (int)courier.CourierType.VechicleType;
                    totalTypeCost[vehicleTypeValue] += courierStatistics.TotalCost;
                    typeOrders[vehicleTypeValue] += courierStatistics.OrderCount;
                }

                // 5. Подсчитываем среднюю стоимость доставки заказа для каждого типа курьера
                rc = 5;
                averageOrderCost = new double[maxValue + 1];

                for (int i = 0; i < typeOrders.Length; i++)
                {
                    if (typeOrders[i] > 0)
                    {
                        averageOrderCost[i] = totalTypeCost[i] / typeOrders[i];
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
                    if (orderInfo != null && 
                        !orderInfo.Completed &&
                        orderInfo.ShippingOrder.Date_collected <= timeStamp &&
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
                return new CourierDeliveryInfo[0];
            }
        }

        /// <summary>
        /// Получить среднюю стоимость доставки одного заказа
        /// для заданного курьера по его типу
        /// </summary>
        /// <param name="courier">Курьер</param>
        /// <returns>Средняя дставки одного заказа или 0</returns>
        public double GetOrderAverageCost(Courier courier)
        {
            int index = GetCouriersDeliveryInfoIndex(courier);
            if (index < 0)
                return 0;
            return allTypeCouriers[index].AverageOrderCost;
        }

        /// <summary>
        /// Дата сборки первого заказа
        /// </summary>
        /// <returns></returns>
        public DateTime GetFirstOrderAssembledDate()
        {
            if (shopOrders == null || shopOrders.Length <= 0)
            {
                return DateTime.MinValue.Date;
            }
            else
            {
                return shopOrders[0].Date_collected.Date;
            }
        }
    }
}
