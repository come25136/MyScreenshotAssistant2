using System.Windows;

namespace MyScreenshotAssistant2
{
    /// <summary>
    /// UpdateWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class UpdateWindow : Window
    {
        public UpdateWindow()
        {
            InitializeComponent();

            DataContext = new update()
            {
                new_version = Update.msajson.version,
                current_version = MainWindow.version
            };
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(Update.msajson.dlurl);
        }
    }
}
