
namespace DeliveryBuilder
{
    using DeliveryBuilder.AverageDeliveryCost;
    using DeliveryBuilder.BuilderParameters;
    using DeliveryBuilder.Cmds;
    using DeliveryBuilder.Couriers;
    using DeliveryBuilder.Db;
    using DeliveryBuilder.Geo;
    using DeliveryBuilder.Log;
    using DeliveryBuilder.Orders;
    using DeliveryBuilder.Queue;
    using DeliveryBuilder.SalesmanProblemLevels;
    using DeliveryBuilder.Shops;
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;
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
        private bool connectionFailed;

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
        private Mutex syncMutex = new Mutex();

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
        /// Ограничения а число заказов в зависимости
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
                Logger.WriteToLog(1, MsessageSeverity.Info, string.Format(Messages.MSG_001, Path.GetFileNameWithoutExtension(fileVersionInfo.FileName), fileVersionInfo.ProductVersion, serviceId));

                // 4. Проверяем исходные данные
                rc = 4;
                if (string.IsNullOrWhiteSpace(conectionString))
                {
                    Logger.WriteToLog(20, MsessageSeverity.Error, Messages.MSG_020);
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
                    { Logger.WriteToLog(21, MsessageSeverity.Error, Messages.MSG_021); }
                    else
                    { Logger.WriteToLog(22, MsessageSeverity.Error, string.Format(Messages.MSG_022, exceptionMessage)); }
                    return rc;
                }

                // 6. Загружаем параметры построителя
                rc = 6;
                config = lsDataDb.SelectConfig(serviceId);
                if (config == null)
                {
                    string exceptionMessage = lsDataDb.GetLastErrorMessage();
                    if (string.IsNullOrEmpty(exceptionMessage))
                    { Logger.WriteToLog(23, MsessageSeverity.Error, Messages.MSG_023); }
                    else
                    { Logger.WriteToLog(24, MsessageSeverity.Error, string.Format(Messages.MSG_024, exceptionMessage)); }
                    return rc;
                }

                // 7. Проверяем параметры построителя
                rc = 7;
                if (!TestBuilderConfig(config))
                {
                    Logger.WriteToLog(25, MsessageSeverity.Error, Messages.MSG_025);
                    return rc;
                }

                // 8. Создаём объект для работы гео-данными
                rc = 8;
                geoData = new GeoData();
                int rc1 = geoData.Create(config);
                if (rc1 != 0)
                {
                    Logger.WriteToLog(26, MsessageSeverity.Error, string.Format(Messages.MSG_026, rc1));
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
                    { Logger.WriteToLog(27, MsessageSeverity.Error, Messages.MSG_027); }
                    else
                    { Logger.WriteToLog(28, MsessageSeverity.Error, string.Format(Messages.MSG_028, exceptionMessage)); }
                    return rc = 10000 * rc + rc1;
                }

                thresholds = new AverageCostThresholds();
                rc1 = thresholds.Create(records);
                if (rc1 != 0)
                {
                    Logger.WriteToLog(29, MsessageSeverity.Error, string.Format(Messages.MSG_029, rc1));
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
                    { Logger.WriteToLog(27, MsessageSeverity.Error, Messages.MSG_030); }
                    else
                    { Logger.WriteToLog(28, MsessageSeverity.Error, string.Format(Messages.MSG_031, exceptionMessage)); }
                    return rc = 10000 * rc + rc1;
                }

                // 11. Создаём курьеров
                rc = 11;
                couriers = new AllCouriersEx();
                rc1 = couriers.Create(vehiclesRecords, config.Parameters.GeoYandex.TypeNames, thresholds);
                if (rc1 != 0)
                {
                    Logger.WriteToLog(32, MsessageSeverity.Error, string.Format(Messages.MSG_032, rc1));
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
                    Logger.WriteToLog(33, MsessageSeverity.Error, string.Format(Messages.MSG_033, rc1));
                    return rc = 10000 * rc + rc1;
                }

                // 14. Создаём Salesman Levels
                rc = 14;
                limitations = new SalesmanLevels();
                rc1 = limitations.Create(config.SalesmanLevels);
                if (rc1 != 0)
                {
                    Logger.WriteToLog(34, MsessageSeverity.Error, string.Format(Messages.MSG_034, rc1));
                    return rc = 10000 * rc + rc1;
                }

                // 15. Создаём очередь
                rc = 15;
                queue = new DeliveryQueue();

                // 16. Устанавливаем счечики тиков
                rc = 16;
                hearbeatTickCount = config.Parameters.HeartbeatInterval;
                queueTickCount = config.Parameters.QueueInterval;
                recalcTickCount = config.Parameters.RecalcInterval;
                geoTickCount = config.Parameters.GeoCache.CheckInterval;

                // 17. Создаём и включаем таймер
                rc = 17;
                connectionFailed = true;
                syncMutex = new Mutex();
                timer = new System.Timers.Timer();
                timer.Interval = config.Parameters.TickInterval;
                timer.AutoReset = true;
                timer.Elapsed += Timer_Elapsed;
                timer.Start();

                // 18. Выход - Ok
                rc = 0;
                IsCreated = true;
                return rc;
            }
            catch (Exception ex)
            {
                Logger.WriteToLog(666, MsessageSeverity.Error, string.Format(Messages.MSG_666, $"{nameof(LogisticsService)}.{nameof(this.Create)}", (ex.InnerException == null ? ex.Message : ex.InnerException.Message)));
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
        /// Закрытие сервиса логистики
        /// </summary>
        public void Close()
        {
            FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);
            Logger.WriteToLog(1, MsessageSeverity.Info, string.Format(Messages.MSG_002, Path.GetFileNameWithoutExtension(fileVersionInfo.FileName), fileVersionInfo.ProductVersion, serviceId));
            Dispose(true);
        }

        /// <summary>
        /// Проверка конфига
        /// </summary>
        /// <param name="config">Конфиг</param>
        /// <returns>true - конфиг проверен; false - конфиг содержит ошибки</returns>
        private bool TestBuilderConfig(BuilderConfig config)
        {
            // 1. Инициализация
             
            try
            {
                // 2. Проверяем исходные данные
                if (config == null)
                {
                    Logger.WriteToLog(3, MsessageSeverity.Error, Messages.MSG_003);
                    return false;
                }

                // 3. Проверка Fuctional Parameters
                bool passed = false;
                if (config.Parameters == null)
                { Logger.WriteToLog(4, MsessageSeverity.Error, Messages.MSG_004); }
                else if (config.Parameters.CourierDeliveryMargin < 0)
                { Logger.WriteToLog(5, MsessageSeverity.Error, string.Format(Messages.MSG_005, nameof(config.Parameters.CourierDeliveryMargin))); }
                else if (config.Parameters.TaxiDeliveryMargin < 0)
                { Logger.WriteToLog(5, MsessageSeverity.Error, string.Format(Messages.MSG_005, nameof(config.Parameters.TaxiDeliveryMargin))); }
                else if (config.Parameters.СheckingMargin < 0)
                { Logger.WriteToLog(5, MsessageSeverity.Error, string.Format(Messages.MSG_005, nameof(config.Parameters.СheckingMargin))); }
                else if (config.Parameters.TickInterval < 50)
                { Logger.WriteToLog(5, MsessageSeverity.Error, string.Format(Messages.MSG_005, nameof(config.Parameters.TickInterval))); }
                else if (config.Parameters.HeartbeatInterval <= 0)
                { Logger.WriteToLog(5, MsessageSeverity.Error, string.Format(Messages.MSG_005, nameof(config.Parameters.HeartbeatInterval))); }
                else if (config.Parameters.RecalcInterval <= 0)
                { Logger.WriteToLog(5, MsessageSeverity.Error, string.Format(Messages.MSG_005, nameof(config.Parameters.RecalcInterval))); }
                else if (config.Parameters.QueueInterval <= 0)
                { Logger.WriteToLog(5, MsessageSeverity.Error, string.Format(Messages.MSG_005, nameof(config.Parameters.QueueInterval))); }
                else if (config.Parameters.QueueCatchingInterval < 0)
                { Logger.WriteToLog(5, MsessageSeverity.Error, string.Format(Messages.MSG_005, nameof(config.Parameters.QueueCatchingInterval))); }
                else if (config.Parameters.GeoCache == null)
                { Logger.WriteToLog(5, MsessageSeverity.Error, string.Format(Messages.MSG_005, nameof(config.Parameters.GeoCache))); }
                else if (config.Parameters.GeoCache.Capacity < 100)
                { Logger.WriteToLog(5, MsessageSeverity.Error, string.Format(Messages.MSG_005, nameof(config.Parameters.GeoCache.Capacity))); }
                else if (config.Parameters.GeoCache.SavingInterval < 10)
                { Logger.WriteToLog(5, MsessageSeverity.Error, string.Format(Messages.MSG_005, nameof(config.Parameters.GeoCache.SavingInterval))); }
                else if (config.Parameters.GeoCache.CheckInterval <= 0)
                { Logger.WriteToLog(5, MsessageSeverity.Error, string.Format(Messages.MSG_005, nameof(config.Parameters.GeoCache.CheckInterval))); }
                else if (config.Parameters.GeoYandex == null)
                { Logger.WriteToLog(5, MsessageSeverity.Error, string.Format(Messages.MSG_005, nameof(config.Parameters.GeoYandex))); }
                else if (string.IsNullOrWhiteSpace(config.Parameters.GeoYandex.ApiKey))
                { Logger.WriteToLog(5, MsessageSeverity.Error, string.Format(Messages.MSG_005, nameof(config.Parameters.GeoYandex.ApiKey))); }
                else if (config.Parameters.GeoYandex.CyclingRatio <= 0 || config.Parameters.GeoYandex.CyclingRatio > 1)
                { Logger.WriteToLog(5, MsessageSeverity.Error, string.Format(Messages.MSG_005, nameof(config.Parameters.GeoYandex.CyclingRatio))); }
                else if (config.Parameters.GeoYandex.Timeout <= 0)
                { Logger.WriteToLog(5, MsessageSeverity.Error, string.Format(Messages.MSG_005, nameof(config.Parameters.GeoYandex.Timeout))); }
                else if (config.Parameters.GeoYandex.TypeNames == null || config.Parameters.GeoYandex.TypeNames.Length <= 0)
                { Logger.WriteToLog(5, MsessageSeverity.Error, string.Format(Messages.MSG_005, nameof(config.Parameters.GeoYandex.TypeNames))); }
                else if (string.IsNullOrWhiteSpace(config.Parameters.GeoYandex.Url))
                { Logger.WriteToLog(5, MsessageSeverity.Error, string.Format(Messages.MSG_005, nameof(config.Parameters.GeoYandex.Url))); }
                else if (config.Parameters.GeoYandex.PairLimit <= 0)
                { Logger.WriteToLog(5, MsessageSeverity.Error, string.Format(Messages.MSG_005, nameof(config.Parameters.GeoYandex.PairLimit))); }
                else if (config.Parameters.StartCondition == null)
                { Logger.WriteToLog(5, MsessageSeverity.Error, string.Format(Messages.MSG_005, nameof(config.Parameters.StartCondition))); }
                else if (config.Parameters.ExternalDb == null)
                { Logger.WriteToLog(5, MsessageSeverity.Error, string.Format(Messages.MSG_005, nameof(config.Parameters.ExternalDb))); }
                else if (string.IsNullOrWhiteSpace(config.Parameters.ExternalDb.СonnectionString))
                { Logger.WriteToLog(5, MsessageSeverity.Error, string.Format(Messages.MSG_005, nameof(config.Parameters.ExternalDb.СonnectionString))); }
                else if (config.Parameters.ExternalDb.ConnectionTimeout <= 0)
                { Logger.WriteToLog(5, MsessageSeverity.Error, string.Format(Messages.MSG_005, nameof(config.Parameters.ExternalDb.ConnectionTimeout))); }
                else if (config.Parameters.ExternalDb.CmdService == null)
                { Logger.WriteToLog(5, MsessageSeverity.Error, string.Format(Messages.MSG_005, nameof(config.Parameters.ExternalDb.CmdService))); }
                else if (string.IsNullOrWhiteSpace(config.Parameters.ExternalDb.CmdService.Cmd1MessageType))
                { Logger.WriteToLog(5, MsessageSeverity.Error, string.Format(Messages.MSG_005, nameof(config.Parameters.ExternalDb.CmdService.Cmd1MessageType))); }
                else if (string.IsNullOrWhiteSpace(config.Parameters.ExternalDb.CmdService.Cmd2MessageType))
                { Logger.WriteToLog(5, MsessageSeverity.Error, string.Format(Messages.MSG_005, nameof(config.Parameters.ExternalDb.CmdService.Cmd2MessageType))); }
                else if (string.IsNullOrWhiteSpace(config.Parameters.ExternalDb.CmdService.Cmd3MessageType))
                { Logger.WriteToLog(5, MsessageSeverity.Error, string.Format(Messages.MSG_005, nameof(config.Parameters.ExternalDb.CmdService.Cmd3MessageType))); }
                else if (string.IsNullOrWhiteSpace(config.Parameters.ExternalDb.CmdService.DataMessageType))
                { Logger.WriteToLog(5, MsessageSeverity.Error, string.Format(Messages.MSG_005, nameof(config.Parameters.ExternalDb.CmdService.DataMessageType))); }
                else if (string.IsNullOrWhiteSpace(config.Parameters.ExternalDb.CmdService.HeartbeatMessageType))
                { Logger.WriteToLog(5, MsessageSeverity.Error, string.Format(Messages.MSG_005, nameof(config.Parameters.ExternalDb.CmdService.HeartbeatMessageType))); }
                else if (string.IsNullOrWhiteSpace(config.Parameters.ExternalDb.CmdService.Name))
                { Logger.WriteToLog(5, MsessageSeverity.Error, string.Format(Messages.MSG_005, nameof(config.Parameters.ExternalDb.CmdService.Name))); }
                else if (config.Parameters.ExternalDb.DataService == null)
                { Logger.WriteToLog(5, MsessageSeverity.Error, string.Format(Messages.MSG_005, nameof(config.Parameters.ExternalDb.DataService))); }
                else if (string.IsNullOrWhiteSpace(config.Parameters.ExternalDb.DataService.CourierMesageType))
                { Logger.WriteToLog(5, MsessageSeverity.Error, string.Format(Messages.MSG_005, nameof(config.Parameters.ExternalDb.DataService.CourierMesageType))); }
                else if (string.IsNullOrWhiteSpace(config.Parameters.ExternalDb.DataService.OrderMesageType))
                { Logger.WriteToLog(5, MsessageSeverity.Error, string.Format(Messages.MSG_005, nameof(config.Parameters.ExternalDb.DataService.OrderMesageType))); }
                else if (string.IsNullOrWhiteSpace(config.Parameters.ExternalDb.DataService.QueueName))
                { Logger.WriteToLog(5, MsessageSeverity.Error, string.Format(Messages.MSG_005, nameof(config.Parameters.ExternalDb.DataService.QueueName))); }
                else if (string.IsNullOrWhiteSpace(config.Parameters.ExternalDb.DataService.ShopMesageType))
                { Logger.WriteToLog(5, MsessageSeverity.Error, string.Format(Messages.MSG_005, nameof(config.Parameters.ExternalDb.DataService.ShopMesageType))); }
                else if (config.Parameters.ExternalDb.DataService.ReceiveTimeout <= 0)
                { Logger.WriteToLog(5, MsessageSeverity.Error, string.Format(Messages.MSG_005, nameof(config.Parameters.ExternalDb.DataService.ReceiveTimeout))); }
                else
                { passed = true; }

                if (!passed)
                    return false;

                // 4. Проверка Salesman Levels
                passed = true;

                if (config.SalesmanLevels == null || config.SalesmanLevels.Length <= 0)
                {
                    Logger.WriteToLog(6, MsessageSeverity.Error, Messages.MSG_006);
                    passed = false;
                }
                else
                {
                    foreach (var slevel in config.SalesmanLevels)
                    {
                        if (slevel.Level <= 0 || slevel.Orders <= 0)
                        {
                            Logger.WriteToLog(6, MsessageSeverity.Error, Messages.MSG_006);
                            passed = false;
                        }
                    }
                }

                if (!passed)
                    return false;

                // 5. Проверка Cloud Parameters
                passed = false;

                if (config.Cloud == null)
                { Logger.WriteToLog(7, MsessageSeverity.Error, Messages.MSG_007);}
                else if (config.Cloud.Delta < 0)
                { Logger.WriteToLog(8, MsessageSeverity.Error, string.Format(Messages.MSG_008, nameof(config.Cloud.Delta))); }
                else if (config.Cloud.Radius <= 0)
                { Logger.WriteToLog(8, MsessageSeverity.Error, string.Format(Messages.MSG_008, nameof(config.Cloud.Radius))); }
                else if (config.Cloud.Size5 <= 0)
                { Logger.WriteToLog(8, MsessageSeverity.Error, string.Format(Messages.MSG_008, nameof(config.Cloud.Size5))); }
                else if (config.Cloud.Size6 <= 0)
                { Logger.WriteToLog(8, MsessageSeverity.Error, string.Format(Messages.MSG_008, nameof(config.Cloud.Size6))); }
                else if (config.Cloud.Size7 <= 0)
                { Logger.WriteToLog(8, MsessageSeverity.Error, string.Format(Messages.MSG_008, nameof(config.Cloud.Size7))); }
                else if (config.Cloud.Size8 <= 0)
                { Logger.WriteToLog(8, MsessageSeverity.Error, string.Format(Messages.MSG_008, nameof(config.Cloud.Size8))); }
                else if (string.IsNullOrWhiteSpace(config.GroupId))
                { Logger.WriteToLog(9, MsessageSeverity.Error, string.Format(Messages.MSG_009, nameof(config.GroupId))); }
                else
                { passed = true; }

                // 6. Выход
                return passed;
            }
            catch (Exception ex)
            {
                Logger.WriteToLog(666, MsessageSeverity.Error, string.Format(Messages.MSG_666, nameof(this.TestBuilderConfig),(ex.InnerException == null ? ex.Message : ex.InnerException.Message)));
                LastException = ex;
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
                    string exceptionMessage = lsDataDb.GetLastErrorMessage();
                    if (string.IsNullOrEmpty(exceptionMessage))
                    { Logger.WriteToLog(35, MsessageSeverity.Error, Messages.MSG_035); }
                    else
                    { Logger.WriteToLog(36, MsessageSeverity.Error, string.Format(Messages.MSG_036, exceptionMessage)); }
                    db.Close();
                    connectionFailed = false;
                    return;
                }

                // 2. Отправляем Hearbeat
                if (hearbeatTickCount <= 0)
                {
                    SendHeartbeat(serviceId, config.Parameters.ExternalDb.CmdService.HeartbeatMessageType, db);
                    hearbeatTickCount = config.Parameters.HeartbeatInterval;
                }

                // 3. Если связь восстановлена
                if (connectionFailed)
                {
                    SendDataReqest(serviceId, config.Parameters.ExternalDb.CmdService.DataMessageType, db);
                    shops.SetAllShopUpdated();
                    connectionFailed = false;
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
                    int rc1 = db.ReceiveData(serviceId, out dataRecords);
                    if (rc1 == 0 && dataRecords != null && dataRecords.Length > 0)
                    { UpdateData(dataRecords); }

                    // 7. Пересчиываем отгрузки
                    RecalcDeliveries();
                }

                // 8. Диспетчируем очередь
                if (queueTickCount <= 0)
                {
                    Queue_Elapsed();
                    queueTickCount = config.Parameters.QueueInterval;
                }
            }
            catch (Exception ex)
            {
                Logger.WriteToLog(666, MsessageSeverity.Error, string.Format(Messages.MSG_666, $"{nameof(LogisticsService)}.{nameof(this.Timer_Elapsed)}", (ex.InnerException == null ? ex.Message : ex.InnerException.Message)));
                LastException = ex;
            }
            finally
            {
                if (isCatched)
                {
                    try
                    { syncMutex.ReleaseMutex(); }
                    catch { }
                    isCatched = false;
                }
                if (db != null)
                {
                    try
                    { db.Close(); }
                    catch { }
                    db = null;
                }
            }
        }

        /// <summary>
        /// Диспетчеризация очереди 
        /// </summary>
        /// <returns></returns>
        private int Queue_Elapsed(ExternalDb db)
        {
            // 1. Инициализация
            int rc = 1;

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

                // 4. Отправляем отгрузки
                rc = 4;

            }
            catch (Exception ex)
            {
                Logger.WriteToLog(666, MsessageSeverity.Error, string.Format(Messages.MSG_666, $"{nameof(LogisticsService)}.{nameof(this.Queue_Elapsed)}", (ex.InnerException == null ? ex.Message : ex.InnerException.Message)));
                LastException = ex;
                return rc;
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
                Heartbeat heartbeat = new Heartbeat();
                heartbeat.ServiceId = serviceId;
                string errorMessage;
                int rc1 = db.SendXmlCmd(serviceId, messageType, heartbeat, out errorMessage);
                if (rc1 != 0)
                {
                    if (string.IsNullOrWhiteSpace(errorMessage))
                    { Logger.WriteToLog(37, MsessageSeverity.Warn, string.Format(Messages.MSG_037, rc1)); }
                    else
                    { Logger.WriteToLog(38, MsessageSeverity.Warn, string.Format(Messages.MSG_038, rc1, errorMessage)); }
                    return rc = 1000 * rc + rc1;
                }

                // 4. Выход - Ok
                rc = 0;
                return rc;
            }
            catch (Exception ex)
            {
                Logger.WriteToLog(38, MsessageSeverity.Error, string.Format(Messages.MSG_038, rc, (ex.InnerException == null ? ex.Message : ex.InnerException.Message)));
                return rc;
            }
        }

        /// <summary>
        /// Отправка запроса данных
        /// </summary>
        /// <param name="serviceId">ID сервиса логистики</param>
        /// <param name="messageType">Тип сообщения</param>
        /// <param name="db">БД ExteralDb</param>
        /// <returns>0 - запрос отправлен; иначе - запрос не оправлен</returns>
        private static int SendDataReqest(int serviceId, string messageType, ExternalDb db)
        {
            // 1. Иициализация
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
                DataRequest request = new DataRequest();
                request.ServiceId = serviceId;
                request.All = 1;
                string errorMessage;
                int rc1 = db.SendXmlCmd(serviceId, messageType, request, out errorMessage);
                if (rc1 != 0)
                {
                    if (string.IsNullOrWhiteSpace(errorMessage))
                    { Logger.WriteToLog(39, MsessageSeverity.Warn, string.Format(Messages.MSG_039, rc1)); }
                    else
                    { Logger.WriteToLog(40, MsessageSeverity.Warn, string.Format(Messages.MSG_040, rc1, errorMessage)); }
                    return rc = 1000 * rc + rc1;
                }

                // 4. Выход - Ok
                rc = 0;
                return rc;
            }
            catch (Exception ex)
            {
                Logger.WriteToLog(38, MsessageSeverity.Error, string.Format(Messages.MSG_040, rc, (ex.InnerException == null ? ex.Message : ex.InnerException.Message)));
                return rc;
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
                DataRequest request = new DataRequest();
                request.ServiceId = serviceId;
                request.All = 1;
                int rc1 = db.ReceiveData(serviceId, out records);
                if (rc1 != 0)
                {
                    string errorMessage = db.GetLastErrorMessage();
                    if (string.IsNullOrWhiteSpace(errorMessage))
                    { Logger.WriteToLog(41, MsessageSeverity.Warn, string.Format(Messages.MSG_041, rc1)); }
                    else
                    { Logger.WriteToLog(42, MsessageSeverity.Warn, string.Format(Messages.MSG_042, rc1, errorMessage)); }
                    return rc = 1000 * rc + rc1;
                }

                // 4. Выход - Ok
                rc = 0;
                return rc;
            }
            catch (Exception ex)
            {
                Logger.WriteToLog(38, MsessageSeverity.Error, string.Format(Messages.MSG_042, rc, (ex.InnerException == null ? ex.Message : ex.InnerException.Message)));
                return rc;
            }
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

                            int rc1 = orders.Update(ordersUpdates, shops, couriers);
                            if (rc1 != 0)
                            {
                                Logger.WriteToLog(45, MsessageSeverity.Warn, string.Format(Messages.MSG_045, rc, dr.MessageBody));
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.WriteToLog(44, MsessageSeverity.Warn, string.Format(Messages.MSG_044, dr.MessageTypeName, dr.MessageBody));
                            Logger.WriteToLog(44, MsessageSeverity.Warn, string.Format(Messages.MSG_044, dr.MessageTypeName, (ex.InnerException == null ? ex.Message : ex.InnerException.Message)));
                        }
                    }
                    else if (dr.MessageTypeName.Equals(courierMessageType, StringComparison.CurrentCultureIgnoreCase))
                    {
                        try
                        {
                            CouriersUpdates couriersUpdates;
                            using (StringReader sr = new StringReader(dr.MessageBody))
                            {
                                couriersUpdates = (CouriersUpdates) couriersSerializer.Deserialize(sr);
                            }

                            int rc1 = couriers.Update(couriersUpdates, thresholds, shops);
                            if (rc1 != 0)
                            {
                                Logger.WriteToLog(46, MsessageSeverity.Warn, string.Format(Messages.MSG_046, rc, dr.MessageBody));
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.WriteToLog(44, MsessageSeverity.Warn, string.Format(Messages.MSG_044, dr.MessageTypeName, dr.MessageBody));
                            Logger.WriteToLog(44, MsessageSeverity.Warn, string.Format(Messages.MSG_044, dr.MessageTypeName, (ex.InnerException == null ? ex.Message : ex.InnerException.Message)));
                        }
                    }
                    else if (dr.MessageTypeName.Equals(shopMessageType, StringComparison.CurrentCultureIgnoreCase))
                    {
                        try
                        {
                            ShopsUpdates shopsUpdates;
                            using (StringReader sr = new StringReader(dr.MessageBody))
                            {
                                shopsUpdates = (ShopsUpdates) shopsSerializer.Deserialize(sr);
                            }

                            int rc1 = shops.Update(shopsUpdates);
                            if (rc1 != 0)
                            {
                                Logger.WriteToLog(47, MsessageSeverity.Warn, string.Format(Messages.MSG_047, rc, dr.MessageBody));
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.WriteToLog(44, MsessageSeverity.Warn, string.Format(Messages.MSG_044, dr.MessageTypeName, dr.MessageBody));
                            Logger.WriteToLog(44, MsessageSeverity.Warn, string.Format(Messages.MSG_044, dr.MessageTypeName, (ex.InnerException == null ? ex.Message : ex.InnerException.Message)));
                        }
                    }
                    else
                    {
                        Logger.WriteToLog(43, MsessageSeverity.Warn, string.Format(Messages.MSG_043, dr.MessageTypeName));
                    }
                }

                // 6. Выход - Ok
                rc = 0;
                return rc;
            }
            catch (Exception ex)
            {
                Logger.WriteToLog(666, MsessageSeverity.Error, string.Format(Messages.MSG_666, $"{nameof(LogisticsService)}.{nameof(this.UpdateData)}", (ex.InnerException == null ? ex.Message : ex.InnerException.Message)));
                LastException = ex;
                return rc;
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

        #region IDisposable Support

        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);
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
                    connectionFailed = false;
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
