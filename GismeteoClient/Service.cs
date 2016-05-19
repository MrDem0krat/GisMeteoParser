using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using Weather;

namespace GismeteoClient
{
    /// <summary>
    /// Класс для работы с WeatherService
    /// </summary>
    public static class Service
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();
        private static WeatherService.WServiceClient client = new WeatherService.WServiceClient("NetTcpBinding_IWService");
        /// <summary>
        /// Доступность сервиса
        /// </summary>
        public static bool isAvailable { get; private set; }
        /// <summary>
        /// Состояние сервиса
        /// </summary>
        public static CommunicationState State { get { return client.State; } }

        /// <summary>
        /// Подключается к сервису
        /// </summary>
        /// <returns></returns>
        public static bool Connect()
        {
            bool result = false;
            try
            {
                if (client.State != CommunicationState.Opened)
                {
                    client = new WeatherService.WServiceClient("NetTcpBinding_IWService");
                    client.Open();
                    _logger.Debug("Service.Open() == true");
                }
                result = true;
            }
            catch(EndpointNotFoundException)
            {
                _logger.Warn("Service unavailable. Try again later");
                result = false;
            }
            catch (Exception ex)
            {
                if (client.State == CommunicationState.Faulted)
                    _logger.Warn("Service.Open() error. Message: {0}", ex.Message);
                result = false;
            }
            isAvailable = result;
            return result;
        }

        /// <summary>
        /// Проверяет связь с сервисом и возвращает true если все хорошо
        /// </summary>
        /// <returns></returns>
        public static bool Check()
        {
            bool result = false;
            try
            {
                if (client.State != CommunicationState.Closed)
                    client.Abort();
                if (client.State != CommunicationState.Opened)
                    client.Open();
                if(client.CheckConnection())
                    result = true;
            }
            catch (Exception)
            {
                result = false;
            }
            return result;
        }

       /// <summary>
       /// Возвращает перечень всех доступных городов
       /// </summary>
       /// <returns></returns>
        public static Dictionary<int, string> GetAllCities()
        {
            if (Connect())
                return client.GetAllCities(); 
            else
                return new Dictionary<int, string>();
        }

        /// <summary>
        /// Возвращает название города для указанного ID
        /// </summary>
        /// <param name="cityID">ID города</param>
        /// <returns></returns>
        public static string GetCityName(int cityID)
        {
            if (Connect())
                return client.GetCityName(cityID);
            else
                return string.Empty;
        }

        /// <summary>
        /// Возвращает ID для указанного города
        /// </summary>
        /// <param name="cityName">Название города</param>
        /// <returns></returns>
        public static int GetCityID(string cityName)
        {
            if (Connect())
                return client.GetCityId(cityName);
            else
                return 0;
        }

        /// <summary>
        /// Возвращает прогноз погоды для указанного города в выбранный день и время суток
        /// </summary>
        /// <param name="cityID">ID города</param>
        /// <param name="date">Дата прогноза</param>
        /// <param name="part">Время суток</param>
        /// <returns></returns>
        public static WeatherItem GetWeather(int cityID, DateTime date, DayPart part)
        {
            if (Connect())
                return client.GetWeather(cityID, date, part);
            else
                return new WeatherItem();
        }

        /// <summary>
        /// Отключение от сервисв
        /// </summary>
        public static void Disconnect()
        {
            if(client.State != CommunicationState.Closed && client.State != CommunicationState.Faulted)
                client.Close();
            _logger.Debug("Service.Close()");
        }
        /// <summary>
        /// Перечень состояний сервиса
        /// </summary>
        enum ClientState
        {
            /// <summary>
            /// Подключен
            /// </summary>
            Connected = 1,
            /// <summary>
            /// Недоступен
            /// </summary>
            Unavailable = 2,

        }
    }
}
