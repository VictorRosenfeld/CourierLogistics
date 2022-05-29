
namespace DeliveryBuilder
{
    using DeliveryBuilder.AverageDeliveryCost;
    using DeliveryBuilder.BuilderParameters;
    using DeliveryBuilder.Couriers;
    using DeliveryBuilder.Db;
    using DeliveryBuilder.DeliveryCover;
    using DeliveryBuilder.Geo;
    using DeliveryBuilder.Log;
    using DeliveryBuilder.Orders;
    using DeliveryBuilder.Queue;
    using DeliveryBuilder.Recalc;
    using DeliveryBuilder.SalesmanProblemLevels;
    using DeliveryBuilder.Shops;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Reflection;
    using System.Text;
    using System.Threading;
    using System.Timers;
    using System.Xml.Serialization;

    /// <summary>
    /// Основной класс построителя отгрузок
    /// </summary>
    public class LogisticsService : IDisposable
    {
        /// <summary>
        /// ID сервиса логистики из Create-аргумента
        /// </summary>
        private int serviceId;

        /// <summary>
        /// ID сервиса логистики из Create-аргумента
        /// </summary>
        public int ServiceId => serviceId;

        /// <summary>
        /// Путь к файлу лога
        /// </summary>
        public string LogFile => Logger.File;

        /// <summary>
        /// Объект для работы с БД LSData
        /// </summary>
        private LSData lsDataDb;
        
        /// <summary>
        /// Объект для работы с внешней БД
        /// </summary>
        private ExternalDb externalDb;

        /// <summary>
        /// Флаг потери связи с ExternalDb 
        /// </summary>
        private int connectionBrokenCount;

        /// <summary>
        /// Объект для работы с БД LSData
        /// </summary>
        public LSData LSDataDb => lsDataDb;

        /// <summary>
        /// Объект для работы с вешней БД
        /// </summary>
        public ExternalDb ExternalDatabase => externalDb;

        /// <summary>
        /// Параметры сервиса 
        /// </summary>
        private BuilderConfig config;

        /// <summary>
        /// Параметры сервиса 
        /// </summary>
        public BuilderConfig Config => config;

        /// <summary>
        /// Мьютекс для синхронизации получения
        /// новых данных и их обработки
        /// </summary>
        private Mutex syncMutex;

        /// <summary>
        /// Объект для работы с гео-данными
        /// </summary>
        private GeoData geoData;
        
        /// <summary>
        /// Гео-кэш
        /// </summary>
        public GeoData Geo => geoData;

        /// <summary>
        /// Пороги для средней стоимсти доставки
        /// </summary>
        private AverageCostThresholds thresholds;

        /// <summary>
        /// Пороги для средней стоимсти доставки
        /// </summary>
        private AverageCostThresholds CostThresholds => thresholds;

        /// <summary>
        /// Все курьеры
        /// </summary>
        private AllCouriersEx couriers;

        /// <summary>
        /// Все курьеры
        /// </summary>
        public AllCouriersEx Couriers => couriers;

        /// <summary>
        /// Все магазины
        /// </summary>
        private AllShops shops;

        /// <summary>
        /// Все магазины
        /// </summary>
        public AllShops Shops => shops;

        /// <summary>
        /// Все заказы
        /// </summary>
        private AllOrdersEx orders;

        /// <summary>
        /// Все заказы
        /// </summary>
        public AllOrdersEx Orders => orders;

        /// <summary>
        /// Ограничения а число заказов в зависимости
        /// от глубины при полном переборе
        /// </summary>
        private SalesmanLevels limitations;

        /// <summary>
        /// Ограничения на число заказов в зависимости
        /// от глубины при полном переборе
        /// </summary>
        public SalesmanLevels Limitations => limitations;

        /// <summary>
        /// Очередь отгрузок
        /// </summary>
        private DeliveryQueue queue;

        /// <summary>
        /// Очередь отгрузок
        /// </summary>
        public DeliveryQueue Queue => queue;

        /// <summary>
        /// Таймер сервиса логистики
        /// </summary>
        private System.Timers.Timer timer;

        /// <summary>
        /// Количество тиков до сердцебиения
        /// </summary>
        private int hearbeatTickCount;

        /// <summary>
        /// Количество тиков до проверки очереди
        /// </summary>
        private int queueTickCount;

        /// <summary>
        /// Количество тиков до пересчета
        /// </summary>
        private int recalcTickCount;

        /// <summary>
        /// Количество тиков до чистки гео-кэша
        /// </summary>
        private int geoTickCount;

        /// <summary>
        /// Последнее прерывание
        /// </summary>
        public Exception LastException { get; private set; }

        /// <summary>
        /// Флаг: true - экземпляр создан; false - экземпляр не создан
        /// </summary>
        public bool IsCreated { get; private set; }

        /// <summary>
        /// Создание экземпляра
        /// </summary>
        /// <param name="serviceId">ID сервиса логистики</param>
        /// <param name="conectionString">Строка подключения к БД LSData</param>
        /// <returns>0 - экземпляр создан; иначе - экземпляр не создан</returns>
        public int Create(int serviceId, string conectionString, string logFolder = @".\Logs", int rollBackups = 7)
        {
            // 1. Инициализация
            int rc = 1;
            Dispose(true);
            this.serviceId = serviceId;
            disposedValue = false;
            IsCreated = false;
            LastException = null;
            FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);

            try
            {
                // 2. Открываем лог
                rc = 2;
                if (string.IsNullOrWhiteSpace(logFolder))
                    logFolder = @".\Logs";
                if (rollBackups <= 0)
                    rollBackups = 7;

                logFolder = Path.GetDirectoryName(logFolder);
                if (!Directory.Exists(logFolder))
                        Directory.CreateDirectory(logFolder);

                string logFile = $"{Path.GetFileNameWithoutExtension(fileVersionInfo.FileName)}({serviceId}).log";
                logFile = Path.Combine(logFolder, logFile);
                Logger.Create(logFile, rollBackups);

                // 3. Выводим сообщение о начале работы
                rc = 3;
                Logger.WriteToLog(1, MessageSeverity.Info, string.Format(Messages.MSG_001, Path.GetFileNameWithoutExtension(fileVersionInfo.FileName), fileVersionInfo.ProductVersion, serviceId));

                // 4. Проверяем исходные данные
                rc = 4;
                if (string.IsNullOrWhiteSpace(conectionString))
                {
                    Logger.WriteToLog(20, MessageSeverity.Error, Messages.MSG_020);
                    return rc;
                }

                // 5. Открываем соединение с БД LSData
                rc = 5;
                lsDataDb = new LSData(conectionString);
                lsDataDb.Open();
                if (!lsDataDb.IsOpen())
                {
                    string exceptionMessage = lsDataDb.GetLastErrorMessage();
                    if (string.IsNullOrEmpty(exceptionMessage))
                    { Logger.WriteToLog(21, MessageSeverity.Error, Messages.MSG_021); }
                    else
                    { Logger.WriteToLog(22, MessageSeverity.Error, string.Format(Messages.MSG_022, exceptionMessage)); }
                    return rc;
                }

                // 6. Загружаем параметры построителя
                rc = 6;
                config = lsDataDb.SelectConfig(serviceId);
                if (config == null)
                {
                    string exceptionMessage = lsDataDb.GetLastErrorMessage();
                    if (string.IsNullOrEmpty(exceptionMessage))
                    { Logger.WriteToLog(23, MessageSeverity.Error, Messages.MSG_023); }
                    else
                    { Logger.WriteToLog(24, MessageSeverity.Error, string.Format(Messages.MSG_024, exceptionMessage)); }
                    return rc;
                }

                // 7. Проверяем параметры построителя
                rc = 7;
                if (!TestBuilderConfig(config))
                {
                    Logger.WriteToLog(25, MessageSeverity.Error, Messages.MSG_025);
                    return rc;
                }

                // 8. Создаём объект для работы гео-данными
                rc = 8;
                geoData = new GeoData();
                int rc1 = geoData.Create(config);
                if (rc1 != 0)
                {
                    Logger.WriteToLog(26, MessageSeverity.Error, string.Format(Messages.MSG_026, rc1));
                    return rc = 10000 * rc + rc1;
                }

                // 9. Cоздаём объект для работы с порогами средней стоимости
                rc = 9;
                AverageDeliveryCostRecord[] records;
                rc1 = lsDataDb.SelectThresholds(out records);
                if (rc1 != 0)
                {
                    string exceptionMessage = lsDataDb.GetLastErrorMessage();
                    if (string.IsNullOrEmpty(exceptionMessage))
                    { Logger.WriteToLog(27, MessageSeverity.Error, Messages.MSG_027); }
                    else
                    { Logger.WriteToLog(28, MessageSeverity.Error, string.Format(Messages.MSG_028, exceptionMessage)); }
                    return rc = 10000 * rc + rc1;
                }

                thresholds = new AverageCostThresholds();
                rc1 = thresholds.Create(records);
                if (rc1 != 0)
                {
                    Logger.WriteToLog(29, MessageSeverity.Error, string.Format(Messages.MSG_029, rc1));
                    return rc = 10000 * rc + rc1;
                }

                // 10. Загружаем параметры способов отгрузки
                rc = 10;
                VehiclesRecord[] vehiclesRecords = null;
                rc1 = lsDataDb.SelectServiceVehicles(serviceId, out vehiclesRecords);
                if (rc1 != 0)
                {
                    string exceptionMessage = lsDataDb.GetLastErrorMessage();
                    if (string.IsNullOrEmpty(exceptionMessage))
                    { Logger.WriteToLog(27, MessageSeverity.Error, Messages.MSG_030); }
                    else
                    { Logger.WriteToLog(28, MessageSeverity.Error, string.Format(Messages.MSG_031, exceptionMessage)); }
                    return rc = 10000 * rc + rc1;
                }

                // 11. Создаём курьеров
                rc = 11;
                couriers = new AllCouriersEx();
                rc1 = couriers.Create(vehiclesRecords, config.Parameters.GeoYandex.TypeNames, thresholds);
                if (rc1 != 0)
                {
                    Logger.WriteToLog(32, MessageSeverity.Error, string.Format(Messages.MSG_032, rc1));
                    return rc = 10000 * rc + rc1;
                }

                // 12. Создаём магазины
                rc = 12;
                shops = new AllShops();

                // 13. Создаём заказы
                rc = 13;
                orders = new AllOrdersEx();
                rc1 = orders.Create();
                if (rc1 != 0)
                {
                    Logger.WriteToLog(33, MessageSeverity.Error, string.Format(Messages.MSG_033, rc1));
                    return rc = 10000 * rc + rc1;
                }

                // 14. Создаём Salesman Levels
                rc = 14;
                limitations = new SalesmanLevels();
                rc1 = limitations.Create(config.SalesmanLevels);
                if (rc1 != 0)
                {
                    Logger.WriteToLog(34, MessageSeverity.Error, string.Format(Messages.MSG_034, rc1));
                    return rc = 10000 * rc + rc1;
                }

                // 15. Создаём очередь
                rc = 15;
                queue = new DeliveryQueue();

                // 16. Устанавливаем счечики тиков
                rc = 16;
                //hearbeatTickCount = config.Parameters.HeartbeatInterval;
                hearbeatTickCount = -1;
                queueTickCount = config.Parameters.QueueInterval;
                //recalcTickCount = config.Parameters.RecalcInterval;
                recalcTickCount = -1;
                geoTickCount = config.Parameters.GeoCache.CheckInterval;

                // 17. Создаём и включаем таймер
                rc = 17;
                connectionBrokenCount = 1;
                syncMutex = new Mutex();
                timer = new System.Timers.Timer();
                timer.Interval = config.Parameters.TickInterval;
                timer.AutoReset = true;
                timer.Elapsed += Timer_Elapsed;
                timer.Start();
                ThreadPool.QueueUserWorkItem(TimerTick);

                // 18. Выход - Ok
                rc = 0;
                IsCreated = true;
                return rc;
            }
            catch (Exception ex)
            {
                Logger.WriteToLog(669, MessageSeverity.Error, string.Format(Messages.MSG_669, $"{nameof(LogisticsService)}.{nameof(this.Create)}", rc, (ex.InnerException == null ? ex.Message : ex.InnerException.Message)));
                LastException = ex;
                return rc;
            }
            finally
            {
                if (lsDataDb != null)
                { lsDataDb.Close(); }              
            }
        }

        /// <summary>
        /// Запуск обработчика события таймера
        /// </summary>
        /// <param name="status"></param>
        private void TimerTick(object status)
        {
            Timer_Elapsed(null, null);
        }

        /// <summary>
        /// Закрытие сервиса логистики
        /// </summary>
        public void Close()
        {
            FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);
            Logger.WriteToLog(1, MessageSeverity.Info, string.Format(Messages.MSG_002, Path.GetFileNameWithoutExtension(fileVersionInfo.FileName), fileVersionInfo.ProductVersion, serviceId));
            Dispose(true);
        }

        /// <summary>
        /// Проверка конфига
        /// </summary>
        /// <param name="config">Конфиг</param>
        /// <returns>true - конфиг проверен; false - конфиг содержит ошибки</returns>
        private static bool TestBuilderConfig(BuilderConfig config)
        {
            // 1. Инициализация
             
            try
            {
                // 2. Проверяем исходные данные
                if (config == null)
                {
                    Logger.WriteToLog(3, MessageSeverity.Error, Messages.MSG_003);
                    return false;
                }

                // 3. Проверка Fuctional Parameters
                bool passed = false;
                if (config.Parameters == null)
                { Logger.WriteToLog(4, MessageSeverity.Error, Messages.MSG_004); }
                else if (config.Parameters.CourierDeliveryMargin < 0)
                { Logger.WriteToLog(5, MessageSeverity.Error, string.Format(Messages.MSG_005, nameof(config.Parameters.CourierDeliveryMargin))); }
                else if (config.Parameters.TaxiDeliveryMargin < 0)
                { Logger.WriteToLog(5, MessageSeverity.Error, string.Format(Messages.MSG_005, nameof(config.Parameters.TaxiDeliveryMargin))); }
                else if (config.Parameters.СheckingMargin < 0)
                { Logger.WriteToLog(5, MessageSeverity.Error, string.Format(Messages.MSG_005, nameof(config.Parameters.СheckingMargin))); }
                else if (config.Parameters.TickInterval < 50)
                { Logger.WriteToLog(5, MessageSeverity.Error, string.Format(Messages.MSG_005, nameof(config.Parameters.TickInterval))); }
                else if (config.Parameters.HeartbeatInterval <= 0)
                { Logger.WriteToLog(5, MessageSeverity.Error, string.Format(Messages.MSG_005, nameof(config.Parameters.HeartbeatInterval))); }
                else if (config.Parameters.RecalcInterval <= 0)
                { Logger.WriteToLog(5, MessageSeverity.Error, string.Format(Messages.MSG_005, nameof(config.Parameters.RecalcInterval))); }
                else if (config.Parameters.QueueInterval <= 0)
                { Logger.WriteToLog(5, MessageSeverity.Error, string.Format(Messages.MSG_005, nameof(config.Parameters.QueueInterval))); }
                else if (config.Parameters.QueueCatchingInterval < 0)
                { Logger.WriteToLog(5, MessageSeverity.Error, string.Format(Messages.MSG_005, nameof(config.Parameters.QueueCatchingInterval))); }
                else if (config.Parameters.GeoCache == null)
                { Logger.WriteToLog(5, MessageSeverity.Error, string.Format(Messages.MSG_005, nameof(config.Parameters.GeoCache))); }
                else if (config.Parameters.GeoCache.Capacity < 100)
                { Logger.WriteToLog(5, MessageSeverity.Error, string.Format(Messages.MSG_005, nameof(config.Parameters.GeoCache.Capacity))); }
                else if (config.Parameters.GeoCache.SavingInterval < 10)
                { Logger.WriteToLog(5, MessageSeverity.Error, string.Format(Messages.MSG_005, nameof(config.Parameters.GeoCache.SavingInterval))); }
                else if (config.Parameters.GeoCache.CheckInterval <= 0)
                { Logger.WriteToLog(5, MessageSeverity.Error, string.Format(Messages.MSG_005, nameof(config.Parameters.GeoCache.CheckInterval))); }
                else if (config.Parameters.GeoYandex == null)
                { Logger.WriteToLog(5, MessageSeverity.Error, string.Format(Messages.MSG_005, nameof(config.Parameters.GeoYandex))); }
                else if (string.IsNullOrWhiteSpace(config.Parameters.GeoYandex.ApiKey))
                { Logger.WriteToLog(5, MessageSeverity.Error, string.Format(Messages.MSG_005, nameof(config.Parameters.GeoYandex.ApiKey))); }
                else if (config.Parameters.GeoYandex.CyclingRatio <= 0 || config.Parameters.GeoYandex.CyclingRatio > 1)
                { Logger.WriteToLog(5, MessageSeverity.Error, string.Format(Messages.MSG_005, nameof(config.Parameters.GeoYandex.CyclingRatio))); }
                else if (config.Parameters.GeoYandex.Timeout <= 0)
                { Logger.WriteToLog(5, MessageSeverity.Error, string.Format(Messages.MSG_005, nameof(config.Parameters.GeoYandex.Timeout))); }
                else if (config.Parameters.GeoYandex.TypeNames == null || config.Parameters.GeoYandex.TypeNames.Length <= 0)
                { Logger.WriteToLog(5, MessageSeverity.Error, string.Format(Messages.MSG_005, nameof(config.Parameters.GeoYandex.TypeNames))); }
                else if (string.IsNullOrWhiteSpace(config.Parameters.GeoYandex.Url))
                { Logger.WriteToLog(5, MessageSeverity.Error, string.Format(Messages.MSG_005, nameof(config.Parameters.GeoYandex.Url))); }
                else if (config.Parameters.GeoYandex.PairLimit <= 0)
                { Logger.WriteToLog(5, MessageSeverity.Error, string.Format(Messages.MSG_005, nameof(config.Parameters.GeoYandex.PairLimit))); }
                else if (config.Parameters.StartCondition == null)
                { Logger.WriteToLog(5, MessageSeverity.Error, string.Format(Messages.MSG_005, nameof(config.Parameters.StartCondition))); }
                else if (config.Parameters.ExternalDb == null)
                { Logger.WriteToLog(5, MessageSeverity.Error, string.Format(Messages.MSG_005, nameof(config.Parameters.ExternalDb))); }
                else if (string.IsNullOrWhiteSpace(config.Parameters.ExternalDb.СonnectionString))
                { Logger.WriteToLog(5, MessageSeverity.Error, string.Format(Messages.MSG_005, nameof(config.Parameters.ExternalDb.СonnectionString))); }
                else if (config.Parameters.ExternalDb.ConnectionTimeout <= 0)
                { Logger.WriteToLog(5, MessageSeverity.Error, string.Format(Messages.MSG_005, nameof(config.Parameters.ExternalDb.ConnectionTimeout))); }
                else if (config.Parameters.ExternalDb.CmdService == null)
                { Logger.WriteToLog(5, MessageSeverity.Error, string.Format(Messages.MSG_005, nameof(config.Parameters.ExternalDb.CmdService))); }
                else if (string.IsNullOrWhiteSpace(config.Parameters.ExternalDb.CmdService.Cmd1MessageType))
                { Logger.WriteToLog(5, MessageSeverity.Error, string.Format(Messages.MSG_005, nameof(config.Parameters.ExternalDb.CmdService.Cmd1MessageType))); }
                else if (string.IsNullOrWhiteSpace(config.Parameters.ExternalDb.CmdService.Cmd2MessageType))
                { Logger.WriteToLog(5, MessageSeverity.Error, string.Format(Messages.MSG_005, nameof(config.Parameters.ExternalDb.CmdService.Cmd2MessageType))); }
                else if (string.IsNullOrWhiteSpace(config.Parameters.ExternalDb.CmdService.Cmd3MessageType))
                { Logger.WriteToLog(5, MessageSeverity.Error, string.Format(Messages.MSG_005, nameof(config.Parameters.ExternalDb.CmdService.Cmd3MessageType))); }
                else if (string.IsNullOrWhiteSpace(config.Parameters.ExternalDb.CmdService.DataMessageType))
                { Logger.WriteToLog(5, MessageSeverity.Error, string.Format(Messages.MSG_005, nameof(config.Parameters.ExternalDb.CmdService.DataMessageType))); }
                else if (string.IsNullOrWhiteSpace(config.Parameters.ExternalDb.CmdService.HeartbeatMessageType))
                { Logger.WriteToLog(5, MessageSeverity.Error, string.Format(Messages.MSG_005, nameof(config.Parameters.ExternalDb.CmdService.HeartbeatMessageType))); }
                else if (string.IsNullOrWhiteSpace(config.Parameters.ExternalDb.CmdService.Name))
                { Logger.WriteToLog(5, MessageSeverity.Error, string.Format(Messages.MSG_005, nameof(config.Parameters.ExternalDb.CmdService.Name))); }
                else if (config.Parameters.ExternalDb.DataService == null)
                { Logger.WriteToLog(5, MessageSeverity.Error, string.Format(Messages.MSG_005, nameof(config.Parameters.ExternalDb.DataService))); }
                else if (string.IsNullOrWhiteSpace(config.Parameters.ExternalDb.DataService.CourierMesageType))
                { Logger.WriteToLog(5, MessageSeverity.Error, string.Format(Messages.MSG_005, nameof(config.Parameters.ExternalDb.DataService.CourierMesageType))); }
                else if (string.IsNullOrWhiteSpace(config.Parameters.ExternalDb.DataService.OrderMesageType))
                { Logger.WriteToLog(5, MessageSeverity.Error, string.Format(Messages.MSG_005, nameof(config.Parameters.ExternalDb.DataService.OrderMesageType))); }
                else if (string.IsNullOrWhiteSpace(config.Parameters.ExternalDb.DataService.QueueName))
                { Logger.WriteToLog(5, MessageSeverity.Error, string.Format(Messages.MSG_005, nameof(config.Parameters.ExternalDb.DataService.QueueName))); }
                else if (string.IsNullOrWhiteSpace(config.Parameters.ExternalDb.DataService.ShopMesageType))
                { Logger.WriteToLog(5, MessageSeverity.Error, string.Format(Messages.MSG_005, nameof(config.Parameters.ExternalDb.DataService.ShopMesageType))); }
                else if (config.Parameters.ExternalDb.DataService.ReceiveTimeout <= 0)
                { Logger.WriteToLog(5, MessageSeverity.Error, string.Format(Messages.MSG_005, nameof(config.Parameters.ExternalDb.DataService.ReceiveTimeout))); }
                else
                { passed = true; }

                if (!passed)
                    return false;

                // 4. Проверка Salesman Levels
                passed = true;

                if (config.SalesmanLevels == null || config.SalesmanLevels.Length <= 0)
                {
                    Logger.WriteToLog(6, MessageSeverity.Error, Messages.MSG_006);
                    passed = false;
                }
                else
                {
                    foreach (var slevel in config.SalesmanLevels)
                    {
                        if (slevel.Level <= 0 || slevel.Orders <= 0)
                        {
                            Logger.WriteToLog(6, MessageSeverity.Error, Messages.MSG_006);
                            passed = false;
                        }
                    }
                }

                if (!passed)
                    return false;

                // 5. Проверка Cloud Parameters
                passed = false;

                if (config.Cloud == null)
                { Logger.WriteToLog(7, MessageSeverity.Error, Messages.MSG_007);}
                else if (config.Cloud.Delta < 0)
                { Logger.WriteToLog(8, MessageSeverity.Error, string.Format(Messages.MSG_008, nameof(config.Cloud.Delta))); }
                else if (config.Cloud.Radius <= 0)
                { Logger.WriteToLog(8, MessageSeverity.Error, string.Format(Messages.MSG_008, nameof(config.Cloud.Radius))); }
                else if (config.Cloud.Size5 <= 0)
                { Logger.WriteToLog(8, MessageSeverity.Error, string.Format(Messages.MSG_008, nameof(config.Cloud.Size5))); }
                else if (config.Cloud.Size6 <= 0)
                { Logger.WriteToLog(8, MessageSeverity.Error, string.Format(Messages.MSG_008, nameof(config.Cloud.Size6))); }
                else if (config.Cloud.Size7 <= 0)
                { Logger.WriteToLog(8, MessageSeverity.Error, string.Format(Messages.MSG_008, nameof(config.Cloud.Size7))); }
                else if (config.Cloud.Size8 <= 0)
                { Logger.WriteToLog(8, MessageSeverity.Error, string.Format(Messages.MSG_008, nameof(config.Cloud.Size8))); }
                //else if (string.IsNullOrWhiteSpace(config.GroupId))
                //{ Logger.WriteToLog(9, MessageSeverity.Error, string.Format(Messages.MSG_009, nameof(config.GroupId))); }
                else
                { passed = true; }

                // 6. Выход
                return passed;
            }
            catch (Exception ex)
            {
                Logger.WriteToLog(669, MessageSeverity.Error, string.Format(Messages.MSG_669, $"{nameof(LogisticsService)}.{nameof(LogisticsService.TestBuilderConfig)}", false, (ex.InnerException == null ? ex.Message : ex.InnerException.Message)));
                //LastException = ex;
                return false;
            }
        }

        /// <summary>
        /// Событие таймера сервиса логистики
        /// </summary>
        /// <param name="sender">Таймер</param>
        /// <param name="e">Аргументы события</param>
        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            // 1. Иициализация
            ExternalDb db = null;
            bool isCatched = false;
            hearbeatTickCount--;
            geoTickCount--;
            recalcTickCount--;
            queueTickCount--;
            Logger.WriteToLog(73, MessageSeverity.Info, string.Format(Messages.MSG_073, hearbeatTickCount, geoTickCount, recalcTickCount, queueTickCount));

            try
            {
                // 0. Если экземпляр не создан
                if (!IsCreated || disposedValue)
                    return;

                // 1. Открываем соединение с ExternalDb
                db = new ExternalDb(config.Parameters.ExternalDb.СonnectionString);
                db.Open();
                if (!db.IsOpen())
                {
                    connectionBrokenCount++;
                    string exceptionMessage = lsDataDb.GetLastErrorMessage();
                    if (string.IsNullOrEmpty(exceptionMessage))
                    { Logger.WriteToLog(35, MessageSeverity.Error, string.Format(Messages.MSG_035, connectionBrokenCount)); }
                    else
                    { Logger.WriteToLog(36, MessageSeverity.Error, string.Format(Messages.MSG_036, connectionBrokenCount, exceptionMessage)); }
                    db.Close();
                    return;
                }

                // 2. Провеяем восстановление связи и делаем запрос данных
                if (connectionBrokenCount != 0)
                {
                    SendDataRequest(serviceId, config.Parameters.ExternalDb.CmdService.DataMessageType, true, db);
                    shops.SetAllShopUpdated();
                    connectionBrokenCount = 0;
                }
                else
                {
                    SendDataRequest(serviceId, config.Parameters.ExternalDb.CmdService.DataMessageType, false, db);
                }

                // 3. Отправляем Hearbeat
                if (hearbeatTickCount <= 0)
                {
                    SendHeartbeat(serviceId, config.Parameters.ExternalDb.CmdService.HeartbeatMessageType, db);
                    hearbeatTickCount = config.Parameters.HeartbeatInterval;
                }

                // 4. Пытаемся захватить mutex
                isCatched = syncMutex.WaitOne(100);
                if (!isCatched)
                    return;

                // 5. Чистим гео-данные
                if (geoTickCount <= 0)
                {
                    geoData.Cache.Refresh();
                    geoTickCount = config.Parameters.GeoCache.CheckInterval;
                }

                if (recalcTickCount <= 0)
                {
                    // 6. Обновляем данные
                    recalcTickCount = config.Parameters.RecalcInterval;
                    DataRecord[] dataRecords;
                    if (sender == null)
                    { Thread.Sleep(1000); }
                    int rc1 = ReceiveData(serviceId, db, out dataRecords);
                    if (rc1 == 0 && dataRecords != null && dataRecords.Length > 0)
                    { UpdateData(dataRecords); }

                    // 7. Пересчиываем отгрузки
                    RecalcDeliveries(db);
                }

                // 8. Диспетчируем очередь
                if (queueTickCount <= 0)
                {
                    DispatchQueue(db);
                    queueTickCount = config.Parameters.QueueInterval;
                }
            }
            catch (Exception ex)
            {
                Logger.WriteToLog(670, MessageSeverity.Error, string.Format(Messages.MSG_670, $"{nameof(LogisticsService)}.{nameof(this.Timer_Elapsed)}", (ex.InnerException == null ? ex.Message : ex.InnerException.Message)));
                LastException = ex;
            }
            finally
            {
                if (isCatched)
                {
                    try { syncMutex.ReleaseMutex(); } catch { }
                    isCatched = false;
                }
                if (db != null)
                {
                    try { db.Close(); } catch { }
                    db = null;
                }
            }
        }

        /// <summary>
        /// Диспетчеризация очереди 
        /// </summary>
        /// <returns></returns>
        private int DispatchQueue(ExternalDb db)
        {
            // 1. Инициализация
            int rc = 1;
            int saveCount = queue.Count;

            try
            {
                // 2. Проверяем исходные даные
                rc = 2;
                if (db == null || !db.IsOpen())
                    return rc;
                
                // 3. Извлекаем отгрузки требующие старта
                rc = 3;
                DateTime toDate = DateTime.Now.AddMinutes(config.Parameters.CourierDeliveryMargin);
                QueueItem[] queueItems = queue.GetToStart(toDate);
                if (queueItems == null || queueItems.Length <= 0)
                    return rc = 0;

                // 4. Отправляем команды на отгрузку
                rc = 4;
                CourierDeliveryInfo[] deliveries = new CourierDeliveryInfo[queueItems.Length];
                for (int i = 0; i < queueItems.Length; i++)
                { deliveries[i] = queueItems[i].Delivery; }

                int rc1 = SendDeliveries(serviceId, deliveries, 1, config.Parameters.ExternalDb.CmdService.Cmd2MessageType, couriers, db);
                if (rc1 != 0)
                {
                    Logger.WriteToLog(62, MessageSeverity.Error, string.Format(Messages.MSG_062, rc1));
                    return rc = 10000 * rc + rc1;
                }

                // 5. Помечаем отгрузки в очереди и заказы в них, как отгруженные
                rc = 5;
                for (int i = 0; i < queueItems.Length; i++)
                {
                    queueItems[i].ItemType = QueueItemType.Completed;
                    foreach (var order in queueItems[i].Delivery.Orders)
                    { order.Completed = false; }
                }

                // 6. Удаляем из очереди переданные отгрузки
                rc = 6;
                queue.Clear();

                // 7. Выход - Ok
                rc = 0;
                return rc;
            }
            catch (Exception ex)
            {
                Logger.WriteToLog(669, MessageSeverity.Error, string.Format(Messages.MSG_669, $"{nameof(LogisticsService)}.{nameof(this.DispatchQueue)}", rc, (ex.InnerException == null ? ex.Message : ex.InnerException.Message)));
                LastException = ex;
                return rc;
            }
            finally
            {
                Logger.WriteToLog(74, MessageSeverity.Info, string.Format(Messages.MSG_074, saveCount, queue.Count));
            }
        }

        /// <summary>
        /// Отправка сердцебиения
        /// </summary>
        /// <param name="serviceId">ID сервиса логистики</param>
        /// <param name="messageType">Тип сообщения</param>
        /// <param name="db">БД ExteralDb</param>
        /// <returns>0 - команда отправлена; иначе - команда не оправлена</returns>
        private static int SendHeartbeat(int serviceId, string messageType, ExternalDb db)
        {
            // 1. Иициализация
            Stopwatch sw = new Stopwatch();
            sw.Start();
            int rc = 1;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (db == null || !db.IsOpen())
                    return rc;
                if (string.IsNullOrWhiteSpace(messageType))
                    return rc;

                // 3. Отправляем сердцебиение
                rc = 3;
                string heartbeat = $@"<heartbeat service_id=""{serviceId}"" />";
                string errorMessage;

                //Logger.WriteToLog(72, MessageSeverity.Info, string.Format(Messages.MSG_072, messageType, heartbeat));

                int rc1 = db.SendXmlCmd(serviceId, messageType, heartbeat, out errorMessage);
                if (rc1 != 0)
                {
                    if (string.IsNullOrWhiteSpace(errorMessage))
                    { Logger.WriteToLog(37, MessageSeverity.Warn, string.Format(Messages.MSG_037, rc1)); }
                    else
                    { Logger.WriteToLog(38, MessageSeverity.Warn, string.Format(Messages.MSG_038, rc1, errorMessage)); }
                    return rc = 1000 * rc + rc1;
                }

                // 4. Выход - Ok
                rc = 0;
                return rc;
            }
            catch (Exception ex)
            {
                Logger.WriteToLog(38, MessageSeverity.Error, string.Format(Messages.MSG_038, rc, (ex.InnerException == null ? ex.Message : ex.InnerException.Message)));
                return rc;
            }
            finally
            {
                sw.Stop();
                Logger.WriteToLog(90, MessageSeverity.Info, string.Format(Messages.MSG_090, rc, serviceId, sw.ElapsedMilliseconds));
            }
        }

        /// <summary>
        /// Отправка запроса данных
        /// </summary>
        /// <param name="serviceId">ID сервиса логистики</param>
        /// <param name="messageType">Тип сообщения</param>
        /// <param name="allData">Флаг: true - все сообщения; false - только новые</param>
        /// <param name="db">БД ExteralDb</param>
        /// <returns>0 - запрос отправлен; иначе - запрос не оправлен</returns>
        private static int SendDataRequest(int serviceId, string messageType, bool allData, ExternalDb db)
        {
            // 1. Иициализация
            Stopwatch sw = new Stopwatch();
            sw.Start();
            int rc = 1;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (db == null || !db.IsOpen())
                    return rc;
                if (string.IsNullOrWhiteSpace(messageType))
                    return rc;

                // 3. Отправляем запрос данных
                rc = 3;
                string request = $@"<data service_id=""{serviceId}"" all=""{(allData ? 1 : 0)}""/>";

                //Logger.WriteToLog(71, MessageSeverity.Info, string.Format(Messages.MSG_071, messageType, request));

                string errorMessage;
                int rc1 = db.SendXmlCmd(serviceId, messageType, request, out errorMessage);
                if (rc1 != 0)
                {
                    if (string.IsNullOrWhiteSpace(errorMessage))
                    { Logger.WriteToLog(39, MessageSeverity.Warn, string.Format(Messages.MSG_039, rc1)); }
                    else
                    { Logger.WriteToLog(40, MessageSeverity.Warn, string.Format(Messages.MSG_040, rc1, errorMessage)); }
                    return rc = 1000 * rc + rc1;
                }

                // 4. Выход - Ok
                rc = 0;
                return rc;
            }
            catch (Exception ex)
            {
                Logger.WriteToLog(38, MessageSeverity.Error, string.Format(Messages.MSG_040, rc, (ex.InnerException == null ? ex.Message : ex.InnerException.Message)));
                return rc;
            }
            finally
            {
                sw.Stop();
                Logger.WriteToLog(91, MessageSeverity.Info, string.Format(Messages.MSG_091, rc, serviceId, allData, sw.ElapsedMilliseconds));
            }
        }

        /// <summary>
        /// Отправка команд на отгрузку или рекомендаций
        /// </summary>
        /// <param name="serviceId">ID сервиса логистики</param>
        /// <param name="deliveries">Отрузки или рекомендации</param>
        /// <param name="cmdType">Тип команды: 0 - рекомендации; 1 - отгрузки</param>
        /// <param name="messageTypeName">Наименование типа отправляемого сообщения</param>
        /// <param name="couriers">Все курьеры</param>
        /// <param name="db">External DB</param>
        /// <returns>0 - команда отправлена; иначе - команда не отправлена;</returns>
        private static int SendDeliveries(int serviceId, CourierDeliveryInfo[] deliveries, int cmdType, string messageTypeName, AllCouriersEx couriers, ExternalDb db)
        {
            // 1. Иициализация
            Stopwatch sw = new Stopwatch();
            sw.Start();
            int rc = 1;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (deliveries == null || deliveries.Length <= 0)
                    return rc;
                if (!(cmdType == 0 || cmdType == 1))
                    return rc;
                if (string.IsNullOrWhiteSpace(messageTypeName))
                    return rc;
                if (couriers == null || !couriers.IsCreated)
                    return rc;
                if (db == null || !db.IsOpen())
                    return rc;

                // 3. Создаём xml-команду
                rc = 3;
                string xmlCmd;
                int rc1 = CreateDeliveryCmd(deliveries, cmdType, serviceId, couriers, out xmlCmd);
                if (rc1 != 0)
                {
                    Logger.WriteToLog(cmdType == 0 ? 55 : 58, MessageSeverity.Warn, string.Format(cmdType == 0 ? Messages.MSG_055 : Messages.MSG_058, rc1));
                    return rc = 1000 * rc + rc1;
                }

                Logger.WriteToLog(69, MessageSeverity.Info, string.Format(Messages.MSG_069, cmdType, messageTypeName, xmlCmd));

                // 4. Отправляем xml-команду
                rc = 4;
                string errorMessage;
                rc1 = db.SendXmlCmd(serviceId, messageTypeName, xmlCmd, out errorMessage);
                if (rc1 != 0)
                {
                    if (string.IsNullOrWhiteSpace(errorMessage))
                    { Logger.WriteToLog(cmdType == 0 ? 56 : 59, MessageSeverity.Warn, string.Format(cmdType == 0 ? Messages.MSG_056 : Messages.MSG_059, rc1)); }
                    else
                    { Logger.WriteToLog(cmdType == 0 ? 57 : 60, MessageSeverity.Warn, string.Format(cmdType == 0 ? Messages.MSG_057 : Messages.MSG_060, rc1, errorMessage)); }

                    Logger.WriteToLog(61, MessageSeverity.Warn, string.Format(Messages.MSG_061, xmlCmd));
                    return rc = 10 * rc + rc1;
                }

                // 5. Выход - Ok
                rc = 0;
                return rc;
            }
            catch (Exception ex)
            {
                Logger.WriteToLog(669, MessageSeverity.Error, string.Format(Messages.MSG_669, $"{nameof(LogisticsService)}.{nameof(LogisticsService.SendDeliveries)}", rc, (ex.InnerException == null ? ex.Message : ex.InnerException.Message)));
                return rc;
            }
            finally
            {
                sw.Stop();
                Logger.WriteToLog(94, MessageSeverity.Info, string.Format(Messages.MSG_094, rc, serviceId, (deliveries == null ? 0 : deliveries.Length), sw.ElapsedMilliseconds));
            }
        }

        /// <summary>
        /// Отказ в доставке заказов
        /// </summary>
        /// <param name="rejectedOrders">Отвергнутые заказы</param>
        /// <param name="serviceId">ID сервиса логистики</param>
        /// <param name="messageType">Тип сообщения</param>
        /// <param name="orders">Все заказы</param>
        /// <param name="db">Exteral DB</param>
        /// <returns>0 - отказы переданы; иначе - отказы не переданы</returns>
        private static int RejectOrders(OrderRejectionCause[] rejectedOrders, int serviceId, string messageType, AllOrdersEx orders, ExternalDb db)
        {
            // 1. Инициализация
            Stopwatch sw = new Stopwatch();
            sw.Start();
            int rc = 1;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (rejectedOrders == null || rejectedOrders.Length <= 0)
                    return rc;
                if (db == null || !db.IsOpen())
                    return rc;
                if (orders == null || !orders.IsCreated)
                    return rc;

                // 3. Создаём xml-аргумент для процедуры lsvSendRejectedOrders
                rc = 3;
                StringBuilder sb = new StringBuilder(75 * (rejectedOrders.Length + 2));
                //sb.AppendLine(@"<?xml version=""1.0"" encoding=""utf-8""?>");

                sb.AppendLine($@"<rejections service_id=""{serviceId}"">");
                int count = 0;

                for (int i = 0; i < rejectedOrders.Length; i++)
                {
                    // 3.1 Пропукаем заказы, для которых нет доступного курьера
                    rc = 31;
                    OrderRejectionCause rejectedOrder = rejectedOrders[i];
                    if (rejectedOrder.Reason == OrderRejectionReason.CourierNA || rejectedOrder.Reason == OrderRejectionReason.None)
                        continue;
                    sb.AppendLine($@"<rejection id=""{rejectedOrder.OrderId}"" type_id=""{rejectedOrder.VehicleId}"" reason=""{rejectedOrder.Reason}"" error_code=""{rejectedOrder.ErrorCode}""/>");
                    count++;
                }

                if (count <= 0)
                    return rc = 0;
                sb.AppendLine("</rejections>");

                Logger.WriteToLog(70, MessageSeverity.Info, string.Format(Messages.MSG_070, 3, messageType, sb.ToString()));

                // 4. Вызываем процедуру для отправки отмены
                rc = 4;
                string errorMessage;
                int rc1 = db.SendXmlCmd(serviceId, messageType, sb.ToString(), out errorMessage);
                if (rc1 != 0)
                {
                    if (string.IsNullOrWhiteSpace(errorMessage))
                    { Logger.WriteToLog(53, MessageSeverity.Warn, string.Format(Messages.MSG_053, rc1)); }
                    else
                    { Logger.WriteToLog(54, MessageSeverity.Warn, string.Format(Messages.MSG_054, rc1, errorMessage)); }
                    return rc = 1000 * rc + rc1;
                }

                // 5. У отмененных заказов устанавливаем completed = 1 и код причины отказа
                rc = 5;
                for (int i = 0; i < rejectedOrders.Length; i++)
                {
                    // 3.1 Пропукаем заказы, для которых нет доступного курьера
                    rc = 31;
                    OrderRejectionCause rejectedOrder = rejectedOrders[i];
                    if (rejectedOrder.Reason != OrderRejectionReason.CourierNA && rejectedOrder.Reason != OrderRejectionReason.None)
                    {
                        orders.SetCompleted(rejectedOrder.OrderId, true);
                    }
                }

                // 6. Выход - Ok
                rc = 0;
                return rc;
            }
            catch (Exception ex)
            {
                Logger.WriteToLog(669, MessageSeverity.Error, string.Format(Messages.MSG_669, $"{nameof(LogisticsService)}.{nameof(LogisticsService.RejectOrders)}", rc, (ex.InnerException == null ? ex.Message : ex.InnerException.Message)));
                return rc;
            }
            finally
            {
                sw.Stop();
                Logger.WriteToLog(93, MessageSeverity.Info, string.Format(Messages.MSG_093, rc, serviceId, (rejectedOrders == null ? 0 : rejectedOrders.Length), sw.ElapsedMilliseconds));
            }
        }

        /// <summary>
        /// Получение данных
        /// </summary>
        /// <param name="serviceId">ID сервиса логистики</param>
        /// <param name="db">БД ExteralDb</param>
        /// <returns>0 - запрос отправлен; иначе - запрос не оправлен</returns>
        private static int ReceiveData(int serviceId, ExternalDb db, out DataRecord[] records)
        {
            // 1. Иициализация
            Stopwatch sw = new Stopwatch();
            sw.Start();
            int rc = 1;
            records = null;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (db == null || !db.IsOpen())
                    return rc;

                // 3. Отправляем запрос данных
                rc = 3;
                string errorMessage;
                int rc1 = db.ReceiveData(serviceId, out records, out errorMessage);
                if (rc1 != 0)
                {
                    if (string.IsNullOrWhiteSpace(errorMessage))
                    { Logger.WriteToLog(41, MessageSeverity.Warn, string.Format(Messages.MSG_041, rc1)); }
                    else
                    { Logger.WriteToLog(42, MessageSeverity.Warn, string.Format(Messages.MSG_042, rc1, errorMessage)); }
                    return rc = 1000 * rc + rc1;
                }

                PrintDataRecords(records);

                // 4. Выход - Ok
                rc = 0;
                return rc;
            }
            catch (Exception ex)
            {
                Logger.WriteToLog(42, MessageSeverity.Error, string.Format(Messages.MSG_042, rc, (ex.InnerException == null ? ex.Message : ex.InnerException.Message)));
                return rc;
            }
            finally
            {
                sw.Stop();
                Logger.WriteToLog(92, MessageSeverity.Info, string.Format(Messages.MSG_092, rc, serviceId, (records == null ? 0 : records.Length), sw.ElapsedMilliseconds));
            }
        }

        /// <summary>
        /// Вывод полученых данных в лог
        /// </summary>
        /// <param name="records">Записи даных</param>
        private static void PrintDataRecords(DataRecord[] records)
        {
            try
            {
                // 2. Провеяем исхдные данные
                if (records == null || records.Length <= 0)
                    return;

                // 3. Выводим даные в лог
                for (int i = 0; i < records.Length; i++)
                {
                    DataRecord record = records[i];
                    Logger.WriteToLog(68, MessageSeverity.Info, string.Format(Messages.MSG_068, record.QueuingOrder, record.MessageTypeName, record.MessageBody));
                }
            }
            catch
            { }
        }

        /// <summary>
        /// Обновление данных о магазинах, курьерах и заказах
        /// </summary>
        /// <param name="dataRecords">Записи с данными</param>
        /// <returns>0 - данные обновлены; иначе - данные не обновлены</returns>
        private int UpdateData(DataRecord[] dataRecords)
        {
            // 1. Инициализация
            int rc = 1;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (dataRecords == null || dataRecords.Length <= 0)
                    return rc = 0;

                // 3. Извлекаем типы сообщений
                rc = 3;
                string shopMessageType = config.Parameters.ExternalDb.DataService.ShopMesageType;
                string orderMessageType = config.Parameters.ExternalDb.DataService.OrderMesageType;
                string courierMessageType = config.Parameters.ExternalDb.DataService.CourierMesageType;

                // 4. Сортируем сообщения по номеру
                rc = 4;
                Array.Sort(dataRecords, CompareDataRecord);

                // 5. Обабатываем записи с данными
                rc = 5;
                XmlSerializer ordersSerializer = new XmlSerializer(typeof(OrdersUpdates));
                XmlSerializer couriersSerializer = new XmlSerializer(typeof(CouriersUpdates));
                XmlSerializer shopsSerializer = new XmlSerializer(typeof(ShopsUpdates));

                for (int i = 0; i < dataRecords.Length; i++)
                {
                    // 5.1 Извлекаем запись
                    rc = 51;
                    DataRecord dr = dataRecords[i];
                    if (string.IsNullOrWhiteSpace(dr.MessageTypeName) ||
                        string.IsNullOrWhiteSpace(dr.MessageBody))
                        continue;

                    // 5.2 Десериализуем и обрабатываем данные
                    rc = 52;
                    if (dr.MessageTypeName.Equals(orderMessageType, StringComparison.CurrentCultureIgnoreCase))
                    {
                        try
                        {
                            OrdersUpdates ordersUpdates;
                            using (StringReader sr = new StringReader(dr.MessageBody))
                            {
                                ordersUpdates = (OrdersUpdates)ordersSerializer.Deserialize(sr);
                            }

                            PrintOrders(ordersUpdates);

                            int rc1 = orders.Update(ordersUpdates, shops, couriers);
                            if (rc1 != 0)
                            {
                                Logger.WriteToLog(45, MessageSeverity.Warn, string.Format(Messages.MSG_045, rc1, dr.MessageBody));
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.WriteToLog(44, MessageSeverity.Warn, string.Format(Messages.MSG_044, dr.MessageTypeName, dr.MessageBody));
                            Logger.WriteToLog(44, MessageSeverity.Warn, string.Format(Messages.MSG_044, dr.MessageTypeName, (ex.InnerException == null ? ex.Message : ex.InnerException.Message)));
                        }
                    }
                    if (dr.MessageTypeName.Equals(courierMessageType, StringComparison.CurrentCultureIgnoreCase))
                    {
                        try
                        {
                            CouriersUpdates couriersUpdates;
                            using (StringReader sr = new StringReader(dr.MessageBody))
                            {
                                couriersUpdates = (CouriersUpdates) couriersSerializer.Deserialize(sr);
                            }

                            PrintCouriers(couriersUpdates);

                            int rc1 = couriers.Update(couriersUpdates, thresholds, shops);
                            if (rc1 != 0)
                            {
                                Logger.WriteToLog(46, MessageSeverity.Warn, string.Format(Messages.MSG_046, rc1, dr.MessageBody));
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.WriteToLog(44, MessageSeverity.Warn, string.Format(Messages.MSG_044, dr.MessageTypeName, dr.MessageBody));
                            Logger.WriteToLog(44, MessageSeverity.Warn, string.Format(Messages.MSG_044, dr.MessageTypeName, (ex.InnerException == null ? ex.Message : ex.InnerException.Message)));
                        }
                    }
                    if (dr.MessageTypeName.Equals(shopMessageType, StringComparison.CurrentCultureIgnoreCase))
                    {
                        try
                        {
                            ShopsUpdates shopsUpdates;
                            using (StringReader sr = new StringReader(dr.MessageBody))
                            {
                                shopsUpdates = (ShopsUpdates) shopsSerializer.Deserialize(sr);
                            }

                            PrintShops(shopsUpdates);

                            int rc1 = shops.Update(shopsUpdates);
                            if (rc1 != 0)
                            {
                                Logger.WriteToLog(47, MessageSeverity.Warn, string.Format(Messages.MSG_047, rc1, dr.MessageBody));
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.WriteToLog(44, MessageSeverity.Warn, string.Format(Messages.MSG_044, dr.MessageTypeName, dr.MessageBody));
                            Logger.WriteToLog(44, MessageSeverity.Warn, string.Format(Messages.MSG_044, dr.MessageTypeName, (ex.InnerException == null ? ex.Message : ex.InnerException.Message)));
                        }
                    }
                    else
                    {
                        Logger.WriteToLog(43, MessageSeverity.Warn, string.Format(Messages.MSG_043, dr.MessageTypeName));
                    }
                }

                // 6. Выход - Ok
                rc = 0;
                return rc;
            }
            catch (Exception ex)
            {
                Logger.WriteToLog(669, MessageSeverity.Error, string.Format(Messages.MSG_669, $"{nameof(LogisticsService)}.{nameof(this.UpdateData)}", rc, (ex.InnerException == null ? ex.Message : ex.InnerException.Message)));
                LastException = ex;
                return rc;
            }
        }

        /// <summary>
        /// Вывод принятых заказов в лог
        /// </summary>
        /// <param name="ordersUpdates">Принятые заказы</param>
        private static void PrintOrders(OrdersUpdates ordersUpdates)
        {
            try
            {
                // 2. Проверяем исходные данные
                if (ordersUpdates == null || ordersUpdates.Updates == null || ordersUpdates.Updates.Length <= 0)
                    return;

                // 3. Выводим иформацию  заказаx
                foreach (var orderUpdates in ordersUpdates.Updates)
                {
                    Logger.WriteToLog(87, MessageSeverity.Info,
                        string.Format(Messages.MSG_087,
                                      ordersUpdates.ServiceId, // {0}
                                      orderUpdates.ShopId,     // {1}
                                      orderUpdates.OrderId,    // {2}
                                      orderUpdates.Status,     // {3}
                                      (orderUpdates.TimeCheckDisabled ? "-" : "+"),  // {4}
                                      orderUpdates.TimeFrom,   // {5}
                                      orderUpdates.TimeTo,     // {6}
                                      orderUpdates.Weight,     // {7}
                                      FormatDServices(orderUpdates.DServices)   // {8}
                        ));
                }
            }
            catch
            { }
        }

        /// <summary>
        /// Вывод принятых магазинов в лог
        /// </summary>
        /// <param name="shopsUpdates">Принятые магазины</param>
        private static void PrintShops(ShopsUpdates shopsUpdates)
        {
            try
            {
                // 2. Проверяем исходные данные
                if (shopsUpdates == null || shopsUpdates.Updates == null || shopsUpdates.Updates.Length <= 0)
                    return;

                // 3. Выводим иформацию  магазинах
                foreach (var shopUpdates in shopsUpdates.Updates)
                {
                    Logger.WriteToLog(88, MessageSeverity.Info,
                        string.Format(Messages.MSG_088,
                                      shopsUpdates.ServiceId, // {0}
                                      shopUpdates.ShopId,     // {1}
                                      shopUpdates.Latitude,   // {2}
                                      shopUpdates.Longitude,  // {3}
                                      shopUpdates.WorkStart,  // {4}
                                      shopUpdates.WorkEnd     // {5}
                        ));
                }
            }
            catch
            { }
        }

        /// <summary>
        /// Вывод принятых курьеров в лог
        /// </summary>
        /// <param name="couriersUpdates">Принятые курьеры</param>
        private static void PrintCouriers(CouriersUpdates couriersUpdates)
        {
            try
            {
                // 2. Проверяем исходные данные
                if (couriersUpdates == null || couriersUpdates.Updates == null || couriersUpdates.Updates.Length <= 0)
                    return;

                // 3. Выводим иформацию о курьерах
                foreach (var courierUpdates in couriersUpdates.Updates)
                {
                    Logger.WriteToLog(89, MessageSeverity.Info,
                        string.Format(Messages.MSG_089,
                                      couriersUpdates.ServiceId,  // {0}
                                      courierUpdates.ShopId,      // {1}
                                      courierUpdates.CourierId,   // {2}
                                      courierUpdates.Status,      // {3}
                                      courierUpdates.CourierType, // {4}
                                      courierUpdates.WorkStart,   // {5}
                                      courierUpdates.WorkEnd      // {6}
                        ));
                }
            }
            catch
            { }
        }

        /// <summary>
        /// Форматирвание массива DService для вывода в лог
        /// </summary>
        /// <param name="dsrvices">Массив DService</param>
        /// <returns></returns>
        private static string FormatDServices(DService[] dservices)
        {
            try
            {
                if (dservices == null || dservices.Length <= 0)
                    return "none";
                StringBuilder sb = new StringBuilder(22 * dservices.Length);
                sb.Append($"{dservices[0].ShopId}-{dservices[0].DServiceId}");

                for (int i = 1; i < dservices.Length; i++)
                {
                    sb.Append(',');
                    sb.Append($"{dservices[i].ShopId}-{dservices[i].DServiceId}");
                }

                return sb.ToString();
            }
            catch
            {
                return "?";
            }
        }

        /// <summary>
        /// Cравнение двух DataRecord по номеру
        /// </summary>
        /// <param name="record1">Запись 1</param>
        /// <param name="record2">Запись 2</param>
        /// <returns></returns>
        private static int CompareDataRecord(DataRecord record1, DataRecord record2)
        {
            if (record1.QueuingOrder < record2.QueuingOrder)
                return -1;
            if (record1.QueuingOrder > record2.QueuingOrder)
                return -1;
            return 0;
        }

        /// <summary>
        /// Пострение команд на отгрузку или рекомендаций
        /// </summary>
        /// <param name="deliveries">Отгрузки</param>
        /// <param name="cmdType">Тип команды: 0 - рекомендации; 1 - команды на отгрузку</param>
        /// <param name="serviceId">ID сервиса логистики</param>
        /// <param name="allCouriers">Данные о курьерах</param>
        /// <param name="xmlCmd">Построенные команды</param>
        /// <returns>0 - команды успешно переданы; иначе - команды не переданы</returns>
        private static int CreateDeliveryCmd(CourierDeliveryInfo[] deliveries, int cmdType, int serviceId, AllCouriersEx allCouriers, out string xmlCmd)
        {
            // 1. Инициализация
            int rc = 1;
            xmlCmd = null;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (deliveries == null || deliveries.Length <= 0)
                    return rc;
                if (!(cmdType == 0 || cmdType == 1))
                    return rc;
                if (allCouriers == null || !allCouriers.IsCreated)
                    return rc;

                // 3. Строим текст команды
                rc = 3;
                StringBuilder cmd = new StringBuilder(1200 * deliveries.Length);
                CourierBase[] baseTypes = allCouriers.BaseTypes;
                int[] baseKeys = allCouriers.BaseKeys;
                int[] baseTypeCount = new int[baseKeys.Length];
                int[] deliveryDserviceId = new int[baseKeys.Length];

                cmd.Append($@"<deliveries service_id=""{serviceId}"">");

                for (int i = 0; i < deliveries.Length; i++)
                {
                    // 4.0 Извлекаем отгрузку
                    rc = 40;
                    CourierDeliveryInfo delivery = deliveries[i];
                    if (delivery.OrderCount <= 0)
                        continue;
                    Order[] deliveryOrders = delivery.Orders;

                    // 4.1 <delivery ... >
                    rc = 41;

                    cmd.Append($@"<delivery guid=""{Guid.NewGuid()}"" status=""{cmdType}"" shop_id=""{delivery.FromShop.Id}"" cause=""{delivery.Cause}"" delivery_service_id=""{delivery.DeliveryCourier.DServiceType}"" courier_id=""{delivery.DeliveryCourier.Id}"" date_target=""{delivery.StartDeliveryInterval:yyyy-MM-ddTHH:mm:ss.fff}"" date_target_end=""{delivery.EndDeliveryInterval:yyyy-MM-ddTHH:mm:ss.fff}"" priority=""{delivery.Priority}"">");

                    // 4.2 <orders>...</orders>
                    rc = 42;
                    cmd.Append("<orders>");
                    int courierVehicleId = delivery.DeliveryCourier.VehicleID;
                    Array.Clear(baseTypeCount, 0, baseTypeCount.Length);
                    baseTypeCount[Array.BinarySearch(baseKeys, courierVehicleId)] = int.MinValue;
                    int orderCount = delivery.OrderCount;

                    for (int j = 0; j < orderCount; j++)
                    {
                        Order order = deliveryOrders[j];
                        cmd.Append($@"<order status=""{(int)order.Status}"" order_id=""{order.Id}""/>");
                        int[] orderVehicleId = order.VehicleTypes;

                        if (orderVehicleId != null && orderVehicleId.Length > 0)
                        {
                            for (int k = 0; k < orderVehicleId.Length; k++)
                            {
                                int index = Array.BinarySearch(baseKeys, orderVehicleId[k]);
                                if (index >= 0)
                                    baseTypeCount[index]++;
                            }
                        }
                    }

                    cmd.Append("</orders>");

                    // 4.3 <alternative_delivery_service>...</alternative_delivery_service>
                    rc = 43;
                    int count = 0;

                    for (int j = 0; j < baseTypeCount.Length; j++)
                    {
                        if (baseTypeCount[j] >= orderCount)
                            deliveryDserviceId[count++] = baseTypes[j].DServiceType;
                    }

                    if (count <= 0)
                    {
                        cmd.Append("<alternative_delivery_service/>");
                    }
                    else
                    {
                        cmd.Append("<alternative_delivery_service>");

                        for (int j = 0; j < count; j++)
                        {
                            cmd.Append($@"<service id=""{deliveryDserviceId[j]}""/>");
                        }

                        cmd.Append("</alternative_delivery_service>");
                    }

                    // 4.4 <node_info ... >
                    rc = 44;
                    string cost = string.Format(CultureInfo.InvariantCulture, "{0:0.0#########}", delivery.Cost);
                    string weight = string.Format(CultureInfo.InvariantCulture, "{0:0.0#########}", delivery.Weight);
                    string reserveTime = string.Format(CultureInfo.InvariantCulture, "{0:0.0#}", delivery.ReserveTime.TotalMinutes);
                    string deliveryTime = string.Format(CultureInfo.InvariantCulture, "{0:0.0#}", delivery.DeliveryTime);
                    string executionTime = string.Format(CultureInfo.InvariantCulture, "{0:0.0#}", delivery.ExecutionTime);

                    cmd.Append($@"<node_info calc_time=""{delivery.CalculationTime:yyyy-MM-ddTHH:mm:ss.fff}"" cost=""{cost}"" weight=""{weight}"" is_loop=""{delivery.IsLoop}"" start_delivery_interval=""{delivery.StartDeliveryInterval:yyyy-MM-ddTHH:mm:ss.fff}"" end_delivery_interval=""{delivery.EndDeliveryInterval:yyyy-MM-ddTHH:mm:ss.fff}"" reserve_time=""{reserveTime}"" delivery_time=""{deliveryTime}"" execution_time=""{executionTime}"">");

                    // 4.5 <node ... />
                    rc = 45;
                    Point[] nodeInfo = delivery.NodeInfo;

                    for (int j = 0; j < nodeInfo.Length; j++)
                    {
                        cmd.Append($@"<node distance=""{nodeInfo[j].X}"" duration=""{nodeInfo[j].Y}""/>");
                    }

                    // 4.6 </node_info>
                    rc = 46;
                    cmd.Append("</node_info>");

                    // 4.7 <node_delivery_time>
                    rc = 47;
                    cmd.Append("<node_delivery_time>");

                    // 4.8 <node ... />
                    rc = 48;

                    double[] nodeDeliveryTime = delivery.NodeDeliveryTime;

                    for (int j = 0; j < nodeDeliveryTime.Length; j++)
                    {
                        deliveryTime = string.Format(CultureInfo.InvariantCulture, "{0:0.0#}", nodeDeliveryTime[j]);
                        cmd.Append($@"<node delivery_time=""{deliveryTime}""/>");
                    }

                    // 4.9 </node_delivery_time>
                    rc = 47;
                    cmd.Append("</node_delivery_time>");

                    // 4.10 </delivery>
                    rc = 410;
                    cmd.Append("</delivery>");
                }

                // 4.11
                rc = 411;
                cmd.Append("</deliveries>");

                xmlCmd = cmd.ToString();

                // 5. Выход - Ok
                rc = 0;
                return rc;
            }
            catch (Exception ex)
            {
                Logger.WriteToLog(669, MessageSeverity.Error, string.Format(Messages.MSG_669, $"{nameof(LogisticsService)}.{nameof(LogisticsService.CreateDeliveryCmd)}", rc, (ex.InnerException == null ? ex.Message : ex.InnerException.Message)));
                return rc;
            }
        }

        /// <summary>
        /// Пересчет покрытий для магазинов
        /// с установленным флагом Updated
        /// </summary>
        /// <param name="db"></param>
        /// <returns></returns>
        private int RecalcDeliveries(ExternalDb db)
        {
            // 1. Иициализация
            int rc = 1;
            Stopwatch sw = new Stopwatch();
            sw.Start();
            LastException = null;
            DateTime calcTime = DateTime.Now;
            int recalcShopCount = 0;
            int recalcOrderCount = 0;
            Order[] recalcOrders = null;
            Shop[] recalcShops = null;
            ManualResetEvent[] syncEvents = null;

            try
            {
                // 2. Сообщение о начале пересчета
                rc = 2;
                Logger.WriteToLog(51, MessageSeverity.Info, string.Format(Messages.MSG_051, serviceId));

                // 3. Выбираем магазины требующие пересчета
                rc = 3;
                recalcShops = shops.GetUpdated();
                if (recalcShops == null || recalcShops.Length <= 0)
                    return rc = 0;

                recalcShopCount = recalcShops.Length;

                // 4. Выбираем заказы требующие пересчета
                rc = 4;
                int[] recalcShopId = new int[recalcShopCount];
                for (int i = 0; i < recalcShopCount; i++)
                { recalcShopId[i] = recalcShops[i].Id; }

                recalcOrders = orders.GetShopOrders(recalcShopId);
                if (recalcOrders == null || recalcOrders.Length <= 0)
                {
                    queue.Update(recalcShopId, null);
                    return rc = 0;
                }

                recalcOrderCount = recalcOrders.Length;

                // 5. Определяем число потоков для расчетов
                rc = 5;
                int threadCount = 2 * Environment.ProcessorCount;
                if (threadCount < 8)
                {
                    threadCount = 8;
                }
                else if (threadCount > 16)
                {
                    threadCount = 16;
                }

                // 6. Строим порции расчетов (ThreadContext), выполняемые в одном потоке
                //    (порция - это все заказы для одного способа доставки (курьера) в одном магазине)
                rc = 6;
                CalcThreadContext[] context = Calcs.GetCalcThreadContext(serviceId, calcTime, recalcShops, orders, couriers, geoData, limitations);
                if (context == null || context.Length <= 0)
                {
                    Logger.WriteToLog(64, MessageSeverity.Warn, string.Format(Messages.MSG_064, recalcShopCount, recalcOrderCount));
                    OrderRejectionCause[] rOrders;
                    CreateCover.TestNotCoveredOrders(calcTime, recalcOrders, shops, couriers, geoData, out rOrders);
                    if (rOrders != null && rOrders.Length > 0)
                    {
                        RejectOrders(rOrders, serviceId, config.Parameters.ExternalDb.CmdService.Cmd3MessageType, orders, db);
                    }

                    return rc = 0;
                }

                if (context.Length < threadCount)
                    threadCount = context.Length;

                for (int i = 0; i < context.Length; i++)
                { context[i].Config = config; }

                // 7. Сортируем контексты по убыванию числа заказов
                rc = 7;
                Array.Sort(context, CompareCalcContextByOrderCount);

                // 8. Создаём объекты синхронизации
                rc = 8;
                syncEvents = new ManualResetEvent[threadCount];
                int[] contextIndex = new int[threadCount];

                // 9. Запускаем первые threadCount- потоков
                rc = 9;
                int errorCount = 0;
                CourierDeliveryInfo[] allDeliveries = new CourierDeliveryInfo[500000];
                int deliveryCount = 0;

                for (int i = 0; i < threadCount; i++)
                {
                    int m = i;
                    contextIndex[m] = i;
                    CalcThreadContext threadContext = context[m];
                    syncEvents[m] = new ManualResetEvent(false);
                    threadContext.SyncEvent = syncEvents[m];
                    ThreadPool.QueueUserWorkItem(Calcs.CalcThreadEs, threadContext);
                }

                // 10. Запускаем последующие потоки
                //     после завершения очередного
                rc = 10;

                for (int i = threadCount; i < context.Length; i++)
                {
                    int threadIndex = WaitHandle.WaitAny(syncEvents);

                    CalcThreadContext executedThreadContext = context[contextIndex[threadIndex]];

                    contextIndex[threadIndex] = i;
                    int m = i;
                    CalcThreadContext threadContext = context[m];
                    threadContext.SyncEvent = syncEvents[threadIndex];
                    threadContext.SyncEvent.Reset();
                    ThreadPool.QueueUserWorkItem(Calcs.CalcThreadEs, threadContext);

                    // Обработка завершившегося потока
                    if (executedThreadContext.ExitCode != 0)
                    {
                        errorCount++;
                    }
                    else
                    {
                        int contextDeliveryCount = executedThreadContext.DeliveryCount;
                        if (contextDeliveryCount > 0)
                        {
                            if (deliveryCount + contextDeliveryCount > allDeliveries.Length)
                            {
                                int size = allDeliveries.Length / 2;
                                if (size < contextDeliveryCount)
                                    size = contextDeliveryCount;
                                Array.Resize(ref allDeliveries, allDeliveries.Length + size);
                            }

                            executedThreadContext.Deliveries.CopyTo(allDeliveries, deliveryCount);
                            deliveryCount += contextDeliveryCount;
                        }
                    }
                }

                WaitHandle.WaitAll(syncEvents);

                for (int i = 0; i < threadCount; i++)
                {
                    syncEvents[i].Dispose();
                    syncEvents[i] = null;

                    // Обработка последних завершившихся потоков
                    CalcThreadContext executedThreadContext = context[contextIndex[i]];
                    int contextDeliveryCount = executedThreadContext.DeliveryCount;
                    if (contextDeliveryCount > 0)
                    {
                        if (deliveryCount + contextDeliveryCount > allDeliveries.Length)
                        {
                            int size = allDeliveries.Length / 2;
                            if (size < contextDeliveryCount)
                                size = contextDeliveryCount;
                            Array.Resize(ref allDeliveries, allDeliveries.Length + size);
                        }

                        executedThreadContext.Deliveries.CopyTo(allDeliveries, deliveryCount);
                        deliveryCount += contextDeliveryCount;
                    }
                }

                // 11. Если не построена ни одна отгрузка
                rc = 11;
                OrderRejectionCause[] rejectedOrders;

                if (deliveryCount <= 0)
                {
                    int rc2 = CreateCover.TestNotCoveredOrders(calcTime, recalcOrders, shops, couriers, geoData, out rejectedOrders);
                    if (rejectedOrders != null && rejectedOrders.Length > 0)
                    {
                        RejectOrders(rejectedOrders, serviceId, config.Parameters.ExternalDb.CmdService.Cmd3MessageType, orders, db);
                    }

                    return rc = 0;
                }

                if (deliveryCount < allDeliveries.Length)
                {
                    Array.Resize(ref allDeliveries, deliveryCount);
                }

                // 12. Строим покрытия
                rc = 12;
                CourierDeliveryInfo[] recomendations;
                CourierDeliveryInfo[] covers;

                int rc1 = CreateCover.Create(allDeliveries, orders, couriers, geoData, out recomendations, out covers, out rejectedOrders);
                if (rc1 != 0)
                {
                    Logger.WriteToLog(65, MessageSeverity.Warn, string.Format(Messages.MSG_065, rc1, recalcShopCount, recalcOrderCount));
                    return rc = 1000000 * rc + rc1;
                }

                // 13. Отменяем заказы, которые не вошли в покрытие
                rc = 13;
                if (rejectedOrders != null && rejectedOrders.Length > 0)
                {
                    RejectOrders(rejectedOrders, serviceId, config.Parameters.ExternalDb.CmdService.Cmd3MessageType, orders, db);
                }

                // 14 Отправляем рекомендации
                rc = 14;
                if (recomendations != null && recomendations.Length > 0)
                {
                    SendDeliveries(serviceId, recomendations, 0, config.Parameters.ExternalDb.CmdService.Cmd1MessageType, couriers, db);
                }

                // 15. Добавляем отгрузки в очередь
                rc = 15;
                if (covers != null && covers.Length > 0)
                {
                    rc1 = UpdateQueue(serviceId, recalcShopId, covers, config, couriers, db, queue);
                    if (rc1 != 0)
                    {
                        Logger.WriteToLog(63, MessageSeverity.Warn, string.Format(Messages.MSG_063, rc1));
                    }
                }

                // 16. Выход - Ok
                rc = 0;
                return rc;
            }
            catch (Exception ex)
            {
                Logger.WriteToLog(669, MessageSeverity.Error, string.Format(Messages.MSG_669, $"{nameof(LogisticsService)}.{nameof(this.RecalcDeliveries)}", rc, (ex.InnerException == null ? ex.Message : ex.InnerException.Message)));
                LastException = ex;
                return rc;
            }
            finally
            {
                // Ликвидиуем события синхронизации
                if (syncEvents != null)
                {
                    for (int i = 0; i < syncEvents.Length; i++)
                    {
                        if (syncEvents[i] != null)
                        {
                            try { syncEvents[i].Dispose(); } catch { }
                            syncEvents[i] = null;
                        }
                    }
                }

                // Сбрасываем флаг Updated у магазинов
                ResetShopUpdated(recalcShops, recalcOrders, shops, queue);

                // Финальное сообщение
                sw.Stop();
                MessageSeverity severity = (rc == 0 ? MessageSeverity.Info : MessageSeverity.Warn);
                Logger.WriteToLog(52, severity, string.Format(Messages.MSG_052, serviceId, rc, recalcShopCount, recalcOrderCount, sw.ElapsedMilliseconds));
            }
        }

        /// <summary>
        /// Сброс флага Updated у магазинов, для которых каждый  
        /// заданный заказ или завершен или находится в очереди на отгрузку
        /// </summary>
        /// <param name="recalcShops">Заданные магазины</param>
        /// <param name="recalcOrders">Заданные заказы</param>
        /// <param name="allShops">Все м</param>
        /// <param name="queue"></param>
        private static void ResetShopUpdated(Shop[] recalcShops, Order[] recalcOrders, AllShops allShops, DeliveryQueue queue)
        {
            // 1. Инициализация

            try
            {
                // 2. Провеяем исходные данные
                if (recalcShops == null || recalcShops.Length <= 0)
                    return;
                if (allShops == null)
                    return;
                if (queue == null)
                    return;

                // 2+ Особый случай
                if (recalcOrders == null || recalcOrders.Length <= 0)
                {
                    foreach (Shop shop in recalcShops)
                    { shop.Updated = false; }
                    return;
                }

                // 3. Выбираем из очереди Id всех заказов требующих отгрузки
                int[] queueOrderIds = queue.GetOrderIds();
                if (queueOrderIds == null)
                    queueOrderIds = new int[0];

                Array.Sort(queueOrderIds);

                // 4. Отбираем магазины, которые имеют не завершеные заказы
                //    и не находящиеся в очереди
                Dictionary<int, bool> allShopIds = new Dictionary<int, bool>(allShops.Count);
                Dictionary<int, bool> allUpdatedShopIds = new Dictionary<int, bool>(allShops.Count);

                for (int i = 0; i < recalcOrders.Length; i++)
                {
                    Order order = recalcOrders[i];
                    allShopIds[order.ShopId] = true;
                    if (!order.Completed)
                    {
                        if (Array.BinarySearch(queueOrderIds, order.Id) < 0)
                        {
                            allUpdatedShopIds[order.ShopId] = true;
                        }
                    }
                }

                // 5. Сбрасываем флаг Updated у магазинов всречающихся в заказах
                foreach(int shopId in allShopIds.Keys)
                {
                    if (!allUpdatedShopIds.ContainsKey(shopId))
                    { allShops.SetShopUpdated(shopId, false); }
                }

                // 6. Сбрасываем флаг Updated у магазинов не имеющих заказов
                foreach (Shop shop in recalcShops)
                {
                    if (!allShopIds.ContainsKey(shop.Id))
                        shop.Updated = false;
                }
            }
            catch
            { }
        }

        /// <summary>
        /// Сравнение двух контекстов по количеству заказов
        /// </summary>
        /// <param name="context1">Контекст 1</param>
        /// <param name="context2">Контекст 2</param>
        /// <returns>- 1  - Контекст1 больше Контекст2; 0 - Контекст1 = Контекст2; 1 - Контекст1 меньше Контекст2</returns>
        private static int CompareCalcContextByOrderCount(CalcThreadContext context1, CalcThreadContext context2)
        {
            if (context1.OrderCount > context2.OrderCount)
                return -1;
            if (context1.OrderCount < context2.OrderCount)
                return 1;
            return 0;
        }

        /// <summary>
        /// Обработка построенного покрытия
        /// </summary>
        /// <param name="serviceId">ID сервиса логистики</param>
        /// <param name="recalcShopId">Id магазинов, для которых создавались покрытия</param>
        /// <param name="deliveries">Отгрузки, образующие покрытия</param>
        /// <param name="config">Параметры построителя</param>
        /// <param name="allCouriers">Все курьеры</param>
        /// <param name="db">External DB</param>
        /// <param name="queue">Очередь</param>
        /// <returns>0 - покрытия успешно обработаны; покрытие не обработано</returns>
        private static int UpdateQueue(int serviceId, int[] recalcShopId, CourierDeliveryInfo[] deliveries, BuilderConfig config, AllCouriersEx allCouriers, ExternalDb db, DeliveryQueue queue)
        {
            // 1. Инициализация
            int rc1 = 1;
            int rc = 1;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (recalcShopId == null || recalcShopId.Length <= 0)
                    return rc;
                if (deliveries == null || deliveries.Length <= 0)
                    return rc;
                if (allCouriers == null || !allCouriers.IsCreated)
                    return rc;
                if (config == null)
                    return rc;
                if (db == null || !db.IsOpen())
                    return rc;
                if (queue == null)
                    return rc;

                // 3. Извлекаем параметры построения очереди
                rc = 3;
                bool averageCost = config.Parameters.StartCondition.AverageCost;
                bool shipmentTrigger = config.Parameters.StartCondition.ShipmentTrigger;
                int taxiMargin = config.Parameters.TaxiDeliveryMargin;
                int courierMargin = config.Parameters.CourierDeliveryMargin;

                // 4. Сортируем отгрузки по Shop.Id
                rc = 4;
                Array.Sort(deliveries, CompareDeliveryByShopId);

                // 5. Делим отгрузки на две группы:
                //   1) со стартом прямо сейчас
                //   2) для размещения в очереди
                rc = 5;
                DateTime thresholdTime = DateTime.Now.AddSeconds(30);
                CourierDeliveryInfo[] startNowDeliveries = new CourierDeliveryInfo[deliveries.Length];
                CourierDeliveryInfo[] queueDeliveries = new CourierDeliveryInfo[deliveries.Length];

                int startNowCount = 0;
                int queueCount = 0;

                for (int i = 0; i < deliveries.Length; i++)
                {
                    // 5.1 Выбираем отгрузку
                    rc = 51;
                    CourierDeliveryInfo delivery = deliveries[i];
                    Courier courier = delivery.DeliveryCourier;

                    // 5.2 Извлекаем порог
                    rc = 52;
                    double costThreshold = courier.AverageOrderCost;

                    // 5.3 Определяем время отгрузки
                    rc = 53;
                    DateTime eventTime = delivery.EndDeliveryInterval;
                    int cause = 0;

                    if (shipmentTrigger && delivery.OrderCount >= courier.MaxOrderCount)
                    {
                        eventTime = delivery.StartDeliveryInterval;
                        cause = 1;
                    }
                    else if (averageCost && !courier.IsTaxi && delivery.OrderCost <= costThreshold)
                    {
                        eventTime = delivery.StartDeliveryInterval;
                        cause = 2;
                    }
                    else if (courier.IsTaxi)
                    {
                        eventTime = delivery.EndDeliveryInterval.AddMinutes(-taxiMargin);
                        if (eventTime < delivery.StartDeliveryInterval)
                            eventTime = delivery.StartDeliveryInterval;
                        cause = 4;
                    }
                    else // (!courier.IsTaxi) 
                    {
                        eventTime = delivery.EndDeliveryInterval.AddMinutes(-courierMargin);
                        if (eventTime < delivery.StartDeliveryInterval)
                            eventTime = delivery.StartDeliveryInterval;
                        cause = 5;
                    }

                    if (eventTime <= thresholdTime)
                    {
                        delivery.Cause = cause;
                        startNowDeliveries[startNowCount++] = delivery;
                    }
                    else
                    {
                        delivery.EventTime = eventTime;
                        queueDeliveries[queueCount++] = delivery;
                    }
                }

                // 7. Отправляем отгрузки со стартом прямо сейчас
                rc = 7;
                if (startNowCount > 0)
                {
                    if (startNowCount < startNowDeliveries.Length)
                    {
                        Array.Resize(ref startNowDeliveries, startNowCount);
                    }

                    rc1 = SendDeliveries(serviceId, startNowDeliveries, 1, config.Parameters.ExternalDb.CmdService.Cmd2MessageType, allCouriers, db);
                    if (rc1 == 0)
                    {
                        foreach (var delivery in startNowDeliveries)
                        {
                            foreach (var order in delivery.Orders)
                            { order.Completed = true; }
                        }
                    }
                }

                // 8. Обновляем очередь
                rc = 8;
                if (queueCount < queueDeliveries.Length)
                { Array.Resize(ref queueDeliveries, queueCount); }

                queue.Update(recalcShopId, queueDeliveries);

                // 9. Выход - Ok
                rc = 0;
                return rc;
            }
            catch (Exception ex)
            {
                Logger.WriteToLog(669, MessageSeverity.Error, string.Format(Messages.MSG_669, $"{nameof(LogisticsService)}.{nameof(LogisticsService.UpdateQueue)}", rc, (ex.InnerException == null ? ex.Message : ex.InnerException.Message)));
                return rc;
            }
        }

        private static int CompareDeliveryByShopId(CourierDeliveryInfo d1, CourierDeliveryInfo d2)
        {
            if (d1.FromShop.Id < d2.FromShop.Id)
                return -1;
            if (d1.FromShop.Id > d2.FromShop.Id)
                return 1;
            return 0;
        }

        #region IDisposable Support

        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    //FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);
                    //Logger.WriteToLog(string.Format(MessagePatterns.STOP_SERVICE, "FixedServiceEz", fileVersionInfo.ProductVersion));

                    if (timer != null)
                    {
                        try { timer.Elapsed -= Timer_Elapsed; } catch { }
                        try { timer.Dispose(); } catch { }
                        timer = null;
                    }

                    if (lsDataDb != null)
                    {
                        lsDataDb.Close();
                        lsDataDb = null;
                    }

                    if (externalDb != null)
                    {
                        externalDb.Close();
                        externalDb = null;
                    }

                    if (syncMutex != null)
                    {
                        try { syncMutex.Dispose(); } catch { };
                        syncMutex = null;
                    }

                    config = null;
                    geoData = null;
                    thresholds = null;
                    couriers = null;
                    shops = null;
                    orders = null;
                    limitations = null;
                    queue = null;
                    hearbeatTickCount = 0;
                    queueTickCount = 0;
                    recalcTickCount = 0;
                    geoTickCount = 0;
                    connectionBrokenCount = 0;
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                IsCreated = false;
                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~FixedService() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }

        #endregion IDisposable Support
    }
}
