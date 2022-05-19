
namespace DeliveryBuilder.Db
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Data.SqlTypes;
    using System.IO;
    using System.Text;
    using System.Xml.Serialization;

    /// <summary>
    /// Работа с внешней БД
    /// </summary>
    public class ExternalDb
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
        public ExternalDb(string connectionString)
        {
            ConnectionString = connectionString;
        }

        /// <summary>
        /// Открытие соединения
        /// </summary>
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
        //use s1service

        /// <summary>
        /// Sql-процедура для приёма сообщений
        /// </summary>
        private const string sqlReceive =
            "DECLARE @dataTable TABLE(" +
            "queuing_order bigint, " +
            "conversation_handle UNIQUEIDENTIFIER, " +
            "message_sequence_number bigint, " +
            "message_type_name nvarchar(256), " +
            "message_body varbinary(max));  " +
            "WAITFOR " +
            " (RECEIVE " +
            "   queuing_order, " +
            "   conversation_handle, " +
            "   message_sequence_number, " +
            "   message_type_name, " +
            "   message_body " +
            " FROM {0} " +
            " INTO @dataTable " +
            " WHERE conversation_group_id = '{1}' ), TIMEOUT {2}; " +

            "DECLARE @sql nvarchar(max) = N'' " +
           @"SELECT @sql = @sql + REPLACE('END CONVERSATION ""' + CONVERT(NVARCHAR(36), [conversation_handle]) + " +
           @"'""', NCHAR(34), NCHAR(39)) + NCHAR(59) + NCHAR(13) + NCHAR(10) " +
            "FROM @dataTable WHERE message_type_name = 'http://schemas.microsoft.com/SQL/ServiceBroker/EndDialog'; " +
            "EXEC (@sql); " +

            "DELETE @dataTable WHERE message_type_name = 'http://schemas.microsoft.com/SQL/ServiceBroker/EndDialog'; " +

            "SELECT queuing_order, conversation_handle, message_sequence_number, message_type_name, CAST(message_body as nvarchar(max)) As messageBody FROM @dataTable;";

        /// <summary>
        /// Sql-процедура для отправки команды в отдельном диалоге
        /// </summary>
        private const string sqlSendCmd =
            "DECLARE @send_conversation_handle uniqueidentifier; " +
            "BEGIN DIALOG CONVERSATION @send_conversation_handle " +
            "FROM SERVICE[{0}] TO SERVICE '{0}' ON CONTRACT {1} WITH ENCRYPTION = OFF; " +
            "SEND ON CONVERSATION @send_conversation_handle MESSAGE TYPE[{2}] (@xml_cmd); " +
            "END CONVERSATION @send_conversation_handle; ";

        /// <summary>
        /// Sql-процедура для отправки команды
        /// (Execute @rc = SendCmd @servive_id, @message_type, @xml_cmd, @error_message OUTPUT
        /// </summary>
        private const string sqlSendCmdEx = "dbo.SendCmd";


        /// <summary>
        /// Sql-процедура для плучения данных
        /// (Execute @rc = SendCmd @servive_id, @message_type, @xml_cmd, @error_message OUTPUT
        /// </summary>
        private const string sqlReceiveData = "dbo.ReceiveData";

        /// <summary>
        /// Получение данных из очереди вешнего сервиса
        /// </summary>
        /// <param name="queueName">Имя очереди сервиса</param>
        /// <param name="group_id">conversation_group_id сервиса логистики</param>
        /// <param name="timeout">Время ожидания сообщений в очереди, мсек</param>
        /// <param name="dataRecords">Считанные записи с сообщениями</param>
        /// <returns></returns>
        public int ReceiveData(string queueName, string group_id, int timeout, out DataRecord[] dataRecords)
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
                if (string.IsNullOrWhiteSpace(queueName))
                    return rc;
                if (string.IsNullOrWhiteSpace(group_id))
                    return rc;

                // 3. Цикл чтения записей
                rc = 3;
                string sqlText = string.Format(sqlReceive, queueName, group_id, timeout);
                DataRecord[] records = new DataRecord[1024];
                int count = 0;

                using (SqlCommand cmd = new SqlCommand(sqlText, connection))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        // 3.1 Выбираем индексы колонок в выборке
                        rc = 31;
                        int iQueuingOrder = reader.GetOrdinal("queuing_order");
                        int iConversationHandle = reader.GetOrdinal("conversation_handle");
                        int iMessageSequenceNumber = reader.GetOrdinal("message_sequence_number");
                        int iMessageTypeName = reader.GetOrdinal("message_type_name");
                        int iMessageBody = reader.GetOrdinal("messageBody");

                        while (reader.Read())
                        {
                            // 3.1 Сохраняем запись
                            rc = 31;
                            if (count >= records.Length)
                            { Array.Resize(ref records, records.Length + 100); }

                            records[count++] =
                                new DataRecord(reader.GetInt64(iQueuingOrder),
                                               reader.GetGuid(iConversationHandle),
                                               reader.GetInt64(iMessageSequenceNumber),
                                               reader.GetString(iMessageTypeName),
                                               reader.GetString(iMessageBody));
                        }
                    }
                }

                if (count < records.Length)
                {
                    Array.Resize(ref records, count);
                }

                dataRecords = records;

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

        /// <summary>
        /// Получение данных из ExternalDb
        /// </summary>
        /// <param name="serviceId">ID сервиса логистики</param>
        /// <param name="dataRecords">Считанные записи с сообщениями</param>
        /// <returns>0 - данные получены; иначе - данные не получены</returns>
        public int ReceiveData(int serviceId, out DataRecord[] dataRecords)
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
                DataRecord[] records = new DataRecord[500];
                int count = 0;

                using (SqlCommand cmd = new SqlCommand(sqlReceiveData, connection))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    // @service_id
                    var parameter = cmd.Parameters.Add("@service_id", SqlDbType.Int);
                    parameter.Direction = ParameterDirection.Input;
                    parameter.Value = serviceId;
                    
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        // 3.1 Выбираем индексы колонок в выборке
                        rc = 31;
                        int iQueuingOrder = reader.GetOrdinal("queuing_order");
                        int iMessageTypeName = reader.GetOrdinal("message_type_name");
                        //int iMessageTypeName = reader.GetOrdinal("message_type");
                        int iMessageBody = reader.GetOrdinal("message_body");

                        while (reader.Read())
                        {
                            // 3.2 Сохраняем запись
                            rc = 32;
                            if (count >= records.Length)
                            { Array.Resize(ref records, records.Length + 100); }

                            records[count++] =
                                new DataRecord(reader.GetInt64(iQueuingOrder),
                                               Guid.Empty,
                                               0,
                                               reader.GetString(iMessageTypeName),
                                               reader.GetSqlXml(iMessageBody).Value);
                        }
                    }
                }

                if (count < records.Length)
                {
                    Array.Resize(ref records, count);
                }

                dataRecords = records;

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

        /// <summary>
        /// Отправка команды в отдельном диалоге
        /// </summary>
        /// <param name="serviceName">Имя сервиса</param>
        /// <param name="contractName">Имя контракта</param>
        /// <param name="messageType">Тип сообщения</param>
        /// <param name="xmlMessage">xml-сообщение</param>
        /// <returns>0 - сообщение оправлено; иначе - сообщение не отправлено</returns>
        public int SendXmlCmd(string serviceName, string contractName, string messageType, string xmlMessage)
        {
            // 1. Инициализация
            int rc = 1;
            LastException = null;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (!IsOpen())
                    return rc;
                if (string.IsNullOrWhiteSpace(serviceName))
                    return rc;
                if (string.IsNullOrWhiteSpace(contractName))
                    return rc;
                if (string.IsNullOrWhiteSpace(messageType))
                    return rc;
                if (string.IsNullOrWhiteSpace(xmlMessage))
                    return rc;

                // 3. Отправляем команду
                rc = 3;
                using (MemoryStream ms = new MemoryStream(Encoding.Unicode.GetBytes(xmlMessage)))
                {
                    using (SqlCommand cmd = new SqlCommand(string.Format(sqlSendCmd, serviceName, contractName, messageType), connection))
                    {
                        var parameter = cmd.Parameters.Add("@xml_cmd", SqlDbType.Xml);
                        parameter.Direction = ParameterDirection.Input;
                        parameter.Value = new SqlXml(ms);

                        cmd.ExecuteNonQuery();
                    }
                }

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

        /// <summary>
        /// Отправка команды в отдельном диалоге
        /// </summary>
        /// <param name="serviceName">Имя сервиса</param>
        /// <param name="contractName">Имя контракта</param>
        /// <param name="messageType">Тип сообщения</param>
        /// <param name="messageClass">Сообщение</param>
        /// <returns>0 - сообщение оправлено; иначе - сообщение не отправлено</returns>
        public int SendXmlCmd<T>(string serviceName, string contractName, string messageType, T messageClass) where T: class
        {
            // 1. Инициализация
            int rc = 1;
            LastException = null;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (!IsOpen())
                    return rc;
                if (string.IsNullOrWhiteSpace(serviceName))
                    return rc;
                if (string.IsNullOrWhiteSpace(contractName))
                    return rc;
                if (string.IsNullOrWhiteSpace(messageType))
                    return rc;
                if (messageClass == null)
                    return rc;
                
                // 3. Отправляем команду
                rc = 3;
                XmlSerializer serializer = new XmlSerializer(typeof(T));

                using (StringWriter writer = new StringWriter())
                {
                    serializer.Serialize(writer, messageClass);

                    using (MemoryStream ms = new MemoryStream(Encoding.Unicode.GetBytes(writer.ToString())))
                    {
                        using (SqlCommand cmd = new SqlCommand(string.Format(sqlSendCmd, serviceName, contractName, messageType), connection))
                        {
                            var parameter = cmd.Parameters.Add("@xml_cmd", SqlDbType.Xml);
                            parameter.Direction = ParameterDirection.Input;
                            parameter.Value = new SqlXml(ms);

                            cmd.ExecuteNonQuery();
                        }
                    }
                }

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

        /// <summary>
        /// Отправка команды
        /// </summary>
        /// <param name="serviceId">ID сервиса логистики</param>
        /// <param name="messageType">Тип сообщения</param>
        /// <param name="messageClass">Сериализуемый класс-сообщение</param>
        /// <param name="errorMessage">Сообщение</param>
        /// <returns>0 - сообщение оправлено; иначе - сообщение не отправлено</returns>
        public int SendXmlCmd<T>(int serviceId, string messageType, T messageClass, out string errorMessage) where T: class
        {
            // 1. Инициализация
            int rc = 1;
            LastException = null;
            errorMessage = null;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (!IsOpen())
                    return rc;
                if (string.IsNullOrWhiteSpace(messageType))
                    return rc;
                if (messageClass == null)
                    return rc;
                
                // 3. Отправляем команду
                rc = 3;
                XmlSerializer serializer = new XmlSerializer(typeof(T));

                using (StringWriter writer = new StringWriter())
                {
                    serializer.Serialize(writer, messageClass);

                    using (MemoryStream ms = new MemoryStream(Encoding.Unicode.GetBytes(writer.ToString())))
                    {
                        using (SqlCommand cmd = new SqlCommand(string.Format(sqlSendCmdEx, connection)))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;

                            // @service_id
                            var parameter = cmd.Parameters.Add("@service_id", SqlDbType.Int);
                            parameter.Direction = ParameterDirection.Input;
                            parameter.Value = serviceId;

                            // @message_type
                            parameter = cmd.Parameters.Add("@message_type", SqlDbType.NVarChar, 256);
                            parameter.Direction = ParameterDirection.Input;
                            parameter.Value = messageType;

                            // @error_message
                            var errorParamreter = cmd.Parameters.Add("@error_message", SqlDbType.NVarChar, 4000);
                            errorParamreter.Direction = ParameterDirection.Output;

                            // @xml_cmd
                            parameter = cmd.Parameters.Add("@xml_cmd", SqlDbType.Xml);
                            parameter.Direction = ParameterDirection.Input;
                            parameter.Value = new SqlXml(ms);

                            // return code
                            var returnParameter = cmd.Parameters.Add("@ReturnCode", SqlDbType.Int);
                            returnParameter.Direction = ParameterDirection.ReturnValue;

                            cmd.ExecuteNonQuery();

                            errorMessage = errorParamreter.Value as string;

                            var retCode = returnParameter.Value;
                            if (!(retCode is int))
                                return rc;
                            int rc1 = (int)retCode;
                            if (rc1 != 0)
                                return rc = 10 * rc + rc1;
                        }
                    }
                }

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
    }
}
