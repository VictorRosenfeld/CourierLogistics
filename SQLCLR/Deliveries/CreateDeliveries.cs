using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
using SQLCLR.Orders;
using SQLCLR.Shops;

public partial class StoredProcedures
{

    /// <summary>
    /// Выбор данных магазина
    /// </summary>
    private const string SELECT_SHOPS = "SELECT * FROM lsvShops WHERE shpUpdated = 1";

    /// <summary>
    /// Выбор заказов магазинов
    /// </summary>
    private const string SELECT_ORDERS =
        "SELECT lsvOrders.* " +
        "FROM   lsvOrders INNER JOIN " +
        "          lsvShops ON lsvOrders.ordShopID = lsvShops.shpShopID " +
        "WHERE  (lsvShops.shpUpdated = 1) AND " +
        "       (lsvOrders.ordStatusID = 1 OR lsvOrders.ordStatusID = 2);";

    /// <summary>
    /// Построение всех возможных отгрузок для всех
    /// отмеченных магазинов
    /// </summary>
    /// <param name="service_id">ID LogisticsService</param>
    /// <returns>0 - отгрузки построены; иначе отгрузки не построены</returns>
    [SqlProcedure]
    public static SqlInt32 CreateDeliveries(SqlInt32 service_id)
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

                // 3.2 Выбираем магазины для пересчета отгрузок
                Shop[] shops;
                rc = 32;
                rc1 = SelectShops(connection, out shops);
                if (rc1 != 0)
                    return rc = 1000 * rc + rc1;

                // 3.3 Создаём объекты - заказы


            }

            return rc;
        }
        catch
        {
            return rc;
        }

    }

    /// <summary>
    /// Выборка магазинов, для которых нужно
    /// построить все отгрузки
    /// </summary>
    /// <param name="connection">Открытое соединение</param>
    /// <param name="shops">Выбранные магазины или null</param>
    /// <returns>0 - магазины выбраны; иначе - магазины не выбраны</returns>
    private static int SelectShops(SqlConnection connection, out Shop[] shops)
    {
        // 1. Инициализация
        int rc = 1;
        shops = null;

        try
        {
            // 2. Проверяем исходные данные
            rc = 2;
            if (connection == null || connection.State != ConnectionState.Open)
                return rc;

            // 3. Выбираем магазины
            rc = 3;
            Shop[] allShops = new Shop[1000];
            int shopCount = 0;

            using (SqlCommand cmd = new SqlCommand(SELECT_SHOPS, connection))
            {

                // 3.2 Читаем записи и строим результат
                rc = 32;
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    int iShopId = reader.GetOrdinal("shpShopID");
                    int iLatitude = reader.GetOrdinal("shpLatitude");
                    int iLongitude = reader.GetOrdinal("shpLongitude");
                    int iWorkStart = reader.GetOrdinal("shpWorkStart");
                    int iWorkEnd = reader.GetOrdinal("shpWorkEnd");

                    while (reader.Read())
                    {
                        Shop shop = new Shop(reader.GetInt32(iShopId));
                        shop.Latitude = reader.GetDouble(iLatitude);
                        shop.Longitude = reader.GetDouble(iLongitude);
                        shop.WorkStart = reader.GetTimeSpan(iWorkStart);
                        shop.WorkEnd = reader.GetTimeSpan(iWorkEnd);

                        if (shopCount >= allShops.Length)
                        {
                            Array.Resize(ref allShops, (int)(1.25 * allShops.Length));
                        }

                        allShops[shopCount++] = shop;
                    }
                }
            }

            if (shopCount < allShops.Length)
            {
                Array.Resize(ref allShops, shopCount);
            }

            shops = allShops;

            // 4. Выход - Ok
            rc = 0;
            return rc;
        }
        catch
        {
            return rc;
        }
    }

    /// <summary>
    /// Выбор заказов, для которых нужно построить отгрузки
    /// </summary>
    /// <param name="connection">Открытое соединение</param>
    /// <param name="orders">Выбранные заказы или null</param>
    /// <returns>0 - заказы выбраны; иначе - заказы не выбраны</returns>
    private static int SelectOrders(SqlConnection connection, out Order[] orders)
    {
        // 1. Инициализация
        int rc = 1;
        orders = null;

        try
        {
            // 2. Проверяем исходные данные
            rc = 2;
            if (connection == null || connection.State != ConnectionState.Open)
                return rc;

            // 3. Выбираем заказы
            rc = 3;
            Order[] allOrders = new Order[1000];
            int count = 0;

            using (SqlCommand cmd = new SqlCommand(SELECT_ORDERS, connection))
            {

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    int iOrderID = reader.GetOrdinal("ordOrderID");
                    int iStatusID = reader.GetOrdinal("ordStatusID");
                    int iShopID = reader.GetOrdinal("ordShopID");
                    int iPriority = reader.GetOrdinal("ordPriority");
                    int iWeight = reader.GetOrdinal("ordWeight");
                    int iLatitude = reader.GetOrdinal("ordLatitude");
                    int iLongitude = reader.GetOrdinal("ordLongitude");
                    int iAssembledDate = reader.GetOrdinal("ordAssembledDate");
                    int iReceiptedDate = reader.GetOrdinal("ordReceiptedDate");
                    int iDeliveryTimeFrom = reader.GetOrdinal("ordDeliveryTimeFrom");
                    int iDeliveryTimeTo = reader.GetOrdinal("ordDeliveryTimeTo");
                    int iCompleted = reader.GetOrdinal("ordCompleted");
                    int iTimeCheckDisabled = reader.GetOrdinal("ordTimeCheckDisabled");

                    while (reader.Read())
                    {
                        Order order = new Order(reader.GetInt32(iOrderID));
                        order.Status = (OrderStatus)reader.GetInt32(iStatusID);
                        order.ShopId = reader.GetInt32(iShopID);
                        order.Priority = reader.GetInt32(iPriority);
                        order.Weight = reader.GetDouble(iWeight);
                        order.Latitude = reader.GetDouble(iLatitude);
                        order.Longitude = reader.GetDouble(iLongitude);
                        order.AssembledDate = reader.GetDateTime(iAssembledDate);
                        order.ReceiptedDate = reader.GetDateTime(iReceiptedDate);
                        order.DeliveryTimeFrom = reader.GetDateTime(iDeliveryTimeFrom);
                        order.DeliveryTimeTo = reader.GetDateTime(iDeliveryTimeTo);
                        order.TimeCheckDisabled = reader.GetBoolean(iTimeCheckDisabled);

                        if (count >= allOrders.Length)
                        {
                            Array.Resize(ref allOrders, (int)(1.25 * allOrders.Length));
                        }

                        allOrders[count++] = order;
                    }


                }
            }

            if (count < allOrders.Length)
            {
                Array.Resize(ref allOrders, count);
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
