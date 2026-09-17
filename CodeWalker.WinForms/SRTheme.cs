using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace CodeWalker.WinForms
{
    public enum SRThemePreset
    {
        SROrange,
        SRCrimson,
        DarkSlate,
        AmoledBlack,
        LightModern
    }

    public static class SRThemeManager
    {
        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        [DllImport("uxtheme.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
        private static extern int SetWindowTheme(IntPtr hWnd, string pszSubAppName, string pszSubIdList);

        public static event Action ThemeChanged;

        private static bool _isDarkMode = true;
        private static SRThemePreset _preset = SRThemePreset.SROrange;
        private static Color _accentColor = Color.FromArgb(255, 122, 41); // #FF7A29 - GTA SR Signature Coral Orange

        private static string ConfigPath
        {
            get
            {
                try
                {
                    string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SRFile");
                    if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                    return Path.Combine(dir, "theme_settings.ini");
                }
                catch
                {
                    return "theme_settings.ini";
                }
            }
        }

        static SRThemeManager()
        {
            LoadSettings();
        }

        public static bool IsDarkMode
        {
            get => _isDarkMode;
            set
            {
                if (_isDarkMode != value)
                {
                    _isDarkMode = value;
                    if (!_isDarkMode && _preset != SRThemePreset.LightModern)
                    {
                        _preset = SRThemePreset.LightModern;
                    }
                    else if (_isDarkMode && _preset == SRThemePreset.LightModern)
                    {
                        _preset = SRThemePreset.SROrange;
                    }
                    UpdatePalette();
                    SaveSettings();
                    ThemeChanged?.Invoke();
                }
            }
        }

        public static SRThemePreset Preset
        {
            get => _preset;
            set
            {
                _preset = value;
                _isDarkMode = (_preset != SRThemePreset.LightModern);
                UpdatePalette();
                SaveSettings();
                ThemeChanged?.Invoke();
            }
        }

        public static Color AccentColor
        {
            get => _accentColor;
            set
            {
                _accentColor = value;
                UpdatePalette();
                SaveSettings();
                ThemeChanged?.Invoke();
            }
        }

        public static Color Background { get; private set; } = Color.FromArgb(10, 14, 22);       // #0A0E16
        public static Color Surface { get; private set; } = Color.FromArgb(18, 24, 36);          // #121824
        public static Color Card { get; private set; } = Color.FromArgb(24, 34, 50);             // #182232
        public static Color CardHover { get; private set; } = Color.FromArgb(34, 46, 66);        // #222E42
        public static Color Border { get; private set; } = Color.FromArgb(34, 45, 66);           // #222D42
        public static Color BorderHover { get; private set; } = Color.FromArgb(255, 122, 41);
        public static Color Text { get; private set; } = Color.FromArgb(255, 255, 255);          // #FFFFFF
        public static Color SubText { get; private set; } = Color.FromArgb(142, 156, 174);       // #8E9CAE
        public static Color InputBackground { get; private set; } = Color.FromArgb(13, 18, 27);  // #0D121B
        public static Color AccentHover { get; private set; } = Color.FromArgb(255, 145, 77);    // #FF914D
        public static Color HeaderGradientStart { get; private set; } = Color.FromArgb(42, 22, 8);
        public static Color HeaderGradientEnd { get; private set; } = Color.FromArgb(18, 24, 36);

        public static void UpdatePalette()
        {
            switch (_preset)
            {
                case SRThemePreset.SROrange:
                    _isDarkMode = true;
                    Background = Color.FromArgb(10, 14, 22);          // #0A0E16
                    Surface = Color.FromArgb(18, 24, 36);             // #121824
                    Card = Color.FromArgb(24, 34, 50);                // #182232
                    CardHover = Color.FromArgb(34, 46, 66);           // #222E42
                    Border = Color.FromArgb(34, 45, 66);              // #222D42
                    BorderHover = _accentColor;
                    Text = Color.FromArgb(255, 255, 255);             // #FFFFFF
                    SubText = Color.FromArgb(142, 156, 174);          // #8E9CAE
                    InputBackground = Color.FromArgb(13, 18, 27);     // #0D121B
                    AccentHover = Color.FromArgb(255, 145, 77);       // #FF914D
                    HeaderGradientStart = Color.FromArgb(42, 22, 8);
                    HeaderGradientEnd = Color.FromArgb(18, 24, 36);
                    break;

                case SRThemePreset.SRCrimson:
                    _isDarkMode = true;
                    Background = Color.FromArgb(14, 15, 20);
                    Surface = Color.FromArgb(22, 24, 32);
                    Card = Color.FromArgb(29, 32, 43);
                    CardHover = Color.FromArgb(39, 43, 58);
                    Border = Color.FromArgb(44, 49, 66);
                    BorderHover = _accentColor;
                    Text = Color.FromArgb(255, 255, 255);
                    SubText = Color.FromArgb(148, 163, 184);
                    InputBackground = Color.FromArgb(18, 19, 25);
                    AccentHover = Color.FromArgb(255, 45, 85);
                    HeaderGradientStart = Color.FromArgb(56, 16, 28);
                    HeaderGradientEnd = Color.FromArgb(22, 24, 32);
                    break;

                case SRThemePreset.DarkSlate:
                    _isDarkMode = true;
                    Background = Color.FromArgb(20, 22, 27);
                    Surface = Color.FromArgb(28, 31, 38);
                    Card = Color.FromArgb(37, 41, 50);
                    CardHover = Color.FromArgb(49, 54, 65);
                    Border = Color.FromArgb(58, 64, 78);
                    BorderHover = _accentColor;
                    Text = Color.FromArgb(248, 250, 252);
                    SubText = Color.FromArgb(148, 163, 184);
                    InputBackground = Color.FromArgb(24, 27, 34);
                    AccentHover = ControlPaint.Dark(_accentColor, 0.15f);
                    HeaderGradientStart = Color.FromArgb(25, 35, 52);
                    HeaderGradientEnd = Color.FromArgb(20, 22, 27);
                    break;

                case SRThemePreset.AmoledBlack:
                    _isDarkMode = true;
                    Background = Color.FromArgb(8, 8, 8);
                    Surface = Color.FromArgb(18, 18, 18);
                    Card = Color.FromArgb(28, 28, 28);
                    CardHover = Color.FromArgb(42, 42, 42);
                    Border = Color.FromArgb(50, 50, 50);
                    BorderHover = _accentColor;
                    Text = Color.FromArgb(255, 255, 255);
                    SubText = Color.FromArgb(160, 160, 160);
                    InputBackground = Color.FromArgb(14, 14, 14);
                    AccentHover = ControlPaint.Dark(_accentColor, 0.15f);
                    HeaderGradientStart = Color.FromArgb(30, 8, 14);
                    HeaderGradientEnd = Color.FromArgb(8, 8, 8);
                    break;

                case SRThemePreset.LightModern:
                    _isDarkMode = false;
                    Background = Color.FromArgb(244, 246, 252);       // #F4F6FC
                    Surface = Color.FromArgb(255, 255, 255);
                    Card = Color.FromArgb(255, 255, 255);
                    CardHover = Color.FromArgb(240, 243, 250);
                    Border = Color.FromArgb(218, 224, 235);
                    BorderHover = _accentColor;
                    Text = Color.FromArgb(17, 14, 29);                // #110E1D
                    SubText = Color.FromArgb(100, 116, 139);
                    InputBackground = Color.FromArgb(255, 255, 255);
                    AccentHover = Color.FromArgb(235, 105, 25);
                    HeaderGradientStart = Color.FromArgb(255, 245, 238);
                    HeaderGradientEnd = Color.FromArgb(244, 246, 252);
                    break;
            }
        }

        public static void ToggleDarkMode()
        {
            IsDarkMode = !IsDarkMode;
        }

        public static void LoadSettings()
        {
            try
            {
                string path = ConfigPath;
                if (File.Exists(path))
                {
                    string[] lines = File.ReadAllLines(path);
                    foreach (string raw in lines)
                    {
                        string line = raw.Trim();
                        if (line.StartsWith("DarkMode=", StringComparison.OrdinalIgnoreCase))
                        {
                            bool b;
                            if (bool.TryParse(line.Substring(9), out b)) _isDarkMode = b;
                        }
                        else if (line.StartsWith("Preset=", StringComparison.OrdinalIgnoreCase))
                        {
                            try
                            {
                                _preset = (SRThemePreset)Enum.Parse(typeof(SRThemePreset), line.Substring(7));
                            }
                            catch { }
                        }
                        else if (line.StartsWith("Accent=", StringComparison.OrdinalIgnoreCase))
                        {
                            string hex = line.Substring(7);
                            _accentColor = ColorTranslator.FromHtml(hex);
                        }
                    }
                }
            }
            catch { }
            UpdatePalette();
        }

        public static void SaveSettings()
        {
            try
            {
                string path = ConfigPath;
                string content = string.Format("DarkMode={0}\nPreset={1}\nAccent=#{2:X2}{3:X2}{4:X2}\n",
                    _isDarkMode, _preset, _accentColor.R, _accentColor.G, _accentColor.B);
                File.WriteAllText(path, content);
            }
            catch { }
        }

        public static void ApplyDwmDarkTitle(Form form)
        {
            if (form == null || form.IsDisposed) return;
            try
            {
                int val = _isDarkMode ? 1 : 0;
                DwmSetWindowAttribute(form.Handle, 20, ref val, sizeof(int));
                DwmSetWindowAttribute(form.Handle, 19, ref val, sizeof(int));
            }
            catch { }
        }

        private static readonly Dictionary<TabControl, DarkTabControlSubclass> _tabSubclasses = new Dictionary<TabControl, DarkTabControlSubclass>();
        private static readonly Dictionary<ComboBox, DarkComboBoxSubclass> _comboSubclasses = new Dictionary<ComboBox, DarkComboBoxSubclass>();

        public static void ApplyTheme(Form form)
        {
            if (form == null || form.IsDisposed) return;

            ApplyDwmDarkTitle(form);
            form.BackColor = Background;
            form.ForeColor = Text;

            ApplyToControls(form.Controls);

            try
            {
                var compField = form.GetType().GetField("components", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
                if (compField != null)
                {
                    var container = compField.GetValue(form) as System.ComponentModel.IContainer;
                    if (container != null && container.Components != null)
                    {
                        foreach (System.ComponentModel.IComponent comp in container.Components)
                        {
                            if (comp is ContextMenuStrip cms) ApplyToContextMenu(cms);
                            else if (comp is ToolStrip ts)
                            {
                                ts.Renderer = new SRToolStripRenderer();
                                ts.BackColor = Surface;
                                ts.ForeColor = Text;
                                ApplyToToolStripItems(ts.Items);
                            }
                        }
                    }
                }

                var fields = form.GetType().GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
                foreach (var field in fields)
                {
                    if (typeof(ContextMenuStrip).IsAssignableFrom(field.FieldType))
                    {
                        var cms = field.GetValue(form) as ContextMenuStrip;
                        if (cms != null) ApplyToContextMenu(cms);
                    }
                }
            }
            catch { }
        }

        public static void ApplyToControls(Control.ControlCollection controls)
        {
            if (controls == null) return;

            foreach (Control c in controls)
            {
                if (c == null || c.IsDisposed) continue;

                if (c is MenuStrip ms)
                {
                    ms.Renderer = new SRToolStripRenderer();
                    ms.BackColor = Surface;
                    ms.ForeColor = Text;
                    ApplyToToolStripItems(ms.Items);
                }
                else if (c is StatusStrip ss)
                {
                    ss.Renderer = new SRToolStripRenderer();
                    ss.BackColor = Surface;
                    ss.ForeColor = SubText;
                    ApplyToToolStripItems(ss.Items);
                }
                else if (c is ToolStrip ts)
                {
                    ts.Renderer = new SRToolStripRenderer();
                    ts.BackColor = Surface;
                    ts.ForeColor = Text;
                    ApplyToToolStripItems(ts.Items);
                }
                else if (c is Button btn)
                {
                    btn.FlatStyle = FlatStyle.Flat;
                    if (btn.Tag != null && btn.Tag.ToString() == "Accent")
                    {
                        btn.FlatAppearance.BorderSize = 0;
                        btn.BackColor = AccentColor;
                        btn.ForeColor = Color.White;
                        btn.FlatAppearance.MouseOverBackColor = AccentHover;
                    }
                    else
                    {
                        btn.FlatAppearance.BorderSize = 1;
                        btn.FlatAppearance.BorderColor = Border;
                        btn.BackColor = Card;
                        btn.ForeColor = Text;
                        btn.FlatAppearance.MouseOverBackColor = CardHover;
                    }
                }
                else if (c is TextBox tb)
                {
                    tb.BackColor = InputBackground;
                    tb.ForeColor = Text;
                    tb.BorderStyle = BorderStyle.FixedSingle;
                }
                else if (c is TreeView tv)
                {
                    tv.BackColor = InputBackground;
                    tv.ForeColor = Text;
                    tv.LineColor = SubText;
                    tv.BorderStyle = BorderStyle.FixedSingle;
                    try { SetWindowTheme(tv.Handle, _isDarkMode ? "DarkMode_Explorer" : "Explorer", null); } catch { }
                    if (tv.ContextMenuStrip != null) ApplyToContextMenu(tv.ContextMenuStrip);
                }
                else if (c is ListView lv)
                {
                    lv.BackColor = InputBackground;
                    lv.ForeColor = Text;
                    lv.BorderStyle = BorderStyle.FixedSingle;
                    try { SetWindowTheme(lv.Handle, _isDarkMode ? "DarkMode_Explorer" : "Explorer", null); } catch { }
                    if (lv.ContextMenuStrip != null) ApplyToContextMenu(lv.ContextMenuStrip);
                }
                else if (c is ListBox lb)
                {
                    lb.BackColor = InputBackground;
                    lb.ForeColor = Text;
                    lb.BorderStyle = BorderStyle.FixedSingle;
                    lb.DrawMode = DrawMode.OwnerDrawFixed;
                    if (lb.ItemHeight < 18) lb.ItemHeight = 18;
                    lb.DrawItem -= OnListBoxDrawItem;
                    lb.DrawItem += OnListBoxDrawItem;
                    if (lb.ContextMenuStrip != null) ApplyToContextMenu(lb.ContextMenuStrip);
                }
                else if (c is PropertyGrid pg)
                {
                    pg.BackColor = Surface;
                    pg.ViewBackColor = InputBackground;
                    pg.ViewForeColor = Text;
                    pg.LineColor = Border;
                    pg.CategoryForeColor = SubText;
                    pg.HelpBackColor = Surface;
                    pg.HelpForeColor = Text;
                }
                else if (c is ComboBox cb)
                {
                    cb.BackColor = InputBackground;
                    cb.ForeColor = Text;
                    cb.FlatStyle = FlatStyle.Flat;
                    cb.DrawMode = DrawMode.OwnerDrawFixed;
                    if (cb.ItemHeight < 17) cb.ItemHeight = 17;
                    cb.DrawItem -= OnComboBoxDrawItem;
                    cb.DrawItem += OnComboBoxDrawItem;
                    if (!_comboSubclasses.ContainsKey(cb))
                    {
                        _comboSubclasses[cb] = new DarkComboBoxSubclass(cb);
                    }
                }
                else if (c is Label lbl)
                {
                    if (lbl.BackColor == SystemColors.Control || lbl.BackColor == Color.FromKnownColor(KnownColor.Control))
                    {
                        lbl.BackColor = Color.Transparent;
                    }
                    if (lbl.Tag != null && lbl.Tag.ToString() == "SubText")
                    {
                        lbl.ForeColor = SubText;
                    }
                    else if (lbl.Tag != null && lbl.Tag.ToString() == "Accent")
                    {
                        lbl.ForeColor = AccentColor;
                    }
                    else
                    {
                        lbl.ForeColor = Text;
                    }
                }
                else if (c is CheckBox chk)
                {
                    chk.FlatStyle = FlatStyle.Flat;
                    chk.FlatAppearance.BorderSize = 0;
                    chk.BackColor = Color.Transparent;
                    chk.ForeColor = Text;
                    chk.Paint -= OnCheckBoxPaint;
                    chk.Paint += OnCheckBoxPaint;
                }
                else if (c is RadioButton rb)
                {
                    rb.FlatStyle = FlatStyle.Flat;
                    rb.FlatAppearance.BorderSize = 0;
                    rb.BackColor = Color.Transparent;
                    rb.ForeColor = Text;
                    rb.Paint -= OnRadioButtonPaint;
                    rb.Paint += OnRadioButtonPaint;
                }
                else if (c is TrackBar trb)
                {
                    trb.BackColor = (trb.Parent != null && trb.Parent.BackColor != Color.Transparent) ? trb.Parent.BackColor : Surface;
                }
                else if (c is NumericUpDown nud)
                {
                    nud.BackColor = InputBackground;
                    nud.ForeColor = Text;
                    nud.BorderStyle = BorderStyle.FixedSingle;
                    foreach (Control child in nud.Controls)
                    {
                        child.BackColor = InputBackground;
                        child.ForeColor = Text;
                    }
                }
                else if (c is GroupBox gb)
                {
                    gb.BackColor = (gb.Parent != null && gb.Parent.BackColor != Color.Transparent) ? gb.Parent.BackColor : Surface;
                    gb.ForeColor = Border;
                    gb.Paint -= OnGroupBoxPaint;
                    gb.Paint += OnGroupBoxPaint;
                }
                else if (c is TabControl tc)
                {
                    try
                    {
                        var pi = typeof(Control).GetProperty("DoubleBuffered", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        pi?.SetValue(tc, true, null);
                    }
                    catch { }

                    tc.DrawMode = TabDrawMode.OwnerDrawFixed;
                    tc.ItemSize = new Size(Math.Max(50, tc.ItemSize.Width), 24);
                    tc.BackColor = Surface;
                    tc.ForeColor = Text;
                    try { SetWindowTheme(tc.Handle, "", ""); } catch { }
                    tc.DrawItem -= OnTabControlDrawItem;
                    tc.DrawItem += OnTabControlDrawItem;
                    tc.SelectedIndexChanged -= OnTabControlSelectedIndexChanged;
                    tc.SelectedIndexChanged += OnTabControlSelectedIndexChanged;

                    if (!_tabSubclasses.ContainsKey(tc))
                    {
                        _tabSubclasses[tc] = new DarkTabControlSubclass(tc);
                    }

                    foreach (TabPage tp in tc.TabPages)
                    {
                        tp.UseVisualStyleBackColor = false;
                        tp.BackColor = Surface;
                        tp.ForeColor = Text;
                        ApplyToControls(tp.Controls);
                    }
                }
                else if (c is Panel p)
                {
                    if (p.Tag != null && p.Tag.ToString() == "Header")
                    {
                        p.BackColor = Surface;
                        p.ForeColor = Text;
                    }
                    else if (p.Tag != null && p.Tag.ToString() == "Card")
                    {
                        p.BackColor = Card;
                        p.ForeColor = Text;
                    }
                    else if (p.BackColor == SystemColors.Control || p.BackColor == SystemColors.ControlDark || p.BackColor == Color.FromKnownColor(KnownColor.Control) || p.BackColor == Color.FromKnownColor(KnownColor.ControlDark))
                    {
                        p.BackColor = Surface;
                        p.ForeColor = Text;
                    }
                    else if (p.BackColor == Color.Transparent)
                    {
                        // keep transparent
                    }
                    else
                    {
                        p.BackColor = Background;
                        p.ForeColor = Text;
                    }
                }
                else if (c is SplitContainer sc)
                {
                    sc.BackColor = Border;
                    sc.Panel1.BackColor = Background;
                    sc.Panel2.BackColor = Background;
                }

                if (c.ContextMenuStrip != null)
                {
                    ApplyToContextMenu(c.ContextMenuStrip);
                }

                if (c.HasChildren)
                {
                    ApplyToControls(c.Controls);
                }
            }
        }

        private static void OnGroupBoxPaint(object sender, PaintEventArgs e)
        {
            if (!(sender is GroupBox gb)) return;
            if (string.IsNullOrEmpty(gb.Text)) return;

            Font font = gb.Font;
            Size textSize = TextRenderer.MeasureText(gb.Text, font, Size.Empty, TextFormatFlags.NoPrefix);
            int textX = 10;
            int textY = 0;
            int pad = 4;

            Rectangle coverRect = new Rectangle(textX - pad, textY, textSize.Width + (pad * 2), textSize.Height + 2);
            Color fillBg = (gb.Parent != null && gb.Parent.BackColor != Color.Transparent) ? gb.Parent.BackColor : gb.BackColor;
            using (var brush = new SolidBrush(fillBg))
            {
                e.Graphics.FillRectangle(brush, coverRect);
            }

            TextRenderer.DrawText(e.Graphics, gb.Text, font, new Point(textX, textY), AccentColor, fillBg, TextFormatFlags.NoPrefix);
        }

        private static void OnTabControlDrawItem(object sender, DrawItemEventArgs e)
        {
            if (!(sender is TabControl tc) || e.Index < 0 || e.Index >= tc.TabPages.Count) return;
            TabPage page = tc.TabPages[e.Index];
            bool isSelected = (tc.SelectedIndex == e.Index);
            Graphics g = e.Graphics;
            Rectangle tabRect = tc.GetTabRect(e.Index);

            Color bg = isSelected ? Card : Surface;
            Color fg = isSelected ? AccentColor : SubText;

            using (var brush = new SolidBrush(bg))
            {
                g.FillRectangle(brush, tabRect);
            }

            if (isSelected)
            {
                using (var accentBrush = new SolidBrush(AccentColor))
                {
                    g.FillRectangle(accentBrush, tabRect.X, tabRect.Bottom - 3, tabRect.Width, 3);
                }
            }

            using (var pen = new Pen(Border, 1))
            {
                g.DrawRectangle(pen, tabRect.X, tabRect.Y, tabRect.Width - 1, tabRect.Height - 1);
            }

            using (var font = new Font(tc.Font.FontFamily, tc.Font.Size, isSelected ? FontStyle.Bold : FontStyle.Regular))
            {
                TextRenderer.DrawText(g, page.Text, font, tabRect, fg, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            }
        }

        private static void OnTabControlSelectedIndexChanged(object sender, EventArgs e)
        {
            if (sender is TabControl tc)
            {
                tc.Invalidate();
            }
        }

        private static void OnComboBoxDrawItem(object sender, DrawItemEventArgs e)
        {
            if (!(sender is ComboBox cb) || e.Index < 0 || e.Index >= cb.Items.Count) return;

            bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            Color bg = isSelected ? CardHover : InputBackground;
            Color fg = isSelected ? AccentColor : (cb.Enabled ? Text : SubText);

            using (var brush = new SolidBrush(bg))
            {
                e.Graphics.FillRectangle(brush, e.Bounds);
            }

            string text = cb.GetItemText(cb.Items[e.Index]);
            using (var font = new Font(cb.Font.FontFamily, cb.Font.Size))
            {
                TextRenderer.DrawText(e.Graphics, text, font,
                    new Rectangle(e.Bounds.X + 3, e.Bounds.Y, e.Bounds.Width - 6, e.Bounds.Height),
                    fg, TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.NoPrefix);
            }

            if (isSelected)
            {
                using (var pen = new Pen(AccentColor, 1))
                {
                    e.Graphics.DrawRectangle(pen, e.Bounds.X, e.Bounds.Y, e.Bounds.Width - 1, e.Bounds.Height - 1);
                }
            }
        }

        private static void OnListBoxDrawItem(object sender, DrawItemEventArgs e)
        {
            if (!(sender is ListBox lb) || e.Index < 0 || e.Index >= lb.Items.Count) return;

            bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            Color bg = isSelected ? CardHover : InputBackground;
            Color fg = isSelected ? AccentColor : (lb.Enabled ? Text : SubText);

            using (var brush = new SolidBrush(bg))
            {
                e.Graphics.FillRectangle(brush, e.Bounds);
            }

            string text = lb.GetItemText(lb.Items[e.Index]);
            using (var font = new Font(lb.Font.FontFamily, lb.Font.Size))
            {
                TextRenderer.DrawText(e.Graphics, text, font,
                    new Rectangle(e.Bounds.X + 3, e.Bounds.Y, e.Bounds.Width - 6, e.Bounds.Height),
                    fg, TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.NoPrefix);
            }

            if (isSelected)
            {
                using (var pen = new Pen(AccentColor, 1))
                {
                    e.Graphics.DrawRectangle(pen, e.Bounds.X, e.Bounds.Y, e.Bounds.Width - 1, e.Bounds.Height - 1);
                }
            }
        }

        private static void OnCheckBoxPaint(object sender, PaintEventArgs e)
        {
            if (!(sender is CheckBox chk)) return;

            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Color parentBg = (chk.Parent != null && chk.Parent.BackColor != Color.Transparent)
                ? chk.Parent.BackColor
                : Surface;

            using (var bgBrush = new SolidBrush(parentBg))
            {
                g.FillRectangle(bgBrush, chk.ClientRectangle);
            }

            int boxSize = 13;
            int boxY = (chk.ClientSize.Height - boxSize) / 2;
            int boxX = 1;
            if (chk.CheckAlign == ContentAlignment.MiddleRight || chk.CheckAlign == ContentAlignment.TopRight || chk.CheckAlign == ContentAlignment.BottomRight)
            {
                boxX = chk.ClientSize.Width - boxSize - 2;
            }

            Rectangle boxRect = new Rectangle(boxX, boxY, boxSize, boxSize);

            if (chk.Checked)
            {
                using (var brush = new SolidBrush(AccentColor))
                {
                    g.FillRectangle(brush, boxRect);
                }
                using (var pen = new Pen(AccentColor, 1))
                {
                    g.DrawRectangle(pen, boxRect);
                }
                using (var checkPen = new Pen(Color.White, 1.8f))
                {
                    checkPen.StartCap = LineCap.Round;
                    checkPen.EndCap = LineCap.Round;
                    g.DrawLine(checkPen, boxRect.X + 2, boxRect.Y + 6, boxRect.X + 5, boxRect.Y + 9);
                    g.DrawLine(checkPen, boxRect.X + 5, boxRect.Y + 9, boxRect.X + 10, boxRect.Y + 3);
                }
            }
            else
            {
                using (var brush = new SolidBrush(InputBackground))
                {
                    g.FillRectangle(brush, boxRect);
                }
                using (var pen = new Pen(Border, 1))
                {
                    g.DrawRectangle(pen, boxRect);
                }
            }

            int textX = boxRect.Right + 5;
            int textW = chk.ClientSize.Width - textX;
            if (textW > 0 && !string.IsNullOrEmpty(chk.Text))
            {
                Rectangle textRect = new Rectangle(textX, 0, textW, chk.ClientSize.Height);
                Color fg = chk.Enabled ? Text : SubText;
                TextRenderer.DrawText(g, chk.Text, chk.Font, textRect, fg,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.SingleLine);
            }
        }

        private static void OnRadioButtonPaint(object sender, PaintEventArgs e)
        {
            if (!(sender is RadioButton rb)) return;

            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Color parentBg = (rb.Parent != null && rb.Parent.BackColor != Color.Transparent)
                ? rb.Parent.BackColor
                : Surface;

            using (var bgBrush = new SolidBrush(parentBg))
            {
                g.FillRectangle(bgBrush, rb.ClientRectangle);
            }

            int circleSize = 13;
            int circleY = (rb.ClientSize.Height - circleSize) / 2;
            int circleX = 1;
            if (rb.CheckAlign == ContentAlignment.MiddleRight || rb.CheckAlign == ContentAlignment.TopRight || rb.CheckAlign == ContentAlignment.BottomRight)
            {
                circleX = rb.ClientSize.Width - circleSize - 2;
            }

            Rectangle circleRect = new Rectangle(circleX, circleY, circleSize, circleSize);

            using (var brush = new SolidBrush(InputBackground))
            {
                g.FillEllipse(brush, circleRect);
            }

            using (var pen = new Pen(rb.Checked ? AccentColor : Border, 1))
            {
                g.DrawEllipse(pen, circleRect);
            }

            if (rb.Checked)
            {
                int innerPad = 3;
                Rectangle innerRect = new Rectangle(circleRect.X + innerPad, circleRect.Y + innerPad, circleRect.Width - (innerPad * 2), circleRect.Height - (innerPad * 2));
                using (var brush = new SolidBrush(AccentColor))
                {
                    g.FillEllipse(brush, innerRect);
                }
            }

            int textX = circleRect.Right + 5;
            int textW = rb.ClientSize.Width - textX;
            if (textW > 0 && !string.IsNullOrEmpty(rb.Text))
            {
                Rectangle textRect = new Rectangle(textX, 0, textW, rb.ClientSize.Height);
                Color fg = rb.Enabled ? Text : SubText;
                TextRenderer.DrawText(g, rb.Text, rb.Font, textRect, fg,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.SingleLine);
            }
        }

        public static void ApplyToContextMenu(ContextMenuStrip cms)
        {
            if (cms == null) return;
            cms.Renderer = new SRToolStripRenderer();
            cms.BackColor = Surface;
            cms.ForeColor = Text;
            ApplyToToolStripItems(cms.Items);
        }

        public static void ApplyToToolStripItems(ToolStripItemCollection items)
        {
            if (items == null) return;
            foreach (ToolStripItem item in items)
            {
                if (item == null) continue;
                item.ForeColor = Text;
                item.BackColor = Surface;
                if (item is ToolStripDropDownItem dd && dd.HasDropDownItems)
                {
                    if (dd.DropDown != null)
                    {
                        dd.DropDown.Renderer = new SRToolStripRenderer();
                        dd.DropDown.BackColor = Surface;
                        dd.DropDown.ForeColor = Text;
                    }
                    ApplyToToolStripItems(dd.DropDownItems);
                }
            }
        }

        public static SRTopNavbar EnsureTopNavbar(Form form, string subtitle = null, Action onRefresh = null, Action openSettingsAction = null)
        {
            if (form == null || form.IsDisposed) return null;

            string typeName = form.GetType().Name;
            if (typeName == "MenuForm" || form is SRSettingsForm)
            {
                return null;
            }

            if (form.FormBorderStyle == FormBorderStyle.FixedDialog ||
                form.FormBorderStyle == FormBorderStyle.FixedToolWindow ||
                form.FormBorderStyle == FormBorderStyle.SizableToolWindow ||
                (!form.MaximizeBox && !form.MinimizeBox && form.Width < 500 && form.Height < 400))
            {
                return null;
            }

            foreach (Control c in form.Controls)
            {
                if (c is SRTopNavbar existing)
                {
                    existing.ApplyThemeState();
                    return existing;
                }
            }

            var navbar = new SRTopNavbar(form, subtitle, onRefresh, openSettingsAction);
            form.Controls.Add(navbar);
            navbar.SendToBack(); // In WinForms docking, SendToBack ensures Top dock at Y=0 above existing controls

            int navHeight = navbar.Height;
            foreach (Control c in form.Controls)
            {
                if (c == navbar || c is StatusStrip) continue;

                // Adjust floating controls anchored to Top
                if (c.Dock == DockStyle.None && c.Top < navHeight)
                {
                    int oldTop = c.Top;
                    c.Top = navHeight + (oldTop > 0 ? oldTop : 4);
                    if ((c.Anchor & AnchorStyles.Bottom) == AnchorStyles.Bottom)
                    {
                        c.Height = Math.Max(50, c.Height - (c.Top - oldTop));
                    }
                }
            }

            return navbar;
        }

        public static bool HasTopNavbar(Form form)
        {
            if (form == null) return false;
            foreach (Control c in form.Controls)
            {
                if (c is SRTopNavbar) return true;
            }
            return false;
        }

        public static void RegisterForm(Form form, ToolStrip navToolStrip = null, Action customUpdate = null)
        {
            if (form == null) return;

            try
            {
                var icon = SRLogo.GetAppIcon();
                if (icon != null) form.Icon = icon;
            }
            catch { }

            var topNavbar = EnsureTopNavbar(form);

            // Backward-compatible toolstrip injection only if top navbar wasn't created and a navToolStrip was supplied
            if (topNavbar == null && navToolStrip != null)
            {
                InjectNavbarControls(navToolStrip, form);
            }

            ApplyTheme(form);
            customUpdate?.Invoke();

            Action syncHandler = () =>
            {
                if (form.IsDisposed) return;
                if (form.InvokeRequired)
                {
                    try
                    {
                        form.BeginInvoke(new Action(() =>
                        {
                            if (!form.IsDisposed)
                            {
                                ApplyTheme(form);
                                customUpdate?.Invoke();
                            }
                        }));
                    }
                    catch { }
                }
                else
                {
                    ApplyTheme(form);
                    customUpdate?.Invoke();
                }
            };

            ThemeChanged += syncHandler;
            form.FormClosed += (s, e) => { ThemeChanged -= syncHandler; };
        }

        private static ToolStrip FindPrimaryToolStrip(Control parent)
        {
            if (parent == null) return null;

            foreach (Control c in parent.Controls)
            {
                if (c is ToolStrip ts && !(c is StatusStrip) && (ts.Dock == DockStyle.Top || c is MenuStrip))
                {
                    return ts;
                }
            }

            foreach (Control c in parent.Controls)
            {
                if (c is ToolStrip ts && !(c is StatusStrip))
                {
                    return ts;
                }
            }

            foreach (Control c in parent.Controls)
            {
                if (c is Panel || c is ToolStripContainer || c is SplitContainer || c is GroupBox)
                {
                    var found = FindPrimaryToolStrip(c);
                    if (found != null) return found;
                }
            }

            return null;
        }

        public static void InjectNavbarControls(ToolStrip toolStrip, Form parentForm, Action openSettingsAction = null)
        {
            if (toolStrip == null) return;
            try { toolStrip.CanOverflow = true; } catch { }

            for (int i = toolStrip.Items.Count - 1; i >= 0; i--)
            {
                var tag = toolStrip.Items[i].Tag as string;
                if (tag == "SR_NAVBAR_ITEM")
                {
                    toolStrip.Items.RemoveAt(i);
                }
            }

            // --- LEFT: SR Ribbon Logo & Brand ---
            Image logoImg = null;
            try { logoImg = SRLogo.GetLogoImage(18); } catch { }

            var logoItem = new ToolStripLabel
            {
                Image = logoImg,
                DisplayStyle = ToolStripItemDisplayStyle.Image,
                Tag = "SR_NAVBAR_ITEM",
                ToolTipText = "SR File - Grand Theft Auto V Suite",
                Margin = new Padding(3, 1, 2, 1)
            };

            var titleItem = new ToolStripLabel
            {
                Text = "SR FILE",
                ForeColor = AccentColor,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Tag = "SR_NAVBAR_ITEM",
                Margin = new Padding(0, 1, 4, 1),
                ToolTipText = "SR File"
            };

            var leftSep = new ToolStripSeparator
            {
                Tag = "SR_NAVBAR_ITEM",
                Margin = new Padding(2, 0, 4, 0)
            };

            toolStrip.Items.Insert(0, leftSep);
            toolStrip.Items.Insert(0, titleItem);
            toolStrip.Items.Insert(0, logoItem);

            // --- RIGHT: Settings, Separator, Refresh, Separator, DarkMode ---
            // WinForms Alignment=Right stacks from right edge inward in addition order:
            // 1. Settings button (farthest right)
            var settingsBtn = new ToolStripButton
            {
                Text = "⚙ Réglages",
                ToolTipText = "Ouvrir les réglages et personnalisation de SR File",
                DisplayStyle = ToolStripItemDisplayStyle.ImageAndText,
                Tag = "SR_NAVBAR_ITEM",
                Alignment = ToolStripItemAlignment.Right,
                Font = new Font(toolStrip.Font, FontStyle.Bold),
                ForeColor = Text
            };
            try
            {
                Image logo = SRLogo.GetLogoImage(16);
                if (logo != null) settingsBtn.Image = logo;
            }
            catch { }
            settingsBtn.Click += (s, e) =>
            {
                if (openSettingsAction != null)
                {
                    openSettingsAction();
                }
                else
                {
                    using (var dlg = new SRSettingsForm())
                    {
                        dlg.ShowDialog(parentForm);
                    }
                }
            };
            toolStrip.Items.Add(settingsBtn);

            // 2. Separator between Settings and Refresh
            var sepSettings = new ToolStripSeparator
            {
                Tag = "SR_NAVBAR_ITEM",
                Alignment = ToolStripItemAlignment.Right,
                Margin = new Padding(2, 0, 2, 0)
            };
            toolStrip.Items.Add(sepSettings);

            // 3. Refresh button
            var refreshBtn = new ToolStripButton
            {
                Text = "🔄 Actualiser",
                ToolTipText = "Actualiser l'affichage et recharger les données",
                DisplayStyle = ToolStripItemDisplayStyle.Text,
                Tag = "SR_NAVBAR_ITEM",
                Alignment = ToolStripItemAlignment.Right,
                Font = new Font(toolStrip.Font, FontStyle.Bold),
                ForeColor = Text
            };
            refreshBtn.Click += (s, e) =>
            {
                try
                {
                    var refreshProp = parentForm?.GetType().GetField("RefreshButton", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (refreshProp?.GetValue(parentForm) is ToolStripButton tb)
                    {
                        tb.PerformClick();
                    }
                    else if (refreshProp?.GetValue(parentForm) is Button b)
                    {
                        b.PerformClick();
                    }

                    var m = parentForm?.GetType().GetMethod("RefreshView", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                         ?? parentForm?.GetType().GetMethod("Reload", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                         ?? parentForm?.GetType().GetMethod("RefreshData", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    m?.Invoke(parentForm, null);

                    ApplyTheme(parentForm);
                    parentForm?.Invalidate(true);
                    parentForm?.Update();
                    parentForm?.Refresh();
                }
                catch { }
            };
            toolStrip.Items.Add(refreshBtn);

            // 4. Separator between Refresh and Theme
            var sepRefresh = new ToolStripSeparator
            {
                Tag = "SR_NAVBAR_ITEM",
                Alignment = ToolStripItemAlignment.Right,
                Margin = new Padding(2, 0, 2, 0)
            };
            toolStrip.Items.Add(sepRefresh);

            // 5. Dark Mode button
            var themeBtn = new ToolStripButton
            {
                Text = IsDarkMode ? "🌙 Sombre" : "☀️ Clair",
                ToolTipText = "Basculer entre Mode Sombre et Mode Clair",
                DisplayStyle = ToolStripItemDisplayStyle.Text,
                Tag = "SR_NAVBAR_ITEM",
                Alignment = ToolStripItemAlignment.Right,
                Font = new Font(toolStrip.Font, FontStyle.Bold),
                ForeColor = Text
            };
            themeBtn.Click += (s, e) =>
            {
                ToggleDarkMode();
            };
            toolStrip.Items.Add(themeBtn);

            Action updateHandler = () =>
            {
                if (!themeBtn.IsDisposed)
                {
                    themeBtn.Text = IsDarkMode ? "🌙 Sombre" : "☀️ Clair";
                    themeBtn.ForeColor = Text;
                }
                if (!refreshBtn.IsDisposed)
                {
                    refreshBtn.ForeColor = Text;
                }
                if (!settingsBtn.IsDisposed)
                {
                    settingsBtn.ForeColor = Text;
                }
                if (!titleItem.IsDisposed)
                {
                    titleItem.ForeColor = AccentColor;
                }
            };
            ThemeChanged += updateHandler;
            parentForm.FormClosed += (s, e) => { ThemeChanged -= updateHandler; };
        }
    }

    public class SRToolStripRenderer : ToolStripProfessionalRenderer
    {
        public SRToolStripRenderer() : base(new SRColorTable()) { }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            using (var pen = new Pen(SRThemeManager.Border, 1))
            {
                if (e.ToolStrip is StatusStrip)
                {
                    e.Graphics.DrawLine(pen, 0, 0, e.ToolStrip.Width, 0);
                }
                else if (e.ToolStrip is MenuStrip)
                {
                    e.Graphics.DrawLine(pen, 0, e.ToolStrip.Height - 1, e.ToolStrip.Width, e.ToolStrip.Height - 1);
                }
                else if (e.ToolStrip is ToolStripDropDownMenu)
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1);
                }
                else
                {
                    e.Graphics.DrawLine(pen, 0, e.ToolStrip.Height - 1, e.ToolStrip.Width, e.ToolStrip.Height - 1);
                }
            }
        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            if (e.Item.Selected || e.Item.Pressed)
            {
                var rc = new Rectangle(2, 1, e.Item.Width - 4, e.Item.Height - 2);
                using (var brush = new SolidBrush(SRThemeManager.CardHover))
                using (var pen = new Pen(SRThemeManager.AccentColor, 1))
                {
                    e.Graphics.FillRectangle(brush, rc);
                    e.Graphics.DrawRectangle(pen, rc);
                }
            }
        }

        protected override void OnRenderButtonBackground(ToolStripItemRenderEventArgs e)
        {
            var btn = e.Item as ToolStripButton;
            var rc = new Rectangle(1, 1, e.Item.Width - 2, e.Item.Height - 2);

            if (btn != null && btn.Checked)
            {
                using (var brush = new SolidBrush(Color.FromArgb(100, SRThemeManager.AccentColor)))
                using (var pen = new Pen(SRThemeManager.AccentColor, 1))
                {
                    e.Graphics.FillRectangle(brush, rc);
                    e.Graphics.DrawRectangle(pen, rc);
                }
            }
            else if (e.Item.Selected || e.Item.Pressed)
            {
                using (var brush = new SolidBrush(SRThemeManager.CardHover))
                using (var pen = new Pen(SRThemeManager.AccentColor, 1))
                {
                    e.Graphics.FillRectangle(brush, rc);
                    e.Graphics.DrawRectangle(pen, rc);
                }
            }
        }

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            using (var pen = new Pen(SRThemeManager.Border, 1))
            {
                if (e.Vertical)
                {
                    int x = e.Item.Width / 2;
                    e.Graphics.DrawLine(pen, x, 3, x, e.Item.Height - 3);
                }
                else
                {
                    int y = e.Item.Height / 2;
                    e.Graphics.DrawLine(pen, 4, y, e.Item.Width - 4, y);
                }
            }
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = (e.Item.Selected || e.Item.Pressed) ? SRThemeManager.Text : (e.Item.Enabled ? SRThemeManager.Text : SRThemeManager.SubText);
            base.OnRenderItemText(e);
        }

        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            using (var brush = new SolidBrush(SRThemeManager.Surface))
            {
                e.Graphics.FillRectangle(brush, e.AffectedBounds);
            }
        }

        protected override void OnRenderImageMargin(ToolStripRenderEventArgs e)
        {
            using (var brush = new SolidBrush(SRThemeManager.Surface))
            {
                e.Graphics.FillRectangle(brush, e.AffectedBounds);
            }
        }
    }

    public class SRColorTable : ProfessionalColorTable
    {
        public override Color ToolStripDropDownBackground => SRThemeManager.Surface;
        public override Color MenuBorder => SRThemeManager.Border;
        public override Color MenuItemBorder => SRThemeManager.AccentColor;
        public override Color MenuItemSelected => SRThemeManager.CardHover;
        public override Color MenuStripGradientBegin => SRThemeManager.Surface;
        public override Color MenuStripGradientEnd => SRThemeManager.Surface;
        public override Color ToolStripGradientBegin => SRThemeManager.Surface;
        public override Color ToolStripGradientMiddle => SRThemeManager.Surface;
        public override Color ToolStripGradientEnd => SRThemeManager.Surface;
        public override Color StatusStripGradientBegin => SRThemeManager.Surface;
        public override Color StatusStripGradientEnd => SRThemeManager.Surface;
        public override Color ImageMarginGradientBegin => SRThemeManager.Surface;
        public override Color ImageMarginGradientMiddle => SRThemeManager.Surface;
        public override Color ImageMarginGradientEnd => SRThemeManager.Surface;
        public override Color SeparatorDark => SRThemeManager.Border;
        public override Color SeparatorLight => Color.Transparent;
        public override Color ButtonSelectedHighlight => SRThemeManager.CardHover;
        public override Color ButtonPressedHighlight => SRThemeManager.Card;
        public override Color ButtonCheckedHighlight => SRThemeManager.Card;
        public override Color ButtonSelectedBorder => SRThemeManager.AccentColor;
        public override Color ButtonPressedBorder => SRThemeManager.AccentColor;
    }

    internal class DarkTabControlSubclass : NativeWindow
    {
        private readonly TabControl _tc;

        public DarkTabControlSubclass(TabControl tc)
        {
            _tc = tc;
            if (tc.IsHandleCreated) AssignHandle(tc.Handle);
            tc.HandleCreated += (s, e) => AssignHandle(tc.Handle);
            tc.HandleDestroyed += (s, e) => ReleaseHandle();
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_ERASEBKGND = 0x0014;
            const int WM_PAINT = 0x000F;

            if (m.Msg == WM_ERASEBKGND)
            {
                if (m.WParam != IntPtr.Zero)
                {
                    try
                    {
                        using (var g = Graphics.FromHdc(m.WParam))
                        using (var b = new SolidBrush(SRThemeManager.Surface))
                        {
                            g.FillRectangle(b, _tc.ClientRectangle);
                        }
                    }
                    catch { }
                }
                m.Result = (IntPtr)1;
                return;
            }

            base.WndProc(ref m);

            if (m.Msg == WM_PAINT)
            {
                try
                {
                    using (var g = Graphics.FromHwnd(_tc.Handle))
                    {
                        int maxRight = 0;
                        int headerHeight = 0;
                        for (int i = 0; i < _tc.TabCount; i++)
                        {
                            Rectangle r = _tc.GetTabRect(i);
                            if (r.Right > maxRight) maxRight = r.Right;
                            if (r.Bottom > headerHeight) headerHeight = r.Bottom;
                        }

                        if (_tc.TabCount == 0) headerHeight = 24;

                        if (maxRight < _tc.Width)
                        {
                            Rectangle emptyArea = new Rectangle(maxRight, 0, _tc.Width - maxRight, headerHeight + 2);
                            using (var b = new SolidBrush(SRThemeManager.Surface))
                            {
                                g.FillRectangle(b, emptyArea);
                            }
                        }

                        using (var pen = new Pen(SRThemeManager.Border, 1))
                        {
                            g.DrawLine(pen, 0, headerHeight + 1, _tc.Width, headerHeight + 1);
                            Rectangle disp = _tc.DisplayRectangle;
                            g.DrawRectangle(pen, disp.X - 1, disp.Y - 1, disp.Width + 1, disp.Height + 1);
                            g.DrawRectangle(pen, 0, 0, _tc.Width - 1, _tc.Height - 1);
                        }
                    }
                }
                catch { }
            }
        }
    }

    internal class DarkComboBoxSubclass : NativeWindow
    {
        private readonly ComboBox _cb;

        public DarkComboBoxSubclass(ComboBox cb)
        {
            _cb = cb;
            if (cb.IsHandleCreated) AssignHandle(cb.Handle);
            cb.HandleCreated += (s, e) => AssignHandle(cb.Handle);
            cb.HandleDestroyed += (s, e) => ReleaseHandle();
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_PAINT = 0x000F;
            base.WndProc(ref m);

            if (m.Msg == WM_PAINT)
            {
                try
                {
                    using (var g = Graphics.FromHwnd(_cb.Handle))
                    {
                        g.SmoothingMode = SmoothingMode.AntiAlias;

                        using (var pen = new Pen(SRThemeManager.Border, 1))
                        {
                            g.DrawRectangle(pen, 0, 0, _cb.Width - 1, _cb.Height - 1);
                        }

                        int btnW = 18;
                        Rectangle btnRect = new Rectangle(_cb.Width - btnW, 1, btnW - 1, _cb.Height - 2);
                        Color btnBg = _cb.Enabled ? SRThemeManager.Surface : SRThemeManager.InputBackground;
                        using (var b = new SolidBrush(btnBg))
                        {
                            g.FillRectangle(b, btnRect);
                        }
                        using (var pen = new Pen(SRThemeManager.Border, 1))
                        {
                            g.DrawLine(pen, btnRect.Left, 0, btnRect.Left, _cb.Height);
                        }

                        int midX = btnRect.Left + btnRect.Width / 2;
                        int midY = btnRect.Top + btnRect.Height / 2;
                        Color arrowColor = _cb.Enabled ? SRThemeManager.SubText : Color.FromArgb(80, 95, 115);
                        using (var arrowPen = new Pen(arrowColor, 1.5f))
                        {
                            arrowPen.StartCap = LineCap.Round;
                            arrowPen.EndCap = LineCap.Round;
                            g.DrawLine(arrowPen, midX - 3, midY - 2, midX, midY + 1);
                            g.DrawLine(arrowPen, midX, midY + 1, midX + 3, midY - 2);
                        }
                    }
                }
                catch { }
            }
        }
    }
}