using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using MySql.Data;
using MySql.Data.MySqlClient;

namespace GisMeteoWeather
{
    /// <summary>
    /// Обслуживающий класс для работы с базой данных
    /// </summary>
    public static class DataBase // Вынести в отдельную библиотеку, чтобы была возможность подтянуть код в службу
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
            get { return Properties.Settings.Default.DBServer; }
            set { Properties.Settings.Default.DBServer = value; }
        }
        /// <summary>
        /// Порт для подключения к серверу MySQL
        /// </summary>
        public static uint Port
        {
            get { return Properties.Settings.Default.DBPort; }
            set { Properties.Settings.Default.DBPort = value; }
        }
        /// <summary>
        /// Имя пользователя для подключения к серверу MySQL
        /// </summary>
        public static string Username
        {
            get { return Properties.Settings.Default.DBUser; }
            set { Properties.Settings.Default.DBUser = value; }
        }
        /// <summary>
        /// Пароль для доступа к серверу MySQL
        /// </summary>
        public static string Password
        {
            private get { return DecodePassword(Properties.Settings.Default.DBPassword); }
            set { Properties.Settings.Default.DBPassword = EncodePassword(value); }
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

                return Convert.ToBase64String(ProtectedData.Protect(Encoding.UTF8.GetBytes(_source), additionalEntropy, DataProtectionScope.CurrentUser));
            }
            catch (CryptographicException ex)
            {
                _logger.Debug(String.Format("Не удалось зашифровать данные: {0}", ex.Message));
                return null;
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
                return Encoding.UTF8.GetString(ProtectedData.Unprotect(Convert.FromBase64String(_source), additionalEntropy, DataProtectionScope.CurrentUser));
            }
            catch (CryptographicException ex)
            {
                _logger.Error("Ошибка при подключении: Повторите ввод пароля MySQL");
                _logger.Debug("Не удалось расшифровать данные: {0}", ex.Message);
                return null;
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
                    if (CheckDataBase(connection.Database))
                    {
                        connection.Open();
                        _logger.Debug("Установлено соединение с базой данных '{0}' на сервере '{1}'", connection.Database, Server);
                    }
                    else
                        _logger.Debug("Попытка подключения к базе {0} завершилась неудачей. База данных с таким именем не найдена на сервере.", connection.Database);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "При подключении к базе данных '{0}' на сервере '{1}' произошла ошибка: {2} - {3}", connection.Database, Server, ex.TargetSite, ex.Message);
                connection = new MySqlConnection();
            }
            return connection;
        }
        /// <summary>
        /// Подключается к указанной базе данных на сервере в асинхронной манере
        /// </summary>
        /// <param name="database">Имя базы данных</param>
        /// <returns></returns>
        public async static Task<MySqlConnection> ConnectAsync(string database = "information_schema")
        {
            MySqlConnection connection = new MySqlConnection();
            MySqlConnectionStringBuilder connectionBuilder = new MySqlConnectionStringBuilder();

            connectionBuilder.Server = Server;
            connectionBuilder.Port = Port;
            connectionBuilder.UserID = Username;
            connectionBuilder.Password = Password;
            connectionBuilder.Database = database;

            connection.ConnectionString = connectionBuilder.ConnectionString;
            return await Task.Run(() =>
            {
                try
                {
                    if (database != "information_schema")
                        if (CheckDataBase(database))
                        {
                            connection.Open();
                            _logger.Debug("Установлено соединение с базой данных '{0}' на сервере '{1}'", connection.Database, Server);
                        }
                        else
                            _logger.Debug("Попытка подключения к базе {0} завершилась неудачей. База данных с таким именем не найдена на сервере.", database);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "При подключении к базе данных '{0}' на сервере '{1}' произошла ошибка: {2} - {3}", connection.Database, Server, ex.TargetSite, ex.Message);
                    connection = new MySqlConnection();
                }
                return connection;
            });
        }

        /// <summary>
        /// Отключается от базы данных
        /// </summary>
        /// <param name="connection">Подключение к базе данных</param>
        public static void Disconnect(MySqlConnection connection)
        {
            connection.Close();
            _logger.Debug("Соединение с базой данных '{0}' закрыто.", connection.Database);
        }
        public static async Task DisconnectAsync(MySqlConnection connection)
        {
            await Task.Run(() =>
            {
                connection.Close();
                _logger.Debug("Соединение с базой данных '{0}' закрыто.", connection.Database);
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

            try
            {
                if(connection.State != System.Data.ConnectionState.Open)
                    connection.Open();      
                reader = command.ExecuteReader();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        result.Add(reader.GetString(0));
                    }
                    reader.Close();
                    Disconnect(connection);
                    if (result.Contains(database))
                        return true;
                    else
                        _logger.Debug("Указанная база '{0}' отсутствует на сервере.", database);
                }
            }
            catch (Exception ex)
            {
                _logger.Error("При подключении к базе данных возникла ошибка:\tМетод: {0}, {1}", ex.TargetSite, ex.Message);
            }
            Disconnect(connection);
            return false;

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
            MySqlCommand command = new MySqlCommand("SHOW DATABASES", connection);
            List<string> result = new List<string>();

            try
            {
                if (connection.State != System.Data.ConnectionState.Open)
                    await connection.OpenAsync();
                var reader = await command.ExecuteReaderAsync();
                return await Task.Run(() =>
                {
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            result.Add(reader.GetString(0));
                        }
                        reader.Close();
                        Disconnect(connection);
                        if (result.IndexOf(database) >= 0)
                            return true;
                        else
                        {
                            _logger.Debug("Указанная база '{0}' отсутствует на сервере.", database);
                        }
                    }
                    return false;
                });
            }
            catch (Exception ex)
            {
                Disconnect(connection);
                _logger.Error("При подключении к базе данных возникла ошибка:\tМетод: {0}, {1}", ex.TargetSite, ex.Message);
                return false;
            }
        } //Можно тоже заменить вызовом CheckDataBase в отдельном потоке

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
            MySqlCommand command= new MySqlCommand("SHOW TABLES", connection);
            List<string> result = new List<string>();
            string _tableName  = "";

            switch (table)
            {
                case TableType.Weather:
                    _tableName = Properties.Settings.Default.TableWeatherName.ToLower();
                    break;
                case TableType.City:
                    _tableName = Properties.Settings.Default.TableCityName.ToLower();
                    break;
            }

            if (connection.State != System.Data.ConnectionState.Open)
                connection.Open();
            try
            {
                var reader = command.ExecuteReader();
                if (reader.HasRows)
                    while (reader.Read())
                        result.Add(reader.GetString(0));
                reader.Close();
                Disconnect(connection);
                if(result.Contains(_tableName))
                {
                    _logger.Debug("Таблица '{0}' уже имеется в базе '{1}'", _tableName, connection.Database);
                    return true;
                }
                else
                {
                    _logger.Debug("Необходимая таблица '{0}' отсутствует в базе '{1}'.", _tableName, connection.Database);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.Error("При создании новой таблицы '{0}' произошла ошибка: {1} (method: {2})", _tableName, ex.Message, ex.TargetSite);
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
            MySqlConnection connection = Connect();
            database = database.ToLower();
            MySqlCommand command = new MySqlCommand("CREATE DATABASE " + database, connection);

            try
            {
                if (connection.State != System.Data.ConnectionState.Open)
                    connection.Open();
                command.ExecuteNonQuery();
                if (CheckDataBase(database))
                    _logger.Debug("Новая база данных {0} успешно создана", database);
                else
                    _logger.Debug("При создании базы данных {0} произошла ошибка. База не создана", database);
            }
            catch (Exception ex)
            {
                _logger.Debug("При создании базы данных {0} произошла ошибка: {1}", database, ex.Message);
            }
            Disconnect(connection);
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
            MySqlCommand command = new MySqlCommand();
            bool isAlreadyExist = false;
            string _tableName = "";
            try
            {
                if (connection.State != System.Data.ConnectionState.Open)
                    connection.Open();
                switch (table)
                {
                    case TableType.Weather:
                        _tableName = Properties.Settings.Default.TableWeatherName.ToLower();
                        command = new MySqlCommand(string.Format(@"CREATE TABLE {0}.{1} (
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
                                CHARACTER SET utf8
                                COLLATE utf8_general_ci;",
                                    connection.Database, _tableName));//можно перенести текст запроса в ресурсы
                        isAlreadyExist = CheckTable(TableType.Weather, connection);
                        break;
                    case TableType.City:
                        _tableName = Properties.Settings.Default.TableCityName.ToLower();
                        command = new MySqlCommand(string.Format(@"CREATE TABLE {0}.{1} (
                                    ID int(10) UNSIGNED NOT NULL, 
                                    Name varchar(50) DEFAULT NULL,
                                    PRIMARY KEY (ID)
                                    )
                                ENGINE = INNODB
                                AUTO_INCREMENT = 1
                                CHARACTER SET utf8
                                COLLATE utf8_general_ci;",
                                    connection.Database, _tableName));//можно перенести текст запроса в ресурсы
                        isAlreadyExist = CheckTable(TableType.City, connection);
                        break;
                }
            
                if (!isAlreadyExist)
                {
                    if (connection.State != System.Data.ConnectionState.Open)
                        connection.Open();
                    command.Connection = connection;
                    command.ExecuteNonQuery();
                    if (CheckTable(table, connection))
                        _logger.Debug("Новая таблица '{0}' в базе данных '{1}' успешно создана.", _tableName, connection.Database);
                    else
                        _logger.Debug("При создании новой таблицы '{0}' в базе данных '{1}' произошла ошибка. Таблица не была создана.", _tableName, connection.Database);
                }
                else
                    _logger.Debug("Таблица '{0}' уже имеется в базе данных '{1}'", _tableName, connection.Database);
                Disconnect(connection);
            }
            catch (Exception ex)
            {
                _logger.Debug("При создании новой таблицы '{0}' в базе данных '{1}' произошла ошибка: (method: {2}) {3}", _tableName, connection.Database, ex.TargetSite, ex.Message);
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
            MySqlConnection connection = Connect();

            if (!CheckDataBase(Properties.Settings.Default.DBName,connection))
                CreateDataBase(Properties.Settings.Default.DBName);
            Disconnect(connection);

            connection = Connect(Properties.Settings.Default.DBName);
            if (!CheckTable(TableType.Weather, connection))
                CreateTable(TableType.Weather, connection);
            if (!CheckTable(TableType.City, connection))
                CreateTable(TableType.City, connection);
            Disconnect(connection);
            _logger.Debug("Подготовка базы данных {0} и необходимых таблиц завершена", Properties.Settings.Default.DBName);
        }
        /// <summary>
        /// Подготавливает необходимую для работы базу данных и набор таблиц
        /// </summary>
        /// <returns></returns>
        public static async Task PrepareAsync()
        {
            MySqlConnection connection = await ConnectAsync();

            if (!await CheckDataBaseAsync(Properties.Settings.Default.DBName))
                await CreateDataBaseAsync(Properties.Settings.Default.DBName);
            await DisconnectAsync(connection);

            connection = await ConnectAsync(Properties.Settings.Default.DBName);
            if (!await CheckTableAsync(TableType.Weather, connection))
                await CreateTableAsync(TableType.Weather, connection);
            if (!await CheckTableAsync(TableType.City, connection))
                await CreateTableAsync(TableType.City, connection);
            await DisconnectAsync(connection);
            _logger.Debug("Подготовка базы данных {0} и необходимых таблиц завершена", Properties.Settings.Default.DBName);
        }

        /// <summary>
        /// Записывает сведения о погоде в базу данных
        /// </summary>
        /// <param name="weather">Прогноз, который требуется записать</param>
        public static bool WriteWeatherInfo(ParserLib.IWeatherInfo weather)
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
            command.Parameters.Add("@ID", MySqlDbType.Int32);
            command.Parameters["@ID"].Value = null;
            command.Parameters.Add("@City_ID", MySqlDbType.Int32);
            command.Parameters["@City_ID"].Value = weather.CityID;
            command.Parameters.Add("@Date", MySqlDbType.Date);
            command.Parameters["@Date"].Value = weather.Date.Date;
            command.Parameters.Add("@DayPart", MySqlDbType.VarChar);
            command.Parameters["@DayPart"].Value = weather.PartOfDay.ToString();
            command.Parameters.Add("@Temerature", MySqlDbType.Int32);
            command.Parameters["@Temerature"].Value = weather.Temperature;
            command.Parameters.Add("@TemperatureFeel", MySqlDbType.Int32);
            command.Parameters["@TemperatureFeel"].Value = weather.TemperatureFeel;
            command.Parameters.Add("@Condition", MySqlDbType.VarChar);
            command.Parameters["@Condition"].Value = weather.Condition;
            command.Parameters.Add("@TypeImage", MySqlDbType.VarChar);
            command.Parameters["@TypeImage"].Value = weather.TypeImage;
            command.Parameters.Add("@Humidity", MySqlDbType.Int32);
            command.Parameters["@Humidity"].Value = weather.Humidity;
            command.Parameters.Add("@Pressure", MySqlDbType.Int32);
            command.Parameters["@Pressure"].Value = weather.Pressure;
            command.Parameters.Add("@WindDirection", MySqlDbType.VarChar);
            command.Parameters["@WindDirection"].Value = weather.WindDirection;
            command.Parameters.Add("@WindSpeed", MySqlDbType.Float);
            command.Parameters["@WindSpeed"].Value = weather.WindSpeed;
            command.Parameters.Add("@RefreshTime", MySqlDbType.DateTime);
            command.Parameters["@RefreshTime"].Value = DateTime.Now; //Временно так, позже добавить в IWeatherInfo свойство RefreshTime

            try
            {
                _logger.Debug("Пробуем записать данные о погоде для г.{0} на {1} - {2}", weather.CityID, weather.Date.ToShortDateString(), weather.PartOfDay);
                command.Connection = Connect(Properties.Settings.Default.DBName);
                command.ExecuteNonQuery();
                _logger.Debug("Запись {0}: {1} - {2} успешно добавлена в базу", weather.CityID, weather.Date.ToShortDateString(),weather.PartOfDay);
                Disconnect(command.Connection);
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error("При добавлении записи {0}: {1} - {2} произошла ошибка: {3}", weather.CityID, weather.Date.ToShortDateString(),weather.PartOfDay, ex.Message);
                Disconnect(command.Connection);
            }
            return false;
        }
        /// <summary>
        /// Записывает список городов, для которых предоставляется прогноз
        /// </summary>
        /// <param name="cities">Список городов</param>
        public static bool WriteCityList(Dictionary<int, string> cities)
        {
            MySqlCommand command = new MySqlCommand();

            command.CommandText = string.Format("INSERT INTO {0} (ID, Name) VALUES (@ID, @Name)",Properties.Settings.Default.TableCityName.ToLower());
            command.Parameters.Add("@ID", MySqlDbType.UInt32);
            command.Parameters.Add("@Name", MySqlDbType.VarChar);

            try
            {
                _logger.Debug("Пробуем записать список городов");
                command.Connection = Connect(Properties.Settings.Default.DBName);

                foreach (var city in cities)
                {
                    var bibi1251 = Encoding.GetEncoding(1251);
                    var bobo = Encoding.UTF8.GetBytes(city.Value);

                    command.Parameters["@ID"].Value = city.Key;
                    command.Parameters["@Name"].Value = bibi1251.GetString(bobo);
                        
                    command.ExecuteNonQuery();
                }
                _logger.Debug("Список городов успешно записан в базу данных");
                Disconnect(command.Connection);
                return true;
            }
            catch(Exception ex)
            {
                _logger.Debug("Во время записи списка городов произошла ошибка: {0}", ex.Message);
                Disconnect(command.Connection);
            }
            return false;
        } 
    }
}
