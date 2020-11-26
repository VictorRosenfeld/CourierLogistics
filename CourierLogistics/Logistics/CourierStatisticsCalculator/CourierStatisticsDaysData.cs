
namespace CourierLogistics.Logistics.CourierStatisticsCalculator
{
    /// <summary>
    /// Ежедневная статистика курьера
    /// </summary>
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Ежедневная статистика курьера
    /// </summary>
    public class CourierStatisticsDaysData
    {
        /// <summary>
        /// Id курьера
        /// </summary>
        public int Id { get; private set; }

        /// <summary>
        /// Ежедневная статистика курьера
        /// </summary>
        public Dictionary<DateTime, CourierStatisticsDayData> DaysData { get; private set; }

        /// <summary>
        /// Параметрический конструктор класса CourierStatisticsDaysData
        /// </summary>
        /// <param name="id">ID курьера</param>
        public CourierStatisticsDaysData(int id)
        {
            Id = id;
            DaysData = new Dictionary<DateTime, CourierStatisticsDayData>(31);
        }

        /// <summary>
        /// Добавление заказа
        /// </summary>
        /// <param name="day">День</param>
        /// <param name="weight">Вес заказа</param>
        /// <param name="deliveryTime">Время доставки до клиента</param>
        /// <param name="executeTime">Время на исполнение заказа (время до клиента + обратная дорога)</param>
        /// <param name="cost">Стоимость доставки</param>
        public bool AddOrder(DateTime day, double weight, double deliveryTime, double executeTime, double cost)
        {
            // 1. Инициализация

            try
            {
                // 2. Извлекаем дневную статистику
                CourierStatisticsDayData dayData;
                if (!DaysData.TryGetValue(day.Date, out dayData))
                {
                    dayData = new CourierStatisticsDayData(Id, day.Date);
                    DaysData.Add(day.Date, dayData);
                }

                if (day < dayData.TimeFrom) dayData.TimeFrom = day;
                if (day > dayData.TimeTo) dayData.TimeTo = day;

                // 3. Пополняем девную статистку курьера
                dayData.AddOrder(weight, deliveryTime, executeTime, cost);

                // 4. Выход - Ok
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
