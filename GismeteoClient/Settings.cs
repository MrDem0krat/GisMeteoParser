using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;
using NLog.Config;
using NLog.Targets;
using System.Windows;
using System.Windows.Controls;

namespace GismeteoClient
{
    public static class Settings
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Настраивает параметры ведения лога приложения
        /// </summary>
        public static void LoggerConfig()
        {
            LoggingConfiguration config = new LoggingConfiguration();

            FileTarget fileTarget = new FileTarget();
            config.AddTarget("file", fileTarget);

            fileTarget.DeleteOldFileOnStartup = true; // Удалатья страый файл при запуске
            fileTarget.KeepFileOpen = false; //Не держать файл открытым
            fileTarget.ConcurrentWrites = true;
            fileTarget.Encoding = Encoding.GetEncoding(1251); // Кодировка файла логов
            fileTarget.ArchiveEvery = FileArchivePeriod.Day; // Период архивирования старых логов (нет смысла, если старый удаляется при запуске)
            fileTarget.Layout = NLog.Layouts.Layout.FromString("${uppercase:${level}} | ${longdate} :\t${message}"); // Структура сообщения
            fileTarget.FileName = NLog.Layouts.Layout.FromString("${basedir}/logs/${shortdate}.log"); //Структура названия файлов
            fileTarget.ArchiveFileName = NLog.Layouts.Layout.FromString("${basedir}/logs/archives/{shortdate}.rar"); // Структура названия архивов

            LoggingRule ruleFile = new LoggingRule("*", LogLevel.Trace, fileTarget); // Минимальный уровень логгирования - Trace
            config.LoggingRules.Add(ruleFile);

            LogManager.Configuration = config;
        }

        /// <summary>
        /// Настраивает параметры и поведение иконки в трее
        /// </summary>
        /// <returns></returns>
        public static System.Windows.Forms.NotifyIcon TrayIconConfig()
        {
            System.Windows.Forms.NotifyIcon TrayIcon = new System.Windows.Forms.NotifyIcon();
            TrayIcon.Icon = Properties.Resources.MainWindowIcon;
            TrayIcon.Text = "Gismeteo.Погода";
            TrayIcon.Click += delegate (object sender, EventArgs e)
            {
                if ((e as System.Windows.Forms.MouseEventArgs).Button == System.Windows.Forms.MouseButtons.Left)
                {
                    MainWindow.ShowHideMainWindow();
                }
                else
                {
                    MainWindow.TrayMenu.IsOpen = true;
                }
            };
            TrayIcon.Visible = true;
            return TrayIcon;
        }
    }
}
