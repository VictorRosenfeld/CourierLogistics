namespace DeliveryBuilderReports
{
    using ClosedXML.Excel;
    using System;
    using System.Diagnostics;

    /// <summary>
    /// Книга Excel с отчетами
    /// </summary>
    public class ExcelReport
    {
        /// <summary>
        /// Путь к файлу результата
        /// </summary>
        public string Filename { get; private set; }

        /// <summary>
        /// Флаг: true - экземпляр создан; false - экземпляр не создан
        /// </summary>
        public bool IsCreated { get; private set; }

        /// <summary>
        /// Преывание во время последней операции
        /// </summary>
        public Exception LastException { get; private set; }

        #region Workbook & Worksheets

        /// <summary>
        /// Книга с отчетами
        /// </summary>
        public IXLWorkbook Workbook { get; private set; }

        /// <summary>
        /// Лист Recalcs
        /// </summary>
        public IXLWorksheet Recalcs { get; private set; }

        /// <summary>
        /// Текущая строка Recalcs
        /// </summary>
        private int recalcsRow;

        /// <summary>
        /// Лист Deliveries Summary
        /// </summary>
        public IXLWorksheet DeliveriesSummary { get; private set; }

        /// <summary>
        /// Текущая строка Deliveries Summary
        /// </summary>
        private int deliverySummaryRow;

        /// <summary>
        /// Лист Orders Summary
        /// </summary>
        public IXLWorksheet OrdersSummary { get; private set; }

        /// <summary>
        /// Текущая строка Orders Summary
        /// </summary>
        private int orderSummaryRow;

        /// <summary>
        /// Лист Errors
        /// </summary>
        public IXLWorksheet Errors { get; private set; }

        /// <summary>
        /// Текущая строка Errors
        /// </summary>
        private int errorsRow;

        /// <summary>
        /// Лист Broken Connection
        /// </summary>
        public IXLWorksheet BrokenConnection { get; private set; }

        /// <summary>
        /// Текущая строка Broken Connection
        /// </summary>
        private int brokenConnectionRow;

        /// <summary>
        /// Лист Deliveries
        /// </summary>
        public IXLWorksheet Deliveries { get; private set; }

        /// <summary>
        /// Текущая строка Deliveries
        /// </summary>
        private int deliveryRow;

        /// <summary>
        /// Лист Orders
        /// </summary>
        public IXLWorksheet Orders { get; private set; }

        /// <summary>
        /// Текущая строка Orders
        /// </summary>
        private int orderRow;

        /// <summary>
        /// Лист Data Request
        /// </summary>
        public IXLWorksheet DataRequest { get; private set; }

        /// <summary>
        /// Текущая строка Data Request
        /// </summary>
        private int dataRequestRow;

        /// <summary>
        /// Лист Receive Data
        /// </summary>
        public IXLWorksheet ReceiveData { get; private set; }

        /// <summary>
        /// Текущая строка Receive Data
        /// </summary>
        private int receiveDataRow;

        /// <summary>
        /// Лист Heartbeat
        /// </summary>
        public IXLWorksheet Heartbeat { get; private set; }

        /// <summary>
        /// Текущая строка Heartbeat
        /// </summary>
        private int heartbeatRow;

        /// <summary>
        /// Лист Reject Orders
        /// </summary>
        public IXLWorksheet RejectOrders { get; private set; }

        /// <summary>
        /// Текущая строка Reject Orders
        /// </summary>
        private int rejectRow;

        /// <summary>
        /// Лист Send Deliveries
        /// </summary>
        public IXLWorksheet SendDeliveries { get; private set; }

        /// <summary>
        /// Текущая строка Send Deliveries
        /// </summary>
        private int sendDeliveryRow;

        #endregion Workbook & Worksheets

        /// <summary>
        /// Создание экземпляра
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public int Create(string filename)
        {
            // 1. Инициализация
            int rc = 1;
            IsCreated = false;
            LastException = null;
            Filename = null;
            recalcsRow = 1;
            deliverySummaryRow = 1;
            orderSummaryRow = 1;
            errorsRow = 1;
            brokenConnectionRow = 1;
            deliveryRow = 1;
            orderRow = 1;
            dataRequestRow = 1;
            receiveDataRow = 1;
            heartbeatRow = 1;
            rejectRow = 1;
            sendDeliveryRow = 1;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (string.IsNullOrWhiteSpace(filename))
                    return rc;

                Filename = filename;

                // 3. Создаём новую книгу
                rc = 3;
                Workbook = new XLWorkbook();

                // 4. Создаём лист Recalcs
                rc = 4;
                Recalcs = Workbook.Worksheets.Add("Recalcs");
                FormatRecalcs(Recalcs);

                // 5. Создаём лист Deliveries Summary
                rc = 5;
                DeliveriesSummary = Workbook.Worksheets.Add("Deliveries Summary");
                FormatDeliveriesSummary(DeliveriesSummary);

                // 6. Создаём лист Orders Summary
                rc = 6;
                OrdersSummary = Workbook.Worksheets.Add("Orders Summary");
                FormatOrdersSummary(OrdersSummary);

                // 7. Создаём лист Errors
                rc = 7;
                Errors = Workbook.Worksheets.Add("Errors");
                FormatErrors(Errors);

                // 8. Создаём лист Broken Connection
                rc = 8;
                BrokenConnection = Workbook.Worksheets.Add("Broken Connection");
                FormatBrokenConnection(BrokenConnection);

                // 9. Создаём лист Deliveries
                rc = 9;
                Deliveries = Workbook.Worksheets.Add("Deliveries");
                FormatDeliveries(Deliveries);

                // 10. Создаём лист Orders
                rc = 10;
                Orders = Workbook.Worksheets.Add("Orders");
                FormatOrders(Orders);

                // 11. Создаём лист Data Request
                rc = 11;
                DataRequest = Workbook.Worksheets.Add("Data Request");
                FormatDataRequest(DataRequest);

                // 12. Создаём лист Receive Data
                rc = 12;
                ReceiveData = Workbook.Worksheets.Add("Receive Data");
                FormatReceiveData(ReceiveData);

                // 13. Создаём лист Heartbeat
                rc = 13;
                Heartbeat = Workbook.Worksheets.Add("Heartbeat");
                FormatHeartbeat(Heartbeat);

                // 14. Создаём лист Reject Orders
                rc = 14;
                RejectOrders = Workbook.Worksheets.Add("Reject Orders");
                FormatRejectOrders(RejectOrders);

                // 15. Создаём лист Send Deliveries
                rc = 15;
                SendDeliveries = Workbook.Worksheets.Add("Send Deliveries");
                FormatSendDeliveries(SendDeliveries);

                // 16. Выход - Ok
                rc = 0;
                IsCreated = true;
                return rc;
            }
            catch (Exception ex)
            {
                LastException = ex;
                return rc;
            }
        }

        /// <summary>
        /// Форматирование листа Recalcs
        /// </summary>
        /// <param name="sheet">Лист Recalcs</param>
        private static void FormatRecalcs(IXLWorksheet sheet)
        {
            try
            {
                // 2. Проверяем исходные данные
                if (sheet == null)
                    return;

                // 3. Форматируем лист
                sheet.Cell(1, 1).Value = "Time";
                sheet.Cell(1, 2).Value = "Elapsed Time";
                sheet.Cell(1, 3).Value = "Orders";
                sheet.Cell(1, 4).Value = "Shops";
                sheet.Range(1, 1, 1, 4).Style.Font.Bold = true;
                sheet.Range(1, 1, 1, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                sheet.Range(1, 1, 1, 4).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                sheet.Column(1).Width = 14.44;
                sheet.Column(2).Width = 15.67;
                sheet.Column(3).Width = 10.22;
                sheet.Column(4).Width = 9.78;
                sheet.Range(1, 1, 1, 4).SetAutoFilter();
                sheet.SheetView.FreezeRows(1);
            }
            catch
            { }
        }

        /// <summary>
        /// Форматирование листа Deliveries Summary
        /// </summary>
        /// <param name="sheet">Лист Deliveries Summary</param>
        private static void FormatDeliveriesSummary(IXLWorksheet sheet)
        {
            try
            {
                // 2. Проверяем исходные данные
                if (sheet == null)
                    return;

                // 3. Форматируем лист
                sheet.Cell(1, 1).Value = "Time";
                sheet.Cell(1, 2).Value = "Level";
                sheet.Cell(1, 3).Value = "Count";
                sheet.Cell(1, 4).Value = "Cost";
                sheet.Cell(1, 5).Value = "Orders";
                sheet.Cell(1, 6).Value = "Avg Cost";
                sheet.Cell(1, 7).Value = "Orders, %";
                sheet.Cell(1, 8).Value = "Cost, %";
                sheet.Range(1, 1, 1, 8).Style.Font.Bold = true;
                sheet.Range(1, 1, 1, 8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                sheet.Range(1, 1, 1, 8).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                sheet.Column(1).Width = 14.44;
                sheet.Column(2).Width = 9.00;
                sheet.Column(3).Width = 9.78;
                sheet.Column(4).Width = 9.78;
                sheet.Column(5).Width = 10.22;
                sheet.Column(6).Width = 12.00;
                sheet.Column(7).Width = 12.67;
                sheet.Column(8).Width = 10.78;
                sheet.Range(1, 1, 1, 8).SetAutoFilter();
                sheet.SheetView.FreezeRows(1);
            }
            catch
            { }
        }

        /// <summary>
        /// Форматирование листа Orders Summary
        /// </summary>
        /// <param name="sheet">Лист Orders Summary</param>
        private static void FormatOrdersSummary(IXLWorksheet sheet)
        {
            try
            {
                // 2. Проверяем исходные данные
                if (sheet == null)
                    return;

                // 3. Форматируем лист
                sheet.Cell(1, 1).Value = "Messages";
                sheet.Cell(1, 2).Value = "Orders";
                sheet.Cell(1, 3).Value = "Rejected";
                sheet.Cell(1, 4).Value = "Delivered";
                sheet.Range(1, 1, 1, 4).Style.Font.Bold = true;
                sheet.Range(1, 1, 1, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                sheet.Range(1, 1, 1, 4).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                sheet.Column(1).Width = 9.22;
                sheet.Column(2).Width = 8.11;
                sheet.Column(3).Width = 8.11;
                sheet.Column(4).Width = 8.11;
            }
            catch
            { }
        }

        /// <summary>
        /// Форматирование листа Errors
        /// </summary>
        /// <param name="sheet">Лист Errors</param>
        private static void FormatErrors(IXLWorksheet sheet)
        {
            try
            {
                // 2. Проверяем исходные данные
                if (sheet == null)
                    return;

                // 3. Форматируем лист
                sheet.Cell(1, 1).Value = "Time";
                sheet.Cell(1, 2).Value = "Msg No.";
                sheet.Cell(1, 3).Value = "Severity";
                sheet.Cell(1, 4).Value = "Message";
                sheet.Range(1, 1, 1, 4).Style.Font.Bold = true;
                sheet.Range(1, 1, 1, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                sheet.Range(1, 1, 1, 4).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                sheet.Column(1).Width = 14.44;
                sheet.Column(2).Width = 11.67;
                sheet.Column(3).Width = 11.44;
                sheet.Column(4).Width = 62.56;
                sheet.Range(1, 1, 1, 4).SetAutoFilter();
                sheet.SheetView.FreezeRows(1);
            }
            catch
            { }
        }

        /// <summary>
        /// Форматирование листа Broken Connection
        /// </summary>
        /// <param name="sheet">Лист Broken Connection</param>
        private static void FormatBrokenConnection(IXLWorksheet sheet)
        {
            try
            {
                // 2. Проверяем исходные данные
                if (sheet == null)
                    return;

                // 3. Форматируем лист
                sheet.Cell(1, 1).Value = "Time";
                sheet.Cell(1, 2).Value = "Count";
                sheet.Range(1, 1, 1, 2).Style.Font.Bold = true;
                sheet.Range(1, 1, 1, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                sheet.Range(1, 1, 1, 2).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                sheet.Column(1).Width = 14.44;
                sheet.Column(2).Width = 9.78;
                sheet.Range(1, 1, 1, 2).SetAutoFilter();
                sheet.SheetView.FreezeRows(1);
            }
            catch
            { }
        }

        /// <summary>
        /// Форматирование листа Deliveries
        /// </summary>
        /// <param name="sheet">Лист Deliveries</param>
        private static void FormatDeliveries(IXLWorksheet sheet)
        {
            try
            {
                // 2. Проверяем исходные данные
                if (sheet == null)
                    return;

                // 3. Форматируем лист
                sheet.Cell(1, 1).Value = "Time";
                sheet.Cell(1, 2).Value = "Shop";
                sheet.Cell(1, 3).Value = "DServiceID";
                sheet.Cell(1, 4).Value = "Courier";
                sheet.Cell(1, 5).Value = "Taxi";
                sheet.Cell(1, 6).Value = "Level";
                sheet.Cell(1, 7).Value = "Cost";
                sheet.Cell(1, 8).Value = "SDI";
                sheet.Cell(1, 9).Value = "EDI";
                sheet.Cell(1, 10).Value = "Cause";
                sheet.Cell(1, 11).Value = "Execution Time";
                sheet.Cell(1, 12).Value = "Orders";
                sheet.Range(1, 1, 1, 12).Style.Font.Bold = true;
                sheet.Range(1, 1, 1, 12).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                sheet.Range(1, 1, 1, 12).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                sheet.Column(1).Width = 14.44;
                sheet.Column(2).Width = 9.00;
                sheet.Column(3).Width = 13.67;
                sheet.Column(4).Width = 10.78;
                sheet.Column(5).Width = 8.00;
                sheet.Column(6).Width = 9.00;
                sheet.Column(7).Width = 8.67;
                sheet.Column(8).Width = 14.44;
                sheet.Column(9).Width = 14.44;
                sheet.Column(10).Width = 9.67;
                sheet.Column(11).Width = 17.56;
                sheet.Column(12).Width = 41.67;
                sheet.Range(1, 1, 1, 12).SetAutoFilter();
                sheet.SheetView.FreezeRows(1);
            }
            catch
            { }
        }

        /// <summary>
        /// Форматирование листа Orders
        /// </summary>
        /// <param name="sheet">Лист Orders</param>
        private static void FormatOrders(IXLWorksheet sheet)
        {
            try
            {
                // 2. Проверяем исходные данные
                if (sheet == null)
                    return;

                // 3. Форматируем лист
                sheet.Cell(1, 1).Value = "Order";
                sheet.Cell(1, 2).Value = "Shop";
                sheet.Cell(1, 3).Value = "Status";
                sheet.Cell(1, 4).Value = "Completed";
                sheet.Cell(1, 5).Value = "Time Check Disabled";
                sheet.Cell(1, 6).Value = "Received";
                sheet.Cell(1, 7).Value = "Assembled";
                sheet.Cell(1, 8).Value = "Rejection Reason";
                sheet.Cell(1, 9).Value = "Start Delivery Window";
                sheet.Cell(1, 10).Value = "End Delivery Window";
                sheet.Cell(1, 11).Value = "Weight";
                sheet.Cell(1, 12).Value = "Latitude";
                sheet.Cell(1, 13).Value = "Longitude";
                sheet.Range(1, 1, 1, 13).Style.Font.Bold = true;
                sheet.Range(1, 1, 1, 13).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                sheet.Range(1, 1, 1, 13).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                sheet.Column(1).Width = 9.44;
                sheet.Column(2).Width = 9.44;
                sheet.Column(3).Width = 13.67;
                sheet.Column(4).Width = 13.67;
                sheet.Column(5).Width = 8.00;
                sheet.Column(6).Width = 14.44;
                sheet.Column(7).Width = 14.44;
                sheet.Column(8).Width = 13.67;
                sheet.Column(9).Width = 14.44;
                sheet.Column(10).Width = 14.44;
                sheet.Column(11).Width = 10.33;
                sheet.Column(12).Width = 13.11;
                sheet.Column(13).Width = 13.11;
                sheet.Range(1, 1, 1, 13).SetAutoFilter();
                sheet.SheetView.FreezeRows(1);
            }
            catch
            { }
        }

        /// <summary>
        /// Форматирование листа Data Request
        /// </summary>
        /// <param name="sheet">Лист Data Request</param>
        private static void FormatDataRequest(IXLWorksheet sheet)
        {
            try
            {
                // 2. Проверяем исходные данные
                if (sheet == null)
                    return;

                // 3. Форматируем лист
                sheet.Cell(1, 1).Value = "Time";
                sheet.Cell(1, 2).Value = "all";
                sheet.Cell(1, 3).Value = "rc";
                sheet.Cell(1, 4).Value = "Elapsed Time";
                sheet.Range(1, 1, 1, 4).Style.Font.Bold = true;
                sheet.Range(1, 1, 1, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                sheet.Range(1, 1, 1, 4).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                sheet.Column(1).Width = 14.44;
                sheet.Column(2).Width = 6.56;
                sheet.Column(3).Width = 6.22;
                sheet.Column(4).Width = 15.67;
                sheet.Range(1, 1, 1, 4).SetAutoFilter();
                sheet.SheetView.FreezeRows(1);
            }
            catch
            { }
        }

        /// <summary>
        /// Форматирование листа Receive Data
        /// </summary>
        /// <param name="sheet">Лист Receive Data</param>
        private static void FormatReceiveData(IXLWorksheet sheet)
        {
            try
            {
                // 2. Проверяем исходные данные
                if (sheet == null)
                    return;

                // 3. Форматируем лист
                sheet.Cell(1, 1).Value = "Time";
                sheet.Cell(1, 2).Value = "Messages";
                sheet.Cell(1, 3).Value = "rc";
                sheet.Cell(1, 4).Value = "Elapsed Time";
                sheet.Range(1, 1, 1, 4).Style.Font.Bold = true;
                sheet.Range(1, 1, 1, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                sheet.Range(1, 1, 1, 4).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                sheet.Column(1).Width = 14.44;
                sheet.Column(2).Width = 12.67;
                sheet.Column(3).Width = 6.22;
                sheet.Column(4).Width = 15.67;
                sheet.Range(1, 1, 1, 4).SetAutoFilter();
                sheet.SheetView.FreezeRows(1);
            }
            catch
            { }
        }

        /// <summary>
        /// Форматирование листа Heartbeat
        /// </summary>
        /// <param name="sheet">Лист Heartbeat</param>
        private static void FormatHeartbeat(IXLWorksheet sheet)
        {
            try
            {
                // 2. Проверяем исходные данные
                if (sheet == null)
                    return;

                // 3. Форматируем лист
                sheet.Cell(1, 1).Value = "Time";
                sheet.Cell(1, 2).Value = "rc";
                sheet.Cell(1, 3).Value = "Elapsed Time";
                sheet.Range(1, 1, 1, 3).Style.Font.Bold = true;
                sheet.Range(1, 1, 1, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                sheet.Range(1, 1, 1, 3).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                sheet.Column(1).Width = 14.44;
                sheet.Column(2).Width = 6.22;
                sheet.Column(3).Width = 15.67;
                sheet.Range(1, 1, 1, 3).SetAutoFilter();
                sheet.SheetView.FreezeRows(1);
            }
            catch
            { }
        }

        /// <summary>
        /// Форматирование листа Reject Orders
        /// </summary>
        /// <param name="sheet">Лист Reject Orders</param>
        private static void FormatRejectOrders(IXLWorksheet sheet)
        {
            try
            {
                // 2. Проверяем исходные данные
                if (sheet == null)
                    return;

                // 3. Форматируем лист
                sheet.Cell(1, 1).Value = "Time";
                sheet.Cell(1, 2).Value = "count";
                sheet.Cell(1, 3).Value = "rc";
                sheet.Cell(1, 4).Value = "Elapsed Time";
                sheet.Range(1, 1, 1, 4).Style.Font.Bold = true;
                sheet.Range(1, 1, 1, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                sheet.Range(1, 1, 1, 4).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                sheet.Column(1).Width = 14.44;
                sheet.Column(2).Width = 9.56;
                sheet.Column(3).Width = 6.22;
                sheet.Column(4).Width = 15.67;
                sheet.Range(1, 1, 1, 4).SetAutoFilter();
                sheet.SheetView.FreezeRows(1);
            }
            catch
            { }
        }

        /// <summary>
        /// Форматирование листа Send Deliveries
        /// </summary>
        /// <param name="sheet">Лист Send Deliveries</param>
        private static void FormatSendDeliveries(IXLWorksheet sheet)
        {
            try
            {
                // 2. Проверяем исходные данные
                if (sheet == null)
                    return;

                // 3. Форматируем лист
                sheet.Cell(1, 1).Value = "Time";
                sheet.Cell(1, 2).Value = "count";
                sheet.Cell(1, 3).Value = "rc";
                sheet.Cell(1, 4).Value = "Elapsed Time";
                sheet.Range(1, 1, 1, 4).Style.Font.Bold = true;
                sheet.Range(1, 1, 1, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                sheet.Range(1, 1, 1, 4).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                sheet.Column(1).Width = 14.44;
                sheet.Column(2).Width = 9.56;
                sheet.Column(3).Width = 6.22;
                sheet.Column(4).Width = 15.67;
                sheet.Range(1, 1, 1, 4).SetAutoFilter();
                sheet.SheetView.FreezeRows(1);
            }
            catch
            { }
        }

        /// <summary>
        /// Добавление записи на лист Recalcs
        /// </summary>
        /// <param name="time">Время записи</param>
        /// <param name="elapsedTime">Длительность пересчета, мсек</param>
        /// <param name="orderCount">Число заказов в пересчете</param>
        /// <param name="shopCount">Число магазинов в пересчете</param>
        /// <returns>0 - запись добавлена; иначе - запись не добавлена</returns>
        public int AddRecalcsRecord(DateTime time, int elapsedTime, int orderCount, int shopCount)
        {
            // 1. Иициализация
            int rc = 1;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (!IsCreated)
                    return rc;

                // 3. Добавляем запись
                rc = 3;
                int row = recalcsRow + 1;
                Recalcs.Cell(row, 1).SetValue(time);
                Recalcs.Cell(row, 2).SetValue(elapsedTime);
                Recalcs.Cell(row, 3).SetValue(orderCount);
                Recalcs.Cell(row, 4).SetValue(shopCount);
                recalcsRow = row;

                // 4. Выход - Ok
                rc = 0;
                return rc;
            }
            catch
            { return rc; }
        }

        /// <summary>
        /// Добавление записи на лист Deliveries Summary
        /// </summary>
        /// <param name="time">Время записи</param>
        /// <param name="level">Число заказов в отгрузке</param>
        /// <param name="count">Общее число отгрузок уровня level</param>
        /// <param name="cost">Общая стоимость отгрузок уровня level</param>
        /// <param name="orderCount">Общее число заказов в отгрузках уровня level</param>
        /// <param name="avgCost">Средняя стоимость доставки заказа по всем отгрузкам уровня level</param>
        /// <param name="ordersPercent">Процент заказов уровня level от всех доставленных заказов</param>
        /// <param name="costPrecent">Процент стоимости доставки заказов уровня level от общей стоимости всех доставленных заказов</param>
        /// <returns>0 - запись добавлена; иначе - запись не добавлена</returns>
        public int AddDeliverySummaryRecord(DateTime time, int level, int count, double cost, int orderCount, double avgCost, double ordersPercent, double costPrecent)
        {
            // 1. Иициализация
            int rc = 1;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (!IsCreated)
                    return rc;

                // 3. Добавляем запись
                rc = 3;
                int row = deliverySummaryRow + 1;
                DeliveriesSummary.Cell(row, 1).SetValue(time);
                DeliveriesSummary.Cell(row, 2).SetValue(level);
                DeliveriesSummary.Cell(row, 3).SetValue(count);
                DeliveriesSummary.Cell(row, 4).SetValue(cost);
                DeliveriesSummary.Cell(row, 5).SetValue(orderCount);
                DeliveriesSummary.Cell(row, 6).SetValue(avgCost);
                DeliveriesSummary.Cell(row, 7).SetValue(ordersPercent);
                DeliveriesSummary.Cell(row, 8).SetValue(costPrecent);
                deliverySummaryRow = row;

                // 4. Выход - Ok
                rc = 0;
                return rc;
            }
            catch
            { return rc; }
        }

        /// <summary>
        /// Добавление записи на лист Orders Summary
        /// </summary>
        /// <param name="messageCount">Число принятых сообщеий с заказами</param>
        /// <param name="orderCount">Общее число принятых заказов</param>
        /// <param name="rejectedCount">Общее число отвернутых заказов</param>
        /// <param name="deliveredCount">Общее число отгруженных заказов</param>
        /// <returns>0 - запись добавлена; иначе - запись не добавлена</returns>
        public int AddOrdersSummaryRecord(int messageCount, int orderCount, int rejectedCount, int deliveredCount)
        {
            // 1. Иициализация
            int rc = 1;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (!IsCreated)
                    return rc;

                // 3. Добавляем запись
                rc = 3;
                int row = orderSummaryRow + 1;
                OrdersSummary.Cell(row, 1).SetValue(messageCount);
                OrdersSummary.Cell(row, 2).SetValue(orderCount);
                OrdersSummary.Cell(row, 3).SetValue(rejectedCount);
                OrdersSummary.Cell(row, 4).SetValue(deliveredCount);
                orderSummaryRow = row;

                // 4. Выход - Ok
                rc = 0;
                return rc;
            }
            catch
            { return rc; }
        }

        /// <summary>
        /// Добавление записи на лист Errors
        /// </summary>
        /// <param name="time">Время записи</param>
        /// <param name="msgNo">Номер сообщения</param>
        /// <param name="severity">Тип сообщения</param>
        /// <param name="message">Текст сообщения</param>
        /// <returns>0 - запись добавлена; иначе - запись не добавлена</returns>
        public int AddErrorsRecord(DateTime time, int msgNo, string severity, string message)
        {
            // 1. Иициализация
            int rc = 1;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (!IsCreated)
                    return rc;

                // 3. Добавляем запись
                rc = 3;
                int row = errorsRow + 1;
                Errors.Cell(row, 1).SetValue(time);
                Errors.Cell(row, 2).SetValue(msgNo);
                Errors.Cell(row, 3).SetValue(severity);
                Errors.Cell(row, 4).SetValue(message);
                errorsRow = row;

                // 4. Выход - Ok
                rc = 0;
                return rc;
            }
            catch
            { return rc; }
        }

        /// <summary>
        /// Добавление записи на лист Broken Connection
        /// </summary>
        /// <param name="time">Время записи</param>
        /// <param name="count">Счетчик подряд идущих тиков с разорванным соединением</param>
        /// <returns>0 - запись добавлена; иначе - запись не добавлена</returns>
        public int AddBrokenConnectionRecord(DateTime time, int count)
        {
            // 1. Иициализация
            int rc = 1;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (!IsCreated)
                    return rc;

                // 3. Добавляем запись
                rc = 3;
                int row = brokenConnectionRow + 1;
                BrokenConnection.Cell(row, 1).SetValue(time);
                BrokenConnection.Cell(row, 2).SetValue(count);
                brokenConnectionRow = row;

                // 4. Выход - Ok
                rc = 0;
                return rc;
            }
            catch
            { return rc; }
        }

        /// <summary>
        /// Добавление записи на лист Deliveries
        /// </summary>
        /// <param name="time">Время записи</param>
        /// <param name="shopId">ID магазина</param>
        /// <param name="dserviceId">DServiceID</param>
        /// <param name="courierId">ID куьера</param>
        /// <param name="isTaxi">Флаг такси</param>
        /// <param name="level">Число заказов в отгрузке</param>
        /// <param name="cost">Стоимсть доставки</param>
        /// <param name="startDeliveryInterval">Начало возмжного интервала старта отрузки</param>
        /// <param name="endDeliveryInterval">Конец возмжного интервала старта отрузки</param>
        /// <param name="cause">Причина старта</param>
        /// <param name="executionTime">Время доставки</param>
        /// <param name="orders">Перечень заказов в отгрузке</param>
        /// <returns>0 - запись добавлена; иначе - запись не добавлена</returns>
        public int AddDeliveryRecord(DateTime time, int shopId, int dserviceId, int courierId, bool isTaxi, 
            int level, double cost, DateTime startDeliveryInterval, DateTime endDeliveryInterval, 
            string cause, double executionTime, string orders)
        {
            // 1. Иициализация
            int rc = 1;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (!IsCreated)
                    return rc;

                // 3. Добавляем запись
                rc = 3;
                int row = deliveryRow + 1;
                Deliveries.Cell(row, 1).SetValue(time);
                Deliveries.Cell(row, 2).SetValue(shopId);
                Deliveries.Cell(row, 3).SetValue(dserviceId);
                Deliveries.Cell(row, 4).SetValue(courierId);
                Deliveries.Cell(row, 5).SetValue(isTaxi);
                Deliveries.Cell(row, 6).SetValue(level);
                Deliveries.Cell(row, 7).SetValue(cost);
                Deliveries.Cell(row, 8).SetValue(startDeliveryInterval);
                Deliveries.Cell(row, 9).SetValue(endDeliveryInterval);
                Deliveries.Cell(row, 10).SetValue(cause);
                Deliveries.Cell(row, 11).SetValue(executionTime);
                Deliveries.Cell(row, 12).SetValue(orders);
                deliveryRow = row;

                // 4. Выход - Ok
                rc = 0;
                return rc;
            }
            catch
            { return rc; }
        }

        /// <summary>
        /// Добавление записи на лист Orders
        /// </summary>
        /// <param name="orderId">ID заказа</param>
        /// <param name="shopId">ID магазина</param>
        /// <param name="status">Статус заказа</param>
        /// <param name="completed">Флаг завершеия обработки заказа</param>
        /// <param name="checkTimeDisabled">Флаг отмены проверки окна вручения</param>
        /// <param name="received">Время поступления заказа в магазин</param>
        /// <param name="assembled">Время сборки заказа</param>
        /// <param name="rejectionReason">Причина отказа</param>
        /// <param name="startDeliveryWindow">Начало окна вручения заказа</param>
        /// <param name="endDeliveryWindow">Конец окна вручения заказа</param>
        /// <param name="weight">Вес заказа</param>
        /// <param name="latitude">Широта</param>
        /// <param name="longitude">Долгота</param>
        /// <returns>0 - запись добавлена; иначе - запись не добавлена</returns>
        public int AddOrdersRecord(int orderId, int shopId, string status, bool completed, bool checkTimeDisabled, 
            DateTime received, DateTime assembled, string rejectionReason, DateTime startDeliveryWindow, 
            DateTime endDeliveryWindow, double weight, double latitude, double longitude)
        {
            // 1. Иициализация
            int rc = 1;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (!IsCreated)
                    return rc;

                // 3. Добавляем запись
                rc = 3;
                int row = orderRow + 1;
                Orders.Cell(row, 1).SetValue(orderId);
                Orders.Cell(row, 2).SetValue(shopId);
                Orders.Cell(row, 3).SetValue(status);
                Orders.Cell(row, 4).SetValue(completed);
                Orders.Cell(row, 5).SetValue(checkTimeDisabled);
                Orders.Cell(row, 6).SetValue(received);
                Orders.Cell(row, 7).SetValue(assembled);
                Orders.Cell(row, 8).SetValue(rejectionReason);
                Orders.Cell(row, 9).SetValue(startDeliveryWindow);
                Orders.Cell(row, 10).SetValue(endDeliveryWindow);
                Orders.Cell(row, 11).SetValue(weight);
                Orders.Cell(row, 12).SetValue(latitude);
                Orders.Cell(row, 13).SetValue(longitude);
                orderRow = row;

                // 4. Выход - Ok
                rc = 0;
                return rc;
            }
            catch
            { return rc; }
        }

        /// <summary>
        /// Добавление записи на лист Data Request
        /// </summary>
        /// <param name="time">Время записи</param>
        /// <param name="all">Флаг запроса</param>
        /// <param name="rcCode">Код возврата</param>
        /// <param name="elapsedTime">Длительность выполнения запроса</param>
        /// <returns>0 - запись добавлена; иначе - запись не добавлена</returns>
        public int AddDataRequestRecord(DateTime time, bool all, int rcCode, int elapsedTime)
        {
            // 1. Иициализация
            int rc = 1;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (!IsCreated)
                    return rc;

                // 3. Добавляем запись
                rc = 3;
                int row = dataRequestRow + 1;
                DataRequest.Cell(row, 1).SetValue(time);
                DataRequest.Cell(row, 2).SetValue(all);
                DataRequest.Cell(row, 3).SetValue(rcCode);
                DataRequest.Cell(row, 4).SetValue(elapsedTime);
                dataRequestRow = row;

                // 4. Выход - Ok
                rc = 0;
                return rc;
            }
            catch
            { return rc; }
        }

        /// <summary>
        /// Добавление записи на лист Receive Data
        /// </summary>
        /// <param name="time">Время записи</param>
        /// <param name="messageCount">Кличество сообщений в принятм пакете</param>
        /// <param name="rcCode">Код возврата</param>
        /// <param name="elapsedTime">Длительность выполнения запроса</param>
        /// <returns>0 - запись добавлена; иначе - запись не добавлена</returns>
        public int AddReceiveDataRecord(DateTime time, int messageCount, int rcCode, int elapsedTime)
        {
            // 1. Иициализация
            int rc = 1;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (!IsCreated)
                    return rc;

                // 3. Добавляем запись
                rc = 3;
                int row = receiveDataRow + 1;
                ReceiveData.Cell(row, 1).SetValue(time);
                ReceiveData.Cell(row, 2).SetValue(messageCount);
                ReceiveData.Cell(row, 3).SetValue(rcCode);
                ReceiveData.Cell(row, 4).SetValue(elapsedTime);
                receiveDataRow = row;

                // 4. Выход - Ok
                rc = 0;
                return rc;
            }
            catch
            { return rc; }
        }

        /// <summary>
        /// Добавление записи на лист Heartbeat
        /// </summary>
        /// <param name="time">Время записи</param>
        /// <param name="rcCode">Код возврата</param>
        /// <param name="elapsedTime">Длительность выполнения команды</param>
        /// <returns>0 - запись добавлена; иначе - запись не добавлена</returns>
        public int AddHeartbeatRecord(DateTime time, int rcCode, int elapsedTime)
        {
            // 1. Иициализация
            int rc = 1;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (!IsCreated)
                    return rc;

                // 3. Добавляем запись
                rc = 3;
                int row = heartbeatRow + 1;
                Heartbeat.Cell(row, 1).SetValue(time);
                Heartbeat.Cell(row, 2).SetValue(rcCode);
                Heartbeat.Cell(row, 3).SetValue(elapsedTime);
                heartbeatRow = row;

                // 4. Выход - Ok
                rc = 0;
                return rc;
            }
            catch
            { return rc; }
        }

        /// <summary>
        /// Добавление записи на лист Reject Orders
        /// </summary>
        /// <param name="time">Время записи</param>
        /// <param name="count">Число отказов в команде</param>
        /// <param name="rcCode">Код возврата</param>
        /// <param name="elapsedTime">Длительность выполнения запроса</param>
        /// <returns>0 - запись добавлена; иначе - запись не добавлена</returns>
        public int AddRejectOrderRecord(DateTime time, int count, int rcCode, int elapsedTime)
        {
            // 1. Иициализация
            int rc = 1;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (!IsCreated)
                    return rc;

                // 3. Добавляем запись
                rc = 3;
                int row = rejectRow + 1;
                RejectOrders.Cell(row, 1).SetValue(time);
                RejectOrders.Cell(row, 2).SetValue(count);
                RejectOrders.Cell(row, 3).SetValue(rcCode);
                RejectOrders.Cell(row, 4).SetValue(elapsedTime);
                rejectRow = row;

                // 4. Выход - Ok
                rc = 0;
                return rc;
            }
            catch
            { return rc; }
        }

        /// <summary>
        /// Добавление записи на лист Send Deliveries
        /// </summary>
        /// <param name="time">Время записи</param>
        /// <param name="count">Число отгрузок в команде</param>
        /// <param name="rcCode">Код возврата</param>
        /// <param name="elapsedTime">Длительность выполнения запроса</param>
        /// <returns>0 - запись добавлена; иначе - запись не добавлена</returns>
        public int AddSendDeliveriesRecord(DateTime time, int count, int rcCode, int elapsedTime)
        {
            // 1. Иициализация
            int rc = 1;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (!IsCreated)
                    return rc;

                // 3. Добавляем запись
                rc = 3;
                int row = sendDeliveryRow + 1;
                SendDeliveries.Cell(row, 1).SetValue(time);
                SendDeliveries.Cell(row, 2).SetValue(count);
                SendDeliveries.Cell(row, 3).SetValue(rcCode);
                SendDeliveries.Cell(row, 4).SetValue(elapsedTime);
                sendDeliveryRow = row;

                // 4. Выход - Ok
                rc = 0;
                return rc;
            }
            catch
            { return rc; }
        }

        /// <summary>
        /// Сохранение книги
        /// </summary>
        /// <returns>0 - книга сохранена; иначе - книга не сохранена</returns>
        public int Save()
        {
            // 1. Инициализация
            int rc = 1;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (!IsCreated)
                    return rc;
                if (Workbook == null)
                    return rc;

                // 3. Сохраняем книгу
                rc = 3;
                Workbook.SaveAs(Filename);

                // 4. Выход - Ok
                rc = 0;
                return rc;
            }
            catch
            { return rc; }
        }

        /// <summary>
        /// Открытие книги
        /// </summary>
        /// <returns>0 - книга открыта; иначе - книга не открыта</returns>
        public int Show()
        {
            // 1. Инициализация
            int rc = 1;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (!IsCreated)
                    return rc;
                if (string.IsNullOrWhiteSpace(Filename))
                    return rc;

                // 3. Сохраняем книгу
                rc = 3;
                Process excel = new Process();
                excel.StartInfo.FileName = Filename;
                excel.StartInfo.UseShellExecute = true;
                excel.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
                excel.Start();

                // 4. Выход - Ok
                rc = 0;
                return rc;
            }
            catch
            { return rc; }
        }
    }
}
