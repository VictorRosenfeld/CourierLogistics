
namespace DeliveryBuilder.Recalc
{
    /// <summary>
    /// Диапазон подмножеств
    /// </summary>
    public struct ThreadSubsetRange
    {
        /// <summary>
        /// Первое подмножество диапазона
        /// </summary>
        public int[] FirstSubset { get; private set; }

        /// <summary>
        /// Количество подмножеств в диапазоне
        /// </summary>
        public int SubsetCount { get; set; }

        /// <summary>
        /// Параметрический конструктор структуры ThreadSubsetRange
        /// </summary>
        /// <param name="firstSubset">Первое подмножество диапазона</param>
        /// <param name="subsetCount">Количество подмножеств в диапазоне</param>
        public ThreadSubsetRange(int[]firstSubset, int subsetCount)
        {
            FirstSubset = firstSubset;
            SubsetCount = subsetCount;
        }
    }
}
