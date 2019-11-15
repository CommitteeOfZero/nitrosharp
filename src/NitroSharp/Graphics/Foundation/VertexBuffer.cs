using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Veldrid;

#nullable enable

namespace NitroSharp.Graphics
{
    internal abstract class VertexBuffer : IDisposable
    {
        protected DeviceBuffer? _deviceBuffer;

        public virtual DeviceBuffer DeviceBuffer
        {
            get => _deviceBuffer!;
            protected set => _deviceBuffer = value;
        }

        public virtual void Dispose()
        {
            _deviceBuffer!.Dispose();
        }
    }

    internal class VertexBuffer<TVertex> : VertexBuffer
        where TVertex : unmanaged
    {
        private readonly DeviceBuffer? _staging;

        public VertexBuffer(
            GraphicsDevice graphicsDevice,
            CommandList commandList,
            Span<TVertex> data,
            bool dynamic = false)
        {
            var flags = dynamic
                ? BufferUsage.VertexBuffer | BufferUsage.Dynamic
                : BufferUsage.VertexBuffer;

            ResourceFactory factory = graphicsDevice.ResourceFactory;
            uint size = (uint)Unsafe.SizeOf<TVertex>() * (uint)data.Length;
            _deviceBuffer = factory.CreateBuffer(new BufferDescription(size, flags));
            if (dynamic)
            {
                Update(graphicsDevice, commandList, data, _deviceBuffer);
            }
            else
            {
                _staging = factory.CreateBuffer(new BufferDescription(size, BufferUsage.Staging));
                Update(graphicsDevice, commandList, data, _staging);
            }
        }

        public void Update(GraphicsDevice graphicsDevice, CommandList commandList, Span<TVertex> data)
            => Update(graphicsDevice, commandList, data, _staging ?? _deviceBuffer!);

        private unsafe void Update(
            GraphicsDevice graphicsDevice,
            CommandList commandList,
            Span<TVertex> data,
            DeviceBuffer mappableBuffer)
        {
            MappedResource map = graphicsDevice.Map(mappableBuffer, MapMode.Write);
            var src = MemoryMarshal.Cast<TVertex, byte>(data);
            var dst = new Span<byte>(map.Data.ToPointer(), (int)map.SizeInBytes);
            src.CopyTo(dst);
            graphicsDevice.Unmap(mappableBuffer);
            if (mappableBuffer != _deviceBuffer)
            {
                commandList.CopyBuffer(
                    source: mappableBuffer,
                    sourceOffset: 0,
                    destination: _deviceBuffer,
                    destinationOffset: 0,
                    map.SizeInBytes
                );
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            _staging?.Dispose();
        }
    }
}
