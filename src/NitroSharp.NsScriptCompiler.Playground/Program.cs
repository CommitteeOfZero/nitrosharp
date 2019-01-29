using NitroSharp.NsScriptNew;
using NitroSharp.NsScriptNew.Symbols;
using System;
using System.Buffers.Binary;
using System.Buffers.Text;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace NitroSharp.NsScriptCompiler.Playground
{
    class Program
    {
        static void Main(string[] args)
        {
            //RunDebuggerUI();
            RunCompiler();
        }

        static void RunDebuggerUI()
        {
            var debugger = new DebuggerUI();
            debugger.Run();
        }

        static void RunCompiler()
        {
            //bool s = GC.TryStartNoGCRegion(125829120);
            var sw = Stopwatch.StartNew();
            var compilation = new Compilation("S:/ChaosContent/Noah/nss");
            SourceModuleSymbol boot = compilation.GetSourceModule("boot.nss");
            compilation.Emit(boot);

            //var buffer = compilation.CompileMember(boot.LookupChapter("main"));
            //var disassember = new BodyDisassembler(buffer.Written);

            //Opcode opcode = Opcode.Nop;
            //int n = 0;
            //do
            //{
            //    //if (n == 21)
            //    //{
            //    //    Debugger.Break();
            //    //}

            //    opcode = disassember.NextOpcode();
            //    disassember.SkipOperands();

            //    Console.WriteLine(opcode.ToString());
            //    n++;
            //} while (opcode != Opcode.Nop);

            sw.Stop();
            Console.WriteLine(sw.Elapsed.TotalSeconds);
            //GC.EndNoGCRegion();
        }

        private ref struct BufferReader
        {
            private readonly ReadOnlySpan<byte> _buffer;
            private int _position;

            public BufferReader(ReadOnlySpan<byte> buffer)
            {
                _buffer = buffer;
                _position = 0;
            }

            public ReadOnlySpan<byte> Consumed => _buffer.Slice(0, _position);
            public ReadOnlySpan<byte> Unconsumed => _buffer.Slice(_position);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public byte ReadByte()
                => TryReadByte(out byte value) ? value : ThrowNoData<byte>();

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int ReadInt32LE()
                => TryReadInt32LE(out int value) ? value : ThrowNoData<int>();

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

            private T ThrowNoData<T>() where T : unmanaged
                => throw new InvalidOperationException("There is no more data in the buffer.");
        }

        [StructLayout(LayoutKind.Explicit)]
        private readonly struct Immediate
        {
            [FieldOffset(0)]
            public readonly BuiltInType Type;

            [FieldOffset(4)]
            public readonly int IntegerValue;
            [FieldOffset(4)]
            public readonly float FloatValue;
            [FieldOffset(4)]
            public readonly ushort StringToken;
            [FieldOffset(4)]
            public readonly BuiltInConstant Constant;

            internal Immediate(int integerValue) : this()
                => (Type, IntegerValue) = (BuiltInType.Integer, integerValue);

            internal Immediate(float floatValue) : this()
                => (Type, FloatValue) = (BuiltInType.Float, floatValue);

            internal Immediate(ushort stringToken) : this()
                => (Type, StringToken) = (BuiltInType.String, stringToken);

            internal Immediate(BuiltInConstant constant) : this()
                => (Type, Constant) = (BuiltInType.BuiltInConstant, constant);
        }

        private ref struct BodyDisassembler
        {
            private BufferReader _reader;

            public BodyDisassembler(ReadOnlySpan<byte> body)
            {
                _reader = new BufferReader(body);
            }

            public Opcode NextOpcode()
            {
                return _reader.TryReadByte(out byte opcode)
                    ? (Opcode)opcode
                    : Opcode.Nop;
            }

            public void SkipOperands()
            {
                ReadOnlySpan<byte> consumed = _reader.Consumed;
                var opcode = (Opcode)consumed[consumed.Length - 1];
                switch (opcode)
                {
                    case Opcode.LoadImm:
                        DecodeImmediateValue();
                        break;

                    case Opcode.LoadArg:
                    case Opcode.LoadVar:
                    case Opcode.StoreArg:
                    case Opcode.StoreVar:
                    case Opcode.Call:
                        DecodeToken();
                        break;

                    case Opcode.Jump:
                    case Opcode.JumpIfTrue:
                    case Opcode.JumpIfFalse:
                        DecodeOffset();
                        break;

                    case Opcode.CallFar:
                        DecodeToken();
                        DecodeToken();
                        break;

                    case Opcode.Binary:
                    case Opcode.Dispatch:
                        ReadByte();
                        break;

                    default:
                        Debug.Assert(!HasOperands(opcode));
                        break;
                }
            }

            public byte ReadByte()
                => _reader.ReadByte();

            public ushort DecodeToken()
                => _reader.ReadUInt16LE();

            public short DecodeOffset()
                => _reader.ReadInt16LE();

            public Immediate DecodeImmediateValue()
            {
                var type = (BuiltInType)_reader.ReadByte();
                switch (type)
                {
                    case BuiltInType.Integer:
                        return new Immediate(_reader.ReadInt32LE());
                    case BuiltInType.Float:
                        return new Immediate(_reader.ReadSingle());
                    case BuiltInType.String:
                        return new Immediate(_reader.ReadUInt16LE());
                    case BuiltInType.BuiltInConstant:
                        return new Immediate((BuiltInConstant)_reader.ReadByte());
                    default:
                        throw new InvalidDataException("Unexpected immediate value type.");
                }
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
                        return true;
                    default:
                        return false;
                }
            }
        }
    }
}
