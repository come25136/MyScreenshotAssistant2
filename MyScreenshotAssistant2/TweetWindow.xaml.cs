using System;
using System.Windows;
using System.Windows.Input;

namespace MyScreenshotAssistant2
{
    /// <summary>
    /// TweetWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class TweetWindow : Window
    {
        public static string value;
        public static bool cancel_flag = false;
        private bool close_flag;

        public TweetWindow()
        {
            InitializeComponent();

            Activate();

            Tweet_value_TextBox.Focus();

            cancel_flag = true;
        }

        private void Tweet_value_TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            // ツイート
            if (Keyboard.IsKeyDown(Key.LeftCtrl) && Keyboard.IsKeyDown(Key.Enter))
            {
                value = '\n' + Tweet_value_TextBox.Text;
                close_flag = false;
                cancel_flag = false;
                Close();
            }
            else if (Keyboard.IsKeyDown(Key.Escape))
            {
                cancel_flag = true;
                Close();
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (close_flag == true)
            {
                cancel_flag = true;
            }
        }
    }
}
