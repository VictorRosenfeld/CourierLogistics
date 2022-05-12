
namespace DeliveryBuilder.Geo
{
    /// <summary>
    /// Координаты точки
    /// </summary>
    public struct Point
    {
        /// <summary>
        /// Координата X
        /// </summary>
        public int X { get; set; }

        /// <summary>
        /// Координата Y
        /// </summary>
        public int Y { get; set; }

        /// <summary>
        /// Параметрический конструктор структуры Point
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public Point(int x, int y)
        {
            X = x;
            Y = y;
        }
    }
}
