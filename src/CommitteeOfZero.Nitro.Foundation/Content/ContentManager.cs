using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace CommitteeOfZero.Nitro.Foundation.Content
{
    public class ContentManager
    {
        private readonly Dictionary<Type, ContentLoader> _contentLoaders;
        private readonly Dictionary<AssetId, (object asset, int refCount)> _loadedAssets;

        public ContentManager(string rootDirectory)
        {
            if (Instance != null)
            {
                throw new InvalidOperationException("Creating more than 1 instance of ContentManager is not allowed.");
            }

            RootDirectory = rootDirectory;
            _contentLoaders = new Dictionary<Type, ContentLoader>();
            _loadedAssets = new Dictionary<AssetId, (object asset, int refCount)>();

            Instance = this;
        }

        public ContentManager() : this(string.Empty)
        {
        }

        internal static ContentManager Instance { get; private set; }
        public string RootDirectory { get; }

        public bool IsLoaded(AssetId assetId) => _loadedAssets.ContainsKey(assetId);
        public bool Exists(AssetId assetId)
        {
            Stream stream = null;
            try
            {
                stream = OpenStream(assetId);
                return true;
            }
            catch
            {
                return false;
            }
            finally
            {
                stream?.Dispose();
            }
        }

        private T Load<T>(AssetId assetId) => (T)Load(assetId, typeof(T));
        private object Load(AssetId assetId) => Load(assetId, contentType: null);

        private object Load(AssetId assetId, Type contentType)
        {
            if (_loadedAssets.TryGetValue(assetId, out var cacheItem))
            {
                return cacheItem.asset;
            }

            var stream = OpenStream(assetId);
            {
                return Load(stream, assetId, contentType);
            }
        }

        private Type IdentifyContentType(BinaryReader reader)
        {
            Type contentType = null;
            foreach (var pair in _contentLoaders)
            {
                var loader = pair.Value;
                bool match = loader.IsSupportedContentType(reader);
                reader.BaseStream.Seek(0, SeekOrigin.Begin);
                if (match)
                {
                    contentType = pair.Key;
                    break;
                }
            }

            return contentType;
        }

        private object Load(Stream stream, AssetId assetId, Type contentType)
        {
            if (stream == null)
            {
                throw new ContentLoadException($"Failed to load asset '{assetId}': file not found.");
            }

            if (contentType == null)
            {
                using (var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true))
                {
                    contentType = IdentifyContentType(reader);
                }
            }

            if (contentType == null || !_contentLoaders.TryGetValue(contentType, out var loader))
            {
                throw UnsupportedFormat(assetId);
            }

            object asset = loader.Load(stream);
            _loadedAssets[assetId] = (asset, 1);
            return asset;
        }

        public T Get<T>(AssetId id) => (T)_loadedAssets[id].asset;
        public bool TryGetAsset<T>(AssetId assetId, out T asset)
        {
            bool result = _loadedAssets.TryGetValue(assetId, out var cacheItem);
            asset = result ? (T)cacheItem.asset : default(T);
            return result;
        }

        public void RegisterContentLoader(Type t, ContentLoader loader)
        {
            _contentLoaders[t] = loader;
        }

        internal void RegisterReference(AssetId assetId)
        {
            if (_loadedAssets.TryGetValue(assetId, out var cacheItem))
            {
                _loadedAssets[assetId] = (cacheItem.asset, cacheItem.refCount + 1);
                return;
            }

            Load(assetId);
        }

        internal void UnregisterReference(AssetId assetId)
        {
            if (_loadedAssets.TryGetValue(assetId, out var cacheItem))
            {
                _loadedAssets[assetId] = (cacheItem.asset, cacheItem.refCount - 1);

                if (cacheItem.refCount - 1 == 0)
                {
                    (cacheItem.asset as IDisposable)?.Dispose();
                    Debug.WriteLine(assetId + " disposed");

                    _loadedAssets.Remove(assetId);
                }
            }
        }

        protected virtual Stream OpenStream(string path)
        {
            string fullPath = Path.Combine(RootDirectory, path);
            return File.OpenRead(fullPath);
        }

        private Exception UnsupportedFormat(string path)
        {
            return new ContentLoadException($"Failed to load asset '{path}': unsupported format.");
        }
    }
}
