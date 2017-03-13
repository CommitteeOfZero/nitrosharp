using System;
using System.IO;
using System.IO.Compression;

namespace ProjectHoppy.Content
{
    public class ZipContentManager : ConcurrentContentManager, IDisposable
    {
        private readonly ZipArchive _archive;

        public ZipContentManager(string archivePath)
        {
            _archive = ZipFile.OpenRead(archivePath);
        }

        public void PreloadToc()
        {
            var randomEntry = _archive.Entries[0].Open();
            randomEntry.Dispose();
        }

        public override Stream OpenStream(string path)
        {
            return _archive.GetEntry(path)?.Open();
        }

        public void Dispose()
        {
            _archive.Dispose();
        }
    }
}
