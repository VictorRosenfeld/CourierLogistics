
namespace LogAnalyzer.ReportData.OrderEvents
{
    using ClosedXML.Excel;
    using System;
    using static LogisticsService.API.GetOrderEvents;

    /// <summary>
    /// Печать события заказа
    /// </summary>
    public class PrintOrderEvents
    {
        #region Column settings

        private const int RECEIVED_COLUMN = 2;
        private const int EVENT_ID_COLUMN = 3;
        private const int ORDER_ID_COLUMN = 4;
        private const int EVENT_TIME_COLUMN = 5;
        private const int TYPE_COLUMN = 6;
        private const int SHOP_ID_COLUMN = 7;
        private const int WEIGHT_COLUMN = 8;
        private const int TIME_FROM_COLUMN = 9;
        private const int TIME_TO_COLUMN = 10;
        private const int LATITUDE_COLUMN = 11;
        private const int LONGITUDE_COLUMN = 12;
        private const int SHOP_LATITUDE_COLUMN = 13;
        private const int SHOP_LONGITUDE_COLUMN = 14;
        private const int SERVICE_SHOP_ID_FIRST_COLUMN = 15;
        private const int SERVICE_ID_FIRST_COLUMN = 16;
        private const int SERVICE_STEP = 2;

        #endregion Column settings

        /// <summary>
        /// Печать события заказа
        /// </summary>
        /// <param name="logDateTime">Время печати сообщения в лог</param>
        /// <param name="orderEvent">Событие</param>
        /// <param name="sheet">Лист книги Excel</param>
        /// <param name="row">Номер последней напечатанной строки</param>
        /// <returns>0 - событие напечатано; иначе - событие не напечатано</returns>
        public static int Print(DateTime logDateTime, OrderEvent orderEvent, IXLWorksheet sheet, ref int row)
        {
            // 1. Инициализация
            int rc = 1;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (orderEvent == null)
                    return rc;
                if (sheet == null)
                    return rc;
                if (row <= 0)
                    row = 1;

                // 3. Печать события
                rc = 3;
                int r = row + 1;
                sheet.Cell(r, RECEIVED_COLUMN).SetValue(logDateTime);
                sheet.Cell(r, EVENT_ID_COLUMN).SetValue(orderEvent.id);
                sheet.Cell(r, ORDER_ID_COLUMN).SetValue(orderEvent.order_id);
                sheet.Cell(r, EVENT_TIME_COLUMN).SetValue(orderEvent.date_event);
                sheet.Cell(r, TYPE_COLUMN).SetValue(orderEvent.type);
                sheet.Cell(r, SHOP_ID_COLUMN).SetValue(orderEvent.shop_id);
                sheet.Cell(r, WEIGHT_COLUMN).SetValue(orderEvent.weight);
                sheet.Cell(r, TIME_FROM_COLUMN).SetValue(orderEvent.delivery_frame_from);
                sheet.Cell(r, TIME_TO_COLUMN).SetValue(orderEvent.delivery_frame_to);
                sheet.Cell(r, LATITUDE_COLUMN).SetValue(orderEvent.geo_lat);
                sheet.Cell(r, LONGITUDE_COLUMN).SetValue(orderEvent.geo_lon);
                sheet.Cell(r, SHOP_LATITUDE_COLUMN).SetValue(orderEvent.shop_geo_lat);
                sheet.Cell(r, SHOP_LONGITUDE_COLUMN).SetValue(orderEvent.shop_geo_lon);

                if (orderEvent.service_available != null && orderEvent.service_available.Length > 0)
                {
                    int j = SERVICE_SHOP_ID_FIRST_COLUMN;
                    foreach (var serv in orderEvent.service_available)
                    {
                        sheet.Cell(r, j).SetValue(serv.shop_id);
                        sheet.Cell(r, j + 1).SetValue(serv.dservice_id);
                        j += SERVICE_STEP;
                    }
                }

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
        /// Печать событий заказов
        /// </summary>
        /// <param name="logDateTime">Время печати сообщения с событиями в лог</param>
        /// <param name="events">События</param>
        /// <param name="sheet">Лист книги Excel</param>
        /// <param name="row">Номер последней напечатанной строки</param>
        /// <returns>0 - события напечатаны; иначе - события не напечатана</returns>
        public static int Print(DateTime logDateTime, OrderEvent[] events, IXLWorksheet sheet, ref int row)
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

                foreach (OrderEvent orderEvent in events)
                {
                    int rc1 = Print(logDateTime, orderEvent, sheet, ref row);
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
