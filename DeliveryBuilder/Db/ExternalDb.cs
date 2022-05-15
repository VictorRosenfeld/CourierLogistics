
namespace DeliveryBuilder.Db
{
    using System;
    using System.Data;
    using System.Data.SqlClient;

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
    }
}
