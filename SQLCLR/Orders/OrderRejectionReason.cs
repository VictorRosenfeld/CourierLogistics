
namespace SQLCLR.Orders
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
        OverWeight = 3,

        /// <summary>
        /// Запоздалый старт отгрузки
        /// </summary>
        LateStart = 4,

        /// <summary>
        /// Слишком большая удаленность
        /// </summary>
        OverDistance = 5,

        /// <summary>
        /// Слишком маленький интервал вручения
        /// </summary>
        ToTimeIsSmall = 6,

        /// <summary>
        /// Не удалось получить гео-данные
        /// </summary>
        GeoError = 7,

        /// <summary>
        /// Неверные аргументы для проверки маршрута
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
        OverOrderCount = 11,

        /// <summary>
        /// Ошибка при вычислении времени и стоимости отгрузки
        /// </summary>
        CalcError = 12,

        /// <summary>
        /// Ошибка при вычислении времени и стоимости отгрузки
        /// </summary>
        CantDeliveredOnTime = 13,

        /// <summary>
        /// Программная ошибка
        /// </summary>
        ProgramError = 14,
    }
}
