using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using NLog;

namespace GismeteoClient
{
    public static class MainWndInterface
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();
        /// <summary>
        /// Хранит ссылку на объект главного окна
        /// </summary>
        public static MainWindow MainWindowReference { private get; set; }

        
    }
}
