
namespace DeliveryBuilder
{
    using DeliveryBuilder.AverageDeliveryCost;
    using DeliveryBuilder.BuilderParameters;
    using DeliveryBuilder.Couriers;
    using DeliveryBuilder.Db;
    using DeliveryBuilder.Geo;
    using DeliveryBuilder.Geo.Cache;
    using DeliveryBuilder.Log;
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;
    using System.Timers;

    /// <summary>
    /// Основной класс построителя отгрузок
    /// </summary>
    public class LogisticsService : IDisposable
    {
        /// <summary>
        /// Объект для работы с БД LSData
        /// </summary>
        private LSData lsDataDb;
        
        /// <summary>
        /// Объект для работы с внешней БД
        /// </summary>
        private ExternalDb externalDb;

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
        /// Таймер сервиса логистики
        /// </summary>
        private Timer timer;

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
        public int Create(int serviceId, string conectionString)
        {
            // 1. Инициализация
            int rc = 1;
            Dispose(true);
            disposedValue = false;
            IsCreated = false;
            LastException = null;
            FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (string.IsNullOrWhiteSpace(conectionString))
                    return rc;

                // 3. Открываем соединение с БД LSData
                rc = 3;
                lsDataDb = new LSData(conectionString);
                lsDataDb.Open();
                if (!lsDataDb.IsOpen())
                    return rc;

                // 4. Загружаем параметры построителя
                rc = 4;
                config = lsDataDb.SelectConfig(serviceId);
                if (config == null)
                    return rc;

                // 5. Открываем лог
                rc = 5;
                if (config.LoggerParameters != null && !string.IsNullOrWhiteSpace(config.LoggerParameters.Filename))
                {
                    string logFolder = Path.GetDirectoryName(config.LoggerParameters.Filename);
                    if (!Directory.Exists(logFolder))
                        Directory.CreateDirectory(logFolder);
                    Logger.Create(config.LoggerParameters.Filename, config.LoggerParameters.SaveDays);
                }
                else
                {
                    string filename = Path.GetFileName(fileVersionInfo.FileName);
                    filename = Path.ChangeExtension(filename, ".log");
                    filename = Path.Combine(Path.GetDirectoryName(fileVersionInfo.FileName), filename);
                    Logger.Create(filename, 7);
                }

                // 6. Выводим сообщение о начале работы
                rc = 6;
                Logger.WriteToLog(1, MsessageSeverity.Info, string.Format(Messages.MSG_001, Path.GetFileNameWithoutExtension(fileVersionInfo.FileName), fileVersionInfo.ProductVersion, serviceId));

                // 7. Проверяем параметры построителя
                rc = 7;
                if (!TestBuilderConfig(config))
                    return rc;

                // 8. Создаём объект для работы гео-данными
                rc = 8;
                geoData = new GeoData();
                int rc1 = geoData.Create(config);
                if (rc1 != 0)
                    return rc = 10000 * rc + rc1;

                // 9. Cоздаём объект для работы с порогами средней стоимости
                rc = 9;
                AverageDeliveryCostRecord[] records;
                rc1 = lsDataDb.SelectThresholds(out records);
                if (rc1 != 0)
                    return rc = 10000 * rc + rc1;
                thresholds = new AverageCostThresholds();
                rc1 = thresholds.Create(records);
                if (rc1 != 0)
                    return rc = 10000 * rc + rc1;

                // 10. Загружаем параметры способов отгрузки
                rc = 35;
                CourierTypeRecord[] courierTypeRecords = null;
                rc1 = lsDataDb.SelectCourierTypes(out courierTypeRecords);
                if (rc1 != 0)


                    // Выход - Ok
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
                // Сообщение о завершении работы
                Logger.WriteToLog(2, MsessageSeverity.Info, string.Format(Messages.MSG_002, Path.GetFileNameWithoutExtension(fileVersionInfo.FileName), fileVersionInfo.ProductVersion, serviceId));
            }
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
            //// 1. Инициализация
            //bool isCatched = false;
            //if (!IsCreated)
            //    return;

            //try
            //{
            //    isCatched = syncMutex.WaitOne(config.functional_parameters.data_request_interval);
            //    Refresh(0);
            //    //Helper.WriteInfoToLog($"GeoCache capacity {geoCache.HashCapacity}. ItemCount = {geoCache.CacheItemCount}");
            //    Logger.WriteToLog(string.Format(MessagePatterns.GEO_CACHE_INFO, geoCache.HashCapacity, geoCache.CacheItemCount));
            //}
            //catch
            //{ }
            //finally
            //{
            //    if (isCatched)
            //        syncMutex.ReleaseMutex();
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
                        //externalDb.Close();
                        externalDb = null;
                    }

                    //if (syncMutex != null)
                    //{
                    //    DisposeMutex(syncMutex);
                    //    syncMutex = null;
                    //}

                    config = null;
                    //geoCache = null;
                    //allCouriers = null;
                    //allOrders = null;
                    //allShops = null;
                    //queue = null;
                    //checkingQueue = null;
                    //salesmanSolution = null;

                    // TODO: dispose managed state (managed objects).
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
