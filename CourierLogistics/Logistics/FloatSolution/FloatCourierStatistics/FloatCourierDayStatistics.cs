namespace CourierLogistics.Logistics.FloatSolution.FloatCourierStatistics
{
    using CourierLogistics.Logistics.FloatSolution.CourierInLive;
    using CourierLogistics.SourceData.Couriers;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Статистика по работе курьера за день
    /// </summary>
    public class FloatCourierDayStatistics
    {
        /// <summary>
        /// Уникальный ключ записи
        /// </summary>
        /// <returns></returns>
        public string Key => GetKey(Date, CourierId);

        /// <summary>
        /// День
        /// </summary>
        public DateTime Date { get; private set; }

        /// <summary>
        /// Курьер
        /// </summary>
        public CourierEx ShopCourier { get; private set; }

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
        /// Коллекция ID магазинов,
        /// из которых курьер осуществлял доставку
        /// </summary>
        private Dictionary<int, int> shopIdCollection;

        /// <summary>
        /// Количество магазинов из которых осуществлялась доставка
        /// </summary>
        public int ShopCount => shopIdCollection.Count;

        /// <summary>
        /// Общее число отгруженных заказов
        /// </summary>
        public int OrderCount { get; set; }

        /// <summary>
        /// Общий вес всех заказов
        /// </summary>
        public double TotalWeight { get; set; }

        /// <summary>
        /// Общая стоимость дотставки всех заказов
        /// </summary>
        public double TotalCost { get; private set; }

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
        public FloatCourierDayStatistics(DateTime date, CourierEx courier)
        {
            Date = date.Date;
            ShopCourier = courier;
            WorkStart = DateTime.MaxValue;
            WorkEnd = DateTime.MinValue;
            shopIdCollection = new Dictionary<int, int>(32);
        }

        /// <summary>
        /// Параметрический конструктор класса ShopCourierStatistics
        /// </summary>
        /// <param name="date">День</param>
        /// <param name="courierId">ID курьера</param>
        /// <param name="courierType">Тип курьера</param>
        /// <param name="courierType">Дневная стоимость курьера</param>
        public FloatCourierDayStatistics(DateTime date, CourierEx courier, double cost)
        {
            Date = date.Date;
            ShopCourier = courier;
            TotalCost = cost;
            WorkStart = DateTime.MaxValue;
            WorkEnd = DateTime.MinValue;
            shopIdCollection = new Dictionary<int, int>(32);
        }

        /// <summary>
        /// Построение уникального ключа записи
        /// </summary>
        /// <param name="shopId">ID магазина</param>
        /// <param name="date">Дата</param>
        /// <param name="courierId">ID курьера</param>
        /// <returns></returns>
        public static string GetKey(DateTime date, int courierId)
        {
            return $"{date.DayOfYear}.{courierId}";
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
                totalExectionTime += (deliveryInfo.DeliveryTime + deliveryInfo.CourierArrivalTime.TotalMinutes);

                // 5. Время начала c учетом прибытия
                rc = 5;
                DateTime startTime = deliveryInfo.StartDelivery.Add(-deliveryInfo.CourierArrivalTime);
                if (startTime < WorkStart) WorkStart = startTime;

                // 6. Время завершения отгрузки
                //DateTime deliveryEnd = deliveryInfo.StartDelivery.AddMinutes(deliveryInfo.DeliveryTime + deliveryInfo.DeliveryCourier.CourierType.HandInTime);
                DateTime deliveryEnd = deliveryInfo.StartDelivery.AddMinutes(deliveryInfo.DeliveryTime);
                if (deliveryEnd > WorkEnd) WorkEnd = deliveryEnd;

                // 7. Коллекция магазинов, из которых осуществлялась доставка
                rc = 7;
                if (deliveryInfo.DeliveredOrders != null && deliveryInfo.DeliveredOrders.Length > 0)
                {
                    foreach(CourierDeliveryInfo delivery in deliveryInfo.DeliveredOrders)
                    {
                        if (delivery.ShippingOrder != null)
                        {
                            if (!shopIdCollection.ContainsKey(delivery.ShippingOrder.ShopNo))
                                shopIdCollection.Add(delivery.ShippingOrder.ShopNo, 1);
                        }
                    }
                }
                else if (deliveryInfo.ShippingOrder != null)
                {
                    if (!shopIdCollection.ContainsKey(deliveryInfo.ShippingOrder.ShopNo))
                        shopIdCollection.Add(deliveryInfo.ShippingOrder.ShopNo, 1);
                }

                // 8. Выход - Ok
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
