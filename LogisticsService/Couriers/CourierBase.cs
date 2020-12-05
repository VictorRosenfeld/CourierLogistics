
namespace LogisticsService.Couriers
{
    using System;
    using System.Drawing;

    /// <summary>
    /// Базовый класс для курьеров и такси
    /// </summary>
    public class CourierBase: ICourierType
    {
        #region ICourierType implementation

        /// <summary>
        /// Тип курьера
        /// </summary>
        public CourierVehicleType VechicleType { get; private set; }

        /// <summary>
        /// Максимальный вес доставляемых заказов, кг
        /// </summary>
        public double MaxWeight { get; private set; }

        /// <summary>
        /// Стоимость часа работы, руб
        /// </summary>
        public double HourlyRate { get; private set; }

        /// <summary>
        /// Максимальное расстояние от исходной точки до точки вручения заказа, км
        /// </summary>
        public double MaxDistance { get; private set; }

        /// <summary>
        /// Максимальное число заказов в одной доставке
        /// </summary>
        public int MaxOrderCount { get; private set; }

        /// <summary>
        /// Страховка в процентах
        /// </summary>
        public double Insurance { get; private set; }

        /// <summary>
        /// Среднее время на приёмку одного заказа, мин
        /// </summary>
        public double GetOrderTime { get; private set; }

        /// <summary>
        /// Среднее время на вручение одного заказа, мин
        /// </summary>
        public double HandInTime { get; private set; }

        /// <summary>
        /// Время подачи такси
        /// </summary>
        public double StartDelay { get; private set; }

        /// <summary>
        /// Оплата за первые FirstDistance километров при использовании такси, руб
        /// </summary>
        public double FirstPay { get; private set; }

        /// <summary>
        /// Стоимость второго заказа
        /// </summary>
        public double SecondPay { get; private set; }

        /// <summary>
        /// Число первых километров, оплаченных FirstPay
        /// </summary>
        public double FirstDistance { get; private set; }

        /// <summary>
        /// Стоимость дополнительного километра (свыше FirstDistance), руб/км
        /// </summary>
        public double AdditionalKilometerCost { get; private set; }

        /// <summary>
        /// Признак такси
        /// </summary>
        public bool IsTaxi { get; private set; }

        /// <summary>
        /// Параметрический конструктор класса CourierBase
        /// </summary>
        /// <param name="parameters">Параметры курьера</param>
        public CourierBase(ICourierType parameters)
        {
            VechicleType = parameters.VechicleType;
            MaxWeight = parameters.MaxWeight;
            HourlyRate = parameters.HourlyRate;
            MaxDistance = parameters.MaxDistance;
            MaxOrderCount = parameters.MaxOrderCount;
            Insurance = parameters.Insurance;
            GetOrderTime = parameters.GetOrderTime;
            HandInTime = parameters.HandInTime;
            StartDelay = parameters.StartDelay;
            FirstPay = parameters.FirstPay;
            SecondPay = parameters.SecondPay;
            FirstDistance = parameters.FirstDistance;
            AdditionalKilometerCost = parameters.AdditionalKilometerCost;
            IsTaxi = parameters.IsTaxi;
        }

        /// <summary>
        /// Расчет времени и стоимости доставки одного заказа
        /// от магазина до вручения
        /// </summary>
        /// <param name="fromShop">Расстояние и время движения от магазина до точки доставки</param>
        /// <param name="weight">Вес заказа</param>
        /// <param name="deliveryTime">Время доставки до вручения заказа</param>
        /// <param name="executionTime">Общее время доставки</param>
        /// <param name="cost">Стоимость доставки</param>
        /// <returns>0 - расчет выполнен; иначе - расчет не выполнен</returns>
        public virtual int GetTimeAndCost(Point fromShop, double weight, out double deliveryTime, out double executionTime, out double cost)
        {
            // 1. Инициализация
            int rc = 1;
            deliveryTime = 0;
            executionTime = 0;
            cost = 0;

            try
            {

                // 2. Проверяем исходные данные
                rc = 21;
                if (fromShop.X > 1000 * this.MaxDistance)
                    return rc;
                rc = 22;
                if (weight > this.MaxWeight)
                    return rc;

                if (!this.IsTaxi)
                {
                    // 3. Расчитываем результат для курьера
                    rc = 3;
                    deliveryTime = this.GetOrderTime + fromShop.Y / 60.0 + this.HandInTime;
                    executionTime = deliveryTime;
                    cost = (1 + this.Insurance) * this.HourlyRate * executionTime / 60;
                }
                else
                {
                    // 3. Расчитываем результат для такси
                    deliveryTime = this.StartDelay + this.GetOrderTime + fromShop.Y / 60.0 + this.HandInTime;
                    executionTime = deliveryTime;
                    cost = this.FirstPay;
                    double distance = fromShop.X / 1000.0;
                    if (distance > this.FirstDistance)
                        cost += AdditionalKilometerCost * Math.Ceiling(distance - this.FirstDistance);
                }

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
        /// Расчет времени и стоимости доставки одного заказа
        /// с возвратом в магазин
        /// </summary>
        /// <param name="fromShop">Расстояние и время движения от магазина до точки доставки</param>
        /// <param name="toShop">Расстояние и время движения от точки доставки до магазина</param>
        /// <param name="weight">Вес заказа</param>
        /// <param name="deliveryTime">Время доставки до вручения заказа</param>
        /// <param name="executionTime">Общее время доставки</param>
        /// <param name="cost">Стоимость доставки</param>
        /// <returns>0 - расчет выполнен; иначе - расчет не выполнен</returns>
        public virtual int GetTimeAndCost(Point fromShop, Point toShop, double weight, out double deliveryTime, out double executionTime, out double cost)
        {
            // 1. Инициализация
            int rc = 1;
            deliveryTime = 0;
            executionTime = 0;
            cost = 0;

            try
            {

                // 2. Проверяем исходные данные
                rc = 21;
                if (this.IsTaxi)
                if ((fromShop.X + fromShop.X) > 1000 * this.MaxDistance)
                    return rc;
                rc = 22;
                if (weight > this.MaxWeight)
                    return rc;

                // 3. Расчитываем результат
                rc = 3;
                deliveryTime = this.GetOrderTime + fromShop.Y / 60.0 + this.HandInTime;
                executionTime = deliveryTime + toShop.Y / 60.0 + this.HandInTime;

                if (!this.IsTaxi)
                {
                    cost = (1 + this.Insurance) * this.HourlyRate * executionTime / 60;
                }
                else
                {
                    cost = this.FirstPay;
                    double distance = (fromShop.X + toShop.X)  / 1000.0;
                    if (distance > this.FirstDistance)
                        cost += this.AdditionalKilometerCost * Math.Ceiling(distance - this.FirstDistance);
                }

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
        /// Расчет времени и стоимости отгрузки
        /// </summary>
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
        public virtual int GetTimeAndCost(Point[] nodeInfo, double totalWeight, bool isLoop, out double[] nodeDeliveryTime, out double totalDeliveryTime, out double totalExecutionTime, out double totalCost)
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
                if (nodeInfo == null || nodeInfo.Length < 3)
                    return rc;
                rc = 22;
                if (totalWeight > this.MaxWeight)
                    return rc;
                int nodeCount = nodeInfo.Length;
                rc = 23;
                if (nodeCount - 2 > this.MaxOrderCount)
                    return rc;

                // 3. Расчитываем время доставки до всех точек доставки
                rc = 3;
                nodeDeliveryTime = new double[nodeCount];
                if (IsTaxi)
                {
                    nodeDeliveryTime[0] = this.StartDelay +  this.GetOrderTime;
                }
                else
                {
                    nodeDeliveryTime[0] = this.GetOrderTime;
                }

                int sumDist = 0;

                for (int i = 1; i < nodeCount; i++)
                {
                    sumDist += nodeInfo[i].X;
                    nodeDeliveryTime[i] = nodeDeliveryTime[i - 1] + this.HandInTime + nodeInfo[i].Y / 60.0;
                }

                if (isLoop && !this.IsTaxi)
                {
                    totalDeliveryTime = nodeDeliveryTime[nodeCount - 2];
                    totalExecutionTime = nodeDeliveryTime[nodeCount - 1];
                }
                else
                {
                    totalDeliveryTime = nodeDeliveryTime[nodeCount - 2];
                    totalExecutionTime = totalDeliveryTime;
                    sumDist -= nodeInfo[nodeCount - 1].X;
                }

                double distance = sumDist / 1000.0;
                if (distance > this.MaxDistance)
                    return rc;

                if (!this.IsTaxi)
                {
                    totalCost = (1 + this.Insurance) * this.HourlyRate * totalExecutionTime / 60;
                }
                else
                {
                    //totalDeliveryTime = nodeDeliveryTime[nodeCount - 2];
                    //totalExecutionTime = totalDeliveryTime;
                    //sumDist -= nodeInfo[nodeCount - 1].X;
                    totalCost = this.FirstPay;

                    if (distance > this.FirstDistance)
                        totalCost += this.AdditionalKilometerCost * Math.Ceiling(distance - this.FirstDistance);
                    if (nodeCount > 3)
                        totalCost += (nodeCount - 3) * this.SecondPay;
                }

                // 4. Выход - Ok
                rc = 0;
                return rc;
            }
            catch
            {
                return rc;
            }
        }

        #endregion ICourierType implementation
    }
}
