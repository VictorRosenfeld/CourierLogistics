
namespace SQLCLR_LogParser
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Text;
    using System.Windows.Forms;

    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void butFormatLog_Click(object sender, EventArgs e)
        {

            //string s1 = "2022-04-22 00:20:51.927";
            //string s2 = "2022-04-22 00:20:51.000";
            //DateTime t1 = DateTime.Parse(s1);
            //DateTime t2 = DateTime.Parse(s2);
            //double ts = (t1 - t2).TotalMilliseconds;

            //FormatLogFile(@"C:\T1\SQL_CLR.log");
            ofdSelectLogFile.Title = "Select log file";
            ofdSelectLogFile.Filter = "Log Files(*.log;*.txt)|*.log;*.txt|All files (*.*)|*.*";
            ofdSelectLogFile.Multiselect = false;
            //ofdSelectLogFile.RestoreDirectory = true;
            ofdSelectLogFile.SupportMultiDottedExtensions = false;
            ofdSelectLogFile.FileName = "";
            if (ofdSelectLogFile.ShowDialog(this) == DialogResult.Cancel)
                return;
            FormatLogFile(ofdSelectLogFile.FileName);
        }

        private int FormatLogFile(string filename)
        {
            // 1. Инициализация
            int rc = 1;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (!File.Exists(filename))
                    return rc;

                // 3. Читаем все строки
                rc = 3;
                string[] lines = File.ReadAllLines(filename, Encoding.UTF8);
                if (lines == null || lines.Length <= 0)
                    return rc;

                // 4. Создаём записи файла результата
                rc = 4;
                string resultFile = Path.ChangeExtension(filename, "csv");
                if (filename.Equals(resultFile, StringComparison.InvariantCultureIgnoreCase))
                    return rc;

                string[] resultLines = new string[lines.Length + 1];
                resultLines[0] = "date-time; msg_no; severity; message"; 
                int count = 1;
               
                // @date time [message_no] severity > message
                for (int i = 0; i < lines.Length; i++)
                {
                    // 4.1. Извлекаем строку
                    rc = 41;
                    string line = lines[i];
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    // 4.2. Извлекаем составляющие строки
                    rc = 42;
                    string dateTime = "";
                    string message_no = "";
                    string severity = "";
                    string message = "";

                    if (line.StartsWith("@"))
                    {
                        int iPos = line.IndexOf('>', 1);
                        if (iPos > 0)
                        {
                            string[] headerItems = line.Substring(1, iPos - 1).Trim().Split(' ');
                            switch (headerItems.Length)
                            {
                                case 4:
                                    dateTime = '@' + headerItems[0] + ' ' + headerItems[1];
                                    message_no = headerItems[2];
                                    severity = headerItems[3];
                                    message = line.Substring(iPos + 1).Trim();
                                    break;
                                case 2:
                                    dateTime = '@' + headerItems[0] + ' ' + headerItems[1];
                                    severity = headerItems[2];
                                    message = line.Substring(iPos + 1).Trim();
                                    break;
                                default:
                                    message = line;
                                    break;
                            }
                        }
                    }
                    else
                    {
                        message = line;
                    }

                    resultLines[count++] = $"{dateTime}; {message_no}; {severity}; {message}";
                }

                if (count <= 0)
                    return rc;

                if (count < resultLines.Length)
                {
                    Array.Resize(ref resultLines, count);
                }

                // 5. Сохраняем результат
                rc = 5;
                File.WriteAllLines(resultFile, resultLines, Encoding.GetEncoding(1251));

                // 6. Отображаем результат
                rc = 6;
                using (Process process = new Process())
                {
                    process.StartInfo.FileName = resultFile;
                    process.StartInfo.UseShellExecute = true;
                    process.Start();
                }

                // 7. Выход - Ok
                rc = 0;
                return rc;
            }
            catch
            {
                return rc;
            }
        }

        private void butCreateExcelFile_Click(object sender, EventArgs e)
        {
            try
            {
                ofdSelectLogFile.Title = "Select LS log file";
                ofdSelectLogFile.Filter = "Log Files(*.log;*.txt)|*.log;*.txt|All files (*.*)|*.*";
                ofdSelectLogFile.Multiselect = false;
                //ofdSelectLogFile.RestoreDirectory = true;
                ofdSelectLogFile.SupportMultiDottedExtensions = false;
                ofdSelectLogFile.FileName = "";
                if (ofdSelectLogFile.ShowDialog(this) == DialogResult.Cancel)
                    return;
                CreateExcelChart.Create(ofdSelectLogFile.FileName, int.Parse(txtServiceID.Text));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Create LS Excel file", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            txtServiceID.Text = Properties.Settings.Default.ServiceId.ToString();
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            int v;
            if (int.TryParse(txtServiceID.Text, out v))
            {
                Properties.Settings.Default.ServiceId = v;
                Properties.Settings.Default.Save();
            }
        }

        private void butCreateCmds_Click(object sender, EventArgs e)
        {
            try
            {
                ofdSelectLogFile.Title = "Select S1 log file";
                ofdSelectLogFile.Filter = "Log Files(*.log;*.txt)|*.log;*.txt|All files (*.*)|*.*";
                ofdSelectLogFile.Multiselect = false;
                //ofdSelectLogFile.RestoreDirectory = true;
                ofdSelectLogFile.SupportMultiDottedExtensions = false;
                ofdSelectLogFile.FileName = "";
                if (ofdSelectLogFile.ShowDialog(this) == DialogResult.Cancel)
                    return;
                CreateExcelChart.CreateS1(ofdSelectLogFile.FileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Create S1 Excel file", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }
    }
}
