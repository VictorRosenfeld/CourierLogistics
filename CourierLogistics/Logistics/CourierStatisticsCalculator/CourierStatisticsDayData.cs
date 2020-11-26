
namespace CourierLogistics.Logistics.CourierStatisticsCalculator
{
    using System;

    /// <summary>
    /// Дневная статистика курьера
    /// </summary>
    public class CourierStatisticsDayData
    {
        /// <summary>
        /// Id курьера
        /// </summary>
        public int Id { get; private set; } 

        /// <summary>
        /// День
        /// </summary>
        public DateTime Day { get; private set; }

        /// <summary>
        /// Время первого заказа за этот день
        /// </summary>
        public DateTime TimeFrom { get; set; }

        /// <summary>
        /// Время последнего заказа за этот день
        /// </summary>
        public DateTime TimeTo { get; set; }

        /// <summary>
        /// Общее число заказов за день
        /// </summary>
        public int OrderCount { get; private set; }

        /// <summary>
        /// Общее вес заказов за день
        /// </summary>
        public double TotalWeight { get; private set; }

        /// <summary>
        /// Общее время доставки всех заказов за день
        /// </summary>
        public double TotalDeliveryTime { get; private set; }

        /// <summary>
        /// Общее время потраченное на доставку (время доставки + время обратной дороги)
        /// </summary>
        public double TotalExecuteTime { get; private set; }

        /// <summary>
        /// Общая стоимость доставки заказов за день
        /// </summary>
        public double TotalCost { get; private set; }

        /// <summary>
        /// Параметрический конструктор класса CourierStatisticsDayData
        /// </summary>
        /// <param name="id">ID курьера</param>
        /// <param name="day">день</param>
        public CourierStatisticsDayData(int id, DateTime day)
        {
            Id = id;
            Day = day;
            TimeFrom = day.Date.AddHours(24);
            TimeTo = day.Date;
        }

        /// <summary>
        /// Добавление заказа
        /// </summary>
        /// <param name="weight">Вес заказа</param>
        /// <param name="deliveryTime">Время доставки до клиента</param>
        /// <param name="executeTime">Время на исполнение заказа (время до клиента + обратная дорога)</param>
        /// <param name="cost">Стоимость доставки</param>
        public void AddOrder(double weight, double deliveryTime, double executeTime, double cost)
        {
            OrderCount++;
            TotalWeight += weight;
            TotalDeliveryTime += deliveryTime;
            TotalExecuteTime += executeTime;
            TotalCost += cost;
        }
    }
}
