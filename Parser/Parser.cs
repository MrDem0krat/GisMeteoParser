using HtmlAgilityPack;
using NLog;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Weather;

namespace ParserLib
{
    public class Parser : IParser
    {

        #region Поля
        /// <summary>
        /// Делегат функции, выполняющей выборку необходимых данных со страницы сайта
        /// </summary>
        /// <param name="_source">HTML-страница, для которой должна осуществляться выборка данных</param>
        /// <param name="dayToParse">День, кля которого необходимо прочитать прогноз</param>
        /// <param name="dayPart">Часть дня, кля которой необходимо прочитать прогноз</param>
        /// <returns></returns>
        public delegate IWeatherItem ParserGetDataHandler(HtmlDocument _source, DayToParse dayToParse = DayToParse.Tomorrow, DayPart dayPart = DayPart.Day);
        /// <summary>
        /// Объект для хранения ссылки на метод, выполняющий выборку информации со страницы
        /// </summary>
        private ParserGetDataHandler parserHandler;

        /// <summary>
        /// Логгер
        /// </summary>
        private static Logger _logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Обрабатываемый Html-документ с прогнозом погоды
        /// </summary>
        private HtmlDocument _weatherDocument = new HtmlDocument();

        /// <summary>
        /// Ссылка на поток в котором выполняется функция парсинга
        /// </summary>
        private Thread parserThread;

        /// <summary>
        /// Поле для хранения пользовательского значения периода обновления, на случай потери соединения с интернетом
        /// </summary>
        private TimeSpan settedRefreshPeriod;
        #endregion

        #region События для оповещения вызывающего кода о состоянии парсера
        /// <summary>
        /// Получен новый прогноз погоды
        /// </summary>
        public event EventHandler<WeatherParsedEventArgs> WeatherParsed;
        /// <summary>
        /// Парсер зарущен
        /// </summary>
        public event EventHandler ParserStarted;
        /// <summary>
        /// Парсер в режиме ожидания
        /// </summary>
        public event EventHandler ParserAsleep;
        /// <summary>
        /// Парсер остановлен
        /// </summary>
        public event EventHandler ParserStopped;
        /// <summary>
        /// Произошла ошибка во время парсинга
        /// </summary>
        public event EventHandler<Exception> ErrorOccured;
        #endregion

        #region Свойства
        /// <summary>
        /// Периодичность запуска парсера
        /// </summary>
        public TimeSpan RefreshPeriod { get; set; }

        /// <summary>
        /// Перечень городов, для которых требуется прогноз вида {ID : Название города}
        /// </summary>
        public Dictionary<int, string> Cities { get; set; }

        /// <summary>
        /// Время последнего обновления
        /// </summary>
        public DateTime LastRefresh { get; private set; }

        /// <summary>
        /// Адрес сайта без указания ID города
        /// </summary>
        public static string TargetUrl { get; set; }

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
        public Parser(string url) : this(url, TimeSpan.FromMinutes(30), new Dictionary<int, string>()) { }
        public Parser(string url, TimeSpan refreshPeriod) : this(url, refreshPeriod, new Dictionary<int, string>()) { }
        public Parser(string url, TimeSpan refreshPeriod, Dictionary<int, string> cities)
        {
            Status = ParserStatus.Stoped;
            RefreshPeriod = refreshPeriod;
            Cities = cities;
            TargetUrl = url;
        }
        #endregion

        #region Методы
        /// <summary>
        /// Проверка наличия соединения с Internet. 
        /// Возвращает true если проверка завершилась успешно
        /// </summary>
        /// <returns></returns>
        public static bool CheckConnection()
        {
            WebClient client = new WebClient();
            string _response;
            bool result = false;
            try
            {
                _response = client.DownloadString("http://www.ya.ru");
                if (_response.Contains("b-table__row layout__search"))
                {
                    _logger.Trace("CheckConnecton() = success");
                    result = true;
                }
            }
            catch (WebException ex)
            {
                _logger.Warn(string.Format("Internet connection error. Message: {0}", ex.Message));
                result = false;
            }
            client.Dispose();
            _response = string.Empty;
            return result;
        }
        public static Task<bool> CheckConnectionAsync()
        {
            return Task.Factory.StartNew(() => CheckConnection());
        }

        /// <summary>
        /// Функция загрузки Html-страницы с погодой
        /// </summary>
        /// <param name="url">Адрес страницы с погодой</param>
        /// <returns></returns>
        public static HtmlDocument DownloadPage(string url)
        {
            HtmlWeb client = new HtmlWeb();
            HtmlDocument result = new HtmlDocument();
            try
            {
                result = client.Load(url);
                _logger.Trace("DownloadPage({0}) = success", url.Replace(TargetUrl, ""));
            }
            catch (HtmlWebException webEx)
            {
                _logger.Warn("FileLoadError: " + webEx.Message, webEx);
                result = new HtmlDocument();
            }
            catch (Exception ex)
            {
                _logger.Warn("Error: cannot load file. Reason: {0}", ex.Message);
                result = new HtmlDocument();
            }
            return result;
        }

        /// <summary>
        /// Запуск работы парсера в отдельном потоке
        /// </summary>
        /// <param name="prms"></param>
        /// <returns></returns>
        public void Start(params string[] prms)
        {
            IWeatherItem weather;
            settedRefreshPeriod = RefreshPeriod;

            Task.Factory.StartNew(() =>
            {
                parserThread = Thread.CurrentThread;
                try
                {
                    while (true)
                    {
                        Status = ParserStatus.Working;
                        if (ParserStarted != null)
                        {
                            ParserStarted(this, new EventArgs());
                        }
                        if (CheckConnection())
                        {
                            if (RefreshPeriod != settedRefreshPeriod)
                                RefreshPeriod = settedRefreshPeriod;
                            if (parserHandler != null)
                            {
                                foreach (var city in Cities)
                                {
                                    _weatherDocument = DownloadPage(TargetUrl + city.Key);
                                    for (int i = 0; i < 4; i++)
                                    {
                                        weather = ParserHandler(_weatherDocument, dayPart: (DayPart)i);
                                        if (weather == WeatherItem.Empty)
                                        {
                                            _logger.Error("Forecast for city {0}({1}) - {2} not loaded", city.Key, city.Value, (DayPart)i);
                                        }
                                        else
                                        {
                                            weather.CityID = city.Key;
                                            weather.RefreshTime = DateTime.Now;

                                            if (WeatherParsed != null)
                                            {
                                                WeatherParsed(this, new WeatherParsedEventArgs(weather, DateTime.Now));
                                            }
                                        }
                                    }
                                }
                                LastRefresh = DateTime.Now;
                            }
                        }
                        else
                        {
                            if (ErrorOccured != null)
                                ErrorOccured(this, new Exception("Connection error. Check your Internet connection!"));
                            RefreshPeriod = new TimeSpan(0, 0, 30);
                        }
                        Status = ParserStatus.Sleeping;
                        if (ParserAsleep != null)
                            ParserAsleep(this, new EventArgs());
                        Thread.Sleep(RefreshPeriod);
                    }
                }
                catch (ThreadAbortException)
                {
                    if (Status == ParserStatus.Working)
                    {
                        Status = ParserStatus.Aborted;
                        if (ErrorOccured != null)
                            ErrorOccured(this, new Exception("Parser has been aborted while working. Writing info to database cancelled."));
                    }
                    if (Status == ParserStatus.Sleeping)
                    {
                        Status = ParserStatus.Stoped;
                    }
                }
                catch (Exception ex)
                {
                    if (ErrorOccured != null)
                        ErrorOccured(this, ex);
                }
            });
        }

        /// <summary>
        /// Остановка парсера
        /// </summary>
        public void Stop()
        {
            if (parserThread != null)
            {
                parserThread.Abort();
            }
            Dispose();
            if (ParserStopped != null)
                ParserStopped(this, new EventArgs());
        }

        #region IDisposable Support
        private bool disposedValue = false; // Для определения избыточных вызовов

        /// <summary>
        /// Освобождение ресурсов парсера
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _weatherDocument = new HtmlDocument();
                    parserThread = null;
                    // DO: освободить управляемое состояние (управляемые объекты).
                }

                // DO: освободить неуправляемые ресурсы (неуправляемые объекты) и переопределить ниже метод завершения.
                // DO: задать большим полям значение NULL.

                disposedValue = true;
            }
        }

        // DO: переопределить метод завершения, только если Dispose(bool disposing) выше включает код для освобождения неуправляемых ресурсов.
        // ~ParserLib() {
        //   // Не изменяйте этот код. Разместите код очистки выше, в методе Dispose(bool disposing).
        //   Dispose(false);
        // }

        // Этот код добавлен для правильной реализации шаблона высвобождаемого класса.
        public void Dispose()
        {
            // Не изменяйте этот код. Разместите код очистки выше, в методе Dispose(bool disposing).
            Dispose(true);
            // DO: раскомментировать следующую строку, если метод завершения переопределен выше.
            // GC.SuppressFinalize(this);
        }
        #endregion 
        #endregion
    }
}