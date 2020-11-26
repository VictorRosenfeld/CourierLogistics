
namespace CourierLogistics.Logistics.FloatSolution.ShopInLive
{
    using CourierLogistics.SourceData.Couriers;

    /// <summary>
    /// Аргументы события ShopTaxiDeliveryAlert в магазине
    /// </summary>
    public class ShopTaxiDeliveryAlertEventArgs
    {
        /// <summary>
        /// Такси
        /// </summary>
        public Courier Taxi { get; private set; }

        /// <summary>
        /// Возможные доставки одиночных заказов с помощью такси
        /// </summary>
        public CourierDeliveryInfo[] TaxiDelivery { get; private set; }

        /// <summary>
        /// Параметрический конструктор класса ShopTaxiDeliveryAlertEventArgs
        /// </summary>
        /// <param name="taxi">Такси</param>
        /// <param name="taxiDelivery">Отгрузки такси</param>
        public ShopTaxiDeliveryAlertEventArgs(Courier taxi, CourierDeliveryInfo[] taxiDelivery)
        {
            Taxi = taxi;
            TaxiDelivery = taxiDelivery;
        }
    }
}
