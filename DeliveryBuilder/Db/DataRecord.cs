
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
        /// Handle диалога
        /// </summary>
        public Guid ConversationHandle { get; private set; }

        /// <summary>
        /// Порядковый номер сообщения в диалоге
        /// </summary>
        public long MessageSequenceNumber { get; private set; }

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
        /// <param name="conversationHandle">Handle диалога</param>
        /// <param name="messageSequenceNumber">Порядковый номер сообщения в диалоге</param>
        /// <param name="messageTypeName">Message Type сообщения</param>
        /// <param name="messageBody">Message Body</param>
        public DataRecord(long queuingOrder, Guid conversationHandle, long messageSequenceNumber, string messageTypeName, string messageBody)
        {
            QueuingOrder = queuingOrder;
            ConversationHandle = conversationHandle;
            MessageSequenceNumber = messageSequenceNumber;
            MessageTypeName = messageTypeName;
            MessageBody = messageBody;
        }
    }
}
