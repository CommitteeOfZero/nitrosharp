using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using NitroSharp.Utilities;
using Veldrid;

namespace NitroSharp.Graphics
{
    internal sealed unsafe class VertexList<TVertex> : VertexBuffer
        where TVertex : unmanaged, IEquatable<TVertex>
    {
        private readonly GraphicsDevice _gd;
        private readonly uint _vertexSize;

        private DeviceBuffer _stagingBuffer;
        private MappedResource _map;
        private uint _capacity;
        private int _cursor;
        private bool _bufferLocked;

        public VertexList(GraphicsDevice graphicsDevice, uint initialCapacity)
        {
            _gd = graphicsDevice;
            _vertexSize = (uint)Unsafe.SizeOf<TVertex>();
            Resize(initialCapacity);
        }

        public override DeviceBuffer DeviceBuffer
        {
            get
            {
                if (_bufferLocked)
                {
                    ThrowBufferLocked();
                }

                return _deviceBuffer;
            }
        }

        public void Begin()
        {
            _cursor = 0;
            _bufferLocked = true;
            _map = _gd.Map(_stagingBuffer, MapMode.Write);
        }

        public ushort Append(in TVertex vertex)
        {
            Debug.Assert(_bufferLocked);
            if (_cursor == _capacity)
            {
                Resize(_capacity * 2);
            }

            var ptr = (TVertex*)Unsafe.Add<TVertex>((void*)_map.Data, _cursor);
            *ptr = vertex;
            return (ushort)_cursor++;
        }

        public ushort Append(ReadOnlySpan<TVertex> vertices)
        {
            Debug.Assert(_bufferLocked);
            if ((_cursor + vertices.Length) >= _capacity)
            {
                Resize((uint)(_cursor + vertices.Length) * 2);
            }

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

        private void Resize(uint newCapacity)
        {
            if (_deviceBuffer != null)
            {
                _gd.DisposeWhenIdle(_deviceBuffer);
                _gd.DisposeWhenIdle(_stagingBuffer);
            }

            uint size = MathUtil.RoundUp(newCapacity * _vertexSize, 16);
            _deviceBuffer = _gd.ResourceFactory.CreateBuffer(
                new BufferDescription(size, BufferUsage.VertexBuffer));
            _stagingBuffer = _gd.ResourceFactory.CreateBuffer(
                new BufferDescription(size, BufferUsage.Staging));

            _capacity = newCapacity;
        }

        public override void Dispose()
        {
            base.Dispose();
            _stagingBuffer.Dispose();
        }

        private void ThrowBufferLocked()
            => throw new InvalidOperationException($"{nameof(End)}() must be called before accessing the underlying device buffer.");
    }
}
