using Veldrid;

namespace NitroSharp.Graphics
{
    internal sealed class RenderQueue
    {
        private struct RenderItem
        {
            
        }

        private struct QuadBatch
        {

            public BlendMode BlendMode;

        }

        private VertexBuffer<QuadVertex> _quadVertices;
        private DeviceBuffer _quadIB;

        public RenderQueue()
        {

        }
    }
}
