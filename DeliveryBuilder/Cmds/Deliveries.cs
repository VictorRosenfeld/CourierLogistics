
namespace DeliveryBuilder.Cmds
{
    using System;
    using System.ComponentModel;
    using System.Xml.Serialization;

    /// <summary>
    /// Отгрузки и рекомендации
    /// </summary>
    [Serializable()]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    [XmlRoot(ElementName = "deliveries", Namespace = "", IsNullable = false)]
    public class Deliveries
    {
        /// <summary>
        /// Список отгрузок или рекомендаций
        /// </summary>
        [XmlElement("delivery")]
        public Delivery[] DeliveryList { get; set; }

        ///// <remarks/>
        //[XmlText()]
        //public string[] Text { get; set; }

        /// <summary>
        /// ID сервиса логистики
        /// </summary>
        [XmlAttribute("service_id")]
        public int ServiceId { get; set; }
    }

    /// <summary>
    /// Отгрузка или рекомендация
    /// </summary>
    [Serializable()]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    public class Delivery
    {

        /// <summary>
        /// Альтеативные DServiceID
        /// </summary>
        [XmlArray("alternative_delivery_service")]
        [XmlArrayItem("service_id", IsNullable = false)]
        public int[] AlternativeDeliveryService { get; set; }

        /// <summary>
        /// Заказы отгузки
        /// </summary>
        [XmlArray("order", IsNullable = false)]
        [XmlArrayItem("order", IsNullable = false)]
        public DeliveryOrder[] Orders { get; set; }

        [XmlElement("node_info")]
        public DeliveryNodeInfo NodeInfo { get; set; }

        /// <summary>
        /// Время доставки заказа в отгрузке
        /// </summary>
        [XmlArray("node_delivery_time", IsNullable = false)]
        [XmlArrayItem("node", IsNullable = false)]
        public NodeDeliveryTime[] NodeTime { get; set; }

        /// <summary>
        /// Guid
        /// </summary>
        [XmlAttribute("guid")]
        public string Guid { get; set; }

        /// <summary>
        /// Статус (отгрузка или рекомендация)
        /// </summary>
        [XmlAttribute("status")]
        public int Status { get; set; }

        /// <summary>
        /// ID магазина
        /// </summary>
        [XmlAttribute("shop_id")]
        public int ShopId { get; set; }

        /// <summary>
        /// DServiceID
        /// </summary>
        [XmlAttribute("delivery_service_id")]
        public int DServiceId { get; set; }

        /// <summary>
        /// ID курьера
        /// </summary>
        [XmlAttribute("courier_id")]
        public int CourierId { get; set; }

        /// <summary>
        /// Время начала допустимого интервала старта
        /// </summary>
        [XmlAttribute("date_target")]
        public DateTime StartDeliveryInterval { get; set; }

        /// <summary>
        /// Время конца допустимого интервала старта
        /// </summary>
        [XmlAttribute("date_target_end")]
        public DateTime EndDeliveryInterval { get; set; }

        /// <summary>
        /// Условие вызвавшее отгрузку
        /// </summary>
        [XmlAttribute("cause")]
        public int Cause { get; set; }
    }

    /// <summary>
    /// Заказ в отгрузке
    /// </summary>
    [Serializable()]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    public class DeliveryOrder
    {
        /// <summary>
        /// Статус заказа
        /// </summary>
        [XmlAttribute("status")]
        public int status { get; set; }

        /// <summary>
        /// ID заказа
        /// </summary>
        [XmlAttribute("order_id")]
        public int OrderId { get; set; }
    }

    /// <summary>
    /// Информация об отгрузке
    /// </summary>
    [Serializable()]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    public class DeliveryNodeInfo
    {

        /// <summary>
        /// Расстояние и время перемещения между вершинами маршута
        /// </summary>
        [XmlElement("node")]
        public NodeInfo[] Nodes { get; set; }

        /// <summary>
        /// Время расчета
        /// </summary>
        [XmlAttribute("calc_time")]
        public DateTime СalcTime { get; set; }

        /// <summary>
        /// Время отгрузки 
        /// </summary>
        [XmlAttribute("cost")]
        public double Cost { get; set; }

        /// <summary>
        /// Вес всех заказов
        /// </summary>
        [XmlAttribute("weight")]
        public double Weight { get; set; }

        /// <summary>
        /// Флаг возврата в магазин
        /// </summary>
        [XmlAttribute("is_loop")]
        public bool IsLoop { get; set; }

        /// <summary>
        /// Время начала интервала старта
        /// </summary>
        [XmlAttribute("start_delivery_interval")]
        public DateTime StartDeliveryInterval { get; set; }

        /// <summary>
        /// Время конца интервала старта
        /// </summary>
        [XmlAttribute("end_delivery_interval")]
        public DateTime EndDeliveryInterval { get; set; }

        /// <summary>
        /// Резерв времени
        /// </summary>
        [XmlAttribute("reserve_time")]
        public double ReserveTime { get; set; }

        /// <summary>
        /// Время от старта до вручения последнего заказа
        /// </summary>
        [XmlAttribute("delivery_time")]
        public double DeliveryTime { get; set; }

        /// <summary>
        /// Общее время отгрузки 
        /// </summary>
        [XmlAttribute("execution_time")]
        public double ExecutionTime { get; set; }
    }

    /// <summary>
    /// Расстояние и время движения от предыдущей вершины
    /// </summary>
    [Serializable()]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    public class NodeInfo
    {
        /// <summary>
        /// Расстояние
        /// </summary>
        [XmlAttribute("distance")]
        public int Distance { get; set; }

        /// <summary>
        /// Время
        /// </summary>
        [XmlAttribute("duration")]
        public int duration { get; set; }
    }

    /// <summary>
    /// Время доставки заказа
    /// </summary>
    [Serializable()]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    public class NodeDeliveryTime
    {
        /// <summary>
        /// Время доставки заказа
        /// </summary>
        [XmlAttribute("delivery_time")]
        public double DeliveryTime { get; set; }
    }
}
