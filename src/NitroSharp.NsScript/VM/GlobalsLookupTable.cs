using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Text;
using System.Threading;
using NitroSharp.NsScript.Utilities;

namespace NitroSharp.NsScript.VM
{
    internal sealed class GlobalsLookupTable
    {
        private readonly Stream _stream;
        private readonly int[] _varOffsets;
        private readonly int[] _flagOffsets;
        private readonly int _nameHeapStart;
        private readonly Dictionary<string, int> _sysVariables;
        private readonly Dictionary<string, int> _sysFlags;

        private readonly Lazy<ImmutableDictionary<string, int>> _allVariables;
        private readonly Lazy<ImmutableDictionary<string, int>> _allFlags;

        private GlobalsLookupTable(
            Stream stream,
            int[] varOffsets,
            int[] sysVars,
            int[] flagOffsets,
            int[] sysFlags,
            int nameHeapStart)
        {
            _stream = stream;
            _varOffsets = varOffsets;
            _flagOffsets = flagOffsets;
            _nameHeapStart = nameHeapStart;
            _sysVariables = new Dictionary<string, int>(sysVars.Length);
            _sysFlags = new Dictionary<string, int>();
            foreach (int idx in sysVars)
            {
                _sysVariables.Add(ReadString(_varOffsets, idx), idx);
            }
            foreach (int idx in sysFlags)
            {
                _sysFlags.Add(ReadString(_flagOffsets, idx), idx);
            }

            _allVariables = new Lazy<ImmutableDictionary<string, int>>(CreateLookup(_varOffsets));
            _allFlags = new Lazy<ImmutableDictionary<string, int>>(CreateLookup(_flagOffsets));
        }

        public ImmutableDictionary<string, int> Variables => _allVariables.Value;
        public ImmutableDictionary<string, int> Flags => _allFlags.Value;

        private ImmutableDictionary<string, int> CreateLookup(int[] nameOffsets)
        {
            var lookup = ImmutableDictionary.CreateBuilder<string, int>();
            for (int i = 0 ; i < nameOffsets.Length; i++)
            {
                lookup.Add(ReadString(nameOffsets, i), i);
            }
            return lookup.ToImmutable();
        }

        //public string[] ReadVariableNames()
        //{
        //    var array = new string[_varOffsets.Length];
        //    for (int i = 0; i < _varOffsets.Length; i++)
        //    {
        //        array[i] = ReadString(_varOffsets, i);
        //    }
        //    return array;
        //}
        //
        //public string[] ReadFlagNames()
        //{
        //    var array = new string[_flagOffsets.Length];
        //    for (int i = 0; i < _flagOffsets.Length; i++)
        //    {
        //        array[i] = ReadString(_flagOffsets, i);
        //    }
        //    return array;
        //}

        public static GlobalsLookupTable Load(Stream stream)
        {
            (int[] varOffsets, int[] sysVars) = ReadTable(stream);
            (int[] flagOffsets, int[] sysFlags) = ReadTable(stream);
            int nameHeapStart = (int)stream.Position;
            return new GlobalsLookupTable(
                stream,
                varOffsets, sysVars,
                flagOffsets, sysFlags,
                nameHeapStart
            );
        }

        private static (int[] offets, int[] indices) ReadTable(Stream stream)
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

            int sysCount = ReadUInt16(stream);
            var sysList = new byte[sysCount * 2];
            stream.Read(sysList);
            var sysListReader = new BufferReader(sysList);
            var sysIndices = new int[sysCount];
            for (int i = 0; i < sysCount; i++)
            {
                sysIndices[i] = sysListReader.ReadUInt16LE();
            }

            return (nameOffsets, sysIndices);
        }

        public bool TryLookupSystemFlag(string name, out int index)
            => _sysFlags.TryGetValue(name, out index);

        public bool TryLookupSystemVariable(string name, out int index)
            => _sysVariables.TryGetValue(name, out index);

        private static int ReadUInt16(Stream stream)
        {
            Span<byte> bytes = stackalloc byte[2];
            stream.Read(bytes);
            return BinaryPrimitives.ReadUInt16LittleEndian(bytes);
        }

        private string ReadString(int[] table, int index)
        {
            _stream.Position = _nameHeapStart + table[index];
            int length = ReadUInt16(_stream);
            Span<byte> bytes = length <= 1024
                ? stackalloc byte[length]
                : new byte[length];
            _stream.Read(bytes);
            return Encoding.UTF8.GetString(bytes);
        }
    }
}
