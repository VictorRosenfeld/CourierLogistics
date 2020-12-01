
namespace LogAnalyzer.ReportData.ShopEvents
{
    using ClosedXML.Excel;
    using System;
    using static LogisticsService.API.GetShopEvents;

    /// <summary>
    /// Печать событий магазинов
    /// </summary>
    public class PrintShopEvents
    {
        #region Column settings

        private const int RECEIVED_COLUMN = 2;
        private const int EVENT_ID_COLUMN = 3;
        private const int SHOP_ID_COLUMN = 4;
        private const int EVENT_TIME_COLUMN = 5;
        private const int WORK_START_COLUMN = 6;
        private const int WORK_END_COLUMN = 7;
        private const int LATITUDE_COLUMN = 8;
        private const int LONGITUDE_COLUMN = 9;

        #endregion Column settings

        /// <summary>
        /// Печать события магазина
        /// </summary>
        /// <param name="logDateTime">Время печати сообщения в лог</param>
        /// <param name="shopEvent">Событие</param>
        /// <param name="sheet">Лист книги Excel</param>
        /// <param name="row">Номер последней напечатанной строки</param>
        /// <returns>0 - событие напечатано; иначе - событие не напечатано</returns>
        public static int Print(DateTime logDateTime, ShopEvent shopEvent, IXLWorksheet sheet, ref int row)
        {
            // 1. Инициализация
            int rc = 1;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (shopEvent == null)
                    return rc;
                if (sheet == null)
                    return rc;
                if (row <= 0)
                    row = 1;

                // 3. Печать события
                rc = 3;
                int r = row + 1;
                sheet.Cell(r, RECEIVED_COLUMN).SetValue(logDateTime);
                sheet.Cell(r, EVENT_ID_COLUMN).SetValue(shopEvent.id);
                sheet.Cell(r, SHOP_ID_COLUMN).SetValue(shopEvent.shop_id);
                sheet.Cell(r, EVENT_TIME_COLUMN).SetValue(shopEvent.date_event);
                sheet.Cell(r, WORK_START_COLUMN).SetValue(shopEvent.work_start);
                sheet.Cell(r, WORK_END_COLUMN).SetValue(shopEvent.work_end);
                sheet.Cell(r, LATITUDE_COLUMN).SetValue(shopEvent.geo_lat);
                sheet.Cell(r, LONGITUDE_COLUMN).SetValue(shopEvent.geo_lon);

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
        /// Печать событий магазинов
        /// </summary>
        /// <param name="logDateTime">Время печати сообщения с событиями в лог</param>
        /// <param name="events">События</param>
        /// <param name="sheet">Лист книги Excel</param>
        /// <param name="row">Номер последней напечатанной строки</param>
        /// <returns>0 - события напечатаны; иначе - события не напечатана</returns>
        public static int Print(DateTime logDateTime, ShopEvent[] events, IXLWorksheet sheet, ref int row)
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

                foreach (ShopEvent shopEvent in events)
                {
                    int rc1 = Print(logDateTime, shopEvent, sheet, ref row);
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
