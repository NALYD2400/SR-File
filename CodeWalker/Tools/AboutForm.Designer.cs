namespace CodeWalker.Tools
{
    partial class AboutForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.LogoPic = new System.Windows.Forms.PictureBox();
            this.TitleLabel = new System.Windows.Forms.Label();
            this.SubtitleLabel = new System.Windows.Forms.Label();
            this.DescTextBox = new System.Windows.Forms.TextBox();
            this.OkButton = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.LogoPic)).BeginInit();
            this.SuspendLayout();
            // 
            // LogoPic
            // 
            this.LogoPic.Location = new System.Drawing.Point(24, 24);
            this.LogoPic.Size = new System.Drawing.Size(64, 64);
            this.LogoPic.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            // 
            // TitleLabel
            // 
            this.TitleLabel.Location = new System.Drawing.Point(104, 24);
            this.TitleLabel.Size = new System.Drawing.Size(350, 28);
            this.TitleLabel.Font = new System.Drawing.Font("Segoe UI", 16F, System.Drawing.FontStyle.Bold);
            this.TitleLabel.Text = "SR FILE";
            this.TitleLabel.Tag = "Accent";
            // 
            // SubtitleLabel
            // 
            this.SubtitleLabel.Location = new System.Drawing.Point(106, 56);
            this.SubtitleLabel.Size = new System.Drawing.Size(350, 20);
            this.SubtitleLabel.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.SubtitleLabel.Text = "Version 2.0 (Modern Edition)";
            this.SubtitleLabel.Tag = "SubText";
            // 
            // DescTextBox
            // 
            this.DescTextBox.Location = new System.Drawing.Point(24, 106);
            this.DescTextBox.Size = new System.Drawing.Size(432, 130);
            this.DescTextBox.Multiline = true;
            this.DescTextBox.ReadOnly = true;
            this.DescTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.DescTextBox.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.DescTextBox.Text = "SR File est une suite logicielle avancée d\'exploration, d\'édition et de rendu 3D pour Grand Theft Auto V.\r\n\r\n• Explorateur natif d\'archives RPF\r\n• Rendu 3D temps réel avec moteur DirectX 11\r\n• Thème sombre synchronisé & personnalisable\r\n• Outils complets d\'extraction et de modding\r\n\r\nRebrandé et modernisé avec succès au nom de SR File.";
            // 
            // OkButton
            // 
            this.OkButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.OkButton.Location = new System.Drawing.Point(366, 252);
            this.OkButton.Size = new System.Drawing.Size(90, 32);
            this.OkButton.Text = "Fermer";
            this.OkButton.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.OkButton.Tag = "Accent";
            this.OkButton.Click += new System.EventHandler(this.OkButton_Click);
            // 
            // AboutForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(480, 300);
            this.Controls.Add(this.OkButton);
            this.Controls.Add(this.DescTextBox);
            this.Controls.Add(this.SubtitleLabel);
            this.Controls.Add(this.TitleLabel);
            this.Controls.Add(this.LogoPic);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "À propos de SR File";
            ((System.ComponentModel.ISupportInitialize)(this.LogoPic)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private System.Windows.Forms.PictureBox LogoPic;
        private System.Windows.Forms.Label TitleLabel;
        private System.Windows.Forms.Label SubtitleLabel;
        private System.Windows.Forms.TextBox DescTextBox;
        private System.Windows.Forms.Button OkButton;
    }
}