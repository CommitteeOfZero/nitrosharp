using Newtonsoft.Json.Linq;
using System.IO;
using System.Text;

namespace ProjectHoppy
{
    public class HoppyConfig
    {
        private const string ConfigFileName = "hoppy.json";

        public string ContentRoot { get; private set; }

        public static HoppyConfig Read()
        {
            var config = new HoppyConfig();

            string json = File.ReadAllText(ConfigFileName, Encoding.UTF8);
            var root = JObject.Parse(json);

            config.ContentRoot = root["debug.contentRoot"].ToString();
            return config;
        }
    }
}
