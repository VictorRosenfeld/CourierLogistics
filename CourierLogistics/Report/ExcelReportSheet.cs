
namespace CourierLogistics.Report
{
    using ClosedXML.Excel;

    public class ExcelReportSheet
    {
        /// <summary>
        /// Имя листа
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Лист книги
        /// </summary>
        public IXLWorksheet Worksheet { get; private set; }

        /// <summary>
        /// Текущая строка
        /// </summary>
        public int Row { get; set; }

        /// <summary>
        /// Параметрический конструктор класса ExcelReportSheet
        /// </summary>
        /// <param name="name">Имя листа</param>
        /// <param name="worksheet">Лист книги</param>
        /// <param name="row">Текущая строка</param>
        public ExcelReportSheet(string name, IXLWorksheet worksheet, int row)
        {
            Name = name;
            Worksheet = worksheet;
            Row = row;
        }
    }
}
