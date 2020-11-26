
namespace CourierLogistics.Logistics.RealSingleShopSolution
{
    using ClosedXML.Excel;
    using CourierLogistics.Logistics.OptimalSingleShopSolution;
    using CourierLogistics.SourceData.Couriers;
    using System;
    using System.Collections.Generic;
    using System.IO;

    /// <summary>
    /// Загрузчик CourierSummary
    /// </summary>
    public class ShopStatistics
    {
        /// <summary>
        /// Имя листа с DH-статистикой
        /// </summary>
        private const string HISTORY_SHEET_NAME = "DELIVERY HISTORY";

        /// <summary>
        /// Имя листа с CS-статистикой
        /// </summary>
        private const string SUMMARY_SHEET_NAME = "COURIER SUMMARY";

        #region CS-statistics columns

        private const int CS_DATE_COLUMN = 1;
        private const int CS_SHOP_ID_COLUMN = 2;
        private const int CS_COURIER_TYPE_COLUMN = 3;
        private const int CS_COURIER_ID_COLUMN = 4;
        private const int CS_ORDERS_COLUMN = 5;
        private const int CS_WEIGHT_COLUMN = 6;
        private const int CS_WORK_START_COLUMN = 7;
        private const int CS_WORK_END_COLUMN = 8;
        private const int CS_WORK_TIME_COLUMN = 9;
        private const int CS_EXEC_TIME_COLUMN = 10;
        private const int CS_DOWNTIME_COLUMN = 11;
        private const int CS_TOTAL_COST_COLUMN = 12;
        private const int CS_TOTAL_ORDER_COST_COLUMN = 13;

        #endregion CS-statistics columns

        #region DH-statistics columns

        private const int DH_SHOP_ID_COLUMN = 1;
        private const int DH_DELIVERY_START_COLUMN = 2;
        private const int DH_DELIVERY_END_COLUMN = 3;
        private const int DH_COURIER_ID_COLUMN = 4;
        private const int DH_COURIER_TYPE_COLUMN = 5;
        private const int DH_ORDERS_COLUMN = 6;
        private const int DH_WEIGHT_COLUMN = 7;
        private const int DH_COST_COLUMN = 8;
        private const int DH_FIRST_NODE_COLUMN = 9;
        private const int DH_FIRST_NODE_STEP = 4;
        private const int DH_NODE_ORDER_ID_OFFSET = 0;
        private const int DH_NODE_DISTANCE_OFFSET = 1;
        private const int DH_NODE_DELIVERY_TIME_OFFSET = 2;
        private const int DH_NODE_TIME_LIMIT_OFFSET = 3;

        #endregion DH-statistics columns

        /// <summary>
        /// Статистика Courier Summary (CS-статистика)
        /// </summary>
        private SummaryStatistics summaryStatistics;

        /// <summary>
        /// Статистика Delivery History (DH-статистика)
        /// </summary>
        private HistoryStatistics historyStatistics;

        /// <summary>
        /// Статистика Courier Summary (CS-статистика)
        /// </summary>
        public SummaryStatistics CSStatistics => summaryStatistics;

        /// <summary>
        /// Статистика Delivery History (DH-статистика)
        /// </summary>
        public HistoryStatistics DHStatistics => historyStatistics;

        /// <summary>
        /// Флаг: true - статистика загружена; false - статистика не загружена
        /// </summary>
        public bool IsLoaded { get; private set; }

        /// <summary>
        /// Загрузка CS-статистики
        /// </summary>
        /// <param name="summaryWorksheet">Лист с CS-статистикой</param>
        /// <param name="summaryStatistics">CS-статистика</param>
        /// <returns>0 - CS-статистика загружена; иначе - CS-статистика не загружена</returns>
        private static int LoadSummaryStatistics(IXLWorksheet summaryWorksheet, out SummaryStatistics summaryStatistics)
        {
            // 1. Инициализация
            int rc = 1;
            summaryStatistics = null;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (summaryWorksheet == null)
                    return rc;

                // 3. Цикл выборки статистики
                rc = 3;
                int lastRow = summaryWorksheet.LastRowUsed().RowNumber();
                if (lastRow <= 1)
                    return rc;
                ShopCourierStatistics[] records = new ShopCourierStatistics[lastRow - 1];
                int count = 0;

                for (int row = 2; row <= lastRow; row++)
                {
                    // 3.1 Если первая ячейка строки пуста
                    rc = 31;
                    if (summaryWorksheet.Cell(row, CS_DATE_COLUMN).IsEmpty())
                        break;

                    // 3.2 Извлекаем значения полей записи
                    rc = 32;
                    DateTime date = summaryWorksheet.Cell(row, CS_DATE_COLUMN).GetDateTime();
                    int shopId = summaryWorksheet.Cell(row, CS_SHOP_ID_COLUMN).GetValue<int>();
                    string courierType = summaryWorksheet.Cell(row, CS_COURIER_TYPE_COLUMN).GetString();
                    int courierId = summaryWorksheet.Cell(row, CS_COURIER_ID_COLUMN).GetValue<int>();
                    int orders = summaryWorksheet.Cell(row, CS_ORDERS_COLUMN).GetValue<int>();
                    double weight = summaryWorksheet.Cell(row, CS_WEIGHT_COLUMN).GetDouble();
                    DateTime workStart = summaryWorksheet.Cell(row, CS_WORK_START_COLUMN).GetDateTime();
                    DateTime workEnd = summaryWorksheet.Cell(row, CS_WORK_END_COLUMN).GetDateTime();
                    double workTime = summaryWorksheet.Cell(row, CS_WORK_TIME_COLUMN).GetDouble();
                    //double execTime = -1;
                    //if (!summaryWorksheet.Cell(row, CS_EXEC_TIME_COLUMN).IsEmpty())
                    //    execTime = summaryWorksheet.Cell(row, CS_EXEC_TIME_COLUMN).GetDouble();
                    double downtime = -1;
                    if (!summaryWorksheet.Cell(row, CS_DOWNTIME_COLUMN).IsEmpty())
                        downtime = summaryWorksheet.Cell(row, CS_DOWNTIME_COLUMN).GetDouble();

                    double totalCost = summaryWorksheet.Cell(row, CS_TOTAL_COST_COLUMN).GetDouble();
                    //double totalOrderCost = summaryWorksheet.Cell(row, CS_TOTAL_ORDER_COST_COLUMN).GetDouble();

                    // 3.3 Создаём и добавляем новую запись
                    rc = 33;
                    Courier courier = CreateCourier(courierId, courierType);

                    ShopCourierStatistics record = new ShopCourierStatistics(shopId, date, courier, totalCost);
                    record.OrderCount = orders;
                    record.TotalWeight = weight;
                    record.WorkStart = workStart;
                    record.WorkEnd = workEnd;
                    record.WorkTime = workTime;
                    if (downtime >= 0)
                        record.SetDowntime(downtime);

                    records[count++] = record;
                }

                if (count < records.Length)
                {
                    Array.Resize(ref records, count);
                }

                summaryStatistics = new SummaryStatistics(records);

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
        /// Создание курьера
        /// </summary>
        /// <param name="courierId">Id курьера</param>
        /// <param name="courierType">Тип курьера</param>
        /// <returns>Курьер или null</returns>
        private static Courier CreateCourier(int courierId, string courierType)
        {
            if (string.IsNullOrWhiteSpace(courierType))
                return null;

            Courier courier = null;

            switch (courierType.Trim().ToUpper())
            {
                case "CAR":
                    courier = new Courier(courierId, new CourierType_Car());
                    break;
                case "BICYCLE":
                    courier = new Courier(courierId, new CourierType_Bicycle());
                    break;
                case "GETTTAXI":
                    courier = new Courier(courierId, new CourierType_GettTaxi());
                    break;
                case "YANDEXTAXI":
                    courier = new Courier(courierId, new CourierType_YandexTaxi());
                    break;
                case "ONFOOT":
                    courier = new Courier(courierId, new CourierType_OnFoot());
                    break;
                case "CAR1":
                    courier = new Courier(courierId, new CourierType_Car1());
                    break;
                case "BICYCLE1":
                    courier = new Courier(courierId, new CourierType_Bicycle1());
                    break;
                case "ONFOOT1":
                    courier = new Courier(courierId, new CourierType_OnFoot1());
                    break;
            }

            return courier;
        }

        /// <summary>
        /// Загрузка DH-статистики
        /// </summary>
        /// <param name="historyWorksheet">Лист с DH-статистикой</param>
        /// <param name="historyStatistics">DH-статистика</param>
        /// <returns>0 - DH-статистика загружена; иначе - DH-статистика не загружена</returns>
        private static int LoadHistoryStatistics(IXLWorksheet historyWorksheet, out HistoryStatistics historyStatistics)
        {
            // 1. Инициализация
            int rc = 1;
            historyStatistics = null;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (historyWorksheet == null)
                    return rc;

                // 3. Цикл выборки статистики
                rc = 3;
                int lastRow = historyWorksheet.LastRowUsed().RowNumber();
                if (lastRow <= 1)
                    return rc;

                DeliveryHistory[] records = new DeliveryHistory[lastRow - 1];
                int count = 0;

                for (int row = 2; row <= lastRow; row++)
                {
                    // 3.1 Если первая ячейка строки пуста
                    rc = 31;
                    if (historyWorksheet.Cell(row, DH_SHOP_ID_COLUMN).IsEmpty())
                        break;

                    // 3.2. Извлекаем значения полей записи
                    rc = 32;
                    int shopId = historyWorksheet.Cell(row, DH_SHOP_ID_COLUMN).GetValue<int>();
                    DateTime deliveryStart = historyWorksheet.Cell(row, DH_DELIVERY_START_COLUMN).GetDateTime();
                    DateTime deliveryEnd = historyWorksheet.Cell(row, DH_DELIVERY_END_COLUMN).GetDateTime();
                    int courierId = historyWorksheet.Cell(row, DH_COURIER_ID_COLUMN).GetValue<int>();
                    CourierVehicleType courierType = (CourierVehicleType) Enum.Parse(typeof(CourierVehicleType), historyWorksheet.Cell(row, DH_COURIER_TYPE_COLUMN).GetString());
                    int orders = historyWorksheet.Cell(row, DH_ORDERS_COLUMN).GetValue<int>();
                    double weight = historyWorksheet.Cell(row, DH_WEIGHT_COLUMN).GetDouble();
                    double cost = historyWorksheet.Cell(row, DH_COST_COLUMN).GetDouble();

                    DeliveryHistory record = new DeliveryHistory(shopId, courierId, courierType, deliveryStart, deliveryEnd);
                    record.OrderCount = orders;
                    record.Weight = weight;
                    record.Cost = cost;
                    List<DeliveryHistoryNode> nodes = new List<DeliveryHistoryNode>(16);

                    for (int j = DH_FIRST_NODE_COLUMN; j < DH_FIRST_NODE_COLUMN + 400; j+= DH_FIRST_NODE_STEP)
                    {
                        if (historyWorksheet.Cell(row, j + DH_NODE_ORDER_ID_OFFSET).IsEmpty())
                            break;
                        int orderId = historyWorksheet.Cell(row, j + DH_NODE_ORDER_ID_OFFSET).GetValue<int>();
                        double distance = historyWorksheet.Cell(row, j + DH_NODE_DISTANCE_OFFSET).GetDouble();
                        DateTime deliveryTime = historyWorksheet.Cell(row, j + DH_NODE_DELIVERY_TIME_OFFSET).GetDateTime();
                        DateTime timeLimit = historyWorksheet.Cell(row, j + DH_NODE_TIME_LIMIT_OFFSET).GetDateTime();
                        nodes.Add(new DeliveryHistoryNode(orderId, distance, deliveryTime, timeLimit));
                    }

                    record.Nodes = nodes.ToArray();
                    records[count++] = record;
                }

                if (count < records.Length)
                {
                    Array.Resize(ref records, count);
                }

                historyStatistics = new HistoryStatistics(records);

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
        /// Загрузка стаистики из Excel-файла
        /// </summary>
        /// <param name="xlsxFileName">Имя xlsx-файла</param>
        /// <returns>0 - статистика загружена; иначе - статистика не занружена</returns>
        public int Load(string xlsxFileName)
        {
            // 1. Инициализация
            int rc = 1;
            IsLoaded = false;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (string.IsNullOrEmpty(xlsxFileName))
                    return rc;
                if (!File.Exists(xlsxFileName))
                    return rc;

                // 3. Загружаем книгу
                rc = 3;
                using (IXLWorkbook wkbStatistics = new XLWorkbook(xlsxFileName))
                {
                    // 4. Загружаем CS-статистику
                    rc = 4;
                    int rc1 = LoadSummaryStatistics(wkbStatistics.Worksheet(SUMMARY_SHEET_NAME), out summaryStatistics);
                    if (rc1 != 0)
                        return rc = 100 * rc + rc1;

                    // 5. Загружаем DH-статистику
                    rc = 5;
                    rc1 = LoadHistoryStatistics(wkbStatistics.Worksheet(HISTORY_SHEET_NAME), out historyStatistics);
                    if (rc1 != 0)
                        return rc = 100 * rc + rc1;
                }

                // 6. Выход - Ok
                rc = 0;
                IsLoaded = true;
                return rc;
            }
            catch
            {
                return rc;
            }
        }
    }
}
