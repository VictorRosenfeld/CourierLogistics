
namespace CourierLogistics
{
    //using NPOI.SS.UserModel;
    using System;
    using System.Collections.Generic;
    using System.Device.Location;

    /// <summary>
    /// Вспомогательные функции
    /// </summary>
    public class Helper
    {
        /// <summary>
        /// Преобразование строкового педставления даты в DateTime
        /// </summary>
        /// <param name="text">Строковое представление даты</param>
        /// <returns>Преобразованное значение или DateTime.MinValue</returns>
        public static DateTime ParseDateTime(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return DateTime.MinValue;
            text = text.Trim();
            if (text.Equals("NULL", StringComparison.CurrentCultureIgnoreCase))
                return DateTime.MinValue;
            DateTime result;
            if (!DateTime.TryParse(text, out result))
                result = DateTime.MinValue;
            return result;
        }

        /// <summary>
        /// Преобразование строкового педставления целого числа в int
        /// </summary>
        /// <param name="text">Строковое представление целого числа</param>
        /// <returns>Преобразованное значение или int.MinValue</returns>
        public static int ParseInt(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return int.MinValue;
            text = text.Trim();
            if (text.Equals("NULL", StringComparison.CurrentCultureIgnoreCase))
                return int.MinValue;
            int result;
            if (!int.TryParse(text, out result))
                result = int.MinValue;
            return result;
        }

        /// <summary>
        /// Преобразование строкового дробного числа в double
        /// </summary>
        /// <param name="text">Строковое представление дробного числа</param>
        /// <returns>Преобразованное значение или double.NaN</returns>
        public static double ParseDouble(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return double.NaN;
            text = text.Trim();
            if (text.Equals("NULL", StringComparison.CurrentCultureIgnoreCase))
                return double.NaN;
            double result;
            if (!double.TryParse(text, out result))
            {
                text = text.Replace('.', ',');
                if (!double.TryParse(text, out result))
                    result = double.NaN;
            }

            return result;
        }

        /// <summary>
        /// Пересечение двух временных интервалов
        /// </summary>
        /// <param name="left1">Левый конец первого интервала</param>
        /// <param name="right1">Правый конец первого интервала</param>
        /// <param name="left2">Левый конец второго интервала</param>
        /// <param name="right2">Правый конец второго интервала</param>
        /// <returns>Пересечение или TimeSpan.Zero</returns>
        public static bool TimeIntervalsIntersection(DateTime left1, DateTime right1, DateTime left2, DateTime right2, out DateTime intersectionLeft, out DateTime intersectionRight)
        {
            intersectionLeft = (left1 >= left2 ? left1 : left2);
            intersectionRight = (right1 <= right2 ? right1 : right2);
            if (intersectionLeft <= intersectionRight)
                return true;
            return false;
        }

        private static Dictionary<ulong, bool> pairCount = new Dictionary<ulong, bool>(300000);

        public static int GeoCount => pairCount.Count;

        /// <summary>
        /// Расстояние между двумя точками Земной поверхности в километрах
        /// </summary>
        /// <param name="latitude1">Широта 1</param>
        /// <param name="longitude1">Долгота 1</param>
        /// <param name="latitude2">Ширина 2</param>
        /// <param name="longitude2">Долгота 2</param>
        /// <returns></returns>
        public static double Distance(double latitude1, double longitude1, double latitude2, double longitude2)
        {
            GeoCoordinate pt1 = new GeoCoordinate(latitude1, longitude1);
            GeoCoordinate pt2 = new GeoCoordinate(latitude2, longitude2);
            int hash1 = pt1.GetHashCode();
            int hash2 = pt2.GetHashCode();
            ulong key = GetPairKey(hash1, hash2);

            pairCount[key] = true;

            return 0.001 * pt1.GetDistanceTo(pt2);
        }



        /// <summary>
        /// Построение ключа
        /// из пары int
        /// </summary>
        /// <param name="hash1">Хэш 1</param>
        /// <param name="hash2">Хэш 2</param>
        /// <returns>Ключ</returns>
        public static ulong GetPairKey(int hash1, int hash2)
        {

            if (hash1 <= hash2)
            {
                return ((ulong)hash1) << 32 | ((ulong) hash2);
            }
            else
            {
                return ((ulong)hash2) << 32 | ((ulong) hash1);
            }
        }


        public static string ReplaceSemicolon(string csvRecord, char newChar = ',')
        {
            try
            {
                // 2. Проверяем исходные данные
                if (string.IsNullOrEmpty(csvRecord))
                    return csvRecord;

                // 3. Цикл замены ';' в тестовых элементах на заданный символ
                int startIndex = 0;
                int startQuotesPos;
                int endQuotesPos;

                while (startIndex < csvRecord.Length)
                {
                    // 3.1 Находим позицию элемента, начинающегося на '"'
                    if ((startQuotesPos = csvRecord.IndexOf(";\"", startIndex)) >= 0)
                    {
                        startQuotesPos++;
                    }
                    else if ((startQuotesPos = csvRecord.IndexOf("; \"", startIndex)) >= 0)
                    {
                        startQuotesPos += 2;
                    }
                    else
                    {
                        return csvRecord;
                    }

                    // 3.2 Находим позицию элемента, закнчивающегося на '"'
                    if ((endQuotesPos = csvRecord.IndexOf("\";", startQuotesPos + 1)) >= 0)
                    { }
                    else if ((endQuotesPos = csvRecord.IndexOf("\" ;", startQuotesPos + 1)) >= 0)
                    { }
                    else
                    {
                        return csvRecord;
                    }

                    // 3.3 Заменяем ';' на newChar
                    csvRecord = csvRecord.Substring(0, startQuotesPos) + csvRecord.Substring(startQuotesPos, endQuotesPos - startQuotesPos + 1).Replace(';', newChar) + csvRecord.Substring(endQuotesPos + 1);
                    startIndex = endQuotesPos + 1;
                }

                // 4. Выход - Ok
                return csvRecord;
            }
            catch
            {
                return csvRecord;
            }
        }

        ///// <summary>
        ///// Проверка и извлечение int из ячейки
        ///// </summary>
        ///// <param name="cell">Ячейка</param>
        ///// <param name="value">Извлеченное значение или 0</param>
        ///// <returns>true - значение извлечено; false - значение не извлечено</returns>
        //public static bool TryGetInt(ICell cell, out int value)
        //{
        //    value = 0;

        //    if (cell == null)
        //        return false;

        //    switch (cell.CellType)
        //    {
        //        case CellType.String:
        //            return int.TryParse(cell.StringCellValue, out value);
        //        case CellType.Numeric:
        //            double val = cell.NumericCellValue;
        //            if (val < int.MinValue || val > int.MaxValue)
        //                return false;
        //            value = (int)val;
        //            return true;
        //        default:
        //            return false;
        //    }
        //}

        ///// <summary>
        ///// Проверка и извлечение double-значени из ячейки
        ///// </summary>
        ///// <param name="cell">Ячейка</param>
        ///// <param name="value">Извлеченное значение или 0</param>
        ///// <returns>true - значение извлечено; false - значение не извлечено</returns>
        //public static bool TryGetDouble(ICell cell, out double value)
        //{
        //    value = 0;

        //    if (cell == null)
        //        return false;

        //    switch (cell.CellType)
        //    {
        //        case CellType.String:
        //            return TryGetDouble(cell.StringCellValue, out value);
        //        case CellType.Numeric:
        //            value = cell.NumericCellValue;
        //            return true;
        //        default:
        //            return false;
        //    }
        //}

        ///// <summary>
        ///// Проверка и извлечение double-значени из строки
        ///// </summary>
        ///// <param name="text">Текстовое значение</param>
        ///// <param name="value">Извлеченное значение или 0</param>
        ///// <returns>true - значение извлечено; false - значение не извлечено</returns>
        //public static bool TryGetDouble(string text, out double value)
        //{
        //    // 1. Инициализация
        //    value = 0;

        //    // 2. Проверяем исходные данные
        //    if (string.IsNullOrEmpty(text))
        //        return false;

        //    // 3. Нам првезет
        //    if (double.TryParse(text, out value))
        //        return true;

        //    // 4. Пытаемся заменить десятичную точку и повторить попытку
        //    if (text.IndexOf('.') >= 0)
        //    {
        //        text = text.Replace('.', ',');
        //    }
        //    else
        //    {
        //        text = text.Replace(',', '.');
        //    }

        //    return double.TryParse(text, out value);
        //}

        ///// <summary>
        ///// Проверка и извлечение string из ячейки
        ///// </summary>
        ///// <param name="cell">Ячейка</param>
        ///// <param name="value">Извлеченное значение или 0</param>
        ///// <returns>true - значение извлечено; false - значение не извлечено</returns>
        //public static bool TryGetText(ICell cell, out string value)
        //{
        //    value = "";

        //    if (cell == null)
        //        return true;

        //    switch (cell.CellType)
        //    {
        //        case CellType.String:
        //            value = cell.StringCellValue;
        //            return true;
        //        case CellType.Numeric:
        //            value = cell.NumericCellValue.ToString();
        //            return true;
        //        case CellType.Blank:
        //            value = "";
        //            return true;
        //        default:
        //            return false;
        //    }
        //}

        /// <summary>
        /// Построение ключа отгрузки
        /// из индексов заказов
        /// </summary>
        /// <param name="index">Индексы</param>
        /// <returns>Ключ</returns>
        public static ulong GetDeliveryKey(params int[] index)
        {
            byte[] keyBytes = new byte[8];
            Array.Sort(index);

            switch (index.Length)
            {
                case 1:
                    keyBytes[0] = (byte) index[0];
                    break;
                case 2:
                    keyBytes[0] = (byte)index[0];
                    keyBytes[1] = (byte)index[1];
                    break;
                case 3:
                    keyBytes[0] = (byte)index[0];
                    keyBytes[1] = (byte)index[1];
                    keyBytes[2] = (byte)index[2];
                    break;
                case 4:
                    keyBytes[0] = (byte) index[0];
                    keyBytes[1] = (byte) index[1];
                    keyBytes[2] = (byte) index[2];
                    keyBytes[3] = (byte) index[3];
                    break;
                case 5:
                    keyBytes[0] = (byte) index[0];
                    keyBytes[1] = (byte) index[1];
                    keyBytes[2] = (byte) index[2];
                    keyBytes[3] = (byte) index[3];
                    keyBytes[4] = (byte) index[4];
                    break;
                case 6:
                    keyBytes[0] = (byte) index[0];
                    keyBytes[1] = (byte) index[1];
                    keyBytes[2] = (byte) index[2];
                    keyBytes[3] = (byte) index[3];
                    keyBytes[4] = (byte) index[4];
                    keyBytes[5] = (byte) index[5];
                    break;
                case 7:
                    keyBytes[0] = (byte)index[0];
                    keyBytes[1] = (byte)index[1];
                    keyBytes[2] = (byte)index[2];
                    keyBytes[3] = (byte)index[3];
                    keyBytes[4] = (byte)index[4];
                    keyBytes[5] = (byte)index[5];
                    keyBytes[6] = (byte)index[6];
                    break;
                //case 8:
                default:
                    keyBytes[0] = (byte)index[0];
                    keyBytes[1] = (byte)index[1];
                    keyBytes[2] = (byte)index[2];
                    keyBytes[3] = (byte)index[3];
                    keyBytes[4] = (byte)index[4];
                    keyBytes[5] = (byte)index[5];
                    keyBytes[6] = (byte)index[6];
                    keyBytes[7] = (byte)index[7];
                    break;
            }

            return BitConverter.ToUInt64(keyBytes, 0);
        }

        /// <summary>
        /// Построение ключа отгрузки
        /// из индексов заказов
        /// </summary>
        /// <param name="index1">Индекс заказа 1</param>
        /// <param name="index2">Индекс заказа 2</param>
        /// <returns>Ключ</returns>
        public static ulong GetDeliveryKey(int index1, int index2)
        {
            byte[] keyBytes = new byte[8];

            if (index1 <= index2)
            {
                keyBytes[0] = (byte)index1;
                keyBytes[1] = (byte)index2;
            }
            else
            {
                keyBytes[0] = (byte)index2;
                keyBytes[1] = (byte)index1;
            }

            return BitConverter.ToUInt64(keyBytes, 0);
        }

        /// <summary>
        /// Построение ключа отгрузки
        /// из индексов заказов
        /// </summary>
        /// <param name="index1">Индекс заказа 1</param>
        /// <param name="index2">Индекс заказа 2</param>
        /// <param name="index3">Индекс заказа 3</param>
        /// <returns>Ключ</returns>
        public static ulong GetDeliveryKey(int index1, int index2, int index3)
        {
            byte[] keyBytes = new byte[8];

            // Пузырьковая сортировка
            if (index1 > index2)
            {
                int temp = index1;
                index1 = index2;
                index2 = temp;
            }

            if (index2 > index3)
            {
                int temp = index2;
                index2 = index3;
                index3 = temp;
            }

            if (index1 > index2)
            {
                int temp = index1;
                index1 = index2;
                index2 = temp;
            }

            keyBytes[0] = (byte)index1;
            keyBytes[1] = (byte)index2;
            keyBytes[2] = (byte)index3;

            return BitConverter.ToUInt64(keyBytes, 0);
        }

        /// <summary>
        /// Построение ключа отгрузки
        /// из индексов заказов
        /// </summary>
        /// <param name="index">Индексы</param>
        /// <param name="index">Количество индексов</param>
        /// <returns>Ключ</returns>
        public static ulong GetDeliveryKey(int[] index, int count)
        {
            byte[] keyBytes = new byte[8];
            Array.Sort(index, 0, count);

            switch (count)
            {
                case 1:
                    keyBytes[0] = (byte) index[0];
                    break;
                case 2:
                    keyBytes[0] = (byte)index[0];
                    keyBytes[1] = (byte)index[1];
                    break;
                case 3:
                    keyBytes[0] = (byte)index[0];
                    keyBytes[1] = (byte)index[1];
                    keyBytes[2] = (byte)index[2];
                    break;
                case 4:
                    keyBytes[0] = (byte) index[0];
                    keyBytes[1] = (byte) index[1];
                    keyBytes[2] = (byte) index[2];
                    keyBytes[3] = (byte) index[3];
                    break;
                case 5:
                    keyBytes[0] = (byte) index[0];
                    keyBytes[1] = (byte) index[1];
                    keyBytes[2] = (byte) index[2];
                    keyBytes[3] = (byte) index[3];
                    keyBytes[4] = (byte) index[4];
                    break;
                case 6:
                    keyBytes[0] = (byte) index[0];
                    keyBytes[1] = (byte) index[1];
                    keyBytes[2] = (byte) index[2];
                    keyBytes[3] = (byte) index[3];
                    keyBytes[4] = (byte) index[4];
                    keyBytes[5] = (byte) index[5];
                    break;
                case 7:
                    keyBytes[0] = (byte)index[0];
                    keyBytes[1] = (byte)index[1];
                    keyBytes[2] = (byte)index[2];
                    keyBytes[3] = (byte)index[3];
                    keyBytes[4] = (byte)index[4];
                    keyBytes[5] = (byte)index[5];
                    keyBytes[6] = (byte)index[6];
                    break;
                //case 8:
                default:
                    keyBytes[0] = (byte)index[0];
                    keyBytes[1] = (byte)index[1];
                    keyBytes[2] = (byte)index[2];
                    keyBytes[3] = (byte)index[3];
                    keyBytes[4] = (byte)index[4];
                    keyBytes[5] = (byte)index[5];
                    keyBytes[6] = (byte)index[6];
                    keyBytes[7] = (byte)index[7];
                    break;
            }

            return BitConverter.ToUInt64(keyBytes, 0);
        }

    }
}
