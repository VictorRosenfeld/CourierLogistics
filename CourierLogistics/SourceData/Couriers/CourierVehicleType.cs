namespace CourierLogistics.SourceData.Couriers
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
        /// Пешком
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

        /// <summary>
        /// Пешком
        /// </summary>
        OnFoot1 = 6,

        /// <summary>
        /// На велосипеде
        /// </summary>
        Bicycle1 = 7,

        /// <summary>
        /// На автомобиле
        /// </summary>
        Car1 = 8,
    }
}
