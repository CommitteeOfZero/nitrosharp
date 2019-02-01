using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Text;

namespace NitroSharp.NsScriptCompiler.Playground
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

    internal ref struct BufferWriter
    {
        private IBuffer<byte> _buffer;
        private Span<byte> _span;
        private int _position;

        public BufferWriter(IBuffer<byte> buffer)
        {
            _buffer = buffer;
            _span = buffer.AsSpan();
            _position = 0;
        }

        public Span<byte> Free => _span.Slice(_position);
        public Span<byte> Written => _span.Slice(0, _position);

        public int Position
        {
            get => _position;
            set
            {
                while (value > _buffer.Length) Resize((uint)value);
                _position = value;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteByte(byte value)
        {
            while (!TryWriteByte(value))
            {
                Resize();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteBytes(ReadOnlySpan<byte> bytes)
        {
            if (Free.Length < bytes.Length)
            {
                Resize((uint)(_buffer.Length + bytes.Length));
            }

            bytes.CopyTo(Free);
            _position += bytes.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteInt32LE(int value)
        {
            while (!TryWriteInt32LE(value))
            {
                Resize();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteInt16LE(short value)
        {
            while (!TryWriteInt16LE(value))
            {
                Resize();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteUInt16LE(ushort value)
        {
            while (!TryWriteUInt16LE(value))
            {
                Resize();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteSingle(float value)
        {
            while (!TryWriteSingle(value))
            {
                Resize();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteLengthPrefixedUtf8String(string text)
        {
            WriteUInt16LE((ushort)text.Length);
            WriteUtf8String(text);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteUtf8String(string text)
        {
            int sz = Encoding.UTF8.GetByteCount(text);
            if (sz > Free.Length)
            {
                Resize((uint)(_buffer.Length + sz));
            }

            Encoding.UTF8.GetBytes(text, Free);
            _position += sz;
        }

        public void Clear() => _position = 0;

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Resize(uint desiredSize = 0)
        {
            uint newSize = Math.Max(desiredSize, _buffer.Length * 2);
            _buffer.Resize(newSize);
            _span = _buffer.AsSpan();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryWriteByte(byte value)
        {
            if (_position == _buffer.Length) { return false; }
            _span[_position++] = value;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryWriteInt16LE(short value)
        {
            if (BinaryPrimitives.TryWriteInt16LittleEndian(Free, value))
            {
                _position += sizeof(short);
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryWriteUInt16LE(ushort value)
        {
            if (BinaryPrimitives.TryWriteUInt16LittleEndian(Free, value))
            {
                _position += sizeof(ushort);
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryWriteInt32LE(int value)
        {
            if (BinaryPrimitives.TryWriteInt32LittleEndian(Free, value))
            {
                _position += sizeof(int);
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryWriteSingle(float value)
        {
            return TryWriteInt32LE(Unsafe.As<float, int>(ref value));
        }
    }
}
