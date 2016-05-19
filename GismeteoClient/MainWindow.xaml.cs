using System;
using System.Windows;
using System.Windows.Controls;
using NLog;
using Weather;
using System.Collections.Generic;

namespace GismeteoClient
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            Application.Current.MainWindow.Hide();
            InitializeComponent();
            _splashScreen.Close(TimeSpan.FromSeconds(0.5));
            Application.Current.MainWindow.Show();
            CurrentWindowState = Application.Current.MainWindow.WindowState;
            _logger.Trace("MainWnd is initialized");
            RefreshWeatherLoop();
        }

        #region Обработчики событий
        /// <summary>
        /// Перегрузка обработчика события инициализации приложения
        /// </summary>
        /// <param name="e"></param>
        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            MessageAdded += messageAdded_refresh;
            RefreshRequired += onRefreshRequired_RefreshInfo;

            _logger.Info("Application started");
            TrayIcon = Settings.TrayIconConfig();
            TrayMenu = Resources["TrayMenu"] as ContextMenu;
            ViewWeatherInfoNow(Properties.Settings.Default.LastForecastNow);
            for (int i = 0; i < 4; i++)
            {
                ViewWeatherInfoDayPart(Properties.Settings.Default["LastForecast" + (DayPart)i] as WeatherItem, (DayPart)i);
            }
        }
        /// <summary>
        /// Перегружает обработчик события сворачивания главного окна
        /// </summary>
        /// <param name="e"></param>
        protected override void OnStateChanged(EventArgs e)
        {
            if (WindowState != WindowState.Minimized)
                CurrentWindowState = WindowState;
            base.OnStateChanged(e);
            _logger.Trace("MainWnd state changed to '{0}'",
                !Properties.Settings.Default.USCanClose && WindowState == WindowState.Minimized ? WindowState.ToString() + " (Tray)" : WindowState.ToString());
            if (!Properties.Settings.Default.USCanClose && WindowState == WindowState.Minimized)
            {
                Hide();
                (TrayMenu.Items[0] as MenuItem).Header = "Показать";
            }
            else
                (TrayMenu.Items[0] as MenuItem).Header = "Свернуть";
        }

        /// <summary>
        /// Перегружает обработчик события закрытия главного окна
        /// </summary>
        /// <param name="e"></param>
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (!Properties.Settings.Default.USCanClose)
            {
                e.Cancel = true;
                WindowState = WindowState.Minimized;
            }
            else
            {
                base.OnClosing(e);
                _logger.Trace("Application is closing");
                if(mainRefreshTaskThread != null)
                    mainRefreshTaskThread.Abort();
                Service.Disconnect();
                TrayIcon.Visible = false;
                TrayIcon.Dispose();
                if(logWnd != null)
                    logWnd.Close();
                _logger.Info("Application closed");
            }
        }

        /// <summary>
        /// Запись в лог ошибки в случае крэша приложения
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            _logger.Fatal("Unhandled exception: {0}", e.ExceptionObject);
            LogManager.Flush();
        }

        /// <summary>
        /// Обработчик события RefreshRequired
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void onRefreshRequired_RefreshInfo(object sender, EventArgs e)
        {
            _logger.Debug("MainWnd refreshing started");
            RefreshMainWndData();
        }
        #endregion
    }
}
