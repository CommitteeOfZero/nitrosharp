using System;
using System.Runtime.CompilerServices;

namespace NitroSharp.Utilities
{
    internal static class ArrayUtil
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void EnsureCapacity<T>(ref T[] array, int capacity)
        {
            if (array.Length < capacity)
            {
                int newSize = Math.Max(array.Length * 2, capacity);
                Array.Resize(ref array, newSize);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DisposeElements<T>(T[] array) where T : IDisposable
        {
            foreach (T element in array)
            {
                element?.Dispose();
            }
        }
    }
}
