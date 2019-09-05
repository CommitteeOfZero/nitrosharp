using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

#nullable enable

namespace NitroSharp.Utilities
{
    internal struct ArrayBuilder<T>
    {
        private T[] _elements;
        private uint _count;

        public ArrayBuilder(int initialCapacity) : this((uint)initialCapacity)
        {
        }

        public ArrayBuilder(uint initialCapacity)
        {
            _elements = initialCapacity > 0
                ? new T[initialCapacity]
                : Array.Empty<T>();
            _count = 0;
        }

        public T[] UnderlyingArray => _elements;
        public uint Count => _count;

        public ref T this[uint index]
        {
            get
            {
                static void oob() => throw new IndexOutOfRangeException();
                if (index >= _count) { oob(); }
                return ref _elements[index];
            }
        }

        public ref T this[int index]
        {
            get
            {
                static void oob() => throw new IndexOutOfRangeException();
                if (index >= _count) { oob(); }
                return ref _elements[index];
            }
        }

        public ref T Add()
        {
            EnsureCapacity(_count + 1);
            return ref _elements[_count++];
        }

        public void Add(T item)
        {
            EnsureCapacity(_count + 1);
            _elements[_count++] = item;
        }

        public void Insert(int index, T item)
        {
            Debug.Assert(index < _count);
            if (_count == _elements.Length)
            {
                Array.Resize(ref _elements, _elements.Length * 2);
            }

            if (index < _count)
            {
                Array.Copy(_elements, index, _elements, index + 1, _count - index);
            }

            _elements[index] = item;
            _count++;
        }

        public void Remove(int index)
        {
            Debug.Assert(index < _count);
            _count--;
            if (index < _count)
            {
                Array.Copy(_elements, index + 1, _elements, index, _count - index);
            }
        }

        public void Truncate(uint length)
        {
            static void outOfRange() => throw new ArgumentOutOfRangeException("length");

            if (length > Count) outOfRange();
            _count = length;
        }

        public void Clear()
        {
            _count = 0;
        }

        public void AddRange(ReadOnlySpan<T> items)
        {
            EnsureCapacity((uint)(_count + items.Length));
            for (int i = 0; i < items.Length; i++)
            {
                _elements[_count++] = items[i];
            }
        }

        public Span<T> Append(uint count)
        {
            EnsureCapacity(_count + count);
            var span = new Span<T>(_elements, (int)_count, (int)count);
            _count += count;
            return span;
        }

        public void RemoveLast()
        {
            if (_count > 0)
            {
                _count--;
            }
        }

        public T[] ToArray()
        {
            var copy = new T[_count];
            Array.Copy(_elements, copy, _count);
            return copy;
        }

        public Span<T> AsSpan() => new Span<T>(_elements, 0, (int)_count);
        public Span<T> AsSpan(int start, int length) => new Span<T>(_elements, start, length);
        public ReadOnlySpan<T> AsReadonlySpan() => new ReadOnlySpan<T>(_elements, 0, (int)_count);
        public ReadOnlySpan<T> AsReadonlySpan(int start, int length)
            => new ReadOnlySpan<T>(_elements, start, length);

        public void Reset()
        {
            _count = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureCapacity(uint requiredCapacity)
        {
            if (_elements.Length < requiredCapacity)
            {
                Grow(requiredCapacity);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Grow(uint requiredCapacity)
        {
            uint newCapacity = Math.Max((uint)_elements.Length * 2, requiredCapacity);
            Array.Resize(ref _elements, (int)newCapacity);
        }
    }
}
