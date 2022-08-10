
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
        /// Базовое имя файла лога (префикс названия с расшиением)
        /// </summary>
        private static string baseFilename;

        /// <summary>
        /// Количество сохраняемых ежедневных логов
        /// </summary>
        private static int savedDays;

        /// <summary>
        /// День последнего выведенного сообщения
        /// </summary>
        private static DateTime currentDate;

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

                currentDate = DateTime.Now.Date;
                baseFilename = Path.GetFileName(filename);
                Logger.savedDays = savedDays;


                // 3. Создаём логгер
                rc = 3;
                logFilePattern = Path.Combine(Path.GetDirectoryName(filename), Path.GetFileNameWithoutExtension(filename) + "_{0}" + Path.GetExtension(filename));
                RemoveLogFiles(Path.GetDirectoryName(GetLogFileName()), baseFilename, savedDays);
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
                            messageType = "ERROR";
                            break;
                        default:
                            messageType = "INFO";
                            break;
                    }

                    string logFile = GetLogFileName();
                    DateTime now = DateTime.Now;

                    using (StreamWriter sw = new StreamWriter(logFile, true))
                    {
                        sw.WriteLine(string.Format(MESSAGE_PATTERN, now.ToString("dd.MM.yy HH:mm:ss.fff"), msgNo, messageType, message));
                        sw.Close();
                    }

                    if (now.Date != currentDate)
                    {
                        RemoveLogFiles(Path.GetDirectoryName(logFile), baseFilename, savedDays);
                        currentDate = now.Date;
                    }
                }
                catch
                { }
            }
        }

        /// <summary>
        /// Удаление наиблее старых лог-файлов свыше заданного количества
        /// </summary>
        /// <param name="folder">Путь к папке с лог-файлами</param>
        /// <param name="filenamePrefix">Базовый префикс названия лог-файла</param>
        /// <param name="savedFiles">Количество сохраняемых файлов</param>
        /// <returns>0 - удаление успешно выполнено; иначе - удаление не выполнено</returns>
        private static int RemoveLogFiles(string folder, string filenamePrefix, int savedFiles = 7)
        {
            // 1. Инициализация
            int rc = 1;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
                    return rc;

                if (string.IsNullOrWhiteSpace(filenamePrefix))
                    return rc;

                if (savedFiles <= 0)
                    savedFiles = 7;

                // 3. Извлекаем имя файла и расширение
                rc = 3;
                string filename = Path.GetFileNameWithoutExtension(filenamePrefix);
                string fileext = Path.GetExtension(filenamePrefix);

                // 4. Извлекаем список файлов логов
                rc = 4;
                string filePattern = filename + "*";
                if (!string.IsNullOrWhiteSpace(fileext))
                    filePattern += fileext;
                string[] logFiles = Directory.GetFiles(folder, filePattern);

                if (logFiles == null || logFiles.Length <= savedFiles)
                    return rc = 0;

                // 5. Извлекаем время создания файлов 
                rc = 5;
                DateTime[] fileCreated = new DateTime[logFiles.Length];

                for (int i = 0; i < logFiles.Length; i++)
                {
                    fileCreated[i] = System.IO.File.GetCreationTime(logFiles[i]);
                }

                // 6. Сортируем названия файлов по времени создания
                rc = 6;
                Array.Sort(fileCreated, logFiles);

                // 7. Удаляем наиболее старые файлы
                rc = 7;
                int count = logFiles.Length - savedFiles;

                for (int i = 0; i < count; i++)
                {
                    try
                    {  System.IO.File.Delete(logFiles[i]); }
                    catch { }
                }

                // 8. Выход - Ok
                rc = 0;
                return rc;
            }
            catch
            { return rc; }
        }
    }
}
