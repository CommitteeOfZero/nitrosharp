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
    }
}
