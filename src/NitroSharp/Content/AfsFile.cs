using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Text;

namespace NitroSharp.Content
{
    internal sealed class AfsFile : ArchiveFile
    {
        private static ReadOnlySpan<byte> Magic =>new byte[] { 0x41, 0x46, 0x53, 0x00 };

        private readonly (uint offset, uint size)[] _entries;
        private readonly Dictionary<string, uint> _builtinFileNames;
        private readonly Dictionary<string, uint> _iniFileNames;

        private readonly MemoryMappedFile _file;
        private readonly uint _archiveOffset;

        private readonly Encoding _encoding;

        private AfsFile(
            MemoryMappedFile file,
            uint archiveOffset,
            Encoding encoding,
            (uint offset, uint size)[] entries,
            Dictionary<string, uint> builtinFileNames)
        {
            _entries = entries;
            _builtinFileNames = builtinFileNames;
            _iniFileNames = new Dictionary<string, uint>();
            _encoding = encoding;
            _file = file;
            _archiveOffset = archiveOffset;
        }

        public static AfsFile Load(AfsFile parent, string name, Encoding encoding)
        {
            uint archiveOffset = parent._archiveOffset + parent.GetFile(name).offset;
            return Load(parent._file, encoding, archiveOffset);
        }

        private static AfsFile Load(MemoryMappedFile mmFile, Encoding encoding, uint archiveOffset = 0)
        {
            return TryLoad(mmFile, encoding, archiveOffset) is AfsFile file
                ? file
                : throw new ArchiveException("AFS", "Unknown magic");
        }

        public static AfsFile? TryLoad(MemoryMappedFile file, Encoding encoding)
            => TryLoad(file, encoding, 0);

        private static AfsFile? TryLoad(MemoryMappedFile file, Encoding encoding, uint archiveOffset)
        {
            using MemoryMappedViewStream stream = file
                .CreateViewStream(archiveOffset, 0, MemoryMappedFileAccess.Read);
            var reader = new BinaryReader(stream);
            Span<byte> magic = stackalloc byte[4];
            reader.Read(magic);
            if (!magic.SequenceEqual(Magic))
            {
                return null;
            }

            uint entryCount = reader.ReadUInt32();
            var entries = new (uint offset, uint size)[entryCount];
            for (uint i = 0; i < entryCount; i++)
            {
                uint offset = reader.ReadUInt32();
                uint size = reader.ReadUInt32();
                entries[i] = (offset, size);
            }

            var builtinFileNames = new Dictionary<string, uint>();
            uint stringOffset = reader.ReadUInt32();
            stream.Seek(stringOffset, SeekOrigin.Begin);
            Span<byte> nameBytes = stackalloc byte[32];
            Span<byte> unk = stackalloc byte[16];
            for (uint i = 0; i < entryCount; i++)
            {
                reader.Read(nameBytes);
                string name = encoding.GetString(nameBytes).ToLowerInvariant();
                builtinFileNames[name] = i;
                reader.Read(unk);
            }

            return new AfsFile(file, archiveOffset, encoding, entries, builtinFileNames);
        }

        public override Stream OpenStream(string path)
        {
            (uint offset, uint size) = GetFile(path);
            return _file.CreateViewStream(_archiveOffset + offset, size, MemoryMappedFileAccess.Read);
        }

        public void LoadFileNames(Stream iniFile)
        {
            using var reader = new StreamReader(iniFile, _encoding);
            string? line = reader.ReadLine();
            if (line is null || !uint.TryParse(line, out uint nameCount))
            {
                malformed();
            }

            Span<char> fileNameBuf = stackalloc char[256];
            for (uint i = 0; i < nameCount; i++)
            {
                line = reader.ReadLine();
                if (line is null) { malformed(); }
                int idxSeprator = line.IndexOf(',');
                if (idxSeprator == -1) { malformed(); }
                int fileNameLen = idxSeprator;
                Span<char> fileName = fileNameLen < 256
                    ? fileNameBuf[..fileNameLen]
                    : new char[fileNameLen];
                line.AsSpan()[..fileNameLen].ToLowerInvariant(fileName);
                ReadOnlySpan<char> fileIndex = line.AsSpan()[(idxSeprator + 1)..];
                if (!uint.TryParse(fileIndex, out uint index)) { malformed(); }
                _iniFileNames[fileName.ToString()] = index;
            }

            [DoesNotReturn]
            static void malformed()
                => throw new ArchiveException("AFS", "Malformed INI file.");
        }

        public override bool Contains(string path)
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

            if (fileId is null)
            {
                throw new FileNotFoundException("File not found in the AFS archive", path);
            }
            return _entries[(int)fileId];
        }

        public override void Dispose()
        {
            // We only dispose the MemoryMappedFile if we're the parent archive
            if (_archiveOffset == 0)
            {
                _file.Dispose();
            }
        }
    }
}
