using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;

namespace NitroSharp.Content
{
    internal interface IArchiveFile : IDisposable
    {
        public Stream OpenStream(string path);
        public bool Contains(string path);
    }

    internal sealed class VFSNode : IDisposable
    {
        public List<IArchiveFile>? MountedArchives { get; set; }
        public Dictionary<string, VFSNode>? Children { get; set; }

        public void Dispose()
        {
            if (Children != null)
            {
                foreach (KeyValuePair<string, VFSNode> pair in Children)
                {
                    pair.Value.Dispose();
                }
            }

            if (MountedArchives != null)
            {
                foreach (IArchiveFile mountedArchive in MountedArchives)
                {
                    mountedArchive.Dispose();
                }
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

    internal sealed class ArchiveManager : IDisposable
    {
        public static readonly Encoding DefaultEncoding;

        private readonly VFSNode _root;
        private readonly Encoding _encoding;

        static ArchiveManager()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            DefaultEncoding = Encoding.GetEncoding("shift-jis");
        }

        public ArchiveManager(Encoding? encoding = null)
        {
            if (encoding == null)
            {
                encoding = DefaultEncoding;
            }
            _encoding = encoding;
            _root = new VFSNode();
        }

        public void Dispose()
        {
            _root.Dispose();
        }

        public void AddMounts(MountPoint[] mountPoints, string rootDirectory, ContentManager content)
        {
            foreach (MountPoint mountPoint in mountPoints)
            {
                AddMount(mountPoint, rootDirectory, content);
            }
        }

        private void AddMount(MountPoint mountPoint, string rootDirectory, ContentManager content)
        {
            string[] splitedPath = mountPoint.MountName.ToLowerInvariant().Split("/");

            VFSNode head = _root;
            foreach (string part in splitedPath)
            {
                if (head.Children == null)
                {
                    head.Children = new Dictionary<string, VFSNode>();
                }

                if (!head.Children.ContainsKey(part))
                {
                    head.Children[part] = new VFSNode();
                }

                head = head.Children[part];
            }

            if (head.MountedArchives == null)
            {
                head.MountedArchives = new List<IArchiveFile>();
            }

            string fullPath = Path.Combine(rootDirectory, mountPoint.ArchiveName);
            if (File.Exists(fullPath))
            {
                Func<MemoryMappedFile,Encoding,IArchiveFile?>[] loadMethods = new Func<MemoryMappedFile,Encoding,IArchiveFile?>[]
                {
                    (m, e) => NPAFile.TryLoad(m, e),
                    (m, e) => AFSFile.TryLoad(m, e),
                };
                MemoryMappedFile mmFile = MemoryMappedFile.CreateFromFile(fullPath, FileMode.Open, null, 0, MemoryMappedFileAccess.Read);
                IArchiveFile? archive = loadMethods
                    .Select(load => load(mmFile, _encoding))
                    .FirstOrDefault(archive => archive is not null);
                if (archive == null)
                {
                    throw new FileLoadException("Archive format not recognized", fullPath);
                }
                if (mountPoint.FileNamesIni != null)
                {
                    // Only AFSFile supports INI files
                    ((AFSFile)archive).LoadFileNames(content.OpenStream(mountPoint.FileNamesIni));
                }
                head.MountedArchives.Add(archive);
            }
            else
            {
                // Only AFSFile supports sub-archives
                (IArchiveFile, string)? parentArchiveDetails = LocateFileInArchives(mountPoint.ArchiveName);
                if (parentArchiveDetails == null)
                {
                    throw new FileNotFoundException("Sub-archive not found", mountPoint.ArchiveName);
                }
                (IArchiveFile parentArchive, string archivePath) = parentArchiveDetails.Value;
                AFSFile archive = (AFSFile) AFSFile.Load((AFSFile) parentArchive, archivePath, _encoding);
                if (mountPoint.FileNamesIni != null)
                {
                    archive.LoadFileNames(content.OpenStream(mountPoint.FileNamesIni));
                }
                head.MountedArchives.Add(archive);
            }
        }

        private (IArchiveFile archive, string path)? LocateFileInArchives(string path)
        {
            string[] splitedPath = path.ToLowerInvariant().Split("/");
            VFSNode head = _root;

            // This way of browsing folders is not perfect beacause we can have two possible paths :
            // one through the Children and one through the MountedArchives.
            // If nobody does weird stuff with the archives, this shouldn't happen.
            int depth;
            for (depth = 0; depth < (splitedPath.Length - 1); depth++)
            {
                if (head.Children != null)
                {
                    head = head.Children[splitedPath[depth]];
                }
                else
                {
                    break;
                }
            }

            string archivePath = String.Join("/", splitedPath, depth, splitedPath.Length - depth);
            if (head.MountedArchives != null)
            {
                foreach (IArchiveFile archive in head.MountedArchives)
                {
                    if (archive.Contains(archivePath))
                    {
                         return (archive, archivePath);
                    }
                }
            }
            return null;
        }

        public Stream OpenStream(string path)
        {
            (IArchiveFile, string)? archiveDetails = LocateFileInArchives(path);
            if (archiveDetails == null)
            {
                throw new FileNotFoundException("File not found in the VFS", path);
            }
            (IArchiveFile archive, string archivePath) = archiveDetails.Value;
            return archive.OpenStream(archivePath);
        }
    }
}
