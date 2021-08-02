
namespace SQLCLR.Deliveries
{
    using SQLCLR.Log;
    using SQLCLR.Orders;
    using SQLCLR.Shops;
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.IO;
    using System.Xml.Serialization;

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
                //using (Task openTask = connection.OpenAsync())
                {
                    connection.Open();
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
                        //openTask.Wait();
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

        ///// <summary>
        ///// Выбор требуемых гео-данных
        ///// </summary>
        ///// <param name="serviceId">ID LogisticsService</param>
        ///// <param name="yandexTypeId">ID способа передвижения Yandex</param>
        ///// <param name="shop">Магазин</param>
        ///// <param name="orders">Заказы</param>
        ///// <param name="geoData">Гео-данные
        ///// (Индексы точек: i - orders[i]; i = orders.Length - shop)
        ///// </param>
        ///// <returns>0 - гео-данные выбраны; гео-данные не выбраны</returns>
        //public static int Select(int serviceId, int yandexTypeId, Shop shop, Order[] orders, out Point[,] geoData)
        //{
        //    // 1. Инициализация
        //    int rc = 1;
        //    geoData = null;

        //    try
        //    {
        //        // 2. Проверяем исходные данные
        //        rc = 2;
        //        if (shop == null || shop.Latitude == 0 || shop.Longitude == 0)
        //            return rc;
        //        if (orders == null || orders.Length <= 0)
        //            return rc;

        //        #if debug
        //            Logger.WriteToLog(401, $"GeoData.Select enter. service_id = {serviceId}, shop_id = {shop.Id}, yandex_type_id = {yandexTypeId}, orders = {orders.Length}", 0);
        //        #endif

        //        // 3. Открываем соединение
        //        rc = 3;
        //        using (SqlConnection connection = new SqlConnection("context connection=true"))
        //        //using (Task openTask = connection.OpenAsync())
        //        {
        //            connection.Open();
        //            // 4. Создаём таблицу с исходными данными
        //            rc = 4;
        //            DataTable table = new DataTable();
        //            table.Columns.Add("latitude", typeof(double));
        //            table.Columns.Add("longitude", typeof(double));

        //            for (int i = 0; i < orders.Length; i++)
        //            {
        //                table.Rows.Add(orders[i].Latitude, orders[i].Longitude);
        //            }

        //            table.Rows.Add(shop.Latitude, shop.Longitude);

        //            // 5. Строим команду для вызова процедуры lsvH4geo_putEx
        //            rc = 5;
        //            using (SqlCommand cmd = new SqlCommand("EXEC dbo.lsvSelectGeoDataFromCache @service_id, @yandex_type_id, @points", connection))
        //            {
        //                cmd.CommandType = CommandType.StoredProcedure;

        //                // @service_id
        //                var parameter = cmd.Parameters.Add("@service_id", SqlDbType.Int);
        //                parameter.Direction = ParameterDirection.Input;
        //                parameter.Value = serviceId;

        //                // @yandex_type_id
        //                parameter = cmd.Parameters.Add("@yandex_type_id", SqlDbType.Int);
        //                parameter.Direction = ParameterDirection.Input;
        //                parameter.Value = yandexTypeId;

        //                // @points
        //                parameter = cmd.Parameters.AddWithValue("@points", table);
        //                parameter.Direction = ParameterDirection.Input;
        //                parameter.SqlDbType = SqlDbType.Udt;
        //                parameter.TypeName = "dbo.lsvGeoPointTable";

        //                // return code
        //                //var returnParameter = cmd.Parameters.Add("@ReturnCode", SqlDbType.Int);
        //                //returnParameter.Direction = ParameterDirection.ReturnValue;

        //                // 6. Исполняем команду
        //                rc = 6;
        //                #if debug
        //                    Logger.WriteToLog(404, $"GeoData.Select openTask.Wait(). service_id = {serviceId}, shop_id = {shop.Id}, yandex_type_id = {yandexTypeId}, orders = {orders.Length}", 0);
        //                #endif
        //                //openTask.Wait();

        //                #if debug
        //                    Logger.WriteToLog(405, $"GeoData.Select ExecuteReader. service_id = {serviceId}, shop_id = {shop.Id}, yandex_type_id = {yandexTypeId}, orders = {orders.Length}", 0);
        //                #endif

        //                using (SqlDataReader reader = cmd.ExecuteReader())
        //                {
        //                    int iIndex1 = reader.GetOrdinal("index1");
        //                    int iIndex2 = reader.GetOrdinal("index2");
        //                    int iDistance = reader.GetOrdinal("distance");
        //                    int iDuration = reader.GetOrdinal("duration");

        //                    geoData = new Point[table.Rows.Count, table.Rows.Count];
        //                    int n = 0;

        //                    while (reader.Read())
        //                    {
        //                        int index1 = reader.GetInt32(iIndex1) - 1;
        //                        int index2 = reader.GetInt32(iIndex2) - 1;
        //                        int distance = reader.GetInt32(iDistance);
        //                        int duration = reader.GetInt32(iDuration);
        //                        geoData[index1, index2].X = distance;
        //                        geoData[index1, index2].Y = duration;
        //                        n++;
        //                    }

        //                    if (n != table.Rows.Count * table.Rows.Count)
        //                    {
        //                        #if debug
        //                            Logger.WriteToLog(402, $"GeoData.Select exit rc = {rc}. service_id = {serviceId}, shop_id = {shop.Id}, yandex_type_id = {yandexTypeId}, orders = {orders.Length}", 1);
        //                        #endif
        //                        return rc;
        //                    }
        //                }

        //                connection.Close();
        //            }
        //        }

        //        // 7. Выход - Ok
        //        rc = 0;

        //        #if debug
        //            Logger.WriteToLog(402, $"GeoData.Select exit rc = {rc}. service_id = {serviceId}, shop_id = {shop.Id}, yandex_type_id = {yandexTypeId}, orders = {orders.Length}", 0);
        //        #endif

        //        return rc;
        //    }
        //    catch (Exception ex)
        //    {
        //        #if debug
        //            Logger.WriteToLog(403, $"GeoData.Select exit rc = {rc}. service_id = {serviceId}, shop_id = {shop.Id}, yandex_type_id = {yandexTypeId}, orders = {orders.Length} Exception = {ex.Message}", 2);
        //        #endif
        //        return rc;
        //    }
        //}

        ///// <summary>
        ///// Выбор требуемых гео-данных
        ///// </summary>
        ///// <param name="connection">Открытое соединение</param>
        ///// <param name="serviceId">ID LogisticsService</param>
        ///// <param name="yandexTypeId">ID способа передвижения Yandex</param>
        ///// <param name="shop">Магазин</param>
        ///// <param name="orders">Заказы</param>
        ///// <param name="geoData">Гео-данные
        ///// (Индексы точек: i - orders[i]; i = orders.Length - shop)
        ///// </param>
        ///// <returns>0 - гео-данные выбраны; гео-данные не выбраны</returns>
        //public static int Select_0(SqlConnection connection, int serviceId, int yandexTypeId, Shop shop, Order[] orders, out Point[,] geoData)
        //{
        //    // 1. Инициализация
        //    int rc = 1;
        //    geoData = null;

        //    try
        //    {
        //        // 2. Проверяем исходные данные
        //        rc = 2;
        //        if (connection == null || connection.State != ConnectionState.Open)
        //            return rc;
        //        if (shop == null || shop.Latitude == 0 || shop.Longitude == 0)
        //            return rc;
        //        if (orders == null || orders.Length <= 0)
        //            return rc;

        //        #if debug
        //            Logger.WriteToLog(401, $"GeoData.Select enter. service_id = {serviceId}, shop_id = {shop.Id}, yandex_type_id = {yandexTypeId}, orders = {orders.Length}", 0);
        //        #endif

        //            // 4. Создаём таблицу с исходными данными
        //            rc = 4;
        //            DataTable table = new DataTable();
        //            table.Columns.Add("latitude", typeof(double));
        //            table.Columns.Add("longitude", typeof(double));

        //            for (int i = 0; i < orders.Length; i++)
        //            {
        //                table.Rows.Add(orders[i].Latitude, orders[i].Longitude);
        //            }

        //            table.Rows.Add(shop.Latitude, shop.Longitude);

        //            // 5. Строим команду для вызова процедуры lsvH4geo_putEx
        //            rc = 5;
        //            //using (SqlCommand cmd = new SqlCommand("dbo.lsvSelectGeoDataFromCache @service_id, @yandex_type_id, @points", connection))
        //            using (SqlCommand cmd = new SqlCommand("dbo.lsvSelectGeoDataFromCache", connection))
        //            {
        //                cmd.CommandType = CommandType.StoredProcedure;

        //                // @service_id
        //                var parameter = cmd.Parameters.Add("@service_id", SqlDbType.Int);
        //                parameter.Direction = ParameterDirection.Input;
        //                parameter.Value = serviceId;

        //                // @yandex_type_id
        //                parameter = cmd.Parameters.Add("@yandex_type_id", SqlDbType.Int);
        //                parameter.Direction = ParameterDirection.Input;
        //                parameter.Value = yandexTypeId;

        //                // @points
        //                parameter = cmd.Parameters.AddWithValue("@points", table);
        //                parameter.Direction = ParameterDirection.Input;
        //                parameter.SqlDbType = SqlDbType.Structured;
        //                parameter.TypeName = "dbo.lsvGeoPointTable";

        //                // return code
        //                var returnParameter = cmd.Parameters.Add("@ReturnCode", SqlDbType.Int);
        //                returnParameter.Direction = ParameterDirection.ReturnValue;

        //                // 6. Исполняем команду
        //                rc = 6;
        //                //#if debug
        //                //    Logger.WriteToLog(404, $"GeoData.Select openTask.Wait(). service_id = {serviceId}, shop_id = {shop.Id}, yandex_type_id = {yandexTypeId}, orders = {orders.Length}", 0);
        //                //#endif
        //                //openTask.Wait();

        //                #if debug
        //                    Logger.WriteToLog(405, $"GeoData.Select ExecuteReader. service_id = {serviceId}, shop_id = {shop.Id}, yandex_type_id = {yandexTypeId}, orders = {orders.Length}", 0);
        //                #endif

        //                using (SqlDataReader reader = cmd.ExecuteReader())
        //                {
        //                    int iIndex1 = reader.GetOrdinal("index1");
        //                    int iIndex2 = reader.GetOrdinal("index2");
        //                    int iDistance = reader.GetOrdinal("distance");
        //                    int iDuration = reader.GetOrdinal("duration");

        //                    geoData = new Point[table.Rows.Count, table.Rows.Count];
        //                    int n = 0;

        //                    while (reader.Read())
        //                    {
        //                        int index1 = reader.GetInt32(iIndex1) - 1;
        //                        int index2 = reader.GetInt32(iIndex2) - 1;
        //                        int distance = reader.GetInt32(iDistance);
        //                        int duration = reader.GetInt32(iDuration);
        //                        geoData[index1, index2].X = distance;
        //                        geoData[index1, index2].Y = duration;
        //                        n++;
        //                    }

        //                    if (n != table.Rows.Count * table.Rows.Count)
        //                    {
        //                        #if debug
        //                            Logger.WriteToLog(402, $"GeoData.Select exit rc = {rc}. service_id = {serviceId}, shop_id = {shop.Id}, yandex_type_id = {yandexTypeId}, orders = {orders.Length}", 1);
        //                        #endif
        //                        return rc;
        //                    }
        //                }
        //            }

        //        // 7. Выход - Ok
        //        rc = 0;

        //        #if debug
        //            Logger.WriteToLog(402, $"GeoData.Select exit rc = {rc}. service_id = {serviceId}, shop_id = {shop.Id}, yandex_type_id = {yandexTypeId}, orders = {orders.Length}", 0);
        //        #endif

        //        return rc;
        //    }
        //    catch (Exception ex)
        //    {
        //        #if debug
        //            Logger.WriteToLog(403, $"GeoData.Select exit rc = {rc}. service_id = {serviceId}, shop_id = {shop.Id}, yandex_type_id = {yandexTypeId}, orders = {orders.Length} Exception = {ex.Message}", 2);
        //        #endif
        //        return rc;
        //    }
        //}

        ///// <summary>
        ///// Выбор требуемых гео-данных
        ///// </summary>
        ///// <param name="connection">Открытое соединение</param>
        ///// <param name="serviceId">ID LogisticsService</param>
        ///// <param name="yandexTypeId">ID способа передвижения Yandex</param>
        ///// <param name="shop">Магазин</param>
        ///// <param name="orders">Заказы</param>
        ///// <param name="geoData">Гео-данные
        ///// (Индексы точек: i - orders[i]; i = orders.Length - shop)
        ///// </param>
        ///// <returns>0 - гео-данные выбраны; гео-данные не выбраны</returns>
        //public static int Select1(SqlConnection connection, int serviceId, int yandexTypeId, Shop shop, Order[] orders, out Point[,] geoData)
        //{
        //    // 1. Инициализация
        //    int rc = 1;
        //    geoData = null;

        //    try
        //    {
        //        // 2. Проверяем исходные данные
        //        rc = 2;
        //        if (connection == null || connection.State != ConnectionState.Open)
        //            return rc;
        //        if (shop == null || shop.Latitude == 0 || shop.Longitude == 0)
        //            return rc;
        //        if (orders == null || orders.Length <= 0)
        //            return rc;

        //        #if debug
        //            Logger.WriteToLog(401, $"GeoData.Select enter. service_id = {serviceId}, shop_id = {shop.Id}, yandex_type_id = {yandexTypeId}, orders = {orders.Length}", 0);
        //        #endif

        //            // 4. Создаём таблицу с исходными данными
        //            rc = 4;
        //            DataTable table = new DataTable();
        //            table.Columns.Add("latitude", typeof(double));
        //            table.Columns.Add("longitude", typeof(double));

        //            for (int i = 0; i < orders.Length; i++)
        //            {
        //                table.Rows.Add(orders[i].Latitude, orders[i].Longitude);
        //            }

        //            table.Rows.Add(shop.Latitude, shop.Longitude);

        //            // 5. Строим команду для вызова процедуры lsvH4geo_putEx
        //            rc = 5;
        //            //using (SqlCommand cmd = new SqlCommand("dbo.lsvSelectGeoDataFromCache @service_id, @yandex_type_id, @points", connection))
        //            using (SqlCommand cmd = new SqlCommand("dbo.lsvSelectGeoDataFromCache", connection))
        //            {
        //                cmd.CommandType = CommandType.StoredProcedure;

        //                // @service_id
        //                SqlParameter parameter = cmd.Parameters.Add("@service_id", SqlDbType.Int);
        //                parameter.Direction = ParameterDirection.Input;
        //                parameter.Value = serviceId;

        //                // @yandex_type_id
        //                parameter = cmd.Parameters.Add("@yandex_type_id", SqlDbType.Int);
        //                parameter.Direction = ParameterDirection.Input;
        //                parameter.Value = yandexTypeId;

        //                // @points
        //                parameter = cmd.Parameters.AddWithValue("@points", table);
        //                parameter.Direction = ParameterDirection.Input;
        //                parameter.SqlDbType = SqlDbType.Structured;
        //                parameter.TypeName = "dbo.lsvGeoPointTable";

        //                 //                   --индекс первой точки пары(0 - base)

        //                 //   index1 int,

        //                 //   --индекс второй точки пары(0 - base)

        //                 //   index2 int,

        //                 //   --ключ пары точек

        //                 //   pair_key bigint,

        //                 //   --расстояние между точками, метров

        //                 //   distance int, --NOT NULL,
	       //                 //--время движения от точки до точки, секунд
        //                 //  duration int --NOT NULL

        //               // return index1 
        //                parameter = cmd.Parameters.Add("@index1", SqlDbType.Int);
        //                parameter.Direction = ParameterDirection.ReturnValue;

        //               // return index2 
        //                parameter = cmd.Parameters.Add("@index2", SqlDbType.Int);
        //                parameter.Direction = ParameterDirection.ReturnValue;

        //                // return pair_key 
        //                parameter = cmd.Parameters.Add("@pair_key", SqlDbType.BigInt);
        //                parameter.Direction = ParameterDirection.ReturnValue;

        //               // return distance 
        //                parameter = cmd.Parameters.Add("@distance", SqlDbType.Int);
        //                parameter.Direction = ParameterDirection.ReturnValue;

        //               // return duration 
        //                parameter = cmd.Parameters.Add("@duration", SqlDbType.Int);
        //                parameter.Direction = ParameterDirection.ReturnValue;

        //               // return code
        //               var returnParameter = cmd.Parameters.Add("@ReturnCode", SqlDbType.Int);
        //                returnParameter.Direction = ParameterDirection.ReturnValue;

        //                // 6. Исполняем команду
        //                rc = 6;
        //                //#if debug
        //                //    Logger.WriteToLog(404, $"GeoData.Select openTask.Wait(). service_id = {serviceId}, shop_id = {shop.Id}, yandex_type_id = {yandexTypeId}, orders = {orders.Length}", 0);
        //                //#endif
        //                //openTask.Wait();

        //                #if debug
        //                    Logger.WriteToLog(405, $"GeoData.Select ExecuteReader. service_id = {serviceId}, shop_id = {shop.Id}, yandex_type_id = {yandexTypeId}, orders = {orders.Length}", 0);
        //                #endif

        //                using (SqlDataReader reader = cmd.ExecuteReader())
        //                {
        //                    int iIndex1 = reader.GetOrdinal("index1");
        //                    int iIndex2 = reader.GetOrdinal("index2");
        //                    int iDistance = reader.GetOrdinal("distance");
        //                    int iDuration = reader.GetOrdinal("duration");

        //                    geoData = new Point[table.Rows.Count, table.Rows.Count];
        //                    int n = 0;

        //                    while (reader.Read())
        //                    {
        //                        int index1 = reader.GetInt32(iIndex1) - 1;
        //                        int index2 = reader.GetInt32(iIndex2) - 1;
        //                        int distance = reader.GetInt32(iDistance);
        //                        int duration = reader.GetInt32(iDuration);
        //                        geoData[index1, index2].X = distance;
        //                        geoData[index1, index2].Y = duration;
        //                        n++;
        //                    }

        //                    if (n != table.Rows.Count * table.Rows.Count)
        //                    {
        //                        #if debug
        //                            Logger.WriteToLog(402, $"GeoData.Select exit rc = {rc}. service_id = {serviceId}, shop_id = {shop.Id}, yandex_type_id = {yandexTypeId}, orders = {orders.Length}", 1);
        //                        #endif
        //                        return rc;
        //                    }
        //                }
        //            }

        //        // 7. Выход - Ok
        //        rc = 0;

        //        #if debug
        //            Logger.WriteToLog(402, $"GeoData.Select exit rc = {rc}. service_id = {serviceId}, shop_id = {shop.Id}, yandex_type_id = {yandexTypeId}, orders = {orders.Length}", 0);
        //        #endif

        //        return rc;
        //    }
        //    catch (Exception ex)
        //    {
        //        #if debug
        //            Logger.WriteToLog(403, $"GeoData.Select exit rc = {rc}. service_id = {serviceId}, shop_id = {shop.Id}, yandex_type_id = {yandexTypeId}, orders = {orders.Length} Exception = {ex.Message}", 2);
        //        #endif
        //        return rc;
        //    }
        //}

        ///// <summary>
        ///// Выбор требуемых гео-данных
        ///// </summary>
        ///// <param name="connection">Открытое соединение</param>
        ///// <param name="serviceId">ID LogisticsService</param>
        ///// <param name="yandexTypeId">ID способа передвижения Yandex</param>
        ///// <param name="shop">Магазин</param>
        ///// <param name="orders">Заказы</param>
        ///// <param name="geoData">Гео-данные
        ///// (Индексы точек: i - orders[i]; i = orders.Length - shop)
        ///// </param>
        ///// <returns>0 - гео-данные выбраны; гео-данные не выбраны</returns>
        //public static int Select2(SqlConnection connection, int serviceId, int yandexTypeId, Shop shop, Order[] orders, out Point[,] geoData)
        //{
        //    // 1. Инициализация
        //    int rc = 1;
        //    geoData = null;

        //    try
        //    {
        //        // 2. Проверяем исходные данные
        //        rc = 2;
        //        if (connection == null || connection.State != ConnectionState.Open)
        //            return rc;
        //        if (shop == null || shop.Latitude == 0 || shop.Longitude == 0)
        //            return rc;
        //        if (orders == null || orders.Length <= 0)
        //            return rc;

        //        #if debug
        //            Logger.WriteToLog(401, $"GeoData.Select enter. service_id = {serviceId}, shop_id = {shop.Id}, yandex_type_id = {yandexTypeId}, orders = {orders.Length}", 0);
        //        #endif

        //            // 4. Создаём таблицу с исходными данными
        //            rc = 4;
        //            DataTable table = new DataTable();
        //            table.Columns.Add("latitude", typeof(double));
        //            table.Columns.Add("longitude", typeof(double));

        //            for (int i = 0; i < orders.Length; i++)
        //            {
        //                table.Rows.Add(orders[i].Latitude, orders[i].Longitude);
        //            }

        //            table.Rows.Add(shop.Latitude, shop.Longitude);

        //            // 5. Строим команду для вызова процедуры lsvH4geo_putEx
        //            rc = 5;
        //        //using (SqlCommand cmd = new SqlCommand("dbo.lsvSelectGeoDataFromCache @service_id, @yandex_type_id, @points", connection))
        //        using (DataTable dt = new DataTable())
        //        using (SqlCommand cmd = new SqlCommand("dbo.lsvSelectGeoDataFromCache", connection))
        //        {
        //            cmd.CommandType = CommandType.StoredProcedure;

        //            // @service_id
        //            SqlParameter parameter = cmd.Parameters.Add("@service_id", SqlDbType.Int);
        //            parameter.Direction = ParameterDirection.Input;
        //            parameter.Value = serviceId;

        //            // @yandex_type_id
        //            parameter = cmd.Parameters.Add("@yandex_type_id", SqlDbType.Int);
        //            parameter.Direction = ParameterDirection.Input;
        //            parameter.Value = yandexTypeId;

        //            // @points
        //            parameter = cmd.Parameters.AddWithValue("@points", table);
        //            parameter.Direction = ParameterDirection.Input;
        //            parameter.SqlDbType = SqlDbType.Structured;
        //            parameter.TypeName = "dbo.lsvGeoPointTable";

        //            ////  //                   --индекс первой точки пары(0 - base)

        //            ////  //   index1 int,

        //            ////  //   --индекс второй точки пары(0 - base)

        //            ////  //   index2 int,

        //            ////  //   --ключ пары точек

        //            ////  //   pair_key bigint,

        //            ////  //   --расстояние между точками, метров

        //            ////  //   distance int, --NOT NULL,
        //            ////  //--время движения от точки до точки, секунд
        //            ////  //  duration int --NOT NULL

        //            ////// return index1 
        //            //// parameter = cmd.Parameters.Add("@index1", SqlDbType.Int);
        //            //// parameter.Direction = ParameterDirection.ReturnValue;

        //            ////// return index2 
        //            //// parameter = cmd.Parameters.Add("@index2", SqlDbType.Int);
        //            //// parameter.Direction = ParameterDirection.ReturnValue;

        //            //// // return pair_key 
        //            //// parameter = cmd.Parameters.Add("@pair_key", SqlDbType.BigInt);
        //            //// parameter.Direction = ParameterDirection.ReturnValue;

        //            ////// return distance 
        //            //// parameter = cmd.Parameters.Add("@distance", SqlDbType.Int);
        //            //// parameter.Direction = ParameterDirection.ReturnValue;

        //            ////// return duration 
        //            //// parameter = cmd.Parameters.Add("@duration", SqlDbType.Int);
        //            //// parameter.Direction = ParameterDirection.ReturnValue;

        //            // return code
        //            var returnParameter = cmd.Parameters.Add("@ReturnCode", SqlDbType.Int);
        //            returnParameter.Direction = ParameterDirection.ReturnValue;

        //            // 6. Исполняем команду
        //            rc = 6;
        //            //#if debug
        //            //    Logger.WriteToLog(404, $"GeoData.Select openTask.Wait(). service_id = {serviceId}, shop_id = {shop.Id}, yandex_type_id = {yandexTypeId}, orders = {orders.Length}", 0);
        //            //#endif
        //            //openTask.Wait();

        //            #if debug
        //                Logger.WriteToLog(405, $"GeoData.Selecta dapter.Fill. service_id = {serviceId}, shop_id = {shop.Id}, yandex_type_id = {yandexTypeId}, orders = {orders.Length}", 0);
        //            #endif

        //            using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
        //            {
        //                adapter.Fill(dt);
        //            }

        //            if (dt.Rows.Count != table.Rows.Count * table.Rows.Count)
        //            {
        //                #if debug
        //                    Logger.WriteToLog(402, $"GeoData.Select exit rc = {rc}. service_id = {serviceId}, shop_id = {shop.Id}, yandex_type_id = {yandexTypeId}, orders = {orders.Length}", 1);
        //                #endif
        //                return rc;
        //            }
                   
        //            geoData = new Point[table.Rows.Count, table.Rows.Count];
        //            int iIndex1 = dt.Columns["index1"].Ordinal;
        //            int iIndex2 = dt.Columns["index2"].Ordinal;
        //            int iDistance = dt.Columns["distance"].Ordinal;
        //            int iDuration = dt.Columns["duration"].Ordinal;

        //            foreach (DataRow row in dt.Rows)
        //            {
        //                int index1 = (int)row[iIndex1] - 1;
        //                int index2 = (int)row[iIndex2] - 1;
        //                int distance = (int)row[iDistance];
        //                int duration = (int)row[iDuration];
        //                Logger.WriteToLog(405, $"GeoData.Select. geoData[{index1}, {index2}] = ({distance}, {duration})", 0);

        //                geoData[index1, index2].X = distance;
        //                geoData[index1, index2].Y = duration;
        //            }
        //        }

        //        // 7. Выход - Ok
        //        rc = 0;

        //        #if debug
        //            Logger.WriteToLog(402, $"GeoData.Select exit rc = {rc}. service_id = {serviceId}, shop_id = {shop.Id}, yandex_type_id = {yandexTypeId}, orders = {orders.Length}", 0);
        //        #endif

        //        return rc;
        //    }
        //    catch (Exception ex)
        //    {
        //        #if debug
        //            Logger.WriteToLog(403, $"GeoData.Select exit rc = {rc}. service_id = {serviceId}, shop_id = {shop.Id}, yandex_type_id = {yandexTypeId}, orders = {orders.Length} Exception = {ex.Message}", 2);
        //        #endif
        //        return rc;
        //    }
        //}

        /// <summary>
        /// Выбор требуемых гео-данных
        /// </summary>
        /// <param name="connection">Открытое соединение</param>
        /// <param name="serviceId">ID LogisticsService</param>
        /// <param name="yandexTypeId">ID способа передвижения Yandex</param>
        /// <param name="shop">Магазин</param>
        /// <param name="orders">Заказы</param>
        /// <param name="geoData">Гео-данные
        /// (Индексы точек: i - orders[i]; i = orders.Length - shop)
        /// </param>
        /// <returns>0 - гео-данные выбраны; гео-данные не выбраны</returns>
        public static int Select(SqlConnection connection, int serviceId, int yandexTypeId, Shop shop, Order[] orders, out Point[,] geoData)
        {
            // 1. Инициализация
            int rc = 1;
            geoData = null;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (connection == null || connection.State != ConnectionState.Open)
                    return rc;
                if (shop == null || shop.Latitude == 0 || shop.Longitude == 0)
                    return rc;
                if (orders == null || orders.Length <= 0)
                    return rc;

                #if debug
                    Logger.WriteToLog(401, $"GeoData.Select enter. service_id = {serviceId}, shop_id = {shop.Id}, yandex_type_id = {yandexTypeId}, orders = {orders.Length}", 0);
                #endif

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
                //using (SqlCommand cmd = new SqlCommand("dbo.lsvSelectGeoDataFromCache @service_id, @yandex_type_id, @points", connection))
                using (SqlCommand cmd = new SqlCommand("dbo.lsvSelectGeoDataFromCacheEx", connection))
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
                    parameter = cmd.Parameters.AddWithValue("@points", table);
                    parameter.Direction = ParameterDirection.Input;
                    parameter.SqlDbType = SqlDbType.Structured;
                    parameter.TypeName = "dbo.lsvGeoPointTable";

                    // @geo_data
                    var outputParameter = cmd.Parameters.Add("@geo_data", SqlDbType.VarChar, int.MaxValue);
                    outputParameter.Direction = ParameterDirection.Output;

                    // return code
                    var returnParameter = cmd.Parameters.Add("@ReturnCode", SqlDbType.Int);
                    returnParameter.Direction = ParameterDirection.ReturnValue;
                    returnParameter.Value = -1;

                    // 6. Исполняем команду
                    rc = 6;
                    cmd.ExecuteNonQuery();
                    #if debug
                        Logger.WriteToLog(405, $"GeoData.Select cmd.ExecuteNonQuery(). rc = {returnParameter.Value}, geo_data = {outputParameter.Value}", 0);
                    #endif
                    if ((int)returnParameter.Value != 0)
                        return rc;

                    // 7. Извлекаем результат
                    rc = 7;
                    XmlSerializer serializer = new XmlSerializer(typeof(geo_data));
                    using (StringReader sr = new StringReader(outputParameter.Value as string))
                    {
                        geo_data data = (geo_data)serializer.Deserialize(sr);
                        if (data != null)
                        {
                            if (data.Count != table.Rows.Count * table.Rows.Count)
                            {
                                #if debug
                                    Logger.WriteToLog(402, $"GeoData.Select exit rc = {rc}. service_id = {serviceId}, shop_id = {shop.Id}, yandex_type_id = {yandexTypeId}, orders = {orders.Length}", 1);
                                #endif

                                return rc;
                            }

                            geoData = data.GetGeoData(table.Rows.Count);
                        }
                        else
                        {
                            #if debug
                                Logger.WriteToLog(402, $"GeoData.Select exit rc = {rc}. service_id = {serviceId}, shop_id = {shop.Id}, yandex_type_id = {yandexTypeId}, orders = {orders.Length}", 2);
                            #endif
                        }
                    }

                }
                // 8. Выход - Ok
                rc = 0;

                #if debug
                    Logger.WriteToLog(402, $"GeoData.Select exit rc = {rc}. service_id = {serviceId}, shop_id = {shop.Id}, yandex_type_id = {yandexTypeId}, orders = {orders.Length}", 0);
                #endif

                return rc;
            }
            catch (Exception ex)
            {
                #if debug
                    Logger.WriteToLog(403, $"GeoData.Select exit rc = {rc}. service_id = {serviceId}, shop_id = {shop.Id}, yandex_type_id = {yandexTypeId}, orders = {orders.Length} Exception = {ex.Message}", 2);
                #endif

                return rc;
            }
        }

        /// <summary>
        /// Выбор требуемых гео-данных
        /// </summary>
        /// <param name="serverNamme">Имя сервера</param>
        /// <param name="dbName">Имя БД</param>
        /// <param name="serviceId">ID LogisticsService</param>
        /// <param name="yandexTypeId">ID способа передвижения Yandex</param>
        /// <param name="shop">Магазин</param>
        /// <param name="orders">Заказы</param>
        /// <param name="geoData">Гео-данные
        /// (Индексы точек: i - orders[i]; i = orders.Length - shop)
        /// </param>
        /// <returns>0 - гео-данные выбраны; гео-данные не выбраны</returns>
        public static int Select(string serverName, string dbName, int serviceId, int yandexTypeId, Shop shop, Order[] orders, out Point[,] geoData)
        {
            // 1. Инициализация
            int rc = 1;
            geoData = null;
            //{

                try
                {
                // 2. Проверяем исходные данные
                rc = 2;
                if (string.IsNullOrEmpty(serverName))
                    return rc;
                if (string.IsNullOrEmpty(dbName))
                    return rc;
                if (shop == null || shop.Latitude == 0 || shop.Longitude == 0)
                    return rc;
                if (orders == null || orders.Length <= 0)
                    return rc;

                #if debug
                    Logger.WriteToLog(401, $"GeoData.Select enter. service_id = {serviceId}, shop_id = {shop.Id}, yandex_type_id = {yandexTypeId}, orders = {orders.Length}", 0);
                #endif

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
                //using (SqlCommand cmd = new SqlCommand("dbo.lsvSelectGeoDataFromCache @service_id, @yandex_type_id, @points", connection))
                using (SqlConnection connection = new SqlConnection($"Server={serverName};Database={dbName};Integrated Security=true"))
                {
                    connection.Open();
                    using (SqlCommand cmd = new SqlCommand("dbo.lsvSelectGeoDataFromCacheEx", connection))
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
                        parameter = cmd.Parameters.AddWithValue("@points", table);
                        parameter.Direction = ParameterDirection.Input;
                        parameter.SqlDbType = SqlDbType.Structured;
                        parameter.TypeName = "dbo.lsvGeoPointTable";

                        // @geo_data
                        var outputParameter = cmd.Parameters.Add("@geo_data", SqlDbType.VarChar, int.MaxValue);
                        outputParameter.Direction = ParameterDirection.Output;

                        // return code
                        var returnParameter = cmd.Parameters.Add("@ReturnCode", SqlDbType.Int);
                        returnParameter.Direction = ParameterDirection.ReturnValue;
                        returnParameter.Value = -1;

                        // 6. Исполняем команду
                        rc = 6;
                        cmd.ExecuteNonQuery();
                    #if debug
                        Logger.WriteToLog(405, $"GeoData.Select cmd.ExecuteNonQuery(). rc = {returnParameter.Value}, geo_data = {outputParameter.Value}", 0);
                    #endif
                        if ((int)returnParameter.Value != 0)
                            return rc;

                        // 7. Извлекаем результат
                        rc = 7;
                        XmlSerializer serializer = new XmlSerializer(typeof(geo_data));
                        using (StringReader sr = new StringReader(outputParameter.Value as string))
                        {
                            geo_data data = (geo_data)serializer.Deserialize(sr);
                            if (data != null)
                            {
                                if (data.Count != table.Rows.Count * table.Rows.Count)
                                {
                                #if debug
                                    Logger.WriteToLog(402, $"GeoData.Select exit rc = {rc}. service_id = {serviceId}, shop_id = {shop.Id}, yandex_type_id = {yandexTypeId}, orders = {orders.Length}", 1);
                                #endif

                                    return rc;
                                }

                                geoData = data.GetGeoData(table.Rows.Count);
                            }
                            else
                            {
                            #if debug
                                Logger.WriteToLog(402, $"GeoData.Select exit rc = {rc}. service_id = {serviceId}, shop_id = {shop.Id}, yandex_type_id = {yandexTypeId}, orders = {orders.Length}", 2);
                            #endif
                            }
                        }
                    }

                    connection.Close();
                }
                // 8. Выход - Ok
                rc = 0;

                #if debug
                    Logger.WriteToLog(402, $"GeoData.Select exit rc = {rc}. service_id = {serviceId}, shop_id = {shop.Id}, yandex_type_id = {yandexTypeId}, orders = {orders.Length}", 0);
                #endif

                return rc;
            }
            catch (Exception ex)
            {
                #if debug
                    Logger.WriteToLog(403, $"GeoData.Select exit rc = {rc}. service_id = {serviceId}, shop_id = {shop.Id}, yandex_type_id = {yandexTypeId}, orders = {orders.Length} Exception = {ex.Message}", 2);
                #endif

                return rc;
            }
        }
    }
}
