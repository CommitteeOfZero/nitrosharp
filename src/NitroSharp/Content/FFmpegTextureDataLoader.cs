using System.IO;
using FFmpeg.AutoGen;
using NitroSharp.Media;
using NitroSharp.Utilities;

namespace NitroSharp.Content
{
    internal sealed class FFmpegTextureDataLoader : ContentLoader
    {
        private readonly DecoderCollection _decoders;
        private unsafe AVInputFormat* _inputFormat;
        private readonly FrameConverter _frameConverter;

        public FFmpegTextureDataLoader(ContentManager content, DecoderCollection decoderCollection)
            : base(content)
        {
            _decoders = decoderCollection;
            _frameConverter = new FrameConverter();
            unsafe
            {
                _inputFormat = ffmpeg.av_find_input_format("image2pipe");
            }
        }

        public override object Load(Stream stream)
        {
            unsafe
            {
                using (var container = new MediaContainer(stream, _inputFormat, leaveOpen: false))
                using (var decodingSession = new DecodingSession(container, container.VideoStreamId.Value, _decoders))
                {
                    var packet = new AVPacket();
                    // Note: av_frame_unref should NOT be called here. The DecodingSession is responsible for doing that.
                    var frame = new AVFrame();
                    bool succ = container.ReadFrame(&packet);
                    succ = decodingSession.TryDecodeFrame(&packet, out frame);

                    ffmpeg.av_packet_unref(&packet);

                    var buffer = NativeMemory.Allocate((uint)(frame.width * frame.height * 4));
                    var size = new Primitives.Size((uint)frame.width, (uint)frame.height);
                    _frameConverter.ConvertToRgba(&frame, size, (byte*)buffer.Pointer);
                    return new FFmpegTextureData(buffer, size);
                }
            }
        }

        public override void Dispose()
        {
            _frameConverter.Dispose();
            unsafe
            {
                _inputFormat = null;
            }
        }
    }
}
