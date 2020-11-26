
namespace CourierLogistics.Logistics.RealSingleShopSolution
{
    using System;

    /// <summary>
    /// Вершина отгрузки 
    /// </summary>
    public class DeliveryHistoryNode
    {
        /// <summary>
        /// Id заказа
        /// </summary>
        public int Id_order { get; private set; }

        /// <summary>
        /// Расстояние от предыдущего узла или магазина, км
        /// </summary>
        public double Distance { get; private set; }

        /// <summary>
        /// Время вручения заказа покупателю
        /// </summary>
        public DateTime DeliveryTime { get; private set; }

        /// <summary>
        /// Предельное время вручения
        /// </summary>
        public DateTime TimeLimit { get; private set; }

        /// <summary>
        /// Параметрический конструктор DeliveryHistoryNode
        /// </summary>
        /// <param name="id">Id заказа</param>
        /// <param name="distance">Расстояние от предыдущего узла или магазина</param>
        /// <param name="deliveryTime">Время вручения</param>
        /// <param name="timeLimit">Предельное время вручения</param>
        public DeliveryHistoryNode(int id, double distance, DateTime deliveryTime, DateTime timeLimit)
        {
            Id_order = id;
            Distance = distance;
            DeliveryTime = deliveryTime;
            TimeLimit = timeLimit;
        }
    }
}
