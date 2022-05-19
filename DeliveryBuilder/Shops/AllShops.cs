
namespace DeliveryBuilder.Shops
{
    using DeliveryBuilder.Log;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Работа с магазинами
    /// </summary>
    public class AllShops
    {
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
        public int Count => shops.Count;

        /// <summary>
        /// Конструктор класса AllShops
        /// </summary>
        public AllShops()
        {
            shops = new Dictionary<int, Shop>(64);
        }

        /// <summary>
        /// Добавление магазина в коллекцию
        /// </summary>
        /// <param name="shop"></param>
        public void Add(Shop shop)
        {
            shops.Add(shop.Id, shop);
        }

        /// <summary>
        /// Установка флага Updated магазина
        /// </summary>
        /// <param name="shopId">ID магазина</param>
        /// <param name="value">Значение флага</param>
        public void SetShopUpdated(int shopId, bool value)
        {
            Shop shop;
            if (shops.TryGetValue(shopId, out shop))
            { shop.Updated = value; }
        }

        /// <summary>
        /// Установка флага Updated у всех магазинов
        /// </summary>
        public void SetAllShopUpdated()
        {
            if (shops != null)
            {
                foreach (var shop in shops.Values)
                { shop.Updated = true; }
            }
        }

        /// <summary>
        /// Обновление магазинов
        /// </summary>
        /// <param name="updates">Новые данные</param>
        /// <returns></returns>
        public int Update(ShopsUpdates updates)
        {
            // 1. Иициализация
            int rc = 1;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (updates == null || updates.Updates == null || updates.Updates.Length <= 0)
                    return rc;

                // 3. Отрабатываем изменения данных магазинов
                rc = 3;
                foreach (var shopUpdates in updates.Updates)
                {
                    Shop shop;
                    if (shops.TryGetValue(shopUpdates.ShopId, out shop))
                    {
                        // 3.1 Обновление сущесвующего магазина
                        rc = 31;
                        if (shopUpdates.Latitude != 0)
                            shop.Latitude = shopUpdates.Latitude;
                        if (shopUpdates.Longitude != 0)
                            shop.Longitude = shopUpdates.Longitude;
                        shop.WorkStart = shopUpdates.WorkStart.TimeOfDay;
                        shop.WorkEnd = shopUpdates.WorkEnd.TimeOfDay;
                    }
                    else
                    {
                        // 3.2 Добавление нового магазина
                        rc = 32;
                        if (shopUpdates.Latitude != 0 && shopUpdates.Longitude != 0)
                        {
                            shop = new Shop(shopUpdates.ShopId);
                            shop.WorkStart = shopUpdates.WorkStart.TimeOfDay;
                            shop.WorkEnd = shopUpdates.WorkEnd.TimeOfDay;
                            shop.Latitude = shopUpdates.Latitude;
                            shop.Longitude = shopUpdates.Longitude;
                            shops.Add(shop.Id, shop);
                        }
                    }
                }

                // 4. Выход - Ok
                rc = 0;
                return rc;
            }
            catch (Exception ex)
            {
                Logger.WriteToLog(666, MsessageSeverity.Error, string.Format(Messages.MSG_666, $"{nameof(AllShops)}.{nameof(this.Update)}", (ex.InnerException == null ? ex.Message : ex.InnerException.Message)));
                return rc;
            }
        }
    }
}
