namespace Z80
{
    partial class FormMPF1
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormMPF1));
            this.pbSpeaker = new System.Windows.Forms.PictureBox();
            this.chkDisplayLatch = new System.Windows.Forms.CheckBox();
            this.chkInsertMonitor = new System.Windows.Forms.CheckBox();
            this.chkEnableSound = new System.Windows.Forms.CheckBox();
            ((System.ComponentModel.ISupportInitialize)(this.pbSpeaker)).BeginInit();
            this.SuspendLayout();
            // 
            // pbSpeaker
            // 
            this.pbSpeaker.BackColor = System.Drawing.Color.Transparent;
            this.pbSpeaker.Image = global::Z80.Properties.Resources.speaker;
            this.pbSpeaker.Location = new System.Drawing.Point(542, 5);
            this.pbSpeaker.Name = "pbSpeaker";
            this.pbSpeaker.Size = new System.Drawing.Size(150, 150);
            this.pbSpeaker.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pbSpeaker.TabIndex = 0;
            this.pbSpeaker.TabStop = false;
            this.pbSpeaker.Visible = false;
            // 
            // chkDisplayLatch
            // 
            this.chkDisplayLatch.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.chkDisplayLatch.AutoSize = true;
            this.chkDisplayLatch.BackColor = System.Drawing.Color.Transparent;
            this.chkDisplayLatch.Checked = true;
            this.chkDisplayLatch.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkDisplayLatch.ForeColor = System.Drawing.Color.Orange;
            this.chkDisplayLatch.Location = new System.Drawing.Point(24, 532);
            this.chkDisplayLatch.Name = "chkDisplayLatch";
            this.chkDisplayLatch.Size = new System.Drawing.Size(90, 17);
            this.chkDisplayLatch.TabIndex = 40;
            this.chkDisplayLatch.Text = "Display Latch";
            this.chkDisplayLatch.UseVisualStyleBackColor = false;
            // 
            // chkInsertMonitor
            // 
            this.chkInsertMonitor.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.chkInsertMonitor.AutoSize = true;
            this.chkInsertMonitor.BackColor = System.Drawing.Color.Transparent;
            this.chkInsertMonitor.ForeColor = System.Drawing.Color.Orange;
            this.chkInsertMonitor.Location = new System.Drawing.Point(321, 532);
            this.chkInsertMonitor.Name = "chkInsertMonitor";
            this.chkInsertMonitor.Size = new System.Drawing.Size(182, 17);
            this.chkInsertMonitor.TabIndex = 41;
            this.chkInsertMonitor.Text = "Insert Monitor Program on Debug";
            this.chkInsertMonitor.UseVisualStyleBackColor = false;
            // 
            // chkEnableSound
            // 
            this.chkEnableSound.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.chkEnableSound.AutoSize = true;
            this.chkEnableSound.BackColor = System.Drawing.Color.Transparent;
            this.chkEnableSound.Checked = true;
            this.chkEnableSound.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkEnableSound.ForeColor = System.Drawing.Color.Orange;
            this.chkEnableSound.Location = new System.Drawing.Point(169, 532);
            this.chkEnableSound.Name = "chkEnableSound";
            this.chkEnableSound.Size = new System.Drawing.Size(93, 17);
            this.chkEnableSound.TabIndex = 42;
            this.chkEnableSound.Text = "Enable Sound";
            this.chkEnableSound.UseVisualStyleBackColor = false;
            // 
            // FormMPF1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.DarkGreen;
            this.BackgroundImage = global::Z80.Properties.Resources.pcb;
            this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.ClientSize = new System.Drawing.Size(784, 561);
            this.ControlBox = false;
            this.Controls.Add(this.chkEnableSound);
            this.Controls.Add(this.chkInsertMonitor);
            this.Controls.Add(this.chkDisplayLatch);
            this.Controls.Add(this.pbSpeaker);
            this.DoubleBuffered = true;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximumSize = new System.Drawing.Size(800, 600);
            this.MinimumSize = new System.Drawing.Size(800, 600);
            this.Name = "FormMPF1";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "MPF-1";
            this.Load += new System.EventHandler(this.FormMPF1_Load);
            ((System.ComponentModel.ISupportInitialize)(this.pbSpeaker)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        public System.Windows.Forms.PictureBox pbSpeaker;
        public System.Windows.Forms.CheckBox chkDisplayLatch;
        public System.Windows.Forms.CheckBox chkInsertMonitor;
        public System.Windows.Forms.CheckBox chkEnableSound;
    }
}