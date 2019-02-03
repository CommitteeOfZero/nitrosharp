using NitroSharp.NsScriptNew;
using NitroSharp.NsScriptNew.Symbols;
using System;
using System.Buffers.Text;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace NitroSharp.NsScriptCompiler.Playground
{
    partial class Program
    {
        static void Main(string[] args)
        {
            RunCompiler();
            RunDisasm();
        }

        static void RunDisasm()
        {
            using var script = File.OpenRead("S:/ChaosContent/Noah/nsx/boot.nsx");
            var boot = NsxModule.LoadModule(script);

            foreach (string import in boot.Imports)
            {
                Console.WriteLine(import);
            }

            ref readonly SubroutineRuntimeInformation srti =
                ref boot.GetSubroutineRuntimeInformation(1);



            //var someFunc = boot.GetSubroutine(0);
            //var disasm = new BodyDisassembler(someFunc.Code);

            //var sw = Stopwatch.StartNew();
            //for (int i = 0; i < boot.StringCount; i++)
            //{
            //    boot.GetString((ushort)i);
            //}
            //sw.Stop();
            //Console.WriteLine(sw.ElapsedMilliseconds);

            //Opcode opcode = Opcode.Nop;
            //int n = 0;
            //do
            //{
            //    opcode = disasm.NextOpcode();
            //    disasm.SkipOperands();

            //    Console.WriteLine(opcode.ToString());
            //    n++;
            //} while (opcode != Opcode.Return);
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
            sw.Stop();
            Console.WriteLine(sw.Elapsed.TotalSeconds);
            //GC.EndNoGCRegion();
        }

        [StructLayout(LayoutKind.Explicit)]
        public readonly struct Immediate
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
                    case Opcode.PresentText:
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
}
