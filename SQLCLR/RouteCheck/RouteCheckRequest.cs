
namespace SQLCLR.RouteCheck
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Text;
    using System.Xml.Serialization;

    /// <summary>
    /// Запрос на проверку маршруту
    /// </summary>
    [Serializable()]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    [XmlRoot(ElementName = "request", Namespace = "", IsNullable = false)]
    public class RootCheckRequest
    {
        /// <summary>
        /// Данные магазина
        /// </summary>
        public ShopData shop { get; set; }

        /// <summary>
        /// Данные заказов
        /// </summary>
        [XmlArrayItem("order", IsNullable = false)]
        public OrderData[] orders { get; set; }

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
        /// Время расчета
        /// </summary>
        [XmlAttribute()]
        public DateTime calc_time { get; set; }

        /// <summary>
        /// Флаг: true - построить отгрузку с минимальной стоимостью; false - проверить заданный маршрут
        /// </summary>
        [XmlAttribute()]
        public bool optimized { get; set; }
    }

    /// <summary>
    /// Данные магазина
    /// </summary>
    [Serializable()]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    public class ShopData
    {
        /// <summary>
        /// ID магазина
        /// </summary>
        [XmlAttribute()]
        public int shop_id { get; set; }
        
        /// <summary>
        /// Широта магазина
        /// </summary>
        [XmlAttribute()]
        public double lat { get; set; }

        /// <summary>
        /// Долгота магазина
        /// </summary>
        [XmlAttribute()]
        public double lon { get; set; }
    }

    /// <summary>
    /// Данные заказа
    /// </summary>
    [Serializable()]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    public class OrderData
    {
        /// <summary>
        /// ID заказа
        /// </summary>
        [XmlAttribute()]
        public int order_id { get; set; }

        /// <summary>
        /// dservice_id
        /// </summary>
        [XmlAttribute()]
        public int desrvice_id { get; set; }

        /// <summary>
        /// Вес заказа
        /// </summary>
        [XmlAttribute()]
        public double weight { get; set; }

        /// <summary>
        /// Широта заказа
        /// </summary>
        [XmlAttribute()]
        public double lat { get; set; }

        /// <summary>
        /// Долгота заказа
        /// </summary>
        [XmlAttribute()]
        public double lon { get; set; }

        /// <summary>
        /// Время начала интервала доставки
        /// </summary>
        [XmlAttribute()]
        public DateTime time_from { get; set; }

        /// <summary>
        /// Время конца интервала доставки
        /// </summary>
        [XmlAttribute()]
        public DateTime time_to { get; set; }
    }
}
