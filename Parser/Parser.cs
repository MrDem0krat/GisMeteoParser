using HtmlAgilityPack;
using NLog;
using System;
using System.Collections.Generic;
using System.Threading;
using Gismeteo.Weather;
using Gismeteo.City;
using System.Text.RegularExpressions;
using System.Linq;
using System.Net;

namespace ParserLib
{
    public class Parser : IParser
    {
        #region Static
        /// <summary>
        /// Логгер
        /// </summary>
        private static Logger _logger = LogManager.GetCurrentClassLogger();
        #endregion

        #region Перечисления
        public enum ParserState
        {
            Stop = 0,
            Working,
            Sleeping,
            Aborted,
            Error
        } 
        #endregion

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
        /// Обрабатываемый Html-документ с прогнозом погоды
        /// </summary>
        private HtmlDocument _weatherDocument = new HtmlDocument();

        /// <summary>
        /// Поле хранит значение, определяющее должен ли парсер продолжать виполнять свою работу
        /// </summary>
        private bool isRun = true;
        #endregion

        #region События 
        /// <summary>
        /// Получен новый прогноз погоды
        /// </summary>
        public event EventHandler<WeatherParsedEventArgs> WeatherParsed;

        /// <summary>
        /// Возникает при изменении состояния парсера
        /// </summary>
        public event EventHandler<StateChangedEventArgs> StateChanged;
        #endregion

        #region Свойства
        /// <summary>
        /// Периодичность запуска парсера
        /// </summary>
        public TimeSpan RefreshPeriod { get; set; }

        /// <summary>
        /// Перечень городов, для которых требуется прогноз вида {ID : Название города}
        /// </summary>
        public List<City> Cities { get; set; }

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
        public ParserState State { get; private set; }

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
        public Parser(string url) : this(url, TimeSpan.FromMinutes(30), new List<City>()) { }
        public Parser(string url, TimeSpan refreshPeriod) : this(url, refreshPeriod, new List<City>()) { }
        public Parser(string url, TimeSpan refreshPeriod, List<City> cities)
        {
            TargetUrl = url;
            RefreshPeriod = refreshPeriod;
            Cities = cities;
            State = ParserState.Stop;
        }
        #endregion

        #region Методы 
        #region Static
        /// <summary>
        /// Функция собирающая названия и id всех городов на главной странице.
        /// Функция возвращает список вида НазваниеГорода:ID
        /// </summary>
        /// <returns></returns>
        public static List<City> GetCities()
        {
            List<City> result = new List<City>();
            string target = "https://www.gismeteo.ru"; //TODO: передавать в параметрах, так же передавать функцию, произвоящую выборку данных
            string buffer = string.Empty;
            City emptyCity = new City();
            int id;
            List<string> wrapIdList = new List<string>() { "cities-teaser", "cities1", "cities2", "cities3" };
            Regex regId = new Regex(@"[\d]{1,}");
            HtmlDocument mainPage = DownloadPage(target);

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
                                {
                                    if ((from c in result where c.ID == id select c).Count() == 0)
                                        result.Add(new City(id, liNode.Element("a").InnerText));
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                _logger.Warn("Can't get main page city list: no data to parse");
                return new List<City>();
            }
            return result;
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
                _logger.Trace("DownloadPage({0}) = success", url.Remove(0, url.LastIndexOf('/') + 1));
            }
            catch (WebException)
            {
                _logger.Warn("Unable to connect to target. Internet coonection uavailable");
                result = new HtmlDocument();
            }
            catch (Exception ex)
            {
                _logger.Warn("Can't load file. Reason: {0}", ex.Message);
                result = new HtmlDocument();
            }
            return result;
        } 
        #endregion

        /// <summary>
        /// Запуск работы парсера в отдельном потоке
        /// </summary>
        /// <param name="prms"></param>
        /// <returns></returns>
        public void Start()
        {
            IWeatherItem weather;
            isRun = true;
            string errorText = string.Empty;
            _logger.Debug("Parser started in Thread: {0}", Thread.CurrentThread.ManagedThreadId);
            while (true)
            {
                try
                {
                    State = ParserState.Working;
                    if (StateChanged != null)
                        StateChanged(this, new StateChangedEventArgs(ParserState.Working));
                    if (parserHandler != null)
                    {
                        foreach (var city in Cities)
                        {
                            _weatherDocument = DownloadPage(TargetUrl + city.ID);
                            for (int i = 0; i < 4; i++)
                            {
                                weather = ParserHandler(_weatherDocument, dayPart: (DayPart)i);
                                if (weather != WeatherItem.Empty)
                                {
                                    weather.CityID = city.ID;
                                    weather.RefreshTime = DateTime.Now;
                                }
                                else
                                {
                                    errorText = string.Format("Forecast for city {0}({1}) - {2} not loaded", city.ID, city.Name, (DayPart)i);
                                    if (StateChanged != null)
                                        StateChanged(this, new StateChangedEventArgs(ParserState.Error, new Exception(errorText)));
                                }
                                if (WeatherParsed != null)
                                    WeatherParsed(this, new WeatherParsedEventArgs(weather, DateTime.Now, errorText: errorText));
                            }
                        }
                        LastRefresh = DateTime.Now;
                    }
                    State = ParserState.Sleeping;
                    if (StateChanged != null)
                        StateChanged(this, new StateChangedEventArgs(ParserState.Sleeping));

                    Thread.Sleep(RefreshPeriod);
                }
                catch (ThreadAbortException)
                {
                    if (State == ParserState.Working)
                    {
                        State = ParserState.Aborted;
                        if (StateChanged != null)
                            StateChanged(this, new StateChangedEventArgs(ParserState.Aborted, new Exception("Parser has been aborted while working.")));
                    }
                    if (State == ParserState.Sleeping)
                    {
                        State = ParserState.Stop;
                        if (StateChanged != null)
                            StateChanged(this, new StateChangedEventArgs(ParserState.Stop));
                    }
                }
                catch (Exception ex)
                {
                    if (StateChanged != null)
                        StateChanged(this, new StateChangedEventArgs(ParserState.Error, ex));
                }
                if (!isRun)
                    break;
            }
        }

        /// <summary>
        /// Остановка парсера
        /// </summary>
        public void Stop()
        {
            isRun = false;
            Dispose();
            if (StateChanged != null)
                StateChanged(this, new StateChangedEventArgs(ParserState.Stop));
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