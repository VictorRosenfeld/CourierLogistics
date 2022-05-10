
namespace DeliveryBuilder.Db
{
    using DeliveryBuilder.BuilderParameters;
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
    }
}
