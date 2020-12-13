using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogisticsService.Couriers
{
    /// <summary>
    /// Общий интерфейс, определяющий тип курьера
    /// и позволяющий ответить на типовые вопросы
    /// </summary>
    public interface ICourierType
    {
        #region Properties

        /// <summary>
        /// Средство передвижения
        /// </summary>
        CourierVehicleType VechicleType { get; }

        // Курьеры

        /// <summary>
        /// Максимальный вес всех заказов в отгрузке
        /// </summary>
        double MaxWeight { get; }

        /// <summary>
        /// Часовая ставка
        /// </summary>
        double HourlyRate { get; }

        /// <summary>
        /// Максимальная дальность в один конец
        /// </summary>
        double MaxDistance { get; }

        /// <summary>
        /// Максимальное число заказов
        /// </summary>
        int MaxOrderCount { get; }

        /// <summary>
        /// Страховка в процентах (VechicleType = OnFoot, Bicycle, Car)
        /// </summary>
        double Insurance { get; }

        /// <summary>
        /// Время приёмки отгрузки
        /// </summary>
        double GetOrderTime { get; }

        /// <summary>
        /// Время вручения заказа
        /// </summary>
        double HandInTime { get; }

        // Такси

        /// <summary>
        /// Среднее время подачи такси, мин
        /// </summary>
        double StartDelay { get; }

        /// <summary>
        /// Стоимость первых FirstDistance километров
        /// </summary>
        double FirstPay { get; }

        /// <summary>
        /// Стоимость вручения заказа, начиная со второго заказа (VechicleType = YandexTaxi, GettTaxi)
        /// </summary>
        double SecondPay { get; }

        /// <summary>
        /// Километры, оплаченные FirstPay
        /// </summary>
        double FirstDistance { get; }

        /// <summary>
        /// Стоимость дополнительного километра
        /// </summary>
        double AdditionalKilometerCost { get; }

        /// <summary>
        /// Такси ?
        /// </summary>
        bool IsTaxi { get; }

        /// <summary>
        /// ID сервиса доставки
        /// </summary>
        int DServiceId { get; }

        /// <summary>
        /// Метод расчета времени и стоимости отгрузки 
        /// </summary>
        string CalcMethod { get; }

        /// <summary>
        /// Максимальное бесплатное время передачи отгрузки, мин
        /// </summary>
        double FirstGetOrderTime { get; }

        /// <summary>
        /// Плата за превышение бесплатного времени передачи отгрузки
        /// (руб/мин)
        /// </summary>
        double FirstGetOrderRate { get; }

        /// <summary>
        /// Предоплаченое время, мин
        /// </summary>
        double FirstTime { get; }

        /// <summary>
        /// Плата за превышение FirstTime для первого заказа
        /// </summary>
        double FirstTimeRate { get; }

        /// <summary>
        /// Плата за время для заказов, начиная со второго
        /// </summary>
        double SeсondTimeRate { get; }

        #endregion Properties

        #region Methods

        /// <summary>
        /// Подсчет времени и стоимости доставки одного заказа
        /// из магазина до вручения
        /// </summary>
        /// <param name="fromShop">Расстояние и время движения из магазина до точки вручения</param>
        /// <param name="weight">Вес заказа</param>
        /// <param name="deliveryTime">Время от начала доставки до вручения заказа</param>
        /// <param name="executionTime">Время от начала доставки до возврщения в исходную точку</param>
        /// <param name="cost">Стоимость доставки</param>
        /// <returns>0 - результат получен; иначе - результат не получен</returns>
        int GetTimeAndCost(Point fromShop, double weight, out double deliveryTime, out double executionTime, out double cost);

        /// <summary>
        /// Подсчет времени и стоимости доставки одного заказа
        /// с возвратом в магазин
        /// </summary>
        /// <param name="fromShop">Расстояние и время движения из магазина до точки вручения</param>
        /// <param name="toShop">Расстояние и время движения из точки вручения до магазина</param>
        /// <param name="weight">Вес заказа</param>
        /// <param name="deliveryTime">Время от начала доставки до вручения заказа</param>
        /// <param name="executionTime">Время от начала доставки до возврщения в исходную точку</param>
        /// <param name="cost">Стоимость доставки</param>
        /// <returns>0 - результат получен; иначе - результат не получен</returns>
        int GetTimeAndCost(Point fromShop, Point toShop, double weight, out double deliveryTime, out double executionTime, out double cost);

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
        int GetTimeAndCost(Point[] nodeInfo, double totalWeight, bool isLoop, out double[] nodeDeliveryTime, out double totalDeliveryTime, out double totalExecutionTime, out double totalCost);

        #endregion Methods
    }
}
