using System;
using System.IO;
using System.Runtime.InteropServices;
using NitroSharp.NsScript.Utilities;

namespace NitroSharp.NsScript.VM
{
    internal ref struct BytecodeStream
    {
        private BufferReader _reader;

        public BytecodeStream(ReadOnlySpan<byte> body, int pos)
        {
            _reader = new BufferReader(body);
            Position = pos;
        }

        public int Position
        {
            get => _reader.Position;
            set => _reader.Position = value;
        }

        public Opcode NextOpcode()
        {
            return _reader.TryReadByte(out byte opcode)
                ? (Opcode)opcode
                : Opcode.Nop;
        }

        public byte ReadByte()
            => _reader.ReadByte();

        public ushort DecodeToken()
            => _reader.ReadUInt16LE();

        public short DecodeOffset()
            => _reader.ReadInt16LE();

        /// <exception cref="InvalidDataException" />
        public Immediate DecodeImmediateValue()
        {
            var type = (BuiltInType)_reader.ReadByte();
            return type switch
            {
                BuiltInType.Numeric => new Immediate(_reader.ReadSingle(), false),
                BuiltInType.DeltaNumeric => new Immediate(_reader.ReadSingle(), true),
                BuiltInType.String => new Immediate(_reader.ReadUInt16LE()),
                BuiltInType.BuiltInConstant => new Immediate((BuiltInConstant)_reader.ReadByte()),
                _ => ThrowHelper.InvalidData<Immediate>("Unexpected immediate value type.")
            };
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    internal readonly struct Immediate
    {
        [FieldOffset(0)]
        public readonly BuiltInType Type;

        [FieldOffset(4)]
        public readonly float Numeric;
        [FieldOffset(4)]
        public readonly ushort StringToken;
        [FieldOffset(4)]
        public readonly BuiltInConstant Constant;

        internal Immediate(float value, bool isDelta) : this()
            => (Type, Numeric) = (isDelta ? BuiltInType.DeltaNumeric : BuiltInType.Numeric, value);

        internal Immediate(ushort stringToken) : this()
            => (Type, StringToken) = (BuiltInType.String, stringToken);

        internal Immediate(BuiltInConstant constant) : this()
            => (Type, Constant) = (BuiltInType.BuiltInConstant, constant);
    }
}
