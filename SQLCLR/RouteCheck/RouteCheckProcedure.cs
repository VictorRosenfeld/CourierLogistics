using SQLCLR.Couriers;
using SQLCLR.Deliveries;
using SQLCLR.Log;
using SQLCLR.Orders;
using SQLCLR.RouteCheck;
using SQLCLR.Shops;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

public partial class StoredProcedures
{
    /// <summary>
    /// Проверка маршрута
    /// </summary>
    /// <param name="request">Запрос</param>
    /// <param name="response">Отклик</param>
    /// <returns>0 - отгрузка построена; иначе - отгрузка не построена</returns>
    /// ------------------------------------------------------------------------
    /// Формат request:
    ///       <request request_id = "123" service_id="3000" calc_time="2021-08-15T12:07:45.239" optimized="1">
    ///         <shop shop_id = "..." lat="..." lon="..."/>
    ///         <orders>
    ///	            <order order_id = "1001" desrvice_id="8" weight="10.1" lat="37.1234789" lon="54.74567345" time_from="2021-08-15T12:07:45.239" time_to="2021-08-15T14:07:45.239"/>
    ///                             <!-- . . . -->
    ///	            <order order_id = "1001" desrvice_id="5" weight="10.1" lat="37.1234789" lon="54.74567345" time_from="2021-08-15T12:07:45.239" time_to="2021-08-15T14:07:45.239"/>
    ///         </orders>
    ///       </request>
    public static SqlInt32 RouteCheck(SqlString request, out SqlString response)
    {
        // 1. Инициализация
        int rc = 1;
        int rc1 = 1;
        response = null;

        try
        {
            // 2. Проверяем исходные данные
            rc = 2;
            if (request.IsNull)
                return rc;

            // 3. Извлекаем request
            rc = 3;
            XmlSerializer serializer = new XmlSerializer(typeof(RootCheckRequest));
            RootCheckRequest requestData;

            using (MemoryStream ms = new MemoryStream(Encoding.Unicode.GetBytes(request.Value)))
            {  requestData = (RootCheckRequest) serializer.Deserialize(ms); }

            if (requestData.service_id <= 0 ||
                requestData.calc_time == default(DateTime) ||
                requestData.shop == null || requestData.shop.lat <= 0 || requestData.shop.lon <= 0 ||
                requestData.orders == null || requestData.orders.Length <= 0)
                return rc;

            // 4. Строим объект "магазин"
            rc = 4;
            Shop shop = new Shop(requestData.shop.shop_id);
            shop.Latitude = requestData.shop.lat;
            shop.Longitude = requestData.shop.lat;
            shop.WorkStart = new TimeSpan(0, 0, 0);
            shop.WorkEnd = new TimeSpan(23, 59, 59);

            // 5. Выбираем dservice_id заказов
            rc = 5;
            int[] dserviceId = new int[requestData.orders.Length];

            for (int i = 0; i < requestData.orders.Length; i++)
            {
                dserviceId[i] = requestData.orders[i].desrvice_id;
            }

            Array.Sort(dserviceId);
            int count = 1;

            for (int i = 1; i < dserviceId.Length; i++)
            {
                if (dserviceId[i] == dserviceId[i - 1])
                    dserviceId[count++] = dserviceId[i];
            }

            if (count < dserviceId.Length)
            {
                Array.Resize(ref dserviceId, count);
            }

            // 6. Создаём объект "курьеры"
            rc = 6;
            Courier[] couriers = null;

            using (SqlConnection connection = new SqlConnection("context connection=true"))
            {
                // 6.1 Открываем соединение
                rc = 61;
                connection.Open();

                // 6.2 Загружаем параметры способов отгрузки
                rc = 62;
                CourierTypeRecord[] courierTypeRecords;
                rc1 = SelectCourierTypesByDserviceId(dserviceId, connection, out courierTypeRecords);
                if (rc1 != 0)
                    return rc = 100 * rc + rc1;

                // 6.3. Выбираем всех делегатов расчета времени и стоимости отгрузки
                rc = 63;
                GetTimeAndCostDelegate[] calculators = TimeAndCostCalculator.SelectCalculators();
                if (calculators == null || calculators.Length <= 0)
                    return rc;
                string[] calculatorKeys = new string[calculators.Length];

                for (int i = 0; i < calculators.Length; i++)
                {
                    calculatorKeys[i] = calculators[i].Method.Name.ToLower();
                }

                Array.Sort(calculatorKeys, calculators);

                // 6.4 Стороим референсных курьеров c заданными типами доставки
                rc = 64;
                couriers = new Courier[courierTypeRecords.Length];
                count = 0;

                for (int i = 0; i < courierTypeRecords.Length; i++)
                {
                    CourierTypeRecord record = courierTypeRecords[i];
                    CourierBase courierBase = new CourierBase(record);

                    int index = Array.BinarySearch(calculatorKeys, TimeAndCostCalculator.GetMethodName(record.CalcMethod).ToLower());
                    if (index >= 0)
                    {
                        courierBase.SetCalculator(calculators[index]);
                        Courier courier = new Courier(record.VehicleID, courierBase);
                        courier.Status = CourierStatus.Ready;
                        courier.WorkStart = TimeSpan.Zero;
                        courier.WorkEnd = TimeSpan.FromHours(24);
                        courier.ShopId = (record.IsTaxi ? 0 : requestData.shop.shop_id);
                        courier.LunchTimeStart = TimeSpan.Zero;
                        courier.LunchTimeEnd = TimeSpan.Zero;
                        couriers[count++] = courier;
                    }
                }

                if (count <= 0)
                    return rc;
                if (count < couriers.Length)
                {
                    Array.Resize(ref couriers, count);
                }

                // 7. Строим объекты "заказы"
                rc = 7;
                Order[] orders = new Order[requestData.orders.Length];

                for (int i = 0; i < requestData.orders.Length; i++)
                {
                    OrderData data = requestData.orders[i];
                    if (data.time_from >= data.time_to)
                        return rc = 51;
                    if (data.lat <= 0 || data.lon <= 0)
                        return rc = 100 * rc + i;

                    Order order = new Order(data.order_id);
                    order.ShopId = requestData.shop.shop_id;
                    order.ReceiptedDate = requestData.calc_time;
                    order.AssembledDate = requestData.calc_time;
                    order.Status = OrderStatus.Assembled;
                    order.Weight = data.weight;
                    order.Priority = 5;
                    order.DeliveryTimeFrom = data.time_from;
                    order.DeliveryTimeFrom = data.time_to;
                    order.Latitude = data.lat;
                    order.Longitude = data.lon;
                    order.VehicleTypes = GetCourierVehicleId(data.desrvice_id, couriers);

                    orders[i] = order;
                }

                // 8. Отбираем общих курьеров
                rc = 8;
                Courier[] deliveryCouriers = GetDeliveryCouriers(orders, couriers);
                if (deliveryCouriers == null || deliveryCouriers.Length <= 0)
                    return rc;

                // 9. Сортируем курьров по YandexType
                rc = 9;
                Array.Sort(deliveryCouriers, CompareCouriersByYandexType);

                // 10. Цикл проверки маршрута по курьере
                rc = 10;
                int currentYandexType = -1;
                Point[,] geoData = null;
                int[] geoOrderIndex = new int[orders.Length + 1];
                for (int i = 0; i <= orders.Length; i++)
                    geoOrderIndex[i] = i;

                for (int i = 0; i < deliveryCouriers.Length; i++)
                {
                    // 10.0 Извлекаем курьера
                    rc = 100;
                    Courier courier = deliveryCouriers[i];

                    // 10.1 Обеспечиваем гео-данные
                    rc = 101;
                    if (courier.YandexType != currentYandexType)
                    {
                        currentYandexType = courier.YandexType;
                        rc1 = GeoData.Select(connection, requestData.service_id, currentYandexType, shop, orders, out geoData);
                        if (rc1 != 0)
                            continue;
                    }

                    // 10.2 Проверяем/находим маршрут
                    rc = 102;
                    CourierDeliveryInfo delivery;
                    if (!requestData.optimized)
                    {
                        rc1 = courier.DeliveryCheck(requestData.calc_time, shop, orders, geoOrderIndex, orders.Length, !courier.IsTaxi, geoData, out delivery);
                    }
                    else
                    {

                    }


                }







                // 6.5 Закрываем соединение
                rc = 65;
                connection.Close();
            }




            return rc;
        }
        catch
        {
            return rc;
        }
    }

    private static int[] GetCourierVehicleId(int dserviceId, Courier[] couriers)
    {
        try
        {
            if (couriers == null || couriers.Length <= 0)
                return new int[0];
            int[] vehicleId = new int[couriers.Length];
            int count = 0;

            for (int i = 0; i < couriers.Length; i++)
            {
                if (couriers[i].DServiceType == dserviceId)
                    vehicleId[count++] = couriers[i].VehicleID;
            }

            if (count < vehicleId.Length)
            {
                Array.Resize(ref vehicleId, count);
            }

            return vehicleId;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Отбор курьеров, способных доставить
    /// все заданные заказы
    /// </summary>
    /// <param name="orders">Заказы</param>
    /// <param name="couriers">Курьеры</param>
    /// <returns>Общие курьеры или null</returns>
    private static Courier[] GetDeliveryCouriers(Order[] orders, Courier[] couriers)
    {
        // 1. Инициализация

        try
        {
            // 2. Проверяем исходные данные
            if (orders == null || orders.Length <= 0)
                return  new Courier[0];
            if (couriers == null || couriers.Length <= 0)
                return new Courier[0];

            // 3. Строим индекс курьров
            Courier[] sortedCouriers = (Courier[])couriers.Clone();
            int[] vehicleId = new int[sortedCouriers.Length];

            for (int i = 0; i < sortedCouriers.Length; i++)
            { vehicleId[i] = sortedCouriers[i].VehicleID; }

            Array.Sort(vehicleId, sortedCouriers);

            // 4. Подсчитываем использование способов доставки
            bool[] isUsed = new bool[vehicleId.Length];
            int[] vehicleCount = new int[vehicleId.Length];

            for (int i = 0; i < orders.Length; i++)
            {
                Array.Clear(isUsed, 0, isUsed.Length);
                int[] orderVehicleId = orders[i].VehicleTypes;

                for (int j = 0; j < orderVehicleId.Length; j++)
                {
                    int index = Array.BinarySearch(vehicleId, orderVehicleId[j]);
                    if (index >= 0)
                    {
                        if (!isUsed[index])
                        {
                            vehicleCount[index]++;
                            isUsed[index] = true;
                        }
                    }
                }
            }

            // 5. Отбираем общих курьеров
            int orderCount = orders.Length;
            int count = 0;

            for (int i = 0; i < vehicleCount.Length; i++)
            {
                if (vehicleCount[i] == orderCount)
                    sortedCouriers[count++] = sortedCouriers[i];
            }
            
            if (count < sortedCouriers.Length)
            {
                Array.Resize(ref sortedCouriers, count);
            }

            // 6. Выход
            return sortedCouriers;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Выбор параметров используемых способов доставки
    /// </summary>
    /// <param name="dserviceId">ID сервисов доставки</param>
    /// <param name="connection">Открытое соединение</param>
    /// <param name="courierTypeRecordss">Параметры способов доставки или null</param>
    /// <returns>0 - параметры выбраны; иначе - параметры не выбраны</returns>
    private static int SelectCourierTypesByDserviceId(int[] dserviceId, SqlConnection connection, out CourierTypeRecord[] courierTypeRecordss)
    {
        // 1. Инициализация
        int rc = 1;
        courierTypeRecordss = null;

        try
        {
            // 2. Проверяем исходные данные
            rc = 2;
            if (dserviceId == null || dserviceId.Length <= 0)
                return rc;
            if (connection == null || connection.State != ConnectionState.Open)
                return rc;

            // 3. Строим список загружаемых типов
            rc = 3;
            StringBuilder sb = new StringBuilder(8 * dserviceId.Length);
            sb.Append(dserviceId[0]);

            for (int i = 1; i < dserviceId.Length; i++)
            {
                sb.Append(',');
                sb.Append(dserviceId[i]);
            }

            // 4. Настраиваем SQL-запрос
            rc = 4;
            string sqlText = string.Format(SELECT_COURIER_TYPES1, sb.ToString());

            // 5. Выбираем данные способов доставки
            rc = 5;
            XmlSerializer serializer = new XmlSerializer(typeof(CourierTypeData));
            CourierTypeRecord[] records = new CourierTypeRecord[64 * dserviceId.Length];
            int count = 0;

            using (SqlCommand cmd = new SqlCommand(sqlText, connection))
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
                            ctd.Create();
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

    private static int CompareCouriersByYandexType(Courier courier1, Courier courier2)
    {
        if (courier1.YandexType < courier2.YandexType)
            return -1;
        if (courier1.YandexType > courier2.YandexType)
            return 1;
        return 0;
    }

}
