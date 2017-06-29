using CommitteeOfZero.NitroSharp.Foundation.Audio;
using System.IO;

namespace CommitteeOfZero.NitroSharp.Foundation.Content
{
    public class FFmpegAudioLoader : ContentLoader
    {
        public override object Load(Stream stream)
        {
            return new FFmpegAudioStream(stream);
        }
    }
}
