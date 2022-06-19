using System;
using System.IO;
using System.Text.Json;
using NitroSharp.Text;
using SprintfNET;

namespace NitroSharp;

public sealed class GameProfile
{
    public string Name { get; private init; } = null!;
    public string ProductName { get; private init; } = null!;
    public string ProductDisplayName { get; private init; } = null!;
    public Size DesignResolution { get; private init; }
    public string ContentRoot { get; private init; } = null!;
    public string ScriptRoot { get; private init; } = null!;
    public bool EnableDiagnostics { get; private init; }
    public SystemScripts SysScripts { get; private init; } = null!;

    public bool UseUtf8 { get; private init; }
    public bool DetectEncoding { get; private init; }
    public bool SkipUpToDateCheck { get; private init; }

    public string FontFamily { get; private init; } = null!;
    public PtFontSize FontSize { get; private init; }

    public IconPathPatterns IconPathPatterns { get; private init; } = null!;
    public int PlatformId { get; private init; }

    public MountPoint[]? MountPoints { get; private init; }

    public static GameProfile Read(Stream stream)
    {
        JsonElement root = JsonDocument.Parse(stream).RootElement;
        string? profileName = root.GetProperty("activeProfile").GetString();

        JsonElement? activeProfile = null;
        if (profileName is not null)
        {
            activeProfile = root.GetProperty("profiles").GetProperty(profileName);
        }

        MountPoint[]? mountPoints = null;
        if (propertyOpt("dev.mounts") is { } jsonMounts)
        {
            int count = jsonMounts.GetArrayLength();
            mountPoints = new MountPoint[count];
            for (int i = 0; i < count; i++)
            {
                mountPoints[i] = mountPoint(jsonMounts[i]);
            }
        }

        uint designWidth = getUInt("designWidth");
        uint designHeight = getUInt("designHeight");
        uint? fontSize = getUIntOpt("font.size");

        return new GameProfile
        {
            Name = profileName ?? "Default",
            ProductName = getString("product.name"),
            ProductDisplayName = getString("product.displayName"),
            DesignResolution = new Size(designWidth, designHeight),
            ContentRoot = getStringOpt("dev.contentRoot") ?? "content",
            ScriptRoot = getStringOpt("dev.scriptRoot") ?? "nss",
            EnableDiagnostics = false,
            UseUtf8 = getBoolOpt("dev.useUtf8") ?? false,
            DetectEncoding = getBoolOpt("dev.detectEncoding") ?? false,
            PlatformId = (int)(getUIntOpt("dev.platformId") ?? 100),
            SkipUpToDateCheck = true,
            SysScripts = new SystemScripts(getStringOpt("startupScript") ?? "boot.nss"),
            MountPoints = mountPoints,
            IconPathPatterns = new IconPathPatterns
            {
                WaitLine = new IconPathPattern(getString("icons.waitLine")),
                WaitPage = new IconPathPattern(getString("icons.waitPage")),
                WaitAuto = new IconPathPattern(getString("icons.waitAuto")),
                BacklogVoice = new IconPathPattern(getString("icons.backlogVoice"))
            },
            FontFamily = getStringOpt("font.family") ?? "VL Gothic",
            FontSize = new PtFontSize(fontSize ?? 20)
        };

        string getString(string key) => property(key).GetString()!;
        string? getStringOpt(string key) => propertyOpt(key)?.GetString();

        uint getUInt(string key) => property(key).GetUInt32();
        uint? getUIntOpt(string key) => propertyOpt(key)?.GetUInt32();

        bool? getBoolOpt(string key) => propertyOpt(key)?.GetBoolean();

        JsonElement property(string key) => propertyOpt(key) ?? throw new BadGameProfileException(key);

        JsonElement? propertyOpt(string key)
        {
            if (activeProfile?.TryGetProperty(key, out JsonElement value) == true)
            {
                return value;
            }

            return root.TryGetProperty(key, out value) ? value : null;
        }

        MountPoint mountPoint(JsonElement json) => new()
        {
            ArchiveName = json.GetProperty("archive").GetString()!,
            MountName = json.GetProperty("mount").GetString()!,
            FileNamesIni = json.TryGetProperty("fileNamesIni", out JsonElement value) ? value.GetString() : null
        };
    }
}

internal sealed class BadGameProfileException : Exception
{
    public BadGameProfileException(string missingProperty, Exception? innerException = null)
        : base($"Bad game profile: missing required property '{missingProperty}'", innerException)
    {
    }
}

public sealed class SystemScripts
{
    public string Startup { get; }
    public string Menu { get; set; } = "sys_menu.nss";
    public string Save { get; set; } = "sys_save.nss";
    public string Load { get; set; } = "sys_load.nss";
    public string Config { get; set; } = "sys_config.nss";
    public string Backlog { get; set; } = "sys_backlog.nss";
    public string ExitConfirmation { get; set; } = "sys_close.nss";
    public string ReturnToMenu { get; set; } = "sys_reset.nss";

    public SystemScripts(string startupScript)
    {
        Startup = startupScript;
    }
}

public readonly struct IconPathPattern
{
    public readonly string FormatString;
    public readonly uint IconCount;

    public IconPathPattern(string pattern)
    {
        int fmtEnd = pattern.IndexOf('#');
        FormatString = pattern[..fmtEnd];
        IconCount = uint.Parse(pattern[(fmtEnd + 1)..]);
    }

    public IconPathEnumerable EnumeratePaths() => new(this);
}

public struct IconPathEnumerable
{
    private readonly IconPathPattern _pattern;
    private int _i;

    public IconPathEnumerable(IconPathPattern pattern)
    {
        _pattern = pattern;
        _i = 1;
        Current = string.Empty;
    }

    public IconPathEnumerable GetEnumerator() => this;

    public string Current { get; private set; }

    public bool MoveNext()
    {
        if (_i == _pattern.IconCount) { return false; }
        Current = StringFormatter.PrintF(_pattern.FormatString, _i);
        _i++;
        return true;
    }
}

public sealed class IconPathPatterns
{
    public IconPathPattern WaitLine { get; init; }
    public IconPathPattern WaitPage { get; init; }
    public IconPathPattern WaitAuto { get; init; }
    public IconPathPattern BacklogVoice { get; init; }
}

public readonly struct MountPoint
{
    public string ArchiveName { get; init; }
    public string MountName { get; init; }
    public string? FileNamesIni { get; init; }
}
