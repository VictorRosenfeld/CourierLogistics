
namespace DeliveryBuilderReports
{
    using System;
    using System.Collections.Generic;
    using System.IO;

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
                if (string.IsNullOrWhiteSpace(ordersFile) || !File.Exists(ordersFile))
                    return rc;
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
                DateTime dateTime;
                int message_no;
                string severity = "";
                string message = "";
                Dictionary<int, DeliveriesSummaryRecord> deliveriesSummary = new Dictionary<int, DeliveriesSummaryRecord>(1500);


                using (StreamReader reader = new StreamReader(logFile))
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
                        }

                    }

                }

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
