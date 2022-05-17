
namespace DeliveryBuilder.Couriers
{
    using DeliveryBuilder.AverageDeliveryCost;
    using DeliveryBuilder.BuilderParameters;
    using DeliveryBuilder.Db;
    using DeliveryBuilder.Log;
    using DeliveryBuilder.Shops;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Работа с курьерами
    /// </summary>
    public class AllCouriersEx
    {
        /// <summary>
        /// ID магазина приписки такси
        /// </summary>
        private const int TAXI_SHOP_ID = -1;

        #region Имена обязательных параметров

        /// <summary>
        /// Время получения заказа курьером, мин
        /// </summary>
        private const string parameterGetOrderTime = "get_order_time";

        /// <summary>
        /// Время вручения заказа заказчику, мин
        /// </summary>
        private const string parameterHandinTime = "handin_time";

        /// <summary>
        /// Максимальный вес одного заказа, кг
        /// </summary>
        private const string parameterMaxОrderWeight = "max_order_weight";

        /// <summary>
        /// Максимальный общий вес всех заказов, кг
        /// </summary>
        private const string parameterMaxWeight = "max_weight";

        /// <summary>
        /// Максимальная длина пути от магазина до последнего заказа в отгрузке, км
        /// </summary>
        private const string parameterMaxDistance = "max_distance";

        /// <summary>
        /// Время подачи (задержка) транспортного средства, мин
        /// </summary>
        private const string parameterStartDelay = "start_delay";

        /// <summary>
        /// Максимальное число заказов в отгузке
        /// </summary>
        private const string parameterMaxOrders = "max_orders";

        /// <summary>
        /// Наименоваие метода расчета стоимости и времени доставки
        /// </summary>
        private const string parameterCalcMethod = "calc_method";

        #endregion Имена обязательных параметров

        #region Базовые типы

        /// <summary>
        /// Базовые типы курьеров
        /// </summary>
        private CourierBase[] baseTypes;

        /// <summary>
        /// Ключи базовых типов (VehicleID)
        /// </summary>
        private int[] baseKeys;

        /// <summary>
        /// Базовые типы курьеров, отсортированные по VehicleID
        /// </summary>
        public CourierBase[] BaseTypes => baseTypes;

        /// <summary>
        /// Отсортированные ключи базовых типов (VehicleID)
        /// </summary>
        public int[] BaseKeys => baseKeys;

        #endregion Базовые типы

        /// <summary>
        /// Все курьеры и такси
        /// </summary>
        private Dictionary<int, Courier> couriers;

        /// <summary>
        /// Все курьеры и такси
        /// </summary>
        public Dictionary<int, Courier> Couriers => couriers;

        /// <summary>
        /// Количество курьеров
        /// </summary>
        public int Count => (couriers == null ? 0 : couriers.Count);

        #region Соответствие courier_type --> vehicle_id

        /// <summary>
        /// Отсортирванные типы курьеров из входных данных
        /// </summary>
        private int[] courierTypes;

        /// <summary>
        /// VehicleID соответствующие CourierTypes
        /// </summary>
        private int[] vehicleTypes;

        #endregion Соответствие courier_type --> vehicle_id

        /// <summary>
        /// Флаг: true - экземпляр создан; false - экземпляр не создан
        /// </summary>
        public bool IsCreated { get; set; }

        /// <summary>
        /// Последнее прерывание
        /// </summary>
        public Exception LastException { get; private set; }

        /// <summary>
        /// Текст последнего сообщения об ошибке
        /// </summary>
        /// <returns></returns>
        public string GetLastErrorMessage()
        {
            if (LastException == null)
            { return null; }
            if (LastException.InnerException == null)
            { return LastException.Message; }

            return LastException.InnerException.Message;
        }

        /// <summary>
        /// Создание экземпляра
        /// </summary>
        /// <param name="vehiclesRecords">Записи таблицы lsvCourierTypes</param>
        /// <param name="courierRecords">Записи таблицы lsvCouriers</param>
        /// <returns>0 - экземпляр создан; экземпляр не создан</returns>
        public int Create(VehiclesRecord[] vehiclesRecords, YandexTypeName[] yandexTypeNames, AverageCostThresholds costThresholds)
        {
            // 1. Инициализация
            int rc = 1;
            IsCreated = false;
            LastException = null;
            couriers = null;
            baseTypes = null;
            baseKeys = null;
            courierTypes = null;
            vehicleTypes = null;

            try
            {
                // 2. Проверяем иcходные данные
                rc = 2;
                if (vehiclesRecords == null || vehiclesRecords.Length <= 0)
                    return rc;
                if (yandexTypeNames == null || yandexTypeNames.Length <= 0)
                    return rc;
                if (costThresholds == null || !costThresholds.IsCreated)
                    return rc;

                // 3. Строим словарь типов yandex
                rc = 3;
                int[] yandexTypeId = new int[yandexTypeNames.Length];
                string[] yandexTypeName = new string[yandexTypeNames.Length];

                for (int i = 0; i <yandexTypeNames.Length; i++)
                {
                    yandexTypeId[i] = yandexTypeNames[i].Id;
                    yandexTypeName[i] = yandexTypeNames[i].Name;
                }

                Array.Sort(yandexTypeName, yandexTypeId);

                // 4. Выбираем всех делегатов расчета времени и стоимости отгрузки
                rc = 4;
                GetTimeAndCostDelegate[] calculators = TimeAndCostCalculator.SelectCalculators();
                if (calculators == null || calculators.Length <= 0)
                    return rc;
                string[] calculatorKeys = new string[calculators.Length];

                for (int i = 0; i < calculators.Length; i++)
                {
                    calculatorKeys[i] = calculators[i].Method.Name.ToLower();
                }

                Array.Sort(calculatorKeys, calculators);

                // 5. Создаём базовые типы курьеров и такси 
                rc = 5;
                baseTypes = new CourierBase[vehiclesRecords.Length];
                baseKeys = new int[vehiclesRecords.Length];
                courierTypes = new int[vehiclesRecords.Length];
                vehicleTypes = new int[vehiclesRecords.Length];

                for (int i = 0; i < vehiclesRecords.Length; i++)
                {
                    // 5.1 Выбираем YandexTypeId
                    rc = 51;
                    VehiclesRecord vehicleRecord = vehiclesRecords[i];
                    int yandexTypeIndex = Array.BinarySearch(yandexTypeName, vehicleRecord.Parameters.Mapper.Yandex.Value);
                    if (yandexTypeIndex < 0)
                    {
                        Logger.WriteToLog(14, MsessageSeverity.Error, string.Format(Messages.MSG_014, vehicleRecord.VehicleId, vehicleRecord.Parameters.Mapper.Yandex.Value));
                        return rc;
                    }
                    vehicleRecord.Parameters.Mapper.Yandex.Id = yandexTypeId[yandexTypeIndex];

                    // 5.2 Выбираем соответсвие courier_type --> vehicle_id
                    rc = 52;
                    courierTypes[i] = vehicleRecord.Parameters.Mapper.Input.Value;
                    vehicleTypes[i] = vehicleRecord.VehicleId;

                    // 5.3 Преобразуем VehiclesRecord в CourierTypeRecord
                    rc = 53;
                    CourierTypeRecord record = ConvertVehiclesRecord(vehicleRecord);
                    if (record == null)
                    {
                        Logger.WriteToLog(14, MsessageSeverity.Error, string.Format(Messages.MSG_014, vehicleRecord.VehicleId, vehicleRecord.Parameters.Mapper.Yandex.Value));
                        return rc;
                    }

                    // 5.4 Создаём базовый тип
                    rc = 54;
                    baseTypes[i] = new CourierBase(record);
                    baseKeys[i] = record.VehicleID;

                    // 5.5 Назначаем метод расчета
                    rc = 55;
                    int index = Array.BinarySearch(calculatorKeys, TimeAndCostCalculator.GetMethodName(record.CalcMethod).ToLower());
                    if (index < 0)
                    {
                        Logger.WriteToLog(17, MsessageSeverity.Error, string.Format(Messages.MSG_017, vehicleRecord.VehicleId, record.CalcMethod));
                        return rc;

                    }
                    
                    baseTypes[i].SetCalculator(calculators[index]);
                }

                Array.Sort(courierTypes, vehicleTypes);

                // 6. Создаём такси
                rc = 6;
                couriers = new Dictionary<int, Courier>(1000);

                for (int i = 0; i < baseTypes.Length; i++)
                {
                    // 6.1 Фильтруем такси
                    rc = 61;
                    CourierBase baseType = baseTypes[i];
                    if (!baseType.IsTaxi)
                        continue;

                    // 6.2 Находим порог для средней стоимоси
                    rc = 62;
                    double threshold = costThresholds.GetThreshold(baseType.VehicleID, TAXI_SHOP_ID);

                    // 6.3 Создаём куьера
                    rc = 63;
                    Courier courier = new Courier(baseType.VehicleID, baseType);
                    courier.Status = CourierStatus.Ready;
                    courier.AverageOrderCost = threshold;
                    courier.LastDeliveryStart = DateTime.Now;
                    courier.LastDeliveryEnd = DateTime.Now;
                    courier.LunchTimeStart = new TimeSpan(0);
                    courier.LunchTimeEnd = new TimeSpan(0);
                    courier.OrderCount = 0;
                    courier.ShopId = TAXI_SHOP_ID;
                    courier.TotalCost = 0;
                    courier.TotalDeliveryTime = 0;
                    courier.WorkStart = new TimeSpan(0);
                    courier.WorkEnd = new TimeSpan(23, 59, 59);
                    couriers.Add(courier.VehicleID, courier);
                }

                // 7. Выход - Ok
                rc = 0;
                IsCreated = true;
                return rc;
            }
            catch (Exception ex)
            {
                Logger.WriteToLog(666, MsessageSeverity.Error, string.Format(Messages.MSG_666, $"{nameof(AllCouriersEx)}.{nameof(this.Create)}", (ex.InnerException == null ? ex.Message : ex.InnerException.Message)));
                return rc;
            }
        }

        /// <summary>
        /// Преобразование VehiclesRecord в CourierTypeRecord
        /// </summary>
        /// <param name="record">Пребразуемая VehiclesRecord</param>
        /// <returns>CourierTypeRecord или null</returns>
        private static CourierTypeRecord ConvertVehiclesRecord(VehiclesRecord record)
        {
            // 1. Иициализация

            try
            {
                // 2. Проверяем исходные данные
                if (record == null)
                    return null;
                if(record.Parameters == null || !record.Parameters.IsCreated)
                {
                    Logger.WriteToLog(16, MsessageSeverity.Error, string.Format(Messages.MSG_016, record.VehicleId));
                    return null;
                }

                // 3. Извлекаем обязательные параметры
                CourierTypeData parameters = record.Parameters;

                double getOrderTime = parameters.GetDoubleParameterValue(parameterGetOrderTime);
                if (double.IsNaN(getOrderTime) || getOrderTime < 0)
                {
                    Logger.WriteToLog(15, MsessageSeverity.Error, string.Format(Messages.MSG_015, record.VehicleId, parameterGetOrderTime));
                    return null;
                }

                double handinTime = parameters.GetDoubleParameterValue(parameterHandinTime);
                if (double.IsNaN(handinTime) || handinTime < 0)
                {
                    Logger.WriteToLog(15, MsessageSeverity.Error, string.Format(Messages.MSG_015, record.VehicleId, parameterHandinTime));
                    return null;
                }

                double maxОrderWeight = parameters.GetDoubleParameterValue(parameterMaxОrderWeight);
                if (double.IsNaN(maxОrderWeight) || maxОrderWeight <= 0)
                {
                    Logger.WriteToLog(15, MsessageSeverity.Error, string.Format(Messages.MSG_015, record.VehicleId, parameterMaxОrderWeight));
                    return null;
                }

                double maxWeight = parameters.GetDoubleParameterValue(parameterMaxWeight);
                if (double.IsNaN(maxWeight) || maxWeight <= 0 || maxWeight < maxОrderWeight)
                {
                    Logger.WriteToLog(15, MsessageSeverity.Error, string.Format(Messages.MSG_015, record.VehicleId, parameterMaxWeight));
                    return null;
                }

                double maxDistance = parameters.GetDoubleParameterValue(parameterMaxDistance);
                if (double.IsNaN(maxDistance) || maxDistance <= 0)
                {
                    Logger.WriteToLog(15, MsessageSeverity.Error, string.Format(Messages.MSG_015, record.VehicleId, parameterMaxDistance));
                    return null;
                }

                double startDelay = parameters.GetDoubleParameterValue(parameterStartDelay);
                if (double.IsNaN(startDelay) || startDelay < 0)
                {
                    Logger.WriteToLog(15, MsessageSeverity.Error, string.Format(Messages.MSG_015, record.VehicleId, parameterStartDelay));
                    return null;
                }

                int maxOrders = parameters.GetIntParameterValue(parameterMaxOrders);
                if (maxOrders <= 0)
                {
                    Logger.WriteToLog(15, MsessageSeverity.Error, string.Format(Messages.MSG_015, record.VehicleId, parameterMaxOrders));
                    return null;
                }

                string calcMethod = parameters.GetStringParameterValue(parameterCalcMethod);
                if (string.IsNullOrWhiteSpace(calcMethod))
                {
                    Logger.WriteToLog(15, MsessageSeverity.Error, string.Format(Messages.MSG_015, record.VehicleId, parameterCalcMethod));
                    return null;
                }

                // 4. Возвращаем результат
                return new CourierTypeRecord(record.VehicleId,
                                             record.IsTaxi,
                                             parameters.Mapper.Yandex.Id,
                                             parameters.Mapper.DService.Value,
                                             parameters.Mapper.Input.Value,
                                             calcMethod,
                                             maxОrderWeight,
                                             maxWeight,
                                             maxOrders,
                                             maxDistance,
                                             getOrderTime,
                                             handinTime,
                                             startDelay,
                                             parameters);
            }
            catch (Exception ex)
            {
                Logger.WriteToLog(666, MsessageSeverity.Error, string.Format(Messages.MSG_666, $"{nameof(AllCouriersEx)}.{nameof(AllCouriersEx.ConvertVehiclesRecord)}", (ex.InnerException == null ? ex.Message : ex.InnerException.Message)));
                return null;
            }
        }

        /// <summary>
        /// Выбор курьеров магазина 
        /// </summary>
        /// <param name="shopId">ID магазина</param>
        /// <param name="includeTaxi"></param>
        /// <returns></returns>
        public Courier[] GetShopCouriers(int shopId, bool includeTaxi = true)
        {
            // 1. Инициализация

            try
            {
                // 2. Проверяем исходные данные
                if (!IsCreated)
                    return null;

                // 3. Выбираем курьеров маазина
                Courier[] shopCouriers = new Courier[couriers.Count];
                int count = 0;

                foreach(var courier in couriers.Values)
                {
                    if (courier.ShopId == shopId)
                    {
                        if (courier.Status == CourierStatus.Ready)
                        { shopCouriers[count++] = courier; }
                    }
                    else if (includeTaxi && courier.IsTaxi)
                    { shopCouriers[count++] = courier; }
                }

                if (count < shopCouriers.Length)
                { Array.Resize(ref shopCouriers, count); }

                // 4. Выход - Ok
                return shopCouriers;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Поиск первого попавшегося курьера
        /// заданного типа в заданном магазине
        /// </summary>
        /// <param name="shopId">ID магазина</param>
        /// <param name="vehicleId">ID способа доставки</param>
        /// <returns>Курьер или null</returns>
        public Courier FindFirstShopCourierByType(int shopId, int vehicleId)
        {
            try
            {
                // 2. Проверяем исходные данные
                if (!IsCreated)
                    return null;

                // 3. Цикл поиска курьера
                Courier[] crs = new Courier[couriers.Count];
                couriers.Values.CopyTo(crs, 0);

                for (int i = 0; i < crs.Length; i++)
                {
                    Courier courier = crs[i];
                    if (courier.ShopId == shopId && courier.VehicleID == vehicleId)
                    { return courier; }
                }

                return null;
            }
            catch
            { return null; }
        }

        /// <summary>
        /// Выборка различных сбособов доставки курьров
        /// </summary>
        /// <param name="couriers">Курьеры</param>
        /// <returns>Различные способы доставки или null</returns>
        public static int[] GetCourierVehicleTypes(Courier[] couriers)
        {
            // 1. Инициализация

            try
            {
                // 2. Проверяем исходные данные
                if (couriers == null || couriers.Length <= 0)
                    return null;

                // 3. Выбираем все типы в один массив
                int[] allTypes = new int[couriers.Length];
                int vehicleTypeCount = 0;

                for (int i = 0; i < couriers.Length; i++)
                {
                    allTypes[vehicleTypeCount++] = couriers[i].VehicleID;
                }

                // 4. Сортируем все типы
                Array.Sort(allTypes);

                // 5. Отбираем различные типы
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

                // 6. Выход - Ok
                return allTypes;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Преобразование courier_type в VehicleID
        /// </summary>
        /// <param name="courierType">Исходный courier_type</param>
        /// <returns>VehicleID</returns>
        private int VehicleIdFromCourierType(int courierType)
        {
            if (!IsCreated)
                return int.MinValue;
            int index = Array.BinarySearch(courierTypes, courierType);
            if (index < 0)
                return int.MinValue;

            return vehicleTypes[index];
        }

        /// <summary>
        /// Обновление курьеров
        /// </summary>
        /// <param name="updates">Новые данные</param>
        /// <param name="costThresholds">Пороги для средней стоимости доставки</param>
        /// <param name="shops">Коллекция магазинов</param>
        /// <returns></returns>
        public int Update(CouriersUpdates updates, AverageCostThresholds costThresholds, AllShops shops)
        {
            // 1. Иициализация
            int rc = 1;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (updates == null || updates.Updates == null || updates.Updates.Length <= 0)
                    return rc;
                if (costThresholds == null || !costThresholds.IsCreated)
                    return rc;
                if (shops == null)
                    return rc;

                // 3. Отрабатываем изменения данных курьеров
                rc = 3;

                foreach (var courierUpdates in updates.Updates)
                {
                    // 3.1 Находим vehicleID куьера
                    rc = 31;
                    int vehicleId = VehicleIdFromCourierType(courierUpdates.CourierType);
                    if (vehicleId == int.MinValue)
                    {
                        Logger.WriteToLog(18, MsessageSeverity.Warn, string.Format(Messages.MSG_018, courierUpdates.CourierId, courierUpdates.CourierType));
                        continue;
                    }

                    // 3.2 Находим Status курьера
                    rc = 32;
                    CourierStatus status = CourierStatusFromInputStatus(courierUpdates.Status);

                    Courier courier;
                    if (couriers.TryGetValue(courierUpdates.CourierId, out courier))
                    {
                        // 3.3 Обновление сущесвующего курьера
                        rc = 33;
                        courier.Status = status;
                        courier.Latitude = courierUpdates.Latitude;
                        courier.Longitude = courierUpdates.Longitude;
                        courier.WorkStart = courierUpdates.WorkStart.TimeOfDay;
                        courier.WorkEnd = courierUpdates.WorkEnd.TimeOfDay;
                    }
                    else
                    {
                        // 3.4 Добавляем нового курьера
                        rc = 34;
                        double threshold = costThresholds.GetThreshold(vehicleId, courierUpdates.ShopId);

                        int baseTypeIndex = Array.BinarySearch(baseKeys, vehicleId);
                        if (baseTypeIndex < 0)
                        {
                            Logger.WriteToLog(19, MsessageSeverity.Warn, string.Format(Messages.MSG_019, courierUpdates.CourierId, courierUpdates.CourierType, vehicleId));
                            continue;
                        }
                        CourierBase baseType = baseTypes[baseTypeIndex];
                        courier = new Courier(vehicleId, baseType);
                        courier.Status = status;
                        courier.AverageOrderCost = threshold;
                        courier.LastDeliveryStart = DateTime.Now;
                        courier.LastDeliveryEnd = DateTime.Now;
                        courier.LunchTimeStart = new TimeSpan(0);
                        courier.LunchTimeEnd = new TimeSpan(0);
                        courier.OrderCount = 0;
                        courier.ShopId = courierUpdates.ShopId;
                        courier.TotalCost = 0;
                        courier.TotalDeliveryTime = 0;
                        courier.WorkStart = courierUpdates.WorkStart.TimeOfDay;
                        courier.WorkEnd = courierUpdates.WorkEnd.TimeOfDay;
                        courier.Latitude = courierUpdates.Latitude;
                        courier.Longitude = courierUpdates.Longitude;
                        couriers.Add(courier.VehicleID, courier);
                    }

                    if (courierUpdates.ShopId != TAXI_SHOP_ID)
                        shops.SetShopUpdated(courierUpdates.ShopId, true);
                }

                // 4. Выход - Ok
                rc = 0;
                return rc;
            }
            catch (Exception ex)
            {
                Logger.WriteToLog(666, MsessageSeverity.Error, string.Format(Messages.MSG_666, $"{nameof(AllCouriersEx)}.{nameof(this.Update)}", (ex.InnerException == null ? ex.Message : ex.InnerException.Message)));
                return rc;
            }
        }

        /// <summary>
        /// Преобразование input_status --> courier_status
        /// </summary>
        /// <param name="inputStatus"></param>
        /// <returns>courier_status</returns>
        private static CourierStatus CourierStatusFromInputStatus(int inputStatus)
        {
            switch (inputStatus)
            {
                case 0:
                case 1:
                    return CourierStatus.Ready;
                case 2:
                case 3:
                    return CourierStatus.DeliversOrder;
                case 5:
                    return CourierStatus.WorkEnded;
                default:
                    return CourierStatus.Unknown;
            }
        }

        /// <summary>
        /// Выбор VehicleID для заданных DServiceID
        /// </summary>
        /// <param name="dserviceId">DServiceId</param>
        /// <returns>VehicleIDs или null</returns>
        public int[] GetVehiclesTypesForDService(int[] dserviceId)
        {
            // 1. Иициализация
            
            try
            {
                // 2. Проверяем исходные данные
                if (!IsCreated || baseTypes == null)
                    return null;
                if (dserviceId == null || dserviceId.Length <= 0)
                    return null;

                // 3. Выбираем VehicleID для заданных DServiceID
                int[] result = new int[baseTypes.Length];
                int count = 0;
                Array.Sort(dserviceId);

                for (int i = 0; i < baseTypes.Length; i++)
                {
                    CourierBase baseType = baseTypes[i];
                    if (Array.BinarySearch(dserviceId, baseType.DServiceType) >= 0)
                    { result[count++] = baseType.VehicleID; }
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
    }
}
