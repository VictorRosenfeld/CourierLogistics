﻿namespace SQLCLR_LogParser
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
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(459, 267);
            this.Controls.Add(this.butFormatLog);
            this.Name = "Form1";
            this.Text = "Form1";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button butFormatLog;
        private System.Windows.Forms.OpenFileDialog ofdSelectLogFile;
    }
}

