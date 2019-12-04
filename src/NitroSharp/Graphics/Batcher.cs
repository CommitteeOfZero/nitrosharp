using Veldrid;

#nullable enable

namespace NitroSharp.Graphics
{
    internal sealed class Batcher
    {
        private struct QuadBatch
        {
            public Texture Texture1;
            public Texture? Texture2;
            public ushort VertexBase;
            public BlendMode BlendMode;
            public byte BatchSize;
        }

        private VertexBuffer<QuadVertex> _quadVertices;
        private DeviceBuffer _quadIB;

        public Batcher()
        {

        }
    }
}
