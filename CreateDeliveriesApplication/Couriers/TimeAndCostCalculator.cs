
namespace CreateDeliveriesApplication.Couriers
{
    using CreateDeliveriesApplication.Deliveries;
    using CreateDeliveriesApplication.Log;
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

        #region Vehicle parameter name

        /// <summary>
        /// Стоимость подачи такси, руб
        /// </summary>
        private const string FIRST_PAY_PARAMETER = "first_pay";

        /// <summary>
        /// Оплаченное время приёмки отгрузки, мин
        /// </summary>
        private const string FIRST_GET_ORDER_TIME_PARAMETER = "first_get_order_time";

        /// <summary>
        /// Стоимость минуты приёмки сверх оплаченной, руб/мин
        /// </summary>
        private const string FIRST_GET_ORDER_RATE_PARAMETER = "first_get_order_rate";

        /// <summary>
        /// Оплаченное расстояние до первого заказа, км
        /// </summary>
        private const string FIRST_DISTANCE_PARAMETER = "first_distance";

        /// <summary>
        /// Стоимость дополнительного километра сверх оплаченных, руб/км
        /// </summary>
        private const string ADDITIONAL_KILOMETER_COST_PARAMETER = "additional_kilometer_cost";

        /// <summary>
        /// Стоимость вручения заказа, начиная со второго, руб
        /// </summary>
        private const string SECOND_PAY_PARAMETER = "second_pay";

        /// <summary>
        /// Оплаченное время доставки первого заказа, мин
        /// </summary>
        private const string FIRST_TIME_PARAMETER = "first_time";

        /// <summary>
        /// Стоимость дополнительного времени доставки первого заказа, руб/мин
        /// </summary>
        private const string FIRST_TIME_RATE_PARAMETER = "first_time_rate";

        /// <summary>
        /// Стоимость дополнительного времени доставки заказов, начиная со второго, руб/мин
        /// </summary>
        private const string SECOND_TIME_RATE_PARAMETER = "second_time_rate";

        /// <summary>
        /// Почасовая ставка, руб/час
        /// </summary>
        private const string HOURLY_RATE_PARAMETER = "hourly_rate";

        #endregion Vehicle parameter name

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

        #region Calc methods

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

        /// <summary>
        /// Расчет времени и стоимости отгрузки из нескольких заказов c помощью Yandex-такси
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
        public static int GetTimeAndCost_yandextaxi(ICourierType courierType, Point[] nodeInfo, double totalWeight, bool isLoop, out double[] nodeDeliveryTime, out double totalDeliveryTime, out double totalExecutionTime, out double totalCost)
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

                // 4. Устанавливаем время выходных параметров total_delivery_time и total_execution_time
                rc = 4;
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

                // 5. Извлекаем параметры для расчета стоимости
                rc = 5;
                double firstPay = courierType.CourierData.GetDoubleParameterValue(FIRST_PAY_PARAMETER);
                if (double.IsNaN(firstPay))
                    return rc;
                double firstGetOrderTime = courierType.CourierData.GetDoubleParameterValue(FIRST_GET_ORDER_TIME_PARAMETER);
                if (double.IsNaN(firstGetOrderTime))
                    return rc;
                double firstGetOrderRate = courierType.CourierData.GetDoubleParameterValue(FIRST_GET_ORDER_RATE_PARAMETER);
                if (double.IsNaN(firstGetOrderRate))
                    return rc;
                double firstDistance = courierType.CourierData.GetDoubleParameterValue(FIRST_DISTANCE_PARAMETER);
                if (double.IsNaN(firstDistance))
                    return rc;
                double additionalKilometerCost = courierType.CourierData.GetDoubleParameterValue(ADDITIONAL_KILOMETER_COST_PARAMETER);
                if (double.IsNaN(additionalKilometerCost))
                    return rc;
                double secondPay = courierType.CourierData.GetDoubleParameterValue(SECOND_PAY_PARAMETER);
                if (double.IsNaN(secondPay))
                    return rc;

                // 6. Расчитываем стомость
                rc = 6;
                totalCost = firstPay;

                // 6.1 Дополнительная плата за получение отгрузки
                rc = 61;
                if (courierType.GetOrderTime > firstGetOrderTime)
                    totalCost += (firstGetOrderRate * (courierType.GetOrderTime - firstGetOrderTime));

                // 6.2 Дополнительная плата за расстояние до первого заказа
                rc = 62;
                double dist1 = nodeInfo[1].X / 1000.0;
                if (dist1 > firstDistance)
                    totalCost += (additionalKilometerCost * (dist1 - firstDistance));

                // 6.3 Оплата за дополнительные километры
                rc = 63;
                totalCost += (additionalKilometerCost * (distance - dist1));

                // 6.4 Оплата за вручения, начиная со второго
                rc = 64;
                if (nodeCount > 3)
                    totalCost += (secondPay * (nodeCount - 3));

                // 7. Выход - Ok
                rc = 0;
                return rc;
            }
            catch
            {
                return rc;
            }
        }

        /// <summary>
        /// Расчет времени и стоимости отгрузки из нескольких заказов c помощью Gett-такси
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
        public static int GetTimeAndCost_getttaxi(ICourierType courierType, Point[] nodeInfo, double totalWeight, bool isLoop, out double[] nodeDeliveryTime, out double totalDeliveryTime, out double totalExecutionTime, out double totalCost)
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

                // 4. Устанавливаем время выходных параметров total_delivery_time и total_execution_time
                rc = 4;
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

                // 5. Извлекаем параметры для расчета стоимости
                rc = 5;
                double firstPay = courierType.CourierData.GetDoubleParameterValue(FIRST_PAY_PARAMETER);
                if (double.IsNaN(firstPay))
                    return rc;
                //double firstGetOrderTime = courierType.CourierData.GetDoubleParameterValue(FIRST_GET_ORDER_TIME_PARAMETER);
                //if (double.IsNaN(firstGetOrderTime))
                //    return rc;
                //double firstGetOrderRate = courierType.CourierData.GetDoubleParameterValue(FIRST_GET_ORDER_RATE_PARAMETER);
                //if (double.IsNaN(firstGetOrderRate))
                //    return rc;
                double firstDistance = courierType.CourierData.GetDoubleParameterValue(FIRST_DISTANCE_PARAMETER);
                if (double.IsNaN(firstDistance))
                    return rc;
                double additionalKilometerCost = courierType.CourierData.GetDoubleParameterValue(ADDITIONAL_KILOMETER_COST_PARAMETER);
                if (double.IsNaN(additionalKilometerCost))
                    return rc;
                double secondPay = courierType.CourierData.GetDoubleParameterValue(SECOND_PAY_PARAMETER);
                if (double.IsNaN(secondPay))
                    return rc;

                double firstTime = courierType.CourierData.GetDoubleParameterValue(FIRST_TIME_PARAMETER);
                if (double.IsNaN(firstTime))
                    return rc;
                double firstTimeRate = courierType.CourierData.GetDoubleParameterValue(FIRST_TIME_RATE_PARAMETER);
                if (double.IsNaN(firstTimeRate))
                    return rc;
                double secondTimeRate = courierType.CourierData.GetDoubleParameterValue(SECOND_TIME_RATE_PARAMETER);
                if (double.IsNaN(firstTimeRate))
                    return rc;

                // 6. Расчитываем стомость
                rc = 6;
                totalCost = firstPay;

                // 6.1 Дополнительная плата за получение отгрузки
                //rc = 61;
                //if (courierType.GetOrderTime > firstGetOrderTime)
                //    totalCost += (firstGetOrderRate * (courierType.GetOrderTime - firstGetOrderTime));

                // 6.2 Дополнительная плата за расстояние и время до первого заказа
                rc = 62;
                double dist1 = nodeInfo[1].X / 1000.0;
                double time1 = nodeInfo[1].Y / 60.0;
                if (dist1 > firstDistance)
                    totalCost += (additionalKilometerCost * (dist1 - firstDistance));
                if (time1 > firstTime)
                    totalCost += (firstTimeRate * (time1 - firstTime));
                

                // 6.3 Оплата за дополнительные километры
                rc = 63;
                totalCost += (additionalKilometerCost * (distance - dist1));

                // 6.4 Оплата за дополнительное время
                rc = 64;
                totalCost += (secondTimeRate * (totalExecutionTime - nodeDeliveryTime[1]));

                // 6.5 Оплата за вручения, начиная со второго
                rc = 64;
                if (nodeCount > 3)
                    totalCost += (secondPay * (nodeCount - 3));

                // 7. Выход - Ok
                rc = 0;
                return rc;
            }
            catch
            {
                return rc;
            }
        }

        /// <summary>
        /// Расчет времени и стоимости отгрузки из нескольких заказов c помощью Gett-такси
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
        public static int GetTimeAndCost_getttaxi3(ICourierType courierType, Point[] nodeInfo, double totalWeight, bool isLoop, out double[] nodeDeliveryTime, out double totalDeliveryTime, out double totalExecutionTime, out double totalCost)
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

                // 4. Устанавливаем время выходных параметров total_delivery_time и total_execution_time
                rc = 4;
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

                // 5. Извлекаем параметры для расчета стоимости
                rc = 5;
                double firstPay = courierType.CourierData.GetDoubleParameterValue(FIRST_PAY_PARAMETER);
                if (double.IsNaN(firstPay))
                    return rc;
                double firstGetOrderTime = courierType.CourierData.GetDoubleParameterValue(FIRST_GET_ORDER_TIME_PARAMETER);
                if (double.IsNaN(firstGetOrderTime))
                    return rc;
                double firstGetOrderRate = courierType.CourierData.GetDoubleParameterValue(FIRST_GET_ORDER_RATE_PARAMETER);
                if (double.IsNaN(firstGetOrderRate))
                    return rc;
                double firstDistance = courierType.CourierData.GetDoubleParameterValue(FIRST_DISTANCE_PARAMETER);
                if (double.IsNaN(firstDistance))
                    return rc;
                double additionalKilometerCost = courierType.CourierData.GetDoubleParameterValue(ADDITIONAL_KILOMETER_COST_PARAMETER);
                if (double.IsNaN(additionalKilometerCost))
                    return rc;
                double secondPay = courierType.CourierData.GetDoubleParameterValue(SECOND_PAY_PARAMETER);
                if (double.IsNaN(secondPay))
                    return rc;

                double firstTime = courierType.CourierData.GetDoubleParameterValue(FIRST_TIME_PARAMETER);
                if (double.IsNaN(firstTime))
                    return rc;
                double firstTimeRate = courierType.CourierData.GetDoubleParameterValue(FIRST_TIME_RATE_PARAMETER);
                if (double.IsNaN(firstTimeRate))
                    return rc;
                double secondTimeRate = courierType.CourierData.GetDoubleParameterValue(SECOND_TIME_RATE_PARAMETER);
                if (double.IsNaN(firstTimeRate))
                    return rc;

                // 6. Расчитываем стомость
                rc = 6;
                totalCost = firstPay;

                // 6.1 Дополнительная плата за получение отгрузки
                rc = 61;
                if (courierType.GetOrderTime > firstGetOrderTime)
                    totalCost += (firstGetOrderRate * (courierType.GetOrderTime - firstGetOrderTime));

                // 6.2 Дополнительная плата за расстояние и время до первого заказа
                rc = 62;
                double dist1 = nodeInfo[1].X / 1000.0;
                double time1 = nodeInfo[1].Y / 60.0;
                if (dist1 > firstDistance)
                    totalCost += (additionalKilometerCost * (dist1 - firstDistance));
                if (time1 > firstTime)
                    totalCost += (firstTimeRate * (time1 - firstTime));
                

                // 6.3 Оплата за дополнительные километры
                rc = 63;
                totalCost += (additionalKilometerCost * (distance - dist1));

                // 6.4 Оплата за дополнительное время
                rc = 64;
                totalCost += (secondTimeRate * (totalExecutionTime - nodeDeliveryTime[1]));

                // 6.5 Оплата за вручения, начиная со второго
                rc = 64;
                if (nodeCount > 3)
                    totalCost += (secondPay * (nodeCount - 3));

                // 7. Выход - Ok
                rc = 0;
                return rc;
            }
            catch
            {
                return rc;
            }
        }

        /// <summary>
        /// Расчет времени и стоимости отгрузки из нескольких заказов c помощью почасового курьера
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
        public static int GetTimeAndCost_HourlyCourier(ICourierType courierType, Point[] nodeInfo, double totalWeight, bool isLoop, out double[] nodeDeliveryTime, out double totalDeliveryTime, out double totalExecutionTime, out double totalCost)
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

                // 4. Устанавливаем время выходных параметров total_delivery_time и total_execution_time
                rc = 4;
                totalDeliveryTime = nodeDeliveryTime[nodeCount - 2];
                if (isLoop)
                {
                    totalExecutionTime = nodeDeliveryTime[nodeCount - 1];
                }
                else
                {
                    totalExecutionTime = totalDeliveryTime;
                    sumDist -= nodeInfo[nodeCount - 1].X;
                }

                double distance = sumDist / 1000.0;
                if (distance > courierType.MaxDistance)
                    return rc;

                // 5. Извлекаем параметры для расчета стоимости
                rc = 5;
                double hourlyRate = courierType.CourierData.GetDoubleParameterValue(HOURLY_RATE_PARAMETER);
                if (double.IsNaN(hourlyRate))
                    return rc;

                // 6. Расчитываем стоимость
                rc = 6;
                totalCost = hourlyRate * totalExecutionTime / 60.0;

                // 7. Выход - Ok
                rc = 0;
                return rc;
            }
            catch
            {
                return rc;
            }
        }

        #endregion Calc methods
    }
}
