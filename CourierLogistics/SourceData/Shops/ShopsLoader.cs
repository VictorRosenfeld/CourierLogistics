
namespace CourierLogistics.SourceData.Shops
{
    using CourierLogistics.Logistics.FloatSolution.ShopInLive;
    using System;
    using System.IO;
    using System.Text;
    using System.Windows.Forms;

    /// <summary>
    /// Загрузчик магазинов и использумые ими типы курьеров
    /// </summary>
    public class ShopsLoader
    {
        /// <summary>
        /// Загрузка магазинов из csv-файла
        /// </summary>
        /// <param name="csvFileName">Путь к загружаемому файлу</param>
        /// <param name="shops">Загруженные магазины</param>
        /// <returns>0 - Магазины успешно загружены; иначе - обнаружены ошибки</returns>
        public static int Load(string csvFileName, out AllShops shops)
        {
            // 1. Инициализация
            int rc = 1;
            shops = null;

            try
            {
                // 2. Проверяем исходные
                rc = 2;
                if (string.IsNullOrWhiteSpace(csvFileName))
                    return rc;

                // 3. Загружаем все записи
                rc = 3;
                string[] records = File.ReadAllLines(csvFileName, Encoding.UTF8);

                // 4. Создаём коллекцию магазинов
                rc = 4;
                shops = new AllShops();
                int invalidRecordCount = 0;

                for (int i = 1; i < records.Length; i++)
                {
                    if (string.IsNullOrWhiteSpace(records[i]))
                        continue;
                    string[] items = records[i].Split(';');
                    if (items != null && items.Length >= 14)
                    {
                        ShopEx shop = new ShopEx();
                        shop.N = Helper.ParseInt(items[0]);
                        shop.Id_TT = Helper.ParseInt(items[1]);
                        shop.Name_TT = items[2].Trim();
                        shop.IsActive = Helper.ParseInt(items[3]);
                        shop.TT_Format = Helper.ParseInt(items[4]);
                        shop.Id_Group = Helper.ParseInt(items[5]);
                        shop.Latitude = Helper.ParseDouble(items[6]);
                        shop.Longitude = Helper.ParseDouble(items[7]);
                        shop.Address = items[8].Trim();
                        shop.Hours = items[9].Trim();
                        shop.Status = items[10].Trim();
                        shop.Region_TT = items[11].Trim();
                        shop.Id_Region_TT = Helper.ParseInt(items[12]);
                        shop.Gettype = Helper.ParseInt(items[13]);

                        shops.Add(shop);
                    }
                    else
                    {
                        invalidRecordCount++;
                    }
                }

                if (invalidRecordCount > 0)
                    return rc;

                // 5. Выход - Ok
                rc = 0;
                return rc;
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "Загрузка магазинов", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return rc;
            }
        }

        /// <summary>
        /// Загрузка способов доставки, применяемых
        /// в магазинах
        /// </summary>
        /// <param name="csvFileName">Имя csv-файла (ShopNo; type  type = 2 - car, type = 3 - onFoot)</param>
        /// <param name="shops">Коллекция магазинов</param>
        /// <returns></returns>
        public static int LoadDeliveryTypes(string csvFileName, AllShops shops)
        {
            // 1. Инициализация
            int rc = 1;
            try
            {
                // 2. Проверяем исходные
                rc = 2;
                if (string.IsNullOrWhiteSpace(csvFileName))
                    return rc;
                if (shops == null || shops.Shops == null || shops.Shops.Count <= 0)
                    return rc;

                // 3. Загружаем все записи
                rc = 3;
                string[] records = File.ReadAllLines(csvFileName, Encoding.UTF8);

                // 4. Подставляем способы доставки магазинов
                rc = 4;
                int n = 0;

                for (int i = 1; i < records.Length; i++)
                {
                    if (string.IsNullOrWhiteSpace(records[i]))
                        continue;
                    string[] items = records[i].Split(';');
                    if (items.Length < 2)
                        continue;

                    int shopId;
                    int deliveryType;

                    if (int.TryParse(items[0], out shopId) &&
                        int.TryParse(items[1], out deliveryType))
                    {
                        Shop shop = shops.GetShop(shopId);
                        if (shop != null)
                        {
                            n++;
                            if (deliveryType == 2)
                            {
                                shop.DeliveryTypes |= DeliveryType.Car;
                            }
                            else if (deliveryType == 3)
                            {
                                shop.DeliveryTypes |= DeliveryType.Bicycle;
                            }
                        }
                    }
                }

                // 5. Выход - Ok
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
