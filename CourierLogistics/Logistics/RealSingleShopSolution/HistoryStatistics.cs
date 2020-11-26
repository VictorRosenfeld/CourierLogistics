
namespace CourierLogistics.Logistics.RealSingleShopSolution
{
    using System;
    using System.Linq;

    /// <summary>
    /// Статистика отгрузок (DH-статистика)
    /// </summary>
    public class HistoryStatistics
    {
        /// <summary>
        /// Отсортированные по ключу (shopId.delivery_start) записи
        /// </summary>
        private readonly DeliveryHistory[] statistics;

        /// <summary>
        /// Отсортированные ключи записей statistics
        /// </summary>
        private readonly string[] keys;

        /// <summary>
        /// Рабочий массив для отбираемвх записей
        /// </summary>
        private DeliveryHistory[] selectedRecords;

        /// <summary>
        /// Счетчик отобранных записей
        /// </summary>
        private int selectedRecordCount;

        /// <summary>
        /// Параметрический конструктор класса HistoryStatistics
        /// </summary>
        /// <param name="deliveryHistory">Отгрузки</param>
        public HistoryStatistics(DeliveryHistory[] deliveryHistory)
        {
            selectedRecords = new DeliveryHistory[1000];
            statistics = deliveryHistory;
            if (statistics != null)
            {
                keys = statistics.Select(rec => rec.Key).ToArray();
                Array.Sort(keys, statistics);
            }
            else
            {
                keys = new string[0];
                statistics = new DeliveryHistory[0];
            }
        }

        /// <summary>
        /// Выбор статистики отгрузок за день в одном магазине
        /// </summary>
        /// <param name="shopId">Id магазина</param>
        /// <param name="date">День</param>
        /// <returns>Найденная статистика</returns>
        public DeliveryHistory[] SelectShopDayStatistics(int shopId, DateTime date)
        {
            // 1. Инициализация
            selectedRecordCount = 0;

            try
            {
                // 3. Находим первую запись для магазина shopId за день day
                DateTime day = date.Date;
                string key = DeliveryHistory.GetKey(shopId, day);
                int index = Array.BinarySearch(keys, key);
                if (index < 0) index = -index;

                // 4. Выбираем все записи, относящиеся к заданному дню
                for (int i = index; i < statistics.Length; i++)
                {
                    DeliveryHistory record = statistics[i];
                    if (record.ShopId != shopId ||
                        record.DeliveryStart.Date != day)
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
