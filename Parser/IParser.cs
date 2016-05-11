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
        /// Событие, информирующее о получении новых данных парсером.
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
        void Start(params string[] prms);

        /// <summary>
        /// Принудительная остановка парсера 
        /// </summary>
        void Stop();
    }

    public enum ParserStatus
    {
        Stoped = 0,
        Working = 1,
        Sleeping = 2,
        Aborted = 3
    }
}
