
namespace DeliveryBuilder.Deliveries
{
    using DeliveryBuilder.Couriers;
    using DeliveryBuilder.Geo;
    using DeliveryBuilder.Orders;
    using DeliveryBuilder.Shops;
    using System;

    /// <summary>
    /// Данные отгрузки
    /// </summary>
    public class CourierDeliveryInfo
    {
        /// <summary>
        /// dlvID 
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Курьер, осуществляющий отгрузку
        /// </summary>
        public Courier DeliveryCourier { get; set; }

        /// <summary>
        /// Магазин, из которого осуществляется отгрузка
        /// </summary>
        public Shop FromShop { get; set; }

        /// <summary>
        /// Заказы в порядке вручения
        /// </summary>
        private Order[] _orders;

        /// <summary>
        /// Заказы в порядке вручения
        /// </summary>
        public Order[] Orders { get => _orders; set => _orders = value; }

        /// <summary>
        /// Время проведения расчетов
        /// </summary>
        public DateTime CalculationTime { get; set; }

        /// <summary>
        /// Число заказов в отгрузке
        /// </summary>
        public int OrderCount => (_orders == null ? 0 : _orders.Length);

        /// <summary>
        /// Средняя стоимость доставки одного заказа
        /// </summary>
        public double OrderCost => (OrderCount > 0 ? Cost / OrderCount : 0);

        /// <summary>
        /// Общий вес, кг
        /// </summary>
        public double Weight { get; set; }

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
        /// Время события в очереди отгрузки
        /// </summary>
        public DateTime EventTime { get; set; }

        /// <summary>
        /// Стоимость доставки
        /// </summary>
        public double Cost { get; set; }

        /// <summary>
        /// Расстояние и время движения между звеньями пути доставки
        /// </summary>
        public Point[] NodeInfo { get; set; }

        /// <summary>
        /// Флаг: true - отгрузка содержит не собранные заказы; false - все заказы в отгрузке собраны
        /// </summary>
        public bool IsReceipted { get; set; }

        /// <summary>
        /// Приритет отгрузки = максимальному приоритету из входящих в отгрузку заказов
        /// </summary>
        public int Priority { get; set;  }

        /// <summary>
        /// Условие отгрузки
        /// </summary>
        public int Cause { get; set;  }

        /// <summary>
        /// Время доставки от начала отгрузки до прибытия в точку вручения, мин
        /// </summary>
        public double[] NodeDeliveryTime { get; set; }

        /// <summary>
        /// Параметрический конструктор класса CourierDeliveryInfo
        /// </summary>
        /// <param name="courier">Курьер</param>
        /// <param name="fromShop">Магазин, из которого осуществляется отгрузка</param>
        /// <param name="orders">Отгружаемые заказы</param>
        /// <param name="calculationTime">Начало отгрузки (время расчета)</param>
        /// <param name="isLoop">Флаг возврата в магазин</param>
        public CourierDeliveryInfo(Courier courier, Shop fromShop, Order[] orders, DateTime calculationTime, bool isLoop)
        {
            DeliveryCourier = courier;
            FromShop = fromShop;
            Orders = orders;
            IsLoop = isLoop;
            CalculationTime = calculationTime;
            //StartDelivery = calculationTime;
        }
       
        /// <summary>
        /// Копирование отгрузки
        /// </summary>
        /// <param name="copy">Отгрузка, в которую копируется данная</param>
        public void CopyTo(CourierDeliveryInfo copy)
        {
            copy.DeliveryCourier = DeliveryCourier;
            copy.FromShop = FromShop;
            copy.Orders = _orders;
            copy.CalculationTime = CalculationTime;
            copy.IsLoop = IsLoop;
            copy.DeliveryTime = DeliveryTime;
            copy.ExecutionTime = ExecutionTime;
            copy.ReserveTime = ReserveTime;
            copy.StartDeliveryInterval = StartDeliveryInterval;
            copy.EndDeliveryInterval = EndDeliveryInterval;
            copy.Cost = Cost;
            copy.Weight = Weight;
            copy.NodeInfo = NodeInfo;
            copy.NodeDeliveryTime = NodeDeliveryTime;
            copy.Priority = Priority;
            copy.Cause = Cause;
        }
    }
}
