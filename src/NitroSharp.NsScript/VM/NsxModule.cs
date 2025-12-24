using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Text;
using NitroSharp.NsScript.Utilities;

namespace NitroSharp.NsScript.VM
{
    public enum SubroutineKind : byte
    {
        Chapter = 0,
        Scene = 1,
        Function = 2
    }

    [DebuggerDisplay("Module '{Name}'")]
    public sealed class NsxModule
    {
        private readonly byte[] _codeSection;
        private readonly StringOffset[] _stringOffsets;
        private readonly string?[] _stringHeap;

        private readonly CodeOffset[] _subroutineOffsets;
        private readonly Subroutine[] _subroutines;
        private readonly SourceMapping[] _sourceMappings;
        private readonly SubroutineRuntimeInfo[] _srti;
        private readonly Dictionary<string, int> _subroutineMap;

        private readonly FileOffset _stringHeapOffset;
        private readonly Stream _stream;

        private NsxModule(
            Stream stream,
            string name,
            DateTimeOffset sourceModificationTime,
            CodeOffset[] subroutineOffsets,
            byte[] rtiTable,
            string[] imports,
            StringOffset[] stringOffsets,
            SourceMapping[] sourceMappings,
            byte[] codeSection,
            FileOffset stringHeapOffset)
        {
            _stream = stream;
            Name = name;
            SourceModificationTime = sourceModificationTime;
            _subroutineOffsets = subroutineOffsets;
            Imports = imports;
            _stringOffsets = stringOffsets;
            _stringHeap = new string?[stringOffsets.Length];
            _subroutines = new Subroutine[_subroutineOffsets.Length];
            _sourceMappings = sourceMappings;
            _codeSection = codeSection;
            _stringHeapOffset = stringHeapOffset;

            int subroutineCount = _subroutines.Length;
            var rtiReader = new BufferReader(rtiTable);
            var rtiEntryOffsets = new int[subroutineCount];
            for (int i = 0; i < subroutineCount; i++)
            {
                rtiEntryOffsets[i] = rtiReader.ReadUInt16LE();
            }

            _srti = new SubroutineRuntimeInfo[subroutineCount];
            _subroutineMap = new Dictionary<string, int>(subroutineCount);
            int rtiStart = rtiReader.Position;
            for (int i = 0; i < subroutineCount; i++)
            {
                rtiReader.Position = rtiStart + rtiEntryOffsets[i];
                var rti = new SubroutineRuntimeInfo(ref rtiReader);
                _srti[i] = rti;
                _subroutineMap[rti.SubroutineName] = i;
            }
        }

        public string Name { get; }
        public string[] Imports { get; }
        public DateTimeOffset SourceModificationTime { get; }
        public ReadOnlySpan<byte> Code => _codeSection;

        public string GetString(ushort token)
        {
            Span<byte> stackBuffer = stackalloc byte[256];
            ref string? s = ref _stringHeap[token];
            if (s is null)
            {
                _stream.Position = _stringHeapOffset + _stringOffsets[token];
                int length = ReadUInt16();
                Span<byte> bytes = length <= stackBuffer.Length
                    ? stackBuffer[..length]
                    : new byte[length];
                _stream.ReadExactly(bytes);
                s = Encoding.UTF8.GetString(bytes);
            }

            return s;
        }

        public Subroutine GetSubroutine(int index)
        {
            ref Subroutine subroutine = ref _subroutines[index];
            if (subroutine.IsEmpty)
            {
                CodeOffset subStart = _subroutineOffsets[index];
                subroutine = new Subroutine(_codeSection, subStart);
            }

            return subroutine;
        }

        public ref readonly SubroutineRuntimeInfo GetSubroutineRuntimeInfo(int subroutineIndex)
            => ref _srti[subroutineIndex];

        public string GetSubroutineName(int subroutineIndex)
            => _srti[subroutineIndex].SubroutineName;

        public bool TryLookupSubroutineIndex(string name, out int index)
            => _subroutineMap.TryGetValue(name, out index);

        public TextSpan? GetSourceSpan(CodeOffset codeOffset)
        {
            int lower = 0;
            int upper = _sourceMappings.Length - 1;

            while (lower <= upper)
            {
                int index = lower + ((upper - lower) / 2);
                ref readonly SourceMapping mapping = ref _sourceMappings[index];

                if (codeOffset >= mapping.BytecodeSpan.Start &&
                    codeOffset < mapping.BytecodeSpan.End)
                {
                    return mapping.SourceSpan;
                }

                if (codeOffset < mapping.BytecodeSpan.Start)
                {
                    upper = index - 1;
                }
                else
                {
                    lower = index + 1;
                }
            }

            return null;
        }

        private ushort ReadUInt16()
        {
            Span<byte> bytes = stackalloc byte[2];
            _stream.ReadExactly(bytes);
            return BinaryPrimitives.ReadUInt16LittleEndian(bytes);
        }

        private unsafe struct TableHeader
        {
            public fixed byte Marker[4];
            public int TableSize;
        }

        public static long GetSourceModificationTime(Stream stream)
        {
            Debug.Assert(stream.Position == 0);
            Span<byte> nsxHeader = stackalloc byte[NsxConstants.NsxHeaderSize];
            stream.ReadExactly(nsxHeader);
            var headerReader = new BufferReader(nsxHeader);
            headerReader.Position += 4;
            return headerReader.ReadInt64LE();
        }

        public static NsxModule LoadModule(Stream stream, string name)
        {
            Span<byte> header = stackalloc byte[NsxConstants.NsxHeaderSize];
            stream.ReadExactly(header);

            var reader = new BufferReader(header);
            ReadOnlySpan<byte> magic = reader.Consume(4);
            long unixTimestamp = reader.ReadInt64LE();
            var modificationTime = DateTimeOffset.FromUnixTimeSeconds(unixTimestamp);
            FileOffset subTableOffset = reader.ReadInt32LE();
            FileOffset rtiTableOffset = reader.ReadInt32LE();
            FileOffset impTableOffset = reader.ReadInt32LE();
            FileOffset strTableOffset = reader.ReadInt32LE();
            FileOffset dbgTableOffset = reader.ReadInt32LE();
            FileOffset codeSectionOffset = reader.ReadInt32LE();
            FileOffset stringHeapOffset = reader.ReadInt32LE();

            int codeSectionSize = stringHeapOffset - codeSectionOffset;
            stream.Position = codeSectionOffset;
            var codeSectionBytes = new byte[codeSectionSize];
            stream.ReadExactly(codeSectionBytes);

            stream.Position = subTableOffset;
            TableHeader subHeader = readTableHeader(stream);
            assertMarker(ref subHeader, NsxConstants.SubTableMarker);
            var subTableBytes = new byte[subHeader.TableSize];
            stream.ReadExactly(subTableBytes);
            reader = new BufferReader(subTableBytes);
            int subCount = reader.ReadUInt16LE();
            var subroutineOffsets = new CodeOffset[subCount];
            for (int i = 0; i < subCount; i++)
            {
                subroutineOffsets[i] = reader.ReadInt32LE();
            }

            stream.Position = rtiTableOffset;
            TableHeader rtiHeader = readTableHeader(stream);
            assertMarker(ref rtiHeader, NsxConstants.RtiTableMarker);
            var rtiBytes = new byte[rtiHeader.TableSize];
            stream.ReadExactly(rtiBytes);

            stream.Position = impTableOffset;
            TableHeader impHeader = readTableHeader(stream);
            assertMarker(ref impHeader, NsxConstants.ImportTableMarker);
            var impBytes = new byte[impHeader.TableSize];
            stream.ReadExactly(impBytes);
            reader = new BufferReader(impBytes);
            int impEntryCount = reader.ReadUInt16LE();
            var imports = new string[impEntryCount];
            for (int i = 0; i < impEntryCount; i++)
            {
                imports[i] = reader.ReadLengthPrefixedUtf8String();
            }

            stream.Position = strTableOffset;
            TableHeader strHeader = readTableHeader(stream);
            assertMarker(ref strHeader, NsxConstants.StringTableMarker);
            var strTableBytes = new byte[strHeader.TableSize];
            stream.ReadExactly(strTableBytes);
            reader = new BufferReader(strTableBytes);
            int stringCount = reader.ReadUInt16LE();
            var stringOffsets = new StringOffset[stringCount];
            for (int i = 0; i < stringCount; i++)
            {
                stringOffsets[i] = reader.ReadInt32LE();
            }

            stream.Position = dbgTableOffset;
            TableHeader dbgHeader = readTableHeader(stream);
            assertMarker(ref dbgHeader, NsxConstants.DebugTableMarker);
            var dbgBytes = new byte[dbgHeader.TableSize];
            stream.ReadExactly(dbgBytes);
            reader = new BufferReader(dbgBytes);
            int dbgEntryCount = reader.ReadUInt16LE();
            var sourceMappings = new SourceMapping[dbgEntryCount];
            for (int i = 0; i < dbgEntryCount; i++)
            {
                int textStart = reader.ReadInt32LE();
                int textLength = reader.ReadInt32LE();
                CodeOffset codeOffset = reader.ReadInt32LE();
                int codeLength = reader.ReadInt32LE();

                sourceMappings[i] = new SourceMapping(
                    new BytecodeSpan(codeOffset, codeLength),
                    new TextSpan(textStart, textLength)
                );
            }

            return new NsxModule(
                stream,
                name,
                modificationTime,
                subroutineOffsets,
                rtiBytes,
                imports,
                stringOffsets,
                sourceMappings,
                codeSectionBytes,
                stringHeapOffset
            );

            static unsafe void assertMarker(ref TableHeader header, ReadOnlySpan<byte> expected)
            {
                fixed (byte* pMarker = &header.Marker[0])
                {
                    var bytes = new Span<byte>(pMarker, 4);
                    Debug.Assert(bytes.SequenceEqual(expected));
                }
            }

            static unsafe TableHeader readTableHeader(Stream stream)
            {
                Span<byte> bytes = stackalloc byte[NsxConstants.TableHeaderSize];
                stream.ReadExactly(bytes);

                TableHeader header;
                bytes[..4].CopyTo(new Span<byte>(header.Marker, 4));
                header.TableSize = BinaryPrimitives.ReadInt32LittleEndian(bytes[4..]);
                return header;
            }
        }
    }

    public readonly struct Subroutine
    {
        private readonly ReadOnlyMemory<byte> _bytecode;

        public bool IsEmpty => _bytecode.IsEmpty;

        public Subroutine(byte[] codeSection, CodeOffset subStart)
        {
            var reader = new BufferReader(codeSection.AsSpan(subStart));
            int size = reader.ReadUInt16LE();
            int dialogueBlockCount = reader.ReadUInt16LE();
            int[] dialogueBlockOffsets = new int[dialogueBlockCount];
            for (int i = 0; i < dialogueBlockCount; i++)
            {
                dialogueBlockOffsets[i] = reader.ReadUInt16LE();
            }

            BytecodeStart = subStart + reader.Position;
            _bytecode = codeSection.AsMemory(BytecodeStart, size);
            var dialogueBlocks = ImmutableArray.CreateBuilder<CompiledDialogueBlock>(dialogueBlockCount);
            for (int i = 0; i < dialogueBlockCount; i++)
            {
                reader.Position = (BytecodeStart - subStart) + dialogueBlockOffsets[i];
                dialogueBlocks.Add(new CompiledDialogueBlock(ref reader));
            }

            DialogueBlocks = dialogueBlocks.ToImmutable();
        }

        public CodeOffset BytecodeStart { get; }
        public ImmutableArray<CompiledDialogueBlock> DialogueBlocks { get; }
    }

    public readonly struct CompiledDialogueBlock
    {
        internal CompiledDialogueBlock(ref BufferReader reader)
        {
            int partCount = reader.ReadByte();
            var parts = ImmutableArray.CreateBuilder<CompiledDialogueBlockPart>(partCount);
            for (int i = 0; i < partCount; i++)
            {
                parts.Add(CompiledDialogueBlockPart.Deserialize(ref reader));
            }

            Parts = parts.ToImmutable();
        }

        public ImmutableArray<CompiledDialogueBlockPart> Parts { get; }
    }

    public abstract class CompiledDialogueBlockPart
    {
        internal enum Kind : byte
        {
            Markup = 0,
            BlankLine = 1,
            CodeBlock = 2
        }

        internal static CompiledDialogueBlockPart Deserialize(ref BufferReader reader)
        {
            var kind = (Kind)reader.ReadByte();
            return kind switch
            {
                Kind.Markup => new Markup(ref reader),
                Kind.BlankLine => BlankLine.Instance,
                Kind.CodeBlock => new CodeBlock(ref reader),
                _ => throw ThrowHelper.UnexpectedValueOf<Kind>()
            };
        }

        public sealed class Markup : CompiledDialogueBlockPart
        {
            private readonly ushort _stringToken;

            internal Markup(ref BufferReader reader)
            {
                _stringToken = reader.ReadUInt16LE();
            }

            public string GetText(NsxModule module) => module.GetString(_stringToken);
        }

        public sealed class BlankLine : CompiledDialogueBlockPart
        {
            internal static readonly BlankLine Instance = new();

            private BlankLine()
            {
            }
        }

        public sealed class CodeBlock : CompiledDialogueBlockPart
        {
            internal CodeBlock(ref BufferReader reader)
            {
                ushort length = reader.ReadUInt16LE();
                Position = reader.Position;
                reader.Position += length;
            }

            internal int Position { get; }
        }
    }

    [DebuggerDisplay("{SubroutineKind} '{SubroutineName}'")]
    public struct SubroutineRuntimeInfo
    {
        private string[]? _parameterNames;
        private readonly Dictionary<string, int>? _dialogueBlockMap;

        public readonly SubroutineKind SubroutineKind;
        public readonly string SubroutineName;
        public readonly (string box, string name)[] DialogueBlockInfos;

        public int HeaderSize => 2 + 2 + (DialogueBlockInfos.Length * 2);

        internal SubroutineRuntimeInfo(ref BufferReader reader)
        {
            SubroutineKind = (SubroutineKind)reader.ReadByte();
            SubroutineName = reader.ReadLengthPrefixedUtf8String();
            int dialogueBlockCount = reader.ReadUInt16LE();
            DialogueBlockInfos = dialogueBlockCount > 0
                ? new (string, string)[dialogueBlockCount]
                : [];

            _dialogueBlockMap = null;
            if (dialogueBlockCount > 0)
            {
                _dialogueBlockMap = new Dictionary<string, int>(dialogueBlockCount);
                for (int i = 0; i < dialogueBlockCount; i++)
                {
                    (string box, string name) info =
                        (reader.ReadLengthPrefixedUtf8String(),
                         reader.ReadLengthPrefixedUtf8String());
                    DialogueBlockInfos[i] = info;
                    _dialogueBlockMap[info.name] = i;
                }
            }

            _parameterNames = null;
        }

        internal readonly int LookupDialogueBlockIndex(string dialogueBlockName)
        {
            Debug.Assert(_dialogueBlockMap is not null);
            return _dialogueBlockMap[dialogueBlockName];
        }

        internal string[] GetParameterNames(byte[] rtiTable)
        {
            if (SubroutineKind != SubroutineKind.Function)
            {
                return [];
            }

            if (_parameterNames is null)
            {
                DecodeParameterNames(rtiTable);
                Debug.Assert(_parameterNames is not null);
            }

            return _parameterNames;
        }

        private void DecodeParameterNames(byte[] rtiTable)
        {
            Debug.Assert(SubroutineKind == SubroutineKind.Function);
            var reader = new BufferReader(rtiTable);
            int parameterCount = reader.ReadByte();
            _parameterNames = parameterCount > 0
                ? new string[parameterCount]
                : [];
            for (int i = 0; i < parameterCount; i++)
            {
                _parameterNames[i] = reader.ReadLengthPrefixedUtf8String();
            }
        }
    }
}
