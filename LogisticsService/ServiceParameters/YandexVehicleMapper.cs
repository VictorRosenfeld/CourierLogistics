
namespace LogisticsService.ServiceParameters
{
    using LogisticsService.Couriers;
    using Newtonsoft.Json;

    public class YandexVehicleMapper
    {
        [JsonProperty("yandex_type")]
        public string YandexType { get; set; }

        [JsonProperty("vehicle_type")]
        public CourierVehicleType[] VechicleTypes { get; set; }
    }
}
