
namespace LogAnalyzer.ReportData.OrdersSummary
{
    using System;

    /// <summary>
    /// Флаги заказа
    /// </summary>
    [Flags]
    public enum OrderFlags
    {
        /// <summary>
        /// Флаги отсутствуют
        /// </summary>
        None = 0x0,

        /// <summary>
        /// Заказ получен
        /// </summary>
        Receipted = 0x1,

        /// <summary>
        /// Заказ собран
        /// </summary>
        Asssembled = 0x2,

        /// <summary>
        /// Заказ отгружен
        /// </summary>
        Shipped = 0x4,

        /// <summary>
        /// Отказ в отгрузке
        /// </summary>
        Canceled = 0x8,

        /// <summary>
        /// Утечка
        /// </summary>
        Leak = 0x10,
    }
}
