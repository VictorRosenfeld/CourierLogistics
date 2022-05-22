
namespace DeliveryBuilder.ExtraOrders
{
    using DeliveryBuilder.Orders;
    using System;

    /// <summary>
    /// Поиск сгустков заказов
    /// </summary>
    public class OrdersCloud
    {
        /// <summary>
        /// Выбор наиболее плотной группы близлежащих точек
        /// среди заданных заказов
        /// </summary>
        /// <param name="orders">Заказы</param>
        /// <param name="ordersLimit">Максимальное число отбираемых точек</param>
        /// <param name="radius">Радиус области, метров</param>
        /// <param name="Δt">Время для расширения вилки доставки</param>
        /// <param name="distance">Попарные расстояния между точками, метров</param>
        /// <param name="cloud">Отобранные заказы</param>
        /// <returns></returns>
        public static int FindCloud(Order[] orders, int ordersLimit, double radius, double Δt, double[,] distance, out Order[] cloud)
        {
            // 1. Инициализация
            int rc = 1;
            cloud = null;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (orders == null || orders.Length <= 1)
                    return rc;
                int orderCount = orders.Length;

                if (ordersLimit <= 0)
                    return rc;
                if (radius <= 0)
                    return rc;
                if (distance == null)
                    return rc;
                if (distance.GetLength(0) != orderCount ||
                    distance.GetLength(1) != orderCount)
                    return rc;

                // 3. Выбираем точку с набольшим числом соседей
                //    удовлетвряющих всем критериям
                rc = 3;
                int bestNeighbourCount = 0;
                double bestDistSum = double.MaxValue;
                int bestOrderIndex = -1;
                DateTime tmin;
                DateTime tmax;

                for (int i = 0; i < orderCount; i++)
                {
                    Order order1 = orders[i];
                    int neighbourCount = 0;
                    double distSum = 0;
                    tmin = order1.DeliveryTimeFrom.AddMinutes(-Δt);
                    tmax = order1.DeliveryTimeTo.AddMinutes(Δt);

                    for (int j = 0; j < orderCount; j++)
                    {
                        Order order2 = orders[j];
                        if (distance[i, j] <= radius)
                        {
                            DateTime t1 = order2.DeliveryTimeFrom;
                            DateTime t2 = order2.DeliveryTimeTo;
                            if (tmin > t1)
                                t1 = tmin;
                            if (tmax < t2)
                                t2 = tmax;
                            if (t1 <= t2)
                            {
                                neighbourCount++;
                                distSum += distance[i, j];
                            }
                        }
                    }

                    if (neighbourCount > bestNeighbourCount)
                    {
                        bestNeighbourCount = neighbourCount;
                        bestDistSum = distSum;
                        bestOrderIndex = i;
                    }
                    else if (neighbourCount == bestNeighbourCount && distSum < bestDistSum)
                    {
                        bestDistSum = distSum;
                        bestOrderIndex = i;
                    }
                }

                if (bestOrderIndex < 0)
                    return rc;

                // 4. Выбираем соседей выбранной точки
                rc = 4;
                Order bestOrder = orders[bestOrderIndex];
                Order[] bestOrders = new Order[orderCount];
                double[] bestDist = new double[orderCount];

                tmin = bestOrder.DeliveryTimeFrom.AddMinutes(-Δt);
                tmax = bestOrder.DeliveryTimeTo.AddMinutes(Δt);
                bestNeighbourCount = 0;

                for (int j = 0; j < orderCount; j++)
                {
                    Order order2 = orders[j];
                    //if (distance[bestOrderIndex, j] <= radius)
                    //{
                    DateTime t1 = order2.DeliveryTimeFrom;
                    DateTime t2 = order2.DeliveryTimeTo;
                    if (tmin > t1)
                        t1 = tmin;
                    if (tmax < t2)
                        t2 = tmax;
                    if (t1 <= t2)
                    {
                        bestDist[bestNeighbourCount] = distance[bestOrderIndex, j];
                        bestOrders[bestNeighbourCount++] = order2;
                    }
                    //}
                }

                if (bestNeighbourCount < bestOrders.Length)
                {
                    Array.Resize(ref bestOrders, bestNeighbourCount);
                    Array.Resize(ref bestDist, bestNeighbourCount);
                }

                // 5. Отбираем заданное число соседей
                rc = 5;
                if (bestNeighbourCount <= ordersLimit)
                {
                    cloud = bestOrders;
                }
                else
                {
                    Array.Sort(bestDist, bestOrders);
                    cloud = new Order[ordersLimit];
                    Array.Copy(bestOrders, cloud, ordersLimit);
                }

                // 6. Выход - Ok
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
