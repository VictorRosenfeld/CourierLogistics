
namespace DeliveryBuilder.Geo.Cache
{
    using System;

    /// <summary>
    /// Элемент гео-кэша
    /// </summary>
    public class GeoCacheItem
    {
        /// <summary>
        /// Время получения данных
        /// </summary>
        public DateTime TimeReceived { get; private set; }

        /// <summary>
        /// Расстояние, метров
        /// </summary>
        public int Distance { get; private set; }

        /// <summary>
        /// Длительность, сек
        /// </summary>
        public int Duration { get; private set; }

        /// <summary>
        /// Параметрический конструктор класса GeoCacheItem
        /// </summary>
        /// <param name="timeReceived">Время получения данных</param>
        /// <param name="distance">Расстояние, метров</param>
        /// <param name="duration">Длительность, сек</param>
        public GeoCacheItem(DateTime timeReceived, int distance, int duration)
        {
            SetData(timeReceived, distance, duration);
        }

        ///// <summary>
        ///// Проверка, что элемент данных не заполнен
        ///// </summary>
        ///// <param name="index">Индекс элемента данных</param>
        ///// <returns>true - элемент данных не заполнен; false - элемент данных заполнен</returns>
        //public bool IsEmpty(int index)
        //{
        //    return Duration == int.MinValue;
        //}

        ///// <summary>
        ///// Помечаем экземпляр, как пустой
        ///// </summary>
        //public void SetEmpty()
        //{
        //    Duration = int.MinValue;
        //}

        /// <summary>
        /// Установка значений свойств экземпляра
        /// </summary>
        /// <param name="timeReceived">Время получения данных</param>
        /// <param name="distance">Расстояние, метров</param>
        /// <param name="duration">Длительность, сек</param>
        public void SetData(DateTime timeReceived, int distance, int duration)
        {
            TimeReceived = timeReceived;
            Distance = distance;
            Duration = duration;
        }
    }
}
