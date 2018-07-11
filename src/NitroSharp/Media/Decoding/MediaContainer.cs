using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using FFmpeg.AutoGen;

using static NitroSharp.Media.Decoding.FFmpegUtil;

namespace NitroSharp.Media.Decoding
{
    public sealed partial class MediaContainer : IDisposable
    {
        private const int IOBufferSize = 4096;

        private readonly FormatContext _formatContext;
        private readonly bool _leaveOpen;
        private avio_alloc_context_read_packet _readFunc;
        private avio_alloc_context_write_packet _writeFunc;
        private avio_alloc_context_seek _seekFunc;
        private byte[] _managedIOBuffer;

        private List<MediaStream> _mediaStreams;

        public static MediaContainer Open(Stream stream, bool leaveOpen = false)
            => new MediaContainer(stream, leaveOpen);

        public static unsafe MediaContainer Open(Stream stream, AVInputFormat* inputFormat, bool leaveOpen = false)
            => new MediaContainer(stream, inputFormat, leaveOpen);

        private unsafe MediaContainer(Stream stream, bool leaveOpen = false)
            : this(stream, null, leaveOpen)
        {
        }

        private unsafe MediaContainer(Stream fileStream, AVInputFormat* inputFormat, bool leaveOpen = false)
        {
            FileStream = fileStream ?? throw new ArgumentNullException(nameof(fileStream));
            _leaveOpen = leaveOpen;
            _formatContext = new FormatContext(OpenInput(inputFormat));
            EnumerateStreams();
        }

        public Stream FileStream { get; }
        public bool HasAudio => BestAudioStream != null;
        public bool HasVideo => BestVideoStream != null;

        public IReadOnlyList<MediaStream> MediaStreams => _mediaStreams;
        public AudioStream BestAudioStream { get; private set; }
        public VideoStream BestVideoStream { get; private set; }
        public AudioStream ActiveAudioStream { get; private set; }
        public VideoStream ActiveVideoStream { get; private set; }

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

            ThrowIfNotZero(ffmpeg.avformat_open_input(&pFormatContext, string.Empty, inputFormat, null));
            ThrowIfNotZero(ffmpeg.avformat_find_stream_info(pFormatContext, null));

            return pFormatContext;
        }

        private unsafe void EnumerateStreams()
        {
            AVFormatContext* ctx = _formatContext.Get();
            AVStream** avStreams = ctx->streams;

            _mediaStreams = new List<MediaStream>((int)ctx->nb_streams);
            for (int i = 0; i < ctx->nb_streams; i++)
            {
                _mediaStreams.Add(MediaStream.FromAvStream(avStreams[i]));
            }

            int n = ffmpeg.av_find_best_stream(ctx, AVMediaType.AVMEDIA_TYPE_AUDIO, -1, -1, null, 0);
            if (n >= 0)
            {
                BestAudioStream = (AudioStream)_mediaStreams[n];
            }

            n = ffmpeg.av_find_best_stream(ctx, AVMediaType.AVMEDIA_TYPE_VIDEO, -1, -1, null, 0);
            if (n >= 0)
            {
                BestVideoStream = (VideoStream)_mediaStreams[n];
            }
        }

        public void SelectStreams(VideoStream videoStream, AudioStream audioStream)
        {
            if (videoStream == ActiveVideoStream && audioStream == ActiveAudioStream)
            {
                return;
            }

            ActiveAudioStream = audioStream;
            ActiveVideoStream = videoStream;
            unsafe
            {
                AVFormatContext* ctx = _formatContext.Get();
                for (uint i = 0; i < ctx->nb_streams; i++)
                {
                    bool active = i == videoStream?.Id || i == audioStream?.Id;
                    ctx->streams[(int)i]->discard = active ? AVDiscard.AVDISCARD_DEFAULT : AVDiscard.AVDISCARD_ALL;
                }
            }
        }

        public unsafe int ReadFrame(ref AVPacket packet)
        {
            unsafe
            {
                fixed (AVPacket* ptr = &packet)
                {
                    return ReadFrame(ptr);
                }
            }
        }

        public unsafe int ReadFrame(AVPacket* packet)
        {
            return ffmpeg.av_read_frame(_formatContext.Get(), packet);
        }

        public int ReadFrame(IntPtr packet)
        {
            unsafe
            {
                return ReadFrame((AVPacket*)packet);
            }
        }

        public void Seek(TimeSpan timestamp)
        {
            long ts = (long)Math.Round(timestamp.TotalSeconds * ffmpeg.AV_TIME_BASE);
            unsafe
            {
                AVFormatContext* ctx = _formatContext.Get();
                int result = ffmpeg.av_seek_frame(ctx, -1, ts, ffmpeg.AVSEEK_FLAG_BACKWARD);
            }
        }

        private unsafe long IOSeek(void* opaque, long offset, int whence)
        {
            Debug.Assert(FileStream.CanSeek);
            if (whence == ffmpeg.AVSEEK_SIZE)
            {
                return FileStream.Length;
            }

            var origin = (SeekOrigin)whence;
            return FileStream.Seek(offset, origin);
        }

        private unsafe int IOReadPacket(void* opaque, byte* buf, int buf_size)
        {
            Debug.Assert(FileStream.CanRead);

            int count = Math.Min(buf_size, IOBufferSize);
            int result = FileStream.Read(_managedIOBuffer, 0, count);
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
            _formatContext.Dispose();
            if (!_leaveOpen)
            {
                FileStream.Dispose();
            }
        }
    }
}
