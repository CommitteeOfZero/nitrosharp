using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NitroSharp.NsScript.Utilities;

namespace NitroSharp.NsScript.VM
{
    internal sealed class GlobalVarLookupTable
    {
        private readonly Stream _stream;
        private readonly int[] _nameOffsets;
        private readonly int _nameHeapStart;
        private readonly Dictionary<string, int> _systemVariables;

        private GlobalVarLookupTable(
            Stream stream,
            int[] nameOffsets,
            int[] systemVarIndices,
            int nameHeapStart)
        {
            _stream = stream;
            _nameOffsets = nameOffsets;
            _nameHeapStart = nameHeapStart;
            _systemVariables = new Dictionary<string, int>(systemVarIndices.Length);
            foreach (int idx in systemVarIndices)
            {
                _systemVariables.Add(ReadString(idx), idx);
            }
        }

        public static GlobalVarLookupTable Load(Stream stream)
        {
            int entryCount = ReadUInt16(stream);
            var offsetTable = new byte[entryCount * 4];
            stream.Read(offsetTable);
            var offsetReader = new BufferReader(offsetTable);
            var nameOffsets = new int[entryCount];
            for (int i = 0; i < entryCount; i++)
            {
                nameOffsets[i] = offsetReader.ReadInt32LE();
            }

            int sysVarCount = ReadUInt16(stream);
            var sysVarList = new byte[sysVarCount * 2];
            stream.Read(sysVarList);
            var sysVarListReader = new BufferReader(sysVarList);
            var sysVarIndices = new int[sysVarCount];
            for (int i = 0; i < sysVarCount; i++)
            {
                sysVarIndices[i] = sysVarListReader.ReadUInt16LE();
            }

            int nameHeapStart = (int)stream.Position;
            return new GlobalVarLookupTable(stream, nameOffsets, sysVarIndices, nameHeapStart);
        }

        public bool TryLookupSystemVariable(string name, out int index)
            => _systemVariables.TryGetValue(name, out index);

        private static int ReadUInt16(Stream stream)
        {
            Span<byte> bytes = stackalloc byte[2];
            stream.Read(bytes);
            return BinaryPrimitives.ReadUInt16LittleEndian(bytes);
        }

        private string ReadString(int index)
        {
            _stream.Position = _nameHeapStart + _nameOffsets[index];
            int length = ReadUInt16(_stream);
            Span<byte> bytes = length <= 1024
                ? stackalloc byte[length]
                : new byte[length];
            _stream.Read(bytes);
            return Encoding.UTF8.GetString(bytes);
        }
    }
}
