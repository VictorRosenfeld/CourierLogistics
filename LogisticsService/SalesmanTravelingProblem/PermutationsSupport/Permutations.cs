
namespace LogisticsService.SalesmanTravelingProblem.PermutationsSupport
{
    /// <summary>
    /// Lazy-хранилище перестановок
    /// с числом элементов n (1 ≤ n ≤ 8)
    /// </summary>
    public class Permutations
    {
        private int[][,] permutations;
        private readonly PermutationsGenerator generator;

        /// <summary>
        /// Конструктор класса Permutations
        /// </summary>
        public Permutations()
        {
            permutations = new int[8][,];
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
    }
}
