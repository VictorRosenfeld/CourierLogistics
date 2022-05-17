
namespace DeliveryBuilder.Deliveries
{
    using DeliveryBuilder.Couriers;
    //using DeliveryBuilder.MaxOrdersOfRoute;
    using DeliveryBuilder.Orders;
    using DeliveryBuilder.SalesmanProblemLevels;
    using DeliveryBuilder.Shops;
    using System;
    using System.Threading;

    /// <summary>
    /// �������� ����������� ��������
    /// � ��������� ������
    /// </summary>
    public class CalcThreadContext
    {
        public CalcConfig Config { get; set; }

        /// <summary>
        /// ������ �������������
        /// </summary>
        public ManualResetEvent SyncEvent { get; set; }

        #region ��������� �������

        /// <summary>
        /// ��� �������
        /// </summary>
        public string ServerName { get; private set; }

        /// <summary>
        /// ��� ��
        /// </summary>
        public string DbName { get; private set; }

        /// <summary>
        /// ID LogisticsService
        /// </summary>
        public int ServiceId { get; private set; }

        /// <summary>
        /// �������
        /// </summary>
        public Shop ShopFrom { get; private set; }

        /// <summary>
        /// ������, ��� ������� ��������� ��������
        /// </summary>
        public Order[] Orders { get; private set; }

        /// <summary>
        /// ���������� �������
        /// </summary>
        public int OrderCount => (Orders == null ? 0 : Orders.Length);

        /// <summary>
        /// ������, c ������� �������� ����������� ��������
        /// </summary>
        public Courier ShopCourier { get; private set; }

        /// <summary>
        /// ����������� �� ����� �������� �� ����� �������
        /// (��� ���������� ������ ���������)
        /// </summary>
        public SalesmanLevels Limitations { get; set; }

        /// <summary>
        /// �����, �� ������� ��������� ��������
        /// </summary>
        public DateTime CalcTime { get; set; }

        ///// <summary>
        ///// Geo-������ ����� ������ �����:
        /////     GeoData[i,j].X  - distance
        /////     GeoData[i,j].Y  - distance
        ///// ���
        /////     i, j ������ ��� ����� OrderCount
        /////     i ������������� Order[i];
        /////     i = OrderCount ������������� ShopFrom
        ///// </summary>
        //public Point[,] GeoData { get; set; }

        #endregion ��������� �������

        #region ��������� ������ ����������� ��������

        /// <summary>
        /// ����������� ��������
        /// </summary>
        public CourierDeliveryInfo[] Deliveries { get; set; }

        /// <summary>
        /// ����� ����������� ��������
        /// </summary>
        public int DeliveryCount => (Deliveries == null ? 0 : Deliveries.Length);

        /// <summary>
        /// ��� ��������
        /// </summary>
        public int ExitCode { get; set; }

        #endregion ��������� ������ ����������� ��������

        /// <summary>
        /// ��������������� ����������� ������ CalcThreadContext
        /// </summary>
        /// <param name="serverName">��� �������</param>
        /// <param name="dbName">��� ��</param>
        /// <param name="serviceId">ID logisticsService</param>
        /// <param name="calcTime">����� �������</param>
        /// <param name="maxRouteLength">������������ ����� ����������� ���������</param>
        /// <param name="shop">�������</param>
        /// <param name="orders">������ ��������</param>
        /// <param name="courier">������</param>
        /// <param name="limitations">����� ��������� � ����������� �� ����� ������� ��� ������ ��������</param>
        /// <param name="syncEvent">������ �������������</param>
        public CalcThreadContext(string serverName, string dbName, int serviceId, DateTime calcTime, Shop shop, Order[] orders, Courier courier, SalesmanLevels limitations, ManualResetEvent syncEvent)
        {
            ServerName = serverName;
            DbName = dbName;
            ServiceId = serviceId;
            CalcTime = calcTime;
            ShopFrom = shop;
            Orders = orders;
            ShopCourier = courier;
            Limitations = limitations;
            SyncEvent = syncEvent;
            ExitCode = -1;
        }
    }
}
