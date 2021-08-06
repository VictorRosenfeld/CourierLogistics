
namespace SQLCLR.DeliveryCover
{
    using SQLCLR.Orders;

    /// <summary>
    /// ѕричина отказа в доставке заказа
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
        /// ѕричина отказа
        /// </summary>
        public OrderRejectionReason Reason { get; set; }

        /// <summary>
        /// ѕараметрический конструктор класса OrderRejectionCause
        /// </summary>
        /// <param name="orderId">ID заказа</param>
        /// <param name="vehicleId">ID способа доставки</param>
        /// <param name="reason">ѕричина отказа</param>
        public OrderRejectionCause(int orderId, int vehicleId, OrderRejectionReason reason)
        {
            OrderId = orderId;
            VehicleId = vehicleId;
            Reason = reason;
        }
    }
}
