using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParserLib
{
    public interface IParser : IDisposable
    {
        /// <summary>
        /// Событие готовых данных, полученных от парсера. На это событие может кто-то подписаться и обрабатывать нотификацию, чтобы что-то сделать с полученными данными.
        /// </summary>
        event EventHandler<WeatherParsedEventArgs> WeatherParsed;
        /// <summary>
        /// Событие генерируется при очередном запуске парсера
        /// </summary>
        event EventHandler ParserStarted;
        /// <summary>
        /// Событие генерируется после того, так парсер собрал информацию и уснул на заданный промежуток времени
        /// </summary>
        event EventHandler ParserAsleep;
        /// <summary>
        /// Событие генерируется при завершении работы парсера
        /// </summary>
        event EventHandler ParserStopped;
        /// <summary>
        /// Текущее соостояние парсера
        /// </summary>
        ParserStatus Status { get; }
        
        /// <summary>
        /// Запуск парсера в отдельном потоке
        /// </summary>
        /// <param name="prms"></param>
        /// <returns></returns>
        Task StartAsync(params string[] prms);

        /// <summary>
        /// Принудительная остановка парсера (например, при завершении приложения). В этом методе нужно запускать Dispose(), 
        /// чтобы очистить данные, закрыть соединения и т.п., чтобы не захламлять и не валилось исключений.
        /// </summary>
        void Stop();
    }

    public enum ParserStatus
    {
        Stoped = 0,
        Started = 1,
        Sleeping = 2,
        Aborted = 3
    }
}
