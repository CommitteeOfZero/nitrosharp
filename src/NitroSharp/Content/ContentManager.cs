using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
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

    internal class ContentManager : IDisposable
    {
        private readonly TextureLoader _textureLoader;
        private readonly Func<Stream, bool, Texture> _loadTextureFunc;

        private readonly FreeList<CacheEntry> _cache;
        private readonly Dictionary<string, FreeListHandle> _strongHandles;
        private readonly ConcurrentBag<(string, IDisposable)> _loadedAssets;
        private volatile int _nbPending;

        [StructLayout(LayoutKind.Auto)]
        private struct CacheEntry
        {
            public uint RefCount;
            public Size TextureSize;
            public IDisposable? Asset;
        }

        public ContentManager(string rootDirectory, TextureLoader textureLoader)
        {
            RootDirectory = rootDirectory;
            _textureLoader = textureLoader;
            _loadTextureFunc = (stream, staging) => _textureLoader.LoadTexture(stream, staging);
            _cache = new FreeList<CacheEntry>();
            _strongHandles = new Dictionary<string, FreeListHandle>();
            _loadedAssets = new ConcurrentBag<(string, IDisposable)>();
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
            if (Path.GetDirectoryName(path) is string dir
                && Path.GetFileName(path) is string filename)
            {
                path = Path.Combine(dir, filename.ToLowerInvariant());
            }
            string fullPath = Path.Combine(RootDirectory, path);
            return File.OpenRead(fullPath);
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
        }
    }
}
