
namespace SQLCLRex.Deliveries
{
    using System;

    /// <summary>
    /// Параметры построения маршрутов
    /// </summary>
    public class CalcConfig
    {
        /// <summary>
        /// ID сервиса логистики
        /// </summary>
        public int ServiceId { get; set; }

        /// <summary>
        /// Время расчета
        /// </summary>
        public DateTime CalcTime { get; set; }

        /// <summary>
        /// Имя сервера
        /// </summary>
        public string ServerName { get; set; }

        /// <summary>
        /// Имя БД
        /// </summary>
        public string DbName { get; set; }

        /// <summary>
        /// Радиус сгустка
        /// </summary>
        public int CloudRadius { get; set; }

        /// <summary>
        /// Максимальное число заказов в сгустке при уровне = 5 
        /// </summary>
        public int Cloud5Size { get; set; }

        /// <summary>
        /// Максимальное число заказов в сгустке при уровне = 6 
        /// </summary>
        public int Cloud6Size { get; set; }

        /// <summary>
        /// Максимальное число заказов в сгустке при уровне = 7 
        /// </summary>
        public int Cloud7Size { get; set; }

        /// <summary>
        /// Максимальное число заказов в сгустке при уровне = 8
        /// </summary>
        public int Cloud8Size { get; set; }

        /// <summary>
        /// Время начала работы построителя
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Время завершения работы построителя
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// Время работы, мсек
        /// </summary>
        public TimeSpan ElapsedTime => (EndTime - StartTime);

        /// <summary>
        /// Код возврата
        /// </summary>
        public int ExitCode { get; set; }
    }
}
