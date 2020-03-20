using System;
using System.Collections.Generic;
using System.IO;

namespace NitroSharp.NsScript
{
    public readonly struct ResolvedPath : IEquatable<ResolvedPath>
    {
        internal ResolvedPath(string path)
        {
            Value = path;
        }

        public string Value { get; }

        public bool Equals(ResolvedPath other) => Value.Equals(other.Value);
        public override int GetHashCode() => Value.GetHashCode();
        public override string ToString() => Value;
    }

    /// <summary>
    /// Resolves Windows-style case insensitive paths that are relative to a given directory
    /// (such as 'nss'). To do so, it needs to enumerate the contents of the folder.
    /// </summary>
    internal sealed class FilePathResolver
    {
        private readonly string _rootDirectory;
        private readonly Dictionary<string, string> _canonicalPaths;

        public FilePathResolver(string rootDirectory, string searchPattern)
        {
            _rootDirectory = rootDirectory;
            _canonicalPaths = new Dictionary<string, string>();
            foreach (string filePath in Directory
                .EnumerateFiles(rootDirectory, searchPattern, SearchOption.AllDirectories))
            {
                string normalized = NormalizeSlashes(filePath);
                _canonicalPaths[normalized.ToUpperInvariant()] = normalized;
            }
        }

        public string RootDirectory => _rootDirectory;

        public bool ResolvePath(string path, out ResolvedPath resolvedPath)
        {
            string fullPath = NormalizeSlashes(Path.Combine(_rootDirectory, path));
            bool res = _canonicalPaths.TryGetValue(fullPath.ToUpperInvariant(), out string? actualPath);
            resolvedPath = new ResolvedPath(actualPath!);
            return res;
        }

        private static string NormalizeSlashes(string path)
            => path.Replace('\\', '/');
    }
}
