using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Reflection;

namespace CodeWalker.WinForms
{
    public static class SRLogo
    {
        private static Image _cachedLogo = null;
        private static Icon _cachedIcon = null;

        public static Image GetLogoImage(int targetSize = 0)
        {
            if (_cachedLogo == null)
            {
                try
                {
                    string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                    string[] possiblePaths = new string[]
                    {
                        Path.Combine(baseDir, "sr_logo.png"),
                        Path.Combine(baseDir, "Resources", "sr_logo.png"),
                        @"C:\Users\dylan\Downloads\sr_app_icon_ribbon_shape_17889406741433.png",
                        Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "sr_logo.png")
                    };

                    foreach (string path in possiblePaths)
                    {
                        if (File.Exists(path))
                        {
                            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                            {
                                using (var img = Image.FromStream(fs))
                                {
                                    _cachedLogo = new Bitmap(img);
                                    break;
                                }
                            }
                        }
                    }
                }
                catch { }
            }

            if (_cachedLogo == null)
            {
                _cachedLogo = new Bitmap(32, 32);
                using (var g = Graphics.FromImage(_cachedLogo))
                {
                    g.Clear(Color.FromArgb(225, 29, 72));
                }
            }

            if (targetSize <= 0 || (_cachedLogo.Width == targetSize && _cachedLogo.Height == targetSize))
            {
                return _cachedLogo;
            }

            var resized = new Bitmap(targetSize, targetSize);
            using (var g = Graphics.FromImage(resized))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.DrawImage(_cachedLogo, 0, 0, targetSize, targetSize);
            }
            return resized;
        }

        public static Icon GetAppIcon()
        {
            if (_cachedIcon == null)
            {
                try
                {
                    string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                    string[] possiblePaths = new string[]
                    {
                        Path.Combine(baseDir, "CW.ico"),
                        Path.Combine(baseDir, "sr_app_icon.ico"),
                        @"C:\Users\dylan\.gemini\antigravity\scratch\SR_File\sr_app_icon.ico"
                    };
                    foreach (string path in possiblePaths)
                    {
                        if (File.Exists(path))
                        {
                            _cachedIcon = new Icon(path);
                            break;
                        }
                    }
                }
                catch { }
            }
            return _cachedIcon;
        }

        private static Image _cachedBadge = null;
        private static int _cachedBadgeSize = 0;

        public static Image GetLogoBadgeImage(int badgeSize = 28, int iconSize = 20)
        {
            if (_cachedBadge != null && _cachedBadgeSize == badgeSize)
            {
                return _cachedBadge;
            }

            var bmp = new Bitmap(badgeSize, badgeSize, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.Clear(Color.Transparent);

                int cornerRadius = Math.Max(4, badgeSize / 4);
                Rectangle rect = new Rectangle(0, 0, badgeSize - 1, badgeSize - 1);
                using (var path = CreateRoundedRectanglePath(rect, cornerRadius))
                {
                    using (var bgBrush = new SolidBrush(Color.White))
                    {
                        g.FillPath(bgBrush, path);
                    }
                    using (var borderPen = new Pen(Color.FromArgb(45, 0, 0, 0), 1f))
                    {
                        g.DrawPath(borderPen, path);
                    }
                }

                Image logo = GetLogoImage(iconSize);
                if (logo != null)
                {
                    int x = (badgeSize - iconSize) / 2;
                    int y = (badgeSize - iconSize) / 2;
                    g.DrawImage(logo, x, y, iconSize, iconSize);
                }
            }

            _cachedBadge = bmp;
            _cachedBadgeSize = badgeSize;
            return bmp;
        }

        private static GraphicsPath CreateRoundedRectanglePath(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int diameter = radius * 2;
            Rectangle arcRect = new Rectangle(rect.Location, new Size(diameter, diameter));

            path.AddArc(arcRect, 180, 90);
            arcRect.X = rect.Right - diameter;
            path.AddArc(arcRect, 270, 90);
            arcRect.Y = rect.Bottom - diameter;
            path.AddArc(arcRect, 0, 90);
            arcRect.X = rect.Left;
            path.AddArc(arcRect, 90, 90);

            path.CloseFigure();
            return path;
        }
    }
}
