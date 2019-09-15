using System.Collections.Generic;
using System.IO;
using System.Json;
using NitroSharp.Media;
using Veldrid;

namespace NitroSharp.Launcher
{
    internal sealed class ConfigurationReader
    {
        public static Configuration Read(string configPath)
        {
            var configuration = new Configuration
            {
                ContentRoot = "Content"
            };

            using (FileStream stream = File.OpenRead(configPath))
            {
                var root = JsonValue.Load(stream);
                foreach (KeyValuePair<string, JsonValue> property in root)
                {
                    SetProperty(configuration, property);
                }

                string profileName = root["activeProfile"];
                JsonValue profile = root["profiles"][profileName];
                foreach (KeyValuePair<string, JsonValue> property in profile)
                {
                    SetProperty(configuration, property);
                }

                return configuration;
            }
        }

        private static void SetProperty(Configuration configuration, KeyValuePair<string, JsonValue> property)
        {
            switch (property.Key)
            {
                case "productName":
                    configuration.ProductName = property.Value;
                    break;
                case "window.width":
                    configuration.WindowWidth = property.Value;
                    break;
                case "window.height":
                    configuration.WindowHeight = property.Value;
                    break;
                case "window.title":
                    configuration.WindowTitle = property.Value;
                    break;

                case "startupScript":
                    configuration.StartupScript = property.Value;
                    break;

                case "graphics.vsync":
                    configuration.EnableVSync = property.Value;
                    break;
                case "graphics.backend":
                    string name = property.Value;
                    name = name.ToUpperInvariant();
                    configuration.PreferredGraphicsBackend = GetGraphicsBackend(name);
                    break;

                case "audio.backend":
                    name = property.Value;
                    name = name.ToUpperInvariant();
                    configuration.PreferredAudioBackend = GetAudioBackend(name);
                    break;

                case "font.family":
                    configuration.FontFamily = property.Value;
                    break;
                case "font.size":
                    configuration.FontSize = property.Value;
                    break;

                case "dev.contentRoot":
                case "debug.contentRoot":
                    configuration.ContentRoot = property.Value;
                    break;
                case "dev.enableDiagnostics":
                    configuration.EnableDiagnostics = property.Value;
                    break;
                case "dev.useDedicatedInterpreterThread":
                    configuration.UseDedicatedInterpreterThread = property.Value;
                    break;
                case "dev.useUtf8":
                    configuration.UseUtf8 = property.Value;
                    break;
                case "dev.skipUpToDateCheck":
                    configuration.SkipUpToDateCheck = property.Value;
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
