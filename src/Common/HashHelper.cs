using System;
using System.Runtime.CompilerServices;

namespace NitroSharp.Utilities
{
    internal static class HashHelper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Combine(int value1, int value2)
        {
            uint rol5 = ((uint)value1 << 5) | ((uint)value1 >> 27);
            return ((int)rol5 + value1) ^ value2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Combine(int value1, int value2, int value3)
            => Combine(value1, Combine(value2, value3));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Combine(int value1, int value2, int value3, int value4)
            => Combine(value1, Combine(value2, Combine(value3, value4)));

        /// <summary>
        /// The offset bias value used in the FNV-1a algorithm
        /// See http://en.wikipedia.org/wiki/Fowler%E2%80%93Noll%E2%80%93Vo_hash_function
        /// </summary>
        internal const int FnvOffsetBias = unchecked((int)2166136261);

        /// <summary>
        /// The generative factor used in the FNV-1a algorithm
        /// See http://en.wikipedia.org/wiki/Fowler%E2%80%93Noll%E2%80%93Vo_hash_function
        /// </summary>
        internal const int FnvPrime = 16777619;

        /// <returns>The FNV-1a hash of <paramref name="text"/></returns>
        internal static int GetFNVHashCode(ReadOnlySpan<char> text)
        {
            int hashCode = FnvOffsetBias;
            int length = text.Length;
            for (int i = 0; i < length; i++)
            {
                hashCode = unchecked((hashCode ^ text[i]) * FnvPrime);
            }

            return hashCode;
        }
    }
}
