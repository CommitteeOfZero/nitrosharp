using System;
using System.IO;
using System.IO.Compression;

namespace ProjectHoppy.Content
{
    public class ZipContentManager : ContentManager, IDisposable
    {
        private readonly ZipArchive _archive;

        public ZipContentManager(Game game, string archivePath)
            : base(game)
        {
            _archive = ZipFile.OpenRead(archivePath);
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
