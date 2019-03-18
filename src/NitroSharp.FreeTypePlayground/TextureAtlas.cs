using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using NitroSharp.Utilities;
using Veldrid;

using Rectangle = NitroSharp.Primitives.Rectangle;

namespace NitroSharp.FreeTypePlayground
{
    internal sealed class TextureAtlas : IDisposable
    {
        [StructLayout(LayoutKind.Auto)]
        private struct Layer
        {
            public Bin Bin;
            public Rectangle DirtyRect;
            public MappedResource Map;
            public readonly uint Subresource;

            public Layer(int width, int height, uint subresource)
            {
                Bin = new Bin(width, height);
                DirtyRect = default;
                Map = default;
                Subresource = subresource;
            }
        }

        private readonly GraphicsDevice _gd;
        private readonly Texture _texture;
        private readonly Texture _staging;

        private readonly Layer[] _layers;
        private uint _currentLayer;

        public TextureAtlas(
            GraphicsDevice graphicsDevice,
            uint width,
            uint height,
            uint layerCount,
            PixelFormat pixelFormat)
        {
            _gd = graphicsDevice;
            ResourceFactory factory = _gd.ResourceFactory;
            _texture = factory.CreateTexture(TextureDescription.Texture2D(
                width, height, mipLevels: 1, layerCount, pixelFormat, TextureUsage.Sampled));
            _staging = factory.CreateTexture(TextureDescription.Texture2D(
                width, height, mipLevels: 1, layerCount, pixelFormat, TextureUsage.Staging));

            _layers = new Layer[layerCount];
            (int w, int h) = ((int)width, (int)height);
            for (uint i = 0; i < layerCount; i++)
            {
                uint subresource = _staging.CalculateSubresource(mipLevel: 0, arrayLayer: i);
                _layers[i] = new Layer(w, h, subresource);
            }
        }

        public Texture Texture => _texture;

        public void Begin(bool clear)
        {
            if (clear)
            {
                foreach (ref Layer layer in _layers.AsSpan())
                {
                    layer.Bin.Clear();
                    layer.DirtyRect = default;
                }
            }

            foreach (ref Layer layer in _layers.AsSpan())
            {
                layer.Map = _gd.Map(_staging, MapMode.Write, layer.Subresource);
            }
        }

        public bool TryPackRect(uint width, uint height, out Rectangle rect)
        {
            uint arrayLayer = _currentLayer;
            do
            {
                if (_layers[arrayLayer].Bin.TryPackRect((int)width, (int)height, out rect))
                {
                    return true;
                }

                arrayLayer++;
            } while (arrayLayer < _layers.Length);

            return false;
        }

        public unsafe bool TryPackSprite<T>(
            ReadOnlySpan<T> pixelData,
            uint width,
            uint height,
            out uint arrayLayer,
            out Rectangle rect)
            where T : unmanaged
        {
            (int w, int h) = ((int)width, (int)height);
            uint layerCount = (uint)_layers.Length;
            bool success = false;
            arrayLayer = _currentLayer;
            do
            {
                if (_layers[arrayLayer].Bin.TryPackRect(w, h, out rect))
                {
                    success = true;
                    break;
                }
                arrayLayer++;
            } while (arrayLayer < layerCount);

            if (success)
            {
                _currentLayer = arrayLayer;
                ref Layer layer = ref _layers[arrayLayer];
                ref MappedResource map = ref layer.Map;
                uint bytesPerPixel = (uint)Unsafe.SizeOf<T>();
                fixed (T* src = &pixelData[0])
                {
                    uint srcRowPitch = width * bytesPerPixel;
                    uint srcDepthPitch = srcRowPitch * height;
                    CopyTextureRegion(
                        src,
                        srcX: 0, srcY: 0, srcZ: 0,
                        srcRowPitch, srcDepthPitch,
                        dst: map.Data.ToPointer(),
                        dstX: (uint)rect.X, dstY: (uint)rect.Y, dstZ: 0,
                        map.RowPitch, map.DepthPitch,
                        width, height, depth: 1, bytesPerPixel);
                }

                layer.DirtyRect = layer.DirtyRect.Width > 0
                    ? Rectangle.Union(layer.DirtyRect, rect)
                    : rect;
            }

            return success;
        }

        public void End(CommandList commandList)
        {
            foreach (ref Layer layer in _layers.AsSpan())
            {
                _gd.Unmap(_staging, layer.Subresource);
                ref Rectangle dirtyRect = ref layer.DirtyRect;
                if (layer.DirtyRect.Width > 0)
                {
                    commandList.CopyTexture(
                        source: _staging,
                        srcX: 0, srcY: 0, srcZ: 0,
                        srcMipLevel: 0,
                        srcBaseArrayLayer: layer.Subresource,
                        destination: _texture,
                        dstX: (uint)dirtyRect.X, dstY: (uint)dirtyRect.Y, dstZ: 0,
                        dstMipLevel: 0,
                        dstBaseArrayLayer: layer.Subresource,
                        (uint)dirtyRect.Width, (uint)dirtyRect.Height,
                        depth: 1, layerCount: 1);

                    dirtyRect = default;
                }
            }
        }

        private static unsafe void CopyTextureRegion(
            void* src,
            uint srcX, uint srcY, uint srcZ,
            uint srcRowPitch,
            uint srcDepthPitch,
            void* dst,
            uint dstX, uint dstY, uint dstZ,
            uint dstRowPitch,
            uint dstDepthPitch,
            uint width,
            uint height,
            uint depth,
            uint bytesPerPixel)
        {
            uint rowSize = width * bytesPerPixel;
            if (srcRowPitch == dstRowPitch && srcDepthPitch == dstDepthPitch)
            {
                uint totalCopySize = depth * srcDepthPitch;
                Buffer.MemoryCopy(src, dst, totalCopySize, totalCopySize);
            }
            else
            {
                for (uint zz = 0; zz < depth; zz++)
                {
                    for (uint yy = 0; yy < height; yy++)
                    {
                        byte* rowCopyDst = (byte*)dst
                            + dstDepthPitch * (zz + dstZ)
                            + dstRowPitch * (yy + dstY)
                            + bytesPerPixel * dstX;

                        byte* rowCopySrc = (byte*)src
                            + srcDepthPitch * (zz + srcZ)
                            + srcRowPitch * (yy + srcY)
                            + bytesPerPixel * srcX;

                        Unsafe.CopyBlock(rowCopyDst, rowCopySrc, rowSize);
                    }
                }
            }
        }

        public void Dispose()
        {
            _gd.DisposeWhenIdle(_texture);
            _gd.DisposeWhenIdle(_staging);
        }
    }

    internal struct Bin
    {
        private struct SkylineSegment
        {
            public int X;
            public int Y;
            public int Width;

            public SkylineSegment(int x, int y, int width) : this()
                => (X, Y, Width) = (x, y, width);
        }

        private ArrayBuilder<SkylineSegment> _skyline;
        private readonly int _binWidth;
        private readonly int _binHeight;

        public Bin(int width, int height)
        {
            _binWidth = width;
            _binHeight = height;
            _skyline = new ArrayBuilder<SkylineSegment>();
            _skyline.Add(
                new SkylineSegment(x: 0, y: 0, width)
            );
        }

        public void Clear()
        {
            _skyline.Clear();
            _skyline.Add(
                new SkylineSegment(x: 0, y: 0, _binWidth));
        }

        public bool TryPackRect(int width, int height, out Rectangle rect)
        {
            (int x, int y)? position = FindBestPosition(
                width, height, out int skylineSegIndex, out _, out _);

            if (position == null)
            {
                rect = default;
                return false;
            }

            (int x, int y) = position.Value;

            // Update skyline level
            var skylineSeg = new SkylineSegment(x, y + height, width);
            _skyline.Insert(skylineSegIndex, skylineSeg);
            for (int i = skylineSegIndex + 1; i < _skyline.Count; i++)
            {
                ref SkylineSegment current = ref _skyline[i];
                ref SkylineSegment prev = ref _skyline[i - 1];
                int diff = prev.X + prev.Width - current.X;
                if (diff == 0) { break; }
                current.X += diff;
                current.Width -= diff;
                if (current.Width <= 0)
                {
                    _skyline.Remove(i);
                    i--;
                }
                else
                {
                    break;
                }
            }

            // Merge skyline segements with equal Y coordinates
            for (int i = 0; i < _skyline.Count - 1; i++)
            {
                ref SkylineSegment current = ref _skyline[i];
                ref SkylineSegment next = ref _skyline[i + 1];
                if (current.Y == next.Y)
                {
                    current.Width += next.Width;
                    _skyline.Remove(i + 1);
                    i--;
                }
            }

            rect = new Rectangle(x, y, width, height);
            return true;
        }

        private (int x, int y)? FindBestPosition(int width, int height, out int bestIndex,
                                                 out int bestHeight, out int bestWidth)
        {
            bestHeight = int.MaxValue;
            bestWidth = int.MaxValue;
            bestIndex = -1;

            (int x, int y)? position = null;
            for (int i = 0; i < _skyline.Count; i++)
            {
                if (TryFitRect(startSkylineSeg: i, width, height, out int y))
                {
                    ref SkylineSegment skylineSeg = ref _skyline[i];
                    int newHeight = y + height;
                    if (newHeight < bestHeight ||
                        (newHeight == bestHeight && skylineSeg.Width < bestWidth))
                    {
                        bestHeight = newHeight;
                        bestWidth = skylineSeg.Width;
                        bestIndex = i;
                        position = (skylineSeg.X, y);
                    }
                }
            }

            return position;
        }

        private bool TryFitRect(int startSkylineSeg, int width, int height, out int y)
        {
            int x = _skyline[startSkylineSeg].X;
            y = -1;
            if (x + width > _binWidth)
            {
                return false;
            }

            int widthLeft = width;
            int i = startSkylineSeg;
            while (widthLeft > 0 && i < _skyline.Count)
            {
                y = Math.Max(y, _skyline[i].Y);
                if (y + height > _binHeight)
                {
                    return false;
                }

                widthLeft -= _skyline[i].Width;
                i++;
            }

            return widthLeft <= 0;
        }
    }
}
