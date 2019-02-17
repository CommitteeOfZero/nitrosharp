using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Text;

namespace NitroSharp.NsScript.Utilities
{
    public interface IBuffer<T> : IDisposable
    {
        uint Length { get; }
        Span<T> AsSpan();
        void Resize(uint newSize);
    }

    public sealed class HeapAllocBuffer<T> : IBuffer<T>
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

    public sealed class PooledBuffer<T> : IBuffer<T>
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
        private IBuffer<byte>? _resizableBuffer;
        private Span<byte> _span;
        private int _position;

        public BufferWriter(IBuffer<byte> resizableBuffer)
        {
            _resizableBuffer = resizableBuffer;
            _span = resizableBuffer.AsSpan();
            _position = 0;
        }

        public BufferWriter(Span<byte> buffer)
        {
            _resizableBuffer = null;
            _span = buffer;
            _position = 0;
        }

        public Span<byte> Free => _span.Slice(_position);
        public Span<byte> Written => _span.Slice(0, _position);

        public int Position
        {
            get => _position;
            set
            {
                while (value > _span.Length)
                {
                    Resize((uint)value);
                }
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
                Resize((uint)(_span.Length + bytes.Length));
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
            int sz = Encoding.UTF8.GetByteCount(text);
            WriteUInt16LE((ushort)sz);
            if (sz > Free.Length)
            {
                Resize((uint)(_span.Length + sz));
            }

            Encoding.UTF8.GetBytes(text, Free);
            _position += sz;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteUtf8String(string text)
        {
            int sz = Encoding.UTF8.GetByteCount(text);
            if (sz > Free.Length)
            {
                Resize((uint)(_span.Length + sz));
            }

            Encoding.UTF8.GetBytes(text, Free);
            _position += sz;
        }

        public void Clear() => _position = 0;

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Resize(uint desiredSize = 0)
        {
            if (_resizableBuffer != null)
            {
                uint newSize = Math.Max(desiredSize, _resizableBuffer.Length * 2);
                _resizableBuffer.Resize(newSize);
                _span = _resizableBuffer.AsSpan();
            }
            else
            {
                throw new InvalidOperationException("The underlying buffer is not expandable.");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryWriteByte(byte value)
        {
            if (_position == _span.Length) { return false; }
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
