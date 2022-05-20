
namespace DeliveryBuilder.Orders
{
    using DeliveryBuilder.Log;
    using DeliveryBuilder.Shops;
    using DeliveryBuilder.Couriers;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Работа с заказами
    /// </summary>
    public class AllOrdersEx
    {
        /// <summary>
        /// Коллекция заказов
        /// </summary>
        private Dictionary<int, Order> orders;

        /// <summary>
        /// Флаг: true - класс создан; false - класс не создан
        /// </summary>
        public bool IsCreated { get; private set; }

        /// <summary>
        /// Количество заказов
        /// </summary>
        public int Count => (orders == null ? 0 : orders.Count);

        /// <summary>
        /// Создание экземпляра
        /// </summary>
        /// <param name="allOrders">Все заказы</param>
        /// <returns></returns>
        public int Create()
        {
            // 1. Инициализация
            int rc = 1;
            IsCreated = false;
            orders = null;

            try
            {
                // 2. Создаём коллекцию заказов
                rc = 2;
                orders = new Dictionary<int, Order>(5000);

                // 3. Выход - Ok
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
                Order[] result = new Order[orders.Count];
                int count = 0;

                foreach (var order in orders.Values)
                {
                    if (!order.Completed && order.ShopId == shopId && 
                        (order.Status == OrderStatus.Receipted || order.Status == OrderStatus.Assembled))
                    { result[count++] = order; }
                }

                if (count < result.Length)
                { Array.Resize(ref result, count); }

                // 4. Выход - Ok
                return result;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Выбрать все заказы требующие доставки
        /// для заданных магазинов
        /// </summary>
        /// <param name="shopId">Id магазинов</param>
        /// <returns>Выбранные заказы или null</returns>
        public Order[] GetShopOrders(int[] shopId)
        {
            try
            {
                // 2. Проверяем исходные данные
                if (!IsCreated)
                    return null;
                if (shopId == null || shopId.Length <= 0)
                    return null;

                // 3. Выбираем заказы магазина требующие доставки
                Order[] result = new Order[orders.Count];
                int count = 0;

                Array.Sort(shopId);

                foreach (var order in orders.Values)
                {
                    if (!order.Completed &&  
                        (order.Status == OrderStatus.Receipted || order.Status == OrderStatus.Assembled))
                    {
                        if (Array.BinarySearch(shopId, order.ShopId) >= 0)
                            result[count++] = order;
                    }
                }

                if (count < result.Length)
                { Array.Resize(ref result, count); }

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
                Order[] result = new Order[orders.Count];
                int count = 0;

                foreach (var order in orders.Values)
                {
                    if (!order.Completed && order.ShopId == shopId &&
                        order.DeliveryTimeTo > calcTime &&
                        (order.Status == OrderStatus.Receipted || order.Status == OrderStatus.Assembled))
                    { result[count++] = order; }
                }

                if (count < result.Length)
                { Array.Resize(ref result, count); }

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
        /// Обновление заказов
        /// </summary>
        /// <param name="updates">Новые данные</param>
        /// <param name="shops">Коллекция магазинов</param>
        /// <returns></returns>
        public int Update(OrdersUpdates updates, AllShops shops, AllCouriersEx couriers)
        {
            // 1. Иициализация
            int rc = 1;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (updates == null || updates.Updates == null || updates.Updates.Length <= 0)
                    return rc;
                if (shops == null)
                    return rc;
                if (couriers == null || !couriers.IsCreated)
                    return rc;

                // 3. Отрабатываем изменения данных курьеров
                rc = 3;

                foreach (var orderUpdates in updates.Updates)
                {
                    // 3.1 Извлекаем статус
                    rc = 31;
                    OrderStatus status = OrderStatusFromInputType(orderUpdates.Status);

                    Order order;
                    if (orders.TryGetValue(orderUpdates.OrderId, out order))
                    {
                        // 3.2 Обновление сущесвующего заказа
                        rc = 32;
                        order.Status = status;
                        if (status == OrderStatus.Receipted || status == OrderStatus.Assembled)
                            order.Completed = false;
                        order.ShopId = orderUpdates.ShopId;
                        order.Priority = orderUpdates.Priority;
                        if (orderUpdates.Weight > 0)
                            order.Weight = orderUpdates.Weight;
                        if (orderUpdates.Latitude != 0)
                            order.Latitude = orderUpdates.Latitude;
                        if (orderUpdates.Longitude != 0)
                            order.Longitude = orderUpdates.Longitude;
                        if (status == OrderStatus.Assembled)
                            order.AssembledDate = orderUpdates.EventTime;
                        if (status == OrderStatus.Receipted)
                            order.ReceiptedDate = orderUpdates.EventTime;
                        order.DeliveryTimeFrom = orderUpdates.TimeFrom;
                        order.DeliveryTimeTo = orderUpdates.TimeTo;
                        order.TimeCheckDisabled = orderUpdates.TimeCheckDisabled;
                        order.VehicleTypes = GetOrderVehicleTypes(orderUpdates.ShopId, orderUpdates.DServices, couriers);
                    }
                    else
                    {
                        // 3.4 Добавляем нового курьера
                        rc = 34;
                        order = new Order(orderUpdates.OrderId);
                        order.Status = status;
                        order.Completed = false;
                        order.ShopId = orderUpdates.ShopId;
                        order.Priority = orderUpdates.Priority;
                        if (orderUpdates.Weight >= 0)
                            order.Weight = orderUpdates.Weight;
                        if (orderUpdates.Latitude != 0)
                            order.Latitude = orderUpdates.Latitude;
                        if (orderUpdates.Longitude != 0)
                            order.Longitude = orderUpdates.Longitude;
                        if (status == OrderStatus.Assembled)
                            order.AssembledDate = orderUpdates.EventTime;
                        if (status == OrderStatus.Receipted)
                            order.ReceiptedDate = orderUpdates.EventTime;
                        order.DeliveryTimeFrom = orderUpdates.TimeFrom;
                        order.DeliveryTimeTo = orderUpdates.TimeTo;
                        order.TimeCheckDisabled = orderUpdates.TimeCheckDisabled;
                        order.VehicleTypes = GetOrderVehicleTypes(orderUpdates.ShopId, orderUpdates.DServices, couriers);
                        orders.Add(order.Id, order);
                    }

                    // 3.5 Добавляем маазин, если он не сущесвует
                    rc = 35;
                    Shop shop = null;
                    if (!shops.Shops.TryGetValue(orderUpdates.ShopId, out shop))
                    {
                        if (orderUpdates.ShopLatitude != 0 && orderUpdates.ShopLongitude != 0)
                        {
                            shop = new Shop(orderUpdates.ShopId);
                            shop.Latitude = orderUpdates.ShopLatitude;
                            shop.Longitude = orderUpdates.ShopLongitude;
                            shop.Updated = true;
                            shops.Add(shop);
                        }
                    }
                    else
                    {
                        shop.Updated = true;
                    }
                }

                // 4. Выход - Ok
                rc = 0;
                return rc;
            }
            catch (Exception ex)
            {
                Logger.WriteToLog(666, MessageSeverity.Error, string.Format(Messages.MSG_666, $"{nameof(AllOrdersEx)}.{nameof(this.Update)}", (ex.InnerException == null ? ex.Message : ex.InnerException.Message)));
                return rc;
            }
        }

        /// <summary>
        /// Преобразование Input type в OrderStatus
        /// </summary>
        /// <param name="inputType"></param>
        /// <returns></returns>
        private static OrderStatus OrderStatusFromInputType(int inputType)
        {
            switch (inputType)
            {
                case 0:
                    return OrderStatus.Receipted;
                case 1:
                    return OrderStatus.Assembled;
                case 3:
                    return OrderStatus.Cancelled;
                default:
                    return OrderStatus.None;
            }
        }

        /// <summary>
        /// Выбор VehicleID для заданных DServiceID
        /// </summary>
        /// <param name="shopId">ID магазина</param>
        /// <param name="dservices">DServiceID</param>
        /// <param name="couriers">Курьеры</param>
        /// <returns>VehicleIDs</returns>
        private static int[] GetOrderVehicleTypes(int shopId, DService[] dservices, AllCouriersEx couriers)
        {
            // 1. Инициализация

            try
            {
                // 2. Проверяем исходные данные
                if (dservices == null || dservices.Length <= 0 ||
                    couriers == null || !couriers.IsCreated)
                    return new int[0];

                // 3. Выбираем DServiceID для заданного магазина
                int[] shopDServiceId = new int[dservices.Length];
                int count = 0;
                
                for (int i = 0; i < dservices.Length; i++)
                {
                    if (dservices[i].ShopId == shopId)
                    { shopDServiceId[count++] = dservices[i].DServiceId; }
                }

                if (count <= 0)
                { return new int[0]; }
                else if (count < shopDServiceId.Length)
                { Array.Resize(ref shopDServiceId, count); }

                // 4. Выбираем VehicleID
                int[] result = couriers.GetVehiclesTypesForDService(shopDServiceId);
                if (result == null)
                { result = new int[0]; }

                // 5. Выход
                return result;
            }
            catch
            {
                return new int[0];
            }
        }

        /// <summary>
        /// Установка свойства Completed заказа
        /// </summary>
        /// <param name="orderId">ID заказа</param>
        /// <param name="value">Значение</param>
        public void SetCompleted(int orderId, bool value)
        {
            if (!IsCreated || orders == null)
                return;
            Order order;
            if (orders.TryGetValue(orderId, out order))
                order.Completed = value;
        }
    }
}
