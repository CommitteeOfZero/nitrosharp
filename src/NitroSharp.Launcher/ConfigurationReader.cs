using System;
using System.Collections.Generic;
using System.IO;
using System.Json;
using NitroSharp.Media;
using Veldrid;

namespace NitroSharp.Launcher
{
    internal sealed class BadConfigException : Exception
    {
        public BadConfigException(string missingProperty, Exception? innerException = null)
            : base($"Bad config file: missing required property '{missingProperty}'", innerException)
        {
        }
    }

    internal static class ConfigurationReader
    {
        /// <exception cref="BadConfigException"></exception>
        public static Configuration Read(string configPath)
        {
            static JsonValue getRequired(JsonValue settings, string key)
            {
                try
                {
                    return settings[key];
                }
                catch (KeyNotFoundException)
                {
                    throw new BadConfigException(key);
                }
            }

            using (FileStream stream = File.OpenRead(configPath))
            {
                var root = JsonValue.Load(stream);

                string waitLineIcon = getRequired(root, "icons.waitLine");
                string waitPageIcon = getRequired(root, "icons.waitPage");
                string waitAutoIcon = getRequired(root, "icons.waitAuto");
                string backlogVoiceIcon = getRequired(root, "icons.backlogVoice");

                var icons = new IconPathPatterns(
                    new IconPathPattern(waitLineIcon),
                    new IconPathPattern(waitPageIcon),
                    new IconPathPattern(waitAutoIcon),
                    new IconPathPattern(backlogVoiceIcon)
                );

                var configuration = new Configuration(icons);
                foreach (KeyValuePair<string, JsonValue>? property in root)
                {
                    if (property is { } p)
                    {
                        Set(configuration, p);
                    }
                }

                string profileName = root["activeProfile"];
                JsonValue profile = root["profiles"][profileName];

                foreach (KeyValuePair<string, JsonValue>? property in profile)
                {
                    if (property is { } prop)
                    {
                        Set(configuration, prop);
                    }
                }

                return configuration;
            }
        }

        private static void Set(Configuration configuration, KeyValuePair<string, JsonValue> property)
        {
            switch (property.Key)
            {
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
                    configuration.SysScripts.Startup = property.Value;
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
