
namespace LogisticsService.SalesmanTravelingProblem.PermutationsSupport
{
    using System;

    /// <summary>
    /// Генератор перестановок для 1 ≤ n ≤ 8
    /// </summary>
    public class PermutationsGenerator
    {
        /// <summary>
        /// Сгенерированные перестановки
        /// </summary>
        private int[,] permutations;

        /// <summary>
        /// Сгенерированные перестановки
        /// </summary>
        public int[,] Permutations => permutations;

        /// <summary>
        /// Флаг: true - перестановки сгенерированы; false - перестановки не сгенерированы
        /// </summary>
        public bool IsCreated { get; set; }

        /// <summary>
        /// Генерация перестановок
        /// </summary>
        /// <param name="n">1 ≤ n ≤ 8</param>
        /// <returns>0 - перестановки сгенерированы; иначе - перестановки не сгенерированы</returns>
        public int Create(int n)
        {
            // 1. Инициализация
            int rc = 1;
            IsCreated = false;
            permutations = null;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (n <= 0 || n > 8)
                    return rc;

                // 3. Подсчитываем общее число перестановок
                rc = 3;
                int size = 1;
                for (int i = 1; i <= n; i++)
                    size *= i;

                // 4. Выделяем память под результат
                rc = 4;
                permutations = new int[size, n];

                switch (n)
                {
                    case 1:
                        break;
                    case 2:
                        permutations[0, 0] = 0;
                        permutations[0, 1] = 1;
                        permutations[1, 0] = 1;
                        permutations[1, 1] = 0;
                        break;
                    case 3:
                        permutations[0, 0] = 0;
                        permutations[0, 1] = 1;
                        permutations[0, 2] = 2;

                        permutations[1, 0] = 0;
                        permutations[1, 1] = 2;
                        permutations[1, 2] = 1;

                        permutations[2, 0] = 1;
                        permutations[2, 1] = 0;
                        permutations[2, 2] = 2;

                        permutations[3, 0] = 1;
                        permutations[3, 1] = 2;
                        permutations[3, 2] = 0;

                        permutations[4, 0] = 2;
                        permutations[4, 1] = 0;
                        permutations[4, 2] = 1;

                        permutations[5, 0] = 2;
                        permutations[5, 1] = 1;
                        permutations[5, 2] = 0;
                        break;
                    case 4:
                        permutations[0, 0] = 0;
                        permutations[0, 1] = 1;
                        permutations[0, 2] = 2;
                        permutations[0, 3] = 3;

                        permutations[1, 0] = 0;
                        permutations[1, 1] = 1;
                        permutations[1, 2] = 3;
                        permutations[1, 3] = 2;

                        permutations[2, 0] = 0;
                        permutations[2, 1] = 2;
                        permutations[2, 2] = 1;
                        permutations[2, 3] = 3;

                        permutations[3, 0] = 0;
                        permutations[3, 1] = 2;
                        permutations[3, 2] = 3;
                        permutations[3, 3] = 1;

                        permutations[4, 0] = 0;
                        permutations[4, 1] = 3;
                        permutations[4, 2] = 1;
                        permutations[4, 3] = 2;

                        permutations[5, 0] = 0;
                        permutations[5, 1] = 3;
                        permutations[5, 2] = 2;
                        permutations[5, 3] = 1;

                        permutations[6, 0] = 1;
                        permutations[6, 1] = 0;
                        permutations[6, 2] = 2;
                        permutations[6, 3] = 3;

                        permutations[7, 0] = 1;
                        permutations[7, 1] = 0;
                        permutations[7, 2] = 3;
                        permutations[7, 3] = 2;

                        permutations[8, 0] = 1;
                        permutations[8, 1] = 2;
                        permutations[8, 2] = 0;
                        permutations[8, 3] = 3;

                        permutations[9, 0] = 1;
                        permutations[9, 1] = 2;
                        permutations[9, 2] = 3;
                        permutations[9, 3] = 0;

                        permutations[10, 0] = 1;
                        permutations[10, 1] = 3;
                        permutations[10, 2] = 0;
                        permutations[10, 3] = 2;

                        permutations[11, 0] = 1;
                        permutations[11, 1] = 3;
                        permutations[11, 2] = 2;
                        permutations[11, 3] = 0;

                        permutations[12, 0] = 2;
                        permutations[12, 1] = 0;
                        permutations[12, 2] = 1;
                        permutations[12, 3] = 3;

                        permutations[13, 0] = 2;
                        permutations[13, 1] = 0;
                        permutations[13, 2] = 3;
                        permutations[13, 3] = 1;

                        permutations[14, 0] = 2;
                        permutations[14, 1] = 1;
                        permutations[14, 2] = 0;
                        permutations[14, 3] = 3;

                        permutations[15, 0] = 2;
                        permutations[15, 1] = 1;
                        permutations[15, 2] = 3;
                        permutations[15, 3] = 0;

                        permutations[16, 0] = 2;
                        permutations[16, 1] = 3;
                        permutations[16, 2] = 0;
                        permutations[16, 3] = 1;

                        permutations[17, 0] = 2;
                        permutations[17, 1] = 3;
                        permutations[17, 2] = 1;
                        permutations[17, 3] = 0;

                        permutations[18, 0] = 3;
                        permutations[18, 1] = 0;
                        permutations[18, 2] = 1;
                        permutations[18, 3] = 2;

                        permutations[19, 0] = 3;
                        permutations[19, 1] = 0;
                        permutations[19, 2] = 2;
                        permutations[19, 3] = 1;

                        permutations[20, 0] = 3;
                        permutations[20, 1] = 1;
                        permutations[20, 2] = 0;
                        permutations[20, 3] = 2;

                        permutations[21, 0] = 3;
                        permutations[21, 1] = 1;
                        permutations[21, 2] = 2;
                        permutations[21, 3] = 0;

                        permutations[22, 0] = 3;
                        permutations[22, 1] = 2;
                        permutations[22, 2] = 0;
                        permutations[22, 3] = 1;

                        permutations[23, 0] = 3;
                        permutations[23, 1] = 2;
                        permutations[23, 2] = 1;
                        permutations[23, 3] = 0;
                        break;
                    default:
                        bool[] isSet = new bool[n];

                        int minNumber = 0;
                        int maxNumber = 0;
                        int power8 = 8 << 3 * (n - 2);

                        for (int i = 1; i <= n; i++, power8 >>= 3)
                        {
                            maxNumber += (n - i) * power8;
                            minNumber += (i - 1) * power8;
                        }

                        int count = 0;
                        bool[] booldef = new bool[n];
                        int sz = n * sizeof(bool);

                        for (int i = minNumber; i <= maxNumber; i++)
                        {
                            Buffer.BlockCopy(booldef, 0, isSet, 0, sz);

                            int k = i;
                            int m = 0;

                            for (int j = 0; j < n; j++, k >>= 3)
                            {
                                int index = k & 0b111;
                                if (index >= n || isSet[index])
                                    goto NextNumber;

                                permutations[count, j] = index;
                                isSet[index] = true;
                                m++;
                            }

                            if (m == n)
                                count++;
                            NextNumber: ;
                        }
                        break;
                }


                // 5. Выход - Ok
                rc = 0;
                IsCreated = true;
                return rc;
            }
            catch
            {
                return rc;
            }
        }
    }
}
