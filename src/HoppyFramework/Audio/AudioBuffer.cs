using System;
using System.Runtime.InteropServices;

namespace HoppyFramework.Audio
{
    public sealed class AudioBuffer : IDisposable
    {
        public AudioBuffer(int id, int capacity)
        {
            Id = id;
            Capacity = capacity;

            StartPointer = Marshal.AllocHGlobal(capacity);
            CurrentPointer = StartPointer;
        }

        public int Id { get; }
        public int Capacity { get; }
        public int Position { get; private set; }

        public IntPtr StartPointer { get; }
        public IntPtr CurrentPointer { get; private set; }

        public int FreeSpace => Capacity - Position;

        public void AdvancePosition(int offset)
        {
            if (Position + offset <= Capacity)
            {
                Position += offset;
                CurrentPointer += offset;
            }
        }

        public void ResetPosition()
        {
            Position = 0;
            CurrentPointer = StartPointer;
        }

        public void Dispose()
        {
            Marshal.FreeHGlobal(StartPointer);
            GC.SuppressFinalize(this);
        }

        ~AudioBuffer()
        {
            Marshal.FreeHGlobal(StartPointer);
        }
    }
}
