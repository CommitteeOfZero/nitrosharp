using System.Runtime.CompilerServices;

namespace NitroSharp.Utilities
{
    internal static class Util
    {
        internal static uint SizeInBytes<T>(this T[] array) where T : struct
        {
            return (uint)(array.Length * Unsafe.SizeOf<T>());
        }
    }
}
