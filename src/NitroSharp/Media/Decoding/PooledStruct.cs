using System;
using System.Runtime.CompilerServices;
using NitroSharp.Utilities;

namespace NitroSharp.Media.Decoding
{
    internal readonly struct PooledStruct<T> where T : struct
    {
        public readonly IntPtr Pointer;
        public readonly UnmanagedMemoryPool Pool;

        public PooledStruct(IntPtr pointer, UnmanagedMemoryPool pool)
        {
            Pointer = pointer;
            Pool = pool;
        }

        public bool IsNull => Pointer == IntPtr.Zero;

        public ref T AsRef()
        {
            unsafe
            {
                return ref Unsafe.AsRef<T>((void*)Pointer);
            }
        }

        public void Free()
        {
            Pool.Return(Pointer);
        }
    }
}
