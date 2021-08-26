using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Text;

namespace NitroSharp.Content
{
    internal sealed class NpaFile : ArchiveFile
    {
        private static ReadOnlySpan<byte> Magic => new byte[] { 0x4E, 0x50, 0x41, 0x01, 0x00, 0x00, 0x00 };

        private readonly uint _dataStart;
        private readonly Dictionary<string, (uint offset, uint size)> _entries;
        private readonly MemoryMappedFile _file;

        private NpaFile(
            MemoryMappedFile file,
            uint dataStart,
            Dictionary<string, (uint offset, uint size)> entries)
        {
            _entries = entries;
            _file = file;
            _dataStart = dataStart;
        }

        public static NpaFile? TryLoad(MemoryMappedFile mmFile, Encoding encoding)
        {
            using MemoryMappedViewStream stream = mmFile
                .CreateViewStream(0, 0, MemoryMappedFileAccess.Read);
            var reader = new BinaryReader(stream);
            Span<byte> magic = stackalloc byte[7];
            reader.Read(magic);
            if (!magic.SequenceEqual(Magic))
            {
                return null;
            }

            uint key1 = reader.ReadUInt32();
            uint key2 = reader.ReadUInt32();

            bool compressed = Convert.ToBoolean(reader.ReadByte());
            if (compressed)
            {
                throw new ArchiveException("NPA", "Compression is not supported");
            }
            bool encryped = Convert.ToBoolean(reader.ReadByte());
            if (encryped)
            {
                throw new ArchiveException("NPA", "Data encryption is not supported");
            }

            uint entryCount = reader.ReadUInt32();
            uint folderCount = reader.ReadUInt32();
            uint fileCount = reader.ReadUInt32();
            if (entryCount != folderCount + fileCount)
            {
                throw new ArchiveException("NPA", "entriesCount != folderCount + fileCount");
            }

            ulong zero = reader.ReadUInt64();
            if (zero != 0)
            {
                throw new ArchiveException("NPA", "Expected 8 null bytes");
            }

            uint dataStart = reader.ReadUInt32();

            var entries = new Dictionary<string, (uint offset, uint size)>();
            for (uint fileIndex = 0; fileIndex < entryCount; fileIndex++)
            {
                (string, (uint, uint))? fileDetails = ReadFileEntry(reader, fileIndex, (key1, key2), encoding);
                if (fileDetails is not null)
                {
                    (string fileName, (uint offset, uint size)) = fileDetails.Value;
                    entries[fileName] = (offset, size);
                }
            }

            return new NpaFile(mmFile, dataStart, entries);
        }

        public override Stream OpenStream(string path)
        {
            if (!Contains(path))
            {
                throw new FileNotFoundException("File not found in the NPA archive", path);
            }
            (uint offset, uint size) = _entries[path];
            // The header is 41 bytes long
            offset += _dataStart + 41;
            return _file.CreateViewStream(offset, size, MemoryMappedFileAccess.Read);
        }

        public override bool Contains(string path)
        {
            return _entries.ContainsKey(path);
        }

        private static byte Crypt(uint charIndex, uint fileIndex, (uint, uint) keys)
        {
            uint key = 0xFC * charIndex;
            uint keysMultiplied = keys.Item1 * keys.Item2;

            key -= keysMultiplied >> 0x18;
            key -= keysMultiplied >> 0x10;
            key -= keysMultiplied >> 0x08;
            key -= keysMultiplied  & 0xff;

            key -= fileIndex >> 0x18;
            key -= fileIndex >> 0x10;
            key -= fileIndex >> 0x08;
            key -= fileIndex;

            return (byte) key;
        }

        private static (string name, (uint offset, uint size))? ReadFileEntry(
            BinaryReader reader,
            uint fileIndex,
            (uint, uint) keys,
            Encoding encoding)
        {
            uint nameLength = reader.ReadUInt32();
            Span<byte> nameBytes = nameLength < 256
                ? stackalloc byte[(int)nameLength]
                : new byte[nameLength];
            reader.Read(nameBytes);
            for (int i = 0; i < nameLength; i++)
            {
                nameBytes[i] += Crypt((uint)i, fileIndex, keys);
            }
            string name = encoding.GetString(nameBytes);

            byte fileType = reader.ReadByte();
            uint id = reader.ReadUInt32();
            uint offset = reader.ReadUInt32();
            uint compressedSize = reader.ReadUInt32();
            uint originalSize = reader.ReadUInt32();
            // We ignore folders
            if (fileType == 2)
            {
                if (compressedSize != originalSize)
                {
                    throw new ArchiveException("NPA", "Compression is not supported");
                }
                // NPA uses '\' but NitroSharp uses '/'
                // This shouldn't break if there is no magic escape character I don't know about
                // and no file name contains a '/'
                name = name.Replace("\\", "/").ToLowerInvariant();
                return (name, (offset, originalSize));
            }

            return null;
        }

        public override void Dispose()
        {
            _file.Dispose();
        }
    }
}
