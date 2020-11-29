

namespace LogAnalyzer.Analyzer
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using ClosedXML.Excel;

    /// <summary>
    /// Анализатор лога сервиса управления курьерами
    /// </summary>
    public class ServiceLogAnalyzer
    {
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

        /// <summary>
        /// Один из обязательных текстов вначале сообщения
        /// </summary>
        private const char MESSAGE_START_CHAR = '@';

        /// <summary>
        /// Текст, за которым следуют данные сообщения
        /// </summary>
        private const string MESSAGE_START_DATA_TEXT = " > ";

        ///// <summary>
        ///// Один из обязательных текстов вначале сообщения
        ///// </summary>
        //private const string START_MESSAGE_TEXT1 = " Info > ";

        ///// <summary>
        ///// Один из обязательных текстов вначале сообщения
        ///// </summary>
        //private const string START_MESSAGE_TEXT2 = " Warning > ";

        ///// <summary>
        ///// Один из обязательных текстов вначале сообщения
        ///// </summary>
        //private const string START_MESSAGE_TEXT3 = " Error > ";

        ///// <summary>
        ///// Максимальная начальная позиция обязательного текста
        ///// </summary>
        //private const int START_TEXT_MAX_POSITION = 33;


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
                if (courierEvents == null)
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

                    int iPos1 = log.IndexOf(' ', startMessagePos, 10);
                    if (iPos1 < 0) goto NextMessage;
                    int iPos2 = log.IndexOf(' ', iPos1 + 2, 10);
                    if (iPos2 < 0) goto NextMessage;
                    int iPos3 = log.IndexOf(' ', iPos2 + 2, 5);
                    if (iPos3 < 0) goto NextMessage;

                    if (!DateTime.TryParse(log.Substring(startMessagePos, iPos2 - startMessagePos).Trim(), out messageDateTime))
                        goto NextMessage;
                    if (!int.TryParse(log.Substring(startMessagePos, iPos3 - iPos2).Trim(), out messageNo) || messageNo <= 0)
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
                            break;
                        case 3: // Shipment response
                            break;
                        case 4: // Shipment error
                            break;
                        case 5: // Cancel request
                            break;
                        case 6: // Cancel post data
                            break;
                        case 7: // Cancel response
                            break;
                        case 8: // Cancel error
                            break;
                        case 9: // Courier events request
                            break;
                        case 10: // Courier events response
                            break;
                        case 11: // Courier events error
                            break;
                        case 12: // Order events request
                            break;
                        case 13: // Order events response
                            break;
                        case 14: // Order events error
                            break;
                        case 15: // Shop events request
                            break;
                        case 16: // Shop events response
                            break;
                        case 17: // Shop events error
                            break;
                        case 18: // ACK request
                            break;
                        case 19: // ACK post data
                            break;
                        case 20: // ACK response
                            break;
                        case 21: // ACK error
                            break;
                        case 22: // Time-Dist request
                            break;
                        case 23: // Time-Dist post data
                            break;
                        case 24: // Time-Dist response
                            break;
                        case 25: // Time-Dist error
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
                            break;
                        case 668: // Called method
                            break;
                        case 667: // Returned method rc
                            break;
                        case 666: // method exception
                            break;
                    }


                    //MessageHandler(messageDateTime, messageNo, messageData);


                    // 7.2 Переходим к следующему сообщению
                    NextMessage:
                    rc = 72;
                    startMessagePos = endMessagePos + 2;
                }

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

    }
}
