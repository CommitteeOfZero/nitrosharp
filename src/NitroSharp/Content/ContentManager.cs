using System;
using System.IO;
using NitroSharp.Media;
using NitroSharp.OldContent;
using NitroSharp.Utilities;
using Veldrid;

#nullable enable

namespace NitroSharp.Content
{
    internal class ContentManager : IDisposable
    {
        private readonly FreeList _handles;
        private readonly TextureLoader _textureLoader;
        private Func<Stream, Texture> _loadTextureFunc;

        private struct CacheEntry
        {

        }

        public ContentManager(string rootDirectory,
            GraphicsDevice graphicsDevice,
            TextureLoader textureLoader)
        {
            RootDirectory = rootDirectory;
            GraphicsDevice = graphicsDevice;
            _handles = new FreeList();
            _textureLoader = textureLoader;
            _loadTextureFunc = stream => _textureLoader.LoadTexture(stream, staging: false);
        }

        public string RootDirectory { get; }
        public GraphicsDevice GraphicsDevice { get; }

        protected virtual Stream OpenStream(string path)
        {
            string fullPath = Path.Combine(RootDirectory, path);
            return File.OpenRead(fullPath);
        }

        public void Dispose()
        {
        }
    }
}
