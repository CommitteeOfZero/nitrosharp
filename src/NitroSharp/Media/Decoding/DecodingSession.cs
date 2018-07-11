using System;
using FFmpeg.AutoGen;

namespace NitroSharp.Media.Decoding
{
    internal sealed class DecodingSession : IDisposable
    {
        private unsafe AVCodecContext* _codecContext;

        public DecodingSession(MediaContainer container, uint streamIndex)
        {
            unsafe
            {
                OpenStream(container.MediaStreams[(int)streamIndex].AvStream);
            }
        }

        public unsafe DecodingSession(AVStream* stream)
        {
            OpenStream(stream);
        }

        private unsafe void OpenStream(AVStream* stream)
        {
            AVCodec* codec = DecoderCollection.Shared.Get(stream->codecpar->codec_id);
            _codecContext = ffmpeg.avcodec_alloc_context3(null);
            ffmpeg.avcodec_parameters_to_context(_codecContext, stream->codecpar);
            FFmpegUtil.OpenStream(_codecContext, codec);

            Stream = stream;
            StreamTimebase = stream->time_base;
        }

        public unsafe AVStream* Stream { get; private set; }
        internal unsafe AVCodecContext* CodecContext => _codecContext;
        public AVRational StreamTimebase { get; private set; }

        public void DecodeFrame(ref AVPacket packet, ref AVFrame frame)
        {
            if (!TryDecodeFrame(ref packet, ref frame))
            {
                throw new FFmpegException(FFmpegUtil.GetErrorMessage(ffmpeg.AVERROR_EOF));
            }
        }

        public unsafe void DecodeFrame(AVPacket* packet, AVFrame* frame)
        {
            if (!TryDecodeFrame(packet, frame))
            {
                throw new FFmpegException(FFmpegUtil.GetErrorMessage(ffmpeg.AVERROR_EOF));
            }
        }

        public bool TryDecodeFrame(ref AVPacket packet, ref AVFrame frame)
        {
            unsafe
            {
                fixed (AVPacket* pPacket = &packet)
                fixed (AVFrame* pFrame = &frame)
                {
                    return TryDecodeFrame(pPacket, pFrame);
                }
            }
        }

        public unsafe bool TryDecodeFrame(AVPacket* packet, AVFrame* frame)
        {
            int error;
            do
            {
                error = ffmpeg.avcodec_send_packet(_codecContext, packet);
                if (error == ffmpeg.AVERROR_EOF)
                {
                    return false;
                }

                error = ffmpeg.avcodec_receive_frame(_codecContext, frame);
            } while (error == ffmpeg.AVERROR(ffmpeg.EAGAIN));

            FFmpegUtil.ThrowIfNotZero(error);
            return true;
        }

        public unsafe int TrySendPacket(AVPacket* packet)
        {
            int error = ffmpeg.avcodec_send_packet(_codecContext, packet);
            return error;
        }

        public void FlushBuffers()
        {
            unsafe
            {
                ffmpeg.avcodec_flush_buffers(_codecContext);
            }
        }

        public int TrySendPacket(IntPtr packet)
        {
            unsafe
            {
                return ffmpeg.avcodec_send_packet(_codecContext, (AVPacket*)packet);
            }
        }

        public int TrySendPacket(ref AVPacket packet)
        {
            unsafe
            {
                fixed (AVPacket* ptr = &packet)
                {
                    return TrySendPacket(ptr);
                }
            }
        }

        public int TryReceiveFrame(ref AVFrame frame)
        {
            unsafe
            {
                fixed (AVFrame* ptr = &frame)
                {
                    return TryReceiveFrame(ptr);
                }
            }
        }

        public unsafe int TryReceiveFrame(AVFrame* frame)
        {
            int error = ffmpeg.avcodec_receive_frame(_codecContext, frame);
            return error;
        }

        public void Dispose()
        {
            unsafe
            {
                ffmpeg.avcodec_close(_codecContext);
                fixed (AVCodecContext** ctx = &_codecContext)
                {
                    ffmpeg.avcodec_free_context(ctx);
                }
            }
        }
    }
}
