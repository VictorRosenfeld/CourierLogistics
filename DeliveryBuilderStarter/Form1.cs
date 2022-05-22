
namespace DeliveryBuilderStarter
{
    using DeliveryBuilder;
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Windows.Forms;

    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        LogisticsService[] startedLs;

        /// <summary>
        /// Событие открытия формы
        /// </summary>
        /// <param name="sender">this Form</param>
        /// <param name="e">Аргументы события</param>
        private void Form1_Load(object sender, EventArgs e)
        {
            txtServiceId.Text = Properties.Settings.Default.ServiceID.ToString();
            txtConnectionString.Text = Properties.Settings.Default.ConnectionString;
        }

        /// <summary>
        /// Событие закрытия формы
        /// </summary>
        /// <param name="sender">this Form</param>
        /// <param name="e">Аргументы события</param>
        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            int serviceId;
            if (int.TryParse(txtServiceId.Text, out serviceId))
            { Properties.Settings.Default.ServiceID = serviceId; }
            Properties.Settings.Default.ConnectionString = txtConnectionString.Text;
            Properties.Settings.Default.Save();
            CloseAllServices();
        }

        /// <summary>
        /// Запуск сервиса логистики
        /// </summary>
        /// <param name="sender">Button</param>
        /// <param name="e">Аргументы события</param>
        private void butStart_Click(object sender, EventArgs e)
        {
            try
            {
                // 1. Извлекаем Service ID
                int serviceId;
                if (!TryGetServiceId(out serviceId))
                    return;

                // 2. Извлекаем Connection string
                string connectionString = txtConnectionString.Text;
                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    MessageBox.Show("Строка подключения не задана", "Logistics Service", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    txtConnectionString.SelectAll();
                    txtConnectionString.Focus();
                    return;
                }

                // 2. Ищем сервис среди запущенных
                int lsIndex = IndexOfLogisticsService(startedLs, serviceId);
                if (lsIndex >= 0)
                {
                    MessageBox.Show("Сервис логистики уже запущен", "Logistics Service", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    txtServiceId.SelectAll();
                    txtServiceId.Focus();
                    return;
                }

                // 3. Создаём новый сервис логистики
                LogisticsService ls = new LogisticsService();
                int rc = ls.Create(serviceId, connectionString);
                if (rc != 0)
                {
                    if (ls.LastException == null)
                    {
                        MessageBox.Show($"Сервис логистики не создан (rc = {rc})", "Logistics Service", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    else
                    {
                        MessageBox.Show($"Сервис логистики не создан (rc = {rc}) Exception: {(ls.LastException.InnerException == null ? ls.LastException.Message : ls.LastException.InnerException.Message)}", "Logistics Service", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    ls.Close();
                    return;
                }

                // 4. Сохраняем сервис логистики
                if (startedLs == null)
                { startedLs = new LogisticsService[] { ls }; }
                else
                {
                    Array.Resize(ref startedLs, startedLs.Length + 1);
                    startedLs[startedLs.Length - 1] = ls;
                }

                // 5. Финальное сообшение
                MessageBox.Show($"Сервис логистики {serviceId} запущен", "Logistics Service", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.InnerException == null ? ex.Message : ex.InnerException.Message, "Logistics Service", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Ликвидация сервиса логистики
        /// </summary>
        /// <param name="sender">Button</param>
        /// <param name="e">Аргументы события</param>
        private void butStop_Click(object sender, EventArgs e)
        {
            try
            {
                // 1. Извлекаем Service ID
                int serviceId;
                if (!TryGetServiceId(out serviceId))
                    return;

                // 2. Ищем сервис среди запущенных
                int lsIndex = IndexOfLogisticsService(startedLs, serviceId);
                if (lsIndex < 0)
                {
                    MessageBox.Show("Сервис логистики не запущен", "Logistics Service", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    txtServiceId.SelectAll();
                    txtServiceId.Focus();
                    return;
                }

                // 3. Закываем сервис логистики
                LogisticsService ls = startedLs[lsIndex];
                ls.Close();

                // 4. Удаляем сервис логистики из массива
                if (startedLs.Length == 1)
                { startedLs = new LogisticsService[0]; }
                else
                {
                    int count = 0;
                    for (int i = 0; i < startedLs.Length; i++)
                    {
                        if (i != lsIndex)
                        { startedLs[count++] = startedLs[i]; }
                    }

                    Array.Resize(ref startedLs, count);
                }

                // 5. Финальное сообшение
                MessageBox.Show($"Сервис логистики {serviceId} остановлен", "Logistics Service", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.InnerException == null ? ex.Message : ex.InnerException.Message, "Logistics Service", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }

        /// <summary>
        /// Отображение лога сервиса логистики
        /// </summary>
        /// <param name="sender">Button</param>
        /// <param name="e">Аргументы события</param>
        private void butShowLog_Click(object sender, EventArgs e)
        {
            try
            {
                // 1. Извлекаем Service ID
                int serviceId;
                if (!TryGetServiceId(out serviceId))
                    return;

                // 2. Ищем сервис среди запущенных
                int lsIndex = IndexOfLogisticsService(startedLs, serviceId);
                if (lsIndex < 0)
                {
                    MessageBox.Show("Сервис логистики не запущен", "Logistics Service", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    txtServiceId.SelectAll();
                    txtServiceId.Focus();
                    return;
                }

                // 3. Извлекаем путь к логу сервиса логистики
                LogisticsService ls = startedLs[lsIndex];
                string logFile = ls.LogFile;
                if (string.IsNullOrWhiteSpace(logFile) ||
                    !File.Exists(logFile))
                {
                    MessageBox.Show($"Файл лога сервиса логистики {serviceId} не найден", "Logistics Service", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // 4. Открываем файл лога
                ProcessStartInfo si = new ProcessStartInfo(logFile, null);
                si.UseShellExecute = true;
                Process.Start(si);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.InnerException == null ? ex.Message : ex.InnerException.Message, "Logistics Service", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }

        /// <summary>
        /// Получение Service ID из text box
        /// </summary>
        /// <param name="serviceId"></param>
        /// <returns>true - Service ID получен; false - Service ID не получен</returns>
        private bool TryGetServiceId(out int serviceId)
        {
            if (int.TryParse(txtServiceId.Text, out serviceId))
                return true;
            MessageBox.Show("Service ID не задан или имеет недопусимое значение", "Logistics Service", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            txtServiceId.SelectAll();
            txtServiceId.Focus();
            return false;
        }

        /// <summary>
        /// Поиск заданного сервиса логистики
        /// </summary>
        /// <param name="services">Массив сервисов логистики</param>
        /// <param name="serviceId">ID разыскиваемого сервиса логистики</param>
        /// <returns>индек сервиса в массиве или -1</returns>
        private static int IndexOfLogisticsService(LogisticsService[] services,  int serviceId)
        {
            if (services == null || services.Length <= 0)
                return -1;
            for (int i = 0; i < services.Length; i++) 
            {
                var ls = services[i];
                if (ls.ServiceId == serviceId && ls.IsCreated)
                    return i;
            }

            return -1;
        }

        /// <summary>
        /// Закрытие всех сервисов
        /// </summary>
        private void CloseAllServices()
        {
            try
            {
                // 2. Проверяем исходные данные
                if (startedLs == null || startedLs.Length <= 0)
                    return;

                // 3. Закрываем все сервисы
                foreach (var ls in startedLs)
                {
                    if (ls != null)
                    {
                        try { ls.Close(); } catch { }
                    }
                }

                startedLs = null;
            }
            catch
            { }
        }
    }
}
