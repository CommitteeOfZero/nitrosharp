using System;
using System.Buffers;

namespace NitroSharp.NsScript.Utilities
{
    internal interface IBuffer<T> : IDisposable
    {
        uint Length { get; }
        Span<T> AsSpan();
        void Resize(uint newSize);
    }

    internal sealed class HeapAllocBuffer<T> : IBuffer<T>
    {
        private T[] _array;

        private HeapAllocBuffer(T[] array)
        {
            _array = array;
        }

        public uint Length => (uint)_array.Length;

        public static HeapAllocBuffer<T> Allocate(uint minimumSize)
            => new HeapAllocBuffer<T>(new T[minimumSize]);

        public void Resize(uint newSize)
            => Array.Resize(ref _array, (int)newSize);

        public Span<T> AsSpan()
            => _array.AsSpan();

        public void Dispose()
        {
        }
    }

    internal sealed class PooledBuffer<T> : IBuffer<T>
    {
        private T[] _pooledArray;
        private uint _size;

        private PooledBuffer(T[] pooledArray, uint size)
        {
            _pooledArray = pooledArray;
            _size = size;
        }

        public uint Length => (uint)_pooledArray.Length;

        public static PooledBuffer<T> Allocate(uint minimumSize)
            => new PooledBuffer<T>(
                ArrayPool<T>.Shared.Rent((int)minimumSize), minimumSize);

        public void Resize(uint newSize)
        {
            T[] newArray = ArrayPool<T>.Shared.Rent((int)newSize);
            Array.Copy(_pooledArray, newArray, (int)_size);
            ArrayPool<T>.Shared.Return(_pooledArray);
            _pooledArray = newArray;
            _size = newSize;
        }

        public Span<T> AsSpan()
            => _pooledArray.AsSpan(0, (int)_size);

        public void Dispose()
            => ArrayPool<T>.Shared.Return(_pooledArray);
    }
}
