
namespace SQLCLRex.DeliveryCover
{
    using SQLCLRex.Orders;

    /// <summary>
    /// Причина отказа в доставке заказа
    /// </summary>
    public class OrderRejectionCause
    {
        /// <summary>
        /// ID заказа
        /// </summary>
        public int OrderId { get; set; }

        /// <summary>
        /// ID способа доставки
        /// </summary>
        public int VehicleId { get; set; }

        /// <summary>
        /// Причина отказа
        /// </summary>
        public OrderRejectionReason Reason { get; set; }

        /// <summary>
        /// Код возврата DeliveryCheck
        /// </summary>
        public int ErrorCode { get; set; }

        /// <summary>
        /// Параметрический конструктор класса OrderRejectionCause
        /// </summary>
        /// <param name="orderId">ID заказа</param>
        /// <param name="vehicleId">ID способа доставки</param>
        /// <param name="reason">Причина отказа</param>
        public OrderRejectionCause(int orderId, int vehicleId, OrderRejectionReason reason)
        {
            OrderId = orderId;
            VehicleId = vehicleId;
            Reason = reason;
            ErrorCode = -2;
        }

        /// <summary>
        /// Параметрический конструктор класса OrderRejectionCause
        /// </summary>
        /// <param name="orderId">ID заказа</param>
        /// <param name="vehicleId">ID способа доставки</param>
        /// <param name="reason">Причина отказа</param>
        /// <param name="errorCode">Код возврата DeliveryCheck</param>
        public OrderRejectionCause(int orderId, int vehicleId, OrderRejectionReason reason, int errorCode)
        {
            OrderId = orderId;
            VehicleId = vehicleId;
            Reason = reason;
            ErrorCode = errorCode;
        }
    }
}
