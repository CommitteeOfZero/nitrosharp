using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using NitroSharp.Utilities;
using Veldrid;

#nullable enable

namespace NitroSharp.Content
{
    internal readonly struct AssetPath : IEquatable<AssetPath>
    {
        public readonly string Value;

        public AssetPath(string value) => Value = value;
        public override int GetHashCode() => Value.GetHashCode();
        public bool Equals(AssetPath other) => Value.Equals(other.Value);
    }

    internal readonly struct AssetRef<T> : IEquatable<AssetRef<T>>
        where T : class, IDisposable
    {
        public readonly string Path;
        public readonly FreeListHandle Handle;

        public AssetRef(string path, FreeListHandle handle)
            => (Path, Handle) = (path, handle);

        public bool IsValid => Handle.Version != 0;

        public bool Equals(AssetRef<T> other) => Handle.Equals(other.Handle);
        public override int GetHashCode() => Handle.GetHashCode();
        public override string ToString() => $"Asset '{Path}'";
    }

    internal class ContentManager : IDisposable
    {
        private readonly TextureLoader _textureLoader;
        private readonly Func<Stream, Texture> _loadTextureFunc;

        private readonly FreeList<CacheEntry> _cache;
        private readonly Dictionary<string, FreeListHandle> _strongHandles;
        private readonly ConcurrentBag<(string, IDisposable)> _loadedAssets;
        private volatile int _nbPending;

        [StructLayout(LayoutKind.Auto)]
        private struct CacheEntry
        {
            public uint RefCount;
            public Size TextureDimensions;
            public IDisposable? Asset;
        }

        public ContentManager(string rootDirectory,
            GraphicsDevice graphicsDevice,
            TextureLoader textureLoader)
        {
            RootDirectory = rootDirectory;
            GraphicsDevice = graphicsDevice;
            _textureLoader = textureLoader;
            _loadTextureFunc = stream => _textureLoader.LoadTexture(stream, staging: false);
            _cache = new FreeList<CacheEntry>();
            _strongHandles = new Dictionary<string, FreeListHandle>();
            _loadedAssets = new ConcurrentBag<(string, IDisposable)>();
        }

        public string RootDirectory { get; }
        public GraphicsDevice GraphicsDevice { get; }

        public T? Get<T>(AssetRef<T> assetRef)
            where T : class, IDisposable
        {
            ref CacheEntry cacheEntry = ref _cache.Get(assetRef.Handle);
            return cacheEntry.Asset as T;
        }

        public AssetRef<Texture>? RequestTexture(string path, out Size dimensions)
        {
            if (_strongHandles.TryGetValue(path, out FreeListHandle existing))
            {
                ref CacheEntry cacheEntry = ref _cache.Get(existing);
                cacheEntry.RefCount++;
                dimensions = cacheEntry.TextureDimensions;
                return new AssetRef<Texture>(path, existing);
            }

            Stream? stream;
            FreeListHandle handle;
            try
            {
                stream = OpenStream(path);
                dimensions = _textureLoader.GetTextureDimensions(stream);
                handle = _cache.Insert(new CacheEntry
                {
                    TextureDimensions = dimensions,
                    RefCount = 1
                });
                _strongHandles.Add(path, handle);
            }
            catch
            {
                dimensions = Size.Zero;
                return null;
            }
            Interlocked.Increment(ref _nbPending);
            Task.Run(() =>
            {
                try
                {
                    Texture tex = _loadTextureFunc(stream);
                    _loadedAssets.Add((path, tex));
                }
                catch
                {
                    Interlocked.Decrement(ref _nbPending);
                }
            });
            return new AssetRef<Texture>(path, handle);
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

        protected virtual Stream OpenStream(string path)
        {
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
        }
    }
}
