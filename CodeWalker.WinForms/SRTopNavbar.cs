using System;
using System.Drawing;
using System.Windows.Forms;

namespace CodeWalker.WinForms
{
    public class SRTopNavbar : ToolStripFix
    {
        private readonly Form _parentForm;
        private readonly Action _onRefresh;
        private readonly Action _openSettingsAction;

        private ToolStripLabel _logoItem;
        private ToolStripLabel _titleItem;
        private ToolStripLabel _subtitleItem;
        private ToolStripButton _themeBtn;
        private ToolStripSeparator _sepTheme;
        private ToolStripButton _refreshBtn;
        private ToolStripSeparator _sepRefresh;
        private ToolStripButton _settingsBtn;

        private Action _themeSyncAction;

        public SRTopNavbar(Form parentForm, string subtitle = null, Action onRefresh = null, Action openSettingsAction = null)
        {
            _parentForm = parentForm;
            _onRefresh = onRefresh;
            _openSettingsAction = openSettingsAction;

            Dock = DockStyle.Top;
            Height = 38;
            AutoSize = false;
            GripStyle = ToolStripGripStyle.Hidden;
            CanOverflow = false;
            ShowItemToolTips = true;
            Padding = new Padding(10, 0, 10, 0);
            Margin = Padding.Empty;
            Renderer = new SRToolStripRenderer();
            BackColor = SRThemeManager.Surface;
            TabStop = false;

            BuildItems(subtitle);

            _themeSyncAction = () =>
            {
                if (IsDisposed) return;
                if (InvokeRequired)
                {
                    try { BeginInvoke(new Action(ApplyThemeState)); } catch { }
                }
                else
                {
                    ApplyThemeState();
                }
            };
            SRThemeManager.ThemeChanged += _themeSyncAction;

            if (_parentForm != null)
            {
                _parentForm.FormClosed += (s, e) =>
                {
                    SRThemeManager.ThemeChanged -= _themeSyncAction;
                };
            }
        }

        public void SetSubtitle(string subtitle)
        {
            if (_subtitleItem != null && !_subtitleItem.IsDisposed)
            {
                if (string.IsNullOrEmpty(subtitle))
                {
                    _subtitleItem.Visible = false;
                }
                else
                {
                    _subtitleItem.Text = "• " + subtitle.ToUpperInvariant();
                    _subtitleItem.Visible = true;
                }
            }
        }

        private void BuildItems(string subtitle)
        {
            Items.Clear();

            // --- LEFT: Brand logo badge & titles ---
            Image logoBadge = null;
            try { logoBadge = SRLogo.GetLogoBadgeImage(28, 20); } catch { }

            _logoItem = new ToolStripLabel
            {
                Image = logoBadge,
                DisplayStyle = ToolStripItemDisplayStyle.Image,
                Tag = "SR_NAVBAR_BRAND",
                ToolTipText = "SR File - Grand Theft Auto V Suite",
                Margin = new Padding(0, 4, 8, 4)
            };
            Items.Add(_logoItem);

            _titleItem = new ToolStripLabel
            {
                Text = "SR FILE",
                ForeColor = SRThemeManager.AccentColor,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Tag = "SR_NAVBAR_BRAND",
                Margin = new Padding(0, 0, 4, 0),
                ToolTipText = "SR File"
            };
            Items.Add(_titleItem);

            if (string.IsNullOrEmpty(subtitle) && _parentForm != null)
            {
                string t = _parentForm.Text ?? "";
                if (t.StartsWith("SR File - ", StringComparison.OrdinalIgnoreCase))
                {
                    subtitle = t.Substring(10).Trim();
                }
                else if (t.StartsWith("SR File", StringComparison.OrdinalIgnoreCase))
                {
                    subtitle = t.Substring(7).Trim();
                }
            }

            _subtitleItem = new ToolStripLabel
            {
                Text = string.IsNullOrEmpty(subtitle) ? "" : "• " + subtitle.ToUpperInvariant(),
                ForeColor = SRThemeManager.SubText,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Tag = "SR_NAVBAR_SUBTITLE",
                Margin = new Padding(2, 0, 6, 0),
                Visible = !string.IsNullOrEmpty(subtitle)
            };
            Items.Add(_subtitleItem);

            // --- RIGHT: Settings, Separator, Refresh, Separator, Theme ---
            // In WinForms ToolStrip, items with Alignment=Right are positioned from right to left
            // in the order they are added.
            // Desired visual display from left to right: [DarkMode] | [Actualiser] | [Réglages]
            // So we add them in reverse: Settings first, then Sep, then Refresh, then Sep, then DarkMode!

            // 1. Settings button (farthest right)
            _settingsBtn = new ToolStripButton
            {
                Text = "⚙ Réglages",
                ToolTipText = "Ouvrir les réglages et la personnalisation de SR File",
                DisplayStyle = ToolStripItemDisplayStyle.Text,
                Tag = "SR_NAVBAR_ACTION",
                Alignment = ToolStripItemAlignment.Right,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = SRThemeManager.Text,
                Margin = new Padding(2, 4, 0, 4)
            };
            _settingsBtn.Click += (s, e) =>
            {
                if (_openSettingsAction != null)
                {
                    _openSettingsAction();
                }
                else
                {
                    using (var dlg = new SRSettingsForm())
                    {
                        dlg.ShowDialog(_parentForm);
                    }
                }
            };
            Items.Add(_settingsBtn);

            // 2. Separator between Refresh and Settings
            _sepRefresh = new ToolStripSeparator
            {
                Tag = "SR_NAVBAR_ACTION",
                Alignment = ToolStripItemAlignment.Right,
                Margin = new Padding(4, 8, 4, 8)
            };
            Items.Add(_sepRefresh);

            // 3. Refresh button
            _refreshBtn = new ToolStripButton
            {
                Text = "🔄 Actualiser",
                ToolTipText = "Actualiser l'affichage et recharger les données",
                DisplayStyle = ToolStripItemDisplayStyle.Text,
                Tag = "SR_NAVBAR_ACTION",
                Alignment = ToolStripItemAlignment.Right,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = SRThemeManager.Text,
                Margin = new Padding(2, 4, 2, 4)
            };
            _refreshBtn.Click += (s, e) =>
            {
                TriggerRefresh();
            };
            Items.Add(_refreshBtn);

            // 4. Separator between Theme and Refresh
            _sepTheme = new ToolStripSeparator
            {
                Tag = "SR_NAVBAR_ACTION",
                Alignment = ToolStripItemAlignment.Right,
                Margin = new Padding(4, 8, 4, 8)
            };
            Items.Add(_sepTheme);

            // 5. Dark Mode button
            _themeBtn = new ToolStripButton
            {
                Text = SRThemeManager.IsDarkMode ? "🌙 Sombre" : "☀️ Clair",
                ToolTipText = "Basculer entre Mode Sombre et Mode Clair",
                DisplayStyle = ToolStripItemDisplayStyle.Text,
                Tag = "SR_NAVBAR_ACTION",
                Alignment = ToolStripItemAlignment.Right,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = SRThemeManager.Text,
                Margin = new Padding(2, 4, 2, 4)
            };
            _themeBtn.Click += (s, e) =>
            {
                SRThemeManager.ToggleDarkMode();
            };
            Items.Add(_themeBtn);
        }

        private void TriggerRefresh()
        {
            try
            {
                if (_onRefresh != null)
                {
                    _onRefresh();
                }
                else if (_parentForm != null)
                {
                    var refreshProp = _parentForm.GetType().GetField("RefreshButton", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (refreshProp?.GetValue(_parentForm) is ToolStripButton tb)
                    {
                        tb.PerformClick();
                    }
                    else if (refreshProp?.GetValue(_parentForm) is Button b)
                    {
                        b.PerformClick();
                    }

                    var m = _parentForm.GetType().GetMethod("RefreshView", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                         ?? _parentForm.GetType().GetMethod("Reload", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                         ?? _parentForm.GetType().GetMethod("RefreshData", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    m?.Invoke(_parentForm, null);

                    SRThemeManager.ApplyTheme(_parentForm);
                    _parentForm.Invalidate(true);
                    _parentForm.Update();
                    _parentForm.Refresh();
                }
            }
            catch { }
        }

        public void ApplyThemeState()
        {
            BackColor = SRThemeManager.Surface;
            Renderer = new SRToolStripRenderer();

            if (_titleItem != null && !_titleItem.IsDisposed)
            {
                _titleItem.ForeColor = SRThemeManager.AccentColor;
            }
            if (_subtitleItem != null && !_subtitleItem.IsDisposed)
            {
                _subtitleItem.ForeColor = SRThemeManager.SubText;
            }
            if (_themeBtn != null && !_themeBtn.IsDisposed)
            {
                _themeBtn.Text = SRThemeManager.IsDarkMode ? "🌙 Sombre" : "☀️ Clair";
                _themeBtn.ForeColor = SRThemeManager.Text;
            }
            if (_refreshBtn != null && !_refreshBtn.IsDisposed)
            {
                _refreshBtn.ForeColor = SRThemeManager.Text;
            }
            if (_settingsBtn != null && !_settingsBtn.IsDisposed)
            {
                _settingsBtn.ForeColor = SRThemeManager.Text;
            }
            Invalidate();
        }
    }
}
