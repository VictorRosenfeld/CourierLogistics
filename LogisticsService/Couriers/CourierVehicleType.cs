
namespace LogisticsService.Couriers
{
    /// <summary>
    /// Способ передвижения курьера
    /// </summary>
    public enum CourierVehicleType
    {
        /// <summary>
        /// Не определен
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Пеший
        /// </summary>
        OnFoot = 1,

        /// <summary>
        /// На велосипеде
        /// </summary>
        Bicycle = 2,

        /// <summary>
        /// На автомобиле
        /// </summary>
        Car = 3,

        /// <summary>
        /// Доставка с помощью Yandex-такси
        /// </summary>
        YandexTaxi = 4,

        /// <summary>
        /// Доставка с помощью Gett-такси
        /// </summary>
        GettTaxi = 5,
    }
}
