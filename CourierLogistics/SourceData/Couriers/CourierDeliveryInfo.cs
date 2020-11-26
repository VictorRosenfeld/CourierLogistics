
namespace CourierLogistics.SourceData.Couriers
{
    using CourierLogistics.SourceData.Orders;
    using System;

    /// <summary>
    /// Информация об отгрузке
    /// </summary>
    public class CourierDeliveryInfo
    {
        /// <summary>
        /// Курьер
        /// </summary>
        public Courier DeliveryCourier { get; set;  }

        /// <summary>
        /// Время проведения расчетов
        /// </summary>
        public DateTime CalculationTime { get; private set;  }

        /// <summary>
        /// Число заказов
        /// </summary>
        public int OrderCount { get; private set;  }

        /// <summary>
        /// Средняя стоимость доставки одного заказа
        /// </summary>
        public double OrderCost => (OrderCount > 0 ? Cost / OrderCount : 0);

        /// <summary>
        /// Общий вес, кг
        /// </summary>
        public double Weight { get; private set;  }

        /// <summary>
        /// Время от момента назначения курьера до вручения, мин
        /// </summary>
        public double DeliveryTime { get; set;  }

        /// <summary>
        /// Время от момента назначения курьера до возврата в исходную точку, мин
        /// </summary>
        public double ExecutionTime { get; set;  }

        /// <summary>
        /// Резерв времени на момент расчетов
        /// </summary>
        public TimeSpan ReserveTime { get; set;  }

        /// <summary>
        /// Самое раннее время, когда
        /// может быть начата доставка 
        /// </summary>
        public DateTime StartDeliveryInterval { get; set;  }

        /// <summary>
        /// Самое позднее время, когда
        /// доставка может быть выполнена вовремя
        /// </summary>
        public DateTime EndDeliveryInterval { get; set;  }

        /// <summary>
        /// Стоимость доставки
        /// </summary>
        public double Cost { get; set;  }

        /// <summary>
        /// Доставляемый заказ
        /// </summary>
        public Order ShippingOrder { get; set;  }

        /// <summary>
        /// Действительное время начала доставки
        /// (StartDeliveryInterval ≤ StartDelivery ≤ EndDeliveryInterval)
        /// </summary>
        public DateTime StartDelivery { get; set;  }

        /// <summary>
        /// Доставленные заказы
        /// </summary>
        public CourierDeliveryInfo[] DeliveredOrders { get; set;  }

        /// <summary>
        /// Расстояние от магазина до точки доставки
        /// </summary>
        public double DistanceFromShop { get; set;  }

        /// <summary>
        /// Флаг: true - заказ обработан; false - заказ не обработан
        /// </summary>
        //public bool Completed { get; set; }

        /// <summary>
        /// Флаг: true - заказ обработан; false - заказ не обработан
        /// </summary>
        public bool Completed
        {
            get
            {
                return (ShippingOrder == null ? false : ShippingOrder.Completed);
            }
            set
            {
                if (ShippingOrder != null)
                    ShippingOrder.Completed = value;
            }
        }

        /// <summary>
        /// Время доставки от начала отгрузки до прибытия в точку вручения, мин
        /// </summary>
        public double[] NodeDeliveryTime { get; set; }

        /// <summary>
        /// Раасстояние от предыдущей точки вручения до текущей, км
        /// </summary>
        public double[] NodeDistance { get; set; }

        /// <summary>
        /// Индекс элемента очереди событий,
        /// с которым связан данный instance
        /// </summary>
        public int QueueItemIndex { get; set; }

        /// <summary>
        /// Время необходимое для прибытия курьера
        /// из текущего положения до магазина
        /// </summary>
        public TimeSpan CourierArrivalTime { get; set; }

        /// <summary>
        /// Параметрический конструктор класса CourierDeliveryInfo
        /// </summary>
        /// <param name="courier">Курьер</param>
        /// <param name="calculationTime">Начало отгрузки (время расчета)</param>
        /// <param name="orderCount">Количество заказов в отгрузке</param>
        /// <param name="weight">Вес всех заказов</param>
        public CourierDeliveryInfo(Courier courier, DateTime calculationTime, int orderCount, double weight)
        {
            DeliveryCourier = courier;
            CalculationTime = calculationTime;
            StartDelivery = calculationTime;
            OrderCount = orderCount;
            Weight = weight;
            CourierArrivalTime = TimeSpan.Zero;
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
                Order lastOrder = null;
                if (DeliveredOrders != null && DeliveredOrders.Length > 0)
                {
                    lastOrder = DeliveredOrders[DeliveredOrders.Length - 1].ShippingOrder;
                }
                else
                {
                    lastOrder = ShippingOrder;
                }

                if (lastOrder == null)
                    return false;

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
    }
}
