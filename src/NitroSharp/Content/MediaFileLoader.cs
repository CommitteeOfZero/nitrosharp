using System.IO;
using NitroSharp.Media;
using NitroSharp.Media.Decoding;

namespace NitroSharp.Content
{
    internal sealed class MediaFileLoader : ContentLoader
    {
        private readonly VideoFrameConverter _frameConverter;
        private readonly AudioParameters _outputAudioParameters;

        public MediaFileLoader(ContentManager content, VideoFrameConverter frameConverter, in AudioParameters outputAudioParameters)
            : base(content)
        {
            _frameConverter = frameConverter;
            _outputAudioParameters = outputAudioParameters;
        }

        public override object Load(Stream stream)
        {
            var container = MediaContainer.Open(stream);
            var options = new MediaProcessingOptions(AudioParameters.Default, _frameConverter);
            return new MediaPlaybackSession(container, options);
        }
    }
}
