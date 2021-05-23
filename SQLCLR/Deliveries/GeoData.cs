
namespace SQLCLR.Deliveries
{
    using SQLCLR.Orders;
    using SQLCLR.Shops;
    using System.Data;
    using System.Data.SqlClient;
    using System.Threading.Tasks;

    /// <summary>
    /// Класс для работы с гео-данными Yandex
    /// </summary>
    public class GeoData
    {
        /// <summary>
        /// Проверка наличия и подкачка
        /// в GeoCache требуемых данных
        /// </summary>
        /// <param name="serviceId">ID LogisticsService</param>
        /// <param name="yandexTypeId">ID способа передвижения Yandex</param>
        /// <param name="shop">Магазин</param>
        /// <param name="orders">Заказы</param>
        /// <returns></returns>
        public static int PutData(int serviceId, int yandexTypeId, Shop shop, Order[] orders)
        {
            // 1. Инициализация
            int rc = 1;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (shop == null || shop.Latitude == 0 || shop.Longitude == 0)
                    return rc;
                if (orders == null || orders.Length <= 0)
                    return rc;

                // 3. Открываем соединение
                rc = 3;
                using (SqlConnection connection = new SqlConnection("context connection=true"))
                using (Task openTask = connection.OpenAsync())
                {
                    // 4. Создаём таблицу с исходными данными
                    rc = 4;
                    DataTable table = new DataTable();
                    table.Columns.Add("latitude", typeof(double));
                    table.Columns.Add("longitude", typeof(double));

                    for (int i = 0; i < orders.Length; i++)
                    {
                        table.Rows.Add(orders[i].Latitude, orders[i].Longitude);
                    }

                    table.Rows.Add(shop.Latitude, shop.Longitude);

                    // 5. Строим команду для вызова процедуры lsvH4geo_putEx
                    rc = 5;
                    using (SqlCommand cmd = new SqlCommand("dbo.lsvH4geo_putEx", connection))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        // @service_id
                        var parameter = cmd.Parameters.Add("@service_id", SqlDbType.Int);
                        parameter.Direction = ParameterDirection.Input;
                        parameter.Value = serviceId;

                        // @yandex_type_id
                        parameter = cmd.Parameters.Add("@yandex_type_id", SqlDbType.Int);
                        parameter.Direction = ParameterDirection.Input;
                        parameter.Value = yandexTypeId;

                        // @points
                        parameter = cmd.Parameters.AddWithValue("@yandex_type_id", table);
                        parameter.Direction = ParameterDirection.Input;
                        parameter.SqlDbType = SqlDbType.Structured;
                        parameter.TypeName = "dbo.lsvGeoPointTable";

                        // return code
                        var returnParameter = cmd.Parameters.Add("@ReturnCode", SqlDbType.Int);
                        returnParameter.Direction = ParameterDirection.ReturnValue;

                        // 6. Исполняем команду
                        rc = 6;
                        openTask.Wait();
                        cmd.ExecuteNonQuery();
                        var retCode = returnParameter.Value;
                        if (!(retCode is int))
                            return rc;
                        int rc1 = (int)retCode;
                        if (rc1 != 0)
                            return rc = 1000 * rc + rc1;
                    }
                }

                // 7. Выход - Ok
                rc = 0;
                return rc;
            }
            catch
            {
                return rc;
            }
        }

        /// <summary>
        /// Выбор требуемых гео-данных
        /// </summary>
        /// <param name="serviceId">ID LogisticsService</param>
        /// <param name="yandexTypeId">ID способа передвижения Yandex</param>
        /// <param name="shop">Магазин</param>
        /// <param name="orders">Заказы</param>
        /// <param name="geoData">Гео-данные
        /// (Индексы точек: i - orders[i]; i = orders.Length - shop)
        /// </param>
        /// <returns>0 - гео-данные выбраны; гео-данные не выбраны</returns>
        public static int Select(int serviceId, int yandexTypeId, Shop shop, Order[] orders, out Point[,] geoData)
        {
            // 1. Инициализация
            int rc = 1;
            geoData = null;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (shop == null || shop.Latitude == 0 || shop.Longitude == 0)
                    return rc;
                if (orders == null || orders.Length <= 0)
                    return rc;

                // 3. Открываем соединение
                rc = 3;
                using (SqlConnection connection = new SqlConnection("context connection=true"))
                using (Task openTask = connection.OpenAsync())
                {
                    // 4. Создаём таблицу с исходными данными
                    rc = 4;
                    DataTable table = new DataTable();
                    table.Columns.Add("latitude", typeof(double));
                    table.Columns.Add("longitude", typeof(double));

                    for (int i = 0; i < orders.Length; i++)
                    {
                        table.Rows.Add(orders[i].Latitude, orders[i].Longitude);
                    }

                    table.Rows.Add(shop.Latitude, shop.Longitude);

                    // 5. Строим команду для вызова процедуры lsvH4geo_putEx
                    rc = 5;
                    using (SqlCommand cmd = new SqlCommand("dbo.lsvSelectGeoDataFromCache", connection))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        // @service_id
                        var parameter = cmd.Parameters.Add("@service_id", SqlDbType.Int);
                        parameter.Direction = ParameterDirection.Input;
                        parameter.Value = serviceId;

                        // @yandex_type_id
                        parameter = cmd.Parameters.Add("@yandex_type_id", SqlDbType.Int);
                        parameter.Direction = ParameterDirection.Input;
                        parameter.Value = yandexTypeId;

                        // @points
                        parameter = cmd.Parameters.AddWithValue("@yandex_type_id", table);
                        parameter.Direction = ParameterDirection.Input;
                        parameter.SqlDbType = SqlDbType.Structured;
                        parameter.TypeName = "dbo.lsvGeoPointTable";

                        // return code
                        var returnParameter = cmd.Parameters.Add("@ReturnCode", SqlDbType.Int);
                        returnParameter.Direction = ParameterDirection.ReturnValue;

                        // 6. Исполняем команду
                        rc = 6;
                        openTask.Wait();

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            int iIndex1 = reader.GetOrdinal("index1");
                            int iIndex2 = reader.GetOrdinal("index2");
                            int iDistance = reader.GetOrdinal("distance");
                            int iDuration = reader.GetOrdinal("duration");

                            geoData = new Point[table.Rows.Count, table.Rows.Count];
                            int n = 0;

                            while (reader.Read())
                            {
                                int index1 = reader.GetInt32(iIndex1) - 1;
                                int index2 = reader.GetInt32(iIndex2) - 1;
                                int distance = reader.GetInt32(iDistance);
                                int duration = reader.GetInt32(iDuration);
                                geoData[index1, index2].X = distance;
                                geoData[index1, index2].Y = duration;
                                n++;
                            }

                            if (n != table.Rows.Count * table.Rows.Count)
                                return rc;
                        }
                    }
                }

                // 7. Выход - Ok
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
