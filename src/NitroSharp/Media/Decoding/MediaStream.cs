using System;
using FFmpeg.AutoGen;

namespace NitroSharp.Media.Decoding
{
    public enum MediaStreamKind
    {
        Audio,
        Video,
        Unsupported
    }

    public abstract class MediaStream
    {
        internal static unsafe MediaStream FromAvStream(AVStream* stream)
        {
            switch (stream->codecpar->codec_type)
            {
                case AVMediaType.AVMEDIA_TYPE_AUDIO:
                    return new AudioStream(stream);
                case AVMediaType.AVMEDIA_TYPE_VIDEO:
                    return new VideoStream(stream);
                default:
                    return new UnsupportedMediaStream(stream);
            }
        }

        protected unsafe MediaStream(AVStream* stream)
        {
            AvStream = stream;
            Id = (uint)stream->index;
            double duration = FFmpegUtil.RebaseTimestamp(stream->duration, stream->time_base);
            Duration = duration > 0 ? TimeSpan.FromSeconds(duration) : TimeSpan.Zero;
        }

        internal unsafe AVStream* AvStream { get; }

        public uint Id { get; }
        public abstract MediaStreamKind Kind { get; }
        public TimeSpan Duration { get; }
    }

    public sealed class AudioStream : MediaStream
    {
        internal unsafe AudioStream(AVStream* stream) : base(stream)
        {
            SampleRate = (uint)stream->codecpar->sample_rate;
        }

        public ChannelLayout ChannelLayout { get; }
        public uint SampleRate { get; }

        public override MediaStreamKind Kind => MediaStreamKind.Audio;
    }

    public sealed class VideoStream : MediaStream
    {
        internal unsafe VideoStream(AVStream* stream) : base(stream)
        {
            Width = (uint)stream->codecpar->width;
            Height = (uint)stream->codecpar->height;
        }

        public uint Width { get; }
        public uint Height { get; }

        public override MediaStreamKind Kind => MediaStreamKind.Video;
    }

    public sealed class UnsupportedMediaStream : MediaStream
    {
        internal unsafe UnsupportedMediaStream(AVStream* stream) : base(stream)
        {
        }

        public override MediaStreamKind Kind => MediaStreamKind.Unsupported;
    }
}
