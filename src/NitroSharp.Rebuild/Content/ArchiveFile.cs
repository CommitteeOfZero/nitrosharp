using System;
using System.Collections.Generic;
using System.IO;
using NitroSharp.Utilities;

namespace NitroSharp.Content
{
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

    internal sealed class ArchiveException : Exception
    {
        public ArchiveException()
            : base("Unable to open the archive")
        {
        }

        public ArchiveException(string format, string message)
            : base($"{format} : {message}")
        {
        }
    }
}
