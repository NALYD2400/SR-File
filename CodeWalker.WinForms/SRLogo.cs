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
    }
}
