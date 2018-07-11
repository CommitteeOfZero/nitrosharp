using System;

namespace NitroSharp.Utilities
{
    internal struct ArrayBuilder<T> where T : struct
    {
        private T[] _elements;
        private readonly uint _initialCapacity;

        public ArrayBuilder(uint initialCapacity)
        {
            _initialCapacity = initialCapacity;
            _elements = new T[initialCapacity];
            Count = 0;
        }

        public uint Count;

        public ref T this[uint index] => ref _elements[index];

        public ref T Add()
        {
            if (_elements.Length <= Count)
            {
                Array.Resize(ref _elements, (int)Count * 2);
            }

            return ref _elements[Count++];
        }

        public void RemoveLast()
        {
            if (Count > 0)
            {
                Count--;
            }
        }

        public T[] ToArray()
        {
            var copy = new T[Count];
            Array.Copy(_elements, copy, Count);
            return copy;
        }

        public Span<T> AsSpan() => new Span<T>(_elements, 0, (int)Count);
        public ReadOnlySpan<T> AsReadonlySpan() => new ReadOnlySpan<T>(_elements, 0, (int)Count);

        public void Reset()
        {
            Count = 0;
        }
    }
}
