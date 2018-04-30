using System;
using FFmpeg.AutoGen;
using NitroSharp.Primitives;

namespace NitroSharp.Media
{
    internal sealed unsafe class DecodingSession : IDisposable
    {
        private AVCodecContext* _codecContext;
        private AVFrame* _frame;
        private AVPacket* _packet;

        public DecodingSession(MediaContainer container, uint streamIndex, DecoderCollection decoderCollection)
        {
            AVStream* stream = container.FormatContext.Get()->streams[streamIndex];
            AVCodec* codec = decoderCollection.Get(stream->codec->codec_id);
            ThrowIfNotZero(ffmpeg.avcodec_open2(stream->codec, codec, null));

            _frame = ffmpeg.av_frame_alloc();
            _packet = ffmpeg.av_packet_alloc();
            _codecContext = stream->codec;
        }

        public bool TryDecodeFrame(AVPacket* packet, out AVFrame frame)
        {
            ffmpeg.av_frame_unref(_frame);
            int error;
            do
            {
                error = ffmpeg.avcodec_send_packet(_codecContext, packet);
                if (error == ffmpeg.AVERROR_EOF)
                {
                    frame = *_frame;
                    return false;
                }

                error = ffmpeg.avcodec_receive_frame(_codecContext, _frame);
            } while (error == ffmpeg.AVERROR(ffmpeg.EAGAIN));

            ThrowIfNotZero(error);
            frame = *_frame;
            return true;
        }

        public unsafe void FlushBuffers()
        {
            ffmpeg.avcodec_flush_buffers(_codecContext);
        }

        private static void ThrowIfNotZero(int result)
        {
            if (result != 0)
            {
                throw new Exception();
            }
        }

        public void Dispose()
        {
            Free();
            GC.SuppressFinalize(this);
        }

        ~DecodingSession()
        {
            Free();
        }

        private void Free()
        {
            ffmpeg.av_frame_unref(_frame);
            ffmpeg.av_packet_unref(_packet);
            _codecContext = null;
            _frame = null;
            _packet = null;
        }
    }
}
