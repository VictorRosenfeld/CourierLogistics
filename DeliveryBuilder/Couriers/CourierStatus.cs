
namespace DeliveryBuilder.Couriers
{
    /// <summary>
    /// Статус курьера
    /// </summary>
    public enum CourierStatus
    {
        /// <summary>
        /// Неизвестен
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Свободен
        /// </summary>
        Ready = 1,

        /// <summary>
        /// Доствляет заказ
        /// </summary>
        DeliversOrder = 2,

        /// <summary>
        /// Обед
        /// </summary>
        LunchTime = 3,

        /// <summary>
        /// Работа закончилась
        /// </summary>
        WorkEnded = 4,

        /// <summary>
        /// Перемещение в заданную точку
        /// </summary>
        MoveToPoint = 5,
    }
}
