using MoeGame.Framework.Audio;
using System.IO;

namespace MoeGame.Framework.Content
{
    public class FFmpegAudioLoader : ContentLoader
    {
        public override object Load(Stream stream)
        {
            return new FFmpegAudioStream(stream);
        }
    }
}
