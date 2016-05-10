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

        public static bool isAvailable { get { return Check(); } }
        public static CommunicationState State { get { return client.State; } }

        public static bool Connect()
        {
            try
            {
                if (client.State != CommunicationState.Opened)
                {
                    client.Open();
                }
                _logger.Debug("Service.Open() == true");
                return true;
            }
            catch(EndpointNotFoundException)
            {
                _logger.Debug("Service.Open() == false");
                _logger.Info("Service unavailable. Try again later");
                
            }
            catch (Exception ex)
            {
                if (client.State == CommunicationState.Faulted)
                {
                    _logger.Warn("Service.Open() error. Message: {0}", ex.Message);
                    return false;
                }
            }
            return false;
        }

        /// <summary>
        /// Проверяет связь с сервисом и возвращает true если все хорошо
        /// </summary>
        /// <returns></returns>
        private static bool Check()
        {
            try
            {
                if (client.State != CommunicationState.Opened)
                    client.Open();
                if(client.CheckConnection())
                    return true;
            }
            catch (Exception)
            {
                
                return false;
            }
            return false;
        }

        public static Dictionary<int, string> GetAllCities()
        {
            if (isAvailable)
                return client.GetAllCities(); 
            else
                return new Dictionary<int, string>();
        }

        public static string GetCityName(int cityID)
        {
            if (isAvailable)
                return client.GetCityName(cityID);
            else
                return string.Empty;
        }

        public static int GetCityID(string cityName)
        {
            if (isAvailable)
                return client.GetCityId(cityName);
            else
                return 0;
        }

        public static WeatherItem GetWeather(int cityID, DateTime date, DayPart part)
        {
            if (isAvailable)
                return client.GetWeather(cityID, date, part);
            else
                return new WeatherItem();
        }

        public static void Disconnect()
        {
            if(client.State != CommunicationState.Closed && client.State != CommunicationState.Faulted)
                client.Close();
            _logger.Debug("Service.Close()");
        }
        enum ClientState
        {
            Connected = 1,
            Unavailable = 2,

        }
    }
}
