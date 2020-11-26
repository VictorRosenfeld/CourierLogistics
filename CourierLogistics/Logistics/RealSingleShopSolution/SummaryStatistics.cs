
namespace CourierLogistics.Logistics.RealSingleShopSolution
{
    using CourierLogistics.Logistics.OptimalSingleShopSolution;
    using System;
    using System.Linq;

    /// <summary>
    /// Courier Summary статистика (в пределах года)
    /// </summary>
    public class SummaryStatistics
    {
        /// <summary>
        /// Отсортированные по ключу (shopId.date) записи
        /// </summary>
        private readonly ShopCourierStatistics[] statistics;

        /// <summary>
        /// Отсортированные ключи записей statistics
        /// </summary>
        private readonly string[] keys;

        /// <summary>
        /// Рабочий массив для отбираемых записей
        /// </summary>
        private readonly ShopCourierStatistics[] selectedRecords;

        /// <summary>
        /// Количество отобранных записей
        /// </summary>
        private int selectedRecordCount;

        /// <summary>
        /// Отсортированные по ключу (shopId.date.courierId) записи
        /// </summary>
        public ShopCourierStatistics[] Statistics => statistics;

        /// <summary>
        /// Параметрический конструктор класса CourierStatistics
        /// </summary>
        /// <param name="courierStatistics"></param>
        public SummaryStatistics(ShopCourierStatistics[] courierStatistics)
        {
            selectedRecords = new ShopCourierStatistics[350];
            statistics = courierStatistics;
            if (statistics != null)
            {
                keys = statistics.Select(rec => rec.Key).ToArray();
                Array.Sort(keys, statistics);
            }
            else
            {
                keys = new string[0];
                statistics = new ShopCourierStatistics[0];
            }
        }

        /// <summary>
        /// Выбор статистики магазина за день
        /// </summary>
        /// <param name="shopId">Id магазина</param>
        /// <param name="date">День</param>
        /// <returns>Найденная статистика</returns>
        public ShopCourierStatistics[] SelectShopDayStatistics(int shopId, DateTime date)
        {
            // 1. Инициализация
            selectedRecordCount = 0;

            try
            {
                // 3. Находим первую запись для магазина shopId за день day
                int courierId = 1;
                string key = ShopCourierStatistics.GetKey(shopId, date, courierId);
                int index = Array.BinarySearch(keys, key);

                if (index < 0)
                {
                    courierId++;
                    key = ShopCourierStatistics.GetKey(shopId, date, courierId);
                    index = Array.BinarySearch(keys, key);

                    if (index < 0)
                    {
                        courierId++;
                        key = ShopCourierStatistics.GetKey(shopId, date, courierId);
                        index = Array.BinarySearch(keys, key);
                        if (index < 0)
                            return new ShopCourierStatistics[0];
                    }
                }

                // 4. Выбираем все записи, относящиеся к заданному дню
                int day = date.DayOfYear;
                selectedRecords[selectedRecordCount++] = statistics[index];

                for (int i = index + 1; i < statistics.Length; i++)
                {
                    ShopCourierStatistics record = statistics[i];
                    if (record.ShopId != shopId ||
                        record.Date.DayOfYear != day)
                        break;
                    selectedRecords[selectedRecordCount++] = record;
                }

                // 5. Выход
                return selectedRecords.Take(selectedRecordCount).ToArray();
            }
            catch
            {
                return null;
            }
        }
    }
}
