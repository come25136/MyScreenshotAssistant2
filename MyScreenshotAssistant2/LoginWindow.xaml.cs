using System.Data;
using System.Text.RegularExpressions;
using System.Windows;

namespace MyScreenshotAssistant2
{
    /// <summary>
    /// LoginWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class LoginWindow : Window
    {
        CoreTweet.OAuth.OAuthSession session = CoreTweet.OAuth.Authorize(API_Keys.consumerKey, API_Keys.cosumerSecret);

        string url; // OAuth_url

        public LoginWindow()
        {
            Title = MainWindow.SoftwareTitle + " - Login";

            InitializeComponent();
        }

        // 認証用urlを既定のブラウザで開く
        private void OAuth_url_open_Button_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(url);
        }

        // 認証ボタンがクリックされた時に実行される
        private void OAuth_Button_Click(object sender, RoutedEventArgs e)
        {
            OAuth();
        }

        // PINコード入力欄でEnterキーが押された時に実行される
        private void OAuth_pin_Textbox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                OAuth();
            }
        }

        // 認証処理
        private void OAuth()
        {
            if (OAuth_pin_Textbox.Text == "")
            {
                //何も入力されていない場合
                Method.message("Error", "PINコードを入力してください");
            }
            else if (Regex.IsMatch(OAuth_pin_Textbox.Text, "/^[0-9]+$/"))
            {
                // 半角数字以外の文字列が入力されていた場合
                Method.message("Error", "半角数字を入力してください");
            }
            else
            {
                try
                {
                    CoreTweet.Tokens tokens = CoreTweet.OAuth.GetTokens(session, OAuth_pin_Textbox.Text);
                    CoreTweet.Tokens info = CoreTweet.Tokens.Create(API_Keys.consumerKey, API_Keys.cosumerSecret, tokens.AccessToken, tokens.AccessTokenSecret);

                    DataRow datarow = Method.AccountTable.NewRow();

                    datarow["UserId"] = info.Account.VerifyCredentials().ScreenName;
                    datarow["AccessToken"] = tokens.AccessToken;
                    datarow["AccessTokenSecret"] = tokens.AccessTokenSecret;
                    Method.AccountTable.Rows.Add(datarow);

                    Method.logfile("Info", "Authentication success.");

                    Hide();
                }
                catch (CoreTweet.TwitterException)
                {
                    // PINコードが間違っている場合
                    Method.message("Error", "正しいPINコードを入力してください");
                }
            }
        }

        // url取得
        public void OAuth_url()
        {
            url = session.AuthorizeUri.ToString();
            OAuth_url_Textbox.Text = url;
        }
    }
}
