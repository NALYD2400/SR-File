using CodeWalker.Properties;
using CodeWalker.Tools;
using CodeWalker.WinForms;
using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace CodeWalker
{
    public partial class MenuForm : Form
    {
        private volatile bool worldFormOpen = false;
        private WorldForm worldForm = null;
        private Microsoft.Web.WebView2.WinForms.WebView2 webView = null;

        public MenuForm()
        {
            InitializeComponent();
            SetupLogoAndIcon();

            SRSettingsForm.GetGtaFolderFunc = () => GTAFolder.CurrentGTAFolder;
            SRSettingsForm.SetGtaFolderFunc = (p) => GTAFolder.SetGTAFolder(p, false);
            SRSettingsForm.SetKeyboardLayoutFunc = (isAzerty) =>
            {
                var kb = new KeyBindings(Settings.Default.KeyBindings);
                if (isAzerty) kb.ApplyAzerty();
                else kb.ApplyQwerty();
                Settings.Default.KeyBindings = kb.GetSetting();
                Settings.Default.Save();
                SendStateToWeb();
            };
            SRSettingsForm.GetIsAzertyFunc = () =>
            {
                var kb = new KeyBindings(Settings.Default.KeyBindings);
                return kb.IsAzertyMode;
            };

            SRThemeManager.RegisterForm(this, null, () => {
                ApplyCurrentTheme();
                SendStateToWeb();
            });
        }

        private void SetupLogoAndIcon()
        {
            try
            {
                var icon = SRLogo.GetAppIcon();
                if (icon != null) this.Icon = icon;
                LogoPic.Image = SRLogo.GetLogoImage(52);
            }
            catch { }
        }

        private void OnThemeChanged()
        {
            if (this.IsDisposed) return;
            ApplyCurrentTheme();
        }

        private void ApplyCurrentTheme()
        {
            DarkModeButton.Text = SRThemeManager.IsDarkMode ? "🌙 Sombre" : "☀️ Clair";
            HeaderPanel.BackColor = SRThemeManager.Surface;
            FooterPanel.BackColor = SRThemeManager.Surface;
            MainScrollPanel.BackColor = SRThemeManager.Background;

            if (TitleLabel != null)
            {
                TitleLabel.ForeColor = SRThemeManager.Text;
                try { TitleLabel.Font = new Font("Bahnschrift", 16F, FontStyle.Bold); } catch { }
            }
            if (SubtitleLabel != null)
            {
                SubtitleLabel.ForeColor = SRThemeManager.SubText;
            }
            if (VersionLabel != null)
            {
                VersionLabel.Text = "SR RANK : 70 930 SR  •  V2.0 MODERN";
                VersionLabel.ForeColor = SRThemeManager.AccentColor;
            }
            if (StatusLabel != null)
            {
                string folder = GTAFolder.CurrentGTAFolder;
                bool valid = GTAFolder.IsCurrentGTAFolderValid();
                StatusLabel.Text = valid ? $"✔ GTA V Détecté : {folder}" : "⚠ GTA V non configuré - Ouvrez '⚙ Réglages' pour spécifier le dossier";
                StatusLabel.ForeColor = valid ? Color.FromArgb(16, 185, 129) : SRThemeManager.AccentColor;
            }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            ApplyCurrentTheme();
            InitializeWebView();
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            Settings.Default.Save();
        }

        private void DarkModeButton_Click(object sender, EventArgs e)
        {
            SRThemeManager.ToggleDarkMode();
        }

        private void SettingsButton_Click(object sender, EventArgs e)
        {
            using (var dlg = new SRSettingsForm())
            {
                dlg.ShowDialog(this);
            }
        }

        private void RPFExplorerButton_Click(object sender, EventArgs e)
        {
            ExploreForm f = new ExploreForm();
            f.Show(this);
        }

        private void RPFBrowserButton_Click(object sender, EventArgs e)
        {
            BrowseForm f = new BrowseForm();
            f.Show(this);
        }

        private void ExtractScriptsButton_Click(object sender, EventArgs e)
        {
            ExtractScriptsForm f = new ExtractScriptsForm();
            f.Show(this);
        }

        private void ExtractTexturesButton_Click(object sender, EventArgs e)
        {
            ExtractTexForm f = new ExtractTexForm();
            f.Show(this);
        }

        private void ExtractRawFilesButton_Click(object sender, EventArgs e)
        {
            ExtractRawForm f = new ExtractRawForm();
            f.Show(this);
        }

        private void ExtractShadersButton_Click(object sender, EventArgs e)
        {
            ExtractShadersForm f = new ExtractShadersForm();
            f.Show(this);
        }

        private void BinarySearchButton_Click(object sender, EventArgs e)
        {
            BinarySearchForm f = new BinarySearchForm();
            f.Show(this);
        }

        private void WorldButton_Click(object sender, EventArgs e)
        {
            if (worldFormOpen)
            {
                if (worldForm != null)
                {
                    worldForm.Invoke(new Action(() => { worldForm.Focus(); }));
                }
                return;
            }

            Thread thread = new Thread(new ThreadStart(() => {
                try
                {
                    worldFormOpen = true;
                    using (WorldForm f = new WorldForm())
                    {
                        worldForm = f;
                        f.ShowDialog();
                        worldForm = null;
                    }
                    worldFormOpen = false;
                }
                catch (Exception ex)
                {
                    worldFormOpen = false;
                    MessageBox.Show("Erreur lors de l'ouverture du Rendu 3D :\n" + ex.Message, "SR File", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }));
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
        }

        private void GCCollectButton_Click(object sender, EventArgs e)
        {
            GC.Collect();
        }

        private void AboutButton_Click(object sender, EventArgs e)
        {
            AboutForm f = new AboutForm();
            f.Show(this);
        }

        private void JenkGenButton_Click(object sender, EventArgs e)
        {
            JenkGenForm f = new JenkGenForm();
            f.Show(this);
        }

        private void JenkIndButton_Click(object sender, EventArgs e)
        {
            JenkIndForm f = new JenkIndForm();
            f.Show(this);
        }

        private void ExtractKeysButton_Click(object sender, EventArgs e)
        {
            ExtractKeysForm f = new ExtractKeysForm();
            f.Show(this);
        }

        private void ProjectButton_Click(object sender, EventArgs e)
        {
            Project.ProjectForm f = new Project.ProjectForm(null);
            f.Show(this);
        }

        private async void InitializeWebView()
        {
            try
            {
                webView = new Microsoft.Web.WebView2.WinForms.WebView2();
                webView.Dock = DockStyle.Fill;
                this.Controls.Add(webView);
                webView.BringToFront();

                string userDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SRFile", "WebViewProfile");
                var env = await Microsoft.Web.WebView2.Core.CoreWebView2Environment.CreateAsync(null, userDataFolder);
                await webView.EnsureCoreWebView2Async(env);

                webView.CoreWebView2.Settings.IsStatusBarEnabled = false;
                webView.CoreWebView2.Settings.AreDevToolsEnabled = false;
                webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;

                webView.CoreWebView2.WebMessageReceived += WebView_WebMessageReceived;

                string htmlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "WebUI", "index.html");
                if (File.Exists(htmlPath))
                {
                    webView.CoreWebView2.Navigate(htmlPath);
                }
            }
            catch
            {
                if (webView != null)
                {
                    try { this.Controls.Remove(webView); } catch { }
                    try { webView.Dispose(); } catch { }
                    webView = null;
                }
            }
        }

        private void WebView_WebMessageReceived(object sender, Microsoft.Web.WebView2.Core.CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                string json = e.WebMessageAsJson;
                if (json.Contains("\"open_world\"")) WorldButton_Click(this, EventArgs.Empty);
                else if (json.Contains("\"open_explorer\"")) RPFExplorerButton_Click(this, EventArgs.Empty);
                else if (json.Contains("\"open_browser\"")) RPFBrowserButton_Click(this, EventArgs.Empty);
                else if (json.Contains("\"open_project\"")) ProjectButton_Click(this, EventArgs.Empty);
                else if (json.Contains("\"open_scripts\"")) ExtractScriptsButton_Click(this, EventArgs.Empty);
                else if (json.Contains("\"open_textures\"")) ExtractTexturesButton_Click(this, EventArgs.Empty);
                else if (json.Contains("\"open_raw\"")) ExtractRawFilesButton_Click(this, EventArgs.Empty);
                else if (json.Contains("\"open_shaders\"")) ExtractShadersButton_Click(this, EventArgs.Empty);
                else if (json.Contains("\"open_keys\"")) ExtractKeysButton_Click(this, EventArgs.Empty);
                else if (json.Contains("\"open_binary\"")) BinarySearchButton_Click(this, EventArgs.Empty);
                else if (json.Contains("\"open_jenkgen\"")) JenkGenButton_Click(this, EventArgs.Empty);
                else if (json.Contains("\"open_jenkind\"")) JenkIndButton_Click(this, EventArgs.Empty);
                else if (json.Contains("\"gc_collect\"")) GCCollectButton_Click(this, EventArgs.Empty);
                else if (json.Contains("\"open_settings\"")) SettingsButton_Click(this, EventArgs.Empty);
                else if (json.Contains("\"open_about\"")) AboutButton_Click(this, EventArgs.Empty);
                else if (json.Contains("\"browse_gta_folder\"")) BrowseGtaFolder();
                else if (json.Contains("\"toggle_theme\"")) SRThemeManager.ToggleDarkMode();
                else if (json.Contains("\"toggle_azerty\"")) ToggleAzerty();
                else if (json.Contains("\"set_theme\"")) SetThemePreset(json);
                else if (json.Contains("\"ui_ready\"")) SendStateToWeb();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur action: " + ex.Message, "SR File", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void SendStateToWeb()
        {
            if (this.IsDisposed || webView == null || webView.CoreWebView2 == null) return;
            try
            {
                string gta = GTAFolder.CurrentGTAFolder?.Replace("\\", "\\\\") ?? "";
                bool valid = GTAFolder.IsCurrentGTAFolderValid();
                bool isDark = SRThemeManager.IsDarkMode;
                var kb = new KeyBindings(Settings.Default.KeyBindings);
                bool isAzerty = kb.IsAzertyMode;

                string msg = $"{{\"type\":\"state_update\",\"isDarkMode\":{isDark.ToString().ToLower()},\"isAzerty\":{isAzerty.ToString().ToLower()},\"gtaFolder\":\"{gta}\",\"isGtaValid\":{valid.ToString().ToLower()},\"rankText\":\"SR RANK : 70 930 SR\"}}";
                this.BeginInvoke(new Action(() => {
                    try { webView.CoreWebView2?.PostWebMessageAsJson(msg); } catch { }
                }));
            }
            catch { }
        }

        private void BrowseGtaFolder()
        {
            using (var fbd = new FolderBrowserDialog())
            {
                fbd.Description = "Sélectionnez le dossier de jeu Grand Theft Auto V";
                fbd.SelectedPath = GTAFolder.CurrentGTAFolder;
                if (fbd.ShowDialog(this) == DialogResult.OK)
                {
                    string path = fbd.SelectedPath;
                    bool gen9 = GTAFolder.IsGen9Folder(path);
                    if (GTAFolder.ValidateGTAFolder(path, gen9, out string reason))
                    {
                        GTAFolder.SetGTAFolder(path, gen9);
                        SendStateToWeb();
                        MessageBox.Show($"Dossier GTA V configuré :\n{path}", "SR File", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show("Dossier non valide : " + reason, "SR File", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
        }

        private void ToggleAzerty()
        {
            var kb = new KeyBindings(Settings.Default.KeyBindings);
            if (kb.IsAzertyMode) kb.ApplyQwerty();
            else kb.ApplyAzerty();
            Settings.Default.KeyBindings = kb.GetSetting();
            Settings.Default.Save();
            SendStateToWeb();
        }

        private void SetThemePreset(string json)
        {
            if (json.Contains("SROrange")) SRThemeManager.Preset = SRThemePreset.SROrange;
            else if (json.Contains("SRCrimson")) SRThemeManager.Preset = SRThemePreset.SRCrimson;
            else if (json.Contains("DarkSlate")) SRThemeManager.Preset = SRThemePreset.DarkSlate;
            SendStateToWeb();
        }
    }
}