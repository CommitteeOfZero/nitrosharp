using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using NitroSharp.Utilities;
using Veldrid;

#nullable enable

namespace NitroSharp.Graphics
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

        private Texture? _stagingTexture;
        private Texture? _sampledTexture;
        private readonly bool _usingGL;
        private readonly MapMode _mapMode;
        private bool _reallocatedThisFrame;
        private uint _dimension;
        private uint _capacity;
        private MappedResource _map;
        private bool _textureMapped;

        private IntPtr _glHostMemory;

        public GpuCache(
            GraphicsDevice graphicsDevice,
            uint typeSizeInGpuBlocks,
            uint initialTextureDimension = 128)
        {
            static uint powerOfTwo(uint size)
            {
                uint sz = size - 1;
                sz |= sz >> 1;
                sz |= sz >> 2;
                sz |= sz >> 4;
                sz |= sz >> 8;
                sz |= sz >> 16;
                return sz + 1;
            }

            _gd = graphicsDevice;
            _blocksPerSlot = powerOfTwo(typeSizeInGpuBlocks);
            _slots = new FreeList();
            //_usingGL = _gd.BackendType == GraphicsBackend.OpenGL
            //        || _gd.BackendType == GraphicsBackend.OpenGLES;
            _usingGL = false;
            _mapMode = _usingGL ? MapMode.Write : MapMode.ReadWrite;
            Resize(powerOfTwo(initialTextureDimension));
        }

        private unsafe void Resize(uint dimension)
        {
            var desc = TextureDescription.Texture2D(
               dimension, dimension, mipLevels: 1, arrayLayers: 1,
               PixelFormat.R32_G32_B32_A32_Float, TextureUsage.Staging
            );
            ResourceFactory rf = _gd.ResourceFactory;
            Texture newStaging = rf.CreateTexture(ref desc);
            desc.Usage = TextureUsage.Sampled;
            Texture newSampled = rf.CreateTexture(ref desc);
            IntPtr newGLHostMemory = IntPtr.Zero;
            if (_usingGL)
            {
                int dataSize = (int)(dimension * dimension * GpuBlockSize);
                newGLHostMemory = Marshal.AllocHGlobal(dataSize);
                var span = new Span<byte>(newGLHostMemory.ToPointer(), dataSize);
                span.Clear();
            }
            if (_stagingTexture != null)
            {
                MappedResource srcMap = _map;
                MappedResource dstMap = _gd.Map(newStaging, _mapMode);
                int blockCount = (int)((_lastDirtySlot + 1) * _blocksPerSlot);
                var src = new Span<Vector4>(srcMap.Data.ToPointer(), blockCount);
                var dst = new Span<Vector4>(dstMap.Data.ToPointer(), blockCount);
                src.CopyTo(dst);
                _gd.Unmap(_stagingTexture);
                _map = dstMap;
                _gd.DisposeWhenIdle(_stagingTexture);
                _gd.DisposeWhenIdle(_sampledTexture);
                if (_glHostMemory != IntPtr.Zero)
                {
                    src = new Span<Vector4>(_glHostMemory.ToPointer(), blockCount);
                    dst = new Span<Vector4>(newGLHostMemory.ToPointer(), blockCount);
                    src.CopyTo(dst);
                    Marshal.FreeHGlobal(_glHostMemory);
                }
                _firstDirtySlot = 0;
                uint oldCapacity = _capacity;
                _lastDirtySlot = oldCapacity - 1;
            }

            _stagingTexture = newStaging;
            _sampledTexture = newSampled;
            _glHostMemory = newGLHostMemory;
            _dimension = dimension;
            _capacity = dimension * dimension / _blocksPerSlot;
            _reallocatedThisFrame = true;
        }

        public Texture GetCacheTexture(out bool reallocatedThisFrame)
        {
            static void inUse() => throw new InvalidOperationException(
                "The requested GpuCache texture is currently in use. " +
                "Did you forget to call EndFrame()?");

            Debug.Assert(_sampledTexture != null);
            if (_textureMapped) { inUse(); }
            reallocatedThisFrame = _reallocatedThisFrame;
            return _sampledTexture;
        }

        private unsafe Span<Vector4> GetBlocks(uint slot, bool freeing = false)
        {
            if (slot >= _capacity)
            {
                Resize(_dimension * 2);
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
            _reallocatedThisFrame = false;
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
            Debug.Assert(_stagingTexture != null);
            Debug.Assert(_sampledTexture != null);
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
