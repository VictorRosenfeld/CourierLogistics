
namespace SQLCLR.RouteCheck
{
    using System;

    /// <summary>
    /// Эффективный генератор перестановок
    /// </summary>
    public class Permutations
    {
        /// <summary>
        /// Генератор всех перестановок заланной длины (0-base)
        /// </summary>
        /// <param name="n">Длина перестановки (1 ≤ n ≤ 10)</param>
        /// <returns>Перестановки или null</returns>
        public static byte[] Generate(int n)
        {
            // 1. Инициализация

            try
            {
                // 2. Проверяем исходные данные
                if (n <= 0 || n > 10)
                    return null;

                // 3. Простые частные случаи
                byte[] permIn;
                switch (n)
                {
                    case 1:
                        return new byte[] { 0 };
                    case 2:
                        return new byte[] { 0, 1,  1, 0 };
                    case 3:
                        return new byte[] { 0, 1, 2, 0, 2, 1,  1, 0, 2, 1, 2, 0,  2, 0, 1, 2, 1, 0 };
                    default:
                        permIn = new byte[] { 0, 1, 2, 0, 2, 1,  1, 0, 2, 1, 2, 0,  2, 0, 1, 2, 1, 0 };
                        break;
                }

                // 4. Доходим до нужного размера
                byte[] permOut = null;

                for (byte k = 4; k <= n; k++, permIn = permOut)
                {
                    GenNext(k - 1, permIn, k, out permOut);
                }

                // 5. Выход - Ok
                return permOut;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Построение всех перестановок длины n + 1 из всех перестановок длины n
        /// </summary>
        /// <param name="n">Исходная длина для построения</param>
        /// <param name="permutIn">Все перестановки длины n</param>
        /// <param name="value">Добавляемый индекс</param>
        /// <param name="permutOut">Построенный результат</param>
        /// <returns>0 - перестановки построены; иначе - перестановки не построены</returns>
        private static int GenNext(int n, byte[] permutIn, byte value, out byte[] permutOut)
        {
            // 1. Инициализация
            int rc = 1;
            permutOut = null;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (n <= 0 || permutIn == null || permutIn.Length <= 0)
                {
                    permutOut = new byte[] { value };
                    return rc = 0;
                }

                int nfact = permutIn.Length / n;

                // 3. Строим результат
                rc = 3;
                permutOut = new byte[(n + 1) * nfact * (n + 1)];
                int n1 = n + 1;
                int offsetOut = 0;

                for (int offsetIn = 0; offsetIn < permutIn.Length; offsetIn += n)
                {
                    // 3.1 Новый элемент перед первым
                    permutOut[offsetOut] = value;
                    Buffer.BlockCopy(permutIn, 0, permutOut, offsetOut + 1, n);
                    offsetOut += n1;

                    // 3.2 Новый элемент после j
                    for (int j = 0; j < n; j++)
                    {
                        Buffer.BlockCopy(permutIn, offsetIn, permutOut, offsetOut, j + 1);
                        offsetOut += (j + 1);
                        permutOut[offsetOut] = value;
                        Buffer.BlockCopy(permutIn, offsetIn + j + 1, permutOut, offsetOut + 1, n - j - 1);
                        offsetOut += (n - j);
                    }
                }

                // 4. Выход - Ok
                return rc;
            }
            catch
            {
                return rc;
            }
        }
    }
}
