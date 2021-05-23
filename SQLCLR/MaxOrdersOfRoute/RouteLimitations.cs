
namespace SQLCLR.MaxOrdersOfRoute
{
    using System;

    /// <summary>
    /// Ограничения на длину маршрута
    /// по числу заказов, из которых
    /// создаются отгрузки
    /// </summary>
    public class RouteLimitations
    {
        /// <summary>
        /// Записи, отсортированные по убыванию RouteLength
        /// </summary>
        private MaxOrdersOfRouteRecord[] records;

        /// <summary>
        /// Записи, отсортированные по убыванию RouteLength
        /// </summary>
        public MaxOrdersOfRouteRecord[] Records => records;

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
        public int Create(MaxOrdersOfRouteRecord[] records)
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
                if (records[i].MaxOrders >= orderCount)
                    return records[i].RouteLength;
            }

            return 1;
        }

        /// <summary>
        /// Сравнение ограничений по длине маршрута
        /// </summary>
        /// <param name="record1">Запись 1</param>
        /// <param name="record2">Запись 2</param>
        /// <returns>-1 - Запись1 больше Запись2; 0 - Запись1 = Запись2; 1 - Запись1 меньше Запись2</returns>
        private static int CompareByRouteLength(MaxOrdersOfRouteRecord record1, MaxOrdersOfRouteRecord record2)
        {
            if (record1.RouteLength > record2.RouteLength)
                return -1;
            if (record1.RouteLength < record2.RouteLength)
                return 1;
            return 0;
        }
    }
}
