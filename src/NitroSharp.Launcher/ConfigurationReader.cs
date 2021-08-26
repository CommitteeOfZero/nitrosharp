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
                var icons = new IconPathPatterns
                {
                    WaitLine = new IconPathPattern(getRequired(root, "icons.waitLine")),
                    WaitPage = new IconPathPattern(getRequired(root, "icons.waitPage")),
                    WaitAuto = new IconPathPattern(getRequired(root, "icons.waitAuto")),
                    BacklogVoice = new IconPathPattern(getRequired(root, "icons.backlogVoice"))
                };

                string profileName = root["activeProfile"];
                JsonValue profile = root["profiles"][profileName];

                var configuration = new Configuration(profileName, icons);
                foreach (KeyValuePair<string, JsonValue>? property in root)
                {
                    if (property is { } p)
                    {
                        Set(configuration, p);
                    }
                }

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
                case "dev.detectEncoding":
                    configuration.DetectEncoding = property.Value;
                    break;
                case "dev.skipUpToDateCheck":
                    configuration.SkipUpToDateCheck = property.Value;
                    break;
                case "dev.scriptRoot":
                    configuration.ScriptRoot = property.Value;
                    break;
                case "dev.platformId":
                    configuration.PlatformId = property.Value;
                    break;
                case "dev.mounts":
                    MountPoint[] mountPoints = new MountPoint[property.Value.Count];
                    for (int i = 0; i < property.Value.Count; i++)
                    {
                        mountPoints[i] = ParseMountPoint(property.Value[i]);
                    }
                    configuration.MountPoints = mountPoints;
                    break;
            }
        }

        private static MountPoint ParseMountPoint(JsonValue inputMountPoint)
        {
            MountPoint mountPoint = new MountPoint();
            foreach (KeyValuePair<string, JsonValue> property in inputMountPoint)
            {
                switch (property.Key)
                {
                    case "archive":
                        mountPoint.ArchiveName = property.Value;
                        break;
                    case "mount":
                        mountPoint.MountName = property.Value;
                        break;
                    case "fileNamesIni":
                        mountPoint.FileNamesIni = property.Value;
                        break;
                }
            }
            return mountPoint;
        }

        private static AudioBackend? GetAudioBackend(string name)
        {
            return name switch
            {
                "XAUDIO" or "XAUDIO2" => AudioBackend.XAudio2,
                "OPENAL" or "OPENALSOFT" or "OPENAL SOFT" => AudioBackend.OpenAL,
                "NULL" or "Null" or "null" => AudioBackend.Null,
                _ => null,
            };
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

                case "Auto":
                case "AUTO":
                case "auto":
                default:
                    return null;
            }
        }
    }
}
