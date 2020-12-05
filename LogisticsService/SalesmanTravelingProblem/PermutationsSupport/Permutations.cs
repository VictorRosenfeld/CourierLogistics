
namespace LogisticsService.SalesmanTravelingProblem.PermutationsSupport
{
    /// <summary>
    /// Lazy-хранилище перестановок
    /// с числом элементов n (1 ≤ n ≤ 8)
    /// </summary>
    public class Permutations
    {
        private int[][,] permutations;
        private int[][,] permutationsFirstFixed;

        private readonly PermutationsGenerator generator;

        /// <summary>
        /// Конструктор класса Permutations
        /// </summary>
        public Permutations()
        {
            permutations = new int[8][,];
            permutationsFirstFixed = new int[8][,];
            generator = new PermutationsGenerator();
        }

        /// <summary>
        /// Все перестановки из n элементов (1 ≤ n ≤ 8)
        /// </summary>
        /// <param name="n">Число переставляемых элементов</param>
        /// <returns>Результат ([i,j] - индекс (0-based) элемента, стоящего на j-ом (0-based) месте в i-ой (0-based) перестановке)</returns>
        public int[,] GetPermutations(int n)
        {
            // 1. Инициализация

            try
            {
                // 2. Проверяем исходные данные
                if (n <= 0 || n > 8)
                    return null;

                // 3. Извлекаем/создаём все перестановки с заданным числом элементов
                if (permutations[n - 1] == null)
                {
                    generator.Create(n);
                    permutations[n - 1] = generator.Permutations;
                }

                // 4. Выход
                return permutations[n - 1];
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Все перестановки из n элементов (1 ≤ n ≤ 8), в которых первый элемент всегда стоит на первом месте
        /// </summary>
        /// <param name="n">Число переставляемых элементов</param>
        /// <returns>Результат ([i,j] - индекс (0-based) элемента, стоящего на j-ом (0-based) месте в i-ой (0-based) перестановке)</returns>
        public int[,] GetPermutationsWithFirstFixed(int n)
        {
            // 1. Инициализация

            try
            {
                // 2. Проверяем исходные данные
                if (n <= 0 || n > 8)
                    return null;

                // 3. Извлекаем перестановки с заданным числом элементов
                if (permutationsFirstFixed[n - 1] != null)
                    return permutationsFirstFixed[n - 1];

                if (permutations[n - 1] == null)
                {
                    generator.Create(n);
                    permutations[n - 1] = generator.Permutations;
                }

                // 4. Отбираем все перестановки с индексом 0 на первом месте
                int[,] perm = permutations[n - 1];
                int rows = perm.GetLength(0);
                int cols =perm.GetLength(1);
                int[,] result = new int[rows / n, n];
                int count = 0;

                for (int i = 0; i < rows; i++)
                {
                    if (perm[i, 0] == 0)
                    {
                        for (int j = 0; j < cols; j++)
                        {
                            result[count, j] = perm[i, j];
                        }

                        count++;
                    }
                }

                permutationsFirstFixed[n - 1] = result;

                // 5. Выход
                return result;
            }
            catch
            {
                return null;
            }
        }
    }
}
