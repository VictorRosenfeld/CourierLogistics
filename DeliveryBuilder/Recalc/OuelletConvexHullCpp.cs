
namespace DeliveryBuilder.Recalc
{
    /// <summary>
    /// Заглушка для OuelletConvexHullCpp
    /// </summary>
    public class OuelletConvexHullCpp
    {
        public PointEx[] OuelletConvexHull(PointEx[] points, int count, bool closeThePath)
        {
            return null;
        }
    }

    /// <summary>
    /// Индексированная точка
    /// </summary>
    public struct PointEx
    {
        public int index;
        public double x;
        public double y;
    }
}
