
//namespace LogisticsService.Orders
//{
//    using LogisticsService.Couriers;
//    using LogisticsService.Locations;
//    using System;
//    using System.Collections.Generic;
//    using System.Linq;
//    using static LogisticsService.API.GetOrderEvents;

//    /// <summary>
//    /// Все заказы
//    /// </summary>
//    public class AllOrders
//    {
//        /// <summary>
//        /// Объект синхронизации для многопоточной работы
//        /// </summary>
//        private object syncRoot = new object();

//        /// <summary>
//        /// Менеджер координат, расстояний и времени движения
//        /// </summary>
//        private LocationManager locationManager;

//        /// <summary>
//        /// Флаг: true - класс создан; false - класс не создан
//        /// </summary>
//        public static bool IsCreated { get; private set; }

//        /// <summary>
//        /// Все заказы
//        /// </summary>
//        private Dictionary<int, Order> orders;

//        public Dictionary<int, Order> Orders => orders;


//        /// <summary>
//        /// Количество заказов
//        /// </summary>
//        public int Count => (orders == null ? 0 : orders.Count);

//        /// <summary>
//        /// Создание экземпляра
//        /// </summary>
//        /// <param name="locManager">Менеджер координат, расстояний и времени движения</param>
//        /// <param name="orderLimit">Макимальное число заказов</param>
//        /// <returns>0 - экземпляр создан; экземпляр не создан</returns>
//        public int Create(LocationManager locManager, int orderLimit = -1)
//        {
//            lock (syncRoot)
//            {
//                // 1. Инициализация
//                int rc = 1;
//                IsCreated = false;
//                locationManager = null;

//                try
//                {
//                    // 2. Проверяем исходные данные
//                    rc = 2;
//                    if (locManager == null)
//                        return rc;

//                    locationManager = locManager;

//                    // 3. Создаём общую коллекцию магазинов
//                    rc = 3;
//                    if (orderLimit <= 0) orderLimit = 20000;
//                    orders = new Dictionary<int, Order>(orderLimit);

//                    // 4. Выход - Ok
//                    rc = 0;
//                    IsCreated = true;
//                    return rc;
//                }
//                catch
//                {
//                    return rc;
//                }
//            }
//        }

//        /// <summary>
//        /// Обновление информации о заказах
//        /// </summary>
//        /// <param name="events">События заказов</param>
//        /// <returns>0 - данные заказах обновлены; иначе - данные о заказах не обновлены</returns>
//        public int Refresh(OrderEvent[] events)
//        {
//            lock (syncRoot)
//            {
//                // 1. Инициализация
//                int rc = 1;

//                try
//                {
//                    // 2. Проверяем исходные данные
//                    rc = 2;
//                    if (!IsCreated)
//                        return rc;
//                    if (events == null || events.Length <= 0)
//                        return rc;

//                    // 3. Отрабатываем изменения
//                    rc = 3;
//                    for (int i = 0; i < events.Length; i++)
//                    {
//                        // 3.1 Извлекаем событие 
//                        rc = 31;
//                        OrderEvent orderEvent = events[i];

//                        // 3.2 Извлекаем магазин
//                        rc = 32;
//                        Order order;

//                        if (!orders.TryGetValue(orderEvent.order_id, out order))
//                        {
//                            // 3.3 Создаём заказ
//                            rc = 33;
//                            order = new Order(orderEvent.order_id);
//                            order.Latitude = orderEvent.geo_lat;
//                            order.Longitude = orderEvent.geo_lon;
//                            order.LocationIndex = locationManager.GetLocationIndex(orderEvent.geo_lat, orderEvent.geo_lon);
//                            order.ShopId = orderEvent.shop_id;
//                            order.Weight = orderEvent.weight;
//                            order.AssembledDate = DateTime.MaxValue;
//                            order.ReceiptedDate = DateTime.MaxValue;
//                            order.DeliveryTimeFrom = orderEvent.delivery_frame_from;
//                            order.DeliveryTimeTo = orderEvent.delivery_frame_to;
//                            order.EnabledTypes = GetDeliveryMask(orderEvent.shop_id, orderEvent.service_available);
//                            order.Status = OrderStatus.None;
//                            order.Completed = false;
//                            orders.Add(order.Id, order);
//                        }
//                        else
//                        {
//                            if (order.Latitude != orderEvent.geo_lat || order.Longitude != orderEvent.geo_lon)
//                            {
//                                order.Latitude = orderEvent.geo_lat;
//                                order.Longitude = orderEvent.geo_lon;
//                                order.LocationIndex = locationManager.GetLocationIndex(orderEvent.geo_lat, orderEvent.geo_lon);
//                            }

//                            order.ShopId = orderEvent.shop_id;
//                            order.Weight = orderEvent.weight;
//                            order.EnabledTypes = GetDeliveryMask(orderEvent.shop_id, orderEvent.service_available);
//                            order.DeliveryTimeFrom = orderEvent.delivery_frame_from;
//                            order.DeliveryTimeTo = orderEvent.delivery_frame_to;
//                        }

//                        // 3.4. Обновляем состояние заказа
//                        rc = 34;

//                        switch (orderEvent.type)
//                        {
//                            case 0:  // поступление заказа в магазин
//                                order.Completed = false;
//                                order.ReceiptedDate = orderEvent.date_event;
//                                order.AssembledDate =  DateTime.MaxValue;
//                                order.Status = OrderStatus.Receipted;
//                                break;
//                            case 1:  // завершение сбоки заказа в магазине
//                                order.Completed = false;
//                                order.AssembledDate = orderEvent.date_event;
//                                order.Status = OrderStatus.Assembled;
//                                break;
//                            case 3:  // отмена заказа
//                                order.Completed = false;
//                                order.Status = OrderStatus.Cancelled;
//                                break;
//                            //case 4:  // корректироввка заказа
//                            //    order.Completed = false;
//                            //    order.Status = OrderStatus.Cancelled;
//                            //    break;

//                        }
//                    }

//                    // 4. Выход - Ok
//                    rc = 0;
//                    return rc;
//                }
//                catch
//                {
//                    return rc;
//                }
//            }
//        }

//        /// <summary>
//        /// Построение маски доступных способов отгрузки
//        /// заказа в заданном магазине
//        /// </summary>
//        /// <param name="shopId"></param>
//        /// <param name="availableService"></param>
//        /// <returns></returns>
//        private static EnabledCourierType GetDeliveryMask(int shopId, ShopService[] availableService)
//        {
//            // 1. Инициализация
            
//            try
//            {
//                // 2. Проверяем исходные данные
//                if (availableService == null || availableService.Length <= 0)
//                    return EnabledCourierType.Unknown;

//                // 3. Строим маску доступных способов отгрузки в заданном магазине
//                EnabledCourierType mask = 0;

//                foreach(ShopService shopSevice in availableService)
//                {
//                    if (shopSevice.shop_id == shopId)
//                    {
//                        mask |= DserviceIdToEnabledCourierType(shopSevice.dservice_id);
//                    }
//                }

//                // 4. Возвращаем результат
//                return mask;
//            }
//            catch
//            {
//                return EnabledCourierType.Unknown;
//            }
//        }

//        /// <summary>
//        /// Построение маски доступных способов отгрузки
//        /// заказа в заданном магазине
//        /// </summary>
//        /// <param name="shopId">Id магазина</param>
//        /// <param name="availableService">Массив доступных сервисов отгрузки заказа</param>
//        /// <returns>Массив кодов спсобов отгрузки</returns>
//        private static CourierVehicleType[] GetDeliveryMaskEx(int shopId, ShopService[] availableService)
//        {
//            // 1. Инициализация
            
//            try
//            {
//                // 2. Проверяем исходные данные
//                if (availableService == null || availableService.Length <= 0)
//                    return new CourierVehicleType[0];

//                // 3. Строим маску доступных способов отгрузки в заданном магазине
//                List<CourierVehicleType> vehicleTypes = new List<CourierVehicleType>(32);

//                foreach(ShopService shopSevice in availableService)
//                {
//                    if (shopSevice.shop_id == shopId)
//                    {
//                        switch (shopSevice.dservice_id)
//                        {
//                            case 4:
//                                vehicleTypes.Add(CourierVehicleType.OnFoot);
//                                vehicleTypes.Add(CourierVehicleType.Bicycle);
//                                vehicleTypes.Add(CourierVehicleType.Car);
//                                break;
//                            case 12:
//                                vehicleTypes.Add(CourierVehicleType.GettTaxi);
//                                break;
//                            case 14:
//                                vehicleTypes.Add(CourierVehicleType.YandexTaxi);
//                                break;
//                        }
//                    }
//                }

//                // 4. Возвращаем результат
//                return vehicleTypes.ToArray();
//            }
//            catch
//            {
//                return null;
//            }
//        }

//        /// <summary>
//        /// Преобразование Id сервиса доставки в 
//        /// маску доступных способов доставки
//        /// </summary>
//        /// <param name="dservice_id">Id сервиса доставки</param>
//        /// <returns>Макска способов отгрузки</returns>
//        private static EnabledCourierType DserviceIdToEnabledCourierType(int dservice_id)
//        {
//            switch (dservice_id)
//            {
//                case 4:
//                    return EnabledCourierType.OnFoot | EnabledCourierType.Bicycle | EnabledCourierType.Car;
//                case 12:
//                    return EnabledCourierType.GettTaxi;
//                case 14:
//                    return EnabledCourierType.YandexTaxi;
//            }

//            return 0;
//        }

//        /// <summary>
//        /// Выбрать все заказы магазина требующие доставки
//        /// </summary>
//        /// <param name="shopId">Id магазина</param>
//        /// <returns>Выбранные заказы или null</returns>
//        public Order[] GetShopOrders(int shopId)
//        {
//            try
//            {
//                // 2. Проверяем исходные данные
//                if (!IsCreated)
//                    return null;
//                if (Count <= 0)
//                    return new Order[0];

//                // 3. Возвращаем результат
//                return orders.Values.Where(order => (order.ShopId == shopId && !order.Completed && (order.Status == OrderStatus.Assembled || order.Status == OrderStatus.Receipted))).ToArray();
//            }
//            catch
//            {
//                return null;
//            }
//        }
//    }
//}
