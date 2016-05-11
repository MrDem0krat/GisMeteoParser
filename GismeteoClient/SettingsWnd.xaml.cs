using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;


namespace GismeteoClient
{
    /// <summary>
    /// Логика взаимодействия для SettingsWnd.xaml
    /// </summary>
    public partial class SettingsWnd : Window
    {
        public SettingsWnd()
        {
            InitializeComponent();
        }

        #region Обработчики кнопок
        /// <summary>
        /// Обработчик нажатия кнопки ОК
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Ok_Click(object sender, RoutedEventArgs e) 
        {
            DialogResult = true;
        }
        /// <summary>
        /// Обработчик нажатия кнопки Отмена
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
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
            CityName.Items.Clear();
            if (Service.Connect())
            {
                Task.Factory.StartNew(() =>
                {
                    var cities = Service.GetAllCities();
                    var cityName = Service.GetCityName(Properties.Settings.Default.USCityID);
                    Dispatcher.Invoke(() =>
                    {
                        CityName.ItemsSource = cities.Values;
                        CityName.Text = cityName;
                    });
                });
            }
            else
            {
                CityName.Items.Add(Properties.Settings.Default.USCityName);
                CityName.SelectedIndex = 0;
            }                
            RefreshPeriod.SelectedIndex = Properties.Settings.Default.USRefreshPeriod;
            isMinimazeToTray.IsChecked = !Properties.Settings.Default.USCanClose;
        }
        #endregion
    }
}
