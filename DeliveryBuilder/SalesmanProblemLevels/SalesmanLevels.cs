
namespace DeliveryBuilder.SalesmanProblemLevels
{
    using DeliveryBuilder.BuilderParameters;
    using System;

    /// <summary>
    /// Ограничения на длину маршрута
    /// по числу заказов, из которых
    /// создаются отгрузки
    /// </summary>
    public class SalesmanLevels
    {
        /// <summary>
        /// Записи, отсортированные по убыванию RouteLength
        /// </summary>
        private SalesmanLevel[] records;

        /// <summary>
        /// Записи, отсортированные по убыванию RouteLength
        /// </summary>
        public SalesmanLevel[] Records => records;

        /// <summary>
        /// Число записей с ограничениями
        /// </summary>
        public int Count => (records == null ? 0 : records.Length);

        /// <summary>
        /// Флаг: true - экземпляр создан; false - экземпляр не создан
        /// </summary>
        public bool IsCreated { get; set; }

        /// <summary>
        /// Создание ограничений на длину маршрута
        /// </summary>
        /// <param name="records">Записи с ограичениями</param>
        /// <returns>0 - ограничения созданы; иначе - ограничения не созданы</returns>
        public int Create(SalesmanLevel[] records)
        {
            // 1. Инициализация
            int rc = 1;
            this.records = null;
            IsCreated = false;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (records == null || records.Length <= 0)
                    return rc;

                // 3. Сортируем записи по убыванию длины маршрута
                rc = 3;
                this.records = records;
                Array.Sort(this.records, CompareByRouteLength);

                // 4. Выход Ok
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
        /// Выбрать максимальную длину маршрута
        /// для зданного числа заказов
        /// </summary>
        /// <param name="orderCount"></param>
        /// <returns></returns>
        public int GetRouteLength(int orderCount)
        {
            if (!IsCreated)
                return 1;
            for (int i = 0; i < records.Length; i++)
            {
                if (records[i].Orders >= orderCount)
                    return records[i].Level;
            }

            return 1;
        }

        /// <summary>
        /// Выбор максимального количества заказов
        /// для заданной длины маршрута
        /// </summary>
        /// <param name="length">Длина маршрута</param>
        /// <returns>Максимальное количество заказов или -1</returns>
        public int GetRouteMaxOrders(int length)
        {
            if (!IsCreated)
                return -1;
            for (int i = 0; i < records.Length; i++)
            {
                if (records[i].Level == length)
                    return records[i].Orders;
            }

            return -1;
        }

        /// <summary>
        /// Выбрать подходящую запись
        /// для зданного числа заказов
        /// </summary>
        /// <param name="orderCount"></param>
        /// <returns>Подходящая запись или null</returns>
        public SalesmanLevel GetFittingRecord(int orderCount)
        {
            if (!IsCreated)
                return null;
            for (int i = 0; i < records.Length; i++)
            {
                if (records[i].Orders >= orderCount)
                    return records[i];
            }

            return null;
        }

        /// <summary>
        /// Сравнение ограничений по длине маршрута
        /// </summary>
        /// <param name="record1">Запись 1</param>
        /// <param name="record2">Запись 2</param>
        /// <returns>-1 - Запись1 больше Запись2; 0 - Запись1 = Запись2; 1 - Запись1 меньше Запись2</returns>
        private static int CompareByRouteLength(SalesmanLevel record1, SalesmanLevel record2)
        {
            if (record1.Level > record2.Level)
                return -1;
            if (record1.Level < record2.Level)
                return 1;
            return 0;
        }
    }
}
