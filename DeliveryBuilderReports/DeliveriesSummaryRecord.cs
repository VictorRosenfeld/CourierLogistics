
namespace DeliveryBuilderReports
{
    /// <summary>
    /// Запись Deliveries Summary
    /// </summary>
    public class DeliveriesSummaryRecord
    {
        /// <summary>
        /// Количество заказов в отгрузке
        /// </summary>
        public int Level { get; private set; }

        /// <summary>
        /// Общее число отгрузок уровня Level
        /// </summary>
        public int Count { get; private set; }

        /// <summary>
        /// Общая стоимость отгрузок уровня Level
        /// </summary>
        public double Cost { get; private set; }

        /// <summary>
        /// Общее число заказов в отгрузках уровня Level
        /// </summary>
        public int OrderCount { get; private set; }

        /// <summary>
        /// Средняя стоимость доставки одного заказа по всем отгрузкам уровня Level
        /// </summary>
        public double AvgCost => Cost / OrderCount;

        ///// <summary>
        ///// Удельный вес стоимости всех отгрузок уровня Level по отношению к стоимости всех отгрузок
        ///// </summary>
        //public double CostPercent { get; set; }

        ///// <summary>
        ///// Удельный вес общего числа заказов в отгрузках уровня Level по отношению к общему числу заказов во всех отгрузках
        ///// </summary>
        //public double OrdersPercent { get; set; }

        /// <summary>
        /// Параметрический конструктор класса DeliveriesSummaryRecord
        /// </summary>
        /// <param name="orderCount">Число заказов в первой отгрузке</param>
        /// <param name="cost">Стоимость первой отгрузки</param>
        public DeliveriesSummaryRecord(int orderCount, double cost)
        {
            Level = orderCount;
            OrderCount = orderCount;
            Cost = cost;
            Count = 1;
        }

        /// <summary>
        /// Добавление отгрузки уровня Level
        /// </summary>
        /// <param name="cost"></param>
        public void AddDelivery(double cost)
        {
            Count++;
            Cost += cost;
            OrderCount += Level;
        }
    }
}
