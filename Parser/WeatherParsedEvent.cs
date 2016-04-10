using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParserLib
{
    public class WeatherParsedEventArgs : EventArgs
    {
        public IWeatherInfo WeatherItem { get; private set; }
        public bool HasError { get; private set; }
        public Exception Exception { get; private set; }
        public string ErrorText { get; private set; }

        public WeatherParsedEventArgs(IWeatherInfo weatherItem, string errorText = null, Exception ee = null)
        {
            WeatherItem = weatherItem;
            HasError = !string.IsNullOrEmpty(errorText) || ee != null;
            ErrorText = errorText;
            Exception = ee;
        }
    }
}
