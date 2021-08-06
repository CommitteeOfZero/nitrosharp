using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;

namespace NitroSharp.Content
{
    internal class AFSFile : IArchiveFile
    {
        public static readonly byte[] Magic = {0x41, 0x46, 0x53, 0x00};

        private uint _entriesCount;

        private (uint offset, uint size)[]? _files;
        private readonly Dictionary<string, uint> _builtinFileNames;
        private readonly Dictionary<string, uint> _iniFileNames;

        protected readonly MemoryMappedFile _mmFile;
        protected readonly uint _archiveOffset;

        private readonly Encoding _encoding;

        private bool _disposed = false;

        public AFSFile(MemoryMappedFile mmFile, Encoding encoding)
        {
            _builtinFileNames = new Dictionary<string, uint>();
            _iniFileNames = new Dictionary<string, uint>();
            _encoding = encoding;
            _mmFile = mmFile;
            _archiveOffset = 0;
        }

        public AFSFile(AFSFile parent, string name, Encoding encoding)
        {
            _builtinFileNames = new Dictionary<string, uint>();
            _iniFileNames = new Dictionary<string, uint>();
            _encoding = encoding;
            _mmFile = parent._mmFile;
            _archiveOffset = parent._archiveOffset + parent.GetFile(name).offset;
        }

        public static IArchiveFile Create(MemoryMappedFile mmFile, Encoding encoding)
        {
            return new AFSFile(mmFile, encoding);
        }

        ~AFSFile()
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
                if (disposing && _archiveOffset == 0)
                {
                    _mmFile?.Dispose();
                }
                _disposed = true;
            }
        }

        public void OpenArchive()
        {
            using (MemoryMappedViewStream stream = _mmFile.CreateViewStream(_archiveOffset, 0, MemoryMappedFileAccess.Read))
            {
                // BinaryReader should always be in little endian
                BinaryReader reader = new BinaryReader(stream);

                byte[] magic = new byte[4];
                reader.Read(magic, 0, 4);
                if (!magic.SequenceEqual(AFSFile.Magic))
                {
                    throw new ArchiveException("AFS", "Unknown magic");
                }
                _entriesCount = reader.ReadUInt32();

                _files = new (uint offset, uint size)[_entriesCount];
                for (uint fileIndex = 0; fileIndex < _entriesCount; fileIndex++)
                {
                    uint offset = reader.ReadUInt32();
                    uint size = reader.ReadUInt32();
                    _files[fileIndex] = (offset, size);
                }

                uint stringOffset = reader.ReadUInt32();
                stream.Seek(stringOffset, SeekOrigin.Begin);
                for (uint fileIndex = 0; fileIndex < _entriesCount; fileIndex++)
                {
                    byte[] name = new byte[32];
                    reader.Read(name, 0, 32);
                    string decodedName = _encoding.GetString(name).ToLowerInvariant();
                    _builtinFileNames[decodedName] = fileIndex;

                    byte[] unk = new byte[16];
                    reader.Read(unk, 0, 16);
                }
            }
        }

        public void LoadFileNames(Stream iniFile)
        {
            using (StreamReader reader = new StreamReader(iniFile, _encoding))
            {
                string? line = reader.ReadLine();
                if (line == null)
                {
                    throw new ArchiveException("AFS", "Expected amount of file names in the INI file");
                }
                uint fileNamesAmount = UInt32.Parse(line);
                for (uint lineIndex = 0; lineIndex < fileNamesAmount; lineIndex++)
                {
                    line = reader.ReadLine();
                    if (line == null)
                    {
                        throw new ArchiveException("AFS", "Unexpected end of the INI file");
                    }
                    string[] lineParts = line.Split(",");
                    string fileName = lineParts[0].ToLowerInvariant();
                    uint fileIndex = UInt32.Parse(lineParts[1]);
                    _iniFileNames[fileName] = fileIndex;
                }
            }
        }

        public bool Contains(string path)
        {
            return _iniFileNames.ContainsKey(path) || _builtinFileNames.ContainsKey(path);
        }

        protected (uint offset, uint size) GetFile(string path)
        {
            uint? fileId = null;
            if (_iniFileNames.ContainsKey(path))
            {
                fileId = _iniFileNames[path];
            }
            else if (_builtinFileNames.ContainsKey(path))
            {
                fileId = _builtinFileNames[path];
            }

            if (_files == null)
            {
                throw new ArchiveException("AFS", "The archive is not opened");
            }
            if (fileId == null)
            {
                throw new FileNotFoundException("File not found in the AFS archive", path);
            }
            return _files[(int) fileId];
        }

        public Stream OpenStream(string path)
        {
            (uint offset, uint size) = GetFile(path);
            return _mmFile.CreateViewStream(_archiveOffset + offset, size, MemoryMappedFileAccess.Read);
        }
    }
}
