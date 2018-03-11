using System.Runtime.InteropServices;
using Veldrid;

namespace NitroSharp.Graphics
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
            var staging = graphicsDevice.ResourceFactory.CreateBuffer(
                new BufferDescription(bufferSize, BufferUsage.Staging, 0));

            var result = graphicsDevice.ResourceFactory.CreateBuffer(
                new BufferDescription(bufferSize, usage, structureByteStride));

            var commandList = graphicsDevice.ResourceFactory.CreateCommandList();
            commandList.Begin();

            var map = graphicsDevice.Map<T>(staging, MapMode.Write, 0);
            for (int i = 0; i < data.Length; i++)
            {
                map[i] = data[i];
            }

            graphicsDevice.Unmap(staging, 0);
            commandList.CopyBuffer(staging, 0, result, 0, bufferSize);
            commandList.End();

            graphicsDevice.SubmitCommands(commandList);

            graphicsDevice.DisposeWhenIdle(commandList);
            graphicsDevice.DisposeWhenIdle(staging);

            return result;
        }
    }
}
