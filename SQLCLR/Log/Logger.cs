
namespace SQLCLR.Log
{
    using System;
    //using System.IO;

    /// <summary>
    /// ������� ���������� ������
    /// </summary>
    public class Logger
    {
        private const string LOG_FILE_PATTERN = @"C:\LogisticsService\Log\LS_CLR_{0}.log";

        /// <summary>
        /// ������ ���������
        /// {0} - ����-�����
        /// {1} - ����� ���������
        /// {2} - ��� ���������
        /// {3} - ����� ���������
        /// </summary>
        private const string MESSAGE_PATTERN = @"@{0} {1} {2} > {3}";

        /// <summary>
        /// ��������� ��� ����� ����������� ����
        /// </summary>
        /// <returns>��� ����� ����</returns>
        private static string GetLogFileName()
        {
            return string.Format(LOG_FILE_PATTERN, DateTime.Now.ToString("yyyy-MM-dd"));
        }

        /// <summary>
        /// ������ � ���
        /// </summary>
        /// <param name="messageNo">����� ���������</param>
        /// <param name="message">����� ���������</param>
        /// <param name="severity">��� ��������� (-1 - �� ��������; 0 - info; 1 - warn; 2 - error</param>
        public static void WriteToLog(int messageNo, string message, int severity)
        {
            try
            {
                // 1. ����������� ��� ���������
                string messageType = null;
                switch (severity)
                {
                    case -1:
                        messageType = "";
                        break;
                    case 0:
                        messageType = "info";
                        break;
                    case 1:
                        messageType = "warn";
                        break;
                    case 2:
                        messageType = "error";
                        break;
                    default:
                        messageType = severity.ToString();
                        break;
                }

                // 2. ������� � ���
                using (StreamWriter sw = new StreamWriter(GetLogFileName(), true))
                {
                    sw.WriteLine(string.Format(MESSAGE_PATTERN, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"), messageNo, messageType, message));
                    sw.Close();
                }
            }
            catch
            {   }
        }
    }
}
