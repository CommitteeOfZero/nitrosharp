using System.Numerics;
using MessagePack;
using NitroSharp.NsScript;
using Veldrid;

namespace NitroSharp;

internal static class RgbaFloatExtensions
{
    extension(in RgbaFloat value)
    {
        public Vector4 AsVector4()
        {
            return new Vector4(value.R, value.G, value.B, value.A);
        }

        public RgbaFloat Multiply(float value1)
        {
            Vector4 v = value.AsVector4() * value1;
            return new RgbaFloat(v.X, v.Y, v.Z, v.W);
        }
    }

    public static void SetAlpha(ref this RgbaFloat color, float alpha)
        => color = new RgbaFloat(color.R, color.G, color.B, alpha);
}

internal static class NsColorExtensions
{
    extension(NsColor nsColor)
    {
        public RgbaByte ToRgbaByte()
            => new(nsColor.R, nsColor.G, nsColor.B, 255);

        public RgbaFloat ToRgbaFloat()
            => new(nsColor.R / 255.0f, nsColor.G / 255.0f, nsColor.B / 255.0f, 1.0f);

        public Vector4 ToVector4()
            => new(nsColor.R / 255.0f, nsColor.G / 255.0f, nsColor.B / 255.0f, 1.0f);
    }
}

internal enum Vector2Component
{
    X,
    Y
}

internal static class VectorExtensions
{
    public static float Get(this Vector2 vec, Vector2Component component)
    {
        return component switch
        {
            Vector2Component.X => vec.X,
            Vector2Component.Y => vec.Y,
            _ => throw ThrowHelper.UnexpectedValueOf<Vector2Component>()
        };
    }

    public static Vector2 XY(this in Vector4 vec) => new(vec.X, vec.Y);
    public static Vector2 XY(this in Vector3 vec) => new(vec.X, vec.Y);

    public static RgbaFloat ToRgbaFloat(this Vector4 v) => new(v);
}

internal static class MessagePackWriterExtensions
{
    public static void Write(this ref MessagePackWriter writer, int? nullableInteger)
    {
        if (nullableInteger is { } n)
        {
            writer.Write(n);
        }
        else
        {
            writer.WriteNil();
        }
    }

    public static int? ReadNullableInt32(this ref MessagePackReader reader)
    {
        return reader.TryReadNil()
            ? null
            : reader.ReadInt32();
    }
}
