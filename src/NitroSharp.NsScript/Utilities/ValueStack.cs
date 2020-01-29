using System;
using System.Runtime.CompilerServices;

namespace NitroSharp.NsScript.Utilities
{
    public struct ValueStack<T> where T : struct
    {
        private T[] _array;
        private int _size;

        public ValueStack(int initialCapacity = 4)
        {
            _array = new T[initialCapacity];
            _size = 0;
        }

        public int Count => _size;

        public ref T this[int index] => ref _array[index];

        public ReadOnlySpan<T> AsSpan(int start, int length)
            => _array.AsSpan(start, length);

        public void Push(T value)
        {
            T local = value;
            Push(ref local);
        }

        public void Push(ref T value)
        {
            int size = _size;
            T[] array = _array;

            if ((uint)size < (uint)array.Length)
            {
                array[size] = value;
                _size = size + 1;
            }
            else
            {
                PushWithResize(ref value);
            }
        }

        public ref T Peek()
        {
            int size = _size;
            if (size == 0)
            {
                ThrowStackEmpty();
            }

            return ref _array[size - 1];
        }

        public ref T Peek(int offset)
        {
            int size = _size;
            if (size == 0)
            {
                ThrowStackEmpty();
            }

            return ref _array[size - offset - 1];
        }

        public T Pop()
        {
            int size = _size - 1;
            T[] array = _array;

            // if size == -1
            if ((uint)size >= (uint)array.Length)
            {
                ThrowStackEmpty();
            }

            _size = size;
            return array[size];
        }

        public void Pop(int count)
        {
            int newSize = _size - count;
            if (newSize < 0)
            {
                _size = 0;
                ThrowStackEmpty();
            }

            _size = newSize;
        }

        public bool TryPop(out T value)
        {
            int size = _size - 1;
            T[] array = _array;

            // if size == -1
            if ((uint)size >= (uint)array.Length)
            {
                value = default;
                return false;
            }

            _size = size;
            value = array[size];
            return true;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void PushWithResize(ref T value)
        {
            Array.Resize(ref _array, _array.Length * 2);
            _array[_size++] = value;
        }

        private void ThrowStackEmpty()
            => throw new InvalidOperationException("Stack is empty.");
    }
}
