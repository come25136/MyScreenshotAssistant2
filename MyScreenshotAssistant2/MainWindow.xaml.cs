using CoreTweet;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.IO;
using System.Linq;
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

        private Tokens tokens = null;
        private List<FileStream> Images = new List<FileStream>();

        private FileSystemWatcher watcher = new FileSystemWatcher();

        private bool IconFlag = false;

        private RamGecTools.KeyboardHook keyboardHook = new RamGecTools.KeyboardHook();
        private RamGecTools.KeyboardHook.VKeys next_key;

        public MainWindow()
        {
            InitializeComponent();

            Title = App.SoftwareTitle;

            Update.Start();

            Method.sql_login();

            Tasktry();

            // アカウントデータの復元
            Method.AccountAdapter = new SQLiteDataAdapter("SELECT * FROM Account", Method.database);
            Method.AccountAdapter.Fill(Method.AccountTable);

            Twitter_id.ItemsSource = Method.AccountTable.DefaultView;
            Twitter_id.DisplayMemberPath = "TwitterId";
            Twitter_id.SelectedValuePath = "TwitterId";

            DataRow datarow = Method.AccountTable.NewRow();
            datarow["TwitterId"] = "アカウントを追加";
            Method.AccountTable.Rows.InsertAt(datarow, 0);

            // ディレクトリデータの復元
            Method.DirectoryAdapter = new SQLiteDataAdapter("SELECT * FROM DirectoryData", Method.database);
            Method.DirectoryAdapter.Fill(Method.DirectoryTable);

            Directory_name_ComboBox.ItemsSource = Method.DirectoryTable.DefaultView;
            Directory_name_ComboBox.DisplayMemberPath = "name";
            Directory_name_ComboBox.SelectedValuePath = "path";

            DirectoryData_DataGrid.ItemsSource = Method.DirectoryTable.DefaultView;

            // user data
            Method.ApplicationAdapter = new SQLiteDataAdapter("SELECT * FROM ApplicationData", Method.database);
            Method.ApplicationAdapter.Fill(Method.ApplicationTable);

            // ユーザーデータの復元
            Twitter_id.Text = Method.ApplicationTable.Select("AppName = '" + App.SoftwareName + "'")[0][1].ToString();

            keyboardHook.KeyDown += new RamGecTools.KeyboardHook.KeyboardHookCallback(keyboardHook_KeyDown);
            keyboardHook.Install();
        }

        // アカウント選択
        private async void Twitter_id_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            await Task.Run(() =>
            {
                Dispatcher.Invoke(() => { ProgressBar.Visibility = Visibility.Visible; });

                if (Dispatcher.Invoke(() => { return Twitter_id.SelectedValue.ToString(); }) == "アカウントを追加")
                {
                    Dispatcher.Invoke(() =>
                    {
                        LoginWindow LoginWindow = new LoginWindow();
                        LoginWindow.OAuth_url();
                        LoginWindow.ShowDialog();

                        ProgressBar.Visibility = Visibility.Hidden;

                        tokens = null;
                        Twitter_icon_Image.ImageSource = null;
                        Twitter_name.Content = "アカウントを選択してください";
                    });

                    return;
                }

                Dispatcher.Invoke(() => { Directory_name_ComboBox.Text = Method.AccountTable.Rows[Twitter_id.SelectedIndex]["Directory_name"].ToString(); });

                var row = Dispatcher.Invoke(() => { return Twitter_id.SelectedIndex; });

                // Twitter API 認証情報
                tokens = Tokens.Create(API_Keys.consumerKey, API_Keys.cosumerSecret, Method.AccountTable.Rows[row]["AccessToken"].ToString(), Method.AccountTable.Rows[row]["AccessTokenSecret"].ToString());

                using (var wc = new WebClient { CachePolicy = new RequestCachePolicy(RequestCacheLevel.CacheIfAvailable) })
                {
                    try
                    {
                        var image = new BitmapImage();
                        image.BeginInit();
                        image.StreamSource = new MemoryStream(wc.DownloadData(tokens.Account.VerifyCredentials().ProfileImageUrlHttps.Replace("normal", "bigger")));
                        image.EndInit();
                        image.Freeze();

                        Dispatcher.Invoke(() => { Twitter_icon_Image.ImageSource = image; });
                    }
                    catch (Exception)
                    {
                        Dispatcher.Invoke(() => { Twitter_icon_Image.ImageSource = null; });
                    }
                }

                Dispatcher.Invoke(() =>
                {
                    try
                    {
                        Twitter_name.Content = tokens.Account.VerifyCredentials().Name;
                    }
                    catch (TwitterException)
                    {
                        Method.message("Twitter認証エラー", "Twitterへの認証回数が上限に達しました、しばらくしてから再度お試しください");
                        Method.logfile("Warning", "Rate limit exceeded.");

                        Twitter_name.Content = null;
                    }

                    ProgressBar.Visibility = Visibility.Hidden;
                });
            });
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
                if (Directory_name_ComboBox.SelectedValue.ToString() == string.Empty)
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

            if (sleep_time.Text == string.Empty)
            {
                Tweet_Button.IsChecked = false;
                Method.message("Error", "スクリーンショット検知からツイートまでの待機時間が指定されていません");
                return;
            }

            try
            {
                watcher.Path = Directory_name_ComboBox.SelectedValue.ToString();
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
                return;
            }

            Title = App.SoftwareTitle + " - Status start";

            Method.logfile("Info", "Assistant start.");

            Tasktray_notify("Info", "Assistant start");
        }
        
        // ディレクトリの監視を止める (メソッド)
        private void watcher_stop()
        {
            watcher.EnableRaisingEvents = false;
            watcher.Dispose();

            Title = App.SoftwareTitle + " - Status stop";

            Method.logfile("Info", "Assistant stop.");

            Tasktray_notify("Info", "Assistant stop");

            notifyIcon1.Icon = Properties.Resources.warning;
        }

        // ディレクトリに変化があった時の処理
        private async void watcher_Changed(object source, FileSystemEventArgs e)
        {
            switch (e.ChangeType)
            {
                case WatcherChangeTypes.Created:

                    // ファイルサイズの確認(5MB)
                    if (e.FullPath.Length < 5120000)
                    {
                        try
                        {
                            await Task.Run(() =>
                            {
                                Tasktray_animation();

                                if (Dispatcher.Invoke(() => { return Next_key_TextBox.Text; }) == next_key.ToString() && Images.Count < 3)
                                {
                                    try
                                    {
                                        Thread.Sleep(Dispatcher.Invoke(() => { return Convert.ToInt32(sleep_time.Text); }));
                                        Images.Add(new FileStream(e.FullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
                                        Tasktray_notify("Info", Images.Count + "枚目の処理が完了しました");

                                        IconFlag = false;
                                        return;
                                    }
                                    catch (Exception er)
                                    {
                                        Tasktray_notify("Warning", Images.Count + "枚目の処理が正常に完了しませんでした");
                                        Method.logfile("Warning", er.Message);
                                    }
                                }
                                else
                                {
                                    Thread.Sleep(Dispatcher.Invoke(() => { return Convert.ToInt32(sleep_time.Text); }));
                                    Images.Add(new FileStream(e.FullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
                                }

                                if (!(bool)Dispatcher.Invoke(() => { return Mode_Button.IsChecked; }))
                                {
                                    Dispatcher.Invoke(() => { new TweetWindow().ShowDialog(); });
                                    if (TweetWindow.cancel_flag)
                                    {
                                        Images.Clear();
                                        return;
                                    }
                                }

                                tokens.Statuses.Update(
                                    status: Dispatcher.Invoke(() => { return Tweet_fixed_value_TextBox.Text.Replace(@"\n", "\n") + TweetWindow.value + " " + Tweet_Hashtag_value_TextBox.Text.Replace(@"\n", "\n"); }),
                                    media_ids: Images.Select(x => tokens.Media.Upload(media: x).MediaId)
                                );

                                Method.logfile("Info", "Success tweet.");
                                TweetWindow.value = null;
                                Images.Clear();
                            });
                        }
                        catch (Exception er)
                        {
                            Method.logfile("Warning", er.Message);

                            Tasktray_notify("Warning", "ツイートに失敗しました");
                        }
                        finally
                        {
                            IconFlag = false;
                        }
                    }
                    else
                    {
                        Method.logfile("Warning", "File size over 5MB " + e.FullPath);

                        Tasktray_notify("Warning", "ファイルサイズが5MBを超えています");
                    }
                    break;
            }
        }

        // ウィンドウを閉じた時の処理
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;

            Hide();

            Tasktray_notify("Info", "タスクトレイに常駐しています");
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

            // アカウント情報を保存
            try
            {
                Method.AccountTable.Rows.RemoveAt(0);

                new SQLiteCommandBuilder(Method.AccountAdapter).GetUpdateCommand();

                Method.AccountAdapter.Update(Method.AccountTable);
            }
            catch (Exception) { }

            // ディレクトリデータを保存
            try
            {
                new SQLiteCommandBuilder(Method.DirectoryAdapter).GetUpdateCommand();

                Method.DirectoryAdapter.Update(Method.DirectoryTable);
            }
            catch (Exception)
            {
                Method.message("Warning", "同じディレクトリ設定が存在します");
                return;
            }

            // アプリケーションデータを保存
            try
            {
                Method.ApplicationTable.Select("AppName = '" + App.SoftwareName + "'")[0][1] = Dispatcher.Invoke(() => { return Twitter_id.SelectedValue.ToString(); });

                Console.WriteLine(new SQLiteCommandBuilder(Method.DirectoryAdapter).GetUpdateCommand().CommandText);

                new SQLiteCommandBuilder(Method.ApplicationAdapter).GetUpdateCommand();

                Method.ApplicationAdapter.Update(Method.ApplicationTable);
            }
            catch (Exception) { }

            Method.database.Close();

            Method.logfile("Info", "Disconnected from the database.");

            Method.logfile("Info", "Application Exit.");

            notifyIcon1.Dispose();

            keyboardHook.Uninstall();

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

                notifyIcon1.BalloonTipTitle = App.SoftwareTitle;

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
            if (level == "Info")
            {
                notifyIcon1.BalloonTipIcon = ToolTipIcon.Info;
            }
            else if (level == "Warning")
            {
                notifyIcon1.BalloonTipIcon = ToolTipIcon.Warning;
            }
            else if (level == "Error")
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
            await Task.Run(() =>
            {
                try
                {
                    var dncsv = Dispatcher.Invoke(() => { try { return Directory_name_ComboBox.SelectedValue.ToString(); } catch { return null; } });

                    if (dncsv != null)
                    {
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.UriSource = new Uri(dncsv + "\\" + Method.getNewestFileName(dncsv));
                        bitmap.EndInit();
                        bitmap.Freeze();

                        Dispatcher.Invoke(() => { Preview_Image.ImageSource = bitmap; });
                    }
                    else
                    {
                        Dispatcher.Invoke(() => { Preview_Image.ImageSource = null; });
                    }
                }
                catch (Exception)
                {
                    Method.m_gc();
                }

                Dispatcher.Invoke(() =>
                {
                    if (Directory_name_ComboBox.SelectedIndex < Method.DirectoryTable.Rows.Count)
                    {
                        Method.AccountTable.Rows[Twitter_id.SelectedIndex]["Directory_name"] = Directory_name_ComboBox.Text;

                        Tweet_fixed_value_TextBox.Text = Method.DirectoryTable.Rows[Directory_name_ComboBox.SelectedIndex]["Tweet_fixed_value"].ToString();
                        Next_key_TextBox.Text = Method.DirectoryTable.Rows[Directory_name_ComboBox.SelectedIndex]["next_key"].ToString();
                        sleep_time.Text = Method.DirectoryTable.Rows[Directory_name_ComboBox.SelectedIndex]["sleep_time"].ToString();
                    }
                    else
                    {
                        Tweet_fixed_value_TextBox.Text = null;
                        Next_key_TextBox.Text = null;
                    }
                });
            });
        }

        private void Next_key_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                e.Handled = true;
                Next_key_TextBox.Text = next_key.ToString();

                try
                {
                    Method.DirectoryTable.Rows[Directory_name_ComboBox.SelectedIndex]["next_key"] = next_key.ToString();
                }
                catch { }
            });
        }

        private void Tweet_fixed_value_TextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                try
                {
                    Method.DirectoryTable.Rows[Directory_name_ComboBox.SelectedIndex]["Tweet_fixed_value"] = Tweet_fixed_value_TextBox.Text;
                }
                catch { }
            });
        }

        private void keyboardHook_KeyDown(RamGecTools.KeyboardHook.VKeys key)
        {
            next_key = key;
        }

        private void sleep_time_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                try
                {
                    Method.DirectoryTable.Rows[Directory_name_ComboBox.SelectedIndex]["sleep_time"] = Dispatcher.Invoke(() => { return sleep_time.Text; });
                }
                catch { }
            });
        }
    }
}
