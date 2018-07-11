using System;
using FFmpeg.AutoGen;
using NitroSharp.Primitives;

namespace NitroSharp.Media.Decoding
{
    internal sealed class VideoProcessor : MediaProcessor
    {
        private readonly Size _dstResolution;
        private readonly VideoFrameConverter _frameConverter;

        protected override uint BufferPoolSize => 30;

        public unsafe VideoProcessor(VideoFrameConverter frameConverter, AVStream* stream, Size? dstResolution)
            : this(frameConverter, stream, dstResolution ?? new Size((uint)stream->codecpar->width, (uint)stream->codecpar->height))
        {
        }

        private unsafe VideoProcessor(VideoFrameConverter frameConverter, AVStream* stream, Size dstResolution)
            : base(stream, DetermineBufferSize(dstResolution))
        {
            _frameConverter = frameConverter;
            _dstResolution = dstResolution;
        }

        private static unsafe uint DetermineBufferSize(Size dstResolution)
        {
            return (uint)ffmpeg.av_image_get_buffer_size(
                AVPixelFormat.AV_PIX_FMT_RGBA,
                (int)dstResolution.Width,
                (int)dstResolution.Height, 1);
        }

        public override uint GetExpectedOutputBufferSize(ref AVFrame srcFrame)
        {
            return DetermineBufferSize(_dstResolution);
        }

        public override unsafe int ProcessFrame(ref AVFrame frame, ref PooledBuffer outBuffer)
        {
            _frameConverter.ConvertToRgba(ref frame, _dstResolution, (byte*)outBuffer.Data);
            return (int)outBuffer.Size;
        }
    }
}
