using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace NitroSharp.NsScript
{
    /// <summary>
    /// A canonical, normalized path to a file that is supposed to exist.
    /// </summary>
    public readonly struct ResolvedPath : IEquatable<ResolvedPath>
    {
        internal ResolvedPath(string value)
        {
            Value = value;
        }

        public string Value { get; }

        public bool Equals(ResolvedPath other) => Value.Equals(other.Value);
        public override int GetHashCode() => Value.GetHashCode();
        public override string ToString() => Value;
    }

    /// <summary>
    /// A relative, normalized path to a file that is supposed to exist.
    /// </summary>
    public readonly struct ResolvedRelativePath
    {
        internal ResolvedRelativePath(string value)
        {
            Value = value;
        }

        public string Value { get; }

        public bool Equals(ResolvedRelativePath other) => Value.Equals(other.Value);
        public override int GetHashCode() => Value.GetHashCode();
        public override string ToString() => Value;
    }

    /// <summary>
    /// Resolves Windows-style case insensitive paths that are relative to a given directory
    /// (such as 'nss'). To do so, it needs to enumerate the contents of the folder.
    /// </summary>
    internal sealed class FilePathResolver
    {
        private readonly Dictionary<string, string> _canonicalPaths;
        private readonly bool _unixPlatform;

        public FilePathResolver(string rootDirectory, string searchPattern)
        {
            RootDirectory = rootDirectory = NormalizeSlashes(rootDirectory);
            _canonicalPaths = new Dictionary<string, string>();
            _unixPlatform = !RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            bool normalizedRoot = false;
            foreach (string filePath in Directory
                .EnumerateFiles(rootDirectory, searchPattern, SearchOption.AllDirectories))
            {
                string normalized = NormalizeDotnetPath(filePath);
                if (!normalizedRoot)
                {
                    RootDirectory = normalized[..rootDirectory.Length];
                    Debug.Assert(RootDirectory.Equals(rootDirectory, StringComparison.OrdinalIgnoreCase));
                    normalizedRoot = true;
                }
                _canonicalPaths[normalized.ToUpperInvariant()] = normalized;
            }
        }

        public string RootDirectory { get; }

        public bool ResolveAbsolute(string relativePath, out ResolvedPath resolvedPath)
        {
            string fullPath = NormalizeSlashes(Path.Combine(RootDirectory, relativePath));
            bool res = _canonicalPaths.TryGetValue(fullPath.ToUpperInvariant(), out string? actualPath);
            resolvedPath = new ResolvedPath(actualPath!);
            return res;
        }

        public bool ResolveRelative(string relativePath, out ResolvedRelativePath resolvedPath)
        {
            if (ResolveAbsolute(relativePath, out ResolvedPath absolutePath))
            {
                ReadOnlySpan<char> canonical = absolutePath.Value
                    .AsSpan(RootDirectory.Length + 1);
                string str = relativePath.AsSpan().Equals(canonical, StringComparison.Ordinal)
                    ? relativePath
                    : canonical.ToString();
                resolvedPath = new ResolvedRelativePath(str);
                return true;
            }

            resolvedPath = default;
            return false;
        }

        private string NormalizeDotnetPath(string path)
        {
            if (_unixPlatform)
            {
                Debug.Assert(!path.Contains('\\'));
                return path;
            }

            return NormalizeSlashes(path);
        }

        private static string NormalizeSlashes(string path)
            => path.Replace('\\', '/');
    }
}
