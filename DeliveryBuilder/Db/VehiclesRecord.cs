
namespace DeliveryBuilder.Db
{
    using DeliveryBuilder.Couriers;

    /// <summary>
    /// Запись таблицы tblVehicles
    /// </summary>
    public class VehiclesRecord
    {
        /// <summary>
        /// ID способа дставки
        /// </summary>
        public int VehicleId { get; private set; }

        /// <summary>
        /// Флаг доступости способа доставки
        /// </summary>
        public bool Enabled { get; private set; }

        /// <summary>
        /// Флаг такси
        /// </summary>
        public bool IsTaxi { get; private set; }

        /// <summary>
        /// Параметры способа доставки
        /// </summary>
        public CourierTypeData Parameters { get; private set; }

        /// <summary>
        /// Описание
        /// </summary>
        public string Description { get; private set; }

        /// <summary>
        /// Параметический конструктор класса ServiceVehiclesRecord
        /// </summary>
        /// <param name="vehicleId">ID способа дставки</param>
        /// <param name="enabled">Флаг доступости способа доставки</param>
        /// <param name="isTaxi">Флаг такси</param>
        /// <param name="parameters">Параметры способа доставки</param>
        /// <param name="description">Описание</param>
        public VehiclesRecord(int vehicleId, bool enabled, bool isTaxi, CourierTypeData parameters, string description)
        {
            VehicleId = vehicleId;
            Enabled = enabled;
            IsTaxi = isTaxi;
            Parameters = parameters;
            Description = description;
        }
    }
}
