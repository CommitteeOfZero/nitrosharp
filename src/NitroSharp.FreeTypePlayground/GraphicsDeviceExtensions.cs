using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Veldrid;

namespace NitroSharp.Utilities
{
    internal static class GraphicsDeviceExtensions
    {
        public static DeviceBuffer CreateStaticBuffer<T>(
            this GraphicsDevice graphicsDevice,
            T[] data,
            BufferUsage usage,
            uint structureByteStride = 0u)
            where T : unmanaged
        {
            uint bufferSize = (uint)MathUtil.RoundUp(data.Length * Marshal.SizeOf<T>(), 16);
            DeviceBuffer buffer = graphicsDevice.ResourceFactory.CreateBuffer(
                new BufferDescription(bufferSize, usage, structureByteStride));

            graphicsDevice.UpdateBuffer(buffer, 0, data);
            return buffer;
        }

        public static DeviceBuffer CreateStaticBuffer<T>(
            this GraphicsDevice graphicsDevice,
            ref T value,
            BufferUsage usage,
            uint structureByteStride = 0u)
            where T : unmanaged
        {
            uint bufferSize = (uint)MathUtil.RoundUp(Marshal.SizeOf<T>(), 16);
            DeviceBuffer buffer = graphicsDevice.ResourceFactory.CreateBuffer(
                new BufferDescription(bufferSize, usage, structureByteStride));

            graphicsDevice.UpdateBuffer(buffer, 0, ref value);
            return buffer;
        }

        public static void InitStagingTexture(this GraphicsDevice graphicsDevice, Texture texture)
        {
            if ((texture.Usage & TextureUsage.Staging) != TextureUsage.Staging)
            {
                throw new ArgumentException("Expected a staging texture.");
            }

            var map = graphicsDevice.Map(texture, MapMode.Write);
            unsafe
            {
                Unsafe.InitBlock(map.Data.ToPointer(), 0x00, map.SizeInBytes);
            }
            graphicsDevice.Unmap(texture);
        }
    }
}
