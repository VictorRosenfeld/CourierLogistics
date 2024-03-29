﻿
namespace SQLCLR.DeliveryCover
{
    using SQLCLR.Couriers;
    using SQLCLR.Deliveries;
    using SQLCLR.Log;
    using System;

    /// <summary>
    /// Репозиторий доступных курьеров и такси,
    /// позволяющий извлекать следующего доступного
    /// курьера заданного типа
    /// </summary>
    public class CourierRepository
    {
        /// <summary>
        /// Курьеры, отсортированные по VehicleId
        /// </summary>
        private Courier[] _couriers;

        /// <summary>
        /// Отсортированные способы доставки
        /// </summary>
        private int[] _vehicleId;

        /// <summary>
        /// Диапазоны курьеров одного типа
        /// (Point.X - начальный индекс в _couriers
        ///  Point>Y - конечный  индекс в _couriers 
        /// </summary>
        private Point[] _vehicleRange;

        /// <summary>
        /// Курьеры
        /// </summary>
        public Courier[] Couriers => _couriers;

        /// <summary>
        /// Флаг: true - репозиторий создан; false - репозиторий не создан
        /// </summary>
        public bool IsCreated { get; private set; }

        /// <summary>
        /// Создание репозитория
        /// </summary>
        /// <param name="couriers">Доступные курьеры</param>
        /// <returns>0 - репозиторий создан; иначе - репозиторий не создан</returns>
        public int Create(Courier[] couriers)
        {
            // 1. Инициализация
            int rc = 1;
            IsCreated = false;
            _couriers = null;
            _vehicleId = null;
            _vehicleRange = null;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (couriers == null || couriers.Length <= 0)
                {
                    _couriers = new Courier[0];
                    _vehicleId = new int[0];
                    _vehicleRange = new Point[0];
                    IsCreated = true;
                    return rc = 0;
                }

                // 3.Сортируем курьеров по VehicleId и OrderCount
                rc = 3;
                _couriers = (Courier[])couriers.Clone();
                Array.Sort(_couriers, CompareCourierByVehicleIdAndOrderCount);

                // 4.Строим индек курьеров
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


                _vehicleId[count] = currentVehicleId;

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

                // 5. Выход - Ok
                rc = 0;
                IsCreated = true;
                return rc;
            }
            catch
            {
                return rc;
            }
//#if debug
//            finally
//            {
//                if (IsCreated)
//                {
//                    if (_couriers != null && _couriers.Length > 0)
//                    {
//                        for (int i = 0; i < _couriers.Length; i++)
//                        {
//                            Logger.WriteToLog(901, $"CourierRepository.Create. _couriers[{i}].VehicleId = {_couriers[i].VehicleID}", 0);
//                        }
//                    }

//                    if (_vehicleId != null && _vehicleId.Length > 0)
//                    {
//                        for (int i = 0; i < _vehicleId.Length; i++)
//                        {
//                            Logger.WriteToLog(902, $"CourierRepository.Create. _vehicleId[{i}] = {_vehicleId[i]}", 0);
//                        }
//                    }

//                    if (_vehicleRange != null && _vehicleRange.Length > 0)
//                    {
//                        for (int i = 0; i < _vehicleRange.Length; i++)
//                        {
//                            Logger.WriteToLog(903, $"CourierRepository.Create. _vehicleRange[{i}] = ({_vehicleRange[i].X}, {_vehicleRange[i].Y})", 0);
//                        }
//                    }

//                    Logger.WriteToLog(904, $"CourierRepository exit. rc = {rc}, IsCreated = {IsCreated}", 0);
//                }
//            }
//#endif
        }

        /// <summary>
        /// Извлечение следующего доступного курьера из репозитория
        /// </summary>
        /// <param name="vehicleId">Требуемый способ доставки</param>
        /// <returns>Курьер или null</returns>
        public Courier GetNextCourier(int vehicleId)
        {
            // 1. Инициализация
            
            try
            {
                // 2. Находим диапазон для заданного способа доставки
                if (!IsCreated)
                    return null;
                int index = Array.BinarySearch(_vehicleId, vehicleId);
                if (index < 0)
                    return null;

                // 3. Выбираем следующего доступного курьера
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
        /// Проверка наличия курьера
        /// </summary>
        /// <param name="vehicleId">Требуемый способ доставки</param>
        /// <returns>true - курьер доступен; false - курьер не доступен</returns>
        public bool IstCourierEnabled(int vehicleId)
        {
            // 1. Инициализация
            
            try
            {
                // 2. Находим диапазон для заданного способа доставки
                if (!IsCreated)
                    return false;
                int index = Array.BinarySearch(_vehicleId, vehicleId);
                if (index < 0)
                    return false;

                // 3. Проверяем наличие курьера
                Point pt = _vehicleRange[index];
                if (pt.Y == -1)
                    return true;

                return pt.X <= pt.Y;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Сравнение двух курьеров по VehicleId, OrderCount
        /// </summary>
        /// <param name="courier1">Курьер 1</param>
        /// <param name="courier2">Курьер 2</param>
        /// <returns>-1 - Курьер 1 меньше Курьер2; 0 - Курьер 1 равен Курьер2; 1 - Курьер 1 больше Курьер2</returns>
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

        /// <summary>
        /// Извлечение первого подходящего курьера заданного типа
        /// </summary>
        /// <param name="vehicleId">Заданный способ доставки</param>
        /// <returns>Курьер или null</returns>
        public Courier GetFirstCourier(int vehicleId)
        {
            // 1. Инициализация

            try
            {
                // 2. Проверяем исходные данные
                if (!IsCreated)
                    return null;

                // 3. Извлекаем курьера заданного типа
                int index = Array.BinarySearch(_vehicleId, vehicleId);
                if (index < 0)
                    return null;

                // 4. Извлекаем курьера 
                Point pt = _vehicleRange[index];
                if (pt.Y == -1)
                    return _couriers[pt.X];
                else
                    return _couriers[pt.Y]; 
            }
            catch
            {
                return null;
            }
        }
    }
}
