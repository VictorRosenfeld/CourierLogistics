

namespace LogisticsService.ServiceParameters
{
    using Newtonsoft.Json;
    using System.IO;

    /// <summary>
    /// Параметры сервиса
    /// </summary>
    public class ServiceConfig
    {
        public FunctionalParameters functional_parameters;
        public DServiceIdMapper[] dservice_mapper;
        public CourierTypeMapper[] courier_type_mapper;
        public CourierParameters[] couriers;
        public AverageCostByVechicle[] average_cost;

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

        public static ServiceConfig Deserialize(string filename)
        {
            using (StreamReader reader = new StreamReader(filename, System.Text.Encoding.UTF8))
            {
                JsonSerializer serializer = new JsonSerializer();
                return (ServiceConfig) serializer.Deserialize(reader, typeof(ServiceConfig));
            }
        }

    }
}
