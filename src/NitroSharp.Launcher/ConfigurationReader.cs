using Newtonsoft.Json.Linq;
using NitroSharp.Media;
using System;
using System.IO;
using System.Text;
using Veldrid;

namespace NitroSharp.Launcher
{
    public static class ConfigurationReader
    {
        public static Configuration Read(string configPath)
        {
            var configuration = new Configuration();
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

        private static void SetProperty(Configuration configuration, JProperty property)
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

                case "graphics.backend":
                    string name = property.Value.Value<string>().ToUpperInvariant();
                    configuration.PreferredGraphicsBackend = GetGraphicsBackend(name);
                    break;

                case "audio.backend":
                    name = property.Value.Value<string>().ToUpperInvariant();
                    configuration.PreferredAudioBackend = GetAudioBackend(name);
                    break;

                case "dev.contentRoot":
                case "debug.contentRoot":
                    configuration.ContentRoot = property.Value.Value<string>();
                    break;

                case "dev.enableDiagnostics":
                    configuration.EnableDiagnostics = property.Value.Value<bool>();
                    break;

                case "dev.useDedicatedInterpreterThread":
                    configuration.UseDedicatedInterpreterThread = property.Value.Value<bool>();
                    break;
            }
        }

        private static AudioBackend? GetAudioBackend(string name)
        {
            switch (name)
            {
                case "XAUDIO":
                case "XAUDIO2":
                    return AudioBackend.XAudio2;

                case "OPENAL":
                case "OPENALSOFT":
                case "OPENAL SOFT":
                    return AudioBackend.OpenAL;

                default:
                    return null;
            }
        }

        private static GraphicsBackend? GetGraphicsBackend(string name)
        {
            switch (name)
            {
                case "DIRECT3D11":
                case "DIRECT3D 11":
                case "DIRECT3D":
                case "D3D11":
                case "D3D 11":
                case "D3D":
                    return GraphicsBackend.Direct3D11;

                case "VULKAN":
                    return GraphicsBackend.Vulkan;

                case "OPENGL":
                case "GL":
                    return GraphicsBackend.OpenGL;

                case "OPENGLES":
                case "OPENGL ES":
                case "GLES":
                case "GL ES":
                    return GraphicsBackend.OpenGLES;

                default:
                    return null;
            }
        }

    }
}
