
namespace SQLCLR.Couriers
{
    using SQLCLR.Deliveries;

    /// <summary>
    /// Базовый тип для курьера
    /// </summary>
    public class CourierBase : ICourierType, ICourierTypeCalculator
    {
        /// <summary>
        /// Калькулятор расчета времени и стоимости доставки
        /// </summary>
        public GetTimeAndCostDelegate Calculator {get; private set;}

        #region ICourierType Implementation

        /// <summary>
        /// ID способа доставки
        /// </summary>
        public int VehicleID { get; private set; }

        /// <summary>
        /// Флаг: true - такси; false - курьер
        /// </summary>
        public bool IsTaxi {get; private set;}

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
        /// Максимальный вес одного заказа, кг
        /// </summary>
        public double MaxOrderWeight { get; private set; }

        /// <summary>
        /// Максимальный вес отгрузки, кг
        /// </summary>
        public double MaxWeight { get; private set; }

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
        public string GetYandexTypeName() =>(CourierData != null && CourierData.IsCreated ? CourierData.Mapper.Yandex.Value : null);

        #endregion ICourierType Implementation

        #region ICourierTypeCalculator Implementation

        /// <summary>
        /// Подсчет времени и стоимости отгрузки
        /// </summary>
        /// <param name="nodeInfo">
        /// nodeInfo[i] - расстояние и время движения от точки i-1 до точки i
        /// (nodeInfo[0] = (0,0) - соотв. магазину)</param>
        /// <param name="totalWeight">Общий вес всех заказов</param>
        /// <param name="isLoop">true - последняя точка пути магазин; false - конечный пункт - последняя точка вручения</param>
        /// <param name="nodeDeliveryTime">Расчетное время доставки в заданную точку</param>
        /// <param name="totalDeliveryTime">Суммарное время доставки всех заказов (время от старта до вручения последнего заказа)</param>
        /// <param name="totalExecutionTime">Общее затраченное время (при isLoop = false совпадает с nodeDeliveryTime)</param>
        /// <param name="totalCost">Общая стоимость доставки</param>
        /// <returns>0 - результат получен; иначе - результат не получен</returns>
        public virtual int GetTimeAndCost(Point[] nodeInfo, double totalWeight, bool isLoop, out double[] nodeDeliveryTime, out double totalDeliveryTime, out double totalExecutionTime, out double totalCost)
        {
            if (Calculator == null)
            {
                nodeDeliveryTime = null;
                totalDeliveryTime = 0;
                totalExecutionTime = 0;
                totalCost = 0;
                return -1;
            }

            return Calculator(this, nodeInfo, totalWeight, isLoop, out nodeDeliveryTime, out totalDeliveryTime, out totalExecutionTime, out totalCost);
        }
        
        #endregion ICourierTypeCalculator Implementation

        /// <summary>
        /// Параметрический конструктор класса CourierBase
        /// </summary>
        /// <param name="parameters">Параметры курьера</param>
        public CourierBase(ICourierType parameters)
        {
            VehicleID = parameters.VehicleID;
            IsTaxi = parameters.IsTaxi;
            YandexType = parameters.YandexType;
            DServiceType = parameters.DServiceType;
            InputType = parameters.InputType;
            CalcMethod = parameters.CalcMethod;
            MaxOrderWeight = parameters.MaxOrderWeight;
            MaxWeight = parameters.MaxWeight;
            MaxOrderCount = parameters.MaxOrderCount;
            MaxDistance = parameters.MaxDistance;
            GetOrderTime = parameters.GetOrderTime;
            HandInTime = parameters.HandInTime;
            StartDelay = parameters.StartDelay;
            CourierData = parameters.CourierData;
            //SetCalculator(TimeAndCostCalculator.FindCalculator(CalcMethod));
        }

        /// <summary>
        /// Установка калькулятора расчета времени и стоимости доставки
        /// </summary>
        /// <param name="vehicleTypeCalculator">Делегат калькулятора</param>
        public void SetCalculator(GetTimeAndCostDelegate vehicleTypeCalculator)
        {
            Calculator = vehicleTypeCalculator;
        }
    }
}
