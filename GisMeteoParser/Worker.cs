using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using NLog;
using System.Text.RegularExpressions;
using ParserLib;
using DataBaseWeather;
using NLog.Config;
using NLog.Targets;
using Weather;


namespace GisMeteoWeather
{
    /// <summary>
    /// Обслуживающий класс, хранящий методы для парсинга информации с сайта gismeteo
    /// </summary>
    public class Worker
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();
        private static Dictionary<int, string> cities = new Dictionary<int, string>();
        private static Parser parser = new Parser();

        /// <summary>
        /// Список полученных прогнозов
        /// </summary>
        public static List<WeatherItem> WeatherInfoList { get; private set; }
        
        /// <summary>
        /// Функция выборки данных прогноза на заданный день в заданное время суток
        /// </summary>
        /// <param name="source">Исходный Html-документ для парсинга</param>
        /// <param name="dayToParse">Требуемый день</param>
        /// <param name="partOfDay">Требуемое время суток</param>
        /// <returns>Возвращает объект, реализующий интерфейс IWeatherInfo</returns>
        public static IWeatherItem ParseWeatherData(HtmlDocument source, DayToParse dayToParse = DayToParse.Today, DayPart partOfDay = DayPart.Day) 
        {
            WeatherItem _weather = new WeatherItem();
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
                            throw new Exception("Error while parsing Date");

                    //PartOfDay
                    _weather.PartOfDay = partOfDay;

                    //Condition
                    HtmlNode ConditionNode = DayPartWeatherInfoNode.Elements("td").Where(x => x.GetAttributeValue("class", "") == "cltext").FirstOrDefault();
                    if (ConditionNode != null)
                        _weather.Condition = ConditionNode.InnerText;
                    else
                        throw new Exception("Error while parsing Condition");

                    //TypeImage
                    HtmlNode TypeImageNode = DayPartWeatherInfoNode.Elements("td").Where(x => x.GetAttributeValue("class", "") == "clicon").FirstOrDefault().FirstChild;
                    if (TypeImageNode != null)
                    {
                        _typeImage = TypeImageNode.GetAttributeValue("src", "");
                        _weather.TypeImage = _typeImage.Substring(_typeImage.LastIndexOf("/") + 1);
                    }
                    else
                        throw new Exception("Error while parsing TypeImage");

                    //Temperature
                    HtmlNode TemperatureNode = DayPartWeatherInfoNode.Elements("td").Where(x => x.GetAttributeValue("class", "") == "temp").FirstOrDefault();
                    if (TemperatureNode != null)
                    {
                        TemperatureNode = TemperatureNode.Elements("span").Where(x => x.GetAttributeValue("class", "").EndsWith("temp c")).FirstOrDefault();
                        if (TemperatureNode != null)
                            if (int.TryParse(TemperatureNode.InnerText, out _temperature))
                                _weather.Temperature = _temperature;
                            else
                                throw new Exception("Error while parsing Temperature");
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
                                throw new Exception("Error while parsing Pressure");

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
                            throw new Exception("Error while parsing Wind");
                    }

                    //Humidity
                    HtmlNode HumidityNode = OtherNodes.ElementAt(2);
                    if (HumidityNode != null)
                    {
                        if (int.TryParse(HumidityNode.InnerText, out _humidity))
                            _weather.Humidity = _humidity;
                        else
                            throw new Exception("Error while parsing Humidity");
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
                                throw new Exception("Error while parsing TemperatureFeel");
                    }
                }
                else
                    throw new Exception("Error: Elements not found!");
            }
            catch(Exception ex)
            {
                HasError = true;
                _logger.Error("Error: "+ ex.Message);
            }

            if (!HasError)
                return _weather;
            else
                return new WeatherItem();
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
            bool hasError = false;
            string _target = "https://www.gismeteo.ru";
            string buffer = "";
            int _id;
            List<string> wrapIdList = new List<string>() { "cities-teaser", "cities1", "cities2", "cities3" };
            Regex reg = new Regex(@"[\d]{1,}");

            HtmlDocument _mainPage = Parser.DownloadPage(_target);
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
                hasError = true;
                _logger.Error("Can't get main page city list: ", ex.Message);
            }
            if (!hasError)
                return _cityDictionary;
            else
                return new Dictionary<int, string>();
        }

        /// <summary>
        /// Подготавливает парсер к работе
        /// </summary>
        public static void Setup()
        {
            LoggerConfig();
            _logger.Debug("Приложение запущено");
            _logger.Debug("Запущена проверка и подготовка необходимой базы данных");
            DataBase.Prepare(); 
            _logger.Debug("Проверка наличия новых городов на главной странице сайта");
            parser.Cities = GetCityList();
            var citylist = DataBase.ReadCityList();
            Dictionary<int, string> other = new Dictionary<int, string>();

            if (!parser.Cities.Equals(citylist))
                foreach (var newcity in parser.Cities)
                {
                    if (!citylist.ContainsKey(newcity.Key))
                        other.Add(newcity.Key,newcity.Value);
                }
            if(other.Count > 0)
            {
                _logger.Debug("Доступны новые города в списке. Добавляем их в базу данных ");
                DataBase.WriteCityList(other);
            }

            _logger.Debug("Настройка параметров парсера");
            parser.TargetUrl = Properties.Resources.TargetSiteUrl;
            parser.RefreshPeriod = new TimeSpan(0, 2, 0);
            parser.ParserHandler = new Parser.ParserGetDataHandler(ParseWeatherData);

            parser.WeatherParsed += parser_WeatherParsed;
            parser.ParserStarted += parser_Started;
            parser.ParserStopped += parser_Stopped;
            parser.ParserAsleep += parser_Asleep;
            _logger.Debug("Парсер успешно настроен");
        }

        /// <summary>
        /// Выводит в консоль список городов, полученных с главной страницы GisMeteo
        /// </summary>
        /// <param name="source">Список городов</param>
        public static void PrintCityList(Dictionary<int,string> source)
        {
            foreach (var item in source)
                Console.WriteLine("ID: " + item.Key + "\tName: " + item.Value);
        } //test; later remove

        /// <summary>
        /// Обработчик пользовательского ввода
        /// </summary>
        /// <param name="command"></param>
        public static void RunCommand(string command) //переделать, убрать отладочные функции
        {
            switch (command)
            {
                case "?":
                    Console.WriteLine("Список доступных команд:\n\n"+
                        "{0,-20}Запустить парсер\n"+
                        "{1,-20}Остановить парсер\n"+
                        "{2,-20}Текущий статус парсера\n"+
                        "{3,-20}Загрузить список городов с главной страницы\n"+
                        "{4,-20}Подготовить базу данных для работы\n"+
                        "{5,-20}Записать список городов в базу данных\n"+
                        "{7,-20}Ввести пароль к БД (root)\n" +
                        "{8,-20}Остановить парсер и закрыть приложение\n",
                                        "start", "stop", "status", "load cl", "prepare", "write cl", "password", "exit");
                    break;

                case "exit":
                    Program.isRun = false;
                    if (parser.Status == ParserStatus.Working || parser.Status == ParserStatus.Sleeping)
                    {
                        parser.Stop();
                    }
                    break;

                case "stop":
                    if (parser.Status == ParserStatus.Working || parser.Status == ParserStatus.Sleeping)
                    {
                        parser.Stop();
                    }
                    else
                    {
                        Console.WriteLine("Парсер уже остановлен!");
                    }
                    break;

                case "start":
                    if (parser.Status == ParserStatus.Stoped || parser.Status == ParserStatus.Aborted)
                    {
#pragma warning disable CS4014 // Так как этот вызов не ожидается, выполнение существующего метода продолжается до завершения вызова
                        parser.StartAsync();
#pragma warning restore CS4014 // Так как этот вызов не ожидается, выполнение существующего метода продолжается до завершения вызова
                    }
                    else
                    {
                        Console.WriteLine("Парсер уже запущен!");
                    }
                    break;

                case "status":
                    Console.WriteLine("Статус парсера: {0}", parser.Status);
                    break;

                case "password":
                    Console.Write("Введите пароль: ");
                    DataBase.Password = Console.ReadLine();
                    DataBase.SaveSettings();
                    Console.WriteLine("Пароль успешно обновлен и сохранен");
                    break;

                case "prepare":
                    DataBase.Prepare();
                    Console.WriteLine("База данных подготовлена");
                    break;

                case "load cl":
                    cities = GetCityList();
                    Console.WriteLine("Список городов успешно загружен.");
                    break;

                case "write cl":
                    if (DataBase.WriteCityList(cities))
                        Console.WriteLine("Список городов успешно записан в базу данных");
                    else
                        Console.WriteLine("Во время записи списка городов произошла ошибка. ");
                    foreach (var item in cities)
                        Console.WriteLine(item.Value);
                    break;

                case "read cl":
                    cities = DataBase.ReadCityList();
                    Console.WriteLine("Список городов успешно прочитан из базы данных");
                    break;

                case "print cl":
                    Worker.PrintCityList(cities);
                    break;

                case "":
                    break;

                default:
                    Console.WriteLine("Неизвестная команда! Введите '?' для просмотра списка доступных команд...");
                    break;
            }
        }

        /// <summary>
        /// Настраивает логгер для работы
        /// </summary>
        public static void LoggerConfig()
        {
            LoggingConfiguration config = new LoggingConfiguration();

            FileTarget fileTarget = new FileTarget();
            config.AddTarget("file", fileTarget);

            fileTarget.DeleteOldFileOnStartup = true; // Удалатья страый файл при запуске
            fileTarget.KeepFileOpen = false; //Не держать файл открытым
            fileTarget.ConcurrentWrites = true; 
            fileTarget.Encoding = Encoding.GetEncoding(1251); // Кодировка файла логов
            fileTarget.ArchiveEvery = FileArchivePeriod.Day; // Период архивирования старых логов (нет смысла, если старый удаляется при запуске)
            fileTarget.Layout = NLog.Layouts.Layout.FromString("${uppercase:${level}} | ${longdate} :\t${message}"); // Структура сообщения
            fileTarget.FileName = NLog.Layouts.Layout.FromString("${basedir}/logs/${shortdate}.log"); //Структура названия файлов
            fileTarget.ArchiveFileName = NLog.Layouts.Layout.FromString("${basedir}/logs/archives/{shortdate}.rar"); // Структура названия архивов

            LoggingRule ruleFile = new LoggingRule("*", LogLevel.Trace, fileTarget); // Минимальный уровень логгирования - Trace
            config.LoggingRules.Add(ruleFile);

            LogManager.Configuration = config;
        }

        #region EventHandlers
        /// <summary>
        /// Событие, информирующее о появлении новых полученных парсером данных
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e">Аргумент, хранящий в себе прогноз погоды и сведения об ошибках</param>
        public static void parser_WeatherParsed(object sender, WeatherParsedEventArgs e)
        {
            if (!e.HasError)
            {
                WeatherInfoList.Add((WeatherItem)e.WeatherItem);
            }
            else
            {
                Console.WriteLine("Has errors:\t" + e.HasError);
                Console.WriteLine("Error text:\t" + e.ErrorText);
                Console.WriteLine("Exception:\t" + e.Exception.Message);
            }
        }

        /// <summary>
        /// Парсер начал работу
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void parser_Started(object sender, EventArgs e)
        {
            _logger.Debug("Parser has been started");
            Console.WriteLine("Parser has been started");
        }

        /// <summary>
        /// Парсер закончил работу
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void parser_Stopped(object sender, EventArgs e)
        {
            WeatherInfoList.Clear();
            _logger.Debug("Parser has been stopped");
            Console.WriteLine("Parser has been stopped");

        }

        /// <summary>
        /// Парсер уснул
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void parser_Asleep(object sender, EventArgs e)
        {
            foreach (var item in WeatherInfoList)
            {
                DataBase.WriteWeatherItem(item);
            }
            WeatherInfoList.Clear();
            _logger.Debug("Parser goes to sleep");
            Console.WriteLine("Parser goes to sleep");

        }
        #endregion
   
        
    }
}
