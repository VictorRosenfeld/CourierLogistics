
namespace DeliveryBuilder.Db
{
    using DeliveryBuilder.BuilderParameters;
    using DeliveryBuilder.Couriers;
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Data.SqlTypes;
    using System.Xml;
    using System.Xml.Serialization;

    /// <summary>
    /// Работа с БД LSData
    /// </summary>
    public class LSData
    {
        /// <summary>
        /// Строка подключения
        /// </summary>
        public string ConnectionString { get; private set; }

        /// <summary>
        /// Последнее прерывание
        /// </summary>
        public Exception LastException { get; private set; }

        /// <summary>
        /// Подключение к БД
        /// </summary>
        private SqlConnection connection;

        /// <summary>
        /// Подключение к БД
        /// </summary>
        public SqlConnection Connection => connection;

        /// <summary>
        /// Соединение открыто ?
        /// </summary>
        /// <returns>true - открыто; false - не открыто</returns>
        public bool IsOpen()
        {
            return connection != null && connection.State == ConnectionState.Open;
        }

        /// <summary>
        /// Параметрический конструктор класса LSData
        /// </summary>
        /// <param name="connectionString"></param>
        public LSData(string connectionString)
        {
            ConnectionString = connectionString;
        }

        public void Open()
        {
            // 1. Инициализация
            Close();
            LastException = null;

            try
            {
                connection = new SqlConnection(ConnectionString);
                connection.Open();
            }
            catch (Exception ex)
            {
                LastException = ex;
            }
        }

        /// <summary>
        /// Закрытие соединения
        /// </summary>
        public void Close()
        {
            if (connection != null)
            {
                try
                {
                    connection.Dispose();
                    connection = null;
                }
                catch
                { }
            }
        }

        /// <summary>
        /// Текст последнего сообщения об ошибке
        /// </summary>
        /// <returns></returns>
        public string GetLastErrorMessage()
        {
            if (LastException == null)
            { return null; }
            if (LastException.InnerException == null)
            { return LastException.Message; }

            return LastException.InnerException.Message;
        }

        #region tblServices

        /// <summary>
        /// Выбор параметров сервиса логистики
        /// </summary>
        private const string tblServices_SelectConfig = "SELECT srvConfig FROM tblServices WHERE srvEnabled = 1 AND srvID = {0}";

        /// <summary>
        /// Выбор параметров сервиса логистики
        /// </summary>
        /// <param name="serviceId">ID сервиса</param>
        /// <returns>Параметры или null</returns>
        public BuilderConfig SelectConfig(int serviceId)
        {
            // 1. Иициализация
            LastException = null;

            try
            {
                // 2. Проверяем исходные данные
                if (!IsOpen())
                    return null;

                // 3. Извлекаем параметры сервиса логистики
                using (SqlCommand cmd = new SqlCommand(string.Format(tblServices_SelectConfig, serviceId), connection))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (!reader.Read())
                        return null;
                    SqlXml dataXml = reader.GetSqlXml(0);
                    using (XmlReader xmlReader = dataXml.CreateReader())
                    {
                        XmlSerializer serializer = new XmlSerializer(typeof(BuilderConfig));
                        BuilderConfig config = null;
                        config = (BuilderConfig)serializer.Deserialize(xmlReader);
                        return config;
                    }
                }
            }
            catch (Exception ex)
            {
                LastException = ex;
                return null;
            }
        }

        #endregion tblServices

        #region tblAverageDeliveryCost

        /// <summary>
        /// Выбор порогов для среднего времени доставки одного заказа
        /// </summary>
        private const string tblAverageDeliveryCost_Select = "SELECT * FROM tblAverageDeliveryCost";

        /// <summary>
        /// Выбор порогов для средней стоимости доставки заказа
        /// </summary>
        /// <param name="records">Записи таблицы tblAverageDeliveryCost</param>
        /// <returns>0 - пороги выбраны; иначе - попроги не выбраны</returns>
        public int SelectThresholds(out AverageDeliveryCostRecord[] records)
        {
            // 1. Инициализация
            int rc = 1;
            records = null;
            LastException = null;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (!IsOpen())
                    return rc;

                // 3. Выбираем заказы
                rc = 3;
                AverageDeliveryCostRecord[] allRecords = new AverageDeliveryCostRecord[30000];
                int count = 0;

                using (SqlCommand cmd = new SqlCommand(tblAverageDeliveryCost_Select, connection))
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

                // 4. Выход - Ok
                rc = 0;
                return rc;
            }
            catch (Exception ex)
            {
                LastException = ex;
                return rc;
            }
        }

        #endregion tblAverageDeliveryCost

        #region tblVehicles

        /// <summary>
        /// Выбор спосбов доставки для заданного сервиса логистики
        /// </summary>
        private const string tblVehicles_Select = "SELECT tblVehicles.* " +
                                                  "FROM tblVehicles INNER JOIN " +
                                                          "tblServiceVehicles ON tblVehicles.vhsID = tblServiceVehicles.svhVehicleID " +
                                                  "WHERE (tblServiceVehicles.svhCfgServiceID = {0}) AND (tblVehicles.vhsEnabled = 1)";

        /// <summary>
        /// Выбор параметров используемых способов доставки
        /// </summary>
        /// <param name="vehicleTypes">ID способов доставки</param>
        /// <param name="connection">Открытое соединение</param>
        /// <param name="dataRecords">Параметры способов доставки или null</param>
        /// <returns>0 - параметры выбраны; иначе - параметры не выбраны</returns>
        private int SelectServiceVehicles(int serviceId, out VehiclesRecord[] dataRecords)
        {
            // 1. Инициализация
            int rc = 1;
            dataRecords = null;
            LastException = null;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (!IsOpen())
                    return rc;

                // 3. Цикл чтения записей
                rc = 3;
                string sqlText = string.Format(tblVehicles_Select, serviceId);

                // 4. Выбираем данные способов доставки
                rc = 4;
                VehiclesRecord[] records = new VehiclesRecord[128];
                int count = 0;

                using (SqlCommand cmd = new SqlCommand(sqlText, connection))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        // 5.1 Выбираем индексы колонок в выборке
                        rc = 51;
                        int iVehicleID = reader.GetOrdinal("vhsID");
                        int iEnabled = reader.GetOrdinal("vhsEnabled");
                        int iIsTaxi = reader.GetOrdinal("vhsIsTaxi");
                        int iData = reader.GetOrdinal("vhsData");
                        int iDescription = reader.GetOrdinal("vhsDescription");
                        XmlSerializer serializer = new XmlSerializer(typeof(CourierTypeData));

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
                            if (count >= records.Length)
                            { Array.Resize(ref records, records.Length + 100); }

                            records[count++] =
                                new VehiclesRecord(reader.GetInt32(iVehicleID),
                                                      reader.GetBoolean(iEnabled),
                                                      reader.GetBoolean(iIsTaxi),
                                                      ctd,
                                                      reader.IsDBNull(iDescription) ? null : reader.GetString(iDescription));
                        }
                    }
                }

                if (count < records.Length)
                {
                    Array.Resize(ref records, count);
                }

                dataRecords = records;

                // 5. Выход - Ok
                rc = 0;
                return rc;
            }
            catch (Exception ex)
            {
                LastException = ex;
                return rc;
            }
        }

        #endregion tblVehicles

    }
}
