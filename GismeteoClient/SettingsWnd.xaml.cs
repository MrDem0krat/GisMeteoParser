using System;
using System.Threading.Tasks;
using System.Windows;
using NLog;
using System.Collections.Generic;

namespace GismeteoClient
{
    /// <summary>
    /// Логика взаимодействия для SettingsWnd.xaml
    /// </summary>
    public partial class SettingsWnd : Window
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();

        public SettingsWnd()
        {
            InitializeComponent();
            _logger.Trace("SettingsWnd initialized");
        }

        #region Обработчики кнопок
        /// <summary>
        /// Обработчик нажатия кнопки ОК
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Ok_Click(object sender, RoutedEventArgs e) 
        {
            _logger.Trace("SettingsWnd: Ok pressed");
            DialogResult = true;
        }
        /// <summary>
        /// Обработчик нажатия кнопки Отмена
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            _logger.Trace("SettingsWnd: Cancel pressed");
            DialogResult = false;
        }
        #endregion

        #region Методы
        /// <summary>
        /// Перегрузка функции обработки события инициализации окна настроек
        /// </summary>
        /// <param name="e"></param>
        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            _logger.Trace("SettingsWnd: initialisation");
            Dictionary<int, string> cities = new Dictionary<int, string>();
            cities.Add(Properties.Settings.Default.USCityID, Properties.Settings.Default.USCityName);
            string cityName = Properties.Settings.Default.USCityName;

            CityName.Text = cityName;
            CityName.ItemsSource = cities.Values;
            RefreshPeriod.SelectedIndex = Properties.Settings.Default.USRefreshPeriod;
            isMinimazeToTray.IsChecked = !Properties.Settings.Default.USCanClose;

            Task.Factory.StartNew(() =>
            {
                if (Service.Connect())
                {
                    _logger.Trace("SettingsWnd: get list of cities");
                    cities = Service.GetAllCities();
                    cityName = Service.GetCityName(Properties.Settings.Default.USCityID);
                    Dispatcher.Invoke(() =>
                    {
                        CityName.ItemsSource = cities.Values;
                        CityName.Text = cityName;
                        _logger.Debug("SettingsWnd: settings updated");
                    });
                }
                else
                    _logger.Trace("SettingsWnd: service unavalable. Loaded short list of cities");
            });
        }
        #endregion
    }
}
