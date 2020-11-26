
namespace LogisticsService.Shops
{
    using System;

    /// <summary>
    /// Магазин
    /// </summary>
    public class Shop
    {
        /// <summary>
        /// Id магазина
        /// </summary>
        public int Id { get; private set; }

        /// <summary>
        /// Начало работы
        /// </summary>
        public TimeSpan WorkStart { get; set; }

        /// <summary>
        /// Конец работы
        /// </summary>
        public TimeSpan WorkEnd { get; set; }

        /// <summary>
        /// Широта магазина
        /// </summary>
        public double Latitude { get; set; }

        /// <summary>
        /// Долгота магазина
        /// </summary>
        public double Longitude { get; set; }

        /// <summary>
        /// Индекс точки расположения магазина
        /// </summary>
        public int LocationIndex { get; set; }

        /// <summary>
        /// Параметрический конструктор класса Shop
        /// </summary>
        /// <param name="id">Id магазина</param>
        public Shop(int id)
        {
            Id = id;
        }
    }
}
