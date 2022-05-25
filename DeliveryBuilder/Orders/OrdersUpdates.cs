
namespace DeliveryBuilder.Orders
{
    using System;
    using System.ComponentModel;
    using System.Xml.Serialization;

    /// <summary>
    /// Обновления заказов сервиса логистики
    /// </summary>
    [Serializable()]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    [XmlRoot(ElementName = "data", Namespace = "", IsNullable = false)]
    public class OrdersUpdates
    {
        /// <summary>
        /// Обновления заказов
        /// </summary>
        [XmlArray("orders", IsNullable = true)]
        [XmlArrayItem("order", IsNullable = false)]
        public OrderUpdates[] Updates { get; set; }

        /// <remarks/>
        /// <summary>
        /// ID сервиса логистики
        /// </summary>
        [XmlAttribute("service_id")]
        public int ServiceId { get; set; }
    }

    /// <summary>
    /// Обновления заказа
    /// </summary>
    [Serializable()]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    public class OrderUpdates
    {
        /// <summary>
        /// Список досупных DServiceID для доставки заказа
        /// </summary>
        [XmlArray("services", IsNullable = false)]
        [XmlArrayItem("service", IsNullable = false)]
        public DService[] DServices { get; set; }

        /// <summary>
        /// ID заказа
        /// </summary>
        [XmlAttribute("order_id")]
        public int OrderId { get; set; }

        /// <summary>
        /// Статус заказа
        /// </summary>
        [XmlAttribute("type")]
        public int Status { get; set; }

        /// <summary>
        /// Магазин заказа
        /// </summary>
        [XmlAttribute("shop_id")]
        public int ShopId { get; set; }

        /// <summary>
        /// Широта магазина
        /// </summary>
        [XmlAttribute("shop_geo_lat")]
        public double ShopLatitude { get; set; }

        /// <summary>
        /// Долгота магазина
        /// </summary>
        [XmlAttribute("shop_geo_lon")]
        public double ShopLongitude { get; set; }

        /// <summary>
        /// Широта заказа
        /// </summary>
        [XmlAttribute("geo_lat")]
        public double Latitude { get; set; }

        /// <summary>
        /// Долота заказа
        /// </summary>
        [XmlAttribute("geo_lon")]
        public double Longitude { get; set; }

        /// <summary>
        /// Вес заказа
        /// </summary>
        [XmlAttribute("weight")]
        public double Weight { get; set; }

        /// <summary>
        /// Приоритет
        /// </summary>
        [XmlAttribute("priority")]
        public int Priority { get; set; }

        /// <summary>
        /// Флаг: true - игнорировать окно доставки; false - учитывать окно доставки
        /// </summary>
        [XmlAttribute("time_check_disabled")]
        public bool TimeCheckDisabled { get; set; }

        /// <summary>
        /// Начало временного окна доставки
        /// </summary>
        [XmlAttribute("delivery_frame_from")]
        public DateTime TimeFrom { get; set; }

        /// <summary>
        /// Конец временного окна доставки
        /// </summary>
        [XmlAttribute("delivery_frame_to")]
        public DateTime TimeTo { get; set; }

        /// <summary>
        /// Время наступления события
        /// </summary>
        [XmlAttribute("date_event")]
        public DateTime EventTime { get; set; }
    }

    /// <summary>
    /// DService
    /// </summary>
    [Serializable()]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    public class DService
    {
        /// <summary>
        /// ID магазина
        /// </summary>
        [XmlAttribute("shop_id")]
        public int ShopId { get; set; }

        /// <summary>
        /// ID DService
        /// </summary>
        [XmlAttribute("dservice_id")]
        public byte DServiceId { get; set; }
    }
}
