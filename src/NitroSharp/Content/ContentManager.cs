using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NitroSharp.Content
{
    public class ContentManager : IDisposable
    {
        private readonly Dictionary<Type, ContentLoader> _contentLoaders;
        private readonly Dictionary<AssetId, (object asset, int refCount)> _loadedAssets;

        public ContentManager(string rootDirectory)
        {
            RootDirectory = rootDirectory;
            _contentLoaders = new Dictionary<Type, ContentLoader>();
            _loadedAssets = new Dictionary<AssetId, (object asset, int refCount)>();
        }

        public ContentManager() : this(string.Empty)
        {
        }

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

        public virtual IEnumerable<AssetId> Search(string relativePath, string searchPattern)
        {
            string path = Path.Combine(RootDirectory.Replace('/', '\\'), relativePath);
            return Directory.EnumerateFiles(path, searchPattern).Select(x => new AssetId(x));
        }

        public AssetRef<T> Get<T>(AssetId assetId)
        {
            if (_loadedAssets.TryGetValue(assetId, out var cachedItem))
            {
                IncrementRefCount(assetId, cachedItem);
            }
            else
            {
                Load<T>(assetId);
            }

            return new AssetRef<T>(assetId, this);
        }

        public bool TryGet<T>(AssetId assetId, out AssetRef<T> asset)
        {
            try
            {
                asset = Get<T>(assetId);
                return true;
            }
            catch (FileNotFoundException)
            {
                asset = null;
                return false;
            }
        }

        internal T InternalGetCached<T>(AssetId assetId) => (T)_loadedAssets[assetId].asset;

        public void RegisterContentLoader(Type t, ContentLoader loader)
        {
            _contentLoaders[t] = loader;
        }

        private T Load<T>(AssetId assetId) => (T)Load(assetId, typeof(T));
        private object Load(AssetId assetId) => Load(assetId, contentType: null);

        private object Load(AssetId assetId, Type contentType)
        {
            var stream = OpenStream(assetId);
            return Load(stream, assetId, contentType);
        }

        private object Load(Stream stream, AssetId assetId, Type contentType)
        {
            if (stream == null)
            {
                throw new ContentLoadException($"Failed to load asset '{assetId}': file not found.");
            }

            if (contentType == null || !_contentLoaders.TryGetValue(contentType, out var loader))
            {
                throw UnsupportedFormat(assetId);
            }

            object asset = loader.Load(stream);
            _loadedAssets[assetId] = (asset, 1);
            return asset;
        }

        private int IncrementRefCount(AssetId assetId, (object asset, int refCount) cachedItem)
        {
            _loadedAssets[assetId] = (cachedItem.asset, cachedItem.refCount + 1);
            return cachedItem.refCount + 1;
        }

        private int DecrementRefCount(AssetId assetId, (object asset, int refCount) cachedItem)
        {
            _loadedAssets[assetId] = (cachedItem.asset, cachedItem.refCount - 1);
            return cachedItem.refCount - 1;
        }

        internal void ReleaseReference(AssetId assetId)
        {
            if (_loadedAssets.TryGetValue(assetId, out var cachedItem))
            {
                int newRefCount = DecrementRefCount(assetId, cachedItem);
                if (newRefCount == 0)
                {
                    (cachedItem.asset as IDisposable)?.Dispose();
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

        public virtual void Dispose()
        {
            foreach (var cachedItem in _loadedAssets.Values)
            {
                (cachedItem.asset as IDisposable)?.Dispose();
            }

            _loadedAssets.Clear();
        }
    }
}
