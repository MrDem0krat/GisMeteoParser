using System;
using System.Windows;
using NLog;

namespace GismeteoClient
{
    /// <summary>
    /// Логика взаимодействия для LogWnd.xaml
    /// </summary>
    public partial class LogWnd : Window
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();

        public LogWnd()
        {
            InitializeComponent();
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            dgLogViewer.ItemsSource = MainWindow.logMsgBuf;
            _logger.Trace("LogViewWnd is shown");
        }

        /// <summary>
        /// Закрывает окно просмотра логов
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void bClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            MainWindow.logWnd = null;
        }
    }

    /// <summary>
    /// Сущность, описывающаю структуру сообщения в логах
    /// </summary>
    public class LoggerMeassage
    {
        /// <summary>
        /// Уровень сообщения
        /// </summary>
        public string Level { get; set; }
        /// <summary>
        /// Время сообщения 
        /// </summary>
        public string Time { get; set; }
        /// <summary>
        /// Текст сообщения
        /// </summary>
        public string Message { get; set; }

        public LoggerMeassage() {}
        public LoggerMeassage(string level, string time, string message)
        {
            Level = level;
            Time = time;
            Message = message;
        }
        public override string ToString()
        {
            return string.Format("[{0}] : [{1}]\t{2}", Time, Level, Message);
        }
    }
}
