using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using NLog;
using System.Text.RegularExpressions;
using ParserLib;

namespace GisMeteoWeather
{
    /// <summary>
    /// Обслуживающий класс, хранящий методы для парсинга информации с сайта gismeteo
    /// </summary>
    public class Worker
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Список полученных прогнозов
        /// </summary>
        public static List<WeatherParsedEventArgs> WeatherInfoCityList { get; private set; }
        
        //Добавить выбрасывание исключений при невозможности чего-то прочитать
        /// <summary>
        /// Функция выборки данных прогноза на заданный день в заданное время суток
        /// </summary>
        /// <param name="source">Исходный Html-документ для парсинга</param>
        /// <param name="dayToParse">Требуемый день</param>
        /// <param name="partOfDay">Требуемое время суток</param>
        /// <returns>Возвращает объект, реализующий интерфейс IWeatherInfo</returns>
        public static IWeatherInfo ParseWeatherData(HtmlDocument source, DayToParse dayToParse = DayToParse.Today, DayPart partOfDay = DayPart.Day) 
        {
            Weather _weather = new Weather();
            bool HasError = false;
            DateTime _date = new DateTime();
            int _humidity = 0;
            int _pressure = 0;
            int _temperature = 0;
            string _typeImage = "";
            int _temperatureFeel = 0;
            float _windSpeed = 0.0f;

            try
            {
                var DayWeatherInfoNodeCollection = source.GetElementbyId("tbwdaily" + (int)dayToParse).ChildNodes.Where(x => x.HasAttributes);
                if (DayWeatherInfoNodeCollection.Count() > 0)
                {
                    HtmlNode DayPartWeatherInfoNode = DayWeatherInfoNodeCollection.ElementAt((int)partOfDay);
                    //Date
                    Regex DateRegex = new Regex(@"([\d]{1,}[^A-Z]){1,}$");
                    string _dateTitle = DayPartWeatherInfoNode.Element("th").GetAttributeValue("title", "");
                    if (DateRegex.IsMatch(_dateTitle))
                        if (DateTime.TryParse(DateRegex.Match(_dateTitle).Value, out _date))
                            _weather.Date = _date;
                        else
                            HasError = true;

                    //PartOfDay
                    _weather.PartOfDay = partOfDay;

                    //Condition
                    HtmlNode ConditionNode = DayPartWeatherInfoNode.Elements("td").Where(x => x.GetAttributeValue("class", "") == "cltext").FirstOrDefault();
                    if (ConditionNode != null)
                        _weather.Condition = ConditionNode.InnerText;
                    else
                        HasError = true;

                    //TypeImage
                    HtmlNode TypeImageNode = DayPartWeatherInfoNode.Elements("td").Where(x => x.GetAttributeValue("class", "") == "clicon").FirstOrDefault().FirstChild;
                    if (TypeImageNode != null)
                    {
                        _typeImage = TypeImageNode.GetAttributeValue("src", "");
                        _weather.TypeImage = _typeImage.Substring(_typeImage.LastIndexOf("/") + 1);
                    }
                    else
                        HasError = true;

                    //Temperature
                    HtmlNode TemperatureNode = DayPartWeatherInfoNode.Elements("td").Where(x => x.GetAttributeValue("class", "") == "temp").FirstOrDefault();
                    if (TemperatureNode != null)
                    {
                        TemperatureNode = TemperatureNode.Elements("span").Where(x => x.GetAttributeValue("class", "").EndsWith("temp c")).FirstOrDefault();
                        if (TemperatureNode != null)
                            if (int.TryParse(TemperatureNode.InnerText, out _temperature))
                                _weather.Temperature = _temperature;
                            else
                                HasError = true;
                    }

                    var OtherNodes = DayPartWeatherInfoNode.Elements("td").Where(x => x.HasAttributes == false);
                    //Pressure
                    HtmlNode PressureNode = OtherNodes.ElementAt(0);
                    if (PressureNode != null)
                    {
                        PressureNode = PressureNode.Elements("span").Where(x => x.GetAttributeValue("class", "").EndsWith("torr")).FirstOrDefault();
                        if (PressureNode != null)
                            if (int.TryParse(PressureNode.InnerText, out _pressure))
                                _weather.Pressure = _pressure;
                            else
                                HasError = true;
                    }

                    //Wind
                    HtmlNode WindNode = OtherNodes.ElementAt(1).Element("dl");
                    if (WindNode != null)
                    {
                        _weather.WindDirection = WindNode.Element("dt").GetAttributeValue("title", "");
                        if (float.TryParse(WindNode.Element("dd").Elements("span").Where(x => x.GetAttributeValue("class", "").EndsWith("ms")).FirstOrDefault().InnerText, out _windSpeed))
                            _weather.WindSpeed = _windSpeed;
                        else
                            HasError = true;
                    }

                    //Humidity
                    HtmlNode HumidityNode = OtherNodes.ElementAt(2);
                    if (HumidityNode != null)
                    {
                        if (int.TryParse(HumidityNode.InnerText, out _humidity))
                            _weather.Humidity = _humidity;
                        else
                            HasError = true;
                    }

                    //TemperatureFeel
                    HtmlNode TempFeelNode = OtherNodes.ElementAt(3);
                    if (TempFeelNode != null)
                    {
                        TempFeelNode = TempFeelNode.Elements("span").Where(x => x.GetAttributeValue("class", "").EndsWith("temp c")).FirstOrDefault();
                        if (TempFeelNode != null)
                            if (int.TryParse(TempFeelNode.InnerText, out _temperatureFeel))
                                _weather.TemperatureFeel = _temperatureFeel;
                            else
                                HasError = true;
                    }
                }
                else
                    _logger.Error("Error:\tElements not found!");
                return _weather;
            }
            catch(Exception ex)
            {
                _logger.Error("Error: "+ ex.Message);
                return new Weather();
            }
        }

        /// <summary>
        /// Функция собирающая названия и id всех городов на главной странице.
        /// Функция возвращает список вида НазваниеГорода:ID
        /// </summary>
        /// <returns></returns>
        public static Dictionary<int, string> GetCityList()
        {
            HtmlWeb _web = new HtmlWeb();
            Dictionary<int, string> _cityDictionary = new Dictionary<int, string>();
            string _target = "https://www.gismeteo.ru";
            string buffer = "";
            int _id;
            List<string> wrapIdList = new List<string>() { "cities-teaser", "cities1", "cities2", "cities3" };
            Regex reg = new Regex(@"[\d]{1,}");

            HtmlDocument _mainPage = Parser.DownloadPage(_target);//_web.Load(_target);
            try
            {
                foreach (string wrapID in wrapIdList)
                {
                    var citiesListNode = _mainPage.GetElementbyId(wrapID).Elements("div").Where(x => x.GetAttributeValue("class", "") == "cities");
                    foreach (var cityNode in citiesListNode)
                    {
                        var ulNodes = cityNode.Elements("ul");
                        foreach (var ulNode in ulNodes)
                        {
                            var liNodes = ulNode.Elements("li");
                            foreach (var liNode in liNodes)
                            {
                                buffer = liNode.Element("a").GetAttributeValue("href", "");
                                buffer = reg.Match(buffer).Value;
                                if (int.TryParse(buffer, out _id))
                                    if (!_cityDictionary.ContainsKey(_id))
                                        _cityDictionary.Add(_id, liNode.Element("a").InnerText);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Debug("Can't get mai page city list: ", ex.Message);
            }
            return _cityDictionary;
        }
       
        #region EventHandlers
        public static void parser_WeatherParsed(object sender, WeatherParsedEventArgs e)
        {
            if (!e.HasError)
            {
                WeatherInfoCityList.Add(e);
            }
            else
            {
                Console.WriteLine("Has errors:\t" + e.HasError);
                Console.WriteLine("Error text:\t" + e.ErrorText);
                Console.WriteLine("Exception:\t" + e.Exception.Message);
            }
        }

        public static void parser_Started(object sender, EventArgs e)
        {
            _logger.Debug("Parser has been started");
            Console.WriteLine("Parser has been started");
        }

        public static void parser_Stopped(object sender, EventArgs e)
        {
            //WeatherInfoCityList.Clear();
            _logger.Debug("Parser has been stopped");
            Console.WriteLine("Parser has been stopped");

        }
        public static void parser_Asleep(object sender, EventArgs e)
        {
            
            //write weather list to db
            //WeatherInfoCityList.Clear();
            _logger.Debug("Parser goes to sleep");
            Console.WriteLine("Parser goes to sleep");

        }

        #endregion
        /*#region Logger Setting
        // Настройка логгера
        public static void LoggerConfig()
        {
            LoggingConfiguration config = new LoggingConfiguration();

            FileTarget fileTarget = new FileTarget();
            config.AddTarget("file", fileTarget);

            fileTarget.DeleteOldFileOnStartup = true; // Удалатья страый файл при запуске
            fileTarget.KeepFileOpen = false; //Держать файл открытым
            fileTarget.ConcurrentWrites = true; //
            fileTarget.Encoding = Encoding.Unicode; // Кодировка файла логгера
            fileTarget.ArchiveEvery = FileArchivePeriod.Day; // Период архивирования старых логов
            fileTarget.Layout = NLog.Layouts.Layout.FromString("${longdate} | ${uppercase:${level}} | ${message}"); // Структура сообщения
            fileTarget.FileName = NLog.Layouts.Layout.FromString("${basedir}/logs/${shortdate}.log"); //Структура названия файлов
            fileTarget.ArchiveFileName = NLog.Layouts.Layout.FromString("${basedir}/logs/archives/{shortdate}.rar"); // Структура названия архивов

            LoggingRule ruleFile = new LoggingRule("*", LogLevel.Trace, fileTarget); // Минимальный уровень логгирования - Trace
            config.LoggingRules.Add(ruleFile);

            LogManager.Configuration = config;
        }
        #endregion
        */
    }
}
