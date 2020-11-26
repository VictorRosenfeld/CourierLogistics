namespace CourierLogistics
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.butStart = new System.Windows.Forms.Button();
            this.butRealModel = new System.Windows.Forms.Button();
            this.butFloatCouriers = new System.Windows.Forms.Button();
            this.butFloatOptimalModel = new System.Windows.Forms.Button();
            this.butCompareDistance = new System.Windows.Forms.Button();
            this.butStartFixedService = new System.Windows.Forms.Button();
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
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.MainForm_FormClosed);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button butStart;
        private System.Windows.Forms.Button butRealModel;
        private System.Windows.Forms.Button butFloatCouriers;
        private System.Windows.Forms.Button butFloatOptimalModel;
        private System.Windows.Forms.Button butCompareDistance;
        private System.Windows.Forms.Button butStartFixedService;
    }
}

