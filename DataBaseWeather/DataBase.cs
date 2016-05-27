using MySql.Data.MySqlClient;
using NLog;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Gismeteo.Weather;
using Gismeteo.City;

namespace Database
{
    /// <summary>
    /// Обслуживающий класс для работы с базой данных
    /// </summary>
    public static class DataBase
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Перечисление доступных для создания типов таблиц базы данных
        /// </summary>
        public enum TableType
        {
            /// <summary>
            /// Таблица для хранения прогноза погоды
            /// </summary>
            Weather,
            /// <summary>
            /// Таблица для хранения списка городов
            /// </summary>
            City
        }

        /// <summary>
        /// Структура таблицы Weather
        /// </summary>
        private enum WeatherTableColumn
        {
            ID = 0,
            City_ID,
            Date,
            DayPart,
            Temperature,
            TemperatureFeel,
            Condition,
            TypeImage,
            Humidity,
            Pressure,
            WindDirection,
            WindSpeed,
            RefreshTime
        }
        private enum CityTableColumn
        {
            ID = 0,
            Name
        }

        /// <summary>
        /// Добавочная энтропия для кодирования пароля от пользователя БД
        /// </summary>
        private static byte[] additionalEntropy
        {
            get { return Convert.FromBase64String(Properties.Settings.Default.AdditionalEntropy); }
        }
        /// <summary>
        /// ConnectionString для подлкючения к MySql-серверу
        /// </summary>
        public static string ConnectionString { private get; set; }

        /// <summary>
        /// Зашифровать пароль
        /// </summary>
        /// <param name="_source">Незашифрованный пароль</param>
        /// <returns></returns>
        private static string EncodePassword(string _source)
        {
            try
            {
                _logger.Trace("Trying to encode password");
                var result = Convert.ToBase64String(ProtectedData.Protect(Encoding.UTF8.GetBytes(_source), additionalEntropy, DataProtectionScope.CurrentUser));
                _logger.Trace("Password encoded succesfully");
                return result;
            }
            catch (CryptographicException ex)
            {
                _logger.Error("Error occurred while connecting to database: can't encode password, {0}", ex.Message);
                return string.Empty;
            }
        }
        /// <summary>
        /// Расшифровать пароль
        /// </summary>
        /// <param name="_source">Зашифрованный пароль</param>
        /// <returns></returns>
        private static string DecodePassword(string _source)
        {
            try
            {
                _logger.Trace("Trying to decode password");
                var result = Encoding.UTF8.GetString(ProtectedData.Unprotect(Convert.FromBase64String(_source), additionalEntropy, DataProtectionScope.CurrentUser));
                _logger.Trace("Password decoded succesfully");
                return result;
            }
            catch (CryptographicException ex)
            {
                _logger.Error("Error occurred while connecting to database: {0}", ex.Message);
                return string.Empty;
            }
        }

        /// <summary>
        /// Подключается к указанной базе данных на сервере
        /// </summary>
        /// <param name="database">Имя базы данных</param>
        /// <returns></returns>
        public static MySqlConnection Connect(string database = "information_schema")
        {
            MySqlConnection connection = new MySqlConnection();
            MySqlConnectionStringBuilder connectionBuilder = new MySqlConnectionStringBuilder();

            connectionBuilder.ConnectionString = ConnectionString;
            connectionBuilder.Database = database.ToLower();

            connection.ConnectionString = connectionBuilder.ConnectionString;
            try
            {
                if (connection.Database != "information_schema")
                {
                    _logger.Trace("Trying to connect to '{0}.{1}'", connectionBuilder.Server, connection.Database);
                    if (CheckDataBase(connection.Database))
                    {
                        if(connection.State != ConnectionState.Open)
                            connection.Open();
                        _logger.Trace("'{0}'.'{1}' - connected", connectionBuilder.Server, connection.Database);
                    }
                    else
                        throw new Exception("Database not found");
                }
                return connection;
            }
            catch (Exception ex)
            {
                _logger.Warn("Error occurred while connecting '{1}.{0}'. Message: {2}", connection.Database, connectionBuilder.Server, ex.Message);
                if (connection.State == ConnectionState.Open || connection.State == ConnectionState.Connecting)
                    connection.Close();
                connection = new MySqlConnection();
            }
            return connection;
        }

        /// <summary>
        /// Отключается от базы данных
        /// </summary>
        /// <param name="connection">Подключение к базе данных</param>
        public static void Disconnect(MySqlConnection connection)
        {
            if(connection.State != ConnectionState.Closed)
            {
                connection.Close();
                _logger.Trace("Disconnected: '{0}'", connection.Database);
            }
        }

        /// <summary>
        /// Проверяет наличие базы данных на сервере
        /// </summary>
        /// <param name="database">Имя искомой базы данных</param>
        /// <param name="connection">Подключение к серверу</param>
        /// <returns></returns>
        public static bool CheckDataBase(string database, MySqlConnection connection)
        {
            MySqlDataReader reader;
            database = database.ToLower();
            MySqlCommand command = new MySqlCommand("SHOW DATABASES", connection);
            bool result = false;
            List<string> databases = new List<string>();

            _logger.Trace("Checking database '{0}'", database);
            using (connection)
            {
                try
                {
                    if (connection.State != ConnectionState.Open)
                        connection.Open();
                    reader = command.ExecuteReader();
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            databases.Add(reader.GetString(0));
                        }
                        reader.Close();
                        if (databases.Contains(database))
                        {
                            _logger.Trace("Check database '{0}' - success", database);
                            result = true;
                        }
                        else
                            _logger.Trace("Check database '{0}' - not found", database);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error("Error occurred while checking database. Message: {0}", ex.Message);
                    result = false;
                }
                Disconnect(connection);
                return result;
            }
        }
        /// <summary>
        /// Проверяет наличие базы данных на сервере
        /// </summary>
        /// <param name="database">Имя базы данных для проверки</param>
        /// <returns></returns>
        public static bool CheckDataBase(string database)
        {
            MySqlConnection connection = Connect();
            return CheckDataBase(database, connection);
        }

        /// <summary>
        /// Проверяет наличие таблицы в базе данных
        /// </summary>
        /// <param name="table">Имя искомой таблицы</param>
        /// <param name="connection">Подключение к базе данных, в которой необходимо произвести поиск</param>
        /// <returns></returns>
        public static bool CheckTable(TableType table, MySqlConnection connection)
        {
            using (connection)
            {
                MySqlCommand command = new MySqlCommand("SHOW TABLES", connection);
                List<string> tables = new List<string>();
                bool result = false;
                string _tableName = string.Empty;

                switch (table)
                {
                    case TableType.Weather:
                        _tableName = Properties.Settings.Default.TableWeatherName.ToLower();
                        break;
                    case TableType.City:
                        _tableName = Properties.Settings.Default.TableCityName.ToLower();
                        break;
                }

                _logger.Trace("Checking table '{0}.{1}'", connection.Database, _tableName);
                try
                {
                    if (connection.State != ConnectionState.Open)
                        connection.Open();
                    var reader = command.ExecuteReader();
                    if (reader.HasRows)
                        while (reader.Read())
                            tables.Add(reader.GetString(0));
                    reader.Close();
                    if (tables.Contains(_tableName))
                    {
                        _logger.Trace("Check table '{0}.{1}' - success", connection.Database, _tableName);
                        result = true;
                    }
                    else
                    {
                        _logger.Trace("Check table '{0}.{1}' - not found", connection.Database, _tableName);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error("Error occurred while checking table. Message: {0}", ex.Message);
                }
                Disconnect(connection);
                return result;
            }
        }

        /// <summary>
        /// Создает новую базу на сервере
        /// </summary>
        /// <param name="database">Имя создаваемой базы данных</param>
        public static void CreateDataBase(string database)
        {
            using (MySqlConnection connection = Connect())
            {
                database = database.ToLower();
                var comText = string.Format("CREATE DATABASE IF NOT EXISTS {0} CHARACTER SET cp1251 COLLATE cp1251_general_ci", database);
                MySqlCommand command = new MySqlCommand();

                _logger.Trace("Trying to create new database '{0}'", database);
                try
                {
                    if (CheckDataBase(database, connection))
                        _logger.Trace("Create DataBase '{0}' -  already exists", database);
                    else
                    {
                        if (connection.State != ConnectionState.Open)
                            connection.Open();
                        command.Connection = connection;
                        command.CommandText = comText;
                        command.ExecuteNonQuery();
                        if (CheckDataBase(database, connection))
                            _logger.Debug("Created DataBase: '{0}'", database);
                        else
                            throw new Exception("'Create' command was sent, but database wasn't created");
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error("Error occurred while creating database '{0}'. Message: {1}", database, ex.Message);
                }
                Disconnect(connection);
            }
        }

        /// <summary>
        /// Создает новую таблицу в базе данных
        /// </summary>
        /// <param name="connection">Соединение с базой данных, в которой требуется создать таблицу</param>
        /// <param name="table">Тип создаваемой таблицы</param>
        public static void CreateTable(TableType table, MySqlConnection connection)
        {
            MySqlCommand command = new MySqlCommand();
            string _tableName = string.Empty;

            using (connection)
            {
                try
                {
                    switch (table)
                    {
                        case TableType.Weather:
                            _tableName = Properties.Settings.Default.TableWeatherName.ToLower();
                            command = new MySqlCommand(string.Format(@"CREATE TABLE IF NOT EXISTS {0}.{1} (
                                    ID int(10) UNSIGNED NOT NULL AUTO_INCREMENT,
                                    City_ID int(10) UNSIGNED NULL,
                                    Date date NULL,
                                    DayPart varchar(20) DEFAULT NULL,
                                    Temperature int(4) DEFAULT NULL,
                                    TemperatureFeel int(4) DEFAULT NULL,
                                    `Condition` varchar(255) DEFAULT NULL,    
                                    TypeImage varchar(25) DEFAULT NULL,
                                    Humidity int(4) UNSIGNED DEFAULT NULL,
                                    Pressure int(4) UNSIGNED DEFAULT NULL,
                                    WindDirection varchar(30) DEFAULT NULL,
                                    WindSpeed float UNSIGNED DEFAULT NULL,
                                    RefreshTime datetime DEFAULT NULL,
                                    PRIMARY KEY (ID)
                                    )
                                ENGINE = INNODB
                                AUTO_INCREMENT = 1
                                CHARACTER SET cp1251
                                COLLATE cp1251_general_ci;",
                                        connection.Database, _tableName), connection);
                            break;
                        case TableType.City:
                            _tableName = Properties.Settings.Default.TableCityName.ToLower();
                            command = new MySqlCommand(string.Format(@"CREATE TABLE IF NOT EXISTS {0}.{1} (
                                    ID int(10) UNSIGNED NOT NULL, 
                                    Name varchar(50) CHARACTER SET cp1251 COLLATE cp1251_general_ci DEFAULT NULL,
                                    PRIMARY KEY (ID)
                                    )
                                ENGINE = INNODB
                                AUTO_INCREMENT = 1
                                CHARACTER SET cp1251
                                COLLATE cp1251_general_ci;",
                                        connection.Database, _tableName), connection);
                            break;
                    }

                    _logger.Trace("Trying to create new table '{0}.{1}'", connection.Database, _tableName);
                    if (!CheckTable(table, connection))
                    {
                        if (connection.State != ConnectionState.Open)
                            connection.Open();
                        command.ExecuteNonQuery();
                        if (CheckTable(table, connection))
                            _logger.Debug("Created Table: '{0}.{1}'", connection.Database, _tableName);
                        else
                            throw new Exception("'Create' command was sent, but table wasn't created");
                    }
                    else
                        _logger.Trace("Create Table '{0}.{1}' - already exists", connection.Database, _tableName);
                }
                catch (Exception ex)
                {
                    _logger.Warn("Error occurred while creating table. Message: {3}", connection.Database, _tableName, ex.Message);
                }
                Disconnect(connection);
            }
        }

        /// <summary>
        /// Подготавливает необходимую для работы базу данных и набор таблиц
        /// </summary>
        /// <returns></returns>
        public static void Prepare()
        {
            _logger.Trace("Preparing database for working");
            CreateDataBase(Properties.Settings.Default.DBName);

            using (var connection = Connect(Properties.Settings.Default.DBName))
            {
                CreateTable(TableType.Weather, connection);
                CreateTable(TableType.City, connection);
                Disconnect(connection);
            }
            _logger.Debug("Prepare Database '{0}' - success", Properties.Settings.Default.DBName);
        }

        /// <summary>
        /// Записывает сведения о погоде в базу данных
        /// </summary>
        /// <param name="weatherList">Массив прогнозов, которй требуется записать в базу</param>
        /// <returns></returns>
        public static bool WriteWeather(IEnumerable<IWeatherItem> weatherList)
        {
            MySqlCommand command = new MySqlCommand();
            bool result = false;
            WeatherItem errorOn = new WeatherItem();
            var commandText = string.Format(@"INSERT INTO {0} (ID, City_ID, Date, DayPart, Temperature, TemperatureFeel, `Condition`, TypeImage, Humidity, Pressure, WindDirection, WindSpeed, RefreshTime)
                                                                     VALUES(@ID,
                                                                            @City_ID,
                                                                            @Date,
                                                                            @DayPart,        
                                                                            @Temperature,
                                                                            @TemperatureFeel,
                                                                            @Condition,
                                                                            @TypeImage,
                                                                            @Humidity,
                                                                            @Pressure,
                                                                            @WindDirection,
                                                                            @WindSpeed,
                                                                            @RefreshTime)
                                                            ", Properties.Settings.Default.TableWeatherName.ToLower());
            using (var connection = Connect(Properties.Settings.Default.DBName))
            {
                try
                {
                    if (connection.State != ConnectionState.Open)
                        connection.Open();
                    foreach (var item in weatherList)
                    {
                        _logger.Trace("Trying to write weather item '{0}:{1}({2})'", item.CityID, item.Date.Date, item.PartOfDay);
                        command = new MySqlCommand();
                        command.Connection = connection;
                        command.CommandText = commandText;
                        errorOn = item as WeatherItem;

                        command.Parameters.AddWithValue("@ID", null);
                        command.Parameters.AddWithValue("@City_ID", item.CityID);
                        command.Parameters.AddWithValue("@Date", item.Date.Date);
                        command.Parameters.AddWithValue("@DayPart", item.PartOfDay.ToString());
                        command.Parameters.AddWithValue("@Temperature", item.Temperature);
                        command.Parameters.AddWithValue("@TemperatureFeel", item.TemperatureFeel);
                        command.Parameters.AddWithValue("@Condition", item.Condition);
                        command.Parameters.AddWithValue("@TypeImage", item.TypeImage);
                        command.Parameters.AddWithValue("@Humidity", item.Humidity);
                        command.Parameters.AddWithValue("@Pressure", item.Pressure);
                        command.Parameters.AddWithValue("@WindDirection", item.WindDirection);
                        command.Parameters.AddWithValue("@WindSpeed", item.WindSpeed);
                        command.Parameters.AddWithValue("@RefreshTime", item.RefreshTime);

                        command.ExecuteNonQuery();
                    }
                    _logger.Trace("WriteWeather() = success");
                    result = true;
                }
                catch (Exception ex)
                {
                    _logger.Warn("Error occurred while writing '{0}:{1}({2})'. Message: {3}", errorOn.CityID, errorOn.Date.Date, errorOn.PartOfDay, ex.Message);
                }
                Disconnect(connection);
                return result;
            }
        }
        /// <summary>
        /// Записывает сведения о погоде в базу данных
        /// </summary>
        /// <param name="weather">Прогноз, который требуется записать</param>
        public static bool WriteWeather(IWeatherItem weather)
        {
            List<WeatherItem> items = new List<WeatherItem>() { weather as WeatherItem };
            return WriteWeather(items);
        }

        /// <summary>
        /// Записывает список городов, для которых предоставляется прогноз
        /// </summary>
        /// <param name="cities">Список городов</param>
        public static bool WriteCities(List<City> cities)
        {
            MySqlCommand command = new MySqlCommand();
            bool result = false;

            command.CommandText = string.Format("INSERT INTO {0} (ID, Name) VALUES (@ID, @Name)", Properties.Settings.Default.TableCityName.ToLower());

            command.Parameters.Add("@ID", MySqlDbType.UInt32);
            command.Parameters.Add("@Name", MySqlDbType.VarChar);

            using (var connection = Connect(Properties.Settings.Default.DBName))
            {
                try
                {
                    _logger.Trace("Trying to write list of cities");
                    if (connection.State != ConnectionState.Open)
                        connection.Open();
                    command.Connection = connection;

                    foreach (var city in cities)
                    {
                        command.Parameters["@ID"].Value = city.ID;
                        command.Parameters["@Name"].Value = city.Name;

                        command.ExecuteNonQuery();
                    }
                    _logger.Debug("WriteCityList() = success");
                    result = true;
                }
                catch (Exception ex)
                {
                    _logger.Warn("Error occurred while writing list of cities. Message: {0}", ex.Message);
                }
                Disconnect(connection);
                return result;
            }
        }

        /// <summary>
        /// Возвращает список городов, прочитанных из базы данных
        /// </summary>
        /// <returns>Список городов, хранящий ID и название города</returns>
        public static List<City> ReadCities()
        {
            List<City> result = new List<City>();

            _logger.Trace("Trying to read list of cities");
            using (var connection = Connect(Properties.Settings.Default.DBName))
            {
                MySqlCommand command = new MySqlCommand("SELECT * FROM " + Properties.Settings.Default.TableCityName, connection);
                try
                {
                    if (connection.State != ConnectionState.Open)
                        connection.Open();
                    var reader = command.ExecuteReader();
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            result.Add(new City(reader.GetInt32((int)CityTableColumn.ID), reader.GetString((int)CityTableColumn.Name)));
                        }
                        reader.Close();
                        _logger.Trace("ReadCirylist() - success");
                    }
                    else
                        throw new Exception("No data was found while reading list of cities from database");
                }
                catch (Exception ex)
                {
                    _logger.Warn("Error occurred while reading list of cities from database. Message: {0}", ex.Message);
                }
                Disconnect(connection);
                return result;
            }
        }

        /// <summary>
        /// Возвращает из базы данных название города по его id
        /// </summary>
        /// <param name="id">ID города для поиска</param>
        /// <returns>Возвращает строку с названием города либо пустую строку</returns>
        public static string ReadCityName(int id)
        {
            string result = string.Empty;

            using (var connection = Connect(Properties.Settings.Default.DBName))
            {
                MySqlCommand command = new MySqlCommand(string.Format("SELECT * FROM {0} WHERE ID = {1}", Properties.Settings.Default.TableCityName, id), connection);
                _logger.Trace("Trying to get city name of '{0}'", id);

                try
                {
                    if (connection.State != ConnectionState.Open)
                        connection.Open();
                    var reader = command.ExecuteReader();
                    var commmandResults = new List<City>();
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            commmandResults.Add(new City(reader.GetInt32((int)CityTableColumn.ID), reader.GetString((int)CityTableColumn.Name)));
                        }
                    }
                    else
                        throw new KeyNotFoundException(string.Format("City with ID {0} not found", id));
                    if (commmandResults.Count == 1)
                        result = commmandResults.First().Name;
                    else
                    {
                        var founded = string.Empty;
                        foreach (var item in commmandResults)
                        {
                            founded += string.Format("'{0}'{1}", item.Name, item.Name != commmandResults.Last().Name ? ", " : "");
                        }
                        throw new Exception(string.Format("Founded more then one city on id '{0}': {1}", id, founded));
                    }
                }
                catch (KeyNotFoundException kex)
                {
                    _logger.Debug(kex.Message);
                    return "Not Found";
                }
                catch (Exception ex)
                {
                    _logger.Warn(ex.Message);
                    return string.Empty;
                }
            }
            return result;
        }

        /// <summary>
        /// Читает из базы данных актуальный (последний из полученных) прогноз для указанного города в указанный день и время суток
        /// </summary>
        /// <param name="cityID">ID города</param>
        /// <param name="date">Дата прогноза</param>
        /// <param name="dayPart">Время суток</param>
        /// <returns></returns>
        public static IWeatherItem ReadWeatherItem(int cityID, DateTime date, DayPart dayPart)
        {
            MySqlCommand command = new MySqlCommand();
            WeatherItem weather = new WeatherItem();
            List<WeatherItem> readerResult = new List<WeatherItem>();
            string cmdString = string.Format("SELECT * FROM {0}.{1} WHERE City_ID={2} AND Date BETWEEN \"{3:yyyy.MM.dd}\" AND \"{3:yyyy.MM.dd}\"", 
                Properties.Settings.Default.DBName, Properties.Settings.Default.TableWeatherName, cityID, date);
            _logger.Trace("Trying to read weather '{0}:{1}({2})'", cityID, date.Date, dayPart);

            using (var connection = Connect(Properties.Settings.Default.DBName))
            {
                try
                {
                    command.Connection = connection;
                    command.CommandText = cmdString;
                    if (connection.State != ConnectionState.Open)
                        connection.Open();
                    var reader = command.ExecuteReader();
                    if (reader.HasRows)
                        while (reader.Read())
                        {
                            weather.CityID = reader.GetInt32((int)WeatherTableColumn.City_ID);
                            weather.Date = reader.GetDateTime((int)WeatherTableColumn.Date);
                            weather.PartOfDay = (DayPart)Enum.Parse(typeof(DayPart), reader.GetString((int)WeatherTableColumn.DayPart));
                            weather.Temperature = reader.GetInt32((int)WeatherTableColumn.Temperature);
                            weather.TemperatureFeel = reader.GetInt32((int)WeatherTableColumn.TemperatureFeel);
                            weather.Condition = reader.GetString((int)WeatherTableColumn.Condition);
                            weather.TypeImage = reader.GetString((int)WeatherTableColumn.TypeImage);
                            weather.Humidity = reader.GetInt32((int)WeatherTableColumn.Humidity);
                            weather.Pressure = reader.GetInt32((int)WeatherTableColumn.Pressure);
                            weather.WindDirection = reader.GetString((int)WeatherTableColumn.WindDirection);
                            weather.WindSpeed = reader.GetFloat((int)WeatherTableColumn.WindSpeed);
                            weather.RefreshTime = reader.GetDateTime((int)WeatherTableColumn.RefreshTime);

                            readerResult.Add(weather);
                            weather = new WeatherItem();
                        }
                    reader.Close();
                    //Выбираем по максимальному RefreshTime самый последний полученный прогноз
                    var wAllPart = from w in readerResult where w.PartOfDay == dayPart select w;
                    weather = (from w in wAllPart where w.RefreshTime == wAllPart.Max(x => x.RefreshTime) select w).First();
                    readerResult.Clear();
                    _logger.Trace("ReadWeatherItem({0}, {1}, {2}) - success", cityID, date, dayPart);
                }
                catch (Exception ex)
                {
                    _logger.Warn("Error was occurred while reading weather item '{0}.{1}({2})'. Message: {3}", cityID, date, dayPart, ex.Message);
                    weather = new WeatherItem();
                }
                Disconnect(connection);
                return weather;
            }
        }
    }
}
