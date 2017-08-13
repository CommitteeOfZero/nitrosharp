using Newtonsoft.Json.Linq;
using System.IO;
using System.Text;

namespace NitroSharp.Launcher
{
    public static class ConfigurationReader
    {
        public static NitroConfiguration Read(string configPath)
        {
            var configuration = new NitroConfiguration();
            configuration.ContentRoot = "Content";

            string json = File.ReadAllText(configPath, Encoding.UTF8);
            var root = JObject.Parse(json);

            foreach (JProperty property in root.AsJEnumerable())
            {
                SetProperty(configuration, property);
            }

            string profileName = root["activeProfile"].ToString();
            var profile = root["profiles"][profileName];

            foreach (JProperty property in profile)
            {
                SetProperty(configuration, property);
            }

            return configuration;
        }

        private static void SetProperty(NitroConfiguration configuration, JProperty property)
        {
            switch (property.Name)
            {
                case "productName":
                    configuration.ProductName = property.Value.Value<string>();
                    break;

                case "window.width":
                    configuration.WindowWidth = property.Value.Value<int>();
                    break;

                case "window.height":
                    configuration.WindowHeight = property.Value.Value<int>();
                    break;

                case "window.title":
                    configuration.WindowTitle = property.Value.Value<string>();
                    break;

                case "startupScript":
                    configuration.StartupScript = property.Value.Value<string>();
                    break;

                case "graphics.vsync":
                    configuration.EnableVSync = property.Value.Value<bool>();
                    break;

                case "dev.contentRoot":
                case "debug.contentRoot":
                    configuration.ContentRoot = property.Value.Value<string>();
                    break;

                case "dev.enableDiagnostics":
                    configuration.EnableDiagnostics = property.Value.Value<bool>();
                    break;
            }
        }
    }
}
