
namespace DeliveryBuilder.Geo
{
    /// <summary>
    /// Идеификатор элемента кэша
    /// </summary>
    public struct GeoCacheItemInfo
    {
        /// <summary>
        /// Индекс способа доставки
        /// (индекс коллекции)
        /// </summary>
        public int VehicleTypeIndex { get; set; }

        /// <summary>
        /// Ключ элемента
        /// </summary>
        public ulong Key { get; set; }

        /// <summary>
        /// Параметрический конструктор структуры GeoCacheItemInfo
        /// </summary>
        /// <param name="vehicleTypeIndex">Индекс коллекции</param>
        /// <param name="key">Ключ</param>
        public GeoCacheItemInfo(int vehicleTypeIndex, ulong key)
        {
            VehicleTypeIndex = vehicleTypeIndex;
            Key = key;
        }
    }
}
