
namespace DeliveryBuilder.Db
{
    /// <summary>
    /// Порог средней стоимости доставки
    /// для выбранного магазина и способа доставки
    /// </summary>
    public class AverageDeliveryCostRecord
    {
        /// <summary>
        /// ID Магазина
        /// </summary>
        public int ShopId { get; set; }

        /// <summary>
        /// ID способа доставки
        /// </summary>
        public int VehicleId { get; set; }

        /// <summary>
        /// Средняя стоимость доставки одного заказа
        /// </summary>
        public double Cost { get; set; }
    }
}
