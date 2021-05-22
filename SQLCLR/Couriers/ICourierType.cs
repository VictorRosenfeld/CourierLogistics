namespace SQLCLR.Couriers
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

        ///// <summary>
        ///// Подсчет времени и стоимости отгрузки
        ///// </summary>
        ///// <param name="nodeInfo">
        ///// nodeInfo[i] - расстояние и время движения от точки i-1 до точки i
        ///// (nodeInfo[0] = (0,0) - соотв. магазину)</param>
        ///// <param name="totalWeight">Общий вес всех заказов</param>
        ///// <param name="isLoop">true - последняя точка пути магазин; false - конечный пункт - последняя точка вручения</param>
        ///// <param name="nodeDeliveryTime">Расчетное время доставки в заданную точку</param>
        ///// <param name="totalDeliveryTime">Суммарное время доставки всех заказов (время от старта до вручения последнего заказа)</param>
        ///// <param name="totalExecutionTime">Общее затраченное время (при isLoop = false совпадает с nodeDeliveryTime)</param>
        ///// <param name="totalCost">Общая стоимость доставки</param>
        ///// <returns>0 - результат получен; иначе - результат не получен</returns>
        //int GetTimeAndCost(Point[] nodeInfo, double totalWeight, bool isLoop, out double[] nodeDeliveryTime, out double totalDeliveryTime, out double totalExecutionTime, out double totalCost);
    }
}
