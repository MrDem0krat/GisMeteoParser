using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ParserLib;

namespace GisMeteoWeather
{
    class Weather : IWeatherInfo
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
        public Weather() { }
        public Weather(int cityID, DateTime date, DayPart partOfDay, string condition, int humidity, int pressure, int temperature, int temperatureFeel, string type, string windDirection, float windSpeed, DateTime refreshTime)
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
            return String.Format("ID города: {10} \nДата прогноза: {0} \nВремя суток: {1} \nTeмпература: {2} \nОщущаемая температура: {3}\nСостояние: {4} \nИзображение: {5} \n"+
                                    "Влажность: {6} \nДавление: {7} \nНаправление ветра: {8} \nСкорость ветра: {9}",
                                    Date.ToShortDateString(), PartOfDay, Temperature, TemperatureFeel, Condition, TypeImage, Humidity, Pressure, WindDirection, WindSpeed, CityID);
        }
    }
}
