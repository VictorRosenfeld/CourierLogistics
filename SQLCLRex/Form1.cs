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
            txtCalcTime.Text = Properties.Settings.Default.CalcTime.ToString();
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            try
            { Properties.Settings.Default.CalcTime = DateTime.Parse(txtCalcTime.Text); } catch { }
            Properties.Settings.Default.Save();
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
    }
}
