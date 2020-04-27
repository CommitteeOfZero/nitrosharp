using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using NitroSharp.Utilities;
using Veldrid;

#nullable enable

namespace NitroSharp.Graphics.Core
{
    internal interface GpuType
    {
        void WriteGpuBlocks(Span<Vector4> blocks);
    }

    internal readonly struct GpuCacheHandle
    {
        public readonly FreeListHandle Value;

        public GpuCacheHandle(FreeListHandle freeListHandle)
            => Value = freeListHandle;

        public static GpuCacheHandle Invalid => default;
    }

    internal sealed class GpuCache<T> : IDisposable
        where T : unmanaged, GpuType
    {
        private const uint GpuBlockSize = 4 * sizeof(float);

        private readonly GraphicsDevice _gd;
        private readonly uint _blocksPerSlot;
        private readonly FreeList _slots;
        private uint _firstDirtySlot;
        private uint _lastDirtySlot;

        private readonly Texture _stagingTexture;
        private readonly Texture _sampledTexture;
        private readonly bool _usingGL;
        private readonly MapMode _mapMode;
        private readonly uint _dimension;
        private readonly uint _capacity;
        private MappedResource _map;
        private bool _textureMapped;

        private IntPtr _glHostMemory;

        public unsafe GpuCache(
            GraphicsDevice graphicsDevice,
            uint typeSizeInGpuBlocks,
            uint dimension = 128)
        {
            _gd = graphicsDevice;
            _blocksPerSlot = MathUtil.NearestPowerOfTwo(typeSizeInGpuBlocks);
            _slots = new FreeList();
            //_usingGL = _gd.BackendType == GraphicsBackend.OpenGL
            //        || _gd.BackendType == GraphicsBackend.OpenGLES;
            _usingGL = false;
            _mapMode = _usingGL ? MapMode.Write : MapMode.ReadWrite;
            _dimension = dimension = MathUtil.NearestPowerOfTwo(dimension);
            _capacity = dimension * dimension / _blocksPerSlot;
            var desc = TextureDescription.Texture2D(
               dimension, dimension, mipLevels: 1, arrayLayers: 1,
               PixelFormat.R32_G32_B32_A32_Float, TextureUsage.Staging
            );
            ResourceFactory rf = _gd.ResourceFactory;
            _stagingTexture = rf.CreateTexture(ref desc);
            desc.Usage = TextureUsage.Sampled;
            _sampledTexture = rf.CreateTexture(ref desc);
            if (_usingGL)
            {
                int dataSize = (int)(dimension * dimension * GpuBlockSize);
                _glHostMemory = Marshal.AllocHGlobal(dataSize);
                var span = new Span<byte>(_glHostMemory.ToPointer(), dataSize);
                span.Clear();
            }
        }

        public Texture Texture => _sampledTexture;

        private unsafe Span<Vector4> GetBlocks(uint slot, bool freeing = false)
        {
            if (slot >= _capacity)
            {
                throw new Exception("BUG: GpuCache is too small.");
            }
            if (!freeing)
            {
                _firstDirtySlot = Math.Min(_firstDirtySlot, slot);
                _lastDirtySlot = Math.Max(_lastDirtySlot, slot);
            }
            IntPtr ptr = _usingGL ? _glHostMemory : _map.Data;
            var allBlocks = new Span<Vector4>(
                ptr.ToPointer(),
                length: (int)(_dimension * _dimension)
            );
            return allBlocks.Slice(
                start: (int)(slot * _blocksPerSlot),
                length: (int)_blocksPerSlot
            );
        }

        public void BeginFrame(bool clear = false)
        {
            if (clear)
            {
                _slots.Clear();
            }

            _map = _gd.Map(_stagingTexture, _mapMode);
            _textureMapped = true;
            _firstDirtySlot = uint.MaxValue;
            _lastDirtySlot = uint.MinValue;
        }

        public GpuCacheHandle Insert(ref T value)
        {
            FreeListHandle slot = _slots.Next();
            Span<Vector4> blocks = GetBlocks(slot.Index);
            value.WriteGpuBlocks(blocks);
            return new GpuCacheHandle(slot);
        }

        public int GetCachePosition(GpuCacheHandle handle)
        {
            FreeListHandle freeListHandle = handle.Value;
            _slots.ThrowIfInvalid(freeListHandle);
            return (int)freeListHandle.Index;
        }

        public void Upsert(ref GpuCacheHandle handle, ref T value)
        {
            FreeListHandle freeListHandle = handle.Value;
            if (_slots.ValidateHandle(freeListHandle))
            {
                Span<Vector4> blocks = GetBlocks(freeListHandle.Index);
                value.WriteGpuBlocks(blocks);
                return;
            }

            handle = Insert(ref value);
        }

        public void Free(ref GpuCacheHandle handle)
        {
            FreeListHandle freeListHandle = handle.Value;
            _slots.ThrowIfInvalid(freeListHandle);
            Span<Vector4> blocks = GetBlocks(freeListHandle.Index, freeing: true);
            blocks.Clear();
            _slots.Free(ref freeListHandle);
            handle = GpuCacheHandle.Invalid;
        }

        private (uint x, uint y) GetCoords(uint slot)
            => (slot * _blocksPerSlot % _dimension,
                slot * _blocksPerSlot / _dimension);

        public unsafe void EndFrame(CommandList commandList)
        {
            if (!_usingGL)
            {
                _gd.Unmap(_stagingTexture);
            }
            if (_firstDirtySlot != uint.MaxValue)
            {
                (uint xFirst, uint yFirst) = GetCoords(_firstDirtySlot);
                (uint xLast, uint yLast) = GetCoords(_lastDirtySlot);

                if (_usingGL)
                {
                    uint start = GpuBlockSize * (yFirst * _dimension + xFirst);
                    uint end = GpuBlockSize * (yLast * _dimension + xLast + _blocksPerSlot) + 1;
                    var src = new Span<byte>(
                        (byte*)_glHostMemory.ToPointer() + start,
                        length: (int)(end - start)
                    );
                    var dst = new Span<byte>(
                        (byte*)_map.Data.ToPointer() + start,
                        length: (int)(end - start)
                    );
                    src.CopyTo(dst);
                    _gd.Unmap(_stagingTexture);
                }

                uint left, width;
                if (yFirst == yLast)
                {
                    left = xFirst;
                    width = xLast - xFirst + _blocksPerSlot;
                }
                else
                {
                    left = 0;
                    width = _dimension;
                }

                commandList.CopyTexture(
                    source: _stagingTexture,
                    srcX: left, srcY: yFirst, srcZ: 0,
                    srcMipLevel: 0, srcBaseArrayLayer: 0,
                    destination: _sampledTexture,
                    dstX: left, dstY: yFirst, dstZ: 0,
                    dstMipLevel: 0, dstBaseArrayLayer: 0,
                    width, height: yLast - yFirst + 1,
                    depth: 1, layerCount: 1
                );
            }
            _textureMapped = false;
        }

        public void Dispose()
        {
            if (_textureMapped)
            {
                _gd.Unmap(_stagingTexture);
                _textureMapped = false;
            }
            _stagingTexture.Dispose();
            _sampledTexture.Dispose();
            if (_glHostMemory != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(_glHostMemory);
                _glHostMemory = IntPtr.Zero;
            }
        }
    }
}
