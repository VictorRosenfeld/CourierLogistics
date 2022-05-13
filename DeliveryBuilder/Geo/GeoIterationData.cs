
namespace DeliveryBuilder.Geo
{
    /// <summary>
    /// Параметры итерации построения GeoContext
    /// </summary>
    public class GeoIterationData
    {
        /// <summary>
        /// Индекс первой точки исходного интервала
        /// </summary>
        public int OriginStartIndex { get; set; }

        /// <summary>
        /// Количество точек в исходном интервале
        /// </summary>
        public int OriginRemainder { get; set; }

        /// <summary>
        /// Индекс первой точки интервала назначения
        /// </summary>
        public int DestinationStartIndex { get; set; }

        /// <summary>
        /// Количество точек в интервале назначения
        /// </summary>
        public int DestinationRemainder { get; set; }

        /// <summary>
        /// Параметрический конструктор класса GeoIterationData
        /// </summary>
        /// <param name="originStartIndex">Индекс первой точки исходного интервала</param>
        /// <param name="originRemainder">Количество точек в исходном интервале</param>
        /// <param name="destinationStartIndex">Индекс первой точки интервала назначения</param>
        /// <param name="destinationRemainder">Количество точек в интервале назначения</param>
        public GeoIterationData(int originStartIndex, int originRemainder, int destinationStartIndex, int destinationRemainder)
        {
            OriginStartIndex = originStartIndex;
            OriginRemainder = originRemainder;
            DestinationStartIndex = destinationStartIndex;
            DestinationRemainder = destinationRemainder;
        }
    }
}
