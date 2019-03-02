using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Text;

namespace NitroSharp.NsScript.Utilities
{
    internal ref struct BufferReader
    {
        private readonly ReadOnlySpan<byte> _buffer;
        private int _position;

        public BufferReader(ReadOnlySpan<byte> buffer)
        {
            _buffer = buffer;
            _position = 0;
        }

        public int Position
        {
            get => _position;
            set => _position = value;
        }

        public ReadOnlySpan<byte> Consumed => _buffer.Slice(0, _position);
        public ReadOnlySpan<byte> Unconsumed => _buffer.Slice(_position);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<byte> Consume(int byteCount)
        {
            if (TryConsume(byteCount, out ReadOnlySpan<byte> slice))
            {
                return slice;
            }

            ThrowNoData<byte>();
            return default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryConsume(int byteCount, out ReadOnlySpan<byte> slice)
        {
            int start = _position;
            int end = start + byteCount;
            if (end > _buffer.Length)
            {
                slice = default;
                return false;
            }

            slice = _buffer.Slice(start, byteCount);
            _position += byteCount;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte ReadByte()
            => TryReadByte(out byte value) ? value : ThrowNoData<byte>();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ReadInt32LE()
            => TryReadInt32LE(out int value) ? value : ThrowNoData<int>();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long ReadInt64LE()
            => TryReadInt64LE(out long value) ? value : ThrowNoData<long>();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public short ReadInt16LE()
            => TryReadInt16LE(out short value) ? value : ThrowNoData<short>();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort ReadUInt16LE()
            => TryReadUInt16LE(out ushort value) ? value : ThrowNoData<ushort>();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float ReadSingle()
            => TryReadSingle(out float value) ? value : ThrowNoData<float>();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ReadLengthPrefixedUtf8String()
            => TryReadLengthPrefixedUtf8String() ?? ThrowNoData<string>();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadByte(out byte value)
        {
            if (_position < _buffer.Length)
            {
                value = _buffer[_position];
                _position++;
                return true;
            }

            value = 0;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadInt32LE(out int value)
        {
            if (BinaryPrimitives.TryReadInt32LittleEndian(Unconsumed, out value))
            {
                _position += sizeof(int);
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadInt64LE(out long value)
        {
            if (BinaryPrimitives.TryReadInt64LittleEndian(Unconsumed, out value))
            {
                _position += sizeof(long);
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadInt16LE(out short value)
        {
            if (BinaryPrimitives.TryReadInt16LittleEndian(Unconsumed, out value))
            {
                _position += sizeof(short);
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadUInt16LE(out ushort value)
        {
            if (BinaryPrimitives.TryReadUInt16LittleEndian(Unconsumed, out value))
            {
                _position += sizeof(ushort);
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadSingle(out float value)
        {
            if (BinaryPrimitives.TryReadInt32LittleEndian(Unconsumed, out int intValue))
            {
                value = Unsafe.As<int, float>(ref intValue);
                _position += sizeof(float);
                return true;
            }

            value = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string? TryReadLengthPrefixedUtf8String()
        {
            if (TryReadUInt16LE(out ushort length)
                && TryConsume(length, out ReadOnlySpan<byte> bytes))
            {
                return Encoding.UTF8.GetString(bytes);
            }

            return null;
        }

        private T ThrowNoData<T>()
            => throw new InvalidOperationException("There is no more data in the buffer.");
    }
}
