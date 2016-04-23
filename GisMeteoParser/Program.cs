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
        public static bool isRun { get; set; }

        public static void Main(string[] args)
        {
            NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();
            isRun = true;
            string answer = string.Empty;
            

            Worker.Setup();

            Console.WriteLine(">>>\tGismeteo weather parser\t<<<\n\nWrite 'help' to view available commands list...");
            while (isRun)
            {
                answer = Console.ReadLine().ToLower();
                Worker.RunCommand(answer);
            }
        }

    }
}
