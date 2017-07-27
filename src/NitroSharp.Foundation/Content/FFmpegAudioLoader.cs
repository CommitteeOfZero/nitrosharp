using NitroSharp.Foundation.Audio;
using System.IO;

namespace NitroSharp.Foundation.Content
{
    public class FFmpegAudioLoader : ContentLoader
    {
        private readonly AudioEngine _audioEngine;

        public FFmpegAudioLoader(AudioEngine audioEngine)
        {
            _audioEngine = audioEngine;
        }

        public override object Load(Stream stream)
        {
            return new FFmpegAudioStream(stream, _audioEngine.BitDepth, _audioEngine.SampleRate, _audioEngine.ChannelCount);
        }
    }
}
