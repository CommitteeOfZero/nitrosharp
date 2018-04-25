using System.Runtime.CompilerServices;
using NitroSharp.NsScript;
using Veldrid;

namespace NitroSharp.Primitives
{
    internal static class NsColorExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RgbaByte ToRgbaByte(this NsColor nsColor)
        {
            return new RgbaByte(nsColor.R, nsColor.G, nsColor.B, 255);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RgbaFloat ToRgbaFloat(this NsColor nsColor)
        {
            return new RgbaFloat(nsColor.R / 255.0f, nsColor.G / 255.0f, nsColor.B / 255.0f, 1.0f);
        }
    }
}
