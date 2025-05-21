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
        private readonly Stream _stream;
        private readonly int[] _stringOffsets;
        private readonly string?[] _stringHeap;

        private readonly int[] _subroutineOffsets;
        private readonly Subroutine[] _subroutines;
        private readonly SourceMapping[] _sourceMappings;
        private readonly SubroutineRuntimeInfo[] _srti;
        private readonly Dictionary<string, int> _subroutineMap;

        private NsxModule(
            Stream stream, string name, DateTimeOffset sourceModificationTime,
            int[] subroutineOffsets, byte[] rtiTable, string[] imports, int[] stringOffsets, SourceMapping[] sourceMappings)
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

        internal Subroutine GetSubroutine(int index)
        {
            ref Subroutine subroutine = ref _subroutines[index];
            if (subroutine.IsEmpty)
            {
                LoadSubroutine(index);
            }

            return subroutine;
        }

        public ref readonly SubroutineRuntimeInfo GetSubroutineRuntimeInfo(int subroutineIndex)
            => ref _srti[subroutineIndex];

        public string GetSubroutineName(int subroutineIndex)
            => _srti[subroutineIndex].SubroutineName;

        public string GetString(ushort token)
        {
            Span<byte> stackBuffer = stackalloc byte[256];
            ref string? s = ref _stringHeap[token];
            if (s is null)
            {
                _stream.Position = _stringOffsets[token];
                int length = ReadUInt16();
                Span<byte> bytes = length <= stackBuffer.Length
                    ? stackBuffer[..length]
                    : new byte[length];
                _stream.ReadExactly(bytes);
                s = Encoding.UTF8.GetString(bytes);

            }

            return s;
        }

        public bool TryLookupSubroutineIndex(string name, out int index)
            => _subroutineMap.TryGetValue(name, out index);

        public int LookupSubroutineIndex(string name)
            => _subroutineMap[name];

        public SourceLocation? GetSourceLocation(int codeOffset)
        {
            int lower = 0;
            int upper = _sourceMappings.Length - 1;

            while (lower <= upper)
            {
                int index = lower + ((upper - lower) / 2);
                ref readonly SourceMapping mapping = ref _sourceMappings[index];

                if (codeOffset >= mapping.BytecodeLocation.Start && codeOffset < mapping.BytecodeLocation.End)
                {
                    return mapping.SourceLocation;
                }

                if (codeOffset < mapping.BytecodeLocation.Start)
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

        private void LoadSubroutine(int index)
        {
            _stream.Position = _subroutineOffsets[index];
            int size = ReadUInt16();
            var bytes = new byte[size];
            _stream.ReadExactly(bytes);
            _subroutines[index] = new Subroutine(bytes);
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
            static unsafe TableHeader readTableHeader(Stream stream)
            {
                Span<byte> bytes = stackalloc byte[6];
                stream.ReadExactly(bytes);

                TableHeader header;
                bytes[..4].CopyTo(new Span<byte>(header.Marker, 4));
                header.TableSize = BinaryPrimitives.ReadUInt16LittleEndian(bytes[4..]);
                return header;
            }

            static unsafe void assertMarker(ref TableHeader header, ReadOnlySpan<byte> expected)
            {
                fixed (byte* pMarker = &header.Marker[0])
                {
                    var bytes = new Span<byte>(pMarker, 4);
                    Debug.Assert(bytes.SequenceEqual(expected));
                }
            }

            Span<byte> header = stackalloc byte[NsxConstants.NsxHeaderSize];
            stream.ReadExactly(header);

            var reader = new BufferReader(header);
            ReadOnlySpan<byte> magic = reader.Consume(4);
            long unixTimestamp = reader.ReadInt64LE();
            var modificationTime = DateTimeOffset.FromUnixTimeSeconds(unixTimestamp);
            _ = reader.ReadInt32LE();
            int rtiTableOffset = reader.ReadInt32LE();

            TableHeader subHeader = readTableHeader(stream);
            assertMarker(ref subHeader, NsxConstants.SubTableMarker);
            var subTableBytes = new byte[subHeader.TableSize];
            stream.ReadExactly(subTableBytes);

            var buf = new byte[16];
            stream.ReadExactly(buf, offset: 0, count: 16);

            reader = new BufferReader(subTableBytes);
            int subCount = reader.ReadUInt16LE();
            var subroutineOffsets = new int[subCount];
            for (int i = 0; i < subCount; i++)
            {
                subroutineOffsets[i] = reader.ReadInt32LE();
            }

            stream.Position = rtiTableOffset;
            TableHeader rtiHeader = readTableHeader(stream);
            assertMarker(ref rtiHeader, NsxConstants.RtiTableMarker);
            var rtiBytes = new byte[rtiHeader.TableSize];
            stream.ReadExactly(rtiBytes);

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

            TableHeader strHeader = readTableHeader(stream);
            assertMarker(ref strHeader, NsxConstants.StringTableMarker);
            var strTableBytes = new byte[strHeader.TableSize];
            stream.ReadExactly(strTableBytes);
            reader = new BufferReader(strTableBytes);
            int stringCount = reader.ReadUInt16LE();
            var stringOffsets = new int[stringCount];
            for (int i = 0; i < stringCount; i++)
            {
                stringOffsets[i] = reader.ReadInt32LE();
            }

            TableHeader dbgHeader = readTableHeader(stream);
            assertMarker(ref dbgHeader, NsxConstants.DebugTableMarker);
            var dbgBytes = new byte[dbgHeader.TableSize];
            stream.ReadExactly(dbgBytes);
            reader = new BufferReader(dbgBytes);
            int dbgEntryCount = reader.ReadUInt16LE();
            var sourceMappings = new SourceMapping[dbgEntryCount];
            for (int i = 0; i < dbgEntryCount; i++)
            {
                int line = reader.ReadInt32LE();
                int column = reader.ReadUInt16LE();
                int textLength = reader.ReadUInt16LE();
                int codeOffset = reader.ReadUInt16LE();
                int codeLength = reader.ReadUInt16LE();
                sourceMappings[i] = new SourceMapping(
                    new BytecodeLocation(codeOffset, codeLength),
                    new SourceLocation(line, column, textLength)
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
                sourceMappings
            );
        }
    }

    internal readonly struct Subroutine
    {
        private readonly byte[] _bytes;

        public bool IsEmpty => _bytes is null;

        public Subroutine(byte[] bytes)
        {
            _bytes = bytes;
            var reader = new BufferReader(bytes);
            int dialogueBlockCount = reader.ReadUInt16LE();
            int[] dialogueBlockOffsets = new int[dialogueBlockCount];
            for (int i = 0; i < dialogueBlockCount; i++)
            {
                dialogueBlockOffsets[i] = reader.ReadUInt16LE();
            }
            int start = reader.Position;
            var dialogueBlocks = ImmutableArray.CreateBuilder<CompiledDialogueBlock>(dialogueBlockCount);
            for (int i = 0; i < dialogueBlockCount; i++)
            {
                reader.Position = start + dialogueBlockOffsets[i];
                dialogueBlocks.Add(new CompiledDialogueBlock(ref reader));
            }

            EntryPoint = start;
            DialogueBlocks = dialogueBlocks.ToImmutable();
        }

        public int EntryPoint { get; }

        public ImmutableArray<CompiledDialogueBlock> DialogueBlocks { get; }

        public ReadOnlySpan<byte> Code => _bytes;
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
            CodeBlock = 1,
        }

        internal static CompiledDialogueBlockPart Deserialize(ref BufferReader reader)
        {
            var kind = (Kind)reader.ReadByte();
            return kind switch
            {
                Kind.Markup => new Markup(ref reader),
                Kind.CodeBlock => new CodeBlock(ref reader),
                _ => ThrowHelper.Unreachable<CompiledDialogueBlockPart>()
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
