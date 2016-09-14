using System.Windows;

namespace MyScreenshotAssistant2
{
    /// <summary>
    /// App.xaml の相互作用ロジック
    /// </summary>
    public partial class App : Application
    {
        public static string SoftwareName = System.Windows.Forms.Application.ProductName;
        public static string version = System.Windows.Forms.Application.ProductVersion;
        public static string SoftwareTitle = SoftwareName + " ver:" + version;
    }
}
