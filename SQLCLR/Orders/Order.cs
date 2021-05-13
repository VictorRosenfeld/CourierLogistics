
namespace SQLCLR.Orders
{
    using System;

    /// <summary>
    /// �����
    /// </summary>
    internal class Order
    {
        /// <summary>
        /// Id ������
        /// </summary>
        public int Id { get; private set; }

        /// <summary>
        /// ��������� ������
        /// </summary>
        public int Status { get; set; }

        /// <summary>
        /// Id �������� � ������� �������� �����
        /// </summary>
        public int ShopId { get; set; }

        /// <summary>
        /// ��� ������
        /// </summary>
        public double Weight { get; set; }

        /// <summary>
        /// ������ ����� ��������
        /// </summary>
        public double Latitude { get; set; }

        /// <summary>
        /// ������� ����� ��������
        /// </summary>
        public double Longitude { get; set; }

        /// <summary>
        /// ����-����� ������ ������
        /// </summary>
        public DateTime AssembledDate { get; set; }

        /// <summary>
        /// ����-����� ����������� ������ � �������
        /// </summary>
        public DateTime ReceiptedDate { get; set; }

        /// <summary>
        /// ����� ������ ��������� ��������
        /// </summary>
        public DateTime DeliveryTimeFrom { get; set; }

        /// <summary>
        /// ����� ����� ��������� ��������
        /// </summary>
        public DateTime DeliveryTimeTo { get; set; }

        /// <summary>
        /// ����: true - ����� ��������; false - ����� �� ��������
        /// </summary>
        public bool Completed { get; set; }

        /// <summary>
        /// ������� ������ � ��������
        /// </summary>
        public int RejectionReason { get; set; }

        /// <summary>
        /// ��������� ������
        /// </summary>
        public int Priority { get; set; }

        /// <summary>
        /// ���� ������ �������� ��������� �����������:
        /// true - �������� ��������; false - �������� �������� � ����
        /// </summary>
        public bool TimeCheckDisabled { get; set; }

        /// <summary>
        /// ID ���������� �������� �������� ������ (VehicleID),
        /// �������������� �� �����������
        /// </summary>
        public int[] VehicleTypes { get; set; }

        /// <summary>
        /// ���������������� ToString()
        /// </summary>
        /// <returns>��������� ������������� ����������</returns>
        public override string ToString()
        {
            return $"{Id}, {Status}, {Completed}";
        }

        /// <summary>
        /// �������� ������� �������� �� ����������� �������� ������
        /// </summary>
        /// <param name="vehicleType">����������� ������ ��������</param>
        /// <returns>true - ������ �������� �������� ����������; false - ������ �������� �� �������� ����������</returns>
        public bool IsVehicleTypeEnabled(int vehicleType)
        {
            if (VehicleTypes == null || VehicleTypes.Length <= 0)
                return false;
            return Array.BinarySearch(VehicleTypes, vehicleType) >= 0;
        }

        /// <summary>
        /// ��������������� ����������� ������ Order
        /// </summary>
        /// <param name="id">Id ������</param>
        public Order(int id)
        {
            Id = id;
        }
    }
}
