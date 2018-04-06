using System;
using System.Runtime.InteropServices;

namespace NitroSharp.Utilities
{
    internal sealed class NativeMemory : IDisposable
    {
        public static NativeMemory Allocate(uint size)
        {
            return new NativeMemory(Marshal.AllocHGlobal((int)size), size);
        }

        private NativeMemory(IntPtr pointer, uint size)
        {
            Pointer = pointer;
            Size = size;
        }

        public IntPtr Pointer { get; private set; }
        public uint Size { get; }

        public Span<T> AsSpan<T>()
        {
            unsafe
            {
                return new Span<T>(Pointer.ToPointer(), (int)Size);
            }
        }

        public void Dispose()
        {
            Marshal.FreeHGlobal(Pointer);
            Pointer = IntPtr.Zero;
        }

        ~NativeMemory()
        {
            Dispose();
        }
    }
}
