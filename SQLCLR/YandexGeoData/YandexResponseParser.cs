
namespace SQLCLR.YandexGeoData
{
    using System;

    /// <summary>
    /// ������ ������� Yandex
    /// </summary>
    public class YandexResponseParser
    {
        #region ������ � ���������� �� ������

        /// <summary>
        /// ������� ������� � ���������� �� ������
        /// </summary>
        private const string ERROR_MESSAGE_PREFIX = @"""errors"":[""";

        /// <summary>
        /// ������� ������� � ���������� �� ������
        /// </summary>
        private const string ERROR_MESSAGE_SUFFIX = @"""]}";

        #endregion ������ � ���������� �� ������

        #region ������ � ����������

        /// <summary>
        /// ������� ������� � �������
        /// </summary>
        private const string DATA_RESPONSE_STARTS_WITH = @"{""rows""";

        /// <summary>
        /// ������� distance
        /// </summary>
        private const string DATA_RESPONSE_ITEM_DISTANCE = @"""distance""";

        /// <summary>
        /// ������� duration
        /// </summary>
        private const string DATA_RESPONSE_ITEM_DURATION = @"""duration""";

        /// <summary>
        /// ������� status
        /// </summary>
        private const string DATA_RESPONSE_ITEM_STATUS = @"""status""";

        /// <summary>
        /// ������� value
        /// </summary>
        private const string DATA_RESPONSE_ITEM_VALUE = @"""value""";

        /// <summary>
        /// ������� ��� ��������� �������� distance
        /// </summary>
        private const string DATA_RESPONSE_ITEM_DISTANCE_START_WITH = @"""di";

        /// <summary>
        /// ������� ��� ��������� �������� duration
        /// </summary>
        private const string DATA_RESPONSE_ITEM_DURATION_START_WITH = @"""du";

        /// <summary>
        /// ������� ��� ��������� �������� status
        /// </summary>
        private const string DATA_RESPONSE_ITEM_STATUS_START_WITH = @"""st";

        #endregion ������ � ����������

        /// <summary>
        /// �������������� ������� � �������
        /// </summary>
        /// <param name="json">Json ����� �������</param>
        /// <param name="items">������ ��� ��� �����</param>
        /// <returns>0 - �������������� ���������; ����� - �������������� �� ���������</returns>
        //{"rows":[{"elements":[{"distance":{"value":4770},"duration":{"value":526},"status":"OK"},{"distance":{"value":0},"duration":{"value":0},"status":"OK"}]},{"elements":[{"distance":{"value":3667},"duration":{"value":387},"status":"OK"},{"distance":{"value":755},"duration":{"value":93},"status":"OK"}]}]}

        public static int TryParse(string json, out YandexResponseItem[] items)
        {
            // 1. �������������
            int rc = 1;
            items = null;

            try
            {
                // 2. ��������� �������� ������
                rc = 2;
                if (string.IsNullOrWhiteSpace(json))
                    return rc;

                json = json.Trim();

                // 3. ��������� ������� ����������� ������
                rc = 3;
                if (!json.StartsWith(DATA_RESPONSE_STARTS_WITH, StringComparison.InvariantCultureIgnoreCase))
                    return rc;

                // 4. ���� ���������
                rc = 4;
                int count = 0;
                int distance = -1;
                int duration = -1;
                string status = null;
                int i = DATA_RESPONSE_STARTS_WITH.Length;
                bool eoi = false;
                int sz = DATA_RESPONSE_ITEM_DISTANCE.Length;
                if (sz < DATA_RESPONSE_ITEM_DURATION.Length)
                    sz = DATA_RESPONSE_ITEM_DURATION.Length;
                if (sz < DATA_RESPONSE_ITEM_STATUS.Length)
                    sz = DATA_RESPONSE_ITEM_STATUS.Length;
                items = new YandexResponseItem[json.Length / sz];

                while (i < json.Length - sz)
                {
                    switch (json.Substring(i, 3))
                    {
                        case DATA_RESPONSE_ITEM_DISTANCE_START_WITH:
                            if (!DATA_RESPONSE_ITEM_DISTANCE.Equals(json.Substring(i, DATA_RESPONSE_ITEM_DISTANCE.Length), StringComparison.InvariantCultureIgnoreCase))
                                return rc;
                            if (distance >= 0)
                                return rc;
                            i += DATA_RESPONSE_ITEM_DISTANCE.Length;
                            distance = ParseIntValue(json, ref i, ref eoi);
                            if (distance < 0)
                                return rc;
                            break;
                        case DATA_RESPONSE_ITEM_DURATION_START_WITH:
                            if (!DATA_RESPONSE_ITEM_DURATION.Equals(json.Substring(i, DATA_RESPONSE_ITEM_DURATION.Length), StringComparison.InvariantCultureIgnoreCase))
                                return rc;
                            if (duration >= 0)
                                return rc;
                            i += DATA_RESPONSE_ITEM_DURATION.Length;
                            duration = ParseIntValue(json, ref i, ref eoi);
                            if (duration < 0)
                                return rc;
                            break;
                        case DATA_RESPONSE_ITEM_STATUS_START_WITH:
                            if (!DATA_RESPONSE_ITEM_STATUS.Equals(json.Substring(i, DATA_RESPONSE_ITEM_STATUS.Length), StringComparison.InvariantCultureIgnoreCase))
                                return rc;
                            if (status != null)
                                return rc;
                            i += DATA_RESPONSE_ITEM_STATUS.Length;
                            status = ParseStringValue(json, ref i, ref eoi);
                            if (status == null)
                                return rc;
                            break;
                        default:
                            i++;
                            break;
                    }

                    if (eoi)
                    {
                        if (distance >= 0 && duration >= 0 && status != null)
                        {
                            if (count >= items.Length)
                            {
                                Array.Resize(ref items, items.Length + 100);
                            }

                            items[count++] = new YandexResponseItem(distance, duration, status);

                            distance = -1;
                            duration = -1;
                            status = null;
                            eoi = false;
                        }
                        else
                        {
                            return rc;
                        }
                    }
                    else if (distance >= 0 && duration >= 0 && status != null)
                    {
                        return rc;
                    }
                }

                if (count < items.Length)
                {
                    Array.Resize(ref items, count);
                }

                // 5. ����� - Ok
                rc = 0;
                return rc;
            }
            catch
            {
                return rc;
            }
        }

        /// <summary>
        /// ����� ��������� �� ������� Yandex
        /// </summary>
        /// <param name="json">json-������ � ��������</param>
        /// <returns>��������� �� ������� ��� null</returns>
        public static string GetErrorMessages(string json)
        {
            try
            {
                // 2. ��������� �������� ������
                if (string.IsNullOrWhiteSpace(json))
                    return null;

                // 3. ������������ �� '"'
                int charIndex = ScanCharIndex(json, '"', 0);
                if (charIndex < 0)
                    return null;

                // 4. ������� �������������� ���������
                int iStart = json.IndexOf(ERROR_MESSAGE_PREFIX, charIndex, StringComparison.CurrentCultureIgnoreCase);
                if (iStart < 0)
                    return null;
                int iEnd = json.IndexOf(ERROR_MESSAGE_SUFFIX, iStart + ERROR_MESSAGE_PREFIX.Length);
                if (iEnd < 0)
                    return null;

                // 4. ���������� ���������
                return json.Substring(iStart + ERROR_MESSAGE_PREFIX.Length - 1, iEnd - iStart - ERROR_MESSAGE_PREFIX.Length + 2);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// ������������� �������� ': { "value" : d...d }'
        /// </summary>
        /// <param name="json"></param>
        /// <param name="startIndex">������ ������� ��������������� ��������</param>
        /// <param name="eoi">������� ���������� �������� ��������</param>
        /// <returns>�������� d...d >= 0 ��� ������</returns>
        /// startIndex                eoi
        ///     |                      |
        ///       : {"value":d...d} } ��� ]
        private static int ParseIntValue(string json, ref int startIndex, ref bool eoi)
        {
            // 1. �������������
            int rc = 1;
            eoi = false;

            try
            {
                // 2. ������������ �� ':'
                rc = 2;
                int charIndex = ScanCharIndex(json, ':', startIndex);
                if (charIndex < 0)
                    return -rc;
                startIndex = charIndex + 1;

                // 3. ������������ �� '{'
                rc = 3;
                charIndex = ScanCharIndex(json, '{', startIndex);
                if (charIndex < 0)
                    return -rc;

                startIndex = charIndex + 1;

                // 4. ������������ �� '"' � ��������� "value"
                rc = 4;
                charIndex = ScanCharIndex(json, '"', startIndex);
                if (charIndex < 0)
                    return -rc;
                if (!DATA_RESPONSE_ITEM_VALUE.Equals(json.Substring(charIndex, DATA_RESPONSE_ITEM_VALUE.Length), StringComparison.InvariantCultureIgnoreCase))
                    return -rc;

                startIndex = charIndex + DATA_RESPONSE_ITEM_VALUE.Length;

                // 5. ������������ �� ':'
                rc = 5;
                charIndex = ScanCharIndex(json, ':', startIndex);
                if (charIndex < 0)
                    return -rc;
                startIndex = charIndex + 1;

                // 6. ������������ �� ������ �����
                rc = 6;
                char s = char.MinValue;

                for (charIndex = startIndex; charIndex < json.Length; charIndex++)
                {
                    s = json[charIndex];
                    if (char.IsDigit(s))
                        break;
                    if (s != ' ')
                        return -rc;
                }

                if (s == char.MinValue)
                    return -rc;

                int firstDigit = charIndex;
                startIndex = charIndex + 1;

                // 7. ������������ �� ������� ���������� �� ��������� ������
                rc = 7;
                for (charIndex = startIndex; charIndex < json.Length; charIndex++)
                {
                    s = json[charIndex];
                    if (!char.IsDigit(s))
                        break;
                }

                if (char.IsDigit(s))
                    return rc;

                int lastDigit = charIndex;

                startIndex = charIndex;

                // 8. ����������� � int
                rc = 8;
                int value;
                if (!int.TryParse(json.Substring(firstDigit, lastDigit - firstDigit), out value))
                    return -rc;

                // 9. ������������ �� '}'
                rc = 9;
                charIndex = ScanCharIndex(json, '}', startIndex);
                if (charIndex < 0)
                {
                    eoi = true;
                    return value;
                }

                startIndex = charIndex + 1;

                // 9. ������������ �� ������� �� �������
                rc = 9;
                for (charIndex = startIndex; charIndex < json.Length; charIndex++)
                {
                    s = json[charIndex];
                    if (s != ' ')
                    {
                        if (s == '}' || s == ']')
                            eoi = true;
                        break;
                    }
                }

                startIndex = charIndex;

                // 10. ����� - Ok
                return value;
            }
            catch
            {
                return -rc;
            }
        }

        /// <summary>
        /// �������������� �������� ': "..."'
        /// </summary>
        /// <param name="json"></param>
        /// <param name="startIndex">������ ������� ��������������� ��������</param>
        /// <param name="eoi">������� ���������� �������� ��������</param>
        /// <returns>�������� ... ��� null</returns>
        /// startIndex   eoi
        ///     |         |
        ///       : "..." }
        private static string ParseStringValue(string json, ref int startIndex, ref bool eoi)
        {
            // 1. �������������
            eoi = false;

            try
            {
                // 2. ������������ �� ':'
                int charIndex = ScanCharIndex(json, ':', startIndex);
                if (charIndex < 0)
                    return null;
                startIndex = charIndex + 1;

                // 3. ������������ �� ������� '"'
                charIndex = ScanCharIndex(json, '"', startIndex);
                if (charIndex < 0)
                    return null;

                int firstSym = startIndex;
                startIndex = charIndex + 1;

                // 4. ������������ �� ���������� '"'
                charIndex = startIndex;
                int endSym = -1;

                while (charIndex < json.Length)
                {
                    char s = json[charIndex];
                    if (s == '"')
                    {
                        if (charIndex == json.Length - 1)
                        {
                            endSym = charIndex;
                            break;
                        }
                        else if (json[charIndex + 1] == '"')
                        {
                            charIndex += 2;
                        }
                        else
                        {
                            endSym = charIndex;
                            break;
                        }
                    }
                    else
                    {
                        charIndex++;
                    }
                }

                if (endSym == -1)
                    return null;

                startIndex = endSym + 1;

                // 5. ������������ �� ������� �� �������
                for (charIndex = startIndex; charIndex < json.Length; charIndex++)
                {
                    char s = json[charIndex];
                    if (s != ' ')
                    {
                        if (s == '}')
                            eoi = true;
                        break;
                    }
                }

                startIndex = charIndex + 1;

                // 5. ����� - Ok
                firstSym++;
                endSym--;
                if (firstSym > endSym)
                    return "";

                return json.Substring(firstSym, endSym - firstSym + 1).Trim();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// ����������� �� ��������� �������
        /// � ��������� ���������� ��������
        /// </summary>
        /// <param name="json">����������� ������</param>
        /// <param name="scanChar">����������� ������</param>
        /// <param name="startIndex">������ ������ ������������</param>
        /// <returns>������ ������� ������� ��� ������������� ��������</returns>
        private static int ScanCharIndex(string json, char scanChar, int startIndex)
        {
            // 1. �������������
            int rc = 1;

            try
            {
                // 2. ��������� �������� ������
                rc = 2;
                if (string.IsNullOrEmpty(json))
                    return -rc;
                if (startIndex < 0 || startIndex >= json.Length)
                    return -rc;

                // 3. ������������ �� ��������� �������
                rc = 3;
                for (int i = startIndex; i < json.Length; i++)
                {
                    char s = json[i];
                    if (s == scanChar)
                        return i;
                    if (s != ' ')
                        return -rc;
                }

                // 4. ������ �� ������
                rc = 4;
                return -rc;
            }
            catch
            {
                return -1;
            }
        }
    }
}
