
namespace SQLCLR.Deliveries
{
    using System.Collections.Generic;

    /// <summary>
    /// Comparer для словаря
    /// </summary>
    public class DeliveryKeyComparer : IEqualityComparer<int[]>
    {
        /// <summary>
        /// Количество заказов
        /// </summary>
        private readonly int _orderCount;

        /// <summary>
        /// Количество заказов
        /// </summary>
        public int OrderCount => _orderCount;

        /// <summary>
        /// Параметрический конструктор класса DeliveryKeyComparer
        /// </summary>
        /// <param name="orderCount"></param>
        public DeliveryKeyComparer(int orderCount)
        {
            _orderCount = orderCount;
        }

        /// <summary>
        /// Сравнение двух ключей
        /// </summary>
        /// <param name="key1">Ключ 1</param>
        /// <param name="key2">Ключ 2</param>
        /// <returns>true - ключи совпадают; false - ключи не совпадают</returns>
        public bool Equals(int[] key1, int[] key2)
        {
            if (key1 == null || key2 == null)
                return key1 == key2;
            if (key1.Length != key2.Length)
                return false;

            for (int i = 0; i < key1.Length; i++)
            {
                if (key1[i] != key2[i])
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Построение hash-значения ключа отгрузки
        /// </summary>
        /// <param name="obj">Ключ отгрузки</param>
        /// <returns>hash-значение</returns>
        public int GetHashCode(int[] obj)
        {
            if (obj == null || obj.Length <= 0)
                return -1;

            if (_orderCount > 256)
            {
                switch (obj.Length)
                {
                    case 1:
                        return obj[0];
                    case 2:
                        return obj[0] << 16 | obj[1];
                    case 3:
                        return (obj[0] << 16 | obj[1]) ^ obj[2];
                    case 4:
                        return (obj[0] << 16 | obj[1]) ^ (obj[2] << 16 | obj[3]);
                    case 5:
                        return (obj[0] << 16 | obj[1]) ^ (obj[2] << 16 | obj[3]) ^ obj[4];
                    case 6:
                        return (obj[0] << 16 | obj[1]) ^ (obj[2] << 16 | obj[3]) ^ (obj[4] << 16 | obj[5]);
                    case 7:
                        return (obj[0] << 16 | obj[1]) ^ (obj[2] << 16 | obj[3]) ^ (obj[4] << 16 | obj[5]) ^ obj[6];
                    default:
                        return (obj[0] << 16 | obj[1]) ^ (obj[2] << 16 | obj[3]) ^ (obj[4] << 16 | obj[5]) ^ (obj[6] << 16 | obj[7]);
                }
            }
            else
            {
                switch (obj.Length)
                {
                    case 1:
                        return obj[0];
                    case 2:
                        return obj[0] << 8 | obj[1];
                    case 3:
                        return obj[0] << 16 | obj[1] << 8 | obj[2];
                    case 4:
                        return obj[0] << 24 | obj[1] << 16 | obj[2] << 8 | obj[3];
                    case 5:
                        return (obj[0] << 24 | obj[1] << 16 | obj[2] << 8 | obj[3]) ^ obj[4];
                    case 6:
                        return (obj[0] << 24 | obj[1] << 16 | obj[2] << 8 | obj[3]) ^ (obj[4] << 8 | obj[5]);
                    case 7:
                        return (obj[0] << 24 | obj[1] << 16 | obj[2] << 8 | obj[3]) ^ (obj[4] << 16 | obj[5] << 8 | obj[6]);
                    default:
                        return (obj[0] << 24 | obj[1] << 16 | obj[2] << 8 | obj[3]) ^ (obj[4] << 24 | obj[5] << 16 | obj[6] << 8 | obj[7]);
                }
            }
        }
    }
}
