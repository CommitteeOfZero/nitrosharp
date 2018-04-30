using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using FFmpeg.AutoGen;

namespace NitroSharp.Media
{
    public sealed partial class MediaContainer : IDisposable
    {
        private const int IOBufferSize = 4096;

        private avio_alloc_context_read_packet _readFunc;
        private avio_alloc_context_write_packet _writeFunc;
        private avio_alloc_context_seek _seekFunc;
        private byte[] _managedIOBuffer;
        private readonly bool _leaveOpen;

        public unsafe MediaContainer(Stream stream, bool leaveOpen = false)
            : this(stream, null, leaveOpen)
        {
        }

        public unsafe MediaContainer(Stream stream, AVInputFormat* inputFormat, bool leaveOpen = false)
        {
            Stream = stream;
            _leaveOpen = leaveOpen;
            unsafe
            {
                FormatContext = new FormatContext(OpenInput(inputFormat));
                var ctx = FormatContext.Get();
                int n = ffmpeg.av_find_best_stream(ctx, AVMediaType.AVMEDIA_TYPE_AUDIO, -1, -1, null, 0);
                AudioStreamId = n >= 0 ? (uint?)n : null;
                n = ffmpeg.av_find_best_stream(ctx, AVMediaType.AVMEDIA_TYPE_VIDEO, -1, -1, null, 0);
                VideoStreamId = n >= 0 ? (uint?)n : null;
                for (int i = 0; i < ctx->nb_streams; i++)
                {
                    if (i != AudioStreamId && i != VideoStreamId)
                    {
                        ctx->streams[i]->discard = AVDiscard.AVDISCARD_ALL;
                    }
                }
            }
        }

        public Stream Stream { get; }
        public uint? AudioStreamId { get; }
        public uint? VideoStreamId { get; }
        public bool HasAudio => AudioStreamId.HasValue;
        public bool HasVideo => VideoStreamId.HasValue;

        internal FormatContext FormatContext { get; }

        private unsafe AVFormatContext* OpenInput(AVInputFormat* inputFormat)
        {
            _readFunc = IOReadPacket;
            _writeFunc = IOWritePacket;
            _seekFunc = IOSeek;

            _managedIOBuffer = new byte[IOBufferSize + ffmpeg.AV_INPUT_BUFFER_PADDING_SIZE];
            var ioBuffer = (byte*)ffmpeg.av_malloc(IOBufferSize);
            var ioContext = ffmpeg.avio_alloc_context(ioBuffer, IOBufferSize, 0, null, _readFunc, _writeFunc, _seekFunc);

            AVFormatContext* pFormatContext = ffmpeg.avformat_alloc_context();
            pFormatContext->pb = ioContext;

            int ret = ffmpeg.avformat_open_input(&pFormatContext, string.Empty, inputFormat, null);
            ret = ffmpeg.avformat_find_stream_info(pFormatContext, null);
            return pFormatContext;
        }

        internal unsafe bool ReadFrame(AVPacket* packet)
        {
            int result = ffmpeg.av_read_frame(FormatContext.Get(), packet);
            return result >= 0;
        }

        public void Seek(TimeSpan timestamp)
        {
            long ts = (long)Math.Round(timestamp.TotalSeconds * ffmpeg.AV_TIME_BASE);
            unsafe
            {
                var ctx = FormatContext.Get();
                int result = ffmpeg.avformat_seek_file(ctx, -1, long.MinValue, ts, long.MaxValue, 0);
            }
        }

        private unsafe long IOSeek(void* opaque, long offset, int whence)
        {
            Debug.Assert(Stream.CanSeek);
            if (whence == ffmpeg.AVSEEK_SIZE)
            {
                return Stream.Length;
            }

            var origin = (SeekOrigin)whence;
            return Stream.Seek(offset, origin);
        }

        private unsafe int IOReadPacket(void* opaque, byte* buf, int buf_size)
        {
            Debug.Assert(Stream.CanRead);

            int count = Math.Min(buf_size, IOBufferSize);
            int result = Stream.Read(_managedIOBuffer, 0, count);
            if (result == 0)
            {
                return ffmpeg.AVERROR_EOF;
            }

            Marshal.Copy(_managedIOBuffer, 0, (IntPtr)buf, result);
            return result;
        }

        private unsafe int IOWritePacket(void* opaque, byte* buf, int buf_size)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            FormatContext.Dispose();
            if (!_leaveOpen)
            {
                Stream.Dispose();
            }
        }
    }
}
