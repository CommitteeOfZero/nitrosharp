using System.IO;
using FFmpeg.AutoGen;
using NitroSharp.Media.Decoding;
using NitroSharp.Utilities;

namespace NitroSharp.Content
{
    internal sealed class FFmpegTextureDataLoader : ContentLoader
    {
        private unsafe AVInputFormat* _inputFormat;
        private readonly VideoFrameConverter _frameConverter;

        public FFmpegTextureDataLoader(ContentManager content)
            : base(content)
        {
            _frameConverter = new VideoFrameConverter();
            unsafe
            {
                _inputFormat = ffmpeg.av_find_input_format("image2pipe");
            }
        }

        public override object Load(Stream stream)
        {
            unsafe
            {
                using (var container = MediaContainer.Open(stream, _inputFormat, leaveOpen: false))
                using (var decodingSession = new DecodingSession(container, container.BestVideoStream.Id))
                {
                    var packet = new AVPacket();
                    var frame = new AVFrame();
                    bool succ = container.ReadFrame(ref packet) == 0;
                    decodingSession.DecodeFrame(ref packet, ref frame);

                    var buffer = NativeMemory.Allocate((uint)(frame.width * frame.height * 4));
                    var size = new Primitives.Size((uint)frame.width, (uint)frame.height);
                    _frameConverter.ConvertToRgba(ref frame, size, (byte*)buffer.Pointer);

                    FFmpegUtil.UnrefBuffers(ref packet);
                    FFmpegUtil.UnrefBuffers(ref frame);

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
