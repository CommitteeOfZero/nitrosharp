using System;
using System.Threading.Tasks.Dataflow;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using SciAdvNet.MediaLayer.Graphics;
using System.Linq;
using SharpDX.Text;
using System.Runtime.CompilerServices;
using SciAdvNet.MediaLayer.Audio;

namespace ProjectHoppy.Content
{
    public class ContentManager
    {
        private readonly Dictionary<Type, ContentLoader> _contentLoaders;

        private readonly ConcurrentDictionary<string, object> _loadedItems;
        private readonly BufferBlock<(string filePath, Type contentType)> _workItems;

        public ContentManager(string rootDirectory)
        {
            RootDirectory = rootDirectory;
            _contentLoaders = new Dictionary<Type, ContentLoader>();

            _loadedItems = new ConcurrentDictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            _workItems = new BufferBlock<(string, Type)>();

            var executionOptions = new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 1 };
            var load = new ActionBlock<(string filePath, Type contentType)>(x =>
            {
                try
                {
                    _loadedItems[x.filePath] = Load(x.filePath, x.contentType);
                }
                catch { }
            }, executionOptions);

            _workItems.LinkTo(load, new DataflowLinkOptions { PropagateCompletion = true });
        }

        public ContentManager() : this(string.Empty)
        {
        }

        public string RootDirectory { get; }

        public void InitContentLoaders(SciAdvNet.MediaLayer.Graphics.ResourceFactory graphicsResourceFactory,
            SciAdvNet.MediaLayer.Audio.ResourceFactory audioResourceFactory)
        {
            RegisterContentLoader(typeof(Texture2D), new TextureLoader(graphicsResourceFactory));
            RegisterContentLoader(typeof(AudioStream), new AudioLoader(audioResourceFactory));
        }

        //public object Load(string path)
        //{
        //    using (var stream = OpenStream(path))
        //    {
        //        var contentType = GetContentType(stream);
        //        if (contentType == null)
        //        {
        //            throw UnsupportedFormat(path);
        //        }

        //        return Load(stream, path, contentType);
        //    }
        //}

        //private Type GetContentType(Stream stream)
        //{
        //    byte[] buffer = new byte[4];
        //    stream.Read(buffer, 0, 4);
        //    stream.Seek(0, SeekOrigin.Begin);

        //    string signature = Encoding.UTF8.GetString(buffer);
        //    return _contentLoaders.Where(x => x.Value.FileSignatures.Contains(signature))
        //        .Select(x => x.Key).FirstOrDefault();
        //}

        public T Load<T>(string path)
        {
            return (T)Load(path, typeof(T));
        }

        public object Load(string path, Type contentType)
        {
            var stream = OpenStream(path);
            {
                return Load(stream, path, contentType);
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
            return asset;
        }

        public void StartLoading<T>(string path) => _workItems.Post((path, typeof(T)));
        public bool IsLoaded(string path) => _loadedItems.ContainsKey(path);
        public T Get<T>(string path) => (T)_loadedItems[path];

        public void RegisterContentLoader(Type t, ContentLoader loader)
        {
            _contentLoaders[t] = loader;
        }

        public virtual Stream OpenStream(string path)
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
