
namespace LogisticsService.ServiceParameters
{
    using LogisticsService.Couriers;
    using Newtonsoft.Json;
    using System.Drawing;
    using System.IO;

    public class CourierParameters : ICourierType
    {
        [JsonProperty("courier_type")]
        public CourierVehicleType VechicleType { get; set; }

        [JsonProperty("max_weight")]
        public double MaxWeight { get; set; }

        [JsonProperty("hourly_rate")]
        public double HourlyRate { get; set; }

        [JsonProperty("max_distance")]
        public double MaxDistance { get; set; }

        [JsonProperty("max_orders")]
        public int MaxOrderCount { get; set; }

        [JsonProperty("insurance")]
        public double Insurance { get; set; }

        [JsonProperty("get_order_time")]
        public double GetOrderTime { get; set; }

        [JsonProperty("handin_time")]
        public double HandInTime { get; set; }

        [JsonProperty("start_delay")]
        public double StartDelay { get; set; }

        [JsonProperty("first_pay")]
        public double FirstPay { get; set; }

        [JsonProperty("second_pay")]
        public double SecondPay { get; set; }

        [JsonProperty("first_distance")]
        public double FirstDistance { get; set; }

        [JsonProperty("additional_kilometer_cost")]
        public double AdditionalKilometerCost { get; set; }

        [JsonProperty("is_taxi")]
        public bool IsTaxi { get; set; }

        [JsonProperty("dservice_id")]
        public int DServiceId { get; set; }

        [JsonProperty("calc_method")]
        public string CalcMethod { get; set; }

        [JsonProperty("first_get_order_time")]
        public double FirstGetOrderTime { get; set; }

        [JsonProperty("first_get_order_rate")]
        public double FirstGetOrderRate { get; set; }

        [JsonProperty("first_time")]
        public double FirstTime { get; set; }

        [JsonProperty("first_time_rate")]
        public double FirstTimeRate { get; set; }

        [JsonProperty("second_time_rate")]
        public double SeсondTimeRate { get; set; }

        public int GetTimeAndCost(Point fromShop, double weight, out double deliveryTime, out double executionTime, out double cost)
        {
            deliveryTime = 0;
            executionTime = 0;
            cost = 0;
            return 0;
        }

        public int GetTimeAndCost(Point fromShop, Point toShop, double weight, out double deliveryTime, out double executionTime, out double cost)
        {
            deliveryTime = 0;
            executionTime = 0;
            cost = 0;
            return 0;
        }

        public int GetTimeAndCost(Point[] nodeInfo, double totalWeight, bool isLoop, out double[] nodeDeliveryTime, out double totalDeliveryTime, out double totalExecutionTime, out double totalCost)
        {
            nodeDeliveryTime = null;
            totalDeliveryTime = 0;
            totalExecutionTime = 0;
            totalCost = 0;
            return 0;
        }

        public string Serialize()
        {
            using (StringWriter writer = new StringWriter())
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(writer, this);
                writer.Close();
                return writer.ToString();
            }
        }
    }
}
