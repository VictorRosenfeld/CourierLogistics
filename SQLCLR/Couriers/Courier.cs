
namespace SQLCLR.Couriers
{
    using System;

    /// <summary>
    /// Курьер (такси)
    /// </summary>
    public class Courier: CourierBase
    {
        /// <summary>
        /// ID курьера
        /// </summary>
        public int Id { get; private set; }

        /// <summary>
        /// Статус курьера
        /// </summary>
        public CourierStatus Status { get; set; }

        /// <summary>
        /// Начало работы
        /// </summary>
        public TimeSpan WorkStart { get; set; }

        /// <summary>
        /// Конец работы
        /// </summary>
        public TimeSpan WorkEnd { get; set; }

        /// <summary>
        /// Начало обеда
        /// </summary>
        public TimeSpan LunchTimeStart { get; set; }

        /// <summary>
        /// Конец обеда
        /// </summary>
        public TimeSpan LunchTimeEnd { get; set; }

        /// <summary>
        /// Время начала отгрузки (Status = DeliversOrder)
        /// </summary>
        public DateTime LastDeliveryStart { get; set; }

        /// <summary>
        /// Время возвращения (Status = DeliversOrder)
        /// </summary>
        public DateTime LastDeliveryEnd { get; set; }

        /// <summary>
        /// Количество выполненных заказов
        /// </summary>
        public int OrderCount { get; set; }

        /// <summary>
        /// Время, потраченное на выполнение всех заказов за день, час
        /// </summary>
        public double TotalDeliveryTime { get; set; }

        /// <summary>
        /// Общая стоимость курьера за день
        /// </summary>
        public double TotalCost { get; set; }

        /// <summary>
        /// Средняя цена стоимости заказа
        /// </summary>
        public double AverageOrderCost { get; set; }

        /// <summary>
        /// Id маназина, к которому привязан курьер
        /// (фиксированная модель)
        /// </summary>
        public int ShopId { get; set; }

        /// <summary>
        /// Широта местоположения курьера
        /// </summary>
        public double Latitude { get; set; }

        /// <summary>
        /// Долгота местоположения курьера
        /// </summary>
        public double Longitude { get; set; }

        /// <summary>
        /// Параметрический констуктор класса Courier
        /// </summary>
        /// <param name="id">ID курьера</param>
        /// <param name="courierType">Параметры способа доставки</param>
        public Courier(int id, ICourierType courierType) : base(courierType)
        {
            Id = id;
            SetCalculator(TimeAndCostCalculator.FindCalculator(CalcMethod));
        }

        /// <summary>
        /// Параметрический констуктор класса Courier
        /// </summary>
        /// <param name="id">ID курьера</param>
        /// <param name="courierBase">Базовый класс курьера</param>
        public Courier(int id, CourierBase courierBase) : base(courierBase)
        {
            SetCalculator(courierBase.Calculator);
        }
    }
}
