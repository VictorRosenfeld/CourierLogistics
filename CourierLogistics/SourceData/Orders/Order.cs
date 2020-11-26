

namespace CourierLogistics.SourceData.Orders
{
    using System;

    /// <summary>
    /// Информация о заказе
    /// </summary>
    public class Order
    {
        // id_order;number; ShopNo;date_order;      date_supply;            source; date_collect_start; date_collected;date_delivery_start;date_delivered;date_closed;date_cancelled;id_courier;date_courier_set;date_completed;date_posted;    date_disassembled;date_check_closed;       id_delivery_job;address;                                            comment;latitude;           longitude;          ves
        // 3482217; 6479912;840;   28.06.2020 19:29;2020-06-28 00:00:00.000;2;      28.06.2020 19:29;   28.06.2020 18:55;   NULL;            NULL;           NULL;        NULL;         NULL;       NULL;          NULL;        28.06.2020 19:29;NULL;            2020-06-28 18:56:00.000; NULL;           ул Рыбная 1-я, д 63, г Сергиев Посад, Московская обл;      ;56.3036910000000000;38.1465450000000000;3.378000

        public int Id_order { get; set; }

        public int Number { get; set; }

        public int ShopNo { get; set; }

        public DateTime Date_order { get; set; }

        public DateTime Date_supply { get; set; }

        public int Source { get; set; }

        public DateTime Date_collect_start { get; set; }

        public DateTime Date_collected { get; set; }

        public DateTime Date_delivery_start { get; set; }

        public DateTime Date_delivered { get; set; }

        public DateTime Date_closed { get; set; }

        public DateTime Date_cancelled { get; set; }

        public int Id_courier { get; set; }

        public DateTime Date_courier_set { get; set; }

        public DateTime Date_completed { get; set; }

        public DateTime Date_posted { get; set; }

        public DateTime Date_disassembled { get; set; }

        public DateTime Date_check_closed { get; set; }

        public int Id_delivery_job { get; set; }

        public string Address { get; set; }

        public string Comment { get; set; }

        public double Latitude { get; set; }

        public double Longitude { get; set; }

        public double Weight { get; set; }

        /// <summary>
        /// Предельное время доставки
        /// </summary>
        public DateTime DeliveryTimeLimit { get; set; }

        /// <summary>
        /// Флаг: true - заказ выполнен; false - заказ не выполнен
        /// </summary>
        public bool Completed { get; set; }

        /// <summary>
        /// Предельное время доставки в срок
        /// </summary>
        /// <param name="deliveryDuration">Гарантированное время доставки от момента заказа с доставкой в тот же день</param>
        /// <returns>Предельное время</returns>
        public DateTime GetDeliveryLimit(double deliveryDuration = 120)
        {
            if (DeliveryTimeLimit != DateTime.MinValue)
                return DeliveryTimeLimit;

            if (Date_supply.Date > Date_order.Date)
            {
                return Date_supply.Date.AddHours(12);
            }
            else
            {
                if (Date_order.TimeOfDay <= TimeSpan.FromHours(10))
                {
                    return Date_order.Date.AddHours(12);
                }
                else
                {
                    DateTime timeLimit = Date_order.AddMinutes(deliveryDuration);
                    if (Date_delivered > timeLimit)
                        timeLimit = Date_delivered;
                    return timeLimit;
                }
            }
        }
    }
}
