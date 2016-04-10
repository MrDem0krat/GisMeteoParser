using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;
using System.Net;

namespace ParserLib
{
    public class Parser : IParser
    {
        public event EventHandler<WeatherParsedEventArgs> WeatherParsed;
        public delegate IWeatherInfo ParserGetDataHandler(HtmlDocument _source, DayToParse dayToParse = DayToParse.Today, DayPart dayPart = DayPart.Day);

        private static Logger _parserLogger = LogManager.GetCurrentClassLogger();
        private ParserGetDataHandler listofHandlers;
        /// <summary>
        /// Обрабатываемый Html-документ с прогнозом погоды
        /// </summary>
        private HtmlDocument _weatherDocument = new HtmlDocument();

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
        /// Адрес сайта без указания ID города
        /// </summary>
        public string TargetUrl { get; set; }
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
                _parserLogger.Debug("Internet Connecton checked succesfully");
                return true;
            }
            catch (WebException _ex)
            {
                _parserLogger.Error("Check your internet connection");
                _parserLogger.Debug(String.Format("Internet connection error: {0}", _ex.Message));
            }
            return false;
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
                _parserLogger.Debug("Try to load weather webpage");
                _doc = _client.Load(Url);
                _parserLogger.Debug("Webpage loaded successfully");
                return _doc;
            }
            catch (HtmlWebException webEx)
            {
                _parserLogger.Error("FileLoadError: " + webEx.Message, webEx);
                return null;
            }
            catch (Exception ex)
            {
                _parserLogger.Error("Error: cannot load file. Reason: {0}", ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Метод регистрирует функцию, которая выполняет выборку данных
        /// </summary>
        /// <param name="handler">Ссылка на функцию-обработчик данных</param>
        public void RegisterParserHandler(ParserGetDataHandler handler)
        {
            listofHandlers += handler;
        }

        public void Start(params string[] prms)
        {
            string _errorText = String.Empty;
            Exception ex = null;
            if (CheckConnection())
                if (listofHandlers != null)
                {
                    foreach (var item in Cities)
                    {
                        Console.WriteLine("\nПогода в г. "+ item.Value + "\n");
                        _weatherDocument = DownloadPage(TargetUrl + item.Key);
                        IWeatherInfo weather = listofHandlers(_weatherDocument);
                        if (WeatherParsed != null)
                        {
                            WeatherParsed(this, new WeatherParsedEventArgs(weather));
                        }
                    }
                }
        }
        

        public void Stop()
        {
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
