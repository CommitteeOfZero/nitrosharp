using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NitroSharp.Utilities;
using Veldrid;

namespace NitroSharp.Content
{
    internal readonly struct AssetRef<T> : IEquatable<AssetRef<T>>, IDisposable
        where T : class, IDisposable
    {
        private readonly ContentManager _contentManager;

        public readonly string Path;
        public readonly FreeListHandle Handle;

        public AssetRef(ContentManager contentManager, string path, FreeListHandle handle)
            => (_contentManager, Path, Handle) = (contentManager, path, handle);

        public AssetRef<T> Clone()
        {
            _contentManager.IncrementRefCount(Path);
            return this;
        }

        public void Dispose()
        {
            _contentManager.ReleaseRef(this);
        }

        public bool Equals(AssetRef<T> other) => Handle.Equals(other.Handle);
        public override int GetHashCode() => Handle.GetHashCode();
        public override string ToString() => $"Asset '{Path}'";
    }

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

    internal class ContentManager : IDisposable
    {
        private readonly TextureLoader _textureLoader;
        private readonly Func<Stream, bool, Texture> _loadTextureFunc;

        private readonly FreeList<CacheEntry> _cache;
        private readonly Dictionary<string, FreeListHandle> _strongHandles;
        private readonly ConcurrentBag<(string, IDisposable)> _loadedAssets;
        private volatile int _nbPending;

        public static readonly Encoding DefaultEncoding;

        private readonly VFSNode _root;
        private readonly Encoding _encoding;

        [StructLayout(LayoutKind.Auto)]
        private struct CacheEntry
        {
            public uint RefCount;
            public Size TextureSize;
            public IDisposable? Asset;
        }

        static ContentManager()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            DefaultEncoding = Encoding.GetEncoding("shift-jis");
        }

        public ContentManager(string rootDirectory, TextureLoader textureLoader, MountPoint[]? mountPoints, Encoding? encoding = null)
        {
            RootDirectory = rootDirectory;
            _textureLoader = textureLoader;
            _loadTextureFunc = (stream, staging) => _textureLoader.LoadTexture(stream, staging);
            _cache = new FreeList<CacheEntry>();
            _strongHandles = new Dictionary<string, FreeListHandle>();
            _loadedAssets = new ConcurrentBag<(string, IDisposable)>();
            _encoding = encoding ?? DefaultEncoding;
            _root = new VFSNode();
            if (mountPoints != null)
            {
                foreach (MountPoint mountPoint in mountPoints)
                {
                    AddMount(mountPoint);
                }
            }
        }

        public string RootDirectory { get; }

        public Texture LoadTexture(string path, bool staging)
        {
            Stream stream = OpenStream(path);
            return _textureLoader.LoadTexture(stream, staging);
        }

        public T Get<T>(AssetRef<T> assetRef)
            where T : class, IDisposable
        {
            if (!(_cache.Get(assetRef.Handle).Asset is T loadedAsset))
            {
                throw new InvalidOperationException(
                    $"BUG: asset '{assetRef.Path}' is missing from the cache."
                );
            }

            return loadedAsset;
        }

        // TODO: consider replacing with RequestAsset<T>
        public void IncrementRefCount(string path)
        {
            if (_strongHandles.TryGetValue(path, out FreeListHandle existing))
            {
                _cache.Get(existing).RefCount++;
            }
        }

        public Size GetTextureSize(AssetRef<Texture> textureRef)
            => _cache.Get(textureRef.Handle).TextureSize;

        public AssetRef<Texture>? RequestTexture(string path, bool staging = false)
            => RequestTexture(path, out _, staging);

        private AssetRef<Texture>? RequestTexture(string path, out Size size, bool staging = false)
        {
            if (_strongHandles.TryGetValue(path, out FreeListHandle existing))
            {
                ref CacheEntry cacheEntry = ref _cache.Get(existing);
                cacheEntry.RefCount++;
                size = cacheEntry.TextureSize;
                return new AssetRef<Texture>(this, path, existing);
            }

            Stream? stream;
            FreeListHandle handle;
            try
            {
                stream = OpenStream(path);
                size = _textureLoader.GetTextureSize(stream);
                handle = _cache.Insert(new CacheEntry
                {
                    TextureSize = size,
                    RefCount = 1
                });
                _strongHandles.Add(path, handle);
            }
            catch
            {
                size = Size.Zero;
                return null;
            }
            Interlocked.Increment(ref _nbPending);
            _ = Task.Run(() =>
            {
                try
                {
                    Texture tex = _loadTextureFunc(stream, staging);
                    _loadedAssets.Add((path, tex));
                }
                catch
                {
                    Interlocked.Decrement(ref _nbPending);
                }
            });
            return new AssetRef<Texture>(this, path, handle);
        }

        public void ReleaseRef<T>(AssetRef<T> assetRef)
            where T : class, IDisposable
        {
            ref CacheEntry cacheEntry = ref _cache.Get(assetRef.Handle);
            if (--cacheEntry.RefCount == 0 && cacheEntry.Asset is T asset)
            {
                asset.Dispose();
                _cache.Free(assetRef.Handle);
                _strongHandles.Remove(assetRef.Path);
            }
        }

        public bool ResolveAssets()
        {
            if (_nbPending > 0)
            {
                while (_loadedAssets.TryTake(out (string path, IDisposable asset) tup))
                {
                    Interlocked.Decrement(ref _nbPending);
                    FreeListHandle handle = _strongHandles[tup.path];
                    ref CacheEntry cacheEntry = ref _cache.Get(handle);
                    cacheEntry.Asset = tup.asset;
                }
            }

            return _nbPending == 0;
        }

        public Stream? TryOpenStream(string path)
        {
            try
            {
                return OpenStream(path);
            }
            catch
            {
                return null;
            }
        }

        public Stream OpenStream(string path)
        {
            string fsPath = path;
            if (Path.GetDirectoryName(path) is string dir
                && Path.GetFileName(path) is string filename)
            {
                fsPath = Path.Combine(dir, filename.ToLowerInvariant());
            }
            string fullPath = Path.Combine(RootDirectory, fsPath);

            if (File.Exists(fullPath))
            {
                return File.OpenRead(fullPath);
            }

            (IArchiveFile, string)? archiveDetails = LocateFileInArchives(path);
            if (archiveDetails == null)
            {
                throw new FileNotFoundException("File not found in the VFS", path);
            }
            (IArchiveFile archive, string archivePath) = archiveDetails.Value;
            return archive.OpenStream(archivePath);
        }

        private void AddMount(MountPoint mountPoint)
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

            string fullPath = Path.Combine(RootDirectory, mountPoint.ArchiveName);
            if (File.Exists(fullPath))
            {
                Func<MemoryMappedFile,Encoding,IArchiveFile?>[] loadMethods = new Func<MemoryMappedFile,Encoding,IArchiveFile?>[]
                {
                    NPAFile.TryLoad,
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
                    ((AFSFile)archive).LoadFileNames(OpenStream(mountPoint.FileNamesIni));
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
                    archive.LoadFileNames(OpenStream(mountPoint.FileNamesIni));
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

        public void Dispose()
        {
            ResolveAssets();
            foreach ((_, FreeListHandle handle) in _strongHandles)
            {
                _cache.Get(handle).Asset?.Dispose();
            }
            _strongHandles.Clear();
            _textureLoader.Dispose();
            _root.Dispose();
        }
    }
}
