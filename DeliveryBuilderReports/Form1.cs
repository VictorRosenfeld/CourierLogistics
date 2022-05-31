
namespace DeliveryBuilderReports
{
    using System;
    using System.IO;
    using System.Windows.Forms;

    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Создание отчета
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void butCreate_Click(object sender, EventArgs e)
        {
            try
            {
                // 2. Проверяем исходные данные
                string logFile = txtLogFile.Text;
                if (string.IsNullOrWhiteSpace(logFile))
                {
                    MessageBox.Show("Файл лога не задан", "Create Delivery Builder Report", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    txtLogFile.Focus();
                    return;
                }

                if (!File.Exists(logFile))
                {
                    MessageBox.Show("Файл лога не найден", "Create Delivery Builder Report", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    txtLogFile.SelectAll();
                    txtLogFile.Focus();
                    return;
                }

                // 3. Создаём отчет
                int rc = ReportCreator.Create(logFile, txtOrdersFile.Text, "DeliveryBuilderReport.xlsx");
                if (rc != 0)
                {
                    MessageBox.Show($"Не удалось построить отчет (rc = {rc})", "Create Delivery Builder Report", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Create Delivery Builder Report", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }

        /// <summary>
        /// Выбор файла лога
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void butSelectLogFile_Click(object sender, EventArgs e)
        {
            try
            {
                ofdSelectLogFile.Title = "Select Log File";
                ofdSelectLogFile.Filter = "Log Files(*.log;*.txt)|*.log;*.txt|All files (*.*)|*.*";
                ofdSelectLogFile.Multiselect = false;
                ofdSelectLogFile.SupportMultiDottedExtensions = false;
                ofdSelectLogFile.FileName = "";
                if (ofdSelectLogFile.ShowDialog(this) == DialogResult.Cancel)
                    return;
                txtLogFile.Text = ofdSelectLogFile.FileName;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Select Log File", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }

        /// <summary>
        /// Выбор файла с заказами
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void butSelectOrdersFile_Click(object sender, EventArgs e)
        {
            try
            {
                ofdSelectLogFile.Title = "Select Orders File";
                ofdSelectLogFile.Filter = "Order Files(*.csv;*.txt)|*.csv;*.txt|All files (*.*)|*.*";
                ofdSelectLogFile.Multiselect = false;
                ofdSelectLogFile.SupportMultiDottedExtensions = false;
                ofdSelectLogFile.FileName = "";
                if (ofdSelectLogFile.ShowDialog(this) == DialogResult.Cancel)
                    return;
                txtOrdersFile.Text = ofdSelectLogFile.FileName;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Select Orders File", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
