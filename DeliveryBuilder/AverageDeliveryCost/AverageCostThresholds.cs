
namespace DeliveryBuilder.AverageDeliveryCost
{
    using DeliveryBuilder.Db;
    using System;

    /// <summary>
    /// Словарь порогов для средней стоимости
    /// доставки одного закааза
    /// </summary>
    public class AverageCostThresholds
    {
        /// <summary>
        /// Пороги
        /// </summary>
        private double[] thresholds;

        /// <summary>
        /// Ключи порогов
        /// </summary>
        private long[] keys;

        /// <summary>
        /// Пороги
        /// </summary>
        public double[] Thresholds => thresholds;

        /// <summary>
        /// Ключи порогов
        /// </summary>
        public long[] Keys => keys;

        /// <summary>
        /// Флаг: true - словарь создан; false - словарь не создан
        /// </summary>
        public bool IsCreated { get; private set; }

        /// <summary>
        /// Построение словаря порогов
        /// </summary>
        /// <param name="records">Записи порогов</param>
        /// <returns>0 - словарь построен; иначе - словарь не построен</returns>
        public int Create(AverageDeliveryCostRecord[] records)
        {
            // 1. Иниуциализация
            int rc = 1;
            IsCreated = false;
            thresholds = null;
            keys = null;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (records == null /*|| records.Length <= 0*/)
                    return rc;

                // 3. Строим словарь порогов
                rc = 3;
                thresholds = new double[records.Length];
                keys = new long[records.Length];

                for (int i = 0; i < records.Length; i++)
                {
                    AverageDeliveryCostRecord record = records[i];
                    thresholds[i] = record.Cost;
                    keys[i] = GetKey(record.VehicleId, record.ShopId);
                }

                Array.Sort(keys, thresholds);

                // 4. Выход - Ok
                rc = 0;
                IsCreated = true;
                return rc;
            }
            catch
            {
                return rc;
            }
        }

        /// <summary>
        /// Выбор порога для выбранных
        /// магазина и способа доставки
        /// </summary>
        /// <param name="vehicleId">ID способа доставки</param>
        /// <param name="shopId">ID магазина</param>
        /// <returns>Порог или 0</returns>
        public double GetThreshold(int vehicleId, int shopId)
        {
            if (!IsCreated)
                return 0;
            int index = Array.BinarySearch(keys, GetKey(vehicleId, shopId));
            if (index < 0)
                return 0;
            return thresholds[index];
        }

        /// <summary>
        /// Построение ключа записи
        /// </summary>
        /// <param name="vehicleId"></param>
        /// <param name="shopId"></param>
        /// <returns></returns>
        private static long GetKey(int vehicleId, int shopId)
        {
            return ((long)vehicleId) << 32 | (shopId & 0x00000000FFFFFFFF);
        }
    }
}
