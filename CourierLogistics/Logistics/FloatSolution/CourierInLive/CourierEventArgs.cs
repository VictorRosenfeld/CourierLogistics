
namespace CourierLogistics.Logistics.FloatSolution.CourierInLive
{
    using CourierLogistics.SourceData.Couriers;
    using System;

    /// <summary>
    /// Аргументы события OrderDelivered
    /// </summary>
    public class CourierEventArgs
    {
        /// <summary>
        /// Время наступления события
        /// </summary>
        public DateTime EventTime { get; private set; }
       
        /// <summary>
        /// Доставленные заказы
        /// </summary>
        public CourierDeliveryInfo Delivery { get; private set; }

        /// <summary>
        /// Широта точки местонахождения курьера
        /// </summary>
        public double Latitude { get; private set; }

        /// <summary>
        /// Долгота точки местонахождения курьера
        /// </summary>
        public double Longitude { get; private set; }

        /// <summary>
        /// Параметрический конструктор класса OrderDeliveredEventArgs
        /// </summary>
        /// <param name="eventTime">Время наступления события</param>
        /// <param name="delivery">Доставленные товары</param>
        /// <param name="latitude">Широта точки местонахождения курьера</param>
        /// <param name="longitude">Долгота точки местонахождения курьера</param>
        public CourierEventArgs(DateTime eventTime, CourierDeliveryInfo delivery, double latitude, double longitude)
        {
            EventTime = eventTime;
            Delivery = delivery;
            Latitude = latitude;
            Longitude = longitude;
        }
    }
}
