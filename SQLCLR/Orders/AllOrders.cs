
namespace SQLCLR.Orders
{
    using System;

    /// <summary>
    /// Все заказы требующие отгрузки
    /// </summary>
    internal class AllOrders
    {
        /// <summary>
        /// Отсортированные по ID заказы
        /// </summary>
        private Order[] orders;

        /// <summary>
        /// Id отсортированных orders
        /// </summary>
        private int[] orderId;

        /// <summary>
        /// Все заказы
        /// </summary>
        public Order[] Orders => orders;

        /// <summary>
        /// Флаг: true - класс создан; false - класс не создан
        /// </summary>
        public bool IsCreated { get; private set; }

        /// <summary>
        /// Количество заказов
        /// </summary>
        public int Count => (orders == null ? 0 : orders.Length);

        /// <summary>
        /// Создание всех заказов
        /// </summary>
        /// <param name="allOrders">Все заказы</param>
        /// <returns></returns>
        public int Create(Order[] allOrders)
        {
            // 1. Инициализация
            int rc = 1;
            orders = null;
            orderId = null;
            IsCreated = false;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (allOrders == null || allOrders.Length <= 0)
                    return rc;

                // 3. Строим индекс заказов
                rc = 3;
                orderId = new int[allOrders.Length];
                orders = (Order[])allOrders.Clone();

                for (int i = 0; i < orders.Length; i++)
                {
                    orderId[i] = allOrders[i].Id;
                }

                Array.Sort(orderId, orders);

                // 4. Выход - Ok
                rc = 0;
                IsCreated = true;
                return rc;
            }
            catch
            {
                return rc;
            }
        }

        /// <summary>
        /// Получить заказ по Id
        /// </summary>
        /// <param name="orderId">Id заказа</param>
        /// <returns>Заказ или null</returns>
        public Order GetOrder(int orderId)
        {
            try
            {
                // 2. Проверяем исходные данные
                if (!IsCreated)
                    return null;
                if (Count <= 0)
                    return null;

                // 3. Находим заказ
                Order order = null;
                int index = Array.BinarySearch(this.orderId, orderId);
                if (index >= 0)
                    order = orders[index];

                // 4. Возвращаем результат
                return order;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Выбрать все заказы магазина требующие доставки
        /// </summary>
        /// <param name="shopId">Id магазина</param>
        /// <returns>Выбранные заказы или null</returns>
        public Order[] GetShopOrders(int shopId)
        {
            try
            {
                // 2. Проверяем исходные данные
                if (!IsCreated)
                    return null;
                if (Count <= 0)
                    return new Order[0];

                // 3. Выбираем заказы магазина требующие доставки
                int orderCount = 0;
                Order[] shopOrders = new Order[Count];

                for (int i = 0; i < shopOrders.Length; i++)
                {
                    Order order = orders[i];
                    if (order.ShopId == shopId &&
                        order.Status == OrderStatus.Assembled || order.Status == OrderStatus.Receipted)
                        shopOrders[orderCount++] = order;
                }

                if (orderCount < shopOrders.Length)
                {
                    Array.Resize(ref shopOrders, orderCount);
                }

                // 4. Выход - Ok
                return shopOrders;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Выбор набора различных способов доставки
        /// для заданных заказов
        /// </summary>
        /// <param name="orders">Заказы, дя котрых выбираются различные способы доставки</param>
        /// <returns>Способы доставки или null</returns>
        public static int[] GetOrderVehicleTypes(Order[] orders)
        {
            // 1. Инициализация
            
            try
            {
                // 2. Проверяем исходные данные
                if (orders == null || orders.Length <= 0)
                    return null;

                // 3. Подсчитываем общее число VehicleType у всех заказов
                int vehicleTypeCount = 0;

                for (int i = 0; i < orders.Length; i++)
                {
                    vehicleTypeCount += orders[i].VehicleTypeCount;
                }

                if (vehicleTypeCount <= 0)
                    return null;

                // 4. Выбираем все типы в один массив
                int[] allTypes = new int[vehicleTypeCount];
                vehicleTypeCount = 0;

                for (int i = 0; i < orders.Length; i++)
                {
                    Order order = orders[i];
                    int count = order.VehicleTypeCount;

                    for (int j = 0; j < count; j++)
                    {
                        allTypes[vehicleTypeCount++] = order.VehicleTypes[j];
                    }
                }

                // 5. Сортируем все типы
                Array.Sort(allTypes);

                // 6. Отбираем различные типы
                vehicleTypeCount = 1;
                int currentVehicleType = allTypes[0];
                
                for (int i = 1; i < allTypes.Length; i++)
                {
                    if (allTypes[i] != currentVehicleType)
                    {
                        currentVehicleType = allTypes[i];
                        allTypes[vehicleTypeCount++] = currentVehicleType;
                    }
                }

                if (vehicleTypeCount < allTypes.Length)
                {
                    Array.Resize(ref allTypes, vehicleTypeCount);
                }

                // 7. Выход - Ok
                return allTypes;
            }
            catch
            {
                return null;
            }
        }
    }
}
