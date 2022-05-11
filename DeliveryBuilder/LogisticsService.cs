
namespace DeliveryBuilder
{
    using DeliveryBuilder.BuilderParameters;
    using DeliveryBuilder.Db;
    using DeliveryBuilder.Geo;
    using DeliveryBuilder.Log;
    //using global::LogisticsService.Log;
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
        /// Гео-кэш
        /// </summary>
        private GeoCache geoCache;
        
        /// <summary>
        /// Гео-кэш
        /// </summary>
        private GeoCache Cache => geoCache;

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
                string logFolder = Path.GetDirectoryName(config.LoggerParameters.Filename);
                if (!Directory.Exists(logFolder))
                    Directory.CreateDirectory(logFolder);
                Logger.Create(config.LoggerParameters.Filename, config.LoggerParameters.SaveDays);

                // 6. Выводим сообщение о начале работы
                rc = 6;
                Logger.WriteToLog(1, MsessageSeverity.Info, string.Format(Messages.MSG_001, Path.GetFileNameWithoutExtension(fileVersionInfo.FileName), fileVersionInfo.ProductVersion, serviceId));

                // 7. Проверяем параметры построителя
                rc = 7;
                if (!TestBuilderConfig(config))
                    return rc;



                // Выход - Ok
                rc = 0;
                IsCreated = true;
                return rc;
            }
            catch (Exception ex)
            {
                Logger.WriteToLog(666, MsessageSeverity.Error, string.Format(Messages.MSG_666, nameof(this.Create),(ex.InnerException == null ? ex.Message : ex.InnerException.Message)));
                LastException = ex;
                return rc;
            }
            finally
            {
                // Сообщение о завершении работы
                Logger.WriteToLog(2, MsessageSeverity.Info, string.Format(Messages.MSG_002, Path.GetFileNameWithoutExtension(fileVersionInfo.FileName), fileVersionInfo.ProductVersion, serviceId));
            }
        }

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
                if (config.Parameters.CourierDeliveryMargin < 0)
                { Logger.WriteToLog(4, MsessageSeverity.Error, string.Format(Messages.MSG_004, nameof(config.Parameters.CourierDeliveryMargin))); }
                else if (config.Parameters.TaxiDeliveryMargin < 0)
                { Logger.WriteToLog(4, MsessageSeverity.Error, string.Format(Messages.MSG_004, nameof(config.Parameters.TaxiDeliveryMargin))); }
                else
                { passed = true; }



                // Выход
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
                    geoCache = null;
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
