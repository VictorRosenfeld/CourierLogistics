﻿namespace DeliveryBuilderReports
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
            this.butCreate = new System.Windows.Forms.Button();
            this.ofdSelectLogFile = new System.Windows.Forms.OpenFileDialog();
            this.SuspendLayout();
            // 
            // butCreate
            // 
            this.butCreate.Location = new System.Drawing.Point(12, 12);
            this.butCreate.Name = "butCreate";
            this.butCreate.Size = new System.Drawing.Size(324, 46);
            this.butCreate.TabIndex = 0;
            this.butCreate.Text = "Create...";
            this.butCreate.UseVisualStyleBackColor = true;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(348, 104);
            this.Controls.Add(this.butCreate);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "Form1";
            this.Text = "Delivery Builder Reports";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button butCreate;
        private System.Windows.Forms.OpenFileDialog ofdSelectLogFile;
    }
}

