using System;

namespace NitroSharp.Utilities
{
    internal static class FnvHasher
    {
        private const int FnvOffsetBias = unchecked((int)2166136261);
        private const int FnvPrime = 16777619;

        public static int HashString(ReadOnlySpan<char> text)
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
