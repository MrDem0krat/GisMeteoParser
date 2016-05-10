using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Weather
{
    public class WeatherItem : IWeatherItem, IComparable<IWeatherItem>
    {
        #region Свойства
        /// <summary>
        /// ID города, для которого предоставлен прогноз
        /// </summary>
        public int CityID { get; set; }

        /// <summary>
        /// Cостояние погоды
        /// </summary>
        public string Condition { get; set; }

        /// <summary>
        /// День, на который предоставляется прогноз
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// Часть дня, на которую предоставляется прогноз
        /// </summary>
        public DayPart PartOfDay { get; set; }

        /// <summary>
        /// Влажность
        /// </summary>
        public int Humidity { get; set; }

        /// <summary>
        /// Давление
        /// </summary>
        public int Pressure { get; set; }

        /// <summary>
        /// Обещанная температура
        /// </summary>
        public int Temperature { get; set; }

        /// <summary>
        /// Температура по ощущениям
        /// </summary>
        public int TemperatureFeel { get; set; }

        /// <summary>
        /// Имя изображения, подходящего под состояние погоды
        /// </summary>
        public string TypeImage { get; set; }

        /// <summary>
        /// Направление ветра
        /// </summary>
        public string WindDirection { get; set; }

        /// <summary>
        /// Скорость ветра
        /// </summary>
        public float WindSpeed { get; set; }

        /// <summary>
        /// Время обновления данных
        /// </summary>
        public DateTime RefreshTime { get; set; }
        #endregion

        #region Конструкоры
        public WeatherItem() { }
        public WeatherItem(int cityID, DateTime date, DayPart partOfDay, string condition, int humidity, int pressure, int temperature, int temperatureFeel, string type, string windDirection, float windSpeed, DateTime refreshTime)
        {
            CityID = cityID;
            Date = date;
            PartOfDay = partOfDay;
            Condition = condition;
            Humidity = humidity;
            Pressure = pressure;
            Temperature = temperature;
            TemperatureFeel = temperatureFeel;
            TypeImage = type;
            WindDirection = windDirection;
            WindSpeed = windSpeed;
            RefreshTime = refreshTime;
        }
        #endregion

        public override string ToString()
        {
            return string.Format("ID города: {0} \n"+
                    "Дата прогноза: {1} \n"+
                    "Время суток: {2} \n"+
                    "Teмпература: {3} \n"+
                    "Ощущаемая температура: {4}\n" + 
                    "Состояние: {5} \n"+
                    "Изображение: {6} \n" +
                    "Влажность: {7} \n"+
                    "Давление: {8} \n"+
                    "Направление ветра: {9} \n"+
                    "Скорость ветра: {10}\n"+
                    "Время обновления: {11}\n",
                                    CityID, Date.ToShortDateString(), PartOfDay, Temperature, TemperatureFeel, Condition, TypeImage, Humidity, Pressure, WindDirection, WindSpeed, RefreshTime.ToString());
        }

        public string GetRusDayPartString()
        {
            switch (PartOfDay)
            {
                case DayPart.Night:
                    return "Ночь";
                case DayPart.Morning:
                    return "Утро";
                case DayPart.Day:
                    return "День";
                case DayPart.Evening:
                    return "Вечер";
                default:
                    return "Not Found";
            }
        }

        public int CompareTo(IWeatherItem other)
        {
            if (other != null)
            {
                if (Date > other.Date)
                    return Date.CompareTo(other.Date);
            }
            else
                throw new ArgumentException("Parameter is not IWeatherItem");
            return -13;
        }
    }
}
