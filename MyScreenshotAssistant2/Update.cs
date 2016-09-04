using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;

namespace MyScreenshotAssistant2
{
    class Update
    {
        public static Msajson msajson;

        public static void Start()
        {
            try
            {
                // アップデート用jsonの取得
                msajson = JsonConvert.DeserializeObject<Msajson>(new StreamReader(WebRequest.Create("https://msa.f5.si/update/update.json").GetResponse().GetResponseStream()).ReadToEnd());


                Console.WriteLine(MainWindow.version);
                Console.WriteLine(msajson.version);

                if (System.Windows.Forms.Application.ProductVersion != msajson.version)
                {
                    // アップデート通知ウィンドウ表示
                    new UpdateWindow().ShowDialog();
                }
            }
            catch(Exception) { }
        }
    }
}

public class Msajson
{
    public string name { get; set; }
    public string version { get; set; }
    public string type { get; set; }
    public string hash { get; set; }
    public string dlurl { get; set; }
}
