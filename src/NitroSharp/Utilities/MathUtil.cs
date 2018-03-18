using System;

namespace NitroSharp.Utilities
{
    internal static class MathUtil
    {
        public const float PI = (float)Math.PI;

        public static float Clamp(float value, float min, float max)
        {
            return value < min ? min : value > max ? max : value;
        }

        public static int RoundUp(int value, int multiple)
        {
            return (int)Math.Round((double)value / multiple, MidpointRounding.AwayFromZero) * multiple;
        }

        public static float ToRadians(float degrees)
        {
            return degrees / 180.0f * PI;
        }

        public static float ToDegrees(float radians)
        {
            return radians / PI * 180.0f;
        }
    }
}
