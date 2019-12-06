using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

#nullable enable

namespace NitroSharp.Utilities
{
    internal struct SmallList<T> where T : struct
    {
        private const int MaxFixed = 2;

        private struct FixedItems
        {
            private T _item0;
            private T _item1;

            public Span<T> AsSpan()
                => MemoryMarshal.CreateSpan(ref _item0, MaxFixed);

            public Span<T> AsSpan(int length)
                => MemoryMarshal.CreateSpan(ref _item0, length);
        }

        private T[]? _array;
        private FixedItems _fixedItems;
        private int _count;

        public int Count => _count;

        public void Add(T item)
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

        public Span<T> Enumerate()
        {
            return _count <= MaxFixed
                ? _fixedItems.AsSpan(_count)
                : _array.AsSpan(0, _count);
        }

        public int HashElements()
        {
            int hash = 0;
            foreach (ref T item in Enumerate())
            {
                hash = HashCode.Combine(hash, item);
            }
            return hash;
        }
    }
}
