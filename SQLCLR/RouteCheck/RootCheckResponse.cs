
namespace SQLCLR.RouteCheck
{
    using System;
    using System.ComponentModel;
    using System.Xml.Serialization;

    /// <summary>
    /// Отклик с данными проверки маршрута
    /// </summary>
    [Serializable()]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    [XmlRoot(ElementName = "response",  Namespace = "", IsNullable = false)]
    public class RootCheckResponse
    {
        /// <summary>
        /// Общие данные об отгрузке
        /// </summary>
        public DeliveryData delivery { get; set; }

        /// <summary>
        /// ID запроса
        /// </summary>
        [XmlAttribute()]
        public int request_id { get; set; }

        /// <summary>
        /// ID сервиса логистики
        /// </summary>
        [XmlAttribute()]
        public int service_id { get; set; }

        /// <summary>
        /// Время расчета отгрузки
        /// </summary>
        [XmlAttribute()]
        public DateTime calc_time { get; set; }

        /// <summary>
        /// Флаг: true - построить отгрузку с минимальной стоимостью; false - проверить заданный маршрут
        /// </summary>
        [XmlAttribute()]
        public bool optimized { get; set; }

        /// <summary>
        /// Код ошибки
        /// </summary>
        [XmlAttribute()]
        public int code { get; set; }
    }

    /// <summary>
    /// Данные отгрузки
    /// </summary>
    [Serializable()]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    public class DeliveryData
    {
        /// <summary>
        /// Альтернативные dservice_id
        /// </summary>
        [XmlArray()]
        [XmlArrayItem("service_id", IsNullable = false)]
        public int[] alternative_delivery_service { get; set; }

        /// <summary>
        /// Заказы отгрузки в порядке доставки
        /// </summary>
        [XmlArrayItem("order", IsNullable = false)]
        public OrderSeq[] orders { get; set; }

        /// <summary>
        /// Гео-данные об узлах доставки
        /// </summary>
        [XmlArrayItem("node", IsNullable = false)]
        public DeliveryNode[] node_info { get; set; }

        /// <summary>
        /// Время от начала отгрузки до вручкения заказа
        /// </summary>
        [XmlArrayItem("node", IsNullable = false)]
        public DeliveryTime[] node_delivery_time { get; set; }

        /// <summary>
        /// ID магазина
        /// </summary>
        [XmlAttribute()]
        public int shop_id { get; set; }

        /// <summary>
        /// dservice_id способа отгрузки
        /// </summary>
        [XmlAttribute()]
        public int dservice_id { get; set; }

        /// <summary>
        /// ID рефересного курьера
        /// </summary>
        [XmlAttribute()]
        public int courier_id { get; set; }

        /// <summary>
        /// Начало интервала начала отгрузки
        /// </summary>
        [XmlAttribute()]
        public DateTime start_delivery_interval { get; set; }

        /// <summary>
        /// Конец интервала начала отгрузки
        /// </summary>
        [XmlAttribute()]
        public DateTime end_delivery_interval { get; set; }

        /// <summary>
        /// Общий вес всех заказов
        /// </summary>
        [XmlAttribute()]
        public double weight { get; set; }

        /// <summary>
        /// Флаг: true - возврат в магазин; false - отгрузка завершается в точке последнего вручения
        /// </summary>
        [XmlAttribute()]
        public bool is_loop { get; set; }

        /// <summary>
        /// Резерв времени для старта отгрузки
        /// </summary>
        [XmlAttribute()]
        public double reserve_time { get; set; }

        /// <summary>
        /// Время от начала отгрузки до вручения последнего заказа
        /// </summary>
        [XmlAttribute()]
        public double delivery_time { get; set; }
      
        /// <summary>
        /// Время выполнения отгрузки
        /// </summary>
        [XmlAttribute()]
        public double execution_time { get; set; }
    }

    /// <summary>
    /// ID заказа
    /// </summary>
    [Serializable()]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    public class OrderSeq
    {
        /// <summary>
        /// ID заказа
        /// </summary>
        [XmlAttribute()]
        public int order_id { get; set; }
    }

    /// <summary>
    /// Гео-данные узлов маршрута
    /// </summary>
    [Serializable()]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    public class DeliveryNode
    {
        /// <summary>
        /// Расстояние между точками в порядке следования
        /// </summary>
        [XmlAttribute()]
        public int distance { get; set; }

        /// <summary>
        /// Время движения от точки к точке
        /// </summary>
        [XmlAttribute()]
        public int duration { get; set; }
    }

    /// <summary>
    /// Время доставки от начала отгрузки до точки вручения
    /// </summary>
    [Serializable()]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    public class DeliveryTime
    {
        /// <summary>
        /// Время доставки от начала отгрузки до точки вручения
        /// </summary>
        [XmlAttribute()]
        public double delivery_time { get; set; }
    }
}
