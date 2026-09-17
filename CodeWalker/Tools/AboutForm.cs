using CodeWalker.WinForms;
using System;
using System.Windows.Forms;

namespace CodeWalker.Tools
{
    public partial class AboutForm : Form
    {
        public AboutForm()
        {
            InitializeComponent();
            SRThemeManager.RegisterForm(this);
            SetupLogo();
            SRThemeManager.ApplyTheme(this);
        }

        private void SetupLogo()
        {
            try
            {
                var icon = SRLogo.GetAppIcon();
                if (icon != null) this.Icon = icon;
                LogoPic.Image = SRLogo.GetLogoImage(64);
            }
            catch { }
        }

        private void OkButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}