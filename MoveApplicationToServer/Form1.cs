
namespace MoveApplicationToServer
{
    using System;
    using System.Data.SqlClient;
    using System.IO;
    using System.Windows.Forms;

    public partial class Form1 : Form
    {
        private const string sqlCopy =
        "USE {0} " +
        "DECLARE @RC int " +
        "DECLARE @filename varchar(256) = '{1}' " +
        "DECLARE @filebytes varbinary(max) " +
        "SELECT @filebytes = cdfFile FROM srvProgram WHERE cdfID = 1; " +
        "EXECUTE @RC = [dbo].[lsvSaveFile] @filename, @filebytes " +
        "SELECT @RC; ";

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            lblFilename.Text = null;
            LoadProperties();
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            SaveProperties();
        }

        private void SaveProperties()
        {
            try
            {
                Properties.Settings.Default.ServerName = txtServerName.Text;
                Properties.Settings.Default.DbName = txtDbName.Text;
                Properties.Settings.Default.ServerFolder = txtServerFolder.Text;

                Properties.Settings.Default.Save();
            }
            catch
            { }
        }

        private void LoadProperties()
        {
            try
            {
                txtServerName.Text = Properties.Settings.Default.ServerName;
                txtDbName.Text = Properties.Settings.Default.DbName;
                txtServerFolder.Text = Properties.Settings.Default.ServerFolder;
            }
            catch
            { }
        }

        private void butCopy_Click(object sender, EventArgs e)
        {
            try
            {
                // 2. Выбираем файлы
                string[] files = SelectFiles();
                if (files == null || files.Length <= 0)
                    return;

                // 3. Откррываем соединение с БД
                using (SqlConnection connection = new SqlConnection($"Server={txtServerName.Text};Database={txtDbName.Text};Integrated Security=true"))
                {
                    // 3.1 Открываем соединение
                    connection.Open();

                    // 3.2 Копируем файлы
                    foreach (var file in files)
                    {
                        lblFilename.Text = Path.GetFileName(file) + "...";
                        Application.DoEvents();
                        int rc = CopyFile(file, connection);
                        if (rc != 0)
                        {
                            MessageBox.Show($"Не удалось скопировать файл {file}", "Copy", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                    }
                }

                // 4. Все файлы успешно скопированы
                MessageBox.Show($"Успешно скопировано файлов - {files.Length}", "Copy", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Copy", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                lblFilename.Text = null;
            }
        }

        private string[] SelectFiles()
        {
            ofdNomFile.Title = "Выбор копируемых файлов";
            ofdNomFile.Filter = "All files(*.*) | *.*";
            ofdNomFile.Multiselect = true;
            ofdNomFile.RestoreDirectory = false;

            if (ofdNomFile.ShowDialog(this) == DialogResult.Cancel)
                return null;

            return ofdNomFile.FileNames;
        }

        /// <summary>
        /// Копирование файла из локальной машины в файл на сервере
        /// </summary>
        /// <param name="filename">Путь к исходному файлу</param>
        /// <param name="connection">SQL connection</param>
        /// <returns>0 - файл скопирован; иначе - файл не скопиован</returns>
        private int CopyFile(string filename, SqlConnection connection)
        {
            // 1. Инициализация
            int rc = 1;

            try
            {
                // 2. Поверяем исходные данные
                rc = 2;
                if (string.IsNullOrWhiteSpace(filename) || !File.Exists(filename))
                    return rc;
                if (connection == null || connection.State == System.Data.ConnectionState.Closed)
                    return rc;

                // 3. Очищаем вспомогательную таблицу
                rc = 3;
                using (SqlCommand cmd = new SqlCommand("DELETE FROM srvProgram", connection))
                { cmd.ExecuteNonQuery(); }

                // 4. Загружаем файл
                rc = 4;
                byte[] bytes = File.ReadAllBytes(filename);

                // 5. Сохраняем файл в таблице
                rc = 5;
                using (SqlCommand cmd = new SqlCommand("INSERT INTO srvProgram(cdfID, cdfFile) VALUES(1, @File)", connection))
                {
                    cmd.Parameters.Add("@File", System.Data.SqlDbType.VarBinary, bytes.Length).Value = bytes;
                    cmd.ExecuteNonQuery();
                }

                // 6. Выгружаем сохраненные байты в файл на сервере
                rc = 6;
                string serverFilename = Path.Combine(txtServerFolder.Text, Path.GetFileName(filename));
                string sqlText = string.Format(sqlCopy, connection.Database, serverFilename);
                using (SqlCommand cmd = new SqlCommand(sqlText, connection))
                {
                    var ret = cmd.ExecuteScalar();
                    if (ret is int)
                        return 100 * (int)ret;
                }

                return rc;
            }
            catch (Exception ex)
            {
                string message = (ex.InnerException == null ? ex.Message : ex.InnerException.Message);
                MessageBox.Show(message, "Copy", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return rc;
            }
        }
    }
}
