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
using System.Windows.Shapes;


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
            if (Properties.Settings.Default.isDbDataSaved)
            {
                CityName.Items.Clear();
                Dictionary<int, string> _source = MainWindow.client.GetCitiesList();
                CityName.ItemsSource = _source.Values;
                CityName.Text = MainWindow.client.GetCityName(Properties.Settings.Default.USCityID);
            }
            else
            {
                CityName.Text = "";
                CityName.ToolTip = "Для отображения списка городов введите данные для подключения к БД";
            }
                RefreshPeriod.SelectedIndex = Properties.Settings.Default.USRefreshPeriod;
                isMinimazeToTray.IsChecked = !Properties.Settings.Default.USCanClose;
                Login.Text = Properties.Settings.Default.USdbLogin;
                Password.Password = "    ";
                Server.Text = Properties.Settings.Default.USdbServer;
                Port.Text = Properties.Settings.Default.USdbPort.ToString();
        }
        #endregion
    }
}
