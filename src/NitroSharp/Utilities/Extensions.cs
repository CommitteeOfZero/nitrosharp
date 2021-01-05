using System.Numerics;
using MessagePack;
using NitroSharp.NsScript;
using Veldrid;

namespace NitroSharp
{
    internal static class RgbaFloatExtensions
    {
        public static RgbaFloat Multiply(this RgbaFloat source, float value)
        {
            var v = source.ToVector4() * value;
            return new RgbaFloat(v.X, v.Y, v.Z, v.W);
        }

        public static void SetAlpha(ref this RgbaFloat color, float alpha)
            => color = new RgbaFloat(color.R, color.G, color.B, alpha);
    }

    internal static class NsColorExtensions
    {
        public static RgbaByte ToRgbaByte(this NsColor nsColor)
            => new RgbaByte(nsColor.R, nsColor.G, nsColor.B, 255);

        public static RgbaFloat ToRgbaFloat(this NsColor nsColor)
            => new RgbaFloat(nsColor.R / 255.0f, nsColor.G / 255.0f, nsColor.B / 255.0f, 1.0f);

        public static Vector4 ToVector4(this NsColor nsColor)
            => new(nsColor.R / 255.0f, nsColor.G / 255.0f, nsColor.B / 255.0f, 1.0f);
    }

    internal enum Vector2Component
    {
        X,
        Y
    }

    internal static class VectorExtensions
    {
        public static float Get(this Vector2 vec, Vector2Component component) => component switch
        {
            Vector2Component.X => vec.X,
            Vector2Component.Y => vec.Y,
            _ => ThrowHelper.Unreachable<float>()
        };

        public static Vector2 XY(this in Vector4 vec) => new Vector2(vec.X, vec.Y);
        public static Vector2 XY(this in Vector3 vec) => new Vector2(vec.X, vec.Y);

        public static RgbaFloat ToRgbaFloat(this Vector4 v) => new(v);
    }

    internal static class MessagePackWriterExtensions
    {
        public static void Write(this ref MessagePackWriter writer, int? nullableInteger)
        {
            if (nullableInteger is int n)
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
}
