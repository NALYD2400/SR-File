using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace CodeWalker.WinForms
{
    public class SRTopNavbar : ToolStripFix
    {
        private readonly Form _parentForm;
        private readonly Action _onRefresh;
        private readonly Action _openSettingsAction;
        private readonly bool _isBorderless;

        private ToolStripLabel _logoItem;
        private ToolStripLabel _titleItem;
        private ToolStripLabel _subtitleItem;
        private ToolStripButton _themeBtn;
        private ToolStripSeparator _sepTheme;
        private ToolStripButton _refreshBtn;
        private ToolStripSeparator _sepRefresh;
        private ToolStripButton _settingsBtn;

        // Window control buttons (close, max, min) for borderless window
        private ToolStripSeparator _sepWin;
        private ToolStripButton _minBtn;
        private ToolStripButton _maxBtn;
        private ToolStripButton _closeBtn;

        private Action _themeSyncAction;

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();
        [DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HT_CAPTION = 0x2;

        public SRTopNavbar(Form parentForm, string subtitle = null, Action onRefresh = null, Action openSettingsAction = null, bool isBorderless = false)
        {
            _parentForm = parentForm;
            _onRefresh = onRefresh;
            _openSettingsAction = openSettingsAction;
            _isBorderless = isBorderless || (_parentForm != null && _parentForm.FormBorderStyle == FormBorderStyle.None);

            Dock = DockStyle.Top;
            Height = 38;
            AutoSize = false;
            GripStyle = ToolStripGripStyle.Hidden;
            CanOverflow = false;
            ShowItemToolTips = true;
            Padding = new Padding(10, 0, 4, 0);
            Margin = Padding.Empty;
            Renderer = new SRToolStripRenderer();
            BackColor = SRThemeManager.Surface;
            TabStop = false;

            BuildItems(subtitle);
            AttachDragAndWindowEvents();

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

        private void AttachDragAndWindowEvents()
        {
            MouseDown += (s, e) => HandleDrag(e);
            if (_logoItem != null) _logoItem.MouseDown += (s, e) => HandleDrag(e);
            if (_titleItem != null) _titleItem.MouseDown += (s, e) => HandleDrag(e);
            if (_subtitleItem != null) _subtitleItem.MouseDown += (s, e) => HandleDrag(e);

            DoubleClick += (s, e) => ToggleMaximize();
            if (_titleItem != null) _titleItem.DoubleClick += (s, e) => ToggleMaximize();
            if (_subtitleItem != null) _subtitleItem.DoubleClick += (s, e) => ToggleMaximize();

            if (_parentForm != null)
            {
                _parentForm.Resize += (s, e) =>
                {
                    if (_maxBtn != null && !_maxBtn.IsDisposed)
                    {
                        _maxBtn.Text = _parentForm.WindowState == FormWindowState.Maximized ? "❐" : "□";
                        _maxBtn.ToolTipText = _parentForm.WindowState == FormWindowState.Maximized ? "Restaurer" : "Agrandir";
                    }
                };
            }
        }

        private void HandleDrag(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && _parentForm != null)
            {
                ToolStripItem item = GetItemAt(e.Location);
                if (item is ToolStripButton || item is ToolStripDropDownButton || item is ToolStripSplitButton)
                {
                    return;
                }

                ReleaseCapture();
                SendMessage(_parentForm.Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        private void ToggleMaximize()
        {
            if (_parentForm != null)
            {
                _parentForm.WindowState = _parentForm.WindowState == FormWindowState.Maximized
                    ? FormWindowState.Normal
                    : FormWindowState.Maximized;
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

            // --- RIGHT: Controls with Alignment=Right ---
            // In ToolStrip, items added first with Alignment=Right go to the farthest right!
            // Desired visual order from left to right on screen:
            // [Theme] | [Refresh] | [Settings] | [—] [□] [✕]
            // Therefore, addition order (rightmost to leftmost):
            // 1. Close [✕]
            // 2. Maximize [□]
            // 3. Minimize [—]
            // 4. Separator
            // 5. Settings [⚙ Réglages]
            // 6. Separator
            // 7. Refresh [🔄 Actualiser]
            // 8. Separator
            // 9. Theme [🌙 Sombre]

            if (_isBorderless)
            {
                // 1. Close button (farthest right)
                _closeBtn = new ToolStripButton
                {
                    Text = "✕",
                    ToolTipText = "Fermer",
                    DisplayStyle = ToolStripItemDisplayStyle.Text,
                    Tag = "SR_NAVBAR_CLOSE",
                    Alignment = ToolStripItemAlignment.Right,
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                    ForeColor = SRThemeManager.SubText,
                    AutoSize = false,
                    Width = 44,
                    Height = 36,
                    Margin = new Padding(0, 0, 2, 0)
                };
                _closeBtn.Click += (s, e) => _parentForm?.Close();
                Items.Add(_closeBtn);

                // 2. Maximize / Restore button
                _maxBtn = new ToolStripButton
                {
                    Text = _parentForm?.WindowState == FormWindowState.Maximized ? "❐" : "□",
                    ToolTipText = _parentForm?.WindowState == FormWindowState.Maximized ? "Restaurer" : "Agrandir",
                    DisplayStyle = ToolStripItemDisplayStyle.Text,
                    Tag = "SR_NAVBAR_WIN_BTN",
                    Alignment = ToolStripItemAlignment.Right,
                    Font = new Font("Segoe UI", 10f, FontStyle.Regular),
                    ForeColor = SRThemeManager.SubText,
                    AutoSize = false,
                    Width = 44,
                    Height = 36,
                    Margin = Padding.Empty
                };
                _maxBtn.Click += (s, e) => ToggleMaximize();
                Items.Add(_maxBtn);

                // 3. Minimize button
                _minBtn = new ToolStripButton
                {
                    Text = "—",
                    ToolTipText = "Réduire",
                    DisplayStyle = ToolStripItemDisplayStyle.Text,
                    Tag = "SR_NAVBAR_WIN_BTN",
                    Alignment = ToolStripItemAlignment.Right,
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                    ForeColor = SRThemeManager.SubText,
                    AutoSize = false,
                    Width = 44,
                    Height = 36,
                    Margin = Padding.Empty
                };
                _minBtn.Click += (s, e) =>
                {
                    if (_parentForm != null) _parentForm.WindowState = FormWindowState.Minimized;
                };
                Items.Add(_minBtn);

                // 4. Separator before window controls
                _sepWin = new ToolStripSeparator
                {
                    Tag = "SR_NAVBAR_ACTION",
                    Alignment = ToolStripItemAlignment.Right,
                    Margin = new Padding(4, 8, 4, 8)
                };
                Items.Add(_sepWin);
            }

            // 5. Settings button
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

            // 6. Separator between Refresh and Settings
            _sepRefresh = new ToolStripSeparator
            {
                Tag = "SR_NAVBAR_ACTION",
                Alignment = ToolStripItemAlignment.Right,
                Margin = new Padding(4, 8, 4, 8)
            };
            Items.Add(_sepRefresh);

            // 7. Refresh button
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

            // 8. Separator between Theme and Refresh
            _sepTheme = new ToolStripSeparator
            {
                Tag = "SR_NAVBAR_ACTION",
                Alignment = ToolStripItemAlignment.Right,
                Margin = new Padding(4, 8, 4, 8)
            };
            Items.Add(_sepTheme);

            // 9. Dark Mode button
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
            if (_minBtn != null && !_minBtn.IsDisposed)
            {
                _minBtn.ForeColor = SRThemeManager.SubText;
            }
            if (_maxBtn != null && !_maxBtn.IsDisposed)
            {
                _maxBtn.ForeColor = SRThemeManager.SubText;
            }
            if (_closeBtn != null && !_closeBtn.IsDisposed)
            {
                _closeBtn.ForeColor = SRThemeManager.SubText;
            }
            Invalidate();
        }
    }

    public class BorderlessFormResizer : NativeWindow
    {
        private readonly Form _form;
        private const int WM_NCCALCSIZE = 0x83;
        private const int WM_NCHITTEST = 0x84;
        private const int WM_GETMINMAXINFO = 0x24;
        private const int HTLEFT = 10;
        private const int HTRIGHT = 11;
        private const int HTTOP = 12;
        private const int HTTOPLEFT = 13;
        private const int HTTOPRIGHT = 14;
        private const int HTBOTTOM = 15;
        private const int HTBOTTOMLEFT = 16;
        private const int HTBOTTOMRIGHT = 17;
        private const int HTCLIENT = 1;

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MINMAXINFO
        {
            public POINT ptReserved;
            public POINT ptMaxSize;
            public POINT ptMaxPosition;
            public POINT ptMinTrackSize;
            public POINT ptMaxTrackSize;
        }

        public BorderlessFormResizer(Form form)
        {
            _form = form;
            if (form.IsHandleCreated) AssignHandle(form.Handle);
            else form.HandleCreated += (s, e) => AssignHandle(form.Handle);
            form.HandleDestroyed += (s, e) => ReleaseHandle();
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_NCCALCSIZE)
            {
                // Suppress Windows non-client frame calculation completely to eliminate the top bar
                m.Result = IntPtr.Zero;
                return;
            }

            if (m.Msg == WM_GETMINMAXINFO && _form != null && !_form.IsDisposed)
            {
                base.WndProc(ref m);
                try
                {
                    var screen = Screen.FromHandle(_form.Handle);
                    var workArea = screen.WorkingArea;
                    var monitorArea = screen.Bounds;

                    MINMAXINFO mmi = (MINMAXINFO)Marshal.PtrToStructure(m.LParam, typeof(MINMAXINFO));
                    mmi.ptMaxPosition.x = Math.Abs(workArea.Left - monitorArea.Left);
                    mmi.ptMaxPosition.y = Math.Abs(workArea.Top - monitorArea.Top);
                    mmi.ptMaxSize.x = workArea.Width;
                    mmi.ptMaxSize.y = workArea.Height;
                    Marshal.StructureToPtr(mmi, m.LParam, true);
                }
                catch { }
                return;
            }

            if (m.Msg == WM_NCHITTEST && _form != null && _form.WindowState == FormWindowState.Normal)
            {
                base.WndProc(ref m);
                if ((int)m.Result == HTCLIENT)
                {
                    Point screenPt = new Point(m.LParam.ToInt32());
                    Point pt = _form.PointToClient(screenPt);
                    int border = 7;
                    bool left = pt.X <= border;
                    bool right = pt.X >= _form.ClientSize.Width - border;
                    bool top = pt.Y <= border;
                    bool bottom = pt.Y >= _form.ClientSize.Height - border;

                    if (top && left) m.Result = (IntPtr)HTTOPLEFT;
                    else if (top && right) m.Result = (IntPtr)HTTOPRIGHT;
                    else if (bottom && left) m.Result = (IntPtr)HTBOTTOMLEFT;
                    else if (bottom && right) m.Result = (IntPtr)HTBOTTOMRIGHT;
                    else if (left) m.Result = (IntPtr)HTLEFT;
                    else if (right) m.Result = (IntPtr)HTRIGHT;
                    else if (top) m.Result = (IntPtr)HTTOP;
                    else if (bottom) m.Result = (IntPtr)HTBOTTOM;
                }
                return;
            }

            base.WndProc(ref m);
        }
    }
}
