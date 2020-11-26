
namespace CourierLogistics.SourceData.Orders
{
    using System;
    using System.IO;
    using System.Text;
    using System.Windows.Forms;

    /// <summary>
    /// Загрузчик заказов
    /// </summary>
    public class OrdersLoader
    {
        // 0        1       2      3                    4                        5       6                   7                8                   9                       10          11             12         13               14                      15               16                17                       18              19                                                   20      21                  22                  23       24  
        // id_order;number; ShopNo;date_order;          date_supply;             source; date_collect_start; date_collected;  date_delivery_start;date_delivered;         date_closed;date_cancelled;id_courier;date_courier_set;date_completed;         date_posted;     date_disassembled;date_check_closed;       id_delivery_job;address;                                             comment;latitude;           longitude;          ves      delivery time limit
        // 3482217; 6479912;840;   28.06.2020 19:29    ;2020-06-28 00:00:00.000; 2;      28.06.2020 19:29;   28.06.2020 18:55;NULL;               NULL;                   NULL;       NULL;          NULL;      NULL;            NULL;                   28.06.2020 19:29;NULL;             2020-06-28 18:56:00.000; NULL;           ул Рыбная 1-я, д 63, г Сергиев Посад, Московская обл;       ;56.3036910000000000;38.1465450000000000;3.378000;28.06.2020 21:29
        // 3531188; 4488235;2374;2020-06-30 10:09:14.093;2020-06-30 00:00:00.000;2;      30.06.2020 10:20;   30.06.2020 10:25;30.06.2020 10:46;   2020-06-30 10:54:14.050;NULL;       NULL;          10244308;  30.06.2020 10:46;2020-06-30 10:54:14.053;30.06.2020 10:09;NULL;2020-06-30 10:28:45.000;962997;Москва, Янтарный проезд, 9;Квартира: 62 Домофон: 62в Подъезд: 2 Этаж: 1 Комментарий: Я выйду к Вам в холл;55.8685360000000000;37.6812450000000000;2.795000

        /// <summary>
        /// Загрузка заказов из csv-файла
        /// </summary>
        /// <param name="csvFileName">Путь к загружаемому файлу</param>
        /// <param name="orders">Загруженные заказы</param>
        /// <returns>0 - Заказы успешно загружены; иначе - обнаружены ошибки</returns>
        public static int Load(string csvFileName, out AllOrders orders)
        {
            // 1. Инициализация
            int rc = 1;
            orders = null;

            try
            {
                // 2. Проверяем исходные
                rc = 2;
                if (string.IsNullOrWhiteSpace(csvFileName))
                    return rc;

                // 3. Загружаем все записи
                rc = 3;
                //string[] records = File.ReadAllLines(csvFileName, Encoding.GetEncoding(1251));
                string[] records = File.ReadAllLines(csvFileName, Encoding.UTF8);

                // 4. Создаём коллекцию магазинов
                rc = 4;
                orders = new AllOrders();
                int invalidRecordCount = 0;


                for (int i = 1; i < records.Length; i++)
                {
                    if (string.IsNullOrWhiteSpace(records[i]))
                        continue;

                    string[] items = Helper.ReplaceSemicolon(records[i]).Split(';');
                    if (items != null && items.Length >= 24)
                    {
                        Order order = new Order();
                        order.Id_order = Helper.ParseInt(items[0]);
                        order.Number = Helper.ParseInt(items[1]);
                        order.ShopNo = Helper.ParseInt(items[2]);
                        order.Date_order = Helper.ParseDateTime(items[3]);
                        order.Date_supply = Helper.ParseDateTime(items[4]);
                        order.Source = Helper.ParseInt(items[5]);
                        order.Date_collect_start = Helper.ParseDateTime(items[6]);
                        order.Date_collected = Helper.ParseDateTime(items[7]);
                        order.Date_delivery_start = Helper.ParseDateTime(items[8]);
                        order.Date_delivered = Helper.ParseDateTime(items[9]);
                        order.Date_closed = Helper.ParseDateTime(items[10]);
                        order.Date_cancelled = Helper.ParseDateTime(items[11]);
                        order.Id_courier = Helper.ParseInt(items[12]);
                        order.Date_courier_set = Helper.ParseDateTime(items[13]);
                        order.Date_completed =Helper. ParseDateTime(items[14]);
                        order.Date_posted = Helper.ParseDateTime(items[15]);
                        order.Date_disassembled = Helper.ParseDateTime(items[16]);
                        order.Date_check_closed = Helper.ParseDateTime(items[17]);
                        order.Id_delivery_job = Helper.ParseInt(items[18]);
                        order.Address = items[19].Trim();
                        order.Comment = items[20].Trim();
                        order.Latitude = Helper.ParseDouble(items[21]);
                        order.Longitude = Helper.ParseDouble(items[22]);
                        order.Weight = Helper.ParseDouble(items[23]);

                        if (order.Date_order == DateTime.MinValue ||
                            //order.Date_collected == DateTime.MinValue ||
                            double.IsNaN(order.Latitude) ||
                            double.IsNaN(order.Longitude) || 
                            double.IsNaN(order.Weight))
                            continue;

                        if (items.Length >= 25)
                        {
                            order.DeliveryTimeLimit = Helper.ParseDateTime(items[24]);
                        }
                        else
                        {
                            order.DeliveryTimeLimit = DateTime.MinValue;
                        }

                        if (items.Length >= 26)
                        {
                            order.Date_collected = Helper.ParseDateTime(items[25]);
                        }

                        orders.Add(order);
                    }
                    else
                    {
                        invalidRecordCount++;
                    }
                }

                //if (invalidRecordCount > 0)
                //    return rc;

                // 5. Выход - Ok
                rc = 0;
                return rc;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Загрузка заказов", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return rc;
            }
        }
    }
}
