using System.Runtime.CompilerServices;
using Veldrid;

namespace NitroSharp.Primitives
{
    internal static class RgbaFloatExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RgbaFloat Multiply(this RgbaFloat source, float value)
        {
            var v = source.ToVector4() * value;
            return new RgbaFloat(v.X, v.Y, v.Z, v.W);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetAlpha(ref this RgbaFloat color, float alpha)
        {
            color = new RgbaFloat(color.R, color.G, color.B, alpha);
        }
    }
}
