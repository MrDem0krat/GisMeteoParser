using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using DataBaseWeather;
using Weather;

namespace WeatherService
{
    public class WeatherService : IWeatherService
    {
        public string GetCityNameByID(int id)
        {
            return DataBase.GetCityNameByID(id);
        }

        public IWeatherItem GetWeatherItem(int cityID, DateTime date, DayPart dayPart)
        {
            return DataBase.GetWeatherItem(cityID, date, dayPart);
        }

        //connect
        //configConnectionData(user,passwort,server,port)
        //savesettings
    }
}
