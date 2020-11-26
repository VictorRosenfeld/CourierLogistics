
namespace CourierLogistics.SourceData.Orders
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Все заказы
    /// </summary>
    public class AllOrders
    {
        /// <summary>
        /// Все заказы
        /// </summary>
        public Dictionary<int, Order> Orders { get; private set; }

        /// <summary>
        /// Конструктор класса AllOrders
        /// </summary>
        public AllOrders()
        {
            Orders = new Dictionary<int, Order>(10000);
        }

        /// <summary>
        /// Добавление заказа в коллекцию
        /// </summary>
        /// <param name="order">Добовляемвй заказ</param>
        public void Add(Order order)
        {
            Orders.Add(order.Id_order, order);
        }

        /// <summary>
        /// Извлечь заказ по ID
        /// </summary>
        /// <param name="key">ID заказа</param>
        /// <returns>Магазин или null</returns>
        public Order GetOrder(int key)
        {
            Order order;
            Orders.TryGetValue(key, out order);
            return order;
        }

        /// <summary>
        /// Выборка заказов заданного магазина
        /// </summary>
        /// <param name="shopNo">Номер магазина</param>
        /// <returns>Найденные заказы</returns>
        public Order[] GetShopOrders(int shopNo)
        {
            return Orders.Values.Where(order => order.ShopNo == shopNo).ToArray();
        }

        /// <summary>
        /// Извлечение списка номеров магазинов, 
        /// для которых имеются заказы
        /// </summary>
        /// <returns>Номера магазинов</returns>
        public int[] GetShops()
        {
            return Orders.Values.Select(order => order.ShopNo).Distinct().ToArray();
        }

        /// <summary>
        /// Получить диапазон времени сборки заказов
        /// </summary>
        /// <param name="minTime">Минимальная дата-время</param>
        /// <param name="maxTime">Максимальная дата-время</param>
        /// <returns>0 - диапазон построен; иначе - диапазон не построен</returns>
        public int GetOrdersTimeRange(out DateTime minTime, out DateTime maxTime)
        {
            // 1. Инициализация
            int rc = 1;
            minTime = DateTime.MaxValue;
            maxTime = DateTime.MinValue;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (Orders == null || Orders.Count <= 0)
                    return rc;

                // 3. Построение временного диапазона для времени сборки заказов
                rc = 3;
                foreach(Order order in Orders.Values)
                {
                    DateTime assembledTime = order.Date_collected;
                    if (assembledTime < minTime) minTime = assembledTime;
                    if (assembledTime > maxTime) maxTime = assembledTime;
                }

                // 4. Выход - Ok
                rc = 0;
                return rc;

            }
            catch
            {
                return rc;
            }
        }

        /// <summary>
        /// Получить все заказы за один день
        /// </summary>
        /// <param name="day">Заданный день</param>
        /// <returns>Заказы за заданный день</returns>
        public Order[] GetDayOrders(DateTime day)
        {
            // 1. Инициализация

            try
            {
                // 2. Проверяем исходные данные
                if (Orders == null || Orders.Count <= 0)
                    return new Order[0];

                // 3. Возвращаем результат
                DateTime d = day.Date;
                return Orders.Values.Where(p => p.Date_collected.Date == d).ToArray();
            }
            catch
            {
                return new Order[0];
            }
        }
    }
}
