namespace SQLCLRex
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
            this.label1 = new System.Windows.Forms.Label();
            this.txtServiceID = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.txtDbName = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.txtServerName = new System.Windows.Forms.TextBox();
            this.butStart = new System.Windows.Forms.Button();
            this.tmrElapsedTime = new System.Windows.Forms.Timer(this.components);
            this.txtElapsedTime = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.txtCalcTime = new System.Windows.Forms.TextBox();
            this.txtRc = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(19, 76);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(76, 17);
            this.label1.TabIndex = 0;
            this.label1.Text = "Service ID:";
            // 
            // txtServiceID
            // 
            this.txtServiceID.Location = new System.Drawing.Point(123, 73);
            this.txtServiceID.Name = "txtServiceID";
            this.txtServiceID.Size = new System.Drawing.Size(68, 22);
            this.txtServiceID.TabIndex = 1;
            this.txtServiceID.Text = "59";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(19, 48);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(72, 17);
            this.label2.TabIndex = 0;
            this.label2.Text = "DB Name:";
            // 
            // txtDbName
            // 
            this.txtDbName.Location = new System.Drawing.Point(123, 45);
            this.txtDbName.Name = "txtDbName";
            this.txtDbName.Size = new System.Drawing.Size(162, 22);
            this.txtDbName.TabIndex = 1;
            this.txtDbName.Text = "logisticservice59";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(19, 20);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(95, 17);
            this.label3.TabIndex = 0;
            this.label3.Text = "Server Name:";
            // 
            // txtServerName
            // 
            this.txtServerName.Location = new System.Drawing.Point(123, 17);
            this.txtServerName.Name = "txtServerName";
            this.txtServerName.Size = new System.Drawing.Size(162, 22);
            this.txtServerName.TabIndex = 1;
            this.txtServerName.Text = "srv-sql15";
            // 
            // butStart
            // 
            this.butStart.Location = new System.Drawing.Point(22, 157);
            this.butStart.Name = "butStart";
            this.butStart.Size = new System.Drawing.Size(263, 51);
            this.butStart.TabIndex = 2;
            this.butStart.Text = "Start";
            this.butStart.UseVisualStyleBackColor = true;
            this.butStart.Click += new System.EventHandler(this.butStart_Click);
            // 
            // tmrElapsedTime
            // 
            this.tmrElapsedTime.Interval = 1000;
            this.tmrElapsedTime.Tick += new System.EventHandler(this.tmrElapsedTime_Tick);
            // 
            // txtElapsedTime
            // 
            this.txtElapsedTime.Location = new System.Drawing.Point(22, 224);
            this.txtElapsedTime.Name = "txtElapsedTime";
            this.txtElapsedTime.ReadOnly = true;
            this.txtElapsedTime.Size = new System.Drawing.Size(263, 22);
            this.txtElapsedTime.TabIndex = 3;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(19, 104);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(74, 17);
            this.label4.TabIndex = 0;
            this.label4.Text = "Calc Time:";
            // 
            // txtCalcTime
            // 
            this.txtCalcTime.Location = new System.Drawing.Point(123, 101);
            this.txtCalcTime.Name = "txtCalcTime";
            this.txtCalcTime.Size = new System.Drawing.Size(162, 22);
            this.txtCalcTime.TabIndex = 1;
            this.txtCalcTime.Text = "05.04.2022 08:00:00";
            // 
            // txtRc
            // 
            this.txtRc.Location = new System.Drawing.Point(22, 252);
            this.txtRc.Name = "txtRc";
            this.txtRc.ReadOnly = true;
            this.txtRc.Size = new System.Drawing.Size(263, 22);
            this.txtRc.TabIndex = 3;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(345, 309);
            this.Controls.Add(this.txtRc);
            this.Controls.Add(this.txtElapsedTime);
            this.Controls.Add(this.butStart);
            this.Controls.Add(this.txtServerName);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.txtDbName);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.txtCalcTime);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.txtServiceID);
            this.Controls.Add(this.label1);
            this.MaximizeBox = false;
            this.Name = "Form1";
            this.Text = "Create Deliveries";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.Form1_FormClosed);
            this.Load += new System.EventHandler(this.Form1_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txtServiceID;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox txtDbName;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox txtServerName;
        private System.Windows.Forms.Button butStart;
        private System.Windows.Forms.Timer tmrElapsedTime;
        private System.Windows.Forms.TextBox txtElapsedTime;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox txtCalcTime;
        private System.Windows.Forms.TextBox txtRc;
    }
}

