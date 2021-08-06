using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;

namespace NitroSharp.Content
{
    internal class NPAFile : IArchiveFile
    {
        public static readonly byte[] Magic = {0x4E, 0x50, 0x41, 0x01, 0x00, 0x00, 0x00};

        private uint _key1, _key2;
        private uint _dataStart;
        private uint _entriesCount;
        private readonly Dictionary<string, (uint offset, uint size)> _files;

        private readonly MemoryMappedFile _mmFile;

        private bool _disposed = false;

        private readonly Encoding _encoding;

        public NPAFile(MemoryMappedFile mmFile, Encoding encoding)
        {
            _files = new Dictionary<string, (uint offset, uint size)>();
            _mmFile = mmFile;
            _encoding = encoding;
        }

        public static IArchiveFile Create(MemoryMappedFile mmFile, Encoding encoding)
        {
            return new NPAFile(mmFile, encoding);
        }

        ~NPAFile()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _mmFile?.Dispose();
                }
                _disposed = true;
            }
        }

        public void OpenArchive()
        {
            using (MemoryMappedViewStream stream = _mmFile.CreateViewStream(0, 0, MemoryMappedFileAccess.Read))
            {
                // BinaryReader should always be in little endian
                BinaryReader reader = new BinaryReader(stream);
                ReadHeader(reader);
                for (uint fileIndex = 0; fileIndex < _entriesCount; fileIndex++)
                {
                    ReadFileEntry(reader, fileIndex);
                }
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

        private void ReadHeader(BinaryReader reader)
        {
            byte[] magic = new byte[7];
            reader.Read(magic, 0, 7);
            if (!magic.SequenceEqual(NPAFile.Magic))
            {
                throw new ArchiveException("NPA", "Unknown magic");
            }

            _key1 = reader.ReadUInt32();
            _key2 = reader.ReadUInt32();

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

            _entriesCount = reader.ReadUInt32();
            uint folderCount = reader.ReadUInt32();
            uint fileCount = reader.ReadUInt32();
            if (_entriesCount != folderCount + fileCount)
            {
                throw new ArchiveException("NPA", "entriesCount != folderCount + fileCount");
            }

            ulong zero = reader.ReadUInt64();
            if (zero != 0)
            {
                throw new ArchiveException("NPA", "Expected 8 null bytes");
            }

            _dataStart = reader.ReadUInt32();
        }

        private byte crypt(uint charIndex, uint fileIndex)
        {
            uint key = 0xFC * charIndex;

            key -= (_key1 * _key2) >> 0x18;
            key -= (_key1 * _key2) >> 0x10;
            key -= (_key1 * _key2) >> 0x08;
            key -= (_key1 * _key2)  & 0xff;

            key -= fileIndex >> 0x18;
            key -= fileIndex >> 0x10;
            key -= fileIndex >> 0x08;
            key -= fileIndex;

            return (byte) key;
        }

        private void ReadFileEntry(BinaryReader reader, uint fileIndex)
        {
            uint nameLength = reader.ReadUInt32();

            byte[] name = new byte[nameLength];
            reader.Read(name, 0, (int) nameLength);
            for (uint charIndex = 0; charIndex < nameLength; charIndex++)
            {
                name[charIndex] += crypt(charIndex, fileIndex);
            }
            string decodedName = _encoding.GetString(name);

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
                _files.Add(decodedName, (offset, originalSize));
            }
        }
    }
}
