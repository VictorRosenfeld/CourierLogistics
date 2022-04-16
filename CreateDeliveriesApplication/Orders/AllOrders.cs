
namespace CreateDeliveriesApplication.Orders
{
    using CreateDeliveriesApplication.Deliveries;
    using System;

    /// <summary>
    /// Все заказы требующие отгрузки
    /// </summary>
    public class AllOrders
    {
        #region Заказы, отсортированные по OrderID

        /// <summary>
        /// Отсортированные по ID заказы
        /// </summary>
        private Order[] orders;

        /// <summary>
        /// Отсортированные ключи (OrderID)
        /// </summary>
        private int[] orderKeys;

        /// <summary>
        /// Отсортированные заказы
        /// </summary>
        public Order[] Orders => orders;

        /// <summary>
        /// Отсортированные ключи (OrderID)
        /// </summary>
        private int[] OrderKeys => orderKeys;

        #endregion Заказы, отсортированные по OrderID

        #region Заказы, отсортированные по ShopID

        /// <summary>
        /// Заказы отсортированные по ShopID
        /// </summary>
        private Order[] shopOrders;

        /// <summary>
        /// Отсортированные ключи магазинов (ShopId)
        /// </summary>
        private int[] shopKeys;

        /// <summary>
        /// Дипазоны заказов одного магазина в shopOrders
        /// (startIndex = Point.X, endIndex = Point.Y)
        /// </summary>
        private Point[] orderRange;

        /// <summary>
        /// Заказы отсортированные по ShopID
        /// </summary>
        public Order[] ShopOrders => shopOrders;

        /// <summary>
        /// Отсортированные ключи магазинов (ShopId)
        /// </summary>
        private int[] ShopKeys => shopKeys;

        /// <summary>
        /// Дипазоны заказов одного магазина в shopOrders
        /// (startIndex = Point.X, endIndex = Point.Y)
        /// </summary>
        private Point[] OrderRange => orderRange;

        #endregion Заказы, отсортированные по ShopID

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
            IsCreated = false;

            orders = null;
            orderKeys = null;

            shopOrders = null;
            shopKeys = null;
            orderRange = null;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (allOrders == null || allOrders.Length <= 0)
                    return rc;

                // 3. Строим индекс заказов
                rc = 3;
                orderKeys = new int[allOrders.Length];
                orders = (Order[])allOrders.Clone();

                for (int i = 0; i < orders.Length; i++)
                {
                    orderKeys[i] = allOrders[i].Id;
                }

                Array.Sort(orderKeys, orders);

                // 4. Строим индекс для быстрой выборки всех курьеров магазина
                rc = 6;
                shopOrders = (Order[])orders.Clone();
                Array.Sort(shopOrders, CompareOrderByShop);

                shopKeys = new int[orders.Length];
                orderRange = new Point[orders.Length];
                int count = 0;
                int currentShopId = shopOrders[0].ShopId;
                int startIndex = 0;
                int endIndex = 0;

                for (int i = 1; i < shopOrders.Length; i++)
                {
                    if (shopOrders[i].ShopId == currentShopId)
                    {
                        endIndex = i;
                    }
                    else
                    {
                        shopKeys[count] = currentShopId;
                        orderRange[count].X = startIndex;
                        orderRange[count++].Y = endIndex;

                        currentShopId = shopOrders[i].ShopId;
                        startIndex = i;
                        endIndex = i;
                    }
                }

                shopKeys[count] = currentShopId;
                orderRange[count].X = startIndex;
                orderRange[count++].Y = endIndex;

                if (count < shopKeys.Length)
                {
                    Array.Resize(ref shopKeys, count);
                    Array.Resize(ref orderRange, count);
                }

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

                // 3. Находим заказ
                Order order = null;
                int index = Array.BinarySearch(this.orderKeys, orderId);
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

                // 3. Выбираем заказы магазина требующие доставки
                int index = Array.BinarySearch(shopKeys, shopId);
                if (index < 0)
                    return new Order[0];

                Point pt = orderRange[index];
                int count = pt.Y - pt.X + 1;
                Order[] result = new Order[count];
                Array.Copy(shopOrders, pt.X, result, 0, count);

                // 4. Выход - Ok
                return result;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Выбрать все заказы магазина требующие доставки
        /// с учетом времени расчетов
        /// </summary>
        /// <param name="shopId">Id магазина</param>
        /// <param name="calcTime">Время проведения расчетов</param>
        /// <returns>Выбранные заказы или null</returns>
        public Order[] GetShopOrders(int shopId, DateTime calcTime)
        {
            try
            {
                // 2. Проверяем исходные данные
                if (!IsCreated)
                    return null;

                // 3. Выбираем заказы магазина требующие доставки
                int index = Array.BinarySearch(shopKeys, shopId);
                if (index < 0)
                    return new Order[0];

                Point pt = orderRange[index];
                int startIndex = pt.X;
                int endIndex = pt.Y;

                Order[] result = new Order[endIndex - startIndex + 1];
                int count = 0;

                for (int i = startIndex; i <= endIndex; i++)
                {
                    Order order = shopOrders[i];
                    if (order.DeliveryTimeTo > calcTime)
                        result[count++] = order;
                }

                if (count < result.Length)
                {
                    Array.Resize(ref result, count);
                }

                // 4. Выход - Ok
                return result;
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

        /// <summary>
        /// Сравнение двух заказов по принадлежности одному магазину
        /// </summary>
        /// <param name="order1">Заказ 1</param>
        /// <param name="order2">Заказ 2</param>
        /// <returns>- 1  - Заказ1 меньше Заказ2; 0 - Заказ1 = Заказ2; 1 - Заказ1 больше Заказ2</returns>
        private static int CompareOrderByShop(Order order1, Order order2)
        {
            if (order1.ShopId < order2.ShopId)
                return -1;
            if (order1.ShopId > order2.ShopId)
                return 1;
            return 0;
        }
    }
}
