using System;

namespace GisMeteoWeather
{
    class Program
    {
        public static bool IsRun { get; set; }

        public static void Main(string[] args)
        {
            NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();
            IsRun = true;
            string answer = string.Empty;

            Console.WriteLine(">>> Gismeteo weather parser <<<");

            Worker.Setup();

            Console.WriteLine("\nAvailable commands:\n");
            Worker.ShowCommand();
            while (IsRun)
            {
                answer = Console.ReadLine();
                Worker.RunCommand(answer);
            }
            _logger.Debug("Application closed");
        }
    }
}
