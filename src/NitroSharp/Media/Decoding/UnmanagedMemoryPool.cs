using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace NitroSharp.Media.Decoding
{
    public sealed class UnmanagedMemoryPool : IDisposable
    {
        private const bool EnableManualContinuationScheduling = false;

        private IntPtr _start;
        private readonly Channel<IntPtr> _availableChunks;
#if DEBUG
        private readonly HashSet<IntPtr> _allPointers;
#endif

        private readonly ConcurrentQueue<IntPtr> _chunksToReturn;
        private readonly WaitCallback _threadPoolCallback;

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

            if (EnableManualContinuationScheduling)
            {
                _chunksToReturn = new ConcurrentQueue<IntPtr>();
                _threadPoolCallback = new WaitCallback(ThreadPoolCallback);
            }

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
            if (EnableManualContinuationScheduling)
            {
                ReturnOnThreadPool(chunkPointer);
            }
            else
            {
                ReturnCore(chunkPointer);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ReturnCore(IntPtr chunkPointer)
        {
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

        public void ReturnOnThreadPool(IntPtr buffer)
        {
            _chunksToReturn.Enqueue(buffer);
            ThreadPool.QueueUserWorkItem(_threadPoolCallback, null);
        }

        private void ThreadPoolCallback(object o)
        {
            if (_chunksToReturn.TryDequeue(out IntPtr chunk))
            {
                ReturnCore(chunk);
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

        private void ReturnFailed()
        {
            throw new InvalidOperationException("Could not return a memory chunk to the pool. This usually means the pool is used incorrectly.");
        }

        public Span<T> AsSpan<T>() where T : struct
        {
            unsafe
            {
                return new Span<T>(_start.ToPointer(), (int)ChunkCount);
            }
        }

        public ReadOnlySpan<T> AsReadOnlySpan<T>() where T : struct
        {
            unsafe
            {
                return new ReadOnlySpan<T>(_start.ToPointer(), (int)(ChunkSize * ChunkCount));
            }
        }

        public void Dispose()
        {
            _availableChunks.Writer.Complete();
            Marshal.FreeHGlobal(_start);
            _start = IntPtr.Zero;
        }
    }
}
