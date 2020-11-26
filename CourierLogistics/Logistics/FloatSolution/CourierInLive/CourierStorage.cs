
namespace CourierLogistics.Logistics.FloatSolution.CourierInLive
{
    using CourierLogistics.SourceData.Couriers;
    using System;
    using System.Linq;

    /// <summary>
    /// Курьеры для построения возможных отгрузок
    /// </summary>
    public class CourierStorage
    {
        /// <summary>
        /// Курьеры отсортированные по индексу типа
        /// и времени прибытия внутри одного типа
        /// </summary>
        public CourierEx[] Couriers { get; private set; }

        /// <summary>
        /// Количество различных типов
        /// </summary>
        public int TypeCount => (typeIndex == null ? 0 : typeIndex.Length);

        /// <summary>
        /// Флаг: true - экземпляр создан; false - экземпляр не создан
        /// </summary>
        public bool IsCreated { get; private set; }

        private bool[] isTaxiType;
        private int[] typeIndex;
        private int[] startTypeRangeIndex;
        private int[] endTypeRangeIndex;
        private int[] typeRangePointer;

        /// <summary>
        /// Создание экземпляра для работы с курьерами
        /// </summary>
        /// <param name="couriers">Курьеры</param>
        /// <returns></returns>
        public int Create(CourierEx[] couriers)
        {
            // 1. Инициализация
            int rc = 1;
            Couriers = null;
            IsCreated = false;
            isTaxiType = null;
            typeIndex = null;
            startTypeRangeIndex = null;
            endTypeRangeIndex = null;
            typeRangePointer = null;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (couriers == null || couriers.Length <= 0)
                    return rc;

                // 3. Сортируем курьеров по типу, а внутри типа по времени прибытия
                rc = 3;
                Array.Sort(couriers, CompareByCourierTypeIndexAndArrivalTime);
                Couriers = couriers;

                // 4. Выделяем в отсортированном массиве диапазоны курьров с одинаковым типом
                rc = 4;
                int typeCount = 0;
                int maxTypeCount = 100;

                startTypeRangeIndex = new int[maxTypeCount];
                endTypeRangeIndex = new int[maxTypeCount];
                typeIndex = new int[maxTypeCount];

                int currentTypeIndex = couriers[0].CourierTypeIndex;
                int startIndex = 0;
                int endIndex = 0;

                for (int i = 0; i < couriers.Length; i++)
                {
                    CourierEx courier = couriers[i];
                    if (courier.CourierTypeIndex == currentTypeIndex)
                    {
                        endIndex = i;
                    }
                    else
                    {
                        typeIndex[typeCount] = currentTypeIndex;
                        startTypeRangeIndex[typeCount] = startIndex;
                        endTypeRangeIndex[typeCount++] = endIndex;

                        currentTypeIndex = courier.CourierTypeIndex;
                        startIndex = i;
                        endIndex = i;
                    }
                }

                typeIndex[typeCount] = currentTypeIndex;
                startTypeRangeIndex[typeCount] = startIndex;
                endTypeRangeIndex[typeCount++] = endIndex;

                if (typeCount < typeIndex.Length)
                {
                    Array.Resize(ref typeIndex, typeCount);
                    Array.Resize(ref startTypeRangeIndex, typeCount);
                    Array.Resize(ref endTypeRangeIndex, typeCount);
                }

                // 5. Отмечаем такси
                rc = 5;
                isTaxiType = new bool[typeCount];

                for (int i = 0; i < startTypeRangeIndex.Length; i++)
                {
                    CourierEx courier = couriers[startTypeRangeIndex[i]];
                    isTaxiType[i] = courier.IsTaxi;
                }

                // 6. Создаём массив указателей диапазонов
                rc = 6;
                typeRangePointer = new int[typeCount];

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

        ///// <summary>
        ///// Создание экземпляра для работы с курьерами
        ///// </summary>
        ///// <param name="couriers">Курьеры</param>
        ///// <returns></returns>
        //public int Create(CourierEx[] couriers)
        //{
        //    // 1. Инициализация
        //    int rc = 1;
        //    Couriers = null;
        //    IsCreated = false;
        //    isTaxiType = null;
        //    typeIndex = null;
        //    startTypeRangeIndex = null;
        //    endTypeRangeIndex = null;
        //    typeRangePointer = null;

        //    try
        //    {
        //        // 2. Проверяем исходные данные
        //        rc = 2;
        //        if (couriers == null || couriers.Length <= 0)
        //            return rc;

        //        // 3. Сортируем курьеров по типу, а внутри типа по времени прибытия
        //        rc = 3;
        //        Array.Sort(couriers, CompareByCourierTypeIndexAndArrivalTime);
        //        Couriers = couriers;

        //        // 4. Выделяем в отсортированном массиве диапазоны курьров с одинаковым типом
        //        rc = 4;
        //        int size = couriers.Max(p => p.CourierTypeIndex) + 1;
        //        int typeCount = 0;

        //        startTypeRangeIndex = new int[size];
        //        endTypeRangeIndex = new int[size];
        //        typeIndex = new int[size];

        //        int currentTypeIndex = couriers[0].CourierTypeIndex;
        //        int startIndex = 0;
        //        int endIndex = 0;

        //        for (int i = 0; i < couriers.Length; i++)
        //        {
        //            CourierEx courier = couriers[i];
        //            if (courier.CourierTypeIndex == currentTypeIndex)
        //            {
        //                endIndex = i;
        //            }
        //            else
        //            {
        //                typeIndex[typeCount] = currentTypeIndex;
        //                startTypeRangeIndex[typeCount] = startIndex;
        //                endTypeRangeIndex[typeCount++] = endIndex;

        //                currentTypeIndex = courier.CourierTypeIndex;
        //                startIndex = i;
        //                endIndex = i;
        //            }
        //        }

        //        typeIndex[typeCount] = currentTypeIndex;
        //        startTypeRangeIndex[typeCount] = startIndex;
        //        endTypeRangeIndex[typeCount++] = endIndex;

        //        if (typeCount < typeIndex.Length)
        //        {
        //            Array.Resize(ref typeIndex, typeCount);
        //            Array.Resize(ref startTypeRangeIndex, typeCount);
        //            Array.Resize(ref endTypeRangeIndex, typeCount);
        //        }

        //        // 5. Отмечаем такси
        //        rc = 5;
        //        isTaxiType = new bool[typeCount];

        //        for (int i = 0; i < startTypeRangeIndex.Length; i++)
        //        {
        //            CourierEx courier = couriers[startTypeRangeIndex[i]];
        //            isTaxiType[i] = courier.IsTaxi;
        //        }

        //        // 6. Создаём массив указателей диапазонов
        //        rc = 6;
        //        typeRangePointer = new int[typeCount];

        //        // 7. Выход - Ok
        //        rc = 0;
        //        IsCreated = true;
        //        return rc;
        //    }
        //    catch
        //    {
        //        return rc;
        //    }
        //}

        /// <summary>
        /// Выбрать следующих курьеров - по одному каждого типа
        /// </summary>
        /// <returns>Выбранные курьеры или null</returns>
        public CourierEx[] GetNextCouriers()
        {
            // 1. Инициализация
            
            try
            {
                // 2. Проверяем исходные данные
                if (!IsCreated)
                    return null;

                // 3. Выбираем следующих доступных курьеров - по одному каждого типа
                int typeCount = TypeCount;
                CourierEx[] couriers = new CourierEx[typeCount];
                int count = 0;

                for (int i = 0; i < typeCount; i++)
                {
                    // 3.1 Пропускаем такси и курьров с "плохим" индексом
                    if (isTaxiType[i] || typeIndex[i] < 0)
                        continue;

                    // 3.2 Выбираем очередного курьера заданного типа
                    int pointer = startTypeRangeIndex[i] + typeRangePointer[i];
                    if (pointer <= endTypeRangeIndex[i])
                    {
                        couriers[count++] = Couriers[pointer];
                        //typeRangePointer[i]++;
                    }
                }

                if (count < couriers.Length)
                {
                    Array.Resize(ref couriers, count);
                }

                // 4. Выход
                return couriers;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Увеличение указателя диапазна
        /// заданного типа
        /// </summary>
        /// <param name="typeIndex"></param>
        public void IncrementPointer(int courierTypeIndex)
        {
            for (int i = 0; i < typeIndex.Length; i++)
            {
                if (typeIndex[i] == courierTypeIndex)
                {
                    typeRangePointer[i]++;
                    break;
                }
            }
        }

        /// <summary>
        /// Выборка такси
        /// </summary>
        /// <returns>Выбранные такси или null</returns>
        public CourierEx[] GetNextTaxi()
        {
            // 1. Инициализация
            
            try
            {
                // 2. Проверяем исходные данные
                if (!IsCreated)
                    return null;

                // 3. Выбираем следующих доступных курьеров - по одному каждого типа
                int typeCount = TypeCount;
                CourierEx[] taxi = new CourierEx[typeCount];
                int count = 0;

                for (int i = 0; i < typeCount; i++)
                {
                    // 3.1 Пропускаем не такси и курьров с "плохим" индексом
                    if (!isTaxiType[i] || typeIndex[i] < 0)
                        continue;

                    // 3.2 Выбираем очередного курьера заданного типа
                    int pointer = startTypeRangeIndex[i] + typeRangePointer[i];
                    if (pointer <= endTypeRangeIndex[i])
                    {
                        taxi[count++] = Couriers[pointer];
                        typeRangePointer[i]++;
                    }
                }

                if (count < taxi.Length)
                {
                    Array.Resize(ref taxi, count);
                }

                // 4. Выход
                return taxi;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Сравнение курьеров по индексу типа
        /// и времени прибытия внутри одного типа
        /// </summary>
        /// <param name="courier1">Курьер 1</param>
        /// <param name="courier2">Курьер 2</param>
        /// <returns>-1 - Курьер1 &lt; Курьер2; 0 - Курьер1 = Курьер2; 1 - Курьер1 &gt; Курьер2</returns>
        private static int CompareByCourierTypeIndexAndArrivalTime(CourierEx courier1, CourierEx courier2)
        {
            if (courier1.CourierTypeIndex < courier2.CourierTypeIndex)
                return -1;
            if (courier1.CourierTypeIndex > courier2.CourierTypeIndex)
                return 1;
            if (courier1.ArrivalTime < courier2.ArrivalTime)
                return -1;
            if (courier1.ArrivalTime > courier2.ArrivalTime)
                return 1;

            return 0;
        }
    }
}
