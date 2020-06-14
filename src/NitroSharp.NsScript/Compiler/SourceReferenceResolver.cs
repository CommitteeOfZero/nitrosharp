using System;
using System.IO;
using System.Text;

namespace NitroSharp.NsScript.Compiler
{
    public abstract class SourceReferenceResolver
    {
        public abstract string RootDirectory { get; }

        /// <exception cref="FileNotFoundException" />
        public abstract ResolvedPath ResolvePath(string path);
        public abstract SourceText ReadText(ResolvedPath path, Encoding? encoding);
        public abstract long GetModificationTimestamp(ResolvedPath path);
    }

    public sealed class DefaultSourceReferenceResolver : SourceReferenceResolver
    {
        private readonly FilePathResolver _pathResolver;
        private readonly string _rootDirectoryName;

        public DefaultSourceReferenceResolver(string rootDirectory)
        {
            _rootDirectoryName = new DirectoryInfo(rootDirectory).Name;
            _pathResolver = new FilePathResolver(rootDirectory, "*.nss");
        }

        public override string RootDirectory => _pathResolver.RootDirectory;

        public override ResolvedPath ResolvePath(string path)
        {
            static ReadOnlySpan<char> getFirstPathSegment(string path)
            {
                int idxStart = (path.Length >= 2 && (path[0] == '/' || path[0] == '\\')) ? 1 : 0;
                ReadOnlySpan<char> span = path.AsSpan(idxStart);
                ReadOnlySpan<char> separators = stackalloc char[] { '/', '\\' };
                int idxSlash = span.IndexOfAny(separators);
                return idxSlash >= 0 ? span.Slice(0, idxSlash) : span;
            }

            // "nss/boot.nss" and "boot.nss" should both resolve to the same path.
            ReadOnlySpan<char> firstSeg = getFirstPathSegment(path);
            if (firstSeg.Equals(_rootDirectoryName, StringComparison.Ordinal))
            {
                path = path[(firstSeg.Length + 1)..];
            }

            return _pathResolver.ResolveAbsolute(path, out ResolvedPath resolved)
                ? resolved
                : throw new FileNotFoundException("File not found", path);
        }

        public override SourceText ReadText(ResolvedPath resolvedPath, Encoding? encoding)
        {
            using (FileStream stream = File.OpenRead(resolvedPath.Value))
            {
                return SourceText.From(stream, resolvedPath, encoding);
            }
        }

        public override long GetModificationTimestamp(ResolvedPath path)
        {
            return new DateTimeOffset(File.GetLastWriteTimeUtc(path.Value), TimeSpan.Zero)
                .ToUnixTimeSeconds();
        }
    }
}
