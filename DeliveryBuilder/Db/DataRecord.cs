
namespace DeliveryBuilder.Db
{
    using System;

    /// <summary>
    /// Сообщение с данными из очереди внешнего сервиса
    /// </summary>
    public class DataRecord
    {
        /// <summary>
        /// Порядковый номер сообщения в очереди
        /// </summary>
        public long QueuingOrder { get; private set; }

        /// <summary>
        /// Message Type сообщения
        /// </summary>
        public string MessageTypeName { get; private set; }

        /// <summary>
        /// Message Body
        /// </summary>
        public string MessageBody { get; private set; }

        /// <summary>
        /// Параметрический конструктор класса DataRecord
        /// </summary>
        /// <param name="queuingOrder">Порядковый номер сообщения в очереди</param>
        /// <param name="messageTypeName">Message Type сообщения</param>
        /// <param name="messageBody">Message Body</param>
        public DataRecord(long queuingOrder, string messageTypeName, string messageBody)
        {
            QueuingOrder = queuingOrder;
            MessageTypeName = messageTypeName;
            MessageBody = messageBody;
        }
    }
}
