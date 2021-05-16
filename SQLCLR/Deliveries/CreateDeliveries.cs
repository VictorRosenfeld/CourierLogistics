using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.SqlServer.Server;
using SQLCLR.AverageDeliveryCost;
using SQLCLR.Couriers;
using SQLCLR.Orders;
using SQLCLR.Shops;

public partial class StoredProcedures
{
    #region SQL instructions

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
        " WHERE  (lsvShops.shpUpdated = 1) AND " +
        "       (lsvOrders.ordStatusID = 1 OR lsvOrders.ordStatusID = 2);";

    /// <summary>
    /// Выбор способов доставки заказов
    /// </summary>
    private const string SELECT_ORDER_VEHICLE_TYPES =
        "SELECT lsvOrders.ordOrderID, lsvOrderVehicleTypes.ovtVehicleID " +
        "FROM   lsvOrders INNER JOIN " +
        "          lsvOrderVehicleTypes ON lsvOrders.ordOrderID = lsvOrderVehicleTypes.ovtOrderID INNER JOIN " +
        "             lsvShops ON lsvOrders.ordShopID = lsvShops.shpShopID " +
        " WHERE  (lsvShops.shpUpdated = 1) AND " +
        "       (lsvOrders.ordStatusID = 1 OR lsvOrders.ordStatusID = 2) " +
        "ORDER BY lsvOrders.ordOrderID;";

    /// <summary>
    /// Выбор параметров способов доставки
    /// </summary>
    private const string SELECT_COURIER_TYPES = "SELECT * FROM lsvCourierTypes WHERE crtVehicleID IN ({0});";

    /// <summary>
    /// Выбор курьеров, доступных для доставки заказов
    /// </summary>
    private const string SELECT_COURIERS =
        "SELECT lsvCouriers.* " +
        "FROM lsvCouriers LEFT JOIN " +
        "        lsvShops ON lsvCouriers.crsShopID = lsvShops.shpShopID " +
        " WHERE  (lsvCouriers.crsStatusID = 1) AND " +
        "        (lsvCouriers.crsShopID = 0 OR lsvShops.shpUpdated = 1);";

    private const string SELECT_AVERAGE_COST_THRESHOLDS = "SELECT * FROM lsvAverageDeliveryCost";

    #endregion SQL instructions

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
            //    и загружаем все необходимые данные
            rc = 3;
            Shop[] shops = null;
            Order[] orders = null;
            int[] requiredVehicleTypes = null;
            CourierTypeRecord[] courierTypeRecords = null;
            CourierRecord[] courierRecords = null;
            AverageDeliveryCostRecord[] thresholdRecords;

            using (SqlConnection connection = new SqlConnection("context connection=true"))
            {
                // 3.1 Открываем соединение
                rc = 31;
                connection.Open();

                // 3.2 Выбираем магазины для пересчета отгрузок
                rc = 32;
                rc1 = SelectShops(connection, out shops);
                if (rc1 != 0)
                    return rc = 1000 * rc + rc1;
                if (shops == null || shops.Length <= 0)
                    return rc = 0;

                // 3.3 Выбираем заказы, для которых нужно построить отгрузки
                rc = 33;
                rc1 = SelectOrders(connection, out orders);
                if (rc1 != 0)
                    return rc = 1000 * rc + rc1;
                if (orders == null || orders.Length <= 0)
                    return rc = 0;

                // 3.4 Выбираем способы доставки заказов,
                //     которые могут быть использованы
                rc = 34;
                requiredVehicleTypes = AllOrders.GetOrderVehicleTypes(orders);
                if (requiredVehicleTypes == null || requiredVehicleTypes.Length <= 0)
                    return rc;

                // 3.5 Загружаем параметры способов отгрузки
                rc = 35;
                rc1 = SelectCourierTypes(requiredVehicleTypes, connection, out courierTypeRecords);
                if (rc1 != 0)
                    return rc = 1000 * rc + rc1;
                if (courierTypeRecords == null || courierTypeRecords.Length <= 0)
                    return rc;

                // 3.6 Загружаем информацию о курьерах
                rc = 36;
                rc1 = SelectCouriers(connection, out courierRecords);
                if (rc1 != 0)
                    return rc = 1000 * rc + rc1;
                if (courierRecords == null || courierRecords.Length <= 0)
                    return rc;

                // 3.7 Загружаем пороги для среднего времени доставки заказов
                rc = 37;
                rc1 = SelectThresholds(connection, out thresholdRecords);
                if (rc1 != 0)
                    return rc = 1000 * rc + rc1;
                if (thresholdRecords == null || thresholdRecords.Length <= 0)
                    return rc;
            }

            // 4. Создаём объект c курьерами
            rc = 4;
            AllCouriers allCouriers = new AllCouriers();
            rc1 = allCouriers.Create(courierTypeRecords, courierRecords);
            if (rc1 != 0)
                return rc = 1000 * rc + rc1;

            // 5. Создаём объект c заказами
            rc = 5;
            AllOrders allOrders = new AllOrders();
            rc1 = allOrders.Create(orders);
            if (rc1 != 0)
                return rc = 1000 * rc + rc1;

            // 6. Создаём объект c порогами средней стоимости доставки
            rc = 6;
            AverageCostThresholds thresholds = new AverageCostThresholds();
            rc1 = thresholds.Create(thresholdRecords);
            if (rc1 != 0)
                return rc = 1000 * rc + rc1;


            // WE ARE READY TO BUILD ALL DELIVERIES NOW !


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
            //int currentOrderId = int.MinValue;
            //int[] vehicleTypes = new int[1024];
            //int vehicleTypeCount = 0;
            //Order order;

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

            // 4. Строим индекс заказов
            rc = 4;
            int[] orderId = new int[count];

            for (int i = 0; i < count; i++)
                orderId[i] = allOrders[i].Id;

            Array.Sort(orderId, allOrders);


            // 5. Выбираем способы доставки заказов
            rc = 5;
            int currentOrderId = int.MinValue;
            int[] vehicleTypes = new int[1024];
            int vehicleTypeCount = 0;

            using (SqlCommand cmd = new SqlCommand(SELECT_ORDER_VEHICLE_TYPES, connection))
            {

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    int iOrderID = reader.GetOrdinal("ordOrderID");
                    int iVehicleID = reader.GetOrdinal("ovtVehicleID");

                    while (reader.Read())
                    {
                        int id = reader.GetInt32(iOrderID);
                        if (id != currentOrderId)
                        {
                            if (vehicleTypeCount > 0)
                            {
                                int index = Array.BinarySearch(orderId, currentOrderId);
                                if (index >= 0)
                                {
                                    int[] orderVehicleTypes = new int[vehicleTypeCount];
                                    Array.Copy(vehicleTypes, 0, orderVehicleTypes, 0, vehicleTypeCount);
                                    allOrders[index].VehicleTypes = orderVehicleTypes;
                                }
                            }

                            vehicleTypeCount = 0;
                            currentOrderId = id;
                        }

                        vehicleTypes[vehicleTypeCount++] = reader.GetInt32(iVehicleID);
                    }

                    if (vehicleTypeCount > 0)
                    {
                        int index = Array.BinarySearch(orderId, currentOrderId);
                        if (index >= 0)
                        {
                            int[] orderVehicleTypes = new int[vehicleTypeCount];
                            Array.Copy(vehicleTypes, 0, orderVehicleTypes, 0, vehicleTypeCount);
                            allOrders[index].VehicleTypes = orderVehicleTypes;
                        }
                    }
                }
            }

            // 6. Выход - Ok
            rc = 0;
            return rc;
        }
        catch
        {
            return rc;
        }
    }

    /// <summary>
    /// Выбор параметров используемых способов доставки
    /// </summary>
    /// <param name="vehicleTypes">ID способов доставки</param>
    /// <param name="connection">Открытое соединение</param>
    /// <param name="courierTypeRecordss">Параметры способов доставки или null</param>
    /// <returns>0 - параметры выбраны; иначе - параметры не выбраны</returns>
    private static int SelectCourierTypes(int[] vehicleTypes, SqlConnection connection, out CourierTypeRecord[] courierTypeRecordss)
    {
        // 1. Инициализация
        int rc = 1;
        courierTypeRecordss = null;

        try
        {
            // 2. Проверяем исходные данные
            rc = 2;
            if (vehicleTypes == null || vehicleTypes.Length <= 0)
                return rc;
            if (connection == null || connection.State != ConnectionState.Open)
                return rc;

            // 3. Строим список загружаемых типов
            rc = 3;
            StringBuilder sb = new StringBuilder(8 * vehicleTypes.Length);
            sb.Append(vehicleTypes[0]);

            for (int i = 1; i < vehicleTypes.Length; i++)
            {
                sb.Append(',');
                sb.Append(vehicleTypes[i]);
            }

            // 4. Настраиваем SQL-запрос
            rc = 4;
            string sqlText = string.Format(SELECT_COURIER_TYPES, sb.ToString());

            // 5. Выбираем данные способов доставки
            rc = 5;
            XmlSerializer serializer = new XmlSerializer(typeof(CourierTypeData));
            CourierTypeRecord[] records = new CourierTypeRecord[vehicleTypes.Length];
            int count = 0;

            using (SqlCommand cmd = new SqlCommand(SELECT_ORDERS, connection))
            {
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    // 5.1 Выбираем индексы колонок в выборке
                    rc = 51;
                    int iVehicleID = reader.GetOrdinal("crtVehicleID");
                    int iIsTaxi = reader.GetOrdinal("crtIsTaxi");
                    int iYandexType = reader.GetOrdinal("crtYandexType");
                    int iDServiceType = reader.GetOrdinal("crtDServiceType");
                    int iCourierType = reader.GetOrdinal("crtCourierType");
                    int iCalcMethod = reader.GetOrdinal("crtCalcMethod");
                    int iMaxOrderWeight = reader.GetOrdinal("crtMaxOrderWeight");
                    int iMaxWeight = reader.GetOrdinal("crtMaxWeight");
                    int iMaxOrderCount = reader.GetOrdinal("crtMaxOrderCount");
                    int iMaxDistance = reader.GetOrdinal("crtMaxDistance");
                    int iGetOrderTime = reader.GetOrdinal("crtGetOrderTime");
                    int iHandInTime = reader.GetOrdinal("crtHandInTime");
                    int iStartDelay = reader.GetOrdinal("crtStartDelay");
                    int iData = reader.GetOrdinal("crtData");

                    while (reader.Read())
                    {
                        // 5.2 Десериализуем xml-данные
                        rc = 52;
                        SqlXml dataXml = reader.GetSqlXml(iData);
                        CourierTypeData ctd = null;
                        using (XmlReader xmlReader = dataXml.CreateReader())
                        {
                            ctd = (CourierTypeData)serializer.Deserialize(xmlReader);
                        }

                        // 5.3 Сохраняем тип
                        rc = 53;
                        records[count++] =
                            new CourierTypeRecord(reader.GetInt32(iVehicleID),
                                                  reader.GetBoolean(iIsTaxi),
                                                  reader.GetInt32(iYandexType),
                                                  reader.GetInt32(iDServiceType),
                                                  reader.GetInt32(iCourierType),
                                                  reader.GetString(iCalcMethod),
                                                  reader.GetDouble(iMaxOrderWeight),
                                                  reader.GetDouble(iMaxWeight),
                                                  reader.GetInt32(iMaxOrderCount),
                                                  reader.GetDouble(iMaxDistance),
                                                  reader.GetDouble(iGetOrderTime),
                                                  reader.GetDouble(iHandInTime),
                                                  reader.GetDouble(iStartDelay),
                                                  ctd);
                    }
                }
            }

            if (count < records.Length)
            {
                Array.Resize(ref records, count);
            }

            courierTypeRecordss = records;

            // 6. Выход - Ok
            rc = 0;
            return rc;
        }
        catch
        {
            return rc;
        }
    }

    /// <summary>
    /// Выбор курьеров и такси, доступных для построения отгрузок
    /// </summary>
    /// <param name="connection">Открытое соединение</param>
    /// <param name="records">Выбранные курьеры и такси или null</param>
    /// <returns>0 - курьеры выбраны; иначе - курьеры не выбраны</returns>
    private static int SelectCouriers(SqlConnection connection, out CourierRecord[] records)
    {
        // 1. Инициализация
        int rc = 1;
        records = null;

        try
        {
            // 2. Проверяем исходные данные
            rc = 2;
            if (connection == null || connection.State != ConnectionState.Open)
                return rc;

            // 3. Выбираем заказы
            rc = 3;
            CourierRecord[] allRecords = new CourierRecord[3000];
            int count = 0;

            using (SqlCommand cmd = new SqlCommand(SELECT_COURIERS, connection))
            {
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    int iCourierID = reader.GetOrdinal("crsCourierID");
                    int iVehicleID = reader.GetOrdinal("crsVehicleID");
                    int iStatusID = reader.GetOrdinal("crsStatusID");
                    int iWorkStart = reader.GetOrdinal("crsWorkStart");
                    int iWorkEnd = reader.GetOrdinal("crsWorkEnd");
                    int iLunchStart = reader.GetOrdinal("crsLunchTimeStart");
                    int iLunchEnd = reader.GetOrdinal("crsLunchTimeEnd");
                    int iLastDeliveryStart = reader.GetOrdinal("crsLastDeliveryStart");
                    int iLastDeliveryEnd = reader.GetOrdinal("crsLastDeliveryEnd");
                    int iOrderCount = reader.GetOrdinal("crsOrderCount");
                    int iTotalDeliveryTime = reader.GetOrdinal("crsTotalDeliveryTime");
                    int iTotalCost = reader.GetOrdinal("crsTotalCost");
                    int iAverageOrderCost = reader.GetOrdinal("crsAverageOrderCost");
                    int iShopID = reader.GetOrdinal("crsShopID");
                    int iLatitude = reader.GetOrdinal("crsLatitude");
                    int iLongitude = reader.GetOrdinal("crsLongitude");

                    while (reader.Read())
                    {
                        CourierRecord record = new CourierRecord();
                        record.CourierId = reader.GetInt32(iCourierID);
                        record.VehicleId = reader.GetInt32(iVehicleID);
                        record.Status = (CourierStatus)reader.GetInt32(iStatusID);
                        record.WorkStart = reader.GetTimeSpan(iWorkStart);
                        record.WorkEnd = reader.GetTimeSpan(iWorkEnd);
                        record.LunchTimeStart = reader.GetTimeSpan(iLunchStart);
                        record.LunchTimeEnd = reader.GetTimeSpan(iLunchEnd);
                        record.LastDeliveryStart = reader.GetDateTime(iLastDeliveryStart);
                        record.LastDeliveryEnd = reader.GetDateTime(iLastDeliveryEnd);
                        record.OrderCount = reader.GetInt32(iOrderCount);
                        record.TotalDeliveryTime = reader.GetDouble(iTotalDeliveryTime);
                        record.TotalCost = reader.GetDouble(iTotalCost);
                        record.AverageOrderCost = reader.GetDouble(iAverageOrderCost);
                        record.ShopId = reader.GetInt32(iShopID);
                        record.Latitude = reader.GetDouble(iLatitude);
                        record.Longitude = reader.GetDouble(iLongitude);

                        if (count >= allRecords.Length)
                        {
                            Array.Resize(ref allRecords, (int)(1.25 * allRecords.Length));
                        }

                        allRecords[count++] = record;
                    }
                }
            }

            if (count < allRecords.Length)
            {
                Array.Resize(ref allRecords, count);
            }

            records = allRecords;

            // 5. Выход - Ok
            rc = 0;
            return rc;
        }
        catch
        {
            return rc;
        }
    }

    /// <summary>
    /// Выбор порогов для средней стоимости доставки заказа
    /// </summary>
    /// <param name="connection">Открытое соединение</param>
    /// <param name="records">Записи таблицы lsvAverageDeliveryCost</param>
    /// <returns>0 - пороги выбраны; иначе - попроги не выбраны</returns>
    private static int SelectThresholds(SqlConnection connection, out AverageDeliveryCostRecord[] records)
    {
        // 1. Инициализация
        int rc = 1;
        records = null;

        try
        {
            // 2. Проверяем исходные данные
            rc = 2;
            if (connection == null || connection.State != ConnectionState.Open)
                return rc;

            // 3. Выбираем заказы
            rc = 3;
            AverageDeliveryCostRecord[] allRecords = new AverageDeliveryCostRecord[30000];
            int count = 0;

            using (SqlCommand cmd = new SqlCommand(SELECT_COURIERS, connection))
            {
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    int iShopId = reader.GetOrdinal("adcShopID");
                    int iVehicleId = reader.GetOrdinal("adcVehicleTypeID");
                    int iCost = reader.GetOrdinal("adcCost");

                    while (reader.Read())
                    {
                        AverageDeliveryCostRecord record = new AverageDeliveryCostRecord();
                        record.ShopId = reader.GetInt32(iShopId);
                        record.VehicleId = reader.GetInt32(iVehicleId);
                        record.Cost = reader.GetDouble(iCost);

                        if (count >= allRecords.Length)
                        {
                            Array.Resize(ref allRecords, (int)(1.25 * allRecords.Length));
                        }

                        allRecords[count++] = record;
                    }
                }
            }

            if (count < allRecords.Length)
            {
                Array.Resize(ref allRecords, count);
            }

            records = allRecords;

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
