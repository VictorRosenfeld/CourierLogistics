
namespace LogAnalyzer.ReportData.OrdersSummary
{
    using System;

    /// <summary>
    /// Флаги доступных способов доставки
    /// </summary>
    [Flags]
    public enum DeliveryServiceFlags
    {
        /// <summary>
        /// Флаги отсутствуют
        /// </summary>
        None = 0x0,

        /// <summary>
        /// Yandex-такси
        /// </summary>
        YandexTaxi = 0x1,

        /// <summary>
        /// Gett-такси
        /// </summary>
        GettTaxi = 0x2,

        /// <summary>
        /// Курьер
        /// </summary>
        Courier = 0x4,
    }
}
