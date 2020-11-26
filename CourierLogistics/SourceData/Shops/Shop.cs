

namespace CourierLogistics.SourceData.Shops
{
    using System;

    /// <summary>
    /// Способы доставки заказов
    /// </summary>
    [Flags]
    public enum DeliveryType
    {
        /// <summary>
        /// Тип  не определен
        /// </summary>
        None = 0x00,

        /// <summary>
        /// Пешком
        /// </summary>
        OnFoot = 0x01,

        /// <summary>
        /// На велосипеде
        /// </summary>
        Bicycle = 0x2,

        /// <summary>
        /// На авто
        /// </summary>
        Car = 0x4,
    }

    public class Shop
    {
        public int N { get; set; }

        public string Name_TT { get; set; }

        public int Id_TT { get; set; }

        public int IsActive { get; set; }

        public int TT_Format { get; set; }

        public int Id_Group { get; set; }

        public double Latitude { get; set; }

        public double Longitude { get; set; }

        public string Address { get; set; }

        public string Hours { get; set; }

        public string Status { get; set; }

        public string Region_TT { get; set; }

        public int Id_Region_TT { get; set; }

        public int Gettype { get; set; }

        public DeliveryType DeliveryTypes { get; set; }
    }
}
