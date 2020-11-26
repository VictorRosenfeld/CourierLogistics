

namespace CourierLogistics.SourceData.Couriers
{
    using System;

    /// <summary>
    /// Яндекс-такси
    /// </summary>
    public class CourierType_YandexTaxi : ICourierType
    {
        /// <summary>
        /// Тип курьера
        /// </summary>
        public CourierVehicleType VechicleType => CourierVehicleType.YandexTaxi;

        /// <summary>
        /// Максимальный вес доставляемых заказов, кг
        /// </summary>
        public double MaxWeight => 3000;

        /// <summary>
        /// Стоимость часа работы, руб
        /// </summary>
        public double HourlyRate => 0;

        /// <summary>
        /// Максимальное расстояние от исходной точки до точки вручения заказа, км
        /// </summary>
        public double MaxDistance => 100;

        /// <summary>
        /// Максимальное число заказов в одной доставке
        /// </summary>
        public int MaxOrderCount => 40;

        /// <summary>
        /// Средняя скорость движения, км/час
        /// </summary>
        public double AverageVelocity => 20;

        /// <summary>
        /// Процент налога при доставке на авто
        /// </summary>
        public double Insurance => 0;

        /// <summary>
        /// Среднее время на приёмку одного заказа, мин
        /// </summary>
        public double GetOrderTime => 3;

        /// <summary>
        /// Среднее время на вручение одного заказа, мин
        /// </summary>
        public double HandInTime => 7;

        /// <summary>
        /// Время подачи такси
        /// </summary>
        public double StartDelay => 18;

        /// <summary>
        /// Оплата за первые FirstDistance километров при использовании такси, руб
        /// </summary>
        public double FirstPay => 149;

        /// <summary>
        /// Стоимость второго заказа
        /// </summary>
        public double SecondPay => 80;

        /// <summary>
        /// Число первых километров, оплаченных FirstPay
        /// </summary>
        public double FirstDistance => 2;

        /// <summary>
        /// Стоимость дополнительного километра (свыше FirstDistance), руб/км
        /// </summary>
        public double AdditionalKilometerCost => 15;

        /// <summary>
        /// Время и стоимость доставки одного товара
        /// </summary>
        /// <param name="distance">Расстояние до точки доставки, км</param>
        /// <param name="weight">Вес заказа</param>
        /// <param name="deliveryTime">Время от старта до прибытия в точку доставки, мин</param>
        /// <param name="executionTime">Время от старта до возвращения в исходную точку, мин</param>
        /// <param name="cost">Стоимость доставки, руб</param>
        /// <returns>0 - результат получен; иначе - результат не получен</returns>
        public int GetTimeAndCost(double distance, double weight, out double deliveryTime, out double executionTime, out double cost)
        {
            // 1. Инициализация
            int rc = 1;
            deliveryTime = 0;
            executionTime = 0;
            cost = 0;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                //if (distance > MaxDistance)
                //    return rc;
                //if (weight > MaxWeight)
                //    return rc;

                // 3. Расчитываем результат
                rc = 3;
                deliveryTime = StartDelay + GetOrderTime + 60 * distance / AverageVelocity + HandInTime;
                executionTime = deliveryTime;
                cost = FirstPay;
                if (distance > FirstDistance)
                    cost += AdditionalKilometerCost * Math.Ceiling(distance - FirstDistance);


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
        /// Подсчет времени и стоимости доставки нескольких заказов
        /// </summary>
        /// <param name="nodeDistance">Расстояние от точки старта до первой точки доставки, от первой точки до второй точки, ... , от последней точки до точки старта</param>
        /// <param name="totalWeight">Общий вес всех заказов</param>
        /// <param name="nodeDeliveryTime">Расчетное время доставки в заданную точку, мин</param>
        /// <param name="totalDeliveryTime">Суммарное время доставки всех заказов (время от старта до вручения последнего заказа)</param>
        /// <param name="totalExecutionTime">Общее затраченное время (для такси = totalDeliveryTime; для курьера = totalDeliveryTime + время возвращения в точку старта)</param>
        /// <param name="totalCost">Общая стоимость доставки</param>
        /// <returns>0 - результат получен; иначе - результат не получен</returns>
        public int GetTimeAndCost(double[] nodeDistance, double totalWeight, out double[] nodeDeliveryTime, out double totalDeliveryTime, out double totalExecutionTime, out double totalCost)
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
                rc = 2;
                if (nodeDistance == null || nodeDistance.Length < 3)
                    return rc;
                //if (totalWeight > MaxWeight)
                //    return rc;

                // 3. Расчитываем время доставки во все точки доставки
                rc = 3;
                nodeDeliveryTime = new double[nodeDistance.Length];
                nodeDeliveryTime[0] = GetOrderTime;
                double sumDist = 0;

                for (int i = 1; i < nodeDistance.Length - 1; i++)
                {
                    sumDist += nodeDistance[i];
                    nodeDeliveryTime[i] = StartDelay + GetOrderTime + i * HandInTime + 60 * sumDist / AverageVelocity;
                }

                totalDeliveryTime = nodeDeliveryTime[nodeDeliveryTime.Length - 2];
                totalExecutionTime = totalDeliveryTime;
                totalCost = FirstPay;
                //sumDist -= nodeDistance[nodeDistance.Length - 1];
                if (sumDist > FirstDistance)
                    totalCost += AdditionalKilometerCost * Math.Ceiling(sumDist - FirstDistance);
                if (nodeDistance.Length > 3)
                    totalCost += (nodeDistance.Length - 3) * SecondPay;

                if (nodeDistance.Length - 2 > MaxOrderCount)
                    return rc;

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
