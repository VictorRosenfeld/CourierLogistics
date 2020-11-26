
namespace CourierLogistics.Report
{
    using ClosedXML.Excel;
    using CourierLogistics.Logistics.CourierStatisticsCalculator;
    using CourierLogistics.Logistics.FloatSolution.FloatCourierStatistics;
    using CourierLogistics.Logistics.OptimalSingleShopSolution;
    using CourierLogistics.SourceData.Couriers;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

    /// <summary>
    /// Отчет в файле Excel
    /// </summary>
    public class ExcelReport
    {
        /// <summary>
        /// Имя листа со статистикой курьеров
        /// </summary>
        private const string COURIER_STATISTICS_PAGE = "Courier statistics";

        /// <summary>
        /// Имя листа с историей отгрузок на основе Gett
        /// </summary>
        public const string HISTORY_PAGE = "Delivery History";

        /// <summary>
        /// Имя листа со сводкой результатов на основе Gett
        /// </summary>
        public const string SUMMARY_PAGE = "Courier Summary";

        /// <summary>
        /// Имя листа со сводкой результатов для свободных курьеров
        /// </summary>
        public const string FLOAT_SUMMARY_PAGE = "Float Summary";

        /// <summary>
        /// Имя листа с историей отгрузок на основе Gett
        /// </summary>
        public const string HISTORY_YANDEX = "History_Yandex";

        /// <summary>
        /// Имя листа со сводкой результатов на основе Gett
        /// </summary>
        public const string SUMMARY_YANDEX = "Summary_Yandex";

        /// <summary>
        /// Имя файла книги
        /// </summary>
        public string FileName { get; private set; }

        /// <summary>
        /// Результирующий Workbook
        /// </summary>
        private IXLWorkbook workbook;

        /// <summary>
        /// Лист Courier Statistics
        /// </summary>
        private IXLWorksheet wksCourierStatistics;

        /// <summary>
        /// Номер текущий строки на листе Courier Statistics
        /// </summary>
        private int courierStatisticsRow;

        private Dictionary<string, ExcelReportSheet> sheets;

        /// <summary>
        /// Параметрический конструктор ExcelReport
        /// </summary>
        /// <param name="fileName">Имя результирующего файла</param>
        /// <param name="isFloat">Флаг: true - модель со свободными курьерами; false - модель с курьрами привязанными к магазину</param>
        public ExcelReport(string fileName, bool isFloat = false)
        {
            // 1. Создаём книгу
            FileName = fileName;
            workbook = new XLWorkbook();
            workbook.Author = "Victor Rosenfeld";

            // 2. Создаём и форматируем лист Courier Statistics
            wksCourierStatistics = workbook.Worksheets.Add(COURIER_STATISTICS_PAGE);
            FormatCourierStatistics(wksCourierStatistics);
            courierStatisticsRow = 1;

            sheets = new Dictionary<string, ExcelReportSheet>(16);
            CreateHistorySheet(HISTORY_PAGE);
            if (isFloat)
            {
                CreateFloatSummarySheet(FLOAT_SUMMARY_PAGE);
            }
            else
            {
                CreateSummarySheet(SUMMARY_PAGE);
            }
            //CreateHistorySheet(HISTORY_YANDEX);
            //CreateSummarySheet(SUMMARY_YANDEX);
        }

        /// <summary>
        /// Деструктор класса ExcelReport
        /// </summary>
        ~ExcelReport()
        {
            wksCourierStatistics = null;
            sheets = null;
            if (workbook != null)
            {
                workbook.Dispose();
                workbook = null;
            }
        }

        /// <summary>
        /// Форматирование листа Courier Statistics
        /// </summary>
        /// <param name="wksSheet">Форматируемая таблица</param>
        /// <returns>0 - Ok; иначе - ошибка</returns>
        private static int FormatCourierStatistics(IXLWorksheet wksSheet)
        {
            // 1. Инициализация
            int rc = 1;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (wksSheet == null)
                    return rc;

                // 3. Форматируем лист
                rc = 3;
                wksSheet.Row(1).Height = 33.60;

                wksSheet.Cell(1, 1).Value = "id_courier";
                wksSheet.Column(1).Width = 10.89;

                wksSheet.Cell(1, 2).Value = "Date";
                wksSheet.Column(2).Width = 9.33;

                wksSheet.Cell(1, 3).Value = "Order count";
                wksSheet.Column(3).Width = 6.56;

                wksSheet.Cell(1, 4).Value = "Total weight";
                wksSheet.Column(4).Width = 7.00;
                wksSheet.Column(4).Style.NumberFormat.Format = "0.00";

                wksSheet.Cell(1, 5).Value = "Total Delivery Time, min";
                wksSheet.Column(5).Width = 12.89;
                wksSheet.Column(5).Style.NumberFormat.Format = "0.00";

                wksSheet.Cell(1, 6).Value = "Total Execute Time, min";
                wksSheet.Column(6).Width = 11.44;
                wksSheet.Column(6).Style.NumberFormat.Format = "0.00";

                wksSheet.Cell(1, 7).Value = "Total cost";
                wksSheet.Column(7).Width = 7.33;
                wksSheet.Column(7).Style.NumberFormat.Format = "0.00";

                wksSheet.Cell(1, 8).Value = "Avg orders per hour";
                wksSheet.Column(8).Width = 9.78;
                wksSheet.Column(8).Style.NumberFormat.Format = "0.00";

                wksSheet.Cell(1, 9).Value = "Avg delivery time, min";
                wksSheet.Column(9).Width = 11.22;
                wksSheet.Column(9).Style.NumberFormat.Format = "0.00";

                wksSheet.Cell(1, 10).Value = "Avg order cost";
                wksSheet.Column(10).Width = 8.89;

                // Header style

                wksSheet.Range(1, 1, 1, 10).Style.Font.Bold = true;
                wksSheet.Range(1, 1, 1, 10).Style.Alignment.WrapText = true;
                wksSheet.Range(1, 1, 1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                wksSheet.Range(1, 2, 1, 10).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                wksSheet.Range(1, 1, 1, 10).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                wksSheet.Range(1, 1, 1, 10).Style.Border.LeftBorder = XLBorderStyleValues.Thin;
                wksSheet.Range(1, 1, 1, 10).Style.Border.RightBorder = XLBorderStyleValues.Thin;
                wksSheet.Range(1, 1, 1, 10).Style.Border.TopBorder = XLBorderStyleValues.Thin;
                wksSheet.Range(1, 1, 1, 10).Style.Border.BottomBorder = XLBorderStyleValues.Thin;

                // Freeze rows & cols

                wksSheet.SheetView.Freeze(1, 0);
                wksSheet.Cell(2, 1).Select();

                // Set autofilter

                wksSheet.Range(1, 1, 1, 10).SetAutoFilter();

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
        /// Форматирование листа History
        /// </summary>
        /// <param name="wksSheet">Форматируемая таблица</param>
        /// <returns>0 - Ok; иначе - ошибка</returns>
        private static int FormatHistoryPage(IXLWorksheet wksSheet)
        {
            // 1. Инициализация
            int rc = 1;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (wksSheet == null)
                    return rc;

                // 3. Форматируем лист
                rc = 3;
                wksSheet.Row(1).Height = 33.60;

                wksSheet.Cell(1, 1).Value = "Shop N";
                wksSheet.Column(1).Width = 6.78;

                wksSheet.Cell(1, 2).Value = "Delivery Start";
                wksSheet.Column(2).Width = 12.22;
                wksSheet.Column(2).Style.NumberFormat.Format = "dd/mm/yy hh:mm;@";

                wksSheet.Cell(1, 3).Value = "Delivery End";
                wksSheet.Column(3).Width = 12.22;
                wksSheet.Column(3).Style.NumberFormat.Format = "dd/mm/yy hh:mm;@";

                wksSheet.Cell(1, 4).Value = "Courier ID";
                wksSheet.Column(4).Width = 8.56;

                wksSheet.Cell(1, 5).Value = "Courier Type";
                wksSheet.Column(5).Width = 10.56;

                wksSheet.Cell(1, 6).Value = "Orders";
                wksSheet.Column(6).Width = 5.33;

                wksSheet.Cell(1, 7).Value = "Weight, kg";
                wksSheet.Column(7).Width = 6.56;
                wksSheet.Column(7).Style.NumberFormat.Format = "0.0";

                wksSheet.Cell(1, 8).Value = "Cost";
                wksSheet.Column(8).Width = 6.22;
                wksSheet.Column(8).Style.NumberFormat.Format = "0.0";

                int orderCount = 16;

                for (int i = 1; i <= orderCount; i++)
                {
                    int j = 9 + 4 * (i - 1);
                    wksSheet.Cell(1, j).Value = $"id_order {i}";
                    wksSheet.Column(j).Width = 8.33;
                    wksSheet.Column(j).Style.NumberFormat.Format = "0";

                    wksSheet.Cell(1, j + 1).Value = $"Dist {i}";
                    wksSheet.Column(j + 1).Width = 5.89;
                    wksSheet.Column(j + 1).Style.NumberFormat.Format = "0.0";

                    wksSheet.Cell(1, j + 2).Value = $"Delivery Time {i}";
                    wksSheet.Column(j + 2).Width = 12.22;
                    wksSheet.Column(j + 2).Style.NumberFormat.Format = "dd/mm/yy hh:mm;@";

                    wksSheet.Cell(1, j + 3).Value = $"Time Limit {i}";
                    wksSheet.Column(j + 3).Width = 12.22;
                    wksSheet.Column(j + 3).Style.NumberFormat.Format = "dd/mm/yy hh:mm;@";
                }

                // Header style
                int lastColumn = 8 + 4 * orderCount;

                wksSheet.Range(1, 1, 1, lastColumn).Style.Font.Bold = true;
                wksSheet.Range(1, 1, 1, lastColumn).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                wksSheet.Range(1, 1, 1, lastColumn).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                wksSheet.Range(1, 1, 1, lastColumn).Style.Border.LeftBorder = XLBorderStyleValues.Thin;
                wksSheet.Range(1, 1, 1, lastColumn).Style.Border.RightBorder = XLBorderStyleValues.Thin;
                wksSheet.Range(1, 1, 1, lastColumn).Style.Border.TopBorder = XLBorderStyleValues.Thin;
                wksSheet.Range(1, 1, 1, lastColumn).Style.Border.BottomBorder = XLBorderStyleValues.Thin;

                // Freeze rows & cols

                wksSheet.SheetView.Freeze(1, 0);
                wksSheet.Cell(2, 1).Select();

                // Set autofilter

                wksSheet.Range(1, 1, 1, lastColumn).SetAutoFilter();

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
        /// Форматирование листа Summary
        /// </summary>
        /// <param name="wksSheet">Форматируемая таблица</param>
        /// <returns>0 - Ok; иначе - ошибка</returns>
        private static int FormatSummaryPage(IXLWorksheet wksSheet)
        {
            // 1. Инициализация
            int rc = 1;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (wksSheet == null)
                    return rc;

                // 3. Форматируем лист
                rc = 3;
                wksSheet.Row(1).Height = 33.60;

                wksSheet.Cell(1, 1).Value = "Date";
                wksSheet.Column(1).Width = 7.33;
                wksSheet.Column(1).Style.NumberFormat.Format = @"dd/mm/yy;@";


                wksSheet.Cell(1, 2).Value = "Shop N";
                wksSheet.Column(2).Width = 6.78;

                wksSheet.Cell(1, 3).Value = "Courier Type";
                wksSheet.Column(3).Width = 10.56;

                wksSheet.Cell(1, 4).Value = "Courier ID";
                wksSheet.Column(4).Width = 8.22;

                wksSheet.Cell(1, 5).Value = "Orders";
                wksSheet.Column(5).Width = 5.33;

                wksSheet.Cell(1, 6).Value = "Total Weight, kg";
                wksSheet.Column(6).Width = 9.78;
                wksSheet.Column(6).Style.NumberFormat.Format = "0.0";

                wksSheet.Cell(1, 7).Value = "Work Start";
                wksSheet.Column(7).Width = 12.22;
                wksSheet.Column(7).Style.NumberFormat.Format = "dd/mm/yy hh:mm;@";

                wksSheet.Cell(1, 8).Value = "Work End";
                wksSheet.Column(8).Width = 12.22;
                wksSheet.Column(8).Style.NumberFormat.Format = "dd/mm/yy hh:mm;@";

                wksSheet.Cell(1, 9).Value = "Work Time";
                wksSheet.Column(9).Width = 8.11;
                wksSheet.Column(9).Style.NumberFormat.Format = "0";

                wksSheet.Cell(1, 10).Value = "Exec Time";
                wksSheet.Column(10).Width = 7.22;
                wksSheet.Column(10).Style.NumberFormat.Format = "0.00";

                wksSheet.Cell(1, 11).Value = "Downtime";
                wksSheet.Column(11).Width = 7.22;
                wksSheet.Column(11).Style.NumberFormat.Format = "0.00";

                wksSheet.Cell(1, 12).Value = "Total Cost";
                wksSheet.Column(12).Width = 8.67;
                wksSheet.Column(12).Style.NumberFormat.Format = "0.0";

                wksSheet.Cell(1, 13).Value = "Order Cost";
                wksSheet.Column(13).Width = 8.67;
                wksSheet.Column(13).Style.NumberFormat.Format = "0.0";

                // Header style
                int lastColumn = 13;

                wksSheet.Range(1, 1, 1, lastColumn).Style.Font.Bold = true;
                wksSheet.Range(1, 1, 1, lastColumn).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                wksSheet.Range(1, 1, 1, lastColumn).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                wksSheet.Range(1, 1, 1, lastColumn).Style.Border.LeftBorder = XLBorderStyleValues.Thin;
                wksSheet.Range(1, 1, 1, lastColumn).Style.Border.RightBorder = XLBorderStyleValues.Thin;
                wksSheet.Range(1, 1, 1, lastColumn).Style.Border.TopBorder = XLBorderStyleValues.Thin;
                wksSheet.Range(1, 1, 1, lastColumn).Style.Border.BottomBorder = XLBorderStyleValues.Thin;

                // Freeze rows & cols

                wksSheet.SheetView.Freeze(1, 0);
                wksSheet.Cell(2, 1).Select();

                // Set autofilter

                wksSheet.Range(1, 1, 1, lastColumn).SetAutoFilter();

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
        /// Форматирование листа Summary c плавающими курьерами
        /// </summary>
        /// <param name="wksSheet">Форматируемая таблица</param>
        /// <returns>0 - Ok; иначе - ошибка</returns>
        private static int FormatFloatSummaryPage(IXLWorksheet wksSheet)
        {
            // 1. Инициализация
            int rc = 1;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (wksSheet == null)
                    return rc;

                // 3. Форматируем лист
                rc = 3;
                wksSheet.Row(1).Height = 33.60;

                wksSheet.Cell(1, 1).Value = "Date";
                wksSheet.Column(1).Width = 7.33;
                wksSheet.Column(1).Style.NumberFormat.Format = @"dd/mm/yy;@";

                wksSheet.Cell(1, 2).Value = "Courier ID";
                wksSheet.Column(2).Width = 8.22;

                wksSheet.Cell(1, 3).Value = "Courier Type";
                wksSheet.Column(3).Width = 10.56;

                wksSheet.Cell(1, 4).Value = "Shops";
                wksSheet.Column(4).Width = 5.33;

                wksSheet.Cell(1, 5).Value = "Orders";
                wksSheet.Column(5).Width = 5.33;

                wksSheet.Cell(1, 6).Value = "Total Weight, kg";
                wksSheet.Column(6).Width = 9.78;
                wksSheet.Column(6).Style.NumberFormat.Format = "0.0";

                wksSheet.Cell(1, 7).Value = "Work Start";
                wksSheet.Column(7).Width = 12.22;
                wksSheet.Column(7).Style.NumberFormat.Format = "dd/mm/yy hh:mm;@";

                wksSheet.Cell(1, 8).Value = "Work End";
                wksSheet.Column(8).Width = 12.22;
                wksSheet.Column(8).Style.NumberFormat.Format = "dd/mm/yy hh:mm;@";

                wksSheet.Cell(1, 9).Value = "Work Time";
                wksSheet.Column(9).Width = 8.11;
                wksSheet.Column(9).Style.NumberFormat.Format = "0";

                wksSheet.Cell(1, 10).Value = "Exec Time";
                wksSheet.Column(10).Width = 7.22;
                wksSheet.Column(10).Style.NumberFormat.Format = "0.00";

                wksSheet.Cell(1, 11).Value = "Downtime";
                wksSheet.Column(11).Width = 7.22;
                wksSheet.Column(11).Style.NumberFormat.Format = "0.00";

                wksSheet.Cell(1, 12).Value = "Total Cost";
                wksSheet.Column(12).Width = 8.67;
                wksSheet.Column(12).Style.NumberFormat.Format = "0.0";

                wksSheet.Cell(1, 13).Value = "Order Cost";
                wksSheet.Column(13).Width = 8.67;
                wksSheet.Column(13).Style.NumberFormat.Format = "0.0";

                // Header style
                int lastColumn = 13;

                wksSheet.Range(1, 1, 1, lastColumn).Style.Font.Bold = true;
                wksSheet.Range(1, 1, 1, lastColumn).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                wksSheet.Range(1, 1, 1, lastColumn).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                wksSheet.Range(1, 1, 1, lastColumn).Style.Border.LeftBorder = XLBorderStyleValues.Thin;
                wksSheet.Range(1, 1, 1, lastColumn).Style.Border.RightBorder = XLBorderStyleValues.Thin;
                wksSheet.Range(1, 1, 1, lastColumn).Style.Border.TopBorder = XLBorderStyleValues.Thin;
                wksSheet.Range(1, 1, 1, lastColumn).Style.Border.BottomBorder = XLBorderStyleValues.Thin;

                // Freeze rows & cols

                wksSheet.SheetView.Freeze(1, 0);
                wksSheet.Cell(2, 1).Select();

                // Set autofilter

                wksSheet.Range(1, 1, 1, lastColumn).SetAutoFilter();

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
        /// Заполнение новой строки на листе Courier Statistics
        /// </summary>
        /// <param name="dayData"></param>
        /// <returns></returns>
        public int PrintCourierStatisticsRow(CourierStatisticsDayData dayData)
        {
            // 1. Инициализация
            int rc = 1;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (wksCourierStatistics == null)
                    return rc;
                if (courierStatisticsRow < 0)
                    return rc;
                if (dayData == null)
                    return rc;

                // 3. Заполняем новую строку
                rc = 3;
                courierStatisticsRow++;
                wksCourierStatistics.Cell(courierStatisticsRow, 1).SetValue(dayData.Id);
                wksCourierStatistics.Cell(courierStatisticsRow, 2).SetValue(dayData.Day);
                wksCourierStatistics.Cell(courierStatisticsRow, 3).SetValue(dayData.OrderCount);
                wksCourierStatistics.Cell(courierStatisticsRow, 4).SetValue(dayData.TotalWeight);
                wksCourierStatistics.Cell(courierStatisticsRow, 5).SetValue(dayData.TotalDeliveryTime);
                wksCourierStatistics.Cell(courierStatisticsRow, 6).SetValue(dayData.TotalExecuteTime);
                wksCourierStatistics.Cell(courierStatisticsRow, 7).SetValue(dayData.TotalCost);

                if (dayData.OrderCount > 0)
                {
                    double count = dayData.OrderCount;
                    wksCourierStatistics.Cell(courierStatisticsRow, 8).SetValue(count / 6);
                    wksCourierStatistics.Cell(courierStatisticsRow, 9).SetValue(dayData.TotalDeliveryTime / count);
                    wksCourierStatistics.Cell(courierStatisticsRow, 10).SetValue(dayData.TotalCost / count);
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

        /// <summary>
        /// Сохранение книги
        /// </summary>
        /// <returns>0 - книга сохранена; иначе - книга не сохранена</returns>
        public int Save()
        {
            // 1. Инициализация
            int rc = 1;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (workbook == null)
                    return rc;
                if (string.IsNullOrEmpty(FileName))
                    return rc;

                // 3. Сохраняем книгу
                rc = 3;
                workbook.SaveAs(FileName);

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
        /// Открытие книги в Excel
        /// </summary>
        /// <returns>0 - книга сохранена; иначе - книга не сохранена</returns>
        public int Show()
        {
            // 1. Инициализация
            int rc = 1;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (workbook == null)
                    return rc;
                if (string.IsNullOrEmpty(FileName))
                    return rc;

                // 3. Открываем файл в приписанном ему приложении
                rc = 3;
                Process excel = new Process();
                excel.StartInfo.FileName = FileName;
                excel.StartInfo.UseShellExecute = true;
                excel.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
                excel.Start();

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
        /// Создание нового History-листа
        /// </summary>
        /// <param name="name">Имя листа</param>
        public void CreateHistorySheet(string name)
        {
            IXLWorksheet newSheet = workbook.Worksheets.Add(name);
            FormatHistoryPage(newSheet);
            ExcelReportSheet reportSheet = new ExcelReportSheet(name, newSheet, 1);
            sheets.Add(name, reportSheet);
        }

        /// <summary>
        /// Создание нового Summary-листа
        /// </summary>
        /// <param name="name">Имя листа</param>
        public void CreateSummarySheet(string name)
        {
            IXLWorksheet newSheet = workbook.Worksheets.Add(name);
            FormatSummaryPage(newSheet);
            ExcelReportSheet reportSheet = new ExcelReportSheet(name, newSheet, 1);
            sheets.Add(name, reportSheet);
        }

        /// <summary>
        /// Создание нового Summary-листа для свободных курьеров
        /// </summary>
        /// <param name="name">Имя листа</param>
        public void CreateFloatSummarySheet(string name)
        {
            IXLWorksheet newSheet = workbook.Worksheets.Add(name);
            FormatFloatSummaryPage(newSheet);
            ExcelReportSheet reportSheet = new ExcelReportSheet(name, newSheet, 1);
            sheets.Add(name, reportSheet);
        }

        ///// <summary>
        ///// Печать очередной строки на History-листе
        ///// </summary>
        ///// <param name="sheetName">Имя листа</param>
        ///// <param name="shopNo">Номер магазина</param>
        ///// <param name="startDelivery">Начало отгрузки</param>
        ///// <param name="endDelivery">Конец отгрузки</param>
        ///// <param name="courierId">ID-курьера</param>
        ///// <param name="courierType">Тип курьера</param>
        ///// <param name="orderCount">Число заказов в отгрузке</param>
        ///// <param name="weight">Вес всех заказов</param>
        ///// <param name="cost">Стоимость доставки</param>
        ///// <param name="orderId">ID заказов в порядке движения</param>
        ///// <returns>0 - строка напечатана; строка не напечатана</returns>
        //public int PrintHistoryRow(string sheetName, 
        //    int shopNo, 
        //    DateTime startDelivery, DateTime endDelivery,
        //    int courierId, string courierType, 
        //    int orderCount, double weight, double cost, int[] orderId)
        //{
        //    // 1. Инициализация
        //    int rc = 1;

        //    try
        //    {
        //        // 2. Проверяем исходные данные
        //        rc = 2;
        //        ExcelReportSheet reportSheet;
        //        if (!sheets.TryGetValue(sheetName, out reportSheet))
        //            return rc;

        //        // 3. Печатаем строку
        //        IXLWorksheet sheet = reportSheet.Worksheet;
        //        int row = reportSheet.Row;

        //        row++;
        //        sheet.Cell(row, 1).SetValue(shopNo);
        //        sheet.Cell(row, 2).SetValue(startDelivery);
        //        sheet.Cell(row, 3).SetValue(endDelivery);
        //        sheet.Cell(row, 4).SetValue(courierId);
        //        sheet.Cell(row, 5).SetValue(courierType);
        //        sheet.Cell(row, 6).SetValue(orderCount);
        //        sheet.Cell(row, 7).SetValue(weight);
        //        if (cost > 0)
        //            sheet.Cell(row, 8).SetValue(cost);
        //        if (orderId != null && orderId.Length > 0)
        //        {
        //            for (int i = 0; i < orderId.Length; i++)
        //            {
        //                sheet.Cell(row, 9 + i).SetValue(orderId[i]);

        //            }
        //        }

        //        reportSheet.Row = row;

        //        // 4. Выход - Ok
        //        rc = 0;
        //        return rc;
        //    }
        //    catch
        //    {
        //        return rc;
        //    }
        //}

        ///// <summary>
        ///// Печать очередной строки на Summary-листе
        ///// </summary>
        ///// <param name="sheetName">Имя листа</param>
        ///// <param name="shopNo">Номер магазина</param>
        ///// <param name="date">Дата</param>
        ///// <param name="courierType">Тип курьера</param>
        ///// <param name="courierId">Id курьера</param>
        ///// <param name="orderCount">Число отгруженных заказов</param>
        ///// <param name="totalWeight">Вес всех отгруженных заказов</param>
        ///// <param name="totalCost">Общая стоимость доставки всех заказов</param>
        ///// <param name="orderId">Средняя стоимость доставки одного заказа</param>
        ///// <returns>0 - строка напечатана; строка не напечатана</returns>
        //public int PrintSummaryRow(string sheetName, 
        //    DateTime date, int shopNo,  
        //    string courierType, int courierId, 
        //    int orderCount, double totalWeight,
        //    double totalCost, double orderCost)
        //{
        //    // 1. Инициализация
        //    int rc = 1;

        //    try
        //    {
        //        // 2. Проверяем исходные данные
        //        rc = 2;
        //        ExcelReportSheet reportSheet;
        //        if (!sheets.TryGetValue(sheetName, out reportSheet))
        //            return rc;

        //        // 3. Печатаем строку
        //        IXLWorksheet sheet = reportSheet.Worksheet;
        //        int row = reportSheet.Row;

        //        row++;
        //        sheet.Cell(row, 1).SetValue(date);
        //        sheet.Cell(row, 2).SetValue(shopNo);
        //        sheet.Cell(row, 3).SetValue(courierType);
        //        sheet.Cell(row, 4).SetValue(courierId);
        //        sheet.Cell(row, 5).SetValue(orderCount);
        //        sheet.Cell(row, 6).SetValue(totalWeight);
        //        sheet.Cell(row, 7).SetValue(totalCost);
        //        sheet.Cell(row, 8).SetValue(orderCost);

        //        reportSheet.Row = row;

        //        // 4. Выход - Ok
        //        rc = 0;
        //        return rc;
        //    }
        //    catch
        //    {
        //        return rc;
        //    }
        //}

        /// <summary>
        /// Печать очередной строки на History-листе
        /// </summary>
        /// <param name="sheetName">Имя листа</param>
        /// <param name="deliveryInfo">Отгрузка</param>
        /// <returns>0 - строка напечатана; строка не напечатана</returns>
        public int PrintHistoryRowEx(string sheetName, CourierDeliveryInfo deliveryInfo)
        {
            // 1. Инициализация
            int rc = 1;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (deliveryInfo == null)
                    return rc;
                ExcelReportSheet reportSheet;
                if (!sheets.TryGetValue(sheetName, out reportSheet))
                    return rc;

                // 3. Печатаем строку
                rc = 3;
                IXLWorksheet sheet = reportSheet.Worksheet;
                int row = reportSheet.Row;

                row++;

                DateTime startDelivery = deliveryInfo.StartDelivery;
                if (deliveryInfo.ShippingOrder != null)
                {
                    sheet.Cell(row, 1).SetValue(deliveryInfo.ShippingOrder.ShopNo);
                }
                else if (deliveryInfo.DeliveredOrders != null && deliveryInfo.DeliveredOrders.Length > 0)
                {
                    sheet.Cell(row, 1).SetValue(deliveryInfo.DeliveredOrders[0].ShippingOrder.ShopNo);
                }
                sheet.Cell(row, 2).SetValue(startDelivery);
                sheet.Cell(row, 3).SetValue(deliveryInfo.StartDelivery.AddMinutes(deliveryInfo.DeliveryTime));
                sheet.Cell(row, 4).SetValue(deliveryInfo.DeliveryCourier.Id);
                sheet.Cell(row, 5).SetValue(Enum.GetName(deliveryInfo.DeliveryCourier.CourierType.VechicleType.GetType(), deliveryInfo.DeliveryCourier.CourierType.VechicleType));
                sheet.Cell(row, 6).SetValue(deliveryInfo.OrderCount);
                sheet.Cell(row, 7).SetValue(deliveryInfo.Weight);
                if (deliveryInfo.Cost > 0)
                    sheet.Cell(row, 8).SetValue(deliveryInfo.Cost);

                // маршрут
                double[] distance = deliveryInfo.NodeDistance;
                double[] deliveryTime = deliveryInfo.NodeDeliveryTime;
                CourierDeliveryInfo[] deliveryOrders = deliveryInfo.DeliveredOrders;
                double handInTime = deliveryInfo.DeliveryCourier.CourierType.HandInTime;

                if (deliveryOrders != null && deliveryOrders.Length > 0)
                {
                    for (int i = 0, j = 9; i < deliveryOrders.Length; i++, j += 4)
                    {
                        CourierDeliveryInfo deliveryOrder = deliveryOrders[i];
                        sheet.Cell(row, j).SetValue(deliveryOrder.ShippingOrder.Id_order);
                        sheet.Cell(row, j + 1).SetValue(distance[i + 1]);
                        //sheet.Cell(row, j + 2).SetValue(startDelivery.AddMinutes(deliveryTime[i + 1] + handInTime));
                        sheet.Cell(row, j + 2).SetValue(startDelivery.AddMinutes(deliveryTime[i + 1]));
                        sheet.Cell(row, j + 3).SetValue(deliveryOrder.ShippingOrder.GetDeliveryLimit());
                    }
                }
                else
                {
                    sheet.Cell(row, 9).SetValue(deliveryInfo.ShippingOrder.Id_order);
                    sheet.Cell(row, 10).SetValue(deliveryInfo.DistanceFromShop);
                    sheet.Cell(row, 11).SetValue(startDelivery.AddMinutes(deliveryInfo.DeliveryTime));
                    sheet.Cell(row, 12).SetValue(deliveryInfo.ShippingOrder.GetDeliveryLimit());
                }

                reportSheet.Row = row;

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
        /// Печать очередной строки на Summary-листе
        /// </summary>
        /// <param name="sheetName">Имя листа</param>
        /// <param name="courierStatistics">Статистика курьера за день</param>
        /// <returns>0 - строка напечатана; строка не напечатана</returns>
        public int PrintSummaryRowEx(string sheetName, ShopCourierStatistics courierStatistics)
        {
            // 1. Инициализация
            int rc = 1;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                ExcelReportSheet reportSheet;
                if (!sheets.TryGetValue(sheetName, out reportSheet))
                    return rc;

                // 3. Печатаем строку
                IXLWorksheet sheet = reportSheet.Worksheet;
                int row = reportSheet.Row;

                row++;
                sheet.Cell(row, 1).SetValue(courierStatistics.Date);
                sheet.Cell(row, 2).SetValue(courierStatistics.ShopId);
                sheet.Cell(row, 3).SetValue(courierStatistics.CourierTypeToString());
                sheet.Cell(row, 4).SetValue(courierStatistics.CourierId);
                sheet.Cell(row, 5).SetValue(courierStatistics.OrderCount);
                sheet.Cell(row, 6).SetValue(courierStatistics.TotalWeight);
                sheet.Cell(row, 7).SetValue(courierStatistics.WorkStart);
                sheet.Cell(row, 8).SetValue(courierStatistics.WorkEnd);
                Courier courier = courierStatistics.ShopCourier;
                double workInterval = -1;
                double totalCost = -1;

                if (courier.CourierType.VechicleType == CourierVehicleType.Car ||
                    courier.CourierType.VechicleType == CourierVehicleType.Bicycle ||
                    courier.CourierType.VechicleType == CourierVehicleType.OnFoot)
                {
                    courier.GetCourierDayCost(courierStatistics.WorkStart, courierStatistics.WorkEnd, courierStatistics.OrderCount, out workInterval, out totalCost);
                    double downtime = courierStatistics.Downtime / 60;
                    sheet.Cell(row, 10).SetValue(workInterval - downtime);
                    sheet.Cell(row, 11).SetValue(downtime);
                }
                else
                {
                    workInterval = (courierStatistics.WorkEnd - courierStatistics.WorkStart).TotalHours;
                    totalCost = courierStatistics.TotalCost;
                }

                if (workInterval > 0)
                    sheet.Cell(row, 9).SetValue(workInterval);

                if (totalCost > 0)
                    sheet.Cell(row, 12).SetValue(totalCost);

                if (totalCost > 0 && courierStatistics.OrderCount > 0)
                    sheet.Cell(row, 13).SetValue(totalCost / courierStatistics.OrderCount);

                reportSheet.Row = row;

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
        /// Печать очередной строки на Summary-листе со свободными курьерами
        /// </summary>
        /// <param name="sheetName">Имя листа</param>
        /// <param name="courierStatistics">Статистика курьера за день</param>
        /// <returns>0 - строка напечатана; строка не напечатана</returns>
        public int PrintFloatSummaryRowEx(string sheetName, FloatCourierDayStatistics courierStatistics)
        {
            // 1. Инициализация
            int rc = 1;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                ExcelReportSheet reportSheet;
                if (!sheets.TryGetValue(sheetName, out reportSheet))
                    return rc;

                // 3. Печатаем строку
                rc = 3;
                IXLWorksheet sheet = reportSheet.Worksheet;
                int row = reportSheet.Row;

                row++;
                sheet.Cell(row, 1).SetValue(courierStatistics.Date);
                sheet.Cell(row, 2).SetValue(courierStatistics.CourierId);
                sheet.Cell(row, 3).SetValue(courierStatistics.CourierTypeToString());
                sheet.Cell(row, 4).SetValue(courierStatistics.ShopCount);
                sheet.Cell(row, 5).SetValue(courierStatistics.OrderCount);
                sheet.Cell(row, 6).SetValue(courierStatistics.TotalWeight);
                sheet.Cell(row, 7).SetValue(courierStatistics.WorkStart);
                sheet.Cell(row, 8).SetValue(courierStatistics.WorkEnd);
                Courier courier = courierStatistics.ShopCourier;
                double workInterval = -1;
                double totalCost = -1;

                if (courier.CourierType.VechicleType == CourierVehicleType.Car ||
                    courier.CourierType.VechicleType == CourierVehicleType.Bicycle ||
                    courier.CourierType.VechicleType == CourierVehicleType.OnFoot)
                {
                    courier.GetCourierDayCost(courierStatistics.WorkStart, courierStatistics.WorkEnd, courierStatistics.OrderCount, out workInterval, out totalCost);
                    double downtime = courierStatistics.Downtime / 60;
                    sheet.Cell(row, 10).SetValue(workInterval - downtime);
                    sheet.Cell(row, 11).SetValue(downtime);
                }
                else
                {
                    workInterval = (courierStatistics.WorkEnd - courierStatistics.WorkStart).TotalHours;
                    totalCost = courierStatistics.TotalCost;
                }

                if (workInterval > 0)
                    sheet.Cell(row, 9).SetValue(workInterval);

                if (totalCost > 0)
                    sheet.Cell(row, 12).SetValue(totalCost);

                if (totalCost > 0 && courierStatistics.OrderCount > 0)
                    sheet.Cell(row, 13).SetValue(totalCost / courierStatistics.OrderCount);

                reportSheet.Row = row;

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
}


