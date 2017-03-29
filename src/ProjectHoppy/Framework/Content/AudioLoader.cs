using SciAdvNet.MediaLayer.Audio;
using System.IO;

namespace ProjectHoppy.Framework.Content
{
    public class AudioLoader : ContentLoader
    {
        private readonly SciAdvNet.MediaLayer.Audio.ResourceFactory _resourceFactory;

        public AudioLoader(SciAdvNet.MediaLayer.Audio.ResourceFactory resourceFactory)
        {
            _resourceFactory = resourceFactory;
        }

        public override object Load(Stream stream)
        {
            return new FFmpegAudioStream(stream);
        }
    }
}
