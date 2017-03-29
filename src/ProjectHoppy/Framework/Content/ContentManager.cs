using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using SciAdvNet.MediaLayer.Graphics;
using SciAdvNet.MediaLayer.Audio;
using System.Threading.Tasks;

namespace ProjectHoppy.Framework.Content
{
    public class ContentManager
    {
        private readonly Dictionary<Type, ContentLoader> _contentLoaders;
        private readonly ConcurrentDictionary<string, object> _loadedItems;

        public ContentManager(string rootDirectory, SciAdvNet.MediaLayer.Graphics.ResourceFactory graphicsResourceFactory,
            SciAdvNet.MediaLayer.Audio.ResourceFactory audioResourceFactory)
        {
            RootDirectory = rootDirectory;
            _contentLoaders = new Dictionary<Type, ContentLoader>();

            _loadedItems = new ConcurrentDictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            RegisterContentLoader(typeof(Texture2D), new TextureLoader(graphicsResourceFactory));
            RegisterContentLoader(typeof(AudioStream), new AudioLoader(audioResourceFactory));
        }

        public ContentManager(SciAdvNet.MediaLayer.Graphics.ResourceFactory graphicsResourceFactory,
            SciAdvNet.MediaLayer.Audio.ResourceFactory audioResourceFactory)
            : this(string.Empty, graphicsResourceFactory, audioResourceFactory)
        {
        }

        public string RootDirectory { get; }

        public T Load<T>(AssetRef assetRef)
        {
            return (T)Load(assetRef, typeof(T));
        }

        public object Load(AssetRef assetRef, Type contentType)
        {
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
            _loadedItems[path] = asset;
            return asset;
        }

        public void StartLoading<T>(AssetRef assetRef)
        {
            Task.Run(() => Load(assetRef, typeof(T))).ContinueWith(x =>
            {
                _loadedItems[assetRef] = (T)x.Result;
            }, TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnRanToCompletion);
        }

        public bool TryGetAsset<T>(AssetRef assetRef, out T asset)
        {
            bool result = _loadedItems.TryGetValue(assetRef, out object value);
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
