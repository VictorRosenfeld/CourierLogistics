
namespace SQLCLR.MaxOrdersOfRoute
{
    /// <summary>
    /// Ограничение по числу заказов
    /// на длину создаваемого маршрута
    /// </summary>
    public class MaxOrdersOfRouteRecord
    {
        /// <summary>
        /// Длина пути
        /// (число заказов в отгрузке)
        /// </summary>
        public int RouteLength { get; private set; }

        /// <summary>
        /// Максимальное число заказов,
        /// для которого строятся отгрузки
        /// с длиной маршрута RouteLength
        /// </summary>
        public int MaxOrders { get; private set; }

        /// <summary>
        /// Параметрический конструктор класса MaxOrdersOfRouteRecord
        /// </summary>
        /// <param name="routeLength">Длина маршрута</param>
        /// <param name="maxOrders">Максимальное число заказов</param>
        public MaxOrdersOfRouteRecord(int routeLength, int maxOrders)
        {
            RouteLength = routeLength;
            MaxOrders = maxOrders;
        }
    }
}
