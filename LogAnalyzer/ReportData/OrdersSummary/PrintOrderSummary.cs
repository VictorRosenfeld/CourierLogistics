namespace LogAnalyzer.ReportData.OrdersSummary
{
    using ClosedXML.Excel;
    using System;

    /// <summary>
    /// Вывод информации о заказах на лист Excel
    /// </summary>
    public class PrintOrderSummary
    {
        #region Column settings

        private const int ORDER_ID_COLUMN = 2;
        private const int SHOP_ID_COLUMN = 3;
        private const int R_COLUMN = 4;
        private const int A_COLUMN = 5;
        private const int S_COLUMN = 6;
        private const int C_COLUMN = 7;
        private const int L_COLUMN = 8;
        private const int EVENTS_COLUMN = 9;

        // Last Receipted event
        private const int RECEIVED_LAST_COLUMN1 = 10;
        private const int EVENT_TIME_LAST_COLUMN1 = 11;
        private const int TYPE_LAST_COLUMN1 = 12;
        private const int TIME_FROM_LAST_COLUMN1 = 13;
        private const int TIME_TO_LAST_COLUMN1 = 14;
        private const int WEIGHT_COLUMN1 = 15;
        private const int Y_COLUMN1 = 16;
        private const int G_COLUMN1 = 17;
        private const int C_COLUMN1 = 18;

        // Last Assembled event
        private const int RECEIVED_LAST_COLUMN2 = 19;
        private const int EVENT_TIME_LAST_COLUMN2 = 20;
        private const int TYPE_LAST_COLUMN2 = 21;
        private const int TIME_FROM_LAST_COLUMN2 = 22;
        private const int TIME_TO_LAST_COLUMN2 = 23;
        private const int WEIGHT_COLUMN2 = 24;
        private const int Y_COLUMN2 = 25;
        private const int G_COLUMN2 = 26;
        private const int C_COLUMN2 = 27;

        // Last Canceled event
        private const int RECEIVED_LAST_COLUMN3 = 28;
        private const int EVENT_TIME_LAST_COLUMN3 = 29;
        private const int TYPE_LAST_COLUMN3 = 30;
        private const int TIME_FROM_LAST_COLUMN3 = 31;
        private const int TIME_TO_LAST_COLUMN3 = 32;
        private const int WEIGHT_COLUMN3 = 33;
        private const int Y_COLUMN3 = 34;
        private const int G_COLUMN3 = 35;
        private const int C_COLUMN3 = 36;

        private const int CMDS_COLUMN = 37;

        // First command
        private const int SENT_COLUMN1 = 38;
        private const int STATUS_COLUMN1 = 39;
        private const int JSON_COLUMN1 = 40;

        // Last command
        private const int SENT_COLUMN2 = 41;
        private const int STATUS_COLUMN2 = 42;
        private const int JSON_COLUMN2 = 43;

        #endregion Column settings

        /// <summary>
        /// Печать OrdersSummary
        /// </summary>
        /// <param name="orders">Сводки по заказам</param>
        /// <param name="sheet">Лист книги, на который выводятся данные</param>
        /// <returns>0 - печать произведена; иначе - печать не произведена</returns>
        public static int Print(OrderSummary[] orders, IXLWorksheet sheet)
        {
            // 1. Инициализация
            int rc = 1;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (orders == null || orders.Length <= 0)
                    return rc;
                if (sheet == null)
                    return rc;

                // 3. Цикл печати
                rc = 3;
                int row = 3;

                for (int i = 0; i < orders.Length; i++, row++)
                {
                    // 3.1 Извлекаем заказ
                    rc = 31;
                    OrderSummary order = orders[i];

                    // 3.2 Печатаем строку
                    rc = 32;
                    sheet.Cell(row, ORDER_ID_COLUMN).SetValue(order.OrderId);
                    sheet.Cell(row, SHOP_ID_COLUMN).SetValue(order.ShopId);
                    if ((order.Flags & OrderFlags.Receipted) != 0) sheet.Cell(row, R_COLUMN).SetValue(true);
                    if ((order.Flags & OrderFlags.Asssembled) != 0) sheet.Cell(row, A_COLUMN).SetValue(true);
                    if ((order.Flags & OrderFlags.Shipped) != 0) sheet.Cell(row, S_COLUMN).SetValue(true);
                    if ((order.Flags & OrderFlags.Canceled) != 0) sheet.Cell(row, C_COLUMN).SetValue(true);
                    if ((order.Flags & OrderFlags.Leak) != 0) sheet.Cell(row, L_COLUMN).SetValue(true);
                    if ((order.Flags & (OrderFlags.Asssembled | OrderFlags.Canceled)) == 0)
                    {
                        if (order.GetTimeTo() <= DateTime.Now)
                            sheet.Cell(row, L_COLUMN).SetValue(true);
                    }

                    sheet.Cell(row, EVENTS_COLUMN).SetValue(order.EventCount);

                    if ((order.Flags & OrderFlags.Receipted) != 0)
                    {
                        sheet.Cell(row, RECEIVED_LAST_COLUMN1).SetValue(order.ReceivedTime_Receipted);
                        sheet.Cell(row, EVENT_TIME_LAST_COLUMN1).SetValue(order.EventTime_Receipted);
                        sheet.Cell(row, TYPE_LAST_COLUMN1).SetValue(order.Type_Receipted);
                        sheet.Cell(row, TIME_FROM_LAST_COLUMN1).SetValue(order.TimeFrom_Receipted);
                        sheet.Cell(row, TIME_TO_LAST_COLUMN1).SetValue(order.TimeTo_Receipted);
                        sheet.Cell(row, WEIGHT_COLUMN1).SetValue(order.Weight_Receipted);
                        if ((order.DeliveryFlags_Receipted & DeliveryServiceFlags.YandexTaxi) != 0)
                            sheet.Cell(row, Y_COLUMN1).SetValue(true);
                        if ((order.DeliveryFlags_Receipted & DeliveryServiceFlags.GettTaxi) != 0)
                            sheet.Cell(row, G_COLUMN1).SetValue(true);
                        if ((order.DeliveryFlags_Receipted & DeliveryServiceFlags.Courier) != 0)
                            sheet.Cell(row, C_COLUMN1).SetValue(true);
                    }

                    if ((order.Flags & OrderFlags.Asssembled) != 0)
                    {
                        sheet.Cell(row, RECEIVED_LAST_COLUMN2).SetValue(order.ReceivedTime_Assembled);
                        sheet.Cell(row, EVENT_TIME_LAST_COLUMN2).SetValue(order.EventTime_Assembled);
                        sheet.Cell(row, TYPE_LAST_COLUMN2).SetValue(order.Type_Assembled);
                        sheet.Cell(row, TIME_FROM_LAST_COLUMN2).SetValue(order.TimeFrom_Assembled);
                        sheet.Cell(row, TIME_TO_LAST_COLUMN2).SetValue(order.TimeTo_Assembled);
                        sheet.Cell(row, WEIGHT_COLUMN2).SetValue(order.Weight_Assembled);
                        if ((order.DeliveryFlags_Assembled & DeliveryServiceFlags.YandexTaxi) != 0)
                            sheet.Cell(row, Y_COLUMN2).SetValue(true);
                        if ((order.DeliveryFlags_Assembled & DeliveryServiceFlags.GettTaxi) != 0)
                            sheet.Cell(row, G_COLUMN2).SetValue(true);
                        if ((order.DeliveryFlags_Assembled & DeliveryServiceFlags.Courier) != 0)
                            sheet.Cell(row, C_COLUMN2).SetValue(true);
                    }

                    if ((order.Flags & OrderFlags.Canceled) != 0)
                    {
                        sheet.Cell(row, RECEIVED_LAST_COLUMN3).SetValue(order.ReceivedTime_Canceled);
                        sheet.Cell(row, EVENT_TIME_LAST_COLUMN3).SetValue(order.EventTime_Canceled);
                        sheet.Cell(row, TYPE_LAST_COLUMN3).SetValue(order.Type_Canceled);
                        sheet.Cell(row, TIME_FROM_LAST_COLUMN3).SetValue(order.TimeFrom_Canceled);
                        sheet.Cell(row, TIME_TO_LAST_COLUMN3).SetValue(order.TimeTo_Canceled);
                        sheet.Cell(row, WEIGHT_COLUMN3).SetValue(order.Weight_Canceled);
                        if ((order.DeliveryFlags_Canceled & DeliveryServiceFlags.YandexTaxi) != 0)
                            sheet.Cell(row, Y_COLUMN3).SetValue(true);
                        if ((order.DeliveryFlags_Canceled & DeliveryServiceFlags.GettTaxi) != 0)
                            sheet.Cell(row, G_COLUMN3).SetValue(true);
                        if ((order.DeliveryFlags_Canceled & DeliveryServiceFlags.Courier) != 0)
                            sheet.Cell(row, C_COLUMN3).SetValue(true);
                    }


                    if (order.CommandCount > 0)
                    {
                        sheet.Cell(row, CMDS_COLUMN).SetValue(order.CommandCount);

                        sheet.Cell(row, SENT_COLUMN1).SetValue(order.SentTime_First);
                        sheet.Cell(row, STATUS_COLUMN1).SetValue(order.Status_First);
                        sheet.Cell(row, JSON_COLUMN1).SetValue(order.CommandText_First);

                        sheet.Cell(row, SENT_COLUMN1).SetValue(order.SentTime_Last);
                        sheet.Cell(row, STATUS_COLUMN1).SetValue(order.Status_Last);
                        sheet.Cell(row, JSON_COLUMN1).SetValue(order.CommandText_Last);
                    }
                }

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
