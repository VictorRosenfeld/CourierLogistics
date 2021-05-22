
namespace SQLCLR.Couriers
{
    using SQLCLR.Deliveries;
    using SQLCLR.Orders;
    using SQLCLR.Shops;
    using System;

    /// <summary>
    /// Курьер (такси)
    /// </summary>
    public class Courier: CourierBase
    {
        /// <summary>
        /// ID курьера
        /// </summary>
        public int Id { get; private set; }

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
        /// Параметрический констуктор класса Courier
        /// </summary>
        /// <param name="id">ID курьера</param>
        /// <param name="courierType">Параметры способа доставки</param>
        public Courier(int id, ICourierType courierType) : base(courierType)
        {
            Id = id;
            SetCalculator(TimeAndCostCalculator.FindCalculator(CalcMethod));
        }

        /// <summary>
        /// Параметрический констуктор класса Courier
        /// </summary>
        /// <param name="id">ID курьера</param>
        /// <param name="courierBase">Базовый класс курьера</param>
        public Courier(int id, CourierBase courierBase) : base(courierBase)
        {
            SetCalculator(courierBase.Calculator);
        }

        /// <summary>
        /// Проверка возможности доставки нескольких заказов
        /// и подсчет резерва времени доставки и её стоимости
        /// </summary>
        /// <param name="calcTime">Время, в которое происходит расчет (время модели).
        /// </param>
        /// <param name="fromShop">Магазин, из которого осуществляется отгрузка</param>
        /// <param name="orders">Отгружаемые заказы</param>
        /// <param name="orderGeoIndex">Гео-индексы заказов и магазина (сначала заказы в порядке следования; последний - магазин)</param>
        /// <param name="isLoop">Флаг возарата в магазин после завершения доставки</param>
        /// <param name="geoData">Расстояние и время движения между точками</param>
        /// <param name="deliveryInfo">Информация об отгрузке</param>
        /// <returns>0 - отгрузка может быть осуществлена; иначе - отгрузка в срок невозможна</returns>
        public int DeliveryCheck(DateTime calcTime, Shop fromShop, Order[] orders, int[] orderGeoIndex, int orderCount, bool isLoop, Point[,] geoData, out CourierDeliveryInfo deliveryInfo)
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
                if (orderCount <= 0)
                    return rc;
                if (orders == null || orders.Length <= 0 || orderCount > orders.Length)
                    return rc;
                if (orderGeoIndex == null ||
                    orderGeoIndex.Length != orders.Length + 1)
                    return rc;
                if (geoData == null || geoData.Length <= 0)
                    return rc;
                //int locCount = geoData.GetLength(0);
                if (geoData.GetLength(1) != geoData.GetLength(0))
                    return rc;

                // 3. Извлекаем расстояния, время между узлами пути и подсчитываем общий вес
                rc = 3;
                double weight = 0;
                //int orderCount = orderGeoIndex.Length - 1;
                Point[] nodeInfo = new Point[orderCount + 2];
                int shopLocIndex = orderGeoIndex[orderCount];
                int prevLocIndex = shopLocIndex;

                for (int i = 0; i < orderCount; i++)
                {
                    Order order = orders[i];
                    if (order.Weight > MaxOrderWeight)
                        return rc;

                    weight += order.Weight;

                    int orderLocIndex = orderGeoIndex[i];
                    nodeInfo[i + 1] = geoData[prevLocIndex, orderLocIndex];
                    prevLocIndex = orderLocIndex;
                }

                if (weight > MaxWeight)
                    return rc;

                nodeInfo[nodeInfo.Length - 1] = geoData[prevLocIndex, shopLocIndex];

                // 4. Подсчитываем время и стоимость доставки
                rc = 4;
                double[] nodeDeliveryTime;
                double deliveryTime;
                double executionTime;
                double cost;
                int rc1 = GetTimeAndCost(nodeInfo, weight, isLoop, out nodeDeliveryTime, out deliveryTime, out executionTime, out cost);
                if (rc1 != 0)
                    return rc = 100 * rc + rc1;

                // 5. Проверяем доставку вовремя и стороим интервал начала доставки
                rc = 5;
                DateTime intervalStart = calcTime;
                DateTime intervalEnd = DateTime.MaxValue;

                for (int i = 0; i < orderCount; i++)
                {
                    Order order = orders[i];
                    if (order.TimeCheckDisabled)
                        continue;
                    double dt = nodeDeliveryTime[i + 1];
                    DateTime minTime = order.DeliveryTimeFrom.AddMinutes(-dt);
                    DateTime maxTime = order.DeliveryTimeTo.AddMinutes(-dt);
                    if (calcTime <= minTime)
                    { }
                    else if (calcTime < maxTime)
                    {
                        minTime = calcTime;
                    }
                    else
                    {
                        return rc;
                    }

                    //if (currentModelTime < minTime || currentModelTime > maxTime)
                    //    return rc;

                    if (minTime > intervalStart)
                        intervalStart = minTime;
                    if (maxTime < intervalEnd)
                        intervalEnd = maxTime;
                    if (minTime > maxTime)
                        return rc;
                }

                if (intervalEnd == DateTime.MaxValue)
                    intervalEnd = intervalStart.AddHours(2);

                // 6. Создаём результат
                rc = 6;
                Order[] deliveryOrders = new Order[orderCount];
                Array.Copy(orders, deliveryOrders, orderCount);
                deliveryInfo = new CourierDeliveryInfo(this, fromShop, deliveryOrders, calcTime, isLoop);
                deliveryInfo.FromShop = fromShop;
                deliveryInfo.NodeInfo = nodeInfo;
                deliveryInfo.Cost = cost;
                deliveryInfo.DeliveryTime = deliveryTime;
                deliveryInfo.ExecutionTime = executionTime;
                deliveryInfo.ReserveTime = intervalEnd - calcTime;
                deliveryInfo.StartDeliveryInterval = intervalStart;
                deliveryInfo.EndDeliveryInterval = intervalEnd;
                deliveryInfo.NodeDeliveryTime = nodeDeliveryTime;

                // 7. Выход - Ok
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
