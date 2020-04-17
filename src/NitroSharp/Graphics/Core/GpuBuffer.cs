using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using NitroSharp.Utilities;
using Veldrid;

#nullable enable

namespace NitroSharp.Graphics
{
    //internal abstract class GpuBuffer : IDisposable
    //{
    //    protected DeviceBuffer? _buffer;
//
    //    public virtual DeviceBuffer VdBuffer
    //    {
    //        get => _buffer!;
    //        protected set => _buffer = value;
    //    }
//
    //    public virtual void Dispose()
    //    {
    //        _buffer!.Dispose();
    //    }
    //}

    internal sealed class GpuBuffer<T> : IDisposable
        where T : unmanaged
    {
        public static GpuBuffer<T> CreateIndex(GraphicsDevice gd, Span<T> data)
            => new GpuBuffer<T>(gd, BufferUsage.IndexBuffer, data);

        public static GpuBuffer<T> CreateUniform(GraphicsDevice gd, in T data)
            => new GpuBuffer<T>(gd, BufferUsage.UniformBuffer, data);

        public GpuBuffer(
            GraphicsDevice graphicsDevice,
            BufferUsage usage,
            T data)
            : this(graphicsDevice, usage, MemoryMarshal.CreateSpan(ref data, 1))
        {
        }

        public GpuBuffer(
            GraphicsDevice graphicsDevice,
            BufferUsage usage,
            Span<T> data)
        {
            ResourceFactory factory = graphicsDevice.ResourceFactory;
            uint bufSize = (uint)Unsafe.SizeOf<T>() * (uint)data.Length;
            bufSize = MathUtil.RoundUp(bufSize, 16);
            Capacity = (uint)data.Length;
            VdBuffer = factory.CreateBuffer(new BufferDescription(bufSize, usage));
            Update(graphicsDevice, data);
        }

        public DeviceBuffer VdBuffer { get; }

        public uint Capacity { get; }

        public void Update(GraphicsDevice gd, T data)
            => Update(gd, MemoryMarshal.CreateSpan(ref data, 1));

        public void Update(GraphicsDevice graphicsDevice, Span<T> data)
        {
            Debug.Assert(data.Length <= Capacity);
            graphicsDevice.UpdateBuffer(
                VdBuffer, 0, ref data[0],
                (uint)(data.Length * Unsafe.SizeOf<T>())
            );
        }

        public void Update(CommandList commandList, T data)
            => Update(commandList, MemoryMarshal.CreateSpan(ref data, 1));

        public void Update(CommandList commandList, Span<T> data)
        {
            Debug.Assert(data.Length <= Capacity);
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
