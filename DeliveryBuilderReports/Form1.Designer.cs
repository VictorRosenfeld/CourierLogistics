namespace DeliveryBuilderReports
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
            this.label1 = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.txtLogFile = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.txtOrdersFile = new System.Windows.Forms.TextBox();
            this.butSelectLogFile = new System.Windows.Forms.Button();
            this.butSelectOrdersFile = new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // butCreate
            // 
            this.butCreate.Location = new System.Drawing.Point(530, 124);
            this.butCreate.Name = "butCreate";
            this.butCreate.Size = new System.Drawing.Size(142, 46);
            this.butCreate.TabIndex = 7;
            this.butCreate.Text = "Create";
            this.butCreate.UseVisualStyleBackColor = true;
            this.butCreate.Click += new System.EventHandler(this.butCreate_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 27);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(36, 17);
            this.label1.TabIndex = 1;
            this.label1.Text = "Log:";
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.butSelectOrdersFile);
            this.groupBox1.Controls.Add(this.butSelectLogFile);
            this.groupBox1.Controls.Add(this.txtOrdersFile);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.txtLogFile);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Location = new System.Drawing.Point(18, 11);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(654, 98);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            // 
            // txtLogFile
            // 
            this.txtLogFile.Location = new System.Drawing.Point(71, 23);
            this.txtLogFile.Name = "txtLogFile";
            this.txtLogFile.Size = new System.Drawing.Size(534, 22);
            this.txtLogFile.TabIndex = 2;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(6, 55);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(56, 17);
            this.label2.TabIndex = 4;
            this.label2.Text = "Orders:";
            // 
            // txtOrdersFile
            // 
            this.txtOrdersFile.Location = new System.Drawing.Point(71, 51);
            this.txtOrdersFile.Name = "txtOrdersFile";
            this.txtOrdersFile.Size = new System.Drawing.Size(534, 22);
            this.txtOrdersFile.TabIndex = 5;
            // 
            // butSelectLogFile
            // 
            this.butSelectLogFile.Location = new System.Drawing.Point(611, 22);
            this.butSelectLogFile.Name = "butSelectLogFile";
            this.butSelectLogFile.Size = new System.Drawing.Size(30, 24);
            this.butSelectLogFile.TabIndex = 3;
            this.butSelectLogFile.Text = "...";
            this.butSelectLogFile.UseVisualStyleBackColor = true;
            this.butSelectLogFile.Click += new System.EventHandler(this.butSelectLogFile_Click);
            // 
            // butSelectOrdersFile
            // 
            this.butSelectOrdersFile.Location = new System.Drawing.Point(611, 50);
            this.butSelectOrdersFile.Name = "butSelectOrdersFile";
            this.butSelectOrdersFile.Size = new System.Drawing.Size(30, 24);
            this.butSelectOrdersFile.TabIndex = 6;
            this.butSelectOrdersFile.Text = "...";
            this.butSelectOrdersFile.UseVisualStyleBackColor = true;
            this.butSelectOrdersFile.Click += new System.EventHandler(this.butSelectOrdersFile_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(687, 182);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.butCreate);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "Form1";
            this.Text = "Delivery Builder Reports";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button butCreate;
        private System.Windows.Forms.OpenFileDialog ofdSelectLogFile;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button butSelectOrdersFile;
        private System.Windows.Forms.Button butSelectLogFile;
        private System.Windows.Forms.TextBox txtOrdersFile;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox txtLogFile;
    }
}

