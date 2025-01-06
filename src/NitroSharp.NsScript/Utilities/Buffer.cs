using System;
using System.Buffers;

namespace NitroSharp.NsScript.Utilities;

internal readonly struct BufferSlice<T>(IBuffer<T> buffer, int length)
{
    private readonly int _length = length;
    public IBuffer<T> Buffer { get; } = buffer;

    public Span<T> AsSpan() => Buffer.AsSpan()[.._length];
}

internal interface IBuffer<T> : IDisposable
{
    int Length { get; }
    Span<T> AsSpan();
    void Resize(int newSize);
}

internal sealed class HeapAllocBuffer<T> : IBuffer<T>
{
    private T[] _array;

    private HeapAllocBuffer(T[] array)
    {
        _array = array;
    }

    public int Length => _array.Length;

    public static HeapAllocBuffer<T> Allocate(int minimumSize) => new(new T[minimumSize]);

    public void Resize(int newSize)
    {
        Array.Resize(ref _array, newSize);
    }

    public Span<T> AsSpan() => _array.AsSpan();

    public void Dispose()
    {
    }
}

internal sealed class PooledBuffer<T> : IBuffer<T>
{
    private T[] _pooledArray;
    private int _size;

    private PooledBuffer(T[] pooledArray, int size)
    {
        _pooledArray = pooledArray;
        _size = size;
    }

    public int Length => _pooledArray.Length;

    public static PooledBuffer<T> Allocate(int minimumSize)
        => new(ArrayPool<T>.Shared.Rent(minimumSize), minimumSize);

    public void Resize(int newSize)
    {
        T[] newArray = ArrayPool<T>.Shared.Rent((int)newSize);
        Array.Copy(_pooledArray, newArray, _size);
        ArrayPool<T>.Shared.Return(_pooledArray);
        _pooledArray = newArray;
        _size = newSize;
    }

    public Span<T> AsSpan() => _pooledArray.AsSpan(0, (int)_size);

    public void Dispose()
    {
        ArrayPool<T>.Shared.Return(_pooledArray);
    }
}
