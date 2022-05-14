
//namespace CreateDeliveriesApplication.Deliveries
//{
//    using CreateDeliveriesApplication.Log;
//    using CreateDeliveriesApplication.Orders;
//    using CreateDeliveriesApplication.Shops;
//    using System;
//    using System.Data;
//    using System.Data.SqlClient;
//    using System.IO;
//    using System.Xml.Serialization;

//    /// <summary>
//    /// Класс для работы с гео-данными Yandex
//    /// </summary>
//    public class GeoData
//    {
//        /// <summary>
//        /// Проверка наличия и подкачка
//        /// в GeoCache требуемых данных
//        /// </summary>
//        /// <param name="serviceId">ID LogisticsService</param>
//        /// <param name="yandexTypeId">ID способа передвижения Yandex</param>
//        /// <param name="shop">Магазин</param>
//        /// <param name="orders">Заказы</param>
//        /// <returns></returns>
//        public static int PutData(int serviceId, int yandexTypeId, Shop shop, Order[] orders)
//        {
//            // 1. Инициализация
//            int rc = 1;

//            try
//            {
//                // 2. Проверяем исходные данные
//                rc = 2;
//                if (shop == null || shop.Latitude == 0 || shop.Longitude == 0)
//                    return rc;
//                if (orders == null || orders.Length <= 0)
//                    return rc;

//                // 3. Открываем соединение
//                rc = 3;
//                using (SqlConnection connection = new SqlConnection("context connection=true"))
//                //using (Task openTask = connection.OpenAsync())
//                {
//                    connection.Open();
//                    // 4. Создаём таблицу с исходными данными
//                    rc = 4;
//                    DataTable table = new DataTable();
//                    table.Columns.Add("latitude", typeof(double));
//                    table.Columns.Add("longitude", typeof(double));

//                    for (int i = 0; i < orders.Length; i++)
//                    {
//                        table.Rows.Add(orders[i].Latitude, orders[i].Longitude);
//                    }

//                    table.Rows.Add(shop.Latitude, shop.Longitude);

//                    // 5. Строим команду для вызова процедуры lsvH4geo_putEx
//                    rc = 5;
//                    using (SqlCommand cmd = new SqlCommand("dbo.lsvH4geo_putEx", connection))
//                    {
//                        cmd.CommandType = CommandType.StoredProcedure;

//                        // @service_id
//                        var parameter = cmd.Parameters.Add("@service_id", SqlDbType.Int);
//                        parameter.Direction = ParameterDirection.Input;
//                        parameter.Value = serviceId;

//                        // @yandex_type_id
//                        parameter = cmd.Parameters.Add("@yandex_type_id", SqlDbType.Int);
//                        parameter.Direction = ParameterDirection.Input;
//                        parameter.Value = yandexTypeId;

//                        // @points
//                        parameter = cmd.Parameters.AddWithValue("@yandex_type_id", table);
//                        parameter.Direction = ParameterDirection.Input;
//                        parameter.SqlDbType = SqlDbType.Structured;
//                        parameter.TypeName = "dbo.lsvGeoPointTable";

//                        // return code
//                        var returnParameter = cmd.Parameters.Add("@ReturnCode", SqlDbType.Int);
//                        returnParameter.Direction = ParameterDirection.ReturnValue;

//                        // 6. Исполняем команду
//                        rc = 6;
//                        //openTask.Wait();
//                        cmd.ExecuteNonQuery();
//                        var retCode = returnParameter.Value;
//                        if (!(retCode is int))
//                            return rc;
//                        int rc1 = (int)retCode;
//                        if (rc1 != 0)
//                            return rc = 1000 * rc + rc1;
//                    }
//                }

//                // 7. Выход - Ok
//                rc = 0;
//                return rc;
//            }
//            catch
//            {
//                return rc;
//            }
//        }

//        /// <summary>
//        /// Выбор требуемых гео-данных
//        /// </summary>
//        /// <param name="connection">Открытое соединение</param>
//        /// <param name="serviceId">ID LogisticsService</param>
//        /// <param name="yandexTypeId">ID способа передвижения Yandex</param>
//        /// <param name="shop">Магазин</param>
//        /// <param name="orders">Заказы</param>
//        /// <param name="geoData">Гео-данные
//        /// (Индексы точек: i - orders[i]; i = orders.Length - shop)
//        /// </param>
//        /// <returns>0 - гео-данные выбраны; гео-данные не выбраны</returns>
//        public static int Select(SqlConnection connection, int serviceId, int yandexTypeId, Shop shop, Order[] orders, out Point[,] geoData)
//        {
//            // 1. Инициализация
//            int rc = 1;
//            geoData = null;

//            try
//            {
//                // 2. Проверяем исходные данные
//                rc = 2;
//                if (connection == null || connection.State != ConnectionState.Open)
//                    return rc;
//                if (shop == null || shop.Latitude == 0 || shop.Longitude == 0)
//                    return rc;
//                if (orders == null || orders.Length <= 0)
//                    return rc;

//                #if debug
//                    Logger.WriteToLog(401, $"GeoData.Select enter. service_id = {serviceId}, shop_id = {shop.Id}, yandex_type_id = {yandexTypeId}, orders = {orders.Length}", 0);
//                #endif

//                // 4. Создаём таблицу с исходными данными
//                rc = 4;
//                DataTable table = new DataTable();
//                table.Columns.Add("latitude", typeof(double));
//                table.Columns.Add("longitude", typeof(double));

//                for (int i = 0; i < orders.Length; i++)
//                {
//                    table.Rows.Add(orders[i].Latitude, orders[i].Longitude);
//                }

//                table.Rows.Add(shop.Latitude, shop.Longitude);

//                // 5. Строим команду для вызова процедуры lsvH4geo_putEx
//                rc = 5;
//                //using (SqlCommand cmd = new SqlCommand("dbo.lsvSelectGeoDataFromCache @service_id, @yandex_type_id, @points", connection))
//                using (SqlCommand cmd = new SqlCommand("dbo.lsvSelectGeoDataFromCacheEx", connection))
//                {
//                    cmd.CommandType = CommandType.StoredProcedure;

//                    // @service_id
//                    var parameter = cmd.Parameters.Add("@service_id", SqlDbType.Int);
//                    parameter.Direction = ParameterDirection.Input;
//                    parameter.Value = serviceId;

//                    // @yandex_type_id
//                    parameter = cmd.Parameters.Add("@yandex_type_id", SqlDbType.Int);
//                    parameter.Direction = ParameterDirection.Input;
//                    parameter.Value = yandexTypeId;

//                    // @points
//                    parameter = cmd.Parameters.AddWithValue("@points", table);
//                    parameter.Direction = ParameterDirection.Input;
//                    parameter.SqlDbType = SqlDbType.Structured;
//                    parameter.TypeName = "dbo.lsvGeoPointTable";

//                    // @geo_data
//                    var outputParameter = cmd.Parameters.Add("@geo_data", SqlDbType.VarChar, int.MaxValue);
//                    outputParameter.Direction = ParameterDirection.Output;

//                    // return code
//                    var returnParameter = cmd.Parameters.Add("@ReturnCode", SqlDbType.Int);
//                    returnParameter.Direction = ParameterDirection.ReturnValue;
//                    returnParameter.Value = -1;

//                    // 6. Исполняем команду
//                    rc = 6;
//                    cmd.ExecuteNonQuery();
//                    #if debug
//                        Logger.WriteToLog(405, $"GeoData.Select cmd.ExecuteNonQuery(). rc = {returnParameter.Value}, geo_data = {outputParameter.Value}", 0);
//                    #endif
//                    if ((int)returnParameter.Value != 0)
//                        return rc;

//                    // 7. Извлекаем результат
//                    rc = 7;
//                    XmlSerializer serializer = new XmlSerializer(typeof(geo_data));
//                    using (StringReader sr = new StringReader(outputParameter.Value as string))
//                    {
//                        geo_data data = (geo_data)serializer.Deserialize(sr);
//                        if (data != null)
//                        {
//                            if (data.Count != table.Rows.Count * table.Rows.Count)
//                            {
//                                #if debug
//                                    Logger.WriteToLog(402, $"GeoData.Select exit rc = {rc}. service_id = {serviceId}, shop_id = {shop.Id}, yandex_type_id = {yandexTypeId}, orders = {orders.Length}", 1);
//                                #endif

//                                return rc;
//                            }

//                            geoData = data.GetGeoData(table.Rows.Count);
//                        }
//                        else
//                        {
//                            #if debug
//                                Logger.WriteToLog(402, $"GeoData.Select exit rc = {rc}. service_id = {serviceId}, shop_id = {shop.Id}, yandex_type_id = {yandexTypeId}, orders = {orders.Length}", 2);
//                            #endif
//                        }
//                    }

//                }
//                // 8. Выход - Ok
//                rc = 0;

//                #if debug
//                    Logger.WriteToLog(402, $"GeoData.Select exit rc = {rc}. service_id = {serviceId}, shop_id = {shop.Id}, yandex_type_id = {yandexTypeId}, orders = {orders.Length}", 0);
//                #endif

//                return rc;
//            }
//            catch (Exception ex)
//            {
//                #if debug
//                    Logger.WriteToLog(403, $"GeoData.Select exit rc = {rc}. service_id = {serviceId}, shop_id = {shop.Id}, yandex_type_id = {yandexTypeId}, orders = {orders.Length} Exception = {ex.Message}", 2);
//                #endif

//                return rc;
//            }
//        }

//        /// <summary>
//        /// Выбор требуемых гео-данных
//        /// </summary>
//        /// <param name="serverNamme">Имя сервера</param>
//        /// <param name="dbName">Имя БД</param>
//        /// <param name="serviceId">ID LogisticsService</param>
//        /// <param name="yandexTypeId">ID способа передвижения Yandex</param>
//        /// <param name="shop">Магазин</param>
//        /// <param name="orders">Заказы</param>
//        /// <param name="geoData">Гео-данные
//        /// (Индексы точек: i - orders[i]; i = orders.Length - shop)
//        /// </param>
//        /// <returns>0 - гео-данные выбраны; гео-данные не выбраны</returns>
//        public static int Select(string serverName, string dbName, int serviceId, int yandexTypeId, Shop shop, Order[] orders, out Point[,] geoData)
//        {
//            // 1. Инициализация
//            int rc = 1;
//            geoData = null;
//            //{

//                try
//                {
//                // 2. Проверяем исходные данные
//                rc = 2;
//                if (string.IsNullOrEmpty(serverName))
//                    return rc;
//                if (string.IsNullOrEmpty(dbName))
//                    return rc;
//                if (shop == null || shop.Latitude == 0 || shop.Longitude == 0)
//                    return rc;
//                if (orders == null || orders.Length <= 0)
//                    return rc;

//                #if debug
//                    Logger.WriteToLog(401, $"GeoData.Select enter. service_id = {serviceId}, shop_id = {shop.Id}, yandex_type_id = {yandexTypeId}, orders = {orders.Length}", 0);
//                #endif

//                // 4. Создаём таблицу с исходными данными
//                rc = 4;
//                DataTable table = new DataTable();
//                table.Columns.Add("latitude", typeof(double));
//                table.Columns.Add("longitude", typeof(double));

//                for (int i = 0; i < orders.Length; i++)
//                {
//                    table.Rows.Add(orders[i].Latitude, orders[i].Longitude);
//                }

//                table.Rows.Add(shop.Latitude, shop.Longitude);

//                // 5. Строим команду для вызова процедуры lsvH4geo_putEx
//                rc = 5;
//                //using (SqlCommand cmd = new SqlCommand("dbo.lsvSelectGeoDataFromCache @service_id, @yandex_type_id, @points", connection))
//                using (SqlConnection connection = new SqlConnection($"Server={serverName};Database={dbName};Integrated Security=true"))
//                {
//                    connection.Open();
//                    using (SqlCommand cmd = new SqlCommand("dbo.lsvSelectGeoDataFromCacheEx", connection))
//                    {
//                        cmd.CommandType = CommandType.StoredProcedure;

//                        // @service_id
//                        var parameter = cmd.Parameters.Add("@service_id", SqlDbType.Int);
//                        parameter.Direction = ParameterDirection.Input;
//                        parameter.Value = serviceId;

//                        // @yandex_type_id
//                        parameter = cmd.Parameters.Add("@yandex_type_id", SqlDbType.Int);
//                        parameter.Direction = ParameterDirection.Input;
//                        parameter.Value = yandexTypeId;

//                        // @points
//                        parameter = cmd.Parameters.AddWithValue("@points", table);
//                        parameter.Direction = ParameterDirection.Input;
//                        parameter.SqlDbType = SqlDbType.Structured;
//                        parameter.TypeName = "dbo.lsvGeoPointTable";

//                        // @geo_data
//                        var outputParameter = cmd.Parameters.Add("@geo_data", SqlDbType.VarChar, int.MaxValue);
//                        outputParameter.Direction = ParameterDirection.Output;

//                        // return code
//                        var returnParameter = cmd.Parameters.Add("@ReturnCode", SqlDbType.Int);
//                        returnParameter.Direction = ParameterDirection.ReturnValue;
//                        returnParameter.Value = -1;

//                        // 6. Исполняем команду
//                        rc = 6;
//                        cmd.ExecuteNonQuery();
//                    #if debug
//                        Logger.WriteToLog(405, $"GeoData.Select cmd.ExecuteNonQuery(). rc = {returnParameter.Value}, geo_data = {outputParameter.Value}", 0);
//                    #endif
//                        if ((int)returnParameter.Value != 0)
//                            return rc;

//                        // 7. Извлекаем результат
//                        rc = 7;
//                        XmlSerializer serializer = new XmlSerializer(typeof(geo_data));
//                        using (StringReader sr = new StringReader(outputParameter.Value as string))
//                        {
//                            geo_data data = (geo_data)serializer.Deserialize(sr);
//                            if (data != null)
//                            {
//                                if (data.Count != table.Rows.Count * table.Rows.Count)
//                                {
//                                #if debug
//                                    Logger.WriteToLog(402, $"GeoData.Select exit rc = {rc}. service_id = {serviceId}, shop_id = {shop.Id}, yandex_type_id = {yandexTypeId}, orders = {orders.Length}", 1);
//                                #endif

//                                    return rc;
//                                }

//                                geoData = data.GetGeoData(table.Rows.Count);
//                            }
//                            else
//                            {
//                            #if debug
//                                Logger.WriteToLog(402, $"GeoData.Select exit rc = {rc}. service_id = {serviceId}, shop_id = {shop.Id}, yandex_type_id = {yandexTypeId}, orders = {orders.Length}", 2);
//                            #endif
//                            }
//                        }
//                    }

//                    connection.Close();
//                }
//                // 8. Выход - Ok
//                rc = 0;

//                #if debug
//                    Logger.WriteToLog(402, $"GeoData.Select exit rc = {rc}. service_id = {serviceId}, shop_id = {shop.Id}, yandex_type_id = {yandexTypeId}, orders = {orders.Length}", 0);
//                #endif

//                return rc;
//            }
//            catch (Exception ex)
//            {
//                #if debug
//                    Logger.WriteToLog(403, $"GeoData.Select exit rc = {rc}. service_id = {serviceId}, shop_id = {shop.Id}, yandex_type_id = {yandexTypeId}, orders = {orders.Length} Exception = {ex.Message}", 2);
//                #endif

//                return rc;
//            }
//        }

//        /// <summary>
//        /// Выбор требуемых гео-данных
//        /// </summary>
//        /// <param name="connection">Открытое соединение</param>
//        /// <param name="yandexTypeId">ID способа передвижения Yandex</param>
//        /// <param name="lat1">Широта 1</param>
//        /// <param name="lon1">Долгота 1</param>
//        /// <param name="lat2">Широта 2</param>
//        /// <param name="lon2">Долгота 2</param>
//        /// <param name="distance1">Расстояние 1 - 2</param>
//        /// <param name="duration1">Время 1 - 2</param>
//        /// <param name="distance2">Расстояние 2 - 1</param>
//        /// <param name="duration2">Время 2 - 1</param>
//        /// (Индексы точек: i - orders[i]; i = orders.Length - shop)
//        /// </param>
//        /// <returns>0 - гео-данные выбраны; гео-данные не выбраны</returns>
//        public static int Select(SqlConnection connection, 
//                                    int yandexTypeId, 
//                                    double lat1, double lon1,
//                                    double lat2, double lon2,
//                                    out int distance1, out int duration1,
//                                    out int distance2, out int duration2
//                                    )
//        {
//            // 1. Инициализация
//            int rc = 1;
//            distance1 = 0;
//            duration1 = 0;
//            distance2 = 0;
//            duration2 = 0;

//            try
//            {
//                // 2. Проверяем исходные данные
//                rc = 2;
//                if (connection == null || connection.State != ConnectionState.Open)
//                    return rc;

//            #if debug
//                Logger.WriteToLog(401, $"GeoData.Select enter. yandex_type_id ={yandexTypeId}, Point1 = ({lat1}, {lon1}), Point2 = ({lat2}, {lon2})", 0);
//            #endif

//                // 3. Строим команду для вызова процедуры lsvH4geo_putEx
//                rc = 3;
//                //using (SqlCommand cmd = new SqlCommand("dbo.lsvSelectGeoDataFromCache @service_id, @yandex_type_id, @points", connection))
//                using (SqlCommand cmd = new SqlCommand("dbo.lsvH4geo_get2", connection))
//                {
//                    cmd.CommandType = CommandType.StoredProcedure;

//                    // @yandexType
//                    var parameter = cmd.Parameters.Add("@yandexType", SqlDbType.Int);
//                    parameter.Direction = ParameterDirection.Input;
//                    parameter.Value = yandexTypeId;

//                    // @latitude1
//                    parameter = cmd.Parameters.Add("@latitude1", SqlDbType.Float);
//                    parameter.Direction = ParameterDirection.Input;
//                    parameter.Value = lat1;

//                    // @longitude1
//                    parameter = cmd.Parameters.Add("@longitude1", SqlDbType.Float);
//                    parameter.Direction = ParameterDirection.Input;
//                    parameter.Value = lon1;

//                    // @latitude2
//                    parameter = cmd.Parameters.Add("@latitude2", SqlDbType.Float);
//                    parameter.Direction = ParameterDirection.Input;
//                    parameter.Value = lat2;

//                    // @longitude2
//                    parameter = cmd.Parameters.Add("@longitude2", SqlDbType.Float);
//                    parameter.Direction = ParameterDirection.Input;
//                    parameter.Value = lon2;

//                    // @distance1
//                    var dist1 = cmd.Parameters.Add("@distance1", SqlDbType.Int);
//                    dist1.Direction = ParameterDirection.Output;

//                    // @duration1
//                    var dur1 = cmd.Parameters.Add("@duration1", SqlDbType.Int);
//                    dur1.Direction = ParameterDirection.Output;

//                    // @distance2
//                    var dist2 = cmd.Parameters.Add("@distance2", SqlDbType.Int);
//                    dist2.Direction = ParameterDirection.Output;

//                    // @duration2
//                    var dur2 = cmd.Parameters.Add("@duration2", SqlDbType.Int);
//                    dur2.Direction = ParameterDirection.Output;

//                    // return code
//                    var returnParameter = cmd.Parameters.Add("@ReturnCode", SqlDbType.Int);
//                    returnParameter.Direction = ParameterDirection.ReturnValue;
//                    returnParameter.Value = -1;

//                    // 4. Исполняем команду
//                    rc = 4;
//                    cmd.ExecuteNonQuery();
//                #if debug
//                    Logger.WriteToLog(405, $"GeoData.Select cmd.ExecuteNonQuery(). rc = {returnParameter.Value}, 1 -> 2 = ({dist1.Value}, {dur1.Value}),  2 -> 1 = ({dist2.Value}, {dur2.Value})", 0);
//                #endif
//                    if ((int)returnParameter.Value != 0)
//                        return rc;

//                    // 5. Извлекаем результат
//                    rc = 5;
//                    distance1 = (int) dist1.Value;
//                    duration1 = (int) dur1.Value;
//                    distance2 = (int) dist2.Value;
//                    duration2 = (int) dur2.Value;
//                }
//                // 8. Выход - Ok
//                rc = 0;

//            #if debug
//                Logger.WriteToLog(402, $"GeoData.Select exit rc = {rc}. yandex_type_id = {yandexTypeId}, Point1 = ({lat1}, {lon1}), Point2 = ({lat2}, {lon2})", 0);
//            #endif

//                return rc;
//            }
//            catch (Exception ex)
//            {
//            #if debug
//                Logger.WriteToLog(403, $"GeoData.Select exit rc = {rc}. yandex_type_id = {yandexTypeId}, Point1 = ({lat1}, {lon1}), Point2 = ({lat2}, {lon2}). Exception = {ex.Message}", 2);
//            #endif

//                return rc;
//            }
//        }
//    }
//}
