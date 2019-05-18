using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NitroSharp.Media;
using NitroSharp.Media.Decoding;
using Veldrid;

#nullable enable

namespace NitroSharp.Content
{
    internal class ContentManager : IDisposable
    {
        private struct CacheEntry
        {
            public IDisposable Asset;
            public int ReferenceCount;
        }

        private readonly Dictionary<AssetId, CacheEntry> _loadedAssets;
        private readonly Func<Stream, Texture> _loadTextureFunc;
        private readonly VideoFrameConverter _videoFrameConverter;
        private readonly Func<Stream, MediaPlaybackSession> _loadMediaClipFunc;
        private readonly TextureLoader _textureLoader;
        private readonly AudioDevice _audioDevice;

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
        public Texture GetTexture(AssetId textureId, bool increaseRefCount = true)
            => GetAsset<Texture>(textureId, _loadTextureFunc, increaseRefCount);

        /// <exception cref="ContentLoadException" />
        public MediaPlaybackSession GetMediaClip(AssetId assetId, bool increaseRefCount = true)
            => GetAsset<MediaPlaybackSession>(assetId, _loadMediaClipFunc, increaseRefCount);

        public MediaPlaybackSession? TryGetMediaClip(AssetId assetId, bool increaseRefCount = true)
        {
            try
            {
                return GetAsset<MediaPlaybackSession>(assetId, _loadMediaClipFunc, increaseRefCount);
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
        private T GetAsset<T>(AssetId assetId, Func<Stream, IDisposable> loader, bool increaseRefCount = true)
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

            if (increaseRefCount)
            {
                cacheEntry.ReferenceCount++;
                _loadedAssets[assetId] = cacheEntry;
            }
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
}
