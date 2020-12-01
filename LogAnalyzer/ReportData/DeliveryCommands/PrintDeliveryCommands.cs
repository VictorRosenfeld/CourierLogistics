
namespace LogAnalyzer.ReportData.DeliveryCommands
{
    using ClosedXML.Excel;
    using System;
    using static LogisticsService.API.BeginShipment;

    /// <summary>
    /// Печать команд отгрузки и рекомендаций
    /// </summary>
    public class PrintDeliveryCommands
    {
        #region Column settings

        private const int SENT_COLUMN = 2;
        private const int GUID_COLUMN = 3;
        private const int STATUS_COLUMN = 4;
        private const int SHOP_ID_COLUMN = 5;
        private const int DSERV_ID_COLUMN = 6;
        private const int COURIER_ID_COLUMN = 7;
        private const int DATE_TARGET_COLUMN = 8;
        private const int DATE_TARGET_END_COLUMN = 9;
        private const int CALCULATION_TIME_COLUMN = 10;
        private const int TOTAL_COST_COLUMN = 11;
        private const int ORDER_COST_COLUMN = 12;
        private const int TOTAL_WEIGHT_COLUMN = 13;
        private const int IS_LOOP_COLUMN = 14;
        private const int START_DELIVERY_INTERVAL_COLUMN = 15;
        private const int END_DELIVERY_INTERVAL_COLUMN = 16;
        private const int RESERVE_TIME_COLUMN = 17;
        private const int TOTAL_DELIVERY_TIME_COLUMN = 18;
        private const int TOTAL_EXEC_TIME_COLUMN = 19;
        private const int ORDER_COUNT_COLUMN = 20;

        // OrderID
        private const int ORDER1_COLUMN = 21;
        private const int ORDER2_COLUMN = 22;
        private const int ORDER3_COLUMN = 23;
        private const int ORDER4_COLUMN = 24;
        private const int ORDER5_COLUMN = 25;
        private const int ORDER6_COLUMN = 26;
        private const int ORDER7_COLUMN = 27;
        private const int ORDER8_COLUMN = 28;

        // Delivery time
        private const int SHOP1_DELIVERY_TIME_COLUMN = 29;
        private const int ORDER1_DELIVERY_TIME_COLUMN = 30;
        private const int ORDER2_DELIVERY_TIME_COLUMN = 31;
        private const int ORDER3_DELIVERY_TIME_COLUMN = 32;
        private const int ORDER4_DELIVERY_TIME_COLUMN = 33;
        private const int ORDER5_DELIVERY_TIME_COLUMN = 34;
        private const int ORDER6_DELIVERY_TIME_COLUMN = 35;
        private const int ORDER7_DELIVERY_TIME_COLUMN = 36;
        private const int ORDER8_DELIVERY_TIME_COLUMN = 37;
        private const int SHOP2_DELIVERY_TIME_COLUMN = 38;

        // Distance & time
        private const int DIST1_COLUMN = 39;
        private const int TIME1_COLUMN = 40;
        private const int DIST2_COLUMN = 41;
        private const int TIME2_COLUMN = 42;
        private const int DIST3_COLUMN = 43;
        private const int TIME3_COLUMN = 44;
        private const int DIST4_COLUMN = 45;
        private const int TIME4_COLUMN = 46;
        private const int DIST5_COLUMN = 47;
        private const int TIME5_COLUMN = 48;
        private const int DIST6_COLUMN = 49;
        private const int TIME6_COLUMN = 50;
        private const int DIST7_COLUMN = 51;
        private const int TIME7_COLUMN = 52;
        private const int DIST8_COLUMN = 53;
        private const int TIME8_COLUMN = 54;
        private const int DISTS_COLUMN = 55;
        private const int TIMES_COLUMN = 56;

        #endregion Column settings

        /// <summary>
        /// Печать отгрузки или рекомендации
        /// </summary>
        /// <param name="logDateTime">Время печати сообщения в лог</param>
        /// <param name="shipment">Отгрузка или рекомендация</param>
        /// <param name="sheet">Лист книги Excel</param>
        /// <param name="row">Номер последней напечатанной строки</param>
        /// <returns>0 - отгрузка/рекомендация напечатана; иначе - отгрузка/рекомендация не напечатана</returns>
        public static int Print(DateTime logDateTime, Shipment shipment, IXLWorksheet sheet, ref int row)
        {
            // 1. Инициализация
            int rc = 1;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (shipment == null)
                    return rc;
                if (sheet == null)
                    return rc;
                if (row <= 0)
                    row = 1;

                // 3. Печать события
                rc = 3;
                int r = row + 1;
                sheet.Cell(r, SENT_COLUMN).SetValue(logDateTime);
                sheet.Cell(r, GUID_COLUMN).SetValue(shipment.id);
                sheet.Cell(r, STATUS_COLUMN).SetValue(shipment.status);
                sheet.Cell(r, SHOP_ID_COLUMN).SetValue(shipment.shop_id);
                sheet.Cell(r, DSERV_ID_COLUMN).SetValue(shipment.delivery_service_id);
                sheet.Cell(r, COURIER_ID_COLUMN).SetValue(shipment.courier_id);
                sheet.Cell(r, DATE_TARGET_COLUMN).SetValue(shipment.date_target);
                sheet.Cell(r, DATE_TARGET_END_COLUMN).SetValue(shipment.date_target_end);
                sheet.Cell(r, CALCULATION_TIME_COLUMN).SetValue(shipment.info.calculationTime);
                sheet.Cell(r, TOTAL_COST_COLUMN).SetValue(shipment.info.sum_cost);
                if (shipment.Count > 0)
                    sheet.Cell(r, ORDER_COST_COLUMN).SetValue(shipment.info.sum_cost / shipment.Count);

                sheet.Cell(r, TOTAL_WEIGHT_COLUMN).SetValue(shipment.info.weight);
                sheet.Cell(r, IS_LOOP_COLUMN).SetValue(shipment.info.is_loop);
                sheet.Cell(r, START_DELIVERY_INTERVAL_COLUMN).SetValue(shipment.info.start_delivery_interval);
                sheet.Cell(r, END_DELIVERY_INTERVAL_COLUMN).SetValue(shipment.info.end_delivery_interval);
                sheet.Cell(r, RESERVE_TIME_COLUMN).SetValue(shipment.info.reserve_time);
                sheet.Cell(r, TOTAL_DELIVERY_TIME_COLUMN).SetValue(shipment.info.delivery_time);
                sheet.Cell(r, TOTAL_EXEC_TIME_COLUMN).SetValue(shipment.info.execution_time);
                sheet.Cell(r, ORDER_COUNT_COLUMN).SetValue(shipment.Count);

                if (shipment.Count > 0)
                {
                    // OrderID
                    int j = ORDER1_COLUMN;
                    for (int i = 0; i < shipment.Count; i++, j++)
                    {
                        sheet.Cell(r, j).SetValue(shipment.orders[i]);
                    }

                    // Delivery time
                    j = SHOP1_DELIVERY_TIME_COLUMN;
                    for (int i = 0; i < shipment.info.node_delivery_time.Length - 1; i++, j++)
                    {
                        sheet.Cell(r, j).SetValue(shipment.info.node_delivery_time[i]);
                    }

                    sheet.Cell(r, SHOP2_DELIVERY_TIME_COLUMN).SetValue(shipment.info.node_delivery_time[shipment.info.node_delivery_time.Length - 1]);

                    // Distance & time
                    j = DIST1_COLUMN;
                    NodeInfo ni;

                    for (int i = 1; i < shipment.info.nodeInfo.Length - 1; i++, j+=2)
                    {
                        ni = shipment.info.nodeInfo[i];
                        sheet.Cell(r, j).SetValue(ni.distance);
                        sheet.Cell(r, j + 1).SetValue(ni.duration);
                    }

                    ni = shipment.info.nodeInfo[shipment.info.nodeInfo.Length - 1];
                    sheet.Cell(r, DISTS_COLUMN).SetValue(ni.distance);
                    sheet.Cell(r, TIMES_COLUMN).SetValue(ni.duration);
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
        /// Печать отгрузок и рекомендаций
        /// </summary>
        /// <param name="logDateTime">Время печати сообщения в лог</param>
        /// <param name="shipment">Отгрузки и рекомендации</param>
        /// <param name="sheet">Лист книги Excel</param>
        /// <param name="row">Номер последней напечатанной строки</param>
        /// <returns>0 - отгрузки/рекомендации напечатаны; иначе - отгрузки/рекомендации не напечатаны</returns>
        public static int Print(DateTime logDateTime, Shipment[] shipments, IXLWorksheet sheet, ref int row)
        {
            // 1. Инициализация
            int rc = 1;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (shipments == null || shipments.Length <= 0)
                    return rc;
                if (sheet == null)
                    return rc;
                if (row <= 0)
                    row = 1;

                // 3. Печатаем события
                rc = 3;
                bool Ok = true;

                foreach (Shipment shipment in shipments)
                {
                    int rc1 = Print(logDateTime, shipment, sheet, ref row);
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
