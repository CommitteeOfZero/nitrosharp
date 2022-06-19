using System.IO;
using System.Text.Json;
using NitroSharp.Media;
using Veldrid;

namespace NitroSharp;

public sealed record Config
{
    public Size RenderResolution { get; private init; }
    public GraphicsBackend? PreferredGraphicsBackend { get; private init; }
    public AudioBackend? PreferredAudioBackend { get; private init; }

    public bool EnableFullScreen { get; private init; }
    public bool EnableVSync { get; private init; }

    public static Config Read(Stream stream)
    {
        JsonElement root = JsonDocument.Parse(stream).RootElement;

        return new Config
        {
            PreferredGraphicsBackend = GetGraphicsBackend(property("graphics.backend").GetString()),
            PreferredAudioBackend = GetAudioBackend(property("audio.backend").GetString()),
            EnableVSync = property("graphics.vsync").GetBoolean()
        };

        JsonElement property(string name) => root.GetProperty(name);
    }

    private static AudioBackend? GetAudioBackend(string? name) => name?.ToUpperInvariant() switch
    {
        "XAUDIO" or "XAUDIO2" => AudioBackend.XAudio2,
        "OPENAL" or "OPENALSOFT" or "OPENAL SOFT" => AudioBackend.OpenAL,
        "NULL" or "Null" or "null" => AudioBackend.Null,
        _ => null,
    };

    private static GraphicsBackend? GetGraphicsBackend(string? name)
    {
        switch (name?.ToUpperInvariant())
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
