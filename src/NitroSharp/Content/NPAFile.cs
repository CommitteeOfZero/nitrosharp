using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;

namespace NitroSharp.Content
{
    internal sealed class NPAFile : IArchiveFile
    {
        public static readonly byte[] Magic = {0x4E, 0x50, 0x41, 0x01, 0x00, 0x00, 0x00};

        private readonly uint _dataStart;
        private readonly Dictionary<string, (uint offset, uint size)> _files;

        private readonly MemoryMappedFile _mmFile;

        private readonly Encoding _encoding;

        private NPAFile(MemoryMappedFile mmFile, Encoding encoding, uint dataStart, Dictionary<string, (uint offset, uint size)> files)
        {
            _files = files;
            _mmFile = mmFile;
            _encoding = encoding;
            _dataStart = dataStart;
        }

        public static IArchiveFile Load(MemoryMappedFile mmFile, Encoding encoding)
        {
            return TryLoad(mmFile, encoding) is NPAFile file
                ? file
                : throw new ArchiveException("NPA", "Unknown magic");
        }

        public void Dispose()
        {
            _mmFile.Dispose();
        }

        public static IArchiveFile? TryLoad(MemoryMappedFile mmFile, Encoding encoding)
        {
            using (MemoryMappedViewStream stream = mmFile.CreateViewStream(0, 0, MemoryMappedFileAccess.Read))
            {
                // BinaryReader should always be in little endian
                BinaryReader reader = new BinaryReader(stream);

                byte[] magic = new byte[7];
                reader.Read(magic, 0, 7);
                if (!magic.SequenceEqual(NPAFile.Magic))
                {
                    return null;
                }

                uint key1 = reader.ReadUInt32();
                uint key2 = reader.ReadUInt32();

                bool compression = System.Convert.ToBoolean(reader.ReadByte());
                if (compression)
                {
                    throw new ArchiveException("NPA", "Compression is not supported");
                }
                bool encryption = System.Convert.ToBoolean(reader.ReadByte());
                if (encryption)
                {
                    throw new ArchiveException("NPA", "Data encryption is not supported");
                }

                uint entriesCount = reader.ReadUInt32();
                uint folderCount = reader.ReadUInt32();
                uint fileCount = reader.ReadUInt32();
                if (entriesCount != folderCount + fileCount)
                {
                    throw new ArchiveException("NPA", "entriesCount != folderCount + fileCount");
                }

                ulong zero = reader.ReadUInt64();
                if (zero != 0)
                {
                    throw new ArchiveException("NPA", "Expected 8 null bytes");
                }

                uint dataStart = reader.ReadUInt32();

                Dictionary<string, (uint offset, uint size)> files = new Dictionary<string, (uint offset, uint size)>();
                for (uint fileIndex = 0; fileIndex < entriesCount; fileIndex++)
                {
                    (string, (uint, uint))? fileDetails = ReadFileEntry(reader, fileIndex, (key1, key2), encoding);
                    if (fileDetails != null)
                    {
                        (string fileName, (uint offset, uint size)) = fileDetails.Value;
                        files[fileName] = (offset, size);
                    }
                }

                return new NPAFile(mmFile, encoding, dataStart, files);
            }
        }

        public Stream OpenStream(string path)
        {
            if (!Contains(path))
            {
                throw new FileNotFoundException("File not found in the NPA archive", path);
            }
            (uint offset, uint size) = _files[path];
            // The header is 41 bytes long
            offset += _dataStart + 41;
            return _mmFile.CreateViewStream(offset, size, MemoryMappedFileAccess.Read);
        }

        public bool Contains(string path)
        {
            return _files.ContainsKey(path);
        }

        private static byte crypt(uint charIndex, uint fileIndex, (uint, uint) keys)
        {
            uint key = 0xFC * charIndex;
            uint keys_multiplied = keys.Item1 * keys.Item2;

            key -= keys_multiplied >> 0x18;
            key -= keys_multiplied >> 0x10;
            key -= keys_multiplied >> 0x08;
            key -= keys_multiplied  & 0xff;

            key -= fileIndex >> 0x18;
            key -= fileIndex >> 0x10;
            key -= fileIndex >> 0x08;
            key -= fileIndex;

            return (byte) key;
        }

        private static (string name, (uint offset, uint size))? ReadFileEntry(BinaryReader reader, uint fileIndex, (uint, uint) keys, Encoding encoding)
        {
            uint nameLength = reader.ReadUInt32();

            byte[] name = new byte[nameLength];
            reader.Read(name, 0, (int) nameLength);
            for (uint charIndex = 0; charIndex < nameLength; charIndex++)
            {
                name[charIndex] += crypt(charIndex, fileIndex, keys);
            }
            string decodedName = encoding.GetString(name);

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
                decodedName = decodedName.Replace("\\", "/");
                decodedName = decodedName.ToLowerInvariant();
                return (decodedName, (offset, originalSize));
            }
            return null;
        }
    }
}
