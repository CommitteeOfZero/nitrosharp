using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace NitroSharp.Media.Decoding
{
    public sealed class UnmanagedMemoryPool : IDisposable
    {
        private IntPtr _start;
        private readonly Channel<IntPtr> _availableChunks;
#if DEBUG
        private readonly HashSet<IntPtr> _allPointers;

#endif

        public UnmanagedMemoryPool(uint chunkSize, uint chunkCount, bool clearMemory = false)
        {
            ChunkSize = chunkSize;
            ChunkCount = chunkCount;
            _start = Marshal.AllocHGlobal((int)(chunkSize * chunkCount));
            if (clearMemory)
            {
                unsafe
                {
                    Unsafe.InitBlock((void*)_start, 0, chunkSize * chunkCount);
                }
            }

            _availableChunks = Channel.CreateUnbounded<IntPtr>(
                new UnboundedChannelOptions()
                {
                    SingleReader = true,
                    SingleWriter = true,
                    AllowSynchronousContinuations = false
                });

#if DEBUG
            _allPointers = new HashSet<IntPtr>();
#endif
            for (int i = 0; i < chunkCount; i++)
            {
                IntPtr ptr = _start + (int)(chunkSize * i);
                _availableChunks.Writer.TryWrite(ptr);
#if DEBUG
                _allPointers.Add(ptr);
#endif
            }
        }

        public uint ChunkSize { get; }
        public uint ChunkCount { get; }

        public ValueTask<IntPtr> RentChunkAsync()
        {
            return _availableChunks.Reader.ReadAsync();
        }

        public void Return(IntPtr chunkPointer)
        {
            EnsureBelongsToPool(chunkPointer);
            try
            {
                if (!_availableChunks.Writer.TryWrite(chunkPointer))
                {
                    ReturnFailed();
                }
            }
            catch (ChannelClosedException)
            {
                return;
            }
        }

        [Conditional("DEBUG")]
        [DebuggerNonUserCode]
        private void EnsureBelongsToPool(IntPtr ptr)
        {
#if DEBUG
            Debug.Assert(_allPointers.Contains(ptr));
#endif
        }

        public unsafe Span<T> AsSpan<T>() where T : struct
            => new(_start.ToPointer(), (int)ChunkCount);

        public unsafe ReadOnlySpan<T> AsReadOnlySpan<T>() where T : struct
            => new(_start.ToPointer(), (int)(ChunkSize * ChunkCount));

        public void Dispose()
        {
            _availableChunks.Writer.Complete();
            Marshal.FreeHGlobal(_start);
            _start = IntPtr.Zero;
        }

        private void ReturnFailed()
            => throw new InvalidOperationException("Could not return a memory chunk to the pool. This usually means the pool is used incorrectly.");
    }
}
