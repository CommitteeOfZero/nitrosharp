using System;
using System.Numerics;
using NitroSharp.Primitives;
using Veldrid;

#nullable enable

namespace NitroSharp.Graphics
{
    internal struct Quad
    {
        public QuadVertex TL;
        public QuadVertex TR;
        public QuadVertex BL;
        public QuadVertex BR;
    }

    internal struct QuadVertex
    {
        public Vector2 Position;
        public Vector2 TexCoord;
        public Vector4 Color;

        public static readonly VertexLayoutDescription LayoutDescription = new VertexLayoutDescription(
            new VertexElementDescription(
                "vs_Position",
                VertexElementSemantic.TextureCoordinate,
                VertexElementFormat.Float2
            ),
            new VertexElementDescription(
                "vs_TexCoord",
                VertexElementSemantic.TextureCoordinate,
                VertexElementFormat.Float2
            ),
            new VertexElementDescription(
                "vs_Color",
                VertexElementSemantic.TextureCoordinate,
                VertexElementFormat.Float4
            )
        );
    }

    internal readonly struct QuadBuffers
    {
        public QuadBuffers(GraphicsDevice gd)
        {
            ResourceFactory rf = gd.ResourceFactory;
            VertexBuffer = rf.CreateBuffer(new BufferDescription(
                sizeInBytes: 128,
                BufferUsage.VertexBuffer | BufferUsage.Dynamic
            ));
            IndexBuffer = rf.CreateBuffer(new BufferDescription(
                sizeInBytes: 6 * sizeof(ushort),
                BufferUsage.IndexBuffer
            ));
            Span<ushort> indices = stackalloc ushort[] { 0, 1, 2, 2, 1, 3 };
            gd.UpdateBuffer(IndexBuffer, 0, ref indices[0]);
        }

        public DeviceBuffer VertexBuffer { get; }
        public DeviceBuffer IndexBuffer { get; }

        public void UpdateVertices(CommandList commandList, ref Quad quad)
        {
            commandList.UpdateBuffer(VertexBuffer, 0, ref quad);
        }
    }

    internal sealed class QuadRenderer
    {
        public static void DrawQuad(
            in QuadBuffers gpuBuffers,
            CommandList commandList,
            ref Quad quad,
            Pipeline pipeline,
            ResourceSet resourceSet)
        {
            gpuBuffers.UpdateVertices(commandList, ref quad);
            commandList.SetPipeline(pipeline);
            commandList.SetGraphicsResourceSet(1, resourceSet);
            commandList.SetVertexBuffer(0, gpuBuffers.VertexBuffer);
            commandList.SetIndexBuffer(gpuBuffers.IndexBuffer, IndexFormat.UInt16);
            commandList.DrawIndexed(
                indexCount: 6,
                instanceCount: 1,
                indexStart: 0,
                vertexOffset: 0,
                instanceStart: 0
            );
        }

        public static void CalcVertices(
            ref Quad quad,
            SizeF localBounds,
            in Matrix4x4 transform,
            Vector2 uvTopLeft,
            Vector2 uvBottomRight,
            Vector4 color)
        {
            ref QuadVertex topLeft = ref quad.TL;
            topLeft.Position.X = 0.0f;
            topLeft.Position.Y = 0.0f;
            topLeft.TexCoord.X = uvTopLeft.X;
            topLeft.TexCoord.Y = uvTopLeft.Y;
            topLeft.Position = Vector2.Transform(topLeft.Position, transform);
            topLeft.Color = color;

            ref QuadVertex topRight = ref quad.TR;
            topRight.Position.X = localBounds.Width;
            topRight.Position.Y = 0.0f;
            topRight.TexCoord.X = uvBottomRight.X;
            topRight.TexCoord.Y = uvTopLeft.Y;
            topRight.Position = Vector2.Transform(topRight.Position, transform);
            topRight.Color = color;

            ref QuadVertex bottomLeft = ref quad.BL;
            bottomLeft.Position.X = 0.0f;
            bottomLeft.Position.Y = 0.0f + localBounds.Height;
            bottomLeft.TexCoord.X = uvTopLeft.X;
            bottomLeft.TexCoord.Y = uvBottomRight.Y;
            bottomLeft.Position = Vector2.Transform(bottomLeft.Position, transform);
            bottomLeft.Color = color;

            ref QuadVertex bottomRight = ref quad.BR;
            bottomRight.Position.X = localBounds.Width;
            bottomRight.Position.Y = localBounds.Height;
            bottomRight.TexCoord.X = uvBottomRight.X;
            bottomRight.TexCoord.Y = uvBottomRight.Y;
            bottomRight.Position = Vector2.Transform(bottomRight.Position, transform);
            bottomRight.Color = color;
        }
    }
}
