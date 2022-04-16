using SQLCLRex.Deliveries;
using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SQLCLRex
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private int tickCount = 0;
        Task<int> tsk = null;
        CalcConfig config = null;

        private void butStart_Click(object sender, EventArgs e)
        {
            // 1. Иициализация
            tickCount = 0;
            txtElapsedTime.Text = null;
            txtRc.Text = null;
            config = null;

            try
            {
                config = GetConfig();
                if (config == null)
                {
                    txtRc.Text = "-1";
                    //return;
                }
                butStart.Enabled = false;
                tmrElapsedTime.Enabled = true;

                tsk = Task.Run(() => StoredProcedures.CreateDeliveriesEx(config));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Create Deliveries", MessageBoxButtons.OK, MessageBoxIcon.Error);
                tmrElapsedTime.Enabled = false;
                butStart.Enabled = true;
            }
            //finally
            //{
            //    tmrElapsedTime.Enabled = false;
            //    butStart.Enabled = true;
            //}
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            LoadProperties();
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            SaveProperties();
        }

        private void tmrElapsedTime_Tick(object sender, EventArgs e)
        {
            tickCount++;
            TimeSpan ts = TimeSpan.FromSeconds(tickCount);
            txtElapsedTime.Text = ts.ToString();
            if (tsk == null)
            {
                tmrElapsedTime.Enabled = false;
                butStart.Enabled = true;
            }
            if (tsk.Status == TaskStatus.Canceled ||
                tsk.Status == TaskStatus.Faulted ||
                tsk.Status == TaskStatus.RanToCompletion)
            {
                tmrElapsedTime.Enabled = false;
                butStart.Enabled = true;
                txtRc.Text = config.ExitCode.ToString();
                txtElapsedTime.Text = ((int) config.ElapsedTime.TotalMilliseconds).ToString();
                tsk.Dispose();
                tsk = null;
            }
        }

        private void SaveProperties()
        {
            try
            {
                Properties.Settings.Default.ServerName = txtServerName.Text;
                Properties.Settings.Default.DbName = txtDbName.Text;

                int value;
                if (int.TryParse(txtServiceID.Text, out value))
                { Properties.Settings.Default.ServiceID =value; }

                DateTime dt;
                if (DateTime.TryParse(txtCalcTime.Text, out dt))
                { Properties.Settings.Default.CalcTime =dt; }

                if (int.TryParse(txtCloudRadius.Text, out value))
                { Properties.Settings.Default.CloudRadius =value; }

                if (int.TryParse(txtCloud5Size.Text, out value))
                { Properties.Settings.Default.Cloud5Size =value; }

                if (int.TryParse(txtCloud6Size.Text, out value))
                { Properties.Settings.Default.Cloud6Size =value; }

                if (int.TryParse(txtCloud7Size.Text, out value))
                { Properties.Settings.Default.Cloud7Size =value; }

                if (int.TryParse(txtCloud8Size.Text, out value))
                { Properties.Settings.Default.Cloud8Size =value; }

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
                txtServiceID.Text = Properties.Settings.Default.ServiceID.ToString();
                txtCalcTime.Text = Properties.Settings.Default.CalcTime.ToString();
                txtCloudRadius.Text = Properties.Settings.Default.CloudRadius.ToString();
                txtCloud5Size.Text = Properties.Settings.Default.Cloud5Size.ToString();
                txtCloud6Size.Text = Properties.Settings.Default.Cloud6Size.ToString();
                txtCloud7Size.Text = Properties.Settings.Default.Cloud7Size.ToString();
                txtCloud8Size.Text = Properties.Settings.Default.Cloud8Size.ToString();
            }
            catch
            { }
        }

        /// <summary>
        /// Выбор установленных парамтров расчета
        /// </summary>
        /// <returns></returns>
        private CalcConfig GetConfig()
        {
            // 1. Инициализация
            CalcConfig config = new CalcConfig();

            // 2. ServerName
            if (string.IsNullOrWhiteSpace(txtServerName.Text))
            {
                MessageBox.Show("Server Name не задан", "Create Deliveries", MessageBoxButtons.OK, MessageBoxIcon.Information);
                txtServerName.Focus();
                return null;
            }

            config.ServerName = txtServerName.Text;

            // 3. DbName
            if (string.IsNullOrWhiteSpace(txtDbName.Text))
            {
                MessageBox.Show("DB Name не задан", "Create Deliveries", MessageBoxButtons.OK, MessageBoxIcon.Information);
                txtDbName.Focus();
                return null;
            }

            config.DbName = txtDbName.Text;

            // 4. ServiceId
            int value;
            if (!int.TryParse(txtServiceID.Text, out value) || value <= 0)
            {
                MessageBox.Show("Service ID не задан или имеет недопустимое значение", "Create Deliveries", MessageBoxButtons.OK, MessageBoxIcon.Information);
                txtServiceID.SelectAll();
                txtServiceID.Focus();
                return null;
            }

            config.ServiceId = value;

            // 5. Calc Time
            DateTime dt;
            if (!DateTime.TryParse(txtCalcTime.Text, out dt))
            {
                MessageBox.Show("Calc Time не задан или имеет недопустимое значение", "Create Deliveries", MessageBoxButtons.OK, MessageBoxIcon.Information);
                txtCalcTime.SelectAll();
                txtCalcTime.Focus();
                return null;
            }

            config.CalcTime = dt;

            // 6. Cloud Readius
            if (!int.TryParse(txtCloudRadius.Text, out value) || value <= 0)
            {
                MessageBox.Show("Cloud Readius не задан или имеет недопустимое значение", "Create Deliveries", MessageBoxButtons.OK, MessageBoxIcon.Information);
                txtCloudRadius.SelectAll();
                txtCloudRadius.Focus();
                return null;
            }

            config.CloudRadius = value;

            // 7. Cloud5 Size
            if (!int.TryParse(txtCloud5Size.Text, out value) || value <= 0)
            {
                MessageBox.Show("Cloud5 Size не задан или имеет недопустимое значение", "Create Deliveries", MessageBoxButtons.OK, MessageBoxIcon.Information);
                txtCloud5Size.SelectAll();
                txtCloud5Size.Focus();
                return null;
            }

            config.Cloud5Size = value;

            // 8. Cloud6 Size
            if (!int.TryParse(txtCloud6Size.Text, out value) || value <= 0)
            {
                MessageBox.Show("Cloud6 Size не задан или имеет недопустимое значение", "Create Deliveries", MessageBoxButtons.OK, MessageBoxIcon.Information);
                txtCloud6Size.SelectAll();
                txtCloud6Size.Focus();
                return null;
            }

            config.Cloud6Size = value;

            // 9. Cloud7 Size
            if (!int.TryParse(txtCloud7Size.Text, out value) || value <= 0)
            {
                MessageBox.Show("Cloud7 Size не задан или имеет недопустимое значение", "Create Deliveries", MessageBoxButtons.OK, MessageBoxIcon.Information);
                txtCloud7Size.SelectAll();
                txtCloud7Size.Focus();
                return null;
            }

            config.Cloud7Size = value;

            // 10. Cloud8 Size
            if (!int.TryParse(txtCloud8Size.Text, out value) || value <= 0)
            {
                MessageBox.Show("Cloud8 Size не задан или имеет недопустимое значение", "Create Deliveries", MessageBoxButtons.OK, MessageBoxIcon.Information);
                txtCloud8Size.SelectAll();
                txtCloud8Size.Focus();
                return null;
            }

            config.Cloud8Size = value;

            return config;
        }
    }
}
