using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using NitroSharp.Utilities;
using Veldrid;

namespace NitroSharp.Graphics
{
    internal readonly ref struct GpuListSlice<T>
        where T : unmanaged
    {
        public readonly DeviceBuffer Buffer;
        public readonly Span<T> Data;

        public GpuListSlice(DeviceBuffer buffer, Span<T> data)
        {
            Buffer = buffer;
            Data = data;
        }
    }

    internal sealed unsafe class GpuList<T> : IDisposable
        where T : unmanaged
    {
        private DeviceBuffer? _buffer;
        private readonly GraphicsDevice _gd;
        private readonly BufferUsage _usage;
        private readonly uint _vertexSize;
        private readonly MapMode _mapMode;
        private DeviceBuffer? _stagingBuffer;
        private MappedResource _map;
        private uint _capacity;
        private int _cursor;

        private readonly List<(DeviceBuffer, DeviceBuffer)> _oldBuffers;

        public GpuList(
            GraphicsDevice graphicsDevice,
            BufferUsage usage,
            uint initialCapacity,
            bool retainBetweenFrames = false)
        {
            _gd = graphicsDevice;
            _usage = usage;
            _vertexSize = (uint)Unsafe.SizeOf<T>();
            _mapMode = retainBetweenFrames
                ? MapMode.ReadWrite
                : MapMode.Write;
            _oldBuffers = new List<(DeviceBuffer, DeviceBuffer)>();
            Grow(initialCapacity);
        }

        public uint Count => (uint)_cursor;
        public uint Capacity => _capacity;

        public ref T this[uint index] => ref Get(index);

        public ref T InsertAt(uint index)
        {
            if (index >= _capacity)
            {
                Grow(Math.Max(_capacity * 2, index));
            }
            if (index >= _cursor)
            {
                _cursor = (int)index + 1;
            }

            return ref Get(index);
        }

        private ref T Get(uint index)
        {
            static void notMapped() =>
                throw new InvalidOperationException(
                    "Cannot access the contents of the buffer as it is not currently mapped.");

            static void outOfBounds() => throw new IndexOutOfRangeException();

            if (_map.Data == IntPtr.Zero) { notMapped(); }
            if (index >= _cursor) { outOfBounds(); }

            var ptr = (T*)Unsafe.Add<T>((void*)_map.Data, (int)index);
            return ref Unsafe.AsRef<T>(ptr);
        }

        public void Begin(bool resetPosition = true)
        {
            foreach ((DeviceBuffer staging, DeviceBuffer gpuBuf) in _oldBuffers)
            {
                staging.Dispose();
                gpuBuf.Dispose();
            }
            _oldBuffers.Clear();
            _map = _gd.Map(_stagingBuffer, _mapMode);
            if (resetPosition)
            {
                _cursor = 0;
            }
        }

        public (DeviceBuffer buffer, uint index) Append(in T vertex)
        {
            Debug.Assert(_buffer != null);
            EnsureCapacity(_cursor + 1);
            var ptr = (T*)Unsafe.Add<T>((void*)_map.Data, _cursor);
            *ptr = vertex;
            return (_buffer, (uint)_cursor++);
        }

        public GpuListSlice<T> Append(uint count, out uint position)
        {
            Debug.Assert(_buffer != null);
            EnsureCapacity(_cursor + count);
            position = (uint)_cursor;
            var span = new Span<T>((void*)_map.Data, (int)_capacity)
                .Slice(_cursor, (int)count);
            _cursor += (int)count;
            return new GpuListSlice<T>(_buffer, span);
        }

        public GpuListSlice<T> Append(uint count)
        {
            Debug.Assert(_buffer != null);
            EnsureCapacity(_cursor + count);
            var dst = new Span<T>((void*)_map.Data, (int)_capacity);
            int cursor = _cursor;
            _cursor += (int)count;
            return new GpuListSlice<T>(_buffer, dst.Slice(cursor, (int)count));
        }

        public void End(CommandList commandList)
        {
            _gd.Unmap(_stagingBuffer);
            _map = default;
            foreach ((DeviceBuffer src, DeviceBuffer dst) in _oldBuffers)
            {
                commandList.CopyBuffer(src, 0, dst, 0, src.SizeInBytes);
            }
            uint totalVertices = (uint)_cursor;
            if (totalVertices > 0)
            {
                uint size = _vertexSize * totalVertices;
                commandList.CopyBuffer(_stagingBuffer, 0, _buffer, 0, size);
            }
        }

        private void EnsureCapacity(long requiredCapacity)
        {
            if (requiredCapacity >= _capacity)
            {
                Grow(Math.Max(_capacity * 2, requiredCapacity));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Grow(long newCapacity)
        {
            Debug.Assert(newCapacity > _capacity);
            uint size = MathUtil.RoundUp((uint)newCapacity * _vertexSize, 16);
            DeviceBuffer newStagingBuffer = _gd.ResourceFactory.CreateBuffer(
                new BufferDescription(size, BufferUsage.Staging)
            );
            DeviceBuffer newDeviceBuffer = _gd.ResourceFactory.CreateBuffer(
                new BufferDescription(size, _usage)
            );

            if (_buffer != null && _stagingBuffer != null)
            {
                _oldBuffers.Add((_stagingBuffer, _buffer));
                MappedResource newMap = _gd.Map(newStagingBuffer, _mapMode);
                var src = new Span<T>((void*)_map.Data, (int)_capacity);
                var dst = new Span<T>((void*)newMap.Data, (int)newCapacity);
                src.CopyTo(dst);
                _gd.Unmap(_map.Resource);
                _map = newMap;
            }

            _capacity = (uint)newCapacity;
            _stagingBuffer = newStagingBuffer;
            _buffer = newDeviceBuffer;
        }

        public void Dispose()
        {
            _stagingBuffer!.Dispose();
            _buffer!.Dispose();
        }
    }
}
