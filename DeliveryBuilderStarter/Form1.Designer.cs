namespace DeliveryBuilderStarter
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.label1 = new System.Windows.Forms.Label();
            this.txtServiceId = new System.Windows.Forms.TextBox();
            this.butStart = new System.Windows.Forms.Button();
            this.butStop = new System.Windows.Forms.Button();
            this.butShowLog = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.txtConnectionString = new System.Windows.Forms.TextBox();
            this.trayIcon = new System.Windows.Forms.NotifyIcon(this.components);
            this.trayContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.trayContextMenu_Open = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.trayContextMenu_Log = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
            this.trayContextMenu_Exit = new System.Windows.Forms.ToolStripMenuItem();
            this.trayContextMenu.SuspendLayout();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 47);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(76, 17);
            this.label1.TabIndex = 0;
            this.label1.Text = "Service ID:";
            // 
            // txtServiceId
            // 
            this.txtServiceId.Location = new System.Drawing.Point(140, 44);
            this.txtServiceId.MaxLength = 8;
            this.txtServiceId.Name = "txtServiceId";
            this.txtServiceId.Size = new System.Drawing.Size(93, 22);
            this.txtServiceId.TabIndex = 1;
            this.txtServiceId.Text = "59";
            // 
            // butStart
            // 
            this.butStart.Location = new System.Drawing.Point(18, 91);
            this.butStart.Name = "butStart";
            this.butStart.Size = new System.Drawing.Size(215, 42);
            this.butStart.TabIndex = 2;
            this.butStart.Text = "Start";
            this.butStart.UseVisualStyleBackColor = true;
            this.butStart.Click += new System.EventHandler(this.butStart_Click);
            // 
            // butStop
            // 
            this.butStop.Location = new System.Drawing.Point(538, 91);
            this.butStop.Name = "butStop";
            this.butStop.Size = new System.Drawing.Size(168, 42);
            this.butStop.TabIndex = 4;
            this.butStop.Text = "Stop";
            this.butStop.UseVisualStyleBackColor = true;
            this.butStop.Click += new System.EventHandler(this.butStop_Click);
            // 
            // butShowLog
            // 
            this.butShowLog.Location = new System.Drawing.Point(250, 91);
            this.butShowLog.Name = "butShowLog";
            this.butShowLog.Size = new System.Drawing.Size(168, 42);
            this.butShowLog.TabIndex = 3;
            this.butShowLog.Text = "Show Log";
            this.butShowLog.UseVisualStyleBackColor = true;
            this.butShowLog.Click += new System.EventHandler(this.butShowLog_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 19);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(122, 17);
            this.label2.TabIndex = 0;
            this.label2.Text = "Connection string:";
            // 
            // txtConnectionString
            // 
            this.txtConnectionString.Location = new System.Drawing.Point(140, 16);
            this.txtConnectionString.MaxLength = 32000;
            this.txtConnectionString.Name = "txtConnectionString";
            this.txtConnectionString.Size = new System.Drawing.Size(566, 22);
            this.txtConnectionString.TabIndex = 0;
            // 
            // trayIcon
            // 
            this.trayIcon.ContextMenuStrip = this.trayContextMenu;
            this.trayIcon.Icon = ((System.Drawing.Icon)(resources.GetObject("trayIcon.Icon")));
            this.trayIcon.Text = "Logistics Service";
            this.trayIcon.Visible = true;
            this.trayIcon.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.trayIcon_MouseDoubleClick);
            // 
            // trayContextMenu
            // 
            this.trayContextMenu.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.trayContextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.trayContextMenu_Open,
            this.toolStripSeparator2,
            this.trayContextMenu_Log,
            this.toolStripSeparator4,
            this.trayContextMenu_Exit});
            this.trayContextMenu.Name = "trayContextMenu";
            this.trayContextMenu.Size = new System.Drawing.Size(146, 88);
            // 
            // trayContextMenu_Open
            // 
            this.trayContextMenu_Open.Name = "trayContextMenu_Open";
            this.trayContextMenu_Open.Size = new System.Drawing.Size(145, 24);
            this.trayContextMenu_Open.Text = "Открыть...";
            this.trayContextMenu_Open.Click += new System.EventHandler(this.trayContextMenu_Open_Click);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(142, 6);
            // 
            // trayContextMenu_Log
            // 
            this.trayContextMenu_Log.Name = "trayContextMenu_Log";
            this.trayContextMenu_Log.Size = new System.Drawing.Size(145, 24);
            this.trayContextMenu_Log.Text = "Лог...";
            this.trayContextMenu_Log.Click += new System.EventHandler(this.trayContextMenu_Log_Click);
            // 
            // toolStripSeparator4
            // 
            this.toolStripSeparator4.Name = "toolStripSeparator4";
            this.toolStripSeparator4.Size = new System.Drawing.Size(142, 6);
            // 
            // trayContextMenu_Exit
            // 
            this.trayContextMenu_Exit.Name = "trayContextMenu_Exit";
            this.trayContextMenu_Exit.Size = new System.Drawing.Size(145, 24);
            this.trayContextMenu_Exit.Text = "Выход";
            this.trayContextMenu_Exit.Click += new System.EventHandler(this.trayContextMenu_Exit_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(724, 162);
            this.Controls.Add(this.butShowLog);
            this.Controls.Add(this.butStop);
            this.Controls.Add(this.butStart);
            this.Controls.Add(this.txtConnectionString);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.txtServiceId);
            this.Controls.Add(this.label1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "Form1";
            this.Text = "Logistics Service";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.Form1_FormClosed);
            this.Load += new System.EventHandler(this.Form1_Load);
            this.trayContextMenu.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txtServiceId;
        private System.Windows.Forms.Button butStart;
        private System.Windows.Forms.Button butStop;
        private System.Windows.Forms.Button butShowLog;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox txtConnectionString;
        private System.Windows.Forms.NotifyIcon trayIcon;
        private System.Windows.Forms.ContextMenuStrip trayContextMenu;
        private System.Windows.Forms.ToolStripMenuItem trayContextMenu_Open;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripMenuItem trayContextMenu_Log;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator4;
        private System.Windows.Forms.ToolStripMenuItem trayContextMenu_Exit;
    }
}

