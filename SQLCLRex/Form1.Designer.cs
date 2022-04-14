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
            this.label5 = new System.Windows.Forms.Label();
            this.txtCloudRadius = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.txtCloud5Size = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.txtCloud6Size = new System.Windows.Forms.TextBox();
            this.label8 = new System.Windows.Forms.Label();
            this.txtCloud7Size = new System.Windows.Forms.TextBox();
            this.label9 = new System.Windows.Forms.Label();
            this.txtCloud8Size = new System.Windows.Forms.TextBox();
            this.label10 = new System.Windows.Forms.Label();
            this.label11 = new System.Windows.Forms.Label();
            this.label12 = new System.Windows.Forms.Label();
            this.label13 = new System.Windows.Forms.Label();
            this.label14 = new System.Windows.Forms.Label();
            this.label15 = new System.Windows.Forms.Label();
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
            this.butStart.Location = new System.Drawing.Point(22, 282);
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
            this.txtElapsedTime.Location = new System.Drawing.Point(22, 349);
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
            this.txtRc.Location = new System.Drawing.Point(22, 377);
            this.txtRc.Name = "txtRc";
            this.txtRc.ReadOnly = true;
            this.txtRc.Size = new System.Drawing.Size(263, 22);
            this.txtRc.TabIndex = 3;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(19, 132);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(91, 17);
            this.label5.TabIndex = 0;
            this.label5.Text = "Cloud radius:";
            // 
            // txtCloudRadius
            // 
            this.txtCloudRadius.Location = new System.Drawing.Point(123, 129);
            this.txtCloudRadius.Name = "txtCloudRadius";
            this.txtCloudRadius.Size = new System.Drawing.Size(68, 22);
            this.txtCloudRadius.TabIndex = 1;
            this.txtCloudRadius.Text = "1300";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(19, 160);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(95, 17);
            this.label6.TabIndex = 0;
            this.label6.Text = "Cloud5 count:";
            // 
            // txtCloud5Size
            // 
            this.txtCloud5Size.Location = new System.Drawing.Point(123, 157);
            this.txtCloud5Size.Name = "txtCloud5Size";
            this.txtCloud5Size.Size = new System.Drawing.Size(68, 22);
            this.txtCloud5Size.TabIndex = 1;
            this.txtCloud5Size.Text = "55";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(19, 188);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(95, 17);
            this.label7.TabIndex = 0;
            this.label7.Text = "Cloud6 count:";
            // 
            // txtCloud6Size
            // 
            this.txtCloud6Size.Location = new System.Drawing.Point(123, 185);
            this.txtCloud6Size.Name = "txtCloud6Size";
            this.txtCloud6Size.Size = new System.Drawing.Size(68, 22);
            this.txtCloud6Size.TabIndex = 1;
            this.txtCloud6Size.Text = "45";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(19, 216);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(95, 17);
            this.label8.TabIndex = 0;
            this.label8.Text = "Cloud7 count:";
            // 
            // txtCloud7Size
            // 
            this.txtCloud7Size.Location = new System.Drawing.Point(123, 213);
            this.txtCloud7Size.Name = "txtCloud7Size";
            this.txtCloud7Size.Size = new System.Drawing.Size(68, 22);
            this.txtCloud7Size.TabIndex = 1;
            this.txtCloud7Size.Text = "35";
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(19, 244);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(95, 17);
            this.label9.TabIndex = 0;
            this.label9.Text = "Cloud8 count:";
            // 
            // txtCloud8Size
            // 
            this.txtCloud8Size.Location = new System.Drawing.Point(123, 241);
            this.txtCloud8Size.Name = "txtCloud8Size";
            this.txtCloud8Size.Size = new System.Drawing.Size(68, 22);
            this.txtCloud8Size.TabIndex = 1;
            this.txtCloud8Size.Text = "30";
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(197, 160);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(34, 17);
            this.label10.TabIndex = 0;
            this.label10.Text = "(65)";
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(19, 160);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(95, 17);
            this.label11.TabIndex = 0;
            this.label11.Text = "Cloud5 count:";
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(197, 188);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(34, 17);
            this.label12.TabIndex = 0;
            this.label12.Text = "(56)";
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Location = new System.Drawing.Point(197, 216);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(34, 17);
            this.label13.TabIndex = 0;
            this.label13.Text = "(51)";
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.Location = new System.Drawing.Point(197, 244);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(34, 17);
            this.label14.TabIndex = 0;
            this.label14.Text = "(48)";
            // 
            // label15
            // 
            this.label15.AutoSize = true;
            this.label15.Location = new System.Drawing.Point(197, 132);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(50, 17);
            this.label15.TabIndex = 0;
            this.label15.Text = "(1300)";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(345, 431);
            this.Controls.Add(this.txtRc);
            this.Controls.Add(this.txtElapsedTime);
            this.Controls.Add(this.butStart);
            this.Controls.Add(this.txtServerName);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.txtDbName);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.txtCalcTime);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.txtCloud8Size);
            this.Controls.Add(this.label9);
            this.Controls.Add(this.txtCloud7Size);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.txtCloud6Size);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.txtCloud5Size);
            this.Controls.Add(this.label14);
            this.Controls.Add(this.label13);
            this.Controls.Add(this.label12);
            this.Controls.Add(this.label15);
            this.Controls.Add(this.label10);
            this.Controls.Add(this.label11);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.txtCloudRadius);
            this.Controls.Add(this.label5);
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
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox txtCloudRadius;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox txtCloud5Size;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TextBox txtCloud6Size;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.TextBox txtCloud7Size;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.TextBox txtCloud8Size;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.Label label15;
    }
}

