namespace CodeWalker
{
    partial class MenuForm
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
            this.LogoPic = new System.Windows.Forms.PictureBox();
            this.TitleLabel = new System.Windows.Forms.Label();
            this.SubtitleLabel = new System.Windows.Forms.Label();
            this.HeaderNavFlow = new System.Windows.Forms.FlowLayoutPanel();
            this.DarkModeButton = new System.Windows.Forms.Button();
            this.SettingsButton = new System.Windows.Forms.Button();
            this.AboutButton = new System.Windows.Forms.Button();
            this.MainScrollPanel = new System.Windows.Forms.Panel();
            this.SectionExplore = new System.Windows.Forms.GroupBox();
            this.RPFExplorerButton = new System.Windows.Forms.Button();
            this.RPFBrowserButton = new System.Windows.Forms.Button();
            this.ProjectButton = new System.Windows.Forms.Button();
            this.SectionWorld = new System.Windows.Forms.GroupBox();
            this.WorldButton = new System.Windows.Forms.Button();
            this.SectionExtract = new System.Windows.Forms.GroupBox();
            this.ExtractScriptsButton = new System.Windows.Forms.Button();
            this.ExtractTexturesButton = new System.Windows.Forms.Button();
            this.ExtractRawFilesButton = new System.Windows.Forms.Button();
            this.ExtractShadersButton = new System.Windows.Forms.Button();
            this.ExtractKeysButton = new System.Windows.Forms.Button();
            this.SectionTools = new System.Windows.Forms.GroupBox();
            this.BinarySearchButton = new System.Windows.Forms.Button();
            this.JenkGenButton = new System.Windows.Forms.Button();
            this.JenkIndButton = new System.Windows.Forms.Button();
            this.GCCollectButton = new System.Windows.Forms.Button();
            this.FooterPanel = new System.Windows.Forms.Panel();
            this.StatusLabel = new System.Windows.Forms.Label();
            this.VersionLabel = new System.Windows.Forms.Label();
            this.HeaderPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.LogoPic)).BeginInit();
            this.HeaderNavFlow.SuspendLayout();
            this.MainScrollPanel.SuspendLayout();
            this.SectionExplore.SuspendLayout();
            this.SectionWorld.SuspendLayout();
            this.SectionExtract.SuspendLayout();
            this.SectionTools.SuspendLayout();
            this.FooterPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // HeaderPanel
            // 
            this.HeaderPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.HeaderPanel.Height = 76;
            this.HeaderPanel.Controls.Add(this.HeaderNavFlow);
            this.HeaderPanel.Controls.Add(this.SubtitleLabel);
            this.HeaderPanel.Controls.Add(this.TitleLabel);
            this.HeaderPanel.Controls.Add(this.LogoPic);
            this.HeaderPanel.Tag = "Header";
            // 
            // LogoPic
            // 
            this.LogoPic.Location = new System.Drawing.Point(18, 12);
            this.LogoPic.Size = new System.Drawing.Size(52, 52);
            this.LogoPic.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            // 
            // TitleLabel
            // 
            this.TitleLabel.Location = new System.Drawing.Point(80, 14);
            this.TitleLabel.Size = new System.Drawing.Size(300, 26);
            this.TitleLabel.Font = new System.Drawing.Font("Segoe UI", 15F, System.Drawing.FontStyle.Bold);
            this.TitleLabel.Text = "SR FILE";
            // 
            // SubtitleLabel
            // 
            this.SubtitleLabel.Location = new System.Drawing.Point(82, 42);
            this.SubtitleLabel.Size = new System.Drawing.Size(350, 20);
            this.SubtitleLabel.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.SubtitleLabel.Text = "GRAND THEFT AUTO V ARCHIVE && ASSET SUITE";
            this.SubtitleLabel.Tag = "SubText";
            // 
            // HeaderNavFlow
            // 
            this.HeaderNavFlow.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.HeaderNavFlow.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            this.HeaderNavFlow.Location = new System.Drawing.Point(440, 18);
            this.HeaderNavFlow.Size = new System.Drawing.Size(420, 42);
            this.HeaderNavFlow.Controls.Add(this.AboutButton);
            this.HeaderNavFlow.Controls.Add(this.SettingsButton);
            this.HeaderNavFlow.Controls.Add(this.DarkModeButton);
            // 
            // AboutButton
            // 
            this.AboutButton.Size = new System.Drawing.Size(100, 34);
            this.AboutButton.Text = "ℹ À propos";
            this.AboutButton.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.AboutButton.Click += new System.EventHandler(this.AboutButton_Click);
            // 
            // SettingsButton
            // 
            this.SettingsButton.Size = new System.Drawing.Size(105, 34);
            this.SettingsButton.Text = "⚙ Réglages";
            this.SettingsButton.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.SettingsButton.Click += new System.EventHandler(this.SettingsButton_Click);
            // 
            // DarkModeButton
            // 
            this.DarkModeButton.Size = new System.Drawing.Size(110, 34);
            this.DarkModeButton.Text = "🌙 Sombre";
            this.DarkModeButton.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.DarkModeButton.Tag = "Accent";
            this.DarkModeButton.Click += new System.EventHandler(this.DarkModeButton_Click);
            // 
            // MainScrollPanel
            // 
            this.MainScrollPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.MainScrollPanel.AutoScroll = true;
            this.MainScrollPanel.Padding = new System.Windows.Forms.Padding(18);
            this.MainScrollPanel.Controls.Add(this.SectionTools);
            this.MainScrollPanel.Controls.Add(this.SectionExtract);
            this.MainScrollPanel.Controls.Add(this.SectionWorld);
            this.MainScrollPanel.Controls.Add(this.SectionExplore);
            // 
            // SectionExplore
            // 
            this.SectionExplore.Location = new System.Drawing.Point(18, 14);
            this.SectionExplore.Size = new System.Drawing.Size(400, 190);
            this.SectionExplore.Text = "📁  EXPLORATION && PROJETS";
            this.SectionExplore.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            this.SectionExplore.Controls.Add(this.RPFExplorerButton);
            this.SectionExplore.Controls.Add(this.RPFBrowserButton);
            this.SectionExplore.Controls.Add(this.ProjectButton);
            // 
            // RPFExplorerButton
            // 
            this.RPFExplorerButton.Location = new System.Drawing.Point(16, 28);
            this.RPFExplorerButton.Size = new System.Drawing.Size(368, 52);
            this.RPFExplorerButton.Text = "RPF Explorer (Principal)\r\nParcourir, éditer et extraire les archives";
            this.RPFExplorerButton.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            this.RPFExplorerButton.Tag = "Accent";
            this.RPFExplorerButton.Click += new System.EventHandler(this.RPFExplorerButton_Click);
            // 
            // RPFBrowserButton
            // 
            this.RPFBrowserButton.Location = new System.Drawing.Point(16, 88);
            this.RPFBrowserButton.Size = new System.Drawing.Size(368, 38);
            this.RPFBrowserButton.Text = "RPF Browser (Navigation lecture seule)";
            this.RPFBrowserButton.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.RPFBrowserButton.Click += new System.EventHandler(this.RPFBrowserButton_Click);
            // 
            // ProjectButton
            // 
            this.ProjectButton.Location = new System.Drawing.Point(16, 134);
            this.ProjectButton.Size = new System.Drawing.Size(368, 38);
            this.ProjectButton.Text = "Éditeur de Projet (Modding)";
            this.ProjectButton.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.ProjectButton.Click += new System.EventHandler(this.ProjectButton_Click);
            // 
            // SectionWorld
            // 
            this.SectionWorld.Location = new System.Drawing.Point(438, 14);
            this.SectionWorld.Size = new System.Drawing.Size(400, 190);
            this.SectionWorld.Text = "🌐  RENDU 3D DU MONDE";
            this.SectionWorld.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            this.SectionWorld.Controls.Add(this.WorldButton);
            // 
            // WorldButton
            // 
            this.WorldButton.Location = new System.Drawing.Point(16, 28);
            this.WorldButton.Size = new System.Drawing.Size(368, 144);
            this.WorldButton.Text = "Vue 3D du Monde (DirectX 11)\r\n\r\nVisualisez la carte complète de GTA V\r\nExplorer Los Santos && Blaine County";
            this.WorldButton.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.WorldButton.Tag = "Accent";
            this.WorldButton.Click += new System.EventHandler(this.WorldButton_Click);
            // 
            // SectionExtract
            // 
            this.SectionExtract.Location = new System.Drawing.Point(18, 218);
            this.SectionExtract.Size = new System.Drawing.Size(400, 200);
            this.SectionExtract.Text = "📦  EXTRACTION D\'ASSETS";
            this.SectionExtract.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            this.SectionExtract.Controls.Add(this.ExtractScriptsButton);
            this.SectionExtract.Controls.Add(this.ExtractTexturesButton);
            this.SectionExtract.Controls.Add(this.ExtractRawFilesButton);
            this.SectionExtract.Controls.Add(this.ExtractShadersButton);
            this.SectionExtract.Controls.Add(this.ExtractKeysButton);
            // 
            // ExtractScriptsButton
            // 
            this.ExtractScriptsButton.Location = new System.Drawing.Point(16, 28);
            this.ExtractScriptsButton.Size = new System.Drawing.Size(176, 36);
            this.ExtractScriptsButton.Text = "Extraire Scripts...";
            this.ExtractScriptsButton.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.ExtractScriptsButton.Click += new System.EventHandler(this.ExtractScriptsButton_Click);
            // 
            // ExtractTexturesButton
            // 
            this.ExtractTexturesButton.Location = new System.Drawing.Point(208, 28);
            this.ExtractTexturesButton.Size = new System.Drawing.Size(176, 36);
            this.ExtractTexturesButton.Text = "Extraire Textures...";
            this.ExtractTexturesButton.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.ExtractTexturesButton.Click += new System.EventHandler(this.ExtractTexturesButton_Click);
            // 
            // ExtractRawFilesButton
            // 
            this.ExtractRawFilesButton.Location = new System.Drawing.Point(16, 72);
            this.ExtractRawFilesButton.Size = new System.Drawing.Size(176, 36);
            this.ExtractRawFilesButton.Text = "Extraire Raw...";
            this.ExtractRawFilesButton.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.ExtractRawFilesButton.Click += new System.EventHandler(this.ExtractRawFilesButton_Click);
            // 
            // ExtractShadersButton
            // 
            this.ExtractShadersButton.Location = new System.Drawing.Point(208, 72);
            this.ExtractShadersButton.Size = new System.Drawing.Size(176, 36);
            this.ExtractShadersButton.Text = "Extraire Shaders...";
            this.ExtractShadersButton.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.ExtractShadersButton.Click += new System.EventHandler(this.ExtractShadersButton_Click);
            // 
            // ExtractKeysButton
            // 
            this.ExtractKeysButton.Location = new System.Drawing.Point(16, 116);
            this.ExtractKeysButton.Size = new System.Drawing.Size(368, 36);
            this.ExtractKeysButton.Text = "Extraire Clés AES...";
            this.ExtractKeysButton.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.ExtractKeysButton.Click += new System.EventHandler(this.ExtractKeysButton_Click);
            // 
            // SectionTools
            // 
            this.SectionTools.Location = new System.Drawing.Point(438, 218);
            this.SectionTools.Size = new System.Drawing.Size(400, 200);
            this.SectionTools.Text = "🛠️  OUTILS && UTILITAIRES";
            this.SectionTools.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            this.SectionTools.Controls.Add(this.BinarySearchButton);
            this.SectionTools.Controls.Add(this.JenkGenButton);
            this.SectionTools.Controls.Add(this.JenkIndButton);
            this.SectionTools.Controls.Add(this.GCCollectButton);
            // 
            // BinarySearchButton
            // 
            this.BinarySearchButton.Location = new System.Drawing.Point(16, 28);
            this.BinarySearchButton.Size = new System.Drawing.Size(176, 36);
            this.BinarySearchButton.Text = "Recherche Binaire...";
            this.BinarySearchButton.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.BinarySearchButton.Click += new System.EventHandler(this.BinarySearchButton_Click);
            // 
            // JenkGenButton
            // 
            this.JenkGenButton.Location = new System.Drawing.Point(208, 28);
            this.JenkGenButton.Size = new System.Drawing.Size(176, 36);
            this.JenkGenButton.Text = "Jenkins Hasher...";
            this.JenkGenButton.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.JenkGenButton.Click += new System.EventHandler(this.JenkGenButton_Click);
            // 
            // JenkIndButton
            // 
            this.JenkIndButton.Location = new System.Drawing.Point(16, 72);
            this.JenkIndButton.Size = new System.Drawing.Size(368, 36);
            this.JenkIndButton.Text = "Jenkins Indexer...";
            this.JenkIndButton.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.JenkIndButton.Click += new System.EventHandler(this.JenkIndButton_Click);
            // 
            // GCCollectButton
            // 
            this.GCCollectButton.Location = new System.Drawing.Point(16, 116);
            this.GCCollectButton.Size = new System.Drawing.Size(368, 36);
            this.GCCollectButton.Text = "Nettoyer Mémoire (GC Collect)";
            this.GCCollectButton.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.GCCollectButton.Click += new System.EventHandler(this.GCCollectButton_Click);
            // 
            // FooterPanel
            // 
            this.FooterPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.FooterPanel.Height = 36;
            this.FooterPanel.Controls.Add(this.VersionLabel);
            this.FooterPanel.Controls.Add(this.StatusLabel);
            this.FooterPanel.Tag = "Header";
            // 
            // StatusLabel
            // 
            this.StatusLabel.Location = new System.Drawing.Point(18, 9);
            this.StatusLabel.Size = new System.Drawing.Size(500, 18);
            this.StatusLabel.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.StatusLabel.Text = "● SR File Prêt - Grand Theft Auto V détecté";
            this.StatusLabel.Tag = "SubText";
            // 
            // VersionLabel
            // 
            this.VersionLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.VersionLabel.Location = new System.Drawing.Point(680, 9);
            this.VersionLabel.Size = new System.Drawing.Size(180, 18);
            this.VersionLabel.Font = new System.Drawing.Font("Segoe UI", 8.5F, System.Drawing.FontStyle.Bold);
            this.VersionLabel.Text = "SR File v2.0 (Modern)";
            this.VersionLabel.TextAlign = System.Drawing.ContentAlignment.TopRight;
            this.VersionLabel.Tag = "Accent";
            // 
            // MenuForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(870, 560);
            this.Controls.Add(this.MainScrollPanel);
            this.Controls.Add(this.HeaderPanel);
            this.Controls.Add(this.FooterPanel);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.MinimumSize = new System.Drawing.Size(880, 570);
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "SR File - Grand Theft Auto V Suite";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.MainForm_FormClosed);
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.HeaderPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.LogoPic)).EndInit();
            this.HeaderNavFlow.ResumeLayout(false);
            this.MainScrollPanel.ResumeLayout(false);
            this.SectionExplore.ResumeLayout(false);
            this.SectionWorld.ResumeLayout(false);
            this.SectionExtract.ResumeLayout(false);
            this.SectionTools.ResumeLayout(false);
            this.FooterPanel.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        private System.Windows.Forms.Panel HeaderPanel;
        private System.Windows.Forms.PictureBox LogoPic;
        private System.Windows.Forms.Label TitleLabel;
        private System.Windows.Forms.Label SubtitleLabel;
        private System.Windows.Forms.FlowLayoutPanel HeaderNavFlow;
        private System.Windows.Forms.Button AboutButton;
        private System.Windows.Forms.Button SettingsButton;
        private System.Windows.Forms.Button DarkModeButton;
        private System.Windows.Forms.Panel MainScrollPanel;
        private System.Windows.Forms.GroupBox SectionExplore;
        private System.Windows.Forms.Button RPFExplorerButton;
        private System.Windows.Forms.Button RPFBrowserButton;
        private System.Windows.Forms.Button ProjectButton;
        private System.Windows.Forms.GroupBox SectionWorld;
        private System.Windows.Forms.Button WorldButton;
        private System.Windows.Forms.GroupBox SectionExtract;
        private System.Windows.Forms.Button ExtractScriptsButton;
        private System.Windows.Forms.Button ExtractTexturesButton;
        private System.Windows.Forms.Button ExtractRawFilesButton;
        private System.Windows.Forms.Button ExtractShadersButton;
        private System.Windows.Forms.Button ExtractKeysButton;
        private System.Windows.Forms.GroupBox SectionTools;
        private System.Windows.Forms.Button BinarySearchButton;
        private System.Windows.Forms.Button JenkGenButton;
        private System.Windows.Forms.Button JenkIndButton;
        private System.Windows.Forms.Button GCCollectButton;
        private System.Windows.Forms.Panel FooterPanel;
        private System.Windows.Forms.Label StatusLabel;
        private System.Windows.Forms.Label VersionLabel;
    }
}