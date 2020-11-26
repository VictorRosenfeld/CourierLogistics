
namespace CourierLogistics.Logistics.CourierStatisticsCalculator
{
    using CourierLogistics.SourceData.Orders;
    using System;

    /// <summary>
    /// Сбор ежедневной статистики по курьерам
    /// </summary>
    public class CourierStatistics
    {
        /// <summary>
        /// Ежедневная статистика по курьерам
        /// </summary>
        public CourierStatisticsData StatisticsData;

        /// <summary>
        /// Флаг: true - статистика собрана; false - статистика не собрана
        /// </summary>
        public bool IsCreated { get; private set; }

        /// <summary>
        /// Прерывание при последнем вызове Create
        /// </summary>
        public Exception LastException  { get; private set; }

        /// <summary>
        /// Сбор ежедневной статистики по курьерам
        /// </summary>
        /// <param name="orders">Исходные заказы</param>
        /// <returns>0 - статистика собрана; иначе - статистика не собрана</returns>
        public int Create(AllOrders orders)
        {
            // 1. Инициалзация
            int rc = 1;
            IsCreated = false;
            LastException = null;
            StatisticsData = new CourierStatisticsData();

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (orders == null || orders.Orders.Count <= 0)
                    return rc;

                // 3. Цикл сбора статистики
                rc = 3;
                bool isOk = true;

                foreach (Order order in orders.Orders.Values)
                {
                    // 3.1 Courier ID
                    int courierId = order.Id_courier;

                    // 3.2 Day
                    DateTime day = order.Date_delivery_start;
                    if (day == DateTime.MinValue)
                        continue;

                    // 3.3 Weight
                    double weight = order.Weight;

                    // 3.4 Start Delivery
                    DateTime startDelivery = order.Date_delivery_start;
                    if (startDelivery == DateTime.MinValue)
                        continue;

                    // 3.5 End Delivery
                    DateTime endDelivery = order.Date_delivered;
                    if (endDelivery == DateTime.MinValue)
                        continue;

                    // 3.6 Delivery time
                    double deliveryTime = (endDelivery - startDelivery).TotalMinutes;

                    // 3.7 Execute time
                    double executeTime = 2 * deliveryTime;

                    // 3.8 Пополняем статистику
                    if (!StatisticsData.AddOrder(courierId, day, weight, deliveryTime, executeTime, 0))
                        isOk = false;
                }

                if (!isOk)
                    return rc = 31;

                // 4. Выход - Ok
                rc = 0;
                IsCreated = true;
                return rc;
            }
            catch (Exception ex)
            {
                LastException =ex;
                return rc;
            }
        }
    }
}
