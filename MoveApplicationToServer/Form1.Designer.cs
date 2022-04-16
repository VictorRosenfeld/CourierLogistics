namespace MoveApplicationToServer
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
            this.txtServerName = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.txtDbName = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.txtServerFolder = new System.Windows.Forms.TextBox();
            this.butCopy = new System.Windows.Forms.Button();
            this.ofdNomFile = new System.Windows.Forms.OpenFileDialog();
            this.lblFilename = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // txtServerName
            // 
            this.txtServerName.Location = new System.Drawing.Point(118, 18);
            this.txtServerName.Name = "txtServerName";
            this.txtServerName.Size = new System.Drawing.Size(162, 22);
            this.txtServerName.TabIndex = 4;
            this.txtServerName.Text = "srv-sql15";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(14, 21);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(95, 17);
            this.label3.TabIndex = 2;
            this.label3.Text = "Server Name:";
            // 
            // txtDbName
            // 
            this.txtDbName.Location = new System.Drawing.Point(118, 46);
            this.txtDbName.Name = "txtDbName";
            this.txtDbName.Size = new System.Drawing.Size(162, 22);
            this.txtDbName.TabIndex = 5;
            this.txtDbName.Text = "logisticservice59";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(14, 49);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(72, 17);
            this.label2.TabIndex = 3;
            this.label2.Text = "DB Name:";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(14, 77);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(98, 17);
            this.label1.TabIndex = 2;
            this.label1.Text = "Server Folder:";
            // 
            // txtServerFolder
            // 
            this.txtServerFolder.Location = new System.Drawing.Point(118, 74);
            this.txtServerFolder.Name = "txtServerFolder";
            this.txtServerFolder.Size = new System.Drawing.Size(286, 22);
            this.txtServerFolder.TabIndex = 4;
            this.txtServerFolder.Text = "C:\\LogisticsService\\";
            // 
            // butCopy
            // 
            this.butCopy.Location = new System.Drawing.Point(252, 115);
            this.butCopy.Name = "butCopy";
            this.butCopy.Size = new System.Drawing.Size(152, 33);
            this.butCopy.TabIndex = 6;
            this.butCopy.Text = "Скопировать...";
            this.butCopy.UseVisualStyleBackColor = true;
            this.butCopy.Click += new System.EventHandler(this.butCopy_Click);
            // 
            // lblFilename
            // 
            this.lblFilename.Location = new System.Drawing.Point(17, 123);
            this.lblFilename.Name = "lblFilename";
            this.lblFilename.Size = new System.Drawing.Size(229, 25);
            this.lblFilename.TabIndex = 2;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(419, 169);
            this.Controls.Add(this.butCopy);
            this.Controls.Add(this.txtServerFolder);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.txtServerName);
            this.Controls.Add(this.lblFilename);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.txtDbName);
            this.Controls.Add(this.label2);
            this.Name = "Form1";
            this.Text = "Копирование файлов на сервер";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.Form1_FormClosed);
            this.Load += new System.EventHandler(this.Form1_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox txtServerName;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox txtDbName;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txtServerFolder;
        private System.Windows.Forms.Button butCopy;
        private System.Windows.Forms.OpenFileDialog ofdNomFile;
        private System.Windows.Forms.Label lblFilename;
    }
}

