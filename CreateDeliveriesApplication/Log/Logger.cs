
namespace SQLCLRex.Log
{
    using System;
    using System.IO;

    /// <summary>
    /// Простой ежедневный логгер
    /// </summary>
    public static class Logger
    {
        private const string LOG_FILE_PATTERN = @"C:\LogisticsService\Log\LS_CLR1_{0}.log";

        /// <summary>
        /// Шаблон сообщения
        /// {0} - дата-время
        /// {1} - номер сообщения
        /// {2} - тип сообщения
        /// {3} - текст сообщения
        /// </summary>
        private const string MESSAGE_PATTERN = @"@{0} {1} {2} > {3}";

        /// <summary>
        /// Построить имя файла ежедневного лога
        /// </summary>
        /// <returns>Имя файла лога</returns>
        private static string GetLogFileName()
        {
            return string.Format(LOG_FILE_PATTERN, DateTime.Now.ToString("yyyy-MM-dd"));
        }

        private static object syncRoot = new object();

        /// <summary>
        /// Запись в лог
        /// </summary>
        /// <param name="messageNo">Номер сообщения</param>
        /// <param name="message">Текст сообщения</param>
        /// <param name="severity">Тип сообщения (-1 - не печатать; 0 - info; 1 - warn; 2 - error</param>
        public static void WriteToLog(int messageNo, string message, int severity)
        {
            lock (syncRoot)
            {
                try
                {
                    // 1. Форматируем тип сообщения
                    string messageType = null;
                    switch (severity)
                    {
                        case -1:
                            messageType = "";
                            break;
                        case 0:
                            messageType = "info";
                            break;
                        case 1:
                            messageType = "warn";
                            break;
                        case 2:
                            messageType = "error";
                            break;
                        default:
                            messageType = severity.ToString();
                            break;
                    }

                    // 2. Выводим в лог
                    using (StreamWriter sw = new StreamWriter(GetLogFileName(), true))
                    {
                        sw.WriteLine(string.Format(MESSAGE_PATTERN, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"), messageNo, messageType, message));
                        sw.Close();
                    }
                }
                catch
                { }
            }
        }
    }
}
