
namespace CreateDeliveriesApplication.Couriers
{
    /// <summary>
    /// Интерфейс описывающий параметры
    /// расчета стоимости и времени доставки
    /// </summary>
    public interface ICourierType
    {
        /// <summary>
        /// ID способа доставки
        /// </summary>
        int VehicleID { get; }

        /// <summary>
        /// Флаг: true - такси; false - курьер
        /// </summary>
        bool IsTaxi { get; }

        /// <summary>
        /// ID гео-типа Yandex
        /// </summary>
        int YandexType { get; }

        /// <summary>
        /// ID сервиса пулинга
        /// </summary>
        int DServiceType { get; }

        /// <summary>
        /// ID способа доставки в данных от сервиса S1
        /// </summary>
        int InputType { get; }

        /// <summary>
        /// ID метода расчета стоимости и времени доставки
        /// </summary>
        string CalcMethod { get; }

        /// <summary>
        /// Максимальный вес одного заказа, кг
        /// </summary>
        double MaxOrderWeight { get; }

        /// <summary>
        /// Максимальный вес отгрузки, кг
        /// </summary>
        double MaxWeight { get; }

        /// <summary>
        /// Максимальное число заказов в отгрузке
        /// </summary>
        int MaxOrderCount { get; }

        /// <summary>
        /// Максимальная длина маршрута, км
        /// </summary>
        double MaxDistance { get; }

        /// <summary>
        /// Время приёма отгрузки курьером, мин
        /// </summary>
        double GetOrderTime { get; }

        /// <summary>
        /// Время вручения заказа клиенту, мин
        /// </summary>
        double HandInTime { get; }

        /// <summary>
        /// Время подачи транспортного средства, мин
        /// </summary>
        double StartDelay { get; }
        
        /// <summary>
        /// Все параметры способа доставки
        /// </summary>
        CourierTypeData CourierData { get; }

        /// <summary>
        /// Оригинальный гео-тип Yandex
        /// </summary>
        /// <returns></returns>
        string GetYandexTypeName();
    }
}
