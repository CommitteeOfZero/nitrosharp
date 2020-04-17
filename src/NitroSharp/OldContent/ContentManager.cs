using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NitroSharp.Media;
using NitroSharp.Media.Decoding;
using Veldrid;

#nullable enable

namespace NitroSharp.OldContent
{
    internal class ContentManager : IDisposable
    {
        private struct CacheEntry
        {
            public IDisposable Asset;
            public uint ReferenceCount;
        }

        private readonly Dictionary<AssetId, CacheEntry> _loadedAssets;
        private readonly Func<Stream, Texture> _loadTextureFunc;
        private readonly VideoFrameConverter _videoFrameConverter;
        private readonly Func<Stream, MediaPlaybackSession> _loadMediaClipFunc;
        private readonly TextureLoader _textureLoader;
        private readonly AudioDevice _audioDevice;

        private readonly ConcurrentQueue<LoadResult> _loadedTextures;
        private readonly ConcurrentDictionary<AssetId, PendingTextureLoad> _pendingTextureLoads;
        private volatile int _nbPendingTextures;

        private struct LoadResult
        {
            public AssetId Id;
            public Texture Texture;

            public LoadResult(AssetId id, Texture texture)
            {
                Id = id;
                Texture = texture;
            }
        }

        private readonly struct PendingTextureLoad : IEquatable<PendingTextureLoad>
        {
            public readonly Size Dimensions;
            public readonly uint ReferenceCount;

            public PendingTextureLoad(Size dimensions, uint referenceCount)
            {
                Dimensions = dimensions;
                ReferenceCount = referenceCount;
            }

            public bool Equals(PendingTextureLoad other)
                => Dimensions.Equals(other.Dimensions)
                    && ReferenceCount == other.ReferenceCount;

            public override int GetHashCode()
                => HashCode.Combine(Dimensions, ReferenceCount);
        }

        public ContentManager(
            string rootDirectory,
            GraphicsDevice graphicsDevice,
            TextureLoader textureLoader,
            AudioDevice audioDevice)
        {
            RootDirectory = rootDirectory;
            GraphicsDevice = graphicsDevice;
            _textureLoader = textureLoader;
            _audioDevice = audioDevice;
            _loadedAssets = new Dictionary<AssetId, CacheEntry>();
            _loadTextureFunc = stream => _textureLoader.LoadTexture(stream, staging: false);
            _videoFrameConverter = new VideoFrameConverter();
            _loadMediaClipFunc = LoadMediaClip;

            _loadedTextures = new ConcurrentQueue<LoadResult>();
            _pendingTextureLoads = new ConcurrentDictionary<AssetId, PendingTextureLoad>();
        }

        public string RootDirectory { get; }
        public GraphicsDevice GraphicsDevice { get; }

        /// <exception cref="ContentLoadException" />
        public Texture LoadTexture(AssetId textureId, bool staging)
        {
            Stream stream = OpenStream(textureId.NormalizedPath);
            return _textureLoader.LoadTexture(stream, staging);
        }

        /// <exception cref="ContentLoadException" />
        public Texture GetTexture(AssetId textureId, bool incrementRefCount = true)
            => GetAsset<Texture>(textureId, _loadTextureFunc, incrementRefCount);

        public Texture? TryGetTexture(AssetId textureId)
        {
            if (_loadedAssets.TryGetValue(textureId, out CacheEntry cacheEntry))
            {
                return cacheEntry.Asset as Texture;
            }
            return null;
        }

        /// <exception cref="ContentLoadException" />
        public MediaPlaybackSession GetMediaClip(AssetId assetId, bool incrementRefCount = true)
            => GetAsset<MediaPlaybackSession>(assetId, _loadMediaClipFunc, incrementRefCount);

        public bool RequestTexture(AssetId textureId, out Size dimensions, bool incrementRefCount = true)
        {
            if (_pendingTextureLoads.TryGetValue(textureId, out PendingTextureLoad pendingLoad))
            {
                if (incrementRefCount)
                {
                    _pendingTextureLoads[textureId] = new PendingTextureLoad(
                        pendingLoad.Dimensions,
                        pendingLoad.ReferenceCount + 1
                    );
                }
                dimensions = pendingLoad.Dimensions;
                return true;
            }
            Stream? stream = null;
            try
            {
                stream = OpenStream(textureId.NormalizedPath);
                dimensions = _textureLoader.GetTextureDimensions(stream);
            }
            catch
            {
                dimensions = default;
                return false;
            }
            Interlocked.Increment(ref _nbPendingTextures);
            uint refCount = incrementRefCount ? 1u : 0u;
            _pendingTextureLoads.TryAdd(textureId, new PendingTextureLoad(dimensions, refCount));
            Task.Run(() =>
            {
                try
                {
                    Texture tex = _loadTextureFunc(stream);
                    _loadedTextures.Enqueue(new LoadResult(textureId, tex));
                }
                catch (Exception)
                {
                    Interlocked.Decrement(ref _nbPendingTextures);
                    _pendingTextureLoads.TryRemove(textureId, out _);
                }
            });
            return true;
        }

        public bool ResolveTextures()
        {
            if (_nbPendingTextures > 0)
            {
                while (_loadedTextures.TryDequeue(out LoadResult loadResult))
                {
                    Interlocked.Decrement(ref _nbPendingTextures);
                    _pendingTextureLoads.TryRemove(loadResult.Id, out PendingTextureLoad load);
                    var cacheEntry = new CacheEntry
                    {
                        Asset = loadResult.Texture,
                        ReferenceCount = load.ReferenceCount
                    };
                    _loadedAssets[loadResult.Id] = cacheEntry;
                }
            }

            return _nbPendingTextures == 0;
        }

        public MediaPlaybackSession? TryGetMediaClip(AssetId assetId, bool incrementRefCount = true)
        {
            try
            {
                return GetAsset<MediaPlaybackSession>(assetId, _loadMediaClipFunc, incrementRefCount);
            }
            catch (ContentLoadException)
            {
                return null;
            }
        }

        private MediaPlaybackSession LoadMediaClip(Stream stream)
        {
            var container = MediaContainer.Open(stream);
            var options = new MediaProcessingOptions(
                _audioDevice.AudioParameters,
                _videoFrameConverter);
            return new MediaPlaybackSession(container, options);
        }

        /// <exception cref="ContentLoadException" />
        private T GetAsset<T>(AssetId assetId, Func<Stream, IDisposable> loader, bool incrementRefCount = true)
            where T : class, IDisposable
        {
            if (!_loadedAssets.TryGetValue(assetId, out CacheEntry cacheEntry))
            {
                try
                {
                    Stream stream = OpenStream(assetId.NormalizedPath);
                    // The loader is responsible for disposing the stream
                    cacheEntry = new CacheEntry
                    {
                        Asset = loader(stream),
                        ReferenceCount = 0
                    };
                }
                catch (Exception ex)
                {
                    throw new ContentLoadException($"Couldn't load asset '{assetId.NormalizedPath}'.", ex);
                }
            }

            if (incrementRefCount)
            {
                cacheEntry.ReferenceCount++;
            }
            _loadedAssets[assetId] = cacheEntry;
            return (T)cacheEntry.Asset;
        }

        public void UnrefAsset(AssetId assetId)
        {
            if (_loadedAssets.TryGetValue(assetId, out CacheEntry cacheEntry))
            {
                if (--cacheEntry.ReferenceCount == 0)
                {
                    cacheEntry.Asset.Dispose();
                    _loadedAssets.Remove(assetId);
                }
                else
                {
                    _loadedAssets[assetId] = cacheEntry;
                }
            }
        }

        public virtual IEnumerable<AssetId> Search(string relativePath, string searchPattern)
        {
            string path = Path.Combine(RootDirectory.Replace("\\", "/"), relativePath);
            try
            {
                return Directory.EnumerateFiles(path, searchPattern)
                    .Select(x => new AssetId(x));
            }
            catch (Exception)
            {
                return Enumerable.Empty<AssetId>();
            }
        }

        protected virtual Stream OpenStream(string path)
        {
            string fullPath = Path.Combine(RootDirectory, path);
            return File.OpenRead(fullPath);
        }

        public virtual void Dispose()
        {
            foreach (CacheEntry cachedItem in _loadedAssets.Values)
            {
                cachedItem.Asset.Dispose();
            }

            _loadedAssets.Clear();
            _textureLoader.Dispose();
            _videoFrameConverter.Dispose();
        }
    }

    public sealed class ContentLoadException : Exception
    {
        public ContentLoadException()
        {
        }

        public ContentLoadException(string message)
            : base(message)
        {
        }

        public ContentLoadException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
