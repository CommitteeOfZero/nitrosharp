using System;
using System.Numerics;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using Veldrid;

namespace NitroSharp.Graphics;

[UsedImplicitly(ImplicitUseTargetFlags.Members)]
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

[UsedImplicitly(ImplicitUseTargetFlags.Members)]
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

internal struct QuadPrimitive
{
    public const int VertexCount = 4;

    public static ushort[] IndexPattern => [0, 1, 2, 2, 1, 3];

    public QuadVertex TopLeft;
    public QuadVertex TopRight;
    public QuadVertex BottomLeft;
    public QuadVertex BottomRight;

    public static QuadPrimitive Create(
        DesignSize size,
        in Matrix4x4 transform,
        Vector2 uvTopLeft,
        Vector2 uvBottomRight,
        in Vector4 color)
    {
        QuadPrimitive quad = default;

        ref QuadVertex topLeft = ref quad.TopLeft;
        topLeft.Position.X = 0.0f;
        topLeft.Position.Y = 0.0f;
        topLeft.TexCoord.X = uvTopLeft.X;
        topLeft.TexCoord.Y = uvTopLeft.Y;
        topLeft.Position = Vector2.Transform(topLeft.Position, transform);
        topLeft.Color = color;

        ref QuadVertex topRight = ref quad.TopRight;
        topRight.Position.X = size.Width;
        topRight.Position.Y = 0.0f;
        topRight.TexCoord.X = uvBottomRight.X;
        topRight.TexCoord.Y = uvTopLeft.Y;
        topRight.Position = Vector2.Transform(topRight.Position, transform);
        topRight.Color = color;

        ref QuadVertex bottomLeft = ref quad.BottomLeft;
        bottomLeft.Position.X = 0.0f;
        bottomLeft.Position.Y = 0.0f + size.Height;
        bottomLeft.TexCoord.X = uvTopLeft.X;
        bottomLeft.TexCoord.Y = uvBottomRight.Y;
        bottomLeft.Position = Vector2.Transform(bottomLeft.Position, transform);
        bottomLeft.Color = color;

        ref QuadVertex bottomRight = ref quad.BottomRight;
        bottomRight.Position.X = size.Width;
        bottomRight.Position.Y = size.Height;
        bottomRight.TexCoord.X = uvBottomRight.X;
        bottomRight.TexCoord.Y = uvBottomRight.Y;
        bottomRight.Position = Vector2.Transform(bottomRight.Position, transform);
        bottomRight.Color = color;

        return quad;
    }

    public readonly ReadOnlySpan<QuadVertex> AsSpan()
        => MemoryMarshal.CreateReadOnlySpan(in TopLeft, 4);
}

[UsedImplicitly(ImplicitUseTargetFlags.Members)]
internal struct QuadPrimitiveUV3
{
    public const uint VertexCount = 4;

    public static ushort[] IndexPattern => [0, 1, 2, 2, 1, 3];

    public QuadVertexUV3 TopLeft;
    public QuadVertexUV3 TopRight;
    public QuadVertexUV3 BottomLeft;
    public QuadVertexUV3 BottomRight;

    public static QuadPrimitiveUV3 FromQuad(in QuadPrimitive quad, uint layer)
    {
        return new QuadPrimitiveUV3
        {
            TopLeft = vertex(quad.TopLeft, layer),
            TopRight = vertex(quad.TopRight, layer),
            BottomLeft = vertex(quad.BottomLeft, layer),
            BottomRight = vertex(quad.BottomRight, layer)
        };

        static QuadVertexUV3 vertex(in QuadVertex v, uint layer) => new()
        {
            Position = v.Position,
            TexCoord = new Vector3(v.TexCoord, layer),
        };
    }

    public readonly ReadOnlySpan<QuadVertexUV3> AsSpan()
        => MemoryMarshal.CreateReadOnlySpan(in TopLeft, 4);
}
