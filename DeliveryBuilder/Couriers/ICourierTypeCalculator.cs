
namespace DeliveryBuilder.Couriers
{
    using DeliveryBuilder.Deliveries;
    using DeliveryBuilder.Geo;

    /// <summary>
    /// Расчет стоимости и времени доставки
    /// </summary>
    public interface ICourierTypeCalculator
    {
        /// <summary>
        /// Калькулятор расчета времени и стоимости доставки
        /// </summary>
        /// <param name="courierType">Параметры способа доставки</param>
        /// <param name="nodeInfo">Информация о расстоянии и времени перемещения между точками маршрута</param>
        /// <param name="totalWeight">Общий вес всех заказов, кг</param>
        /// <param name="isLoop">Флаг: true - требуется возырат в магазин; false - маршрут завершается в последней точке вручения</param>
        /// <param name="nodeDeliveryTime">Время он начала отгрузки до вручения заказа для каждой точки маршрута, мин</param>
        /// <param name="totalDeliveryTime">Общее время от начала отгрзки до вручения последнего заказа, мин</param>
        /// <param name="totalExecutionTime">Общее время доставки, мин (Если isLoop = false, то totalExecutionTime = totalDeliveryTime)</param>
        /// <param name="totalCost">Стоимость отгрузки</param>
        /// <returns>0 - расчеты успешно выполнены; иначе расчеты не выполнены</returns>
        int GetTimeAndCost(Point[] nodeInfo, double totalWeight, bool isLoop, out double[] nodeDeliveryTime, out double totalDeliveryTime, out double totalExecutionTime, out double totalCost);
    }
}
