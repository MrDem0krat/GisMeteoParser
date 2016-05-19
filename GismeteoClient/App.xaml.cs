using System.Windows;
using NLog;

namespace GismeteoClient
{
    /// <summary>
    /// Логика взаимодействия для App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            //В случае крэша включить песню The Pixies - Where is my Mind﻿
            GismeteoClient.MainWindow._splashScreen.Show(false, true);
        }
    }
}
