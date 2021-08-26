using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;

namespace NitroSharp.Content
{
    internal sealed class AFSFile : IArchiveFile
    {
        public static readonly byte[] Magic = {0x41, 0x46, 0x53, 0x00};

        private readonly (uint offset, uint size)[] _files;
        private readonly Dictionary<string, uint> _builtinFileNames;
        private readonly Dictionary<string, uint> _iniFileNames;

        private readonly MemoryMappedFile _mmFile;
        private readonly uint _archiveOffset;

        private readonly Encoding _encoding;

        private AFSFile(MemoryMappedFile mmFile, uint archiveOffset, Encoding encoding, (uint offset, uint size)[] files, Dictionary<string, uint> builtinFileNames)
        {
            _files = files;
            _builtinFileNames = builtinFileNames;
            _iniFileNames = new Dictionary<string, uint>();
            _encoding = encoding;
            _mmFile = mmFile;
            _archiveOffset = archiveOffset;
        }

        public static IArchiveFile Load(MemoryMappedFile mmFile, Encoding encoding, uint archiveOffset = 0)
        {
            return TryLoad(mmFile, encoding, archiveOffset) is AFSFile file
                ? file
                : throw new ArchiveException("AFS", "Unknown magic");
        }

        public static IArchiveFile Load(AFSFile parent, string name, Encoding encoding)
        {
            uint archiveOffset = parent._archiveOffset + parent.GetFile(name).offset;
            return Load(parent._mmFile, encoding, archiveOffset);
        }

        public void Dispose()
        {
            // We only dispose the MemoryMappedFile if we're the parent archive
            if (_archiveOffset == 0)
            {
                _mmFile.Dispose();
            }
        }

        public static IArchiveFile? TryLoad(MemoryMappedFile mmFile, Encoding encoding, uint archiveOffset = 0)
        {
            using (MemoryMappedViewStream stream = mmFile.CreateViewStream(archiveOffset, 0, MemoryMappedFileAccess.Read))
            {
                // BinaryReader should always be in little endian
                BinaryReader reader = new BinaryReader(stream);

                byte[] magic = new byte[4];
                reader.Read(magic, 0, 4);
                if (!magic.SequenceEqual(AFSFile.Magic))
                {
                    return null;
                }
                uint entriesCount = reader.ReadUInt32();

                (uint offset, uint size)[] files = new (uint offset, uint size)[entriesCount];
                for (uint fileIndex = 0; fileIndex < entriesCount; fileIndex++)
                {
                    uint offset = reader.ReadUInt32();
                    uint size = reader.ReadUInt32();
                    files[fileIndex] = (offset, size);
                }

                Dictionary<string, uint> builtinFileNames = new Dictionary<string, uint>();
                uint stringOffset = reader.ReadUInt32();
                stream.Seek(stringOffset, SeekOrigin.Begin);
                Span<byte> name = stackalloc byte[32];
                Span<byte> unk = stackalloc byte[16];
                for (uint fileIndex = 0; fileIndex < entriesCount; fileIndex++)
                {
                    reader.Read(name);
                    string decodedName = encoding.GetString(name).ToLowerInvariant();
                    builtinFileNames[decodedName] = fileIndex;

                    reader.Read(unk);
                }

                return new AFSFile(mmFile, archiveOffset, encoding, files, builtinFileNames);
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

        private (uint offset, uint size) GetFile(string path)
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
