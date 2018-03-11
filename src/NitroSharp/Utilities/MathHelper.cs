using System;

namespace NitroSharp.Utilities
{
    internal static class MathHelper
    {
        public static float Clamp(float value, float min, float max)
        {
            return value < min ? min : value > max ? max : value;
        }

        public static int RoundUp(int value, int multiple)
        {
            return (int)Math.Round((double)value / multiple, MidpointRounding.AwayFromZero) * multiple;
        }
    }
}
