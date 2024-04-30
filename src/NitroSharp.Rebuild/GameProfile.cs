using System;
using System.IO;
using System.Text.Json;
using NitroSharp.Text;
using SprintfNET;

namespace NitroSharp;

public sealed class GameProfile
{
    public required string Name { get; init; }
    public required string ProductName { get; init; }
    public required string ProductDisplayName { get; init; }
    public required DesignSizeU DesignResolution { get; init; }
    public required string ContentRoot { get; init; }
    public required string ScriptRoot { get; init; }
    public required bool EnableDiagnostics { get; init; }
    public required SystemScripts SysScripts { get; init; }

    public required bool UseUtf8 { get; init; }
    public required bool DetectEncoding { get; init; }
    public required bool SkipUpToDateCheck { get; init; }

    public required string FontFamily { get; init; }
    public required PtFontSize FontSize { get; init; }

    public required IconPathPatterns IconPathPatterns { get; init; }
    public required int PlatformId { get; init; }

    public required MountPoint[]? MountPoints { get; init; }

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

        uint? fontSize = getUIntOpt("font.size");

        return new GameProfile
        {
            Name = profileName ?? "Default",
            ProductName = getString("product.name"),
            ProductDisplayName = getString("product.displayName"),
            DesignResolution = Config.TryParseResolution<DesignPixel>(getString("designResolution")) ?? throw new BadGameProfileException("designResolution"),
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

        MountPoint mountPoint(JsonElement json)
            => new()
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
        IconCount = uint.Parse(pattern.AsSpan()[(fmtEnd + 1)..]);
    }

    public IconPathEnumerable EnumeratePaths() => new(this);
}

public struct IconPathEnumerable
{
    private readonly IconPathPattern _pattern;
    private int _index;

    public IconPathEnumerable(IconPathPattern pattern)
    {
        _pattern = pattern;
        _index = 1;
        Current = string.Empty;
    }

    public IconPathEnumerable GetEnumerator() => this;

    public string Current { get; private set; }

    public bool MoveNext()
    {
        if (_index == _pattern.IconCount) { return false; }

        Current = StringFormatter.PrintF(_pattern.FormatString, _index);
        _index++;
        return true;
    }
}

public sealed class IconPathPatterns
{
    public required IconPathPattern WaitLine { get; init; }
    public required IconPathPattern WaitPage { get; init; }
    public required IconPathPattern WaitAuto { get; init; }
    public required IconPathPattern BacklogVoice { get; init; }
}

public readonly struct MountPoint
{
    public required string ArchiveName { get; init; }
    public required string MountName { get; init; }
    public required string? FileNamesIni { get; init; }
}
