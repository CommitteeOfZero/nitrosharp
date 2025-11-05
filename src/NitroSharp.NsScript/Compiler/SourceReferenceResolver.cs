using System;
using System.IO;
using System.Text;

namespace NitroSharp.NsScript.Compiler;

public abstract class SourceReferenceResolver
{
    public abstract ResolvedPath RootDirectory { get; }

    public abstract ResolvedPath? TryResolvePath(string relativePath);

    /// <exception cref="FileNotFoundException" />
    public ResolvedPath ResolvePath(string relativePath)
    {
        return TryResolvePath(relativePath)
            ?? throw new FileNotFoundException($"File not found: '{relativePath}'", relativePath);
    }

    public abstract SourceText ReadText(ResolvedPath path, Encoding? encoding);
    public abstract long GetModificationTimestamp(ResolvedPath path);
}

public sealed class DefaultSourceReferenceResolver(string rootDirectory) : SourceReferenceResolver
{
    private readonly FilePathResolver _pathResolver = new(rootDirectory, "*.nss");
    private readonly string _rootDirectoryName = new DirectoryInfo(rootDirectory).Name;

    public override ResolvedPath RootDirectory => _pathResolver.RootDirectory;

    public override ResolvedPath? TryResolvePath(string relativePath)
    {
        static ReadOnlySpan<char> getFirstPathSegment(string path)
        {
            int idxStart = (path.Length >= 2 && (path[0] == '/' || path[0] == '\\')) ? 1 : 0;
            ReadOnlySpan<char> span = path.AsSpan(idxStart);
            ReadOnlySpan<char> separators = ['/', '\\'];
            int idxSlash = span.IndexOfAny(separators);
            return idxSlash >= 0 ? span[..idxSlash] : span;
        }

        // "nss/boot.nss" and "boot.nss" should both resolve to the same path.
        ReadOnlySpan<char> firstSeg = getFirstPathSegment(relativePath);
        if (firstSeg.Equals(_rootDirectoryName, StringComparison.Ordinal))
        {
            relativePath = relativePath[(firstSeg.Length + 1)..];
        }

        return _pathResolver.ResolveAbsolute(relativePath, out ResolvedPath resolved)
            ? resolved
            : null;
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
