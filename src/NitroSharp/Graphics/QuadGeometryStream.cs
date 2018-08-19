using System;
using System.Numerics;
using NitroSharp.Primitives;
using NitroSharp.Utilities;
using Veldrid;

namespace NitroSharp.Graphics
{
    internal sealed class QuadGeometryStream : IDisposable
    {
        public QuadGeometryStream(GraphicsDevice graphicsDevice, uint initialCapacity = 512)
        {
            var indexArray = new ushort[] { 0, 1, 2, 2, 1, 3 };
            VertexBuffer = new CircularVertexBuffer<QuadVertex>(graphicsDevice, initialCapacity * 4);
            InstanceDataBuffer = new CircularVertexBuffer<QuadInstanceData>(graphicsDevice, initialCapacity);
            IndexBuffer = graphicsDevice.CreateStaticBuffer(indexArray, BufferUsage.IndexBuffer);
        }

        public CircularVertexBuffer<QuadVertex> VertexBuffer { get; }
        public CircularVertexBuffer<QuadInstanceData> InstanceDataBuffer { get; }
        public DeviceBuffer IndexBuffer { get; }

        public void Begin()
        {
            VertexBuffer.Begin();
            InstanceDataBuffer.Begin();
        }

        public (ushort vertexBase, ushort instanceBase) Append(
            in RectangleF rectangle,
            in Vector2 uvTopLeft,
            in Vector2 uvBottomRight,
            in Matrix4x4 transform,
            ref RgbaFloat color)
        {
            Span<QuadVertex> vertices = stackalloc QuadVertex[4];
            int offset = 0;

            ref var VertexTL = ref vertices[offset];
            VertexTL.Position.X = rectangle.X;
            VertexTL.Position.Y = rectangle.Y;
            VertexTL.TexCoord.X = uvTopLeft.X;
            VertexTL.TexCoord.Y = uvTopLeft.Y;

            VertexTL.Position = Vector2.Transform(VertexTL.Position, transform);

            ref var VertexTR = ref vertices[++offset];
            VertexTR.Position.X = rectangle.X + rectangle.Width;
            VertexTR.Position.Y = rectangle.Y;
            VertexTR.TexCoord.X = uvBottomRight.X;
            VertexTR.TexCoord.Y = uvTopLeft.Y;

            VertexTR.Position = Vector2.Transform(VertexTR.Position, transform);

            ref var VertexBL = ref vertices[++offset];
            VertexBL.Position.X = rectangle.X;
            VertexBL.Position.Y = rectangle.Y + rectangle.Height;
            VertexBL.TexCoord.X = uvTopLeft.X;
            VertexBL.TexCoord.Y = uvBottomRight.Y;

            VertexBL.Position = Vector2.Transform(VertexBL.Position, transform);

            ref var VertexBR = ref vertices[++offset];
            VertexBR.Position.X = rectangle.X + rectangle.Width;
            VertexBR.Position.Y = rectangle.Y + rectangle.Height;
            VertexBR.TexCoord.X = uvBottomRight.X;
            VertexBR.TexCoord.Y = uvBottomRight.Y;

            VertexBR.Position = Vector2.Transform(VertexBR.Position, transform);

            ushort vertexBase = VertexBuffer.Append(vertices);
            ushort instanceBase = InstanceDataBuffer.Append(new QuadInstanceData { Color = color });
            return (vertexBase, instanceBase);
        }

        public void End(CommandList commandList)
        {
            VertexBuffer.End(commandList);
            InstanceDataBuffer.End(commandList);
        }

        public void Dispose()
        {
            VertexBuffer.Dispose();
            IndexBuffer.Dispose();
            InstanceDataBuffer.Dispose();
        }
    }

    internal struct QuadVertex : IEquatable<QuadVertex>
    {
        public Vector2 Position;
        public Vector2 TexCoord;

        public static readonly VertexLayoutDescription LayoutDescription = new VertexLayoutDescription(
            new VertexElementDescription("Position", VertexElementSemantic.Position, VertexElementFormat.Float2),
            new VertexElementDescription("TexCoord", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2));

        public bool Equals(QuadVertex other)
        {
            return Position == other.Position
                && TexCoord == other.TexCoord;
        }
    }

    internal struct QuadInstanceData : IEquatable<QuadInstanceData>
    {
        public RgbaFloat Color;

        public static VertexLayoutDescription LayoutDescription => new VertexLayoutDescription(
            stride: 16, instanceStepRate: 1,
            new VertexElementDescription("Color", VertexElementSemantic.Color, VertexElementFormat.Float4));

        public bool Equals(QuadInstanceData other)
        {
            return Color.Equals(other.Color);
        }
    }
}
