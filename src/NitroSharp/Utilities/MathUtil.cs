namespace NitroSharp.Utilities
{
    internal static class MathUtil
    {
        public static float Clamp(float value, float min, float max)
            => value < min ? min : value > max ? max : value;

        public static int RoundUp(int value, int multiple)
        {
            int remainder = value % multiple;
            return remainder == 0
                ? value
                : value + multiple - remainder;
        }

        public static uint RoundUp(uint value, uint multiple)
        {
            uint remainder = value % multiple;
            return remainder == 0
                ? value
                : value + multiple - remainder;
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
