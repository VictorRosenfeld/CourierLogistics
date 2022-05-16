
namespace DeliveryBuilder.Cmds
{
    using System;
    using System.ComponentModel;
    using System.Xml.Serialization;

    /// <summary>
    /// Команда 'Запрос данных'
    /// </summary>
    [Serializable()]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    [XmlRoot(ElementName = "data", Namespace = "", IsNullable = false)]
    public class DataRequest
    {
        /// <summary>
        /// ID сервиса логистики
        /// </summary>
        [XmlAttribute("service_id")]
        public int ServiceId { get; set; }

        /// <summary>
        /// Флаг All
        /// </summary>
        [XmlAttribute("all")]
        public byte All { get; set; }

        /// <summary>
        /// Conversation_Group_ID для сообщения с данными
        /// </summary>
        [XmlAttribute("group_id")]
        public string GroupId { get; set; }
    }
}
