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
            Dictionary<int, string> test = new Dictionary<int, string>();
            Parser parser = new Parser(Properties.Resources.TargetSiteUrl);
            parser.WeatherParsed += parser_WeatherParsed;

            parser.Cities = Worker.GetCityList();

            parser.RegisterParserHandler(new Parser.ParserGetDataHandler(Worker.ParseWeatherData));
            parser.Start();

            Console.ReadLine();
        }

        static void parser_WeatherParsed(object sender, WeatherParsedEventArgs e)
        {
            if(!e.HasError)
            {
                Console.WriteLine(e.WeatherItem);
            }
            else
            {
                Console.WriteLine("Has errors:\t" + e.HasError);
                Console.WriteLine("Error text:\t" + e.ErrorText);
                Console.WriteLine("Exception:\t" + e.Exception.Message);
            }
        }
    }
}
