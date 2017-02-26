using NVorbis;
using System.IO;

namespace SciAdvNet.MediaLayer.Audio
{
    public class OggFile : AudioFile
    {
        private readonly Stream _stream;
        private readonly VorbisReader _reader;

        public OggFile(Stream stream)
        {
            _stream = stream;
            _reader = new VorbisReader(stream, closeStreamOnDispose: true);
            stream.Position = 0;
            SampleRate = _reader.SampleRate;
            TotalSamples = _reader.TotalSamples;
            Channels = _reader.Channels;
        }

        public override int ReadSamples(float[] buffer, int offset, int count)
        {
            return _reader.ReadSamples(buffer, offset, count);
        }

        public override void Dispose()
        {
            _reader.Dispose();
        }
    }
}
