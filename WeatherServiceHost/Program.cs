using NLog;
using NLog.Config;
using NLog.Targets;
using System;
using System.Linq;
using System.ServiceModel;
using System.Text;
using WeatherService;

namespace WeatherServiceHost
{
    class Program
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();
        static void Main(string[] args)
        {
            using (var host = new ServiceHost(typeof(WService)))
            {
                bool isRun = true;
                bool isChanged = false;
                string answer = string.Empty;
                string value = string.Empty;

                Console.WriteLine(">>>\tGismeteo Weather Service\t<<<\n");
                Console.WriteLine("Available commands:\n");
                Console.WriteLine("{0,-20} - Start service", Command.start);
                Console.WriteLine("{0,-20} - Set username for DB connection", Command.user + " [username]");
                Console.WriteLine("{0,-20} - Set password for DB connection", Command.pass + " [password]");
                Console.WriteLine("{0,-20} - Set server for DB connection", Command.server + " [server]");
                Console.WriteLine("{0,-20} - Set port for DB connection", Command.port + " [port]");
                Console.WriteLine("{0,-20} - Stop service and exit programm\n\n", Command.exit);

                ConfigLogger();
                _logger.Debug("Weather service started");
                

                while (isRun)
                {
                    answer = Console.ReadLine();

                    var command = GetCommand((from cid in (int[])Enum.GetValues(typeof(Command)) where answer.StartsWith(Enum.GetName(typeof(Command), cid)) select cid).FirstOrDefault());

                    switch (command)
                    {
                        case Command.start:
                            if(host.State != CommunicationState.Opened)
                            {
                                host.Open();
                                _logger.Debug("host.Open()");
                                Console.WriteLine("Host started");
                            }
                            else
                                Console.WriteLine("Host already started");
                            break;


                        case Command.exit:

                            if (isChanged)
                            {
                                Console.WriteLine("Save settings? (y/n)\t");
                                if (Console.ReadLine() == "y")
                                {
                                    WService.SaveSettings();
                                    _logger.Debug("SaveSettings()");
                                    _logger.Debug("host.Close()");
                                } 
                            }
                            isRun = false;
                            break;

                        case Command.user:
                            value = answer.Remove(0, Command.user.ToString().Length + 1);
                            WService.SetUser(value);
                            isChanged = true;
                            _logger.Debug("DBuser = {0}", value);
                            break;

                        case Command.pass:
                            value = answer.Remove(0, Command.pass.ToString().Length + 1);
                            WService.SetPassword(value);
                            isChanged = true;
                            _logger.Debug("DBpassword updated");

                            break;

                        case Command.server:
                            value = answer.Remove(0, Command.server.ToString().Length + 1);
                            WService.SetServer(value);
                            isChanged = true;
                            _logger.Debug("DBserver = {0}", value);
                            break;

                        case Command.port:
                            value = answer.Remove(0, Command.port.ToString().Length + 1);
                            uint port = 0;
                            if (uint.TryParse(value, out port) && port < 65535)
                            {
                                WService.SetPort(port);
                                isChanged = true;
                                _logger.Debug("DBport = {0}", port);
                            }
                            else
                                Console.WriteLine("Wrong port value");
                            break;
                        case Command.none:
                            Console.WriteLine("Command not found\n");
                            break;
                        default:
                            Console.WriteLine("Command not found\n");
                            break;
                    }
                }
                _logger.Debug("Service closed");
            }
        }

        enum Command
        {
            none = 0,
            start = 1,
            exit = 2,
            user = 3,
            pass = 4 ,
            server = 5,
            port = 6            
        }
        private static Command GetCommand(int commandID)
        {
            switch (commandID)
            {
                case (int)Command.start:
                    return Command.start;
                case (int)Command.exit:
                    return Command.exit;
                case (int)Command.user:
                    return Command.user;
                case (int)Command.pass:
                    return Command.pass;
                case (int)Command.server:
                    return Command.server;
                case (int)Command.port:
                    return Command.port;
                default:
                    return Command.none;

            }
        }

        static void ConfigLogger()
        {
            LoggingConfiguration config = new LoggingConfiguration();

            FileTarget fileTarget = new FileTarget();
            config.AddTarget("file", fileTarget);

            fileTarget.DeleteOldFileOnStartup = true; // Удалатья страый файл при запуске
            fileTarget.KeepFileOpen = false; //Не держать файл открытым
            fileTarget.ConcurrentWrites = true;
            fileTarget.Encoding = Encoding.GetEncoding(1251); // Кодировка файла логов
            fileTarget.ArchiveEvery = FileArchivePeriod.Day; // Период архивирования старых логов (нет смысла, если старый удаляется при запуске)
            fileTarget.Layout = NLog.Layouts.Layout.FromString("${uppercase:${level}} | ${longdate} :\t${message}"); // Структура сообщения
            fileTarget.FileName = NLog.Layouts.Layout.FromString("${basedir}/logs/${shortdate}.log"); //Структура названия файлов
            fileTarget.ArchiveFileName = NLog.Layouts.Layout.FromString("${basedir}/logs/archives/{shortdate}.rar"); // Структура названия архивов

            LoggingRule ruleFile = new LoggingRule("*", LogLevel.Trace, fileTarget); // Минимальный уровень логгирования - Trace
            config.LoggingRules.Add(ruleFile);

            LogManager.Configuration = config;
        }
    }
}
