using System.Runtime.InteropServices;
using Veldrid;

#nullable enable

namespace NitroSharp.Graphics
{
    internal sealed class Bucket
    {
        private enum RenderItemKind
        {
            Quad,
            Text
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct RenderItem
        {
            [FieldOffset(0)]
            public Pipeline Pipeline;
            [FieldOffset(8)]
            public ResourceSet ResourceSet1;
            [FieldOffset(16)]
            public ResourceSet? ResourceSet2;
            [FieldOffset(24)]
            public UniformUpdate UniformUpdate;

            [FieldOffset(40)]
            public RenderItemKind Kind;
        }

        private struct Quad
        {
            public ushort IndexBase;
        }

        private VertexBuffer<QuadVertex> _quadVertices;
        private DeviceBuffer _quadIB;

        public Bucket()
        {

        }
    }
}
