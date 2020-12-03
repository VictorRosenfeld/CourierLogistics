
namespace LogAnalyzer.Analyzer
{
    using ClosedXML.Excel;
    using LogAnalyzer.ReportData.CancelCommands;
    using LogAnalyzer.ReportData.CourierEvents;
    using LogAnalyzer.ReportData.DeliveryCommands;
    using LogAnalyzer.ReportData.ErrorsSummary;
    using LogAnalyzer.ReportData.OrderEvents;
    using LogAnalyzer.ReportData.OrdersSummary;
    using LogAnalyzer.ReportData.ShopEvents;
    using LogisticsService.API;
    using Newtonsoft.Json;
    using System;
    using System.Diagnostics;
    using System.IO;
    using static LogisticsService.API.BeginShipment;
    using static LogisticsService.API.GetOrderEvents;
    using static LogisticsService.API.GetShopEvents;

    /// <summary>
    /// Анализатор лога сервиса управления курьерами
    /// </summary>
    public class ServiceLogAnalyzer
    {
        #region Report page names

        /// <summary>
        /// Наименование листа с заказами
        /// </summary>
        private const string ORDERS_SUMMARY_PAGE = "Orders Summary";

        /// <summary>
        /// Наименование листа c ошибками
        /// </summary>
        private const string ERRORS_SUMMARY_PAGE = "Errors Summary";

        /// <summary>
        /// Наименование листа c событиями заказов
        /// </summary>
        private const string ORDER_EVENTS_PAGE = "Order Events";

        /// <summary>
        /// Наименование листа c событиями курьеров
        /// </summary>
        private const string COURIER_EVENTS_PAGE = "Courier Events";

        /// <summary>
        /// Наименование листа c событиями магазинов
        /// </summary>
        private const string SHOP_EVENTS_PAGE = "Shop Events";

        /// <summary>
        /// Наименование листа c командами на отгрузку и рекомендациями
        /// </summary>
        private const string DELIVERY_COMMANDS_PAGE = "Delivery Commands";

        /// <summary>
        /// Наименование листа c командами на отказ в доставке
        /// </summary>
        private const string CANCEL_COMMANDS_PAGE = "Cancel Commands";

        #endregion Report page names

        #region Log message separators

        /// <summary>
        /// Один из обязательных текстов вначале сообщения
        /// </summary>
        private const char MESSAGE_START_CHAR = '@';

        /// <summary>
        /// Текст, за которым следуют данные сообщения
        /// </summary>
        private const string MESSAGE_START_DATA_TEXT = " > ";

        #endregion Log message separators

        #region API Error parser data

        private const string API_ERROR_PARSER_STATUSCODE = "StatusCode";
        private const string API_ERROR_PARSER_STATUSCODE_END_CHAR = ":";

        #endregion API Error parser data

        #region Message 43 parser data

        private const string MESSAGE_43_METHOD_END_TEXT = ". ";
        private const string MESSAGE_43_ERROR_CODE_TEXT = "Код ошибки -";

        #endregion Message 43 parser data

        #region Message 666 parser data

        private const string MESSAGE_666_METHOD = "Method";
        private const string MESSAGE_666_METHOD_END_TEXT = ". ";

        #endregion Message 666 parser data

        #region Message 667 parser data

        private const string MESSAGE_667_METHOD = "Method";
        private const string MESSAGE_667_METHOD_END_TEXT = ". rc = ";

        #endregion Message 667 parser data

        #region Message 668 parser data

        private const string MESSAGE_668_METHOD_END_TEXT = "(";

        #endregion Message 668 parser data

        /// <summary>
        /// Параметры анализатора
        /// </summary>
        public AnalyzerConfig Config { get; private set; }

        /// <summary>
        /// Параметрический конструктор класса ServiceLogAnalyzer
        /// </summary>
        /// <param name="config"></param>
        public ServiceLogAnalyzer(AnalyzerConfig config)
        {
            Config = config;
        }

        /// <summary>
        /// Создание отчета по результатам обработки лога
        /// </summary>
        /// <param name="sourceLogFile">Исходный файл лога</param>
        /// <param name="reportFile">Файл с построенным отчетом</param>
        /// <returns></returns>
        public int Create(string sourceLogFile, string reportFile)
        {
            // 1. Инициализация
            int rc = 1;
            IXLWorkbook report = null;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (Config == null)
                    return rc;
                if (string.IsNullOrWhiteSpace(sourceLogFile))
                    return rc;
                if (string.IsNullOrWhiteSpace(reportFile))
                {
                    reportFile = Path.Combine(Path.GetDirectoryName(sourceLogFile), $"{Path.GetFileNameWithoutExtension(sourceLogFile)}_{DateTime.Now:dd_MM_yy_HH_mm_ss}.xlsx");
                }

                // 3. Загружаем файл лога
                rc = 3;
                string log = File.ReadAllText(sourceLogFile);
                if (log == null || log.Length <= 0)
                    return rc;

                // 4. Клонируем шаблон
                rc = 4;
                report = new XLWorkbook(Config.ExcelPatternFile);
                report.SaveAs(reportFile);

                // 5. Извлекаем листы отчета
                rc = 5;
                IXLWorksheet ordersSummary = report.Worksheets.Worksheet(ORDERS_SUMMARY_PAGE);
                if (ordersSummary == null)
                    return rc;
                IXLWorksheet errorsSummary = report.Worksheets.Worksheet(ERRORS_SUMMARY_PAGE);
                if (errorsSummary == null)
                    return rc;
                IXLWorksheet orderEvents = report.Worksheets.Worksheet(ORDER_EVENTS_PAGE);
                if (orderEvents == null)
                    return rc;
                IXLWorksheet courierEvents = report.Worksheets.Worksheet(COURIER_EVENTS_PAGE);
                if (courierEvents == null)
                    return rc;
                IXLWorksheet shopEvents = report.Worksheets.Worksheet(SHOP_EVENTS_PAGE);
                if (shopEvents == null)
                    return rc;
                IXLWorksheet deliveryCommands = report.Worksheets.Worksheet(DELIVERY_COMMANDS_PAGE);
                if (deliveryCommands == null)
                    return rc;
                IXLWorksheet cancelCommands = report.Worksheets.Worksheet(CANCEL_COMMANDS_PAGE);
                if (cancelCommands == null)
                    return rc;

                // 6. Продвигаемся до начала первого сообщения
                rc = 6;
                int startMessagePos = log.IndexOf(MESSAGE_START_CHAR, 0);
                if (startMessagePos < 0)
                    return rc;

                startMessagePos++;

                // 7. Цикл обработки сообщений лога
                rc = 7;
                int size = log.Length;
                JsonSerializer serializer = JsonSerializer.Create();
                int deliveryCommandsRow = 0;
                int cancelCommandsRow = 0;
                int courierEventsRow = 0;
                int orderEventsRow = 0;
                int shopEventsRow = 0;
                int errorsSummaryRow = 0;
                //int orderSummaryRow = 0;
                string method = null;
                string exceptionText;
                int errorCode;
                string methodArgs;

                AllOrders allOrders = new AllOrders();
                int statusCode;
                string statusDescription;

                while (startMessagePos < size)
                {
                    // 7.1 Находим индекс последнего символа сообщения
                    rc = 71;
                    int endMessagePos = log.IndexOf(MESSAGE_START_CHAR, startMessagePos);
                    if (endMessagePos < 0)
                    {
                        endMessagePos = size - 1;
                    }
                    else
                    {
                        endMessagePos--;
                    }

                    // 7.2 Извлекаем дату-время, номер сообщения
                    rc = 72;
                    DateTime messageDateTime;
                    int messageNo;

                    int iPos1 = log.IndexOf(' ', startMessagePos, 11);
                    if (iPos1 < 0) goto NextMessage;
                    int iPos2 = log.IndexOf(' ', iPos1 + 2, 10);
                    if (iPos2 < 0) goto NextMessage;
                    int iPos3 = log.IndexOf(' ', iPos2 + 2, 5);
                    if (iPos3 < 0) goto NextMessage;

                    if (!DateTime.TryParse(log.Substring(startMessagePos, iPos2 - startMessagePos).Trim(), out messageDateTime))
                        goto NextMessage;
                    if (!int.TryParse(log.Substring(iPos2, iPos3 - iPos2).Trim(), out messageNo) || messageNo <= 0)
                        goto NextMessage;

                    // 7.3 Извлекаем данные сообщения
                    rc = 73;
                    int startDataPos = log.IndexOf(MESSAGE_START_DATA_TEXT, iPos3, 12);
                    if (startDataPos < 0 || startDataPos > endMessagePos)
                        goto NextMessage;
                    startDataPos += MESSAGE_START_DATA_TEXT.Length;
                    string messageData = log.Substring(startDataPos, endMessagePos - startDataPos + 1).Trim();

                    // 7.4 Обрабатываем сообщение
                    rc = 74;
                    switch (messageNo)
                    {
                        case 1: // Shipment request
                            break;
                        case 2: // Shipment post data
                            using (StringReader sr = new StringReader(messageData))
                            {
                                Shipment[] shipments = (Shipment[])serializer.Deserialize(sr, typeof(Shipment[]));
                                PrintDeliveryCommands.Print(messageDateTime, shipments, deliveryCommands, ref deliveryCommandsRow);
                                allOrders.AddCommand(messageDateTime, shipments);
                            }
                            break;
                        case 3: // Shipment response
                            break;
                        case 4: // Shipment error
                            if (TryParseApiError(messageData, out statusCode, out statusDescription))
                            {
                                PrintErrorSummary.Print(messageDateTime, messageNo, statusCode, "BeginShipment.Begin", statusDescription, errorsSummary, ref errorsSummaryRow);
                            }
                            else
                            {
                                PrintErrorSummary.Print(messageDateTime, messageNo, -1, "BeginShipment.Begin", messageData, errorsSummary, ref errorsSummaryRow);
                            }
                            break;
                        case 5: // Cancel request
                           break;
                        case 6: // Cancel post data
                            using (StringReader sr = new StringReader(messageData))
                            {
                                RejectedOrder[] rejectedOrders = (RejectedOrder[])serializer.Deserialize(sr, typeof(RejectedOrder[]));
                                PrintCancelCommands.Print(messageDateTime, rejectedOrders, cancelCommands, ref cancelCommandsRow);
                                allOrders.AddCommand(messageDateTime, rejectedOrders);
                            }
                             break;
                        case 7: // Cancel response
                            break;
                        case 8: // Cancel error
                            if (TryParseApiError(messageData, out statusCode, out statusDescription))
                            {
                                PrintErrorSummary.Print(messageDateTime, messageNo, statusCode, "BeginShipment.Reject", statusDescription, errorsSummary, ref errorsSummaryRow);
                            }
                            else
                            {
                                PrintErrorSummary.Print(messageDateTime, messageNo, -1, "BeginShipment.Reject", messageData, errorsSummary, ref errorsSummaryRow);
                            }
                            break;
                        case 9: // Courier events request
                            break;
                        case 10: // Courier events response
                            using (StringReader sr = new StringReader(messageData))
                            {
                                CourierEvent[] events = (CourierEvent[])serializer.Deserialize(sr, typeof(CourierEvent[]));
                                if (events != null && events.Length > 0)
                                    PrintCourierEvents.Print(messageDateTime, events, courierEvents, ref courierEventsRow);
                            }
                            break;
                        case 11: // Courier events error
                            if (TryParseApiError(messageData, out statusCode, out statusDescription))
                            {
                                PrintErrorSummary.Print(messageDateTime, messageNo, statusCode, "GetCourierEvents.GetEvents", statusDescription, errorsSummary, ref errorsSummaryRow);
                            }
                            else
                            {
                                PrintErrorSummary.Print(messageDateTime, messageNo, -1, "GetCourierEvents.GetEvents", messageData, errorsSummary, ref errorsSummaryRow);
                            }
                            break;
                        case 12: // Order events request
                            break;
                        case 13: // Order events response
                            using (StringReader sr = new StringReader(messageData))
                            {
                                OrderEvent[]  events = (OrderEvent[])serializer.Deserialize(sr, typeof(OrderEvent[]));
                                if (events != null && events.Length > 0)
                                {
                                    PrintOrderEvents.Print(messageDateTime, events, orderEvents, ref orderEventsRow);
                                    allOrders.AddOrderEvent(messageDateTime, events);
                                }
                            }
                            break;
                        case 14: // Order events error
                            if (TryParseApiError(messageData, out statusCode, out statusDescription))
                            {
                                PrintErrorSummary.Print(messageDateTime, messageNo, statusCode, "GetOrderEvents.GetEvents", statusDescription, errorsSummary, ref errorsSummaryRow);
                            }
                            else
                            {
                                PrintErrorSummary.Print(messageDateTime, messageNo, -1, "GetOrderEvents.GetEvents", messageData, errorsSummary, ref errorsSummaryRow);
                            }
                            break;
                        case 15: // Shop events request
                            break;
                        case 16: // Shop events response
                            using (StringReader sr = new StringReader(messageData))
                            {
                                ShopEvent[] events = (ShopEvent[])serializer.Deserialize(sr, typeof(ShopEvent[]));
                                PrintShopEvents.Print(messageDateTime, events, shopEvents, ref shopEventsRow);
                            }
                            break;
                        case 17: // Shop events error
                            if (TryParseApiError(messageData, out statusCode, out statusDescription))
                            {
                                PrintErrorSummary.Print(messageDateTime, messageNo, statusCode, "GetShopEvents.GetEvents", statusDescription, errorsSummary, ref errorsSummaryRow);
                            }
                            else
                            {
                                PrintErrorSummary.Print(messageDateTime, messageNo, -1, "GetShopEvents.GetEvents", messageData, errorsSummary, ref errorsSummaryRow);
                            }
                            break;
                        case 18: // ACK request
                            break;
                        case 19: // ACK post data
                            break;
                        case 20: // ACK response
                            break;
                        case 21: // ACK error
                            if (TryParseApiError(messageData, out statusCode, out statusDescription))
                            {
                                PrintErrorSummary.Print(messageDateTime, messageNo, statusCode, "SendAck.Send", statusDescription, errorsSummary, ref errorsSummaryRow);
                            }
                            else
                            {
                                PrintErrorSummary.Print(messageDateTime, messageNo, -1, "SendAck.Send", messageData, errorsSummary, ref errorsSummaryRow);
                            }
                            break;
                        case 22: // Time-Dist request
                            break;
                        case 23: // Time-Dist post data
                            break;
                        case 24: // Time-Dist response
                            break;
                        case 25: // Time-Dist error
                            if (TryParseApiError(messageData, out statusCode, out statusDescription))
                            {
                                PrintErrorSummary.Print(messageDateTime, messageNo, statusCode, "GetShippingInfo.GetInfo", statusDescription, errorsSummary, ref errorsSummaryRow);
                            }
                            else
                            {
                                PrintErrorSummary.Print(messageDateTime, messageNo, -1, "GetShippingInfo.GetInfo", messageData, errorsSummary, ref errorsSummaryRow);
                            }
                            break;
                        case 26: // Queue timer elapsed
                            break;
                        case 27: // Create shipment queue
                            break;
                        case 28: // Queue info timer elapsed
                            break;
                        case 29: // Start service
                            break;
                        case 30: // Stop service
                            break;
                        case 31: // GeoCache info
                            break;
                        case 32: // Queue state
                            break;
                        case 33: // Queue state item
                            break;
                        case 34: // ((((( Shipmtnt from checking queue
                            break;
                        case 35: // ))))) Shipmtnt from checking queue
                            break;
                        case 36: // ((((( Cancel from checking queue
                            break;
                        case 37: // ))))) Cancel from checking queue
                            break;
                        case 38: // Cancel order by time
                            break;
                        case 39: // Cancel order by courier
                            break;
                        case 40: // Cancel assembled order by courier
                            break;
                        case 41: // Cancel receipted order by courier
                            break;
                        case 42: // Checking queue timer elapsed
                            break;
                        case 43: // GeoCache.PutLocationInfo error
                            if (TryParseMsg43(messageData, out method, out errorCode))
                            {
                                PrintErrorSummary.Print(messageDateTime, messageNo, errorCode, method, messageData, errorsSummary, ref errorsSummaryRow);
                            }
                            else
                            {
                                PrintErrorSummary.Print(messageDateTime, messageNo, -1, null, messageData, errorsSummary, ref errorsSummaryRow);
                            }
                            break;
                        case 666: // method exception
                            if (TryParseMsg666(messageData, out method, out exceptionText))
                            {
                                PrintErrorSummary.Print(messageDateTime, messageNo, -1, method, exceptionText, errorsSummary, ref errorsSummaryRow);
                            }
                            else
                            {
                                PrintErrorSummary.Print(messageDateTime, messageNo, -1, null, messageData, errorsSummary, ref errorsSummaryRow);
                            }
                            break;
                        case 667: // Returned method rc
                            if (TryParseMsg667(messageData, out method, out errorCode))
                            {
                                PrintErrorSummary.Print(messageDateTime, messageNo, errorCode, method, messageData, errorsSummary, ref errorsSummaryRow);
                            }
                            else
                            {
                                PrintErrorSummary.Print(messageDateTime, messageNo, -1, null, messageData, errorsSummary, ref errorsSummaryRow);
                            }
                            break;
                        case 668: // Called method
                            if (TryParseMsg668(messageData, out method, out methodArgs))
                            {
                                PrintErrorSummary.Print(messageDateTime, messageNo, -1, method, methodArgs, errorsSummary, ref errorsSummaryRow);
                            }
                            else
                            {
                                PrintErrorSummary.Print(messageDateTime, messageNo, -1, null, messageData, errorsSummary, ref errorsSummaryRow);
                            }
                            break;
                    }

                    // 7.2 Переходим к следующему сообщению
                    NextMessage:
                    rc = 72;
                    startMessagePos = endMessagePos + 2;
                }

                // 8. Печать OrdersSummary
                rc = 8;
                OrderSummary[] orders = new OrderSummary[allOrders.Orders.Count];
                allOrders.Orders.Values.CopyTo(orders, 0);
                PrintOrderSummary.Print(orders, ordersSummary);
                orders = null;

                // 9. Сохраняем построенный отчет
                rc = 9;
                report.Save();

                // 10. Открываем сохраненный отчет
                rc = 10;
                try
                {
                    if (Config.OpenReport)
                    {
                        using (Process process = new Process())
                        {
                            process.StartInfo.FileName = reportFile;
                            process.StartInfo.UseShellExecute = true;
                            process.Start();
                        }
                    }
                }
                catch { }

                ordersSummary = null;
                errorsSummary = null;
                orderEvents = null;
                courierEvents = null;
                shopEvents = null;
                deliveryCommands = null;
                cancelCommands = null;
                allOrders = null;

                // 11. Выход - Ok
                rc = 0;
                return rc;
            }
            catch (Exception ex)
            {
                return rc;
            }
            finally
            {
                if (report != null)
                {
                    report.Dispose();
                    report = null;
                }
            }
        }

        #region Meassage parsers

        /// <summary>
        /// Парсер сообщения об API-ошибке:
        /// StatusCode {0}: {1}
        /// </summary>
        /// <param name="msgData">Данные сообщения</param>
        /// <param name="statusCode">StatusCode из отклика сервера</param>
        /// <param name="statusDescription">StatusDescription из отклика сервера</param>
        /// <returns>true - данные извлечены; данные не извлечены</returns>
        private static bool TryParseApiError(string msgData, out int statusCode, out string statusDescription)
        {
            // 1. Инициализация
            statusCode = -1;
            statusDescription = null;

            try
            {
                // 2. Проверяем исходные данные
                if (string.IsNullOrWhiteSpace(msgData))
                    return false;

                // 3. Находим "StatusCode" и ":" после него
                int iPos1 = msgData.IndexOf(API_ERROR_PARSER_STATUSCODE, StringComparison.CurrentCultureIgnoreCase);
                if (iPos1 < 0)
                    return false;

                int iPos2 = msgData.IndexOf(API_ERROR_PARSER_STATUSCODE_END_CHAR, iPos1 + API_ERROR_PARSER_STATUSCODE.Length,  StringComparison.CurrentCultureIgnoreCase);
                if (iPos2 < 0)
                    return false;

                // 4. Извлекаем значение StatusCode и StatusDescription
                statusDescription = msgData.Substring(iPos2 + 1).Trim();
                if (!int.TryParse(msgData.Substring(iPos1 + API_ERROR_PARSER_STATUSCODE.Length, iPos2 - iPos1 - API_ERROR_PARSER_STATUSCODE.Length).Trim(), out statusCode))
                    return false;

                // 5. Выход - Ok
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Парсер сообщения 666:
        /// Method {0}. {1}
        /// </summary>
        /// <param name="msgData">Данные сообщения</param>
        /// <param name="method">Название метода, вызвавшего прерывания</param>
        /// <param name="exceptionText">текст прерывания</param>
        /// <returns>true - данные извлечены; данные не извлечены</returns>
        private static bool TryParseMsg666(string msgData, out string method, out string exceptionText)
        {
            // 1. Инициализация
            method = null;
            exceptionText = null;

            try
            {
                // 2. Проверяем исходные данные
                if (string.IsNullOrWhiteSpace(msgData))
                    return false;

                // 3. Находим "Method" и ". " после него
                int iPos1 = msgData.IndexOf(MESSAGE_666_METHOD, StringComparison.CurrentCultureIgnoreCase);
                if (iPos1 < 0)
                    return false;

                int iPos2 = msgData.IndexOf(MESSAGE_666_METHOD_END_TEXT, iPos1 + MESSAGE_666_METHOD.Length,  StringComparison.CurrentCultureIgnoreCase);
                if (iPos2 < 0)
                    return false;

                // 4. Извлекаем значение method и exceptionText
                method = msgData.Substring(iPos1 + MESSAGE_666_METHOD.Length, iPos2 - iPos1 - MESSAGE_666_METHOD.Length).Trim();
                exceptionText = msgData.Substring(iPos2 + MESSAGE_666_METHOD_END_TEXT.Length).Trim();

                // 5. Выход - Ok
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Парсер сообщения 667:
        /// Method {0}. rc = {1}
        /// </summary>
        /// <param name="msgData">Данные сообщения</param>
        /// <param name="method">Наименование метода вызвавшего ошибку</param>
        /// <param name="errorCode">Код ошибки</param>
        /// <returns>true - данные извлечены; данные не извлечены</returns>
        private static bool TryParseMsg667(string msgData, out string method, out int errorCode)
        {
            // 1. Инициализация
            errorCode = -1;
            method = null;

            try
            {
                // 2. Проверяем исходные данные
                if (string.IsNullOrWhiteSpace(msgData))
                    return false;

                // 3. Находим "StatusCode" и ":" после него
                int iPos1 = msgData.IndexOf(MESSAGE_667_METHOD, StringComparison.CurrentCultureIgnoreCase);
                if (iPos1 < 0)
                    return false;

                int iPos2 = msgData.IndexOf(MESSAGE_667_METHOD_END_TEXT, iPos1 + MESSAGE_667_METHOD.Length, StringComparison.CurrentCultureIgnoreCase);
                if (iPos2 < 0)
                    return false;

                // 4. Извлекаем значение StatusCode и StatusDescription
                method = msgData.Substring(iPos1 + MESSAGE_667_METHOD.Length, iPos2 - iPos1 - MESSAGE_667_METHOD.Length).Trim();
                if (!int.TryParse(msgData.Substring(iPos2 + 1).Trim(), out errorCode))
                    return false;

                // 5. Выход - Ok
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Парсер сообщения 668:
        /// {0}({1})
        /// </summary>
        /// <param name="msgData">Данные сообщения</param>
        /// <param name="method">Название метода, вызвавшего прерывания</param>
        /// <param name="methodArgs">Аргументы метода</param>
        /// <returns>true - данные извлечены; данные не извлечены</returns>
        private static bool TryParseMsg668(string msgData, out string method, out string methodArgs)
        {
            // 1. Инициализация
            method = null;
            methodArgs = null;

            try
            {
                // 2. Проверяем исходные данные
                if (string.IsNullOrWhiteSpace(msgData))
                    return false;

                // 3. Находим "Method" и ". " после него
                int iPos = msgData.IndexOf(MESSAGE_668_METHOD_END_TEXT, StringComparison.CurrentCultureIgnoreCase);
                if (iPos < 0)
                    return false;

                // 4. Извлекаем значение method и methodArgs
                method = msgData.Substring(0, iPos).Trim();
                methodArgs = msgData.Substring(iPos).Trim();


                // 5. Выход - Ok
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Парсер сообщения 43:
        /// GeoCache.PutLocationInfo. Способ доставки {0}. Исходных точек: {1}. Точек назначения: {2}. Код ошибки - {3}
        /// </summary>
        /// <param name="msgData">Данные сообщения</param>
        /// <param name="method">Наименование метода вызвавшего ошибку</param>
        /// <param name="errorCode">Код ошибки</param>
        /// <returns>true - данные извлечены; данные не извлечены</returns>
        private static bool TryParseMsg43(string msgData, out string method, out int errorCode)
        {
            // 1. Инициализация
            errorCode = -1;
            method = null;

            try
            {
                // 2. Проверяем исходные данные
                if (string.IsNullOrWhiteSpace(msgData))
                    return false;

                // 3. Находим "StatusCode" и ":" после него
                int iPos1 = msgData.IndexOf(MESSAGE_43_METHOD_END_TEXT, StringComparison.CurrentCultureIgnoreCase);
                if (iPos1 < 0)
                    return false;

                int iPos2 = msgData.IndexOf(MESSAGE_43_ERROR_CODE_TEXT, iPos1 + MESSAGE_43_METHOD_END_TEXT.Length, StringComparison.CurrentCultureIgnoreCase);
                if (iPos2 < 0)
                    return false;

                // 4. Извлекаем значение Method и ErrorCode
                method = msgData.Substring(0, iPos1).Trim();
                if (!int.TryParse(msgData.Substring(iPos2 + MESSAGE_43_ERROR_CODE_TEXT.Length).Trim(), out errorCode))
                    return false;

                // 5. Выход - Ok
                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion Meassage parsers
    }
}
