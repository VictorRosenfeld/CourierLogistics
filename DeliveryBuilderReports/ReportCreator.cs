
namespace DeliveryBuilderReports
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;

    /// <summary>
    /// Построитель отчета
    /// </summary>
    public class ReportCreator
    {
        /// <summary>
        /// Создание отчета
        /// <returns></returns>
        public static int Create(string logFile, string ordersFile, string resultFile)
        {
            // 1. Инициализация
            int rc = 1;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (string.IsNullOrWhiteSpace(logFile) || !File.Exists(logFile))
                    return rc;
                //if (string.IsNullOrWhiteSpace(ordersFile) || !File.Exists(ordersFile))
                //    return rc;
                if (string.IsNullOrWhiteSpace(resultFile))
                    return rc;

                // 3. Создаём объект с отчетом
                rc = 3;
                ExcelReport report = new ExcelReport();
                int rc1 = report.Create(resultFile);
                if (rc1 != 0)
                    return rc = 100 * rc + rc1;

                // 4. Читаем и разбиаем файл лога
                rc = 4;
                string line;
                DateTime dateTime = default(DateTime);
                int message_no;
                string severity = "";
                string message = "";
                Dictionary<int, DeliveriesSummaryRecord> deliveriesSummary = new Dictionary<int, DeliveriesSummaryRecord>(32);

                using (StreamReader reader = new StreamReader(logFile, Encoding.GetEncoding(1251)))
                {
                    while ((line = reader.ReadLine()) != null)
                    {
                        // 4.1 Запись должна начинаться с "@"
                        rc = 41;
                        if (!line.StartsWith("@"))
                            continue;

                        // 4.2 Разбираем сообщение
                        rc = 42;
                        int iPos = line.IndexOf('>', 1);
                        if (iPos < 0)
                            continue;

                        string[] headerItems = line.Substring(1, iPos - 1).Trim().Split(' ');
                        if (headerItems == null || headerItems.Length != 4)
                            continue;
                        if (!DateTime.TryParse(headerItems[0] + ' ' + headerItems[1], out dateTime))
                            continue;
                        if (!int.TryParse(headerItems[2], out message_no))
                            continue;

                        severity = headerItems[3];
                        if (severity != null)
                            severity = severity.Trim();
                        message = line.Substring(iPos + 1).Trim();

                        // 4.3 Ошибки в Errors
                        rc = 43;
                        if ("Error".Equals(severity, StringComparison.CurrentCultureIgnoreCase) ||
                            "Warn".Equals(severity, StringComparison.CurrentCultureIgnoreCase))
                            report.AddErrorsRecord(dateTime, message_no, severity, message);

                        // 4.4 Обрабаываем нужные сообщения
                        rc = 44;
                        switch (message_no)
                        {
                            case 95: // @"Send Deliveries. service_id = {0}, time = {1}, shop_id = {2}, type = {3}, dservice_id = {4}, courier_id = {5}, taxi = {6}, level = {7}, cost = {8}, interval = ({9} - {10}), cause = {11}, duration = {12}, orders = [{13}]"
                                int serviceId = GetIntParameterValue(message, "service_id");
                                DateTime deliveryTime  = GetDateTimeParameterValue(message, "time");
                                if (deliveryTime == DateTime.MinValue)
                                    deliveryTime = dateTime;

                                int shopId = GetIntParameterValue(message, "shop_id");
                                if (shopId == int.MinValue)
                                    shopId = 0;

                                int cmdType = GetIntParameterValue(message, "type");
                                if (cmdType != 1)
                                    continue;

                                int dserviceId = GetIntParameterValue(message, "dservice_id");
                                if (dserviceId == int.MinValue)
                                    dserviceId = 0;

                                int courierId = GetIntParameterValue(message, "courier_id");
                                if (courierId == int.MinValue)
                                    courierId = 0;

                                bool isTaxi = GetBoolParameterValue(message, "taxi");

                                int level = GetIntParameterValue(message, "level");
                                if (level == int.MinValue)
                                    level = 0;

                                double cost = GetDoubleParameterValue(message, "cost");
                                if (double.IsNaN(cost))
                                    cost = 0;

                                DateTime sdi  = GetDateTimeParameterValue(message, "interval", '(', '-');
                                if (sdi == DateTime.MinValue)
                                    sdi = new DateTime(2000, 1, 1);

                                DateTime edi  = GetDateTimeParameterValue(message, "interval", '-', ')');
                                if (edi == DateTime.MinValue)
                                    edi = new DateTime(2000, 1, 1);

                                string cause = GetParameterValue(message, "cause");

                                double executionTime = GetDoubleParameterValue(message, "duration");
                                if (double.IsNaN(executionTime))
                                    executionTime = 0;

                                string ordersList = GetParameterValue(message, "orders", '[', ']');

                                report.AddDeliveryRecord(deliveryTime, shopId, dserviceId, courierId, isTaxi, level, cost, sdi, edi, cause, executionTime, ordersList);

                                AddDeliveriesSummary(deliveriesSummary, level, cost);
                                break;
                            case 52: // RecalcDeliveries Exit. service_id = {0}, rc = {1}, shops = {2}, orders = {3}, elapsed_time = {4}
                                int rcCode = GetIntParameterValue(message, "rc");
                                if (rcCode == int.MinValue)
                                    rcCode = -1;

                                int shopCount = GetIntParameterValue(message, "shops");
                                if (shopCount == int.MinValue)
                                    shopCount = 0;

                                int orderCount = GetIntParameterValue(message, "orders");
                                if (orderCount == int.MinValue)
                                    orderCount = 0;

                                int elapsedTime = GetIntParameterValue(message, "elapsed_time");
                                if (elapsedTime == int.MinValue)
                                    elapsedTime = 0;

                                report.AddRecalcsRecord(dateTime, elapsedTime, orderCount, shopCount);
                                break;
                            case 35: // БД ExternalDb broken_count = {0}. Не удалось установить соединение
                            case 36: // БД ExternalDb broken_count = {0}. Не удалось установить соединение. Exception: {0}                                
                                int brokenCount = GetIntParameterValue(message, "broken_count", '=', '.');
                                if (brokenCount == int.MinValue)
                                    brokenCount = 0;

                                report.AddBrokenConnectionRecord(dateTime, brokenCount);
                                break;
                            case 90: // Send Heartbeat rc = {0}. service_id = {1}, elapsed_time = {2}
                                rcCode = GetIntParameterValue(message, "rc", '=', '.');
                                if (rcCode == int.MinValue)
                                    rcCode = -1;

                                elapsedTime = GetIntParameterValue(message, "elapsed_time");
                                if (elapsedTime == int.MinValue)
                                    elapsedTime = 0;

                                report.AddHeartbeatRecord(dateTime, rcCode, elapsedTime);
                                break;
                            case 91: // Send DataRequest rc = {0}. service_id = {1}, all = {2}, elapsed_time = {3}
                                rcCode = GetIntParameterValue(message, "rc", '=', '.');
                                if (rcCode == int.MinValue)
                                    rcCode = -1;

                                bool all = GetBoolParameterValue(message, "all");

                                elapsedTime = GetIntParameterValue(message, "elapsed_time");
                                if (elapsedTime == int.MinValue)
                                    elapsedTime = 0;

                                report.AddDataRequestRecord(dateTime, all, rcCode, elapsedTime);
                                break;
                            case 92: // Send ReceiveData rc = {0}. service_id = {1}, record_count = {2}, elapsed_time = {3}
                                rcCode = GetIntParameterValue(message, "rc", '=', '.');
                                if (rcCode == int.MinValue)
                                    rcCode = -1;

                                int recordCount = GetIntParameterValue(message, "record_count");
                                if (recordCount == int.MinValue)
                                    recordCount = 0;

                                elapsedTime = GetIntParameterValue(message, "elapsed_time");
                                if (elapsedTime == int.MinValue)
                                    elapsedTime = 0;

                                report.AddReceiveDataRecord(dateTime, recordCount, rcCode, elapsedTime);
                                break;
                            case 93: // Send RejectOrders rc = {0}. service_id = {1}, rejection_count = {2}, elapsed_time = {3}
                                rcCode = GetIntParameterValue(message, "rc", '=', '.');
                                if (rcCode == int.MinValue)
                                    rcCode = -1;

                                int rejectionCount = GetIntParameterValue(message, "rejection_count");
                                if (rejectionCount == int.MinValue)
                                    rejectionCount = 0;

                                elapsedTime = GetIntParameterValue(message, "elapsed_time");
                                if (elapsedTime == int.MinValue)
                                    elapsedTime = 0;

                                report.AddRejectOrderRecord(dateTime, rejectionCount, rcCode, elapsedTime);
                                break;
                            case 94: // Send Deliveries rc = {0}. service_id = {1}, delivery_count = {2}, elapsed_time = {3}
                                rcCode = GetIntParameterValue(message, "rc", '=', '.');
                                if (rcCode == int.MinValue)
                                    rcCode = -1;

                                int deliveryCount = GetIntParameterValue(message, "delivery_count");
                                if (deliveryCount == int.MinValue)
                                    deliveryCount = 0;

                                elapsedTime = GetIntParameterValue(message, "elapsed_time");
                                if (elapsedTime == int.MinValue)
                                    elapsedTime = 0;

                                report.AddSendDeliveriesRecord(dateTime, deliveryCount, rcCode, elapsedTime);
                                break;
                            case 123: // GeoYandex.GeoThread. mode = {0}, origins = {1}, destinations = {2}, Url = {3}
                                string mode = GetParameterValue(message, "mode");
                                int origins = GetIntParameterValue(message, "origins");
                                int destinations = GetIntParameterValue(message, "destinations");
                                report.AddYandexRequestRecord(dateTime, mode, origins, destinations);
                                break;
                        }
                    }
                }

                // 5. Deliveries Summary
                rc = 5;
                int ordersTotal = 0;
                double costTotal = 0;

                foreach (var record in deliveriesSummary.Values)
                {
                    ordersTotal += record.OrderCount;
                    costTotal += record.Cost;
                }

                int[] levels = new int[deliveriesSummary.Count];
                DeliveriesSummaryRecord[] records = new DeliveriesSummaryRecord[deliveriesSummary.Count];
                deliveriesSummary.Keys.CopyTo(levels, 0);
                deliveriesSummary.Values.CopyTo(records, 0);
                Array.Sort(levels, records);

                for (int i = 0; i < records.Length; i++)
                {
                    DeliveriesSummaryRecord record = records[i];
                    report.AddDeliverySummaryRecord(DateTime.Now.Date, record.Level, record.Count, record.Cost, record.OrderCount, record.AvgCost, 
                        (double)record.OrderCount / ordersTotal, record.Cost / costTotal);
                }

                // 6. Orders & Orders Summary
                rc = 6;

                if (!string.IsNullOrWhiteSpace(ordersFile) || File.Exists(ordersFile))
                {

                    int receivedOrders = 0;
                    int rejectedOrders = 0;
                    DateTime doi = dateTime.Date;
                    //     0          1       2        3              4                5         6               7               8             9         10       11         12
                    // ("order_id; shop_id; status; completed; time_check_disabled; received; assembled; rejection_reason; delivery_from; delivery_to; weight; latitude; longitude");

                    using (StreamReader reader = new StreamReader(ordersFile))
                    {
                        while ((line = reader.ReadLine()) != null)
                        {
                            // 6.1 Извлекаем поля записи
                            rc = 61;
                            if (string.IsNullOrWhiteSpace(line))
                                continue;
                            string[] fields = line.Split(';');
                            if (fields == null || fields.Length != 13)
                                continue;

                            // 6.2 Получаем значения полей
                            rc = 62;
                            int orderId;
                            if (!int.TryParse(fields[0], out orderId))
                                continue;

                            int shopId;
                            if (!int.TryParse(fields[1], out shopId))
                                continue;

                            if (string.IsNullOrWhiteSpace(fields[2]))
                                continue;
                            string status = fields[2].Trim();

                            bool completed;
                            if (!bool.TryParse(fields[3], out completed))
                                completed = false;

                            bool timeCheckDisabled;
                            if (!bool.TryParse(fields[4], out timeCheckDisabled))
                                timeCheckDisabled = false;

                            DateTime received;
                            if (!DateTime.TryParse(fields[5], out received) || received == DateTime.MinValue)
                                received = new DateTime(2000, 1, 1);

                            DateTime assembled;
                            if (!DateTime.TryParse(fields[6], out assembled) || assembled == DateTime.MinValue)
                                assembled = new DateTime(2000, 1, 1);

                            string rejectionReason = (string.IsNullOrWhiteSpace(fields[7]) ? null : fields[7].Trim());

                            DateTime deliveryFrom;
                            if (!DateTime.TryParse(fields[8], out deliveryFrom) || deliveryFrom == DateTime.MinValue)
                                deliveryFrom = new DateTime(2000, 1, 1);

                            DateTime deliveryTo;
                            if (!DateTime.TryParse(fields[9], out deliveryTo) || deliveryTo == DateTime.MinValue)
                                deliveryTo = new DateTime(2000, 1, 1);

                            double weight;
                            if (!double.TryParse(fields[10], out weight))
                                weight = 0;

                            double latitude;
                            if (!double.TryParse(fields[11], out latitude))
                                latitude = 0;

                            double longitude;
                            if (!double.TryParse(fields[12], out longitude))
                                longitude = 0;

                            // 6.3 Фильтруем по дате
                            rc = 63;
                            if (deliveryTo.Date != doi)
                                continue;

                            // 6.4 Выводим запись в Orders
                            rc = 64;
                            report.AddOrdersRecord(orderId, shopId, status, completed, timeCheckDisabled, received, assembled,
                                rejectionReason, deliveryFrom, deliveryTo, weight, latitude, longitude);

                            // 6.5 Подсчитываем Orders Summary
                            rc = 65;
                            receivedOrders++;
                            if (rejectionReason != null
                                && !"None".Equals(rejectionReason, StringComparison.CurrentCultureIgnoreCase))
                            { rejectedOrders++; }
                        }
                    }

                    // 6.6. Выводим запись в Orders Summary
                    rc = 66;
                    report.AddOrdersSummaryRecord(0, receivedOrders, rejectedOrders, ordersTotal);
                }

                // 7. Сохраняем отчет
                rc = 7;
                report.Save();

                // 8. Открываем отчет
                rc = 8;
                report.Show();

                // 9. Выход - Ok
                rc = 0;
                return rc;
            }
            catch
            {
                return rc;
            }
        }

        /// <summary>
        /// Извлечение значения параметра из сообщения
        /// </summary>
        /// <param name="message">Сообщение</param>
        /// <param name="parameterName">Имя параметра</param>
        /// <param name="valueSep">Символ-разделитель названия параметра и его значения</param>
        /// <param name="endSep">Символ-разделитель параметров</param>
        /// <returns>Значение или null</returns>
        private static string GetParameterValue(string message, string parameterName, char valueSep = '=', char endSep = ',')
        {
            // 1. Иициализация

            try
            {
                // 2. Проверяем исходные данные
                if (string.IsNullOrWhiteSpace(message))
                    return null;
                if (string.IsNullOrWhiteSpace(parameterName))
                    return null;

                // 3. Находим параметр в сообщении
                int parameterIndex = message.IndexOf(parameterName, StringComparison.CurrentCultureIgnoreCase);
                if (parameterIndex < 0)
                    return null;

                // 4. Находим value separator
                int valueSeparatorIndex = message.IndexOf(valueSep, parameterIndex + parameterName.Length);
                if (valueSeparatorIndex < 0)
                    return null;

                // 5. Находим end separator
                int endSeparatorIndex = message.IndexOf(endSep, valueSeparatorIndex + 1);
                if (endSeparatorIndex < 0)
                    endSeparatorIndex = message.Length;

                // 6. Взващаем значение
                return message.Substring(valueSeparatorIndex + 1, endSeparatorIndex - valueSeparatorIndex - 1).Trim();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Извлечение значения int-параметра из сообщения
        /// </summary>
        /// <param name="message">Сообщение</param>
        /// <param name="parameterName">Имя параметра</param>
        /// <param name="valueSep">Символ-разделитель названия параметра и его значения</param>
        /// <param name="endSep">Символ-разделитель параметров</param>
        /// <returns>Значение или int.MinValue</returns>
        private static int GetIntParameterValue(string message, string parameterName, char valueSep = '=', char endSep = ',')
        {
            try
            {
                string value = GetParameterValue(message, parameterName, valueSep, endSep);
                if (string.IsNullOrWhiteSpace(value))
                    return int.MinValue;
                int paramValue;
                if (int.TryParse(value, out paramValue))
                    return paramValue;
                return int.MinValue;
            }
            catch
            {
                return int.MinValue;
            }
        }

        /// <summary>
        /// Извлечение значения double-параметра из сообщения
        /// </summary>
        /// <param name="message">Сообщение</param>
        /// <param name="parameterName">Имя параметра</param>
        /// <param name="valueSep">Символ-разделитель названия параметра и его значения</param>
        /// <param name="endSep">Символ-разделитель параметров</param>
        /// <returns>Значение или double.NaN</returns>
        private static double GetDoubleParameterValue(string message, string parameterName, char valueSep = '=', char endSep = ',')
        {
            try
            {
                string value = GetParameterValue(message, parameterName, valueSep, endSep);
                if (string.IsNullOrWhiteSpace(value))
                    return double.NaN;
                double paramValue;
                if (double.TryParse(value, out paramValue))
                    return paramValue;
                return double.NaN;
            }
            catch
            {
                return double.NaN;
            }
        }

        /// <summary>
        /// Извлечение значения DateTime-параметра из сообщения
        /// </summary>
        /// <param name="message">Сообщение</param>
        /// <param name="parameterName">Имя параметра</param>
        /// <param name="valueSep">Символ-разделитель названия параметра и его значения</param>
        /// <param name="endSep">Символ-разделитель параметров</param>
        /// <returns>Значение или DateTime.MinValue</returns>
        private static DateTime GetDateTimeParameterValue(string message, string parameterName, char valueSep = '=', char endSep = ',')
        {
            try
            {
                string value = GetParameterValue(message, parameterName, valueSep, endSep);
                if (string.IsNullOrWhiteSpace(value))
                    return DateTime.MinValue;
                DateTime paramValue;
                if (DateTime.TryParse(value, out paramValue))
                    return paramValue;
                return DateTime.MinValue;
            }
            catch
            {
                return DateTime.MinValue;
            }
        }

        /// <summary>
        /// Извлечение значения bool-параметра из сообщения
        /// </summary>
        /// <param name="message">Сообщение</param>
        /// <param name="parameterName">Имя параметра</param>
        /// <param name="valueSep">Символ-разделитель названия параметра и его значения</param>
        /// <param name="endSep">Символ-разделитель параметров</param>
        /// <returns>Значение или false</returns>
        private static bool GetBoolParameterValue(string message, string parameterName, char valueSep = '=', char endSep = ',')
        {
            try
            {
                string value = GetParameterValue(message, parameterName, valueSep, endSep);
                if (string.IsNullOrWhiteSpace(value))
                    return false;
                bool paramValue;
                if (bool.TryParse(value, out paramValue))
                    return paramValue;
                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Пополнение Deliveries Summary
        /// </summary>
        /// <param name="summary">Deliveries Summary</param>
        /// <param name="orderCount">Количество заказов в отгрузке</param>
        /// <param name="cost">Стоимость отгрузки</param>
        private static void AddDeliveriesSummary(Dictionary<int, DeliveriesSummaryRecord> summary, int orderCount, double cost)
        {
            try
            {
                // 2. Проверяем исходные данные
                if (summary == null)
                    return;

                // 3. Извлекаем пополняемую запись
                DeliveriesSummaryRecord record;
                if (summary.TryGetValue(orderCount, out record))
                { record.AddDelivery(cost); }
                else
                {
                    record = new DeliveriesSummaryRecord(orderCount, cost);
                    summary.Add(orderCount, record);
                }
            }
            catch
            { }
        }
    }
}
