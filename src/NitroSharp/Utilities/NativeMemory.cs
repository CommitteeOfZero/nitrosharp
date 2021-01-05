using System;
using System.Runtime.InteropServices;

namespace NitroSharp.Utilities
{
    internal struct NativeMemory : IDisposable
    {
        public IntPtr Pointer;
        public readonly uint Size;

        private NativeMemory(IntPtr pointer, uint size)
            => (Pointer, Size) = (pointer, size);

        public static NativeMemory Allocate(uint size)
            => new(Marshal.AllocHGlobal((int)size), size);

        public unsafe Span<T> AsSpan<T>()
            => new(Pointer.ToPointer(), (int)Size);

        public unsafe ReadOnlySpan<T> AsReadOnlySpan<T>()
            => new(Pointer.ToPointer(), (int)Size);

        public void Dispose()
        {
            Marshal.FreeHGlobal(Pointer);
            Pointer = IntPtr.Zero;
        }
    }
}
