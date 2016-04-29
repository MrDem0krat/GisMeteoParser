using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using NLog;

namespace GismeteoClient
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static Logger _logger = LogManager.GetCurrentClassLogger();
        public static System.Windows.Forms.NotifyIcon TrayIcon = new System.Windows.Forms.NotifyIcon();
        public static ContextMenu TrayMenu = new ContextMenu();
        public static SplashScreen _splashScreen = new SplashScreen("Resources/graphics/SplashScreen.png");
        private SettingsWnd settingsWnd;
        private static WindowState CurrentWindowState { get; set; }
        public static WeatherService.WServiceClient client = new WeatherService.WServiceClient("NetTcpBinding_IWService");


        public MainWindow()
        {
            Application.Current.MainWindow.Hide();
            InitializeComponent();
            TrayIcon = Settings.TrayIconConfig();
            TrayMenu = Resources["TrayMenu"] as ContextMenu;
            GridWeatherWeek.Visibility = Visibility.Hidden;
            MainWndInterface.MainWindowReference = this;
            _logger.Debug("Настройки успешно загружены");
            //refresh
            _splashScreen.Close(TimeSpan.FromSeconds(0.5));
            Application.Current.MainWindow.Show();
        }
        #region Обработчики событий

        /// <summary>
        /// Перегружает обработчик события сворачивания главного окна
        /// </summary>
        /// <param name="e"></param>
        protected override void OnStateChanged(EventArgs e)
        {
            base.OnStateChanged(e);
            if (WindowState == WindowState.Minimized)
            {
                Hide();
                (TrayMenu.Items[0] as MenuItem).Header = "Показать";
            }
            else
            {
                CurrentWindowState = WindowState;
            }
        }

        /// <summary>
        /// Перегружает обработчик события закрытия главного окна
        /// </summary>
        /// <param name="e"></param>
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (!Properties.Settings.Default.USCanClose)
            {
                CurrentWindowState = WindowState;
                (TrayMenu.Items[0] as MenuItem).Header = "Показать";
                Hide();
            }
            else
            {
                base.OnClosing(e);
                client.Close();
                Properties.Settings.Default.Save();
                TrayIcon.Visible = false;
                TrayIcon.Dispose();
                _logger.Info("Приложение закрыто");
            }
        }

        #endregion

        #region Обработчики кнопок
        /// <summary>
        /// Обработчик нажатия кнопки обновить
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button_load_Click(object sender, RoutedEventArgs e)
        {
            //Task.Factory.StartNew(() => RefreshAsync());
        }

        /// <summary>
        /// Обработчик нажатия кнопки Настройки
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button_settings_Click(object sender, RoutedEventArgs e)//как-то слишком замудрёно
        {
            settingsWnd = new SettingsWnd();
            if (settingsWnd.ShowDialog() == true)
            {
                if(!Properties.Settings.Default.isDbDataSaved)
                {
                    if (settingsWnd.Password.Password != "    ")
                    {
                        Properties.Settings.Default.USdbLogin = settingsWnd.Login.Text;
                        Properties.Settings.Default.USdbServer = settingsWnd.Server.Text;
                        Properties.Settings.Default.USdbPort = uint.Parse(settingsWnd.Port.Text);
                        client.SetAuthData(
                            Properties.Settings.Default.USdbServer,
                            Properties.Settings.Default.USdbPort,
                            Properties.Settings.Default.USdbLogin,
                            settingsWnd.Password.Password);
                        client.SaveSettings();
                        Properties.Settings.Default.isDbDataSaved = true;
                    }
                    Properties.Settings.Default.USCityName = settingsWnd.CityName.SelectedItem != null ? settingsWnd.CityName.SelectedItem.ToString() : string.Empty;
                    Properties.Settings.Default.USCityID = 0;
                    Properties.Settings.Default.USRefreshPeriod = settingsWnd.RefreshPeriod.SelectedIndex;
                    Properties.Settings.Default.USCanClose = !settingsWnd.isMinimazeToTray.IsChecked.Value;
                }
                else
                {
                    Properties.Settings.Default.USCityName = settingsWnd.CityName.SelectedItem != null ? settingsWnd.CityName.SelectedItem.ToString() : string.Empty;
                    Properties.Settings.Default.USCityID = client.GetCityId(settingsWnd.CityName.SelectedItem != null ? settingsWnd.CityName.SelectedItem.ToString() : string.Empty);
                    Properties.Settings.Default.USRefreshPeriod = settingsWnd.RefreshPeriod.SelectedIndex;
                    Properties.Settings.Default.USCanClose = !settingsWnd.isMinimazeToTray.IsChecked.Value;
                }
                Properties.Settings.Default.Save();
            }
        }

        private void button_forecast_type_Click(object sender, RoutedEventArgs e)//убрать
        {
            if (GridWeatherDay.Visibility != Visibility.Hidden)
            {
                GridWeatherDay.Visibility = Visibility.Hidden;
                GridWeatherWeek.Visibility = Visibility.Visible;
                btnForecastType.Content = "Прогноз на сегодня";
                btnMoveLeft.IsEnabled = false;
                btnMoveRight.IsEnabled = true;
            }
            else
            {
                GridWeatherWeek.Visibility = Visibility.Hidden;
                GridWeatherDay.Visibility = Visibility.Visible;
                btnForecastType.Content = "Прогноз на 8 дней";
            }
        }

        private void button_last_days_Click(object sender, RoutedEventArgs e)//убрать
        {
            //Task.Factory.StartNew(() => RefreshWeekAsync(4));
            btnMoveRight.IsEnabled = false;
            btnMoveLeft.IsEnabled = true;
        }
        
        private void button_first_days_Click(object sender, RoutedEventArgs e)//убрать
        {
            //Task.Factory.StartNew(() => RefreshWeekAsync(0));
            btnMoveLeft.IsEnabled = false;
            btnMoveRight.IsEnabled = true;
        }

        /// <summary>
        /// Обработчик нажатия тестовой кнопки
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void test_button_Click(object sender, RoutedEventArgs e)
        {
            _logger.Debug("Нажата тестовая кнопка настроек");
            MainWndInterface.RefreshMainWndData();
        }

        // Обработчики нажатий пунков контекстного меню иконки в трее
        /// <summary>
        /// Обработчик кнопки Скрыть/Показать
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ShowHideTray_Click(object sender, RoutedEventArgs e)
        {
            ShowHideMainWindow();
        }

        /// <summary>
        /// Обработчик кнопки Выход
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuExitTray_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.USCanClose = true;
            Close();
        }
        #endregion

        #region Методы
        /// <summary>
        /// Скрывает и отображает главное окно программы, запоминая состояние окна
        /// </summary>
        public static void ShowHideMainWindow()
        {
            if (Application.Current.MainWindow.WindowState != WindowState.Minimized)
                CurrentWindowState = Application.Current.MainWindow.WindowState;
            TrayMenu.IsOpen = false;
            if (Application.Current.MainWindow.IsVisible)
            {
                Application.Current.MainWindow.Hide();
                (TrayMenu.Items[0] as MenuItem).Header = "Показать";
            }
            else
            {
                Application.Current.MainWindow.Show();
                (TrayMenu.Items[0] as MenuItem).Header = "Свернуть";
                Application.Current.MainWindow.WindowState = CurrentWindowState;
                Application.Current.MainWindow.Activate();
            }
        }
        #endregion

        
    }
}
