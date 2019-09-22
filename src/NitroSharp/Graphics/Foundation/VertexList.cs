using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using NitroSharp.Utilities;
using Veldrid;

#nullable enable

namespace NitroSharp.Graphics
{
    internal sealed unsafe class VertexList<TVertex> : VertexBuffer
        where TVertex : unmanaged
    {
        private readonly GraphicsDevice _gd;
        private readonly uint _vertexSize;
        private readonly MapMode _mapMode;
        private DeviceBuffer? _stagingBuffer;
        private MappedResource _map;
        private uint _capacity;
        private int _cursor;
        private bool _bufferLocked;

        public VertexList(
            GraphicsDevice graphicsDevice,
            uint initialCapacity,
            bool retainBetweenFrames = false)
        {
            _gd = graphicsDevice;
            _vertexSize = (uint)Unsafe.SizeOf<TVertex>();
            _mapMode = retainBetweenFrames
                ? MapMode.ReadWrite
                : MapMode.Write;
            Grow(initialCapacity);
        }

        public override DeviceBuffer DeviceBuffer
        {
            get
            {
                Debug.Assert(_deviceBuffer != null);
                if (_bufferLocked)
                {
                    ThrowBufferLocked();
                }

                return _deviceBuffer;
            }
        }

        public uint Count => (uint)_cursor;

        public ref TVertex this[uint index] => ref Get(index);

        public ref TVertex InsertAt(uint index)
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

        private ref TVertex Get(uint index)
        {
            static void notMapped() =>
                throw new InvalidOperationException(
                    "Cannot access the contents of the buffer as it is not currently mapped.");

            static void outOfBounds() => throw new IndexOutOfRangeException();

            if (!_bufferLocked) { notMapped(); }
            if (index >= _cursor) { outOfBounds(); }

            var ptr = (TVertex*)Unsafe.Add<TVertex>((void*)_map.Data, (int)index);
            return ref Unsafe.AsRef<TVertex>(ptr);
        }

        public void Begin(bool resetPosition = true)
        {
            _bufferLocked = true;
            _map = _gd.Map(_stagingBuffer, _mapMode);
            if (resetPosition)
            {
                _cursor = 0;
            }
        }

        public ushort Append(in TVertex vertex)
        {
            Debug.Assert(_bufferLocked);
            EnsureCapacity(_cursor + 1);
            var ptr = (TVertex*)Unsafe.Add<TVertex>((void*)_map.Data, _cursor);
            *ptr = vertex;
            return (ushort)_cursor++;
        }

        public Span<TVertex> Append(uint count, out uint position)
        {
            Debug.Assert(_bufferLocked);
            EnsureCapacity(_cursor + count);
            position = (uint)_cursor;
            var span = new Span<TVertex>((void*)_map.Data, (int)_capacity)
                .Slice(_cursor, (int)count);
            _cursor += (int)count;
            return span;
        }

        public Span<TVertex> Append(uint count)
        {
            Debug.Assert(_bufferLocked);
            EnsureCapacity(_cursor + count);
            var dst = new Span<TVertex>((void*)_map.Data, (int)_capacity);
            int cursor = _cursor;
            _cursor += (int)count;
            return dst.Slice(cursor, (int)count);
        }

        public ushort Append(ReadOnlySpan<TVertex> vertices)
        {
            Debug.Assert(_bufferLocked);
            EnsureCapacity(_cursor + vertices.Length);
            int oldPosition = _cursor;
            var dst = new Span<TVertex>((void*)_map.Data, (int)_capacity);
            vertices.CopyTo(dst.Slice(_cursor, vertices.Length));
            _cursor += vertices.Length;
            return (ushort)oldPosition;
        }

        public void End(CommandList commandList)
        {
            _gd.Unmap(_stagingBuffer);
            _map = default;
            _bufferLocked = false;
            uint totalVertices = (uint)_cursor;
            if (totalVertices > 0)
            {
                uint size = _vertexSize * totalVertices;
                commandList.CopyBuffer(_stagingBuffer, 0, _deviceBuffer, 0, size);
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
                new BufferDescription(size, BufferUsage.VertexBuffer)
            );

            if (_deviceBuffer != null)
            {
                MappedResource newMap = _gd.Map(newStagingBuffer, _mapMode);
                var src = new Span<TVertex>((void*)_map.Data, (int)_capacity);
                var dst = new Span<TVertex>((void*)newMap.Data, (int)newCapacity);
                src.CopyTo(dst);
                _gd.Unmap(_map.Resource);
                _gd.DisposeWhenIdle(_deviceBuffer);
                _gd.DisposeWhenIdle(_stagingBuffer);
                _map = newMap;
            }

            _capacity = (uint)newCapacity;
            _stagingBuffer = newStagingBuffer;
            _deviceBuffer = newDeviceBuffer;
        }

        public override void Dispose()
        {
            base.Dispose();
            _stagingBuffer!.Dispose();
        }

        private void ThrowBufferLocked()
            => throw new InvalidOperationException($"{nameof(End)}() must be called before accessing the underlying device buffer.");
    }
}
