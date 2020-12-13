
namespace LogisticsService.Log
{
    using log4net;
    using log4net.Appender;
    using log4net.Core;
    using log4net.Layout;
    using log4net.Repository;
    using log4net.Repository.Hierarchy;

    /// <summary>
    /// Логгер
    /// </summary>
    public class Logger
    {
        /// <summary>
        /// Флаг: true - логгер создан; false - логгер не создан
        /// </summary>
        public bool IsCreated { get; private set; }

        /// <summary>
        /// Создание логгера
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="savedDays"></param>
        public static void Create(string filename, int savedDays)
        {
            Hierarchy hierarchy = (Hierarchy)LogManager.GetRepository();

            PatternLayout patternLayout = new PatternLayout();
            patternLayout.ConversionPattern = "@%date{dd.MM.yy HH:mm:ss} %property{msgNo} %-5level > %message%newline";
            patternLayout.ActivateOptions();

            RollingFileAppender roller = new RollingFileAppender();
            roller.AppendToFile = true;
            roller.File = @"Logs\CourierLogistics.log";
            roller.Layout = patternLayout;
            roller.PreserveLogFileNameExtension = true;
            roller.MaxSizeRollBackups = 5;
            roller.MaximumFileSize = "300MB";
            roller.RollingStyle = RollingFileAppender.RollingMode.Date;
            roller.Name = "CourierLogistics.Logger";
            roller.DatePattern = "_yy-MM-dd";
            roller.ImmediateFlush = true;
            roller.StaticLogFileName = false;
            
            roller.ActivateOptions();
            hierarchy.Root.AddAppender(roller);

            MemoryAppender memory = new MemoryAppender();
            memory.ActivateOptions();
            hierarchy.Root.AddAppender(memory);

            hierarchy.Root.Level = Level.Info;
            hierarchy.Configured = true;
            var logger = hierarchy.LoggerFactory.CreateLogger((ILoggerRepository)hierarchy, "myLogger");
            ILog myLogger = LogManager.GetLogger(hierarchy.Name, "myLogger");
            log4net.GlobalContext.Properties["myProperty"] = "2000";
            myLogger.Error("First message");


            //            ILogger logger = hierarchy.GetLogger("CourierLogistics.Logger");
            //LogManager.GetLogger(hierarchy.Name, )
            //                hierarchy.GetLogger()


            //logger.Log()
        }

    }
}
