
namespace DeliveryBuilder.Cmds
{
    using System;
    using System.ComponentModel;
    using System.Xml.Serialization;

    /// <summary>
    /// Команда отмены
    /// </summary>
    [Serializable()]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    [XmlRoot(ElementName = "rejections",  Namespace = "", IsNullable = false)]
    public class Rejections
    {
        /// <summary>
        /// Отклоненные заказы
        /// </summary>
        [XmlElement("rejection")]
        public RejectedOrder[] RejectedOrders { get; set; }

        /// <summary>
        /// ID сервиса логистики
        /// </summary>
        [XmlAttribute("service_id")]
        public int ServiceId { get; set; }
    }

    /// <summary>
    /// Отклоненный заказ
    /// </summary>
    [Serializable()]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    public class RejectedOrder
    {
        /// <summary>
        /// ID заказа
        /// </summary>
        [XmlAttribute("id")]
        public int Id { get; set; }

        /// <summary>
        /// VehicleID
        /// </summary>
        [XmlAttribute("type_id")]
        public int VehicleId { get; set; }

        /// <summary>
        /// Код причины
        /// </summary>
        [XmlAttribute("reason")]
        public int Reason { get; set; }

        /// <summary>
        /// Дополнительный код ошибки
        /// </summary>
        [XmlAttribute("error_code")]
        public int ErrorCode { get; set; }
    }
}
