using System;
using System.Numerics;
using Veldrid;

#nullable enable

namespace NitroSharp.Graphics
{
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

    internal struct QuadGeometry
    {
        public static ushort[] Indices => new ushort[] { 0, 1, 2, 2, 1, 3 };

        public QuadVertex TopLeft;
        public QuadVertex TopRight;
        public QuadVertex BottomLeft;
        public QuadVertex BottomRight;

        public static (QuadGeometry, RectangleF) Create(
            SizeF localBounds,
            in Matrix4x4 transform,
            Vector2 uvTopLeft,
            Vector2 uvBottomRight,
            in Vector4 color,
            in RectangleF? boxConstraint = null)
        {
            QuadGeometry quad = default;
            ref QuadVertex topLeft = ref quad.TopLeft;
            topLeft.Position.X = 0.0f;
            topLeft.Position.Y = 0.0f;
            topLeft.TexCoord.X = uvTopLeft.X;
            topLeft.TexCoord.Y = uvTopLeft.Y;
            topLeft.Position = Vector2.Transform(topLeft.Position, transform);
            topLeft.Color = color;

            ref QuadVertex topRight = ref quad.TopRight;
            topRight.Position.X = localBounds.Width;
            topRight.Position.Y = 0.0f;
            topRight.TexCoord.X = uvBottomRight.X;
            topRight.TexCoord.Y = uvTopLeft.Y;
            topRight.Position = Vector2.Transform(topRight.Position, transform);
            topRight.Color = color;

            ref QuadVertex bottomLeft = ref quad.BottomLeft;
            bottomLeft.Position.X = 0.0f;
            bottomLeft.Position.Y = 0.0f + localBounds.Height;
            bottomLeft.TexCoord.X = uvTopLeft.X;
            bottomLeft.TexCoord.Y = uvBottomRight.Y;
            bottomLeft.Position = Vector2.Transform(bottomLeft.Position, transform);
            bottomLeft.Color = color;

            ref QuadVertex bottomRight = ref quad.BottomRight;
            bottomRight.Position.X = localBounds.Width;
            bottomRight.Position.Y = localBounds.Height;
            bottomRight.TexCoord.X = uvBottomRight.X;
            bottomRight.TexCoord.Y = uvBottomRight.Y;
            bottomRight.Position = Vector2.Transform(bottomRight.Position, transform);
            bottomRight.Color = color;

            if (boxConstraint is RectangleF constraint)
            {
                static void clamp(ref Vector2 vec, in RectangleF constraint)
                {
                    vec = Vector2.Clamp(
                        vec,
                        min: constraint.TopLeft,
                        max: constraint.BottomRight
                    );
                }

                clamp(ref quad.TopLeft.Position, constraint);
                clamp(ref quad.TopRight.Position, constraint);
                clamp(ref quad.BottomLeft.Position, constraint);
                clamp(ref quad.BottomRight.Position, constraint);
            }

            float left = MathF.Min(topLeft.Position.X, bottomLeft.Position.X);
            float top = MathF.Min(topLeft.Position.Y, topRight.Position.Y);
            float right = MathF.Max(topRight.Position.X, bottomRight.Position.Y);
            float bottom = MathF.Max(bottomLeft.Position.Y, bottomRight.Position.Y);
            var layoutRect = RectangleF.FromLTRB(left, top, right, bottom);
            return (quad, layoutRect);
        }
    }
}
