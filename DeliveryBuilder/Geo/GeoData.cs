
namespace DeliveryBuilder.Geo
{
    using DeliveryBuilder.BuilderParameters;
    using DeliveryBuilder.Geo.Yandex;
    using DeliveryBuilder.Log;
    using System;

    /// <summary>
    /// Получение гео-данных
    /// </summary>
    public class GeoData
    {
        /// <summary>
        /// Флаг: true - экземпляр создан; false - экземпляр не создан
        /// </summary>
        public bool IsCreated { get; private set; }

        /// <summary>
        /// Последнее прерывание
        /// </summary>
        public Exception LastException { get; private set; }

        /// <summary>
        /// Гео-кэш
        /// </summary>
        private GeoCache cache;

        /// <summary>
        /// Гео-кэш
        /// </summary>
        public GeoCache Cache => cache;

        /// <summary>
        /// Yandex-гео
        /// </summary>
        private GeoYandex yandex;

        /// <summary>
        /// Yandex-гео
        /// </summary>
        public GeoYandex Yandex => yandex;

        /// <summary>
        /// Создание экземпляра GeoData
        /// </summary>
        /// <param name="config">Ппараметры</param>
        /// <returns>0 - экземпляр создан; иначе - экземпляр не создан</returns>
        public int Create(BuilderConfig config)
        {
            // 1. Инициализация
            int rc = 1;
            IsCreated = false;
            LastException = null;
            cache = null;
            yandex = null;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (config == null || config.Parameters == null ||
                    config.Parameters.GeoCache == null ||
                    config.Parameters.GeoYandex == null)
                    return rc;

                // 3. Создаём Geo Cache
                rc = 3;
                cache = new GeoCache();
                int rc1 = cache.Create(config.Parameters.GeoCache, config.Parameters.GeoYandex.TypeNameCount);
                if (rc1 != 0)
                    return rc = 1000 * rc + rc1;

                // 4. Создаём Geo Yandex
                rc = 4;
                yandex = new GeoYandex();
                rc1 = yandex.Create(config.Parameters.GeoYandex);
                if (rc1 != 0)
                    return rc = 1000 * rc + rc1;

                // 5. Выход
                rc = 0;
                IsCreated = true;
                return rc;
            }
            catch (Exception ex)
            {
                LastException = ex;
                Logger.WriteToLog(666, MsessageSeverity.Error, string.Format(Messages.MSG_666, $"{nameof(GeoData)}.{nameof(this.Create)}", (ex.InnerException == null ? ex.Message : ex.InnerException.Message)));
                return rc;
            }
        }


    }
}
