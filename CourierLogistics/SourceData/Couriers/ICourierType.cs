
namespace CourierLogistics.SourceData.Couriers
{
    /// <summary>
    /// Общий интерфейс, определяющий тип курьера
    /// и позволяющий ответить на типовые вопросы
    /// </summary>
    public interface ICourierType
    {
        #region Properties

        /// <summary>
        /// Средство передвижения
        /// </summary>
        CourierVehicleType VechicleType { get; }

        // Курьеры

        /// <summary>
        /// Максимальный вес заказа
        /// </summary>
        double MaxWeight { get; }

        /// <summary>
        /// Часовая ставка
        /// </summary>
        double HourlyRate { get; }

        /// <summary>
        /// Максимальная дальность в один конец
        /// </summary>
        double MaxDistance { get; }

        /// <summary>
        /// Максимальное число заказов
        /// </summary>
        int MaxOrderCount { get; }

        /// <summary>
        /// Средняя скорость
        /// </summary>
        double AverageVelocity { get; }

        /// <summary>
        /// Страховка в процентах (VechicleType = OnFoot, OnBicicle, OnCar)
        /// </summary>
        double Insurance { get; }

        /// <summary>
        /// Время приёмки одного заказа
        /// </summary>
        double GetOrderTime { get; }

        /// <summary>
        /// Время вручения заказа
        /// </summary>
        double HandInTime { get; }

        // Такси

        /// <summary>
        /// Среднее время подачи такси, мин
        /// </summary>
        double StartDelay { get; }

        /// <summary>
        /// Стоимость первых FirstDistance километров
        /// </summary>
        double FirstPay { get; }

        /// <summary>
        /// Стоимость второго заказа (VechicleType = YandexTaxi)
        /// </summary>
        double SecondPay { get; }

        /// <summary>
        /// Километры, оплаченные FirstPay
        /// </summary>
        double FirstDistance { get; }

        /// <summary>
        /// Стоимость дополнительного километра
        /// </summary>
        double AdditionalKilometerCost { get; }

        #endregion Properties

        #region Methods

        /// <summary>
        /// Подсчет времени и стоимости доставки одного заказа
        /// </summary>
        /// <param name="distance">Расстояние до точки доставки</param>
        /// <param name="weight">Вес заказа</param>
        /// <param name="deliveryTime">Время от начала доставки до точки доставки</param>
        /// <param name="executionTime">Время от начала доставки до возврщения в исходную точку</param>
        /// <param name="cost">Стоимость доставки</param>
        /// <returns>0 - результат получен; иначе - результат не получен</returns>
        int GetTimeAndCost(double distance, double weight, out double deliveryTime, out double executionTime, out double cost);

        /// <summary>
        /// Подсчет времени и стоимости доставки нескольких заказов
        /// </summary>
        /// <param name="nodeDistance">Расстояние от точки старта до первой точки доставки, от первой точки до второй точки, ... , от последней точки до точки старта</param>
        /// <param name="totalWeight">Общий вес всех заказов</param>
        /// <param name="nodeDeliveryTime">Расчетное время доставки в заданную точку</param>
        /// <param name="totalDeliveryTime">Суммарное время доставки всех заказов (время от старта до вручения последнего заказа)</param>
        /// <param name="totalExecutionTime">Общее затраченное время (для такси = totalDeliveryTime; для курьера = totalDeliveryTime + время возвращения в точку старта)</param>
        /// <param name="totalCost">Общая стоимость доставки</param>
        /// <returns>0 - результат получен; иначе - результат не получен</returns>
        int GetTimeAndCost(double[] nodeDistance, double totalWeight, out double[] nodeDeliveryTime, out double totalDeliveryTime, out double totalExecutionTime, out double totalCost);

        #endregion Methods
    }
}
