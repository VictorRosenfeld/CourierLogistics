
namespace LogisticsService.Orders
{
    using LogisticsService.Couriers;
    using System;

    /// <summary>
    /// Заказ
    /// </summary>
    public class Order
    {
        /// <summary>
        /// Id заказа
        /// </summary>
        public int Id { get; private set; }

        /// <summary>
        /// Состояние заказа
        /// </summary>
        public OrderStatus Status { get; set; }

        /// <summary>
        /// Id магазина в который поступил заказ
        /// </summary>
        public int ShopId { get; set; }

        /// <summary>
        /// Вес заказа
        /// </summary>
        public double Weight { get; set; }

        /// <summary>
        /// Широта точки доставки
        /// </summary>
        public double Latitude { get; set; }

        /// <summary>
        /// Долгота точки доставки
        /// </summary>
        public double Longitude { get; set; }

        /// <summary>
        /// Дата-время сборки заказа
        /// </summary>
        public DateTime AssembledDate { get; set; }

        /// <summary>
        /// Дата-время поступления заказа в магазин
        /// </summary>
        public DateTime ReceiptedDate { get; set; }

        /// <summary>
        /// Время начала интервала вручения
        /// </summary>
        public DateTime DeliveryTimeFrom { get; set; }

        /// <summary>
        /// Время конца интервала вручения
        /// </summary>
        public DateTime DeliveryTimeTo { get; set; }

        /// <summary>
        /// Флаг: true - заказ отгружен; false - заказ не отгружен
        /// </summary>
        public bool Completed { get; set; }

        /// <summary>
        /// Индекс точки с координатами (Latitude, Longitude)
        /// </summary>
        public int LocationIndex { get; set; }

        /// <summary>
        /// Индекс заказа
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Причина отказа в доставке
        /// </summary>
        public OrderRejectionReason RejectionReason { get; set; }

        /// <summary>
        /// Переопределенный ToString()
        /// </summary>
        /// <returns>Текстовое представление экземпляра</returns>
        public override string ToString()
        {
            return $"{Id}, {Status}, {Completed}";
        }

        /// <summary>
        /// Типы курьеров, с помощью которых
        /// может быть осуществлена доставка
        /// </summary>
        public EnabledCourierType EnabledTypes { get; set; }

        /// <summary>
        /// Типы курьеров, с помощью которых
        /// может быть осуществлена доставка
        /// </summary>
        public CourierVehicleType[] EnabledTypesEx { get; set; } 

        /// <summary>
        /// Параметрический конструктор класса Order
        /// </summary>
        /// <param name="id">Id заказа</param>
        public Order(int id)
        {
            Id = id;
        }
    }
}
