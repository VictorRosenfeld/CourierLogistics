
namespace SQLCLR.DeliveryCover
{
    using SQLCLR.Couriers;
    using SQLCLR.Deliveries;
    using System;

    /// <summary>
    /// ����������� ��������� �������� � �����,
    /// ����������� ��������� ���������� ����������
    /// ������� ��������� ����
    /// </summary>
    public class CourierRepository
    {
        /// <summary>
        /// �������, ��������������� �� VehicleId
        /// </summary>
        private Courier[] _couriers;

        /// <summary>
        /// ��������������� �������� ��������
        /// </summary>
        private int[] _vehicleId;

        /// <summary>
        /// ��������� �������� ������ ����
        /// (Point.X - ��������� ������ � _couriers
        ///  Point>Y - ��������  ������ � _couriers 
        /// </summary>
        private Point[] _vehicleRange;

        /// <summary>
        /// �������
        /// </summary>
        public Courier[] Couriers => _couriers;

        /// <summary>
        /// ����: true - ����������� ������; false - ����������� �� ������
        /// </summary>
        public bool IsCreated { get; private set; }

        /// <summary>
        /// �������� �����������
        /// </summary>
        /// <param name="couriers">��������� �������</param>
        /// <returns>0 - ����������� ������; ����� - ����������� �� ������</returns>
        public int Create(Courier[] couriers)
        {
            // 1. �������������
            int rc = 1;
            IsCreated = false;
            _couriers = null;
            _vehicleId = null;
            _vehicleRange = null;

            try
            {
                // 2. ��������� �������� ������
                rc = 2;
                if (couriers == null || couriers.Length <= 0)
                    return rc;

                // 3.��������� �������� �� VehicleId � OrderCount
                rc = 3;
                _couriers = (Courier[])couriers.Clone();
                Array.Sort(_couriers, CompareCourierByVehicleIdAndOrderCount);

                // 4.������ ����� ��������
                rc = 4;
                _vehicleId = new int[_couriers.Length];
                _vehicleRange = new Point[_couriers.Length];

                int currentVehicleId = _couriers[0].VehicleID;
                int startIndex = 0;
                int endIndex = 0;
                int count = 0;

                for (int i = 1; i < _couriers.Length; i++)
                {
                    if (_couriers[i].VehicleID == currentVehicleId)
                    {
                        endIndex = i;
                    }
                    else
                    {
                        _vehicleId[count] = currentVehicleId;

                        if (_couriers[i].IsTaxi)
                        {
                            _vehicleRange[count].X = startIndex;
                            _vehicleRange[count++].Y = -1;

                        }
                        else
                        {
                            _vehicleRange[count].X = startIndex;
                            _vehicleRange[count++].Y = endIndex;
                        }

                        startIndex = i;
                        endIndex = i;
                        currentVehicleId = _couriers[i].VehicleID;
                    }
                }

                if (_couriers[_couriers.Length - 1].IsTaxi)
                {
                    _vehicleRange[count].X = startIndex;
                    _vehicleRange[count++].Y = -1;

                }
                else
                {
                    _vehicleRange[count].X = startIndex;
                    _vehicleRange[count++].Y = endIndex;
                }

                if (count < _vehicleId.Length)
                {
                    Array.Resize(ref _vehicleId, count);
                    Array.Resize(ref _vehicleRange, count);
                }

                // 5. ����� - Ok
                rc = 0;
                IsCreated = true;
                return rc;
            }
            catch
            {
                return rc;
            }
        }

        /// <summary>
        /// ���������� ���������� ���������� ������� �� �����������
        /// </summary>
        /// <param name="vehicleId">��������� ������ ��������</param>
        /// <returns>������ ��� null</returns>
        public Courier GetNextCourier(int vehicleId)
        {
            // 1. �������������
            
            try
            {
                // 2. ������� �������� ��� ��������� ������� ��������
                if (!IsCreated)
                    return null;
                int index = Array.BinarySearch(_vehicleId, vehicleId);
                if (index < 0)
                    return null;

                // 3. �������� ���������� ���������� �������
                Point pt = _vehicleRange[index];
                if (pt.Y == -1)
                    return _couriers[pt.X];
                if (pt.X <= pt.Y)
                {
                    //Courier c = _couriers[pt.X];
                    //_vehicleRange[index].X++;
                    //return c;
                    return _couriers[_vehicleRange[index].X++];
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// ��������� ���� �������� �� VehicleId, OrderCount
        /// </summary>
        /// <param name="courier1">������ 1</param>
        /// <param name="courier2">������ 2</param>
        /// <returns>-1 - ������ 1 ������ ������2; 0 - ������ 1 ����� ������2; 1 - ������ 1 ������ ������2</returns>
        private static int CompareCourierByVehicleIdAndOrderCount(Courier courier1, Courier courier2)
        {
            if (courier1.VehicleID < courier2.VehicleID)
                return -1;
            if (courier1.VehicleID > courier2.VehicleID)
                return 1;
            if (courier1.OrderCount < courier2.OrderCount)
                return -1;
            if (courier1.OrderCount > courier2.OrderCount)
                return 1;
            return 0;
        }
    }
}
