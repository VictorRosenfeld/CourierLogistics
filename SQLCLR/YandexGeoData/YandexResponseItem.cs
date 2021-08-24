
namespace SQLCLR.YandexGeoData
{
    /// <summary>
    /// Данные для пары точек из отклика Yandex
    /// </summary>
    public class YandexResponseItem
    {
        /// <summary>
        /// Расстояние, метров
        /// </summary>
        public int distance { get; set; }

        /// <summary>
        /// Время передвижения, секунд
        /// </summary>
        public int duration { get; set; }

        /// <summary>
        /// Статус
        /// ('OK' - успех)
        /// </summary>
        public string status { get; set; }

        /// <summary>
        /// Параметрический конструктор класса YandexResponseItem
        /// </summary>
        /// <param name="arg_distance">Расстояние, метров</param>
        /// <param name="arg_duration">Время передвижения, секунд</param>
        /// <param name="arg_status">Статус</param>
        public YandexResponseItem(int arg_distance, int arg_duration, string arg_status)
        {
            distance = arg_distance;
            duration = arg_duration;
            status = arg_status;
        }
    }
}
