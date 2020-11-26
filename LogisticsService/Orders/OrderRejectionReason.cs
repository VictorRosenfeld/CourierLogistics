
namespace LogisticsService.Orders
{
    /// <summary>
    /// Причины отказа доставки в срок
    /// </summary>
    public enum OrderRejectionReason
    {
        None = 0,
        LateAssembled = 1,
        CourierNa = 2,
        Overweight = 3,
        Overdistance = 4,
        LateStart = 5,
        ToTimeIsSmall = 6
    }
}
