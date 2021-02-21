
namespace LogisticsService.Couriers
{
    using LogisticsService.Orders;
    using LogisticsService.Shops;
    using System;
    using System.Drawing;

    /// <summary>
    /// Курьер
    /// </summary>
    public class Courier
    {
        /// <summary>
        /// Минимальное время оплаты, час
        /// </summary>
        public const double MIN_WORK_TIME = 4;

        /// <summary>
        /// ID курьера
        /// </summary>
        public int Id { get; private set; }

        /// <summary>
        /// Тип курьера
        /// </summary>
        public ICourierType CourierType { get; private set; }

        /// <summary>
        /// Статус курьера
        /// </summary>
        public CourierStatus Status { get; set; }

        /// <summary>
        /// Начало работы
        /// </summary>
        public TimeSpan WorkStart { get; set; }

        /// <summary>
        /// Конец работы
        /// </summary>
        public TimeSpan WorkEnd { get; set; }

        /// <summary>
        /// Начало обеда
        /// </summary>
        public TimeSpan LunchTimeStart { get; set; }

        /// <summary>
        /// Конец обеда
        /// </summary>
        public TimeSpan LunchTimeEnd { get; set; }

        /// <summary>
        /// Время начала отгрузки (Status = DeliversOrder)
        /// </summary>
        public DateTime LastDeliveryStart { get; set; }

        /// <summary>
        /// Время возвращения (Status = DeliversOrder)
        /// </summary>
        public DateTime LastDeliveryEnd { get; set; }

        /// <summary>
        /// Количество выполненных заказов
        /// </summary>
        public int OrderCount { get; set; }

        /// <summary>
        /// Время, потраченное на выполнение всех заказов за день, час
        /// </summary>
        public double TotalDeliveryTime { get; set; }

        /// <summary>
        /// Общая стоимость курьера за день
        /// </summary>
        public double TotalCost { get; set; }

        /// <summary>
        /// Средняя цена стоимости заказа
        /// </summary>
        public double AverageOrderCost { get; set; }

        /// <summary>
        /// Индекс курьера
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Такси ?
        /// </summary>
        //public bool IsTaxi => (CourierType.VechicleType == CourierVehicleType.GettTaxi || CourierType.VechicleType == CourierVehicleType.YandexTaxi);
        public bool IsTaxi => CourierType.IsTaxi;

        /// <summary>
        /// Флаг сервиса доставки для курьера
        /// </summary>
        public EnabledCourierType ServiceFlags { get; private set; }

        /// <summary>
        /// Id маназина, к которому привязан курьер
        /// (фиксированная модель)
        /// </summary>
        public int ShopId { get; set; }

        /// <summary>
        /// Широта местоположения курьера
        /// </summary>
        public double Latitude { get; set; }

        /// <summary>
        /// Долгота местоположения курьера
        /// </summary>
        public double Longitude { get; set; }

        /// <summary>
        /// Параметрический конструктор класса Courier
        /// </summary>
        /// <param name="id"></param>
        /// <param name="parameters"></param>
        public Courier(int id, ICourierType courierType)
        {
            CourierType = courierType;
            Id = id;
            //ServiceFlags = GetEnabledCourierType();
        }

        /// <summary>
        /// Проверка возможности доставки нескольких заказов
        /// и подсчет резерва времени доставки и её стоимости
        /// </summary>
        /// <param name="currentModelTime">Время, в которое происходит расчет (время модели).
        /// (Оно должно указывать на момент после сборки всех заказов !)
        /// </param>
        /// <param name="fromShop">Магазин, из которого осуществляется отгрузка</param>
        /// <param name="orders">Отгружаемые заказы</param>
        /// <param name="isLoop">Флаг возарата в магазин после завершения доставки</param>
        /// <param name="locInfo">Расстояние и время движения между точками</param>
        /// <param name="deliveryInfo">Информация об отгрузке</param>
        /// <returns>0 - отгрузка может быть осуществлена; иначе - отгрузка в срок невозможна</returns>
        public int DeliveryCheck(DateTime currentModelTime, Shop fromShop, Order[] orders, bool isLoop, Point[,] locInfo, out CourierDeliveryInfo deliveryInfo)
        {
            // 1. Инициализация 
            int rc = 1;
            deliveryInfo = null;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (fromShop == null)
                    return rc;
                if (orders == null || orders.Length <= 0)
                    return rc;
                if (locInfo == null || locInfo.Length <= 0)
                    return rc;
                int locCount = locInfo.GetLength(0);
                if (locInfo.GetLength(1) != locCount)
                    return rc;

                // 3. Создаем результат
                rc = 3;
                deliveryInfo = new CourierDeliveryInfo(this, orders, currentModelTime, isLoop);
                deliveryInfo.FromShop = fromShop;
                if (this.Status == CourierStatus.Unknown)
                    return rc;

                // 4. Извлекаем расстояния, время между узлами пути и подсчитываем общий вес
                rc = 4;
                double weight = 0;
                Point[] nodeInfo = new Point[orders.Length + 2];
                int shopLocIndex = fromShop.LocationIndex;
                int prevLocIndex = shopLocIndex;
                //EnabledCourierType serviceFlag = ServiceFlags;
                CourierVehicleType thisVehicleType = CourierType.VechicleType;

                for (int i = 0; i < orders.Length; i++)
                {
                    Order order = orders[i];
                    //if ((order.EnabledTypes & serviceFlag) == 0)
                    //    return rc;
                    if (!order.IsVehicleTypeEnabled(thisVehicleType))
                        return rc;

                    if (order.Weight > CourierType.MaxOrderWeight)
                        return rc;

                    weight += order.Weight;

                    int orderLocIndex = order.LocationIndex;
                    nodeInfo[i + 1] = locInfo[prevLocIndex, orderLocIndex];
                    prevLocIndex = orderLocIndex;
                }

                nodeInfo[nodeInfo.Length - 1] = locInfo[prevLocIndex, shopLocIndex];
                deliveryInfo.NodeInfo = nodeInfo;

                //LogisticsService.API.GetShippingInfo.ShippingInfoRequestEx req = new API.GetShippingInfo.ShippingInfoRequestEx();
                //req.modes = new string[] { "driving" };
                //req.origins = new double[1][];
                //req.origins[0] = new double[]{ fromShop.Latitude, fromShop.Longitude };
                //req.destinations = new double[1][];
                //req.destinations[0] = new double[]{ orders[0].Latitude, orders[0].Longitude };
                //LogisticsService.API.GetShippingInfo.ShippingInfoResponse rsp;
                //int rcz =  LogisticsService.API.GetShippingInfo.GetInfo(req, out rsp);

                //req.destinations[0] = new double[]{ fromShop.Latitude, fromShop.Longitude };
                //req.origins[0] = new double[]{ orders[0].Latitude, orders[0].Longitude };
                //LogisticsService.API.GetShippingInfo.ShippingInfoResponse rspinv;
                //rcz =  LogisticsService.API.GetShippingInfo.GetInfo(req, out rspinv);




                // 5. Подсчитываем время и стоимость доставки
                rc = 5;
                double[] nodeDeliveryTime;
                double deliveryTime;
                double executionTime;
                double cost;
                int rc1 = CourierType.GetTimeAndCost(nodeInfo, weight, isLoop, out nodeDeliveryTime, out deliveryTime, out executionTime, out cost);
                if (rc1 != 0)
                    return rc = 100 * rc + rc1;

                // 6. Проверяем доставку вовремя и стороим интервал начала доставки
                rc = 6;
                DateTime intervalStart = currentModelTime;
                DateTime intervalEnd = DateTime.MaxValue;

                //for (int i = 0; i < orders.Length; i++)
                //{
                //    Order order = orders[i];
                //    double dt = nodeDeliveryTime[i + 1];
                //    DateTime minTime = order.DeliveryTimeFrom.AddMinutes(-dt);
                //    DateTime maxTime = order.DeliveryTimeTo.AddMinutes(-dt);
                //    if (currentModelTime < minTime || currentModelTime > maxTime)
                //        return rc;

                //    if (minTime > intervalStart) intervalStart = minTime;
                //    if (maxTime < intervalEnd) intervalEnd = maxTime;
                //    if (minTime > maxTime)
                //        return rc;
                //}


                for (int i = 0; i < orders.Length; i++)
                {
                    Order order = orders[i];
                    double dt = nodeDeliveryTime[i + 1];
                    DateTime minTime = order.DeliveryTimeFrom.AddMinutes(-dt);
                    DateTime maxTime = order.DeliveryTimeTo.AddMinutes(-dt);
                    if (currentModelTime <= minTime)
                    { }
                    else if (currentModelTime < maxTime)
                    {
                        minTime = currentModelTime;
                    }
                    else
                    {
                        return rc;
                    }

                    //if (currentModelTime < minTime || currentModelTime > maxTime)
                    //    return rc;

                    if (minTime > intervalStart) intervalStart = minTime;
                    if (maxTime < intervalEnd) intervalEnd = maxTime;
                    if (minTime > maxTime)
                        return rc;
                }


                // 7. Считаем резерв времени для отгрузки
                rc = 7;
                TimeSpan reserveTime = intervalEnd - currentModelTime;

                // 8. ИНД = ИНД ∩ Рабочее_Время
                rc = 8;
                if (!IsTaxi)
                {
                    DateTime workStart = currentModelTime.Date.Add(WorkStart);
                    DateTime workEnd = currentModelTime.Date.Add(WorkEnd);
                    if (!Helper.TimeIntervalsIntersection(intervalStart, intervalEnd, workStart, workEnd, out intervalStart, out intervalEnd))
                        return rc;

                    // 9. Если ИНД лежит целиком внутри обеда
                    rc = 9;
                    DateTime lunchStart = currentModelTime.Date.Add(LunchTimeStart);
                    DateTime lunchEnd = currentModelTime.Date.Add(LunchTimeEnd);
                    if (intervalStart >= lunchStart && intervalEnd <= lunchEnd)
                        return rc;

                    // 10. Строим интервал доставки с учетом обеда
                    rc = 10;
                    if (lunchStart >= intervalStart && lunchStart <= intervalEnd)
                    {
                        intervalEnd = lunchStart;
                        reserveTime = intervalEnd - currentModelTime;
                    }
                    if (lunchEnd >= intervalStart && lunchStart <= intervalEnd)
                    {
                        if (lunchStart < intervalStart) intervalStart = lunchEnd;
                    }

                    // 11. Если сейчас осуществляется доставка
                    //rc = 11;
                    //if (Status == CourierStatus.DeliversOrder)
                    //{
                    //    if (LastDeliveryEnd > intervalEnd)
                    //        return rc;
                    //    if (LastDeliveryEnd > intervalStart)
                    //    {
                    //        intervalStart = LastDeliveryEnd;
                    //        reserveTime = intervalEnd - intervalStart;
                    //    }
                    //}
                }

                // 12. Присвоение результата
                rc = 12;
                deliveryInfo.Cost = cost;
                deliveryInfo.DeliveryTime = deliveryTime;
                deliveryInfo.ExecutionTime = executionTime;
                deliveryInfo.ReserveTime = reserveTime;
                deliveryInfo.StartDeliveryInterval = intervalStart;
                deliveryInfo.EndDeliveryInterval = intervalEnd;
                deliveryInfo.NodeDeliveryTime = nodeDeliveryTime;

                // 13. Выход - Ok
                rc = 0;
                return rc;
            }
            catch
            {
                return rc;
            }


        }

        /// <summary>
        /// Проверка возможности доставки нескольких заказов
        /// и подсчет резерва времени доставки и её стоимости
        /// </summary>
        /// <param name="currentModelTime">Время, в которое происходит расчет (время модели).
        /// (Оно должно указывать на момент после сборки всех заказов !)
        /// </param>
        /// <param name="fromShop">Магазин, из которого осуществляется отгрузка</param>
        /// <param name="orders">Отгружаемые заказы</param>
        /// <param name="orderGeoIndex">Гео-индексы заказов и магазина (сначала заказы в порядке следования; последний - магазин)</param>
        /// <param name="isLoop">Флаг возарата в магазин после завершения доставки</param>
        /// <param name="geoData">Расстояние и время движения между точками</param>
        /// <param name="deliveryInfo">Информация об отгрузке</param>
        /// <returns>0 - отгрузка может быть осуществлена; иначе - отгрузка в срок невозможна</returns>
        public int DeliveryCheck_new(DateTime currentModelTime, Shop fromShop, Order[] orders, int[] orderGeoIndex, bool isLoop, Point[,] geoData, out CourierDeliveryInfo deliveryInfo)
        {
            // 1. Инициализация 
            int rc = 1;
            deliveryInfo = null;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (fromShop == null)
                    return rc;
                if (orders == null || orders.Length <= 0)
                    return rc;
                if (orderGeoIndex == null ||
                    orderGeoIndex.Length != orders.Length + 1)
                    return rc;
                if (geoData == null || geoData.Length <= 0)
                    return rc;
                int locCount = geoData.GetLength(0);
                if (geoData.GetLength(1) != locCount)
                    return rc;

                // 3. Создаем результат
                rc = 3;
                deliveryInfo = new CourierDeliveryInfo(this, orders, currentModelTime, isLoop);
                deliveryInfo.FromShop = fromShop;
                if (this.Status == CourierStatus.Unknown)
                    return rc;

                // 4. Извлекаем расстояния, время между узлами пути и подсчитываем общий вес
                rc = 4;
                double weight = 0;
                Point[] nodeInfo = new Point[orders.Length + 2];
                int shopLocIndex = orderGeoIndex[orders.Length];
                int prevLocIndex = shopLocIndex;
                //EnabledCourierType serviceFlag = ServiceFlags;
                CourierVehicleType thisVehicleType = CourierType.VechicleType;

                for (int i = 0; i < orders.Length; i++)
                {
                    Order order = orders[i];
                    //if ((order.EnabledTypes & serviceFlag) == 0)
                    //    return rc;
                    if (!order.IsVehicleTypeEnabled(thisVehicleType))
                        return rc;

                    if (order.Weight > CourierType.MaxOrderWeight)
                        return rc;

                    weight += order.Weight;

                    int orderLocIndex = orderGeoIndex[i];
                    nodeInfo[i + 1] = geoData[prevLocIndex, orderLocIndex];
                    prevLocIndex = orderLocIndex;
                }

                nodeInfo[nodeInfo.Length - 1] = geoData[prevLocIndex, shopLocIndex];
                deliveryInfo.NodeInfo = nodeInfo;

                //LogisticsService.API.GetShippingInfo.ShippingInfoRequestEx req = new API.GetShippingInfo.ShippingInfoRequestEx();
                //req.modes = new string[] { "driving" };
                //req.origins = new double[1][];
                //req.origins[0] = new double[]{ fromShop.Latitude, fromShop.Longitude };
                //req.destinations = new double[1][];
                //req.destinations[0] = new double[]{ orders[0].Latitude, orders[0].Longitude };
                //LogisticsService.API.GetShippingInfo.ShippingInfoResponse rsp;
                //int rcz =  LogisticsService.API.GetShippingInfo.GetInfo(req, out rsp);

                //req.destinations[0] = new double[]{ fromShop.Latitude, fromShop.Longitude };
                //req.origins[0] = new double[]{ orders[0].Latitude, orders[0].Longitude };
                //LogisticsService.API.GetShippingInfo.ShippingInfoResponse rspinv;
                //rcz =  LogisticsService.API.GetShippingInfo.GetInfo(req, out rspinv);




                // 5. Подсчитываем время и стоимость доставки
                rc = 5;
                double[] nodeDeliveryTime;
                double deliveryTime;
                double executionTime;
                double cost;
                int rc1 = CourierType.GetTimeAndCost(nodeInfo, weight, isLoop, out nodeDeliveryTime, out deliveryTime, out executionTime, out cost);
                if (rc1 != 0)
                    return rc = 100 * rc + rc1;

                // 6. Проверяем доставку вовремя и стороим интервал начала доставки
                rc = 6;
                DateTime intervalStart = currentModelTime;
                DateTime intervalEnd = DateTime.MaxValue;

                //for (int i = 0; i < orders.Length; i++)
                //{
                //    Order order = orders[i];
                //    double dt = nodeDeliveryTime[i + 1];
                //    DateTime minTime = order.DeliveryTimeFrom.AddMinutes(-dt);
                //    DateTime maxTime = order.DeliveryTimeTo.AddMinutes(-dt);
                //    if (currentModelTime < minTime || currentModelTime > maxTime)
                //        return rc;

                //    if (minTime > intervalStart) intervalStart = minTime;
                //    if (maxTime < intervalEnd) intervalEnd = maxTime;
                //    if (minTime > maxTime)
                //        return rc;
                //}


                for (int i = 0; i < orders.Length; i++)
                {
                    Order order = orders[i];
                    double dt = nodeDeliveryTime[i + 1];
                    DateTime minTime = order.DeliveryTimeFrom.AddMinutes(-dt);
                    DateTime maxTime = order.DeliveryTimeTo.AddMinutes(-dt);
                    if (currentModelTime <= minTime)
                    { }
                    else if (currentModelTime < maxTime)
                    {
                        minTime = currentModelTime;
                    }
                    else
                    {
                        return rc;
                    }

                    //if (currentModelTime < minTime || currentModelTime > maxTime)
                    //    return rc;

                    if (minTime > intervalStart) intervalStart = minTime;
                    if (maxTime < intervalEnd) intervalEnd = maxTime;
                    if (minTime > maxTime)
                        return rc;
                }


                // 7. Считаем резерв времени для отгрузки
                rc = 7;
                TimeSpan reserveTime = intervalEnd - currentModelTime;

                // 8. ИНД = ИНД ∩ Рабочее_Время
                rc = 8;
                if (!IsTaxi)
                {
                    DateTime workStart = currentModelTime.Date.Add(WorkStart);
                    DateTime workEnd = currentModelTime.Date.Add(WorkEnd);
                    if (!Helper.TimeIntervalsIntersection(intervalStart, intervalEnd, workStart, workEnd, out intervalStart, out intervalEnd))
                        return rc;

                    // 9. Если ИНД лежит целиком внутри обеда
                    rc = 9;
                    DateTime lunchStart = currentModelTime.Date.Add(LunchTimeStart);
                    DateTime lunchEnd = currentModelTime.Date.Add(LunchTimeEnd);
                    if (intervalStart >= lunchStart && intervalEnd <= lunchEnd)
                        return rc;

                    // 10. Строим интервал доставки с учетом обеда
                    rc = 10;
                    if (lunchStart >= intervalStart && lunchStart <= intervalEnd)
                    {
                        intervalEnd = lunchStart;
                        reserveTime = intervalEnd - currentModelTime;
                    }
                    if (lunchEnd >= intervalStart && lunchStart <= intervalEnd)
                    {
                        if (lunchStart < intervalStart) intervalStart = lunchEnd;
                    }

                    // 11. Если сейчас осуществляется доставка
                    //rc = 11;
                    //if (Status == CourierStatus.DeliversOrder)
                    //{
                    //    if (LastDeliveryEnd > intervalEnd)
                    //        return rc;
                    //    if (LastDeliveryEnd > intervalStart)
                    //    {
                    //        intervalStart = LastDeliveryEnd;
                    //        reserveTime = intervalEnd - intervalStart;
                    //    }
                    //}
                }

                // 12. Присвоение результата
                rc = 12;
                deliveryInfo.Cost = cost;
                deliveryInfo.DeliveryTime = deliveryTime;
                deliveryInfo.ExecutionTime = executionTime;
                deliveryInfo.ReserveTime = reserveTime;
                deliveryInfo.StartDeliveryInterval = intervalStart;
                deliveryInfo.EndDeliveryInterval = intervalEnd;
                deliveryInfo.NodeDeliveryTime = nodeDeliveryTime;

                // 13. Выход - Ok
                rc = 0;
                return rc;
            }
            catch
            {
                return rc;
            }


        }

        /// <summary>
        /// Создание клона курьера с заданным id
        /// </summary>
        /// <param name="id">Новый Id или -1, если требуется оставить текущий</param>
        /// <returns>Клон курьера</returns>
        public Courier Clone(int id = -1)
        {
            if (id == -1)
                id = Id;
            Courier courierClone = new Courier(id, CourierType);
            courierClone.Status = Status;
            courierClone.WorkStart = WorkStart;
            courierClone.WorkEnd = WorkEnd;
            courierClone.LunchTimeStart = LunchTimeStart;
            courierClone.LunchTimeEnd = LunchTimeEnd;
            courierClone.LastDeliveryStart = LastDeliveryStart;
            courierClone.LastDeliveryEnd = LastDeliveryEnd;
            courierClone.OrderCount = OrderCount;
            courierClone.TotalDeliveryTime = TotalDeliveryTime;
            courierClone.TotalCost = TotalCost;
            courierClone.AverageOrderCost = AverageOrderCost;
            courierClone.Index = Index;
            courierClone.Latitude = Latitude;
            courierClone.Longitude = Longitude;
            //courierClone.ServiceFlags = ServiceFlags;
            courierClone.ShopId = ShopId;
            return courierClone;
        }
        
        public bool GetCourierDayCost(DateTime workStart, DateTime workEnd, int orderCount, out double workInterval, out double cost)
        {
            // 1. Инициализация
            workInterval = 0;
            cost = 0;

            try
            {
                // 2. Проверяем исходные данные
                if (workStart > workEnd)
                    return false;
                if (CourierType.HourlyRate <= 0)
                    return false;

                // 3. Расчитываем временной интервал в часах
                TimeSpan ts = (workEnd - workStart);
                workInterval = ts.TotalHours;
                if (workInterval < MIN_WORK_TIME)
                {
                    workInterval = MIN_WORK_TIME;
                }
                else
                {
                    workInterval = ts.Hours;
                    if (ts.Minutes != 0 || ts.Seconds != 0)
                    {
                        workInterval++;
                    }
                }

                // 4. Считаем стоимость рабочего дня почасового курьера
                cost = CourierType.HourlyRate * workInterval;
                if (CourierType.SecondPay > 0)
                    cost += orderCount * CourierType.SecondPay;
                cost *= (1 + CourierType.Insurance);

                // 5. Выход
                return true;
            }
            catch
            {
                return false;
            }
        }

        ///// <summary>
        ///// Получить флаг сервиса доставки
        ///// </summary>
        ///// <returns>Флаг сервиса доставки</returns>
        //private EnabledCourierType GetEnabledCourierType()
        //{
        //    if (CourierType == null)
        //        return EnabledCourierType.Unknown;

        //    switch (CourierType.VechicleType)
        //    {
        //        case CourierVehicleType.Car:
        //            return EnabledCourierType.Car;
        //        case CourierVehicleType.Bicycle:
        //            return EnabledCourierType.Bicycle;
        //        case CourierVehicleType.OnFoot:
        //            return EnabledCourierType.OnFoot;
        //        case CourierVehicleType.YandexTaxi:
        //            return EnabledCourierType.YandexTaxi;
        //        case CourierVehicleType.GettTaxi:
        //            return EnabledCourierType.GettTaxi;
        //    }

        //    return EnabledCourierType.Unknown;
        //}
    }
}
