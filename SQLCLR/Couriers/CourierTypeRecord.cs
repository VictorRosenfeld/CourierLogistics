
namespace SQLCLR.Couriers
{
    /// <summary>
    /// Запись таблицы lsvCourierTypes
    /// </summary>
    public class CourierTypeRecord: ICourierType
    {
        #region ICourierType Implementation

        /// <summary>
        /// ID способа доставки
        /// </summary>
        public int VehicleID { get; private set; }

        /// <summary>
        /// Флаг: true - такси; false - курьер
        /// </summary>
        public bool IsTaxi { get; private set; }

        /// <summary>
        /// ID гео-типа Yandex
        /// </summary>
        public int YandexType { get; private set; }

        /// <summary>
        /// ID сервиса пулинга
        /// </summary>
        public int DServiceType { get; private set; }

        /// <summary>
        /// ID способа доставки в данных от сервиса S1
        /// </summary>
        public int InputType { get; private set; }

        /// <summary>
        /// ID метода расчета стоимости и времени доставки
        /// </summary>
        public string CalcMethod { get; private set; }

        /// <summary>
        /// Максимальный вес отгрузки, кг
        /// </summary>
        public double MaxWeight { get; private set; }

        /// <summary>
        /// Максимальный вес одного заказа, кг
        /// </summary>
        public double MaxOrderWeight { get; private set; }

        /// <summary>
        /// Максимальное число заказов в отгрузке
        /// </summary>
        public int MaxOrderCount { get; private set; }

        /// <summary>
        /// Максимальная длина маршрута, км
        /// </summary>
        public double MaxDistance { get; private set; }

        /// <summary>
        /// Время приёма отгрузки курьером, мин
        /// </summary>
        public double GetOrderTime { get; private set; }

        /// <summary>
        /// Время вручения заказа клиенту, мин
        /// </summary>
        public double HandInTime { get; private set; }

        /// <summary>
        /// Время подачи транспортного средства, мин
        /// </summary>
        public double StartDelay { get; private set; }

        /// <summary>
        /// Все параметры способа доставки
        /// </summary>
        public CourierTypeData CourierData { get; private set; }

        /// <summary>
        /// Оригинальный гео-тип Yandex
        /// </summary>
        /// <returns></returns>
        public string GetYandexTypeName() => (CourierData != null && CourierData.IsCreated ? CourierData.Mapper.Yandex.Value : null);

        #endregion ICourierType Implementation

        /// <summary>
        /// Параметрический конструктор класса CourierTypeRecord
        /// </summary>
        /// <param name="vehicleId">ID способа доставки</param>
        /// <param name="istaxi">Флаг: true - такси; false - курьер</param>
        /// <param name="yandexType">ID гео-типа Yandex</param>
        /// <param name="dserviceType">ID сервиса пулинга</param>
        /// <param name="inputType">ID способа доставки в данных от сервиса S1</param>
        /// <param name="calcMethod">ID метода расчета стоимости и времени доставки</param>
        /// <param name="maxOrderWeight">Максимальный вес одного заказа, кг</param>
        /// <param name="maxWeight">Максимальный вес отгрузки, кг</param>
        /// <param name="maxOrderCount">Максимальное число заказов в отгрузке</param>
        /// <param name="maxDistance">Максимальная длина маршрута, км</param>
        /// <param name="getOrderTime">Время приёма отгрузки курьером, мин</param>
        /// <param name="handInTime">Время вручения заказа клиенту, мин</param>
        /// <param name="startDeley">Время подачи транспортного средства, мин</param>
        /// <param name="courierData">Все параметры способа доставки</param>
        public CourierTypeRecord(int vehicleId, bool istaxi, int yandexType, int dserviceType, int inputType,
                                 string calcMethod, double maxOrderWeight, double maxWeight, int maxOrderCount,
                                 double maxDistance, double getOrderTime, double handInTime, double startDeley,
                                 CourierTypeData courierData)
        {
            VehicleID = vehicleId;
            IsTaxi = istaxi;
            YandexType = yandexType;
            DServiceType = dserviceType;
            InputType = inputType;
            CalcMethod = calcMethod;
            MaxWeight = maxWeight;
            MaxOrderWeight = maxOrderWeight;
            MaxOrderCount = maxOrderCount;
            MaxDistance = maxDistance;
            GetOrderTime = getOrderTime;
            HandInTime = handInTime;
            StartDelay = startDeley;
            CourierData = courierData;
        }
    }
}
