using Newtonsoft.Json.Linq;
using System.IO;
using System.Text;

namespace ProjectHoppy
{
    public class HoppyConfig
    {
        private const string ConfigFileName = "hoppy.json";

        public string ContentPath { get; private set; }

        public static HoppyConfig Read()
        {
            var config = new HoppyConfig();

            string json = File.ReadAllText(ConfigFileName, Encoding.UTF8);
            var root = JObject.Parse(json);
            config.ContentPath = root["contentRoot"].ToString();

            return config;
        }
    }
}
