//using Microsoft.SqlServer.Server;
//using SQLCLRex.Couriers;
//using SQLCLRex.Deliveries;
//using SQLCLRex.Log;
//using SQLCLRex.Orders;
//using SQLCLRex.RouteCheck;
//using SQLCLRex.Shops;
//using System;
//using System.Collections.Generic;
//using System.Data;
//using System.Data.SqlClient;
//using System.Data.SqlTypes;
//using System.IO;
//using System.Text;
//using System.Xml;
//using System.Xml.Serialization;

//public partial class StoredProcedures
//{
//    /// <summary>
//    /// Проверка маршрута
//    /// </summary>
//    /// <param name="request">Запрос</param>
//    /// <param name="response">Отклик</param>
//    /// <returns>0 - отгрузка построена; иначе - отгрузка не построена</returns>
//    /// ------------------------------------------------------------------------
//    /// Формат request:
//    ///       <request request_id = "123" service_id="3000" calc_time="2021-08-15T12:07:45.239" optimized="1">
//    ///         <shop shop_id = "..." lat="..." lon="..."/>
//    ///         <orders>
//    ///	            <order order_id = "1001" desrvice_id="8" weight="10.1" lat="37.1234789" lon="54.74567345" time_from="2021-08-15T12:07:45.239" time_to="2021-08-15T14:07:45.239"/>
//    ///                             <!-- . . . -->
//    ///	            <order order_id = "1001" desrvice_id="5" weight="10.1" lat="37.1234789" lon="54.74567345" time_from="2021-08-15T12:07:45.239" time_to="2021-08-15T14:07:45.239"/>
//    ///         </orders>
//    ///       </request>
//    /// ------------------------------------------------------------------------
//    /// Формат response:
//    ///       <response request_id = "123" service_id="3000" calc_time="2021-08-15T12:07:45.239" optimized="1">
//    ///          <deliveries>
//    ///	            <delivery shop_id="9999" dservice_id="14" courier_id="6" start_delivery_interval="2021-08-15T12:07:45.239" end_delivery_interval="2021-08-15T12:07:45.239" weight="29.123" is_loop="1" reserve_time="37.23" delivery_time="63.78" execution_time="63.78" code="0">
//    ///                <orders>
//    ///                   <order order_id = "1" />
//    ///                       < !-- . . . -- >
//    ///                   < order order_id="10"/>
//    ///                </orders>
//    ///                <node_info>
//    ///                   <!-- Гео-данные Yandex (первая и последняя точка всегда соответвтвуют магазину -->
//    ///    	              <node distance = "1000" duration= "123" />
//    ///                                < !-- . . . -- >
//    ///                   <node distance= "5000" duration= "475" />
//    ///                </node_info>
//    ///                <node_delivery_time>
//    ///                   <!--Время от начала отгрузки до вручения (в случае такси - от момента вызова). Первая и последняя точка соответствуют магазину  -->
//    ///             	  <node delivery_time = "3" />
//    ///                         < !-- . . . -- >
//    ///                   <node delivery_time="123.98"/>
//    ///                </node_delivery_time>
//    ///             </delivery>
//    ///	            <delivery shop_id="9999" dservice_id="14" courier_id="6" weight="29.123" is_loop="1" code="0"/>
//    ///                       < !-- . . . -- >
//    ///	            <delivery shop_id="9999" dservice_id="14" courier_id="6" start_delivery_interval="2021-08-15T12:07:45.239" end_delivery_interval="2021-08-15T12:07:45.239" weight="29.123" is_loop="1" reserve_time="37.23" delivery_time="63.78" execution_time="63.78" code="0">
//    ///                <orders>
//    ///                   <order order_id = "1" />
//    ///                       < !-- . . . -- >
//    ///                   < order order_id="10"/>
//    ///                </orders>
//    ///                <node_info>
//    ///                   <!-- Гео-данные Yandex (первая и последняя точка всегда соответвтвуют магазину -->
//    ///    	              <node distance = "1000" duration= "123" />
//    ///                                < !-- . . . -- >
//    ///                   <node distance= "5000" duration= "475" />
//    ///                </node_info>
//    ///                <node_delivery_time>
//    ///                   <!--Время от начала отгрузки до вручения (в случае такси - от момента вызова). Первая и последняя точка соответствуют магазину  -->
//    ///             	  <node delivery_time = "3" />
//    ///                         < !-- . . . -- >
//    ///                   <node delivery_time="123.98"/>
//    ///                </node_delivery_time>
//    ///             </delivery>
//    ///          </deliveries>
//    ///       </response>
//    [SqlProcedure]
//    public static SqlInt32 RouteCheck(SqlString request, out SqlString response)
//    {
//        // 1. Инициализация
//        int rc = 1;
//        int rc1 = 1;
//        response = null;
//        RootCheckResponse checkResponse = null;

//        try
//        {
//            // 2. Проверяем исходные данные
//            rc = 2;
//            if (request.IsNull)
//                return rc;

//            // 3. Извлекаем request
//            rc = 3;
//            XmlSerializer serializer = new XmlSerializer(typeof(RootCheckRequest));
//            RootCheckRequest requestData;

//            using (MemoryStream ms = new MemoryStream(Encoding.Unicode.GetBytes(request.Value)))
//            {  requestData = (RootCheckRequest) serializer.Deserialize(ms); }

//            if (requestData.service_id <= 0 ||
//                requestData.calc_time == default(DateTime) ||
//                requestData.shop == null || requestData.shop.lat <= 0 || requestData.shop.lon <= 0 ||
//                requestData.orders == null || requestData.orders.Length <= 0)
//                return rc;

//            // 4. Строим объект "магазин"
//            rc = 4;
//            Shop shop = new Shop(requestData.shop.shop_id);
//            shop.Latitude = requestData.shop.lat;
//            shop.Longitude = requestData.shop.lon;
//            shop.WorkStart = new TimeSpan(0, 0, 0);
//            shop.WorkEnd = new TimeSpan(23, 59, 59);

//            // 5. Выбираем dservice_id заказов
//            rc = 5;
//            int[] dserviceId = new int[requestData.orders.Length];

//            for (int i = 0; i < requestData.orders.Length; i++)
//            {
//                dserviceId[i] = requestData.orders[i].desrvice_id;
//            }

//            Array.Sort(dserviceId);
//            int count = 1;

//            for (int i = 1; i < dserviceId.Length; i++)
//            {
//                if (dserviceId[i] == dserviceId[i - 1])
//                    dserviceId[count++] = dserviceId[i];
//            }

//            if (count < dserviceId.Length)
//            {
//                Array.Resize(ref dserviceId, count);
//            }

//            // 6. Создаём объект "курьеры"
//            rc = 6;
//            Courier[] couriers = null;

//            using (SqlConnection connection = new SqlConnection("context connection=true"))
//            {
//                // 6.1 Открываем соединение
//                rc = 61;
//                connection.Open();

//                // 6.2 Загружаем параметры способов отгрузки
//                rc = 62;
//                CourierTypeRecord[] courierTypeRecords;
//                rc1 = SelectCourierTypesByDserviceId(dserviceId, connection, out courierTypeRecords);
//                if (rc1 != 0)
//                    return rc = 100 * rc + rc1;

//                // 6.3. Выбираем всех делегатов расчета времени и стоимости отгрузки
//                rc = 63;
//                GetTimeAndCostDelegate[] calculators = TimeAndCostCalculator.SelectCalculators();
//                if (calculators == null || calculators.Length <= 0)
//                    return rc;
//                string[] calculatorKeys = new string[calculators.Length];

//                for (int i = 0; i < calculators.Length; i++)
//                {
//                    calculatorKeys[i] = calculators[i].Method.Name.ToLower();
//                }

//                Array.Sort(calculatorKeys, calculators);

//                // 6.4 Стороим референсных курьеров c заданными типами доставки
//                rc = 64;
//                couriers = new Courier[courierTypeRecords.Length];
//                count = 0;

//                for (int i = 0; i < courierTypeRecords.Length; i++)
//                {
//                    CourierTypeRecord record = courierTypeRecords[i];
//                    CourierBase courierBase = new CourierBase(record);

//                    int index = Array.BinarySearch(calculatorKeys, TimeAndCostCalculator.GetMethodName(record.CalcMethod).ToLower());
//                    if (index >= 0)
//                    {
//                        courierBase.SetCalculator(calculators[index]);
//                        Courier courier = new Courier(record.VehicleID, courierBase);
//                        courier.Status = CourierStatus.Ready;
//                        courier.WorkStart = TimeSpan.Zero;
//                        courier.WorkEnd = TimeSpan.FromHours(24);
//                        courier.ShopId = (record.IsTaxi ? 0 : requestData.shop.shop_id);
//                        courier.LunchTimeStart = TimeSpan.Zero;
//                        courier.LunchTimeEnd = TimeSpan.Zero;
//                        couriers[count++] = courier;
//                    }
//                }

//                if (count <= 0)
//                    return rc;
//                if (count < couriers.Length)
//                {
//                    Array.Resize(ref couriers, count);
//                }

//                // 7. Строим объекты "заказы"
//                rc = 7;
//                Order[] orders = new Order[requestData.orders.Length];
//                double weight = 0;

//                for (int i = 0; i < requestData.orders.Length; i++)
//                {
//                    OrderData data = requestData.orders[i];
//                    weight += data.weight;
//                    if (data.time_from >= data.time_to)
//                        return rc = 51;
//                    if (data.lat <= 0 || data.lon <= 0)
//                        return rc = 100 * rc + i;

//                    Order order = new Order(data.order_id);
//                    order.ShopId = requestData.shop.shop_id;
//                    order.ReceiptedDate = requestData.calc_time;
//                    order.AssembledDate = requestData.calc_time;
//                    order.Status = OrderStatus.Assembled;
//                    order.Weight = data.weight;
//                    order.Priority = 5;
//                    order.DeliveryTimeFrom = data.time_from;
//                    order.DeliveryTimeTo = data.time_to;
//                    order.Latitude = data.lat;
//                    order.Longitude = data.lon;
//                    order.VehicleTypes = GetCourierVehicleId(data.desrvice_id, couriers);

//                    orders[i] = order;
//                }

//                // 8. Отбираем общих курьеров
//                rc = 8;
//                Courier[] deliveryCouriers = GetDeliveryCouriers(orders, couriers);
//                if (deliveryCouriers == null || deliveryCouriers.Length <= 0)
//                    return rc;

//                // 9. Сортируем курьров по YandexType
//                rc = 9;
//                Array.Sort(deliveryCouriers, CompareCouriersByYandexType);

//                // 10. Создаём все перестановки, если требуется
//                rc = 10;
//                int orderCount = orders.Length;
//                byte[] permutations = null;
//                int[] geoOrderIndex = new int[orderCount + 1];
//                if (requestData.optimized && orderCount > 1)
//                {
//                    permutations = Permutations.Generate(orderCount);
//                    if (permutations == null)
//                        return rc;
//                    geoOrderIndex[orderCount] = orderCount;
//                }
//                else
//                {
//                    for (int i = 0; i <= orders.Length; i++)
//                        geoOrderIndex[i] = i;
//                }

//                // 11. Инициализируем отклик
//                rc = 11;
//                checkResponse = new RootCheckResponse();
//                checkResponse.calc_time = requestData.calc_time;
//                checkResponse.optimized = requestData.optimized;
//                checkResponse.request_id = requestData.request_id;
//                checkResponse.service_id = requestData.service_id;

//                // 12. Цикл проверки маршрута по способам доставки
//                rc = 12;
//                int currentYandexType = -1;
//                Point[,] geoData = null;
//                Order[] permutOrders = new Order[orderCount];

//                for (int i = 0; i < deliveryCouriers.Length; i++)
//                {
//                    // 12.0 Извлекаем курьера
//                    rc = 120;
//                    Courier courier = deliveryCouriers[i];
//                    DeliveryData data = new DeliveryData();
//                    data.courier_id = courier.Id;
//                    data.dservice_id = courier.DServiceType;
//                    data.is_loop = !courier.IsTaxi;
//                    data.shop_id = shop.Id;
//                    data.weight = weight;

//                    // 12.1 Обеспечиваем гео-данные
//                    rc = 121;
//                    if (courier.YandexType != currentYandexType)
//                    {
//                        rc1 = GeoData.Select(connection, requestData.service_id, courier.YandexType, shop, orders, out geoData);
//                        if (rc1 != 0)
//                        {
//                            data.code = rc;
//                            checkResponse.AddDelivery(data);
//                            continue;
//                        }
//                        currentYandexType = courier.YandexType;
//                    }

//                    // 12.2 Проверяем/находим маршрут
//                    rc = 122;
//                    CourierDeliveryInfo delivery;
//                    CourierDeliveryInfo bestDelivery = null;

//                    if (permutations == null)
//                    {
//                        rc1 = courier.DeliveryCheck(requestData.calc_time, shop, orders, geoOrderIndex, orderCount, !courier.IsTaxi, geoData, out delivery);
//                        if (rc1 == 0)
//                            bestDelivery = delivery;
//                    }
//                    else
//                    {
////#if debug
////                        Logger.WriteToLog(1, $"RouteCheck calc_time = {requestData.calc_time}, permutations.Length = {permutations.Length}, orderCount = {orderCount}", 0);
////#endif

//                        for (int permutOffest = 0; permutOffest < permutations.Length; permutOffest += orderCount)
//                        {
//                            for (int j = 0; j < orderCount; j++)
//                            {
//                                int orderIndex = permutations[permutOffest + j];
//                                geoOrderIndex[j] = orderIndex;
//                                permutOrders[j] = orders[orderIndex];
//                            }
////#if debug
////                            //7 < order order_id = "31316590" />
////                            // 4 < order order_id = "31310785" />
////                            //  6 < order order_id = "31315952" />
////                            //   5 < order order_id = "31310862" />
////                            //    0 < order order_id = "31299795" />
////                            //     1 < order order_id = "31301311" />
////                            //      2 < order order_id = "31307172" />
////                            //       3 < order order_id = "31308851" />

////                            if (geoOrderIndex[0] == 7 &&
////                                geoOrderIndex[1] == 4 &&
////                                geoOrderIndex[2] == 6 &&
////                                geoOrderIndex[3] == 5 &&
////                                geoOrderIndex[4] == 0 &&
////                                geoOrderIndex[5] == 1 &&
////                                geoOrderIndex[6] == 2 &&
////                                geoOrderIndex[7] == 3)
////                            {
////                                Logger.WriteToLog(5, $"RouteCheck (7, 4, 6, 5, 0, 1, 2, 3) is exists", 0);
////                            }                                                              
////#endif

//                            rc1 = courier.DeliveryCheck(requestData.calc_time, shop, permutOrders, geoOrderIndex, orderCount, !courier.IsTaxi, geoData, out delivery);
//                            if (rc1 == 0)
//                            {
//                                if (bestDelivery == null)
//                                { bestDelivery = delivery; }
//                                else if (delivery.Cost < bestDelivery.Cost)
//                                { bestDelivery = delivery; }
//                            }
//                        }
//                    }

//                    // 12.3 Добавляем отгрузку
//                    rc = 123;
//                    if (bestDelivery == null)
//                    {
//                        data.code = 100000 * rc + rc1;
//                    }
//                    else
//                    {
//                        data.delivery_time = bestDelivery.DeliveryTime;
//                        data.execution_time = bestDelivery.ExecutionTime;
//                        data.reserve_time = bestDelivery.ReserveTime.TotalMinutes;
//                        data.start_delivery_interval = bestDelivery.StartDeliveryInterval;
//                        data.end_delivery_interval = bestDelivery.EndDeliveryInterval;
//                        data.weight = bestDelivery.Weight;
//                        data.cost = bestDelivery.Cost;
//                        data.code = 0;

//                        // orders
//                        rc = 1231;
//                        OrderSeq[] seq = new OrderSeq[bestDelivery.Orders.Length];
//                        for (int j = 0; j < seq.Length; j++)
//                        {
//                            seq[j] = new OrderSeq();
//                            seq[j].order_id = bestDelivery.Orders[j].Id;
//                        }
//                        data.orders = seq;

//                        // node_info
//                        rc = 1232;
//                        DeliveryNode[] nodeInfo = new DeliveryNode[bestDelivery.NodeInfo.Length];
//                        for (int j = 0; j < nodeInfo.Length; j++)
//                        {
//                            nodeInfo[j] = new DeliveryNode();
//                            nodeInfo[j].distance = bestDelivery.NodeInfo[j].X;
//                            nodeInfo[j].duration = bestDelivery.NodeInfo[j].Y;
//                        }
//                        data.node_info = nodeInfo;

//                        // node_delivery_time
//                        rc = 1233;
//                        DeliveryTime[] nodeDeliveryTime = new DeliveryTime[bestDelivery.NodeDeliveryTime.Length];
//                        for (int j = 0; j < nodeDeliveryTime.Length; j++)
//                        {
//                            nodeDeliveryTime[j] = new DeliveryTime();
//                            nodeDeliveryTime[j].delivery_time = bestDelivery.NodeDeliveryTime[j];
//                        }
//                        data.node_delivery_time = nodeDeliveryTime;

//                    }

//                    checkResponse.AddDelivery(data);
//                }

//                // 13. Готовим отклик
//                rc = 13;
//                serializer = new XmlSerializer(typeof(RootCheckResponse));
//                var ns = new XmlSerializerNamespaces();
//                ns.Add("", "");

//                using (StringWriter stringWriter = new StringWriter())
//                {
//                    using (XmlWriter xmlWriter = XmlWriter.Create(stringWriter, new XmlWriterSettings() { OmitXmlDeclaration = true }))
//                    {
//                        serializer = new XmlSerializer(typeof(RootCheckResponse));
//                        serializer.Serialize(xmlWriter, checkResponse, ns);
//                        response = new SqlString(stringWriter.ToString());
//                        xmlWriter.Close();
//                    }
//                }

//                // 14. Закрываем соединение
//                rc = 14;
//                connection.Close();
//            }

//            // 15. Выход - Ok
//            rc = 0;
//            return rc;
//        }
//        catch (Exception ex)
//        {
//#if debug
//            Logger.WriteToLog(3, $"RouteCheck. rc = {rc}. Exception {ex.Message}", 2);
//#endif
//            return rc;
//        }
//    }

//    private static int[] GetCourierVehicleId(int dserviceId, Courier[] couriers)
//    {
//        try
//        {
//            if (couriers == null || couriers.Length <= 0)
//                return new int[0];
//            int[] vehicleId = new int[couriers.Length];
//            int count = 0;

//            for (int i = 0; i < couriers.Length; i++)
//            {
//                if (couriers[i].DServiceType == dserviceId)
//                    vehicleId[count++] = couriers[i].VehicleID;
//            }

//            if (count < vehicleId.Length)
//            {
//                Array.Resize(ref vehicleId, count);
//            }

//            return vehicleId;
//        }
//        catch
//        {
//            return null;
//        }
//    }

//    /// <summary>
//    /// Отбор курьеров, способных доставить
//    /// все заданные заказы
//    /// </summary>
//    /// <param name="orders">Заказы</param>
//    /// <param name="couriers">Курьеры</param>
//    /// <returns>Общие курьеры или null</returns>
//    private static Courier[] GetDeliveryCouriers(Order[] orders, Courier[] couriers)
//    {
//        // 1. Инициализация

//        try
//        {
//            // 2. Проверяем исходные данные
//            if (orders == null || orders.Length <= 0)
//                return  new Courier[0];
//            if (couriers == null || couriers.Length <= 0)
//                return new Courier[0];

//            // 3. Строим индекс курьров
//            Courier[] sortedCouriers = (Courier[])couriers.Clone();
//            int[] vehicleId = new int[sortedCouriers.Length];

//            for (int i = 0; i < sortedCouriers.Length; i++)
//            { vehicleId[i] = sortedCouriers[i].VehicleID; }

//            Array.Sort(vehicleId, sortedCouriers);

//            // 4. Подсчитываем использование способов доставки
//            bool[] isUsed = new bool[vehicleId.Length];
//            int[] vehicleCount = new int[vehicleId.Length];

//            for (int i = 0; i < orders.Length; i++)
//            {
//                Array.Clear(isUsed, 0, isUsed.Length);
//                int[] orderVehicleId = orders[i].VehicleTypes;

//                for (int j = 0; j < orderVehicleId.Length; j++)
//                {
//                    int index = Array.BinarySearch(vehicleId, orderVehicleId[j]);
//                    if (index >= 0)
//                    {
//                        if (!isUsed[index])
//                        {
//                            vehicleCount[index]++;
//                            isUsed[index] = true;
//                        }
//                    }
//                }
//            }

//            // 5. Отбираем общих курьеров
//            int orderCount = orders.Length;
//            int count = 0;

//            for (int i = 0; i < vehicleCount.Length; i++)
//            {
//                if (vehicleCount[i] == orderCount)
//                    sortedCouriers[count++] = sortedCouriers[i];
//            }
            
//            if (count < sortedCouriers.Length)
//            {
//                Array.Resize(ref sortedCouriers, count);
//            }

//            // 6. Выход
//            return sortedCouriers;
//        }
//        catch
//        {
//            return null;
//        }
//    }

//    /// <summary>
//    /// Выбор параметров используемых способов доставки
//    /// </summary>
//    /// <param name="dserviceId">ID сервисов доставки</param>
//    /// <param name="connection">Открытое соединение</param>
//    /// <param name="courierTypeRecordss">Параметры способов доставки или null</param>
//    /// <returns>0 - параметры выбраны; иначе - параметры не выбраны</returns>
//    private static int SelectCourierTypesByDserviceId(int[] dserviceId, SqlConnection connection, out CourierTypeRecord[] courierTypeRecordss)
//    {
//        // 1. Инициализация
//        int rc = 1;
//        courierTypeRecordss = null;

//        try
//        {
//            // 2. Проверяем исходные данные
//            rc = 2;
//            if (dserviceId == null || dserviceId.Length <= 0)
//                return rc;
//            if (connection == null || connection.State != ConnectionState.Open)
//                return rc;

//            // 3. Строим список загружаемых типов
//            rc = 3;
//            StringBuilder sb = new StringBuilder(8 * dserviceId.Length);
//            sb.Append(dserviceId[0]);

//            for (int i = 1; i < dserviceId.Length; i++)
//            {
//                sb.Append(',');
//                sb.Append(dserviceId[i]);
//            }

//            // 4. Настраиваем SQL-запрос
//            rc = 4;
//            string sqlText = string.Format(SELECT_COURIER_TYPES1, sb.ToString());

//            // 5. Выбираем данные способов доставки
//            rc = 5;
//            XmlSerializer serializer = new XmlSerializer(typeof(CourierTypeData));
//            CourierTypeRecord[] records = new CourierTypeRecord[64 * dserviceId.Length];
//            int count = 0;

//            using (SqlCommand cmd = new SqlCommand(sqlText, connection))
//            {
//                using (SqlDataReader reader = cmd.ExecuteReader())
//                {
//                    // 5.1 Выбираем индексы колонок в выборке
//                    rc = 51;
//                    int iVehicleID = reader.GetOrdinal("crtVehicleID");
//                    int iIsTaxi = reader.GetOrdinal("crtIsTaxi");
//                    int iYandexType = reader.GetOrdinal("crtYandexType");
//                    int iDServiceType = reader.GetOrdinal("crtDServiceType");
//                    int iCourierType = reader.GetOrdinal("crtCourierType");
//                    int iCalcMethod = reader.GetOrdinal("crtCalcMethod");
//                    int iMaxOrderWeight = reader.GetOrdinal("crtMaxOrderWeight");
//                    int iMaxWeight = reader.GetOrdinal("crtMaxWeight");
//                    int iMaxOrderCount = reader.GetOrdinal("crtMaxOrderCount");
//                    int iMaxDistance = reader.GetOrdinal("crtMaxDistance");
//                    int iGetOrderTime = reader.GetOrdinal("crtGetOrderTime");
//                    int iHandInTime = reader.GetOrdinal("crtHandInTime");
//                    int iStartDelay = reader.GetOrdinal("crtStartDelay");
//                    int iData = reader.GetOrdinal("crtData");

//                    while (reader.Read())
//                    {
//                        // 5.2 Десериализуем xml-данные
//                        rc = 52;
//                        SqlXml dataXml = reader.GetSqlXml(iData);
//                        CourierTypeData ctd = null;
//                        using (XmlReader xmlReader = dataXml.CreateReader())
//                        {
//                            ctd = (CourierTypeData)serializer.Deserialize(xmlReader);
//                            ctd.Create();
//                        }

//                        // 5.3 Сохраняем тип
//                        rc = 53;
//                        records[count++] =
//                            new CourierTypeRecord(reader.GetInt32(iVehicleID),
//                                                  reader.GetBoolean(iIsTaxi),
//                                                  reader.GetInt32(iYandexType),
//                                                  reader.GetInt32(iDServiceType),
//                                                  reader.GetInt32(iCourierType),
//                                                  reader.GetString(iCalcMethod),
//                                                  reader.GetDouble(iMaxOrderWeight),
//                                                  reader.GetDouble(iMaxWeight),
//                                                  reader.GetInt32(iMaxOrderCount),
//                                                  reader.GetDouble(iMaxDistance),
//                                                  reader.GetDouble(iGetOrderTime),
//                                                  reader.GetDouble(iHandInTime),
//                                                  reader.GetDouble(iStartDelay),
//                                                  ctd);
//                    }
//                }
//            }

//            if (count < records.Length)
//            {
//                Array.Resize(ref records, count);
//            }

//            courierTypeRecordss = records;

//            // 6. Выход - Ok
//            rc = 0;
//            return rc;
//        }
//        catch
//        {
//            return rc;
//        }
//    }

//    private static int CompareCouriersByYandexType(Courier courier1, Courier courier2)
//    {
//        if (courier1.YandexType < courier2.YandexType)
//            return -1;
//        if (courier1.YandexType > courier2.YandexType)
//            return 1;
//        return 0;
//    }

//}
