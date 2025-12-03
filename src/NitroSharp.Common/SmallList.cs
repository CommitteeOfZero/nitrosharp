using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace NitroSharp.Common;

public struct SmallList<T>
{
    public const int MaxFixed = 4;

    private struct FixedItems
    {
#pragma warning disable CS0649
        public T Item0;
        public T Item1;
        public T Item2;
        public T Item3;
#pragma warning restore CS0649

        public Span<T> AsSpan()
            => MemoryMarshal.CreateSpan(ref Item0, MaxFixed);

        public Span<T> AsSpan(int length)
            => MemoryMarshal.CreateSpan(ref Item0, length);
    }

    private T[]? _array;
    private FixedItems _fixedItems;
    private int _count;

    public SmallList(T elem)
    {
        _array = null;
        _fixedItems = new FixedItems
        {
            Item0 = elem
        };
        _count = 1;
    }

    public int Count => _count;

    public void Add(in T item)
    {
        Span<T> fixedElements = _fixedItems.AsSpan();
        if (_count < MaxFixed)
        {
            fixedElements[_count++] = item;
        }
        else
        {
            if (_array is null)
            {
                _array = new T[MaxFixed * 2];
                fixedElements.CopyTo(_array);
                fixedElements.Clear();
            }
            if (_count == _array.Length)
            {
                Array.Resize(ref _array, _array.Length * 2);
            }
            _array[_count++] = item;
        }
    }

    public void Remove(in T item)
    {
        Span<T> elems = AsSpan();
        for (int i = 0; i < _count; i++)
        {
            if (EqualityComparer<T>.Default.Equals(elems[i], item))
            {
                SwapRemove(i);
                break;
            }
        }
    }

    public void Clear() => _count = 0;

    private T SwapRemove(int index)
    {
        Span<T> elements = AsSpan();
        ref T ptr = ref elements[index];
        T elem = ptr;
        ref T last = ref elements[--_count];
        ptr = last;
        last = default!;

        if (_array is not null && _count <= MaxFixed)
        {
            elements = elements[1..];
            elements.CopyTo(_fixedItems.AsSpan());
            elements.Clear();
        }

        return elem;
    }

    public ref T this[int index]
    {
        get
        {
            if (index >= _count) { outOfRange(); }
            if (_count <= MaxFixed)
            {
                Span<T> fixedElements = _fixedItems.AsSpan();
                return ref fixedElements[index];
            }

            Debug.Assert(_array is not null);
            return ref _array[index];

            static void outOfRange() => throw new IndexOutOfRangeException();
        }
    }

    public Span<T> AsSpan()
    {
        return _count <= MaxFixed
            ? _fixedItems.AsSpan(_count)
            : _array.AsSpan(0, _count);
    }

    public readonly ReadOnlySpan<T> AsReadOnlySpan()
    {
        return _count <= MaxFixed
            ? MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in _fixedItems.Item0), _count)
            : _array.AsSpan(0, _count);
    }

    public Span<T>.Enumerator GetEnumerator()
        => AsSpan().GetEnumerator();

    public int HashElements()
    {
        int hash = 0;
        foreach (ref T item in AsSpan())
        {
            hash = HashCode.Combine(hash, item);
        }
        return hash;
    }
}
