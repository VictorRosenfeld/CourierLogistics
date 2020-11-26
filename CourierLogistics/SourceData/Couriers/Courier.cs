
namespace CourierLogistics.SourceData.Couriers
{
    using System;

    /// <summary>
    /// Курьер
    /// </summary>
    public class Courier
    {
        /// <summary>
        /// Минимальное время оплаты, час
        /// </summary>
        public const double MIN_WORK_TIME = 4;

        /// <summary>
        /// Передельное время доставки, мин
        /// </summary>
        public const double DELIVERY_LIMIT = 120;

        /// <summary>
        /// ID курьера
        /// </summary>
        public int Id { get; private set; }

        /// <summary>
        /// Тип курьера
        /// </summary>
        public ICourierType CourierType { get; private set; }

        /// <summary>
        /// Статус курьера
        /// </summary>
        public CourierStatus Status  { get; set; }

        /// <summary>
        /// Начало работы
        /// </summary>
        public TimeSpan WorkStart { get; set; }

        /// <summary>
        /// Конец работы
        /// </summary>
        public TimeSpan WorkEnd { get; set; }

        /// <summary>
        /// Начало обеда
        /// </summary>
        public TimeSpan LunchTimeStart { get; set; }

        /// <summary>
        /// Конец обеда
        /// </summary>
        public TimeSpan LunchTimeEnd { get; set; }

        /// <summary>
        /// Время начала отгрузки (Status = DeliversOrder)
        /// </summary>
        public DateTime LastDeliveryStart { get; set; }

        /// <summary>
        /// Время возвращения (Status = DeliversOrder)
        /// </summary>
        public DateTime LastDeliveryEnd { get; set; }

        /// <summary>
        /// Количество выполненных заказов
        /// </summary>
        public int OrderCount { get; set; }

        /// <summary>
        /// Время, потраченное на выполнение всех заказов за день, час
        /// </summary>
        public double TotalDeliveryTime { get; set; }

        /// <summary>
        /// Общая стоимость курьера за день
        /// </summary>
        public double TotalCost { get; set; }

        /// <summary>
        /// Средняя цена стоимости заказа
        /// </summary>
        public double AverageOrderCost { get; set; }

        /// <summary>
        /// Индекс курьера
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Параметрический конструктор класса Courier
        /// </summary>
        /// <param name="id">Id курьера</param>
        /// <param name="courierType">Тип курьера</param>
        public Courier(int id, ICourierType courierType)
        {
            Id = id;
            CourierType = courierType;
        }

        ///// <summary>
        ///// Проверка возможности доставки одного заказа
        ///// и подсчет резерва времени доставки и её стоимости
        ///// </summary>
        ///// <param name="currentModelTime">Время, в которое происходит расчет (время модели).
        ///// (Это время должно быть после подготовки заказа для доставки !)
        ///// </param>
        ///// <param name="distance">Расстояние до точки доставки, км</param>
        ///// <param name="deliveryTimeLimit">Предельное время доставки</param>
        ///// <param name="weight">Вес заказа</param>
        ///// <param name="reserveTime">Расчитанный резерв времени на доставку</param>
        ///// <param name="cost">Расчитанная стоимость, руб</param>
        ///// <returns>0 - заказ может быть доставлен в срок; иначе - заказ в срок не может быть доставлен</returns>
        //public int DeliveryCheck(DateTime currentModelTime, double distance, DateTime deliveryTimeLimit, double weight, out TimeSpan reserveTime, out double cost)
        //{
        //    // 1. Инициализация
        //    int rc = 1;
        //    reserveTime = TimeSpan.Zero;
        //    cost = 0;

        //    try
        //    {
        //        // 2. Проверяем статус
        //        rc = 2;
        //        if (Status == CourierStatus.Unknown)
        //            return rc;

        //        // 3. Запрашиваем время и стоимость доставки
        //        rc = 3;
        //        double deliveryTime;
        //        double executionTime;

        //        int rc1 = CourierType.GetTimeAndCost(distance, weight, out deliveryTime, out executionTime, out cost);
        //        if (rc1 != 0)
        //            return rc = 100 * rc + rc1;

        //        // 4. Если невозможно доставить в срок
        //        rc = 4;
        //        //DateTime currentModelTime = DateTime.Now;
        //        DateTime firstPossibleDeliveryTime = currentModelTime.AddMinutes(deliveryTime);
        //        if (firstPossibleDeliveryTime > deliveryTimeLimit)
        //            return rc;

        //        // 5. Расчитываем резерв по времени
        //        rc = 5;
        //        reserveTime = (deliveryTimeLimit - firstPossibleDeliveryTime);

        //        // 6. Расчитываем возможный интервал начала доставки (ИНД)
        //        rc = 6;
        //        DateTime intervalStart = currentModelTime;
        //        DateTime intervalEnd = currentModelTime.Add(reserveTime);

        //        // 7. ИНД = ИНД ∩ Рабочее_Время
        //        rc = 7;
        //        DateTime workStart = currentModelTime.Add(WorkStart);
        //        DateTime workEnd = currentModelTime.Add(WorkEnd);
        //        if (!Helper.TimeIntervalsIntersection(intervalStart, intervalEnd, workStart, workEnd, out intervalStart, out intervalEnd))
        //            return rc;

        //        // 8. Если ИНД лежит целиком внутри обеда
        //        rc = 8;
        //        DateTime lunchStart = currentModelTime.Add(LunchTimeStart);
        //        DateTime lunchEnd = currentModelTime.Add(LunchTimeEnd);
        //        if (intervalStart >= lunchStart && intervalEnd <= lunchEnd)
        //            return rc;

        //        // 9. Строим интервал доставки с учетом обеда
        //        rc = 9;
        //        // Завершение доставки до начала обеда
        //        if (firstPossibleDeliveryTime <= lunchStart)
        //        {
        //            TimeSpan interval = lunchStart - firstPossibleDeliveryTime;
        //            if (interval < reserveTime)
        //            {
        //                intervalEnd = currentModelTime.Add(interval);
        //                reserveTime = interval;
        //            }
        //        }
        //        // Начало доставки после конца обеда
        //        else if (intervalEnd >= lunchEnd)
        //        {
        //            if (intervalStart < lunchEnd)
        //            {
        //                intervalStart = lunchEnd;
        //                reserveTime = intervalEnd - intervalStart;
        //            }
        //        }
        //        else
        //        {
        //            return rc;
        //        }

        //        // 10. Если сейчас осуществляется доставка
        //        rc = 10;
        //        if (Status == CourierStatus.DeliversOrder)
        //        {
        //            if (LastDeliveryEnd > intervalEnd)
        //                return rc;
        //            if (LastDeliveryEnd > intervalStart)
        //            {
        //                intervalStart = LastDeliveryEnd;
        //                reserveTime = intervalEnd - intervalStart;
        //            }
        //        }

        //        // 11. Все проверки пройдены
        //        rc = 0;
        //        return rc;
        //    }
        //    catch
        //    {
        //        return rc;
        //    }
        //}

        ///// <summary>
        ///// Проверка возможности доставки нескольких заказов
        ///// и подсчет резерва времени доставки и её стоимости
        ///// </summary>
        ///// <param name="currentModelTime">Время, в которое происходит расчет (время модели).
        ///// (Это время должно быть после времени подготовки всех заказов для доставки !)
        ///// </param>
        ///// <param name="distance">Расстояние до точки доставки, км</param>
        ///// <param name="deliveryTimeLimit">Предельное время доставки</param>
        ///// <param name="weight">Вес заказа</param>
        ///// <param name="reserveTime">Расчитанный резерв времени на доставку</param>
        ///// <param name="cost">Расчитанная стоимость, руб</param>
        ///// <returns>0 - заказ может быть доставлен в срок; иначе - заказ в срок не может быть доставлен</returns>
        //public int DeliveryCheck(DateTime currentModelTime, double[] distance, DateTime[] deliveryTimeLimit, double weight, out TimeSpan reserveTime, out double cost)
        //{
        //    // 1. Инициализация
        //    int rc = 1;
        //    reserveTime = TimeSpan.Zero;
        //    cost = 0;

        //    try
        //    {
        //        // 2. Проверяем исходные данные
        //        rc = 2;
        //        if (distance == null || distance.Length < 3)
        //            return rc;
        //        if (deliveryTimeLimit == null || deliveryTimeLimit.Length != distance.Length)
        //            return rc;
        //        if (weight <= 0)
        //            return rc;

        //        // 3. Проверяем статус
        //        rc = 3;
        //        if (Status == CourierStatus.Unknown)
        //            return rc;

        //        // 4. Запрашиваем время и стоимость доставки
        //        rc = 4;
        //        double deliveryTime;
        //        double executionTime;
        //        double[] nodeDeliveryTime;

        //        int rc1 = CourierType.GetTimeAndCost(distance, weight, out nodeDeliveryTime, out deliveryTime, out executionTime, out cost);
        //        if (rc1 != 0)
        //            return rc = 100 * rc + rc1;

        //        // 5. Если невозможно доставить в срок
        //        rc = 5;
        //        //DateTime currentModelTime = DateTime.Now;
        //        DateTime firstPossibleDeliveryTime = currentModelTime.AddMinutes(deliveryTime);
        //        if (firstPossibleDeliveryTime > deliveryTimeLimit.Max())
        //            return rc;

        //        // 6. Расчитываем резерв по времени
        //        rc = 6;
        //        reserveTime = TimeSpan.MaxValue;

        //        for (int i = 1; i < distance.Length - 1; i++)
        //        {
        //            DateTime dTime = currentModelTime.AddMinutes(nodeDeliveryTime[i]);
        //            if (dTime > deliveryTimeLimit[i])
        //                return rc;
        //            TimeSpan ts = deliveryTimeLimit[i] - dTime;
        //            if (ts < reserveTime) reserveTime = ts;
        //        }

        //        // 7. Расчитываем возможный интервал начала доставки (ИНД)
        //        rc = 7;
        //        DateTime intervalStart = currentModelTime;
        //        DateTime intervalEnd = currentModelTime.Add(reserveTime);

        //        // 8. ИНД = ИНД ∩ Рабочее_Время
        //        rc = 8;
        //        DateTime workStart = currentModelTime.Add(WorkStart);
        //        DateTime workEnd = currentModelTime.Add(WorkEnd);
        //        if (!Helper.TimeIntervalsIntersection(intervalStart, intervalEnd, workStart, workEnd, out intervalStart, out intervalEnd))
        //            return rc;

        //        // 9. Если ИНД лежит целиком внутри обеда
        //        rc = 9;
        //        DateTime lunchStart = currentModelTime.Add(LunchTimeStart);
        //        DateTime lunchEnd = currentModelTime.Add(LunchTimeEnd);
        //        if (intervalStart >= lunchStart && intervalEnd <= lunchEnd)
        //            return rc;

        //        // 10. Строим интервал доставки с учетом обеда
        //        rc = 10;
        //        // Завершение доставки до начала обеда
        //        if (firstPossibleDeliveryTime <= lunchStart)
        //        {
        //            TimeSpan interval = lunchStart - firstPossibleDeliveryTime;
        //            if (interval < reserveTime)
        //            {
        //                intervalEnd = currentModelTime.Add(interval);
        //                reserveTime = interval;
        //            }
        //        }
        //        // Начало доставки после конца обеда
        //        else if (intervalEnd >= lunchEnd)
        //        {
        //            if (intervalStart < lunchEnd)
        //            {
        //                intervalStart = lunchEnd;
        //                reserveTime = intervalEnd - intervalStart;
        //            }
        //        }
        //        else
        //        {
        //            return rc;
        //        }

        //        // 11. Если сейчас осуществляется доставка
        //        rc = 11;
        //        if (Status == CourierStatus.DeliversOrder)
        //        {
        //            if (LastDeliveryEnd > intervalEnd)
        //                return rc;
        //            if (LastDeliveryEnd > intervalStart)
        //            {
        //                intervalStart = LastDeliveryEnd;
        //                reserveTime = intervalEnd - intervalStart;
        //            }
        //        }

        //        // 12. Все проверки пройдены
        //        rc = 0;
        //        return rc;
        //    }
        //    catch
        //    {
        //        return rc;
        //    }
        //}

        /// <summary>
        /// Проверка возможности доставки одного заказа
        /// и подсчет резерва времени доставки и её стоимости
        /// </summary>
        /// <param name="currentModelTime">Время, в которое происходит расчет (время модели).
        /// (Это время должно быть после подготовки заказа для доставки !)
        /// </param>
        /// <param name="distance">Расстояние до точки доставки, км</param>
        /// <param name="deliveryTimeLimit">Предельное время доставки</param>
        /// <param name="weight">Вес заказа</param>
        /// <param name="deliveryInfo">Результаты расчетов</param>
        /// <returns>0 - заказ может быть доставлен в срок; иначе - заказ в срок не может быть доставлен</returns>
        public int DeliveryCheck(DateTime currentModelTime, double distance, DateTime deliveryTimeLimit, double weight, out CourierDeliveryInfo deliveryInfo)
        {
            // 1. Инициализация
            int rc = 1;
            deliveryInfo = new CourierDeliveryInfo(this, currentModelTime, 1, weight);
            //reserveTime = TimeSpan.Zero;
            //cost = 0;

            try
            {
                // 2. Проверяем статус
                rc = 2;
                if (Status == CourierStatus.Unknown)
                    return rc;

                // 3. Запрашиваем время и стоимость доставки
                rc = 3;
                double deliveryTime;
                double executionTime;
                double cost;

                int rc1 = CourierType.GetTimeAndCost(distance, weight, out deliveryTime, out executionTime, out cost);
                if (rc1 != 0)
                    return rc = 100 * rc + rc1;

                // 4. Если невозможно доставить в срок
                rc = 4;
                //DateTime currentModelTime = DateTime.Now;
                DateTime firstPossibleDeliveryTime = currentModelTime.AddMinutes(deliveryTime);
                if (firstPossibleDeliveryTime > deliveryTimeLimit)
                    return rc;

                // 5. Расчитываем резерв по времени
                rc = 5;
                TimeSpan reserveTime = (deliveryTimeLimit - firstPossibleDeliveryTime);

                // 6. Расчитываем возможный интервал начала доставки (ИНД)
                rc = 6;
                DateTime intervalStart = currentModelTime;
                DateTime intervalEnd = currentModelTime.Add(reserveTime);

                // 7. ИНД = ИНД ∩ Рабочее_Время
                rc = 7;
                DateTime workStart = currentModelTime.Date.Add(WorkStart);
                DateTime workEnd = currentModelTime.Date.Add(WorkEnd);
                if (!Helper.TimeIntervalsIntersection(intervalStart, intervalEnd, workStart, workEnd, out intervalStart, out intervalEnd))
                    return rc;

                // 8. Если ИНД лежит целиком внутри обеда
                rc = 8;
                DateTime lunchStart = currentModelTime.Date.Add(LunchTimeStart);
                DateTime lunchEnd = currentModelTime.Date.Add(LunchTimeEnd);
                if (intervalStart >= lunchStart && intervalEnd <= lunchEnd)
                    return rc;

                // 9. Строим интервал доставки с учетом обеда
                rc = 9;
                // Завершение доставки до начала обеда
                if (firstPossibleDeliveryTime <= lunchStart)
                {
                    TimeSpan interval = lunchStart - firstPossibleDeliveryTime;
                    if (interval < reserveTime)
                    {
                        intervalEnd = currentModelTime.Add(interval);
                        reserveTime = interval;
                    }
                }
                // Начало доставки после конца обеда
                else if (intervalEnd >= lunchEnd)
                {
                    if (intervalStart < lunchEnd)
                    {
                        intervalStart = lunchEnd;
                        reserveTime = intervalEnd - intervalStart;
                    }
                }
                else
                {
                    return rc;
                }

                // 10. Если сейчас осуществляется доставка
                rc = 10;
                if (Status == CourierStatus.DeliversOrder)
                {
                    if (LastDeliveryEnd > intervalEnd)
                        return rc;
                    if (LastDeliveryEnd > intervalStart)
                    {
                        intervalStart = LastDeliveryEnd;
                        reserveTime = intervalEnd - intervalStart;
                    }
                }

                deliveryInfo.Cost = cost;
                deliveryInfo.DeliveryTime = deliveryTime;
                deliveryInfo.ExecutionTime = executionTime;
                deliveryInfo.ReserveTime = reserveTime;
                deliveryInfo.StartDeliveryInterval = intervalStart;
                deliveryInfo.EndDeliveryInterval = intervalEnd;
                deliveryInfo.NodeDeliveryTime = new double[] { 0, deliveryTime };
                deliveryInfo.NodeDistance = new double[] { 0, distance };

                // 11. Все проверки пройдены
                rc = 0;
                return rc;
            }
            catch
            {
                return rc;
            }
        }

        ///// <summary>
        ///// Проверка возможности доставки нескольких заказов
        ///// и подсчет резерва времени доставки и её стоимости
        ///// </summary>
        ///// <param name="currentModelTime">Время, в которое происходит расчет (время модели).
        ///// (Это время должно быть после времени подготовки всех заказов для доставки !)
        ///// </param>
        ///// <param name="distance">Расстояние до точки доставки, км</param>
        ///// <param name="deliveryTimeLimit">Предельное время доставки</param>
        ///// <param name="weight">Вес заказа</param>
        ///// <param name="deliveryInfo">Результаты расчетов</param>
        ///// <returns>0 - заказ может быть доставлен в срок; иначе - заказ в срок не может быть доставлен</returns>
        //public int DeliveryCheck(DateTime currentModelTime, double[] distance, DateTime[] deliveryTimeLimit, double weight, out CourierDeliveryInfo deliveryInfo)
        //{
        //    // 1. Инициализация
        //    int rc = 1;
        //    deliveryInfo = null;

        //    try
        //    {
        //        // 2. Проверяем исходные данные
        //        rc = 2;
        //        if (distance == null || distance.Length < 3)
        //            return rc;

        //        deliveryInfo = new CourierDeliveryInfo(this, currentModelTime, distance.Length - 2, weight);

        //        if (deliveryTimeLimit == null || deliveryTimeLimit.Length != distance.Length)
        //            return rc;
        //        if (weight <= 0)
        //            return rc;


        //        // 3. Проверяем статус
        //        rc = 3;
        //        if (Status == CourierStatus.Unknown)
        //            return rc;

        //        // 4. Запрашиваем время и стоимость доставки
        //        rc = 4;
        //        double deliveryTime;
        //        double executionTime;
        //        double[] nodeDeliveryTime;
        //        double cost;

        //        int rc1 = CourierType.GetTimeAndCost(distance, weight, out nodeDeliveryTime, out deliveryTime, out executionTime, out cost);
        //        if (rc1 != 0)
        //            return rc = 100 * rc + rc1;

        //        // 5. Если невозможно доставить в срок
        //        rc = 5;
        //        //DateTime currentModelTime = DateTime.Now;
        //        DateTime firstPossibleDeliveryTime = currentModelTime.AddMinutes(deliveryTime);
        //        //if (firstPossibleDeliveryTime > deliveryTimeLimit.Max())
        //        //    return rc;

        //        // 6. Расчитываем резерв по времени
        //        rc = 6;
        //        TimeSpan reserveTime = TimeSpan.MaxValue;

        //        for (int i = 1; i < distance.Length - 1; i++)
        //        {
        //            DateTime dTime = currentModelTime.AddMinutes(nodeDeliveryTime[i]);
        //            if (dTime > deliveryTimeLimit[i])
        //                return rc = 1000 * rc + i;
        //            TimeSpan ts = deliveryTimeLimit[i] - dTime;
        //            if (ts < reserveTime) reserveTime = ts;
        //        }

        //        // 7. Расчитываем возможный интервал начала доставки (ИНД)
        //        rc = 7;
        //        DateTime intervalStart = currentModelTime;
        //        DateTime intervalEnd = currentModelTime.Add(reserveTime);

        //        // 8. ИНД = ИНД ∩ Рабочее_Время
        //        rc = 8;
        //        if (CourierType.VechicleType != CourierVehicleType.GettTaxi && CourierType.VechicleType != CourierVehicleType.YandexTaxi)
        //        {
        //            DateTime workStart = currentModelTime.Add(WorkStart);
        //            DateTime workEnd = currentModelTime.Add(WorkEnd);
        //            if (!Helper.TimeIntervalsIntersection(intervalStart, intervalEnd, workStart, workEnd, out intervalStart, out intervalEnd))
        //                return rc;

        //            // 9. Если ИНД лежит целиком внутри обеда
        //            rc = 9;
        //            DateTime lunchStart = currentModelTime.Add(LunchTimeStart);
        //            DateTime lunchEnd = currentModelTime.Add(LunchTimeEnd);
        //            if (intervalStart >= lunchStart && intervalEnd <= lunchEnd)
        //                return rc;

        //            // 10. Строим интервал доставки с учетом обеда
        //            rc = 10;
        //            // Завершение доставки до начала обеда
        //            if (firstPossibleDeliveryTime <= lunchStart)
        //            {
        //                TimeSpan interval = lunchStart - firstPossibleDeliveryTime;
        //                if (interval < reserveTime)
        //                {
        //                    intervalEnd = currentModelTime.Add(interval);
        //                    reserveTime = interval;
        //                }
        //            }
        //            // Начало доставки после конца обеда
        //            else if (intervalEnd >= lunchEnd)
        //            {
        //                if (intervalStart < lunchEnd)
        //                {
        //                    intervalStart = lunchEnd;
        //                    reserveTime = intervalEnd - intervalStart;
        //                }
        //            }
        //            else
        //            {
        //                return rc;
        //            }

        //            // 11. Если сейчас осуществляется доставка
        //            rc = 11;
        //            if (Status == CourierStatus.DeliversOrder)
        //            {
        //                if (LastDeliveryEnd > intervalEnd)
        //                    return rc;
        //                if (LastDeliveryEnd > intervalStart)
        //                {
        //                    intervalStart = LastDeliveryEnd;
        //                    reserveTime = intervalEnd - intervalStart;
        //                }
        //            }
        //        }

        //        deliveryInfo.Cost = cost;
        //        deliveryInfo.DeliveryTime = deliveryTime;
        //        deliveryInfo.ExecutionTime = executionTime;
        //        deliveryInfo.ReserveTime = reserveTime;
        //        deliveryInfo.StartDeliveryInterval = intervalStart;
        //        deliveryInfo.EndDeliveryInterval = intervalEnd;

        //        // 12. Все проверки пройдены
        //        rc = 0;
        //        return rc;
        //    }
        //    catch
        //    {
        //        return rc;
        //    }
        //}

        /// <summary>
        /// Проверка возможности доставки нескольких заказов
        /// и подсчет резерва времени доставки и её стоимости
        /// </summary>
        /// <param name="currentModelTime">Время, в которое происходит расчет (время модели).
        /// (Это время должно быть после времени подготовки всех заказов для доставки !)
        /// </param>
        /// <param name="distance">Расстояние до точки доставки, км</param>
        /// <param name="deliveryTimeLimit">Предельное время доставки</param>
        /// <param name="weight">Вес заказа</param>
        /// <param name="deliveryInfo">Результаты расчетов</param>
        /// <param name="nodeDeliveryTime">Время доставки до точек вручения</param>
        /// <returns>0 - заказ может быть доставлен в срок; иначе - заказ в срок не может быть доставлен</returns>
        public int DeliveryCheck(DateTime currentModelTime, double[] distance, DateTime[] deliveryTimeLimit, double weight, out CourierDeliveryInfo deliveryInfo, out double[] nodeDeliveryTime)
        {
            // 1. Инициализация
            int rc = 1;
            deliveryInfo = null;
            nodeDeliveryTime = null;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (distance == null || distance.Length < 3)
                    return rc;

                deliveryInfo = new CourierDeliveryInfo(this, currentModelTime, distance.Length - 2, weight);

                if (deliveryTimeLimit == null || deliveryTimeLimit.Length != distance.Length)
                    return rc;
                if (weight <= 0)
                    return rc;

                // 3. Проверяем статус
                rc = 3;
                if (Status == CourierStatus.Unknown)
                    return rc;

                // 4. Запрашиваем время и стоимость доставки
                rc = 4;
                double deliveryTime;
                double executionTime;
                double cost;

                int rc1 = CourierType.GetTimeAndCost(distance, weight, out nodeDeliveryTime, out deliveryTime, out executionTime, out cost);
                if (rc1 != 0)
                    return rc = 100 * rc + rc1;

                // 5. Если невозможно доставить в срок
                rc = 5;
                //DateTime currentModelTime = DateTime.Now;
                DateTime firstPossibleDeliveryTime = currentModelTime.AddMinutes(deliveryTime);
                //if (firstPossibleDeliveryTime > deliveryTimeLimit.Max())
                //    return rc;

                // 6. Расчитываем резерв по времени
                rc = 6;
                TimeSpan reserveTime = TimeSpan.MaxValue;

                for (int i = 1; i < distance.Length - 1; i++)
                {
                    DateTime dTime = currentModelTime.AddMinutes(nodeDeliveryTime[i]);
                    if (dTime > deliveryTimeLimit[i])
                        return rc = 1000 * rc + i;
                    TimeSpan ts = deliveryTimeLimit[i] - dTime;
                    if (ts < reserveTime) reserveTime = ts;
                }

                // 7. Расчитываем возможный интервал начала доставки (ИНД)
                rc = 7;
                DateTime intervalStart = currentModelTime;
                DateTime intervalEnd = currentModelTime.Add(reserveTime);

                // 8. ИНД = ИНД ∩ Рабочее_Время
                rc = 8;
                if (CourierType.VechicleType != CourierVehicleType.GettTaxi && CourierType.VechicleType != CourierVehicleType.YandexTaxi)
                {
                    DateTime workStart = currentModelTime.Date.Add(WorkStart);
                    DateTime workEnd = currentModelTime.Date.Add(WorkEnd);
                    if (!Helper.TimeIntervalsIntersection(intervalStart, intervalEnd, workStart, workEnd, out intervalStart, out intervalEnd))
                        return rc;

                    // 9. Если ИНД лежит целиком внутри обеда
                    rc = 9;
                    DateTime lunchStart = currentModelTime.Date.Add(LunchTimeStart);
                    DateTime lunchEnd = currentModelTime.Date.Add(LunchTimeEnd);
                    if (intervalStart >= lunchStart && intervalEnd <= lunchEnd)
                        return rc;

                    // 10. Строим интервал доставки с учетом обеда
                    rc = 10;
                    // Завершение доставки до начала обеда
                    if (firstPossibleDeliveryTime <= lunchStart)
                    {
                        TimeSpan interval = lunchStart - firstPossibleDeliveryTime;
                        if (interval < reserveTime)
                        {
                            intervalEnd = currentModelTime.Add(interval);
                            reserveTime = interval;
                        }
                    }
                    // Начало доставки после конца обеда
                    else if (intervalEnd >= lunchEnd)
                    {
                        if (intervalStart < lunchEnd)
                        {
                            intervalStart = lunchEnd;
                            reserveTime = intervalEnd - intervalStart;
                        }
                    }
                    else
                    {
                        return rc;
                    }

                    // 11. Если сейчас осуществляется доставка
                    rc = 11;
                    if (Status == CourierStatus.DeliversOrder)
                    {
                        if (LastDeliveryEnd > intervalEnd)
                            return rc;
                        if (LastDeliveryEnd > intervalStart)
                        {
                            intervalStart = LastDeliveryEnd;
                            reserveTime = intervalEnd - intervalStart;
                        }
                    }
                }

                deliveryInfo.Cost = cost;
                deliveryInfo.DeliveryTime = deliveryTime;
                deliveryInfo.ExecutionTime = executionTime;
                deliveryInfo.ReserveTime = reserveTime;
                deliveryInfo.StartDeliveryInterval = intervalStart;
                deliveryInfo.EndDeliveryInterval = intervalEnd;
                deliveryInfo.NodeDeliveryTime = nodeDeliveryTime;
                deliveryInfo.NodeDistance = distance;

                // 12. Все проверки пройдены
                rc = 0;
                return rc;
            }
            catch
            {
                return rc;
            }
        }

        /// <summary>
        /// Создание клона курьера с заданным id
        /// </summary>
        /// <param name="id">Новый Id или -1, если требуется оставить текущий</param>
        /// <returns>Клон курьера</returns>
        public Courier Clone(int id = -1)
        {
            if (id == -1) id = Id;
            Courier courierClone = new Courier(id, CourierType);
            courierClone.Status = Status;
            courierClone.WorkStart = WorkStart;
            courierClone.WorkEnd = WorkEnd;
            courierClone.LunchTimeStart = LunchTimeStart;
            courierClone.LunchTimeEnd = LunchTimeEnd;
            courierClone.LastDeliveryStart = LastDeliveryStart;
            courierClone.LastDeliveryEnd = LastDeliveryEnd;
            courierClone.OrderCount = OrderCount;
            courierClone.TotalDeliveryTime = TotalDeliveryTime;
            courierClone.TotalCost = TotalCost;
            courierClone.AverageOrderCost = AverageOrderCost;
            courierClone.Index = Index;
            return courierClone;
        }

        /// <summary>
        /// Расчет стоимости курьра c почасовой оплатой за день
        /// </summary>
        /// <param name="workStart">Начало работы</param>
        /// <param name="workEnd">Конец работы</param>
        /// <param name="workInterval">Округленная для целей расчета стоимости длительность работы</param>
        /// <param name="cost">Расчитанная стоимость</param>
        /// <returns></returns>
        public bool GetCourierDayCost(DateTime workStart, DateTime workEnd, int orderCount, out double workInterval, out double cost)
        {
            // 1. Инициализация
            workInterval = 0;
            cost = 0;

            try
            {
                // 2. Проверяем исходные данные
                if (workStart > workEnd)
                    return false;
                if (CourierType.HourlyRate <= 0)
                    return false;

                // 3. Расчитываем временной интервал в часах
                TimeSpan ts = (workEnd - workStart);
                workInterval = ts.TotalHours;
                if (workInterval < MIN_WORK_TIME)
                {
                    workInterval = MIN_WORK_TIME;
                }
                else
                {
                    workInterval = ts.Hours;
                    if (ts.Minutes != 0 || ts.Seconds != 0)
                    {
                        workInterval++;
                    }
                }

                // 4. Считаем стоимость рабочего дня почасового курьера
                cost = CourierType.HourlyRate * workInterval;
                if (CourierType.SecondPay > 0) cost += orderCount * CourierType.SecondPay;
                cost *= (1 + CourierType.Insurance);

                // 5. Выход
                return true;
            }
            catch
            {
                return false;
            }         
        }

        /// <summary>
        /// Такси ?
        /// </summary>
        public bool IsTaxi => (CourierType.VechicleType == CourierVehicleType.GettTaxi || CourierType.VechicleType == CourierVehicleType.YandexTaxi);
    }
}
