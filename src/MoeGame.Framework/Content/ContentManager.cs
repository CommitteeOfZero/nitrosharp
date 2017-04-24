using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MoeGame.Framework.Content
{
    public class ContentManager
    {
        private int _nbCurrentlyLoading;

        private readonly Dictionary<Type, ContentLoader> _contentLoaders;
        private readonly ConcurrentDictionary<string, object> _cache;

        public ContentManager(string rootDirectory)
        {
            RootDirectory = rootDirectory;
            _contentLoaders = new Dictionary<Type, ContentLoader>();

            _cache = new ConcurrentDictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        }

        public ContentManager() : this(string.Empty)
        {
        }

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

        public void Unload(AssetRef assetRef)
        {
            if (_cache.TryGetValue(assetRef, out object asset))
            {
                (asset as IDisposable)?.Dispose();
                _cache.TryRemove(assetRef, out var _);
            }
        }

        public object Load(AssetRef assetRef, Type contentType)
        {
            if (_cache.TryGetValue(assetRef, out var asset))
            {
                return asset;
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
            _cache[path] = asset;
            return asset;
        }

        public Task<T> LoadAsync<T>(AssetRef assetRef)
        {
            Interlocked.Increment(ref _nbCurrentlyLoading);
            return Task.Run(() =>
            {
                try
                {
                    var result = Load(assetRef, typeof(T));
                    _cache[assetRef] = (T)result;
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

        public bool TryGetAsset<T>(AssetRef assetRef, out T asset)
        {
            bool result = _cache.TryGetValue(assetRef, out object value);
            asset = result ? (T)value : default(T);
            return result;
        }

        public void RegisterContentLoader(Type t, ContentLoader loader)
        {
            _contentLoaders[t] = loader;
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
