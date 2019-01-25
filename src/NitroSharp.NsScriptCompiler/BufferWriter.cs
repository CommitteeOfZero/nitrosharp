using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Buffers.Text;
using System.Runtime.CompilerServices;
using System.Text;

namespace NitroSharp.NsScriptCompiler.Playground
{
    public struct BufferWriter : IDisposable
    {
        private byte[] _buffer;
        private int _written;

        public BufferWriter(uint minimumCapacity)
        {
            //_buffer = new byte[minimumCapacity];
            _buffer = ArrayPool<byte>.Shared.Rent((int)minimumCapacity);
            _written = 0;
        }

        public Span<byte> Free => _buffer.AsSpan(_written);
        public Span<byte> Written => _buffer.AsSpan(0, _written);

        public int WrittenCount
        {
            get => _written;
            set
            {
                while (value > _buffer.Length) Resize(value);
                _written = value;
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
            Span<byte> free = Free;
            if (free.Length < bytes.Length)
            {
                Resize(_buffer.Length + bytes.Length);
            }

            bytes.CopyTo(Free);
            _written += bytes.Length;
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
        public void WriteStringAsUtf8(string text)
        {
            while (!TryWriteUtf8String(text))
            {
                Resize();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryWriteUtf8String(string text)
        {
            Span<byte> free = Free;
            if (free.Length == 0) { return false; }
            try
            {
                _written += Encoding.UTF8.GetBytes(text, free);
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        public void Clear() => _written = 0;

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Resize(int desiredSize = 0)
        {
            int newSize = Math.Max(desiredSize, _buffer.Length * 2);
            //var newBuffer = new byte[newSize];
            var newBuffer = ArrayPool<byte>.Shared.Rent(newSize);
            Written.CopyTo(newBuffer);
            ArrayPool<byte>.Shared.Return(_buffer);
            _buffer = newBuffer;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryWriteByte(byte value)
        {
            if (_written == _buffer.Length) { return false; }
            _buffer[_written++] = value;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryWriteInt16LE(short value)
        {
            if (BinaryPrimitives.TryWriteInt16LittleEndian(Free, value))
            {
                _written += sizeof(short);
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryWriteUInt16LE(ushort value)
        {
            if (BinaryPrimitives.TryWriteUInt16LittleEndian(Free, value))
            {
                _written += sizeof(ushort);
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryWriteInt32LE(int value)
        {
            if (BinaryPrimitives.TryWriteInt32LittleEndian(Free, value))
            {
                _written += sizeof(int);
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryWriteSingle(float value)
        {
            if (!Utf8Formatter.TryFormat(value, Free, out int written))
            {
                return false;
            }
            _written += written;
            return true;
        }

        public void Dispose()
        {
            ArrayPool<byte>.Shared.Return(_buffer);
            _buffer = null;
        }
    }
}
