﻿using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using NitroSharp.NsScriptNew;

namespace NitroSharp.NsScriptCompiler.Playground
{
    public struct SubroutineRuntimeInformation
    {
        private readonly int _offset;
        private readonly int _parameterInfoOffset;
        private string[]? _parameterNames;

        public readonly SubroutineKind SubroutineKind;
        public readonly string SubroutineName;
        public readonly string[] DialogueBlockNames;

        internal SubroutineRuntimeInformation(ref BufferReader reader)
        {
            _offset = reader.Position;
            SubroutineKind = (SubroutineKind)reader.ReadByte();
            SubroutineName = reader.ReadLengthPrefixedUtf8String();
            int dialogueBlockCount = reader.ReadUInt16LE();
            DialogueBlockNames = dialogueBlockCount > 0
                ? new string[dialogueBlockCount]
                : Array.Empty<string>();

            for (int i = 0; i < dialogueBlockCount; i++)
            {
                DialogueBlockNames[i] = reader.ReadLengthPrefixedUtf8String();
            }

            _parameterInfoOffset = reader.Position;
            _parameterNames = null;
        }

        internal string[] GetParameterNames(byte[] rtiTable)
        {
            if (SubroutineKind != SubroutineKind.Function)
            {
                return Array.Empty<string>();
            }

            if (_parameterNames == null)
            {
                DecodeParameterNames(rtiTable);
                Debug.Assert(_parameterNames != null);
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
                : Array.Empty<string>();
            for (int i = 0; i < parameterCount; i++)
            {
                _parameterNames[i] = reader.ReadLengthPrefixedUtf8String();
            }
        }

        public override string ToString()
            => $"SRTI '{SubroutineName}'";
    }

    public class NsxModule
    {
        private readonly string?[] _stringHeap;

        public int StringCount { get; }

        private readonly Stream _stream;
        private readonly int[] _subroutineOffsets;

        public string[] Imports { get; }

        private readonly byte[] _rtiTable;
        private readonly int[] _stringOffsets;
        private readonly Subroutine[] _subroutines;
        private readonly SubroutineRuntimeInformation[] _srti;
        private readonly Dictionary<string, int> _subroutineMap;

        public NsxModule(Stream stream, int[] subroutineOffsets, byte[] rtiTable, string[] imports, int[] stringOffsets)
        {
            _stream = stream;
            _subroutineOffsets = subroutineOffsets;
            Imports = imports;
            _stringOffsets = stringOffsets;
            _subroutines = new Subroutine[_subroutineOffsets.Length];
            _stringHeap = new string?[stringOffsets.Length];

            StringCount = stringOffsets.Length;

            _rtiTable = rtiTable;
            int subroutineCount = _subroutines.Length;
            var rtiReader = new BufferReader(rtiTable);
            var rtiEntryOffsets = new int[subroutineCount];
            for (int i = 0; i < subroutineCount; i++)
            {
                rtiEntryOffsets[i] = rtiReader.ReadUInt16LE();
            }

            _srti = new SubroutineRuntimeInformation[subroutineCount];
            _subroutineMap = new Dictionary<string, int>(subroutineCount);
            int rtiStart = rtiReader.Position;
            for (int i = 0; i < subroutineCount; i++)
            {
                rtiReader.Position = rtiStart + rtiEntryOffsets[i];
                var rti = new SubroutineRuntimeInformation(ref rtiReader);
                _srti[i] = rti;
                _subroutineMap[rti.SubroutineName] = i;
            }
        }

        public Subroutine GetSubroutine(int index)
        {
            ref Subroutine subroutine = ref _subroutines[index];
            if (subroutine.IsEmpty)
            {
                LoadSubroutine(index);
            }

            return subroutine;
        }

        public ref readonly SubroutineRuntimeInformation GetSubroutineRuntimeInformation(
            int subroutineIndex)
        {
            return ref _srti[subroutineIndex];
        }

        public string[] GetParameterNames(int subroutineIndex)
        {
            return _srti[subroutineIndex].GetParameterNames(_rtiTable);
        }

        public string GetString(ushort token)
        {
            ref string? s = ref _stringHeap[token];
            if (s == null)
            {
                _stream.Position = _stringOffsets[token];
                int length = ReadUInt16();
                Span<byte> bytes = length <= 1024
                    ? stackalloc byte[length]
                    : new byte[length];
                _stream.Read(bytes);
                s = Encoding.UTF8.GetString(bytes);
                Debug.Assert(s != null);
            }

            return s;
        }

        public int LookupSubroutineIndex(string name)
        {
            return _subroutineMap[name];
        }

        private void LoadSubroutine(int index)
        {
            _stream.Position = _subroutineOffsets[index];
            int size = ReadUInt16();
            var bytes = new byte[size];
            _stream.Read(bytes);
            _subroutines[index] = new Subroutine(index, bytes);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ushort ReadUInt16()
        {
            Span<byte> bytes = stackalloc byte[2];
            _stream.Read(bytes);
            return BinaryPrimitives.ReadUInt16LittleEndian(bytes);
        }

        public readonly struct Subroutine
        {
            private readonly byte[] _bytes;
            private readonly int _codeStart;
            private readonly int _codeSize;
            private readonly int[] _dialogueBlockOffsets;

            public readonly int Index;
            public bool IsEmpty => _bytes == null;

            public Subroutine(int index, byte[] bytes)
            {
                Index = index;
                _bytes = bytes;
                var reader = new BufferReader(bytes);
                int dialogoueBlockCount = reader.ReadUInt16LE();
                _dialogueBlockOffsets = Array.Empty<int>();
                int codeEnd = bytes.Length;
                if (dialogoueBlockCount > 0)
                {
                    _dialogueBlockOffsets = new int[dialogoueBlockCount];
                    for (int i = 0; i < dialogoueBlockCount; i++)
                    {
                        _dialogueBlockOffsets[i] = reader.ReadUInt16LE();
                    }

                    codeEnd = _dialogueBlockOffsets[0];
                }

                _codeStart = reader.Position;
                _codeSize = codeEnd - _codeStart;
            }

            public ReadOnlySpan<byte> Code
                => new ReadOnlySpan<byte>(_bytes, _codeStart, _codeSize);
        }

        private unsafe struct TableHeader
        {
            public fixed byte Marker[4];
            public int TableSize; 
        }

        public static NsxModule LoadModule(Stream stream)
        {
            static unsafe TableHeader readTableHeader(Stream stream)
            {
                Span<byte> bytes = stackalloc byte[6];
                stream.Read(bytes);

                TableHeader header;
                bytes.Slice(0, 4).CopyTo(new Span<byte>(header.Marker, 4));
                header.TableSize = BinaryPrimitives.ReadUInt16LittleEndian(bytes.Slice(4));
                return header;
            }

            static unsafe void assertMarker(ref TableHeader header, ReadOnlySpan<byte> expected)
            {
                //Span<byte> correct = stackalloc byte[4];
                //Encoding.UTF8.GetBytes(marker, correct);

                fixed (byte* pMarker = &header.Marker[0])
                {
                    var bytes = new Span<byte>(pMarker, 4);
                    Debug.Assert(bytes.SequenceEqual(expected));
                }
            }

            Span<byte> header = stackalloc byte[20];
            stream.Read(header);

            var reader = new BufferReader(header);
            ReadOnlySpan<byte> magic = reader.Consume(4);
            int rtiTableOffset = reader.ReadInt32LE();
            int stringTableOffset = reader.ReadInt32LE();
            int codeOffset = reader.ReadInt32LE();

            TableHeader subHeader = readTableHeader(stream);
            assertMarker(ref subHeader, NsxConstants.SubTableMarker);
            var subTableBytes = new byte[subHeader.TableSize];
            stream.Read(subTableBytes);
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
            stream.Read(rtiBytes);

            TableHeader impHeader = readTableHeader(stream);
            assertMarker(ref impHeader, NsxConstants.ImportTableMarker);
            var impBytes = new byte[impHeader.TableSize];
            stream.Read(impBytes);
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
            stream.Read(strTableBytes);
            reader = new BufferReader(strTableBytes);
            int stringCount = reader.ReadUInt16LE();
            var stringOffsets = new int[stringCount];
            for (int i = 0; i < stringCount; i++)
            {
                stringOffsets[i] = reader.ReadInt32LE();
            }

            return new NsxModule(stream, subroutineOffsets, rtiBytes, imports, stringOffsets);
        }
    }
}