
namespace CourierLogistics.SourceData.Shops
{
    using CourierLogistics.Logistics.FloatSolution.ShopInLive;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Коллекция магазинов
    /// </summary>
    public class AllShops
    {
        /// <summary>
        /// Все магазины
        /// </summary>
        public Dictionary<int, ShopEx> Shops { get; private set; }

        /// <summary>
        /// Конструктор класса AllShops
        /// </summary>
        public AllShops()
        {
            Shops = new Dictionary<int, ShopEx>(1000);
        }

        /// <summary>
        /// Добавление магазина в коллекцию
        /// </summary>
        /// <param name="shop"></param>
        public void Add(ShopEx shop)
        {
            Shops.Add(shop.N, shop);
        }

        /// <summary>
        /// Извлечь магазин по значению N
        /// </summary>
        /// <param name="key">N</param>
        /// <returns>Магазин или null</returns>
        public Shop GetShop(int key)
        {
            ShopEx shop;
            Shops.TryGetValue(key, out shop);
            return shop;
        }

        /// <summary>
        /// Выобор всех магазинов заданного региона
        /// </summary>
        /// <param name="id_region_tt">Код региона</param>
        /// <returns>Все найденные магазины</returns>
        public Shop[] SelectShopsByIdRegion(int id_region_tt)
        {
            return Shops.Values.Where(s => s.Id_Region_TT == id_region_tt).ToArray();
        }
    }
}
