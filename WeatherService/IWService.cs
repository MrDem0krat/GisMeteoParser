using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using Weather;

namespace WeatherService
{
    // ПРИМЕЧАНИЕ. Команду "Переименовать" в меню "Рефакторинг" можно использовать для одновременного изменения имени интерфейса "IWService" в коде и файле конфигурации.
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
        Dictionary<int, string> GetAllCities();

        /// <summary>
        /// Возвращает прогноз для указанного города на выбранный промежуток времени
        /// </summary>
        /// <param name="id">ID города</param>
        /// <param name="date">Дата прогноза</param>
        /// <param name="part">Часть суток</param>
        /// <returns></returns>
        [OperationContract]
        WeatherItem GetWeather(int id, DateTime date, DayPart part);

        /// <summary>
        /// Функция для проверки связи с сервисом
        /// </summary>
        /// <returns></returns>
        [OperationContract]
        bool CheckConnection();
    }
}
