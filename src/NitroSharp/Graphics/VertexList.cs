using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using NitroSharp.Utilities;
using Veldrid;

#nullable enable

namespace NitroSharp.Graphics
{
    internal sealed unsafe class VertexList<TVertex> : VertexBuffer
        where TVertex : unmanaged, IEquatable<TVertex>
    {
        private readonly GraphicsDevice _gd;
        private readonly uint _vertexSize;

        private CommandList? _cl;
        private DeviceBuffer? _stagingBuffer;
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

        public void Begin(CommandList commandList)
        {
            _cl = commandList;
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

        public void End()
        {
            Debug.Assert(_cl != null);
            _gd.Unmap(_stagingBuffer);
            _map = default;
            _bufferLocked = false;
            uint totalVertices = (uint)_cursor;
            if (totalVertices > 0)
            {
                uint size = _vertexSize * totalVertices;
                _cl.CopyBuffer(_stagingBuffer, 0, _deviceBuffer, 0, size);
            }
            _cl = null;
        }

        private void Resize(uint newCapacity)
        {
            Debug.Assert(newCapacity > _capacity);
            uint size = MathUtil.RoundUp(newCapacity * _vertexSize, 16);
            DeviceBuffer newStagingBuffer = _gd.ResourceFactory.CreateBuffer(
                new BufferDescription(size, BufferUsage.Staging));
            DeviceBuffer newDeviceBuffer = _gd.ResourceFactory.CreateBuffer(
                new BufferDescription(size, BufferUsage.VertexBuffer));

            if (_deviceBuffer != null)
            {
                Debug.Assert(_cl != null);
                _cl.CopyBuffer(
                    source: _stagingBuffer,
                    sourceOffset: 0,
                    destination: newStagingBuffer,
                    destinationOffset: 0,
                    sizeInBytes: (uint)(_cursor * _vertexSize)
                );

                _gd.DisposeWhenIdle(_deviceBuffer);
                _gd.DisposeWhenIdle(_stagingBuffer);
            }

            _capacity = newCapacity;
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
