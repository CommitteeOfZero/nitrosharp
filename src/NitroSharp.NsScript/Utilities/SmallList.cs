using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace NitroSharp.Utilities
{
    public struct SmallList<T>
    {
        private const int MaxFixed = 2;

        private struct FixedItems
        {
            public T Item0;
            public T Item1;

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
                if (_array == null)
                {
                    _array = new T[MaxFixed * 2];
                    fixedElements.CopyTo(_array);
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
                    RemoveAt(i);
                    break;
                }
            }
        }

        private void RemoveAt(int index)
        {
            Debug.Assert(index < _count);
            if (index == _count - 1)
            {
                _count--;
                return;
            }
            Span<T> elems = AsSpan();
            elems[(index + 1)..].CopyTo(elems[index..^1]);
            _count--;
        }

        public ref T this[int index]
        {
            get
            {
                static void oob() => throw new IndexOutOfRangeException();

                if (index >= _count) { oob(); }
                if (index < MaxFixed)
                {
                    Span<T> fixedElements = _fixedItems.AsSpan();
                    return ref fixedElements[index];
                }

                Debug.Assert(_array != null);
                return ref _array[index];
            }
        }

        public Span<T> AsSpan()
        {
            return _count <= MaxFixed
                ? _fixedItems.AsSpan(_count)
                : _array.AsSpan(0, _count);
        }

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
}
