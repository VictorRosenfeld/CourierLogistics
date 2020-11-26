
namespace CourierLogistics.Logistics.OptimalSingleShopSolution
{
    using CourierLogistics.SourceData.Couriers;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Курьеры магазина
    /// </summary>
    public class ShopCouriers
    {
        /// <summary>
        /// Курьеры магазина
        /// </summary>
        public Dictionary<int, Courier> Couriers;

        /// <summary>
        /// Конструктор класса ShopCouriers
        /// </summary>
        public ShopCouriers()
        {
            Couriers = new Dictionary<int, Courier>(32);
        }

        /// <summary>
        /// Добавление курьера
        /// </summary>
        /// <param name="courier"></param>
        public void AddCourier(Courier courier)
        {
            Couriers.Add(courier.Id, courier);
        }

        /// <summary>
        /// Назначение доставки курьеру
        /// </summary>
        /// <param name="id">ID курьера</param>
        /// <param name="orderCount">Количество заказов</param>
        /// <param name="startDelivery">Начало доставки</param>
        /// <param name="endDelivery">Конец доставки</param>
        /// <returns>true - заказ назначен; false - заказ не назначен</returns>
        public bool SetOrderToCourier(int id, int orderCount, DateTime startDelivery, DateTime endDelivery)
        {
            Courier courier;
            if (!Couriers.TryGetValue(id, out courier))
                return false;

            courier.OrderCount += orderCount;
            courier.Status = CourierStatus.DeliversOrder;
            courier.LastDeliveryStart = startDelivery;
            courier.LastDeliveryEnd = endDelivery;

            return true;
        }

        /// <summary>
        /// Обновление статусов курьеров
        /// </summary>
        /// <param name="modelTime">Время модели</param>
        public void UpdateStatus(DateTime modelTime)
        {
            foreach (Courier courier in Couriers.Values)
            {
                switch (courier.Status)
                {
                    case CourierStatus.Unknown:
                        break;
                    case CourierStatus.Ready:
                        if (modelTime >= modelTime.Date.Add(courier.LunchTimeStart)
                            && modelTime <= modelTime.Date.Add(courier.LunchTimeEnd))
                        {
                            courier.Status = CourierStatus.LunchTime;
                        }
                        else if (modelTime < modelTime.Date.Add(courier.WorkStart)
                            || modelTime > modelTime.Date.Add(courier.WorkEnd))
                        {
                            courier.Status = CourierStatus.Unknown;
                        }
                        break;
                    case CourierStatus.DeliversOrder:
                        if (modelTime >= courier.LastDeliveryStart &&
                            modelTime <= courier.LastDeliveryEnd)
                        { }
                        else if (modelTime < modelTime.Date.Add(courier.WorkStart)
                            || modelTime > modelTime.Date.Add(courier.WorkEnd))
                        {
                            courier.Status = CourierStatus.Unknown;
                        }
                        else
                        {
                            courier.Status = CourierStatus.Ready;
                        }
                        break;
                    case CourierStatus.LunchTime:
                        if (modelTime >= modelTime.Date.Add(courier.LunchTimeStart)
                            && modelTime <= modelTime.Date.Add(courier.LunchTimeEnd))
                        { }
                        else if (modelTime < modelTime.Date.Add(courier.WorkStart)
                            || modelTime > modelTime.Date.Add(courier.WorkEnd))
                        {
                            courier.Status = CourierStatus.Unknown;
                        }
                        else
                        {
                            courier.Status = CourierStatus.Ready;
                        }
                        break;
                }
            }
        }

        public int SelectAvailableCouriers(double distance, DateTime deliveryTimeLimit, double weight)
        {
            return 0;
        }
    }
}
