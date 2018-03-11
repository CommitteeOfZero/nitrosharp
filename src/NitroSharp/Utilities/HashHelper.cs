using System;

namespace NitroSharp.Utilities
{
    internal static class HashHelper
    {
        public static int Combine(int value1, int value2)
        {
            uint rol5 = ((uint)value1 << 5) | ((uint)value1 >> 27);
            return ((int)rol5 + value1) ^ value2;
        }

        public static int Combine(int value1, int value2, int value3)
        {
            return Combine(value1, Combine(value2, value3));
        }

        public static int Combine(int value1, int value2, int value3, int value4)
        {
            return Combine(value1, Combine(value2, Combine(value3, value4)));
        }

        public static int Combine(int value1, int value2, int value3, int value4, int value5)
        {
            return Combine(value1, Combine(value2, Combine(value3, Combine(value4, value5))));
        }
    }
}
