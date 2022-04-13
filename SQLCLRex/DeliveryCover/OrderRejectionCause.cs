
namespace SQLCLRex.DeliveryCover
{
    using SQLCLRex.Orders;

    /// <summary>
    /// ������� ������ � �������� ������
    /// </summary>
    public class OrderRejectionCause
    {
        /// <summary>
        /// ID ������
        /// </summary>
        public int OrderId { get; set; }

        /// <summary>
        /// ID ������� ��������
        /// </summary>
        public int VehicleId { get; set; }

        /// <summary>
        /// ������� ������
        /// </summary>
        public OrderRejectionReason Reason { get; set; }

        /// <summary>
        /// ��� �������� DeliveryCheck
        /// </summary>
        public int ErrorCode { get; set; }

        /// <summary>
        /// ��������������� ����������� ������ OrderRejectionCause
        /// </summary>
        /// <param name="orderId">ID ������</param>
        /// <param name="vehicleId">ID ������� ��������</param>
        /// <param name="reason">������� ������</param>
        public OrderRejectionCause(int orderId, int vehicleId, OrderRejectionReason reason)
        {
            OrderId = orderId;
            VehicleId = vehicleId;
            Reason = reason;
            ErrorCode = -2;
        }

        /// <summary>
        /// ��������������� ����������� ������ OrderRejectionCause
        /// </summary>
        /// <param name="orderId">ID ������</param>
        /// <param name="vehicleId">ID ������� ��������</param>
        /// <param name="reason">������� ������</param>
        /// <param name="errorCode">��� �������� DeliveryCheck</param>
        public OrderRejectionCause(int orderId, int vehicleId, OrderRejectionReason reason, int errorCode)
        {
            OrderId = orderId;
            VehicleId = vehicleId;
            Reason = reason;
            ErrorCode = errorCode;
        }
    }
}
