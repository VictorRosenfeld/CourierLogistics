
namespace LogisticsService.SalesmanTravelingProblem
{
    using LogisticsService.Couriers;
    using LogisticsService.Geo;
    using LogisticsService.Log;
    using LogisticsService.Orders;
    using LogisticsService.SalesmanTravelingProblem.PermutationsSupport;
    using LogisticsService.ServiceParameters;
    using LogisticsService.Shops;
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Решение задачи доставки заказов магазина
    /// с помощью имеющихся курьеров и такси
    /// </summary>
    public class SalesmanSolutionEy
    {
        /// <summary>
        /// Менеджер расстояний и времени движения между точками
        /// </summary>
        private GeoCache geoCache;

        /// <summary>
        /// Хранилище перестановок
        /// </summary>
        private Permutations permutations;

        /// <summary>
        /// Пределы числа закзов для путей разной длины от 1 до 8
        /// </summary>
        private int[] orderLimitsForPathLength;

        /// <summary>
        /// Параметрический конструктор класса SalesmanSolution
        /// </summary>
        /// <param name="geoCache">Менеджер расстояний и времени движения между точками</param>
        public SalesmanSolutionEy(GeoCache geoCache, SalesmanProblemLevel[] orderLimits)
        {
            this.geoCache = geoCache;
            orderLimitsForPathLength = CreateOrderLimits(orderLimits);
            permutations = new Permutations();
            for (int i = 1; i <= 8; i++)
                permutations.GetPermutations(i);
        }

        #region debug only

        //public void CheckPath(Courier courier)
        //{
        //    Shop shop = new Shop(3641);
        //    shop.Latitude = 53.196871;
        //    shop.Longitude = 45.005578;
        //    shop.WorkStart = new TimeSpan(0);
        //    shop.WorkEnd = new TimeSpan(23, 59, 59);

        //    Order order1 = new Order(9137011);
        //    order1.ShopId = 3641;
        //    DateTime dt;
        //    DateTime.TryParse("2020-11-15T11:36:56.920+03:00", out dt);
        //    order1.ReceiptedDate = dt;
        //    order1.Completed = false;
        //    DateTime.TryParse("2020-11-15T11:36:56.890+03:00", out dt);
        //    order1.DeliveryTimeFrom = dt;
        //    DateTime.TryParse("2020-11-15T13:21:56.890+03:00", out dt);
        //    order1.DeliveryTimeTo = dt;
        //    order1.EnabledTypes = EnabledCourierType.YandexTaxi;
        //    order1.Latitude = 53.226582;
        //    order1.Longitude = 44.874439;
        //    order1.Weight = 3;
        //    order1.Status = OrderStatus.Receipted;

        //    Order order2 = new Order(9137027);
        //    order2.ShopId = 3641;
        //    DateTime.TryParse("2020-11-15T11:37:05.843+03:00", out dt);
        //    order2.ReceiptedDate = dt;
        //    order2.Completed = false;
        //    DateTime.TryParse("2020-11-15T11:37:05.830+03:00", out dt);
        //    order2.DeliveryTimeFrom = dt;
        //    DateTime.TryParse("2020-11-15T13:22:05.830+03:00", out dt);
        //    order2.DeliveryTimeTo = dt;
        //    order2.EnabledTypes = EnabledCourierType.YandexTaxi;
        //    order2.Latitude = 53.223831;
        //    order2.Longitude = 44.922005;
        //    order2.Weight = 3;
        //    order2.Status = OrderStatus.Receipted;

        //    Order[] orders = new Order[] { order1, order2 };
        //    DateTime calcTime;
        //    DateTime.TryParse("2020-11-15T11:27:39.830+03:00", out calcTime);

        //    double[] latitude = new double[] { order1.Latitude, order2.Latitude, shop.Latitude };
        //    double[] longitude = new double[] { order1.Longitude, order2.Longitude, shop.Longitude };

        //    int rcv = geoCache.PutLocationInfo(latitude, longitude, CourierVehicleType.YandexTaxi);

        //    CourierDeliveryInfo[] deliveries;
        //    int rc = CreateShopDeliveriesEx(shop, orders, courier, false, calcTime, out deliveries);

        //}

        #endregion debug only

        ///// <summary>
        ///// Классификация заказов на четыре группы:
        ///// 1) Заказы, которые могут быть доставлены вовремя
        ///// 2) Заказы, которые могут быть доставлены с опозданием
        ///// 3) Заказы, которые не могут быть доставлены в данный момент (например, нет курьера)
        ///// 4) Заказы, которые вообще не могут быть доставлены (например, превышение веса или расстояния)
        ///// </summary>
        ///// <param name="shop">Магазин</param>
        ///// <param name="shopOrders">Отггружаемые заказы</param>
        ///// <param name="shopCouriers">Все курьеры и такси приписанные к магазину</param>
        ///// <param name="calcTime">Время проведения расчетов</param>
        ///// <param name="onTimeOrders">Заказы,которые могут быть доставлены вовремя</param>
        ///// <param name="behindTimeOrders">Заказы,которые могут быть доставлены с опозданием</param>
        ///// <param name="noCouriersOrders">Заказы, для которых нет подходящего ресурса</param>
        ///// <param name="alwaysUndeliveredOrders">Заказы, которые вообще не могут быть доставлены</param>
        ///// <returns></returns>
        //public int ClassifyOrders(Shop shop, Order[] shopOrders, Courier[] shopCouriers, DateTime calcTime,
        //    out Order[] onTimeOrders,
        //    out Order[] behindTimeOrders,
        //    out Order[] noCouriersOrders,
        //    out Order[] alwaysUndeliveredOrders)
        //{
        //    // 1. Инициализация
        //    int rc = 1;
        //    int rc1 = 1;
        //    onTimeOrders = null;
        //    behindTimeOrders = null;
        //    noCouriersOrders = null;
        //    alwaysUndeliveredOrders = null;

        //    try
        //    {
        //        // 2. Проверяем исходные данные
        //        rc = 2;
        //        if (shop == null)
        //            return rc;
        //        if (shopOrders == null || shopOrders.Length <= 0)
        //            return rc;
        //        if (shopCouriers == null || shopCouriers.Length <= 0)
        //            return rc;
        //        if (geoCache == null)
        //            return rc;

        //        int orderCount = shopOrders.Length;

        //        // 3. Обеспечиваем наличие необходимых данных о расстояниях и времени движения из магазина в точки доставки
        //        rc = 3;
        //        double[] srcLatitude = new double[] { shop.Latitude };
        //        double[] srcLongitude = new double[] { shop.Longitude };
        //        double[] dstLatitude = new double[orderCount];
        //        double[] dstLongitude = new double[orderCount];
        //        int[] allVechicleTypes = GetCourierVehicleTypes(shopCouriers);

        //        for (int i = 0; i < orderCount; i++)
        //        {
        //            Order order = shopOrders[i];
        //            order.RejectionReason = OrderRejectionReason.None;
        //            dstLatitude[i] = order.Latitude;
        //            dstLongitude[i] = order.Longitude;
        //        }

        //        for (int i = 0; i < allVechicleTypes.Length; i++)
        //        {
        //            rc1 = geoCache.PutLocationInfo(srcLatitude, srcLongitude, dstLatitude, dstLongitude, (CourierVehicleType)allVechicleTypes[i]);
        //            rc1 = geoCache.PutLocationInfo(dstLatitude, dstLongitude, srcLatitude, srcLongitude, (CourierVehicleType)allVechicleTypes[i]);
        //        }

        //        // 4. Выбираем по одному курьру каждого типа среди всех курьров магазина 
        //        rc = 4;
        //        Dictionary<CourierVehicleType, Courier> allTypeCouriers = new Dictionary<CourierVehicleType, Courier>(8);
        //        for (int i = 0; i < shopCouriers.Length; i++)
        //        {
        //            Courier courier = shopCouriers[i];

        //            Courier courierX;
        //            if (!allTypeCouriers.TryGetValue(courier.CourierType.VechicleType, out courierX))
        //            {
        //                if (courier.IsTaxi)
        //                {
        //                    allTypeCouriers.Add(courier.CourierType.VechicleType, courier);
        //                }
        //                else
        //                {
        //                    allTypeCouriers.Add(courier.CourierType.VechicleType, courier);
        //                }
        //            }
        //            else
        //            {
        //                if (courierX.Status != CourierStatus.Ready && courier.Status == CourierStatus.Ready)
        //                {
        //                    allTypeCouriers[courier.CourierType.VechicleType] = courier;
        //                }
        //            }
        //        }

        //        if (allTypeCouriers.Count <= 0)
        //            return rc;

        //        // 5. Классификация
        //        rc = 5;
        //        // label = 0  - onTimeOrders
        //        // label = 1  - behindTimeOrders
        //        // label = 2  - noCouriersOrders
        //        // label = 3  - alwaysUndeliveredOrders
        //        int[] orderLabel = new int[orderCount];

        //        //DateTime calcTime = DateTime.Now;
        //        double[] orderLatitude = new double[2];
        //        double[] orderLongitude = new double[2];
        //        orderLatitude[0] = shop.Latitude;
        //        orderLongitude[0] = shop.Longitude;
        //        Point[,] dataTable;

        //        double deliveryTime;
        //        double executionTime;
        //        double cost;

        //        for (int i = 0; i < orderCount; i++)
        //        {
        //            // 5.1 Извлекаем заказ
        //            rc = 51;
        //            orderLabel[i] = -1;
        //            Order order = shopOrders[i];

        //            // 5.2. Подставляем широту и долготу точки вручения
        //            rc = 52;
        //            orderLatitude[1] = order.Latitude;
        //            orderLongitude[1] = order.Longitude;

        //            // 5.3 Предустановки
        //            rc = 53;
        //            int yandexLabel = 3;
        //            int gettLabel = 3;
        //            int onFootLabel = 3;
        //            int bicycleLabel = 3;
        //            int carLabel = 3;

        //            OrderRejectionReason yandexRejectReason = OrderRejectionReason.None;
        //            OrderRejectionReason gettRejectReason = OrderRejectionReason.None;
        //            OrderRejectionReason onFootRejectReason = OrderRejectionReason.None;
        //            OrderRejectionReason bicycleRejectReason = OrderRejectionReason.None;
        //            OrderRejectionReason carRejectReason = OrderRejectionReason.None;

        //            // 5.4 Обработка доставки Yandex-такси
        //            rc = 54;
        //            if ((order.EnabledTypes & EnabledCourierType.YandexTaxi) != 0)
        //            {
        //                Courier yandexTaxi;
        //                if (allTypeCouriers.TryGetValue(CourierVehicleType.YandexTaxi, out yandexTaxi))
        //                {

        //                    rc1 = geoCache.GetPointsDataTable(orderLatitude, orderLongitude, CourierVehicleType.YandexTaxi, out dataTable);
        //                    if (rc1 == 0)
        //                    {
        //                        rc1 = yandexTaxi.CourierType.GetTimeAndCost(dataTable[0, 1], order.Weight, out deliveryTime, out executionTime, out cost);
        //                        if (rc1 != 0)
        //                        {
        //                            yandexLabel = 3;

        //                            switch (rc1)
        //                            {
        //                                case 21:
        //                                    yandexRejectReason = OrderRejectionReason.Overdistance;
        //                                    break;
        //                                case 22:
        //                                    yandexRejectReason = OrderRejectionReason.Overweight;
        //                                    break;
        //                            }
        //                        }
        //                        else
        //                        {
        //                            if (calcTime.AddMinutes(deliveryTime) <= order.DeliveryTimeTo)
        //                            {
        //                                yandexLabel = 0;
        //                            }
        //                            else
        //                            {
        //                                yandexRejectReason = OrderRejectionReason.ToTimeIsSmall;
        //                                yandexLabel = 1;
        //                            }
        //                        }
        //                    }
        //                }

        //                if (yandexLabel == 0)
        //                {
        //                    orderLabel[i] = 0;
        //                    continue;
        //                }
        //            }

        //            // 5.5 Обработка доставки Gett-такси
        //            rc = 55;
        //            if ((order.EnabledTypes & EnabledCourierType.GettTaxi) != 0)
        //            {
        //                Courier gettTaxi;
        //                if (allTypeCouriers.TryGetValue(CourierVehicleType.GettTaxi, out gettTaxi))
        //                {
        //                    rc1 = geoCache.GetPointsDataTable(orderLatitude, orderLongitude, CourierVehicleType.GettTaxi, out dataTable);
        //                    if (rc1 == 0)
        //                    {
        //                        rc1 = gettTaxi.CourierType.GetTimeAndCost(dataTable[0, 1], order.Weight, out deliveryTime, out executionTime, out cost);
        //                        if (rc1 != 0)
        //                        {
        //                            gettLabel = 3;

        //                            switch (rc1)
        //                            {
        //                                case 21:
        //                                    gettRejectReason = OrderRejectionReason.Overdistance;
        //                                    break;
        //                                case 22:
        //                                    gettRejectReason = OrderRejectionReason.Overweight;
        //                                    break;
        //                            }
        //                        }
        //                        else
        //                        {
        //                            if (calcTime.AddMinutes(deliveryTime) <= order.DeliveryTimeTo)
        //                            {
        //                                gettLabel = 0;
        //                            }
        //                            else
        //                            {
        //                                gettRejectReason = OrderRejectionReason.ToTimeIsSmall;
        //                                gettLabel = 1;
        //                            }
        //                        }
        //                    }
        //                }

        //                if (gettLabel == 0)
        //                {
        //                    orderLabel[i] = 0;
        //                    continue;
        //                }
        //            }

        //            // 5.6 Обработка доставки пешим курьером
        //            rc = 56;
        //            if ((order.EnabledTypes & EnabledCourierType.OnFoot) != 0)
        //            {
        //                Courier onFoot;
        //                if (allTypeCouriers.TryGetValue(CourierVehicleType.OnFoot, out onFoot))
        //                {
        //                    if (onFoot.Status != CourierStatus.Ready)
        //                    {
        //                        onFootLabel = 2;
        //                        onFootRejectReason = OrderRejectionReason.CourierNa;
        //                    }
        //                    else
        //                    {
        //                        rc1 = geoCache.GetPointsDataTable(orderLongitude, orderLongitude, CourierVehicleType.OnFoot, out dataTable);
        //                        if (rc1 == 0)
        //                        {
        //                            rc1 = onFoot.CourierType.GetTimeAndCost(dataTable[0, 1], order.Weight, out deliveryTime, out executionTime, out cost);
        //                            if (rc1 != 0)
        //                            {
        //                                onFootLabel = 3;

        //                                switch (rc1)
        //                                {
        //                                    case 21:
        //                                        onFootRejectReason = OrderRejectionReason.Overdistance;
        //                                        break;
        //                                    case 22:
        //                                        onFootRejectReason = OrderRejectionReason.Overweight;
        //                                        break;
        //                                }
        //                            }
        //                            else
        //                            {
        //                                if (calcTime.AddMinutes(deliveryTime) <= order.DeliveryTimeTo)
        //                                {
        //                                    onFootLabel = 0;
        //                                }
        //                                else
        //                                {
        //                                    onFootRejectReason = OrderRejectionReason.ToTimeIsSmall;
        //                                    onFootLabel = 1;
        //                                }
        //                            }
        //                        }
        //                    }
        //                }

        //                if (onFootLabel == 0)
        //                {
        //                    orderLabel[i] = 0;
        //                    continue;
        //                }
        //            }

        //            // 5.7 Обработка доставки курьером на велосипеде
        //            rc = 57;
        //            if ((order.EnabledTypes & EnabledCourierType.Bicycle) != 0)
        //            {
        //                Courier bicycle;
        //                if (allTypeCouriers.TryGetValue(CourierVehicleType.Bicycle, out bicycle))
        //                {
        //                    if (bicycle.Status != CourierStatus.Ready)
        //                    {
        //                        bicycleLabel = 2;
        //                        bicycleRejectReason = OrderRejectionReason.CourierNa;
        //                    }
        //                    else
        //                    {
        //                        rc1 = geoCache.GetPointsDataTable(orderLongitude, orderLongitude, CourierVehicleType.Bicycle, out dataTable);
        //                        if (rc1 == 0)
        //                        {
        //                            rc1 = bicycle.CourierType.GetTimeAndCost(dataTable[0, 1], order.Weight, out deliveryTime, out executionTime, out cost);
        //                            if (rc1 != 0)
        //                            {
        //                                bicycleLabel = 3;

        //                                switch (rc1)
        //                                {
        //                                    case 21:
        //                                        bicycleRejectReason = OrderRejectionReason.Overdistance;
        //                                        break;
        //                                    case 22:
        //                                        bicycleRejectReason = OrderRejectionReason.Overweight;
        //                                        break;
        //                                }
        //                            }
        //                            else
        //                            {
        //                                if (calcTime.AddMinutes(deliveryTime) <= order.DeliveryTimeTo)
        //                                {
        //                                    bicycleLabel = 0;
        //                                }
        //                                else
        //                                {
        //                                    bicycleRejectReason = OrderRejectionReason.ToTimeIsSmall;
        //                                    bicycleLabel = 1;
        //                                }
        //                            }
        //                        }
        //                    }
        //                }

        //                if (bicycleLabel == 0)
        //                {
        //                    orderLabel[i] = 0;
        //                    continue;
        //                }
        //            }

        //            // 5.8 Обработка доставки курьером на авто
        //            rc = 78;
        //            if ((order.EnabledTypes & EnabledCourierType.Car) != 0)
        //            {
        //                Courier car;
        //                if (allTypeCouriers.TryGetValue(CourierVehicleType.Car, out car))
        //                {
        //                    if (car.Status != CourierStatus.Ready)
        //                    {
        //                        carLabel = 2;
        //                        carRejectReason = OrderRejectionReason.CourierNa;
        //                    }
        //                    else
        //                    {
        //                        rc1 = geoCache.GetPointsDataTable(orderLongitude, orderLongitude, CourierVehicleType.Car, out dataTable);
        //                        if (rc1 == 0)
        //                        {
        //                            rc1 = car.CourierType.GetTimeAndCost(dataTable[0, 1], order.Weight, out deliveryTime, out executionTime, out cost);
        //                            if (rc1 != 0)
        //                            {
        //                                carLabel = 3;

        //                                switch (rc1)
        //                                {
        //                                    case 21:
        //                                        carRejectReason = OrderRejectionReason.Overdistance;
        //                                        break;
        //                                    case 22:
        //                                        carRejectReason = OrderRejectionReason.Overweight;
        //                                        break;
        //                                }
        //                            }
        //                            else
        //                            {
        //                                if (calcTime.AddMinutes(deliveryTime) <= order.DeliveryTimeTo)
        //                                {
        //                                    carLabel = 0;
        //                                }
        //                                else
        //                                {
        //                                    carRejectReason = OrderRejectionReason.ToTimeIsSmall;
        //                                    carLabel = 1;
        //                                }
        //                            }
        //                        }
        //                    }
        //                }

        //                if (carLabel == 0)
        //                {
        //                    orderLabel[i] = 0;
        //                    continue;
        //                }
        //            }

        //            // 5.9 Классификация заказа
        //            rc = 59;
        //            for (int label = 1; label <= 3; label++)
        //            {
        //                if (yandexLabel == label)
        //                {
        //                    orderLabel[i] = label;
        //                    order.RejectionReason = yandexRejectReason;
        //                }
        //                else if (gettLabel == label)
        //                {
        //                    orderLabel[i] = label;
        //                    order.RejectionReason = gettRejectReason;
        //                }
        //                else if (carLabel == label)
        //                {
        //                    orderLabel[i] = label;
        //                    order.RejectionReason = carRejectReason;
        //                }
        //                else if (bicycleLabel == label)
        //                {
        //                    orderLabel[i] = label;
        //                    order.RejectionReason = bicycleRejectReason;
        //                }
        //                else if (onFootLabel == label)
        //                {
        //                    orderLabel[i] = label;
        //                    order.RejectionReason = onFootRejectReason;
        //                }

        //                if (orderLabel[i] != -1)
        //                    break;
        //            }

        //            if (orderLabel[i] == -1)
        //            {
        //                orderLabel[i] = 3;
        //                order.RejectionReason = OrderRejectionReason.CourierNa;
        //            }
        //        }

        //        // 6. Раскладываем заказы по группам
        //        rc = 6;
        //        List<Order>[] orderCluster = new List<Order>[4];
        //        for (int i = 0; i < orderCluster.Length; i++)
        //        {
        //            orderCluster[i] = new List<Order>(32);
        //        }

        //        for (int i = 0; i < orderCount; i++)
        //        {
        //            orderCluster[orderLabel[i]].Add(shopOrders[i]);
        //        }

        //        // 7. Формируем результирующие группы
        //        rc = 7;
        //        onTimeOrders = orderCluster[0].ToArray();
        //        behindTimeOrders = orderCluster[1].ToArray();
        //        noCouriersOrders = orderCluster[2].ToArray();
        //        alwaysUndeliveredOrders = orderCluster[3].ToArray();

        //        // 8. Выход - Ok
        //        rc = 0;
        //        return rc;
        //    }
        //    catch
        //    {
        //        return rc;
        //    }
        //}

        /// <summary>
        /// Классификация заказов на четыре группы:
        /// 1) Заказы, которые могут быть доставлены вовремя
        /// 2) Заказы, которые могут быть доставлены с опозданием
        /// 3) Заказы, которые не могут быть доставлены в данный момент (например, нет курьера)
        /// 4) Заказы, которые вообще не могут быть доставлены (например, превышение веса или расстояния)
        /// </summary>
        /// <param name="shop">Магазин</param>
        /// <param name="shopOrders">Отггружаемые заказы</param>
        /// <param name="shopCouriers">Все курьеры и такси приписанные к магазину</param>
        /// <param name="calcTime">Время проведения расчетов</param>
        /// <param name="onTimeOrders">Заказы,которые могут быть доставлены вовремя</param>
        /// <param name="behindTimeOrders">Заказы,которые могут быть доставлены с опозданием</param>
        /// <param name="noCourierOrders">Заказы, для которых нет подходящего ресурса</param>
        /// <param name="alwaysUndeliveredOrders">Заказы, которые вообще не могут быть доставлены</param>
        /// <returns></returns>
        public int ClassifyOrders(Shop shop, Order[] shopOrders, Courier[] shopCouriers, DateTime calcTime,
            out Order[] onTimeOrders,
            out Order[] behindTimeOrders,
            out Order[] noCourierOrders,
            out Order[] alwaysUndeliveredOrders)
        {
            // 1. Инициализация
            int rc = 1;
            int rc1 = 1;
            onTimeOrders = null;
            behindTimeOrders = null;
            noCourierOrders = null;
            alwaysUndeliveredOrders = null;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (shop == null)
                    return rc;
                if (shopOrders == null || shopOrders.Length <= 0)
                    return rc;
                if (shopCouriers == null || shopCouriers.Length <= 0)
                    return rc;
                if (geoCache == null)
                    return rc;

                int orderCount = shopOrders.Length;

                // 3. Обеспечиваем наличие необходимых данных о расстояниях и времени движения из магазина в точки доставки
                rc = 3;
                double[] srcLatitude = new double[] { shop.Latitude };
                double[] srcLongitude = new double[] { shop.Longitude };
                double[] dstLatitude = new double[orderCount];
                double[] dstLongitude = new double[orderCount];
                int[] allVechicleTypes = GetCourierVehicleTypes(shopCouriers);

                for (int i = 0; i < orderCount; i++)
                {
                    Order order = shopOrders[i];
                    order.RejectionReason = OrderRejectionReason.None;
                    dstLatitude[i] = order.Latitude;
                    dstLongitude[i] = order.Longitude;
                }

                for (int i = 0; i < allVechicleTypes.Length; i++)
                {
                    rc1 = geoCache.PutLocationInfo(srcLatitude, srcLongitude, dstLatitude, dstLongitude, (CourierVehicleType)allVechicleTypes[i]);
                    rc1 = geoCache.PutLocationInfo(dstLatitude, dstLongitude, srcLatitude, srcLongitude, (CourierVehicleType)allVechicleTypes[i]);
                }

                // 4. Выбираем по одному курьру каждого типа среди всех курьров магазина 
                rc = 4;
                Dictionary<CourierVehicleType, Courier> allTypeCouriers = new Dictionary<CourierVehicleType, Courier>(8);

                for (int i = 0; i < shopCouriers.Length; i++)
                {
                    Courier courier = shopCouriers[i];

                    Courier courierX;
                    if (!allTypeCouriers.TryGetValue(courier.CourierType.VechicleType, out courierX))
                    {
                        allTypeCouriers.Add(courier.CourierType.VechicleType, courier);
                    }
                    else if (courierX.Status != CourierStatus.Ready && courier.Status == CourierStatus.Ready)
                    {
                        allTypeCouriers[courier.CourierType.VechicleType] = courier;
                    }
                }

                if (allTypeCouriers.Count <= 0)
                    return rc;

                // 5. Классификация
                rc = 5;
                // label = 0  - onTimeOrders
                // label = 1  - behindTimeOrders
                // label = 2  - noCouriersOrders
                // label = 3  - alwaysUndeliveredOrders
                int[] orderLabel = new int[orderCount];

                //DateTime calcTime = DateTime.Now;
                double[] orderLatitude = new double[2];
                double[] orderLongitude = new double[2];
                orderLatitude[0] = shop.Latitude;
                orderLongitude[0] = shop.Longitude;
                Point[,] dataTable;

                double deliveryTime;
                double executionTime;
                double cost;

                for (int i = 0; i < orderCount; i++)
                {
                    // 5.1 Извлекаем заказ
                    rc = 51;
                    orderLabel[i] = 3;
                    Order order = shopOrders[i];

                    // 5.2. Подставляем широту и долготу точки вручения
                    rc = 52;
                    orderLatitude[1] = order.Latitude;
                    orderLongitude[1] = order.Longitude;

                    // 5.3 Предустановки
                    rc = 53;
                    OrderRejectionReason rejectionReason = OrderRejectionReason.CourierNa;
                    int label = 3;

                    // 5.4 Извлекаем возможные способы доставки заказа
                    rc = 54;
                    CourierVehicleType[] orderVehicleTypes = order.EnabledTypesEx;
                    if (orderVehicleTypes == null || orderVehicleTypes.Length <= 0)
                    {
                        label = 3;
                        rejectionReason = OrderRejectionReason.CourierNa;
                    }
                    else
                    {
                        // 5.5 Обрабытываем каждый способ доставки заказа
                        rc = 55;
                        foreach (CourierVehicleType vehicleType in orderVehicleTypes)
                        {
                            Courier courier;
                            if (allTypeCouriers.TryGetValue(vehicleType, out courier))
                            {
                                if (courier.IsTaxi)
                                {
                                    // 5.5.1 Доставка такси
                                    rc = 551;
                                    rc1 = geoCache.GetPointsDataTable(orderLatitude, orderLongitude, vehicleType, out dataTable);
                                    if (rc1 == 0)
                                    {
                                        rc1 = courier.CourierType.GetTimeAndCost(dataTable[0, 1], order.Weight, out deliveryTime, out executionTime, out cost);
                                        if (rc1 != 0)
                                        {
                                            if (label >= 3)
                                            {
                                                switch (rc1)
                                                {
                                                    case 21:
                                                        rejectionReason = OrderRejectionReason.Overdistance;
                                                        break;
                                                    case 22:
                                                        rejectionReason = OrderRejectionReason.Overweight;
                                                        break;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            if (calcTime.AddMinutes(deliveryTime) <= order.DeliveryTimeTo)
                                            {
                                                label = 0;
                                                rejectionReason = OrderRejectionReason.None;
                                                goto SetOrderLabel;
                                            }
                                            else
                                            {
                                                if (label > 1)
                                                {
                                                    rejectionReason = OrderRejectionReason.ToTimeIsSmall;
                                                    label = 1;
                                                }
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    // 5.5.2 Доставка курьером
                                    rc = 552;
                                    if (courier.Status != CourierStatus.Ready)
                                    {
                                        if (label > 2)
                                        {
                                            label = 2;
                                            rejectionReason = OrderRejectionReason.CourierNa;
                                        }
                                    }
                                    else
                                    {
                                        rc1 = geoCache.GetPointsDataTable(orderLatitude, orderLongitude, vehicleType, out dataTable);
                                        if (rc1 == 0)
                                        {
                                            rc1 = courier.CourierType.GetTimeAndCost(dataTable[0, 1], order.Weight, out deliveryTime, out executionTime, out cost);
                                            if (rc1 != 0)
                                            {
                                                if (label >= 3)
                                                {
                                                    switch (rc1)
                                                    {
                                                        case 21:
                                                            rejectionReason = OrderRejectionReason.Overdistance;
                                                            break;
                                                        case 22:
                                                            rejectionReason = OrderRejectionReason.Overweight;
                                                            break;
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                if (calcTime.AddMinutes(deliveryTime) <= order.DeliveryTimeTo)
                                                {
                                                    label = 0;
                                                    rejectionReason = OrderRejectionReason.None;
                                                    goto SetOrderLabel;
                                                }
                                                else
                                                {
                                                    if (label > 1)
                                                    {
                                                        rejectionReason = OrderRejectionReason.ToTimeIsSmall;
                                                        label = 1;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            else if (label >= 3)
                            {
                                orderLabel[i] = 3;
                                rejectionReason = OrderRejectionReason.CourierNa;
                            }
                        }
                    }
SetOrderLabel:
                    orderLabel[i] = label;
                    order.RejectionReason = rejectionReason;
                }

                // 6. Раскладываем заказы по группам
                rc = 6;
                List<Order>[] orderCluster = new List<Order>[4];
                for (int i = 0; i < orderCluster.Length; i++)
                {
                    orderCluster[i] = new List<Order>(32);
                }

                for (int i = 0; i < orderCount; i++)
                {
                    orderCluster[orderLabel[i]].Add(shopOrders[i]);
                }

                // 7. Формируем результирующие группы
                rc = 7;
                onTimeOrders = orderCluster[0].ToArray();
                behindTimeOrders = orderCluster[1].ToArray();
                noCourierOrders = orderCluster[2].ToArray();
                alwaysUndeliveredOrders = orderCluster[3].ToArray();

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
        /// Создание отгрузок для заказов магазина
        /// </summary>
        /// <param name="shop">Магазин</param>
        /// <param name="allOrdersOfShop">Заказы магазина, для которых создаются отгрузки</param>
        /// <param name="allShopCouriers">Все курьеры и такси приписанные к магазину</param>
        /// <param name="assembledOrders">Отгрузки покрывающие все собранные заказы</param>
        /// <param name="receiptedOrders">Отгрузки, в которые могут попасть поступившие, но не собранные заказы</param>
        /// <param name="tabuOrders">Заказы, которые вообще не могут быть доставлены</param>
        /// <returns>0 - отгрузки созданы; иначе - отгрузки не созданы</returns>
        public int CreateShopDeliveries(
            Shop shop, Order[] allOrdersOfShop, Courier[] allShopCouriers, 
            out CourierDeliveryInfo[] assembledOrders, out CourierDeliveryInfo[] receiptedOrders, out Order[] undeliveredOrders, out Order[] tabuOrders)
        {
            // 1. Инициализация
            int rc = 1;
            int rc1 = 1;
            assembledOrders = null;
            receiptedOrders = null;
            undeliveredOrders = null;
            tabuOrders = null;

            try
            {
                // 2. Проверяем исходные 
                rc = 2;
                if (geoCache == null)
                    return rc;
                if (shop == null)
                    return rc;
                if (allShopCouriers == null || allShopCouriers.Length <= 0)
                    return rc;
                if (allOrdersOfShop == null || allOrdersOfShop.Length <= 0)
                    return rc;

                // 3. Классифицируем заказы
                rc = 3;
                DateTime calcTime = DateTime.Now;
                Order[] onTimeOrders;
                Order[] behindTimeOrders;
                Order[] noCouriersOrders;
                Order[] alwaysUndeliveredOrders;

                rc1 = ClassifyOrders(shop, allOrdersOfShop, allShopCouriers, calcTime, out onTimeOrders, out behindTimeOrders, out noCouriersOrders, out alwaysUndeliveredOrders);
                if (rc1 != 0)
                    return rc = 1000 * rc + rc1;

                tabuOrders = alwaysUndeliveredOrders;

                // 4. Обрабатываем различные случаи
                rc = 4;
                Courier[] shopCouriers = allShopCouriers.Where(courier => courier.IsTaxi || courier.Status == CourierStatus.Ready).ToArray();

                bool isOnTime = (onTimeOrders != null && onTimeOrders.Length > 0);
                bool isBehindTime = (behindTimeOrders != null && behindTimeOrders.Length > 0);

                if (isOnTime && !isBehindTime)
                {
                    rc1 = CreateShopDeliveries_OnTime(shop, onTimeOrders, shopCouriers, calcTime, out assembledOrders, out receiptedOrders, out undeliveredOrders);
                }
                else if (!isOnTime && isBehindTime)
                {
                    rc1 = CreateShopDeliveries_BehindTime(shop, behindTimeOrders, shopCouriers, calcTime, out assembledOrders, out receiptedOrders, out undeliveredOrders);

                }
                else if (isOnTime && isBehindTime)
                {
                    rc1 = CreateShopDeliveries_OnTimeAndBehindTime(shop, behindTimeOrders, onTimeOrders, shopCouriers, calcTime, out assembledOrders, out receiptedOrders, out undeliveredOrders);

                }
                else // (!isOnTime && !isBehindTime)
                {
                    //return rc = 0;
                }

                if (rc1 != 0)
                    return rc = 100000 * rc + rc1;

                // 14. Выход - Ok
                rc = 0;
                return rc;
            }
            catch (Exception ex)
            {
                Logger.WriteToLog(string.Format(MessagePatterns.METHOD_CALL, "CreateShopDeliveries", $"CreateShopDeliveries(Shop {shop.Id}, Orders {Helper.ArrayToString(allOrdersOfShop.Select(order => order.Id).ToArray())}, couriers { Helper.ArrayToString(allShopCouriers.Select(courier => courier.Id).ToArray())})"));
                Logger.WriteToLog(string.Format(MessagePatterns.METHOD_RC, "CreateShopDeliveries", rc));
                Logger.WriteToLog(string.Format(MessagePatterns.METHOD_FAIL, "CreateShopDeliveries", ex.ToString()));

                return rc;
            }
        }

        /// <summary>
        /// Создание отгрузок для заказов магазина
        /// </summary>
        /// <param name="shop">Магазин</param>
        /// <param name="onTimeOrders">Заказы магазина, для которых создаются отгрузки</param>
        /// <param name="shopCouriers">Доступные для доставки заказов курьеры и такси</param>
        /// <param name="calcTime">Время проведения расчетов</param>
        /// <param name="assembledOrders">Отгрузки покрывающие все собранные заказы</param>
        /// <param name="receiptedOrders">Отгрузки, в которые могут попасть поступившие, но не собранные заказы</param>
        /// <param name="undeliveredOrders">Заказы, которые не могут быть доставлены в срок</param>
        /// <returns>0 - отгрузки созданы; иначе - отгрузки не созданы</returns>
        private int CreateShopDeliveries_OnTime(Shop shop, Order[] onTimeOrders, Courier[] shopCouriers, DateTime calcTime, out CourierDeliveryInfo[] assembledOrders, out CourierDeliveryInfo[] receiptedOrders, out Order[] undeliveredOrders)
        {
            // 1. Инициализация
            int rc = 1;
            int rc1 = 1;
            assembledOrders = null;
            receiptedOrders = null;
            undeliveredOrders = null;

            try
            {
                // 2. Проверяем исходные 
                rc = 2;
                if (geoCache == null)
                    return rc;
                if (shop == null)
                    return rc;
                if (shopCouriers == null || shopCouriers.Length <= 0)
                    return rc;
                if (onTimeOrders == null || onTimeOrders.Length <= 0)
                    return rc;

                // 3. Выбираем по одному курьру каждого типа среди заданных
                rc = 3;
                Dictionary<CourierVehicleType, Courier> allTypeCouriers = new Dictionary<CourierVehicleType, Courier>(8);
                for (int i = 0; i < shopCouriers.Length; i++)
                {
                    Courier courier = shopCouriers[i];
                    if (courier.Status != CourierStatus.Ready)
                        continue;

                    if (!allTypeCouriers.ContainsKey(courier.CourierType.VechicleType))
                    {
                        if (courier.IsTaxi)
                        {
                            allTypeCouriers.Add(courier.CourierType.VechicleType, courier);
                        }
                        else
                        {
                            Courier courierClone = courier.Clone();
                            courierClone.WorkStart = TimeSpan.Zero;
                            courierClone.WorkEnd = new TimeSpan(23, 59, 59);
                            courierClone.LunchTimeStart = TimeSpan.Zero;
                            courierClone.LunchTimeEnd = TimeSpan.Zero;
                            allTypeCouriers.Add(courierClone.CourierType.VechicleType, courierClone);
                        }
                    }
                }

                if (allTypeCouriers.Count <= 0)
                    return rc;

                // 4. Обеспечиваем наличие всех необходимых расстояний и времени движения между парами точек в двух направлениях
                rc = 4;
                Courier[] allCouriers = new Courier[allTypeCouriers.Count];
                allTypeCouriers.Values.CopyTo(allCouriers, 0);
                int size = onTimeOrders.Length + 1;
                double[] latitude = new double[size];
                double[] longitude = new double[size];

                latitude[size - 1] = shop.Latitude;
                longitude[size - 1] = shop.Longitude;
                shop.LocationIndex = size - 1;

                for (int i = 0; i < onTimeOrders.Length; i++)
                {
                    Order order = onTimeOrders[i];
                    order.LocationIndex = i;
                    latitude[i] = order.Latitude;
                    longitude[i] = order.Longitude;
                }

                for (int i = 0; i < allTypeCouriers.Count; i++)
                {
                    rc1 = geoCache.PutLocationInfo(latitude, longitude, allCouriers[i].CourierType.VechicleType);
                    if (rc1 != 0)
                        return rc = 100 * rc + rc1;
                }

                // 5. Запускаем построение всех возможных путей всеми возможными способами
                //    Каждый способ доставки обрабатывается в отдельном потоке
                rc = 5;
                //DateTime calcTime = new DateTime(2020, 11, 4, 18, 50, 0);
                Task<int>[] tasks = new Task<int>[allTypeCouriers.Count];
                CourierDeliveryInfo[][] taskDeliveries = new CourierDeliveryInfo[allTypeCouriers.Count][];

                switch (allCouriers.Length)
                {
                    case 1:
                        tasks[0] = Task.Run(() => CreateShopDeliveriesEx(shop, onTimeOrders, allCouriers[0], !allCouriers[0].IsTaxi, calcTime, out taskDeliveries[0]));
                        break;
                    case 2:
                        tasks[0] = Task.Run(() => CreateShopDeliveriesEx(shop, onTimeOrders, allCouriers[0], !allCouriers[0].IsTaxi, calcTime, out taskDeliveries[0]));
                        tasks[1] = Task.Run(() => CreateShopDeliveriesEx(shop, onTimeOrders, allCouriers[1], !allCouriers[1].IsTaxi, calcTime, out taskDeliveries[1]));
                        break;
                    case 3:
                        tasks[0] = Task.Run(() => CreateShopDeliveriesEx(shop, onTimeOrders, allCouriers[0], !allCouriers[0].IsTaxi, calcTime, out taskDeliveries[0]));
                        tasks[1] = Task.Run(() => CreateShopDeliveriesEx(shop, onTimeOrders, allCouriers[1], !allCouriers[1].IsTaxi, calcTime, out taskDeliveries[1]));
                        tasks[2] = Task.Run(() => CreateShopDeliveriesEx(shop, onTimeOrders, allCouriers[2], !allCouriers[2].IsTaxi, calcTime, out taskDeliveries[2]));
                        break;
                    case 4:
                        tasks[0] = Task.Run(() => CreateShopDeliveriesEx(shop, onTimeOrders, allCouriers[0], !allCouriers[0].IsTaxi, calcTime, out taskDeliveries[0]));
                        tasks[1] = Task.Run(() => CreateShopDeliveriesEx(shop, onTimeOrders, allCouriers[1], !allCouriers[1].IsTaxi, calcTime, out taskDeliveries[1]));
                        tasks[2] = Task.Run(() => CreateShopDeliveriesEx(shop, onTimeOrders, allCouriers[2], !allCouriers[2].IsTaxi, calcTime, out taskDeliveries[2]));
                        tasks[3] = Task.Run(() => CreateShopDeliveriesEx(shop, onTimeOrders, allCouriers[3], !allCouriers[3].IsTaxi, calcTime, out taskDeliveries[3]));
                        break;
                    case 5:
                        tasks[0] = Task.Run(() => CreateShopDeliveriesEx(shop, onTimeOrders, allCouriers[0], !allCouriers[0].IsTaxi, calcTime, out taskDeliveries[0]));
                        tasks[1] = Task.Run(() => CreateShopDeliveriesEx(shop, onTimeOrders, allCouriers[1], !allCouriers[1].IsTaxi, calcTime, out taskDeliveries[1]));
                        tasks[2] = Task.Run(() => CreateShopDeliveriesEx(shop, onTimeOrders, allCouriers[2], !allCouriers[2].IsTaxi, calcTime, out taskDeliveries[2]));
                        tasks[3] = Task.Run(() => CreateShopDeliveriesEx(shop, onTimeOrders, allCouriers[3], !allCouriers[3].IsTaxi, calcTime, out taskDeliveries[3]));
                        tasks[4] = Task.Run(() => CreateShopDeliveriesEx(shop, onTimeOrders, allCouriers[4], !allCouriers[4].IsTaxi, calcTime, out taskDeliveries[4]));
                        break;
                    case 6:
                        tasks[0] = Task.Run(() => CreateShopDeliveriesEx(shop, onTimeOrders, allCouriers[0], !allCouriers[0].IsTaxi, calcTime, out taskDeliveries[0]));
                        tasks[1] = Task.Run(() => CreateShopDeliveriesEx(shop, onTimeOrders, allCouriers[1], !allCouriers[1].IsTaxi, calcTime, out taskDeliveries[1]));
                        tasks[2] = Task.Run(() => CreateShopDeliveriesEx(shop, onTimeOrders, allCouriers[2], !allCouriers[2].IsTaxi, calcTime, out taskDeliveries[2]));
                        tasks[3] = Task.Run(() => CreateShopDeliveriesEx(shop, onTimeOrders, allCouriers[3], !allCouriers[3].IsTaxi, calcTime, out taskDeliveries[3]));
                        tasks[4] = Task.Run(() => CreateShopDeliveriesEx(shop, onTimeOrders, allCouriers[4], !allCouriers[4].IsTaxi, calcTime, out taskDeliveries[4]));
                        tasks[5] = Task.Run(() => CreateShopDeliveriesEx(shop, onTimeOrders, allCouriers[5], !allCouriers[5].IsTaxi, calcTime, out taskDeliveries[5]));
                        break;
                    case 7:
                        tasks[0] = Task.Run(() => CreateShopDeliveriesEx(shop, onTimeOrders, allCouriers[0], !allCouriers[0].IsTaxi, calcTime, out taskDeliveries[0]));
                        tasks[1] = Task.Run(() => CreateShopDeliveriesEx(shop, onTimeOrders, allCouriers[1], !allCouriers[1].IsTaxi, calcTime, out taskDeliveries[1]));
                        tasks[2] = Task.Run(() => CreateShopDeliveriesEx(shop, onTimeOrders, allCouriers[2], !allCouriers[2].IsTaxi, calcTime, out taskDeliveries[2]));
                        tasks[3] = Task.Run(() => CreateShopDeliveriesEx(shop, onTimeOrders, allCouriers[3], !allCouriers[3].IsTaxi, calcTime, out taskDeliveries[3]));
                        tasks[4] = Task.Run(() => CreateShopDeliveriesEx(shop, onTimeOrders, allCouriers[4], !allCouriers[4].IsTaxi, calcTime, out taskDeliveries[4]));
                        tasks[5] = Task.Run(() => CreateShopDeliveriesEx(shop, onTimeOrders, allCouriers[5], !allCouriers[5].IsTaxi, calcTime, out taskDeliveries[5]));
                        tasks[6] = Task.Run(() => CreateShopDeliveriesEx(shop, onTimeOrders, allCouriers[6], !allCouriers[6].IsTaxi, calcTime, out taskDeliveries[6]));
                        break;
                    case 8:
                        tasks[0] = Task.Run(() => CreateShopDeliveriesEx(shop, onTimeOrders, allCouriers[0], !allCouriers[0].IsTaxi, calcTime, out taskDeliveries[0]));
                        tasks[1] = Task.Run(() => CreateShopDeliveriesEx(shop, onTimeOrders, allCouriers[1], !allCouriers[1].IsTaxi, calcTime, out taskDeliveries[1]));
                        tasks[2] = Task.Run(() => CreateShopDeliveriesEx(shop, onTimeOrders, allCouriers[2], !allCouriers[2].IsTaxi, calcTime, out taskDeliveries[2]));
                        tasks[3] = Task.Run(() => CreateShopDeliveriesEx(shop, onTimeOrders, allCouriers[3], !allCouriers[3].IsTaxi, calcTime, out taskDeliveries[3]));
                        tasks[4] = Task.Run(() => CreateShopDeliveriesEx(shop, onTimeOrders, allCouriers[4], !allCouriers[4].IsTaxi, calcTime, out taskDeliveries[4]));
                        tasks[5] = Task.Run(() => CreateShopDeliveriesEx(shop, onTimeOrders, allCouriers[5], !allCouriers[5].IsTaxi, calcTime, out taskDeliveries[5]));
                        tasks[6] = Task.Run(() => CreateShopDeliveriesEx(shop, onTimeOrders, allCouriers[6], !allCouriers[6].IsTaxi, calcTime, out taskDeliveries[6]));
                        tasks[7] = Task.Run(() => CreateShopDeliveriesEx(shop, onTimeOrders, allCouriers[7], !allCouriers[7].IsTaxi, calcTime, out taskDeliveries[7]));
                        break;
                }

                // Дожидаёмся завершения обработки
                Task.WaitAll(tasks);

                // 6. Объединяем все построенные отгрузки
                rc = 6;
                int deliveryCount = 0;
                CourierDeliveryInfo[] allDeliveries;

                for (int i = 0; i < tasks.Length; i++)
                {
                    int rcx = tasks[i].Result;

                    if (rcx == 0)
                    {
                        if (taskDeliveries[i] != null)
                            deliveryCount += taskDeliveries[i].Length;
                    }
                }

                if (deliveryCount <= 0)
                {
                    for (int i = 0; i < onTimeOrders.Length; i++)
                    {
                        onTimeOrders[i].RejectionReason = OrderRejectionReason.CourierNa;
                    }

                    undeliveredOrders = onTimeOrders;
                    return rc = 0;
                }

                allDeliveries = new CourierDeliveryInfo[deliveryCount];
                deliveryCount = 0;

                for (int i = 0; i < tasks.Length; i++)
                {
                    if (tasks[i].Result == 0)
                    {
                        if (taskDeliveries[i] != null)
                        {
                            taskDeliveries[i].CopyTo(allDeliveries, deliveryCount);
                            deliveryCount += taskDeliveries[i].Length;
                        }
                    }
                }

                // 7. Сортируем по средней стоимости доставки одного заказа
                rc = 7;
                Array.Sort(allDeliveries, CompareByOrderCost);

                // 8. Строим покрытие из всех построенных отгрузок
                rc = 8;
                CourierDeliveryInfo[] deiveryCover;
                bool[] orderCoverMap;
                rc1 = BuildDeliveryCoverEy(shopCouriers, allDeliveries, onTimeOrders, false, geoCache, out deiveryCover, out orderCoverMap);
                if (rc1 != 0)
                    return rc = 100 * rc + rc1;

                // 9. Отбираем не доставленные заказы и подсчитываем число не собранных
                rc = 9;
                int receiptedCount = 0;
                Order[] undelivOrders = new Order[onTimeOrders.Length];
                int undeliveredCount = 0;

                for (int i = 0; i < orderCoverMap.Length; i++)
                {
                    if (!orderCoverMap[i] && onTimeOrders[i].Status == OrderStatus.Receipted)
                    {
                        Order order = onTimeOrders[i];
                        order.RejectionReason = OrderRejectionReason.CourierNa;
                        //Helper.WriteWarningToLog($"Receipted order can't delivery By Courier. CreateShopDeliveries. Shop {shop.Id}. Order {shopOrders[i].Id}, calcTime {calcTime}");
                        Logger.WriteToLog(string.Format(MessagePatterns.REJECT_RECEIPTED_ORDER_BY_COURIER, order.Id, shop.Id, order.DeliveryTimeTo, calcTime));
                        undelivOrders[undeliveredCount++] = order;
                    }
                    else if (onTimeOrders[i].Status == OrderStatus.Receipted)
                    {
                        receiptedCount++;
                    }
                }

                // 10. Если в покрытии есть не собранные заказы
                rc = 10;
                if (receiptedCount > 0)
                {
                    receiptedOrders = new CourierDeliveryInfo[receiptedCount];
                    receiptedCount = 0;

                    for (int i = 0; i < deiveryCover.Length; i++)
                    {
                        if (!deiveryCover[i].HasAssembledOnly)
                        {
                            receiptedOrders[receiptedCount++] = deiveryCover[i];
                        }
                    }

                    if (receiptedCount < receiptedOrders.Length)
                    {
                        Array.Resize(ref receiptedOrders, receiptedCount);
                    }

                    rc1 = BuildDeliveryCoverEy(shopCouriers, allDeliveries, onTimeOrders, true, geoCache, out deiveryCover, out orderCoverMap);
                    if (rc1 != 0)
                        return rc = 100 * rc + rc1;
                }

                assembledOrders = deiveryCover;

                // 11. Формируем список заказов которые не могут быть доставлены в срок
                rc = 11;
                for (int i = 0; i < onTimeOrders.Length; i++)
                {
                    Order order = onTimeOrders[i];
                    if (!orderCoverMap[i] && order.Status == OrderStatus.Assembled)
                    {
                        Logger.WriteToLog(string.Format(MessagePatterns.REJECT_ASSEMBLED_ORDER_BY_COURIER, order.Id, shop.Id, order.DeliveryTimeTo, calcTime));
                        undelivOrders[undeliveredCount++] = order;
                    }
                }

                if (undeliveredCount < undelivOrders.Length)
                {
                    Array.Resize(ref undelivOrders, undeliveredCount);
                }

                undeliveredOrders = undelivOrders;

                // 12. Выход - Ok
                rc = 0;
                return rc;
            }
            catch (Exception ex)
            {
                //Helper.WriteErrorToLog($"CreateShopDeliveriesEx(Shop {shop.Id}, Orders {Helper.ArrayToString(allOrdersOfShop.Select(order => order.Id).ToArray())}, couriers { Helper.ArrayToString(shopCouriers.Select(courier => courier.Id).ToArray())})");
                //Helper.WriteErrorToLog($"(rc = {rc})");
                //Logger.WriteToLog(ex.ToString());

                Logger.WriteToLog(string.Format(MessagePatterns.METHOD_CALL, "CreateShopDeliveries_OnTime", $"Shop {shop.Id}, Orders {Helper.ArrayToString(onTimeOrders.Select(order => order.Id).ToArray())}, couriers { Helper.ArrayToString(shopCouriers.Select(courier => courier.Id).ToArray())}"));
                Logger.WriteToLog(string.Format(MessagePatterns.METHOD_RC, "CreateShopDeliveries_OnTime", rc));
                Logger.WriteToLog(string.Format(MessagePatterns.METHOD_FAIL, "CreateShopDeliveries_OnTime", ex.ToString()));

                return rc;
            }
        }

        /// <summary>
        /// Создание отгрузок для заказов магазина
        /// </summary>
        /// <param name="shop">Магазин</param>
        /// <param name="behindTimeOrders">Просроченные заказы магазина, для которых создаются отгрузки</param>
        /// <param name="shopCouriers">Доступные для доставки заказов курьеры и такси</param>
        /// <param name="calcTime">Время проведения расчетов</param>
        /// <param name="assembledOrders">Отгрузки покрывающие все собранные заказы</param>
        /// <param name="receiptedOrders">Отгрузки, в которые могут попасть поступившие, но не собранные заказы</param>
        /// <param name="undeliveredOrders">Заказы, которые не могут быть доставлены в срок</param>
        /// <returns>0 - отгрузки созданы; иначе - отгрузки не созданы</returns>
        private int CreateShopDeliveries_BehindTime(Shop shop, Order[] behindTimeOrders, Courier[] shopCouriers, DateTime calcTime, out CourierDeliveryInfo[] assembledOrders, out CourierDeliveryInfo[] receiptedOrders, out Order[] undeliveredOrders)
        {
            // 1. Инициализация
            int rc = 1;
            int rc1 = 1;
            assembledOrders = null;
            receiptedOrders = null;
            undeliveredOrders = null;
            DateTime[] saveTimeTo = null;

            try
            {
                // 2. Проверяем исходные 
                rc = 2;
                if (geoCache == null)
                    return rc;
                if (shop == null)
                    return rc;
                if (shopCouriers == null || shopCouriers.Length <= 0)
                    return rc;
                if (behindTimeOrders == null || behindTimeOrders.Length <= 0)
                    return rc;

                // 3. Выбираем по одному курьру каждого типа среди заданных
                rc = 3;
                Dictionary<CourierVehicleType, Courier> allTypeCouriers = new Dictionary<CourierVehicleType, Courier>(8);
                for (int i = 0; i < shopCouriers.Length; i++)
                {
                    Courier courier = shopCouriers[i];
                    if (courier.Status != CourierStatus.Ready)
                        continue;

                    if (!allTypeCouriers.ContainsKey(courier.CourierType.VechicleType))
                    {
                        if (courier.IsTaxi)
                        {
                            allTypeCouriers.Add(courier.CourierType.VechicleType, courier);
                        }
                        else
                        {
                            Courier courierClone = courier.Clone();
                            courierClone.WorkStart = TimeSpan.Zero;
                            courierClone.WorkEnd = new TimeSpan(23, 59, 59);
                            courierClone.LunchTimeStart = TimeSpan.Zero;
                            courierClone.LunchTimeEnd = TimeSpan.Zero;
                            allTypeCouriers.Add(courierClone.CourierType.VechicleType, courierClone);
                        }
                    }
                }

                if (allTypeCouriers.Count <= 0)
                    return rc;

                saveTimeTo = new DateTime[behindTimeOrders.Length];
                for (int i = 0; i < behindTimeOrders.Length; i++)
                {
                    Order order = behindTimeOrders[i];
                    saveTimeTo[i] = order.DeliveryTimeTo;
                    order.DeliveryTimeTo = calcTime.AddHours(2);
                }

                // 4. Обеспечиваем наличие всех необходимых расстояний и времени движения между парами точек в двух направлениях
                rc = 4;
                Courier[] allCouriers = new Courier[allTypeCouriers.Count];
                allTypeCouriers.Values.CopyTo(allCouriers, 0);
                int size = behindTimeOrders.Length + 1;
                double[] latitude = new double[size];
                double[] longitude = new double[size];

                latitude[size - 1] = shop.Latitude;
                longitude[size - 1] = shop.Longitude;
                shop.LocationIndex = size - 1;

                for (int i = 0; i < behindTimeOrders.Length; i++)
                {
                    Order order = behindTimeOrders[i];
                    order.LocationIndex = i;
                    latitude[i] = order.Latitude;
                    longitude[i] = order.Longitude;
                }

                for (int i = 0; i < allTypeCouriers.Count; i++)
                {
                    rc1 = geoCache.PutLocationInfo(latitude, longitude, allCouriers[i].CourierType.VechicleType);
                    if (rc1 != 0)
                        return rc = 100 * rc + rc1;
                }

                // 5. Запускаем построение всех возможных путей всеми возможными способами
                //    Каждый способ доставки обрабатывается в отдельном потоке
                rc = 5;
                CourierDeliveryInfo[] allDeliveries = new CourierDeliveryInfo[behindTimeOrders.Length * allTypeCouriers.Count];
                int deliveryCount = 0;
                CourierDeliveryInfo singleDelivery;

                for (int i = 0; i < behindTimeOrders.Length; i++)
                {
                    Order order = behindTimeOrders[i];

                    foreach (Courier shopCourier in allCouriers)
                    {
                        rc1 = CreateSingleDelivery(shop, order, shopCourier, !shopCourier.IsTaxi, calcTime, geoCache, out singleDelivery);
                        if (rc1 == 0 && singleDelivery != null)
                        {
                            allDeliveries[deliveryCount++] = singleDelivery;
                        }
                    }
                }

                if (deliveryCount <= 0)
                {
                    for (int i = 0; i < behindTimeOrders.Length; i++)
                    {
                        behindTimeOrders[i].RejectionReason = OrderRejectionReason.CourierNa;
                    }

                    undeliveredOrders = behindTimeOrders;
                    return rc = 0;
                }

                if (deliveryCount < allDeliveries.Length)
                {
                    Array.Resize(ref allDeliveries, deliveryCount);
                }

                // 6. Сортируем по средней стоимости доставки одного заказа
                rc = 6;
                //Array.Sort(allDeliveries, CompareByOrderCost);
                Array.Sort(allDeliveries, CompareByOrderTimeCost);

                // 7. Строим покрытие из всех построенных отгрузок
                rc = 7;
                CourierDeliveryInfo[] deiveryCover;
                bool[] orderCoverMap;
                rc1 = BuildDeliveryCoverEy(shopCouriers, allDeliveries, behindTimeOrders, false, geoCache, out deiveryCover, out orderCoverMap);
                if (rc1 != 0)
                    return rc = 100 * rc + rc1;

                // 8. Отбираем не доставленные заказы и подсчитываем число не собранных
                rc = 8;
                int receiptedCount = 0;
                Order[] undelivOrders = new Order[behindTimeOrders.Length];
                int undeliveredCount = 0;

                for (int i = 0; i < orderCoverMap.Length; i++)
                {
                    if (!orderCoverMap[i] && behindTimeOrders[i].Status == OrderStatus.Receipted)
                    {
                        Order order = behindTimeOrders[i];
                        order.RejectionReason = OrderRejectionReason.CourierNa;
                        //Helper.WriteWarningToLog($"Receipted order can't delivery By Courier. CreateShopDeliveries. Shop {shop.Id}. Order {shopOrders[i].Id}, calcTime {calcTime}");
                        Logger.WriteToLog(string.Format(MessagePatterns.REJECT_RECEIPTED_ORDER_BY_COURIER, order.Id, shop.Id, order.DeliveryTimeTo, calcTime));
                        undelivOrders[undeliveredCount++] = order;
                    }
                    else if (behindTimeOrders[i].Status == OrderStatus.Receipted)
                    {
                        receiptedCount++;
                    }
                }

                // 9. Если в покрытии есть не собранные заказы
                rc = 9;
                if (receiptedCount > 0)
                {
                    receiptedOrders = new CourierDeliveryInfo[receiptedCount];
                    receiptedCount = 0;

                    for (int i = 0; i < deiveryCover.Length; i++)
                    {
                        if (!deiveryCover[i].HasAssembledOnly)
                        {
                            receiptedOrders[receiptedCount++] = deiveryCover[i];
                        }
                    }

                    if (receiptedCount < receiptedOrders.Length)
                    {
                        Array.Resize(ref receiptedOrders, receiptedCount);
                    }

                    rc1 = BuildDeliveryCoverEy(shopCouriers, allDeliveries, behindTimeOrders, true, geoCache, out deiveryCover, out orderCoverMap);
                    if (rc1 != 0)
                        return rc = 100 * rc + rc1;
                }

                SetEndDeliveryIntervalForBehindTimeDeliveries(deiveryCover);
                assembledOrders = deiveryCover;

                // 10. Формируем список заказов которые не могут быть доставлены в срок
                rc = 10;
                for (int i = 0; i < behindTimeOrders.Length; i++)
                {
                    Order order = behindTimeOrders[i];
                    if (!orderCoverMap[i] && order.Status == OrderStatus.Assembled)
                    {
                        Logger.WriteToLog(string.Format(MessagePatterns.REJECT_ASSEMBLED_ORDER_BY_COURIER, order.Id, shop.Id, order.DeliveryTimeTo, calcTime));
                        undelivOrders[undeliveredCount++] = order;
                    }
                }

                if (undeliveredCount < undelivOrders.Length)
                {
                    Array.Resize(ref undelivOrders, undeliveredCount);
                }

                undeliveredOrders = undelivOrders;

                // 11. Выход - Ok
                rc = 0;
                return rc;
            }
            catch (Exception ex)
            {

                Logger.WriteToLog(string.Format(MessagePatterns.METHOD_CALL, "CreateShopDeliveries_OnTime", $"Shop {shop.Id}, Orders {Helper.ArrayToString(behindTimeOrders.Select(order => order.Id).ToArray())}, couriers { Helper.ArrayToString(shopCouriers.Select(courier => courier.Id).ToArray())}"));
                Logger.WriteToLog(string.Format(MessagePatterns.METHOD_RC, "CreateShopDeliveries_OnTime", rc));
                Logger.WriteToLog(string.Format(MessagePatterns.METHOD_FAIL, "CreateShopDeliveries_OnTime", ex.ToString()));

                return rc;
            }
            finally
            {
                if (behindTimeOrders != null && behindTimeOrders.Length > 0 &&
                    saveTimeTo != null && saveTimeTo.Length > 0)
                {
                    for (int i = 0; i < behindTimeOrders.Length; i++)
                    {
                        behindTimeOrders[i].DeliveryTimeTo = saveTimeTo[i];
                    }
                }
            }
        }

        /// <summary>
        /// Создание отгрузок для заказов магазина
        /// </summary>
        /// <param name="shop">Магазин</param>
        /// <param name="behindTimeOrders">Просроченные заказы, которые должны быть первыми в отгрузках</param>
        /// <param name="onTimeOrders">Заказы, которые могут быть отгружены в срок</param>
        /// <param name="shopCouriers">Доступные для доставки заказов курьеры и такси</param>
        /// <param name="calcTime">Время проведения расчетов</param>
        /// <param name="assembledOrders">Отгрузки покрывающие все собранные заказы</param>
        /// <param name="receiptedOrders">Отгрузки, в которые могут попасть поступившие, но не собранные заказы</param>
        /// <param name="undeliveredOrders">Заказы, которые не могут быть доставлены в срок</param>
        /// <returns>0 - отгрузки созданы; иначе - отгрузки не созданы</returns>
        private int CreateShopDeliveries_OnTimeAndBehindTime(Shop shop, Order[] behindTimeOrders, Order[] onTimeOrders, Courier[] shopCouriers, DateTime calcTime, out CourierDeliveryInfo[] assembledOrders, out CourierDeliveryInfo[] receiptedOrders, out Order[] undeliveredOrders)
        {
            // 1. Инициализация
            int rc = 1;
            int rc1 = 1;
            int rc2 = 1;
            assembledOrders = null;
            receiptedOrders = null;
            undeliveredOrders = null;
            DateTime[] saveBehindTimeOrderTimeTo = null;

            try
            {
                // 2. Проверяем исходные 
                rc = 2;
                if (geoCache == null)
                    return rc;
                if (shop == null)
                    return rc;
                if (shopCouriers == null || shopCouriers.Length <= 0)
                    return rc;
                if (behindTimeOrders == null || behindTimeOrders.Length <= 0)
                    return rc;
                if (onTimeOrders == null || onTimeOrders.Length <= 0)
                    return rc;

                // 3. Отбираем собранные onTime-заказы
                rc = 3;
                Order[] onTimeAssembledOrders = onTimeOrders.Where(order => order.Status == OrderStatus.Assembled).ToArray();
                if (onTimeAssembledOrders == null || onTimeAssembledOrders.Length <= 0)
                {
                    // 3.1 Организуем отгрузку просроченных заказов
                    rc = 31;
                    CourierDeliveryInfo[] assembledOrders1;
                    CourierDeliveryInfo[] receiptedOrders1;
                    Order[] undeliveredOrders1;
                    rc1 = CreateShopDeliveries_BehindTime(shop, behindTimeOrders, shopCouriers, calcTime, out assembledOrders1, out receiptedOrders1, out undeliveredOrders1);

                    // 3.2 Выбираем свободных курьеров
                    rc = 32;
                    Courier[] unusedCouriers = null;

                    if (rc1 == 0 && assembledOrders1 != null)
                    {
                        for (int i = 0; i < shopCouriers.Length; i++)
                            shopCouriers[i].Index = -1;

                        foreach (CourierDeliveryInfo delivery in assembledOrders1)
                        {
                            Courier courier = delivery.DeliveryCourier;
                            if (courier != null && !courier.IsTaxi)
                                courier.Index = 1;
                        }

                        unusedCouriers = shopCouriers.Where(courier => courier.Index == -1).ToArray();
                        if (unusedCouriers == null || unusedCouriers.Length <= 0)
                        {
                            assembledOrders = assembledOrders1;
                            receiptedOrders = receiptedOrders1;
                            undeliveredOrders = undeliveredOrders1;
                            return rc = 0;
                        }
                    }
                    else
                    {
                        unusedCouriers = shopCouriers;
                    }

                    // 3.3 Построение отгрузок для собранных и рекомендаций для не собранных
                    rc = 3;
                    CourierDeliveryInfo[] assembledOrders2;
                    CourierDeliveryInfo[] receiptedOrders2;
                    Order[] undeliveredOrders2;
                    rc2 = CreateShopDeliveries_OnTime(shop, onTimeOrders, unusedCouriers, calcTime, out assembledOrders2, out receiptedOrders2, out undeliveredOrders2);

                    // 3.4 Слияние двух результатов
                    rc = 34;

                    if (rc1 != 0)
                    {
                        assembledOrders1 = null;
                        receiptedOrders1 = null;
                        undeliveredOrders1 = null;
                    }

                    if (rc2 != 0)
                    {
                        assembledOrders2 = null;
                        receiptedOrders2 = null;
                        undeliveredOrders2 = null;
                    }

                    assembledOrders = MergeCourierDeliveryInfo(assembledOrders1, assembledOrders2);
                    receiptedOrders = MergeCourierDeliveryInfo(receiptedOrders1, receiptedOrders2);
                    undeliveredOrders = MergeOrders(undeliveredOrders1, undeliveredOrders2);

                    return 1000 * rc1 + rc2;
                }

                // 4. Выбираем по одному курьру каждого типа среди заданных
                rc = 4;
                Dictionary<CourierVehicleType, Courier> allTypeCouriers = new Dictionary<CourierVehicleType, Courier>(8);
                for (int i = 0; i < shopCouriers.Length; i++)
                {
                    Courier courier = shopCouriers[i];
                    if (courier.Status != CourierStatus.Ready)
                        continue;

                    if (!allTypeCouriers.ContainsKey(courier.CourierType.VechicleType))
                    {
                        if (courier.IsTaxi)
                        {
                            allTypeCouriers.Add(courier.CourierType.VechicleType, courier);
                        }
                        else
                        {
                            Courier courierClone = courier.Clone();
                            courierClone.WorkStart = TimeSpan.Zero;
                            courierClone.WorkEnd = new TimeSpan(23, 59, 59);
                            courierClone.LunchTimeStart = TimeSpan.Zero;
                            courierClone.LunchTimeEnd = TimeSpan.Zero;
                            allTypeCouriers.Add(courierClone.CourierType.VechicleType, courierClone);
                        }
                    }
                }

                if (allTypeCouriers.Count <= 0)
                    return rc;

                // 5. Обеспечиваем наличие всех необходимых расстояний и времени движения между парами точек в двух направлениях
                rc = 5;
                Courier[] allCouriers = new Courier[allTypeCouriers.Count];
                allTypeCouriers.Values.CopyTo(allCouriers, 0);
                int size = onTimeOrders.Length + behindTimeOrders.Length + 1;
                double[] latitude = new double[size];
                double[] longitude = new double[size];

                latitude[size - 1] = shop.Latitude;
                longitude[size - 1] = shop.Longitude;
                shop.LocationIndex = size - 1;

                for (int i = 0; i < onTimeOrders.Length; i++)
                {
                    Order order = onTimeOrders[i];
                    order.LocationIndex = i;
                    latitude[i] = order.Latitude;
                    longitude[i] = order.Longitude;
                }

                int k = onTimeOrders.Length;

                for (int i = 0; i < behindTimeOrders.Length; i++, k++)
                {
                    Order order = behindTimeOrders[i];
                    order.LocationIndex = k;
                    latitude[k] = order.Latitude;
                    longitude[k] = order.Longitude;
                }

                for (int i = 0; i < allTypeCouriers.Count; i++)
                {
                    rc1 = geoCache.PutLocationInfo(latitude, longitude, allCouriers[i].CourierType.VechicleType);
                    if (rc1 != 0)
                        return rc = 100 * rc + rc1;
                }

                // 6. Сохраняем to-время behindTimeOrders и назначаем новое
                rc = 6;
                saveBehindTimeOrderTimeTo = new DateTime[behindTimeOrders.Length];
                DateTime newToTime = calcTime.AddHours(2);

                for (int i = 0; i < behindTimeOrders.Length; i++)
                {
                    Order order = behindTimeOrders[i];
                    saveBehindTimeOrderTimeTo[i] = order.DeliveryTimeTo;
                    order.DeliveryTimeTo = newToTime;
                }

                // 7. Запускаем построение всех возможных путей всеми возможными способами
                //    Каждый способ доставки обрабатывается в отдельном потоке
                rc = 7;
                //DateTime calcTime = new DateTime(2020, 11, 4, 18, 50, 0);
                Task<int>[] tasks = new Task<int>[allTypeCouriers.Count];
                CourierDeliveryInfo[][] taskDeliveries = new CourierDeliveryInfo[allTypeCouriers.Count][];

                switch (allCouriers.Length)
                {
                    case 1:
                        tasks[0] = Task.Run(() => CreateShopDeliveriesEx(shop, behindTimeOrders, onTimeAssembledOrders, allCouriers[0], !allCouriers[0].IsTaxi, calcTime, out taskDeliveries[0]));
                        break;
                    case 2:
                        tasks[0] = Task.Run(() => CreateShopDeliveriesEx(shop, behindTimeOrders, onTimeAssembledOrders, allCouriers[0], !allCouriers[0].IsTaxi, calcTime, out taskDeliveries[0]));
                        tasks[1] = Task.Run(() => CreateShopDeliveriesEx(shop, behindTimeOrders, onTimeAssembledOrders, allCouriers[1], !allCouriers[1].IsTaxi, calcTime, out taskDeliveries[1]));
                        break;
                    case 3:
                        tasks[0] = Task.Run(() => CreateShopDeliveriesEx(shop, behindTimeOrders, onTimeAssembledOrders, allCouriers[0], !allCouriers[0].IsTaxi, calcTime, out taskDeliveries[0]));
                        tasks[1] = Task.Run(() => CreateShopDeliveriesEx(shop, behindTimeOrders, onTimeAssembledOrders, allCouriers[1], !allCouriers[1].IsTaxi, calcTime, out taskDeliveries[1]));
                        tasks[2] = Task.Run(() => CreateShopDeliveriesEx(shop, behindTimeOrders, onTimeAssembledOrders, allCouriers[2], !allCouriers[2].IsTaxi, calcTime, out taskDeliveries[2]));
                        break;
                    case 4:
                        tasks[0] = Task.Run(() => CreateShopDeliveriesEx(shop, behindTimeOrders, onTimeAssembledOrders, allCouriers[0], !allCouriers[0].IsTaxi, calcTime, out taskDeliveries[0]));
                        tasks[1] = Task.Run(() => CreateShopDeliveriesEx(shop, behindTimeOrders, onTimeAssembledOrders, allCouriers[1], !allCouriers[1].IsTaxi, calcTime, out taskDeliveries[1]));
                        tasks[2] = Task.Run(() => CreateShopDeliveriesEx(shop, behindTimeOrders, onTimeAssembledOrders, allCouriers[2], !allCouriers[2].IsTaxi, calcTime, out taskDeliveries[2]));
                        tasks[3] = Task.Run(() => CreateShopDeliveriesEx(shop, behindTimeOrders, onTimeAssembledOrders, allCouriers[3], !allCouriers[3].IsTaxi, calcTime, out taskDeliveries[3]));
                        break;
                    case 5:
                        tasks[0] = Task.Run(() => CreateShopDeliveriesEx(shop, behindTimeOrders, onTimeAssembledOrders, allCouriers[0], !allCouriers[0].IsTaxi, calcTime, out taskDeliveries[0]));
                        tasks[1] = Task.Run(() => CreateShopDeliveriesEx(shop, behindTimeOrders, onTimeAssembledOrders, allCouriers[1], !allCouriers[1].IsTaxi, calcTime, out taskDeliveries[1]));
                        tasks[2] = Task.Run(() => CreateShopDeliveriesEx(shop, behindTimeOrders, onTimeAssembledOrders, allCouriers[2], !allCouriers[2].IsTaxi, calcTime, out taskDeliveries[2]));
                        tasks[3] = Task.Run(() => CreateShopDeliveriesEx(shop, behindTimeOrders, onTimeAssembledOrders, allCouriers[3], !allCouriers[3].IsTaxi, calcTime, out taskDeliveries[3]));
                        tasks[4] = Task.Run(() => CreateShopDeliveriesEx(shop, behindTimeOrders, onTimeAssembledOrders, allCouriers[4], !allCouriers[4].IsTaxi, calcTime, out taskDeliveries[4]));
                        break;
                    case 6:
                        tasks[0] = Task.Run(() => CreateShopDeliveriesEx(shop, behindTimeOrders, onTimeAssembledOrders, allCouriers[0], !allCouriers[0].IsTaxi, calcTime, out taskDeliveries[0]));
                        tasks[1] = Task.Run(() => CreateShopDeliveriesEx(shop, behindTimeOrders, onTimeAssembledOrders, allCouriers[1], !allCouriers[1].IsTaxi, calcTime, out taskDeliveries[1]));
                        tasks[2] = Task.Run(() => CreateShopDeliveriesEx(shop, behindTimeOrders, onTimeAssembledOrders, allCouriers[2], !allCouriers[2].IsTaxi, calcTime, out taskDeliveries[2]));
                        tasks[3] = Task.Run(() => CreateShopDeliveriesEx(shop, behindTimeOrders, onTimeAssembledOrders, allCouriers[3], !allCouriers[3].IsTaxi, calcTime, out taskDeliveries[3]));
                        tasks[4] = Task.Run(() => CreateShopDeliveriesEx(shop, behindTimeOrders, onTimeAssembledOrders, allCouriers[4], !allCouriers[4].IsTaxi, calcTime, out taskDeliveries[4]));
                        tasks[5] = Task.Run(() => CreateShopDeliveriesEx(shop, behindTimeOrders, onTimeAssembledOrders, allCouriers[5], !allCouriers[5].IsTaxi, calcTime, out taskDeliveries[5]));
                        break;
                    case 7:
                        tasks[0] = Task.Run(() => CreateShopDeliveriesEx(shop, behindTimeOrders, onTimeAssembledOrders, allCouriers[0], !allCouriers[0].IsTaxi, calcTime, out taskDeliveries[0]));
                        tasks[1] = Task.Run(() => CreateShopDeliveriesEx(shop, behindTimeOrders, onTimeAssembledOrders, allCouriers[1], !allCouriers[1].IsTaxi, calcTime, out taskDeliveries[1]));
                        tasks[2] = Task.Run(() => CreateShopDeliveriesEx(shop, behindTimeOrders, onTimeAssembledOrders, allCouriers[2], !allCouriers[2].IsTaxi, calcTime, out taskDeliveries[2]));
                        tasks[3] = Task.Run(() => CreateShopDeliveriesEx(shop, behindTimeOrders, onTimeAssembledOrders, allCouriers[3], !allCouriers[3].IsTaxi, calcTime, out taskDeliveries[3]));
                        tasks[4] = Task.Run(() => CreateShopDeliveriesEx(shop, behindTimeOrders, onTimeAssembledOrders, allCouriers[4], !allCouriers[4].IsTaxi, calcTime, out taskDeliveries[4]));
                        tasks[5] = Task.Run(() => CreateShopDeliveriesEx(shop, behindTimeOrders, onTimeAssembledOrders, allCouriers[5], !allCouriers[5].IsTaxi, calcTime, out taskDeliveries[5]));
                        tasks[6] = Task.Run(() => CreateShopDeliveriesEx(shop, behindTimeOrders, onTimeAssembledOrders, allCouriers[6], !allCouriers[6].IsTaxi, calcTime, out taskDeliveries[6]));
                        break;
                    case 8:
                        tasks[0] = Task.Run(() => CreateShopDeliveriesEx(shop, behindTimeOrders, onTimeAssembledOrders, allCouriers[0], !allCouriers[0].IsTaxi, calcTime, out taskDeliveries[0]));
                        tasks[1] = Task.Run(() => CreateShopDeliveriesEx(shop, behindTimeOrders, onTimeAssembledOrders, allCouriers[1], !allCouriers[1].IsTaxi, calcTime, out taskDeliveries[1]));
                        tasks[2] = Task.Run(() => CreateShopDeliveriesEx(shop, behindTimeOrders, onTimeAssembledOrders, allCouriers[2], !allCouriers[2].IsTaxi, calcTime, out taskDeliveries[2]));
                        tasks[3] = Task.Run(() => CreateShopDeliveriesEx(shop, behindTimeOrders, onTimeAssembledOrders, allCouriers[3], !allCouriers[3].IsTaxi, calcTime, out taskDeliveries[3]));
                        tasks[4] = Task.Run(() => CreateShopDeliveriesEx(shop, behindTimeOrders, onTimeAssembledOrders, allCouriers[4], !allCouriers[4].IsTaxi, calcTime, out taskDeliveries[4]));
                        tasks[5] = Task.Run(() => CreateShopDeliveriesEx(shop, behindTimeOrders, onTimeAssembledOrders, allCouriers[5], !allCouriers[5].IsTaxi, calcTime, out taskDeliveries[5]));
                        tasks[6] = Task.Run(() => CreateShopDeliveriesEx(shop, behindTimeOrders, onTimeAssembledOrders, allCouriers[6], !allCouriers[6].IsTaxi, calcTime, out taskDeliveries[6]));
                        tasks[7] = Task.Run(() => CreateShopDeliveriesEx(shop, behindTimeOrders, onTimeAssembledOrders, allCouriers[7], !allCouriers[7].IsTaxi, calcTime, out taskDeliveries[7]));
                        break;
                }

                // Дожидаёмся завершения обработки
                Task.WaitAll(tasks);

                // 8. Подсчитываем общее число построенных отгрузок
                rc = 8;
                int deliveryCount = 0;

                for (int i = 0; i < tasks.Length; i++)
                {
                    int rcx = tasks[i].Result;

                    if (rcx == 0)
                    {
                        if (taskDeliveries[i] != null)
                            deliveryCount += taskDeliveries[i].Length;
                    }
                }

                // 9. Стром покрытие для просроченных заказов
                rc = 9;
                CourierDeliveryInfo[] behindTimeDeliveries = null;
                CourierDeliveryInfo[] allDeliveries;

                if (deliveryCount > 0)
                {
                    // 9.1 Выбираем все построенные отгрузки
                    rc = 91;
                    allDeliveries = new CourierDeliveryInfo[deliveryCount];
                    deliveryCount = 0;

                    for (int i = 0; i < tasks.Length; i++)
                    {
                        if (tasks[i].Result == 0)
                        {
                            if (taskDeliveries[i] != null)
                            {
                                taskDeliveries[i].CopyTo(allDeliveries, deliveryCount);
                                deliveryCount += taskDeliveries[i].Length;
                            }
                        }
                    }

                    // 9.2 Сортируем по средней стоимости доставки одного заказа
                    rc = 92;
                    //Array.Sort(allDeliveries, CompareByOrderCost);
                    Array.Sort(allDeliveries, CompareByOrderTimeCost);

                    // 9.3 Строим покрытие для просроченных заказов
                    bool[] orderCoverMap;
                    Order[] allOrders = onTimeOrders.Concat(behindTimeOrders).ToArray();
                    rc1 = BuildDeliveryCoverEy(shopCouriers, allDeliveries, allOrders, true, geoCache, out behindTimeDeliveries, out orderCoverMap);

                    if (rc1 == 0)
                    {
                        SetEndDeliveryIntervalForBehindTimeDeliveries(behindTimeDeliveries);
                    }
                    else
                    {
                        behindTimeDeliveries = null;
                    }
                }

                // 10. Отбираем свободных курьеров и оставшиеся onTime-заказы
                rc = 10;
                Courier[] freeCouriers = null;
                Order[] freeOrders = null;

                if (behindTimeDeliveries != null && behindTimeDeliveries.Length > 0)
                {
                    for (int i = 0; i < shopCouriers.Length; i++)
                        shopCouriers[i].Index = -1;

                    for (int i = 0; i < onTimeOrders.Length; i++)
                        onTimeOrders[i].Index = -1;

                    foreach (CourierDeliveryInfo delivery in behindTimeDeliveries)
                    {
                        Courier courier = delivery.DeliveryCourier;
                        if (courier != null && !courier.IsTaxi)
                            courier.Index = 1;
                        Order[] deliveryOrders = delivery.Orders;
                        if (deliveryOrders != null)
                        {
                            foreach (Order order in deliveryOrders)
                            {
                                order.Index = 1;
                            }
                        }
                    }

                    freeCouriers = shopCouriers.Where(courier => courier.Index == -1).ToArray();
                    freeOrders = onTimeOrders.Where(order => order.Index == -1).ToArray();

                    if (freeCouriers == null || freeCouriers.Length <= 0 ||
                        freeOrders == null || freeOrders.Length <= 0)
                    {
                        assembledOrders = behindTimeDeliveries;
                        return rc = 0;
                    }
                }
                else
                {
                    freeCouriers = shopCouriers;
                    freeOrders = onTimeOrders;
                }

                // 10. Стороим покрытие для заказов, которые могут быть доставлены в срок
                rc = 10;
                rc1 = CreateShopDeliveries_OnTime(shop, freeOrders, freeCouriers, calcTime, out assembledOrders, out receiptedOrders, out undeliveredOrders);
                if (rc1 == 0)
                {
                    assembledOrders = MergeCourierDeliveryInfo(behindTimeDeliveries, assembledOrders);
                }
                else
                {
                    assembledOrders = behindTimeDeliveries;
                }

                // 11. Выход - Ok
                rc = 0;
                return rc;
            }
            catch (Exception ex)
            {
                Logger.WriteToLog(string.Format(MessagePatterns.METHOD_CALL, "CreateShopDeliveries_OnTimeAndBehindTime", $"Shop {shop.Id}, BehindTimeOrders {Helper.ArrayToString(behindTimeOrders.Select(order => order.Id).ToArray())}, OnTimeOrders {Helper.ArrayToString(onTimeOrders.Select(order => order.Id).ToArray())}, couriers { Helper.ArrayToString(shopCouriers.Select(courier => courier.Id).ToArray())}"));
                Logger.WriteToLog(string.Format(MessagePatterns.METHOD_RC, "CreateShopDeliveries_OnTimeAndBehindTime", rc));
                Logger.WriteToLog(string.Format(MessagePatterns.METHOD_FAIL, "CreateShopDeliveries_OnTimeAndBehindTime", ex.ToString()));

                return rc;
            }
            finally
            {
                if (behindTimeOrders != null && behindTimeOrders.Length > 0 &&
                    saveBehindTimeOrderTimeTo != null && saveBehindTimeOrderTimeTo.Length > 0)
                {
                    for (int i = 0; i < behindTimeOrders.Length; i++)
                    {
                        behindTimeOrders[i].DeliveryTimeTo = saveBehindTimeOrderTimeTo[i];
                    }
                }
            }
        }

        /// <summary>
        /// Изменение вресени конца интервала начала доставки,
        /// чтобы отгрузка была отправлена сразу (хак) 
        /// </summary>
        /// <param name="courierDeliveries">Отгрузки, в которых изменяется EndDeliveryInterval:
        /// EndDeliveryInterval = StartDeliveryInterval + 1 мин
        /// </param>
        private static void SetEndDeliveryIntervalForBehindTimeDeliveries(CourierDeliveryInfo[] courierDeliveries)
        {
            try
            {
                if (courierDeliveries == null || courierDeliveries.Length <= 0)
                    return;
                foreach (CourierDeliveryInfo delivery in courierDeliveries)
                {
                    delivery.EndDeliveryInterval = delivery.StartDeliveryInterval.AddMinutes(1);
                }
            }
            catch
            {   }
        }

        ///// <summary>
        ///// Создание отгрузок для заказов магазина
        ///// </summary>
        ///// <param name="shop">Магазин</param>
        ///// <param name="allOrdersOfShop">Заказы магазина, для которых создаются отгрузки</param>
        ///// <param name="shopCouriers">Доступные для доставки заказов курьеры и такси</param>
        ///// <param name="assembledOrders">Отгрузки покрывающие все собранные заказы</param>
        ///// <param name="receiptedOrders">Отгрузки, в которые могут попасть поступившие, но не собранные заказы</param>
        ///// <param name="undeliveredOrders">Заказы, которые не могут быть доставлены в срок</param>
        ///// <returns>0 - отгрузки созданы; иначе - отгрузки не созданы</returns>
        //public int CreateShopDeliveriesEx(Shop shop, Order[] allOrdersOfShop, Courier[] shopCouriers, out CourierDeliveryInfo[] assembledOrders, out CourierDeliveryInfo[] receiptedOrders, out Order[] undeliveredOrders)
        //{
        //    // 1. Инициализация
        //    int rc = 1;
        //    int rc1 = 1;
        //    assembledOrders = null;
        //    receiptedOrders = null;
        //    undeliveredOrders = null;

        //    try
        //    {
        //        // 2. Проверяем исходные 
        //        rc = 2;
        //        if (geoCache == null)
        //            return rc;
        //        if (shop == null)
        //            return rc;
        //        if (shopCouriers == null || shopCouriers.Length <= 0)
        //            return rc;
        //        if (allOrdersOfShop == null || allOrdersOfShop.Length <= 0)
        //            return rc;

        //        // 3. Отбираем заказы, которые не могут быть доставлены в срок на данный момент
        //        rc = 3;
        //        DateTime calcTime = DateTime.Now;
        //        //calcTime = new DateTime(2020, 11, 8, 10, 19, 0);
        //        Order[] undelivOrders = new Order[allOrdersOfShop.Length];
        //        Order[] shopOrders = new Order[allOrdersOfShop.Length];
        //        int undeliveredCount = 0;
        //        int orderCount = 0;
        //        int[] courierVehicleTypes = GetCourierVehicleTypes(shopCouriers);
        //        int[] orderVehicleTypes = new int[8];
        //        int vcount = 0;

        //        Order[] undeliveredOnTimeOrders = new Order[allOrdersOfShop.Length];
        //        int undeliveredOnTimeCount = 0;


        //        for (int i = 0; i < allOrdersOfShop.Length; i++)
        //        {
        //            Order order = allOrdersOfShop[i];
        //            order.RejectionReason = OrderRejectionReason.None;

        //            if (calcTime > order.DeliveryTimeTo)
        //            {
        //                undeliveredOnTimeOrders[undeliveredOnTimeCount++] = order;
        //                //Helper.WriteWarningToLog($"Undelivered By Time. CreateShopDeliveries. Shop {shop.Id}. Order {order.Id}, order.DeliveryTimeTo {order.DeliveryTimeTo}, calcTime {calcTime}");
        //                Logger.WriteToLog(string.Format(MessagePatterns.REJECT_ORDER_BY_TIME, order.Id, shop.Id, order.DeliveryTimeTo, calcTime));

        //                order.RejectionReason = OrderRejectionReason.LateStart;
        //            }
        //            else
        //            {
        //                vcount = 0;
        //                if ((order.EnabledTypes & EnabledCourierType.YandexTaxi) != 0)
        //                    orderVehicleTypes[vcount++] = (int)CourierVehicleType.YandexTaxi;
        //                if ((order.EnabledTypes & EnabledCourierType.GettTaxi) != 0)
        //                    orderVehicleTypes[vcount++] = (int)CourierVehicleType.GettTaxi;
        //                if ((order.EnabledTypes & EnabledCourierType.Car) != 0)
        //                    orderVehicleTypes[vcount++] = (int)CourierVehicleType.Car;
        //                if ((order.EnabledTypes & EnabledCourierType.Bicycle) != 0)
        //                    orderVehicleTypes[vcount++] = (int)CourierVehicleType.Bicycle;
        //                if ((order.EnabledTypes & EnabledCourierType.OnFoot) != 0)
        //                    orderVehicleTypes[vcount++] = (int)CourierVehicleType.OnFoot;

        //                bool isCourier = false;

        //                if (vcount > 0)
        //                {
        //                    for (int j = 0; j < vcount; j++)
        //                    {
        //                        if (Array.BinarySearch(courierVehicleTypes, orderVehicleTypes[j]) >= 0)
        //                        {
        //                            isCourier = true;
        //                            break;
        //                        }
        //                    }
        //                }

        //                if (isCourier)
        //                {
        //                    shopOrders[orderCount++] = order;
        //                }
        //                else
        //                {
        //                    undelivOrders[undeliveredCount++] = order;
        //                    //Helper.WriteWarningToLog($"Order Rejected. Courier is not available. CreateShopDeliveries. Shop {shop.Id}. Order {order.Id}, order.DeliveryTimeTo {order.DeliveryTimeTo}, calcTime {calcTime}");
        //                    Logger.WriteToLog(string.Format(MessagePatterns.REJECT_ORDER_BY_COURIER, order.Id, shop.Id, order.DeliveryTimeTo, calcTime));

        //                    order.RejectionReason = OrderRejectionReason.CourierNa;
        //                }
        //            }
        //        }

        //        if (orderCount <= 0)
        //        {
        //            if (undeliveredCount < undelivOrders.Length)
        //            {
        //                Array.Resize(ref undelivOrders, undeliveredCount);
        //            }
        //            undeliveredOrders = undelivOrders;
        //            return rc = 0;
        //        }

        //        if (orderCount < shopOrders.Length)
        //        {
        //            Array.Resize(ref shopOrders, orderCount);
        //        }

        //        // 4. Выбираем по одному курьру каждого типа среди заданных
        //        rc = 4;
        //        Dictionary<CourierVehicleType, Courier> allTypeCouriers = new Dictionary<CourierVehicleType, Courier>(8);
        //        for (int i = 0; i < shopCouriers.Length; i++)
        //        {
        //            Courier courier = shopCouriers[i];
        //            if (courier.Status != CourierStatus.Ready)
        //                continue;

        //            if (!allTypeCouriers.ContainsKey(courier.CourierType.VechicleType))
        //            {
        //                if (courier.IsTaxi)
        //                {
        //                    allTypeCouriers.Add(courier.CourierType.VechicleType, courier);
        //                }
        //                else
        //                {
        //                    Courier courierClone = courier.Clone();
        //                    courierClone.WorkStart = TimeSpan.Zero;
        //                    courierClone.WorkEnd = new TimeSpan(23, 59, 59);
        //                    courierClone.LunchTimeStart = TimeSpan.Zero;
        //                    courierClone.LunchTimeEnd = TimeSpan.Zero;
        //                    allTypeCouriers.Add(courierClone.CourierType.VechicleType, courierClone);
        //                }
        //            }
        //        }

        //        if (allTypeCouriers.Count <= 0)
        //            return rc;

        //        // 5. Обеспечиваем наличие всех необходимых расстояний и времени движения между парами точек в двух направлениях
        //        rc = 5;
        //        Courier[] allCouriers = new Courier[allTypeCouriers.Count];
        //        allTypeCouriers.Values.CopyTo(allCouriers, 0);
        //        int size = shopOrders.Length + 1;
        //        double[] latitude = new double[size];
        //        double[] longitude = new double[size];

        //        latitude[size - 1] = shop.Latitude;
        //        longitude[size - 1] = shop.Longitude;
        //        shop.LocationIndex = size - 1;

        //        for (int i = 0; i < shopOrders.Length; i++)
        //        {
        //            Order order = shopOrders[i];
        //            order.LocationIndex = i;
        //            latitude[i] = order.Latitude;
        //            longitude[i] = order.Longitude;
        //        }

        //        for (int i = 0; i < allTypeCouriers.Count; i++)
        //        {
        //            rc1 = geoCache.PutLocationInfo(latitude, longitude, allCouriers[i].CourierType.VechicleType);
        //            if (rc1 != 0)
        //                return rc = 100 * rc + rc1;
        //        }

        //        // 6. Запускаем построение всех возможных путей всеми возможными способами
        //        //    Каждый способ доставки обрабатывается в отдельном потоке
        //        rc = 6;
        //        //DateTime calcTime = new DateTime(2020, 11, 4, 18, 50, 0);
        //        Task<int>[] tasks = new Task<int>[allTypeCouriers.Count];
        //        CourierDeliveryInfo[][] taskDeliveries = new CourierDeliveryInfo[allTypeCouriers.Count][];

        //        //rc1 = CreateShopDeliveriesEx(shop, shopOrders, allCouriers[0], isLoop, calcTime, out taskDeliveries[0]);

        //        switch (allCouriers.Length)
        //        {
        //            case 1:
        //                tasks[0] = Task.Run(() => CreateShopDeliveriesEx(shop, shopOrders, allCouriers[0], !allCouriers[0].IsTaxi, calcTime, out taskDeliveries[0]));
        //                break;
        //            case 2:
        //                tasks[0] = Task.Run(() => CreateShopDeliveriesEx(shop, shopOrders, allCouriers[0], !allCouriers[0].IsTaxi, calcTime, out taskDeliveries[0]));
        //                tasks[1] = Task.Run(() => CreateShopDeliveriesEx(shop, shopOrders, allCouriers[1], !allCouriers[1].IsTaxi, calcTime, out taskDeliveries[1]));
        //                break;
        //            case 3:
        //                tasks[0] = Task.Run(() => CreateShopDeliveriesEx(shop, shopOrders, allCouriers[0], !allCouriers[0].IsTaxi, calcTime, out taskDeliveries[0]));
        //                tasks[1] = Task.Run(() => CreateShopDeliveriesEx(shop, shopOrders, allCouriers[1], !allCouriers[1].IsTaxi, calcTime, out taskDeliveries[1]));
        //                tasks[2] = Task.Run(() => CreateShopDeliveriesEx(shop, shopOrders, allCouriers[2], !allCouriers[2].IsTaxi, calcTime, out taskDeliveries[2]));
        //                break;
        //            case 4:
        //                tasks[0] = Task.Run(() => CreateShopDeliveriesEx(shop, shopOrders, allCouriers[0], !allCouriers[0].IsTaxi, calcTime, out taskDeliveries[0]));
        //                tasks[1] = Task.Run(() => CreateShopDeliveriesEx(shop, shopOrders, allCouriers[1], !allCouriers[1].IsTaxi, calcTime, out taskDeliveries[1]));
        //                tasks[2] = Task.Run(() => CreateShopDeliveriesEx(shop, shopOrders, allCouriers[2], !allCouriers[2].IsTaxi, calcTime, out taskDeliveries[2]));
        //                tasks[3] = Task.Run(() => CreateShopDeliveriesEx(shop, shopOrders, allCouriers[3], !allCouriers[3].IsTaxi, calcTime, out taskDeliveries[3]));
        //                break;
        //            case 5:
        //                tasks[0] = Task.Run(() => CreateShopDeliveriesEx(shop, shopOrders, allCouriers[0], !allCouriers[0].IsTaxi, calcTime, out taskDeliveries[0]));
        //                tasks[1] = Task.Run(() => CreateShopDeliveriesEx(shop, shopOrders, allCouriers[1], !allCouriers[1].IsTaxi, calcTime, out taskDeliveries[1]));
        //                tasks[2] = Task.Run(() => CreateShopDeliveriesEx(shop, shopOrders, allCouriers[2], !allCouriers[2].IsTaxi, calcTime, out taskDeliveries[2]));
        //                tasks[3] = Task.Run(() => CreateShopDeliveriesEx(shop, shopOrders, allCouriers[3], !allCouriers[3].IsTaxi, calcTime, out taskDeliveries[3]));
        //                tasks[4] = Task.Run(() => CreateShopDeliveriesEx(shop, shopOrders, allCouriers[4], !allCouriers[4].IsTaxi, calcTime, out taskDeliveries[4]));
        //                break;
        //            case 6:
        //                tasks[0] = Task.Run(() => CreateShopDeliveriesEx(shop, shopOrders, allCouriers[0], !allCouriers[0].IsTaxi, calcTime, out taskDeliveries[0]));
        //                tasks[1] = Task.Run(() => CreateShopDeliveriesEx(shop, shopOrders, allCouriers[1], !allCouriers[1].IsTaxi, calcTime, out taskDeliveries[1]));
        //                tasks[2] = Task.Run(() => CreateShopDeliveriesEx(shop, shopOrders, allCouriers[2], !allCouriers[2].IsTaxi, calcTime, out taskDeliveries[2]));
        //                tasks[3] = Task.Run(() => CreateShopDeliveriesEx(shop, shopOrders, allCouriers[3], !allCouriers[3].IsTaxi, calcTime, out taskDeliveries[3]));
        //                tasks[4] = Task.Run(() => CreateShopDeliveriesEx(shop, shopOrders, allCouriers[4], !allCouriers[4].IsTaxi, calcTime, out taskDeliveries[4]));
        //                tasks[5] = Task.Run(() => CreateShopDeliveriesEx(shop, shopOrders, allCouriers[5], !allCouriers[5].IsTaxi, calcTime, out taskDeliveries[5]));
        //                break;
        //            case 7:
        //                tasks[0] = Task.Run(() => CreateShopDeliveriesEx(shop, shopOrders, allCouriers[0], !allCouriers[0].IsTaxi, calcTime, out taskDeliveries[0]));
        //                tasks[1] = Task.Run(() => CreateShopDeliveriesEx(shop, shopOrders, allCouriers[1], !allCouriers[1].IsTaxi, calcTime, out taskDeliveries[1]));
        //                tasks[2] = Task.Run(() => CreateShopDeliveriesEx(shop, shopOrders, allCouriers[2], !allCouriers[2].IsTaxi, calcTime, out taskDeliveries[2]));
        //                tasks[3] = Task.Run(() => CreateShopDeliveriesEx(shop, shopOrders, allCouriers[3], !allCouriers[3].IsTaxi, calcTime, out taskDeliveries[3]));
        //                tasks[4] = Task.Run(() => CreateShopDeliveriesEx(shop, shopOrders, allCouriers[4], !allCouriers[4].IsTaxi, calcTime, out taskDeliveries[4]));
        //                tasks[5] = Task.Run(() => CreateShopDeliveriesEx(shop, shopOrders, allCouriers[5], !allCouriers[5].IsTaxi, calcTime, out taskDeliveries[5]));
        //                tasks[6] = Task.Run(() => CreateShopDeliveriesEx(shop, shopOrders, allCouriers[6], !allCouriers[6].IsTaxi, calcTime, out taskDeliveries[6]));
        //                break;
        //            case 8:
        //                tasks[0] = Task.Run(() => CreateShopDeliveriesEx(shop, shopOrders, allCouriers[0], !allCouriers[0].IsTaxi, calcTime, out taskDeliveries[0]));
        //                tasks[1] = Task.Run(() => CreateShopDeliveriesEx(shop, shopOrders, allCouriers[1], !allCouriers[1].IsTaxi, calcTime, out taskDeliveries[1]));
        //                tasks[2] = Task.Run(() => CreateShopDeliveriesEx(shop, shopOrders, allCouriers[2], !allCouriers[2].IsTaxi, calcTime, out taskDeliveries[2]));
        //                tasks[3] = Task.Run(() => CreateShopDeliveriesEx(shop, shopOrders, allCouriers[3], !allCouriers[3].IsTaxi, calcTime, out taskDeliveries[3]));
        //                tasks[4] = Task.Run(() => CreateShopDeliveriesEx(shop, shopOrders, allCouriers[4], !allCouriers[4].IsTaxi, calcTime, out taskDeliveries[4]));
        //                tasks[5] = Task.Run(() => CreateShopDeliveriesEx(shop, shopOrders, allCouriers[5], !allCouriers[5].IsTaxi, calcTime, out taskDeliveries[5]));
        //                tasks[6] = Task.Run(() => CreateShopDeliveriesEx(shop, shopOrders, allCouriers[6], !allCouriers[6].IsTaxi, calcTime, out taskDeliveries[6]));
        //                tasks[7] = Task.Run(() => CreateShopDeliveriesEx(shop, shopOrders, allCouriers[7], !allCouriers[7].IsTaxi, calcTime, out taskDeliveries[7]));
        //                break;
        //        }

        //        //int rcz = CreateShopDeliveriesEx(shop, shopOrders, allCouriers[1], isLoop, calcTime, out taskDeliveries[1]);

        //        //for (int i = 0; i < tasks.Length; i++)
        //        //{
        //        //    tasks[i] = Task.Run(() => CreateShopDeliveriesEx(shop, shopOrders, allCouriers[i], isLoop, calcTime, out taskDeliveries[i]));
        //        //}

        //        Task.WaitAll(tasks);

        //        // 7. Объединяем все построенные отгрузки
        //        rc = 7;
        //        int deliveryCount = 0;
        //        CourierDeliveryInfo[] allDeliveries;

        //        for (int i = 0; i < tasks.Length; i++)
        //        {
        //            int rcx = tasks[i].Result;

        //            //if (tasks[i].Result == 0)
        //            if (rcx == 0)
        //            {
        //                if (taskDeliveries[i] != null)
        //                    deliveryCount += taskDeliveries[i].Length;
        //            }
        //            //else
        //            //{
        //            //    rc = rc;
        //            //}
        //        }

        //        if (deliveryCount <= 0)
        //        {
        //            undeliveredOrders = shopOrders;
        //            return rc = 0;
        //        }

        //        allDeliveries = new CourierDeliveryInfo[deliveryCount];
        //        deliveryCount = 0;

        //        for (int i = 0; i < tasks.Length; i++)
        //        {
        //            if (tasks[i].Result == 0)
        //            {
        //                if (taskDeliveries[i] != null)
        //                {
        //                    taskDeliveries[i].CopyTo(allDeliveries, deliveryCount);
        //                    deliveryCount += taskDeliveries[i].Length;
        //                }
        //            }
        //        }

        //        // 8. Сортируем по средней стоимости доставки одного заказа
        //        rc = 8;
        //        Array.Sort(allDeliveries, CompareByOrderCost);

        //        //// 9. Присваиваем заказам индексы
        //        //rc = 9;
        //        //for (int i = 0; i < shopOrders.Length; i++)
        //        //{
        //        //    shopOrders[i].Index = i;
        //        //}

        //        // 10. Строим покрытие из всех построенных отгрузок
        //        rc = 10;
        //        CourierDeliveryInfo[] deiveryCover;
        //        bool[] orderCoverMap;
        //        //rc1 = BuildDeliveryCover(allDeliveries, shopOrders.Length, false, out deiveryCover, out orderCoverMap);
        //        rc1 = BuildDeliveryCoverEy(shopCouriers, allDeliveries, shopOrders, false, geoCache, out deiveryCover, out orderCoverMap);
        //        if (rc1 != 0)
        //            return rc = 100 * rc + rc1;

        //        // 11. Если ли в построенном покрытии не собранные заказы
        //        rc = 11;
        //        int receiptedCount = 0;

        //        for (int i = 0; i < orderCoverMap.Length; i++)
        //        {
        //            if (!orderCoverMap[i] && shopOrders[i].Status == OrderStatus.Receipted)
        //            {
        //                Order order = shopOrders[i];
        //                //Helper.WriteWarningToLog($"Receipted order can't delivery By Courier. CreateShopDeliveries. Shop {shop.Id}. Order {shopOrders[i].Id}, calcTime {calcTime}");
        //                Logger.WriteToLog(string.Format(MessagePatterns.REJECT_RECEIPTED_ORDER_BY_COURIER, order.Id, shop.Id, order.DeliveryTimeTo, calcTime));
        //                undelivOrders[undeliveredCount++] = order;
        //            }
        //            else if (shopOrders[i].Status == OrderStatus.Receipted)
        //            {
        //                receiptedCount++;
        //            }
        //        }

        //        // 12. Если в покрытии есть не собранные заказы
        //        rc = 12;
        //        if (receiptedCount > 0)
        //        {
        //            receiptedOrders = new CourierDeliveryInfo[receiptedCount];
        //            receiptedCount = 0;

        //            for (int i = 0; i < deiveryCover.Length; i++)
        //            {
        //                if (!deiveryCover[i].HasAssembledOnly)
        //                {
        //                    receiptedOrders[receiptedCount++] = deiveryCover[i];
        //                }
        //            }

        //            if (receiptedCount < receiptedOrders.Length)
        //            {
        //                Array.Resize(ref receiptedOrders, receiptedCount);
        //            }

        //            //rc1 = BuildDeliveryCoverEx(shopCouriers, allDeliveries, shopOrders.Length, true, geoCache, out deiveryCover, out orderCoverMap);
        //        }

        //        // 13. Отбираем заказы, которые не могут быть доставлены такси и относящиеся к ним отгрузки
        //        rc = 13;
        //        Order[] ordersByCourierOnly;
        //        CourierDeliveryInfo[] deliveriesByCourierOnly;

        //        rc1 = SelectCourierOrders(allDeliveries, shopOrders, true, out ordersByCourierOnly, out deliveriesByCourierOnly);
        //        if (rc1 != 0 || ordersByCourierOnly == null || ordersByCourierOnly.Length <= 0)
        //        {
        //            // 13.1 Если нет заказов, которые нужно доставлять только курьерами
        //            rc = 131;
        //            if (receiptedCount > 0)
        //            {
        //                BuildDeliveryCoverEy(shopCouriers, allDeliveries, shopOrders, true, geoCache, out deiveryCover, out orderCoverMap);
        //            }

        //            assembledOrders = deiveryCover;
        //        }
        //        else
        //        {
        //            // 13.2 Строим покрытие только для собранных заказов требующих отгрузки курьерами
        //            rc = 132;
        //            CourierDeliveryInfo[] deliveriesA = null;
        //            bool[] orderCoverMapA = null;
        //            CourierDeliveryInfo[] deliveriesB = null;
        //            bool[] orderCoverMapB = null;
        //            int rcA = BuildDeliveryCoverEy(shopCouriers, deliveriesByCourierOnly, ordersByCourierOnly, true, geoCache, out deliveriesA, out orderCoverMapA);
        //            int rcB = -1;

        //            // 13.3 Отбираем оставшиеся собранные заказы
        //            rc = 133;
        //            Order[] anotherAssembledOrders;
        //            rc1 = SelectOrdersWithoutOrders(shopOrders, ordersByCourierOnly, true, out anotherAssembledOrders);
        //            if (rc1 == 0 && anotherAssembledOrders != null && anotherAssembledOrders.Length > 0)
        //            {
        //                // 13.4 Отбираем отгрузки, которые не включают заказы, которые могут быть отгружены только курьерами
        //                rc = 134;
        //                CourierDeliveryInfo[] anotherDeliveries;
        //                rc1 = SelectDeliveriestWithoutOrders(allDeliveries, ordersByCourierOnly, out anotherDeliveries);
        //                if (rc1 == 0 && anotherDeliveries != null && anotherDeliveries.Length > 0)
        //                {
        //                    // 13.5 Отбираем доступных курьеров
        //                    rc = 135;
        //                    Courier[] unusedCouriers = null;
        //                    if (deliveriesA == null || deliveriesA.Length <= 0)
        //                    {
        //                        unusedCouriers = shopCouriers;
        //                    }
        //                    else
        //                    {
        //                        Courier[] usedCouriers = deliveriesA.Select(p => p.DeliveryCourier).ToArray();
        //                        SelectCouriersWithoutCouriers(shopCouriers, usedCouriers, out unusedCouriers);
        //                    }

        //                    // 13.6 Строим покрытие
        //                    rc = 136;
        //                    rcB = BuildDeliveryCoverEy(unusedCouriers, anotherDeliveries, anotherAssembledOrders, true, geoCache, out deliveriesB, out orderCoverMapB);
        //                }
        //            }

        //            // 13.7 Объединяем два покрытия
        //            rc = 137;
        //            int count = 0;
        //            if (rcA == 0)
        //                count += deliveriesA.Length;
        //            if (rcB == 0)
        //                count += deliveriesB.Length;

        //            assembledOrders = new CourierDeliveryInfo[count];
        //            count = 0;
        //            if (rcA == 0)
        //            {
        //                deliveriesA.CopyTo(assembledOrders, 0);
        //                count += deliveriesA.Length;
        //            }
        //            if (rcB == 0)
        //            {
        //                deliveriesB.CopyTo(assembledOrders, count);
        //                count += deliveriesB.Length;
        //            }

        //            // 13.8 Помечаем покрытые заказы
        //            rc = 138;
        //            Array.Clear(orderCoverMap, 0, orderCoverMap.Length);

        //            if (count > 0)
        //            {
        //                for (int i = 0; i < shopOrders.Length; i++)
        //                {
        //                    shopOrders[i].Index = i;
        //                }

        //                for (int i = 0; i < count; i++)
        //                {
        //                    foreach (Order order in assembledOrders[i].Orders)
        //                    {
        //                        orderCoverMap[order.Index] = true;
        //                    }
        //                }
        //            }
        //        }

        //        // 14. Формируем список заказов которые не могут быть доставлены в срок
        //        rc = 14;
        //        for (int i = 0; i < shopOrders.Length; i++)
        //        {
        //            if (!orderCoverMap[i] && shopOrders[i].Status == OrderStatus.Assembled)
        //            //if (!orderCoverMap[i])
        //            {
        //                Order order = shopOrders[i];
        //                //Helper.WriteWarningToLog($"Assembled order can't delivery By Courier. CreateShopDeliveries. Shop {shop.Id}. Order {shopOrders[i].Id}, calcTime {calcTime}");
        //                Logger.WriteToLog(string.Format(MessagePatterns.REJECT_ASSEMBLED_ORDER_BY_COURIER, order.Id, shop.Id, order.DeliveryTimeTo, calcTime));
        //                undelivOrders[undeliveredCount++] = order;
        //            }
        //        }

        //        if (undeliveredCount < undelivOrders.Length)
        //        {
        //            Array.Resize(ref undelivOrders, undeliveredCount);
        //        }

        //        undeliveredOrders = undelivOrders;

        //        // 15. Выход - Ok
        //        rc = 0;
        //        return rc;
        //    }
        //    catch (Exception ex)
        //    {
        //        //Helper.WriteErrorToLog($"CreateShopDeliveriesEx(Shop {shop.Id}, Orders {Helper.ArrayToString(allOrdersOfShop.Select(order => order.Id).ToArray())}, couriers { Helper.ArrayToString(shopCouriers.Select(courier => courier.Id).ToArray())})");
        //        //Helper.WriteErrorToLog($"(rc = {rc})");
        //        //Logger.WriteToLog(ex.ToString());

        //        Logger.WriteToLog(string.Format(MessagePatterns.METHOD_CALL, "CreateShopDeliveriesEx", $"Shop {shop.Id}, Orders {Helper.ArrayToString(allOrdersOfShop.Select(order => order.Id).ToArray())}, couriers { Helper.ArrayToString(shopCouriers.Select(courier => courier.Id).ToArray())}"));
        //        Logger.WriteToLog(string.Format(MessagePatterns.METHOD_RC, "CreateShopDeliveriesEx", rc));
        //        Logger.WriteToLog(string.Format(MessagePatterns.METHOD_FAIL, "CreateShopDeliveriesEx", ex.ToString()));

        //        return rc;
        //    }
        //}

        /// <summary>
        /// Выбор заказов,
        /// которые не могут быть доставлены такси
        /// </summary>
        /// <param name="allDeliveries">Все отгрузки</param>
        /// <param name="allOrders">Все заказы</param>
        /// <param name="assembledOnly">Флаг: true - отбирать только собранные заказы; false - отбирать поступившие и собранные</param>
        /// <param name="courierOrders">Заказы, которые не могут быть доставлены на такси</param>
        /// <param name="courierDeliveries">Отгрузки с отобранными заказами</param>
        /// <returns>0 - заказы и отгрузки отобраны; иначе - заказы и отгрузки не отобраны</returns>
        private static int SelectCourierOrders(CourierDeliveryInfo[] allDeliveries, Order[] allOrders, bool assembledOnly, out Order[] courierOrders, out CourierDeliveryInfo[] courierDeliveries)
        {
            // 1. Инициализация
            int rc = 1;
            courierDeliveries = null;
            courierOrders = null;
            //int[] saveOrderIndex = null;
            //bool isSaved = false;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (allDeliveries == null || allDeliveries.Length <= 0)
                    return rc;
                if (allOrders == null || allOrders.Length <= 0)
                    return rc;

                // 3. Помечаем заказы, которые могут быть отгружены такси
                rc = 3;
                int orderCount = allOrders.Length;
                bool[] taxiEnabled = new bool[orderCount];

                foreach (CourierDeliveryInfo delivery in allDeliveries)
                {
                    if (delivery.DeliveryCourier.IsTaxi)
                    {
                        foreach (Order order in delivery.Orders)
                        {
                            taxiEnabled[order.Index] = true;
                        }
                    }
                }

                // 4. Отбираем заказы, которые не могут быть отгружены такси
                rc = 4;
                courierOrders = new Order[orderCount];
                int count = 0;

                for (int i = 0; i < orderCount; i++)
                {
                    if (!taxiEnabled[i])
                    {
                        if (!assembledOnly || allOrders[i].Status == OrderStatus.Assembled)
                        {
                            courierOrders[count++] = allOrders[i];
                        }
                        else
                        {
                            taxiEnabled[i] = true;
                        }
                    }
                }

                if (count <= 0)
                {
                    courierDeliveries = new CourierDeliveryInfo[0];
                    courierOrders = new Order[0];
                    return rc = 0;
                }

                if (count < courierOrders.Length)
                {
                    Array.Resize(ref courierOrders, count);
                }

                // 5. Отбираем отгрузки курьерами содержащие отобранные заказы
                rc = 5;
                courierDeliveries = new CourierDeliveryInfo[allDeliveries.Length];
                count = 0;

                foreach (CourierDeliveryInfo delivery in allDeliveries)
                {
                    if (!delivery.DeliveryCourier.IsTaxi)
                    {
                        foreach (Order order in delivery.Orders)
                        {
                            if (!taxiEnabled[order.Index])
                            {
                                courierDeliveries[count++] = delivery;
                                break;
                            }
                        }
                    }
                }

                if (count < courierDeliveries.Length)
                {
                    Array.Resize(ref courierDeliveries, count);
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
        /// Выбор отгрузок, не содержащих данные заказы
        /// </summary>
        /// <param name="allDeliveries">Все отгрузки</param>
        /// <param name="orders">Заказы</param>
        /// <param name="deliveriesWithoutOrders">Отгрузки с отобранными заказами</param>
        /// <returns>0 - отгрузки отобраны; иначе - отгрузки не отобраны</returns>
        private static int SelectDeliveriestWithoutOrders(CourierDeliveryInfo[] allDeliveries, Order[] orders, out CourierDeliveryInfo[] deliveriesWithoutOrders)
        {
            // 1. Инициализация
            int rc = 1;
            deliveriesWithoutOrders = null;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (allDeliveries == null || allDeliveries.Length <= 0)
                    return rc;
                if (orders == null || orders.Length <= 0)
                    return rc;

                // 3. Метим исходные заказы
                rc = 3;
                int orderCount = orders.Length;

                for (int i = 0; i < orderCount; i++)
                {
                    orders[i].Index = -1;
                }

                // 4. Отбираем требуемые заказы
                rc = 4;
                CourierDeliveryInfo[] selectedDeliveries = new CourierDeliveryInfo[allDeliveries.Length];
                int count = 0;

                foreach (CourierDeliveryInfo delivery in allDeliveries)
                {
                    foreach (Order order in delivery.Orders)
                    {
                        if (order.Index == -1)
                            goto NextDelivery;
                    }

                    selectedDeliveries[count++] = delivery;

                    NextDelivery:
                    ;
                }

                if (count <= 0)
                {
                    deliveriesWithoutOrders = new CourierDeliveryInfo[0];
                    return rc;
                }

                if (count < selectedDeliveries.Length)
                {
                    Array.Resize(ref selectedDeliveries, count);
                }

                deliveriesWithoutOrders = selectedDeliveries;

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
        /// Выбор отгрузок, не содержащих данные заказы
        /// </summary>
        /// <param name="allOrders">Все заказы</param>
        /// <param name="minusOrders">Без заказов</param>
        /// <param name="assembledOnly">Флаг: true - отбирать только собранные заказы; false - отбирать поступившие и собранные</param>
        /// <param name="deliveriesWithoutOrders">Заказы без заказов</param>
        /// <returns>0 - заказы отобраны; иначе - заказы не отобраны</returns>
        private static int SelectOrdersWithoutOrders(Order[] allOrders, Order[] minusOrders, bool assembledOnly, out Order[] ordersWithoutOrders)
        {
            // 1. Инициализация
            int rc = 1;
            ordersWithoutOrders = null;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (allOrders == null || allOrders.Length <= 0)
                    return rc;

                if (minusOrders == null || minusOrders.Length <= 0)
                {
                    ordersWithoutOrders = allOrders;
                    return rc = 0;
                }

                // 3. Метим исходные заказы
                rc = 3;

                for (int i = 0; i < allOrders.Length; i++)
                {
                    allOrders[i].Index = i;
                }

                for (int i = 0; i < minusOrders.Length; i++)
                {
                    allOrders[i].Index = -1;
                }

                // 4. Отбираем требуемые заказы
                rc = 4;
                if (assembledOnly)
                {
                    ordersWithoutOrders = allOrders.Where(order => order.Index != -1 && order.Status == OrderStatus.Assembled).ToArray();
                }
                else
                {
                    ordersWithoutOrders = allOrders.Where(order => order.Index != -1).ToArray();
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
        /// Выбор курьеров, не содержащих данных курьеров
        /// (unusedCouriers = allCouriers - usedCouriers)
        /// </summary>
        /// <param name="allCouriers">Все курьеры</param>
        /// <param name="usedCouriers">Использованные курьеры</param>
        /// <param name="unusedCouriers">Отобранные, не использованные курьеры</param>
        /// <returns>0 - курьеры отобраны; иначе - курьеры не отобраны</returns>
        private static int SelectCouriersWithoutCouriers(Courier[] allCouriers, Courier[] usedCouriers, out Courier[] unusedCouriers)
        {
            // 1. Инициализация
            int rc = 1;
            unusedCouriers = null;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (allCouriers == null || allCouriers.Length <= 0)
                    return rc;

                if (usedCouriers == null || usedCouriers.Length <= 0)
                {
                    unusedCouriers = allCouriers;
                    return rc = 0;
                }

                // 3. Метим исходные курьеры
                rc = 3;

                for (int i = 0; i < allCouriers.Length; i++)
                {
                    allCouriers[i].Index = i;
                }

                for (int i = 0; i < usedCouriers.Length; i++)
                {
                    allCouriers[i].Index = -1;
                }

                // 4. Отбираем не использованных курьеров
                rc = 4;
                unusedCouriers = allCouriers.Where(courier => courier.Index != -1 || courier.IsTaxi).ToArray();

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
        /// Создание всех возможных отгрузок заказов магазина для заданного курьера
        /// </summary>
        /// <param name="shop">Магазин</param>
        /// <param name="shopOrders">Заданные заказы</param>
        /// <param name="shopCourier">Курьер</param>
        /// <param name="isLoop">Флаг возврата в магазин: true - требуется возврат в магазин; false - отгрузка завершается после вручения последнего заказа</param>
        /// <param name="calcTime">Момент расчетов</param>
        /// <param name="allPossibleDeliveries">Все возможные отгрузки состоящие не более, чем из 8 заказов</param>
        /// <returns>0 - отгрузки построены; иначе - отгрузки не построены</returns>
        private int CreateShopDeliveriesEx(Shop shop, Order[] shopOrders, Courier shopCourier, bool isLoop, DateTime calcTime, out CourierDeliveryInfo[] allPossibleDeliveries)
        {
            // 1. Инициализация
            int rc = 1;
            allPossibleDeliveries = null;

            try
            {
                // 2. Проверяем исходные 
                rc = 2;
                if (shop == null)
                    return rc;
                if (shopCourier == null)
                    return rc;
                if (shopOrders == null || shopOrders.Length <= 0)
                    return rc;

                // 3. Отбираем заказы, которые могут быть доставлены курьером
                rc = 3;
                Order[] courierOrders = new Order[shopOrders.Length];
                int orderCount = 0;
                //EnabledCourierType serviceFlags = shopCourier.ServiceFlags;
                CourierVehicleType shopCourierVehicleType = shopCourier.CourierType.VechicleType;

                for (int i = 0; i < shopOrders.Length; i++)
                {
                    Order order = shopOrders[i];

                    //if ((order.EnabledTypes & serviceFlags) != 0)
                    if (order.IsVehicleTypeEnabled(shopCourierVehicleType))
                    {
                        courierOrders[orderCount++] = order;
                    }
                }

                if (orderCount <= 0)
                    return rc = 0;

                if (orderCount < courierOrders.Length)
                {
                    Array.Resize(ref courierOrders, orderCount);
                }

                // 4. Строим отгрузки в зависимости от числа заказов
                rc = 4;

                if (orderCount <= orderLimitsForPathLength[8])
                {
                    return CreateShopDeliveries8(shop, courierOrders, shopCourier, isLoop, calcTime, out allPossibleDeliveries);
                }
                else if (orderCount <= orderLimitsForPathLength[7])
                {
                    return CreateShopDeliveries7(shop, courierOrders, shopCourier, isLoop, calcTime, out allPossibleDeliveries);
                }
                else if (orderCount <= orderLimitsForPathLength[6])
                {
                    return CreateShopDeliveries6(shop, courierOrders, shopCourier, isLoop, calcTime, out allPossibleDeliveries);
                }
                else if (orderCount <= orderLimitsForPathLength[5])
                {
                    return CreateShopDeliveries5(shop, courierOrders, shopCourier, isLoop, calcTime, out allPossibleDeliveries);
                }
                else if (orderCount <= orderLimitsForPathLength[4])
                {
                    return CreateShopDeliveries4(shop, courierOrders, shopCourier, isLoop, calcTime, out allPossibleDeliveries);
                }
                else if (orderCount <= orderLimitsForPathLength[3])
                {
                    return CreateShopDeliveries3(shop, courierOrders, shopCourier, isLoop, calcTime, out allPossibleDeliveries);
                }
                else
                {
                    return CreateShopDeliveries2(shop, courierOrders, shopCourier, isLoop, calcTime, out allPossibleDeliveries);
                }
            }
            catch (Exception ex)
            {
                Logger.WriteToLog(string.Format(MessagePatterns.METHOD_CALL, "CreateShopDeliveriesEx", $"Shop {shop.Id}, Orders {Helper.ArrayToString(shopOrders.Select(order => order.Id).ToArray())}, courier {shopCourier.Id}, isloop {isLoop}, calcTime {calcTime}"));
                Logger.WriteToLog(string.Format(MessagePatterns.METHOD_RC, "CreateShopDeliveriesEx", rc));
                Logger.WriteToLog(string.Format(MessagePatterns.METHOD_FAIL, "CreateShopDeliveriesEx", ex.ToString()));

                return rc;
            }
        }

        /// <summary>
        /// Создание всех возможных отгрузок заказов магазина для заданного курьера.
        /// При этом заказ, который не может быть доставлен вовремя может находиться
        /// только на первом месте в отгрузке
        /// </summary>
        /// <param name="shop">Магазин</param>
        /// <param name="behindTimeOrders">Заказы, которые не могут быть доставлены вовремя</param>
        /// <param name="onTimeOrders">Заказы, которые могут быть доставлены вовремя</param>
        /// <param name="shopCourier">Курьер</param>
        /// <param name="isLoop">Флаг возврата в магазин: true - требуется возврат в магазин; false - отгрузка завершается после вручения последнего заказа</param>
        /// <param name="calcTime">Момент расчетов</param>
        /// <param name="allPossibleDeliveries">Все возможные отгрузки состоящие не более, чем из 8 заказов</param>
        /// <returns>0 - отгрузки построены; иначе - отгрузки не построены</returns>
        private int CreateShopDeliveriesEx(Shop shop, Order[] behindTimeOrders, Order[] onTimeOrders, Courier shopCourier, bool isLoop, DateTime calcTime, out CourierDeliveryInfo[] allPossibleDeliveries)
        {
            // 1. Инициализация
            int rc = 1;
            allPossibleDeliveries = null;

            try
            {
                // 2. Проверяем исходные 
                rc = 2;
                if (shop == null)
                    return rc;
                if (shopCourier == null)
                    return rc;
                if (behindTimeOrders == null || behindTimeOrders.Length <= 0)
                    return rc;
                if (onTimeOrders == null || onTimeOrders.Length <= 0)
                    return rc;

                // 3. Отбираем заказы, которые могут быть доставлены курьером
                rc = 3;
                Order[] courierOrders = new Order[onTimeOrders.Length];
                int orderCount = 0;
                //EnabledCourierType serviceFlags = shopCourier.ServiceFlags;
                CourierVehicleType shopCourierVehicleType = shopCourier.CourierType.VechicleType;

                for (int i = 0; i < onTimeOrders.Length; i++)
                {
                    Order order = onTimeOrders[i];
                    //if ((order.EnabledTypes & serviceFlags) != 0)
                    if (order.IsVehicleTypeEnabled(shopCourierVehicleType))
                    {
                        courierOrders[orderCount++] = order;
                    }
                }

                if (orderCount <= 0)
                    return rc = 0;

                if (orderCount < courierOrders.Length)
                {
                    Array.Resize(ref courierOrders, orderCount);
                }

                // 4. Строим отгрузки в зависимомти от числа заказов
                rc = 4;

                if (orderCount <= orderLimitsForPathLength[8])
                {
                    return CreateShopDeliveries8_FirstFixed(shop, behindTimeOrders, courierOrders, shopCourier, isLoop, calcTime, out allPossibleDeliveries);
                }
                else if (orderCount <= orderLimitsForPathLength[7])
                {
                    return CreateShopDeliveries7_FirstFixed(shop, behindTimeOrders, courierOrders, shopCourier, isLoop, calcTime, out allPossibleDeliveries);
                }
                else if (orderCount <= orderLimitsForPathLength[6])
                {
                    return CreateShopDeliveries6_FirstFixed(shop, behindTimeOrders, courierOrders, shopCourier, isLoop, calcTime, out allPossibleDeliveries);
                }
                else if (orderCount <= orderLimitsForPathLength[5])
                {
                    return CreateShopDeliveries5_FirstFixed(shop, behindTimeOrders, courierOrders, shopCourier, isLoop, calcTime, out allPossibleDeliveries);
                }
                else if (orderCount <= orderLimitsForPathLength[4])
                {
                    return CreateShopDeliveries4_FirstFixed(shop, behindTimeOrders, courierOrders, shopCourier, isLoop, calcTime, out allPossibleDeliveries);
                }
                else if (orderCount <= orderLimitsForPathLength[3])
                {
                    return CreateShopDeliveries3_FirstFixed(shop, behindTimeOrders, courierOrders, shopCourier, isLoop, calcTime, out allPossibleDeliveries);
                }
                else
                {
                    return CreateShopDeliveries2_FirstFixed(shop, behindTimeOrders, courierOrders, shopCourier, isLoop, calcTime, out allPossibleDeliveries);
                }
            }
            catch (Exception ex)
            {
                Logger.WriteToLog(string.Format(MessagePatterns.METHOD_CALL, "CreateShopDeliveriesEx", $"Shop {shop.Id}, behindTimeOrders {Helper.ArrayToString(behindTimeOrders.Select(order => order.Id).ToArray())}, onTimeOrders {Helper.ArrayToString(onTimeOrders.Select(order => order.Id).ToArray())}, courier {shopCourier.Id}, isloop {isLoop}, calcTime {calcTime}"));
                Logger.WriteToLog(string.Format(MessagePatterns.METHOD_RC, "CreateShopDeliveriesEx", rc));
                Logger.WriteToLog(string.Format(MessagePatterns.METHOD_FAIL, "CreateShopDeliveriesEx", ex.ToString()));

                return rc;
            }
        }

        /// <summary>
        /// Создание отгрузок заказов магазина
        /// </summary>
        /// <param name="shop">Магазин</param>
        /// <param name="courierOrders">Отгружаемые заказы</param>
        /// <param name="shopCourier">Курьер</param>
        /// <param name="isLoop">Флаг возврата в магазин: true - требуется возврат в магазин; false - отгрузка завершается после вручения последнего заказа</param>
        /// <param name="calcTime">Момент расчетов</param>
        /// <param name="allPossibleDeliveries">Все возможные отгрузки состоящие не более, чем из 8 заказов</param>
        /// <returns>0 - отгрузки построены; иначе - отгрузки не построены</returns>
        private int CreateShopDeliveries8(Shop shop, Order[] courierOrders, Courier shopCourier, bool isLoop, DateTime calcTime, out CourierDeliveryInfo[] allPossibleDeliveries)
        {
            // 1. Инициализация
            int rc = 1;
            allPossibleDeliveries = null;

            try
            {
                // 2. Проверяем исходные 
                rc = 2;
                if (shop == null)
                    return rc;
                if (shopCourier == null)
                    return rc;
                if (courierOrders == null || courierOrders.Length <= 0)
                    return rc;

                // 3. Выбираем расстояния и времена движения между точками для заданного способа передвижения
                rc = 3;
                Point[,] locInfo;
                int rc1 = GetDistTimeTable(shop, courierOrders, shopCourier.CourierType.VechicleType, geoCache, out locInfo);
                if (rc1 != 0)
                    return rc = 10000 * rc + rc1;

                // 5. Выделяем память под все возможные отгрузки
                rc = 5;
                int orderCount = courierOrders.Length;
                int size;
                if (orderCount <= 12)
                {
                    size = (int)(Math.Pow(2, orderCount) + 0.5);
                }
                else
                {
                    size = 4096;
                }

                // 6. Извлекаем перестановки
                rc = 6;
                int[,] permutations1 = null;
                int[,] permutations2 = null;
                int[,] permutations3 = null;
                int[,] permutations4 = null;
                int[,] permutations5 = null;
                int[,] permutations6 = null;
                int[,] permutations7 = null;
                int[,] permutations8 = null;

                permutations1 = permutations.GetPermutations(1);
                if (orderCount >= 2)
                    permutations2 = permutations.GetPermutations(2);
                if (orderCount >= 3)
                    permutations3 = permutations.GetPermutations(3);
                if (orderCount >= 4)
                    permutations4 = permutations.GetPermutations(4);
                if (orderCount >= 5)
                    permutations5 = permutations.GetPermutations(5);
                if (orderCount >= 6)
                    permutations6 = permutations.GetPermutations(6);
                if (orderCount >= 7)
                    permutations7 = permutations.GetPermutations(7);
                if (orderCount >= 8)
                    permutations8 = permutations.GetPermutations(8);

                // 7. Цикл построения всех возможных отгрузок
                rc = 7;
                Order[] orders = new Order[8];
                CourierDeliveryInfo delivery;
                CourierDeliveryInfo[] allDeliveries = new CourierDeliveryInfo[size];
                int count = 0;
                int rcFind = 1;

                for (int i1 = 0; i1 < orderCount; i1++)
                {
                    orders[0] = courierOrders[i1];
                    rcFind = FindSalesmanProblemSolution(shop, orders, 1, shopCourier, isLoop, permutations1, locInfo, calcTime, out delivery);
                    if (rcFind != 0 || delivery == null)
                        continue;
                    if (count >= allDeliveries.Length)
                        Array.Resize(ref allDeliveries, allDeliveries.Length + 2048);
                    allDeliveries[count++] = delivery;

                    for (int i2 = i1 + 1; i2 < orderCount; i2++)
                    {
                        orders[1] = courierOrders[i2];
                        rcFind = FindSalesmanProblemSolution(shop, orders, 2, shopCourier, isLoop, permutations2, locInfo, calcTime, out delivery);
                        if (rcFind != 0 || delivery == null)
                            continue;
                        if (count >= allDeliveries.Length)
                            Array.Resize(ref allDeliveries, allDeliveries.Length + 2048);
                        allDeliveries[count++] = delivery;

                        for (int i3 = i2 + 1; i3 < orderCount; i3++)
                        {
                            orders[2] = courierOrders[i3];
                            rcFind = FindSalesmanProblemSolution(shop, orders, 3, shopCourier, isLoop, permutations3, locInfo, calcTime, out delivery);
                            if (rcFind != 0 || delivery == null)
                                continue;
                            if (count >= allDeliveries.Length)
                                Array.Resize(ref allDeliveries, allDeliveries.Length + 2048);
                            allDeliveries[count++] = delivery;

                            for (int i4 = i3 + 1; i4 < orderCount; i4++)
                            {
                                orders[3] = courierOrders[i4];
                                rcFind = FindSalesmanProblemSolution(shop, orders, 4, shopCourier, isLoop, permutations4, locInfo, calcTime, out delivery);
                                if (rcFind != 0 || delivery == null)
                                    continue;
                                if (count >= allDeliveries.Length)
                                    Array.Resize(ref allDeliveries, allDeliveries.Length + 2048);
                                allDeliveries[count++] = delivery;

                                for (int i5 = i4 + 1; i5 < orderCount; i5++)
                                {
                                    orders[4] = courierOrders[i5];
                                    rcFind = FindSalesmanProblemSolution(shop, orders, 5, shopCourier, isLoop, permutations5, locInfo, calcTime, out delivery);
                                    if (rcFind != 0 || delivery == null)
                                        continue;
                                    if (count >= allDeliveries.Length)
                                        Array.Resize(ref allDeliveries, allDeliveries.Length + 2048);
                                    allDeliveries[count++] = delivery;

                                    for (int i6 = i5 + 1; i6 < orderCount; i6++)
                                    {
                                        orders[5] = courierOrders[i6];
                                        rcFind = FindSalesmanProblemSolution(shop, orders, 6, shopCourier, isLoop, permutations6, locInfo, calcTime, out delivery);
                                        if (rcFind != 0 || delivery == null)
                                            continue;
                                        if (count >= allDeliveries.Length)
                                            Array.Resize(ref allDeliveries, allDeliveries.Length + 2048);
                                        allDeliveries[count++] = delivery;

                                        for (int i7 = i6 + 1; i7 < orderCount; i7++)
                                        {
                                            orders[6] = courierOrders[i7];
                                            rcFind = FindSalesmanProblemSolution(shop, orders, 7, shopCourier, isLoop, permutations7, locInfo, calcTime, out delivery);
                                            if (rcFind != 0 || delivery == null)
                                                continue;
                                            if (count >= allDeliveries.Length)
                                                Array.Resize(ref allDeliveries, allDeliveries.Length + 2048);
                                            allDeliveries[count++] = delivery;

                                            for (int i8 = i7 + 1; i8 < orderCount; i8++)
                                            {
                                                orders[7] = courierOrders[i8];
                                                rcFind = FindSalesmanProblemSolution(shop, orders, 8, shopCourier, isLoop, permutations8, locInfo, calcTime, out delivery);
                                                if (rcFind != 0 || delivery == null)
                                                    continue;
                                                if (count >= allDeliveries.Length)
                                                    Array.Resize(ref allDeliveries, allDeliveries.Length + 2048);
                                                allDeliveries[count++] = delivery;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                if (count < allDeliveries.Length)
                {
                    Array.Resize(ref allDeliveries, count);
                }

                allPossibleDeliveries = allDeliveries;

                // 8. Выход - Ok
                rc = 0;
                return rc;
            }
            catch (Exception ex)
            {
                //Helper.WriteErrorToLog($"CreateShopDeliveries8(Shop {shop.Id}, Orders {Helper.ArrayToString(courierOrders.Select(order => order.Id).ToArray())}, courier {shopCourier.Id}, isloop {isLoop}, calcTime {calcTime})");
                //Helper.WriteErrorToLog($"(rc = {rc})");
                //Logger.WriteToLog(ex.ToString());
                Logger.WriteToLog(string.Format(MessagePatterns.METHOD_CALL, "CreateShopDeliveries8", $"Shop {shop.Id}, Orders {Helper.ArrayToString(courierOrders.Select(order => order.Id).ToArray())}, courier {shopCourier.Id}, isloop {isLoop}, calcTime {calcTime}"));
                Logger.WriteToLog(string.Format(MessagePatterns.METHOD_RC, "CreateShopDeliveries8", rc));
                Logger.WriteToLog(string.Format(MessagePatterns.METHOD_FAIL, "CreateShopDeliveries8", ex.ToString()));

                return rc;
            }
        }

        /// <summary>
        /// Создание отгрузок заказов магазина
        /// </summary>
        /// <param name="shop">Магазин</param>
        /// <param name="courierOrders">Отгружаемые заказы</param>
        /// <param name="shopCourier">Курьер</param>
        /// <param name="isLoop">Флаг возврата в магазин: true - требуется возврат в магазин; false - отгрузка завершается после вручения последнего заказа</param>
        /// <param name="calcTime">Момент расчетов</param>
        /// <param name="allPossibleDeliveries">Все возможные отгрузки состоящие не более, чем из 7 заказов</param>
        /// <returns>0 - отгрузки построены; иначе - отгрузки не построены</returns>
        private int CreateShopDeliveries7(Shop shop, Order[] courierOrders, Courier shopCourier, bool isLoop, DateTime calcTime, out CourierDeliveryInfo[] allPossibleDeliveries)
        {
            // 1. Инициализация
            int rc = 1;
            allPossibleDeliveries = null;

            try
            {
                // 2. Проверяем исходные 
                rc = 2;
                if (shop == null)
                    return rc;
                if (shopCourier == null)
                    return rc;
                if (courierOrders == null || courierOrders.Length <= 0)
                    return rc;

                // 3. Выбираем расстояния и времена движения между точками для заданного способа передвижения
                rc = 3;
                Point[,] locInfo;
                int rc1 = GetDistTimeTable(shop, courierOrders, shopCourier.CourierType.VechicleType, geoCache, out locInfo);
                if (rc1 != 0)
                    return rc = 10000 * rc + rc1;

                // 5. Выделяем память под все возможные отгрузки
                rc = 5;
                int orderCount = courierOrders.Length;
                int size;
                if (orderCount <= 12)
                {
                    size = (int)(Math.Pow(2, orderCount) + 0.5);
                }
                else
                {
                    size = 4096;
                }

                // 6. Извлекаем перестановки
                rc = 6;
                int[,] permutations1 = null;
                int[,] permutations2 = null;
                int[,] permutations3 = null;
                int[,] permutations4 = null;
                int[,] permutations5 = null;
                int[,] permutations6 = null;
                int[,] permutations7 = null;

                permutations1 = permutations.GetPermutations(1);
                if (orderCount >= 2) permutations2 = permutations.GetPermutations(2);
                if (orderCount >= 3) permutations3 = permutations.GetPermutations(3);
                if (orderCount >= 4) permutations4 = permutations.GetPermutations(4);
                if (orderCount >= 5) permutations5 = permutations.GetPermutations(5);
                if (orderCount >= 6) permutations6 = permutations.GetPermutations(6);
                if (orderCount >= 7) permutations7 = permutations.GetPermutations(7);

                // 7. Цикл построения всех возможных отгрузок
                rc = 7;
                Order[] orders = new Order[8];
                CourierDeliveryInfo delivery;
                CourierDeliveryInfo[] allDeliveries = new CourierDeliveryInfo[size];
                int count = 0;
                int rcFind = 1;

                for (int i1 = 0; i1 < orderCount; i1++)
                {
                    orders[0] = courierOrders[i1];
                    rcFind = FindSalesmanProblemSolution(shop, orders, 1, shopCourier, isLoop, permutations1, locInfo, calcTime, out delivery);
                    if (rcFind != 0 || delivery == null)
                        continue;
                    if (count >= allDeliveries.Length)
                        Array.Resize(ref allDeliveries, allDeliveries.Length + 2048);
                    allDeliveries[count++] = delivery;

                    for (int i2 = i1 + 1; i2 < orderCount; i2++)
                    {
                        orders[1] = courierOrders[i2];
                        rcFind = FindSalesmanProblemSolution(shop, orders, 2, shopCourier, isLoop, permutations2, locInfo, calcTime, out delivery);
                        if (rcFind != 0 || delivery == null)
                            continue;
                        if (count >= allDeliveries.Length)
                            Array.Resize(ref allDeliveries, allDeliveries.Length + 2048);
                        allDeliveries[count++] = delivery;

                        for (int i3 = i2 + 1; i3 < orderCount; i3++)
                        {
                            orders[2] = courierOrders[i3];
                            rcFind = FindSalesmanProblemSolution(shop, orders, 3, shopCourier, isLoop, permutations3, locInfo, calcTime, out delivery);
                            if (rcFind != 0 || delivery == null)
                                continue;
                            if (count >= allDeliveries.Length)
                                Array.Resize(ref allDeliveries, allDeliveries.Length + 2048);
                            allDeliveries[count++] = delivery;

                            for (int i4 = i3 + 1; i4 < orderCount; i4++)
                            {
                                orders[3] = courierOrders[i4];
                                rcFind = FindSalesmanProblemSolution(shop, orders, 4, shopCourier, isLoop, permutations4, locInfo, calcTime, out delivery);
                                if (rcFind != 0 || delivery == null)
                                    continue;
                                if (count >= allDeliveries.Length)
                                    Array.Resize(ref allDeliveries, allDeliveries.Length + 2048);
                                allDeliveries[count++] = delivery;

                                for (int i5 = i4 + 1; i5 < orderCount; i5++)
                                {
                                    orders[4] = courierOrders[i5];
                                    rcFind = FindSalesmanProblemSolution(shop, orders, 5, shopCourier, isLoop, permutations5, locInfo, calcTime, out delivery);
                                    if (rcFind != 0 || delivery == null)
                                        continue;
                                    if (count >= allDeliveries.Length)
                                        Array.Resize(ref allDeliveries, allDeliveries.Length + 2048);
                                    allDeliveries[count++] = delivery;

                                    for (int i6 = i5 + 1; i6 < orderCount; i6++)
                                    {
                                        orders[5] = courierOrders[i6];
                                        rcFind = FindSalesmanProblemSolution(shop, orders, 6, shopCourier, isLoop, permutations6, locInfo, calcTime, out delivery);
                                        if (rcFind != 0 || delivery == null)
                                            continue;
                                        if (count >= allDeliveries.Length)
                                            Array.Resize(ref allDeliveries, allDeliveries.Length + 2048);
                                        allDeliveries[count++] = delivery;

                                        for (int i7 = i6 + 1; i7 < orderCount; i7++)
                                        {
                                            orders[6] = courierOrders[i7];
                                            rcFind = FindSalesmanProblemSolution(shop, orders, 7, shopCourier, isLoop, permutations7, locInfo, calcTime, out delivery);
                                            if (rcFind != 0 || delivery == null)
                                                continue;
                                            if (count >= allDeliveries.Length)
                                                Array.Resize(ref allDeliveries, allDeliveries.Length + 2048);
                                            allDeliveries[count++] = delivery;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                if (count < allDeliveries.Length)
                {
                    Array.Resize(ref allDeliveries, count);
                }

                allPossibleDeliveries = allDeliveries;

                // 8. Выход - Ok
                rc = 0;
                return rc;
            }
            catch (Exception ex)
            {
                //Helper.WriteErrorToLog($"CreateShopDeliveries7(Shop {shop.Id}, Orders {Helper.ArrayToString(courierOrders.Select(order => order.Id).ToArray())}, courier {shopCourier.Id}, isloop {isLoop}, calcTime {calcTime})");
                //Helper.WriteErrorToLog($"(rc = {rc})");
                //Logger.WriteToLog(ex.ToString());
                Logger.WriteToLog(string.Format(MessagePatterns.METHOD_CALL, "CreateShopDeliveries7", $"Shop {shop.Id}, Orders {Helper.ArrayToString(courierOrders.Select(order => order.Id).ToArray())}, courier {shopCourier.Id}, isloop {isLoop}, calcTime {calcTime}"));
                Logger.WriteToLog(string.Format(MessagePatterns.METHOD_RC, "CreateShopDeliveries7", rc));
                Logger.WriteToLog(string.Format(MessagePatterns.METHOD_FAIL, "CreateShopDeliveries7", ex.ToString()));

                return rc;
            }
        }

        /// <summary>
        /// Создание отгрузок заказов магазина
        /// </summary>
        /// <param name="shop">Магазин</param>
        /// <param name="courierOrders">Отгружаемые заказы</param>
        /// <param name="shopCourier">Курьер</param>
        /// <param name="isLoop">Флаг возврата в магазин: true - требуется возврат в магазин; false - отгрузка завершается после вручения последнего заказа</param>
        /// <param name="calcTime">Момент расчетов</param>
        /// <param name="allPossibleDeliveries">Все возможные отгрузки состоящие не более, чем из 6 заказов</param>
        /// <returns>0 - отгрузки построены; иначе - отгрузки не построены</returns>
        private int CreateShopDeliveries6(Shop shop, Order[] courierOrders, Courier shopCourier, bool isLoop, DateTime calcTime, out CourierDeliveryInfo[] allPossibleDeliveries)
        {
            // 1. Инициализация
            int rc = 1;
            allPossibleDeliveries = null;

            try
            {
                // 2. Проверяем исходные 
                rc = 2;
                if (shop == null)
                    return rc;
                if (shopCourier == null)
                    return rc;
                if (courierOrders == null || courierOrders.Length <= 0)
                    return rc;

                // 3. Выбираем расстояния и времена движения между точками для заданного способа передвижения
                rc = 3;
                Point[,] locInfo;
                int rc1 = GetDistTimeTable(shop, courierOrders, shopCourier.CourierType.VechicleType, geoCache, out locInfo);
                if (rc1 != 0)
                    return rc = 10000 * rc + rc1;

                // 5. Выделяем память под все возможные отгрузки
                rc = 5;
                int orderCount = courierOrders.Length;
                int size;
                if (orderCount <= 12)
                {
                    size = (int)(Math.Pow(2, orderCount) + 0.5);
                }
                else
                {
                    size = 4096;
                }

                // 6. Извлекаем перестановки
                rc = 6;
                int[,] permutations1 = null;
                int[,] permutations2 = null;
                int[,] permutations3 = null;
                int[,] permutations4 = null;
                int[,] permutations5 = null;
                int[,] permutations6 = null;

                permutations1 = permutations.GetPermutations(1);
                if (orderCount >= 2) permutations2 = permutations.GetPermutations(2);
                if (orderCount >= 3) permutations3 = permutations.GetPermutations(3);
                if (orderCount >= 4) permutations4 = permutations.GetPermutations(4);
                if (orderCount >= 5) permutations5 = permutations.GetPermutations(5);
                if (orderCount >= 6) permutations6 = permutations.GetPermutations(6);

                // 7. Цикл построения всех возможных отгрузок
                rc = 7;
                Order[] orders = new Order[8];
                CourierDeliveryInfo delivery;
                CourierDeliveryInfo[] allDeliveries = new CourierDeliveryInfo[size];
                int count = 0;
                int rcFind = 1;

                for (int i1 = 0; i1 < orderCount; i1++)
                {
                    orders[0] = courierOrders[i1];
                    rcFind = FindSalesmanProblemSolution(shop, orders, 1, shopCourier, isLoop, permutations1, locInfo, calcTime, out delivery);
                    if (rcFind != 0 || delivery == null)
                        continue;
                    if (count >= allDeliveries.Length)
                        Array.Resize(ref allDeliveries, allDeliveries.Length + 2048);
                    allDeliveries[count++] = delivery;

                    for (int i2 = i1 + 1; i2 < orderCount; i2++)
                    {
                        orders[1] = courierOrders[i2];
                        rcFind = FindSalesmanProblemSolution(shop, orders, 2, shopCourier, isLoop, permutations2, locInfo, calcTime, out delivery);
                        if (rcFind != 0 || delivery == null)
                            continue;
                        if (count >= allDeliveries.Length)
                            Array.Resize(ref allDeliveries, allDeliveries.Length + 2048);
                        allDeliveries[count++] = delivery;

                        for (int i3 = i2 + 1; i3 < orderCount; i3++)
                        {
                            orders[2] = courierOrders[i3];
                            rcFind = FindSalesmanProblemSolution(shop, orders, 3, shopCourier, isLoop, permutations3, locInfo, calcTime, out delivery);
                            if (rcFind != 0 || delivery == null)
                                continue;
                            if (count >= allDeliveries.Length)
                                Array.Resize(ref allDeliveries, allDeliveries.Length + 2048);
                            allDeliveries[count++] = delivery;

                            for (int i4 = i3 + 1; i4 < orderCount; i4++)
                            {
                                orders[3] = courierOrders[i4];
                                rcFind = FindSalesmanProblemSolution(shop, orders, 4, shopCourier, isLoop, permutations4, locInfo, calcTime, out delivery);
                                if (rcFind != 0 || delivery == null)
                                    continue;
                                if (count >= allDeliveries.Length)
                                    Array.Resize(ref allDeliveries, allDeliveries.Length + 2048);
                                allDeliveries[count++] = delivery;

                                for (int i5 = i4 + 1; i5 < orderCount; i5++)
                                {
                                    orders[4] = courierOrders[i5];
                                    rcFind = FindSalesmanProblemSolution(shop, orders, 5, shopCourier, isLoop, permutations5, locInfo, calcTime, out delivery);
                                    if (rcFind != 0 || delivery == null)
                                        continue;
                                    if (count >= allDeliveries.Length)
                                        Array.Resize(ref allDeliveries, allDeliveries.Length + 2048);
                                    allDeliveries[count++] = delivery;

                                    for (int i6 = i5 + 1; i6 < orderCount; i6++)
                                    {
                                        orders[5] = courierOrders[i6];
                                        rcFind = FindSalesmanProblemSolution(shop, orders, 6, shopCourier, isLoop, permutations6, locInfo, calcTime, out delivery);
                                        if (rcFind != 0 || delivery == null)
                                            continue;
                                        if (count >= allDeliveries.Length)
                                            Array.Resize(ref allDeliveries, allDeliveries.Length + 2048);
                                        allDeliveries[count++] = delivery;
                                    }
                                }
                            }
                        }
                    }
                }

                if (count < allDeliveries.Length)
                {
                    Array.Resize(ref allDeliveries, count);
                }

                allPossibleDeliveries = allDeliveries;

                // 8. Выход - Ok
                rc = 0;
                return rc;
            }
            catch (Exception ex)
            {
                //Helper.WriteErrorToLog($"CreateShopDeliveries6(Shop {shop.Id}, Orders {Helper.ArrayToString(courierOrders.Select(order => order.Id).ToArray())}, courier {shopCourier.Id}, isloop {isLoop}, calcTime {calcTime})");
                //Helper.WriteErrorToLog($"(rc = {rc})");
                //Logger.WriteToLog(ex.ToString());
                Logger.WriteToLog(string.Format(MessagePatterns.METHOD_CALL, "CreateShopDeliveries6", $"Shop {shop.Id}, Orders {Helper.ArrayToString(courierOrders.Select(order => order.Id).ToArray())}, courier {shopCourier.Id}, isloop {isLoop}, calcTime {calcTime}"));
                Logger.WriteToLog(string.Format(MessagePatterns.METHOD_RC, "CreateShopDeliveries6", rc));
                Logger.WriteToLog(string.Format(MessagePatterns.METHOD_FAIL, "CreateShopDeliveries6", ex.ToString()));

                return rc;
            }
        }

        /// <summary>
        /// Создание отгрузок заказов магазина
        /// </summary>
        /// <param name="shop">Магазин</param>
        /// <param name="courierOrders">Отгружаемые заказы</param>
        /// <param name="shopCourier">Курьер</param>
        /// <param name="isLoop">Флаг возврата в магазин: true - требуется возврат в магазин; false - отгрузка завершается после вручения последнего заказа</param>
        /// <param name="fixedFirst">Флаг: true - заказ с индексом 0 должен быть на первом месте отгрузке; false - заказы могут быть на любом месте в отгрузке</param>
        /// <param name="calcTime">Момент расчетов</param>
        /// <param name="allPossibleDeliveries">Все возможные отгрузки состоящие не более, чем из 5 заказов</param>
        /// <returns>0 - отгрузки построены; иначе - отгрузки не построены</returns>
        private int CreateShopDeliveries5(Shop shop, Order[] courierOrders, Courier shopCourier, bool isLoop, DateTime calcTime, out CourierDeliveryInfo[] allPossibleDeliveries)
        {
            // 1. Инициализация
            int rc = 1;
            allPossibleDeliveries = null;

            try
            {
                // 2. Проверяем исходные 
                rc = 2;
                if (shop == null)
                    return rc;
                if (shopCourier == null)
                    return rc;
                if (courierOrders == null || courierOrders.Length <= 0)
                    return rc;

                // 3. Выбираем расстояния и времена движения между точками для заданного способа передвижения
                rc = 3;
                Point[,] locInfo;
                int rc1 = GetDistTimeTable(shop, courierOrders, shopCourier.CourierType.VechicleType, geoCache, out locInfo);
                if (rc1 != 0)
                    return rc = 10000 * rc + rc1;

                // 5. Выделяем память под все возможные отгрузки
                rc = 5;
                int orderCount = courierOrders.Length;
                int size;
                if (orderCount <= 12)
                {
                    size = (int)(Math.Pow(2, orderCount) + 0.5);
                }
                else
                {
                    size = 4096;
                }

                // 6. Извлекаем перестановки
                rc = 6;
                int[,] permutations1 = null;
                int[,] permutations2 = null;
                int[,] permutations3 = null;
                int[,] permutations4 = null;
                int[,] permutations5 = null;

                permutations1 = permutations.GetPermutations(1);
                if (orderCount >= 2) permutations2 = permutations.GetPermutations(2);
                if (orderCount >= 3) permutations3 = permutations.GetPermutations(3);
                if (orderCount >= 4) permutations4 = permutations.GetPermutations(4);
                if (orderCount >= 5) permutations5 = permutations.GetPermutations(5);

                // 7. Цикл построения всех возможных отгрузок
                rc = 7;
                Order[] orders = new Order[8];
                CourierDeliveryInfo delivery;
                CourierDeliveryInfo[] allDeliveries = new CourierDeliveryInfo[size];
                int count = 0;
                int rcFind = 1;

                for (int i1 = 0; i1 < orderCount; i1++)
                {
                    orders[0] = courierOrders[i1];
                    rcFind = FindSalesmanProblemSolution(shop, orders, 1, shopCourier, isLoop, permutations1, locInfo, calcTime, out delivery);
                    if (rcFind != 0 || delivery == null)
                        continue;
                    if (count >= allDeliveries.Length)
                        Array.Resize(ref allDeliveries, allDeliveries.Length + 2048);
                    allDeliveries[count++] = delivery;

                    for (int i2 = i1 + 1; i2 < orderCount; i2++)
                    {
                        orders[1] = courierOrders[i2];
                        rcFind = FindSalesmanProblemSolution(shop, orders, 2, shopCourier, isLoop, permutations2, locInfo, calcTime, out delivery);
                        if (rcFind != 0 || delivery == null)
                            continue;
                        if (count >= allDeliveries.Length)
                            Array.Resize(ref allDeliveries, allDeliveries.Length + 2048);
                        allDeliveries[count++] = delivery;

                        for (int i3 = i2 + 1; i3 < orderCount; i3++)
                        {
                            orders[2] = courierOrders[i3];
                            rcFind = FindSalesmanProblemSolution(shop, orders, 3, shopCourier, isLoop, permutations3, locInfo, calcTime, out delivery);
                            if (rcFind != 0 || delivery == null)
                                continue;
                            if (count >= allDeliveries.Length)
                                Array.Resize(ref allDeliveries, allDeliveries.Length + 2048);
                            allDeliveries[count++] = delivery;

                            for (int i4 = i3 + 1; i4 < orderCount; i4++)
                            {
                                orders[3] = courierOrders[i4];
                                rcFind = FindSalesmanProblemSolution(shop, orders, 4, shopCourier, isLoop, permutations4, locInfo, calcTime, out delivery);
                                if (rcFind != 0 || delivery == null)
                                    continue;
                                if (count >= allDeliveries.Length)
                                    Array.Resize(ref allDeliveries, allDeliveries.Length + 2048);
                                allDeliveries[count++] = delivery;

                                for (int i5 = i4 + 1; i5 < orderCount; i5++)
                                {
                                    orders[4] = courierOrders[i5];
                                    rcFind = FindSalesmanProblemSolution(shop, orders, 5, shopCourier, isLoop, permutations5, locInfo, calcTime, out delivery);
                                    if (rcFind != 0 || delivery == null)
                                        continue;
                                    if (count >= allDeliveries.Length)
                                        Array.Resize(ref allDeliveries, allDeliveries.Length + 2048);
                                    allDeliveries[count++] = delivery;
                                }
                            }
                        }
                    }
                }

                if (count < allDeliveries.Length)
                {
                    Array.Resize(ref allDeliveries, count);
                }

                allPossibleDeliveries = allDeliveries;

                // 8. Выход - Ok
                rc = 0;
                return rc;
            }
            catch (Exception ex)
            {
                //Helper.WriteErrorToLog($"CreateShopDeliveries5(Shop {shop.Id}, Orders {Helper.ArrayToString(courierOrders.Select(order => order.Id).ToArray())}, courier {shopCourier.Id}, isloop {isLoop}, calcTime {calcTime})");
                //Helper.WriteErrorToLog($"(rc = {rc})");
                //Logger.WriteToLog(ex.ToString());
                Logger.WriteToLog(string.Format(MessagePatterns.METHOD_CALL, "CreateShopDeliveries5", $"Shop {shop.Id}, Orders {Helper.ArrayToString(courierOrders.Select(order => order.Id).ToArray())}, courier {shopCourier.Id}, isloop {isLoop}, calcTime {calcTime}"));
                Logger.WriteToLog(string.Format(MessagePatterns.METHOD_RC, "CreateShopDeliveries5", rc));
                Logger.WriteToLog(string.Format(MessagePatterns.METHOD_FAIL, "CreateShopDeliveries5", ex.ToString()));

                return rc;
            }
        }

        /// <summary>
        /// Создание отгрузок заказов магазина
        /// </summary>
        /// <param name="shop">Магазин</param>
        /// <param name="courierOrders">Отгружаемые заказы</param>
        /// <param name="shopCourier">Курьер</param>
        /// <param name="isLoop">Флаг возврата в магазин: true - требуется возврат в магазин; false - отгрузка завершается после вручения последнего заказа</param>
        /// <param name="calcTime">Момент расчетов</param>
        /// <param name="allPossibleDeliveries">Все возможные отгрузки состоящие не более, чем из 4 заказов</param>
        /// <returns>0 - отгрузки построены; иначе - отгрузки не построены</returns>
        private int CreateShopDeliveries4(Shop shop, Order[] courierOrders, Courier shopCourier, bool isLoop, DateTime calcTime, out CourierDeliveryInfo[] allPossibleDeliveries)
        {
            // 1. Инициализация
            int rc = 1;
            allPossibleDeliveries = null;

            try
            {
                // 2. Проверяем исходные 
                rc = 2;
                if (shop == null)
                    return rc;
                if (shopCourier == null)
                    return rc;
                if (courierOrders == null || courierOrders.Length <= 0)
                    return rc;

                // 3. Выбираем расстояния и времена движения между точками для заданного способа передвижения
                rc = 3;
                Point[,] locInfo;
                int rc1 = GetDistTimeTable(shop, courierOrders, shopCourier.CourierType.VechicleType, geoCache, out locInfo);
                if (rc1 != 0)
                    return rc = 10000 * rc + rc1;

                // 5. Выделяем память под все возможные отгрузки
                rc = 5;
                int orderCount = courierOrders.Length;
                int size;
                if (orderCount <= 12)
                {
                    size = (int)(Math.Pow(2, orderCount) + 0.5);
                }
                else
                {
                    size = 4096;
                }

                // 6. Извлекаем перестановки
                rc = 6;
                int[,] permutations1 = null;
                int[,] permutations2 = null;
                int[,] permutations3 = null;
                int[,] permutations4 = null;

                permutations1 = permutations.GetPermutations(1);
                if (orderCount >= 2) permutations2 = permutations.GetPermutations(2);
                if (orderCount >= 3) permutations3 = permutations.GetPermutations(3);
                if (orderCount >= 4) permutations4 = permutations.GetPermutations(4);

                // 7. Цикл построения всех возможных отгрузок
                rc = 7;
                Order[] orders = new Order[8];
                CourierDeliveryInfo delivery;
                CourierDeliveryInfo[] allDeliveries = new CourierDeliveryInfo[size];
                int count = 0;
                int rcFind = 1;

                for (int i1 = 0; i1 < orderCount; i1++)
                {
                    orders[0] = courierOrders[i1];
                    rcFind = FindSalesmanProblemSolution(shop, orders, 1, shopCourier, isLoop, permutations1, locInfo, calcTime, out delivery);
                    if (rcFind != 0 || delivery == null)
                        continue;
                    if (count >= allDeliveries.Length)
                        Array.Resize(ref allDeliveries, allDeliveries.Length + 2048);
                    allDeliveries[count++] = delivery;

                    for (int i2 = i1 + 1; i2 < orderCount; i2++)
                    {
                        orders[1] = courierOrders[i2];
                        rcFind = FindSalesmanProblemSolution(shop, orders, 2, shopCourier, isLoop, permutations2, locInfo, calcTime, out delivery);
                        if (rcFind != 0 || delivery == null)
                            continue;
                        if (count >= allDeliveries.Length)
                            Array.Resize(ref allDeliveries, allDeliveries.Length + 2048);
                        allDeliveries[count++] = delivery;

                        for (int i3 = i2 + 1; i3 < orderCount; i3++)
                        {
                            orders[2] = courierOrders[i3];
                            rcFind = FindSalesmanProblemSolution(shop, orders, 3, shopCourier, isLoop, permutations3, locInfo, calcTime, out delivery);
                            if (rcFind != 0 || delivery == null)
                                continue;
                            if (count >= allDeliveries.Length)
                                Array.Resize(ref allDeliveries, allDeliveries.Length + 2048);
                            allDeliveries[count++] = delivery;

                            for (int i4 = i3 + 1; i4 < orderCount; i4++)
                            {
                                orders[3] = courierOrders[i4];
                                rcFind = FindSalesmanProblemSolution(shop, orders, 4, shopCourier, isLoop, permutations4, locInfo, calcTime, out delivery);
                                if (rcFind != 0 || delivery == null)
                                    continue;
                                if (count >= allDeliveries.Length)
                                    Array.Resize(ref allDeliveries, allDeliveries.Length + 2048);
                                allDeliveries[count++] = delivery;
                            }
                        }
                    }
                }

                if (count < allDeliveries.Length)
                {
                    Array.Resize(ref allDeliveries, count);
                }

                allPossibleDeliveries = allDeliveries;

                // 8. Выход - Ok
                rc = 0;
                return rc;
            }
            catch (Exception ex)
            {
                //Helper.WriteErrorToLog($"CreateShopDeliveries4(Shop {shop.Id}, Orders {Helper.ArrayToString(courierOrders.Select(order => order.Id).ToArray())}, courier {shopCourier.Id}, isloop {isLoop}, calcTime {calcTime})");
                //Helper.WriteErrorToLog($"(rc = {rc})");
                //Logger.WriteToLog(ex.ToString());
                Logger.WriteToLog(string.Format(MessagePatterns.METHOD_CALL, "CreateShopDeliveries4", $"Shop {shop.Id}, Orders {Helper.ArrayToString(courierOrders.Select(order => order.Id).ToArray())}, courier {shopCourier.Id}, isloop {isLoop}, calcTime {calcTime}"));
                Logger.WriteToLog(string.Format(MessagePatterns.METHOD_RC, "CreateShopDeliveries4", rc));
                Logger.WriteToLog(string.Format(MessagePatterns.METHOD_FAIL, "CreateShopDeliveries4", ex.ToString()));

                return rc;
            }
        }

        /// <summary>
        /// Создание отгрузок заказов магазина
        /// </summary>
        /// <param name="shop">Магазин</param>
        /// <param name="courierOrders">Отгружаемые заказы</param>
        /// <param name="shopCourier">Курьер</param>
        /// <param name="isLoop">Флаг возврата в магазин: true - требуется возврат в магазин; false - отгрузка завершается после вручения последнего заказа</param>
        /// <param name="calcTime">Момент расчетов</param>
        /// <param name="allPossibleDeliveries">Все возможные отгрузки состоящие не более, чем из 4 заказов</param>
        /// <returns>0 - отгрузки построены; иначе - отгрузки не построены</returns>
        private int CreateShopDeliveries3(Shop shop, Order[] courierOrders, Courier shopCourier, bool isLoop, DateTime calcTime, out CourierDeliveryInfo[] allPossibleDeliveries)
        {
            // 1. Инициализация
            int rc = 1;
            allPossibleDeliveries = null;

            try
            {
                // 2. Проверяем исходные 
                rc = 2;
                if (shop == null)
                    return rc;
                if (shopCourier == null)
                    return rc;
                if (courierOrders == null || courierOrders.Length <= 0)
                    return rc;

                // 3. Выбираем расстояния и времена движения между точками для заданного способа передвижения
                rc = 3;
                Point[,] locInfo;
                int rc1 = GetDistTimeTable(shop, courierOrders, shopCourier.CourierType.VechicleType, geoCache, out locInfo);
                if (rc1 != 0)
                    return rc = 10000 * rc + rc1;

                // 5. Выделяем память под все возможные отгрузки
                rc = 5;
                int orderCount = courierOrders.Length;
                int size;
                if (orderCount <= 12)
                {
                    size = (int)(Math.Pow(2, orderCount) + 0.5);
                }
                else
                {
                    size = 4096;
                }

                // 6. Извлекаем перестановки
                rc = 6;
                int[,] permutations1 = null;
                int[,] permutations2 = null;
                int[,] permutations3 = null;

                permutations1 = permutations.GetPermutations(1);
                if (orderCount >= 2) permutations2 = permutations.GetPermutations(2);
                if (orderCount >= 3) permutations3 = permutations.GetPermutations(3);

                // 7. Цикл построения всех возможных отгрузок
                rc = 7;
                Order[] orders = new Order[8];
                CourierDeliveryInfo delivery;
                CourierDeliveryInfo[] allDeliveries = new CourierDeliveryInfo[size];
                int count = 0;
                int rcFind = 1;

                for (int i1 = 0; i1 < orderCount; i1++)
                {
                    orders[0] = courierOrders[i1];
                    rcFind = FindSalesmanProblemSolution(shop, orders, 1, shopCourier, isLoop, permutations1, locInfo, calcTime, out delivery);
                    if (rcFind != 0 || delivery == null)
                        continue;
                    if (count >= allDeliveries.Length)
                        Array.Resize(ref allDeliveries, allDeliveries.Length + 2048);
                    allDeliveries[count++] = delivery;

                    for (int i2 = i1 + 1; i2 < orderCount; i2++)
                    {
                        orders[1] = courierOrders[i2];
                        rcFind = FindSalesmanProblemSolution(shop, orders, 2, shopCourier, isLoop, permutations2, locInfo, calcTime, out delivery);
                        if (rcFind != 0 || delivery == null)
                            continue;
                        if (count >= allDeliveries.Length)
                            Array.Resize(ref allDeliveries, allDeliveries.Length + 2048);
                        allDeliveries[count++] = delivery;

                        for (int i3 = i2 + 1; i3 < orderCount; i3++)
                        {
                            orders[2] = courierOrders[i3];
                            rcFind = FindSalesmanProblemSolution(shop, orders, 3, shopCourier, isLoop, permutations3, locInfo, calcTime, out delivery);
                            if (rcFind != 0 || delivery == null)
                                continue;
                            if (count >= allDeliveries.Length)
                                Array.Resize(ref allDeliveries, allDeliveries.Length + 2048);
                            allDeliveries[count++] = delivery;
                        }
                    }
                }

                if (count < allDeliveries.Length)
                {
                    Array.Resize(ref allDeliveries, count);
                }

                allPossibleDeliveries = allDeliveries;

                // 8. Выход - Ok
                //rc = 0;
                return rc;
            }
            catch (Exception ex)
            {
                //Helper.WriteErrorToLog($"CreateShopDeliveries3(Shop {shop.Id}, Orders {Helper.ArrayToString(courierOrders.Select(order => order.Id).ToArray())}, courier {shopCourier.Id}, isloop {isLoop}, calcTime {calcTime})");
                //Helper.WriteErrorToLog($"(rc = {rc})");
                //Logger.WriteToLog(ex.ToString());
                Logger.WriteToLog(string.Format(MessagePatterns.METHOD_CALL, "CreateShopDeliveries3", $"Shop {shop.Id}, Orders {Helper.ArrayToString(courierOrders.Select(order => order.Id).ToArray())}, courier {shopCourier.Id}, isloop {isLoop}, calcTime {calcTime}"));
                Logger.WriteToLog(string.Format(MessagePatterns.METHOD_RC, "CreateShopDeliveries3", rc));
                Logger.WriteToLog(string.Format(MessagePatterns.METHOD_FAIL, "CreateShopDeliveries3", ex.ToString()));

                return rc;
            }
        }

        /// <summary>
        /// Создание отгрузок заказов магазина
        /// </summary>
        /// <param name="shop">Магазин</param>
        /// <param name="courierOrders">Отгружаемые заказы</param>
        /// <param name="shopCourier">Курьер</param>
        /// <param name="isLoop">Флаг возврата в магазин: true - требуется возврат в магазин; false - отгрузка завершается после вручения последнего заказа</param>
        /// <param name="calcTime">Момент расчетов</param>
        /// <param name="allPossibleDeliveries">Все возможные отгрузки состоящие не более, чем из 4 заказов</param>
        /// <returns>0 - отгрузки построены; иначе - отгрузки не построены</returns>
        private int CreateShopDeliveries2(Shop shop, Order[] courierOrders, Courier shopCourier, bool isLoop, DateTime calcTime, out CourierDeliveryInfo[] allPossibleDeliveries)
        {
            // 1. Инициализация
            int rc = 1;
            allPossibleDeliveries = null;

            try
            {
                // 2. Проверяем исходные 
                rc = 2;
                if (shop == null)
                    return rc;
                if (shopCourier == null)
                    return rc;
                if (courierOrders == null || courierOrders.Length <= 0)
                    return rc;

                // 3. Выбираем расстояния и времена движения между точками для заданного способа передвижения
                rc = 3;
                Point[,] locInfo;
                int rc1 = GetDistTimeTable(shop, courierOrders, shopCourier.CourierType.VechicleType, geoCache, out locInfo);
                if (rc1 != 0)
                    return rc = 10000 * rc + rc1;

                // 5. Выделяем память под все возможные отгрузки
                rc = 5;
                int orderCount = courierOrders.Length;
                int size;
                if (orderCount <= 12)
                {
                    size = (int)(Math.Pow(2, orderCount) + 0.5);
                }
                else
                {
                    size = 4096;
                }

                // 6. Извлекаем перестановки
                rc = 6;
                int[,] permutations1 = null;
                int[,] permutations2 = null;

                permutations1 = permutations.GetPermutations(1);
                if (orderCount >= 2) permutations2 = permutations.GetPermutations(2);

                // 7. Цикл построения всех возможных отгрузок
                rc = 7;
                Order[] orders = new Order[8];
                CourierDeliveryInfo delivery;
                CourierDeliveryInfo[] allDeliveries = new CourierDeliveryInfo[size];
                int count = 0;
                int rcFind = 1;

                for (int i1 = 0; i1 < orderCount; i1++)
                {
                    orders[0] = courierOrders[i1];
                    rcFind = FindSalesmanProblemSolution(shop, orders, 1, shopCourier, isLoop, permutations1, locInfo, calcTime, out delivery);
                    if (rcFind != 0 || delivery == null)
                        continue;
                    if (count >= allDeliveries.Length)
                        Array.Resize(ref allDeliveries, allDeliveries.Length + 2048);
                    allDeliveries[count++] = delivery;

                    for (int i2 = i1 + 1; i2 < orderCount; i2++)
                    {
                        orders[1] = courierOrders[i2];
                        rcFind = FindSalesmanProblemSolution(shop, orders, 2, shopCourier, isLoop, permutations2, locInfo, calcTime, out delivery);
                        if (rcFind != 0 || delivery == null)
                            continue;
                        if (count >= allDeliveries.Length)
                            Array.Resize(ref allDeliveries, allDeliveries.Length + 2048);
                        allDeliveries[count++] = delivery;
                    }
                }

                if (count < allDeliveries.Length)
                {
                    Array.Resize(ref allDeliveries, count);
                }

                allPossibleDeliveries = allDeliveries;

                // 8. Выход - Ok
                rc = 0;
                return rc;
            }
            catch (Exception ex)
            {
                //Helper.WriteErrorToLog($"CreateShopDeliveries2(Shop {shop.Id}, Orders {Helper.ArrayToString(courierOrders.Select(order => order.Id).ToArray())}, courier {shopCourier.Id}, isloop {isLoop}, calcTime {calcTime})");
                //Helper.WriteErrorToLog($"(rc = {rc})");
                //Logger.WriteToLog(ex.ToString());
                Logger.WriteToLog(string.Format(MessagePatterns.METHOD_CALL, "CreateShopDeliveries2", $"Shop {shop.Id}, Orders {Helper.ArrayToString(courierOrders.Select(order => order.Id).ToArray())}, courier {shopCourier.Id}, isloop {isLoop}, calcTime {calcTime}"));
                Logger.WriteToLog(string.Format(MessagePatterns.METHOD_RC, "CreateShopDeliveries2", rc));
                Logger.WriteToLog(string.Format(MessagePatterns.METHOD_FAIL, "CreateShopDeliveries2", ex.ToString()));

                return rc;
            }
        }

        /// <summary>
        /// Создание отгрузок заказов магазина
        /// </summary>
        /// <param name="shop">Магазин</param>
        /// <param name="firstFixedOrders">Заказы, которые должны быть на первом месте</param>
        /// <param name="onTimeOrders">Отгружаемые заказы</param>
        /// <param name="shopCourier">Курьер</param>
        /// <param name="isLoop">Флаг возврата в магазин: true - требуется возврат в магазин; false - отгрузка завершается после вручения последнего заказа</param>
        /// <param name="calcTime">Момент расчетов</param>
        /// <param name="allPossibleDeliveries">Все возможные отгрузки состоящие не более, чем из 8 заказов</param>
        /// <returns>0 - отгрузки построены; иначе - отгрузки не построены</returns>
        private int CreateShopDeliveries8_FirstFixed(Shop shop, Order[] firstFixedOrders, Order[] onTimeOrders, Courier shopCourier, bool isLoop, DateTime calcTime, out CourierDeliveryInfo[] allPossibleDeliveries)
        {
            // 1. Инициализация
            int rc = 1;
            allPossibleDeliveries = null;

            try
            {
                // 2. Проверяем исходные 
                rc = 2;
                if (shop == null)
                    return rc;
                if (shopCourier == null)
                    return rc;
                if (firstFixedOrders == null || firstFixedOrders.Length <= 0)
                    return rc;
                if (onTimeOrders == null || onTimeOrders.Length <= 0)
                    return rc;

                // 3. Выбираем расстояния и времена движения между точками для заданного способа передвижения
                rc = 3;
                Point[,] locInfo;
                int rc1 = GetDistTimeTable(shop, onTimeOrders.Concat(firstFixedOrders).ToArray(), shopCourier.CourierType.VechicleType, geoCache, out locInfo);
                if (rc1 != 0)
                    return rc = 10000 * rc + rc1;

                // 5. Выделяем память под все возможные отгрузки
                rc = 5;
                int orderCount = onTimeOrders.Length;
                int n = orderCount + 1;
                int size;
                if (n <= 12)
                {
                    size = (int)(Math.Pow(2, n) + 0.5);
                }
                else
                {
                    size = 4096;
                }

                // 6. Извлекаем перестановки
                rc = 6;
                int[,] permutations1 = null;
                int[,] permutations2 = null;
                int[,] permutations3 = null;
                int[,] permutations4 = null;
                int[,] permutations5 = null;
                int[,] permutations6 = null;
                int[,] permutations7 = null;
                int[,] permutations8 = null;

                permutations1 = permutations.GetPermutationsWithFirstFixed(1);
                if (n >= 2) permutations2 = permutations.GetPermutationsWithFirstFixed(2);
                if (n >= 3) permutations3 = permutations.GetPermutationsWithFirstFixed(3);
                if (n >= 4) permutations4 = permutations.GetPermutationsWithFirstFixed(4);
                if (n >= 5) permutations5 = permutations.GetPermutationsWithFirstFixed(5);
                if (n >= 6) permutations6 = permutations.GetPermutationsWithFirstFixed(6);
                if (n >= 7) permutations7 = permutations.GetPermutationsWithFirstFixed(7);
                if (n >= 8) permutations8 = permutations.GetPermutationsWithFirstFixed(8);

                // 7. Цикл построения всех возможных отгрузок
                rc = 7;
                Order[] orders = new Order[8];
                CourierDeliveryInfo delivery;
                CourierDeliveryInfo[] allDeliveries = new CourierDeliveryInfo[size];
                int count = 0;
                int rcFind = 1;
                int m = firstFixedOrders.Length;

                for (int i1 = 0; i1 < m; i1++)
                {
                    orders[0] = firstFixedOrders[i1];
                    rcFind = FindSalesmanProblemSolution(shop, orders, 1, shopCourier, isLoop, permutations1, locInfo, calcTime, out delivery);
                    if (rcFind != 0 || delivery == null)
                        continue;
                    if (count >= allDeliveries.Length)
                        Array.Resize(ref allDeliveries, allDeliveries.Length + 2048);
                    allDeliveries[count++] = delivery;

                    for (int i2 = 0; i2 < orderCount; i2++)
                    {
                        orders[1] = onTimeOrders[i2];
                        rcFind = FindSalesmanProblemSolution(shop, orders, 2, shopCourier, isLoop, permutations2, locInfo, calcTime, out delivery);
                        if (rcFind != 0 || delivery == null)
                            continue;
                        if (count >= allDeliveries.Length)
                            Array.Resize(ref allDeliveries, allDeliveries.Length + 2048);
                        allDeliveries[count++] = delivery;

                        for (int i3 = i2 + 1; i3 < orderCount; i3++)
                        {
                            orders[2] = onTimeOrders[i3];
                            rcFind = FindSalesmanProblemSolution(shop, orders, 3, shopCourier, isLoop, permutations3, locInfo, calcTime, out delivery);
                            if (rcFind != 0 || delivery == null)
                                continue;
                            if (count >= allDeliveries.Length)
                                Array.Resize(ref allDeliveries, allDeliveries.Length + 2048);
                            allDeliveries[count++] = delivery;

                            for (int i4 = i3 + 1; i4 < orderCount; i4++)
                            {
                                orders[3] = onTimeOrders[i4];
                                rcFind = FindSalesmanProblemSolution(shop, orders, 4, shopCourier, isLoop, permutations4, locInfo, calcTime, out delivery);
                                if (rcFind != 0 || delivery == null)
                                    continue;
                                if (count >= allDeliveries.Length)
                                    Array.Resize(ref allDeliveries, allDeliveries.Length + 2048);
                                allDeliveries[count++] = delivery;

                                for (int i5 = i4 + 1; i5 < orderCount; i5++)
                                {
                                    orders[4] = onTimeOrders[i5];
                                    rcFind = FindSalesmanProblemSolution(shop, orders, 5, shopCourier, isLoop, permutations5, locInfo, calcTime, out delivery);
                                    if (rcFind != 0 || delivery == null)
                                        continue;
                                    if (count >= allDeliveries.Length)
                                        Array.Resize(ref allDeliveries, allDeliveries.Length + 2048);
                                    allDeliveries[count++] = delivery;

                                    for (int i6 = i5 + 1; i6 < orderCount; i6++)
                                    {
                                        orders[5] = onTimeOrders[i6];
                                        rcFind = FindSalesmanProblemSolution(shop, orders, 6, shopCourier, isLoop, permutations6, locInfo, calcTime, out delivery);
                                        if (rcFind != 0 || delivery == null)
                                            continue;
                                        if (count >= allDeliveries.Length)
                                            Array.Resize(ref allDeliveries, allDeliveries.Length + 2048);
                                        allDeliveries[count++] = delivery;

                                        for (int i7 = i6 + 1; i7 < orderCount; i7++)
                                        {
                                            orders[6] = onTimeOrders[i7];
                                            rcFind = FindSalesmanProblemSolution(shop, orders, 7, shopCourier, isLoop, permutations7, locInfo, calcTime, out delivery);
                                            if (rcFind != 0 || delivery == null)
                                                continue;
                                            if (count >= allDeliveries.Length)
                                                Array.Resize(ref allDeliveries, allDeliveries.Length + 2048);
                                            allDeliveries[count++] = delivery;

                                            for (int i8 = i7 + 1; i8 < orderCount; i8++)
                                            {
                                                orders[7] = onTimeOrders[i8];
                                                rcFind = FindSalesmanProblemSolution(shop, orders, 8, shopCourier, isLoop, permutations8, locInfo, calcTime, out delivery);
                                                if (rcFind != 0 || delivery == null)
                                                    continue;
                                                if (count >= allDeliveries.Length)
                                                    Array.Resize(ref allDeliveries, allDeliveries.Length + 2048);
                                                allDeliveries[count++] = delivery;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                if (count < allDeliveries.Length)
                {
                    Array.Resize(ref allDeliveries, count);
                }

                allPossibleDeliveries = allDeliveries;

                // 8. Выход - Ok
                rc = 0;
                return rc;
            }
            catch (Exception ex)
            {
                Logger.WriteToLog(string.Format(MessagePatterns.METHOD_CALL, "CreateShopDeliveries8_FirstFixed", $"Shop {shop.Id}, Fixed {Helper.ArrayToString(firstFixedOrders.Select(order => order.Id).ToArray())}, Orders {Helper.ArrayToString(onTimeOrders.Select(order => order.Id).ToArray())}, courier {shopCourier.Id}, isloop {isLoop}, calcTime {calcTime}"));
                Logger.WriteToLog(string.Format(MessagePatterns.METHOD_RC, "CreateShopDeliveries8_FirstFixed", rc));
                Logger.WriteToLog(string.Format(MessagePatterns.METHOD_FAIL, "CreateShopDeliveries8_FirstFixed", ex.ToString()));

                return rc;
            }
        }

        /// <summary>
        /// Создание отгрузок заказов магазина
        /// </summary>
        /// <param name="shop">Магазин</param>
        /// <param name="firstFixedOrders">Заказы, которые должны быть на первом месте</param>
        /// <param name="onTimeOrders">Отгружаемые заказы</param>
        /// <param name="shopCourier">Курьер</param>
        /// <param name="isLoop">Флаг возврата в магазин: true - требуется возврат в магазин; false - отгрузка завершается после вручения последнего заказа</param>
        /// <param name="calcTime">Момент расчетов</param>
        /// <param name="allPossibleDeliveries">Все возможные отгрузки состоящие не более, чем из 8 заказов</param>
        /// <returns>0 - отгрузки построены; иначе - отгрузки не построены</returns>
        private int CreateShopDeliveries7_FirstFixed(Shop shop, Order[] firstFixedOrders, Order[] onTimeOrders, Courier shopCourier, bool isLoop, DateTime calcTime, out CourierDeliveryInfo[] allPossibleDeliveries)
        {
            // 1. Инициализация
            int rc = 1;
            allPossibleDeliveries = null;

            try
            {
                // 2. Проверяем исходные 
                rc = 2;
                if (shop == null)
                    return rc;
                if (shopCourier == null)
                    return rc;
                if (firstFixedOrders == null || firstFixedOrders.Length <= 0)
                    return rc;
                if (onTimeOrders == null || onTimeOrders.Length <= 0)
                    return rc;

                // 3. Выбираем расстояния и времена движения между точками для заданного способа передвижения
                rc = 3;
                Point[,] locInfo;
                int rc1 = GetDistTimeTable(shop, onTimeOrders.Concat(firstFixedOrders).ToArray(), shopCourier.CourierType.VechicleType, geoCache, out locInfo);
                if (rc1 != 0)
                    return rc = 10000 * rc + rc1;

                // 5. Выделяем память под все возможные отгрузки
                rc = 5;
                int orderCount = onTimeOrders.Length;
                int n = orderCount + 1;
                int size;
                if (n <= 12)
                {
                    size = (int)(Math.Pow(2, n) + 0.5);
                }
                else
                {
                    size = 4096;
                }

                // 6. Извлекаем перестановки
                rc = 6;
                int[,] permutations1 = null;
                int[,] permutations2 = null;
                int[,] permutations3 = null;
                int[,] permutations4 = null;
                int[,] permutations5 = null;
                int[,] permutations6 = null;
                int[,] permutations7 = null;

                permutations1 = permutations.GetPermutationsWithFirstFixed(1);
                if (n >= 2) permutations2 = permutations.GetPermutationsWithFirstFixed(2);
                if (n >= 3) permutations3 = permutations.GetPermutationsWithFirstFixed(3);
                if (n >= 4) permutations4 = permutations.GetPermutationsWithFirstFixed(4);
                if (n >= 5) permutations5 = permutations.GetPermutationsWithFirstFixed(5);
                if (n >= 6) permutations6 = permutations.GetPermutationsWithFirstFixed(6);
                if (n >= 7) permutations7 = permutations.GetPermutationsWithFirstFixed(7);

                // 7. Цикл построения всех возможных отгрузок
                rc = 7;
                Order[] orders = new Order[8];
                CourierDeliveryInfo delivery;
                CourierDeliveryInfo[] allDeliveries = new CourierDeliveryInfo[size];
                int count = 0;
                int rcFind = 1;
                int m = firstFixedOrders.Length;

                for (int i1 = 0; i1 < m; i1++)
                {
                    orders[0] = firstFixedOrders[i1];
                    rcFind = FindSalesmanProblemSolution(shop, orders, 1, shopCourier, isLoop, permutations1, locInfo, calcTime, out delivery);
                    if (rcFind != 0 || delivery == null)
                        continue;
                    if (count >= allDeliveries.Length)
                        Array.Resize(ref allDeliveries, allDeliveries.Length + 2048);
                    allDeliveries[count++] = delivery;

                    for (int i2 = 0; i2 < orderCount; i2++)
                    {
                        orders[1] = onTimeOrders[i2];
                        rcFind = FindSalesmanProblemSolution(shop, orders, 2, shopCourier, isLoop, permutations2, locInfo, calcTime, out delivery);
                        if (rcFind != 0 || delivery == null)
                            continue;
                        if (count >= allDeliveries.Length)
                            Array.Resize(ref allDeliveries, allDeliveries.Length + 2048);
                        allDeliveries[count++] = delivery;

                        for (int i3 = i2 + 1; i3 < orderCount; i3++)
                        {
                            orders[2] = onTimeOrders[i3];
                            rcFind = FindSalesmanProblemSolution(shop, orders, 3, shopCourier, isLoop, permutations3, locInfo, calcTime, out delivery);
                            if (rcFind != 0 || delivery == null)
                                continue;
                            if (count >= allDeliveries.Length)
                                Array.Resize(ref allDeliveries, allDeliveries.Length + 2048);
                            allDeliveries[count++] = delivery;

                            for (int i4 = i3 + 1; i4 < orderCount; i4++)
                            {
                                orders[3] = onTimeOrders[i4];
                                rcFind = FindSalesmanProblemSolution(shop, orders, 4, shopCourier, isLoop, permutations4, locInfo, calcTime, out delivery);
                                if (rcFind != 0 || delivery == null)
                                    continue;
                                if (count >= allDeliveries.Length)
                                    Array.Resize(ref allDeliveries, allDeliveries.Length + 2048);
                                allDeliveries[count++] = delivery;

                                for (int i5 = i4 + 1; i5 < orderCount; i5++)
                                {
                                    orders[4] = onTimeOrders[i5];
                                    rcFind = FindSalesmanProblemSolution(shop, orders, 5, shopCourier, isLoop, permutations5, locInfo, calcTime, out delivery);
                                    if (rcFind != 0 || delivery == null)
                                        continue;
                                    if (count >= allDeliveries.Length)
                                        Array.Resize(ref allDeliveries, allDeliveries.Length + 2048);
                                    allDeliveries[count++] = delivery;

                                    for (int i6 = i5 + 1; i6 < orderCount; i6++)
                                    {
                                        orders[5] = onTimeOrders[i6];
                                        rcFind = FindSalesmanProblemSolution(shop, orders, 6, shopCourier, isLoop, permutations6, locInfo, calcTime, out delivery);
                                        if (rcFind != 0 || delivery == null)
                                            continue;
                                        if (count >= allDeliveries.Length)
                                            Array.Resize(ref allDeliveries, allDeliveries.Length + 2048);
                                        allDeliveries[count++] = delivery;

                                        for (int i7 = i6 + 1; i7 < orderCount; i7++)
                                        {
                                            orders[6] = onTimeOrders[i7];
                                            rcFind = FindSalesmanProblemSolution(shop, orders, 7, shopCourier, isLoop, permutations7, locInfo, calcTime, out delivery);
                                            if (rcFind != 0 || delivery == null)
                                                continue;
                                            if (count >= allDeliveries.Length)
                                                Array.Resize(ref allDeliveries, allDeliveries.Length + 2048);
                                            allDeliveries[count++] = delivery;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                if (count < allDeliveries.Length)
                {
                    Array.Resize(ref allDeliveries, count);
                }

                allPossibleDeliveries = allDeliveries;

                // 8. Выход - Ok
                rc = 0;
                return rc;
            }
            catch (Exception ex)
            {
                Logger.WriteToLog(string.Format(MessagePatterns.METHOD_CALL, "CreateShopDeliveries6_FirstFixed", $"Shop {shop.Id}, Fixed {Helper.ArrayToString(firstFixedOrders.Select(order => order.Id).ToArray())}, Orders {Helper.ArrayToString(onTimeOrders.Select(order => order.Id).ToArray())}, courier {shopCourier.Id}, isloop {isLoop}, calcTime {calcTime}"));
                Logger.WriteToLog(string.Format(MessagePatterns.METHOD_RC, "CreateShopDeliveries6_FirstFixed", rc));
                Logger.WriteToLog(string.Format(MessagePatterns.METHOD_FAIL, "CreateShopDeliveries6_FirstFixed", ex.ToString()));

                return rc;
            }
        }

        /// <summary>
        /// Создание отгрузок заказов магазина
        /// </summary>
        /// <param name="shop">Магазин</param>
        /// <param name="firstFixedOrders">Заказы, которые должны быть на первом месте</param>
        /// <param name="onTimeOrders">Отгружаемые заказы</param>
        /// <param name="shopCourier">Курьер</param>
        /// <param name="isLoop">Флаг возврата в магазин: true - требуется возврат в магазин; false - отгрузка завершается после вручения последнего заказа</param>
        /// <param name="calcTime">Момент расчетов</param>
        /// <param name="allPossibleDeliveries">Все возможные отгрузки состоящие не более, чем из 8 заказов</param>
        /// <returns>0 - отгрузки построены; иначе - отгрузки не построены</returns>
        private int CreateShopDeliveries6_FirstFixed(Shop shop, Order[] firstFixedOrders, Order[] onTimeOrders, Courier shopCourier, bool isLoop, DateTime calcTime, out CourierDeliveryInfo[] allPossibleDeliveries)
        {
            // 1. Инициализация
            int rc = 1;
            allPossibleDeliveries = null;

            try
            {
                // 2. Проверяем исходные 
                rc = 2;
                if (shop == null)
                    return rc;
                if (shopCourier == null)
                    return rc;
                if (firstFixedOrders == null || firstFixedOrders.Length <= 0)
                    return rc;
                if (onTimeOrders == null || onTimeOrders.Length <= 0)
                    return rc;

                // 3. Выбираем расстояния и времена движения между точками для заданного способа передвижения
                rc = 3;
                Point[,] locInfo;
                int rc1 = GetDistTimeTable(shop, onTimeOrders.Concat(firstFixedOrders).ToArray(), shopCourier.CourierType.VechicleType, geoCache, out locInfo);
                if (rc1 != 0)
                    return rc = 10000 * rc + rc1;

                // 5. Выделяем память под все возможные отгрузки
                rc = 5;
                int orderCount = onTimeOrders.Length;
                int n = orderCount + 1;
                int size;
                if (n <= 12)
                {
                    size = (int)(Math.Pow(2, n) + 0.5);
                }
                else
                {
                    size = 4096;
                }

                // 6. Извлекаем перестановки
                rc = 6;
                int[,] permutations1 = null;
                int[,] permutations2 = null;
                int[,] permutations3 = null;
                int[,] permutations4 = null;
                int[,] permutations5 = null;
                int[,] permutations6 = null;

                permutations1 = permutations.GetPermutationsWithFirstFixed(1);
                if (n >= 2) permutations2 = permutations.GetPermutationsWithFirstFixed(2);
                if (n >= 3) permutations3 = permutations.GetPermutationsWithFirstFixed(3);
                if (n >= 4) permutations4 = permutations.GetPermutationsWithFirstFixed(4);
                if (n >= 5) permutations5 = permutations.GetPermutationsWithFirstFixed(5);
                if (n >= 6) permutations6 = permutations.GetPermutationsWithFirstFixed(6);

                // 7. Цикл построения всех возможных отгрузок
                rc = 7;
                Order[] orders = new Order[8];
                CourierDeliveryInfo delivery;
                CourierDeliveryInfo[] allDeliveries = new CourierDeliveryInfo[size];
                int count = 0;
                int rcFind = 1;
                int m = firstFixedOrders.Length;

                for (int i1 = 0; i1 < m; i1++)
                {
                    orders[0] = firstFixedOrders[i1];
                    rcFind = FindSalesmanProblemSolution(shop, orders, 1, shopCourier, isLoop, permutations1, locInfo, calcTime, out delivery);
                    if (rcFind != 0 || delivery == null)
                        continue;
                    if (count >= allDeliveries.Length)
                        Array.Resize(ref allDeliveries, allDeliveries.Length + 2048);
                    allDeliveries[count++] = delivery;

                    for (int i2 = 0; i2 < orderCount; i2++)
                    {
                        orders[1] = onTimeOrders[i2];
                        rcFind = FindSalesmanProblemSolution(shop, orders, 2, shopCourier, isLoop, permutations2, locInfo, calcTime, out delivery);
                        if (rcFind != 0 || delivery == null)
                            continue;
                        if (count >= allDeliveries.Length)
                            Array.Resize(ref allDeliveries, allDeliveries.Length + 2048);
                        allDeliveries[count++] = delivery;

                        for (int i3 = i2 + 1; i3 < orderCount; i3++)
                        {
                            orders[2] = onTimeOrders[i3];
                            rcFind = FindSalesmanProblemSolution(shop, orders, 3, shopCourier, isLoop, permutations3, locInfo, calcTime, out delivery);
                            if (rcFind != 0 || delivery == null)
                                continue;
                            if (count >= allDeliveries.Length)
                                Array.Resize(ref allDeliveries, allDeliveries.Length + 2048);
                            allDeliveries[count++] = delivery;

                            for (int i4 = i3 + 1; i4 < orderCount; i4++)
                            {
                                orders[3] = onTimeOrders[i4];
                                rcFind = FindSalesmanProblemSolution(shop, orders, 4, shopCourier, isLoop, permutations4, locInfo, calcTime, out delivery);
                                if (rcFind != 0 || delivery == null)
                                    continue;
                                if (count >= allDeliveries.Length)
                                    Array.Resize(ref allDeliveries, allDeliveries.Length + 2048);
                                allDeliveries[count++] = delivery;

                                for (int i5 = i4 + 1; i5 < orderCount; i5++)
                                {
                                    orders[4] = onTimeOrders[i5];
                                    rcFind = FindSalesmanProblemSolution(shop, orders, 5, shopCourier, isLoop, permutations5, locInfo, calcTime, out delivery);
                                    if (rcFind != 0 || delivery == null)
                                        continue;
                                    if (count >= allDeliveries.Length)
                                        Array.Resize(ref allDeliveries, allDeliveries.Length + 2048);
                                    allDeliveries[count++] = delivery;

                                    for (int i6 = i5 + 1; i6 < orderCount; i6++)
                                    {
                                        orders[5] = onTimeOrders[i6];
                                        rcFind = FindSalesmanProblemSolution(shop, orders, 6, shopCourier, isLoop, permutations6, locInfo, calcTime, out delivery);
                                        if (rcFind != 0 || delivery == null)
                                            continue;
                                        if (count >= allDeliveries.Length)
                                            Array.Resize(ref allDeliveries, allDeliveries.Length + 2048);
                                        allDeliveries[count++] = delivery;
                                    }
                                }
                            }
                        }
                    }
                }

                if (count < allDeliveries.Length)
                {
                    Array.Resize(ref allDeliveries, count);
                }

                allPossibleDeliveries = allDeliveries;

                // 8. Выход - Ok
                rc = 0;
                return rc;
            }
            catch (Exception ex)
            {
                Logger.WriteToLog(string.Format(MessagePatterns.METHOD_CALL, "CreateShopDeliveries6_FirstFixed", $"Shop {shop.Id}, Fixed {Helper.ArrayToString(firstFixedOrders.Select(order => order.Id).ToArray())}, Orders {Helper.ArrayToString(onTimeOrders.Select(order => order.Id).ToArray())}, courier {shopCourier.Id}, isloop {isLoop}, calcTime {calcTime}"));
                Logger.WriteToLog(string.Format(MessagePatterns.METHOD_RC, "CreateShopDeliveries6_FirstFixed", rc));
                Logger.WriteToLog(string.Format(MessagePatterns.METHOD_FAIL, "CreateShopDeliveries6_FirstFixed", ex.ToString()));

                return rc;
            }
        }

        /// <summary>
        /// Создание отгрузок заказов магазина
        /// </summary>
        /// <param name="shop">Магазин</param>
        /// <param name="firstFixedOrders">Заказы, которые должны быть на первом месте</param>
        /// <param name="onTimeOrders">Отгружаемые заказы</param>
        /// <param name="shopCourier">Курьер</param>
        /// <param name="isLoop">Флаг возврата в магазин: true - требуется возврат в магазин; false - отгрузка завершается после вручения последнего заказа</param>
        /// <param name="calcTime">Момент расчетов</param>
        /// <param name="allPossibleDeliveries">Все возможные отгрузки состоящие не более, чем из 8 заказов</param>
        /// <returns>0 - отгрузки построены; иначе - отгрузки не построены</returns>
        private int CreateShopDeliveries5_FirstFixed(Shop shop, Order[] firstFixedOrders, Order[] onTimeOrders, Courier shopCourier, bool isLoop, DateTime calcTime, out CourierDeliveryInfo[] allPossibleDeliveries)
        {
            // 1. Инициализация
            int rc = 1;
            allPossibleDeliveries = null;

            try
            {
                // 2. Проверяем исходные 
                rc = 2;
                if (shop == null)
                    return rc;
                if (shopCourier == null)
                    return rc;
                if (firstFixedOrders == null || firstFixedOrders.Length <= 0)
                    return rc;
                if (onTimeOrders == null || onTimeOrders.Length <= 0)
                    return rc;

                // 3. Выбираем расстояния и времена движения между точками для заданного способа передвижения
                rc = 3;
                Point[,] locInfo;
                int rc1 = GetDistTimeTable(shop, onTimeOrders.Concat(firstFixedOrders).ToArray(), shopCourier.CourierType.VechicleType, geoCache, out locInfo);
                if (rc1 != 0)
                    return rc = 10000 * rc + rc1;

                // 5. Выделяем память под все возможные отгрузки
                rc = 5;
                int orderCount = onTimeOrders.Length;
                int n = orderCount + 1;
                int size;
                if (n <= 12)
                {
                    size = (int)(Math.Pow(2, n) + 0.5);
                }
                else
                {
                    size = 4096;
                }

                // 6. Извлекаем перестановки
                rc = 6;
                int[,] permutations1 = null;
                int[,] permutations2 = null;
                int[,] permutations3 = null;
                int[,] permutations4 = null;
                int[,] permutations5 = null;

                permutations1 = permutations.GetPermutationsWithFirstFixed(1);
                if (n >= 2) permutations2 = permutations.GetPermutationsWithFirstFixed(2);
                if (n >= 3) permutations3 = permutations.GetPermutationsWithFirstFixed(3);
                if (n >= 4) permutations4 = permutations.GetPermutationsWithFirstFixed(4);
                if (n >= 5) permutations5 = permutations.GetPermutationsWithFirstFixed(5);

                // 7. Цикл построения всех возможных отгрузок
                rc = 7;
                Order[] orders = new Order[8];
                CourierDeliveryInfo delivery;
                CourierDeliveryInfo[] allDeliveries = new CourierDeliveryInfo[size];
                int count = 0;
                int rcFind = 1;
                int m = firstFixedOrders.Length;

                for (int i1 = 0; i1 < m; i1++)
                {
                    orders[0] = firstFixedOrders[i1];
                    rcFind = FindSalesmanProblemSolution(shop, orders, 1, shopCourier, isLoop, permutations1, locInfo, calcTime, out delivery);
                    if (rcFind != 0 || delivery == null)
                        continue;
                    if (count >= allDeliveries.Length)
                        Array.Resize(ref allDeliveries, allDeliveries.Length + 2048);
                    allDeliveries[count++] = delivery;

                    for (int i2 = 0; i2 < orderCount; i2++)
                    {
                        orders[1] = onTimeOrders[i2];
                        rcFind = FindSalesmanProblemSolution(shop, orders, 2, shopCourier, isLoop, permutations2, locInfo, calcTime, out delivery);
                        if (rcFind != 0 || delivery == null)
                            continue;
                        if (count >= allDeliveries.Length)
                            Array.Resize(ref allDeliveries, allDeliveries.Length + 2048);
                        allDeliveries[count++] = delivery;

                        for (int i3 = i2 + 1; i3 < orderCount; i3++)
                        {
                            orders[2] = onTimeOrders[i3];
                            rcFind = FindSalesmanProblemSolution(shop, orders, 3, shopCourier, isLoop, permutations3, locInfo, calcTime, out delivery);
                            if (rcFind != 0 || delivery == null)
                                continue;
                            if (count >= allDeliveries.Length)
                                Array.Resize(ref allDeliveries, allDeliveries.Length + 2048);
                            allDeliveries[count++] = delivery;

                            for (int i4 = i3 + 1; i4 < orderCount; i4++)
                            {
                                orders[3] = onTimeOrders[i4];
                                rcFind = FindSalesmanProblemSolution(shop, orders, 4, shopCourier, isLoop, permutations4, locInfo, calcTime, out delivery);
                                if (rcFind != 0 || delivery == null)
                                    continue;
                                if (count >= allDeliveries.Length)
                                    Array.Resize(ref allDeliveries, allDeliveries.Length + 2048);
                                allDeliveries[count++] = delivery;

                                for (int i5 = i4 + 1; i5 < orderCount; i5++)
                                {
                                    orders[4] = onTimeOrders[i5];
                                    rcFind = FindSalesmanProblemSolution(shop, orders, 5, shopCourier, isLoop, permutations5, locInfo, calcTime, out delivery);
                                    if (rcFind != 0 || delivery == null)
                                        continue;
                                    if (count >= allDeliveries.Length)
                                        Array.Resize(ref allDeliveries, allDeliveries.Length + 2048);
                                    allDeliveries[count++] = delivery;
                                }
                            }
                        }
                    }
                }

                if (count < allDeliveries.Length)
                {
                    Array.Resize(ref allDeliveries, count);
                }

                allPossibleDeliveries = allDeliveries;

                // 8. Выход - Ok
                rc = 0;
                return rc;
            }
            catch (Exception ex)
            {
                Logger.WriteToLog(string.Format(MessagePatterns.METHOD_CALL, "CreateShopDeliveries5_FirstFixed", $"Shop {shop.Id}, Fixed {Helper.ArrayToString(firstFixedOrders.Select(order => order.Id).ToArray())}, Orders {Helper.ArrayToString(onTimeOrders.Select(order => order.Id).ToArray())}, courier {shopCourier.Id}, isloop {isLoop}, calcTime {calcTime}"));
                Logger.WriteToLog(string.Format(MessagePatterns.METHOD_RC, "CreateShopDeliveries5_FirstFixed", rc));
                Logger.WriteToLog(string.Format(MessagePatterns.METHOD_FAIL, "CreateShopDeliveries5_FirstFixed", ex.ToString()));

                return rc;
            }
        }

        /// <summary>
        /// Создание отгрузок заказов магазина
        /// </summary>
        /// <param name="shop">Магазин</param>
        /// <param name="firstFixedOrders">Заказы, которые должны быть на первом месте</param>
        /// <param name="onTimeOrders">Отгружаемые заказы</param>
        /// <param name="shopCourier">Курьер</param>
        /// <param name="isLoop">Флаг возврата в магазин: true - требуется возврат в магазин; false - отгрузка завершается после вручения последнего заказа</param>
        /// <param name="calcTime">Момент расчетов</param>
        /// <param name="allPossibleDeliveries">Все возможные отгрузки состоящие не более, чем из 8 заказов</param>
        /// <returns>0 - отгрузки построены; иначе - отгрузки не построены</returns>
        private int CreateShopDeliveries4_FirstFixed(Shop shop, Order[] firstFixedOrders, Order[] onTimeOrders, Courier shopCourier, bool isLoop, DateTime calcTime, out CourierDeliveryInfo[] allPossibleDeliveries)
        {
            // 1. Инициализация
            int rc = 1;
            allPossibleDeliveries = null;

            try
            {
                // 2. Проверяем исходные 
                rc = 2;
                if (shop == null)
                    return rc;
                if (shopCourier == null)
                    return rc;
                if (firstFixedOrders == null || firstFixedOrders.Length <= 0)
                    return rc;
                if (onTimeOrders == null || onTimeOrders.Length <= 0)
                    return rc;

                // 3. Выбираем расстояния и времена движения между точками для заданного способа передвижения
                rc = 3;
                Point[,] locInfo;
                int rc1 = GetDistTimeTable(shop, onTimeOrders.Concat(firstFixedOrders).ToArray(), shopCourier.CourierType.VechicleType, geoCache, out locInfo);
                if (rc1 != 0)
                    return rc = 10000 * rc + rc1;

                // 5. Выделяем память под все возможные отгрузки
                rc = 5;
                int orderCount = onTimeOrders.Length;
                int n = orderCount + 1;
                int size;
                if (n <= 12)
                {
                    size = (int)(Math.Pow(2, n) + 0.5);
                }
                else
                {
                    size = 4096;
                }

                // 6. Извлекаем перестановки
                rc = 6;
                int[,] permutations1 = null;
                int[,] permutations2 = null;
                int[,] permutations3 = null;
                int[,] permutations4 = null;

                permutations1 = permutations.GetPermutationsWithFirstFixed(1);
                if (n >= 2) permutations2 = permutations.GetPermutationsWithFirstFixed(2);
                if (n >= 3) permutations3 = permutations.GetPermutationsWithFirstFixed(3);
                if (n >= 4) permutations4 = permutations.GetPermutationsWithFirstFixed(4);

                // 7. Цикл построения всех возможных отгрузок
                rc = 7;
                Order[] orders = new Order[8];
                CourierDeliveryInfo delivery;
                CourierDeliveryInfo[] allDeliveries = new CourierDeliveryInfo[size];
                int count = 0;
                int rcFind = 1;
                int m = firstFixedOrders.Length;

                for (int i1 = 0; i1 < m; i1++)
                {
                    orders[0] = firstFixedOrders[i1];
                    rcFind = FindSalesmanProblemSolution(shop, orders, 1, shopCourier, isLoop, permutations1, locInfo, calcTime, out delivery);
                    if (rcFind != 0 || delivery == null)
                        continue;
                    if (count >= allDeliveries.Length)
                        Array.Resize(ref allDeliveries, allDeliveries.Length + 2048);
                    allDeliveries[count++] = delivery;

                    for (int i2 = 0; i2 < orderCount; i2++)
                    {
                        orders[1] = onTimeOrders[i2];
                        rcFind = FindSalesmanProblemSolution(shop, orders, 2, shopCourier, isLoop, permutations2, locInfo, calcTime, out delivery);
                        if (rcFind != 0 || delivery == null)
                            continue;
                        if (count >= allDeliveries.Length)
                            Array.Resize(ref allDeliveries, allDeliveries.Length + 2048);
                        allDeliveries[count++] = delivery;

                        for (int i3 = i2 + 1; i3 < orderCount; i3++)
                        {
                            orders[2] = onTimeOrders[i3];
                            rcFind = FindSalesmanProblemSolution(shop, orders, 3, shopCourier, isLoop, permutations3, locInfo, calcTime, out delivery);
                            if (rcFind != 0 || delivery == null)
                                continue;
                            if (count >= allDeliveries.Length)
                                Array.Resize(ref allDeliveries, allDeliveries.Length + 2048);
                            allDeliveries[count++] = delivery;

                            for (int i4 = i3 + 1; i4 < orderCount; i4++)
                            {
                                orders[3] = onTimeOrders[i4];
                                rcFind = FindSalesmanProblemSolution(shop, orders, 4, shopCourier, isLoop, permutations4, locInfo, calcTime, out delivery);
                                if (rcFind != 0 || delivery == null)
                                    continue;
                                if (count >= allDeliveries.Length)
                                    Array.Resize(ref allDeliveries, allDeliveries.Length + 2048);
                                allDeliveries[count++] = delivery;
                            }
                        }
                    }
                }

                if (count < allDeliveries.Length)
                {
                    Array.Resize(ref allDeliveries, count);
                }

                allPossibleDeliveries = allDeliveries;

                // 8. Выход - Ok
                rc = 0;
                return rc;
            }
            catch (Exception ex)
            {
                Logger.WriteToLog(string.Format(MessagePatterns.METHOD_CALL, "CreateShopDeliveries4_FirstFixed", $"Shop {shop.Id}, Fixed {Helper.ArrayToString(firstFixedOrders.Select(order => order.Id).ToArray())}, Orders {Helper.ArrayToString(onTimeOrders.Select(order => order.Id).ToArray())}, courier {shopCourier.Id}, isloop {isLoop}, calcTime {calcTime}"));
                Logger.WriteToLog(string.Format(MessagePatterns.METHOD_RC, "CreateShopDeliveries4_FirstFixed", rc));
                Logger.WriteToLog(string.Format(MessagePatterns.METHOD_FAIL, "CreateShopDeliveries4_FirstFixed", ex.ToString()));

                return rc;
            }
        }

        /// <summary>
        /// Создание отгрузок заказов магазина
        /// </summary>
        /// <param name="shop">Магазин</param>
        /// <param name="firstFixedOrders">Заказы, которые должны быть на первом месте</param>
        /// <param name="onTimeOrders">Отгружаемые заказы</param>
        /// <param name="shopCourier">Курьер</param>
        /// <param name="isLoop">Флаг возврата в магазин: true - требуется возврат в магазин; false - отгрузка завершается после вручения последнего заказа</param>
        /// <param name="calcTime">Момент расчетов</param>
        /// <param name="allPossibleDeliveries">Все возможные отгрузки состоящие не более, чем из 8 заказов</param>
        /// <returns>0 - отгрузки построены; иначе - отгрузки не построены</returns>
        private int CreateShopDeliveries3_FirstFixed(Shop shop, Order[] firstFixedOrders, Order[] onTimeOrders, Courier shopCourier, bool isLoop, DateTime calcTime, out CourierDeliveryInfo[] allPossibleDeliveries)
        {
            // 1. Инициализация
            int rc = 1;
            allPossibleDeliveries = null;

            try
            {
                // 2. Проверяем исходные 
                rc = 2;
                if (shop == null)
                    return rc;
                if (shopCourier == null)
                    return rc;
                if (firstFixedOrders == null || firstFixedOrders.Length <= 0)
                    return rc;
                if (onTimeOrders == null || onTimeOrders.Length <= 0)
                    return rc;

                // 3. Выбираем расстояния и времена движения между точками для заданного способа передвижения
                rc = 3;
                Point[,] locInfo;
                int rc1 = GetDistTimeTable(shop, onTimeOrders.Concat(firstFixedOrders).ToArray(), shopCourier.CourierType.VechicleType, geoCache, out locInfo);
                if (rc1 != 0)
                    return rc = 10000 * rc + rc1;

                // 5. Выделяем память под все возможные отгрузки
                rc = 5;
                int orderCount = onTimeOrders.Length;
                int n = orderCount + 1;
                int size;
                if (n <= 12)
                {
                    size = (int)(Math.Pow(2, n) + 0.5);
                }
                else
                {
                    size = 4096;
                }

                // 6. Извлекаем перестановки
                rc = 6;
                int[,] permutations1 = null;
                int[,] permutations2 = null;
                int[,] permutations3 = null;

                permutations1 = permutations.GetPermutationsWithFirstFixed(1);
                if (n >= 2) permutations2 = permutations.GetPermutationsWithFirstFixed(2);
                if (n >= 3) permutations3 = permutations.GetPermutationsWithFirstFixed(3);

                // 7. Цикл построения всех возможных отгрузок
                rc = 7;
                Order[] orders = new Order[8];
                CourierDeliveryInfo delivery;
                CourierDeliveryInfo[] allDeliveries = new CourierDeliveryInfo[size];
                int count = 0;
                int rcFind = 1;
                int m = firstFixedOrders.Length;

                for (int i1 = 0; i1 < m; i1++)
                {
                    orders[0] = firstFixedOrders[i1];
                    rcFind = FindSalesmanProblemSolution(shop, orders, 1, shopCourier, isLoop, permutations1, locInfo, calcTime, out delivery);
                    if (rcFind != 0 || delivery == null)
                        continue;
                    if (count >= allDeliveries.Length)
                        Array.Resize(ref allDeliveries, allDeliveries.Length + 2048);
                    allDeliveries[count++] = delivery;

                    for (int i2 = 0; i2 < orderCount; i2++)
                    {
                        orders[1] = onTimeOrders[i2];
                        rcFind = FindSalesmanProblemSolution(shop, orders, 2, shopCourier, isLoop, permutations2, locInfo, calcTime, out delivery);
                        if (rcFind != 0 || delivery == null)
                            continue;
                        if (count >= allDeliveries.Length)
                            Array.Resize(ref allDeliveries, allDeliveries.Length + 2048);
                        allDeliveries[count++] = delivery;

                        for (int i3 = i2 + 1; i3 < orderCount; i3++)
                        {
                            orders[2] = onTimeOrders[i3];
                            rcFind = FindSalesmanProblemSolution(shop, orders, 3, shopCourier, isLoop, permutations3, locInfo, calcTime, out delivery);
                            if (rcFind != 0 || delivery == null)
                                continue;
                            if (count >= allDeliveries.Length)
                                Array.Resize(ref allDeliveries, allDeliveries.Length + 2048);
                            allDeliveries[count++] = delivery;
                        }
                    }
                }

                if (count < allDeliveries.Length)
                {
                    Array.Resize(ref allDeliveries, count);
                }

                allPossibleDeliveries = allDeliveries;

                // 8. Выход - Ok
                rc = 0;
                return rc;
            }
            catch (Exception ex)
            {
                Logger.WriteToLog(string.Format(MessagePatterns.METHOD_CALL, "CreateShopDeliveries3_FirstFixed", $"Shop {shop.Id}, Fixed {Helper.ArrayToString(firstFixedOrders.Select(order => order.Id).ToArray())}, Orders {Helper.ArrayToString(onTimeOrders.Select(order => order.Id).ToArray())}, courier {shopCourier.Id}, isloop {isLoop}, calcTime {calcTime}"));
                Logger.WriteToLog(string.Format(MessagePatterns.METHOD_RC, "CreateShopDeliveries3_FirstFixed", rc));
                Logger.WriteToLog(string.Format(MessagePatterns.METHOD_FAIL, "CreateShopDeliveries3_FirstFixed", ex.ToString()));

                return rc;
            }
        }

        /// <summary>
        /// Создание отгрузок заказов магазина
        /// </summary>
        /// <param name="shop">Магазин</param>
        /// <param name="firstFixedOrders">Заказы, которые должны быть на первом месте</param>
        /// <param name="onTimeOrders">Отгружаемые заказы</param>
        /// <param name="shopCourier">Курьер</param>
        /// <param name="isLoop">Флаг возврата в магазин: true - требуется возврат в магазин; false - отгрузка завершается после вручения последнего заказа</param>
        /// <param name="calcTime">Момент расчетов</param>
        /// <param name="allPossibleDeliveries">Все возможные отгрузки состоящие не более, чем из 8 заказов</param>
        /// <returns>0 - отгрузки построены; иначе - отгрузки не построены</returns>
        private int CreateShopDeliveries2_FirstFixed(Shop shop, Order[] firstFixedOrders, Order[] onTimeOrders, Courier shopCourier, bool isLoop, DateTime calcTime, out CourierDeliveryInfo[] allPossibleDeliveries)
        {
            // 1. Инициализация
            int rc = 1;
            allPossibleDeliveries = null;

            try
            {
                // 2. Проверяем исходные 
                rc = 2;
                if (shop == null)
                    return rc;
                if (shopCourier == null)
                    return rc;
                if (firstFixedOrders == null || firstFixedOrders.Length <= 0)
                    return rc;
                if (onTimeOrders == null || onTimeOrders.Length <= 0)
                    return rc;

                // 3. Выбираем расстояния и времена движения между точками для заданного способа передвижения
                rc = 3;
                Point[,] locInfo;
                int rc1 = GetDistTimeTable(shop, onTimeOrders.Concat(firstFixedOrders).ToArray(), shopCourier.CourierType.VechicleType, geoCache, out locInfo);
                if (rc1 != 0)
                    return rc = 10000 * rc + rc1;

                // 5. Выделяем память под все возможные отгрузки
                rc = 5;
                int orderCount = onTimeOrders.Length;
                int n = orderCount + 1;
                int size;
                if (n <= 12)
                {
                    size = (int)(Math.Pow(2, n) + 0.5);
                }
                else
                {
                    size = 4096;
                }

                // 6. Извлекаем перестановки
                rc = 6;
                int[,] permutations1 = null;
                int[,] permutations2 = null;

                permutations1 = permutations.GetPermutationsWithFirstFixed(1);
                if (n >= 2) permutations2 = permutations.GetPermutationsWithFirstFixed(2);

                // 7. Цикл построения всех возможных отгрузок
                rc = 7;
                Order[] orders = new Order[8];
                CourierDeliveryInfo delivery;
                CourierDeliveryInfo[] allDeliveries = new CourierDeliveryInfo[size];
                int count = 0;
                int rcFind = 1;
                int m = firstFixedOrders.Length;

                for (int i1 = 0; i1 < m; i1++)
                {
                    orders[0] = firstFixedOrders[i1];
                    rcFind = FindSalesmanProblemSolution(shop, orders, 1, shopCourier, isLoop, permutations1, locInfo, calcTime, out delivery);
                    if (rcFind != 0 || delivery == null)
                        continue;
                    if (count >= allDeliveries.Length)
                        Array.Resize(ref allDeliveries, allDeliveries.Length + 2048);
                    allDeliveries[count++] = delivery;

                    for (int i2 = 0; i2 < orderCount; i2++)
                    {
                        orders[1] = onTimeOrders[i2];
                        rcFind = FindSalesmanProblemSolution(shop, orders, 2, shopCourier, isLoop, permutations2, locInfo, calcTime, out delivery);
                        if (rcFind != 0 || delivery == null)
                            continue;
                        if (count >= allDeliveries.Length)
                            Array.Resize(ref allDeliveries, allDeliveries.Length + 2048);
                        allDeliveries[count++] = delivery;
                    }
                }

                if (count < allDeliveries.Length)
                {
                    Array.Resize(ref allDeliveries, count);
                }

                allPossibleDeliveries = allDeliveries;

                // 8. Выход - Ok
                rc = 0;
                return rc;
            }
            catch (Exception ex)
            {
                Logger.WriteToLog(string.Format(MessagePatterns.METHOD_CALL, "CreateShopDeliveries2_FirstFixed", $"Shop {shop.Id}, Fixed {Helper.ArrayToString(firstFixedOrders.Select(order => order.Id).ToArray())}, Orders {Helper.ArrayToString(onTimeOrders.Select(order => order.Id).ToArray())}, courier {shopCourier.Id}, isloop {isLoop}, calcTime {calcTime}"));
                Logger.WriteToLog(string.Format(MessagePatterns.METHOD_RC, "CreateShopDeliveries2_FirstFixed", rc));
                Logger.WriteToLog(string.Format(MessagePatterns.METHOD_FAIL, "CreateShopDeliveries2_FirstFixed", ex.ToString()));

                return rc;
            }
        }

        /// <summary>
        /// Поиск пути с минимальной стоимостью среди заданных перестановок
        /// </summary>
        /// <param name="shop">Магазин, из которого осуществляется отгрузка</param>
        /// <param name="shopOrders">Отгружаемые заказы</param>
        /// <param name="orderCount">Количество заказов</param>
        /// <param name="courier">Курьер</param>
        /// <param name="isLoop">Флаг возврата в магазин: true - требуется возврат в магазин; false - отгрузка завершается после вручения последнего заказа</param>
        /// <param name="permutations">Перестановки, среди которых осуществляется поиск</param>
        /// <param name="courierLocationInfo">Расстояния и время движения между точками</param>
        /// <param name="calcTime">Время, начиная с которого можно использовать курьера</param>
        /// <param name="bestDelivery">Путь с минимальной стоимостью</param>
        /// <returns>0 - путь найден; иначе - путь не найден</returns>
        private static int FindSalesmanProblemSolution(Shop shop, Order[] shopOrders, int orderCount, Courier courier, bool isLoop, int[,] permutations, Point[,] courierLocationInfo, DateTime calcTime, out CourierDeliveryInfo bestDelivery)
        {
            // 1. Инициализация
            int rc = 1;
            bestDelivery = null;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (shop == null)
                    return rc;
                if (courier == null)
                    return rc;
                if (shopOrders == null || shopOrders.Length <= 0)
                    return rc;
                if (orderCount < 0 || orderCount >= shopOrders.Length)
                    return rc;

                if (courierLocationInfo == null || courierLocationInfo.Length <= 0)
                    return rc;

                if (permutations == null || permutations.Length <= 0)
                    return rc;
                if (permutations.GetLength(1) != orderCount)
                    return rc;

                // 3. Решаем задачу комивояжера - построение пути обхода c минимальной стоимостью
                rc = 3;
                int permutationCount = permutations.GetLength(0);
                Order[] permutOrders = new Order[orderCount];
                double bestCost = double.MaxValue;
                int bestOrderCount = -1;

                for (int i = 0; i < permutationCount; i++)
                {
                    // 4.1 Строим перестановку заказов
                    rc = 41;
                    for (int j = 0; j < orderCount; j++)
                    {
                        permutOrders[j] = shopOrders[permutations[i, j]];
                    }

                    // 4.2 Проверяем построенный путь и отбираем наилучший среди всех перестановок
                    rc = 42;
                    CourierDeliveryInfo pathInfo;
                    int rc1 = courier.DeliveryCheck(calcTime, shop, (Order[])permutOrders.Clone(), isLoop, courierLocationInfo, out pathInfo);
                    if (rc1 == 0)
                    {
                        if (bestOrderCount < pathInfo.OrderCount)
                        {
                            bestOrderCount = pathInfo.OrderCount;
                            bestCost = pathInfo.Cost;
                            bestDelivery = pathInfo;
                        }
                        else if (bestOrderCount == pathInfo.OrderCount && pathInfo.Cost < bestCost)
                        {
                            bestCost = pathInfo.Cost;
                            bestDelivery = pathInfo;
                        }
                    }
                }

                if (bestDelivery == null)
                    return rc;

                // 5. Выход - Ok
                rc = 0;
                return rc;
            }
            catch (Exception ex)
            {
                //Helper.WriteErrorToLog($"FindSalesmanProblemSolution(Shop {shop.Id}, Orders {Helper.ArrayToString(shopOrders.Take(orderCount).Select(order => order.Id).ToArray())}, courier {courier.Id}, isloop {isLoop}, calcTime {calcTime})");
                //Helper.WriteErrorToLog($"(rc = {rc})");
                //Logger.WriteToLog(ex.ToString());
                Logger.WriteToLog(string.Format(MessagePatterns.METHOD_CALL, "FindSalesmanProblemSolution", $"Shop {shop.Id}, Orders {Helper.ArrayToString(shopOrders.Take(orderCount).Select(order => order.Id).ToArray())}, courier {courier.Id}, isloop {isLoop}, calcTime {calcTime}"));
                Logger.WriteToLog(string.Format(MessagePatterns.METHOD_RC, "FindSalesmanProblemSolution", rc));
                Logger.WriteToLog(string.Format(MessagePatterns.METHOD_FAIL, "FindSalesmanProblemSolution", ex.ToString()));

                return rc;
            }
        }

        /// <summary>
        /// Сравнение двух отгрузок по средней стоимости доставки одного заказа
        /// </summary>
        /// <param name="delivery1">Отгрузка 1</param>
        /// <param name="delivery2">Отгрузка 2</param>
        /// <returns>-1 - delivery1 меньше delivery2; 0 - delivery1 = delivery2; delivery1 больше delivery2</returns>
        private static int CompareByOrderCost(CourierDeliveryInfo delivery1, CourierDeliveryInfo delivery2)
        {
            if (delivery1.OrderCost < delivery2.OrderCost)
                return -1;
            if (delivery1.OrderCost > delivery2.OrderCost)
                return 1;
            return 0;
        }

        ///// <summary>
        ///// Сравнение двух отгрузок по времени вручения первого заказа и средней стоимости доставки заказа
        ///// (используется при сортировке отгрузок с просроченными заказами)
        ///// </summary>
        ///// <param name="delivery1">Отгрузка 1</param>
        ///// <param name="delivery2">Отгрузка 2</param>
        ///// <returns>-1 - delivery1 меньше delivery2; 0 - delivery1 = delivery2; delivery1 больше delivery2</returns>
        //private static int CompareByOrderTimeCost(CourierDeliveryInfo delivery1, CourierDeliveryInfo delivery2)
        //{
        //    if (delivery1.Orders[0].Id < delivery2.Orders[0].Id)
        //        return -1;
        //    if (delivery1.Orders[0].Id > delivery2.Orders[0].Id)
        //        return 1;

        //    if (delivery1.NodeDeliveryTime[1] < delivery2.NodeDeliveryTime[1])
        //        return -1;
        //    if (delivery1.NodeDeliveryTime[1] > delivery2.NodeDeliveryTime[1])
        //        return 1;

        //    if (delivery1.OrderCost < delivery2.OrderCost)
        //        return -1;
        //    if (delivery1.OrderCost > delivery2.OrderCost)
        //        return 1;

        //    return 0;
        //}

        /// <summary>
        /// Сравнение двух отгрузок по времени вручения первого заказа и средней стоимости доставки заказа
        /// (используется при сортировке отгрузок с просроченными заказами)
        /// </summary>
        /// <param name="delivery1">Отгрузка 1</param>
        /// <param name="delivery2">Отгрузка 2</param>
        /// <returns>-1 - delivery1 меньше delivery2; 0 - delivery1 = delivery2; delivery1 больше delivery2</returns>
        private static int CompareByOrderTimeCost(CourierDeliveryInfo delivery1, CourierDeliveryInfo delivery2)
        {
            if (delivery1.Orders[0].Id < delivery2.Orders[0].Id)
                return -1;
            if (delivery1.Orders[0].Id > delivery2.Orders[0].Id)
                return 1;

            if (delivery1.StartDeliveryInterval.AddMinutes(delivery1.NodeDeliveryTime[1]) < delivery2.StartDeliveryInterval.AddMinutes(delivery2.NodeDeliveryTime[1]))
                return -1;
            if (delivery1.StartDeliveryInterval.AddMinutes(delivery1.NodeDeliveryTime[1]) > delivery2.StartDeliveryInterval.AddMinutes(delivery2.NodeDeliveryTime[1]))
                return 1;

            if (delivery1.OrderCost < delivery2.OrderCost)
                return -1;
            if (delivery1.OrderCost > delivery2.OrderCost)
                return 1;

            return 0;
        }


        /// <summary>
        /// Построение покрытия из заданных отгрузок
        /// </summary>
        /// <param name="allDeliveries">
        /// Все возможные отгрузки, отсортированные по  
        /// возрастанию средней стоимости доставки одного заказа
        /// </param>
        /// <param name="orderCount">Общее количество заказов, для которых строились отгрузки</param>
        /// <param name="orderCount">Общее количество заказов, для которых строились отгрузки</param>
        /// <param name="isAssembledOnly">Флаг:
        /// true - использовать для построения покрытия отгрузки только с собранными заказами; 
        /// false - использовать для построения покрытия все переданные отгрузки
        /// </param>
        /// <param name="orderCoverMap">Фаги заказов, вошедших в построенное покрытие</param>
        /// <returns>0 - покрытие построено; иначе - покрытие не построено</returns>
        private static int BuildDeliveryCover(CourierDeliveryInfo[] allDeliveries, int orderCount, bool isAssembledOnly, out CourierDeliveryInfo[] deiveryCover, out bool[] orderCoverMap)
        {
            // 1. Инициализация
            int rc = 1;
            deiveryCover = null;
            orderCoverMap = null;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (allDeliveries == null || allDeliveries.Length <= 0)
                    return rc;
                if (orderCount <= 0)
                    return rc;

                // 3. Цикл построения покрытия
                rc = 3;
                deiveryCover = new CourierDeliveryInfo[orderCount];
                orderCoverMap = new bool[orderCount];
                int deliveryCount = 0;
                int coverCount = 0;

                for (int i = 0; i < allDeliveries.Length; i++)
                {
                    // 3.1 Извлекаем заказы отгрузки
                    rc = 31;
                    CourierDeliveryInfo delivery = allDeliveries[i];
                    Order[] deliveryOrders = delivery.Orders;
                    if (deliveryOrders == null || deliveryOrders.Length <= 0)
                        continue;

                    // 3.2 Фильтруем отгрузки состоящие не только из собранных заказов
                    rc = 32;
                    if (isAssembledOnly)
                    {
                        for (int j = 0; j < deliveryOrders.Length; j++)
                        {
                            if (deliveryOrders[j].Status != OrderStatus.Assembled)
                                goto NextDelivery;
                        }
                    }

                    // 3.3 Проверяем, что все заказы отгрузки не входят в уже отобранные отгрузки
                    rc = 33;
                    for (int j = 0; j < deliveryOrders.Length; j++)
                    {
                        if (orderCoverMap[deliveryOrders[j].Index])
                            goto NextDelivery;
                    }

                    // 3.4 Добавляем отгрузку в покрытие
                    rc = 34;
                    deiveryCover[deliveryCount++] = delivery;

                    // 3.5 Помечаем заказы, как попавшие в покрытие
                    rc = 35;
                    for (int j = 0; j < deliveryOrders.Length; j++)
                    {
                        orderCoverMap[deliveryOrders[j].Index] = true;
                    }

                    // 3.6 Если все заказы уже покрыты
                    rc = 36;
                    coverCount += deliveryOrders.Length;
                    if (coverCount >= orderCount)
                        break;

                    NextDelivery:
                    ;
                }

                if (deliveryCount < deiveryCover.Length)
                {
                    Array.Resize(ref deiveryCover, deliveryCount);
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
        /// Построение покрытия из заданных отгрузок
        /// </summary>
        /// <param name="allDeliveries">
        /// Все возможные отгрузки, отсортированные по  
        /// возрастанию средней стоимости доставки одного заказа
        /// </param>
        /// <param name="orderCount">Общее количество заказов, для которых строились отгрузки</param>
        /// <param name="isAssembledOnly">Флаг:
        /// true - использовать для построения покрытия отгрузки только с собранными заказами; 
        /// false - использовать для построения покрытия все переданные отгрузки
        /// </param>
        /// <param name="geoCache">Менеджер расстояний и времени движения между точками</param>
        /// <param name="orderCoverMap">Фаги заказов, вошедших в построенное покрытие</param>
        /// <returns>0 - покрытие построено; иначе - покрытие не построено</returns>
        private static int BuildDeliveryCoverEx(Courier[] shopCouriers, CourierDeliveryInfo[] allDeliveries, int orderCount, bool isAssembledOnly, GeoCache geoCache, out CourierDeliveryInfo[] deiveryCover, out bool[] orderCoverMap)
        {
            // 1. Инициализация
            int rc = 1;
            deiveryCover = null;
            orderCoverMap = null;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (allDeliveries == null || allDeliveries.Length <= 0)
                    return rc;
                if (orderCount <= 0)
                    return rc;

                // 3. Цикл построения покрытия
                rc = 3;
                deiveryCover = new CourierDeliveryInfo[orderCount];
                orderCoverMap = new bool[orderCount];
                int deliveryCount = 0;
                int coverCount = 0;
                Courier[] availableCourier = new Courier[shopCouriers.Length];
                shopCouriers.CopyTo(availableCourier, 0);

                for (int i = 0; i < allDeliveries.Length; i++)
                {
                    // 3.1 Извлекаем заказы отгрузки
                    rc = 31;
                    CourierDeliveryInfo delivery = allDeliveries[i];
                    Order[] deliveryOrders = delivery.Orders;
                    if (deliveryOrders == null || deliveryOrders.Length <= 0)
                        continue;

                    // 3.2 Фильтруем отгрузки состоящие не только из собранных заказов
                    rc = 32;
                    if (isAssembledOnly)
                    {
                        for (int j = 0; j < deliveryOrders.Length; j++)
                        {
                            if (deliveryOrders[j].Status != OrderStatus.Assembled)
                                goto NextDelivery;
                        }
                    }

                    // 3.3 Проверяем, что все заказы отгрузки не входят в уже отобпвнные отгрузки
                    rc = 33;
                    for (int j = 0; j < deliveryOrders.Length; j++)
                    {
                        if (orderCoverMap[deliveryOrders[j].Index])
                            goto NextDelivery;
                    }

                    // 3.4 Подбираем подходящего доступного курьера 
                    rc = 34;
                    CourierDeliveryInfo dstDelivery;
                    int rc1 = SelectCourier(delivery, ref availableCourier, geoCache, out dstDelivery);
                    if (rc1 != 0 || dstDelivery == null)
                        goto NextDelivery;

                    // 3.5 Добавляем отгрузку в покрытие
                    rc = 35;
                    deiveryCover[deliveryCount++] = dstDelivery;

                    // 3.6 Помечаем заказы, как попавшие в покрытие
                    rc = 36;
                    for (int j = 0; j < deliveryOrders.Length; j++)
                    {
                        orderCoverMap[deliveryOrders[j].Index] = true;
                    }

                    // 3.7 Если все заказы уже покрыты
                    rc = 37;
                    coverCount += deliveryOrders.Length;
                    if (coverCount >= orderCount)
                        break;

                    NextDelivery:
                    ;
                }

                if (deliveryCount < deiveryCover.Length)
                {
                    Array.Resize(ref deiveryCover, deliveryCount);
                }

                // 4. Выход - Ok
                rc = 0;
                return rc;
            }
            catch (Exception ex)
            {

                //Helper.WriteErrorToLog($"BuildDeliveryCoverEx(...)");
                //Helper.WriteErrorToLog($"(rc = {rc})");
                //Logger.WriteToLog(ex.ToString());
                Logger.WriteToLog(string.Format(MessagePatterns.METHOD_CALL, "BuildDeliveryCoverEx", "..."));
                Logger.WriteToLog(string.Format(MessagePatterns.METHOD_RC, "BuildDeliveryCoverEx", rc));
                Logger.WriteToLog(string.Format(MessagePatterns.METHOD_FAIL, "BuildDeliveryCoverEx", ex.ToString()));
                return rc;
            }
        }

        /// <summary>
        /// Построение покрытия из заданных отгрузок
        /// </summary>
        /// <param name="allDeliveries">
        /// Все возможные отгрузки, отсортированные по  
        /// возрастанию средней стоимости доставки одного заказа
        /// </param>
        /// <param name="orderCount">Общее количество заказов, для которых строились отгрузки</param>
        /// <param name="isAssembledOnly">Флаг:
        /// true - использовать для построения покрытия отгрузки только с собранными заказами; 
        /// false - использовать для построения покрытия все переданные отгрузки
        /// </param>
        /// <param name="geoCache">Менеджер расстояний и времени движения между точками</param>
        /// <param name="orderCoverMap">Фаги заказов, вошедших в построенное покрытие</param>
        /// <returns>0 - покрытие построено; иначе - покрытие не построено</returns>
        private static int BuildDeliveryCoverEy(Courier[] shopCouriers, CourierDeliveryInfo[] allDeliveries, Order[] allOrders, bool isAssembledOnly, GeoCache geoCache, out CourierDeliveryInfo[] deiveryCover, out bool[] orderCoverMap)
        {
            // 1. Инициализация
            int rc = 1;
            deiveryCover = null;
            orderCoverMap = null;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (allDeliveries == null || allDeliveries.Length <= 0)
                    return rc;
                if (allOrders == null || allOrders.Length <= 0)
                    return rc;

                // 3. Перенумеровыаем заказы
                rc = 3;
                int orderCount = allOrders.Length;
                for (int i = 0; i < orderCount; i++)
                {
                    allOrders[i].Index = i;
                }

                // 4. Цикл построения покрытия
                rc = 4;
                deiveryCover = new CourierDeliveryInfo[orderCount];
                orderCoverMap = new bool[orderCount];
                int deliveryCount = 0;
                int coverCount = 0;
                Courier[] availableCourier = new Courier[shopCouriers.Length];
                shopCouriers.CopyTo(availableCourier, 0);

                for (int i = 0; i < allDeliveries.Length; i++)
                {
                    // 4.1 Извлекаем заказы отгрузки
                    rc = 41;
                    CourierDeliveryInfo delivery = allDeliveries[i];
                    Order[] deliveryOrders = delivery.Orders;
                    if (deliveryOrders == null || deliveryOrders.Length <= 0)
                        continue;

                    // 4.2 Фильтруем отгрузки состоящие не только из собранных заказов
                    rc = 42;
                    if (isAssembledOnly)
                    {
                        for (int j = 0; j < deliveryOrders.Length; j++)
                        {
                            if (deliveryOrders[j].Status != OrderStatus.Assembled)
                                goto NextDelivery;
                        }
                    }

                    // 4.3 Проверяем, что все заказы отгрузки не входят в уже отобранные отгрузки
                    rc = 43;
                    for (int j = 0; j < deliveryOrders.Length; j++)
                    {
                        if (orderCoverMap[deliveryOrders[j].Index])
                            goto NextDelivery;
                    }

                    // 4.4 Подбираем подходящего доступного курьера 
                    rc = 44;
                    CourierDeliveryInfo dstDelivery;
                    int rc1 = SelectCourier(delivery, ref availableCourier, geoCache, out dstDelivery);
                    if (rc1 != 0 || dstDelivery == null)
                        goto NextDelivery;

                    // 4.5 Добавляем отгрузку в покрытие
                    rc = 45;
                    deiveryCover[deliveryCount++] = dstDelivery;

                    // 4.6 Помечаем заказы, как попавшие в покрытие
                    rc = 46;
                    for (int j = 0; j < deliveryOrders.Length; j++)
                    {
                        orderCoverMap[deliveryOrders[j].Index] = true;
                    }

                    // 4.7 Если все заказы уже покрыты
                    rc = 47;
                    coverCount += deliveryOrders.Length;
                    if (coverCount >= orderCount)
                        break;

                    NextDelivery:
                    ;
                }

                if (deliveryCount < deiveryCover.Length)
                {
                    Array.Resize(ref deiveryCover, deliveryCount);
                }

                // 5. Выход - Ok
                rc = 0;
                return rc;
            }
            catch (Exception ex)
            {

                //Helper.WriteErrorToLog($"BuildDeliveryCoverEy(...)");
                //Helper.WriteErrorToLog($"(rc = {rc})");
                //Logger.WriteToLog(ex.ToString());
                Logger.WriteToLog(string.Format(MessagePatterns.METHOD_CALL, "BuildDeliveryCoverEy", "..."));
                Logger.WriteToLog(string.Format(MessagePatterns.METHOD_RC, "BuildDeliveryCoverEy", rc));
                Logger.WriteToLog(string.Format(MessagePatterns.METHOD_FAIL, "BuildDeliveryCoverEy", ex.ToString()));

                return rc;
            }
        }

        /// <summary>
        /// Выбор подходящего курьера или такси для отгрузки
        /// </summary>
        /// <param name="srcDelivery">Отгрузка</param>
        /// <param name="shopCouriers">Доступные курьеры и такси</param>
        /// <param name="geoCache">Менеджер расстояний и времени движения</param>
        /// <param name="dstDelivery">Отгрузка для выбранного курьера</param>
        /// <returns></returns>
        private static int SelectCourier(CourierDeliveryInfo srcDelivery, ref Courier[] shopCouriers, GeoCache geoCache, out CourierDeliveryInfo dstDelivery)
        {
            // 1. Инициализация
            int rc = 1;
            dstDelivery = null;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (srcDelivery == null)
                    return rc;
                if (shopCouriers == null || shopCouriers.Length <= 0)
                    return rc;
                if (geoCache == null)
                    return rc;

                // 3. Ищем подходящего курьера
                rc = 3;
                CourierVehicleType srcVehicleType = srcDelivery.DeliveryCourier.CourierType.VechicleType;

                for (int i = 0; i < shopCouriers.Length; i++)
                {
                    Courier courier = shopCouriers[i];
                    if (courier != null)
                    {
                        if (courier.CourierType.VechicleType == srcVehicleType)
                        {
                            if (courier.IsTaxi)
                            {
                                srcDelivery.DeliveryCourier = courier;
                                dstDelivery = srcDelivery;
                                return rc = 0;
                            }
                            else
                            {
                                Point[,] locInfo;
                                int rc1 = GetDistTimeTable(srcDelivery.FromShop, srcDelivery.Orders, srcVehicleType, geoCache, out locInfo);
                                if (rc1 != 0)
                                    return rc = 100000 * rc + rc1;

                                rc1 = courier.DeliveryCheck(srcDelivery.CalculationTime, srcDelivery.FromShop, srcDelivery.Orders, srcDelivery.IsLoop, locInfo, out dstDelivery);
                                if (rc1 == 0 && dstDelivery != null)
                                {
                                    srcDelivery.DeliveryCourier = courier;
                                    shopCouriers[i] = null;
                                    return rc = 0;
                                }
                            }
                        }
                    }
                }

                dstDelivery = null;

                // 4. Не удалось подобрать курьера
                rc = 4;
                return rc;
            }
            catch
            {
                return rc;
            }
        }

        /// <summary>
        /// Запрос таблицы попарных расстояний и времени движения для магазина и заказов
        /// </summary>
        /// <param name="shop">Магазин</param>
        /// <param name="orders">Заказы</param>
        /// <param name="vehicleType">Тип способа доставки</param>
        /// <param name="geoCache">Geo-кэш</param>
        /// <param name="distTimeTable">Таблица результата</param>
        /// <returns>0 - таблица построена; иначе - таблица не построена</returns>
        private static int GetDistTimeTable(Shop shop, Order[] orders, CourierVehicleType vehicleType, GeoCache geoCache, out Point[,] distTimeTable)
        {
            // 1. Инициализация
            int rc = 1;
            distTimeTable = null;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (shop == null)
                    return rc;
                if (orders == null || orders.Length <= 0)
                    return rc;
                if (geoCache == null)
                    return rc;

                // 3. создаём массивы с аргументами
                rc = 3;
                int size = orders.Length + 1;
                double[] latitude = new double[size];
                double[] longitude = new double[size];
                latitude[size - 1] = shop.Latitude;
                longitude[size - 1] = shop.Longitude;
                shop.LocationIndex = size - 1;

                for (int i = 0; i < orders.Length; i++)
                {
                    Order order = orders[i];
                    order.LocationIndex = i;
                    latitude[i] = order.Latitude;
                    longitude[i] = order.Longitude;
                }

                // 4. Запрашиваем таблицу результата
                rc = 4;
                int rc1 = geoCache.GetPointsDataTable(latitude, longitude, vehicleType, out distTimeTable);
                if (rc1 != 0 || distTimeTable == null)
                    return rc = 1000 * rc + rc1;

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
        /// Создание огранчений числа заказов
        /// для длины путей от 1 до 8
        /// </summary>
        /// <param name="orderLimits">Требуемые ограничения</param>
        /// <returns>Построенные ограничения</returns>
        private static int[] CreateOrderLimits(SalesmanProblemLevel[] orderLimits)
        {
            // 1. Инициализация
            int[] limits = new int[] { 0, 1000000, 1000, 56, 48, 40, 32, 24, 14 };

            try
            {
                // 2. Проверяем исходные данные
                if (orderLimits == null || orderLimits.Length <= 0)
                    return limits;

                // 3. Цикл заполнения пределов
                foreach (SalesmanProblemLevel spl in orderLimits)
                {
                    if (spl.level > 0 && spl.level <= 8 && spl.to_orders > 0)
                        limits[spl.level] = spl.to_orders;
                }

                // 4. Возвращаем результат
                return limits;
            }
            catch
            {
                return limits;
            }
        }

        /// <summary>
        /// Построение отсортированного списка кодов способов доставки
        /// для заданных курьеров
        /// </summary>
        /// <param name="couriers">Курьеры</param>
        /// <returns>Отсортированные способы доставки</returns>
        private static int[] GetCourierVehicleTypes(Courier[] couriers)
        {
            if (couriers == null || couriers.Length <= 0)
                return new int[0];
            int[] vehicleTypes = couriers.Select(courier => (int)courier.CourierType.VechicleType).Distinct().ToArray();
            Array.Sort(vehicleTypes);
            return vehicleTypes;
        }

        /// <summary>
        /// Построение отгрузки из одного заказа
        /// </summary>
        /// <param name="shop">Магазин</param>
        /// <param name="order">Заказ</param>
        /// <param name="shopCourier">Курьер</param>
        /// <param name="isLoop">Флаг возврата в магазин: true - требуется возврат в магазин; false - отгрузка завершается после вручения последнего заказа</param>
        /// <param name="calcTime">Момент расчетов</param>
        /// <param name="singleDelivery">Построенная одиночная отгрузка или null</param>
        /// <returns>0 - отгрузка построена; иначе - отгрузка не построена</returns>
        public static int CreateSingleDelivery(Shop shop, Order order, Courier shopCourier, bool isLoop, DateTime calcTime, GeoCache geoCache, out CourierDeliveryInfo singleDelivery)
        {
            // 1. Инициализация
            int rc = 1;
            singleDelivery = null;

            try
            {
                // 2. Проверяем исходные 
                rc = 2;
                if (shop == null)
                    return rc;
                if (shopCourier == null)
                    return rc;
                if (order == null)
                    return rc;
                if (geoCache == null)
                    return rc;

                // 3. Выбираем расстояния и времена движения между точками для заданного способа передвижения
                rc = 3;
                Point[,] locInfo;
                Order[] orders = new Order[] { order };
                int rc1 = GetDistTimeTable(shop, orders, shopCourier.CourierType.VechicleType, geoCache, out locInfo);
                if (rc1 != 0)
                    return rc = 10000 * rc + rc1;

                // 4. Строим отгрузку
                rc = 4;
                rc1 = shopCourier.DeliveryCheck(calcTime, shop, orders, isLoop, locInfo, out singleDelivery);
                if (rc1 != 0 || singleDelivery == null)
                    return rc = 10000 * rc + rc1;

                // 5. Выход - Ok
                rc = 0;
                return rc;
            }
            catch (Exception ex)
            {
                //Helper.WriteErrorToLog($"CreateSingleDelivery(Shop {shop.Id}, OrderId {shop}, courier {order.Id}, isloop {isLoop}, calcTime {calcTime})");
                //Helper.WriteErrorToLog($"(rc = {rc})");
                //Logger.WriteToLog(ex.ToString());
                Logger.WriteToLog(string.Format(MessagePatterns.METHOD_CALL, "CreateSingleDeliveryEx", $"Shop {shop.Id}, OrderId {shop}, courier {order.Id}, isloop {isLoop}, calcTime {calcTime}"));
                Logger.WriteToLog(string.Format(MessagePatterns.METHOD_RC, "CreateSingleDeliveryEx", rc));
                Logger.WriteToLog(string.Format(MessagePatterns.METHOD_FAIL, "CreateSingleDeliveryEx", ex.ToString()));

                return rc;
            }
        }

        /// <summary>
        /// Объединение двух массивов CourierDeliveryInfo
        /// </summary>
        /// <param name="cdi1">Первый массив</param>
        /// <param name="cdi2">Второй массив</param>
        /// <returns>Объединение двух массивов или null</returns>
        private static CourierDeliveryInfo[] MergeCourierDeliveryInfo(CourierDeliveryInfo[] cdi1, CourierDeliveryInfo[] cdi2)
        {
            if (cdi1 != null && cdi2 != null)
            {
                CourierDeliveryInfo[] union = new CourierDeliveryInfo[cdi1.Length + cdi2.Length];
                cdi1.CopyTo(union, 0);
                cdi2.CopyTo(union, cdi1.Length);
                return union;
            }
            else if (cdi1 != null && cdi2 == null)
            {
                return cdi1;
            }
            else if (cdi1 == null && cdi2 != null)
            {
                return cdi2;
            }

            return null;
        }

        /// <summary>
        /// Объединение двух массивов c заказами
        /// </summary>
        /// <param name="orders1">Первый массив</param>
        /// <param name="orders2">Второй массив</param>
        /// <returns>Объединение двух массивов или null</returns>
        private static Order[] MergeOrders(Order[] orders1, Order[] orders2)
        {
            if (orders1 != null && orders2 != null)
            {
                Order[] union = new Order[orders1.Length + orders2.Length];
                orders1.CopyTo(union, 0);
                orders2.CopyTo(union, orders1.Length);
                return union;
            }
            else if (orders1 != null && orders2 == null)
            {
                return orders1;
            }
            else if (orders1 == null && orders2 != null)
            {
                return orders2;
            }

            return null;
        }
    }
}
