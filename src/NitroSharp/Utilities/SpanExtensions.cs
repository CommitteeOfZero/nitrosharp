using System;
using Veldrid;

namespace NitroSharp.Utilities
{
    internal static class SpanExtensions
    {
        public static ref RgbaByte GetPixelRef(this Span<RgbaByte> pixelData, uint x, uint y, uint rowPitch)
        {
            int index = (int)(y * rowPitch + x);
            return ref pixelData[index];
        }

        public static ref RgbaFloat GetPixelRef(this Span<RgbaFloat> pixelData, uint x, uint y, uint rowPitch)
        {
            int index = (int)(y * rowPitch + x);
            return ref pixelData[index];
        }
    }
}
