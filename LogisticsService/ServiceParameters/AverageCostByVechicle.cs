
namespace LogisticsService.ServiceParameters
{
    using LogisticsService.Couriers;

    /// <summary>
    /// Средняя стоимость заказа при заданном
    /// способе доставки в заданном магазине
    /// </summary>
    public class AverageCostByVechicle
    {
        //[JsonIgnore]
        //public string Key => GetKey(shop_id, vehicle_type);

        /// <summary>
        /// Id магазина
        /// </summary>
        public int shop_id { get; set; }

        /// <summary>
        /// Тип курьера (способ доставки)
        /// </summary>
        public CourierVehicleType vehicle_type { get; set; }

        /// <summary>
        /// Средняя стоимость доставки заказа
        /// </summary>
        public double average_cost { get; set; }

        //public static string GetKey(int id, CourierVehicleType vType)
        //{
        //    return  $"{id}.{vType}";
        //}
    }
}
