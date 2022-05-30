

namespace DeliveryBuilderReports
{
    using System;
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


                using (StreamReader reader = new StreamReader(logFile))
                {
                    while ((line = reader.ReadLine()) != null)
                    {
                        // 4.1 Запись должна начинаться с "@"
                        rc = 41;
                        if (!line.StartsWith("@"))
                            continue;

                        // 4.2 Разбираем запись
                        rc = 42;
                        int iPos = line.IndexOf('>', 1);
                        if (iPos < 0)
                            continue;

                        string[] headerItems = line.Substring(1, iPos - 1).Trim().Split(' ');
                        if (headerItems == null || headerItems.Length != 4)
                            continue;

                        dateTime = DateTime.Parse(headerItems[0] + ' ' + headerItems[1]);
                        message_no = int.Parse(headerItems[2]);
                        severity = headerItems[3];
                        if (severity != null)
                            severity = severity.Trim();
                        message = line.Substring(iPos + 1).Trim();

                        // 4.3 Ошибки в Errors
                        rc = 43;
                        if ("Error".Equals(severity, StringComparison.CurrentCultureIgnoreCase) ||
                            "Warn".Equals(severity, StringComparison.CurrentCultureIgnoreCase))
                            report.AddErrorsRecord(dateTime, message_no, severity, message);




                    }

                }

                return rc;
            }
            catch
            {
                return rc;
            }
        }
    }
}
