using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Text;

namespace NitroSharp.NsScript.Utilities
{
    internal ref struct BufferWriter
    {
        private readonly IBuffer<byte>? _resizableBuffer;
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

        public Span<byte> Free => _span[_position..];
        public Span<byte> Written => _span[.._position];

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
        public void WriteInt64LE(long value)
        {
            while (!TryWriteInt64LE(value))
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
            if (_resizableBuffer is not null)
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
        private bool TryWriteInt64LE(long value)
        {
            if (BinaryPrimitives.TryWriteInt64LittleEndian(Free, value))
            {
                _position += sizeof(long);
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
