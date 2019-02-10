using System;
using System.IO;
using NitroSharp.NsScriptNew.Utilities;

namespace NitroSharp.NsScriptNew.VM
{
    internal ref struct BytecodeStream
    {
        private BufferReader _reader;

        public BytecodeStream(ReadOnlySpan<byte> body)
        {
            _reader = new BufferReader(body);
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
                BuiltInType.Integer => new Immediate(_reader.ReadInt32LE()),
                BuiltInType.Float => new Immediate(_reader.ReadSingle()),
                BuiltInType.String => new Immediate(_reader.ReadUInt16LE()),
                BuiltInType.BuiltInConstant => new Immediate((BuiltInConstant)_reader.ReadByte()),
                _ => ThrowHelper.InvalidData<Immediate>("Unexpected immediate value type.")
            };
        }

        public static bool HasOperands(Opcode opcode)
        {
            switch (opcode)
            {
                case Opcode.LoadImm:
                case Opcode.StoreArg:
                case Opcode.StoreVar:
                case Opcode.Binary:
                case Opcode.Jump:
                case Opcode.JumpIfTrue:
                case Opcode.JumpIfFalse:
                case Opcode.Call:
                case Opcode.CallFar:
                case Opcode.PresentText:
                    return true;
                default:
                    return false;
            }
        }
    }

}
