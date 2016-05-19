using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using WeatherService;

namespace WeatherServiceHost
{
    class Program
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();
        private static bool isRun = true;
        private static bool isChanged = false;

        static void Main(string[] args)
        {
            using (var host = new ServiceHost(typeof(WService)))
            {
                string answer = string.Empty;
                string value = string.Empty;

                Console.WriteLine(">>>\tGismeteo Weather Service\t<<<\n");
                PrintHelp();
                _logger.Debug("Application started");
                
                while (isRun)
                {
                    answer = Console.ReadLine();
                    RunCommand(host, answer);
                }
                _logger.Debug("Service closed");
            }
        }

        /// <summary>
        /// Перечень доступных команд
        /// </summary>
        enum Command
        {
            /// <summary>
            /// Комманда не распознана
            /// </summary>
            None = 0,
            /// <summary>
            /// Запустить сервис
            /// </summary>
            Start = 1,
            /// <summary>
            /// Остановить сервис и выйти из приложения
            /// </summary>
            Exit = 2,
            /// <summary>
            /// Установить значение имени пользователя для подключения к базе данных
            /// </summary>
            User = 3,
            /// <summary>
            /// Установить значение пароля для подключения к базе данных
            /// </summary>
            Pass = 4 ,
            /// <summary>
            /// Установить значение адреса сервера, на котором расположена база данных
            /// </summary>
            Server = 5,
            /// <summary>
            /// Установить значение номера порта для подключения к серверу
            /// </summary>
            Port = 6,
            /// <summary>
            /// Отобразить список доступных команд
            /// </summary>
            Help = 7
                      
        }
        
        /// <summary>
        /// Возвращает распознанную из пользовательского ввода команду
        /// </summary>
        /// <param name="answer">Пользовательский ввод</param>
        /// <returns></returns>
        private static Command GetCommand(string answer)
        {
            Command result;
            //Перечень команд, принимающих значение
            List<Command> complexCommands = new List<Command>();
            complexCommands.Add(Command.User);
            complexCommands.Add(Command.Server);
            complexCommands.Add(Command.Port);

            var comID = (from cid in (int[])Enum.GetValues(typeof(Command)) where answer.StartsWith(Enum.GetName(typeof(Command), cid).ToLower()) select cid).FirstOrDefault();

            #region SwitchCommand
            switch (comID)
            {
                case (int)Command.Start:
                    result = Command.Start;
                    break;
                case (int)Command.Exit:
                    result = Command.Exit;
                    break;
                case (int)Command.User:
                    result = Command.User;
                    break;
                case (int)Command.Pass:
                    result = Command.Pass;
                    break;
                case (int)Command.Server:
                    result = Command.Server;
                    break;
                case (int)Command.Port:
                    result = Command.Port;
                    break;
                case (int)Command.Help:
                    result = Command.Help;
                    break;
                default:
                    result = Command.None;
                    break;
            }
            #endregion

            if(!complexCommands.Contains(result))
            {
                //Дополнительная проверка правильности ввода команды
                var anotherID = (from cid in (int[])Enum.GetValues(typeof(Command)) where Enum.GetName(typeof(Command), cid).ToLower() == answer select cid).FirstOrDefault();
                if (anotherID == 0)
                    return Command.None;
            }
            return result;
        }

        /// <summary>
        /// Выполняет действия, предписанные для введенной команды
        /// </summary>
        /// <param name="host">Ссылка на сервис</param>
        /// <param name="answer">Введенная пользователем команда</param>
        private static void RunCommand(ServiceHost host, string answer)
        {
            string value = string.Empty;
            var command = GetCommand(answer);

            switch (command)
            {
                case Command.Start:
                    if (host.State != CommunicationState.Opened)
                    {
                        host.Open();
                        _logger.Debug("host.Open()");
                        Console.WriteLine("Host started");
                    }
                    else
                        Console.WriteLine("Host already started");
                    break;

                case Command.Exit:
                    if (isChanged)
                    {
                        Console.WriteLine("Save settings? (y/n)\t");
                        if (Console.ReadLine().ToLower() == "y")
                        {
                            WService.SaveSettings();
                            _logger.Debug("SaveSettings()");
                        }
                    }
                    isRun = false;
                    break;

                case Command.User:
                    value = answer.Replace(" ", "");
                    value = value.Remove(0, Command.User.ToString().Length);
                    if (value.Length != 0)
                    {
                        WService.SetUser(value);
                        isChanged = true;
                        _logger.Debug("DBuser = {0}", value);
                    }
                    else
                        Console.WriteLine("Incorrect value. Try again");
                    value = string.Empty;
                    break;

                case Command.Pass:
                    ConsoleKeyInfo key;

                    Console.Write("Enter password: ");
                    do
                    {
                        key = Console.ReadKey(true);
                        if (key.Key == ConsoleKey.Enter)
                            break;
                        if (key.Key == ConsoleKey.Backspace)
                        {
                            if (value.Length != 0)
                            {
                                value = value.Remove(value.Length - 1);
                                Console.Write("\b \b");
                            }
                        }
                        else
                        {
                            value += key.KeyChar;
                            Console.Write("*");
                        }
                    } while (true);
                    Console.WriteLine();
                    WService.SetPassword(value);
                    isChanged = true;
                    _logger.Debug("Password updated");
                    value = string.Empty;
                    break;

                case Command.Server:
                    value = answer.Replace(" ", "");
                    value = value.Remove(0, Command.Server.ToString().Length);
                    if (value.Length != 0)
                    {
                        WService.SetServer(value);
                        isChanged = true;
                        _logger.Debug("DBserver = {0}", value); 
                    }
                    else
                        Console.WriteLine("Incorrect value. Try again");
                    value = string.Empty;
                    break;

                case Command.Port:
                    value = answer.Replace(" ", "");
                    value = value.Remove(0, Command.Port.ToString().Length);
                    uint port = 0;
                    if (uint.TryParse(value, out port) && port < 65535)
                    {
                        WService.SetPort(port);
                        isChanged = true;
                        _logger.Debug("DBport = {0}", port);
                    }
                    else
                        Console.WriteLine("Incorrect value. Try again");
                    value = string.Empty;
                    break;

                case Command.Help:
                    PrintHelp();
                    break;

                default:
                    Console.WriteLine("Unknown command. Print '{0}' to view list of available commands", Command.Help.ToString().ToLower());
                    break;
            }
        }

        /// <summary>
        /// Выводит в консоль перечень доступных команд
        /// </summary>
        private static void PrintHelp()
        {
            Console.WriteLine("\nAvailable commands:\n");
            Console.WriteLine("{0,-20} - Start service", Command.Start.ToString().ToLower());
            Console.WriteLine("{0,-20} - Set username for DB connection", Command.User.ToString().ToLower() + " [username]");
            Console.WriteLine("{0,-20} - Set password for DB connection", Command.Pass.ToString().ToLower());
            Console.WriteLine("{0,-20} - Set server for DB connection", Command.Server.ToString().ToLower() + " [server]");
            Console.WriteLine("{0,-20} - Set port for DB connection", Command.Port.ToString().ToLower() + " [port]");
            Console.WriteLine("{0,-20} - Stop service and exit programm\n", Command.Exit.ToString().ToLower());
        }
    }
}
