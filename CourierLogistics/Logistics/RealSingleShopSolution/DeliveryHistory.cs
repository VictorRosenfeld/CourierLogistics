
namespace CourierLogistics.Logistics.RealSingleShopSolution
{
    using CourierLogistics.SourceData.Couriers;
    using System;

    /// <summary>
    /// Запись DH-статистики с одной отгрузкой
    /// </summary>
    public class DeliveryHistory
    {
        /// <summary>
        /// Уникальный ключ записи
        /// </summary>
        /// <returns></returns>
        public string Key => GetKey(ShopId, DeliveryStart);

        /// <summary>
        /// ID магазина
        /// </summary>
        public int ShopId { get; private set; }

        /// <summary>
        /// Начало отгрузки
        /// </summary>
        public DateTime DeliveryStart { get; private set; }

        /// <summary>
        /// Конец отгрузки
        /// </summary>
        public DateTime DeliveryEnd { get; private set; }

        /// <summary>
        /// ID курьера
        /// </summary>
        public int CourierId { get; private set; }

        /// <summary>
        /// Тип курьера
        /// </summary>
        public CourierVehicleType CourierType { get; private set; }

        /// <summary>
        /// Число заказов в отгрузке
        /// </summary>
        public int OrderCount { get; set; }

        /// <summary>
        /// Общий вес всех заказов, кг
        /// </summary>
        public double Weight { get; set; }

        /// <summary>
        /// Стоимость отгрузки
        /// </summary>
        public double Cost { get; set; }

        /// <summary>
        /// Вершины пути
        /// </summary>
        public DeliveryHistoryNode[] Nodes  { get; set; }

        /// <summary>
        /// Параметрический конструктор класса DeliveryHistory
        /// </summary>
        /// <param name="shopId">Id магазина</param>
        /// <param name="courierId">Id курьера</param>
        /// <param name="courierType">Тип курьера</param>
        /// <param name="deliveryStart">Начало отгрузки</param>
        /// <param name="deliveryEnd">Завершение отгрузки</param>
        public DeliveryHistory(int shopId, int courierId, CourierVehicleType courierType, DateTime deliveryStart, DateTime deliveryEnd)
        {
            ShopId = shopId;
            CourierId = courierId;
            CourierType = courierType;
            CourierType = courierType;
            DeliveryStart = deliveryStart;
            DeliveryEnd = deliveryEnd;
        }

        /// <summary>
        /// Построение ключа записи
        /// </summary>
        /// <param name="shopId">Id магазина</param>
        /// <param name="deliveryStart">Начало отгрузки</param>
        /// <param name="courierId">Id крьера</param>
        /// <returns>Ключ</returns>
        public static string GetKey(int shopId, DateTime deliveryStart)
        {
            return $"{shopId}.{(int) (deliveryStart - (new DateTime(deliveryStart.Year, 01, 01))).TotalSeconds}";
        }
    }
}
