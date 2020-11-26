
namespace CourierLogistics.Logistics.RealSingleShopSolution.PartitionOfASet
{
    /// <summary>
    /// Генератор разбиения множества
    /// не более чем на заданное число частей
    /// </summary>
    public class PartitionGenerator
    {
        /// <summary>
        /// Сгенерированные рабиения
        /// </summary>
        int[,] partitions;

        /// <summary>
        /// Сгенерированные рабиения
        /// </summary>
        public int[,] Partition => partitions;

        /// <summary>
        /// Число элементов множества
        /// </summary>
        public int ElementCount { get; private set; }

        /// <summary>
        /// Число коробок, по которым ракладываются элементы
        /// </summary>
        public int BoxCount { get; private set; }

        /// <summary>
        /// Флаг: true - разбиения сгенерированы; false - разбиения не сгенерированы
        /// </summary>
        public bool IsCreated { get; set; }

        /// <summary>
        /// Создание всех разбиений множества
        /// </summary>
        /// <param name="elementCount">Число элементов множества</param>
        /// <param name="boxCount">Число коробок, по которым ракладываются элементы</param>
        /// <returns>0 - разбиения построены; иначе - разбиения не построены</returns>
        public int Create(int elementCount, int boxCount)
        {
            // 1. Инициализация
            int rc = 1;
            IsCreated = false;
            partitions = null;
            ElementCount = elementCount;
            BoxCount = boxCount;

            try
            {
                // 2. Проверяем иходные данные
                rc = 2;
                if (elementCount <= 0 || elementCount > Partitions.MAX_ELEMENT_COUNT)
                    return rc;
                if (boxCount <= 0 || boxCount > Partitions.MAX_BOX_COUNT)
                    return rc;

                // 3. Считаем число разбиений = boxCount ^ elementCount
                rc = 3;
                int partitionCount = 0;

                switch (elementCount)
                {
                    case 1:
                        partitionCount = boxCount;
                        break;
                    case 2:
                        partitionCount = boxCount * boxCount;
                        break;
                    case 3:
                        partitionCount = boxCount * boxCount * boxCount;
                        break;
                    case 4:
                        partitionCount = boxCount * boxCount;
                        partitionCount *= partitionCount;
                        break;
                    case 5:
                        partitionCount = boxCount * boxCount;
                        partitionCount *= partitionCount;
                        partitionCount *= boxCount;
                        break;
                    case 6:
                        partitionCount = boxCount * boxCount;
                        partitionCount = partitionCount * partitionCount * partitionCount;
                        break;
                    case 7:
                        partitionCount = boxCount * boxCount;
                        partitionCount *= partitionCount;
                        partitionCount *= partitionCount;
                        partitionCount /= boxCount;
                        break;
                    case 8:
                        partitionCount = boxCount * boxCount;
                        partitionCount *= partitionCount;
                        partitionCount *= partitionCount;
                        break;
                    case 9:
                        partitionCount = boxCount * boxCount;
                        partitionCount *= partitionCount;
                        partitionCount *= partitionCount;
                        partitionCount *= boxCount;
                        break;
                    case 10:
                        partitionCount = boxCount * boxCount;
                        partitionCount *= partitionCount;
                        partitionCount *= partitionCount;
                        partitionCount *= boxCount;
                        partitionCount *= boxCount;
                        break;
                }

                if (partitionCount <= 0 || partitionCount > Partitions.MAX_PARTITION_COUNT)    // 6^8 = 1679616
                    return rc;

                // 4. Количество бит в числе
                rc = 4;
                int bitCount = 0;
                int mask = 0;

                switch (boxCount)
                {
                    case 1:
                    case 2:
                        bitCount = 1;
                        mask = 0x1;
                        break;
                    case 3:
                    case 4:
                        bitCount = 2;
                        mask = 0x3;
                        break;
                    case 5:
                    case 6:
                    case 7:
                    case 8:
                        bitCount = 3;
                        mask = 0x7;
                        break;
                }

                if (bitCount <= 0)
                    return rc;

                // 5. Определяем максимальное значение счетчика цикла
                rc = 5;
                int maxnumber = 0;
                int maxBoxIndex = boxCount - 1;
                maxnumber = maxBoxIndex;

                for (int i = 1; i < elementCount; i++)
                {
                    maxnumber = (maxnumber << bitCount);
                    maxnumber |= maxBoxIndex;
                }
                
                // 6. Генерация всех комбинаций раскладывания элементов по ящикам
                rc = 6;
                partitions = new int[partitionCount, elementCount];
                int count = 0;

                for (int i = 0; i <= maxnumber; i++)
                {
                    int boxIndex = i & mask;
                    if (boxIndex > maxBoxIndex)
                        continue;

                    partitions[count, 0] = boxIndex;
                    int number = i;

                    for (int j = 1; j < elementCount; j++)
                    {
                        number >>= bitCount;
                        boxIndex = number & mask;
                        if (boxIndex > maxBoxIndex)
                            goto Next;
                        partitions[count, j] = boxIndex;
                    }

                    count++;

                    Next:;
                }

                if (count < partitionCount)
                {
                    count = count;
                }

                // 7. Выход - Ok
                rc = 0;
                return rc;
            }
            catch
            {
                return rc;
            }


        }

    }
}
