using System;
using System.Collections.Generic;
using System.Linq;
using Database;
using NLog;
using Gismeteo.City;
using Gismeteo.Weather;

namespace WeatherService
{
    public class WService : IWService
    {
        
        private static Logger _logger = LogManager.GetCurrentClassLogger();

        public static void Configure()
        {
            DataBase.ConnectionString = Properties.Settings.Default.dbConnectionString;
        }

        /// <summary>
        /// Возвращает список городов из базы данных, для которых хранится прогноз
        /// </summary>
        /// <returns></returns>
        public List<City> GetAllCities()
        {
            _logger.Trace("GetAllCities()");
            return DataBase.ReadCities();
        }

        /// <summary>
        /// Возвращает ID города по его названию
        /// </summary>
        /// <param name="cityName">Название искомого города</param>
        /// <returns></returns>
        public int GetCityId(string cityName)
        {
            _logger.Trace("GetCityID()");
            var list = GetAllCities();
            var result = from c in list where c.Name == cityName select c.ID;
            return result.FirstOrDefault();
        }

        /// <summary>
        /// Возвращает название города по его ID
        /// </summary>
        /// <param name="cityID">ID города</param>
        /// <returns></returns>
        public string GetCityName(int cityID)
        {
            _logger.Trace("GetCityName()");
            return DataBase.ReadCityName(cityID);
        }

        /// <summary>
        /// Возвращает прочитанный из базы данных последний полученный прогноз для выбранного города, в указанный день и время суток
        /// </summary>
        /// <param name="cityID">ID города</param>
        /// <param name="date">Дата прогноза</param>
        /// <param name="part">Время суток</param>
        /// <returns></returns>
        public WeatherItem GetWeather(int cityID, DateTime date, DayPart part)
        {
            _logger.Trace("GetWeather()");
            return DataBase.ReadWeatherItem(cityID, date, part) as WeatherItem;
        }
    }
}
