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
            _logger.Debug("Parser has been stopped");
            Console.WriteLine("Parser has been stopped");

        }
        public static void parser_Asleep(object sender, EventArgs e)
        {
            _logger.Debug("Parser goes to sleep");
            Console.WriteLine("Parser goes to sleep");

        }

        #endregion
        /*#region Database from yandex
        public static class WeatherDatabase
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        private static string BaseName = "weather";

        private static byte[] additionalEntropy
        {
            get { return Convert.FromBase64String(WeatherLib.Properties.Settings.Default.AdditionalEntropy); }
        }

        public static string Server 
        {
            get { return WeatherLib.Properties.Settings.Default.Server; }
            set { WeatherLib.Properties.Settings.Default.Server = value; } 
        }
        public static uint Port 
        {
            get { return WeatherLib.Properties.Settings.Default.Port; }
            set { WeatherLib.Properties.Settings.Default.Port = value; }
        }
        public static string User 
        {
            get { return WeatherLib.Properties.Settings.Default.UserID; }
            set { WeatherLib.Properties.Settings.Default.UserID = value; }  
        }
        public static string Password 
        { 
            private get
            {
                return UnprotectPassword(WeatherLib.Properties.Settings.Default.PassWord);
            } 
            set
            {
                WeatherLib.Properties.Settings.Default.PassWord = ProtectPassword(value);
            } 
        }

        // Шифрование пароля
        private static string ProtectPassword(string src)
        {
            try
            {
                return Convert.ToBase64String(ProtectedData.Protect(Encoding.UTF8.GetBytes(src), additionalEntropy, DataProtectionScope.CurrentUser));
            }
            catch (CryptographicException e)
            {
                logger.Debug(String.Format("Не удалось зашифровать данные: {0}", e.ToString()));
                return null;
            }
        }
        private static string UnprotectPassword(string src)
        {
            try
            {
                 return Encoding.UTF8.GetString(ProtectedData.Unprotect(Convert.FromBase64String(src),additionalEntropy,DataProtectionScope.CurrentUser));
            }
            catch (CryptographicException e)
            {
                logger.Trace("Ошибка при подключении: Повторите ввод пароля MySQL в настрйоках приложения");
                logger.Debug(String.Format("Не удалось расшифровать данные: {0}",e.ToString()));
                return null;
            }
        }

        // Подключение к базе данных
        private static MySqlConnection Connect(string _DBName)
        {
            MySqlConnectionStringBuilder connectionBuilder = new MySqlConnectionStringBuilder();
            connectionBuilder.Password = Password;
            connectionBuilder.UserID = User;
            connectionBuilder.Server = Server;
            connectionBuilder.Port = Port;
            connectionBuilder.Database = _DBName;

            MySqlConnection connection;
            connection = new MySqlConnection();
            connection.ConnectionString = connectionBuilder.ConnectionString;
            try
            {
                connection.Open();
                logger.Debug(String.Format("Установлено соединение с базой данных '{0}' на сервере '{1}'", connection.Database, Server));
            }
            catch (Exception e)
            {
                logger.ErrorException(String.Format("При подключении к базе данных '{0}' на сервере '{1}' произошла ошибка: ", connection.Database, Server), e);
                throw e; 
            }
            return connection;
        }
        private static void Disconnect(MySqlConnection _connect)
        {
            string baseName = Regex.Match(_connect.ConnectionString, @"(?<=database=).*?(?=\;|$)").Value;
            _connect.Close();
            logger.Debug(String.Format("Соединение с базой данных '{0}' закрыто.", baseName));
        }

        // Проверка наличия базы на серевере и ее создание в случае отсутствия
        private static bool CheckDatabase()
        {
            MySqlConnection connect = new MySqlConnection();
            MySqlCommand command;
            MySqlDataReader reader;
            List<string> result = new List<string>();
            string dbName = "information_schema";
            string commandCheck = "SHOW DATABASES";
            string commandCreate = "CREATE DATABASE " + BaseName;
            try
            {
                connect = Connect(dbName);
                command = new MySqlCommand(commandCheck, connect);
                reader = command.ExecuteReader();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        result.Add(reader.GetString(0));
                    }
                    reader.Close();
                    if (result.IndexOf(BaseName) < 0)
                    {
                        logger.Debug(String.Format("Необходимая база '{0}' отсутствует на сервере.", BaseName));
                        command = new MySqlCommand(commandCreate, connect);
                        command.ExecuteNonQuery();
                        logger.Debug(String.Format("Новая база '{0}' успешно создана на сервере '{1}'.", BaseName, Server));
                    }
                    else
                    {
                        logger.Debug(String.Format("База данных '{0}' уже имеется на сервере", BaseName));
                    }
                }
            }
            catch (Exception e)
            {
                logger.Error(String.Format("При создании новой базы данных '{0}' произошла ошибка: {1}|{2}|{3}", BaseName, e.Source, e.TargetSite, e.Message));
                return false;
            }
            finally
            {
                Disconnect(connect);
            }
            return true;
        }
        private async static Task<bool> CheckDatabaseAsync()
        {
            MySqlConnection connection = new MySqlConnection();
            MySqlCommand command;
            MySqlDataReader reader;
            List<string> result = new List<string>();
            string dbName = "information_schema";
            string commandCheck = "SHOW DATABASES";
            string commandCreate = "CREATE DATABASE " + BaseName;
            try 
            {
                connection = Connect(dbName);
                command = new MySqlCommand(commandCheck, connection);
                reader = await Task<MySqlDataReader>.Factory.StartNew(() => { return command.ExecuteReader(); });
                if (reader.HasRows)
                {
                    await Task.Factory.StartNew(() =>
                    {
                        while (reader.Read())
                        {
                            result.Add(reader.GetString(0));
                        }
                        reader.Close();
                    });
                    if (result.IndexOf(BaseName) < 0)
                    {
                        logger.Debug(String.Format("Необходимая база '{0}' отсутствует на сервере.", BaseName));
                        command = new MySqlCommand(commandCreate, connection);
                        await command.ExecuteNonQueryAsync();
                        logger.Debug(String.Format("Новая база '{0}' успешно создана на сервере '{1}'.", BaseName, Server));
                    }
                    else
                    {
                        logger.Debug(String.Format("База данных '{0}' уже имеется на сервере", BaseName));
                    }
                }
            }
            catch (MySqlException e)
            {
                logger.Error(String.Format("При создании новой базы данных '{0}' произошла ошибка: {1}|{2}|{3}", BaseName, e.Source, e.TargetSite, e.Message));
                return false;
            }
            finally
            {
                Disconnect(connection);
            }
            return true;
        }

        // Проверка наличия необходимой таблицы в базе и ее создание в случае отсутствия
        private static bool CheckTable()
        {
            MySqlConnection connect = new MySqlConnection();
            MySqlCommand command;
            MySqlDataReader reader;
            string comCreate = String.Format(@"CREATE TABLE {0}.{1} (
                                    ID int(10) UNSIGNED NOT NULL AUTO_INCREMENT,
                                    Date date NOT NULL,
                                    Time time NOT NULL,
                                    PartOfDay varchar(255) NOT NULL,
                                    Temperature int(11) NOT NULL,
                                    `Condition` varchar(255) NOT NULL,
                                    Type varchar(255) NOT NULL,
                                    TypeShort varchar(255) NOT NULL,
                                    WindDirection varchar(255) NOT NULL,
                                    WindSpeed varchar(255) NOT NULL,
                                    Humidity int(11) NOT NULL,
                                    Pressure int(11) NOT NULL,
                                    PRIMARY KEY (ID)
                                )
                                ENGINE = INNODB
                                AUTO_INCREMENT = 1
                                CHARACTER SET cp1251
                                COLLATE cp1251_general_ci;",
                                BaseName, Properties.Settings.Default.CityNameEng);
            string comCheck = @"SHOW TABLES";
            try
            {
                connect = Connect(BaseName);
                command = new MySqlCommand(comCheck, connect);
                reader = command.ExecuteReader();
                List<string> resultList = new List<string>();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        resultList.Add(reader.GetString(0));
                    }
                }
                reader.Close();
                if (resultList.IndexOf(Properties.Settings.Default.CityNameEng) < 0)
                {
                    logger.Debug(String.Format("Необходимая таблица '{0}' отсутствует в базе '{1}'.", Properties.Settings.Default.CityNameEng, BaseName));
                    command = new MySqlCommand(comCreate, connect);
                    command.ExecuteReader();
                    logger.Trace(String.Format("Новая таблица '{0}' в базе данных '{1}' успешно создана.", Properties.Settings.Default.CityNameEng, BaseName));
                }
                else
                {
                    logger.Debug(String.Format("Таблица '{0}' уже имеется в базе '{1}'", Properties.Settings.Default.CityNameEng, BaseName));
                }
            }
            catch (Exception e)
            {
                logger.Error(String.Format("При создании новой таблицы '{0}' произошла ошибка: {1}|{2}|{3}", Properties.Settings.Default.CityNameEng, e.Source, e.TargetSite, e.Message));
                return false;
            }
            finally
            {
                Disconnect(connect);
            }
            return true;
        }
        private async static Task<bool> CheckTableAsync()
        {
            MySqlConnection connection = new MySqlConnection();
            MySqlCommand command;
            MySqlDataReader reader;
            string comCreate = String.Format(@"CREATE TABLE {0}.{1} (
                                    ID int(10) UNSIGNED NOT NULL AUTO_INCREMENT,
                                    Date date NOT NULL,
                                    Time time NOT NULL,
                                    PartOfDay varchar(255) NOT NULL,
                                    Temperature int(11) NOT NULL,
                                    `Condition` varchar(255) NOT NULL,
                                    Type varchar(255) NOT NULL,
                                    TypeShort varchar(255) NOT NULL,
                                    WindDirection varchar(255) NOT NULL,
                                    WindSpeed varchar(255) NOT NULL,
                                    Humidity int(11) NOT NULL,
                                    Pressure int(11) NOT NULL,
                                    PRIMARY KEY (ID)
                                )
                                ENGINE = INNODB
                                AUTO_INCREMENT = 1
                                CHARACTER SET cp1251
                                COLLATE cp1251_general_ci;",
                                BaseName, Properties.Settings.Default.CityNameEng);
            string comCheck = @"SHOW TABLES";
            try
            {
                connection = Connect(BaseName);
                command = new MySqlCommand(comCheck, connection);
                reader = await Task<MySqlDataReader>.Factory.StartNew(() => { return command.ExecuteReader(); }); 
                List<string> resultList = new List<string>();
                if (reader.HasRows)
                {
                    await Task.Factory.StartNew(() =>
                        {
                            while (reader.Read())
                            {
                                resultList.Add(reader.GetString(0));
                            }
                        });
                }
                reader.Close();
                if (resultList.IndexOf(Properties.Settings.Default.CityNameEng) < 0)
                {
                    logger.Debug(String.Format("Необходимая таблица '{0}' отсутствует в базе '{1}'.", Properties.Settings.Default.CityNameEng, BaseName));
                    command = new MySqlCommand(comCreate, connection);
                    await command.ExecuteReaderAsync();
                    logger.Trace(String.Format("Новая таблица '{0}' в базе данных '{1}' успешно создана.", Properties.Settings.Default.CityNameEng, BaseName));
                }
                else
                {
                    logger.Debug(String.Format("Таблица '{0}' уже имеется в базе '{1}'", Properties.Settings.Default.CityNameEng, BaseName));
                }
            }
            catch (MySqlException e)
            {
                logger.Error(String.Format("При создании новой таблицы '{0}' произошла ошибка: {1}|{2}|{3}", Properties.Settings.Default.CityNameEng, e.Source, e.TargetSite, e.Message));
                return false;
            }
            finally
            {
                Disconnect(connection);
            }
            return true;
        }
       
        /// <summary>
        /// Проверяет наличие на MySQL-сервере необходимых для хранения истории базы данных
        /// и таблиц. Если необходимые компоненты отсутствуют - создает новые.
        /// </summary>
        /// <returns>
        /// Возвращает true если все компоненты уже имеются или вновь созданы.
        /// В случае ошибки возвращает false.
        /// </returns>
        // Проверка наличия необходимых компонентов на сервере
        public static bool Check()
        {
            if (CheckDatabase() && CheckTable())
            {
                logger.Debug(String.Format("Проверка наличия необходимых компонентов на сервере '{0}' прошла успешно.", Server));
                return true;
            }
            else
            {
                logger.Error(String.Format("В ходе проверки наличия необходимых компонентов на сервере '{0}' произошла ошибка.", Server));
                return false;
                // добавить выброс исключения
            }
        }
        public async static Task<bool> CheckAsync()
        {
            if(await CheckDatabaseAsync() && await CheckTableAsync())
            {
                logger.Debug(String.Format("Проверка наличия необходимых компонентов на сервере '{0}' прошла успешно", Server));
                return true;
            }
            else
            {
                logger.Error(String.Format("В ходе проверки наличия необходимых компонентов на сервере '{0}' произошла ошибка", Server));
                return false;
            }
        }

        //Запись показаний в базу
        public static void Write(Weather data)
        {
            MySqlCommand command = new MySqlCommand();
            command.CommandText = String.Format(@"INSERT INTO {0}(ID,Date,Time,PartOfDay,Temperature,`Condition`,Type,TypeShort,WindDirection,WindSpeed,Humidity,Pressure)
                                    VALUES(@ID,
                                           @Date,
                                           @Time,
                                           @PartOfDay,
                                           @Temperature,
                                           @Condition,
                                           @Type,
                                           @TypeShort,
                                           @WindDirection,
                                           @WindSpeed,
                                           @Humidity,
                                           @Pressure
                                          )", Properties.Settings.Default.CityNameEng);
            command.Parameters.Add("@ID", MySqlDbType.Int32);
            command.Parameters["@ID"].Value = null;
            command.Parameters.Add("@Date", MySqlDbType.Date);
            command.Parameters["@Date"].Value = data.Date.Date;
            command.Parameters.Add("@Time", MySqlDbType.Time);
            command.Parameters["@Time"].Value = new TimeSpan(DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second);
            command.Parameters.Add("@PartOfDay", MySqlDbType.VarChar);
            command.Parameters["@PartOfDay"].Value = data.PartOfDay;
            command.Parameters.Add("@Temperature", MySqlDbType.UInt32);
            command.Parameters["@Temperature"].Value = data.Temperature;
            command.Parameters.Add("@Condition", MySqlDbType.VarChar);
            command.Parameters["@Condition"].Value = data.Condition;
            command.Parameters.Add("@Type", MySqlDbType.VarChar);
            command.Parameters["@Type"].Value = data.Type;
            command.Parameters.Add("@TypeShort", MySqlDbType.VarChar);
            command.Parameters["@TypeShort"].Value = data.TypeShort;
            command.Parameters.Add("@WindDirection", MySqlDbType.VarChar);
            command.Parameters["@WindDirection"].Value = data.WindDirection;
            command.Parameters.Add("@WindSpeed", MySqlDbType.VarChar);
            command.Parameters["@WindSpeed"].Value = data.WindSpeed;
            command.Parameters.Add("@Humidity", MySqlDbType.Int32);
            command.Parameters["@Humidity"].Value = data.Humidity;
            command.Parameters.Add("@Pressure", MySqlDbType.Int32);
            command.Parameters["@Pressure"].Value = data.Pressure;
            try
            {
                command.Connection = Connect(BaseName);
                command.ExecuteNonQuery();
                logger.Debug(String.Format("Запись {0}:{1} успешно добавлена в базу", data.Date.ToShortDateString(), data.PartOfDay));
            }
            catch (Exception e)
            {
                logger.Error(String.Format("При добавлении записи {0}:{1} произошла ошибка: {2}|{3}|{4}", data.Date.ToShortDateString(), data.PartOfDay, e.Source, e.TargetSite, e.Message));
            }
            finally
            {
                Disconnect(command.Connection);
            }
        }
        public async static Task WriteAsync(Weather data)
        {
            MySqlCommand command = new MySqlCommand();
            command.CommandText = String.Format(@"INSERT INTO {0}(ID,Date,Time,PartOfDay,Temperature,`Condition`,Type,TypeShort,WindDirection,WindSpeed,Humidity,Pressure)
                                    VALUES(@ID,
                                           @Date,
                                           @Time,
                                           @PartOfDay,
                                           @Temperature,
                                           @Condition,
                                           @Type,
                                           @TypeShort,
                                           @WindDirection,
                                           @WindSpeed,
                                           @Humidity,
                                           @Pressure
                                          )", Properties.Settings.Default.CityNameEng);
            command.Parameters.Add("@ID", MySqlDbType.Int32);
            command.Parameters["@ID"].Value = null;
            command.Parameters.Add("@Date", MySqlDbType.Date);
            command.Parameters["@Date"].Value = data.Date.Date;
            command.Parameters.Add("@Time", MySqlDbType.Time);
            command.Parameters["@Time"].Value = new TimeSpan(DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second);
            command.Parameters.Add("@PartOfDay", MySqlDbType.VarChar);
            command.Parameters["@PartOfDay"].Value = data.PartOfDay;
            command.Parameters.Add("@Temperature", MySqlDbType.UInt32);
            command.Parameters["@Temperature"].Value = data.Temperature;
            command.Parameters.Add("@Condition", MySqlDbType.VarChar);
            command.Parameters["@Condition"].Value = data.Condition;
            command.Parameters.Add("@Type", MySqlDbType.VarChar);
            command.Parameters["@Type"].Value = data.Type;
            command.Parameters.Add("@TypeShort", MySqlDbType.VarChar);
            command.Parameters["@TypeShort"].Value = data.TypeShort;
            command.Parameters.Add("@WindDirection", MySqlDbType.VarChar);
            command.Parameters["@WindDirection"].Value = data.WindDirection;
            command.Parameters.Add("@WindSpeed", MySqlDbType.VarChar);
            command.Parameters["@WindSpeed"].Value = data.WindSpeed;
            command.Parameters.Add("@Humidity", MySqlDbType.Int32);
            command.Parameters["@Humidity"].Value = data.Humidity;
            command.Parameters.Add("@Pressure", MySqlDbType.Int32);
            command.Parameters["@Pressure"].Value = data.Pressure;
            try
            {
                command.Connection = Connect(BaseName);
                await command.ExecuteNonQueryAsync();
                logger.Debug(String.Format("Запись {0}:{1} успешно добавлена в базу", data.Date.ToShortDateString(), data.PartOfDay));
            }
            catch (MySqlException e)
            {
                logger.Error(String.Format("При добавлении записи {0}:{1} произошла ошибка: {2}|{3}|{4}", data.Date.ToShortDateString(), data.PartOfDay, e.Source, e.TargetSite, e.Message));
            }
            finally
            {
                Disconnect(command.Connection);
            }
        }

    }
        #endregion*/
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
