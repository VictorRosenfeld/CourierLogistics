
namespace LogisticsService.Shops
{
    using LogisticsService.Locations;
    using System.Collections.Generic;
    using static LogisticsService.API.GetShopEvents;

    /// <summary>
    /// Все магазины
    /// </summary>
    public class AllShops
    {
        /// <summary>
        /// Объект синхронизации для многопоточной работы
        /// </summary>
        private object syncRoot = new object();

        /// <summary>
        /// Менеджер координат, расстояний и времени движения
        /// </summary>
        private LocationManager locationManager;

        /// <summary>
        /// Флаг: true - класс создан; false - класс не создан
        /// </summary>
        public static bool IsCreated { get; private set; }

        /// <summary>
        /// Все магазины
        /// </summary>
        private Dictionary<int, Shop> shops;

        /// <summary>
        /// Все магазины
        /// </summary>
        public Dictionary<int, Shop> Shops => shops;

        /// <summary>
        /// Количество магазинов
        /// </summary>
        public int Count => (shops == null ? 0 : shops.Count);

        /// <summary>
        /// Создание экземпляра
        /// </summary>
        /// <param name="locManager">Менеджер координат, расстояний и времени движения</param>
        /// <param name="shopLimit">Макимальное число магазинов</param>
        /// <returns>0 - экземпляр создан; экземпляр не создан</returns>
        public int Create(LocationManager locManager, int shopLimit = -1)
        {
            lock (syncRoot)
            {
                // 1. Инициализация
                int rc = 1;
                IsCreated = false;
                shops = null;
                locationManager = null;

                try
                {
                    // 2. Проверяем исходные данные
                    rc = 2;
                    if (locManager == null)
                        return rc;

                    locationManager = locManager;

                    // 3. Создаём общую коллекцию магазинов
                    rc = 3;
                    if (shopLimit <= 0) shopLimit = 3000;
                    shops = new Dictionary<int, Shop>(shopLimit);

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
        }

        /// <summary>
        /// Обновление информации о магазинах
        /// </summary>
        /// <param name="events">События магазинов</param>
        /// <returns>0 - данные магазинов обновлены; иначе - данные о магазинах не обновлены</returns>
        public int Refresh(ShopEvent[] events)
        {
            lock (syncRoot)
            {
                // 1. Инициализация
                int rc = 1;

                try
                {
                    // 2. Проверяем исходные данные
                    rc = 2;
                    if (!IsCreated)
                        return rc;
                    if (events == null || events.Length <= 0)
                        return rc;

                    // 3. Отрабатываем изменения
                    rc = 3;
                    for (int i = 0; i < events.Length; i++)
                    {
                        // 3.1 Извлекаем событие 
                        rc = 31;
                        ShopEvent shopEvent = events[i];

                        // 3.2 Извлекаем магазин
                        rc = 32;
                        Shop shop;

                        if (!shops.TryGetValue(shopEvent.shop_id, out shop))
                        {
                            // 3.4 Создаём магазин
                            rc = 34;
                            shop = new Shop(shopEvent.shop_id);
                            shop.Latitude = shopEvent.geo_lat;
                            shop.Longitude = shopEvent.geo_lon;
                            shop.LocationIndex = locationManager.GetLocationIndex(shopEvent.geo_lat, shopEvent.geo_lon);
                            shops.Add(shop.Id, shop);
                        }

                        // 3.5. Обновляем параметры магазина
                        rc = 35;
                        shop.WorkStart = shopEvent.work_start.TimeOfDay;
                        shop.WorkEnd = shopEvent.work_end.TimeOfDay;
                    }

                    // 4. Выход - Ok
                    rc = 0;
                    return rc;
                }
                catch
                {
                    return rc;
                }
            }
        }
    }
}
