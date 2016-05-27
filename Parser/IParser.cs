using System;

namespace ParserLib
{
    public interface IParser : IDisposable
    {
        /// <summary>
        /// Событие, информирующее о получении новых данных парсером.
        /// </summary>
        event EventHandler<WeatherParsedEventArgs> WeatherParsed;
        /// <summary>
        /// Возникает при изменении состояния парсера
        /// </summary>
        event EventHandler<StateChangedEventArgs> StateChanged;

        /// <summary>
        /// Текущее соостояние парсера
        /// </summary>
        Parser.ParserState State { get; }
        
        /// <summary>
        /// Запуск парсера в отдельном потоке
        /// </summary>
        /// <param name="prms"></param>
        /// <returns></returns>
        void Start();

        /// <summary>
        /// Принудительная остановка парсера 
        /// </summary>
        void Stop();
    }

}
