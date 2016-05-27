using Gismeteo.City;
using Gismeteo.Weather;
using System;
using System.Collections.Generic;
using System.ServiceModel;


namespace WeatherService
{
    [ServiceContract]
    public interface IWService
    {
        /// <summary>
        /// Возвращает название города по указанному ID
        /// </summary>
        /// <param name="cityID">ID города</param>
        /// <returns></returns>
        [OperationContract]
        string GetCityName(int cityID);

        /// <summary>
        /// Возвращает ID города по указанному названию города
        /// </summary>
        /// <param name="cityName">Название города</param>
        /// <returns></returns>
        [OperationContract]
        int GetCityId(string cityName);

        /// <summary>
        /// Возвращает список всех доступных городов
        /// </summary>
        /// <returns></returns>
        [OperationContract]
        List<City> GetAllCities();

        /// <summary>
        /// Возвращает прогноз для указанного города на выбранный промежуток времени
        /// </summary>
        /// <param name="id">ID города</param>
        /// <param name="date">Дата прогноза</param>
        /// <param name="part">Часть суток</param>
        /// <returns></returns>
        [OperationContract]
        WeatherItem GetWeather(int id, DateTime date, DayPart part);
    }
}
