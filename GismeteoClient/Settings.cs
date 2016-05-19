using NLog;
using System;
using System.Windows.Forms;

namespace GismeteoClient
{
    public static class Settings
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Настраивает параметры и поведение иконки в трее
        /// </summary>
        /// <returns></returns>
        public static NotifyIcon TrayIconConfig()
        {
            NotifyIcon TrayIcon = new NotifyIcon();
            TrayIcon.Icon = Properties.Resources.MainWindowIcon;
            TrayIcon.Text = "Gismeteo.Погода";
            TrayIcon.Click += delegate (object sender, EventArgs e)
            {
                if ((e as MouseEventArgs).Button == MouseButtons.Left)
                {
                    _logger.Trace("TrayIcon: clicked LMB");
                    MainWindow.ShowHideMainWindow();
                }
                if ((e as MouseEventArgs).Button == MouseButtons.Right)
                {
                    _logger.Trace("TrayIcon: clicked RMB");
                    MainWindow.TrayMenu.IsOpen = true;
                }
            };
            TrayIcon.Visible = true;
            _logger.Trace("TrayIcon: configured");
            return TrayIcon;
        }
    }
}
