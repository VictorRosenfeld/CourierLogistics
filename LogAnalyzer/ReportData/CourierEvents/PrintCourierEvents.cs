
namespace LogAnalyzer.ReportData.CourierEvents
{
    using ClosedXML.Excel;
    using System;
    using LogisticsService.API;

    /// <summary>
    /// Печать событий курьеров
    /// </summary>
    public class PrintCourierEvents
    {
        #region Column settings

        private const int RECEIVED_COLUMN = 2;
        private const int EVENT_ID_COLUMN = 3;
        private const int COURIER_ID_COLUMN = 4;
        private const int EVENT_TIME_COLUMN = 5;
        private const int COURIER_TYPE_COLUMN = 6;
        private const int SHOP_ID_COLUMN = 7;
        private const int TYPE_COLUMN = 8;
        private const int WORK_START_COLUMN = 9;
        private const int WORK_END_COLUMN = 10;
        private const int LATITUDE_COLUMN = 11;
        private const int LONGITUDE_COLUMN = 12;

        #endregion Column settings

        /// <summary>
        /// Печать события курьера
        /// </summary>
        /// <param name="logDateTime">Время печати сообщения в лог</param>
        /// <param name="courierEvent">Событие</param>
        /// <param name="sheet">Лист книги Excel</param>
        /// <param name="row">Номер последней напечатанной строки</param>
        /// <returns>0 - событие напечатано; иначе - событие не напечатано</returns>
        public static int Print(DateTime logDateTime, CourierEvent courierEvent, IXLWorksheet sheet, ref int row)
        {
            // 1. Инициализация
            int rc = 1;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (courierEvent == null)
                    return rc;
                if (sheet == null)
                    return rc;
                if (row <= 0)
                    row = 1;

                // 3. Печать события
                rc = 3;
                int r = row + 1;
                sheet.Cell(r, RECEIVED_COLUMN).SetValue(logDateTime);
                sheet.Cell(r, EVENT_ID_COLUMN).SetValue(courierEvent.id);
                sheet.Cell(r, COURIER_ID_COLUMN).SetValue(courierEvent.courier_id);
                sheet.Cell(r, EVENT_TIME_COLUMN).SetValue(courierEvent.date_event);
                sheet.Cell(r, COURIER_TYPE_COLUMN).SetValue(courierEvent.courier_type);
                sheet.Cell(r, SHOP_ID_COLUMN).SetValue(courierEvent.shop_id);
                sheet.Cell(r, TYPE_COLUMN).SetValue(courierEvent.type);
                sheet.Cell(r, WORK_START_COLUMN).SetValue(courierEvent.work_start);
                sheet.Cell(r, WORK_END_COLUMN).SetValue(courierEvent.work_end);
                sheet.Cell(r, LATITUDE_COLUMN).SetValue(courierEvent.geo_lat);
                sheet.Cell(r, LONGITUDE_COLUMN).SetValue(courierEvent.geo_lon);

                row = r;

                // 4. Выход - Ok
                rc = 0;
                return rc;
            }
            catch
            {
                return rc;
            }
        }

        /// <summary>
        /// Печать событий курьров
        /// </summary>
        /// <param name="logDateTime">Время печати сообщения с событиями в лог</param>
        /// <param name="events">События</param>
        /// <param name="sheet">Лист книги Excel</param>
        /// <param name="row">Номер последней напечатанной строки</param>
        /// <returns>0 - события напечатаны; иначе - события не напечатана</returns>
        public static int Print(DateTime logDateTime, CourierEvent[] events, IXLWorksheet sheet, ref int row)
        {
            // 1. Инициализация
            int rc = 1;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (events == null || events.Length <= 0)
                    return rc;
                if (sheet == null)
                    return rc;
                if (row <= 0)
                    row = 1;

                // 3. Печатаем события
                rc = 3;
                bool Ok = true;

                foreach (CourierEvent courierEvent in events)
                {
                    int rc1 = Print(logDateTime, courierEvent, sheet, ref row);
                    if (rc1 != 0)
                        Ok = false;
                }

                if (!Ok)
                    return rc;

                // 4. Выход - Ok
                rc = 0;
                return rc;
            }
            catch
            {
                return rc;
            }
        }
    }
}
