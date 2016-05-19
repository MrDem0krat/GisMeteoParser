using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using NLog;
using Weather;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Media.Animation;
using System.Windows.Media;
using System.Collections.ObjectModel;

namespace GismeteoClient
{
    public partial class MainWindow : Window
    {
        #region Поля
        /// <summary>
        /// Логер приложения
        /// </summary>
        public static Logger _logger = LogManager.GetCurrentClassLogger();
        /// <summary>
        /// Буфер сообщений логера для отображения в приложении
        /// </summary>
        public static ObservableCollection<LoggerMeassage> logMsgBuf = new ObservableCollection<LoggerMeassage>();

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
        /// Окно логов
        /// </summary>
        public static LogWnd logWnd;
        /// <summary>
        /// Task с циклом периодического обновления данных погоды
        /// </summary>
        private Thread mainRefreshTaskThread;
        /// <summary>
        /// Статус обновления данных
        /// </summary>
        public enum ImageState
        {
            /// <summary>
            /// Ошибка
            /// </summary>
            Error = 0,
            /// <summary>
            /// Ок
            /// </summary>
            Ok = 1,
            /// <summary>
            /// Обновление информации
            /// </summary>
            Refreshing = 2
        }
        #endregion

        #region События
        /// <summary>
        /// Возникает при необходимости обновления информации главного окна
        /// </summary>
        public event EventHandler RefreshRequired;
       /// <summary>
       /// Возникает при появлении нового сообщения от логгера
       /// </summary>
        public static event EventHandler<LoggerMeassage> MessageAdded;
        #endregion

        #region Свойства
        /// <summary>
        /// Текущее состояние главного окна
        /// </summary>
        private static WindowState CurrentWindowState { get; set; }
        #endregion

        #region Обработчики кнопок
        /// <summary>
        /// Обработчик нажатия кнопки Обновить
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button_refresh_Click(object sender, RoutedEventArgs e)
        {
            _logger.Trace("MainWnd: button Refresh clicked");
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
            _logger.Trace("MainWnd: button Settings clicked");
            settingsWnd = new SettingsWnd();
            if (settingsWnd.ShowDialog() == true)
            {
                _logger.Trace("MainWnd: check changed data");
                bool isRefreshRequired = false;
                if (settingsWnd.CityName.SelectedItem != null)
                {
                    if (Properties.Settings.Default.USCityName != settingsWnd.CityName.SelectedItem.ToString())
                    {
                        Properties.Settings.Default.USCityName = settingsWnd.CityName.SelectedItem.ToString();
                        Properties.Settings.Default.USCityID = Service.GetCityID(settingsWnd.CityName.SelectedItem.ToString());
                        isRefreshRequired = true;
                    }
                }

                Properties.Settings.Default.USCanClose = !settingsWnd.isMinimazeToTray.IsChecked.Value;
                Properties.Settings.Default.USRefreshPeriod = settingsWnd.RefreshPeriod.SelectedIndex;

                Properties.Settings.Default.Save();
                _logger.Info("MainWnd: settings saved");
                if (isRefreshRequired)
                    if (RefreshRequired != null)
                        RefreshRequired(this, new EventArgs());
            }
        }

        /// <summary>
        /// Обработчик кнопки Просмотреть логи
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ShowLog_Click(object sender, RoutedEventArgs e)
        {
            _logger.Trace("MainWnd: ShowLog pressed");
            if (logWnd == null)
            {
                logWnd = new LogWnd();
                logWnd.Show();
            }
            if(!logWnd.IsActive)
            {
                logWnd.Activate();
            }
        }

        // Обработчики нажатий пунктов контекстного меню иконки в трее
        /// <summary>
        /// Обработчик кнопки Скрыть/Показать
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ShowHideTray_Click(object sender, RoutedEventArgs e)
        {
            _logger.Trace("TrayMenu: button Hide/Show pressed");
            ShowHideMainWindow();
        }

        /// <summary>
        /// Обработчик кнопки Выход
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuExitTray_Click(object sender, RoutedEventArgs e)
        {
            _logger.Trace("TrayMenu: button Exit pressed");
            Properties.Settings.Default.USCanClose = true;
            Close();
        }
        #endregion

        #region Методы
        /// <summary>
        /// Скрывает либо отображает главное окно программы
        /// </summary>
        public static void ShowHideMainWindow()
        {
            if (TrayMenu != null)
                TrayMenu.IsOpen = false;
            if (Application.Current.MainWindow.WindowState != WindowState.Minimized)
                Application.Current.MainWindow.WindowState = WindowState.Minimized;
            else
            {
                Application.Current.MainWindow.WindowState = CurrentWindowState;
                Application.Current.MainWindow.Show();
                Application.Current.MainWindow.Activate();
            }
        }

        /// <summary>
        /// Подбирает иображение соответствующее прогнозу
        /// </summary>
        /// <param name="imageType">Имя изображения</param>
        /// <returns></returns>
        public BitmapImage SelectImage(string imageType)
        {
            BitmapImage result = new BitmapImage();
            Regex cloud = new Regex(@"c\d");
            Regex rain = new Regex(@"r\d");
            Regex snow = new Regex(@"s\d");
            if (imageType != null)
            {
                if (cloud.Match(imageType).Value == "c2")
                    imageType = imageType.Replace("c2", "c1");
                if (rain.Match(imageType).Value == "r3")
                    imageType = imageType.Replace("r3", "r2");
                if (snow.Match(imageType).Value == "s3")
                    imageType = imageType.Replace("s3", "s2");
            }
            else
                imageType = "d.sun.png"; //изображение, отображаемое по умолчанию, если прогноз не получен

            result.BeginInit();
            result.UriSource = new Uri(Properties.Resources.weatherIconsPath + imageType, UriKind.Relative);
            result.EndInit();

            return result;
        }
        /// <summary>
        /// Выбирает изображение, отображающее статус выполнения обновления данных
        /// </summary>
        /// <param name="state">Статус обновления</param>
        /// <param name="_ToolTip">Сообщение, отображаемое при наведении мыши на изображение</param>
        public void SetStatusStateImage(ImageState state, string _ToolTip)
        {
            string path = "Resources/graphics/status/";
            BitmapImage image = new BitmapImage();
            DoubleAnimation dAnimation = new DoubleAnimation(360, 0, new Duration(TimeSpan.FromSeconds(3)));
            dAnimation.RepeatBehavior = RepeatBehavior.Forever;
            RotateTransform rotateTransform = new RotateTransform();
            bool isRotate = false;

            switch (state)
            {
                case ImageState.Error:
                    path += "Warning.png";
                    image = new BitmapImage(new Uri(path, UriKind.Relative));
                    break;
                case ImageState.Ok:
                    path += "Success.png";
                    image = new BitmapImage(new Uri(path, UriKind.Relative));
                    break;
                case ImageState.Refreshing:
                    path += "Repeat.png";
                    image = new BitmapImage(new Uri(path, UriKind.Relative));
                    isRotate = true;
                    break;
            }

            _logger.Trace("StatusImage = {0}, ToolTip = '{1}'", state, _ToolTip);
            imgStatusRefresh.ToolTip = _ToolTip;
            imgStatusRefresh.Source = image;
            imgStatusRefresh.RenderTransform = new RotateTransform();
            if (isRotate)
            {
                imgStatusRefresh.RenderTransform = rotateTransform;
                imgStatusRefresh.RenderTransformOrigin = new Point(0.5, 0.5);
                rotateTransform.BeginAnimation(RotateTransform.AngleProperty, dAnimation);
            }
        }
        /// <summary>
        /// Выбирает изображение, отображающее статус выполнения обновления данных
        /// </summary>
        /// <param name="state">Статус обновления</param>
        public void SetStatusStateImage(ImageState state)
        {
            SetStatusStateImage(state, string.Empty);
        }

        /// <summary>
        /// Обовляет информацию в блоке отображающем текущий прогноз
        /// </summary>
        /// <param name="weather">Прогноз погоды</param>
        /// <param name="cityName">Название города</param>
        public void ViewWeatherInfoNow(WeatherItem weather)
        {
            lblCityName.Content = Properties.Settings.Default.USCityName + ", сейчас";
            lblCityName.ToolTip = "Прогноз актуален на " + weather.Date.ToShortDateString();
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
            imgWeatherNow.Source = SelectImage(weather.TypeImage);
            imgWeatherNow.ToolTip = weather.Condition;
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

            date.Content = string.Format("{0} ({1})", weather.Date.ToShortDateString(), weather.GetRusDayPart());
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
            image.Source = SelectImage(weather.TypeImage);
            image.ToolTip = weather.Condition;
        }

        /// <summary>
        /// Обновляет информацию, отображаемую в главном окне
        /// </summary>
        public void RefreshMainWndData()
        {
            WeatherItem item = new WeatherItem();
            DayPart dayPartNow;

            _logger.Trace("RefreshMainWndData(): started");
            Dispatcher.Invoke(() => SetStatusStateImage(ImageState.Refreshing, "Идет проверка наличия новых данных"));
            Task.Factory.StartNew(() =>
            {
                if (Service.Connect())
                {
                    if (DateTime.Now.Hour > 0 && DateTime.Now.Hour <= 5)
                        dayPartNow = DayPart.Night;
                    else
                        if (DateTime.Now.Hour > 5 && DateTime.Now.Hour <= 11)
                            dayPartNow = DayPart.Morning;
                        else
                            if (DateTime.Now.Hour > 11 && DateTime.Now.Hour <= 17)
                                dayPartNow = DayPart.Day;
                            else
                                dayPartNow = DayPart.Evening;
                    //Грузим свежий прогноз, либо, если он отсутствует, отображаем сохраненный прогноз
                    //Прогноз на сегодня
                    item = GetWeather(Properties.Settings.Default.USCityID, DateTime.Today, dayPartNow);
                    if (item.CityID != 0)
                        Properties.Settings.Default.LastForecastNow = item;
                    else
                        item = Properties.Settings.Default.LastForecastNow;
                    _logger.Debug("WeatherNow.Date = {0}, DayPart = {1}", item.Date.ToShortDateString(), item.PartOfDay);
                    Dispatcher.Invoke(() => ViewWeatherInfoNow(item));
                    //Прогноз на завтра
                    for (int i = 0; i < 4; i++)
                    {
                        item = GetWeather(Properties.Settings.Default.USCityID, (DayPart)i);
                        if(item.CityID != 0)
                            Properties.Settings.Default["LastForecast" + (DayPart)i] = item;
                        else
                            item = Properties.Settings.Default["LastForecast" + (DayPart)i] as WeatherItem;
                        Dispatcher.Invoke(() => ViewWeatherInfoDayPart(item, (DayPart)i));
                    }
                    Dispatcher.Invoke(() =>
                    {
                        lblLastRefresh.Content = string.Format("Последнее обновление в {0:hh:mm dd.MM.yyyy}", item.RefreshTime);
                        SetStatusStateImage(ImageState.Ok, "Заружены последние доступные данные");
                    });
                }
                else
                {
                    Dispatcher.Invoke(() =>
                    {
                        ViewWeatherInfoNow(Properties.Settings.Default.LastForecastNow);
                        for (int i = 0; i < 4; i++)
                        {
                            ViewWeatherInfoDayPart(Properties.Settings.Default["LastForecast" + (DayPart)i] as WeatherItem, (DayPart)i);
                        }
                        SetStatusStateImage(ImageState.Error, "Сервис временно недоступен");
                        _logger.Info("Service unavailable. Forecast date is {0}", Properties.Settings.Default.LastForecastDay.Date.ToShortDateString());
                    });
                }
                Properties.Settings.Default.Save();
            });
        }

        /// <summary>
        /// Возвращает прогноз погоды на указанный день
        /// </summary>
        /// <param name="cityID">ID города</param>
        /// <param name="date">Дата прогноза</param>
        /// <param name="part">Время суток</param>
        /// <returns></returns>
        public WeatherItem GetWeather(int cityID, DateTime date, DayPart part)
        {
            WeatherItem result = new WeatherItem();
            if (Service.Connect())
            {
                result = Service.GetWeather(cityID, date, part);
                _logger.Trace("GetWeather({0},{1},{2}) = success", cityID, date.ToShortDateString(), part);
            }
            return result;
        }
        /// <summary>
        /// Возвраает прогноз погоды на завтрашний день
        /// </summary>
        /// <param name="cityID">ID города</param>
        /// <param name="part">Время суток</param>
        /// <returns></returns>
        public WeatherItem GetWeather(int cityID, DayPart part)
        {
            return GetWeather(cityID, DateTime.Today.AddDays(1), part);
        }

        /// <summary>
        /// Основной цикл выполнения обновления главного окна, выполняемый в отдельном потоке
        /// </summary>
        public void RefreshWeatherLoop()
        {
            Task.Factory.StartNew(() =>
            {
                mainRefreshTaskThread = Thread.CurrentThread;
                while (true)
                {
                    if (RefreshRequired != null)
                        RefreshRequired(this, new EventArgs());
                    Thread.Sleep(SelectRefreshPeriod());
                }
            });
        }
        /// <summary>
        /// Возвращает временной промежуток для обновления, основываясь на значении, выбранном в окне настроек
        /// </summary>
        /// <returns></returns>
        public TimeSpan SelectRefreshPeriod()
        {
            switch (Properties.Settings.Default.USRefreshPeriod)
            {
                case 0:
                    return TimeSpan.FromMinutes(30);
                case 1:
                    return TimeSpan.FromHours(1);
                case 2:
                    return TimeSpan.FromHours(2);
                case 3:
                    return TimeSpan.FromHours(4);
                default:
                    return TimeSpan.FromMinutes(30);
            }
        }

        /// <summary>
        /// Вызывается логером для добавления сообщений в программный буфер сообщений
        /// </summary>
        /// <param name="level">Уровень сообщения</param>
        /// <param name="time">Время сообщения</param>
        /// <param name="message">Текст сообщения</param>
        public static void AddMessage(string level, string time, string message)
        {
            if (MessageAdded != null)
                MessageAdded(new object(), new LoggerMeassage(level, time, message));
        }
        void messageAdded_refresh(object sender, LoggerMeassage e)
        {
            Dispatcher.Invoke(() => logMsgBuf.Add(e));
        }
        #endregion
    }
}
