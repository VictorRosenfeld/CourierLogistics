
namespace SQLCLR.Deliveries
{
    using System;
    using System.ComponentModel;
    using System.Xml.Serialization;

    /// <summary>
    /// ������ � Geo-�������
    /// </summary>
    [Serializable()]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    [XmlRoot(Namespace = "", IsNullable = false)]
    public class geo_data
    {
        /// <summary>
        /// Geo-������ ��� ��� �����
        /// </summary>
        [XmlElement("p")]
        public GeoDataElement[] Elements { get; set; }

        /// <summary>
        /// ����� Geo-��������� ������
        /// </summary>
        public int Count => (Elements == null ? 0 : Elements.Length);

        /// <summary>
        /// ��������� ������ � �������
        /// </summary>
        /// <param name="pointCount">���������� �����</param>
        /// <returns>Geo-������ ��� null</returns>
        public Point[,] GetGeoData(int pointCount)
        {
            try
            {
                // 2. ��������� �������� ������
                if (pointCount <= 0)
                    return null;
                if (Elements == null || Elements.Length <= 0)
                    return null;

                // 3. ������ ���������
                Point[,] geoData = new Point[pointCount, pointCount];

                foreach(GeoDataElement element in Elements)
                {
                    int i = element.i - 1;
                    int j = element.j - 1;

                    if (i >= 0 && i < pointCount &&
                        j >= 0 && j < pointCount &&
                        element.distance >= 0 &&
                        element.duration >= 0)
                    {
                        geoData[i, j].X = element.distance;
                        geoData[i, j].Y = element.duration;
                    }
                }

                // 4. �����
                return geoData;
            }
            catch
            {
                return null;
            }
        }
    }

    /// <summary>
    /// ��������� � ������ � 
    /// ����� �������� � ��������
    /// ����� ����� �����
    /// </summary>
    [Serializable()]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    public class GeoDataElement
    {
        /// <remarks/>
        [XmlAttribute("i")]
        public int i { get; set; }

        /// <remarks/>
        [XmlAttribute("j")]
        public int j { get; set; }

        /// <remarks/>
        [XmlAttribute("d")]
        public int distance { get; set; }

        /// <remarks/>
        [XmlAttribute("t")]
        public int duration { get; set; }
    }
}
