using System;
using System.Collections.Generic;
using System.Text;

namespace SQLCLR
{
//    crsCourierID
//    crsVehicleID
//crsStatusID
//crsWorkStart
//crsWorkEnd
//crsLunchTimeStart
//crsLunchTimeEnd
//crsLastDeliveryStart
//crsLastDeliveryEnd
//crsOrderCount
//crsTotalDeliveryTime
//crsTotalCost
//crsAverageOrderCost
//crsIndex
//crsShopID
//crsLatitude
//crsLongitude

    /// <summary>
    /// Курьер
    /// </summary>
    internal class Courier
    {
        /// <summary>
        /// ID курьера
        /// </summary>
        public int Id { get; private set; }

        /// <summary>
        /// ID способа доставки
        /// </summary>
        public int VehicleId  { get; private set; }

    }
}
