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
        /// Этот метод должен быть запущен в другом потоке, чтобы не вешать приложение. 
        /// Он должен постоянно работать, возможно периодически просыпаясь или засыпая, чтобы не делать лишней работы.
        /// Он в фоне парсит погоду и генерирует событие WeatherParsedEventArgs, если есть разобранные данные.
        /// </summary>
        /// <param name="prms"></param>
        void Start(params string[] prms);

        /// <summary>
        /// Принудительная остановка парсера (например, при завершении приложения). В этом методе нужно запускать Dispose(), 
        /// чтобы очистить данные, закрыть соединения и т.п., чтобы не захламлять и не валилось исключений.
        /// </summary>
        void Stop();
    }
}
