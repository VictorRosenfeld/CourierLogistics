
namespace LogAnalyzer.Analyzer
{
    /// <summary>
    /// Параметры анализатора
    /// </summary>
    public class AnalyzerConfig
    {
        /// <summary>
        /// Шаблон отчета
        /// </summary>
        public string ExcelPatternFile { get; set; }

        /// <summary>
        /// Файл результата
        /// </summary>
        public string ReportFile { get; set; }

        /// <summary>
        /// Открыть отчет после построения
        /// </summary>
        public bool OpenReport { get; set; }
    }
}
