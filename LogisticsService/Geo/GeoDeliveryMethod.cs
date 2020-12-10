
namespace LogisticsService.Geo
{
    using LogisticsService.Couriers;

    /// <summary>
    /// Способ доставки
    /// </summary>
    public class GeoDeliveryMethod
    {
        /// <summary>
        /// Уникальный индекс способа доставки
        /// </summary>
        public int Index { get; private set; }

        /// <summary>
        /// Код способа доставки используемый в программе
        /// </summary>
        public CourierVehicleType VehicleType { get; private set; }

        /// <summary>
        /// Название способа доставки используемое в API
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Параметрический конструктор класса GeoDeliveryMethod
        /// </summary>
        /// <param name="index"></param>
        /// <param name="vehicleType"></param>
        /// <param name="name"></param>
        public GeoDeliveryMethod(int index, CourierVehicleType vehicleType, string name)
        {
            Index = index;
            VehicleType = vehicleType;
            Name = name;
        }
    }
}
