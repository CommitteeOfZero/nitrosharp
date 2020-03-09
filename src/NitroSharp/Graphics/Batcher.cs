using Veldrid;

#nullable enable

namespace NitroSharp.Graphics
{
    internal struct Draw
    {
        public VertexBuffer? VertexBuffer0;
        public VertexBuffer? VertexBuffer1;
        public DeviceBuffer? IndexBuffer;
        public Pipeline Pipeline;
        public ResourceSet SharedResourceSet;
        public ResourceSet ObjectResourceSet0;
        public ResourceSet? ObjectResourceSet1;
        public ushort VertexBase;
        public ushort VertexCount;
        public ushort IndexBase;
        public ushort IndexCount;
        public ushort InstanceBase;
        public ushort InstanceCount;
    }

    internal sealed class Batcher
    {
        private Draw _last;

        public Batcher()
        {
        }

        public void Batch(ref Draw draw)
        {

        }
    }
}
