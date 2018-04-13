using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using NitroSharp.Primitives;
using Veldrid;

namespace NitroSharp.Graphics
{
    internal sealed class RgbaTexturePool : IDisposable
    {
        private readonly GraphicsDevice _gd;
        private readonly Bucket _staging, _sampled;
        private object _stagingBucketLock = new object();
        private object _sampledBucketLock = new object();

        public RgbaTexturePool(GraphicsDevice graphicsDevice)
        {
            _gd = graphicsDevice;
            _staging = new Bucket(_gd.ResourceFactory, TextureUsage.Staging);
            _sampled = new Bucket(_gd.ResourceFactory, TextureUsage.Sampled);
        }
        
        public Texture RentStaging(Size minimalSize, bool clearMemory = false)
        {
            lock (_stagingBucketLock)
            {
                var texture = _staging.Rent(minimalSize);
                if (clearMemory)
                {
                    var map = _gd.Map(texture, MapMode.Write);
                    unsafe
                    {
                        Unsafe.InitBlock(map.Data.ToPointer(), 0x00, map.SizeInBytes);
                    }
                    _gd.Unmap(texture);
                }

                return texture;
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
            switch (texture.Usage)
            {
                case TextureUsage.Staging:
                    lock (_stagingBucketLock)
                    {
                        _staging.Return(texture);
                    }
                    break;

                case TextureUsage.Sampled:
                    lock (_sampledBucketLock)
                    {
                        _sampled.Return(texture);
                    }
                    break;

                default:
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

        private sealed class Bucket
        {
            private readonly ResourceFactory _resourceFactory;
            private readonly TextureUsage _textureUsage;
            private readonly List<Texture> _available, _leased;

            public Bucket(ResourceFactory resourceFactory, TextureUsage textureUsage)
            {
                _resourceFactory = resourceFactory;
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
                if (texture.Usage != _textureUsage)
                {
                    throw new InvalidOperationException($"Texture cannot be returned to the {_textureUsage} texture bucket.");
                }

                _leased.Remove(texture);
                _available.Add(texture);
            }

            private Texture Create(in Size size)
            {
                return _resourceFactory.CreateTexture(TextureDescription.Texture2D(
                    size.Width, size.Height, 1, 1, PixelFormat.R8_G8_B8_A8_UNorm, _textureUsage));
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
