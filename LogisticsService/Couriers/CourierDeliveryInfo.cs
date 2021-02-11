
namespace LogisticsService.Couriers
{
    using LogisticsService.Orders;
    using LogisticsService.Shops;
    using System;
    using System.Drawing;
    using System.Linq;

    /// <summary>
    /// Информация об отгрузке
    /// </summary>
    public class CourierDeliveryInfo
    {
        /// <summary>
        /// Курьер, осуществляющий отгрузку
        /// </summary>
        public Courier DeliveryCourier { get; set; }

        /// <summary>
        /// Магазин, из которого осуществляется отгрузка
        /// </summary>
        public Shop FromShop { get; set; }

        /// <summary>
        /// Отгружаемые заказы
        /// </summary>
        public Order[] Orders { get; set; }

        /// <summary>
        /// Время проведения расчетов
        /// </summary>
        public DateTime CalculationTime { get; private set; }

        /// <summary>
        /// Число заказов в отгрузке
        /// </summary>
        public int OrderCount => (Orders == null ? 0 : Orders.Length);

        /// <summary>
        /// Средняя стоимость доставки одного заказа
        /// </summary>
        public double OrderCost => (OrderCount > 0 ? Cost / OrderCount : 0);

        /// <summary>
        /// Общий вес, кг
        /// </summary>
        public double Weight => (Orders == null ? 0 : Orders.Sum(p => p.Weight));

        /// <summary>
        /// Время от момента назначения курьера до вручения последнего товара в отгрузке, мин
        /// </summary>
        public double DeliveryTime { get; set; }

        /// <summary>
        /// Полное время на отгрузку, мин
        /// </summary>
        public double ExecutionTime { get; set; }

        /// <summary>
        /// Флаг возврата в исходную точку:
        /// true - возврат в магазин; 
        /// false - отгрузка завершается при вручении последнего заказа.
        /// </summary>
        public bool IsLoop { get; set; }

        /// <summary>
        /// Резерв времени на момент расчетов
        /// </summary>
        public TimeSpan ReserveTime { get; set; }

        /// <summary>
        /// Самое раннее время, когда
        /// может быть начата отгрузка 
        /// </summary>
        public DateTime StartDeliveryInterval { get; set; }

        /// <summary>
        /// Самое позднее время, когда
        /// доставка может быть выполнена вовремя
        /// </summary>
        public DateTime EndDeliveryInterval { get; set; }

        /// <summary>
        /// Стоимость доставки
        /// </summary>
        public double Cost { get; set; }

        /// <summary>
        /// Действительное время начала доставки
        /// (StartDeliveryInterval ≤ StartDelivery ≤ EndDeliveryInterval)
        /// </summary>
        public DateTime StartDelivery { get; set; }

        /// <summary>
        /// Расстояние и время движения между звеньями пути доставки
        /// </summary>
        public Point[] NodeInfo { get; set; }

        /// <summary>
        /// Флаг: 
        /// true - все заказы в отгрузке помечены, как отгруженные
        /// false - все заказы в отгрузке помечены, как  отгруженные
        /// </summary>
        public bool Completed { get; private set; }

        /// <summary>
        /// Пометить все заказы, как отгруженные
        /// </summary>
        public void SetCompleted(bool value)
        {
            if (Orders != null)
            {
                for (int i = 0; i < Orders.Length; i++)
                {
                    Orders[i].Completed = value;
                }
            }

            Completed = value;

            if (value)
            {
                if (DeliveryCourier != null)
                {
                    if (!DeliveryCourier.IsTaxi)
                        DeliveryCourier.Status = CourierStatus.DeliversOrder;
                }
            }
        }

        /// <summary>
        /// Все заказы в отгрузке уже доставлены ?
        /// </summary>
        /// <returns>true - все заказы доставлены; false - имеются не доставленные заказы или они осутствуют</returns>
        public bool IsCompleted()
        {
            try
            {
                // 2. Проверяем исходные данные
                if (Orders == null || Orders.Length <= 0)
                    return false;

                // 3. Проверяем доставку всех заказов в отгрузке
                foreach (Order order in Orders)
                {
                    if (!order.Completed)
                        return false;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Время доставки от начала отгрузки до прибытия в точку вручения, мин
        /// </summary>
        public double[] NodeDeliveryTime { get; set; }

        ///// <summary>
        ///// Индекс элемента очереди событий,
        ///// с которым связан данный instance
        ///// </summary>
        //public int QueueItemIndex { get; set; }

        ///// <summary>
        ///// Время необходимое для прибытия курьера
        ///// из текущего положения до магазина
        ///// </summary>
        //public TimeSpan CourierArrivalTime { get; set; }

        /// <summary>
        /// Параметрический конструктор класса CourierDeliveryInfo
        /// </summary>
        /// <param name="courier">Курьер</param>
        /// <param name="fromShop">Магазин, из которого осуществляется отгрузка</param>
        /// <param name="orders">Отгружаемые заказы</param>
        /// <param name="calculationTime">Начало отгрузки (время расчета)</param>
        /// <param name="isLoop">Флаг возврата в магазин</param>
        public CourierDeliveryInfo(Courier courier, /*Shop fromShop,*/ Order[] orders, DateTime calculationTime, bool isLoop)
        {
            DeliveryCourier = courier;
            //FromShop = fromShop;
            Orders = orders;
            IsLoop = isLoop;
            CalculationTime = calculationTime;
            StartDelivery = calculationTime;
        }

        /// <summary>
        /// Координаты последнего заказа
        /// </summary>
        /// <param name="latitude">Широта</param>
        /// <param name="longitude">Долгота</param>
        /// <returns>true - координаты выбраны; иначе координаты не выбраны (значения = 0)</returns>
        public bool GetLastOrderLatLong(out double latitude, out double longitude)
        {
            // 1. Инициализация
            latitude = 0;
            longitude = 0;

            try
            {
                // 2. Выбираем координаты последнего заказа в отгрузке
                if (Orders == null || Orders.Length <= 0)
                    return false;
                Order lastOrder = Orders[Orders.Length - 1];

                latitude = lastOrder.Latitude;
                longitude = lastOrder.Longitude;

                // 3. Выход - Ok
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Флаг: true - все заказы в отгрузке собраны; false - в отгрузке имеются не собранные заказы
        /// </summary>
        public bool HasAssembledOnly => (Orders == null ? false : Orders.FirstOrDefault(p => p.Status == OrderStatus.Receipted) == null);

        /// <summary>
        /// Проверка наличия заказа в отгрузке
        /// </summary>
        /// <returns>true - все заказы доставлены; false - имеются не доставленные заказы или они осутствуют</returns>
        public bool ContainsOrder(int orderId)
        {
            try
            {
                // 2. Проверяем исходные данные
                if (Orders == null || Orders.Length <= 0)
                    return false;

                // 3. Проверяем наличие заказа среди отгружаемых
                foreach (Order order in Orders)
                {
                    if (order.Id == orderId)
                        return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }
    }
}
