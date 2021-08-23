namespace SQLCLR_TEST
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
            this.butGetGeoContext = new System.Windows.Forms.Button();
            this.butParse = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // butGetGeoContext
            // 
            this.butGetGeoContext.Location = new System.Drawing.Point(39, 34);
            this.butGetGeoContext.Name = "butGetGeoContext";
            this.butGetGeoContext.Size = new System.Drawing.Size(165, 53);
            this.butGetGeoContext.TabIndex = 0;
            this.butGetGeoContext.Text = "GetGeoContext";
            this.butGetGeoContext.UseVisualStyleBackColor = true;
            this.butGetGeoContext.Click += new System.EventHandler(this.butGetGeoContext_Click);
            // 
            // butParse
            // 
            this.butParse.Location = new System.Drawing.Point(39, 102);
            this.butParse.Name = "butParse";
            this.butParse.Size = new System.Drawing.Size(165, 53);
            this.butParse.TabIndex = 1;
            this.butParse.Text = "Parse";
            this.butParse.UseCompatibleTextRendering = true;
            this.butParse.UseVisualStyleBackColor = true;
            this.butParse.Click += new System.EventHandler(this.butParse_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.butParse);
            this.Controls.Add(this.butGetGeoContext);
            this.Name = "Form1";
            this.Text = "Form1";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button butGetGeoContext;
        private System.Windows.Forms.Button butParse;
    }
}

