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


            New_version_Label.Content = Update.msajson.version;
            Current_version_Label.Content = App.version;
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(Update.msajson.dlurl);
        }
    }
}
