
namespace CourierLogistics.Logistics.CourierStatisticsCalculator
{
    using System;
    using System.Collections.Generic;

    public class CourierStatisticsData
    {
        /// <summary>
        /// Дата первого заказа
        /// </summary>
        public DateTime DateFrom { get; private set; }

        /// <summary>
        /// Дата последнего заказа
        /// </summary>
        public DateTime DateTo { get; private set; }
         
        public Dictionary<int, CourierStatisticsDaysData> StatisticsData { get; private set; }

        /// <summary>
        /// Конструктор класса CourierStatisticsData
        /// </summary>
        public CourierStatisticsData()
        {
            StatisticsData = new Dictionary<int, CourierStatisticsDaysData>(3000);
            DateFrom = DateTime.MaxValue;
            DateTo = DateTime.MinValue;
        }

        /// <summary>
        /// Добавление заказа
        /// </summary>
        /// <param name="id">ID курьера</param>
        /// <param name="day">День</param>
        /// <param name="weight">Вес заказа</param>
        /// <param name="deliveryTime">Время доставки до клиента</param>
        /// <param name="executeTime">Время на исполнение заказа (время до клиента + обратная дорога)</param>
        /// <param name="cost">Стоимость доставки</param>
        public bool AddOrder(int id, DateTime day, double weight, double deliveryTime, double executeTime, double cost)
        {
            // 1. Инициализация

            try
            {
                // 2. Извлекаем дневную статистику
                CourierStatisticsDaysData daysData;
                if (!StatisticsData.TryGetValue(id, out daysData))
                {
                    daysData = new CourierStatisticsDaysData(id);
                    StatisticsData.Add(id, daysData);
                }

                if (day < DateFrom) DateFrom = day;
                if (day > DateTo) DateTo = day;

                // 3. Пополняем девную статистку курьера
                daysData.AddOrder(day, weight, deliveryTime, executeTime, cost);

                // 4. Выход - Ok
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Выборка всей ежедневной статистики
        /// </summary>
        /// <returns></returns>
        public CourierStatisticsDayData[] SelelectAllDailyData()
        {
            // 1. Инициализация

            try
            {
                // 2. Проверяем исходные данные
                if (StatisticsData.Count <= 0)
                    return new CourierStatisticsDayData[0];

                // 3. Подсчитываем число дневных элементов статистики
                int size = 0;
                foreach (CourierStatisticsDaysData courierDaysData in StatisticsData.Values)
                {
                    size +=  courierDaysData.DaysData.Count;
                }

                // 4. Цикл выборки всех ежедневных данных курьеров
                CourierStatisticsDayData[] allDailyData = new CourierStatisticsDayData[size];
                int count = 0;

                foreach (CourierStatisticsDaysData courierDaysData in StatisticsData.Values)
                {
                    courierDaysData.DaysData.Values.CopyTo(allDailyData, count);
                    count += courierDaysData.DaysData.Count;
                }

                // 5. Выход - Ok
                return allDailyData;
            }
            catch
            {
                return null;
            }
        }
    }
}
