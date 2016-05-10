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
        private void Ok_Click(object sender, RoutedEventArgs e) // Проверка ввода данных
        {
            
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        #endregion

        #region Методы
        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            CityName.Items.Clear();
            Task loadCities = Task.Factory.StartNew(() =>
            {
                Dictionary<int, string> _source = Service.GetAllCities();
                Dispatcher.Invoke(() => CityName.ItemsSource = _source.Values);
            });

            CityName.Text = Service.GetCityName(Properties.Settings.Default.USCityID);
            RefreshPeriod.SelectedIndex = Properties.Settings.Default.USRefreshPeriod;
            isMinimazeToTray.IsChecked = !Properties.Settings.Default.USCanClose;
        }
        #endregion
    }
}
