

namespace SQLCLR.YandexGeoData
{
    //using System;
    //using System.Collections.Generic;
    //using System.Text;

//{"rows":[{"elements":[{"distance":{"value":4770},"duration":{"value":526},"status":"OK"},{"distance":{"value":0},"duration":{"value":0},"status":"OK"}]},{"elements":[{"distance":{"value":3667},"duration":{"value":387},"status":"OK"},{"distance":{"value":755},"duration":{"value":93},"status":"OK"}]}]}
    public class YandexResponse
    {
        public Row[] rows { get; set; }
    }

    public class Row
    {
        public Element[] elements { get; set; }
    }

    public class Element
    {
        public Distance distance { get; set; }
        public Duration duration { get; set; }
        public string status { get; set; }
    }

    public class Distance
    {
        public int value { get; set; }
    }

    public class Duration
    {
        public int value { get; set; }
    }

}
