using Veldrid;

namespace NitroSharp.Graphics
{
    internal sealed class SharedResources
    {
        public SharedResources(
            ResourceLayout layout,
            DeviceBuffer viewProjectionBuffer,
            ResourceSet orthographicProjection)
        {
            Layout = layout;
            ViewProjectionBuffer = viewProjectionBuffer;
            OrthographicProjection = orthographicProjection;
        }

        public ResourceLayout Layout { get; }
        public DeviceBuffer ViewProjectionBuffer { get; }
        public ResourceSet OrthographicProjection { get; }
    }
}
