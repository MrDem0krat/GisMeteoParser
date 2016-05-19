using NLog;
using System;
using System.Collections.Generic;
using System.ServiceModel;
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
        private static TimeSpan timeout = TimeSpan.FromSeconds(6);

        /// <summary>
        /// Покдлючается к сервису
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
                }
                _logger.Trace("Service.Open() == success");
                result = true;
            }
            catch (EndpointNotFoundException)
            {
                client.Abort();
                _logger.Warn("Service unavailable");
                result = false;
            }
            catch (Exception ex)
            {
                client.Abort();
                if (client.State == CommunicationState.Faulted)
                    _logger.Warn("Service.Open() error. Message: {0}", ex.Message);
                result = false;
            }
            return result;
        }
        /// <summary>
        /// Подключается к сервису
        /// </summary>
        /// <param name="timeout">Таймаут ожидания подключения</param>
        /// <returns></returns>
        public static bool Connect(TimeSpan timeout)
        {
            bool result = false;
            var task = Task.Factory.StartNew(() => result = Connect());
            if (!task.Wait(timeout))
            {
                _logger.Info("Service connection is timed out");
                return false;
            }
            return result;
        }
        /// <summary>
        /// Отключение от сервисв
        /// </summary>
        public static void Disconnect()
        {
            try
            {
                if (client.State == CommunicationState.Faulted)
                {
                    _logger.Trace("Servise.State = Faulted. Abort()");
                    client.Abort();
                }
                if (client.State != CommunicationState.Closed)
                {
                    _logger.Trace("Service.State = {0}. Close()", client.State);
                    client.Close();
                }
                _logger.Debug("Service closed");
            }
            catch (CommunicationException ex)
            {
                _logger.Warn("Error while closing service connection. Message: {0}", ex.Message);
                client.Abort();
            }
        }

        /// <summary>
        /// Возвращает перечень всех доступных городов
        /// </summary>
        /// <returns></returns>
        public static Dictionary<int, string> GetAllCities()
        {
            var result = new Dictionary<int, string>();
            if (Connect())
                result = client.GetAllCities();
            _logger.Trace("GetAllCities() = success");
            Disconnect();
            return result;
        }
        /// <summary>
        /// Возвращает название города для указанного ID
        /// </summary>
        /// <param name="cityID">ID города</param>
        /// <returns></returns>
        public static string GetCityName(int cityID)
        {
            var result = string.Empty;
            if (Connect())
                result = client.GetCityName(cityID);
            _logger.Trace("GetCityName({0}) = {1}", cityID, result);
            Disconnect();
            return result;
        }
        /// <summary>
        /// Возвращает ID для указанного города
        /// </summary>
        /// <param name="cityName">Название города</param>
        /// <returns></returns>
        public static int GetCityID(string cityName)
        {
            int result = 0;
            if (Connect())
                result = client.GetCityId(cityName);
            _logger.Trace("GetCityID({0}) = {1}", cityName, result);
            Disconnect();
            return result;
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
            var result = new WeatherItem();
            _logger.Trace("GetWeather({0}, {1}, {2})", cityID, date.ToShortDateString(), part);
            if (Connect())
                result = client.GetWeather(cityID, date, part);
            Disconnect();
            return result;
        }
    }    
}