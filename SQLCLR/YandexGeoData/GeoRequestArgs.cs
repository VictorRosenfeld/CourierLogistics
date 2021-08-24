
namespace SQLCLR.YandexGeoData
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Text;
    using System.Xml.Serialization;

    /// <summary>
    /// Аргументы гео-запроса Yandex
    /// </summary>
    [Serializable()]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    [XmlRoot(ElementName = "yandex",  Namespace = "", IsNullable = false)]
    public class GeoRequestArgs
    {
        /// <summary>
        /// Способы передвижения
        /// </summary>
        [XmlArrayItem("mode", IsNullable = false)]
        public string[] modes { get; set; }

        /// <summary>
        /// Координаты исходных точек
        /// </summary>
        [XmlArrayItem("point", IsNullable = false)]
        public GeoPoint[] origins { get; set; }

        /// <summary>
        /// Координаты точек назначения
        /// </summary>
        [XmlArrayItem("point", IsNullable = false)]
        public GeoPoint[] destinations { get; set; }

        /// <summary>
        /// Флаг: true - задан способ передвижения 'driving'
        /// </summary>
        [XmlIgnore]
        public bool HasDriving { get; set; }

        /// <summary>
        /// Флаг: true - задан способ передвижения 'walking'
        /// </summary>
        [XmlIgnore]
        public bool HasWalking { get; set; }

        /// <summary>
        /// Флаг: true - задан способ передвижения 'transit'
        /// </summary>
        [XmlIgnore]
        public bool HasTransit { get; set; }

        /// <summary>
        /// Флаг: true - задан способ передвижения 'truck'
        /// </summary>
        [XmlIgnore]
        public bool HasTruck { get; set; }

        /// <summary>
        /// Флаг: true - задан способ передвижения 'cycling'
        /// </summary>
        [XmlIgnore]
        public bool HasCycling { get; set; }

        /// <summary>
        /// Индекс mode = driving в geoData
        /// </summary>
        [XmlIgnore]
        public int DrivingIndex { get; set;  }

        /// <summary>
        /// Индекс mode = cycling в geoData
        /// </summary>
        [XmlIgnore]
        public int CyclingIndex { get; set;  }

        /// <summary>
        /// Индекс mode = walking в geoData
        /// </summary>
        [XmlIgnore]
        public int WalkingIndex { get; set;  }

        /// <summary>
        /// Индекс mode = truck в geoData
        /// </summary>
        [XmlIgnore]
        public int TruckIndex { get; set;  }

        /// <summary>
        /// Индекс mode = transit в geoData
        /// </summary>
        [XmlIgnore]
        public int TransitIndex { get; set;  }
    }

    /// <summary>
    /// Координаты точки
    /// </summary>
    [Serializable()]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    public class GeoPoint
    {
        /// <summary>
        /// Широта
        /// </summary>
        [XmlAttribute()]
        public double lat { get; set; }

        /// <summary>
        /// Долгота
        /// </summary>
        [XmlAttribute()]
        public double lon { get; set; }
    }
}
