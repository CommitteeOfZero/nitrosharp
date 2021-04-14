using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Text;

namespace NitroSharp.NsScript.Utilities
{
    internal ref struct BufferReader
    {
        private readonly ReadOnlySpan<byte> _buffer;

        public BufferReader(ReadOnlySpan<byte> buffer)
        {
            _buffer = buffer;
            Position = 0;
        }

        public int Position { get; set; }

        public ReadOnlySpan<byte> Consumed => _buffer[..Position];
        public ReadOnlySpan<byte> Unconsumed => _buffer[Position..];

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
            int start = Position;
            int end = start + byteCount;
            if (end > _buffer.Length)
            {
                slice = default;
                return false;
            }

            slice = _buffer.Slice(start, byteCount);
            Position += byteCount;
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
            if (Position < _buffer.Length)
            {
                value = _buffer[Position];
                Position++;
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
                Position += sizeof(int);
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadInt64LE(out long value)
        {
            if (BinaryPrimitives.TryReadInt64LittleEndian(Unconsumed, out value))
            {
                Position += sizeof(long);
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadInt16LE(out short value)
        {
            if (BinaryPrimitives.TryReadInt16LittleEndian(Unconsumed, out value))
            {
                Position += sizeof(short);
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadUInt16LE(out ushort value)
        {
            if (BinaryPrimitives.TryReadUInt16LittleEndian(Unconsumed, out value))
            {
                Position += sizeof(ushort);
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
                Position += sizeof(float);
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
