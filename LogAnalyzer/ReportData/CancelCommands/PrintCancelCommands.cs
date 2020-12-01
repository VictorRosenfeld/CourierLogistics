
namespace LogAnalyzer.ReportData.CancelCommands
{
    using ClosedXML.Excel;
    using System;
    using static LogisticsService.API.BeginShipment;

    /// <summary>
    /// Печать команд отмены
    /// </summary>
    public class PrintCancelCommands
    {
        #region Column settings

        private const int SENT_COLUMN = 2;
        private const int GUID_COLUMN = 3;
        private const int STATUS_COLUMN = 4;
        private const int ORDER_ID_COLUMN = 5;
        private const int SHOP_ID_COLUMN = 6;
        private const int DSERV_ID_COLUMN = 7;
        private const int COURIER_ID_COLUMN = 8;
        private const int DATE_TARGET_COLUMN = 9;
        private const int DATE_TARGET_END_COLUMN = 10;
        private const int CALCULATION_TIME_COLUMN = 11;
        private const int WEIGHT_COLUMN = 12;

        // Available deliveries
        private const int TYPE1_COLUMN = 13;
        private const int REASON1_COLUMN = 14;
        private const int DELIVERY_TIME1_COLUMN = 15;

        private const int TYPE2_COLUMN = 16;
        private const int REASON2_COLUMN = 17;
        private const int DELIVERY_TIME2_COLUMN = 18;

        private const int TYPE3_COLUMN = 19;
        private const int REASON3_COLUMN = 20;
        private const int DELIVERY_TIME3_COLUMN = 21;

        private const int TYPE4_COLUMN = 22;
        private const int REASON4_COLUMN = 23;
        private const int DELIVERY_TIME4_COLUMN = 24;

        private const int TYPE5_COLUMN = 25;
        private const int REASON5_COLUMN = 26;
        private const int DELIVERY_TIME5_COLUMN = 27;

        #endregion Column settings

        /// <summary>
        /// Печать команды отмены
        /// </summary>
        /// <param name="logDateTime">Время печати сообщения в лог</param>
        /// <param name="rejectOrder">Команда отмены</param>
        /// <param name="sheet">Лист книги Excel</param>
        /// <param name="row">Номер последней напечатанной строки</param>
        /// <returns>0 - команда отмены напечатана; иначе - команда отмены не напечатана</returns>
        public static int Print(DateTime logDateTime, RejectedOrder rejectOrder, IXLWorksheet sheet, ref int row)
        {
            // 1. Инициализация
            int rc = 1;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (rejectOrder == null)
                    return rc;
                if (sheet == null)
                    return rc;
                if (row <= 0)
                    row = 1;

                // 3. Печать события
                rc = 3;
                int r = row + 1;
                sheet.Cell(r, SENT_COLUMN).SetValue(logDateTime);
                sheet.Cell(r, GUID_COLUMN).SetValue(rejectOrder.id);
                sheet.Cell(r, STATUS_COLUMN).SetValue(rejectOrder.status);
                if (rejectOrder.Count > 0)
                    sheet.Cell(r, ORDER_ID_COLUMN).SetValue(rejectOrder.orders[0]);
                sheet.Cell(r, SHOP_ID_COLUMN).SetValue(rejectOrder.shop_id);
                sheet.Cell(r, DSERV_ID_COLUMN).SetValue(rejectOrder.delivery_service_id);
                sheet.Cell(r, COURIER_ID_COLUMN).SetValue(rejectOrder.courier_id);
                sheet.Cell(r, DATE_TARGET_COLUMN).SetValue(rejectOrder.date_target);
                sheet.Cell(r, DATE_TARGET_END_COLUMN).SetValue(rejectOrder.date_target_end);
                sheet.Cell(r, CALCULATION_TIME_COLUMN).SetValue(rejectOrder.info.calculationTime);
                sheet.Cell(r, WEIGHT_COLUMN).SetValue(rejectOrder.info.weight);

                if (rejectOrder.info.delivery_method != null && rejectOrder.info.delivery_method.Length > 0)
                {
                    // OrderID
                    int j = TYPE1_COLUMN;
                    for (int i = 0; i < rejectOrder.info.delivery_method.Length; i++, j += 3)
                    {
                        RejectedInfoItem item = rejectOrder.info.delivery_method[i];
                        sheet.Cell(r, j).SetValue(item.delivery_type);
                        sheet.Cell(r, j + 1).SetValue(item.rejection_reason);
                        sheet.Cell(r, j + 2).SetValue(item.delivery_time);
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
        /// Печать команд отмены
        /// </summary>
        /// <param name="logDateTime">Время печати сообщения в лог</param>
        /// <param name="rejectOrders">Команды отмены</param>
        /// <param name="sheet">Лист книги Excel</param>
        /// <param name="row">Номер последней напечатанной строки</param>
        /// <returns>0 - команды отмены напечатаны; иначе - команды отмены не напечатаны</returns>
        public static int Print(DateTime logDateTime, RejectedOrder[] rejectOrders, IXLWorksheet sheet, ref int row)
        {
            // 1. Инициализация
            int rc = 1;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (rejectOrders == null || rejectOrders.Length <= 0)
                    return rc;
                if (sheet == null)
                    return rc;
                if (row <= 0)
                    row = 1;

                // 3. Печатаем команды
                rc = 3;
                bool Ok = true;

                foreach (RejectedOrder rejectOrder in rejectOrders)
                {
                    int rc1 = Print(logDateTime, rejectOrder, sheet, ref row);
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
