namespace ClydesdaleMon
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
            this.button1 = new System.Windows.Forms.Button();
            this.labelSensorMonitor = new System.Windows.Forms.Label();
            this.labelSensorMaxMinDetected = new System.Windows.Forms.Label();
            this.labelSensorId = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(90, 42);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 0;
            this.button1.Text = "Start";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // labelSensorMonitor
            // 
            this.labelSensorMonitor.AutoSize = true;
            this.labelSensorMonitor.Location = new System.Drawing.Point(12, 129);
            this.labelSensorMonitor.Name = "labelSensorMonitor";
            this.labelSensorMonitor.Size = new System.Drawing.Size(97, 13);
            this.labelSensorMonitor.TabIndex = 1;
            this.labelSensorMonitor.Text = "labelSensorMonitor";
            // 
            // labelSensorMaxMinDetected
            // 
            this.labelSensorMaxMinDetected.AutoSize = true;
            this.labelSensorMaxMinDetected.Location = new System.Drawing.Point(12, 177);
            this.labelSensorMaxMinDetected.Name = "labelSensorMaxMinDetected";
            this.labelSensorMaxMinDetected.Size = new System.Drawing.Size(143, 13);
            this.labelSensorMaxMinDetected.TabIndex = 2;
            this.labelSensorMaxMinDetected.Text = "labelSensorMaxMinDetected";
            // 
            // labelSensorId
            // 
            this.labelSensorId.AutoSize = true;
            this.labelSensorId.Location = new System.Drawing.Point(12, 86);
            this.labelSensorId.Name = "labelSensorId";
            this.labelSensorId.Size = new System.Drawing.Size(71, 13);
            this.labelSensorId.TabIndex = 3;
            this.labelSensorId.Text = "labelSensorId";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(284, 262);
            this.Controls.Add(this.labelSensorId);
            this.Controls.Add(this.labelSensorMaxMinDetected);
            this.Controls.Add(this.labelSensorMonitor);
            this.Controls.Add(this.button1);
            this.Name = "Form1";
            this.Text = "Form1";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Label labelSensorMonitor;
        private System.Windows.Forms.Label labelSensorMaxMinDetected;
        private System.Windows.Forms.Label labelSensorId;
    }
}

