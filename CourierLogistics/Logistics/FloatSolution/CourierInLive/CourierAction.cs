
namespace CourierLogistics.Logistics.FloatSolution.CourierInLive
{
    /// <summary>
    /// Действие курьера
    /// </summary>
    public enum CourierAction
    {
        /// <summary>
        /// Нет
        /// </summary>
        None = 0,

        /// <summary>
        /// Начало работы
        /// </summary>
        WorkStated = 1,

        /// <summary>
        /// Завершение работы
        /// </summary>
        WorkEnded = 2,

        /// <summary>
        /// Начало перемещения в заданную точку
        /// </summary>
        BeginMoveToPoint = 3,

        /// <summary>
        /// Завершение перемещения в точку
        /// </summary>
        EndMoveToPoint = 4,

        /// <summary>
        /// Начало доставки
        /// </summary>
        BeginDelivery = 5,

        /// <summary>
        /// Завершение доставки
        /// </summary>
        EndDelivery = 6,
    }
}
