
namespace LogisticsService.ServiceParameters
{
    using Newtonsoft.Json;

    public class LoggerParameters
    {
        [JsonProperty("saved_days")]
        public int SavedDays { get; set; }

        [JsonProperty("log_file")]
        public string LogFile { get; set; }
    }
}
