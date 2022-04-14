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

        private void butStart_Click(object sender, EventArgs e)
        {
            // 1. Иициализация
            tickCount = 0;
            txtElapsedTime.Text = null;
            txtRc.Text = null;

            try
            {
                int id = int.Parse(txtServiceID.Text);
                string serverName = txtServerName.Text;
                string dbName = txtDbName.Text;
                DateTime calcTime = DateTime.Parse(txtCalcTime.Text);
                tmrElapsedTime.Enabled = true;

                tsk = Task.Run(() => StoredProcedures.CreateDeliveriesEx(id, calcTime, serverName, dbName));
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
                txtRc.Text = tsk.Result.ToString();
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
    }
}
