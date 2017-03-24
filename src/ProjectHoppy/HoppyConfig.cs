using Newtonsoft.Json.Linq;
using System.IO;
using System.Text;

namespace ProjectHoppy
{
    public class HoppyConfig
    {
        private const string ConfigFileName = "hoppy.json";

        public string ContentPath { get; private set; }
        public string NssFolderPath { get; private set; }

        public static HoppyConfig Read()
        {
            var config = new HoppyConfig();

            string json = File.ReadAllText(ConfigFileName, Encoding.UTF8);
            var root = JObject.Parse(json);
//#if DEBUG
            config.ContentPath = root["debug.contentRoot"].ToString();
            config.NssFolderPath = root["debug.nssFolder"].ToString();
//#else
//            config.ContentPath = "./Content";
//            config.NssFolderPath = "./nss";
//#endif

            return config;
        }
    }
}
