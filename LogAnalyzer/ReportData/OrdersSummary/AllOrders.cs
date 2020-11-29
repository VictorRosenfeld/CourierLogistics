
namespace LogAnalyzer.ReportData.OrdersSummary
{
    using System.Collections.Generic;

    /// <summary>
    /// Данные для OrdersSummary
    /// </summary>
    public class AllOrders
    {
        /// <summary>
        /// Коллекция OrderSummary
        /// </summary>
        private Dictionary<int, OrderSummary> orders;

        /// <summary>
        /// Коллекция OrderSummary
        /// </summary>
        public Dictionary<int, OrderSummary> Orders => orders;

        /// <summary>
        /// Параметрический конструктор класса AllOrders
        /// </summary>
        /// <param name="capacity">Начальная ёмкость коллекции</param>
        public AllOrders(int capacity = 25000)
        {
            if (capacity <= 0)
                capacity = 25000;
            orders = new Dictionary<int, OrderSummary>(capacity);
        }

        /// <summary>
        /// Обновление состояния заказа по событию заказа
        /// </summary>
        public void AddOrderEvent()
        {

        }

        /// <summary>
        /// Обновление состояния заказа по команде
        /// </summary>
        public void AddCommand()
        {

        }
    }
}
