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
using System.Windows.Forms;

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

            FileTarget target = new FileTarget();
            config.AddTarget("full_file", target);

            target.DeleteOldFileOnStartup = true; // Удалатья страый файл при запуске
            target.KeepFileOpen = false; //Не держать файл открытым
            target.ConcurrentWrites = true;
            target.Encoding = Encoding.GetEncoding(1251); // Кодировка файла логов
            target.ArchiveEvery = FileArchivePeriod.Day; // Период архивирования старых логов (нет смысла, если старый удаляется при запуске)
            target.Layout = NLog.Layouts.Layout.FromString("${uppercase:${level}} | ${longdate} :\t${message}"); // Структура сообщения
            target.FileName = NLog.Layouts.Layout.FromString("${basedir}/logs/${shortdate}.full.log"); //Структура названия файлов
            target.ArchiveFileName = NLog.Layouts.Layout.FromString("${basedir}/logs/archives/{shortdate}full.rar"); // Структура названия архивов

            LoggingRule ruleFile = new LoggingRule("*", LogLevel.Trace, target); // Минимальный уровень логгирования - Trace
            config.LoggingRules.Add(ruleFile);

            target = new FileTarget();
            config.AddTarget("debug_file", target);

            target.DeleteOldFileOnStartup = true;
            target.KeepFileOpen = false;
            target.ConcurrentWrites = true;
            target.Encoding = Encoding.GetEncoding(1251);
            target.ArchiveEvery = FileArchivePeriod.Day;
            target.Layout = NLog.Layouts.Layout.FromString("${uppercase:${level}} | ${longdate} :\t${message}"); // Структура сообщения
            target.FileName = NLog.Layouts.Layout.FromString("${basedir}/logs/${shortdate}.debug.log"); //Структура названия файлов
            target.ArchiveFileName = NLog.Layouts.Layout.FromString("${basedir}/logs/archives/{shortdate}.debug.rar"); // Структура названия архивов

            ruleFile = new LoggingRule("*", LogLevel.Debug, target);
            config.LoggingRules.Add(ruleFile);

            LogManager.Configuration = config;
        }

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
