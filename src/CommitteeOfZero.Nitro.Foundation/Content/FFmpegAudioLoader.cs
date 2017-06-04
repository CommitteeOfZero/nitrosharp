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
    }
}
