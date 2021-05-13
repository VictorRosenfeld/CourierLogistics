
namespace SQLCLR.Orders
{
    using System;

    /// <summary>
    /// Заказ
    /// </summary>
    internal class Order
    {
        /// <summary>
        /// Id заказа
        /// </summary>
        public int Id { get; private set; }

        /// <summary>
        /// Состояние заказа
        /// </summary>
        public int Status { get; set; }

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
        /// Причина отказа в доставке
        /// </summary>
        public int RejectionReason { get; set; }

        /// <summary>
        /// Приоритет заказа
        /// </summary>
        public int Priority { get; set; }

        /// <summary>
        /// Флаг отмены проверок временных ограничений:
        /// true - проверки отменены; false - проверки остаются в силе
        /// </summary>
        public bool TimeCheckDisabled { get; set; }

        /// <summary>
        /// ID допустимых способов доставки заказа (VehicleID),
        /// осортированных по возрастанию
        /// </summary>
        public int[] VehicleTypes { get; set; }

        /// <summary>
        /// Переопределенный ToString()
        /// </summary>
        /// <returns>Текстовое представление экземпляра</returns>
        public override string ToString()
        {
            return $"{Id}, {Status}, {Completed}";
        }

        /// <summary>
        /// Проверка способа доставки на возможность доставки заказа
        /// </summary>
        /// <param name="vehicleType">Проверяемый способ доставки</param>
        /// <returns>true - способ доставки является подходящим; false - способ доставки не является подходящим</returns>
        public bool IsVehicleTypeEnabled(int vehicleType)
        {
            if (VehicleTypes == null || VehicleTypes.Length <= 0)
                return false;
            return Array.BinarySearch(VehicleTypes, vehicleType) >= 0;
        }

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
