
namespace LogisticsService.ServiceParameters
{
    using LogisticsService.Couriers;
    using Newtonsoft.Json;

    public class DServiceIdMapper
    {
        [JsonProperty("dservice_id")]
        public int DserviceId { get; set; }

        [JsonProperty("vehicle_types")]
        public CourierVehicleType[] VechicleTypes { get; set; }
    }
}
