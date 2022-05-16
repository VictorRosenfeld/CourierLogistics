
namespace DeliveryBuilder.Couriers
{
    using System;
    using System.ComponentModel;
    using System.Xml.Serialization;

    /// <summary>
    /// Обовления курьров сервиса логистики
    /// </summary>
    [Serializable()]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    [XmlRoot(ElementName ="data",  Namespace = "", IsNullable = false)]
    public class CouriersUpdates
    {
        /// <summary>
        /// Обновления курьеров
        /// </summary>
        [XmlArray("couriers", IsNullable = false)]
        [XmlArrayItem("courier", IsNullable = false)]
        public CourierUpdates[] Updates { get; set; }

        /// <summary>
        /// ID сервиса логистики
        /// </summary>
        [XmlAttribute("service_id")]
        public int ServiceId { get; set; }
    }

    /// <summary>
    /// Обновления курьера
    /// </summary>
    [Serializable()]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    public class CourierUpdates
    {
        /// <summary>
        /// ID курьера
        /// </summary>
        [XmlAttribute("courier_id")]
        public int CourierId { get; set; }

        /// <summary>
        /// Тип способа доставки
        /// </summary>
        [XmlAttribute("courier_type")]
        public int CourierType { get; set; }

        /// <summary>
        /// Состояние курьера
        /// </summary>
        [XmlAttribute("type")]
        public int Status { get; set; }

        /// <summary>
        /// Время начала работы
        /// </summary>
        [XmlAttribute("work_start")]
        public DateTime WorkStart { get; set; }

        /// <summary>
        /// Время конца работы
        /// </summary>
        [XmlAttribute("work_end")]
        public DateTime WorkEnd { get; set; }

        /// <summary>
        /// ID магазина
        /// </summary>
        [XmlAttribute("shop_id")]
        public int ShopId { get; set; }

        /// <summary>
        /// Широта
        /// </summary>
        [XmlAttribute("geo_lat")]
        public double Latitude { get; set; }

        /// <summary>
        /// Долгота
        /// </summary>
        [XmlAttribute("geo_lon")]
        public double Longitude { get; set; }

        /// <summary>
        /// Время наступления события
        /// </summary>
        [XmlAttribute("date_event")]
        public DateTime EventTime { get; set; }
    }
}
