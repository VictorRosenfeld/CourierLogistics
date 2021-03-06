﻿namespace CourierLogistics
{
    partial class MainForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.butStart = new System.Windows.Forms.Button();
            this.butRealModel = new System.Windows.Forms.Button();
            this.butFloatCouriers = new System.Windows.Forms.Button();
            this.butFloatOptimalModel = new System.Windows.Forms.Button();
            this.butCompareDistance = new System.Windows.Forms.Button();
            this.butStartFixedService = new System.Windows.Forms.Button();
            this.TrayIcon = new System.Windows.Forms.NotifyIcon(this.components);
            this.menuTrayIcon = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.menuTrayIcon_ShowLog = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.menuTrayIcon_Exit = new System.Windows.Forms.ToolStripMenuItem();
            this.menuTrayIcon_LogAnalysis = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.menuTrayIcon.SuspendLayout();
            this.SuspendLayout();
            // 
            // butStart
            // 
            this.butStart.Location = new System.Drawing.Point(22, 25);
            this.butStart.Name = "butStart";
            this.butStart.Size = new System.Drawing.Size(146, 46);
            this.butStart.TabIndex = 0;
            this.butStart.Text = "Start";
            this.butStart.UseVisualStyleBackColor = true;
            this.butStart.Visible = false;
            this.butStart.Click += new System.EventHandler(this.butStart_Click);
            // 
            // butRealModel
            // 
            this.butRealModel.Location = new System.Drawing.Point(22, 77);
            this.butRealModel.Name = "butRealModel";
            this.butRealModel.Size = new System.Drawing.Size(146, 48);
            this.butRealModel.TabIndex = 1;
            this.butRealModel.Text = "Real Model";
            this.butRealModel.UseVisualStyleBackColor = true;
            this.butRealModel.Visible = false;
            this.butRealModel.Click += new System.EventHandler(this.butRealModel_Click);
            // 
            // butFloatCouriers
            // 
            this.butFloatCouriers.Location = new System.Drawing.Point(22, 131);
            this.butFloatCouriers.Name = "butFloatCouriers";
            this.butFloatCouriers.Size = new System.Drawing.Size(146, 48);
            this.butFloatCouriers.TabIndex = 2;
            this.butFloatCouriers.Text = "Float Model";
            this.butFloatCouriers.UseVisualStyleBackColor = true;
            this.butFloatCouriers.Visible = false;
            this.butFloatCouriers.Click += new System.EventHandler(this.butFloatCouriers_Click);
            // 
            // butFloatOptimalModel
            // 
            this.butFloatOptimalModel.Location = new System.Drawing.Point(22, 185);
            this.butFloatOptimalModel.Name = "butFloatOptimalModel";
            this.butFloatOptimalModel.Size = new System.Drawing.Size(146, 48);
            this.butFloatOptimalModel.TabIndex = 3;
            this.butFloatOptimalModel.Text = "FloatOptimalModel";
            this.butFloatOptimalModel.UseVisualStyleBackColor = true;
            this.butFloatOptimalModel.Visible = false;
            this.butFloatOptimalModel.Click += new System.EventHandler(this.butFloatOptimalModel_Click);
            // 
            // butCompareDistance
            // 
            this.butCompareDistance.Location = new System.Drawing.Point(194, 25);
            this.butCompareDistance.Name = "butCompareDistance";
            this.butCompareDistance.Size = new System.Drawing.Size(146, 46);
            this.butCompareDistance.TabIndex = 4;
            this.butCompareDistance.Text = "Compare Distance";
            this.butCompareDistance.UseVisualStyleBackColor = true;
            this.butCompareDistance.Visible = false;
            this.butCompareDistance.Click += new System.EventHandler(this.butCompareDistance_Click);
            // 
            // butStartFixedService
            // 
            this.butStartFixedService.Location = new System.Drawing.Point(194, 77);
            this.butStartFixedService.Name = "butStartFixedService";
            this.butStartFixedService.Size = new System.Drawing.Size(146, 48);
            this.butStartFixedService.TabIndex = 5;
            this.butStartFixedService.Text = "Fixed Service";
            this.butStartFixedService.UseVisualStyleBackColor = true;
            this.butStartFixedService.Click += new System.EventHandler(this.butStartFixedService_Click);
            // 
            // TrayIcon
            // 
            this.TrayIcon.ContextMenuStrip = this.menuTrayIcon;
            this.TrayIcon.Icon = ((System.Drawing.Icon)(resources.GetObject("TrayIcon.Icon")));
            this.TrayIcon.Text = "notifyIcon1";
            this.TrayIcon.Visible = true;
            // 
            // menuTrayIcon
            // 
            this.menuTrayIcon.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.menuTrayIcon.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuTrayIcon_ShowLog,
            this.toolStripSeparator1,
            this.menuTrayIcon_LogAnalysis,
            this.toolStripSeparator2,
            this.menuTrayIcon_Exit});
            this.menuTrayIcon.Name = "menuTrayIcon";
            this.menuTrayIcon.Size = new System.Drawing.Size(211, 116);
            // 
            // menuTrayIcon_ShowLog
            // 
            this.menuTrayIcon_ShowLog.Name = "menuTrayIcon_ShowLog";
            this.menuTrayIcon_ShowLog.Size = new System.Drawing.Size(210, 24);
            this.menuTrayIcon_ShowLog.Text = "Show Log";
            this.menuTrayIcon_ShowLog.ToolTipText = "Show log";
            this.menuTrayIcon_ShowLog.Click += new System.EventHandler(this.menuTrayIcon_ShowLog_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(207, 6);
            // 
            // menuTrayIcon_Exit
            // 
            this.menuTrayIcon_Exit.Name = "menuTrayIcon_Exit";
            this.menuTrayIcon_Exit.Size = new System.Drawing.Size(210, 24);
            this.menuTrayIcon_Exit.Text = "Exit";
            this.menuTrayIcon_Exit.ToolTipText = "Exit";
            this.menuTrayIcon_Exit.Click += new System.EventHandler(this.menuTrayIcon_Exit_Click);
            // 
            // menuTrayIcon_LogAnalysis
            // 
            this.menuTrayIcon_LogAnalysis.Name = "menuTrayIcon_LogAnalysis";
            this.menuTrayIcon_LogAnalysis.Size = new System.Drawing.Size(210, 24);
            this.menuTrayIcon_LogAnalysis.Text = "Log Analysis";
            this.menuTrayIcon_LogAnalysis.ToolTipText = "Log Analysis";
            this.menuTrayIcon_LogAnalysis.Click += new System.EventHandler(this.menuTrayIcon_LogAnalysis_Click);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(207, 6);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.butStartFixedService);
            this.Controls.Add(this.butCompareDistance);
            this.Controls.Add(this.butFloatOptimalModel);
            this.Controls.Add(this.butFloatCouriers);
            this.Controls.Add(this.butRealModel);
            this.Controls.Add(this.butStart);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "MainForm";
            this.Text = "Анализ и построение логистики курьеров";
            this.WindowState = System.Windows.Forms.FormWindowState.Minimized;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.MainForm_FormClosed);
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.Shown += new System.EventHandler(this.MainForm_Shown);
            this.menuTrayIcon.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button butStart;
        private System.Windows.Forms.Button butRealModel;
        private System.Windows.Forms.Button butFloatCouriers;
        private System.Windows.Forms.Button butFloatOptimalModel;
        private System.Windows.Forms.Button butCompareDistance;
        private System.Windows.Forms.Button butStartFixedService;
        private System.Windows.Forms.NotifyIcon TrayIcon;
        private System.Windows.Forms.ContextMenuStrip menuTrayIcon;
        private System.Windows.Forms.ToolStripMenuItem menuTrayIcon_ShowLog;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem menuTrayIcon_Exit;
        private System.Windows.Forms.ToolStripMenuItem menuTrayIcon_LogAnalysis;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
    }
}

