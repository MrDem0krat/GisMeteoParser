using DataBaseLib;
using HtmlAgilityPack;
using NLog;
using ParserLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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
        private static Parser _parser = new Parser();

        private static Properties.Settings defSet = Properties.Settings.Default;

        /// <summary>
        /// Введены ли сведения для подключения к базе данных
        /// </summary>
        private static bool isDbInfoSetted
        {
            get
            {
                return defSet.isUserSaved && defSet.isPasswordSaved && defSet.isServerSaved && defSet.isPortSaved;
            }
        }

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
                            if (int.TryParse(TemperatureNode.InnerText.Replace("&minus;", "-"), out _temperature))
                                _weather.Temperature = _temperature;
                            else
                                throw new Exception("Error while parsing Temperature ["+ TemperatureNode.InnerText.Replace('−', '-') + "]" );
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
                            if (int.TryParse(TempFeelNode.InnerText.Replace("&minus;", "-"), out _temperatureFeel))
                                _weather.TemperatureFeel = _temperatureFeel;
                            else
                                throw new Exception("Error while parsing TemperatureFeel [" + TempFeelNode.InnerText.Replace('−', '-') + "]");
                    }
                    _logger.Trace("Parse() = success");
                }
                else
                    throw new Exception("Error: Elements not found!");
            }
            catch (Exception ex)
            {
                HasError = true;
                _logger.Error("Error occured while parsing '{0}'('{1}'). Message: {2}", dayToParse, partOfDay, ex.Message);
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
        public static Dictionary<int, string> ParseCityList()
        {
            Dictionary<int, string> result = new Dictionary<int, string>();
            bool hasError = false;
            string target = "https://www.gismeteo.ru";
            string buffer = "";
            int id;
            List<string> wrapIdList = new List<string>() { "cities-teaser", "cities1", "cities2", "cities3" };
            Regex regId = new Regex(@"[\d]{1,}");
            HtmlDocument mainPage = Parser.DownloadPage(target);

            try
            {
                foreach (string wrapID in wrapIdList)
                {
                    var citiesListNode = mainPage.GetElementbyId(wrapID).Elements("div").Where(x => x.GetAttributeValue("class", "") == "cities");
                    foreach (var cityNode in citiesListNode)
                    {
                        var ulNodes = cityNode.Elements("ul");
                        foreach (var ulNode in ulNodes)
                        {
                            var liNodes = ulNode.Elements("li");
                            foreach (var liNode in liNodes)
                            {
                                buffer = liNode.Element("a").GetAttributeValue("href", "");
                                buffer = regId.Match(buffer).Value;
                                if (int.TryParse(buffer, out id))
                                    if (!result.ContainsKey(id))
                                        result.Add(id, liNode.Element("a").InnerText);
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

            wrapIdList.Clear();

            if (!hasError)
                return result;
            else
                return new Dictionary<int, string>();
        }

        /// <summary>
        /// Подготавливает парсер к работе
        /// </summary>
        public static void Setup()
        {
            _logger.Info("Application started");
            bool isSettingDone = false;
            string _answer = string.Empty;

            _logger.Debug("Setting default parser params");
            Parser.TargetUrl = Properties.Resources.TargetSiteUrl;
            _parser.RefreshPeriod = new TimeSpan(0, 30, 0);
            _parser.ParserHandler = new Parser.ParserGetDataHandler(ParseWeatherData);
            WeatherInfoList = new List<WeatherItem>();

            _parser.WeatherParsed += parser_WeatherParsed;
            _parser.ParserStarted += parser_Started;
            _parser.ParserStopped += parser_Stopped;
            _parser.ParserAsleep += parser_Asleep;
            _parser.ErrorOccured += parser_ErrorOccured;

            do
            {
                if (isDbInfoSetted)
                {
                    DataBase.Prepare();
                    isSettingDone = true;
                }
                else
                {
                    Console.WriteLine("\nSet connection information to continue...\n");
                    var comList = new List<Command>() { Command.SetServer, Command.SetPort, Command.SetUser, Command.SetPassword };
                    ShowCommand(comList);

                    do
                    {
                        _answer = Console.ReadLine();
                        RunCommand(_answer);
                    } while (!isDbInfoSetted);
                }
            } while (!isSettingDone);

            _logger.Debug("Parser setting completed");
        }

        /// <summary>
        /// Обработчик пользовательского ввода
        /// </summary>
        /// <param name="answer">Команда. введенная пользователем</param>
        public static void RunCommand(string answer)
        {
            string value = string.Empty;
            var command = GetCommand(answer);

            switch (command)
            {
                case Command.Start:
                    _logger.Trace("Checking new cities on the gismeteo main page");
                    _parser.Cities = ParseCityList();
                    //Проверяем наличие новых городов на главной странице, добавляем новые в базу
                    var citylist = DataBase.ReadCityList();

                    var newCities = _parser.Cities.Except(citylist).ToDictionary( k => k.Key, v => v.Value);

                    if (newCities.Count != 0)
                    {
                        _logger.Debug("New cities available. Adding to database");
                        DataBase.WriteCityList(newCities);
                        _logger.Info("Added {0} cities", newCities.Count);
                        newCities.Clear();
                    }
                    //Запускаем парсер
                    if (_parser.Status != ParserStatus.Working && _parser.Status != ParserStatus.Sleeping)
                        _parser.Start();
                    else
                        Console.WriteLine("Parser already working");
                    break;

                case Command.Stop:
                    if (_parser.Status != ParserStatus.Aborted && _parser.Status != ParserStatus.Stoped)
                        _parser.Stop();
                    else
                        Console.WriteLine("Parser already stopped");
                    break;

                case Command.Status:
                    Console.WriteLine("Parser status is: {0}", _parser.Status);
                    break;

                case Command.SetPeriod:
                    value = answer.Replace(" ", "");
                    value = value.Remove(0, Command.SetPeriod.ToString().Length);
                    int minutes;
                    if (int.TryParse(value, out minutes))
                    {
                        _parser.RefreshPeriod = new TimeSpan(0, minutes, 0);
                        _logger.Debug("Parser.RefreshPeriod = {0}", _parser.RefreshPeriod.ToString());
                    }
                    else
                        Console.WriteLine("Incorrect value. Try again");
                    value = string.Empty;
                    break;

                case Command.CityId:
                    value = answer.Replace(" ", "");
                    value = value.Remove(0, Command.CityId.ToString().Length);
                    int id;
                    if (int.TryParse(value, out id))
                        Console.WriteLine("Name of {0} is {1}", id, DataBase.ReadCityName(id));
                    else
                        Console.WriteLine("Incorrect value. Try again");
                    value = string.Empty;
                    break;

                case Command.SetUser:
                    value = answer.Replace(" ", "");
                    value = value.Remove(0, Command.SetUser.ToString().Length);
                    if (value.Length != 0)
                    {
                        DataBase.Username = value;
                        DataBase.SaveSettings();
                        defSet.isUserSaved = true;
                        defSet.Save();
                    }
                    else
                        Console.WriteLine("Incorrect value. Try again");
                    value = string.Empty;
                    break;

                case Command.SetPassword:
                    ConsoleKeyInfo key;

                    Console.Write("Enter password: ");
                    do
                    {
                        key = Console.ReadKey(true);
                        if (key.Key == ConsoleKey.Enter)
                            break;
                        if (key.Key == ConsoleKey.Backspace)
                        {
                            if (value.Length != 0)
                            {
                                value = value.Remove(value.Length - 1);
                                Console.Write("\b \b");
                            }
                        }
                        else
                        {
                            value += key.KeyChar;
                            Console.Write("*");
                        }
                    } while (true);
                    Console.WriteLine();
                    DataBase.Password = value;
                    DataBase.SaveSettings();
                    defSet.isPasswordSaved = true;
                    defSet.Save();
                    Console.WriteLine("Password updated");
                    value = string.Empty;
                    break;

                case Command.SetServer:
                    value = answer.Replace(" ", "");
                    value = value.Remove(0, Command.SetServer.ToString().Length);
                    if (value.Length != 0)
                    {
                        DataBase.Server = value;
                        DataBase.SaveSettings();
                        defSet.isServerSaved = true;
                        defSet.Save();
                    }
                    else
                        Console.WriteLine("Incorrect value. Try again");
                    value = string.Empty;
                    break;

                case Command.SetPort:
                    value = answer.Replace(" ", "");
                    value = value.Remove(0, Command.SetPort.ToString().Length);
                    uint port;
                    if (uint.TryParse(value, out port))
                    {
                        DataBase.Port = port;
                        DataBase.SaveSettings();
                        defSet.isPortSaved = true;
                        defSet.Save();
                    }
                    else
                        Console.WriteLine("Incorrect value. Try again");
                    value = string.Empty;
                    break;

                case Command.Help:
                    Console.WriteLine("Available commands:\n");
                    ShowCommand();
                    break;

                case Command.Exit:
                    Program.IsRun = false;
                    if (_parser.Status == ParserStatus.Working || _parser.Status == ParserStatus.Sleeping)
                        _parser.Stop();
                    break;

                default:
                    Console.WriteLine("Unknown command. Print '{0}' to view list of available commands", Command.Help.ToString().ToLower());
                    break;
            }
        }
        /// <summary>
        /// Выводит список доступных команд
        /// </summary>
        /// <param name="commands">Список команд для отображения</param>
        private static void ShowCommand(IEnumerable<Command> commands)
        {
            if(commands.Contains(Command.Start))
                Console.WriteLine("{0,-20} - start parser", Command.Start.ToString().ToLower());
            if(commands.Contains(Command.Stop))
                Console.WriteLine("{0,-20} - stop parser", Command.Stop.ToString().ToLower());
            if (commands.Contains(Command.Status))
                Console.WriteLine("{0,-20} - parser status", Command.Status.ToString().ToLower());
            if (commands.Contains(Command.SetPeriod))
                Console.WriteLine("{0,-20} - set parser starting period", Command.SetPeriod.ToString().ToLower() + " [minutes]");
            if (commands.Contains(Command.CityId))
                Console.WriteLine("{0,-20} - get city name by id", Command.CityId.ToString().ToLower() + " [id]");
            if (commands.Contains(Command.SetUser))
                Console.WriteLine("{0,-20} - set DB username", Command.SetUser.ToString().ToLower() + " [username]");
            if (commands.Contains(Command.SetPassword))
                Console.WriteLine("{0,-20} - set DB password", Command.SetPassword.ToString().ToLower());
            if (commands.Contains(Command.SetServer))
                Console.WriteLine("{0,-20} - set DB server", Command.SetServer.ToString().ToLower() + " [server]");
            if (commands.Contains(Command.SetPort))
                Console.WriteLine("{0,-20} - set DB port", Command.SetPort.ToString().ToLower() + " [port]");
            if (commands.Contains(Command.Help))
                Console.WriteLine("{0,-20} - shows list of available commands", Command.Help.ToString().ToLower());
            if (commands.Contains(Command.Exit))
                Console.WriteLine("{0,-20} - stop parser and exit\n", Command.Exit.ToString().ToLower());
        }
        /// <summary>
        /// Выводит список всех доступных команд
        /// </summary>
        public static void ShowCommand()
        {
            List<Command> list = new List<Command>()
            {
                Command.Start,
                Command.Stop,
                Command.Status,
                Command.CityId,
                Command.SetUser,
                Command.SetPassword,
                Command.SetServer,
                Command.SetPort,
                Command.Help,
                Command.Exit,
                Command.SetPeriod
            };
            ShowCommand(list);
        }

        /// <summary>
        /// Список доступных команд
        /// </summary>
        public enum Command
        {
            /// <summary>
            /// Команда не распознана
            /// </summary>
            None = 0,
            /// <summary>
            /// Запустить парсер
            /// </summary>
            Start = 1,
            /// <summary>
            /// Остановить парсер
            /// </summary>
            Stop = 2,
            /// <summary>
            /// Статус парсера
            /// </summary>
            Status = 3,
            /// <summary>
            /// Получить название города из базы по его ID
            /// </summary>
            CityId = 4,
            /// <summary>
            /// Задать имя пользователя БД
            /// </summary>
            SetUser = 5,
            /// <summary>
            /// Задать пароль пользователя БД
            /// </summary>
            SetPassword = 6,
            /// <summary>
            /// Задать сервер БД
            /// </summary>
            SetServer = 7,
            /// <summary>
            /// Задать порт БД
            /// </summary>
            SetPort = 8,
            /// <summary>
            /// Отобразить список доступных команд
            /// </summary>
            Help = 9,
            /// <summary>
            /// Завершить работу приложения
            /// </summary>
            Exit = 10,
            /// <summary>
            /// Задать период запуска парсера в минутах
            /// </summary>
            SetPeriod = 11
        }

        /// <summary>
        /// Проверяет корректность и возвращает введенную пользователем команду
        /// </summary>
        /// <param name="answer">Пользовательский ввод</param>
        /// <returns></returns>
        private static Command GetCommand(string answer)
        {
            Command result;
            //Список комманд принимающих значение 
            List<Command> complexCommands = new List<Command>();
            complexCommands.Add(Command.CityId);
            complexCommands.Add(Command.SetUser);
            complexCommands.Add(Command.SetServer);
            complexCommands.Add(Command.SetPort);
            complexCommands.Add(Command.SetPeriod);
            
            var commandId = (from cid in (int[])Enum.GetValues(typeof(Command)) where answer.StartsWith(Enum.GetName(typeof(Command), cid).ToLower()) select cid).FirstOrDefault();
            
            #region SwitchCommand
            switch (commandId)
            {
                case (int)Command.Start:
                    result = Command.Start;
                    break;
                case (int)Command.Stop:
                    result = Command.Stop;
                    break;
                case (int)Command.Status:
                    result = Command.Status;
                    break;
                case (int)Command.SetPeriod:
                    result = Command.SetPeriod;
                    break;
                case (int)Command.CityId:
                    result = Command.CityId;
                    break;
                case (int)Command.SetUser:
                    result =  Command.SetUser;
                    break;
                case (int)Command.SetPassword:
                    result = Command.SetPassword;
                    break;
                case (int)Command.SetServer:
                    result = Command.SetServer;
                    break;
                case (int)Command.SetPort:
                    result = Command.SetPort;
                    break;
                case (int)Command.Help:
                    result = Command.Help;
                    break;
                case (int)Command.Exit:
                    result = Command.Exit;
                    break;
                default:
                    result = Command.None;
                    break;
            }
            #endregion

            if(!complexCommands.Contains(result))
            {
                //дополнительная проверка правильности ввода команды
                var anotherID = (from cid in (int[])Enum.GetValues(typeof(Command)) where Enum.GetName(typeof(Command), cid).ToLower() == answer select cid).FirstOrDefault();
                if (anotherID == 0)
                    return Command.None;
            }
            return result;
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
        /// Парсер запущен
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void parser_Started(object sender, EventArgs e)
        {
            _logger.Debug("Parser status: {0}", _parser.Status);
            Console.WriteLine("Parser has been started [{0}]", DateTime.Now.ToLongTimeString());
        }

        /// <summary>
        /// Парсер остановлен
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void parser_Stopped(object sender, EventArgs e)
        {
            WeatherInfoList.Clear();
            _logger.Debug("Parser status: {0}", _parser.Status);
            Console.WriteLine("Parser has been stopped [{0}]", DateTime.Now.ToLongTimeString());
        }

        /// <summary>
        /// Парсер уснул
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void parser_Asleep(object sender, EventArgs e)
        {
            DataBase.WriteWeather(WeatherInfoList);
            _logger.Debug("Database: Added {0} items", WeatherInfoList.Count);
            _logger.Debug("Parser status: {0}", _parser.Status);
            Console.WriteLine("Parser goes to sleep [{0}]", DateTime.Now.ToLongTimeString());

            WeatherInfoList.Clear();
        }

        /// <summary>
        /// Произошла ошибка во время парсинга
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void parser_ErrorOccured(object sender, Exception e)
        {
            _logger.Warn("Error was occured while parsing. Message: {0}", e.Message);
        }
        #endregion
    }
}