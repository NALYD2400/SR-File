using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace CodeWalker.WinForms
{
    public partial class SRSettingsForm : Form
    {
        public static Func<string> GetGtaFolderFunc;
        public static Action<string> SetGtaFolderFunc;
        public static Action<bool> SetKeyboardLayoutFunc;
        public static Func<bool> GetIsAzertyFunc;

        private Panel _generalPanel;
        private Panel _themePanel;
        private Panel _shortcutsPanel;
        private Panel _aboutPanel;

        private Button _toggleDarkBtn;
        private Button _presetOrangeBtn;
        private Button _presetCrimsonBtn;
        private Button _presetSlateBtn;
        private Button _presetAmoledBtn;
        private Button _presetLightBtn;
        private Label _accentPreviewLabel;

        private TextBox _gtaFolderTextBox;

        public SRSettingsForm()
        {
            InitializeComponent();
            SetupLogoAndIcon();
            BuildTabs();
            ShowTab(1); // Default to Theme tab

            SRThemeManager.ThemeChanged += OnThemeChanged;
            this.FormClosed += (s, e) => { SRThemeManager.ThemeChanged -= OnThemeChanged; };

            ApplyCurrentTheme();
        }

        private void SetupLogoAndIcon()
        {
            try
            {
                var icon = SRLogo.GetAppIcon();
                if (icon != null) this.Icon = icon;
                LogoPictureBox.Image = SRLogo.GetLogoImage(46);
            }
            catch { }
        }

        private void BuildTabs()
        {
            // 1. General Panel
            _generalPanel = new Panel { Dock = DockStyle.Fill, Visible = false };
            var gtaLabel = new Label
            {
                Text = "Dossier d\'installation de GTA V :",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Location = new Point(10, 15),
                AutoSize = true
            };

            string currentGta = GetGtaFolderFunc != null ? GetGtaFolderFunc() : null;
            if (string.IsNullOrEmpty(currentGta))
            {
                currentGta = @"C:\Program Files (x86)\Steam\steamapps\common\Grand Theft Auto V";
            }

            _gtaFolderTextBox = new TextBox
            {
                Location = new Point(10, 42),
                Size = new Size(360, 25),
                Text = currentGta
            };
            var browseBtn = new Button
            {
                Text = "Parcourir...",
                Location = new Point(376, 40),
                Size = new Size(90, 27)
            };
            var applyGtaBtn = new Button
            {
                Text = "Appliquer",
                Location = new Point(470, 40),
                Size = new Size(80, 27),
                Tag = "Accent"
            };
            browseBtn.Click += (s, e) =>
            {
                using (var fbd = new FolderBrowserDialog())
                {
                    fbd.Description = "Sélectionnez le dossier principal de GTA V";
                    if (Directory.Exists(_gtaFolderTextBox.Text)) fbd.SelectedPath = _gtaFolderTextBox.Text;
                    if (fbd.ShowDialog(this) == DialogResult.OK)
                    {
                        _gtaFolderTextBox.Text = fbd.SelectedPath;
                        SetGtaFolderFunc?.Invoke(fbd.SelectedPath);
                    }
                }
            };
            applyGtaBtn.Click += (s, e) =>
            {
                if (SetGtaFolderFunc != null)
                {
                    SetGtaFolderFunc(_gtaFolderTextBox.Text);
                    MessageBox.Show(this, "Dossier GTA V mis à jour avec succès !", "SR File - Paramètres", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };

            var startupLabel = new Label
            {
                Text = "Mode de démarrage par défaut :",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Location = new Point(10, 90),
                AutoSize = true
            };
            var startupCombo = new ComboBox
            {
                Location = new Point(10, 115),
                Size = new Size(250, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            startupCombo.Items.AddRange(new object[] { "RPF Explorer (Exploration rapide)", "Menu Principal (Tous les outils)", "Monde 3D (World Viewer)" });
            startupCombo.SelectedIndex = 0;

            var autoSaveCheck = new CheckBox
            {
                Text = "Mémoriser et sauvegarder automatiquement les préférences",
                Font = new Font("Segoe UI", 9F),
                Location = new Point(10, 160),
                AutoSize = true,
                Checked = true
            };

            _generalPanel.Controls.AddRange(new Control[] { gtaLabel, _gtaFolderTextBox, browseBtn, applyGtaBtn, startupLabel, startupCombo, autoSaveCheck });
            ContentPanel.Controls.Add(_generalPanel);

            // 2. Theme Panel
            _themePanel = new Panel { Dock = DockStyle.Fill, Visible = false };

            var themeHeaderLabel = new Label
            {
                Text = "Bascule Mode Sombre / Mode Clair :",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Location = new Point(10, 10),
                AutoSize = true
            };

            _toggleDarkBtn = new Button
            {
                Location = new Point(10, 35),
                Size = new Size(200, 36),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Text = SRThemeManager.IsDarkMode ? "🌙 Mode Sombre Actif" : "☀️ Mode Clair Actif",
                Tag = "Accent"
            };
            _toggleDarkBtn.Click += (s, e) =>
            {
                SRThemeManager.ToggleDarkMode();
            };

            var presetLabel = new Label
            {
                Text = "Préréglages de Thèmes :",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Location = new Point(10, 85),
                AutoSize = true
            };

            _presetOrangeBtn = new Button
            {
                Text = "SR Orange (Défaut GTA)",
                Location = new Point(10, 110),
                Size = new Size(160, 32),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Tag = "Accent"
            };
            _presetOrangeBtn.Click += (s, e) =>
            {
                SRThemeManager.Preset = SRThemePreset.SROrange;
                SRThemeManager.AccentColor = Color.FromArgb(255, 122, 41);
            };

            _presetCrimsonBtn = new Button
            {
                Text = "SR Crimson",
                Location = new Point(176, 110),
                Size = new Size(100, 32),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold)
            };
            _presetCrimsonBtn.Click += (s, e) =>
            {
                SRThemeManager.Preset = SRThemePreset.SRCrimson;
                SRThemeManager.AccentColor = Color.FromArgb(225, 29, 72);
            };

            _presetSlateBtn = new Button
            {
                Text = "Dark Slate",
                Location = new Point(282, 110),
                Size = new Size(100, 32),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold)
            };
            _presetSlateBtn.Click += (s, e) =>
            {
                SRThemeManager.Preset = SRThemePreset.DarkSlate;
                SRThemeManager.AccentColor = Color.FromArgb(59, 130, 246);
            };

            _presetAmoledBtn = new Button
            {
                Text = "AMOLED",
                Location = new Point(388, 110),
                Size = new Size(95, 32),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold)
            };
            _presetAmoledBtn.Click += (s, e) =>
            {
                SRThemeManager.Preset = SRThemePreset.AmoledBlack;
                SRThemeManager.AccentColor = Color.FromArgb(255, 122, 41);
            };

            _presetLightBtn = new Button
            {
                Text = "Light Modern (Clair)",
                Location = new Point(10, 148),
                Size = new Size(160, 32),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold)
            };
            _presetLightBtn.Click += (s, e) =>
            {
                SRThemeManager.Preset = SRThemePreset.LightModern;
                SRThemeManager.AccentColor = Color.FromArgb(255, 122, 41);
            };

            var accentLabel = new Label
            {
                Text = "Couleur d\'accentuation personnalisée :",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Location = new Point(10, 195),
                AutoSize = true
            };

            Color[] colors = new Color[]
            {
                Color.FromArgb(255, 122, 41),  // SR Orange (#FF7A29)
                Color.FromArgb(225, 29, 72),   // Crimson
                Color.FromArgb(59, 130, 246),  // Blue
                Color.FromArgb(16, 185, 129),  // Emerald
                Color.FromArgb(6, 182, 212),   // Cyan
                Color.FromArgb(139, 92, 246),  // Purple
                Color.FromArgb(245, 158, 11),  // Amber / Gold
            };

            int cx = 10;
            for (int i = 0; i < colors.Length; i++)
            {
                var c = colors[i];
                var swatch = new Button
                {
                    Location = new Point(cx, 222),
                    Size = new Size(36, 36),
                    BackColor = c,
                    FlatStyle = FlatStyle.Flat
                };
                swatch.FlatAppearance.BorderSize = 0;
                swatch.Click += (s, e) =>
                {
                    SRThemeManager.AccentColor = c;
                };
                _themePanel.Controls.Add(swatch);
                cx += 44;
            }

            var customColorBtn = new Button
            {
                Text = "🎨 Autre couleur...",
                Location = new Point(cx + 8, 224),
                Size = new Size(140, 32)
            };
            customColorBtn.Click += (s, e) =>
            {
                using (var cd = new ColorDialog())
                {
                    cd.Color = SRThemeManager.AccentColor;
                    if (cd.ShowDialog(this) == DialogResult.OK)
                    {
                        SRThemeManager.AccentColor = cd.Color;
                    }
                }
            };

            _accentPreviewLabel = new Label
            {
                Text = "● Exemple de mise en valeur avec la couleur sélectionnée",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Location = new Point(10, 275),
                AutoSize = true,
                Tag = "Accent"
            };

            _themePanel.Controls.AddRange(new Control[]
            {
                themeHeaderLabel, _toggleDarkBtn, presetLabel,
                _presetOrangeBtn, _presetCrimsonBtn, _presetSlateBtn, _presetAmoledBtn, _presetLightBtn,
                accentLabel, customColorBtn, _accentPreviewLabel
            });
            ContentPanel.Controls.Add(_themePanel);

            // 3. Shortcuts Panel
            _shortcutsPanel = new Panel { Dock = DockStyle.Fill, Visible = false };

            var layoutTopPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 84,
                BackColor = SRThemeManager.Surface
            };
            var layoutLabel = new Label
            {
                Text = "Disposition du clavier pour la navigation 3D :",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Location = new Point(10, 10),
                AutoSize = true
            };
            var azertyBtn = new Button
            {
                Text = "🇫🇷 Français (AZERTY - ZQSD)",
                Location = new Point(10, 36),
                Size = new Size(210, 36),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Tag = "Accent"
            };
            var qwertyBtn = new Button
            {
                Text = "🇬🇧 English (QWERTY - WASD)",
                Location = new Point(230, 36),
                Size = new Size(210, 36),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };

            azertyBtn.Click += (s, e) =>
            {
                SetKeyboardLayoutFunc?.Invoke(true);
                MessageBox.Show(this, "Disposition française AZERTY (ZQSD) configurée avec succès !\n\nVous pouvez maintenant vous déplacer en 3D avec les touches Z-Q-S-D sans avoir à changer la langue de votre clavier Windows.", "SR File - Clavier", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };

            qwertyBtn.Click += (s, e) =>
            {
                SetKeyboardLayoutFunc?.Invoke(false);
                MessageBox.Show(this, "Disposition anglaise QWERTY (WASD) activée !", "SR File - Clavier", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };

            layoutTopPanel.Controls.AddRange(new Control[] { layoutLabel, azertyBtn, qwertyBtn });

            var shortcutsBox = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 9.5F),
                ScrollBars = ScrollBars.Vertical,
                Text = @"RACCOURCIS CLAVIER & COMMANDES :
===================================================

[ NAVIGATION CAMÉRA 3D ]
  Z / W           : Avancer (ZQSD sur AZERTY, WASD sur QWERTY)
  S               : Reculer
  Q / A           : Pas à gauche
  D               : Pas à droite
  R               : Monter en altitude
  F               : Descendre en altitude
  Flèches         : Déplacement directionnel (Haut / Bas / Gauche / Droite)
  W (AZERTY) / Z  : Zoom avant / Ralentir
  X               : Zoom arrière / Accélérer
  Shift           : Déplacement rapide (Sprint caméra)
  Molette souris  : Zoom avant / Zoom arrière
  Clic Droit      : Regarder autour / Rotation caméra à 360°

[ MODE ÉDITION ]
  A (AZERTY) / Q  : Quitter le mode édition
  Z (AZERTY) / W  : Mode translation (Position)
  E               : Mode rotation (Orientation)
  R               : Mode mise à l'échelle (Scale)
  C               : Activer / Désactiver la sélection à la souris

[ GESTION DES ARCHIVES & EXPLORATEUR RPF ]
  F5              : Rafraîchir le dossier / archive
  Ctrl + F        : Recherche dans l'archive active
  Entrée          : Ouvrir le sous-dossier ou fichier sélectionné
  Retour Arrière  : Remonter d'un dossier (Up)
  Suppr           : Supprimer le fichier sélectionné (Mode Édition)
  Ctrl + C        : Copier le chemin complet du fichier
  F2              : Renommer le fichier (Mode Édition)

[ VUES & OUTILS ]
  T               : Afficher / Masquer la barre d'outils
  Espace          : Sauter / Sélectionner
==================================================="
            };

            _shortcutsPanel.Controls.Add(shortcutsBox);
            _shortcutsPanel.Controls.Add(layoutTopPanel);
            ContentPanel.Controls.Add(_shortcutsPanel);

            // 4. About Panel
            _aboutPanel = new Panel { Dock = DockStyle.Fill, Visible = false };
            var aboutLogo = new PictureBox
            {
                Location = new Point(20, 20),
                Size = new Size(80, 80),
                SizeMode = PictureBoxSizeMode.Zoom,
                Image = SRLogo.GetLogoImage(80)
            };
            var aboutTitle = new Label
            {
                Text = "SR FILE",
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                Location = new Point(120, 20),
                AutoSize = true,
                Tag = "Accent"
            };
            var aboutDesc = new Label
            {
                Text = "Suite complète d'exploration, visualisation 3D et modding pour Grand Theft Auto V.\n\n" +
                       "Nom officiel : SR File\n" +
                       "Version : 2.0 (Modern Edition)\n" +
                       "Auteur : Dédié à votre nom (SR File)\n\n" +
                       "Fonctionnalités :\n" +
                       "• Explorateur RPF natif avec recherche instantanée\n" +
                       "• Vue 3D du monde de GTA V avec DirectX 11\n" +
                       "• Système de thèmes modernes synchronisés avec Mode Sombre\n" +
                       "• Extraction et conversion de textures, modèles, animations et scripts",
                Font = new Font("Segoe UI", 9.5F),
                Location = new Point(120, 60),
                Size = new Size(390, 220),
                Tag = "SubText"
            };
            _aboutPanel.Controls.AddRange(new Control[] { aboutLogo, aboutTitle, aboutDesc });
            ContentPanel.Controls.Add(_aboutPanel);
        }

        private void ShowTab(int tabIndex)
        {
            _generalPanel.Visible = (tabIndex == 0);
            _themePanel.Visible = (tabIndex == 1);
            _shortcutsPanel.Visible = (tabIndex == 2);
            _aboutPanel.Visible = (tabIndex == 3);

            TabGeneralBtn.Tag = (tabIndex == 0) ? "Accent" : null;
            TabThemeBtn.Tag = (tabIndex == 1) ? "Accent" : null;
            TabShortcutsBtn.Tag = (tabIndex == 2) ? "Accent" : null;
            TabAboutBtn.Tag = (tabIndex == 3) ? "Accent" : null;

            ApplyCurrentTheme();
        }

        private void OnThemeChanged()
        {
            if (this.IsDisposed) return;
            _toggleDarkBtn.Text = SRThemeManager.IsDarkMode ? "🌙 Mode Sombre Actif" : "☀️ Mode Clair Actif";
            ApplyCurrentTheme();
        }

        private void ApplyCurrentTheme()
        {
            SRThemeManager.ApplyTheme(this);
            SidebarPanel.BackColor = SRThemeManager.Card;
            HeaderPanel.BackColor = SRThemeManager.Surface;
            FooterPanel.BackColor = SRThemeManager.Surface;

            if (_accentPreviewLabel != null)
            {
                _accentPreviewLabel.ForeColor = SRThemeManager.AccentColor;
            }
        }

        private void TabGeneralBtn_Click(object sender, EventArgs e) => ShowTab(0);
        private void TabThemeBtn_Click(object sender, EventArgs e) => ShowTab(1);
        private void TabShortcutsBtn_Click(object sender, EventArgs e) => ShowTab(2);
        private void TabAboutBtn_Click(object sender, EventArgs e) => ShowTab(3);
        private void CloseBtn_Click(object sender, EventArgs e) => this.Close();
    }
}