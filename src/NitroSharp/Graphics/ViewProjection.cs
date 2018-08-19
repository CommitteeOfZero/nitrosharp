using Veldrid;

namespace NitroSharp.Graphics
{
    internal sealed class ViewProjection
    {
        public ViewProjection(ResourceLayout layout, ResourceSet set, DeviceBuffer buffer)
        {
            ResourceLayout = layout;
            ResourceSet = set;
            DeviceBuffer = buffer;
        }

        public ResourceLayout ResourceLayout { get; }
        public ResourceSet ResourceSet { get; }
        public DeviceBuffer DeviceBuffer { get; }
    }
}
