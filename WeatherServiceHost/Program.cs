using NLog;
using NLog.Config;
using NLog.Targets;
using System;
using System.ServiceModel;
using System.Text;

namespace WeatherServiceHost
{
    class Program
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();
        static void Main(string[] args)
        {
            ConfigLogger();
            using (var host = new ServiceHost(typeof(WeatherService.WService)))
            {
                host.Open();
                _logger.Debug("Host started...");
                Console.WriteLine("Host started...");
                Console.ReadLine();
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
