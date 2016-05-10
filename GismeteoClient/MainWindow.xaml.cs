using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using NLog;
using Weather;

namespace GismeteoClient
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Переменные и События
        //TODO: Сделать отлов невоможности (проверки) соединения с сервисом. В случае отсутствия - отобразить посление доступные и в статусе вывести ошибку подключения
        /// <summary>
        /// Логер приложения
        /// </summary>
        public static Logger _logger = LogManager.GetCurrentClassLogger();
        /// <summary>
        /// Иконка для трея
        /// </summary>
        public static System.Windows.Forms.NotifyIcon TrayIcon = new System.Windows.Forms.NotifyIcon();
        /// <summary>
        /// Меню для иконки в трее
        /// </summary>
        public static ContextMenu TrayMenu = new ContextMenu();
        /// <summary>
        /// Splash-screen отображаемый при запуске приложения
        /// </summary>
        public static SplashScreen _splashScreen = new SplashScreen("Resources/graphics/SplashScreen.png");
        /// <summary>
        /// Окно настроек
        /// </summary>
        public SettingsWnd settingsWnd;
        /// <summary>
        /// Хранит состояние доступности сервиса
        /// </summary>
        protected static bool isServiceAvailable = false;

        /// <summary>
        /// Возникает при необходимости обновления информации главного окна
        /// </summary>
        public event EventHandler RefreshRequired;
        #endregion

        #region Свойства
        /// <summary>
        /// Текущее состояние главного окна
        /// </summary>
        private static WindowState CurrentWindowState { get; set; }

        #endregion

        public MainWindow()
        {
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            Application.Current.MainWindow.Hide();
            InitializeComponent();
            TrayIcon = Settings.TrayIconConfig();
            TrayMenu = Resources["TrayMenu"] as ContextMenu;
            GridWeatherWeek.Visibility = Visibility.Hidden;
            _logger.Debug("Settings loaded");
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
                Service.Disconnect();
                Properties.Settings.Default.Save();
                TrayIcon.Visible = false;
                TrayIcon.Dispose();
                _logger.Info("Приложение закрыто");
            }
        }

        /// <summary>
        /// Перегрузка обработчика события инициализации приложения
        /// </summary>
        /// <param name="e"></param>
        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            _logger.Info("Приложение запущено");

            isServiceAvailable = Service.Connect();

            RefreshRequired += onRefreshRequired_RefreshInfo;
            Application.Current.MainWindow.Loaded += onRefreshRequired_RefreshInfo;

        }

        /// <summary>
        /// Запись в лог ошибки в случае крэша приложеня
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
            RefreshMainWndData();
        }
        #endregion

        #region Обработчики кнопок
        /// <summary>
        /// Обработчик нажатия кнопки Обновить
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button_load_Click(object sender, RoutedEventArgs e)
        {
            if (RefreshRequired != null)
                RefreshRequired(this, new EventArgs());
        } 

        /// <summary>
        /// Обработчик нажатия кнопки Настройки
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button_settings_Click(object sender, RoutedEventArgs e)
        {
            settingsWnd = new SettingsWnd();
            if (settingsWnd.ShowDialog() == true)
            {
                bool isRefreshRequired = false;
                if (settingsWnd.CityName.SelectedItem != null)
                {
                    if(Properties.Settings.Default.USCityName != settingsWnd.CityName.SelectedItem.ToString())
                    {
                        Properties.Settings.Default.USCityName = settingsWnd.CityName.SelectedItem.ToString();
                        Properties.Settings.Default.USCityID = Service.GetCityID(settingsWnd.CityName.SelectedItem.ToString());
                        isRefreshRequired = true;
                    }
                }

                Properties.Settings.Default.USCanClose = !settingsWnd.isMinimazeToTray.IsChecked.Value;
                Properties.Settings.Default.USRefreshPeriod = settingsWnd.RefreshPeriod.SelectedIndex;

                Properties.Settings.Default.Save();
                if (isRefreshRequired)
                    if (RefreshRequired != null)
                        RefreshRequired(this, new EventArgs());
            }
        }

        /// <summary>
        /// Обработчик нажатия кнопки смены режима отображения прогноза
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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
        /// Обработчик нажатия кнопки Тест
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void test_button_Click(object sender, RoutedEventArgs e)
        {
            _logger.Debug("Нажата тестовая кнопка настроек");
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


        /// <summary>
        /// Обовляет информацию в блоке отображающем текущий прогноз
        /// </summary>
        /// <param name="weather">Прогноз погоды</param>
        /// <param name="cityName">Название города</param>
        public void ViewWeatherInfoNow(WeatherItem weather)
        {
            lblCityName.Content = Properties.Settings.Default.USCityName;
            lblLastRefresh.Content = string.Format("Последнее обновление в {0} {1}", weather.RefreshTime.ToShortTimeString(), weather.RefreshTime.ToShortDateString());
            lblTemperatureNow.Content = string.Format("{0}{1}°С", weather.Temperature > 0 ? "+" : "", weather.Temperature);
            lblTemperatureNow.ToolTip = string.Format("Ощущаемая температура: {0}{1}°С", weather.TemperatureFeel > 0 ? "+" : "", weather.TemperatureFeel);
            lblWindNow.Content = string.Format("Ветер: {0}, {1} м/с", weather.WindDirection, weather.WindSpeed);
            lblWindNow.ToolTip = string.Format("{0}, {1} м/с", weather.WindDirection, weather.WindSpeed);
            lblPressureNow.Content = string.Format("Давление: {0} мм.рт.ст.", weather.Pressure);
            lblPressureNow.ToolTip = string.Format("{0} мм.рт.ст.", weather.Pressure);
            lblHumidityNow.Content = string.Format("Влажность: {0}%", weather.Humidity);
            lblHumidityNow.ToolTip = string.Format("{0}%", weather.Humidity);
            lblWeatherTypeNow.Content = weather.Condition;
            lblWeatherTypeNow.ToolTip = weather.Condition;
        }

        /// <summary>
        /// Обновляет информацию в блоке, отображающем прогноз на часть суток
        /// </summary>
        /// <param name="weather">Прогноз погоды</param>
        /// <param name="part">Часть суток</param>
        public void ViewWeatherInfoDayPart(WeatherItem weather, DayPart part)
        {
            var date = FindName("lblDate_" + (int)part) as Label;
            var condition = FindName("lblCondition_" + (int)part) as Label;
            var humidity = FindName("lblHumidity_" + (int)part) as Label;
            var pressure = FindName("lblPressure_" + (int)part) as Label;
            var wind = FindName("lblWind_" + (int)part) as Label;
            var temperature = FindName("lblTemperature_" + (int)part) as Label;
            var image = FindName("imgWeather_" + (int)part) as Image;

            date.Content = string.Format("{0} ({1})", weather.Date.ToShortDateString(), weather.GetRusDayPartString());
            condition.Content = weather.Condition;
            condition.ToolTip = weather.Condition;
            humidity.Content = string.Format("Влажность: {0}%", weather.Humidity);
            humidity.ToolTip = string.Format("{0}%", weather.Humidity);
            pressure.Content = string.Format("Давление: {0} мм.рт.ст.", weather.Pressure);
            pressure.ToolTip = string.Format("{0} мм.рт.ст.", weather.Pressure);
            wind.Content = string.Format("Ветер: {0}, {1} м/с", weather.WindDirection, weather.WindSpeed);
            wind.ToolTip = string.Format("{0}, {1} м/с", weather.WindDirection, weather.WindSpeed);
            temperature.Content = string.Format("{0}{1}°С", weather.Temperature > 0 ? "+" : "", weather.Temperature);
            temperature.ToolTip = string.Format("Ощущаемая температура: {0}{1}°С", weather.TemperatureFeel > 0 ? "+" : "", weather.TemperatureFeel);
            BitmapImage _wImage = new BitmapImage();
            _wImage.BeginInit();
            _wImage.UriSource = new Uri("Resources/graphics/weather_icons/bkn_n.png", UriKind.Relative);
            _wImage.EndInit();
            image.Source = _wImage;
            image.ToolTip = weather.TypeImage;
        }

        /// <summary>
        /// Обновляет информацию, отображаемую в главном окне
        /// </summary>
        //public void RefreshMainWndData()
        //{
        //    WeatherItem item = new WeatherItem();
        //    var defaultWeather = new WeatherItem();
        //    if (isServiceAvailable)
        //        Task.Run(() => 
        //        {
        //            int addDays = 1;
        //            bool isForecastFound = false;
        //            while (!isForecastFound)
        //            {
        //                item = Service.GetWeather(Properties.Settings.Default.USCityID, DateTime.Today.AddDays(addDays), DayPart.Day);
        //                if(item == defaultWeather)
        //                {
        //                    addDays--;
        //                }
        //                else
        //                {
        //                    isForecastFound = true;
        //                }
        //            }
        //            Properties.Settings.Default.LastForecastNow = item;
        //            Dispatcher.Invoke(() =>
        //            {
        //                ViewWeatherInfoNow(item);
        //            });

        //            for (int i = 0; i < 4; i++)
        //            {
        //                item = new WeatherItem();
        //                item = Service.GetWeather(Properties.Settings.Default.USCityID, DateTime.Today.AddDays(addDays), (DayPart)i);
        //                Properties.Settings.Default["LastForecast" + (DayPart)i] = item;
        //                Dispatcher.Invoke(() =>
        //                {
        //                    ViewWeatherInfoDayPart(item, (DayPart)i);
        //                });
        //            }
        //        });
        //    else
        //    {
        //        ViewWeatherInfoNow(Properties.Settings.Default.LastForecastNow); //dispatcher
        //        for (int i = 0; i < 4; i++)
        //        {
        //            ViewWeatherInfoDayPart(Properties.Settings.Default["LastForecast" + (DayPart)i] as WeatherItem, (DayPart)i); //dispatcher
        //        }
        //    }
        //}

        public void RefreshMainWndData()
        {
            WeatherItem item = new WeatherItem();
            if (isServiceAvailable)
            {
                int addDays = 1;
                bool isForecastFound = false;
                while (!isForecastFound)
                {
                    item = Service.GetWeather(Properties.Settings.Default.USCityID, DateTime.Today.AddDays(addDays), DayPart.Day);
                    if (item.CityID == 0)
                    {
                        addDays--;
                    }
                    else
                    {
                        isForecastFound = true;
                    }
                }
                Properties.Settings.Default.LastForecastNow = item;
                ViewWeatherInfoNow(item);

                for (int i = 0; i < 4; i++)
                {
                    item = new WeatherItem();
                    item = Service.GetWeather(Properties.Settings.Default.USCityID, DateTime.Today.AddDays(addDays), (DayPart)i);
                    Properties.Settings.Default["LastForecast" + (DayPart)i] = item;
                    ViewWeatherInfoDayPart(item, (DayPart)i);
                }
            }
            else
            {
                ViewWeatherInfoNow(Properties.Settings.Default.LastForecastNow);
                for (int i = 0; i < 4; i++)
                {
                    ViewWeatherInfoDayPart(Properties.Settings.Default["LastForecast" + (DayPart)i] as WeatherItem, (DayPart)i);
                }
            }
        }



        #endregion
    }
}
