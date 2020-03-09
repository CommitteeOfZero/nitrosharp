using System;
using System.Numerics;
using System.Runtime.InteropServices;
using NitroSharp.Content;
using NitroSharp.Primitives;
using NitroSharp.Utilities;
using Veldrid;

namespace NitroSharp.Graphics
{
    internal enum BlendMode : byte
    {
        Alpha,
        Additive,
        ReverseSubtractive,
        Multiplicative
    }

    internal enum MaterialKind : byte
    {
        SolidColor,
        Texture,
        Screenshot,
        Lens
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct Material
    {
        [FieldOffset(0)]
        public TextureMaterial TextureVariant;

        [FieldOffset(0)]
        public AssetId LensTextureHandle;

        [FieldOffset(8)]
        public RgbaFloat Color;

        [FieldOffset(24)]
        public Vector2 UvTopLeft;

        [FieldOffset(32)]
        public Vector2 UvBottomRight;

        [FieldOffset(40)]
        public TransitionParameters TransitionParameters;

        [FieldOffset(56)]
        public SmallList<EffectDescription> Effects;

        [FieldOffset(72)]
        public readonly MaterialKind Kind;

        [FieldOffset(73)]
        public BlendMode BlendMode;

        [FieldOffset(74)]
        public bool UseLinearFiltering;

        [FieldOffset(80)]
        public AssetId AlphaMask;

        public Material(MaterialKind kind)
            : this() => (Kind, Color, UvBottomRight) = (kind, RgbaFloat.White, Vector2.One);

        public static Material Lens(AssetId lensTexture)
        {
            return new Material(MaterialKind.Lens)
            {
                LensTextureHandle = lensTexture,
                UvTopLeft = Vector2.Zero,
                UvBottomRight = Vector2.One
            };
        }

        public static Material SolidColor(in RgbaFloat color)
        {
            return new Material(MaterialKind.SolidColor)
            {
                Color = color
            };
        }

        public static Material Texture(
            AssetId textureHandle,
            Size textureSize,
            RectangleF sourceRect)
        {
            var dimensions = textureSize.ToVector2();
            var srcTopLeft = new Vector2(sourceRect.Left, sourceRect.Top);
            var srcBottomRight = new Vector2(sourceRect.Right, sourceRect.Bottom);
            return new Material(MaterialKind.Texture)
            {
                TextureVariant = new TextureMaterial
                {
                    TextureHandle = textureHandle
                },
                UvTopLeft = srcTopLeft / dimensions,
                UvBottomRight = srcBottomRight / dimensions
            };
        }

        public static Material Screenshot()
        {
            return new Material(MaterialKind.Screenshot);
        }

        public override int GetHashCode()
        {
            return Kind switch
            {
                MaterialKind.SolidColor => HashCode.Combine(
                    BlendMode,
                    Effects.HashElements(),
                    TransitionParameters,
                    AlphaMask,
                    UvTopLeft,
                    UvBottomRight
                ),
                MaterialKind.Texture => HashCode.Combine(
                    TextureVariant.TextureHandle,
                    BlendMode,
                    UseLinearFiltering,
                    Effects.HashElements(),
                    TransitionParameters,
                    AlphaMask,
                    UvTopLeft,
                    UvBottomRight
                ),
                MaterialKind.Screenshot => HashCode.Combine(
                    BlendMode,
                    UseLinearFiltering,
                    Effects.HashElements(),
                    TransitionParameters,
                    AlphaMask,
                    UvTopLeft,
                    UvBottomRight
                ),
                MaterialKind.Lens => 0
            };
        }
    }

    internal struct TextureMaterial
    {
        public AssetId TextureHandle;
    }

    internal struct TransitionParameters
    {
        public AssetId MaskHandle;
        public float FadeAmount;

        public override int GetHashCode()
            => HashCode.Combine(MaskHandle);
    }
}
