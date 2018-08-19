using System;

namespace NitroSharp.Utilities
{
    internal static class ArrayUtil
    {
        public static void EnsureCapacity<T>(ref T[] array, int capacity)
        {
            if (array.Length < capacity)
            {
                int newSize = Math.Max(array.Length * 2, capacity);
                Array.Resize(ref array, newSize);
            }
        }
    }
}
