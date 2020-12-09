
namespace LogisticsService.ServiceParameters
{
    using LogisticsService.Couriers;
    using Newtonsoft.Json;

    public class CourierTypeMapper
    {
        [JsonProperty("courier_type")]
        public string CourierType { get; set; }

        [JsonProperty("vehicle_type")]
        public CourierVehicleType VechicleType { get; set; }
    }
}
