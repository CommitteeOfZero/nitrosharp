using System;
using System.IO;
using System.Text.Json;
using NitroSharp.Media;
using NitroSharp.NsScript.Utilities;
using Veldrid;

namespace NitroSharp;

public sealed record Config
{
    public ScreenSizeU? RenderResolution { get; private init; }
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
            EnableVSync = property("graphics.vsync").GetBoolean(),
            RenderResolution = TryParseResolution<ScreenPixel>(propertyOpt("graphics.renderResolution")?.GetString())
        };

        JsonElement property(string name) => root.GetProperty(name);
        JsonElement? propertyOpt(string name) => root.TryGetProperty(name, out JsonElement value) ? value : null;
    }

    public static SizeU<TUnit>? TryParseResolution<TUnit>(string? resolutionString)
    {
        if (resolutionString is null) { return null; }

        int index = 0;
        uint width = 0;
        uint height = 0;
        foreach (ReadOnlySpan<char> part in resolutionString.AsSpan().Split('x'))
        {
            if (uint.TryParse(part, out uint value))
            {
                if (index == 0)
                {
                    width = value;
                }
                else if (index == 1)
                {
                    height = value;
                }
                else
                {
                    width = height = 0;
                    break;
                }
            }

            index++;
        }

        return width != 0 && height != 0 ? new SizeU<TUnit>(width, height) : null;
    }

    private static AudioBackend? GetAudioBackend(string? name) => name?.ToUpperInvariant() switch
    {
        "XAUDIO" or "XAUDIO2" => AudioBackend.XAudio2,
        "OPENAL" or "OPENALSOFT" or "OPENAL SOFT" => AudioBackend.OpenAL,
        "NULL" => AudioBackend.Null,
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

            case "AUTO":
            default:
                return null;
        }
    }
}
