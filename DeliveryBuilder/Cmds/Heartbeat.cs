namespace DeliveryBuilder.Cmds
{
    using System;
    using System.ComponentModel;
    using System.Xml.Serialization;

    /// <summary>
    /// Команда 'Сердцебиение'
    /// </summary>
    [Serializable()]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    [XmlRoot(ElementName = "heartbeat",  Namespace = "", IsNullable = false)]
    public class Heartbeat
    {
        /// <summary>
        /// ID сервиса логистики
        /// </summary>
        [XmlAttribute("service_id")]
        public int ServiceId { get; set; }
    }
}
