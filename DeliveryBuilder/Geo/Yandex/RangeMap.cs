
namespace DeliveryBuilder.Geo.Yandex
{
    /// <summary>
    /// Отображение диапазонов
    /// </summary>
    public struct RangeMap
    {
        /// <summary>
        /// Индекс начала исходного диапазона
        /// </summary>
        public int OriginStartIndex { get; private set; }

        /// <summary>
        /// Длина исходного диапазона
        /// </summary>
        public int OriginLength { get; private set; }

        /// <summary>
        /// Индекс начала целевого диапазона
        /// </summary>
        public int DestiationStartIndex { get; private set; }

        /// <summary>
        /// Длина целевго диапазона
        /// </summary>
        public int DestinationLength { get; private set; }

        /// <summary>
        /// Параметрический конструктор класса RangeMap
        /// </summary>
        /// <param name="originStartIndex">Индекс начала исходного диапазона</param>
        /// <param name="originLength">Длина исходного диапазона</param>
        /// <param name="destinationStartIndex">Индекс начала целевого диапазона</param>
        /// <param name="destinationLength">Длина целевго диапазона</param>
        public RangeMap(int originStartIndex, int originLength, int destinationStartIndex, int destinationLength)
        {
            OriginStartIndex = originStartIndex;
            OriginLength = originLength;
            DestiationStartIndex = destinationStartIndex;
            DestinationLength = destinationLength;
        }

        /// <summary>
        /// Переопределенный ToString:
        /// (OriginStartIndex, OriginLength) -> (DestiationStartIndex, DestinationLength)
        /// </summary>
        /// <returns>Строка результата</returns>
        public override string ToString()
        {
            return $"({OriginStartIndex}, {OriginLength}) -> ({DestiationStartIndex}, {DestinationLength})";
        }
    }
}
