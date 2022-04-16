
namespace CreateDeliveriesApplication.Deliveries
{
    using CreateDeliveriesApplication.Orders;
    using System.Threading;

    /// <summary>
    /// Контекст потока для построения расширений отгрузок
    /// </summary>
    public class DilateRoutesContext
    {
        /// <summary>
        /// Объект синхронизации
        /// </summary>
        public ManualResetEvent SyncEvent { get; set; }

        #region Аргументы для расширения

        /// <summary>
        /// Исходные отгрузки
        /// (Расширяются отгрузки с индексом StartIndex + i * Step)
        /// </summary>
        public CourierDeliveryInfo[] SourceDeliveries;

        /// <summary>
        /// Стартовый индекс для расширяемых отгрузок в SourceDeliveries
        /// </summary>
        public int StartIndex { get; private set; }

        /// <summary>
        /// Шаг для расширяемых отгрузок
        /// </summary>
        public int Step { get; private set; }

        /// <summary>
        /// Стартовый уровень для расширения
        /// </summary>
        public int FromLevel { get; private set; }

        /// <summary>
        /// Целевой уровень расширения
        /// </summary>
        public int ToLevel { get; private set; }

        /// <summary>
        /// Заказы для раширения, отсортированные по ID
        /// </summary>
        public Order[] Orders { get; private set; }

        /// <summary>
        /// Попарные расстояния между заказами, включая магазин
        /// </summary>
        public Point[,] GeoData { get; private set; }

        #endregion Аргументы для расширения

        #region Результат расширения отгрузок

        /// <summary>
        /// Построенные отгрузки
        /// </summary>
        public CourierDeliveryInfo[] ExtendedDeliveries { get; set; }

        /// <summary>
        /// Число построенных отгрузок
        /// </summary>
        public int ExtendedCount => (ExtendedDeliveries == null ? 0 : ExtendedDeliveries.Length);

        /// <summary>
        /// Код возврата
        /// </summary>
        public int ExitCode { get; set; }

        #endregion Результат расширения отгрузок

        /// <summary>
        /// Параметрический конструктор класса DilateRoutesContext
        /// </summary>
        /// <param name="sourceDeliveries">Исходные отгрузки (Расширяются отгрузки с индексом StartIndex + i * Step)</param>
        /// <param name="startIndex">Стартовый индекс для расширяемых отгрузок в SourceDeliveries</param>
        /// <param name="step">Шаг для расширяемых отгрузок</param>
        /// <param name="fromLevel">Стартовый уровень для расширения</param>
        /// <param name="toLevel">Целевой уровень расширения</param>
        /// <param name="orders">Заказы для раширения, отсортированные по ID</param>
        /// <param name="geoData">Попарные расстояния между заказами, включая магазин</param>
        /// <param name="syncEvent">Объект синхронизации</param>
        public DilateRoutesContext(CourierDeliveryInfo[] sourceDeliveries, int startIndex, int step, int fromLevel, int toLevel, Order[] orders, Point[,] geoData, ManualResetEvent syncEvent)
        {
            SourceDeliveries = sourceDeliveries;
            StartIndex = startIndex;
            Step = step;
            FromLevel = fromLevel;
            ToLevel = toLevel;
            Orders = orders;
            GeoData = geoData;
            SyncEvent = syncEvent;
        }
    }
}
