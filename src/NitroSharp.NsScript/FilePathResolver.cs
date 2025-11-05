using System;
using System.Collections.Generic;
using System.IO;

namespace NitroSharp.NsScript;

/// <summary>
/// A canonical, normalized path to a file that is supposed to exist.
/// </summary>
public readonly record struct ResolvedPath
{
    internal ResolvedPath(string value)
    {
        Value = value;
    }

    public string Value { get; }
    public string FileName => Path.GetFileName(Value);

    public static ResolvedPath FromExistingFile(FileInfo fileInfo)
    {
        if (!fileInfo.Exists)
        {
            throw new FileNotFoundException(fileInfo.FullName);
        }

        return new ResolvedPath(FilePathResolver.NormalizePath(fileInfo.FullName));
    }

    public ResolvedRelativePath RelativeTo(ResolvedPath directory)
        => new(Path.GetRelativePath(directory.Value, Value));

    public override string ToString() => Value;
}

/// <summary>
/// A relative, normalized path to a file that is supposed to exist.
/// </summary>
public readonly record struct ResolvedRelativePath
{
    internal ResolvedRelativePath(string Value)
    {
        this.Value = Value;
    }

    public string Value { get; }

    public override string ToString() => Value;
}

/// <summary>
/// Resolves Windows-style case-insensitive paths that are relative to a given directory
/// (such as 'nss'). To do so, it needs to enumerate the contents of the folder.
/// </summary>
internal sealed class FilePathResolver
{
    private readonly Dictionary<string, string> _canonicalPaths;

    public FilePathResolver(string rootDirectory, string searchPattern)
    {
        RootDirectory = new ResolvedPath(NormalizePath(rootDirectory));
        _canonicalPaths = new Dictionary<string, string>();
        foreach (string filePath in Directory
                     .EnumerateFiles(rootDirectory, searchPattern, SearchOption.AllDirectories))
        {
            string normalized = NormalizePath(filePath);
            _canonicalPaths[normalized.ToUpperInvariant()] = normalized;
        }
    }

    public ResolvedPath RootDirectory { get; }

    public bool ResolveAbsolute(string relativePath, out ResolvedPath resolvedPath)
    {
        string fullPath = NormalizePath(Path.Combine(RootDirectory.Value, relativePath));
        bool res = _canonicalPaths.TryGetValue(fullPath.ToUpperInvariant(), out string? actualPath);
        resolvedPath = new ResolvedPath(actualPath!);
        return res;
    }

    public bool ResolveRelative(string relativePath, out ResolvedRelativePath resolvedPath)
    {
        if (ResolveAbsolute(relativePath, out ResolvedPath absolutePath))
        {
            ReadOnlySpan<char> canonical = absolutePath.Value
                .AsSpan(RootDirectory.Value.Length + 1);
            string str = relativePath.AsSpan().Equals(canonical, StringComparison.Ordinal)
                ? relativePath
                : canonical.ToString();
            resolvedPath = new ResolvedRelativePath(str);
            return true;
        }

        resolvedPath = default;
        return false;
    }

    public static string NormalizePath(string path)
    {
        string canonicalPath = Path.GetFullPath(path);
        return OperatingSystem.IsWindows() ? path.Replace('\\', '/') : canonicalPath;
    }
}
