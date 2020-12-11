
namespace LogisticsService.Couriers
{
    using System;
    using System.Drawing;
    using System.Linq;
    using System.Reflection;

    /// <summary>
    /// Вычисление времени
    /// </summary>
    public class TimeAndCostCalculator
    {
        public delegate int GetTimeAndCostDelegate1(ICourierType courierType, Point fromShop, double weight, out double deliveryTime, out double executionTime, out double cost);
        public delegate int GetTimeAndCostDelegate2(ICourierType courierType, Point fromShop, Point toShop, double weight, out double deliveryTime, out double executionTime, out double cost);
        public delegate int GetTimeAndCostDelegate3(ICourierType courierType, Point[] nodeInfo, double totalWeight, bool isLoop, out double[] nodeDeliveryTime, out double totalDeliveryTime, out double totalExecutionTime, out double totalCost);

        /// <summary>
        /// Префикс методов расчета
        /// </summary>
        private const string METHOD_PREFIX = "GetTimeAndCost_";

        /// <summary>
        /// Количество параметров делегата GetTimeAndCostDelegate1
        /// </summary>
        private const int DELEGATE1_PARAMETER_COUNT = 6;

        /// <summary>
        /// Количество параметров делегата GetTimeAndCostDelegate2
        /// </summary>
        private const int DELEGATE2_PARAMETER_COUNT = 7;

        /// <summary>
        /// Количество параметров делегата GetTimeAndCostDelegate3
        /// </summary>
        private const int DELEGATE3_PARAMETER_COUNT = 8;

        /// <summary>
        /// Извлечение делегатов для подсчета времени
        /// и стоимости отгрузки для заданного способа доставки
        /// </summary>
        /// <param name="methodName">Способ доставки</param>
        /// <param name="delegate1">Делегат для доставки одного заказа без возврата в магазин</param>
        /// <param name="delegate2">Делегат для доставки одного заказа с возвратом в магазин</param>
        /// <param name="delegate3">Делегат для доставки нескольких заказов</param>
        /// <returns></returns>
        public static int GetTimeAndCostDelеgates(string methodName, out GetTimeAndCostDelegate1 delegate1, out GetTimeAndCostDelegate2 delegate2, out GetTimeAndCostDelegate3 delegate3)
        {
            // 1. Инциализация
            int rc = 1;
            delegate1 = null;
            delegate2 = null;
            delegate3 = null;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (string.IsNullOrWhiteSpace(methodName))
                    return rc;

                // 3. Извлекаем методы, соответствующие methodName
                rc = 3;
                string findMethod = METHOD_PREFIX + methodName;
                MethodInfo[] methods = typeof(TimeAndCostCalculator).GetMethods(BindingFlags.Public | BindingFlags.Static).Where(method => method.Name.Equals(findMethod, StringComparison.CurrentCultureIgnoreCase)).ToArray();
                if (methods == null || methods.Length != 3)
                    return rc;

                // 4. Устанавливаем результат
                rc = 4;

                foreach (MethodInfo method in methods)
                {
                    switch (method.GetParameters().Length)
                    {
                        case DELEGATE1_PARAMETER_COUNT:
                            delegate1 = (GetTimeAndCostDelegate1)Delegate.CreateDelegate(typeof(GetTimeAndCostDelegate1), null, method);
                            break;
                        case DELEGATE2_PARAMETER_COUNT:
                            delegate2 = (GetTimeAndCostDelegate2)Delegate.CreateDelegate(typeof(GetTimeAndCostDelegate2), null, method);
                            break;
                        case DELEGATE3_PARAMETER_COUNT:
                            delegate3 = (GetTimeAndCostDelegate3)Delegate.CreateDelegate(typeof(GetTimeAndCostDelegate3), null, method);
                            break;
                    }
                }

                if (delegate1 == null || delegate2 == null || delegate3 == null)
                    return rc;

                // 5. Выход - Ok
                rc = 0;
                return rc;
            }
            catch
            {
                return rc;
            }
        }

        #region HourlyCourier. Почасовые курьеры 

        /// <summary>
        /// Подсчет времени и стоимости доставки одного заказа
        /// без возврата в магазин для почасового курьера
        /// </summary>
        /// <param name="courierType">Параметры курьера</param>
        /// <param name="fromShop">Растояние и время движения из магазина в точку вручения</param>
        /// <param name="weight">Вес заказа</param>
        /// <param name="deliveryTime">Время до момента вручения, мин</param>
        /// <param name="executionTime">Время до момента вручения, мин</param>
        /// <param name="cost">Стоимость отгрузки</param>
        /// <returns>0 - подсчет выполнен; иначе - подсчет не выполнен</returns>
        public static int GetTimeAndCost_HourlyCourier(ICourierType courierType, Point fromShop, double weight, out double deliveryTime, out double executionTime, out double cost)
        {
            // 1. Инициализация
            int rc = 1;
            deliveryTime = 0;
            executionTime = 0;
            cost = 0;

            try
            {

                // 2. Проверяем исходные данные
                rc = 20;
                if (courierType == null)
                    return rc;
                rc = 21;
                if (fromShop.X > 1000 * courierType.MaxDistance)
                    return rc;
                rc = 22;
                if (weight > courierType.MaxWeight)
                    return rc;

                // 3. Производим вычисление времени и стоимости
                rc = 3;
                deliveryTime = courierType.GetOrderTime + fromShop.Y / 60.0 + courierType.HandInTime;
                executionTime = deliveryTime;
                cost = (1 + courierType.Insurance) * courierType.HourlyRate * executionTime / 60;

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
        /// Подсчет времени и стоимости доставки одного заказа
        /// с возвратом в магазин для почасового курьера
        /// </summary>
        /// <param name="courierType">Параметры курьера</param>
        /// <param name="fromShop">Растояние и время движения из магазина в точку вручения</param>
        /// <param name="toShop">Растояние и время движенияв из точки вручения в магазин</param>
        /// <param name="weight">Вес заказа</param>
        /// <param name="deliveryTime">Время до момента вручения, мин</param>
        /// <param name="executionTime">Время до момента вручения, мин</param>
        /// <param name="cost">Стоимость отгрузки</param>
        /// <returns>0 - подсчет выполнен; иначе - подсчет не выполнен</returns>
        public static int GetTimeAndCost_HourlyCourier(ICourierType courierType, Point fromShop, Point toShop, double weight, out double deliveryTime, out double executionTime, out double cost)
        {
            // 1. Инициализация
            int rc = 1;
            deliveryTime = 0;
            executionTime = 0;
            cost = 0;

            try
            {

                // 2. Проверяем исходные данные
                rc = 20;
                if (courierType == null)
                    return rc;
                rc = 21;
                if (fromShop.X > 1000 * courierType.MaxDistance)
                    return rc;
                rc = 22;
                if (weight > courierType.MaxWeight)
                    return rc;

                // 3. Производим вычисление времени и стоимости
                rc = 3;
                deliveryTime = courierType.GetOrderTime + fromShop.Y / 60.0 + courierType.HandInTime;
                executionTime = deliveryTime + toShop.Y / 60.0 + courierType.HandInTime;
                cost = (1 + courierType.Insurance) * courierType.HourlyRate * executionTime / 60;

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
        /// Расчет времени и стоимости отгрузки из нескольких заказов для почасового курьера
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
                nodeDeliveryTime[0] = courierType.GetOrderTime;

                int sumDist = 0;

                for (int i = 1; i < nodeCount; i++)
                {
                    sumDist += nodeInfo[i].X;
                    nodeDeliveryTime[i] = nodeDeliveryTime[i - 1] + courierType.HandInTime + nodeInfo[i].Y / 60.0;
                }

                if (isLoop)
                {
                    totalDeliveryTime = nodeDeliveryTime[nodeCount - 2];
                    totalExecutionTime = nodeDeliveryTime[nodeCount - 1];
                }

                double distance = sumDist / 1000.0;
                if (distance > courierType.MaxDistance)
                    return rc;

                totalCost = (1 + courierType.Insurance) * courierType.HourlyRate * totalExecutionTime / 60;

                // 4. Выход - Ok
                rc = 0;
                return rc;
            }
            catch
            {
                return rc;
            }
        }

        #endregion HourlyCourier. Почасовые курьеры

        #region Taxi. Такси

        /// <summary>
        /// Подсчет времени и стоимости доставки одного заказа
        /// без возврата в магазин для такси
        /// </summary>
        /// <param name="courierType">Параметры курьера</param>
        /// <param name="fromShop">Растояние и время движения из магазина в точку вручения</param>
        /// <param name="weight">Вес заказа</param>
        /// <param name="deliveryTime">Время до момента вручения, мин</param>
        /// <param name="executionTime">Время до момента вручения, мин</param>
        /// <param name="cost">Стоимость отгрузки</param>
        /// <returns>0 - подсчет выполнен; иначе - подсчет не выполнен</returns>
        public static int GetTimeAndCost_Taxi(ICourierType courierType, Point fromShop, double weight, out double deliveryTime, out double executionTime, out double cost)
        {
            // 1. Инициализация
            int rc = 1;
            deliveryTime = 0;
            executionTime = 0;
            cost = 0;

            try
            {

                // 2. Проверяем исходные данные
                rc = 20;
                if (courierType == null)
                    return rc;
                rc = 21;
                if (fromShop.X > 1000 * courierType.MaxDistance)
                    return rc;
                rc = 22;
                if (weight > courierType.MaxWeight)
                    return rc;

                // 3. Производим вычисление времени и стоимости
                rc = 3;
                deliveryTime = courierType.StartDelay + courierType.GetOrderTime + fromShop.Y / 60.0 + courierType.HandInTime;
                executionTime = deliveryTime;
                cost = courierType.FirstPay;
                double distance = fromShop.X / 1000.0;
                if (distance > courierType.FirstDistance)
                    cost += courierType.AdditionalKilometerCost * Math.Ceiling(distance - courierType.FirstDistance);

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
        /// Подсчет времени и стоимости доставки одного заказа
        /// с возвратом в магазин для такси
        /// </summary>
        /// <param name="courierType">Параметры курьера</param>
        /// <param name="fromShop">Растояние и время движения из магазина в точку вручения</param>
        /// <param name="toShop">Растояние и время движенияв из точки вручения в магазин</param>
        /// <param name="weight">Вес заказа</param>
        /// <param name="deliveryTime">Время до момента вручения, мин</param>
        /// <param name="executionTime">Время до момента вручения, мин</param>
        /// <param name="cost">Стоимость отгрузки</param>
        /// <returns>0 - подсчет выполнен; иначе - подсчет не выполнен</returns>
        public static int GetTimeAndCost_Taxi(ICourierType courierType, Point fromShop, Point toShop, double weight, out double deliveryTime, out double executionTime, out double cost)
        {
            // 1. Инициализация
            int rc = 1;
            deliveryTime = 0;
            executionTime = 0;
            cost = 0;

            try
            {
                // 2. Проверяем исходные данные
                rc = 20;
                if (courierType == null)
                    return rc;
                rc = 21;
                if (fromShop.X > 1000 * courierType.MaxDistance)
                    return rc;
                rc = 22;
                if (weight > courierType.MaxWeight)
                    return rc;

                // 3. Производим вычисление времени и стоимости
                rc = 3;
                deliveryTime = courierType.GetOrderTime + fromShop.Y / 60.0 + courierType.HandInTime;
                executionTime = deliveryTime + toShop.Y / 60.0 + courierType.HandInTime;

                cost = courierType.FirstPay;
                double distance = (fromShop.X + toShop.X) / 1000.0;
                if (distance > courierType.FirstDistance)
                    cost += courierType.AdditionalKilometerCost * Math.Ceiling(distance - courierType.FirstDistance);

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

                totalCost = courierType.FirstPay;

                if (distance > courierType.FirstDistance)
                    totalCost += courierType.AdditionalKilometerCost * Math.Ceiling(distance - courierType.FirstDistance);
                if (nodeCount > 3)
                    totalCost += (nodeCount - 3) * courierType.SecondPay;

                // 4. Выход - Ok
                rc = 0;
                return rc;
            }
            catch
            {
                return rc;
            }
        }

        #endregion Taxi. Такси
    }
}
