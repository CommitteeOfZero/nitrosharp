using System;
using System.Collections.Generic;
using System.IO;
using NitroSharp.NsScript.Text;

namespace NitroSharp.NsScript
{
    public abstract class SourceReferenceResolver
    {
        /// <exception cref="FileNotFoundException" />
        public abstract ResolvedPath ResolvePath(string path);
        public abstract SourceText ReadText(ResolvedPath path);
        public abstract long GetModificationTimestamp(ResolvedPath path);
    }

    public sealed class DefaultSourceReferenceResolver : SourceReferenceResolver
    {
        private readonly string _rootDirectory;
        private readonly string _rootDirectoryName;
        private readonly Dictionary<string, string> _canonicalPaths;

        public DefaultSourceReferenceResolver(string rootDirectory)
        {
            _rootDirectory = rootDirectory;
            _rootDirectoryName = new DirectoryInfo(rootDirectory).Name;
            _canonicalPaths = new Dictionary<string, string>();

            foreach (string filePath in Directory
                .EnumerateFiles(rootDirectory, "*.nss", SearchOption.AllDirectories))
            {
                string normalizedPath = NormalizePath(filePath);
                _canonicalPaths[normalizedPath.ToUpperInvariant()] = normalizedPath;
            }
        }

        public override SourceText ReadText(ResolvedPath resolvedPath)
        {
            using (FileStream stream = File.OpenRead(resolvedPath.Value))
            {
                return SourceText.From(stream, resolvedPath.Value);
            }
        }

        /// <exception cref="FileNotFoundException" />
        public override ResolvedPath ResolvePath(string path)
        {
            if (GetFirstPathSegment(path).Equals(_rootDirectoryName, StringComparison.Ordinal))
            {
                path = path.Substring(_rootDirectoryName.Length + 1);
            }

            string fullPath = NormalizePath(Path.GetFullPath(path, _rootDirectory));
            return _canonicalPaths.TryGetValue(fullPath.ToUpperInvariant(), out string actualPath)
                ? new ResolvedPath(actualPath)
                : throw new FileNotFoundException($"File '{fullPath}' does not exist.", fullPath);
        }

        public override long GetModificationTimestamp(ResolvedPath path)
        {
            return new DateTimeOffset(File.GetLastWriteTimeUtc(path.Value), TimeSpan.Zero)
                .ToUnixTimeSeconds();
        }

        private static ReadOnlySpan<char> GetFirstPathSegment(string path)
        {
            int idxStart = (path.Length >= 2 && (path[0] == '/' || path[0] == '\\')) ? 1 : 0;
            ReadOnlySpan<char> span = path.AsSpan(idxStart);
            ReadOnlySpan<char> separators = stackalloc char[] { '/', '\\' };
            int idxSlash = span.IndexOfAny(separators);
            return idxSlash >= 0 ? span.Slice(0, idxSlash) : span;
        }

        private string NormalizePath(string path)
        {
            return path.Replace('\\', '/');
        }
    }
}
