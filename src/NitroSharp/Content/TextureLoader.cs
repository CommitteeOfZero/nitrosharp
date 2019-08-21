using System;
using System.IO;
using NitroSharp.Graphics;
using Veldrid;

#nullable enable

namespace NitroSharp.Content
{
    internal abstract class TextureLoader : IDisposable
    {
        protected readonly GraphicsDevice _gd;
        protected readonly ResourceFactory _rf;
        protected readonly TexturePool _texturePool;
        private readonly CommandList _cl;

        protected TextureLoader(GraphicsDevice graphicsDevice, TexturePool texturePool)
        {
            _gd = graphicsDevice;
            _rf = graphicsDevice.ResourceFactory;
            _texturePool = texturePool;
            _cl = _gd.ResourceFactory.CreateCommandList();
        }

        protected abstract Texture LoadStaging(Stream stream);

        public Texture LoadTexture(Stream stream, bool staging)
        {
            using (stream)
            {
                Texture stagingTex = LoadStaging(stream);
                if (staging) { return stagingTex; }

                Texture sampledTex = _rf.CreateTexture(TextureDescription.Texture2D(
                    stagingTex.Width, stagingTex.Height, mipLevels: 1, arrayLayers: 1,
                    PixelFormat.R8_G8_B8_A8_UNorm, TextureUsage.Sampled
                ));

                _cl.Begin();
                _cl.CopyTexture(source: stagingTex, destination: sampledTex);
                _cl.End();
                _gd.SubmitCommands(_cl);
                // TODO: report the GL backend's quirk upstream
                if (_gd.BackendType == GraphicsBackend.OpenGL)
                {
                    _gd.DisposeWhenIdle(stagingTex);
                }
                else
                {
                    stagingTex.Dispose();
                }
                return sampledTex;
            }
        }

        public virtual void Dispose()
        {
            _cl.Dispose();
        }
    }
}
