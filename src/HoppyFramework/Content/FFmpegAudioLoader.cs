using HoppyFramework.Audio;
using System.IO;

namespace HoppyFramework.Content
{
    public class FFmpegAudioLoader : ContentLoader
    {
        public override object Load(Stream stream)
        {
            return new FFmpegAudioStream(stream);
        }
    }
}
