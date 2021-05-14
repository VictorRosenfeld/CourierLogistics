using Microsoft.SqlServer.Server;
using SQLCLR.Orders;
//using SQLCLR.Shop;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;

public partial class StoredProcedures
{
    #region SQL instructions

    /// <summary>
    /// Доступные курьеры и такси
    /// </summary>
    private const string SELECT_AVAILABLE_COURIERS =
        "SELECT DISTINCT lsvCourierTypes.crtVehicleID, lsvCouriers.crsCourierID " +
        "FROM lsvCourierTypes INNER JOIN " +
        "        lsvCouriers ON lsvCourierTypes.crtVehicleID = lsvCouriers.crsVehicleID INNER JOIN " +
        "           lsvOrders INNER JOIN " +
        "              lsvOrderVehicleTypes ON lsvOrders.ordOrderID = lsvOrderVehicleTypes.ovtOrderID ON lsvCourierTypes.crtVehicleID = lsvOrderVehicleTypes.ovtVehicleID " +
        "WHERE (lsvOrders.ordShopID = @shop_id) AND " +
        "      (lsvCouriers.crsStatusID = 1) AND " +
        "      (lsvCouriers.crsShopID = @shop_id OR lsvCouriers.crsShopID = 0) AND " +
        "      (lsvOrders.ordStatusID = 1 OR lsvOrders.ordStatusID = 2);";

    /// <summary>
    /// Выбор данных магазина
    /// </summary>
    private const string SELECT_SHOP = "SELECT * FROM lsvShops WHERE shpShopID = @shop_id";

    ///// <summary>
    ///// Выбор заказов магазина
    ///// </summary>
    //private const string SELECT_ORDERS =
    //    "SELECT lsvOrders.*, lsvOrderVehicleTypes.lsvOrderVehicleTypes.ovtVehicleID " +
    //    "FROM   lsvOrders INNER JOIN " +
    //    "          lsvOrderVehicleTypes ON lsvOrders.ordOrderID = lsvOrderVehicleTypes.ovtOrderID " +
    //    "WHERE  (lsvOrders.ordShopID = @shop_id) AND (lsvOrders.ordStatusID = 1 OR lsvOrders.ordStatusID = 2);";

    #endregion SQL instructions


    [SqlProcedure]
    public static SqlInt32 CreateShopDeliveries(SqlInt32 serviceId, SqlInt32 shopId)
    {
        // 1. Инициализация
        int rc = 1;
        int rc1 = 1;

        try
        {
            // 2. Проверяем исходные данные
            rc = 2;
            if (!SqlContext.IsAvailable)
                return rc;

            // 3. Открываем соединение в контексте текущей сессии
            rc = 3;
            using (SqlConnection connection = new SqlConnection("context connection=true"))
            {
                // 3.1 Открываем соединение
                rc = 31;
                connection.Open();

                // 3.2 Создаём объект - магазин
                //Shop shop;
                //rc1 = SelectShop(shopId.Value, connection, out shop);
                //if (rc1 != 0)
                //    return rc = 1000 * rc + rc1;

                // 3.3 Создаём объекты - заказы


            }

            return rc;
        }
        catch
        {
            return rc;
        }


        // Put your code here

        return 0;
    }

    ///// <summary>
    ///// Загружаем данные магазина
    ///// </summary>
    ///// <param name="connection"></param>
    ///// <param name="shop"></param>
    ///// <returns></returns>
    //private static int SelectShop(int shop_id, SqlConnection connection, out Shop shop)
    //{
    //    // 1. Инициализация
    //    int rc = 1;
    //    shop = null;

    //    try
    //    {
    //        // 2. Проверяем исходные данные
    //        rc = 2;
    //        if (connection == null || connection.State != ConnectionState.Open)
    //            return rc;

    //        // 3. Выбираем данные
    //        rc = 3;
    //        using (SqlCommand cmd = new SqlCommand(SELECT_SHOP, connection))
    //        {
    //            // 3.1 Строим команду
    //            rc = 31;
    //            SqlParameter param = new SqlParameter("@shop_id", SqlDbType.Int);
    //            param.IsNullable = false;
    //            param.Direction = ParameterDirection.Input;
    //            param.Value = shop_id;
    //            cmd.Parameters.Add(param);

    //            // 3.2 Исполняем команду и строим результат
    //            rc = 32;
    //            using (SqlDataReader reader = cmd.ExecuteReader())
    //            {
    //                if (!reader.Read())
    //                    return rc;
    //                shop = new Shop(shop_id);
    //                shop.Latitude = reader.GetDouble(reader.GetOrdinal("shpLatitude"));
    //                shop.Longitude = reader.GetDouble(reader.GetOrdinal("shpLongitude"));
    //                shop.WorkStart = reader.GetTimeSpan(reader.GetOrdinal("shpWorkStart"));
    //                shop.WorkEnd = reader.GetTimeSpan(reader.GetOrdinal("shpWorkEnd"));
    //            }
    //        }

    //        // 4. Выход - Ok
    //        rc = 0;
    //        return rc;
    //    }
    //    catch
    //    {
    //        return rc;
    //    }
    //}

    //private static int SelectOrders(int shop_id, SqlConnection connection, out Order[] orders)
    //{
    //    // 1. Инициализация
    //    int rc = 1;
    //    orders = null;

    //    try
    //    {
    //        // 2. Проверяем исходные данные
    //        rc = 2;
    //        if (connection == null || connection.State != ConnectionState.Open)
    //            return rc;

    //        // 3. Выбираем данные
    //        rc = 3;
    //        using (SqlCommand cmd = new SqlCommand(SELECT_ORDERS, connection))
    //        {
    //            // 3.1 Строим команду
    //            rc = 31;
    //            SqlParameter param = new SqlParameter("@shop_id", SqlDbType.Int);
    //            param.IsNullable = false;
    //            param.Direction = ParameterDirection.Input;
    //            param.Value = shop_id;
    //            cmd.Parameters.Add(param);

    //            // 3.2 Исполняем команду и строим результат
    //            rc = 32;
    //            Order[] readOrders = new Order[100];
    //            int count = 0;

    //            using (SqlDataReader reader = cmd.ExecuteReader())
    //            {
    //                int iOrderID = reader.GetOrdinal("ordOrderID");
    //                int iStatusID = reader.GetOrdinal("ordStatusID");
    //                int iShopID = reader.GetOrdinal("ordShopID");
    //                int iPriority = reader.GetOrdinal("ordPriority");
    //                int iWeight = reader.GetOrdinal("ordWeight");
    //                int iLatitude = reader.GetOrdinal("ordLatitude");
    //                int iLongitude = reader.GetOrdinal("ordLongitude");
    //                int iAssembledDate = reader.GetOrdinal("ordAssembledDate");
    //                int iReceiptedDate = reader.GetOrdinal("ordReceiptedDate");
    //                int iDeliveryTimeFrom = reader.GetOrdinal("ordDeliveryTimeFrom");
    //                int iDeliveryTimeTo = reader.GetOrdinal("ordDeliveryTimeTo");
    //                int iCompleted = reader.GetOrdinal("ordCompleted");
    //                int iTimeCheckDisabled = reader.GetOrdinal("ordTimeCheckDisabled");

    //                while (reader.Read())
    //                {
    //                    Order order = new Order(reader.GetInt32(iOrderID));
    //                    //shop = new Shop(shop_id);
    //                    //shop.Latitude = reader.GetDouble(reader.GetOrdinal("shpLatitude"));
    //                    //shop.Longitude = reader.GetDouble(reader.GetOrdinal("shpLongitude"));
    //                    //shop.WorkStart = reader.GetTimeSpan(reader.GetOrdinal("shpWorkStart"));
    //                    //shop.WorkEnd = reader.GetTimeSpan(reader.GetOrdinal("shpWorkEnd"));
    //                }
    //            }
    //        }

    //        // 4. Выход - Ok
    //        rc = 0;
    //        return rc;
    //    }
    //    catch
    //    {
    //        return rc;
    //    }


    //}
}
