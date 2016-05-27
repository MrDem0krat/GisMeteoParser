using NLog;
using System;
using System.Runtime.InteropServices;
using System.ServiceModel;
using WeatherService;

namespace WeatherServiceHost
{
    class Program
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();
        private static bool isRun = true;

        static void Main(string[] args)
        {
            using (var host = new ServiceHost(typeof(WService)))
            {
                Console.WriteLine(">>>\tGismeteo Weather Service\t<<<\n");
                _logger.Debug("Application started");
                WService.Configure();
                if (host.State != CommunicationState.Opened)
                {
                    host.Open();
                    _logger.Trace("host.Open() = success");
                    Console.WriteLine("Host started");
                }
                while (isRun) ;
            }
        }

        /// <summary>
        /// Обработка события закрытия приложения
        /// </summary>
        /// <param name="CtrlType">Тип события, вызвавшего закрытие приложения</param>
        /// <returns></returns>
        private static bool ConsoleCtrlCheck(CtrlTypes CtrlType)
        {
            switch (CtrlType)
            {
                case CtrlTypes.CTRL_C_EVENT:
                    appClose("Ctrl + C command received");
                    break;
                case CtrlTypes.CTRL_BREAK_EVENT:
                    appClose("Ctrl + Break command received");
                    break;
                case CtrlTypes.CTRL_CLOSE_EVENT:
                    appClose("Programm being closed by User");
                    break;
                case CtrlTypes.CTRL_LOGOFF_EVENT:
                case CtrlTypes.CTRL_SHUTDOWN_EVENT:
                    appClose("User is logging off!");
                    break;
            }
            return true;
        }
        /// <summary>
        /// Применяется для закрытия приложения
        /// </summary>
        /// <param name="reason">Причина закрытия приложения</param>
        private static void appClose(string reason)
        {
            isRun = false;
            _logger.Debug("Service closed. Reason: {0}", reason);
        }

        #region unmanaged
        // Declare the SetConsoleCtrlHandler function
        // as external and receiving a delegate.

        [DllImport("Kernel32")]
        public static extern bool SetConsoleCtrlHandler(HandlerRoutine Handler, bool Add);

        // A delegate type to be used as the handler routine
        // for SetConsoleCtrlHandler.
        public delegate bool HandlerRoutine(CtrlTypes CtrlType);

        // An enumerated type for the control messages
        // sent to the handler routine.
        public enum CtrlTypes
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT,
            CTRL_CLOSE_EVENT,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT
        }
        #endregion
    }
}
