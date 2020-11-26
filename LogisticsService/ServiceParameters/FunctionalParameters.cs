
namespace LogisticsService.ServiceParameters
{
    /// <summary>
    /// Функциональные параметры сервиса
    /// </summary>
    public class FunctionalParameters
    {
        /// <summary>
        /// ID сервиса
        /// </summary>
        public int service_id { get; set; }

        /// <summary>
        /// Миниальное число рабочих часов курьера
        /// </summary>
        public double min_work_time { get; set; }

        /// <summary>
        /// Интервал времени до истечения отгрузки курьером, мин
        /// </summary>
        public double courier_alert_interval { get; set; }

        /// <summary>
        /// Интервал времени до истечения отгрузки на такси, мин
        /// </summary>
        public double taxi_alert_interval { get; set; }

        /// <summary>
        /// Интервал времени для отступа от
        /// предельного времени отгрузки в
        /// очереди предотвращения утечек, мин
        /// </summary>
        public double checking_alert_interval { get; set; }

        /// <summary>
        /// Максимальное число заказов среди которых
        /// ищется оптимальное
        /// </summary>
        public int max_orders_for_search_solution { get; set; }

        /// <summary>
        /// Максимальное число заказов в задаче коммивояжера
        /// </summary>
        public int max_orders_at_traveling_salesman_problem { get; set; }

        /// <summary>
        /// Интервал опроса сервера, мсек
        /// </summary>
        public int data_request_interval { get; set; }

        /// <summary>
        /// Интервал времени для выборки событий очереди при срабатывании таймера, мсек
        /// </summary>
        public int event_time_interval { get; set; }

        /// <summary>
        /// Максимальное число точек, для которых расчитываются координаты
        /// (число заказов + число магазинов, к которым они относятся)
        /// </summary>
        public int max_points_number { get; set; }

        /// <summary>
        /// Корневой URL
        /// </summary>
        public string api_root { get; set; }

        /// <summary>
        /// Ёмкость Geo-кэша
        /// (число хранимых попарных расстояний и времени движения)
        /// </summary>
        public int geo_cache_capacity { get; set; }

        /// <summary>
        /// Интервал времени информирования
        /// сервера о состоянии очереди отгрузок, мсек
        /// </summary>
        public int queue_Info_interval { get; set; }

        /// <summary>
        /// Максимальное число заказов для кадой глубины просчета
        /// </summary>
        public SalesmanProblemLevel[] salesman_problem_levels { get; set; }
    }
}
