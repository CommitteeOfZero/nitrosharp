using SciAdvNet.MediaLayer.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ProjectHoppy.Content
{
    public class ContentManager
    {
        private readonly Dictionary<Type, ContentLoader> _contentLoaders;

        public ContentManager()
        {
            _contentLoaders = new Dictionary<Type, ContentLoader>();
        }

        public void InitContentLoaders(SciAdvNet.MediaLayer.Graphics.ResourceFactory resourceFactory)
        {
            RegisterContentLoader(typeof(Texture2D), new TextureLoader(resourceFactory));
        }

        public object Load(string path)
        {
            using (var stream = OpenStream(path))
            {
                var contentType = GetContentType(path);
                if (contentType == null)
                {
                    throw UnsupportedFormat(path);
                }

                return Load(stream, path, contentType);
            }
        }

        private Type GetContentType(string path)
        {
            string ext = Path.GetExtension(path);
            return _contentLoaders.Where(x => x.Value.FileExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase))
                .Select(x => x.Key).FirstOrDefault();
        }

        //private Type GetContentType(Stream stream)
        //{
        //    byte[] buffer = new byte[4];
        //    stream.Read(buffer, 0, 4);
        //    stream.Seek(0, SeekOrigin.Begin);
        //    stream.pe

        //    string signature = Encoding.UTF8.GetString(buffer);
        //    return _contentLoaders.Where(x => x.Value.FileSignatures.Contains(signature))
        //        .Select(x => x.Key).FirstOrDefault();
        //}

        public T Load<T>(string path)
        {
            using (var stream = OpenStream(path))
            {
                return (T)Load(stream, path, typeof(T));
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

        public void RegisterContentLoader(Type t, ContentLoader loader)
        {
            _contentLoaders[t] = loader;
        }

        public virtual Stream OpenStream(string path)
        {
            return File.OpenRead(path);
        }

        private Exception UnsupportedFormat(string path)
        {
            return new ContentLoadException($"Failed to load asset '{path}': unsupported format.");
        }
    }
}
