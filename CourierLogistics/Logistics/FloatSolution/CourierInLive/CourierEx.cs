
namespace CourierLogistics.Logistics.FloatSolution.CourierInLive
{
    using CourierLogistics.SourceData.Couriers;
    using System;

    public delegate void CourierEvent(CourierEx sender, CourierEventArgs args);

    /// <summary>
    /// Расширенный курьер
    /// </summary>
    public class CourierEx : Courier
    {
        /// <summary>
        /// Событие завершения доставки
        /// </summary>
        public event CourierEvent OrderDelivered;

        /// <summary>
        /// Событие начала работы
        /// </summary>
        public event CourierEvent WorkStarted;

        /// <summary>
        /// Событие завершения работы
        /// </summary>
        public event CourierEvent WorkEnded;

        /// <summary>
        /// Событие перемещение в точку
        /// </summary>
        public event CourierEvent MovedToPoint;

        /// <summary>
        /// Текущая широта курьера
        /// (-1 если широта не определена)
        /// </summary>
        public double Latitude { get; set; }

        /// <summary>
        /// Текущая долгота курьера
        /// (-1 если долгота не определена)
        /// </summary>
        public double Longitude { get; set; }

        /// <summary>
        /// Время начала последнего действия
        /// </summary>
        public DateTime LastActionTime { get; private set; }

        /// <summary>
        /// Последнее действие выполненное курьером
        /// </summary>
        public CourierAction LastAction { get; private set; }

        /// <summary>
        /// Время начала или завершения последней отгрузки
        /// </summary>
        public DateTime LastDeliveryTime  { get; private set; }

        /// <summary>
        /// Последняя начатая или завершенная отгрузка
        /// </summary>
        public CourierDeliveryInfo LastDelivery  { get; private set; }

        /// <summary>
        /// Время прибытия курьера из текущей точки
        /// в заданную (например, магазин)
        /// </summary>
        public DateTime ArrivalTime  { get; set; }

        /// <summary>
        /// Индекс типа курьера
        /// </summary>
        public int CourierTypeIndex  { get; set; }

        /// <summary>
        /// Индекс магазина, из которого
        /// курьер осуществляет отгрузку
        /// </summary>
        public int ShopIndex  { get; set; } 

        /// <summary>
        /// Параметрический конструктор класса CourierEx
        /// </summary>
        /// <param name="id"></param>
        /// <param name="courierType"></param>
        public CourierEx(int id, ICourierType courierType) : base(id, courierType)
        { }

        /// <summary>
        /// Начало дня
        /// </summary>
        /// <param name="day">День</param>
        /// <returns>0 - обработка успешно завершена; иначе - не удалось начать день</returns>
        public int StartDay(DateTime day)
        {
            // 1. Инициализация
            int rc = 1;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (IsTaxi)
                    return rc = 0;

                // 3. Устанавливаем состояние
                rc = 3;
                Status = CourierStatus.Unknown;

                // 4. Генерируем событие о начале работы курьера
                rc = 4;
                DateTime workStartTime = day.Date.Add(WorkStart);
                CourierEventArgs startEventArgs = new CourierEventArgs(workStartTime, null, Latitude, Longitude);
                WorkStarted?.Invoke(this, startEventArgs);

                // 5. Генерируем событие о конце работы курьера
                rc = 5;
                DateTime workEndTime = day.Date.Add(WorkEnd);
                CourierEventArgs endEventArgs = new CourierEventArgs(workEndTime, null, 0, 0);
                WorkEnded?.Invoke(this, endEventArgs);

                // 6. Выход - Ok
                rc = 0;
                return rc;
            }
            catch
            {
                return rc;
            }
        }

        /// <summary>
        /// Начать работу в заданное время в заданной точке
        /// </summary>
        /// <param name="beginTime">Время начала работы</param>
        /// <param name="latitude">Широта стартовой точки</param>
        /// <param name="longitude">Долгота стартовой точки</param>
        /// <returns></returns>
        public int BeginWork(DateTime beginTime, double latitude, double longitude)
        {
            // 1. Инициализация
            int rc = 1;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (IsTaxi)
                    return rc;

                LastActionTime = beginTime;
                LastAction = CourierAction.WorkStated;

                // 3. Устанавливаем текущие координаты и делаем курьера готовым к работе
                rc = 3;
                Latitude = latitude;
                Longitude = longitude;
                Status = CourierStatus.Ready;
                LastDeliveryStart = beginTime;
                LastDeliveryEnd = beginTime;

                //// 4. Генерируем событие событие
                //rc = 4;
                //WorkStarted?.Invoke(this, new CourierEventArgs(beginTime, null, latitude, longitude));

                // 5. Выход - Ok
                rc = 0;
                return rc;
            }
            catch
            {
                return rc;
            }
        }

        /// <summary>
        /// Завершение работы
        /// </summary>
        /// <param name="endTime">Время завершения</param>
        /// <param name="latitude">Широта</param>
        /// <param name="longitude">Долгота</param>
        /// <returns>0 - работа завершена; работа не завершена</returns>
        public int EndWork(DateTime endTime, double latitude, double longitude)
        {
            // 1. Инициализация
            int rc = 1;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (IsTaxi)
                    return rc;

                LastActionTime = endTime;
                LastAction = CourierAction.WorkEnded;

                // 3. Устанавливаем текущие координаты и меняем статус курьера
                rc = 3;
                if (latitude != 0)
                    Latitude = latitude;
                if (longitude != 0)
                    Longitude = longitude;
                Status = CourierStatus.WorkEnded;

                //// 4. Генерируем событие событие
                //rc = 4;
                //WorkEnded?.Invoke(this, new CourierEventArgs(endTime, null, latitude, longitude));

                // 5. Выход - Ok
                rc = 0;
                return rc;
            }
            catch
            {
                return rc;
            }

        }

        /// <summary>
        /// Начать перемщение в заданную точку
        /// </summary>
        /// <param name="startTime">Время начала перемещения</param>
        /// <param name="latitude">Широта точки, в которую следует переместиться</param>
        /// <param name="longitude">Долгота точки, в которую следует переместиться</param>
        /// <returns></returns>
        public int BeginMoveToPoint(DateTime startTime, double latitude, double longitude)
        {
            // 1. Инициализация
            int rc = 1;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (Status != CourierStatus.Ready)
                    return rc;
                if (IsTaxi)
                    return rc;

                LastActionTime = startTime;
                LastAction = CourierAction.BeginMoveToPoint;

                // 3. Вычисляем время необходимое для перемещения в новую точку, час
                rc = 3;
                double dt = FloatSolutionParameters.DISTANCE_ALLOWANCE * Helper.Distance(this.Latitude, this.Longitude, latitude, longitude) / CourierType.AverageVelocity;
                DateTime arrivalTime = startTime.AddHours(dt);
                LastDeliveryStart = startTime;
                LastDeliveryEnd = startTime;

                Status = CourierStatus.MoveToPoint;

                // 4. Генерируем событие
                rc = 4;
                MovedToPoint?.Invoke(this, new CourierEventArgs(arrivalTime, null, latitude, longitude));

                // 5. Выход - Ok
                rc = 0;
                return rc;
            }
            catch
            {
                return rc;
            }
        }
      
        /// <summary>
        /// Завершение перемещения в заданную точку
        /// </summary>
        /// <param name="endTime">Время прибытия в звдвнную точку</param>
        /// <param name="latitude">Широта</param>
        /// <param name="longitude">Долгота</param>
        /// <returns></returns>
        public int EndMoveToPoint(DateTime endTime, double latitude, double longitude)
        {
            // 1. Инициализация
            int rc = 1;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (IsTaxi)
                    return rc;
                if (Status != CourierStatus.MoveToPoint)
                    return rc;

                LastActionTime = endTime;
                LastAction = CourierAction.EndMoveToPoint;

                // 3. Меняем местоположение и состояние курьера
                rc = 3;
                Latitude = latitude;
                Longitude = longitude;
                LastDeliveryEnd = endTime;
                Status = CourierStatus.Ready;

                // 4. Выход - Ok
                rc = 0;
                return rc;
            }
            catch
            {
                return rc;
            }
        }

        /// <summary>
        /// Начать доставку 
        /// </summary>
        /// <param name="startTime">Время начала доставки</param>
        /// <param name="delivery">Отгрузка, которую следует начать</param>
        /// <returns></returns>
        public int BeginDelivery(DateTime startTime, CourierDeliveryInfo delivery)
        {
            // 1. Инициализация
            int rc = 1;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                //if (Id == 521)
                //    rc = rc;
                if (Status != CourierStatus.Ready)
                    return rc;
                if (delivery == null)
                    return rc;
                if (delivery.DeliveryCourier != this)
                    return rc;

                LastActionTime = startTime;
                LastAction = CourierAction.BeginDelivery;

                LastDeliveryTime = startTime;
                LastDelivery = delivery;

                // 3. Помечаем все заказы, как доставленные и
                //    определяем координаты конечной точки маршрута
                rc = 3;
                delivery.ShippingOrder.Completed = true;
                double lastLatitude = delivery.ShippingOrder.Latitude;
                double lastLongitude = delivery.ShippingOrder.Longitude;
             
                if (delivery.DeliveredOrders != null &&  delivery.DeliveredOrders.Length > 0)
                {
                    foreach (CourierDeliveryInfo di in delivery.DeliveredOrders)
                    {
                        di.Completed = true;
                        lastLatitude = di.ShippingOrder.Latitude;
                        lastLongitude = di.ShippingOrder.Longitude;
                    }
                }

                // 4. Генерируем событие завершения доставки
                rc = 4;
                // ?=?=?=?=?????????????? ===================
                if (!IsTaxi)
                {
                    Status = CourierStatus.DeliversOrder;
                    double deliveryMinutes = delivery.DeliveryTime + CourierType.HandInTime;
                    // ((((( Изменено 18.08.2020 (vrr)
                    //DateTime deliveredTime = startTime.AddMinutes(deliveryMinutes);
                    DateTime deliveredTime = startTime.Add(delivery.CourierArrivalTime).AddMinutes(deliveryMinutes);
                    // )))) Изменено 18.08.2020 (vrr)

                    OrderDelivered?.Invoke(this, new CourierEventArgs(deliveredTime, delivery, lastLatitude, lastLongitude));
                }
                else
                {
                    DateTime deliveredTime = startTime.AddMinutes(delivery.DeliveryTime);
                    OrderDelivered?.Invoke(this, new CourierEventArgs(deliveredTime, delivery, lastLatitude, lastLongitude));
                }

                // 5. Выход - Ok
                rc = 0;
                return rc;
            }
            catch
            {
                return rc;
            }
        }

        /// <summary>
        /// Завершить доставку
        /// </summary>
        /// <param name="endTime">Время завершения доставки</param>
        /// <param name="delivery">Отгрузка, которую следует начать</param>
        /// <returns></returns>
        public int EndDelivery(DateTime endTime, CourierDeliveryInfo delivery)
        {
            // 1. Инициализация
            int rc = 1;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (IsTaxi)
                    return rc;
                if (delivery == null)
                    return rc;
                //if (delivery.DeliveryCourier != this)
                //    return rc;
                LastActionTime = endTime;
                LastAction = CourierAction.EndDelivery;

                LastDeliveryTime = endTime;
                LastDelivery = delivery;

                // 3. Определяем координаты последней точки маршрута
                rc = 3;
                CourierDeliveryInfo lastDelivery = null;
             
                if (delivery.DeliveredOrders != null &&  delivery.DeliveredOrders.Length > 0)
                {
                    lastDelivery = delivery.DeliveredOrders[delivery.DeliveredOrders.Length - 1];
                }
                else
                {
                    lastDelivery = delivery;
                }

                Latitude = lastDelivery.ShippingOrder.Latitude;
                Longitude = lastDelivery.ShippingOrder.Longitude;

                // 4. Меняем статус курьера
                rc = 4;
                Status = CourierStatus.Ready;

                // 5. Выход - Ok
                rc = 0;
                return rc;
            }
            catch
            {
                return rc;
            }
        }

        /// <summary>
        /// Время перемещения в заданную точку, мин
        /// </summary>
        /// <param name="targetLatitude">Широта целевой точки</param>
        /// <param name="targetLongitude">Долгота целевой точки</param>
        /// <returns>Время перемещения, мин</returns>
        public double GetArrivalTime(double targetLatitude, double targetLongitude)
        {
            return 60 * (FloatSolutionParameters.DISTANCE_ALLOWANCE * Helper.Distance(Latitude, Longitude, targetLatitude, targetLongitude) / CourierType.AverageVelocity);
        }
    }
}
