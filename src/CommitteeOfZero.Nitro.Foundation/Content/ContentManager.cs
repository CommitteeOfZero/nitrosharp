using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace CommitteeOfZero.Nitro.Foundation.Content
{
    internal class CacheItem
    {
        private volatile int _refCount;

        public CacheItem(object asset)
        {
            Asset = asset;
            _refCount = 1;
        }

        public object Asset { get; }
        public int RefCount => _refCount;

        public void IncrementRefCount()
        {
            Interlocked.Increment(ref _refCount);
        }

        public void DecrementRefCount()
        {
            Interlocked.Decrement(ref _refCount);
        }
    }

    public class ContentManager
    {
        private int _nbCurrentlyLoading;

        private readonly Dictionary<Type, ContentLoader> _contentLoaders;
        private readonly ConcurrentDictionary<AssetRef, CacheItem> _cache;
        private readonly ConcurrentDictionary<AssetRef, object> _currentlyLoading;
        private readonly Queue<AssetRef> _assetsToDispose;

        public ContentManager(string rootDirectory)
        {
            if (Instance != null)
            {
                throw new InvalidOperationException("Creating more than 1 instance of ContentManager is not allowed.");
            }

            RootDirectory = rootDirectory;
            _contentLoaders = new Dictionary<Type, ContentLoader>();

            _cache = new ConcurrentDictionary<AssetRef, CacheItem>();
            _assetsToDispose = new Queue<AssetRef>();
            _currentlyLoading = new ConcurrentDictionary<AssetRef, object>();

            Instance = this;
        }

        public ContentManager() : this(string.Empty)
        {
        }

        internal static ContentManager Instance { get; private set; }
        public string RootDirectory { get; }
        public bool IsBusy => _nbCurrentlyLoading > 0;

        public bool Exists(string path)
        {
            Stream stream = null;
            try
            {
                stream = OpenStream(path);
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

        public T Load<T>(AssetRef assetRef)
        {
            return (T)Load(assetRef, typeof(T));
        }

        public object Load(AssetRef assetRef, Type contentType)
        {
            if (_cache.TryGetValue(assetRef, out var cacheItem))
            {
                cacheItem.IncrementRefCount();
                return cacheItem.Asset;
            }

            var stream = OpenStream(assetRef);
            {
                return Load(stream, assetRef, contentType);
            }
        }

        private object Load(Stream stream, string path, Type contentType)
        {
            if (stream == null)
            {
                throw new ContentLoadException($"Failed to load asset '{path}': file not found.");
            }

            if (!_contentLoaders.TryGetValue(contentType, out var loader))
            {
                throw UnsupportedFormat(path);
            }

            object asset = loader.Load(stream);
            _cache[path] = new CacheItem(asset);
            return asset;
        }

        public Task<T> LoadOnThreadPool<T>(AssetRef assetRef)
        {
            Interlocked.Increment(ref _nbCurrentlyLoading);
            return Task.Run(() =>
            {
                try
                {
                    var result = Load(assetRef, typeof(T));
                    return Task.FromResult((T)result);
                }
                catch
                {
                    return Task.FromResult(default(T));
                }
                finally
                {
                    Interlocked.Decrement(ref _nbCurrentlyLoading);
                }
            });
        }

        public void Unref(AssetRef assetRef)
        {
            if (_cache.TryGetValue(assetRef, out var cacheItem))
            {
                cacheItem.DecrementRefCount();
                if (cacheItem.RefCount <= 0)
                {
                    _assetsToDispose.Enqueue(assetRef);
                }
            }
        }

        public void FlushUnusedAssets()
        {
            while (_assetsToDispose.Count > 0)
            {
                var assetRef = _assetsToDispose.Dequeue();
                if (_cache.TryRemove(assetRef, out var cacheItem))
                {
                    (cacheItem.Asset as IDisposable)?.Dispose();
                }
            }
        }

        public bool TryGetAsset<T>(AssetRef assetRef, out T asset)
        {
            bool result = _cache.TryGetValue(assetRef, out var cacheItem);
            asset = result ? (T)cacheItem.Asset : default(T);
            return result;
        }

        public void RegisterContentLoader(Type t, ContentLoader loader)
        {
            _contentLoaders[t] = loader;
        }

        internal void RegisterReference(AssetRef reference)
        {
            if (_cache.ContainsKey(reference) || _currentlyLoading.ContainsKey(reference))
            {
                return;
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
