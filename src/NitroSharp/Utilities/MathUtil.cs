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
        public static uint RoundUp(uint value, uint multiple)
        {
            uint remainder = value % multiple;
            return remainder == 0
                ? value
                : value + multiple - remainder;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ToRadians(float degrees)
        {
            return degrees / 180.0f * PI;
        }

        public static uint NearestPowerOfTwo(uint n)
        {
            uint res = n - 1;
            res |= res >> 1;
            res |= res >> 2;
            res |= res >> 4;
            res |= res >> 8;
            res |= res >> 16;
            return res + 1;
        }
    }
}
