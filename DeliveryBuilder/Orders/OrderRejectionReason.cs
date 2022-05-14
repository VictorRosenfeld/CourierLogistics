
namespace DeliveryBuilder.Orders
{
    /// <summary>
    /// Причины отклонения заказа
    /// </summary>
    public enum OrderRejectionReason
    {
        /// <summary>
        /// Нет
        /// </summary>
        None = 0,

        /// <summary>
        /// Поздняя сборка
        /// </summary>
        LateAssembled = 1,

        /// <summary>
        /// Курьер не доступен
        /// </summary>
        CourierNA = 2,

        /// <summary>
        /// Слишком тяжелый заказ
        /// </summary>
        WeightOver = 3,

        /// <summary>
        /// Запоздалый старт отгрузки
        /// </summary>
        LateStart = 4,

        /// <summary>
        /// Слишком длинный маршрут
        /// </summary>
        DistanceOver = 5,

        /// <summary>
        /// Слишком маленький интервал вручения
        /// </summary>
        ToTimeIsSmall = 6,

        /// <summary>
        /// Гео-данные недоступны
        /// </summary>
        GeoDataNA = 7,

        /// <summary>
        /// Неверные или неполные данные
        /// </summary>
        InvalidArgs = 8,

        /// <summary>
        /// Неизвестный метод расчета
        /// </summary>
        UnknownCalcMethod = 9,

        /// <summary>
        /// Неизвестный способ доставки
        /// </summary>
        UnknownVehicleType = 10,

        /// <summary>
        /// Превышено число заказов
        /// </summary>
        OrderOver = 11,

        /// <summary>
        /// Ошибка при вычислении времени и стоимости отгрузки
        /// </summary>
        CalcError = 12,

        /// <summary>
        /// Заказ не может быть доставлен в срок
        /// </summary>
        TimeOver = 13,

        /// <summary>
        /// Программная ошибка
        /// </summary>
        ProgramError = 14,

        /// <summary>
        /// Нераспознанная причина
        /// </summary>
        Unrecognized = 15,
    }
}
