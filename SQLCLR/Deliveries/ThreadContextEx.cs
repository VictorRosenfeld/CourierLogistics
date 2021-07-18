
namespace SQLCLR.Deliveries
{
    using SQLCLR.Couriers;
    using SQLCLR.Orders;
    using SQLCLR.Shops;
    using System;
    using System.Threading;

    /// <summary>
    /// �������� ����������� ��������
    /// � ��������� ������
    /// </summary>
    public class ThreadContextEx : ThreadContext
    {
        /// <summary>
        /// ��������������� ����� ���� ��������� ��������
        /// </summary>
        public long[] DeliveryKeys { get; private set; }

        /// <summary>
        /// ������ ������� ��������������� ������
        /// </summary>
        public int StartOrderIndex  { get; private set; }

        /// <summary>
        /// ��� ��������� ������� �������������� �������
        /// </summary>
        public int OrderIndexStep  { get; private set; }

        /// <summary>
        /// ��������������� ����������� ������ ThreadContextEx
        /// </summary>
        /// <param name="serviceId">ID logisticsService</param>
        /// <param name="calcTime">����� �������</param>
        /// <param name="maxRouteLength">������������ ����� ����������� ���������</param>
        /// <param name="shop">�������</param>
        /// <param name="orders">������ ��������</param>
        /// <param name="courier">������</param>
        /// <param name="geoData">���-������</param>
        /// <param name="syncEvent">������ �������������</param>
        /// <param name="deliveryKeys">��������������� ����� ���� ��������� ��������</param>
        /// <param name="startOrderIndex">������ ������� ��������������� ������ � ������� orders</param>
        /// <param name="orderIndexStep">��� ��������� ������� �������������� ������� � ������� orders</param>
        public ThreadContextEx(int serviceId, 
                               DateTime calcTime, 
                               int maxRouteLength, 
                               Shop shop, Order[] orders, 
                               Courier courier, 
                               Point[,] geoData, 
                               ManualResetEvent syncEvent,
                               long[] deliveryKeys,
                               int startOrderIndex,
                               int orderIndexStep) : base(serviceId, calcTime, maxRouteLength, shop, orders, courier, geoData, syncEvent)
        {
            DeliveryKeys = deliveryKeys;
            StartOrderIndex = startOrderIndex;
            OrderIndexStep = orderIndexStep;
        }

        /// <summary>
        /// ��������������� ����������� ������ ThreadContextEx
        /// </summary>
        /// <param name="context">�������� ����������� ��������</param>
        /// <param name="syncEvent">������ �������������</param>
        /// <param name="deliveryKeys">��������������� ����� ���� ��������� ��������</param>
        /// <param name="startOrderIndex">������ ������� ��������������� ������ � ������� orders</param>
        /// <param name="orderIndexStep">��� ��������� ������� �������������� ������� � ������� orders</param>
        public ThreadContextEx(ThreadContext context,
                               ManualResetEvent syncEvent,
                               long[] deliveryKeys,
                               int startOrderIndex,
                               int orderIndexStep) : base(context.ServiceId, context.CalcTime, context.MaxRouteLength, context.ShopFrom, context.Orders, context.ShopCourier, context.GeoData, syncEvent)
        {
            DeliveryKeys = deliveryKeys;
            StartOrderIndex = startOrderIndex;
            OrderIndexStep = orderIndexStep;
        }
    }
}
