
namespace LogisticsService.ServiceParameters
{
    /// <summary>
    /// Максимальное число заказов для просчета с заданным уровнем
    /// </summary>
    public class SalesmanProblemLevel
    {
        /// <summary>
        /// Максимальное число заказов в отгрузке
        /// </summary>
        public int level { get; set; }

        /// <summary>
        /// Максимальное число закзазов для просчета с уровнем level
        /// </summary>
        public int to_orders { get; set; }
    }
}
