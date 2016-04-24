using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;

namespace WeatherServiceHost
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var host = new ServiceHost(typeof(WeatherService.WeatherService)))
            {
                host.Open();

                Console.WriteLine("Host has been started...");
                Console.ReadLine();
            }
        }
    }
}
