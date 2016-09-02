using System;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Net;
using System.Net.Cache;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Media.Imaging;

namespace MyScreenshotAssistant2
{
    class Method
    {
        public static DataTable DirectoryTable = new DataTable();
        public static DataTable AccountTable = new DataTable();

        public static SQLiteDataAdapter AccountAdapter;
        public static SQLiteDataAdapter DirectoryAdapter;

        public static SQLiteConnection database = new SQLiteConnection("Data Source=msa.db");
        public static SQLiteCommand statement = null;
        public static SQLiteDataReader reader = null;

        /// <summary>
        /// MessageBox
        /// </summary>

        // MessageBoxのコード省略化
        public static void message(string title, string value)
        {
            MessageBox.Show(value,
                title,
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            return;
        }

        /// <summary>
        /// Make log file
        /// </summary>

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

        /// <summary>
        /// Login SQLite
        /// </summary>
        /// 
        public static void sql_login()
        {
            try
            {
                database.Open();
                logfile("Info", "connect database.");

                statement = database.CreateCommand();

                sql("create table if not exists Account (UserId string primary key, AccessToken string, AccessTokenSecret string)");
                sql("create table if not exists DirectoryData (name string, path string primary key)");
            }
            catch (Exception)
            {
                logfile("Error", "Unable to connect to database.");
            }
        }

        /// <summary>
        /// sql SQLite
        /// </summary>

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

        /// <summary>
        /// sql_reader SQLite
        /// </summary>

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
    }
}
