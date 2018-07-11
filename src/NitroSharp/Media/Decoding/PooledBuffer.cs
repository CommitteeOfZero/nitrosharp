using System;

namespace NitroSharp.Media.Decoding
{
    public struct PooledBuffer
    {
        public readonly UnmanagedMemoryPool Pool;

        internal PooledBuffer(IntPtr pointer, uint size, UnmanagedMemoryPool pool)
        {
            Pool = pool;
            Data = pointer;
            Size = size;
            Position = 0;
        }

        public readonly IntPtr Data;
        public uint Size;
        public uint Position;

        public uint FreeSpace => Size - Position;

        public void Free()
        {
            if (Pool != null && Data != IntPtr.Zero)
            {
                Pool.Return(Data);
            }
        }
    }
}
