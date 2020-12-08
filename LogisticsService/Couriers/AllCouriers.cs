
namespace LogisticsService.Couriers
{
    using LogisticsService.API;
    using LogisticsService.ServiceParameters;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Все курьеры
    /// </summary>
    public class AllCouriers
    {
        /// <summary>
        /// Объект синхронизации для многопоточной работы
        /// </summary>
        private object syncRoot = new object();

        /// <summary>
        /// Флаг: true - класс создан; false - класс не создан
        /// </summary>
        public static bool IsCreated { get; private set; }

        /// <summary>
        /// Параметры всех допустимых курьеров
        /// </summary>
        private Dictionary<CourierVehicleType, ICourierType> courierTypes;

        #region Средняя стоимость заказа различными способами в каждом магазине

        /// <summary>
        /// Отсортированные ключи средней стоимости доставки
        /// Ключ = (shopId, CourierVehicleType)
        /// </summary>
        private ulong[] averageCostKey;

        /// <summary>
        /// Средняя стоимость доставки заказа заданным способом в заданном магазине, руб
        /// </summary>
        private double[] averageCost;

        #endregion Средняя стоимость заказа заданным способом в каждом магазине

        /// <summary>
        /// Все курьеры
        /// </summary>
        private Dictionary<int, Courier> couriers;

        /// <summary>
        /// Все курьеры
        /// </summary>
        public Dictionary<int, Courier> Couriers => couriers;

        /// <summary>
        /// Количество курьеров
        /// </summary>
        public int Count => (couriers == null ? 0 : couriers.Count);

        /// <summary>
        /// Создание экземпляра
        /// </summary>
        /// <param name="courierLimit">Ограничение на общее число курьеров</param>
        public int Create(ServiceConfig config, int courierLimit = -1)
        {
            lock (syncRoot)
            {
                // 1. Инициализация
                int rc = 1;
                IsCreated = false;
                couriers = null;
                courierTypes = null;
                averageCostKey = null;
                averageCost = null;

                try
                {
                    // 2. Проверяем исходные данные
                    rc = 2;
                    if (config == null || 
                        config.couriers == null || config.couriers.Length <= 0 ||
                        config.average_cost == null || config.average_cost.Length <= 0)
                        return rc;

                    // 3. Создаём общую коллекцию курьеров
                    rc = 3;
                    if (courierLimit <= 0) courierLimit = 2000;
                    couriers = new Dictionary<int, Courier>(courierLimit);

                    // 4. Создаём коллекцию типов курьеров
                    rc = 4;
                    courierTypes = new Dictionary<CourierVehicleType, ICourierType>(config.couriers.Length);

                    foreach (CourierParameters courierPararams in config.couriers)
                    {
                        courierTypes[courierPararams.VechicleType] = courierPararams;
                        if (courierPararams.VechicleType == CourierVehicleType.YandexTaxi ||
                            courierPararams.VechicleType == CourierVehicleType.GettTaxi)
                        {
                            CourierBase courierBase = new CourierBase(courierPararams);
                            Courier courier = new Courier((int)courierPararams.VechicleType, courierBase);
                            courier.WorkStart = TimeSpan.Zero;
                            courier.WorkEnd = TimeSpan.FromHours(24);
                            courier.ShopId = -1;
                            courier.Status = CourierStatus.Ready;
                            couriers[courier.Id] = courier;
                        }
                    }

                    // 4. Создаём коллекцию средних времен доставки для каждого типа курьера в каждом магазине
                    rc = 4;
                    averageCostKey = new ulong[config.average_cost.Length];
                    averageCost = new double[averageCostKey.Length];


                    for (int i = 0; i < averageCost.Length; i++)
                    {
                        AverageCostByVechicle vechicleAverageCost = config.average_cost[i];
                        averageCostKey[i] = GetCostKey(vechicleAverageCost.shop_id, (int) vechicleAverageCost.vehicle_type);
                        averageCost[i] = vechicleAverageCost.average_cost;
                    }

                    Array.Sort(averageCostKey, averageCost);

                    // 5. Выход - Ok
                    rc = 0;
                    IsCreated = true;
                    return rc;
                }
                catch
                {
                    return rc;
                }
            }
        }

        /// <summary>
        /// Средняя стоимость доставки заданным способом в заданном магазине
        /// </summary>
        /// <param name="shopId">Id магазина</param>
        /// <param name="courierType">Способ доставки</param>
        /// <returns>Средняя стоимость или NaN</returns>
        private double GetAverageCost(int shopId, CourierVehicleType courierType)
        {
            //lock(syncRoot)
            //{
                try
                {
                    // 2. Проверяем исходные данные
                    if (!IsCreated)
                        return double.NaN;

                    // 3. Находим индекс значения
                    int index = Array.BinarySearch(averageCostKey, GetCostKey(shopId, (int)courierType));
                    if (index < 0)
                        return double.NaN;

                    // 4. Возвращаем результат
                    return averageCost[index];
                }
                catch
                {
                    return double.NaN;
                }
            //}
        }

        /// <summary>
        /// Обновление информации о курьерах
        /// </summary>
        /// <param name="events">События курьеров</param>
        /// <returns>0 - данные курьеров обновлены; иначе - данные о курьерах не обновлены</returns>
        public int Refresh(CourierEvent[] events)
        {
            lock (syncRoot)
            {
                // 1. Инициализация
                int rc = 1;

                try
                {
                    // 2. Проверяем исходные данные
                    rc = 2;
                    if (!IsCreated)
                        return rc;
                    if (events == null || events.Length <= 0)
                        return rc;

                    // 3. Отрабатываем изменения
                    rc = 3;
                    for (int i = 0; i < events.Length; i++)
                    {
                        // 3.1 Извлекаем событие 
                        rc = 31;
                        CourierEvent courierEvent = events[i];
                        CourierVehicleType vehicleType = CourierEventTypeToCourierVehicleType(courierEvent.courier_type);
                        if (vehicleType == CourierVehicleType.Unknown)
                            continue;

                        // 3.2 Извлекаем курьера
                        rc = 32;
                        Courier courier;

                        if (!couriers.TryGetValue(courierEvent.courier_id, out courier))
                        {
                            // 3.3 Извлекаем параметры курьера заданного типа
                            rc = 33;
                            ICourierType courierType;
                            if (!courierTypes.TryGetValue(vehicleType, out courierType))
                                continue;

                            // 3.4 Создаём курьера
                            rc = 34;
                            CourierBase courierBase = new CourierBase(courierType);
                            courier = new Courier(courierEvent.courier_id, courierBase);
                            couriers.Add(courier.Id, courier);
                        }

                        // 3.5. Обновляем параметры курьера
                        rc = 35;
                        courier.WorkStart = courierEvent.work_start.TimeOfDay;
                        courier.WorkEnd = courierEvent.work_end.TimeOfDay;
                        courier.Latitude = courierEvent.geo_lat;
                        courier.Longitude = courierEvent.geo_lon;
                        courier.ShopId = courierEvent.shop_id;

                        // 3.6 Обновляем состояние
                        rc = 36;
                        if (courier.IsTaxi)
                        {
                            courier.Status = CourierStatus.Ready;
                        }
                        else
                        {
                            switch (courierEvent.type)
                            {
                                case 0:  // доступен, но не в магазине
                                    courier.Status = CourierStatus.MoveToPoint;
                                    break;
                                case 1:  // прибытие в магазин
                                    courier.Status = CourierStatus.Ready;
                                    courier.LastDeliveryEnd = courierEvent.date_event;
                                    break;
                                case 2:  // подтверждение начала доставки
                                    courier.Status = CourierStatus.DeliversOrder;
                                    courier.LastDeliveryStart = courierEvent.date_event;
                                    break;
                                case 3:  // выполняет задание на доставку
                                    courier.Status = CourierStatus.DeliversOrder;
                                    break;
                                case 5:  // завершение работы
                                    courier.Status = CourierStatus.WorkEnded;
                                    break;
                                case 6:  // не доступен
                                    courier.Status = CourierStatus.Unknown;
                                    break;
                            }
                        }

                        // 3.7 Пытаемся найти среднюю стоимость доставки
                        rc = 37;
                        double averageCost = GetAverageCost(courier.ShopId, courier.CourierType.VechicleType);
                        if (!double.IsNaN(averageCost))
                            courier.AverageOrderCost = averageCost;
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
        }

        /// <summary>
        /// Курьеры магазина
        /// (Фиксированная модель)
        /// </summary>
        /// <param name="shopId">Id магазина</param>
        /// <param name="readyOnly">true - только доступные для отгрузки; false - все курьеры</param>
        /// <returns>Курьры или null</returns>
        public Courier[] GetShopCouriers(int shopId, bool readyOnly = true)
        {
            lock (syncRoot)
            {
                if (!IsCreated)
                    return null;

                Courier[] shopCouriers = new Courier[couriers.Count];
                int count = 0;

                foreach (Courier courier in couriers.Values)
                {
                    if (readyOnly && courier.Status != CourierStatus.Ready)
                        continue;
                    if (courier.IsTaxi)
                    {
                        double averageCost = GetAverageCost(shopId, courier.CourierType.VechicleType);
                        if (!double.IsNaN(averageCost))
                        {
                            courier.AverageOrderCost = averageCost;
                        }
                        else
                        {
                            courier.AverageOrderCost = 0.95 * courier.CourierType.FirstPay;
                        }

                        courier.ShopId = shopId;
                        shopCouriers[count++] = courier;
                    }
                    else if (courier.ShopId == shopId)
                    {
                        shopCouriers[count++] = courier;
                    }
                }

                Array.Resize(ref shopCouriers, count);
                return shopCouriers;
            }
        }

        /// <summary>
        /// Построение ulong ключа для двух int
        /// </summary>
        /// <param name="value1">Значение 1</param>
        /// <param name="value2">Значение 2</param>
        /// <returns>Ключ</returns>
        private ulong GetCostKey(int value1, int value2)
        {
            return (ulong) value1 << 32 | ((ulong)value2);
        }

        /// <summary>
        /// Преобразование способа доставки из API-отклика
        /// во внутреннюю форму
        /// </summary>
        /// <param name="courierEventType">Способ доставки из отклика</param>
        /// <returns>Тип курьера</returns>
        private CourierVehicleType CourierEventTypeToCourierVehicleType(string courierEventType)
        {
            if (string.IsNullOrWhiteSpace(courierEventType))
                return CourierVehicleType.Unknown;
            switch (courierEventType.Trim().ToUpper())
            {
                case "WALKING":
                    return CourierVehicleType.OnFoot;
                case "DRIVING":
                    return CourierVehicleType.Car;
                case "CYCLING":
                    return CourierVehicleType.Bicycle;
            }

            return CourierVehicleType.Unknown;
        }

        /// <summary>
        /// Поиск первого попавшегося курьера
        /// заданного типа
        /// </summary>
        /// <param name="vehicleType">Тип курьера</param>
        /// <returns>Курьер или null</returns>
        public Courier FindFirstByType(CourierVehicleType vehicleType)
        {
            try
            {
                // 2. Проверяем исходные данные
                if (!IsCreated)
                    return null;

                // 3. Находим первого курьера заданного типа
                foreach (Courier courier in couriers.Values)
                {
                    if (courier.CourierType.VechicleType == vehicleType)
                        return courier;
                }

                // 4. Курьер заданного типа не найден
                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Построение эталонного курьера
        /// </summary>
        /// <param name="vehicleType">Способ доставки</param>
        /// <returns>Построенный курьер</returns>
        public Courier GetReferenceCourier(CourierVehicleType vehicleType)
        {
            // 1. Инициализация

            try
            {
                // 2. Проверяем исходные данные
                if (!IsCreated)
                    return null;

                // 3. Извлекаем параметры курьера заданного типа
                ICourierType courierType;
                if (!courierTypes.TryGetValue(vehicleType, out courierType))
                    return null;

                // 4. Строим эталонного курьера заданного типа
                CourierBase courierBase = new CourierBase(courierType);
                Courier courier = new Courier((int) vehicleType, courierBase);
                courier.WorkStart = TimeSpan.Zero;
                courier.WorkEnd = TimeSpan.FromHours(24);
                courier.ShopId = -1;
                courier.Status = CourierStatus.Ready;
                courier.LunchTimeStart = TimeSpan.Zero;
                courier.LunchTimeEnd = TimeSpan.Zero;

                // 5. Выход - Ok
                return courier;
            }
            catch
            {
                return null;
            }
        }
    }
}
