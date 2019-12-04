using Veldrid;

#nullable enable

namespace NitroSharp.Graphics
{
    internal sealed class Bucket
    {
        private struct Quad
        {
            public ushort IndexBase;
            public ResourceSet ResourceSet1;
            public ResourceSet? ResourceSet2;

        }

        private VertexBuffer<QuadVertex> _quadVertices;
        private DeviceBuffer _quadIB;

        public Bucket()
        {

        }
    }
}
