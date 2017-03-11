using SciAdvNet.MediaLayer.Graphics;
using System;
using System.Collections.Generic;
using System.IO;

namespace ProjectHoppy.Content
{
    public class ContentManager
    {
        private readonly Dictionary<Type, ContentLoader> _contentLoaders;

        public ContentManager(SciAdvNet.MediaLayer.Graphics.ResourceFactory resourceFactory)
        {
            _contentLoaders = new Dictionary<Type, ContentLoader>();

            RegisterContentLoader(typeof(Texture2D), new TextureLoader(resourceFactory));
        }

        public T Load<T>(string path)
        {
            using (var stream = OpenStream(path))
            {
                if (stream == null || !_contentLoaders.TryGetValue(typeof(T), out var loader))
                {
                    throw new ContentLoadException($"Failed to load asset '{path}'");
                }

                return (T)loader.Load(stream);
            }
        }

        public void RegisterContentLoader(Type t, ContentLoader loader)
        {
            _contentLoaders[t] = loader;
        }

        public virtual Stream OpenStream(string path)
        {
            return File.OpenRead(path);
        }
    }
}
