
namespace DeliveryBuilder.BuilderParameters
{
    using System;
    using System.ComponentModel;
    using System.Xml.Serialization;

    /// <summary>
    /// Параметры построителя отгрузок 
    /// </summary>
    [Serializable()]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    [XmlRoot(ElementName = "service", Namespace = "", IsNullable = false)]
    public class BuilderConfig
    {
        /// <summary>
        /// Параметры логгера
        /// </summary>
        [XmlElement("log")]
        public LogParameters LoggerParameters { get; set; }

        /// <summary>
        /// Фукциональные параметры
        /// </summary>
        [XmlElement("functional_parameters")]
        public FunctionalParameters Parameters { get; set; }

        /// <summary>
        /// Предельное число заказов для разных глубин
        /// при полном переборе
        /// </summary>
        [XmlArray("salesman_levels", IsNullable = false)]
        [XmlArrayItem("salesman_level", IsNullable = false)]
        public SalesmanLevel[] SalesmanLevels { get; set; }

        /// <summary>
        /// Параметры сгустков
        /// </summary>
        [XmlElement("cloud_parameters")]
        public CloudParameters Cloud { get; set; }

        /// <summary>
        /// ID сервиса логистики
        /// </summary>
        [XmlAttribute("id")]
        public int Id { get; set; }

        /// <summary>
        /// Наименование сервиса логистики
        /// </summary>
        [XmlAttribute("name")]
        public string Name { get; set; }

        ///// <summary>
        ///// Параметр Conversation_group_id в
        ///// T-SQL инструкции Begin Dialog
        ///// </summary>
        //[XmlAttribute("group_id")]
        //public string GroupId { get; set; }

        /// <summary>
        /// Описание
        /// (до 256 символов)
        /// </summary>
        [XmlAttribute("description")]
        public string Description { get; set; }
    }

    /// <summary>
    /// Функциональные параметры
    /// </summary>
    [Serializable()]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    public class FunctionalParameters
    {
        /// <summary>
        /// Отступ от максимального времени старта отгрузки
        /// с доставкой вовремя для курьера, мин
        /// </summary>
        [XmlElement("courier_delivery_margin")]
        public int CourierDeliveryMargin { get; set; }

        /// <summary>
        /// Отступ от максимального времени старта отгрузки
        /// с доставкой вовремя для такси, мин
        /// </summary>
        [XmlElement("taxi_delivery_margin")]
        public int TaxiDeliveryMargin { get; set; }

        /// <summary>
        /// Отступ от максимального времени старта отгрузки
        /// с доставкой вовремя для проверки утечек, мин
        /// </summary>
        [XmlElement("checking_margin")]
        public int СheckingMargin { get; set; }

        /// <remarks/>
        /// <summary>
        /// Интервалов тиков таймера, мсек
        /// </summary>
        [XmlElement("tick_interval")]
        public int TickInterval { get; set; }

        /// <summary>
        /// Интервал сердцебиений
        /// в тиках таймера
        /// </summary>
        [XmlElement("heartbeat_interval")]
        public int HeartbeatInterval { get; set; }

        /// <summary>
        /// Интервал пересчета отгрузок
        /// в тиках таймера
        /// </summary>
        [XmlElement("recalc_interval")]
        public int RecalcInterval { get; set; }

        /// <summary>
        /// Интервал выдачи команд на отгрузку
        /// из очереди в тиках таймера
        /// </summary>
        [XmlElement("queue_interval")]
        public int QueueInterval { get; set; }

        /// <summary>
        /// Интервал упреждающего захвата отгрузок
        /// из очереди при выдаче команд а отгрузку в тиках таймера
        /// </summary>
        [XmlElement("queue_catching_interval")]
        public int QueueCatchingInterval { get; set; }

        /// <summary>
        /// Параметры гео-кэша
        /// </summary>
        [XmlElement("geo_cache")]
        public GeoCacheParameters GeoCache { get; set; }

        /// <summary>
        /// Параметры запроса гео-данных Yandex
        /// </summary>
        [XmlElement("geo_yandex")]
        public GeoYandexParameters GeoYandex { get; set; }

        /// <summary>
        /// Условия старта отгрузок
        /// </summary>
        [XmlElement("delivery_start_condition")]
        public DeliveryStartCondition StartCondition { get; set; }

        /// <remarks/>
        /// <summary>
        /// Параметры работы со службами Service Broker
        /// во внешней БД
        /// </summary>
        [XmlElement("external_db")]
        public ExternalDbParameters ExternalDb { get; set; }
    }

    /// <summary>
    /// Параметры логгера
    /// </summary>
    [Serializable()]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    public class LogParameters
    {
        /// <summary>
        /// Путь к файлу лога
        /// </summary>
        [XmlAttribute("file")]
        public string Filename { get; set; }

        /// <summary>
        /// Число дней хранения лога
        /// </summary>
        [XmlAttribute("save_days")]
        public int SaveDays { get; set; }
    }

    /// <summary>
    /// Параметры гео-кэша
    /// </summary>
    [Serializable()]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    public class GeoCacheParameters
    {
        /// <summary>
        /// Максимальное число хранимых пар
        /// расстояий в гео-кэше для всех
        /// способов передвижения
        /// </summary>
        [XmlAttribute("capacity")]
        public int Capacity { get; set; }

        /// <summary>
        /// Флаг восстановления гео-кэша из
        /// ранее сохраненного файла
        /// </summary>
        [XmlAttribute("restored")]
        public bool Restored { get; set; }

        /// <summary>
        /// Интервал актуальности даных
        /// в гео-кэше, мин
        /// </summary>
        [XmlAttribute("saving_interval")]
        public int SavingInterval { get; set; }
       
        /// <summary>
        /// Интервал проверки актуальности
        /// данных в гео-кэше в тиках таймера
        /// </summary>
        [XmlAttribute("check_interval")]
        public int CheckInterval { get; set; }

        /// <summary>
        /// Конструктор класса GeoCacheParameters
        /// </summary>
        public GeoCacheParameters()
        {
            Capacity = 50000;
            SavingInterval = 120;
            CheckInterval = 10;
        }
    }

    /// <summary>
    /// Параметры запроса гео-данных Yandex
    /// </summary>
    [Serializable()]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    public class GeoYandexParameters
    {
        /// <summary>
        /// Соответствия типов способов доставки типам Yandex
        /// </summary>
        [XmlElement("type_name")]
        public YandexTypeName[] TypeNames { get; set; }

        /// <summary>
        /// Кличество способов передвижеия Yandex
        /// </summary>
        [XmlIgnore()]
        public int TypeNameCount => (TypeNames == null ? 0 : TypeNames.Length);

        /// <summary>
        /// GET-запрос к Yandex
        /// </summary>
        [XmlAttribute("url")]
        public string Url { get; set; }

        /// <summary>
        /// Предельное число пар точек в одном запросе
        /// </summary>
        [XmlAttribute("pair_limit")]
        public int PairLimit { get; set; }

        /// <summary>
        /// API Key
        /// </summary>
        [XmlAttribute("api_key")]
        public string ApiKey { get; set; }

        /// <summary>
        /// Timeout для ожидания отклика, мсек 
        /// </summary>
        [XmlAttribute("timeout")]
        public int Timeout { get; set; }

        /// <summary>
        /// Коэффициент пересчета времени доставки
        /// велокурьера из времени пешего курьера
        /// </summary>
        [XmlAttribute("cycling_ratio")]
        public double CyclingRatio { get; set; }
    }

    /// <summary>
    /// Соответствие кода способа доставки
    /// наименованию спсособа передвижения Yandex
    /// </summary>
    [Serializable()]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    public class YandexTypeName
    {
        /// <summary>
        /// Код способа доставки
        /// </summary>
        [XmlAttribute("id")]
        public int Id { get; set; }

        /// <summary>
        /// Наименование Yandex
        /// </summary>
        [XmlAttribute("name")]
        public string Name { get; set; }
    }

    /// <summary>
    /// Условия старта отгрузки
    /// </summary>
    [Serializable()]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    public class DeliveryStartCondition
    {
        /// <summary>
        /// Флаг отгрузки при достижении
        /// максимального числа заказов для способа доставки
        /// </summary>
        [XmlAttribute("shipment_trigger")]
        public bool ShipmentTrigger { get; set; }

        /// <summary>
        /// Флаг старта отгрузки, если
        /// средняя стоимость отгрузки
        /// меньше порогового значения для
        /// данного магазина и способа доставки
        /// </summary>
        [XmlAttribute("average_cost")]
        public bool AverageCost { get; set; }
    }

    /// <summary>
    /// Пааметры работы со службами Service Broker
    /// во вешей БД
    /// </summary>
    [Serializable()]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    public class ExternalDbParameters
    {
        /// <summary>
        /// Строка подключения к внешней БД
        /// </summary>
        [XmlAttribute("connection_string")]
        public string СonnectionString { get; set; }

        /// <summary>
        /// Timeout для ожидания подключения, сек
        /// </summary>
        [XmlAttribute("connection_timeout")]
        public int ConnectionTimeout { get; set; }

        /// <summary>
        /// Параметры службы CmdService
        /// </summary>
        [XmlElement("cmd_service")]
        public CmdServiceParameters CmdService { get; set; }
        
        /// <summary>
        /// Параметры службы DataService
        /// </summary>
        [XmlElement("data_service")]
        public DataServiceParameters DataService { get; set; }
    }

    /// <summary>
    /// Параметры для работы со службой CmdService
    /// </summary>
    [Serializable()]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    public class CmdServiceParameters
    {
        /// <summary>
        /// Наименование службы
        /// </summary>
        [XmlAttribute("name")]
        public string Name { get; set; }

        /// <summary>
        /// Наименование контракта для передачи команд
        /// </summary>
        [XmlAttribute("contract")]
        public string Сontract { get; set; }

        /// <summary>
        /// Message Type Cmd1
        /// </summary>
        [XmlAttribute("cmd1_message_type")]
        public string Cmd1MessageType { get; set; }

        /// <summary>
        /// Message Type Cmd2
        /// </summary>
        [XmlAttribute("cmd2_message_type")]
        public string Cmd2MessageType { get; set; }

        /// <summary>
        /// Message Type Cmd3
        /// </summary>
        [XmlAttribute("cmd3_message_type")]
        public string Cmd3MessageType { get; set; }

        /// <summary>
        /// Message Type Heartbeat
        /// </summary>
        [XmlAttribute("heartbeat_message_type")]
        public string HeartbeatMessageType { get; set; }

        /// <summary>
        /// Message Type Data
        /// </summary>
        [XmlAttribute("data_message_type")]
        public string DataMessageType { get; set; }
    }

    /// <summary>
    /// Параметры для работы со службой DataService
    /// </summary>
    [Serializable()]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    public class DataServiceParameters
    {
        /// <summary>
        /// Имя очереди службы
        /// </summary>
        [XmlAttribute("queue")]
        public string QueueName { get; set; }

        /// <summary>
        /// Timeout в команде Receive
        /// для ожидания появления сообщеий в очереди, мсек
        /// </summary>
        [XmlAttribute("receive_timeout")]
        public int ReceiveTimeout { get; set; }

        /// <summary>
        /// Message Type для сообщений с данными о курьерах
        /// </summary>
        [XmlAttribute("courier_mesage_type")]
        public string CourierMesageType { get; set; }

        /// <summary>
        /// Message Type для сообщений с данными о заказах
        /// </summary>
        [XmlAttribute("order_mesage_type")]
        public string OrderMesageType { get; set; }

        /// <summary>
        /// Message Type для сообщений с данными о магазинах
        /// </summary>
        [XmlAttribute("shop_mesage_type")]
        public string ShopMesageType { get; set; }
    }

    /// <summary>
    /// Максимальное число заказов
    /// при полном переборе для заданной глубины
    /// </summary>
    [Serializable()]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    public class SalesmanLevel
    {
        /// <summary>
        /// Глубина
        /// </summary>
        [XmlAttribute("level")]
        public int Level { get; set; }

        /// <summary>
        /// Максимальное число заказов
        /// </summary>
        [XmlAttribute("orders")]
        public int Orders { get; set; }
    }

    /// <summary>
    /// Параметры сгустков (облака точек)
    /// </summary>
    [Serializable()]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    public class CloudParameters
    {
        /// <summary>
        /// Радиус облака, метров
        /// </summary>
        [XmlAttribute("radius")]
        public int Radius { get; set; }

        /// <summary>
        /// Интервал расширения окна доставки влево и вправо, мин
        /// </summary>
        [XmlAttribute("delta")]
        public int Delta { get; set; }

        /// <summary>
        /// Максимальное число заказов в облаке при построении отгрузок с глубиной 5
        /// </summary>
        [XmlAttribute("size5")]
        public int Size5 { get; set; }

        /// <summary>
        /// Максимальное число заказов в облаке при построении отгрузок с глубиной 6
        /// </summary>
        [XmlAttribute("size6")]
        public int Size6 { get; set; }

        /// <summary>
        /// Максимальное число заказов в облаке при построении отгрузок с глубиной 7
        /// </summary>
        [XmlAttribute("size7")]
        public int Size7 { get; set; }

        /// <summary>
        /// Максимальное число заказов в облаке при построении отгрузок с глубиной 8
        /// </summary>
        [XmlAttribute("size8")]
        public int Size8 { get; set; }
    }
}
