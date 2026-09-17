namespace CodeWalker.WinForms
{
    partial class SRSettingsForm
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
            this.HeaderPanel = new System.Windows.Forms.Panel();
            this.LogoPictureBox = new System.Windows.Forms.PictureBox();
            this.TitleLabel = new System.Windows.Forms.Label();
            this.SubtitleLabel = new System.Windows.Forms.Label();
            this.SidebarPanel = new System.Windows.Forms.Panel();
            this.TabGeneralBtn = new System.Windows.Forms.Button();
            this.TabThemeBtn = new System.Windows.Forms.Button();
            this.TabShortcutsBtn = new System.Windows.Forms.Button();
            this.TabAboutBtn = new System.Windows.Forms.Button();
            this.ContentPanel = new System.Windows.Forms.Panel();
            this.FooterPanel = new System.Windows.Forms.Panel();
            this.CloseBtn = new System.Windows.Forms.Button();
            this.HeaderPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.LogoPictureBox)).BeginInit();
            this.SidebarPanel.SuspendLayout();
            this.FooterPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // HeaderPanel
            // 
            this.HeaderPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.HeaderPanel.Height = 70;
            this.HeaderPanel.Controls.Add(this.SubtitleLabel);
            this.HeaderPanel.Controls.Add(this.TitleLabel);
            this.HeaderPanel.Controls.Add(this.LogoPictureBox);
            this.HeaderPanel.Tag = "Header";
            // 
            // LogoPictureBox
            // 
            this.LogoPictureBox.Location = new System.Drawing.Point(16, 12);
            this.LogoPictureBox.Size = new System.Drawing.Size(46, 46);
            this.LogoPictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            // 
            // TitleLabel
            // 
            this.TitleLabel.Location = new System.Drawing.Point(72, 14);
            this.TitleLabel.Size = new System.Drawing.Size(500, 24);
            this.TitleLabel.Font = new System.Drawing.Font("Segoe UI", 13F, System.Drawing.FontStyle.Bold);
            this.TitleLabel.Text = "SR FILE  |  RÉGLAGES";
            // 
            // SubtitleLabel
            // 
            this.SubtitleLabel.Location = new System.Drawing.Point(74, 38);
            this.SubtitleLabel.Size = new System.Drawing.Size(500, 20);
            this.SubtitleLabel.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.SubtitleLabel.Text = "Configuration globale, thèmes synchronisés et personnalisation";
            this.SubtitleLabel.Tag = "SubText";
            // 
            // SidebarPanel
            // 
            this.SidebarPanel.Dock = System.Windows.Forms.DockStyle.Left;
            this.SidebarPanel.Width = 170;
            this.SidebarPanel.Controls.Add(this.TabAboutBtn);
            this.SidebarPanel.Controls.Add(this.TabShortcutsBtn);
            this.SidebarPanel.Controls.Add(this.TabThemeBtn);
            this.SidebarPanel.Controls.Add(this.TabGeneralBtn);
            this.SidebarPanel.Tag = "Card";
            // 
            // TabGeneralBtn
            // 
            this.TabGeneralBtn.Location = new System.Drawing.Point(8, 12);
            this.TabGeneralBtn.Size = new System.Drawing.Size(154, 38);
            this.TabGeneralBtn.Text = "⚙  Général";
            this.TabGeneralBtn.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.TabGeneralBtn.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            this.TabGeneralBtn.Click += new System.EventHandler(this.TabGeneralBtn_Click);
            // 
            // TabThemeBtn
            // 
            this.TabThemeBtn.Location = new System.Drawing.Point(8, 56);
            this.TabThemeBtn.Size = new System.Drawing.Size(154, 38);
            this.TabThemeBtn.Text = "🎨  Affichage & Thème";
            this.TabThemeBtn.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.TabThemeBtn.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            this.TabThemeBtn.Click += new System.EventHandler(this.TabThemeBtn_Click);
            // 
            // TabShortcutsBtn
            // 
            this.TabShortcutsBtn.Location = new System.Drawing.Point(8, 100);
            this.TabShortcutsBtn.Size = new System.Drawing.Size(154, 38);
            this.TabShortcutsBtn.Text = "⌨  Raccourcis";
            this.TabShortcutsBtn.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.TabShortcutsBtn.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            this.TabShortcutsBtn.Click += new System.EventHandler(this.TabShortcutsBtn_Click);
            // 
            // TabAboutBtn
            // 
            this.TabAboutBtn.Location = new System.Drawing.Point(8, 144);
            this.TabAboutBtn.Size = new System.Drawing.Size(154, 38);
            this.TabAboutBtn.Text = "ℹ  À propos";
            this.TabAboutBtn.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.TabAboutBtn.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            this.TabAboutBtn.Click += new System.EventHandler(this.TabAboutBtn_Click);
            // 
            // ContentPanel
            // 
            this.ContentPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ContentPanel.Padding = new System.Windows.Forms.Padding(16);
            // 
            // FooterPanel
            // 
            this.FooterPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.FooterPanel.Height = 54;
            this.FooterPanel.Controls.Add(this.CloseBtn);
            this.FooterPanel.Tag = "Header";
            // 
            // CloseBtn
            // 
            this.CloseBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.CloseBtn.Location = new System.Drawing.Point(620, 10);
            this.CloseBtn.Size = new System.Drawing.Size(100, 34);
            this.CloseBtn.Text = "Fermer";
            this.CloseBtn.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.CloseBtn.Tag = "Accent";
            this.CloseBtn.Click += new System.EventHandler(this.CloseBtn_Click);
            // 
            // SRSettingsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(740, 480);
            this.Controls.Add(this.ContentPanel);
            this.Controls.Add(this.SidebarPanel);
            this.Controls.Add(this.HeaderPanel);
            this.Controls.Add(this.FooterPanel);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Réglages - SR File";
            this.HeaderPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.LogoPictureBox)).EndInit();
            this.SidebarPanel.ResumeLayout(false);
            this.FooterPanel.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        private System.Windows.Forms.Panel HeaderPanel;
        private System.Windows.Forms.PictureBox LogoPictureBox;
        private System.Windows.Forms.Label TitleLabel;
        private System.Windows.Forms.Label SubtitleLabel;
        private System.Windows.Forms.Panel SidebarPanel;
        private System.Windows.Forms.Button TabGeneralBtn;
        private System.Windows.Forms.Button TabThemeBtn;
        private System.Windows.Forms.Button TabShortcutsBtn;
        private System.Windows.Forms.Button TabAboutBtn;
        private System.Windows.Forms.Panel ContentPanel;
        private System.Windows.Forms.Panel FooterPanel;
        private System.Windows.Forms.Button CloseBtn;
    }
}