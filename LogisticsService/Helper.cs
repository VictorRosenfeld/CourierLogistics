
namespace LogisticsService
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Text;

    /// <summary>
    /// Вспомогательные методы
    /// </summary>
    public class Helper
    {
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
            return (intersectionLeft <= intersectionRight);
        }

        /// <summary>
        /// Запись сообщения в лог
        /// </summary>
        /// <param name="message">Сообщение</param>
        public static void WriteToLog(string message)
        {
            try
            {
                //string xx = Path.ChangeExtension(Process.GetCurrentProcess().MainModule.FileName, "log");
                using (StreamWriter sw = new StreamWriter(Path.ChangeExtension(Process.GetCurrentProcess().MainModule.FileName, "log"), true))
                {
                    sw.WriteLine($"{DateTime.Now} > {message}");
                }
            }
            catch
            { }
        }

        /// <summary>
        /// Запись сообщения в лог
        /// </summary>
        /// <param name="message">Сообщение</param>
        public static void WriteInfoToLog(string message)
        {
            try
            {
                //string xx = Path.ChangeExtension(Process.GetCurrentProcess().MainModule.FileName, "log");
                using (StreamWriter sw = new StreamWriter(Path.ChangeExtension(Process.GetCurrentProcess().MainModule.FileName, "log"), true))
                {
                    sw.WriteLine($"{DateTime.Now} > Info {message}");
                }
            }
            catch
            { }
        }

        /// <summary>
        /// Запись сообщения в лог
        /// </summary>
        /// <param name="message">Сообщение</param>
        public static void WriteWariningToLog(string message)
        {
            try
            {
                //string xx = Path.ChangeExtension(Process.GetCurrentProcess().MainModule.FileName, "log");
                using (StreamWriter sw = new StreamWriter(Path.ChangeExtension(Process.GetCurrentProcess().MainModule.FileName, "log"), true))
                {
                    sw.WriteLine($"{DateTime.Now} > Warning {message}");
                }
            }
            catch
            { }
        }

        /// <summary>
        /// Запись сообщения в лог
        /// </summary>
        /// <param name="message">Сообщение</param>
        public static void WriteErrorToLog(string message)
        {
            try
            {
                //string xx = Path.ChangeExtension(Process.GetCurrentProcess().MainModule.FileName, "log");
                using (StreamWriter sw = new StreamWriter(Path.ChangeExtension(Process.GetCurrentProcess().MainModule.FileName, "log"), true))
                {
                    sw.WriteLine($"{DateTime.Now} > Error {message}");
                }
            }
            catch
            { }
        }

        /// <summary>
        /// Преобразование массива в строку
        /// </summary>
        /// <param name="id">Массив int</param>
        /// <returns>Строковое представление массива</returns>
        public static string ArrayToString(int[] id)
        {
            if (id == null || id.Length <= 0)
                return "[]";
            StringBuilder sb = new StringBuilder(10 * id.Length);
            sb.Append("[");
            sb.Append(id[0]);
            for (int i = 1; i < id.Length; i++)
            {
                sb.Append(", ");
                sb.Append(id[i]);
            }
            sb.Append("]");
            return sb.ToString();
        }
    }
}
