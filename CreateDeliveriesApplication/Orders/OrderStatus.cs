
namespace CreateDeliveriesApplication.Orders
{
    /// <summary>
    /// Состояние заказа
    /// </summary>
    public enum OrderStatus
    {
        /// <summary>
        /// Неопределенное состояние
        /// </summary>
        None = 0,

        /// <summary>
        /// Поступил в магазин
        /// </summary>
        Receipted = 1,

        /// <summary>
        /// Собран и готов к отгрузке
        /// </summary>
        Assembled = 2,

        /// <summary>
        /// Отменен
        /// </summary>
        Cancelled = 3,

        /// <summary>
        /// Отгружен
        /// </summary>
        Completed = 4,
    }
}
