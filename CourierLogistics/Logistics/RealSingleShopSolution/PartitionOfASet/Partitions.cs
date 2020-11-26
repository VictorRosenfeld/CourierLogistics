
using System;

namespace CourierLogistics.Logistics.RealSingleShopSolution.PartitionOfASet
{
    /// <summary>
    /// Lazy-хранилище разбиений
    /// с числом элементов множества n (1 ≤ n ≤ 10) и числом ящиков m (1 ≤ m ≤ 8)
    /// </summary>
    public class Partitions
    {
        public const int MAX_ELEMENT_COUNT = 10;
        public const int MAX_BOX_COUNT = 8;
        public const int MAX_PARTITION_COUNT = 1679616; // 6^8 = 1679616

        private int[][,] partitions;

        private readonly PartitionGenerator generator;

        /// <summary>
        /// Конструктор класса Partitions
        /// </summary>
        public Partitions()
        {
            partitions = new int[GetPartitionIndex(MAX_ELEMENT_COUNT, MAX_BOX_COUNT) + 1][,];
            generator = new PartitionGenerator();
        }

        /// <summary>
        /// Извлечение разбиений с заданными аргументами
        /// </summary>
        /// <param name="elementCount">Число элементов множества</param>
        /// <param name="boxCount">Число коробок</param>
        /// <returns>Разбиения или null</returns>
        public int[,] GetPartition(int elementCount, int boxCount)
        {
            try
            {
                // 2. Извлекаем и проверяем индекс
                int partitionIndex = GetPartitionIndex(elementCount, boxCount);
                if (partitionIndex < 0)
                    return null;

                // 3. Если разбиение с заданными аргументами ещё не построено
                if (partitions[partitionIndex] == null)
                {
                    if (generator.Create(elementCount, boxCount) == 0)
                    {
                        partitions[partitionIndex] = generator.Partition;
                    }
                }

                return partitions[partitionIndex];
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        ///  Построение индекса разбиения
        ///  по заданным аргументам
        /// </summary>
        /// <param name="elementCount">Число элементов множества</param>
        /// <param name="boxCount">Число коробок</param>
        /// <returns></returns>
        private static int GetPartitionIndex(int elementCount, int boxCount)
        {
            if (elementCount <= 0 || elementCount > MAX_ELEMENT_COUNT)
                return -1;
            if (boxCount <= 0 || boxCount > MAX_BOX_COUNT)
                return -1;
            return MAX_ELEMENT_COUNT * (elementCount - 1) + (boxCount - 1);
        }

        /// <summary>
        /// Проверка аргументов
        /// </summary>
        /// <param name="elementCount">Число элементов множества</param>
        /// <param name="boxCount">Число коробок</param>
        /// <returns>true - аргументы являются допустимыми; иначе - аргументы не являются допустимыми</returns>
        public bool CheckArgs(int elementCount, int boxCount)
        {
            if (elementCount <= 0 || elementCount > MAX_ELEMENT_COUNT)
                return false;
            if (boxCount <= 0 || boxCount > MAX_BOX_COUNT)
                return false;

            return Math.Pow(boxCount, elementCount) <= MAX_PARTITION_COUNT;
        }
    }
}
