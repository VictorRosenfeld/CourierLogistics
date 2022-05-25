
namespace DeliveryBuilder.Shops
{
    using System;
    using System.ComponentModel;
    using System.Xml.Serialization;

    /// <summary>
    /// Обновления магазинов сервиса логистики
    /// </summary>
    [Serializable()]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    [XmlRoot(ElementName = "data", Namespace = "", IsNullable = false)]
    public class ShopsUpdates
    {
        /// <summary>
        /// Обновления магазинв
        /// </summary>
        [XmlArray("shops", IsNullable = true)]
        [XmlArrayItem("shop", IsNullable = false)]
        public ShopUpdates[] Updates { get; set; }

        /// <summary>
        /// ID сервиса логистики
        /// </summary>
        [XmlAttribute("service_id")]
        public int ServiceId { get; set; }
    }

    /// <summary>
    /// Обновления магазина
    /// </summary>
    [Serializable()]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    public class ShopUpdates
    {
        /// <summary>
        /// ID магазина
        /// </summary>
        [XmlAttribute("shop_id")]
        public int ShopId { get; set; }

        /// <summary>
        /// Начало работы магазина
        /// </summary>
        [XmlAttribute("work_start")]
        public DateTime WorkStart { get; set; }

        /// <summary>
        /// Конец работы магазина
        /// </summary>
        [XmlAttribute("work_end")]
        public DateTime WorkEnd { get; set; }

        /// <summary>
        /// Широта магазина
        /// </summary>
        [XmlAttribute("geo_lat")]
        public double Latitude { get; set; }

        /// <summary>
        /// Долгота магазина
        /// </summary>
        [XmlAttribute("geo_lon")]
        public double Longitude { get; set; }

        /// <summary>
        /// Время события
        /// </summary>
        [XmlAttribute("date_event")]
        public DateTime EventTime { get; set; }
    }
}
