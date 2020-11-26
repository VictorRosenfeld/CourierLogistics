
namespace CourierLogistics.Logistics.FloatSolution
{
    public class FloatSolutionParameters
    {
        /// <summary>
        /// Надбавка для преобразования
        /// сферического расстония между точками на земной поверхности в реальное
        /// </summary>
        public const double DISTANCE_ALLOWANCE = 1.2;

        /// <summary>
        /// Передельное время доставки, мин
        /// </summary>
        public const double DELIVERY_LIMIT = 120;

        /// <summary>
        /// Максимальное растояние от курьера до
        /// магазина, в котором он может взять заказы, км
        /// </summary>
        public const double COURIER_DISTANCE_TO_SHOP_LIMIT = 5;

        /// <summary>
        /// Минимальный интервал времени до оптимальной отгрузки
        /// </summary>
        public const double DELIVERY_ALERT_INTERVAL = 1;

        /// <summary>
        /// Минимальное число магазинов для освободившегося курьера
        /// </summary>
        public const int MIN_AVAILABLE_SHOP_COUNT = 3;

        /// <summary>
        /// Максимальное расстояние до доступного магазина, км
        /// </summary>
        public const double MAX_DISTANCE_TO_AVAILABLE_SHOP = 15;
        //public const double MAX_DISTANCE_TO_AVAILABLE_SHOP = 12;

        /// <summary>
        /// Предел для рабочего времени курьера, час
        /// </summary>
        public const double COURIER_WORK_TIME_LIMIT = 10;

        /// <summary>
        /// Предел для рабочего времени курьера, час
        /// </summary>
        public const double COURIER_MIN_WORK_TIME = 4;

        /// <summary>
        /// Максимальное число заказов для построения оптимального решения
        /// </summary>
        public const int MAX_ORDERS_FOR_OPTIMAL_SOLUTION = 15;

        /// <summary>
        /// Максимальное число заказов среди которых ищется оптимальное
        /// при построении покрытия
        /// </summary>
        public const int MAX_ORDERS_FOR_COVER_SOLUTION = 10;
    }
}
