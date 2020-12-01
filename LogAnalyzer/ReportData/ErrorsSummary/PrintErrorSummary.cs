
namespace LogAnalyzer.ReportData.ErrorsSummary
{
    using ClosedXML.Excel;
    using System;

    /// <summary>
    /// Печать сообщений об ошибках
    /// </summary>
    public class PrintErrorSummary
    {
        #region Column settings

        private const int MSG_TIME_COLUMN = 2;
        private const int MSG_NO_COLUMN = 3;
        private const int METHOD_COLUMN = 4;
        private const int MSG_RC_COLUMN = 5;
        private const int MSG_COLUMN = 6;

        #endregion Column settings

        /// <summary>
        /// Печать сообщения об ошибке
        /// </summary>
        /// <param name="logDateTime">Дата-время, когда произошло событие</param>
        /// <param name="msgNo">Номер сообщения</param>
        /// <param name="code">Код возврата метода, в котором произошла ошибка</param>
        /// <param name="methodName">Название метода</param>
        /// <param name="message">Текст сообщения</param>
        /// <param name="sheet">Лист книги Excel</param>
        /// <param name="row">Номер последней напечатанной строки</param>
        /// <returns>0 - сообщение напечатано; иначе - сообщение не напечатано</returns>
        public static int Print(DateTime logDateTime, int msgNo, int code, string methodName, string message, IXLWorksheet sheet, ref int row)
        {
            // 1. Инициализация
            int rc = 1;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (sheet == null)
                    return rc;
                if (row <= 0)
                    row = 1;

                // 3. Печатаем сообщение
                rc = 3;
                int r = row + 1;
                sheet.Cell(r, MSG_TIME_COLUMN).SetValue(logDateTime);
                sheet.Cell(r, MSG_NO_COLUMN).SetValue(msgNo);
                sheet.Cell(r, METHOD_COLUMN).SetValue(methodName);
                if (code >= 0)
                    sheet.Cell(r, MSG_RC_COLUMN).SetValue(code);
                sheet.Cell(r, MSG_COLUMN).SetValue(message);

                row = r;

                // 4. Выход - Ok
                rc = 0;
                return rc;
            }
            catch
            {
                return rc;
            }
        }
    }
}
