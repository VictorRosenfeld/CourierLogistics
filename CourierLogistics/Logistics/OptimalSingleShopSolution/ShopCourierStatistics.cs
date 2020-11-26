
namespace CourierLogistics.Logistics.OptimalSingleShopSolution
{
    using CourierLogistics.SourceData.Couriers;
    using System;

    /// <summary>
    /// Статистика по работе курьера за день
    /// </summary>
    public class ShopCourierStatistics
    {
        /// <summary>
        /// Уникальный ключ записи
        /// </summary>
        /// <returns></returns>
        public string Key => GetKey(ShopId, Date, CourierId);

        /// <summary>
        /// ID магазина
        /// </summary>
        public int ShopId { get; private set; }

        /// <summary>
        /// День
        /// </summary>
        public DateTime Date { get; private set; }

        /// <summary>
        /// Курьер
        /// </summary>
        public Courier ShopCourier { get; private set; }

        /// <summary>
        /// ID курьера
        /// </summary>
        public int CourierId => (ShopCourier == null ? 0 : ShopCourier.Id);

        /// <summary>
        /// Тип курьера
        /// </summary>
        public CourierVehicleType CourierType => (ShopCourier == null ? CourierVehicleType.Unknown : ShopCourier.CourierType.VechicleType);

        /// <summary>
        /// Строковое представление типа курьера
        /// </summary>
        /// <returns></returns>
        public string CourierTypeToString() => Enum.GetName(CourierType.GetType(), CourierType);

        /// <summary>
        /// Общее число отгруженных заказов
        /// </summary>
        public int OrderCount { get; set;  }

        /// <summary>
        /// Общий вес всех заказов
        /// </summary>
        public double TotalWeight { get; set;  }

        /// <summary>
        /// Общая стоимость дотставки всех заказов
        /// </summary>
        public double TotalCost { get; private set;  }

        /// <summary>
        /// Средняя стоимость доставки одного заказа
        /// </summary>
        public double OrderCost => (OrderCount <= 0 ? 0 : TotalCost / OrderCount);

        /// <summary>
        /// Время начала работы (время начала первой отгрузки)
        /// </summary>
        public DateTime WorkStart { get; set; }

        /// <summary>
        /// Время конца работы (время вручения последнего заказа)
        /// </summary>
        public DateTime WorkEnd { get; set; }

        /// <summary>
        /// Период времени для целей подсчета стоимости курьеров
        /// </summary>
        public double WorkTime { get; set; }

        /// <summary>
        /// Время выполнения всех отгрузок с возвратом в магазин, включая последнюю отгрузку
        /// </summary>
        private double totalExectionTime;

        /// <summary>
        /// lastDelta = executionTime - deliveryTime для последней отгрузки за день
        /// </summary>
        private double lastDelta;

        /// <summary>
        /// Время простоя в магазине, мин
        /// </summary>
        public double Downtime => ((WorkEnd - WorkStart).TotalMinutes - (totalExectionTime - lastDelta));

        /// <summary>
        /// Параметрический конструктор класса ShopCourierStatistics
        /// </summary>
        /// <param name="shopId">ID магазина</param>
        /// <param name="date">День</param>
        /// <param name="courierId">ID курьера</param>
        /// <param name="courierType">Тип курьера</param>
        public ShopCourierStatistics(int shopId, DateTime date, Courier courier)
        {
            ShopId = shopId;
            Date = date.Date;
            ShopCourier = courier;
        }

        /// <summary>
        /// Параметрический конструктор класса ShopCourierStatistics
        /// </summary>
        /// <param name="shopId">ID магазина</param>
        /// <param name="date">День</param>
        /// <param name="courierId">ID курьера</param>
        /// <param name="courierType">Тип курьера</param>
        /// <param name="courierType">Дневная стоимость курьера</param>
        public ShopCourierStatistics(int shopId, DateTime date, Courier courier, double cost)
        {
            ShopId = shopId;
            Date = date.Date;
            ShopCourier = courier;
            TotalCost = cost;
            WorkStart = DateTime.MaxValue;
            WorkEnd = DateTime.MinValue;
        }

        /// <summary>
        /// Построение уникального ключа записи
        /// </summary>
        /// <param name="shopId">ID магазина</param>
        /// <param name="date">Дата</param>
        /// <param name="courierId">ID курьера</param>
        /// <returns></returns>
        public static string GetKey(int shopId, DateTime date, int courierId)
        {
            return $"{shopId}.{date.DayOfYear}.{courierId}";
        }

        /// <summary>
        /// Пополнение статистики
        /// </summary>
        /// <param name="deliveryInfo">Отгрузка</param>
        /// <returns>0 - статистика пополнена; иначе - статистика не пополнена</returns>
        public int Add(CourierDeliveryInfo deliveryInfo)
        {
            // 1. Инициализация
            int rc = 1;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (deliveryInfo == null ||
                    deliveryInfo.StartDelivery.Date != Date ||
                    deliveryInfo.DeliveryCourier == null ||
                    deliveryInfo.DeliveryCourier.Id != CourierId)
                    return rc;

                // 3. Пополняем статистику
                rc = 3;
                OrderCount += deliveryInfo.OrderCount;
                TotalWeight += deliveryInfo.Weight;
                if (deliveryInfo.DeliveryCourier.CourierType.VechicleType == CourierVehicleType.GettTaxi ||
                    deliveryInfo.DeliveryCourier.CourierType.VechicleType == CourierVehicleType.YandexTaxi ||
                    deliveryInfo.DeliveryCourier.CourierType.VechicleType == CourierVehicleType.Car1 ||
                    deliveryInfo.DeliveryCourier.CourierType.VechicleType == CourierVehicleType.Bicycle1 ||
                     deliveryInfo.DeliveryCourier.CourierType.VechicleType == CourierVehicleType.OnFoot1)
                    TotalCost += deliveryInfo.Cost;

                // 4. Рабочее время и общее время выполнения всех отгрузок за день
                rc = 4;
                totalExectionTime += deliveryInfo.ExecutionTime;
                if (deliveryInfo.StartDelivery < WorkStart) WorkStart = deliveryInfo.StartDelivery;
                //DateTime deliveryEnd = deliveryInfo.StartDelivery.AddMinutes(deliveryInfo.DeliveryTime + deliveryInfo.DeliveryCourier.CourierType.HandInTime);
                DateTime deliveryEnd = deliveryInfo.StartDelivery.AddMinutes(deliveryInfo.DeliveryTime);
                if (deliveryEnd > WorkEnd)
                {
                    WorkEnd = deliveryEnd;
                    lastDelta = deliveryInfo.ExecutionTime - deliveryInfo.DeliveryTime;
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
        /// Вспомогательная процедура
        /// установки downtime
        /// </summary>
        /// <param name="downTime">Время простоя, час</param>
        public void SetDowntime(double downTime)
        {
            totalExectionTime = (WorkEnd - WorkStart).TotalMinutes - 60 * downTime;
            lastDelta = 0;
        }
    }
}
