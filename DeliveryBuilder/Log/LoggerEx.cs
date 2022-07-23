
namespace DeliveryBuilder.Log
{
    using System;
    using System.IO;

    /// <summary>
    /// Логгер
    /// </summary>
    public class Logger
    {
        /// <summary>
        /// Шаблон сообщения
        /// {0} - дата-время
        /// {1} - номер сообщения
        /// {2} - тип сообщения
        /// {3} - текст сообщения
        /// </summary>
        private const string MESSAGE_PATTERN = @"@{0} {1} {2} > {3}";

        /// <summary>
        /// Объект синхронизации
        /// </summary>
        private static object syncRoot = new object();

        /// <summary>
        /// ".../filename_{0}.log"
        /// </summary>
        private static string logFilePattern;

        /// <summary>
        /// Количество соханяемых ежедневных логов
        /// </summary>
        private static int savedDays;

        /// <summary>
        /// Флаг: true - логгер создан; false - логгер не создан
        /// </summary>
        public static bool IsCreated { get; private set; }

        /// <summary>
        /// Имя текущего файла лога
        /// </summary>
        public static string File => GetLogFileName();

        /// <summary>
        /// Создание логгера
        /// </summary>
        /// <param name="filename">Путь к файлу с логом</param>
        /// <param name="savedDays">Количество сохраняемых дней</param>
        public static int Create(string filename, int savedDays)
        {
            // 1. Инициализация
            int rc = 1;
            IsCreated = false;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (string.IsNullOrWhiteSpace(filename))
                    filename = "DeliveryBuilder.log"; 
                if (savedDays <= 0)
                    savedDays = 7;

                // 3. Создаём логгер
                rc = 3;
                logFilePattern = Path.Combine(Path.GetDirectoryName(filename), Path.GetFileNameWithoutExtension(filename) + "_{0}" + Path.GetExtension(filename));
                Logger.savedDays = savedDays;

                // 4. Выход - Ok
                rc = 0;
                IsCreated = true;
                return rc;
            }
            catch
            {
                return rc;
            }
        }

        /// <summary>
        /// Построить имя текущего файла ежедневного лога
        /// </summary>
        /// <returns>Имя файла лога</returns>
        private static string GetLogFileName()
        {
            return string.Format(logFilePattern, DateTime.Now.ToString("yy-MM-dd"));
        }

        /// <summary>
        /// Запись в лог
        /// </summary>
        /// <param name="message"></param>
        public static void WriteToLog(int msgNo, MessageSeverity severity, string message)
        {
            lock (syncRoot)
            {
                try
                {
                    // 2. Проверяем исходные данные
                    if (!IsCreated)
                        return;
                    if (string.IsNullOrWhiteSpace(message))
                        return;

                    // 3. Печатаем сообщение
                    string messageType = null;

                    switch (severity)
                    {
                        case MessageSeverity.Info:
                            messageType = "INFO";
                            break;
                        case MessageSeverity.Warn:
                            messageType = "WARN";
                            break;
                        case MessageSeverity.Error:
                            break;
                        default:
                            messageType = "INFO";
                            break;
                    }

                    using (StreamWriter sw = new StreamWriter(GetLogFileName(), true))
                    {
                        sw.WriteLine(string.Format(MESSAGE_PATTERN, DateTime.Now.ToString("dd.MM.yy HH:mm:ss.fff"), msgNo, messageType, message));
                        sw.Close();
                    }
                }
                catch
                { }
            }
        }
    }
}
