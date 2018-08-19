using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using NitroSharp.Utilities;
using Veldrid;

namespace NitroSharp.Graphics
{
    internal sealed class CircularVertexBuffer<TVertex> : VertexBuffer
        where TVertex : unmanaged, IEquatable<TVertex>
    {
        private readonly GraphicsDevice _gd;
        private readonly ushort _vertexSize;

        private DeviceBuffer _stagingBuffer;
        private TVertex[] _vertices;
        private List<ushort> _changes;
        private ushort _cursor;
        private bool _firstUpdate = true;
        private bool _bufferLocked;

        public CircularVertexBuffer(GraphicsDevice graphicsDevice, uint initialCapacity)
        {
            _gd = graphicsDevice;
            _vertexSize = (ushort)Unsafe.SizeOf<TVertex>();
            _changes = new List<ushort>((int)initialCapacity);
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
            _changes.Clear();
            _bufferLocked = true;
        }

        public ushort Append(in TVertex vertex)
        {
            if (_cursor >= _vertices.Length)
            {
                Resize((ushort)(_vertices.Length * 2));
            }

            if (!vertex.Equals(_vertices[_cursor]))
            {
                _vertices[_cursor] = vertex;
                _changes.Add(_cursor);
            }

            return _cursor++;
        }

        public ushort Append(ReadOnlySpan<TVertex> vertices)
        {
            if (_cursor >= _vertices.Length - 1)
            {
                Resize((ushort)(_vertices.Length * 2));
            }

            ushort oldPosition = _cursor;
            for (int i = 0; i < vertices.Length; i++)
            {
                if (!vertices[i].Equals(_vertices[_cursor]))
                {
                    _vertices[_cursor] = vertices[i];
                    _changes.Add(_cursor);
                }

                _cursor++;
            }

            return oldPosition;
        }

        public void End(CommandList commandList)
        {
            uint totalVertices = _cursor;
            if (totalVertices == 0) { return; }

            if (_firstUpdate || _changes.Count == totalVertices)
            {
                uint size = _vertexSize * totalVertices;
                MappedResource map = _gd.Map(_stagingBuffer, MapMode.Write);
                unsafe
                {
                    fixed (TVertex* ptr = &_vertices[0])
                    {
                        Buffer.MemoryCopy(ptr, map.Data.ToPointer(), size, size);
                    }
                }
                _gd.Unmap(_stagingBuffer);
                commandList.CopyBuffer(_stagingBuffer, 0, _deviceBuffer, 0, size);
                _firstUpdate = false;
            }
            else
            {
                MappedResource map = _gd.Map(_stagingBuffer, MapMode.Write);
                uint stagingOffset = 0;
                for (int i = 0; i < _changes.Count; i++)
                {
                    uint batchStartIdx = _changes[i];
                    uint batchSize = 1;
                    while (i < _changes.Count - 1 && _changes[i + 1] - _changes[i] == 1)
                    {
                        batchSize++;
                        i++;
                    }

                    uint batchSizeInBytes = batchSize * _vertexSize;
                    unsafe
                    {
                        byte* dst = (byte*)map.Data + stagingOffset;
                        fixed (TVertex* batchStartPtr = &_vertices[batchStartIdx])
                        {
                            if (batchSizeInBytes > 1024)
                            {
                                Buffer.MemoryCopy(batchStartPtr, dst, batchSizeInBytes, batchSizeInBytes);
                            }
                            else
                            {
                                Unsafe.CopyBlock(dst, batchStartPtr, batchSizeInBytes);
                            }
                        }
                    }

                    commandList.CopyBuffer(
                              source: _stagingBuffer,
                              sourceOffset: stagingOffset,
                              destination: _deviceBuffer,
                              destinationOffset: batchStartIdx * _vertexSize,
                              sizeInBytes: batchSizeInBytes);

                    stagingOffset += batchSizeInBytes;
                }
                _gd.Unmap(_stagingBuffer);
            }

            _bufferLocked = false;
        }

        private void Resize(uint vertexCount)
        {
            if (_deviceBuffer != null)
            {
                _gd.DisposeWhenIdle(_deviceBuffer);
                _gd.DisposeWhenIdle(_stagingBuffer);
            }

            uint size = MathUtil.RoundUp(vertexCount * _vertexSize, 16);
            _deviceBuffer = _gd.ResourceFactory.CreateBuffer(
                new BufferDescription(size, BufferUsage.VertexBuffer));
            _stagingBuffer = _gd.ResourceFactory.CreateBuffer(
                new BufferDescription(size, BufferUsage.Staging));

            Array.Resize(ref _vertices, (int)vertexCount);
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
