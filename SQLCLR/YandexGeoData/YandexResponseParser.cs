
namespace SQLCLR.YandexGeoData
{
    using System;

    /// <summary>
    /// Парсер отклика Yandex
    /// </summary>
    public class YandexResponseParser
    {
        #region Отклик с сообщением об ошибке

        /// <summary>
        /// Префикс отклика с сообщением об ошибке
        /// </summary>
        private const string ERROR_MESSAGE_PREFIX = @"""errors"":[""";

        /// <summary>
        /// Суффикс отклика с сообщением об ошибке
        /// </summary>
        private const string ERROR_MESSAGE_SUFFIX = @"""]}";

        #endregion Отклик с сообщением об ошибке

        #region Отклик с геоданными

        /// <summary>
        /// Префикс отклика с данными
        /// </summary>
        private const string DATA_RESPONSE_STARTS_WITH = @"{""rows""";

        /// <summary>
        /// Атрибут distance
        /// </summary>
        private const string DATA_RESPONSE_ITEM_DISTANCE = @"""distance""";

        /// <summary>
        /// Атрибут duration
        /// </summary>
        private const string DATA_RESPONSE_ITEM_DURATION = @"""duration""";

        /// <summary>
        /// Атрибут status
        /// </summary>
        private const string DATA_RESPONSE_ITEM_STATUS = @"""status""";

        /// <summary>
        /// Атрибут value
        /// </summary>
        private const string DATA_RESPONSE_ITEM_VALUE = @"""value""";

        /// <summary>
        /// Префикс для выделения атрибута distance
        /// </summary>
        private const string DATA_RESPONSE_ITEM_DISTANCE_START_WITH = @"""di";

        /// <summary>
        /// Префикс для выделения атрибута duration
        /// </summary>
        private const string DATA_RESPONSE_ITEM_DURATION_START_WITH = @"""du";

        /// <summary>
        /// Префикс для выделения атрибута status
        /// </summary>
        private const string DATA_RESPONSE_ITEM_STATUS_START_WITH = @"""st";

        #endregion Отклик с геоданными

        /// <summary>
        /// Десериализация отклика с данными
        /// </summary>
        /// <param name="json">Json текст отклика</param>
        /// <param name="items">Данные для пар точек</param>
        /// <returns>0 - Десериализация выполнена; иначе - десериализация не выполнена</returns>
        //{"rows":[{"elements":[{"distance":{"value":4770},"duration":{"value":526},"status":"OK"},{"distance":{"value":0},"duration":{"value":0},"status":"OK"}]},{"elements":[{"distance":{"value":3667},"duration":{"value":387},"status":"OK"},{"distance":{"value":755},"duration":{"value":93},"status":"OK"}]}]}

        public static int TryParse(string json, out YandexResponseItem[] items)
        {
            // 1. Инициализация
            int rc = 1;
            items = null;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (string.IsNullOrWhiteSpace(json))
                    return rc;

                json = json.Trim();

                // 3. Проверяем наличие правильного начала
                rc = 3;
                if (!json.StartsWith(DATA_RESPONSE_STARTS_WITH, StringComparison.InvariantCultureIgnoreCase))
                    return rc;

                // 4. Цикл обработки
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

                // 5. Выход - Ok
                rc = 0;
                return rc;
            }
            catch
            {
                return rc;
            }
        }

        /// <summary>
        /// Выбор сообщений об ошибках Yandex
        /// </summary>
        /// <param name="json">json-отклик с ошибками</param>
        /// <returns>Сообщения об ошибках или null</returns>
        public static string GetErrorMessages(string json)
        {
            try
            {
                // 2. Проверяем исходные данные
                if (string.IsNullOrWhiteSpace(json))
                    return null;

                // 3. Продвигаемся до '"'
                int charIndex = ScanCharIndex(json, '"', 0);
                if (charIndex < 0)
                    return null;

                // 4. Находим местоположение сообщения
                int iStart = json.IndexOf(ERROR_MESSAGE_PREFIX, charIndex, StringComparison.CurrentCultureIgnoreCase);
                if (iStart < 0)
                    return null;
                int iEnd = json.IndexOf(ERROR_MESSAGE_SUFFIX, iStart + ERROR_MESSAGE_PREFIX.Length);
                if (iEnd < 0)
                    return null;

                // 4. Возвращаем результат
                return json.Substring(iStart + ERROR_MESSAGE_PREFIX.Length - 1, iEnd - iStart - ERROR_MESSAGE_PREFIX.Length + 2);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Десерализация значения ': { "value" : d...d }'
        /// </summary>
        /// <param name="json"></param>
        /// <param name="startIndex">Индекс символа предшествующего элементу</param>
        /// <param name="eoi">Признак последнего значения элемента</param>
        /// <returns>Значение d...d >= 0 или ошибка</returns>
        /// startIndex                eoi
        ///     |                      |
        ///       : {"value":d...d} } или ]
        private static int ParseIntValue(string json, ref int startIndex, ref bool eoi)
        {
            // 1. Инициализация
            int rc = 1;
            eoi = false;

            try
            {
                // 2. Продвигаемся до ':'
                rc = 2;
                int charIndex = ScanCharIndex(json, ':', startIndex);
                if (charIndex < 0)
                    return -rc;
                startIndex = charIndex + 1;

                // 3. Продвигаемся до '{'
                rc = 3;
                charIndex = ScanCharIndex(json, '{', startIndex);
                if (charIndex < 0)
                    return -rc;

                startIndex = charIndex + 1;

                // 4. Продвигаемся до '"' и проверяем "value"
                rc = 4;
                charIndex = ScanCharIndex(json, '"', startIndex);
                if (charIndex < 0)
                    return -rc;
                if (!DATA_RESPONSE_ITEM_VALUE.Equals(json.Substring(charIndex, DATA_RESPONSE_ITEM_VALUE.Length), StringComparison.InvariantCultureIgnoreCase))
                    return -rc;

                startIndex = charIndex + DATA_RESPONSE_ITEM_VALUE.Length;

                // 5. Продвигаемся до ':'
                rc = 5;
                charIndex = ScanCharIndex(json, ':', startIndex);
                if (charIndex < 0)
                    return -rc;
                startIndex = charIndex + 1;

                // 6. Продвигаемся до первой цифры
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

                // 7. Продвигаемся до символа следующего за последней цифрой
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

                // 8. Преобразуем в int
                rc = 8;
                int value;
                if (!int.TryParse(json.Substring(firstDigit, lastDigit - firstDigit), out value))
                    return -rc;

                // 9. Продвигаемся до '}'
                rc = 9;
                charIndex = ScanCharIndex(json, '}', startIndex);
                if (charIndex < 0)
                {
                    eoi = true;
                    return value;
                }

                startIndex = charIndex + 1;

                // 9. Продвигаемся до первого не пробела
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

                // 10. Выход - Ok
                return value;
            }
            catch
            {
                return -rc;
            }
        }

        /// <summary>
        /// Десериализация значения ': "..."'
        /// </summary>
        /// <param name="json"></param>
        /// <param name="startIndex">Индекс символа предшествующего элементу</param>
        /// <param name="eoi">Признак последнего значения элемента</param>
        /// <returns>Значение ... или null</returns>
        /// startIndex   eoi
        ///     |         |
        ///       : "..." }
        private static string ParseStringValue(string json, ref int startIndex, ref bool eoi)
        {
            // 1. Инициализация
            eoi = false;

            try
            {
                // 2. Продвигаемся до ':'
                int charIndex = ScanCharIndex(json, ':', startIndex);
                if (charIndex < 0)
                    return null;
                startIndex = charIndex + 1;

                // 3. Продвигаемся до первого '"'
                charIndex = ScanCharIndex(json, '"', startIndex);
                if (charIndex < 0)
                    return null;

                int firstSym = startIndex;
                startIndex = charIndex + 1;

                // 4. Продвигаемся до последнего '"'
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

                // 5. Продвигаемся до первого не пробела
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

                // 5. Выход - Ok
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
        /// Продвижение до заданного символа
        /// с пропуском лидирующих пробелов
        /// </summary>
        /// <param name="json">Сканируемая строка</param>
        /// <param name="scanChar">Сканируемый символ</param>
        /// <param name="startIndex">Индекс начала сканирования</param>
        /// <returns>Индеск позиции симвода или отрицательное значение</returns>
        private static int ScanCharIndex(string json, char scanChar, int startIndex)
        {
            // 1. Инициализация
            int rc = 1;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (string.IsNullOrEmpty(json))
                    return -rc;
                if (startIndex < 0 || startIndex >= json.Length)
                    return -rc;

                // 3. Продвигаемся до заданного символа
                rc = 3;
                for (int i = startIndex; i < json.Length; i++)
                {
                    char s = json[i];
                    if (s == scanChar)
                        return i;
                    if (s != ' ')
                        return -rc;
                }

                // 4. Символ не найден
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
