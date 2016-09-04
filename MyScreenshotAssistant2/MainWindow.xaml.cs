using CoreTweet;
using System;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Cache;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media.Imaging;

namespace MyScreenshotAssistant2
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        private NotifyIcon notifyIcon1;
        private Icon[] Icons = new Icon[] { Properties.Resources.connecting1, Properties.Resources.connecting2, Properties.Resources.connecting3, Properties.Resources.connecting4 };

        public static string SoftwareName = System.Windows.Forms.Application.ProductName;
        public static string version = " " + System.Windows.Forms.Application.ProductVersion;

        private Tokens tokens = null;
        private FileSystemWatcher watcher = null;

        private bool IconFlag = false;

        public MainWindow()
        {
            InitializeComponent();

            Title = Title + version;

            Tasktry();

            Method.sql_login();

            Update.Start();

            // アカウントデータの復元
            Method.AccountAdapter = new SQLiteDataAdapter("SELECT * FROM Account", Method.database);
            Method.AccountAdapter.Fill(Method.AccountTable);

            Twitter_id.ItemsSource = Method.AccountTable.DefaultView;
            Twitter_id.DisplayMemberPath = "UserId";
            Twitter_id.SelectedValuePath = "UserId";

            DataRow datarow = Method.AccountTable.NewRow();
            datarow["UserId"] = "アカウントを追加";
            Method.AccountTable.Rows.InsertAt(datarow, 0);

            // ディレクトリデータの復元
            Method.DirectoryAdapter = new SQLiteDataAdapter("SELECT * FROM DirectoryData", Method.database);
            Method.DirectoryAdapter.Fill(Method.DirectoryTable);

            Directory_name_ComboBox.ItemsSource = Method.DirectoryTable.DefaultView;
            Directory_name_ComboBox.DisplayMemberPath = "name";
            Directory_name_ComboBox.SelectedValuePath = "path";

            DirectoryData_DataGrid.ItemsSource = Method.DirectoryTable.DefaultView;

            // ユーザーデータの復元
            Twitter_id.Text = Properties.Settings.Default.Twitter_id;
            Tweet_fixed_value_TextBox.Text = Properties.Settings.Default.Tweet_fixed_value;
            Directory_name_ComboBox.Text = Properties.Settings.Default.Directory_name;
        }

        // アカウント選択
        private async void Twitter_id_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (Dispatcher.Invoke(() => { return Twitter_id.SelectedValue.ToString(); }) == "アカウントを追加")
            {
                Dispatcher.Invoke(() =>
                {
                    LoginWindow LoginWindow = new LoginWindow();
                    LoginWindow.OAuth_url();
                    LoginWindow.ShowDialog();
                });
                return;
            }

            Dispatcher.Invoke(() => { ProgressBar.Visibility = Visibility.Visible; });

            var row = Twitter_id.SelectedIndex;

            // Twitter API 認証情報
            tokens = CoreTweet.Tokens.Create(API_Keys.consumerKey, API_Keys.cosumerSecret, Convert.ToString(Method.AccountTable.Rows[row]["AccessToken"]), Convert.ToString(Method.AccountTable.Rows[row]["AccessTokenSecret"]));

            DataContext = new Person()
            {
                AccountIcon = await Dispatcher.InvokeAsync(() =>
                {
                    var wc = new WebClient { CachePolicy = new RequestCachePolicy(RequestCacheLevel.CacheIfAvailable) };
                    try
                    {
                        var image = new BitmapImage();
                        image.BeginInit();
                        image.StreamSource = new MemoryStream(wc.DownloadData(tokens.Account.VerifyCredentials().ProfileImageUrlHttps.Replace("normal", "bigger")));
                        image.EndInit();
                        image.Freeze();

                        return image;
                    }
                    catch (Exception)
                    {
                        wc.Dispose();
                        return null;
                    }
                }),

                AccountName = await Dispatcher.InvokeAsync(() =>
                {
                    try
                    {
                        return tokens.Account.VerifyCredentials().Name;
                    }
                    catch (TwitterException)
                    {
                        Method.message("Twitter認証エラー", "Twitterへの認証回数が上限に達しました、しばらくしてから再度お試しください");
                        Method.logfile("Warning", "Rate limit exceeded.");

                        return null;
                    }
                })
            };

            await Dispatcher.InvokeAsync(() => { ProgressBar.Visibility = Visibility.Hidden; });
        }

        // Start, Stopボタン
        private void Tweet_Button_Click(object sender, RoutedEventArgs e)
        {
            if ((bool)Tweet_Button.IsChecked)
            {
                watcher_start();
            }
            else
            {
                watcher_stop();
            }
        }

        // ディレクトリの監視を開始 (メソッド)
        private void watcher_start()
        {
            if (tokens == null)
            {
                Tweet_Button.IsChecked = false;
                Method.message("Error", "Twitterアカウントが選択されていません");
                return;
            }

            try
            {
                if (Convert.ToString(Directory_name_ComboBox.SelectedValue) == "-1")
                {
                    Tweet_Button.IsChecked = false;
                    Method.message("Error", "ディレクトリが記入されていません");
                    return;

                }
            }
            catch (Exception)
            {
                Tweet_Button.IsChecked = false;
                Method.message("Error", "設定データが見つかりません");
                return;
            }

            try
            {
                watcher = new FileSystemWatcher();
                watcher.Path = Convert.ToString(Directory_name_ComboBox.SelectedValue);
                watcher.NotifyFilter =
                    (NotifyFilters.LastAccess
                    | NotifyFilters.LastWrite
                    | NotifyFilters.FileName
                    | NotifyFilters.DirectoryName); ;
                watcher.Filter = "*.png";

                watcher.Created += new FileSystemEventHandler(watcher_Changed);

                watcher.EnableRaisingEvents = true;

                notifyIcon1.Icon = Properties.Resources.done;
            }
            catch (ArgumentException)
            {
                Tweet_Button.IsChecked = false;
                Method.message("Error", "ディレクトリが存在しません");
                watcher = null;
                return;
            }

            Title = SoftwareName + " - Status start" + version;

            Method.logfile("Info", "Assistant start.");

            Tasktray_notify("info", "Assistant start");
        }

        // ディレクトリの監視を止める (メソッド)
        private void watcher_stop()
        {
            watcher.EnableRaisingEvents = false;
            watcher.Dispose();
            watcher = null;

            Title = SoftwareName + " - Status stop" + version;

            Method.logfile("Info", "Assistant stop.");

            Tasktray_notify("info", "Assistant stop");

            notifyIcon1.Icon = Properties.Resources.warning;
        }

        // ディレクトリに変化があった時の処理
        private async void watcher_Changed(object source, FileSystemEventArgs e)
        {
            switch (e.ChangeType)
            {
                case WatcherChangeTypes.Created:

                    // ファイルサイズの確認(5MB)
                    if (e.FullPath.Length < 5242880)
                    {
                        try
                        {
                            await Dispatcher.InvokeAsync(() => { Tasktray_animation(); });

                            if (!(bool)await Dispatcher.InvokeAsync(() => { return Mode_Button.IsChecked; }))
                            {
                                Dispatcher.Invoke(() => { new TweetWindow().ShowDialog(); });
                                if (TweetWindow.cancel_flag)
                                {
                                    break;
                                }
                            }
                            MediaUploadResult file = tokens.Media.Upload(media: new FileStream(e.FullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));

                            await tokens.Statuses.UpdateAsync(
                                status: (Dispatcher.Invoke(() => { return Tweet_fixed_value_TextBox.Text.Replace(@"\n", "\n") + TweetWindow.value + Tweet_Hashtag_value_TextBox.Text.Replace(@"\n", "\n"); })),
                                media_ids: new long[] { file.MediaId }
                            );
                            Method.logfile("Info", "Success tweet.");
                            TweetWindow.value = null;
                        }
                        catch (Exception ex)
                        {
                            Method.logfile("Warning", ex.Message);

                            Tasktray_notify("warning", "ツイートに失敗しました");
                        }
                        finally
                        {
                            IconFlag = false;
                        }
                    }
                    else
                    {
                        Method.logfile("Warning", "File size over 5MB " + e.FullPath);

                        Tasktray_notify("warning", "ファイルサイズが5MBを超えています");
                    }
                    break;
            }
        }

        // ウィンドウを閉じた時の処理
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;

            Hide();

            Tasktray_notify("info", "タスクトレイに常駐しています");
        }

        // 終了処理
        private void exit(object sender, EventArgs e)
        {
            if (watcher != null)
            {
                watcher.EnableRaisingEvents = false;
                watcher.Dispose();
                watcher = null;

                Method.logfile("Info", "Assistant stop.");
            }

            // ユーザーデータを保存
            if (Twitter_id.Text != "アカウントを追加")
            {
                Properties.Settings.Default.Twitter_id = Twitter_id.Text;
            }
            Properties.Settings.Default.Tweet_fixed_value = Tweet_fixed_value_TextBox.Text;
            Properties.Settings.Default.Directory_name = Directory_name_ComboBox.Text;
            Properties.Settings.Default.Save();

            // アカウント情報を保存
            try
            {
                Method.AccountTable.Rows.RemoveAt(0);

                SQLiteCommandBuilder builder = new SQLiteCommandBuilder(Method.AccountAdapter);
                builder.GetUpdateCommand();

                Method.AccountAdapter.Update(Method.AccountTable);
            }
            catch (Exception) { }

            // ディレクトリデータを保存
            try
            {
                SQLiteCommandBuilder builder2 = new SQLiteCommandBuilder(Method.DirectoryAdapter);
                builder2.GetUpdateCommand();

                Method.DirectoryAdapter.Update(Method.DirectoryTable);
            }
            catch (Exception) { }


            Method.database.Close();

            Method.logfile("Info", "Disconnected from the database.");

            Method.logfile("Info", "Application Exit.");

            notifyIcon1.Dispose();

            Environment.Exit(0);
        }

        // タスクトレイ登録処理
        private async void Tasktry()
        {
            await Dispatcher.InvokeAsync(() =>
            {
                notifyIcon1 = new NotifyIcon();
                notifyIcon1.Text = Title;
                notifyIcon1.Icon = Properties.Resources.warning;

                notifyIcon1.BalloonTipTitle = SoftwareName;

                notifyIcon1.Visible = true;

                ContextMenuStrip menuStrip = new ContextMenuStrip();

                ToolStripMenuItem exitItem = new ToolStripMenuItem();
                exitItem.Text = "終了";
                menuStrip.Items.Add(exitItem);
                exitItem.Click += new EventHandler(exit);

                notifyIcon1.Click += new EventHandler(Tasktray_click);

                notifyIcon1.ContextMenuStrip = menuStrip;
            });
        }

        // タスクトレイクリック
        private void Tasktray_click(object sender, EventArgs e)
        {
            Show();
        }

        // タスクトレイ通知
        private void Tasktray_notify(string level, string value)
        {
            if (level == "info")
            {
                notifyIcon1.BalloonTipIcon = ToolTipIcon.Info;
            }
            else if (level == "warning")
            {
                notifyIcon1.BalloonTipIcon = ToolTipIcon.Warning;
            }
            else if (level == "error")
            {
                notifyIcon1.BalloonTipIcon = ToolTipIcon.Error;
            }
            else
            {
                notifyIcon1.BalloonTipIcon = ToolTipIcon.None;
            }

            notifyIcon1.BalloonTipText = value;
            notifyIcon1.ShowBalloonTip(2000);
        }

        // タスクトレイ アニメーション
        private async void Tasktray_animation()
        {
            await Task.Run(() =>
            {
                int i;
                IconFlag = true;
                while (IconFlag)
                {
                    for (i = 0; i <= 3; i++)
                    {
                        notifyIcon1.Icon = Icons[i];
                        Thread.Sleep(200);
                    }
                }
                notifyIcon1.Icon = Properties.Resources.done;
            });
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Method.DirectoryTable.Rows.RemoveAt(DirectoryData_DataGrid.SelectedIndex);
            }
            catch (Exception) { }
        }

        private async void Directory_name_ComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            await Dispatcher.InvokeAsync(() =>
            {
                try
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(Directory_name_ComboBox.SelectedValue + "\\" + Method.getNewestFileName(Convert.ToString(Directory_name_ComboBox.SelectedValue)));
                    bitmap.EndInit();
                    bitmap.Freeze();
                    
                    Preview_Image.ImageSource = bitmap;
                }
                catch (Exception) { }
            });
        }
    }
}
