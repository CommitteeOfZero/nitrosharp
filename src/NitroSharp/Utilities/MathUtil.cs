using System;
using System.Runtime.CompilerServices;

namespace NitroSharp.Utilities
{
    internal static class MathUtil
    {
        public const float PI = (float)Math.PI;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Clamp(float value, float min, float max)
        {
            return value < min ? min : value > max ? max : value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int RoundUp(int value, int multiple)
        {
            int remainder = value % multiple;
            return remainder == 0
                ? value
                : value + multiple - remainder;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ToRadians(float degrees)
        {
            return degrees / 180.0f * PI;
        }
    }
}
