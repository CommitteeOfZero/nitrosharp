using System;
using System.Collections.Generic;
using System.IO;
using NitroSharp.Common;

namespace NitroSharp.Content;

internal abstract class ArchiveFile : IDisposable
{
    public abstract Stream OpenStream(string path);
    public abstract bool Contains(string path);
    public abstract void Dispose();
}

internal sealed class VfsNode : IDisposable
{
    public SmallList<ArchiveFile> MountedArchives;
    public Dictionary<string, VfsNode>? Children;

    public void Dispose()
    {
        if (Children is not null)
        {
            foreach (VfsNode node in Children.Values)
            {
                node.Dispose();
            }
        }
        foreach (ArchiveFile mountedArchive in MountedArchives.AsSpan())
        {
            mountedArchive.Dispose();
        }
    }
}

internal sealed class ArchiveException(string format, string message)
    : Exception($"{format} : {message}");
