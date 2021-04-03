using System;
using System.Diagnostics;
using System.IO;
using FFmpeg.AutoGen;
using static NitroSharp.Media.FFmpegUtil;

namespace NitroSharp.Media
{
    internal sealed unsafe class FormatContext : IDisposable
    {
        private const int IoBufferSize = 4096;

        private readonly Stream _stream;
        private readonly AVFormatContext* _ctx;
        private readonly AVPacket* _recvPacket;

        // ReSharper disable PrivateFieldCanBeConvertedToLocalVariable
        // The delegates must be kept alive for the entire lifetime of the object.
        private readonly avio_alloc_context_read_packet _readFunc;
        private readonly avio_alloc_context_seek _seekFunc;
        private readonly AVCodecContext_get_format _getFormatFunc;
        // ReSharper restore PrivateFieldCanBeConvertedToLocalVariable

        public FormatContext(Stream stream)
        {
            _stream = stream;
            _readFunc = IoReadPacket;
            _seekFunc = IoSeek;
            _getFormatFunc = GetFormat;
            // Both the buffer and the IO context are freed by avformat_close_input.
            byte* ioBuffer = (byte*)ffmpeg.av_malloc(IoBufferSize);
            AVIOContext* ioContext = ffmpeg.avio_alloc_context(
                ioBuffer, IoBufferSize,
                write_flag: 0, opaque: null,
                _readFunc, null, _seekFunc
            );

            AVFormatContext* ctx = ffmpeg.avformat_alloc_context();
            ctx->pb = ioContext;
            _ctx = ctx;

            _recvPacket = ffmpeg.av_packet_alloc();
            CheckResult(ffmpeg.avformat_open_input(&ctx, string.Empty, null, null));
            CheckResult(ffmpeg.avformat_find_stream_info(ctx, null));
        }

        private static AVPixelFormat GetFormat(AVCodecContext* s, AVPixelFormat* fmt)
        {
            int i = 0;
            while ((int)fmt[i] != -1)
            {
                if (fmt[i] == AVPixelFormat.AV_PIX_FMT_YUVJ444P)
                {
                    fmt[i] = AVPixelFormat.AV_PIX_FMT_YUV444P;
                }
                i++;
            }

            return ffmpeg.avcodec_default_get_format(s, fmt);
        }

        public AVFormatContext* Inner => _ctx;
        public AVPacket* RecvPacket => _recvPacket;

        public AVCodecContext* OpenStream(int index)
        {
            if (index < 0) { return null; }
            AVStream* stream = _ctx->streams[index];
            AVCodec* codec = DecoderCollection.Shared.Get(stream->codecpar->codec_id);
            AVCodecContext* codecCtx = ffmpeg.avcodec_alloc_context3(codec);
            Debug.Assert(codecCtx is not null);
            codecCtx->get_format = _getFormatFunc;
            CheckResult(ffmpeg.avcodec_parameters_to_context(codecCtx, stream->codecpar));
            CheckResult(ffmpeg.avcodec_open2(codecCtx, codec, null));
            return codecCtx;
        }

        private long IoSeek(void* opaque, long offset, int whence)
        {
            Debug.Assert(_stream.CanSeek);
            if (whence == ffmpeg.AVSEEK_SIZE)
            {
                return _stream.Length;
            }

            var origin = (SeekOrigin)whence;
            return _stream.Seek(offset, origin);
        }

        private int IoReadPacket(void* opaque, byte* buf, int bufSize)
        {
            Debug.Assert(_stream.CanRead);
            int size = Math.Min(bufSize, IoBufferSize);
            var dst = new Span<byte>(buf, size);
            int result = _stream.Read(dst);
            return result == 0 ? ffmpeg.AVERROR_EOF : result;
        }

        public void Dispose()
        {
            fixed (AVFormatContext** pCtx = &_ctx)
            {
                ffmpeg.avformat_close_input(pCtx);
            }
            fixed (AVPacket** pkt = &_recvPacket)
            {
                ffmpeg.av_packet_free(pkt);
            }
        }
    }
}
