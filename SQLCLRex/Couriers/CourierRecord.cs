
namespace SQLCLRex.Couriers
{
    using System;

    /// <summary>
    /// Запись lsvCouriers
    /// </summary>
    public class CourierRecord
    {
        /// <summary>
        /// ID курьера
        /// </summary>
        public int CourierId { get; set; }

        /// <summary>
        /// ID способа доставки
        /// </summary>
        public int VehicleId { get; set; }

        /// <summary>
        /// Статус курьера
        /// </summary>
        public CourierStatus Status { get; set; }

        /// <summary>
        /// Время начала рабочего дня
        /// </summary>
        public TimeSpan WorkStart { get; set; }

        /// <summary>
        /// Время завершения рабочего дня
        /// </summary>
        public TimeSpan WorkEnd { get; set; }
        
        /// <summary>
        /// Время начала обеда
        /// </summary>
        public TimeSpan LunchTimeStart { get; set; }

        /// <summary>
        /// Время конца обеда
        /// </summary>
        public TimeSpan LunchTimeEnd { get; set; }

        /// <summary>
        /// Время начала последней отгрузки
        /// </summary>
        public DateTime LastDeliveryStart { get; set; }

        /// <summary>
        /// Время завершения последней отгрузки
        /// </summary>
        public DateTime LastDeliveryEnd { get; set; }

        /// <summary>
        /// Общее число отгруженых заказов
        /// </summary>
        public int OrderCount { get; set; }

        /// <summary>
        /// Общее время, потраченное на все отгрузки
        /// </summary>
        public double TotalDeliveryTime { get; set; }

        /// <summary>
        /// Общая стоимость доставки
        /// </summary>
        public double TotalCost { get; set; }

        /// <summary>
        /// Порог для средней стоимости доставки одного заказа,
        /// используемый для принятия решения об отгрузке
        /// </summary>
        public double AverageOrderCost { get; set; }

        /// <summary>
        /// ID магазина курьера
        /// (для такси - 0)
        /// </summary>
        public int ShopId { get; set; }

        /// <summary>
        /// Широта местонахождения курьера
        /// </summary>
        public double Latitude { get; set; }

        /// <summary>
        /// Долгота местонахождения курьера
        /// </summary>
        public double Longitude { get; set; }
    }
}
