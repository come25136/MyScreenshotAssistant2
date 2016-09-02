using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;

namespace MyScreenshotAssistant2
{
    class Update
    {
        public static void Start()
        {
            try
            {
                // アップデート用jsonの取得
                Msajson msajson = JsonConvert.DeserializeObject<Msajson>(new StreamReader(WebRequest.Create("https://msa.f5.si/update/update.json").GetResponse().GetResponseStream()).ReadToEnd());
                String msa = msajson.name + "_v" + msajson.version + "." + msajson.type;


                if (MainWindow.version != msajson.version)
                {
                    // Todo アップデート通知ウィンドウ表示
                }
            }
            catch(Exception) { }
        }
    }
}

public class Msajson
{
    public string name { get; set; }
    public String version { get; set; }
    public string type { get; set; }
    public string hash { get; set; }
    public string dlurl { get; set; }
}
