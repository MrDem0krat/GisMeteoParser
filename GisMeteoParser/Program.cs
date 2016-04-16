using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ParserLib;

namespace GisMeteoWeather
{
    class Program
    {
        static void Main(string[] args)
        {
            bool isRun = true;
            string answer = String.Empty;

            Parser parser = new Parser(Properties.Resources.TargetSiteUrl, new TimeSpan(0, 1, 0), Worker.GetCityList());

            //parser.WeatherParsed += Worker.parser_WeatherParsed;
            parser.ParserStarted += Worker.parser_Started;
            parser.ParserStopped += Worker.parser_Stopped;
            parser.ParserAsleep += Worker.parser_Asleep;

            parser.ParserHandler = new Parser.ParserGetDataHandler(Worker.ParseWeatherData);
            parser.StartAsync();

            #region Обработка пользовательского ввода
            while (isRun)
            {
                answer = Console.ReadLine().ToLower();
                switch (answer)
                {
                    case "exit":
                        isRun = false;
                        if (parser.Status == ParserStatus.Started || parser.Status == ParserStatus.Sleeping)
                        {
                            parser.Stop();
                        }
                        break;
                    case "stop":
                        if (parser.Status == ParserStatus.Started || parser.Status == ParserStatus.Sleeping)
                        {
                            parser.Stop();
                        }
                        else
                        {
                            Console.WriteLine("Parser already stopped!");
                        }
                        break;
                    case "start":
                        if (parser.Status == ParserStatus.Stoped || parser.Status == ParserStatus.Aborted)
                        {
                            parser.StartAsync();
                        }
                        else
                        {
                            Console.WriteLine("Parser already working!");
                        }
                        break;
                    case "?":
                        Console.WriteLine("List of available commands:\n\n" +
                                            "start\tStart parsing\n" +
                                            "stop\tStop parsing\n" +
                                            "status\tCurrent parser status\n" +
                                            "exit\tStop parsing and close application");
                        break;
                    case "status":
                        Console.WriteLine("Parser status is: {0}", parser.Status);
                        break;
                    case "":
                        break;
                    default:
                        Console.WriteLine("Unknown command! Print '?' to view list of available commands...");
                        break;
                }
            }
            #endregion
        }
    }
}
