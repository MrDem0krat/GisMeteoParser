using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using NLog;
using System.Net;
using Weather;

namespace ParserLib
{
    public class Parser : IParser
    {
        //Событие опопвещающее о том, что прогноз получен
        public event EventHandler<WeatherParsedEventArgs> WeatherParsed;
        //События для оповещения вызывающего кода о состоянии парсера
        public event EventHandler ParserStarted; //Парсер запущен
        public event EventHandler ParserAsleep; //Парсер в режиме ожидания
        public event EventHandler ParserStopped; //Парсер остановлен

        public delegate IWeatherItem ParserGetDataHandler(HtmlDocument _source, DayToParse dayToParse = DayToParse.Tomorrow, DayPart dayPart = DayPart.Day);

        private static Logger _logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Обрабатываемый Html-документ с прогнозом погоды
        /// </summary>
        private HtmlDocument _weatherDocument = new HtmlDocument();
        /// <summary>
        /// Объект для хранения ссылки на метод, выполняющий выборку информации со страницы
        /// </summary>
        private ParserGetDataHandler parserHandler;
        /// <summary>
        /// Ссылка на поток в котором выполняется функция парсинга
        /// </summary>
        private Thread parserThread;

        #region Свойства
        /// <summary>
        /// Период обновления
        /// </summary>
        public TimeSpan RefreshPeriod { get; set; }

        /// <summary>
        /// Перечень городов, для которых требуется прогноз вида НазваниеГорода:ID
        /// </summary>
        public Dictionary<int, string> Cities { get; set; }

        /// <summary>
        /// Время последнего обновления
        /// </summary>
        public DateTime LastRefresh { get; private set; }

        /// <summary>
        /// Адрес сайта без указания ID города
        /// </summary>
        public string TargetUrl { get; set; }

        /// <summary>
        /// Текущее состояние парсера
        /// </summary>
        public ParserStatus Status { get; private set; }

        /// <summary>
        /// Ссылка на метод, выполняющий выборку данных со страницы
        /// </summary>
        public ParserGetDataHandler ParserHandler
        {
            private get { return parserHandler; }
            set { parserHandler += value; }
        }
        #endregion

        #region Конструкторы
        public Parser() { }
        public Parser(string url) : this(url, TimeSpan.FromMinutes(5), new Dictionary<int, string>()) { }
        public Parser(string url, TimeSpan refreshPeriod) : this(url, refreshPeriod, new Dictionary<int, string>()) { }
        public Parser(string url, TimeSpan refreshPeriod, Dictionary<int, string> cities)
        {
            RefreshPeriod = refreshPeriod;
            Cities = cities;
            TargetUrl = url;
        }
        #endregion


        /// <summary>
        /// Проверка наличия соединения с Internet. 
        /// Возвращает true если проверка завершилась успешно
        /// </summary>
        /// <returns></returns>
        public static bool CheckConnection()
        {
            HtmlWeb _client = new HtmlWeb();
            string _response;
            try
            {
                _response = _client.Load("http://www.ya.ru").ToString();
                _logger.Debug("Internet Connecton checked succesfully");
                return true;
            }
            catch (WebException _ex)
            {
                _logger.Error("Check your internet connection");
                _logger.Debug(String.Format("Internet connection error: {0}", _ex.Message));
            }
            return false;
        }
        public static Task<bool> CheckConnectionAsync()
        {
            var task = new Task<bool>(() => Parser.CheckConnection());
            task.Start();
            return task;
        }

        /// <summary>
        /// Функция загрузки Html-страницы с погодой
        /// </summary>
        /// <param name="Url">Адрес страницы с погодой</param>
        /// <returns></returns>
        public static HtmlDocument DownloadPage(string Url)
        {
            HtmlWeb _client = new HtmlWeb();
            HtmlDocument _doc = new HtmlDocument();
            try
            {
                _logger.Debug("Try to load weather webpage");
                _doc = _client.Load(Url);
                _logger.Debug("Webpage loaded successfully");
                return _doc;
            }
            catch (HtmlWebException webEx)
            {
                _logger.Error("FileLoadError: " + webEx.Message, webEx);
                return null;
            }
            catch (Exception ex)
            {
                _logger.Error("Error: cannot load file. Reason: {0}", ex.Message);
                return null;
            }
        }

        /*
        public void Start(params string[] prms)
        {
            Status = ParserStatus.Working;
            if(ParserStarted != null)
            {
                ParserStarted(this, new EventArgs());
            }

            string _errorText = String.Empty;
            Exception ex = null;

            if (CheckConnection())
            {
                if (parserHandler != null)
                {
                    foreach (var city in Cities)
                    {
                        _weatherDocument = DownloadPage(TargetUrl + city.Key);
                        IWeatherInfo weather = ParserHandler(_weatherDocument);
                        weather.CityID = city.Key;
                        if (WeatherParsed != null)
                        {
                            WeatherParsed(this, new WeatherParsedEventArgs(weather, DateTime.Now));
                        }
                    }
                }
            }
            else
            {
                _errorText = "Connection error. Check your Internet connection!";
                ex = new WebException(_errorText, WebExceptionStatus.ConnectFailure);
            }
            if (ParserAsleep != null)
                ParserAsleep(this, new EventArgs());
            Status = ParserStatus.Sleeping;
            Thread.Sleep(RefreshPeriod);
        }
        */
        //Добавить загрузку прогноза на остальные части дня
        /// <summary>
        /// Запуск работы парсера в отдельном потоке
        /// </summary>
        /// <param name="prms"></param>
        /// <returns></returns>
        public async Task StartAsync(params string[] prms)
        {
            if (ParserStarted != null)
            {
                ParserStarted(this, new EventArgs());
            }
            string _errorText = String.Empty;
            Exception ex = null;
            IWeatherItem weather;
            Status = ParserStatus.Working;
            await Task.Run(() =>
            {
                parserThread = Thread.CurrentThread;
                try
                {
                    while (true)
                    {
                        Status = ParserStatus.Working;
                        if (CheckConnection())
                        {
                            if (parserHandler != null)
                            {
                                foreach (var city in Cities)
                                {
                                    _weatherDocument = DownloadPage(TargetUrl + city.Key);
                                    for(int i=0; i < 4; i++)
                                    {
                                        weather = ParserHandler(_weatherDocument, dayPart: (DayPart)i);
                                        weather.CityID = city.Key;
                                        weather.RefreshTime = DateTime.Now;
                                        if (WeatherParsed != null)
                                        {
                                            WeatherParsed(this, new WeatherParsedEventArgs(weather, DateTime.Now));
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            _errorText = "Connection error. Check your Internet connection!";
                            ex = new WebException(_errorText, WebExceptionStatus.ConnectFailure);
                        }
                        LastRefresh = DateTime.Now;
                        if (ParserAsleep != null)
                            ParserAsleep(this, new EventArgs());
                        Status = ParserStatus.Sleeping;
                        Thread.Sleep(RefreshPeriod);
                    }
                }
                catch (ThreadAbortException)
                {
                    if (Status == ParserStatus.Working)
                    {
                        Console.WriteLine("Parser has been aborted while working. Writing info to database cancelled.");
                        Status = ParserStatus.Aborted;
                    }
                    if (Status == ParserStatus.Sleeping)
                    {
                        Console.WriteLine("Parser has been safely stopped");
                        Status = ParserStatus.Stoped;
                    }
                }
            });
        }

        public void Stop()
        {
            if (parserThread != null)
            {
                parserThread.Abort();
            }
            this.Dispose();
            if (ParserStopped != null)
                ParserStopped(this, new EventArgs());
        }

        #region IDisposable Support
        private bool disposedValue = false; // Для определения избыточных вызовов

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {

                    // TODO: освободить управляемое состояние (управляемые объекты).
                }

                // TODO: освободить неуправляемые ресурсы (неуправляемые объекты) и переопределить ниже метод завершения.
                // TODO: задать большим полям значение NULL.

                disposedValue = true;
            }
        }

        // TODO: переопределить метод завершения, только если Dispose(bool disposing) выше включает код для освобождения неуправляемых ресурсов.
        // ~ParserLib() {
        //   // Не изменяйте этот код. Разместите код очистки выше, в методе Dispose(bool disposing).
        //   Dispose(false);
        // }

        // Этот код добавлен для правильной реализации шаблона высвобождаемого класса.
        public void Dispose()
        {
            // Не изменяйте этот код. Разместите код очистки выше, в методе Dispose(bool disposing).
            Dispose(true);
            // TODO: раскомментировать следующую строку, если метод завершения переопределен выше.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}