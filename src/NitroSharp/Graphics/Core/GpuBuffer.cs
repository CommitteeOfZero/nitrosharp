using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using NitroSharp.Utilities;
using Veldrid;

namespace NitroSharp.Graphics.Core
{
    internal sealed class GpuBuffer<T> : IDisposable
        where T : unmanaged
    {
        private readonly uint _capacity;

        public static GpuBuffer<T> CreateIndex(GraphicsDevice gd, Span<T> data)
            => new(gd, BufferUsage.IndexBuffer, data);

        public static GpuBuffer<T> CreateUniform(GraphicsDevice gd, in T data)
            => new(gd, BufferUsage.UniformBuffer, data);

        public GpuBuffer(
            GraphicsDevice graphicsDevice,
            BufferUsage usage,
            T data)
            : this(graphicsDevice, usage, MemoryMarshal.CreateSpan(ref data, 1))
        {
        }

        private GpuBuffer(
            GraphicsDevice graphicsDevice,
            BufferUsage usage,
            Span<T> data)
        {
            ResourceFactory factory = graphicsDevice.ResourceFactory;
            uint bufSize = (uint)Unsafe.SizeOf<T>() * (uint)data.Length;
            bufSize = MathUtil.RoundUp(bufSize, 16);
            _capacity = (uint)data.Length;
            VdBuffer = factory.CreateBuffer(new BufferDescription(bufSize, usage));
            Update(graphicsDevice, data);
        }

        public DeviceBuffer VdBuffer { get; }

        public void Update(GraphicsDevice gd, T data)
            => Update(gd, MemoryMarshal.CreateSpan(ref data, 1));

        private void Update(GraphicsDevice graphicsDevice, Span<T> data)
        {
            Debug.Assert(data.Length <= _capacity);
            graphicsDevice.UpdateBuffer(
                VdBuffer, 0, ref data[0],
                (uint)(data.Length * Unsafe.SizeOf<T>())
            );
        }

        public void Update(CommandList commandList, T data)
            => Update(commandList, MemoryMarshal.CreateSpan(ref data, 1));

        private void Update(CommandList commandList, Span<T> data)
        {
            Debug.Assert(data.Length <= _capacity);
            commandList.UpdateBuffer(
                VdBuffer, 0, ref data[0],
                (uint)(data.Length * Unsafe.SizeOf<T>())
            );
        }

        public void Dispose()
        {
            VdBuffer.Dispose();
        }
    }
}
