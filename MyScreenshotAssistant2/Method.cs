using System;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Windows.Forms;
using System.Windows.Media.Imaging;

namespace MyScreenshotAssistant2
{
    class Method
    {
        public static DataTable DirectoryTable = new DataTable();
        public static DataTable AccountTable = new DataTable();
        public static DataTable ApplicationTable = new DataTable();

        public static SQLiteDataAdapter AccountAdapter;
        public static SQLiteDataAdapter DirectoryAdapter;
        public static SQLiteDataAdapter ApplicationAdapter;

        public static SQLiteConnection database = new SQLiteConnection("Data Source=msa.db");
        public static SQLiteCommand statement = null;
        public static SQLiteDataReader reader = null;

        /// <summary>MessageBox</summary>
        /// <param name="title">ダイアログタイトルを指定</param>
        /// <param name="value">ダイアログに表示する内容を指定</param>

        // MessageBoxのコード省略化
        public static void message(string title, string value)
        {
            MessageBox.Show(value,
                title,
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            return;
        }

        /// <summary>Write log file</summary>
        /// <param name="level">エラーレベルを指定</param>
        /// <param name="value">エラー内容を指定</param>

        // ログファイルへ記入
        public static void logfile(string level, string value)
        {
            try
            {
                System.IO.File.AppendAllText("MSA.log", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + " " + level + ": " + value + '\r' + '\n');
                return;
            }
            catch (Exception) { }
        }

        /// <summary>SQLite Logine</summary>

        // sqlのログイン処理
        public static void sql_login()
        {
            try
            {
                database.Open();
                logfile("Info", "connect database.");

                statement = database.CreateCommand();

                sql("create table if not exists Account (TwitterId string primary key, AccessToken string, AccessTokenSecret string, Directory_name string)");
                sql("create table if not exists DirectoryData (name string, path string primary key, Tweet_fixed_value string, next_key string, sleep_time int)");
                sql("create table if not exists ApplicationData (AppName string primary key, TwitterId string)");

                sql("INSERT INTO ApplicationData (AppName) SELECT '" + App.SoftwareName + "' WHERE NOT EXISTS ( SELECT AppName FROM ApplicationData WHERE AppName = '" + App.SoftwareName + "');");
            }
            catch (Exception)
            {
                logfile("Error", "Unable to connect to database.");
            }
        }

        /// <summary>SQLite sql</summary>
        /// <param name="value">sql文を指定</param>

        // sql文の実行
        public static void sql(string value)
        {
            try
            {
                statement.CommandText = value;
                statement.ExecuteNonQuery();
            }
            catch (Exception)
            {
                logfile("Error", "Failed to execute the sql statement.");
            }
        }

        /// <summary>SQLite sql_reader</summary>
        /// <param name="value">sql文を指定</param>
        /// <returns>sqlの実行結果を返します</returns>

        // sql文の実行
        public static SQLiteDataReader sql_reader(string value)
        {
            try
            {
                statement.CommandText = value;
                return statement.ExecuteReader();
            }
            catch (Exception)
            {
                logfile("Error", "Failed to execute the sql_reader statement.");
                return null;
            }
            finally
            {
                statement.Dispose();
            }
        }

        /// <summary>フォルダ内の最新pngファイルのファイル名を取得</summary>
        /// <param name="folderName">フォルダ名を指定</param>
        /// <returns>最新のpngファイルのファイル名を返します</returns>
        public static string getNewestFileName(string folderName)
        {
            try
            {
                string[] files = System.IO.Directory.GetFiles(folderName, "*.png", System.IO.SearchOption.TopDirectoryOnly);

                string newestFileName = string.Empty;
                System.DateTime updateTime = System.DateTime.MinValue;
                foreach (string file in files)
                {
                    System.IO.FileInfo fi = new System.IO.FileInfo(file);
                    if (fi.LastWriteTime > updateTime)
                    {
                        updateTime = fi.LastWriteTime;
                        newestFileName = file;
                    }
                }
                return Path.GetFileName(newestFileName);
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>GC強制実行</summary>
        public static void m_gc()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        } 
    }
}
