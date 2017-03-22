using System;
using System.IO;
using System.IO.Compression;

namespace ProjectHoppy.Content
{
    public class ZipContentManager : ContentManager, IDisposable
    {
        internal readonly ZipArchive _archive;

        public ZipContentManager(string archivePath)
        {
            _archive = ZipFile.Open(archivePath, ZipArchiveMode.Update);
        }

        public void PreloadToc()
        {
            var randomEntry = _archive.Entries[0];
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
