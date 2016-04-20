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
            Dictionary<int, string> cities = new Dictionary<int, string>();
            IWeatherInfo weather = new Weather(123,DateTime.Now, DayPart.Morning,"cool", 75,768,25,"ololo,","south",2.1f);

            Parser parser = new Parser(Properties.Resources.TargetSiteUrl, new TimeSpan(0, 1, 0), Worker.GetCityList());

            //parser.WeatherParsed += Worker.parser_WeatherParsed;
            parser.ParserStarted += Worker.parser_Started;
            parser.ParserStopped += Worker.parser_Stopped;
            parser.ParserAsleep += Worker.parser_Asleep;

            parser.ParserHandler = new Parser.ParserGetDataHandler(Worker.ParseWeatherData);

            Console.WriteLine(">>>\tGismeteo weather parser\t<<<\n\nWrite '?' to view help...");

            #region Обработка пользовательского ввода
            while (isRun)
            {
                answer = Console.ReadLine().ToLower();
                switch (answer)
                {
                    case "?":
                        Console.WriteLine("Список доступных команд:\n\n{0,-20}Запустить парсер\n{1,-20}Остановить парсер\n{2,-20}Текущий статус парсера\n{3,-20}Загрузить список городов с главной страницы\n"+
                            "{4,-20}Подготовить базу данных для работы\n{5,-20}Записать список городов в базу данных\n{6,-20}Записать прогноз погоды в базу данных\n{7,-20}Ввести пароль к БД (root)\n" +
                            "{8,-20}Остановить парсер и закрыть приложение\n",
                                            "start", "stop", "status", "city list", "prepare", "write cl", "write w", "password", "exit");
                        break;

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
                            Console.WriteLine("Парсер уже остановлен!");
                        }
                        break;

                    case "start":
                        if (parser.Status == ParserStatus.Stoped || parser.Status == ParserStatus.Aborted)
                        {
                            parser.StartAsync();
                        }
                        else
                        {
                            Console.WriteLine("Парсер уже запущен!");
                        }
                        break;

                    case "status":
                        Console.WriteLine("Статус парсера: {0}", parser.Status);
                        break;
                    case "password":
                        Console.Write("Введите пароль: ");
                        DataBase.Password = Console.ReadLine();
                        Properties.Settings.Default.Save();
                        Console.WriteLine("Пароль успешно обновлен и сохранен");
                        break;
                    case "city list":
                        cities = Worker.GetCityList();
                        Console.WriteLine("Список городов успешно загружен.");
                        break;
                    case "prepare":
                        DataBase.Prepare();
                        Console.WriteLine("База данных подготовлена");
                        break;

                    case "write cl":
                        if (DataBase.WriteCityList(cities))
                            Console.WriteLine("Список городов успешно записан в базу данных");
                        else
                            Console.WriteLine("Во время записи списка городов произошла ошибка. ");
                        foreach (var item in cities)
                            Console.WriteLine(item.Value);
                        break;

                    case "write w":
                        if (DataBase.WriteCityList(cities))
                            Console.WriteLine("Прогноз погоды успешно записан в базу данных");
                        else
                            Console.WriteLine("Во время записи прогноза погоды произошла ошибка. ");
                        break;

                    case "":
                        break;

                    default:
                        Console.WriteLine("Неизвестная команда! Введите '?' для просмотра списка доступных команд...");
                        break;
                }
            }
            #endregion
        }
    }
}
