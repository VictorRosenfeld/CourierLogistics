using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQLCLR_TEST
{
    public class YandexResponseParser
    {
        //{"rows":[{"elements":[{"distance":{"value":4770},"duration":{"value":526},"status":"OK"},{"distance":{"value":0},"duration":{"value":0},"status":"OK"}]},{"elements":[{"distance":{"value":3667},"duration":{"value":387},"status":"OK"},{"distance":{"value":755},"duration":{"value":93},"status":"OK"}]}]}

        public static int Parse(string json)
        {

            int iDistance = json.IndexOf(@"""distance""");
            int iDuration = json.IndexOf(@"""duration""");
            int iStatus = json.IndexOf(@"""status""");

            while(iDistance > 0 && iDuration > 0 && iStatus > 0)
            {
                // distance
                int iValue = json.IndexOf(@"""value""", iDistance + 10);
                if (iValue < 0)
                    break;
                iValue += 7;
                int value1 = ParseValue(json, iValue);

                // duration
                iValue = json.IndexOf(@"""value""", iDuration + 10);
                int value2 = ParseValue(json, iValue);

                // status
                string status = ParseStatus(json, iStatus + 8);

                iDistance = json.IndexOf(@"""distance""", iDistance + 10);
                iDuration = json.IndexOf(@"""duration""", iDuration + 10);
                iStatus = json.IndexOf(@"""status""", iStatus + 8);
            }






            string[] elements = json.Split(new string[] { @"""distance""" }, int.MaxValue, StringSplitOptions.RemoveEmptyEntries);

            return 0;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="json"></param>
        /// <param name="startIndex"></param>
        /// <returns></returns>
        ///        startIndex
        ///            |
        ///    {"value":4770}
        private static int ParseValue(string json, int startIndex)
        {
            int startPos = json.IndexOf(':', startIndex);
            if (startPos < 0)
                return -1;
            int endPos = json.IndexOf('}', startPos + 1);
            if (endPos < 0)
                return -2;
            if (!int.TryParse(json.Substring(startPos + 1, endPos - startPos - 1).Trim(), out startPos))
                return -3;
            return startPos;
        }
        private static string ParseStatus(string json, int startIndex)
        {
            int startPos = json.IndexOf('"', startIndex);
            if (startPos < 0)
                return null;
            int endPos = json.IndexOf('"', startPos + 1);
            if (endPos < 0)
                return null;
            return json.Substring(startPos + 1, endPos - startPos - 1).Trim();
        }
    }
}
