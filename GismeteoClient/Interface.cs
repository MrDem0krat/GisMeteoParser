using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using NLog;
using Weather;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace GismeteoClient
{
    public static class MainWndInterface
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();
        /// <summary>
        /// Хранит ссылку на объект главного окна
        /// </summary>
        public static MainWindow MainWindowReference { private get; set; }

        public static async Task ViewWeatherInfoNow(WeatherItem weather) //временно, забирать id из настроек, прогноз получать по id(отставить)
        {
            MainWindowReference.lblCityName.Content = await MainWindow.client.GetCityNameAsync(weather.CityID);
            MainWindowReference.lblLastRefresh.Content = string.Format("Последнее обновление в {0} {1}",weather.RefreshTime.ToShortTimeString(), weather.RefreshTime.ToShortDateString());
            MainWindowReference.lblTemperatureNow.Content = string.Format("{0}{1}°С", weather.Temperature > 0 ? "+" : "", weather.Temperature);
            MainWindowReference.lblTemperatureNow.ToolTip = string.Format("Ощущаемая температура: {0}{1}°С", weather.TemperatureFeel > 0 ? "+" : "", weather.TemperatureFeel);
            MainWindowReference.lblWindNow.Content = string.Format("Ветер: {0}, {1} м/с", weather.WindDirection, weather.WindSpeed);
            MainWindowReference.lblWindNow.ToolTip = string.Format("{0}, {1} м/с", weather.WindDirection, weather.WindSpeed);
            MainWindowReference.lblPressureNow.Content = string.Format("Давление: {0} мм.рт.ст.", weather.Pressure);
            MainWindowReference.lblPressureNow.ToolTip = string.Format("{0} мм.рт.ст.", weather.Pressure);
            MainWindowReference.lblHumidityNow.Content = string.Format("Влажность: {0}%",weather.Humidity);
            MainWindowReference.lblHumidityNow.ToolTip = string.Format("{0}%", weather.Humidity);
            MainWindowReference.lblWeatherTypeNow.Content = weather.Condition;
            MainWindowReference.lblWeatherTypeNow.ToolTip = weather.Condition;
        }

        public static void ViewWeatherInfoDayPart(WeatherItem weather, DayPart part)
        {
            var date = MainWindowReference.FindName("lblDate_" + (int)part) as Label;
            var condition = MainWindowReference.FindName("lblCondition_" + (int)part) as Label;
            var humidity = MainWindowReference.FindName("lblHumidity_" + (int)part) as Label;
            var pressure = MainWindowReference.FindName("lblPressure_" + (int)part) as Label;
            var wind = MainWindowReference.FindName("lblWind_" + (int)part) as Label;
            var temperature = MainWindowReference.FindName("lblTemperature_" + (int)part) as Label;
            var image = MainWindowReference.FindName("imgWeather_" + (int)part) as Image;

            date.Content = string.Format("{0} ({1})",weather.Date.ToShortDateString(), weather.PartOfDay.ToString());
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

        public static async Task RefreshMainWndData()
        {
            WeatherItem item = new WeatherItem();
            item = await MainWindow.client.GetWeatherAsync(Properties.Settings.Default.USCityID, DateTime.Today.AddDays(1), DayPart.Day);
            await ViewWeatherInfoNow(item);
            for (int i = 0; i < 4; i++)
            {
                item = new WeatherItem();
                item = await MainWindow.client.GetWeatherAsync(4258, DateTime.Today.AddDays(1), (DayPart)i);
                ViewWeatherInfoDayPart(item, (DayPart)i);
            }
        }
        
    }
}
