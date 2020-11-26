
namespace LogisticsService.Geo
{
    using System.Drawing;

    /// <summary>
    /// Элемент Geo-кэша
    /// </summary>
    public class CacheItem
    {
        /// <summary>
        /// Ключ элемента
        /// </summary>
        public ulong Key { get; private set; }

        /// <summary>
        /// Данные для пары точек с ключом Key:
        /// (Data[i].X - расстояние для способа доставки i, метров;
        ///  Data[i].Y - время движения для способа доставки i, сек.)
        /// </summary>
        public Point[] Data { get; private set; }

        /// <summary>
        /// Параметрический конструктор класса CacheItem
        /// </summary>
        /// <param name="key"></param>
        /// <param name="capacity"></param>
        public CacheItem(ulong key, int capacity)
        {
            Key = key;
            Data = new Point[capacity];
            for (int i = 0; i < capacity; i++)
            {
                Data[i].X = -1;
            }
        }

        /// <summary>
        /// Установка значения элемента данных
        /// </summary>
        /// <param name="index">Индекс элемента</param>
        /// <param name="dataItem">Значение элемента</param>
        public void SetData(int index, Point dataItem)
        {
            Data[index] = dataItem;
        }

        /// <summary>
        /// Проверка, что элемент данных не заполнен
        /// </summary>
        /// <param name="index">Индекс элемента данных</param>
        /// <returns>true - элемент данных не заполнен; false - элемент данных заполнен</returns>
        public bool IsEmpty(int index)
        {
            return Data[index].X == -1;
        }
    }
}
