using CommitteeOfZero.Nitro.Foundation.Audio;
using System.IO;

namespace CommitteeOfZero.Nitro.Foundation.Content
{
    public class FFmpegAudioLoader : ContentLoader
    {
        public override object Load(Stream stream)
        {
            return new FFmpegAudioStream(stream);
        }

        public override bool IsSupportedContentType(BinaryReader reader)
        {
            return reader.ReadInt16() == 0x80;
        }
    }
}
