namespace DeliveryBuilderReports
{
    using ClosedXML.Excel;
    using System;

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
        private int errorRow;

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
            errorRow = 1;
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
                OrdersSummary = Workbook.Worksheets.Add("Errors");
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
    }
}
