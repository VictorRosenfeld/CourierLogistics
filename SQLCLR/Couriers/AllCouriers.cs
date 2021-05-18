
namespace SQLCLR.Couriers
{
    using SQLCLR.Deliveries;
    using System;

    /// <summary>
    /// Курьеры и работа с ними
    /// </summary>
    public class AllCouriers
    {
        /// <summary>
        /// ID магазина приписки такси
        /// </summary>
        private const int TAXI_SHOP_ID = -1;

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

        #region Курьеры, отсортированные по CourierID

        /// <summary>
        /// Курьеры, отсортированные
        /// по возрастанию CourierID
        /// </summary>
        private Courier[] couriers;

        /// <summary>
        /// Отсортированные ключи курьеров (CourierID)
        /// </summary>
        private int[] courierKeys;

        /// <summary>
        /// Курьеры, отсортированные
        /// по возрастанию CourierID
        /// </summary>
        public Courier[] AvailableCouriers => couriers;

        /// <summary>
        /// Отсортированные ключи курьеров (CourierID)
        /// </summary>
        public int[] CourierKeys => courierKeys;

        #endregion Курьеры, отсортированные по CourierID

        #region Курьеры, отсортированные по ShopID, VehicleID

        /// <summary>
        /// Курьеры, отсортированные по ShopId, VehicleID
        /// </summary>
        private Courier[] shopCouriers;

        /// <summary>
        /// Отсортированные ключи магазинов (ShopId)
        /// </summary>
        private int[] shopKeys;

        /// <summary>
        /// Дипазоны курьров одного магазина в shopCouriers
        /// (startIndex = Point.X, endIndex = Point.Y)
        /// </summary>
        private Point[] courierRange;

        /// <summary>
        /// Курьеры, отсортированные по ShopId, VehicleID
        /// </summary>
        public Courier[] ShopCouriers => shopCouriers;

        /// <summary>
        /// Отсортированные ключи магазинов (ShopId)
        /// </summary>
        public int[] ShopKeys => shopKeys;

        /// <summary>
        /// Дипазоны курьров одного магазина в shopCouriers
        /// (startIndex = Point.X, endIndex = Point.Y)
        /// </summary>
        public Point[] CourierRange => courierRange;

        #endregion Курьеры, отсортированные по ShopID, VehicleID

        /// <summary>
        /// Количество курьеров
        /// </summary>
        public int Count => (couriers == null ? 0 : couriers.Length);

        /// <summary>
        /// Флаг: true - экземпляр создан; false - экземпляр не создан
        /// </summary>
        public bool IsCreated { get; set; }

        /// <summary>
        /// Создание экземпляра
        /// </summary>
        /// <param name="courierTypeRecords">Записи таблицы lsvCourierTypes</param>
        /// <param name="courierRecords">Записи таблицы lsvCouriers</param>
        /// <returns>0 - экземпляр создан; экземпляр не создан</returns>
        public int Create(CourierTypeRecord[] courierTypeRecords, CourierRecord[] courierRecords)
        {
            // 1. Инициализация
            int rc = 1;
            IsCreated = false;
            baseTypes = null;
            baseKeys = null;

            couriers = null;
            courierKeys = null;

            shopCouriers = null;
            shopKeys = null;
            courierRange = null;

            try
            {
                // 2. Проверяем иcходные данные
                rc = 2;
                if (courierTypeRecords == null || courierTypeRecords.Length <= 0)
                    return rc;
                if (courierRecords == null || courierRecords.Length <= 0)
                    return rc;

                // 3. Выбираем всех делегатов расчета времени и стоимости отгрузки
                rc = 3;
                GetTimeAndCostDelegate[] calculators = TimeAndCostCalculator.SelectCalculators();
                if (calculators == null || calculators.Length <= 0)
                    return rc;
                string[] calculatorKeys = new string[calculators.Length];

                for (int i = 0; i < calculators.Length; i++)
                {
                    calculatorKeys[i] = calculators[i].Method.Name.ToLower();
                }

                Array.Sort(calculatorKeys, calculators);

                // 4. Создаём базовые типы курьеров и такси 
                rc = 4;
                baseTypes = new CourierBase[courierTypeRecords.Length];
                baseKeys = new int[courierTypeRecords.Length];

                for (int i = 0; i < courierTypeRecords.Length; i++)
                {
                    CourierTypeRecord record = courierTypeRecords[i];
                    baseTypes[i] = new CourierBase(record);
                    baseKeys[i] = record.VehicleID;

                    int index = Array.BinarySearch(calculatorKeys, TimeAndCostCalculator.GetMethodName(record.CalcMethod).ToLower());
                    if (index >= 0)
                        baseTypes[i].SetCalculator(calculators[index]);
                }

                // 5. Создаём курьеров и такси
                rc = 5;
                couriers = new Courier[courierRecords.Length];
                courierKeys = new int[courierRecords.Length];
                int count = 0;

                for (int i = 0; i < courierRecords.Length; i++)
                {
                    CourierRecord record = courierRecords[i];
                    int index = Array.BinarySearch(baseKeys, record.VehicleId);
                    if (index >= 0)
                    {
                        Courier courier = new Courier(record.CourierId, baseTypes[i]);
                        courier.Status = record.Status;
                        courier.AverageOrderCost = record.AverageOrderCost;
                        courier.LastDeliveryStart = record.LastDeliveryStart;
                        courier.LastDeliveryEnd = record.LastDeliveryEnd;
                        courier.LunchTimeStart = record.LunchTimeStart;
                        courier.LunchTimeEnd = record.LunchTimeEnd;
                        courier.OrderCount = record.OrderCount;
                        courier.ShopId = record.ShopId;
                        courier.TotalCost = record.TotalCost;
                        courier.TotalDeliveryTime = record.TotalDeliveryTime;
                        courier.WorkStart = record.WorkStart;
                        courier.WorkEnd = record.WorkEnd;
                        if (courier.IsTaxi)
                            courier.ShopId = TAXI_SHOP_ID;
                        courierKeys[count] = record.CourierId;
                        couriers[count++] = courier;
                    }
                }

                if (count <= 0)
                    return rc;

                if (count < couriers.Length)
                {
                    Array.Resize(ref couriers, count);
                    Array.Resize(ref courierKeys, count);
                }

                // 6. Строим индекс для быстрой выборки всех курьеров магазина
                rc = 6;
                shopCouriers = (Courier[])couriers.Clone();
                Array.Sort(shopCouriers, CompareCourierByShop);

                shopKeys = new int[couriers.Length];
                courierRange = new Point[couriers.Length];
                count = 0;
                int currentShopId = shopCouriers[0].ShopId;
                int startIndex = 0;
                int endIndex = 0;

                for (int i = 1; i < shopCouriers.Length; i++)
                {
                    if (shopCouriers[i].ShopId == currentShopId)
                    {
                        endIndex = i;
                    }
                    else
                    {
                        shopKeys[count] = currentShopId;
                        courierRange[count].X = startIndex;
                        courierRange[count++].Y = endIndex;

                        currentShopId = shopCouriers[i].ShopId;
                        startIndex = i;
                        endIndex = i;
                    }
                }

                shopKeys[count] = currentShopId;
                courierRange[count].X = startIndex;
                courierRange[count++].Y = endIndex;

                if (count < shopKeys.Length)
                {
                    Array.Resize(ref shopKeys, count);
                    Array.Resize(ref courierRange, count);
                }

                // 7. Выход - Ok
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

                // 3. Находим диапазон курьеров магазина
                int index = Array.BinarySearch(shopKeys, shopId);
                Point shopRange = new Point();
                Point taxiRange = new Point();
                int count1 = 0;
                int count2 = 0;

                if (index >= 0)
                {
                    shopRange = courierRange[index];
                    count1 = shopRange.Y - shopRange.X + 1;
                }

                // 4. Находим диапазон такси
                if (includeTaxi)
                {
                    index = Array.BinarySearch(shopKeys, TAXI_SHOP_ID);
                    if (index >= 0)
                    {
                        taxiRange = courierRange[index];
                        count2 = taxiRange.Y - taxiRange.X + 1;
                    }
                }

                if (count1 + count2 <= 0)
                    return new Courier[0];

                // 5. Объединяем найденных курьеров и такси
                Courier[] result = new Courier[count1 + count2];
                if (count1 > 0)
                    Array.Copy(shopCouriers, shopRange.X, result, 0, count1);
                if (count2 > 0)
                    Array.Copy(shopCouriers, taxiRange.X, result, count1, count2);

                // 6. Выход - Ok
                return result;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Выбор курьера по ID
        /// </summary>
        /// <param name="courierId">ID курьера</param>
        /// <returns>Курьер или null</returns>
        public Courier GetCourier(int courierId)
        {
            // 1. Инициализация

            try
            {
                // 2. Проверяем исходные данные
                if (!IsCreated)
                    return null;

                // 3. Находим диапазон курьеров магазина
                int index = Array.BinarySearch(courierKeys, courierId);
                if (index < 0)
                    return null;

                // 4. Выход - Ok
                return couriers[index];
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Построение эталонного курьера
        /// </summary>
        /// <param name="vehicleId">ID cпособа доставки</param>
        /// <returns>Курьер или null</returns>
        public Courier CreateReferenceCourier(int vehicleId)
        {
            // 1. Инициализация

            try
            {
                // 2. Проверяем исходные данные
                if (!IsCreated)
                    return null;

                // 3. Находим базовый тип
                int index = Array.BinarySearch(baseKeys, vehicleId);
                if (index <= 0)
                    return null;

                // 4. Строим эталонного курьера заданного типа
                Courier courier = new Courier(vehicleId, baseTypes[index]);
                courier.Status = CourierStatus.Ready;
                courier.WorkStart = TimeSpan.Zero;
                courier.WorkEnd = TimeSpan.FromHours(24);
                courier.ShopId = int.MaxValue;
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

        /// <summary>
        /// Сравнение двух курьеров по принадлежности
        /// одному магазину и способу доставки
        /// </summary>
        /// <param name="courier1">Курьер 1</param>
        /// <param name="courier2">Курьер 2</param>
        /// <returns>- 1  - Курьер1 меньше Курьер2; 0 - Курьер1 = Курьер2; 1 - Курьер1 больше Курьер2</returns>
        private static int CompareCourierByShop(Courier courier1, Courier courier2)
        {
            if (courier1.ShopId < courier2.ShopId)
                return -1;
            if (courier1.ShopId > courier2.ShopId)
                return 1;
            if (courier1.VehicleID < courier2.VehicleID)
                return -1;
            if (courier1.VehicleID > courier2.VehicleID)
                return 1;
            return 0;
        }

        /// <summary>
        /// Поиск первого попавшегося курьера
        /// заданного типа
        /// </summary>
        /// <param name="vehicleId">ID способа доставки</param>
        /// <returns>Курьер или null</returns>
        public Courier FindFirstByType(int vehicleId)
        {
            try
            {
                // 2. Проверяем исходные данные
                if (!IsCreated)
                    return null;

                // 3. Находим первого курьера заданного типа
                foreach (Courier courier in couriers)
                {
                    if (courier.VehicleID == vehicleId)
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

                // 3. Находим диапазон курьеров магазина
                int index = Array.BinarySearch(shopKeys, shopId);
                if (index < 0)
                    return null;

                // 4. Находим курьера заданного типа
                int startIndex = courierRange[index].X;
                int endIndex = courierRange[index].Y;

                for (int i = startIndex; i <= endIndex; i++)
                {
                    if (shopCouriers[i].VehicleID == vehicleId)
                        return shopCouriers[i];
                }

                // 5. Курьер заданного типа не найден
                return null;
            }
            catch
            {
                return null;
            }
        }

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
    }
}
