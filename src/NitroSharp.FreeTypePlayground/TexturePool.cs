using System;
using System.Collections.Generic;
using System.Linq;
using NitroSharp.Primitives;
using Veldrid;

namespace NitroSharp.FreeTypePlayground
{
    internal sealed class TexturePool : IDisposable
    {
        private readonly GraphicsDevice _gd;
        private readonly Bucket _staging, _sampled;
        private readonly PixelFormat _pixelFormat;

        private object _stagingBucketLock = new object();
        private object _sampledBucketLock = new object();

        public TexturePool(GraphicsDevice graphicsDevice, PixelFormat pixelFormat)
        {
            _gd = graphicsDevice;
            _pixelFormat = pixelFormat;
            _staging = new Bucket(this, TextureUsage.Staging);
            _sampled = new Bucket(this, TextureUsage.Sampled);
        }

        public Texture RentStaging(Size minimalSize)
        {
            lock (_stagingBucketLock)
            {
                return _staging.Rent(minimalSize);
            }
        }

        public Texture RentSampled(Size minimalSize)
        {
            lock (_sampledBucketLock)
            {
                return _sampled.Rent(minimalSize);
            }
        }

        public void Return(Texture texture)
        {
            if ((texture.Usage & TextureUsage.Staging) == TextureUsage.Staging)
            {
                lock (_stagingBucketLock)
                {
                    _staging.Return(texture);
                }
            }
            else if ((texture.Usage & TextureUsage.Sampled) == TextureUsage.Sampled)
            {
                lock (_sampledBucketLock)
                {
                    _sampled.Return(texture);
                }
            }
            else
            {
                throw new InvalidOperationException("Texture does not belong to the pool.");
            }
        }

        public void Dispose()
        {
            lock (_stagingBucketLock)
            {
                _staging.Dispose();
            }
            lock (_sampledBucketLock)
            {
                _sampled.Dispose();
            }
        }

        private readonly struct Bucket
        {
            private readonly ResourceFactory _resourceFactory;
            private readonly PixelFormat _pixelFormat;
            private readonly TextureUsage _textureUsage;
            private readonly List<Texture> _available, _leased;

            public Bucket(TexturePool pool, TextureUsage textureUsage)
            {
                _resourceFactory = pool._gd.ResourceFactory;
                _pixelFormat = pool._pixelFormat;
                _textureUsage = textureUsage;
                _available = new List<Texture>();
                _leased = new List<Texture>();
            }

            public Texture Rent(Size minimalSize)
            {
                if (_available.Count == 0)
                {
                    var newTexture = Create(minimalSize);
                    _leased.Add(newTexture);
                    return newTexture;
                }

                var texture = _available.FirstOrDefault(
                    x => x.Width >= minimalSize.Width && x.Height >= minimalSize.Height)
                    ?? Create(minimalSize);

                _available.Remove(texture);
                _leased.Add(texture);
                return texture;
            }

            public void Return(Texture texture)
            {
                if ((texture.Usage & _textureUsage) != _textureUsage)
                {
                    throw new InvalidOperationException($"Texture cannot be returned to the {_textureUsage} texture bucket.");
                }

                _leased.Remove(texture);
                _available.Add(texture);
            }

            private Texture Create(in Size size)
            {
                return _resourceFactory.CreateTexture(TextureDescription.Texture2D(
                    size.Width, size.Height, 1, 1, _pixelFormat, _textureUsage));
            }

            public void Dispose()
            {
                foreach (var texture in _leased)
                {
                    texture.Dispose();
                }
                foreach (var texture in _available)
                {
                    texture.Dispose();
                }

                _leased.Clear();
                _available.Clear();
            }
        }
    }
}
