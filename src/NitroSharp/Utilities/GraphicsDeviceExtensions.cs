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
            where T : struct
        {
            uint bufferSize = (uint)(data.Length * Marshal.SizeOf<T>());

            var result = graphicsDevice.ResourceFactory.CreateBuffer(
                new BufferDescription(bufferSize, usage, structureByteStride));

            graphicsDevice.UpdateBuffer(result, 0, data);
            return result;
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
