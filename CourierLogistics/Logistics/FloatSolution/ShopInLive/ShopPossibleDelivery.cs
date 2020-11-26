
namespace CourierLogistics.Logistics.FloatSolution.ShopInLive
{
    using CourierLogistics.Logistics.FloatSolution.CourierInLive;
    using CourierLogistics.SourceData.Couriers;

    /// <summary>
    /// Возможная отгрузка
    /// </summary>
    public class ShopPossibleDelivery
    {
        /// <summary>
        /// Магазин, из которого осуществляется отгрузка
        /// </summary>
        public ShopEx SourceShop { get; private set; }

        /// <summary>
        /// Отгрузка
        /// </summary>
        public CourierDeliveryInfo PossibleDelivery { get; private set; }

        /// <summary>
        /// Курьер, осуществляющий отгрузку
        /// </summary>
        public CourierEx DeliveryCourier => (PossibleDelivery == null ? null : (CourierEx)PossibleDelivery.DeliveryCourier);

        /// <summary>
        /// ID курьера, осуществляющего отгрузку
        /// </summary>
        public int CourierId =>  (PossibleDelivery == null ? 0 : PossibleDelivery.DeliveryCourier.Id);

        /// <summary>
        /// Параметрический конструктор класса ShopPossibleDelivery
        /// </summary>
        /// <param name="shop">Магазин</param>
        /// <param name="delivery">Доставка</param>
        public ShopPossibleDelivery(ShopEx shop, CourierDeliveryInfo delivery)
        {
            SourceShop = shop;
            PossibleDelivery = delivery;
        }
    }
}
