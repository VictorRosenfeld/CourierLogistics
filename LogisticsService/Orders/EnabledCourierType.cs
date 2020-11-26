
namespace LogisticsService.Orders
{
    using System;

    /// <summary>
    /// Флаги способов доставки
    /// </summary>
    [Flags]
    public enum EnabledCourierType
    {
        /// <summary>
        /// Непределенный сервис
        /// </summary>
        Unknown = 0x0,

        /// <summary>
        /// Пеший
        /// </summary>
        OnFoot = 0x1,

        /// <summary>
        /// На велосипеде
        /// </summary>
        Bicycle = 0x2,

        /// <summary>
        /// На автомобиле
        /// </summary>
        Car = 0x4,

        /// <summary>
        /// Доставка с помощью Yandex-такси
        /// </summary>
        YandexTaxi = 0x8,

        /// <summary>
        /// Доставка с помощью Gett-такси
        /// </summary>
        GettTaxi = 0x10,
    }
}
