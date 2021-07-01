
namespace SQLCLR.Couriers
{
    using SQLCLR.Deliveries;
    using SQLCLR.Log;
    using System;
    using System.Reflection;

    /// <summary>
    /// Делегат калькулятора расчета времени и стоимости доставки
    /// </summary>
    /// <param name="courierType">Параметры способа доставки</param>
    /// <param name="nodeInfo">Информация о расстояниях и времени движения между вершинами</param>
    /// <param name="totalWeight">Общий вес всех заказов, кг</param>
    /// <param name="isLoop">Флаг: true - возврат в магазин; false - завершение доставки в последней точке вручения</param>
    /// <param name="nodeDeliveryTime">Расчетное время доставки до всех точек вручения, мин</param>
    /// <param name="totalDeliveryTime">Время от начала отгрузки до вручения последнего заказа, мин</param>
    /// <param name="totalExecutionTime">Общее время доставки, мин</param>
    /// <param name="totalCost">Стоимость доставки</param>
    /// <returns>0 - расчеты успешно выполнены; иначе - расчеты не выполнены</returns>
    public delegate int GetTimeAndCostDelegate(ICourierType courierType, Point[] nodeInfo, double totalWeight, bool isLoop, out double[] nodeDeliveryTime, out double totalDeliveryTime, out double totalExecutionTime, out double totalCost);

    /// <summary>
    /// Методы расчета времени и стоимости доставки
    /// для всех спосоов доставки
    /// </summary>
    public class TimeAndCostCalculator
    {
        /// <summary>
        /// Префикс методов расчета
        /// </summary>
        private const string METHOD_PREFIX = "GetTimeAndCost_";

        /// <summary>
        /// Количество параметров делегата GetTimeAndCostDelegate
        /// </summary>
        private const int DELEGATE_PARAMETER_COUNT = 8;

        /// <summary>
        /// Извлечение делегата подсчета времени
        /// и стоимости доставки для заданного способа доставки
        /// </summary>
        /// <param name="methodId">Способ доставки</param>
        /// <returns>Делегат калькулятора или null</returns>
        public static GetTimeAndCostDelegate FindCalculator(string methodId)
        {
            // 1. Инциализация

            #if debug
                Logger.WriteToLog(601, $"TimeAndCostCalculator.GetTimeAndCostDelegate enter. methodId = {methodId}", 0);
            #endif

            try
            {
                // 2. Проверяем исходные данные
                if (string.IsNullOrWhiteSpace(methodId))
                    return null;

                // 3. Находим метод methodId
                string methodName = METHOD_PREFIX + methodId;
                MethodInfo[] methods = typeof(TimeAndCostCalculator).GetMethods(BindingFlags.Public | BindingFlags.Static);

                foreach(var method in methods)
                {
                    if (methodName.Equals(method.Name, StringComparison.CurrentCultureIgnoreCase) &&
                        method.GetParameters().Length == DELEGATE_PARAMETER_COUNT)
                    {
                        #if debug
                            Logger.WriteToLog(602, $"TimeAndCostCalculator.GetTimeAndCostDelegate exit. methodId = {methodId} found", 0);
                        #endif

                        return (GetTimeAndCostDelegate)Delegate.CreateDelegate(typeof(GetTimeAndCostDelegate), null, method);
                    }
                }

                // 5. Выход - калькулятор не найден
                #if debug
                    Logger.WriteToLog(602, $"TimeAndCostCalculator.GetTimeAndCostDelegate exit. methodId = {methodId} not found", 1);
                #endif
                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Выбор делегатов всех калькуляторов
        /// расчета времени и стоимости доставки
        /// </summary>
        /// <returns></returns>
        public static GetTimeAndCostDelegate[] SelectCalculators()
        {
            // 1. Инициализация

            try
            {
                // 2. Выбираем все статические публичные методы
                MethodInfo[] methods = typeof(TimeAndCostCalculator).GetMethods(BindingFlags.Public | BindingFlags.Static);
                if (methods == null || methods.Length <= 0)
                    return null;

                // 3. Создаём делегаты калькуляторов
                GetTimeAndCostDelegate[] calculators = new GetTimeAndCostDelegate[methods.Length];
                int count = 0;

                foreach (var method in methods)
                {
                    if (method.Name.StartsWith(METHOD_PREFIX, StringComparison.CurrentCultureIgnoreCase) &&
                        method.GetParameters().Length == DELEGATE_PARAMETER_COUNT)
                    {
                        calculators[count++] = (GetTimeAndCostDelegate)Delegate.CreateDelegate(typeof(GetTimeAndCostDelegate), null, method);
                    }
                }

                if (count < calculators.Length)
                {
                    Array.Resize(ref calculators, count);
                }

                // 4. Выход - Ok
                return calculators;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Построение имени метода расчета времени и стоимости доставки
        /// </summary>
        /// <param name="calcMethod">ID метода</param>
        /// <returns>Имя метода</returns>
        public static string GetMethodName(string calcMethod)
        {
            return METHOD_PREFIX + calcMethod;
        }

        /// <summary>
        /// Расчет времени и стоимости отгрузки из нескольких заказов такси
        /// </summary>
        /// <param name="courierType">Параметры курьера</param>
        /// <param name="nodeInfo">
        /// nodeInfo[i] - расстояние и время движения от точки i-1 до точки i
        /// (nodeInfo[0] = (0,0) - соотв. магазину; общее число точек = число заказов + 2)
        /// </param>
        /// <param name="totalWeight">Общий вес всех заказов</param>
        /// <param name="isLoop">true - возврат в магазин; false - без возврата в магазин</param>
        /// <param name="nodeDeliveryTime">Расчетное время доставки в заданную точку, мин</param>
        /// <param name="totalDeliveryTime">Суммарное время доставки всех заказов (время от старта до вручения последнего заказа)</param>
        /// <param name="totalExecutionTime">Общее время отгрузки (при isLoop = false совпадает с nodeDeliveryTime)</param>
        /// <param name="totalCost">Общая стоимость отгрузки</param>
        /// <returns>0 - расчет выполнен; иначе - расчет не выполнен</returns>
        public static int GetTimeAndCost_Taxi(ICourierType courierType, Point[] nodeInfo, double totalWeight, bool isLoop, out double[] nodeDeliveryTime, out double totalDeliveryTime, out double totalExecutionTime, out double totalCost)
        {
            // 1. Инициализация
            int rc = 1;
            nodeDeliveryTime = null;
            totalDeliveryTime = 0;
            totalExecutionTime = 0;
            totalCost = 0;

            try
            {
                // 2. Проверяем исходные данные
                rc = 20;
                if (courierType == null)
                    return rc;
                if (nodeInfo == null || nodeInfo.Length < 3)
                    return rc;
                rc = 22;
                if (totalWeight > courierType.MaxWeight)
                    return rc;
                int nodeCount = nodeInfo.Length;
                rc = 23;
                if (nodeCount - 2 > courierType.MaxOrderCount)
                    return rc;

                // 3. Расчитываем время доставки до всех точек доставки
                rc = 3;
                nodeDeliveryTime = new double[nodeCount];
                nodeDeliveryTime[0] = courierType.StartDelay + courierType.GetOrderTime;

                int sumDist = 0;

                for (int i = 1; i < nodeCount; i++)
                {
                    sumDist += nodeInfo[i].X;
                    nodeDeliveryTime[i] = nodeDeliveryTime[i - 1] + courierType.HandInTime + nodeInfo[i].Y / 60.0;
                }

                totalDeliveryTime = nodeDeliveryTime[nodeCount - 2];
                if (isLoop)
                {
                    totalExecutionTime = totalDeliveryTime;
                }
                else
                {
                    totalExecutionTime = totalDeliveryTime;
                    sumDist -= nodeInfo[nodeCount - 1].X;
                }

                double distance = sumDist / 1000.0;
                if (distance > courierType.MaxDistance)
                    return rc;

                //totalCost = courierType.FirstPay;

                //if (distance > courierType.FirstDistance)
                //    totalCost += courierType.AdditionalKilometerCost * Math.Ceiling(distance - courierType.FirstDistance);
                //if (nodeCount > 3)
                //    totalCost += (nodeCount - 3) * courierType.SecondPay;

                // 4. Выход - Ok
                rc = 0;
                return rc;
            }
            catch
            {
                return rc;
            }
        }
    }
}
