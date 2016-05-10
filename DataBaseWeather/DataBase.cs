﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;
using System.Security.Cryptography;
using MySql.Data.MySqlClient;
using Weather;
using System.Data;

namespace DataBaseWeather
{
    /// <summary>
    /// Обслуживающий класс для работы с базой данных
    /// </summary>
    public static class DataBase
    {
        public static Logger _logger = LogManager.GetCurrentClassLogger();

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
            City_ID = 1,
            Date = 2,
            DayPart = 3,
            Temperature = 4,
            TemperatureFeel = 5,
            Condition = 6,
            TypeImage = 7,
            Humidity = 8,
            Pressure = 9,
            WindDirection = 10,
            WindSpeed = 11,
            RefreshTime = 12
        }

        /// <summary>
        /// Добавочная энтропия для кодирования пароля от пользователя БД
        /// </summary>
        private static byte[] additionalEntropy
        {
            get { return Convert.FromBase64String(Properties.Settings.Default.AdditionalEntropy); }
        }
        /// <summary>
        /// Адрес сервера MySQL
        /// </summary>
        public static string Server
        {
            get { return Properties.Settings.Default.Server; }
            set { Properties.Settings.Default.Server = value; }
        }
        /// <summary>
        /// Порт для подключения к серверу MySQL
        /// </summary>
        public static uint Port
        {
            get { return Properties.Settings.Default.Port; }
            set { Properties.Settings.Default.Port = value; }
        }
        /// <summary>
        /// Имя пользователя для подключения к серверу MySQL
        /// </summary>
        public static string Username
        {
            get { return Properties.Settings.Default.User; }
            set { Properties.Settings.Default.User = value; }
        }
        /// <summary>
        /// Пароль для доступа к серверу MySQL
        /// </summary>
        public static string Password
        {
            private get { return DecodePassword(Properties.Settings.Default.Password); }
            set { Properties.Settings.Default.Password = EncodePassword(value); }
        }

        /// <summary>
        /// Зашифровать пароль
        /// </summary>
        /// <param name="_source">Незашифрованный пароль</param>
        /// <returns></returns>
        private static string EncodePassword(string _source)
        {
            try
            {
                _logger.Debug("Try to encode password");
                var result = Convert.ToBase64String(ProtectedData.Protect(Encoding.UTF8.GetBytes(_source), additionalEntropy, DataProtectionScope.CurrentUser));
                _logger.Debug("Password encoded succesfully");
                return result;
            }
            catch (CryptographicException ex)
            {
                _logger.Error("Error occurred while connecting to database: can't encode password");
                _logger.Debug(string.Format("Password encode error: {0}", ex.Message));
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
                _logger.Debug("Try to decode password");
                var result = Encoding.UTF8.GetString(ProtectedData.Unprotect(Convert.FromBase64String(_source), additionalEntropy, DataProtectionScope.CurrentUser));
                _logger.Debug("Password decoded succesfully");
                return result;
            }
            catch (CryptographicException ex)
            {
                _logger.Error("Error occurred while connecting to database: enter your password again");
                _logger.Debug("Password decode error: {0}", ex.Message);
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

            connectionBuilder.Server = Server;
            connectionBuilder.Port = Port;
            connectionBuilder.UserID = Username;
            connectionBuilder.Password = Password;
            connectionBuilder.Database = database.ToLower();

            connection.ConnectionString = connectionBuilder.ConnectionString;
            try
            {
                if (connection.Database != "information_schema")
                {
                    _logger.Debug("Try to connect to database '{0}' at server '{1}'", connection.Database, Server);

                    if (CheckDataBase(connection.Database))
                    {
                        connection.Open();
                        _logger.Debug("Connected to database '{0}' at server '{1}'", connection.Database, Server);
                    }
                    else
                        _logger.Debug("Error occurred while connecting to database {0} at server '{1}'. Database '{0} not found at server", connection.Database, Server);
                }
                return connection;
            }
            catch (Exception ex)
            {
                _logger.Debug("Error occurred while connecting to database {0} at server '{1}': {2} - {3}", connection.Database, Server, ex.TargetSite, ex.Message);
                if (connection.State == ConnectionState.Open || connection.State == ConnectionState.Connecting)
                    connection.Close();
                connection.Dispose();
            }
            return new MySqlConnection();
        }
        /// <summary>
        /// Подключается к указанной базе данных на сервере в асинхронной манере
        /// </summary>
        /// <param name="database">Имя базы данных</param>
        /// <returns></returns>
        public async static Task<MySqlConnection> ConnectAsync(string database = "information_schema")
        {
            return await Task.Run(() =>
            {
                return Connect(database);
            });
        }

        /// <summary>
        /// Отключается от базы данных
        /// </summary>
        /// <param name="connection">Подключение к базе данных</param>
        public static void Disconnect(MySqlConnection connection)
        {
            connection.Close();
            _logger.Debug("Connection to database '{0}' at '{1}' closed", connection.Database, Server);
        }
        public static async Task DisconnectAsync(MySqlConnection connection)
        {
            await Task.Run(() =>
            {
                Disconnect(connection);
            });
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
            List<string> result = new List<string>();

            _logger.Debug("Checking database '{0}' at '{1}'", database, Server);
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
                            result.Add(reader.GetString(0));
                        }
                        reader.Close();
                        Disconnect(connection); // для записи в логе
                        if (result.Contains(database))
                        {
                            _logger.Debug("Database '{0}' successfully checked at '{1}'", database, Server);
                            return true;
                        }
                        else
                            _logger.Debug("Database '{0}' not found at '{1}'", database, Server);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error("Error occurred while checking database:\tMethod: {0}; Message: {1}", ex.TargetSite, ex.Message);
                }
                Disconnect(connection); // для записи в логе
                return false;
            }

        }
        /// <summary>
        /// Проверяет наличие базы данных на сервере в асинхронной манере
        /// </summary>
        /// <param name="database">Имя искомой базы данных</param>
        /// <returns></returns>
        public static async Task<bool> CheckDataBaseAsync(string database)
        {
            MySqlConnection connect = await ConnectAsync();

            return await CheckDataBaseAsync(database);
        }
        /// <summary>
        /// Проверяет наличие базы данных на сервере в асинхронной манере
        /// </summary>
        /// <param name="database">Имя искомой базы данных</param>
        /// <param name="connection">Подключение к серверу</param>
        /// <returns></returns>
        public static async Task<bool> CheckDataBaseAsync(string database, MySqlConnection connection)
        {
            return await Task.Run(() =>
            {
                return CheckDataBase(database, connection);
            });
        } 

        /// <summary>
        /// Проверяет наличие таблицы в базе данных
        /// </summary>
        /// <param name="table">Имя искомой таблицы</param>
        /// <param name="database">Имя базы данных, в которой необходимо произвести поиск</param>
        /// <returns></returns>
        public static bool CheckTable(TableType table, string database)
        {
            return CheckTable(table, Connect(database));
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
                List<string> result = new List<string>();
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

                _logger.Debug("Checking table '{0}' in database '{1}'", _tableName, connection.Database);

                try
                {
                    if (connection.State != ConnectionState.Open)
                        connection.Open();

                    var reader = command.ExecuteReader();
                    if (reader.HasRows)
                        while (reader.Read())
                            result.Add(reader.GetString(0));
                    reader.Close();
                    Disconnect(connection); //для записи в логе
                    if (result.Contains(_tableName))
                    {
                        _logger.Debug("Table '{0}' successfully checked in '{1}'", _tableName, connection.Database);
                        return true;
                    }
                    else
                    {
                        _logger.Debug("Table '{0}' not found in '{1}'", _tableName, connection.Database);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error("Error occurred while checking table '{0}' in {1}: Method: {2}; Message: {3}", _tableName, ex.TargetSite, ex.Message);
                }
                return false;
            }
        }
        /// <summary>
        /// Проверяет наличие таблицы в базе данных в асинхронной манере
        /// </summary>
        /// <param name="table">Имя искомой таблицы</param>
        /// <param name="database">Имя базы данных, в которой необходимо произвести поиск</param>
        /// <returns></returns>
        public static async Task<bool> CheckTableAsync(TableType table, string database)
        {
            return await CheckTableAsync(table, await ConnectAsync(database));
        }
        /// <summary>
        /// Проверяет наличие таблицы в базе данных в асинхронной манере
        /// </summary>
        /// <param name="table">Имя искомой таблицы</param>
        /// <param name="connection">Подключение к базе данных, в которой необходимо произвести поиск</param>
        /// <returns></returns>
        public static async Task<bool> CheckTableAsync(TableType table, MySqlConnection connection)
        {
            return await Task.Run(() => { return CheckTable(table, connection); });
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
                MySqlCommand command = new MySqlCommand(string.Format("CREATE DATABASE IF NOT EXISTS {0} CHARACTER SET cp1251 COLLATE cp1251_general_ci", database), connection);
                _logger.Debug("Try to creat new database '{0}'", database);
                try
                {
                    if (connection.State != ConnectionState.Open)
                        connection.Open();
                    if (CheckDataBase(database, connection))
                        _logger.Debug("Database '{0}' is already exists at '{1}'", database, Server);
                    else
                    {
                        command.ExecuteNonQuery();
                        if (CheckDataBase(database, connection))
                            _logger.Debug("Database '{0}' successfully created at '{1}'", database, Server);
                        else
                            _logger.Error("Error occerred while creating database '{0}' at '{1}'. Database was not created");
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error("Error occerred while creating database '{0}' at '{1}': Message: {2}", database, Server, ex.Message);
                }
                Disconnect(connection);
            }
        }
        /// <summary>
        /// Создает новую базу на сервере в асинхронной манере
        /// </summary>
        /// <param name="database">Имя создаваемой базы данных</param>
        /// <returns></returns>
        public static async Task CreateDataBaseAsync(string database)
        {
            await Task.Run(() => { CreateDataBase(database); });
        }

        /// <summary>
        /// Создает новую таблицу в базе данных
        /// </summary>
        /// <param name="database">Имя базы данных, в которой требуется создать таблицу</param>
        /// <param name="table">Тип создаваемой таблицы</param>
        public static void CreateTable(TableType table, string database)
        {
            if (CheckDataBase(database))
                CreateTable(table, Connect(database));
        }
        /// <summary>
        /// Создает новую таблицу в базе данных
        /// </summary>
        /// <param name="connection">Соединение с базой данных, в которой требуется создать таблицу</param>
        /// <param name="table">Тип создаваемой таблицы</param>
        public static void CreateTable(TableType table, MySqlConnection connection)
        {
            using (connection)
            {
                MySqlCommand command = new MySqlCommand();
                bool isAlreadyExists = false;
                string _tableName = string.Empty;

                try
                {

                    if (connection.State != ConnectionState.Open)
                        connection.Open();
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
                            isAlreadyExists = CheckTable(TableType.Weather, connection);
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
                            isAlreadyExists = CheckTable(TableType.City, connection);
                            break;
                    }

                    _logger.Debug("Try to creat new table '{0}' in '{1}'",_tableName, connection.Database);

                    if (!isAlreadyExists)
                    {
                        command.ExecuteNonQuery();
                        if (CheckTable(table, connection))
                            _logger.Debug("Table '{0}' successfully created in '{1}'", _tableName, connection.Database);
                        else
                            _logger.Error("Error occurred while creating table '{0}' in '{1}'. Table was not created", _tableName, connection.Database);
                    }
                    else
                        _logger.Debug("Table '{0}' already exists in '{1}'", _tableName, connection.Database);
                }
                catch (Exception ex)
                {
                    _logger.Debug("Error occurred while creating table '{0}' in '{1}': Message: {3}", _tableName, connection.Database, ex.Message);
                }
                Disconnect(connection);
            }
        }
        /// <summary>
        /// Создает новую таблицу в базе данных в асинхронной манере
        /// </summary>
        /// <param name="database">Имя базы данных, в которой требуется создать таблицу</param>
        /// <param name="table">Тип создаваемой таблицы</param>
        /// <returns></returns>
        public static async Task CreateTableAsync(TableType table, string database)
        {
            if (await CheckDataBaseAsync(database))
                await CreateTableAsync(table, await ConnectAsync(database));
        }
        /// <summary>
        /// Создает новую таблицу в базе данных в асинхронной манере
        /// </summary>
        /// <param name="connection">Соединение с базой данных, в которой требуется создать таблицу</param>
        /// <param name="table">Тип создаваемой таблицы</param>
        /// <returns></returns>
        public static async Task CreateTableAsync(TableType table, MySqlConnection connection)
        {
            await Task.Run(() => { CreateTable(table, connection); });
        }

        /// <summary>
        /// Подготавливает необходимую для работы базу данных и набор таблиц
        /// </summary>
        /// <returns></returns>
        public static void Prepare()
        {
            _logger.Debug("Preparing database for working");
            using (var connection = Connect())
            {
                if (!CheckDataBase(Properties.Settings.Default.DBName, connection))
                    CreateDataBase(Properties.Settings.Default.DBName);
                Disconnect(connection);
            }
            using (var connection = Connect(Properties.Settings.Default.DBName))
            {
                CreateTable(TableType.Weather, connection);
                CreateTable(TableType.City, connection);
                Disconnect(connection);
            }
            _logger.Debug("Database {0} prepared successfully", Properties.Settings.Default.DBName);
        }
        /// <summary>
        /// Подготавливает необходимую для работы базу данных и набор таблиц
        /// </summary>
        /// <returns></returns>
        public static async Task PrepareAsync()
        {
            await Task.Run(() =>
            {
                Prepare();
            });
        }

        /// <summary>
        /// Записывает сведения о погоде в базу данных
        /// </summary>
        /// <param name="weather">Прогноз, который требуется записать</param>
        public static bool WriteWeatherItem(IWeatherItem weather)
        {
            MySqlCommand command = new MySqlCommand();
            command.CommandText = string.Format(@"INSERT INTO {0} (ID, City_ID, Date, DayPart, Temperature, TemperatureFeel, `Condition`, TypeImage, Humidity, Pressure, WindDirection, WindSpeed, RefreshTime)
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
            command.Parameters.AddWithValue("@ID", null);
            command.Parameters.AddWithValue("@City_ID", weather.CityID);
            command.Parameters.AddWithValue("@Date", weather.Date.Date);
            command.Parameters.AddWithValue("@DayPart", weather.PartOfDay.ToString());
            command.Parameters.AddWithValue("@Temperature", weather.Temperature);
            command.Parameters.AddWithValue("@TemperatureFeel", weather.TemperatureFeel);
            command.Parameters.AddWithValue("@Condition", weather.Condition);
            command.Parameters.AddWithValue("@TypeImage", weather.TypeImage);
            command.Parameters.AddWithValue("@Humidity", weather.Humidity);
            command.Parameters.AddWithValue("@Pressure", weather.Pressure);
            command.Parameters.AddWithValue("@WindDirection", weather.WindDirection);
            command.Parameters.AddWithValue("@WindSpeed", weather.WindSpeed);
            command.Parameters.AddWithValue("@RefreshTime", weather.RefreshTime);

            using (var connection = Connect(Properties.Settings.Default.DBName))
            {
                _logger.Debug("Try to write weather info for city '{0}' for '{1} ({2})'", weather.CityID, weather.Date.ToShortDateString(), weather.PartOfDay);
                try
                {
                    if (connection.State != ConnectionState.Open)
                        connection.Open();
                    command.Connection = connection;
                    command.ExecuteNonQuery();
                    _logger.Debug("Record '{0}: {1} ({2})' successfully added", weather.CityID, weather.Date.ToShortDateString(), weather.PartOfDay);
                    Disconnect(connection);
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.Error("Error occurred while writing '{0}: {1} ({2})': Message: {3}", weather.CityID, weather.Date.ToShortDateString(), weather.PartOfDay, ex.Message);
                }
                Disconnect(connection);
                return false; 
            }
        }

        /// <summary>
        /// Записывает список городов, для которых предоставляется прогноз
        /// </summary>
        /// <param name="cities">Список городов</param>
        public static bool WriteCityList(Dictionary<int, string> cities)
        {
            MySqlCommand command = new MySqlCommand();

            command.CommandText = string.Format("INSERT INTO {0} (ID, Name) VALUES (@ID, @Name)", Properties.Settings.Default.TableCityName.ToLower());

            command.Parameters.Add("@ID", MySqlDbType.UInt32);
            command.Parameters.Add("@Name", MySqlDbType.VarChar);

            using (var connection = Connect(Properties.Settings.Default.DBName))
            {
                try
                {
                    _logger.Debug("Try to write list of cities");
                    if (connection.State != ConnectionState.Open)
                        connection.Open();
                    command.Connection = connection;

                    foreach (var city in cities)
                    {
                        command.Parameters["@ID"].Value = city.Key;
                        command.Parameters["@Name"].Value = city.Value;

                        command.ExecuteNonQuery();
                    }
                    _logger.Debug("List of cities writen successfully");
                    Disconnect(connection);
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.Error("Error occurred while writing list of cities: Message: {0}", ex.Message);
                }
                Disconnect(connection);
                return false;
            }
        }

        /// <summary>
        /// Возвращает список городов, прочитанных из базы данных
        /// </summary>
        /// <returns>Список городов, хранящий ID и название города</returns>
        public static Dictionary<int, string> ReadCityList()
        {
            Dictionary<int, string> result = new Dictionary<int, string>();

            _logger.Debug("Trying to read list of cities");
            using (var connection = Connect(Properties.Settings.Default.DBName))
            {
                MySqlCommand command = new MySqlCommand("SELECT * FROM " + Properties.Settings.Default.TableCityName, connection);
                if (connection.State != ConnectionState.Open)
                    connection.Open();
                try
                {
                    var reader = command.ExecuteReader();
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            result.Add(reader.GetInt32(0), reader.GetString(1));
                        }
                        _logger.Debug("List of cities readed successfully");
                    }
                    else
                        _logger.Debug("No data found while reading list of cities from database");
                    reader.Close();
                }
                catch (Exception ex)
                {
                    _logger.Error("Error occurred while reading list of cities from database: Message: {0}", ex.Message);
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
        public static string GetCityName(int id)
        {
            _logger.Debug("Try to get city name by ID '{0}'", id);
            Dictionary<int, string> cities = ReadCityList();
            bool isFound = false;

            var cityName = from c in cities where c.Key == id select c.Value;
            if (cityName.ToArray().Count() > 0)
            {
                isFound = true;
                _logger.Debug("Founded city is: '{0}'", cityName.FirstOrDefault());
            }
            else
            {
                _logger.Debug("City with ID '{0}' not founded");
            }

            return isFound? cityName.First() : string.Empty;
        }

        /// <summary>
        /// Читает из базы данных актуальный (последний из полученных) прогноз для указанного города в указанный день и время суток
        /// </summary>
        /// <param name="cityID">ID города</param>
        /// <param name="date">Дата прогноза</param>
        /// <param name="dayPart">Время суток</param>
        /// <returns></returns>
        public static IWeatherItem GetWeatherItem(int cityID, DateTime date, DayPart dayPart)
        {
            MySqlCommand command = new MySqlCommand();
            WeatherItem weather = new WeatherItem();
            List<WeatherItem> readerResult = new List<WeatherItem>();
            string cmdString = string.Format("SELECT * FROM {0}.{1} WHERE City_ID={2}", Properties.Settings.Default.DBName, Properties.Settings.Default.TableWeatherName, cityID);
            _logger.Trace("Trying to read actual weather info for the city '{0}' on {1} ({2})", cityID, date, dayPart);
            _logger.Debug("GetWeatherItem({0}, {1}, {2}", cityID, date, dayPart);
            using (var connection = Connect(Properties.Settings.Default.DBName))
            {
                try
                {
                    if (connection.State != ConnectionState.Open)
                        connection.Open();
                    command.Connection = connection;
                    command.CommandText = cmdString;

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
                    Disconnect(connection);

                    weather = (from w in readerResult where ((w.PartOfDay == dayPart) && (w.Date.Date == date.Date)) select w).Where(x =>
                                    x.RefreshTime == (from we in readerResult select we).Max(m => m.RefreshTime)).First();
                    readerResult.Clear();
                    _logger.Trace("Weather info for the city '{0}' readed successfully", cityID);
                    _logger.Debug("ID: {0}, {1}-{2}: success", cityID, date, dayPart);
                    return weather;
                }
                catch (Exception ex)
                {
                    _logger.Error("Error occurred while reading weather info for city '{0}' on '{1}({2})': Message: {3}", cityID, date, dayPart, ex.Message);
                    return new WeatherItem();
                }
            }
        }

        /// <summary>
        /// Возвращает прочитанный из базы список прогнозо для указанного города за указанный переиод времени
        /// </summary>
        /// <param name="cityID">Идентификатор города</param>
        /// <param name="firstDate">Начальная дата периода</param>
        /// <param name="lastDate">Конечная дата периода</param>
        /// <returns></returns>
        public static List<IWeatherItem> GetWeatherPeriod(int cityID, DateTime firstDate, DateTime lastDate)
        {
            List<IWeatherItem> result = new List<IWeatherItem>();
            IWeatherItem item = new WeatherItem();
            var days = (lastDate - firstDate).Days;
            for(var i = 0; i <days;i++)
            {
                for(var j = 0; j<4;j++)
                {
                    item = GetWeatherItem(cityID, firstDate.AddDays(i), (DayPart)j);
                    if (item != new WeatherItem())
                    result.Add(item);
                }
            }
            return result;
        }
        /// <summary>
        /// Возвращает прочитанный из базы список прогнозов для указанного города в указанный день на все части суток
        /// </summary>
        /// <param name="cityID">Идентификатор города</param>
        /// <param name="oneDay">Дата прогноза</param>
        /// <returns></returns>
        public static List<IWeatherItem> GetWeatherPeriod(int cityID, DateTime oneDay)
        {
            return GetWeatherPeriod(cityID, oneDay, oneDay);
        }

        /// <summary>
        /// Сохраняет настройки подключения к базе данных
        /// </summary>
        public static void SaveSettings()
        {
            Properties.Settings.Default.Save();
            _logger.Debug("MySQL connection data saved");
        }
    }
}
