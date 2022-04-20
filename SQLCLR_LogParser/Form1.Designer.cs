namespace SQLCLR_LogParser
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
            this.butFormatLog = new System.Windows.Forms.Button();
            this.ofdSelectLogFile = new System.Windows.Forms.OpenFileDialog();
            this.butCreateExcelFile = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.txtServiceID = new System.Windows.Forms.TextBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // butFormatLog
            // 
            this.butFormatLog.Location = new System.Drawing.Point(12, 26);
            this.butFormatLog.Name = "butFormatLog";
            this.butFormatLog.Size = new System.Drawing.Size(149, 49);
            this.butFormatLog.TabIndex = 0;
            this.butFormatLog.Text = "Format Log";
            this.butFormatLog.UseVisualStyleBackColor = true;
            this.butFormatLog.Click += new System.EventHandler(this.butFormatLog_Click);
            // 
            // ofdSelectLogFile
            // 
            this.ofdSelectLogFile.FileName = "openFileDialog1";
            // 
            // butCreateExcelFile
            // 
            this.butCreateExcelFile.Location = new System.Drawing.Point(16, 35);
            this.butCreateExcelFile.Name = "butCreateExcelFile";
            this.butCreateExcelFile.Size = new System.Drawing.Size(149, 49);
            this.butCreateExcelFile.TabIndex = 0;
            this.butCreateExcelFile.Text = "Create Excel File";
            this.butCreateExcelFile.UseVisualStyleBackColor = true;
            this.butCreateExcelFile.Click += new System.EventHandler(this.butCreateExcelFile_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(199, 35);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(72, 17);
            this.label1.TabIndex = 1;
            this.label1.Text = "Service ID";
            // 
            // txtServiceID
            // 
            this.txtServiceID.Location = new System.Drawing.Point(202, 62);
            this.txtServiceID.Name = "txtServiceID";
            this.txtServiceID.Size = new System.Drawing.Size(69, 22);
            this.txtServiceID.TabIndex = 2;
            this.txtServiceID.Text = "2";
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.butCreateExcelFile);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.txtServiceID);
            this.groupBox1.Location = new System.Drawing.Point(12, 95);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(345, 118);
            this.groupBox1.TabIndex = 3;
            this.groupBox1.TabStop = false;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(377, 267);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.butFormatLog);
            this.Name = "Form1";
            this.Text = "Form1";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.Form1_FormClosed);
            this.Load += new System.EventHandler(this.Form1_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button butFormatLog;
        private System.Windows.Forms.OpenFileDialog ofdSelectLogFile;
        private System.Windows.Forms.Button butCreateExcelFile;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txtServiceID;
        private System.Windows.Forms.GroupBox groupBox1;
    }
}

