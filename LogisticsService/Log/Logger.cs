
namespace LogisticsService.Log
{
    using log4net;
    using log4net.Appender;
    using log4net.Core;
    using log4net.Layout;
    using log4net.Repository.Hierarchy;
    using System.Diagnostics;
    using System.IO;

    /// <summary>
    /// Логгер
    /// </summary>
    public class Logger
    {
        /// <summary>
        /// Имя свойства с номером сообщения
        /// </summary>
        private const string MESSAGE_NUMBER_PROPERTY = "msgNo";

        /// <summary>
        /// Флаг: true - логгер создан; false - логгер не создан
        /// </summary>
        public static bool IsCreated { get; private set; }

        /// <summary>
        /// Logger Appender
        /// </summary>
        private static RollingFileAppender appender;

        ///// <summary>
        ///// Logger Appender
        ///// </summary>
        //public static RollingFileAppender Appender => appender;

        /// <summary>
        /// log4net log
        /// </summary>
        private static ILog log;

        /// <summary>
        /// log4net log
        /// </summary>
        public static ILog Log => log;

        /// <summary>
        /// Имя текущего файла лога
        /// </summary>
        public static string File => (appender == null ? null : appender.File);

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
            log = null;
            appender = null;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (string.IsNullOrWhiteSpace(filename))
                    filename = $@"Logs\{Path.GetFileNameWithoutExtension(Process.GetCurrentProcess().MainModule.FileName)}.log";
                if (savedDays <= 0)
                    savedDays = 7;

                // 3. Создаём логгер
                rc = 3;
                Hierarchy hierarchy = (Hierarchy)LogManager.GetRepository();

                // 3.1 PatternLayout
                rc = 31;
                PatternLayout patternLayout = new PatternLayout();
                patternLayout.ConversionPattern = $"@%date{{dd.MM.yy HH:mm:ss}} %property{{{MESSAGE_NUMBER_PROPERTY}}} %-5level > %message%newline";
                patternLayout.ActivateOptions();

                // 3.2 Appender
                rc = 32;
                appender = new RollingFileAppender();
                appender.AppendToFile = true;
                appender.File = filename;
                appender.Layout = patternLayout;
                appender.PreserveLogFileNameExtension = true;
                appender.MaxSizeRollBackups = savedDays;
                appender.MaximumFileSize = "300MB";
                appender.RollingStyle = RollingFileAppender.RollingMode.Date;
                appender.Name = "CourierLogistics.Logger";
                appender.DatePattern = "_yy-MM-dd";
                appender.ImmediateFlush = true;
                appender.StaticLogFileName = false;
                //appender.LockingModel.ReleaseLock();

                appender.ActivateOptions();
                hierarchy.Root.AddAppender(appender);

                MemoryAppender memory = new MemoryAppender();
                memory.ActivateOptions();
                hierarchy.Root.AddAppender(memory);

                hierarchy.Root.Level = Level.Info;
                hierarchy.Configured = true;

                // 3.3 Собственно логгер
                rc = 33;
                ILogger logger = hierarchy.LoggerFactory.CreateLogger(hierarchy, "myLogger");
                log = LogManager.GetLogger(hierarchy.Name, "myLogger");

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
        /// Запись в лог
        /// </summary>
        /// <param name="message"></param>
        public static void WriteToLog(string message)
        {
            try
            {
                // 2. Проверяем исходные данные
                if (!IsCreated)
                    return;
                if (string.IsNullOrWhiteSpace(message))
                    return;

                // 3. Печатаем сообщение
                int iPos = message.IndexOf('>');
                if (iPos < 0)
                {
                    GlobalContext.Properties[MESSAGE_NUMBER_PROPERTY] = "";
                    log.Info(message);
                }
                else
                {
                    string text = message.Substring(iPos + 1).Trim();
                    string[] items = message.Substring(0, iPos).Trim().Split(' ');
                    if (items != null && items.Length == 2)
                    {
                        GlobalContext.Properties[MESSAGE_NUMBER_PROPERTY] = items[0];
                        switch (items[1].Trim().ToLower())
                        {
                            case "info":
                                log.Info(text);
                                break;
                            case "warning":
                            case "warn":
                                log.Warn(text);
                                break;
                            case "error":
                                log.Error(text);
                                break;
                            default:
                                log.Info(text);
                                break;
                        }
                    }
                }
            }
            catch
            { }
        }
    }
}
