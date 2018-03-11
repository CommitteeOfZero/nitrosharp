using System;

namespace NitroSharp.Utilities
{
    internal struct ArrayBuilder<T>
    {
        private T[] _elements;
        private readonly uint _initialCapacity;

        public ArrayBuilder(uint initialCapacity)
        {
            _initialCapacity = initialCapacity;
            _elements = new T[initialCapacity];
            ElementCount = 0;
        }

        public uint ElementCount { get; private set; }

        public ref T Add()
        {
            if (_elements.Length <= ElementCount)
            {
                Array.Resize(ref _elements, (int)ElementCount * 2);
            }

            return ref _elements[ElementCount++];
        }

        public T[] ToArray()
        {
            if (_elements.Length != ElementCount)
            {
                Array.Resize(ref _elements, (int)ElementCount);
            }

            return _elements;
        }

        public void Reset()
        {
            _elements = new T[_initialCapacity];
            ElementCount = 0;
        }
    }
}
