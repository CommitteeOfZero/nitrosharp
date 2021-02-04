using System;
using System.Numerics;
using Veldrid;

namespace NitroSharp.Graphics
{
    internal struct QuadVertex
    {
        public Vector2 Position;
        public Vector2 TexCoord;
        public Vector4 Color;

        public static readonly VertexLayoutDescription LayoutDescription = new(
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

    internal struct QuadVertexUV3
    {
        public Vector2 Position;
        public Vector3 TexCoord;
        private Vector3 _padding;

        public static readonly VertexLayoutDescription LayoutDescription = new(
            stride: 32,
            new VertexElementDescription(
                "vs_Position",
                VertexElementSemantic.TextureCoordinate,
                VertexElementFormat.Float2
            ),
            new VertexElementDescription(
                "vs_TexCoord",
                VertexElementSemantic.TextureCoordinate,
                VertexElementFormat.Float3
            ),
            new VertexElementDescription(
                "vs_Padding",
                VertexElementSemantic.TextureCoordinate,
                VertexElementFormat.Float3
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
            in RectangleF? constraintRect = null)
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

            RectangleF boundingRect = quad.GetBoundingRect();
            if (constraintRect is RectangleF constraint)
            {
                quad.Constrain(ref boundingRect, constraint);
            }
            return (quad, boundingRect);
        }

        private RectangleF GetBoundingRect()
        {
            float left = MathF.Min(TopLeft.Position.X, BottomLeft.Position.X);
            float top = MathF.Min(TopLeft.Position.Y, TopRight.Position.Y);
            float right = MathF.Max(TopRight.Position.X, BottomRight.Position.X);
            float bottom = MathF.Max(BottomLeft.Position.Y, BottomRight.Position.Y);
            return RectangleF.FromLTRB(left, top, right, bottom);
        }

        private void Constrain(ref RectangleF boundingRect, in RectangleF constraintRect)
        {
            static void clamp(ref QuadVertex vert, Vector2 bounds, in RectangleF constraint)
            {
                Vector2 oldPos = vert.Position;
                vert.Position = Vector2.Clamp(
                    vert.Position,
                    min: constraint.TopLeft,
                    max: constraint.BottomRight
                );
                vert.TexCoord += (vert.Position - oldPos) / bounds;
            }

            var bounds = boundingRect.Size.ToVector2();
            clamp(ref TopLeft, bounds, constraintRect);
            clamp(ref TopRight, bounds, constraintRect);
            clamp(ref BottomLeft, bounds, constraintRect);
            clamp(ref BottomRight, bounds, constraintRect);
            boundingRect = GetBoundingRect();
        }
    }

    internal struct QuadGeometryUV3
    {
        public QuadVertexUV3 TopLeft;
        public QuadVertexUV3 TopRight;
        public QuadVertexUV3 BottomLeft;
        public QuadVertexUV3 BottomRight;

        public static QuadGeometryUV3 FromQuad(in QuadGeometry quad, uint layer)
        {
            static QuadVertexUV3 vertex(in QuadVertex v, uint layer)
            {
                return new()
                {
                    Position = v.Position,
                    TexCoord = new Vector3(v.TexCoord, layer),
                };
            }

            return new QuadGeometryUV3
            {
                TopLeft = vertex(quad.TopLeft, layer),
                TopRight = vertex(quad.TopRight, layer),
                BottomLeft = vertex(quad.BottomLeft, layer),
                BottomRight = vertex(quad.BottomRight, layer)
            };
        }
    }
}
