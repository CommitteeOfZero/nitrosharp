using FFmpeg.AutoGen;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace SciAdvNet.MediaLayer.Audio
{
    public class FFmpegAudioStream : AudioStream
    {
        private const int AVError_EAgain = -11;

        private static AVRational s_msTimeBase;

        private const int IOBufferSize = 4096;
        private byte[] _managedIOBuffer;
        private avio_alloc_context_read_packet ReadFunc;
        private avio_alloc_context_seek SeekFunc;
        private avio_alloc_context_write_packet WriteFunc;

        private Context _context;
        private AVSampleFormat _targetSampleFormat;
        private int _targetBytesPerSample;
        private int _frameSize;

        private long _positionInSamples;

        static FFmpegAudioStream()
        {
            FFmpegLibraries.Init();
            s_msTimeBase = new AVRational { num = 1, den = 1000 };
        }

        public FFmpegAudioStream(Stream fileStream) : base(fileStream)
        {
            OpenStream();
        }

        public override TimeSpan Position => TimeSpan.FromSeconds((double)_positionInSamples / OriginalSampleRate);

        private unsafe long StreamToBclTimestamp(long streamTimestamp)
        {
            var streamTimeBase = _context.Stream->time_base;
            return ffmpeg.av_rescale_q(streamTimestamp, streamTimeBase, s_msTimeBase);
        }

        private unsafe long BclToStreamTimestamp(TimeSpan timeSpan)
        {
            var streamTimeBase = _context.Stream->time_base;
            long msRounded = (long)Math.Round(timeSpan.TotalMilliseconds);
            return ffmpeg.av_rescale_q(msRounded, s_msTimeBase, streamTimeBase);
        }

        private unsafe void OpenStream()
        {
            ReadFunc = IOReadPacket;
            WriteFunc = IOWritePacket;
            SeekFunc = IOSeek;

            _managedIOBuffer = new byte[IOBufferSize];
            var ioBuffer = (byte*)ffmpeg.av_malloc(IOBufferSize);
            var ioContext = ffmpeg.avio_alloc_context(ioBuffer, IOBufferSize, 0, null, ReadFunc, WriteFunc, SeekFunc);

            AVFormatContext* pFormatContext = ffmpeg.avformat_alloc_context();
            pFormatContext->pb = ioContext;

            ThrowIfNotZero(ffmpeg.avformat_open_input(&pFormatContext, string.Empty, null, null));
            ThrowIfNotZero(ffmpeg.avformat_find_stream_info(pFormatContext, null));

            AVStream* pAudioStream = pFormatContext->streams[0];
            AVCodecContext* pCodecContext = pAudioStream->codec;

            AVCodec* pCodec = ffmpeg.avcodec_find_decoder(pCodecContext->codec_id);
            ffmpeg.avcodec_open2(pCodecContext, pCodec, null);

            AVPacket* pPacket = ffmpeg.av_packet_alloc();
            ffmpeg.av_init_packet(pPacket);
            AVFrame* pFrame = ffmpeg.av_frame_alloc();

            _context = new Context
            {
                UnmanagedIOBuffer = ioBuffer,
                FormatContext = pFormatContext,
                CodecContext = pCodecContext,
                Packet = pPacket,
                CurrentFrame = pFrame,
                Stream = pFormatContext->streams[0]
            };

            OriginalBitDepth = ffmpeg.av_get_bytes_per_sample(pCodecContext->sample_fmt) * 8;
            OriginalSampleRate = pCodecContext->sample_rate;
            OriginalChannelCount = pCodecContext->channels;

            Duration = TimeSpan.FromSeconds(pFormatContext->duration / ffmpeg.AV_TIME_BASE);

            Seek(TimeSpan.FromSeconds(54));
            ReceiveFrame();

            var dts = _context.Stream->cur_dts;
            var pts = _context.Stream->pts;
            var streamTs = _context.CurrentFrame->best_effort_timestamp;
            long ms = StreamToBclTimestamp(streamTs);
        }

        internal override void OnAttachedToSource()
        {
            _targetSampleFormat = BitDepthToSampleFormat(TargetBitDepth);
            _targetBytesPerSample = ffmpeg.av_get_bytes_per_sample(_targetSampleFormat);
            SetupResampler();
        }

        private unsafe void SetupResampler()
        {
            SwrContext* pSwrContext = ffmpeg.swr_alloc();
            ffmpeg.av_opt_set_int(pSwrContext, "in_channel_count", OriginalChannelCount, 0);
            ffmpeg.av_opt_set_int(pSwrContext, "out_channel_count", TargetChannelCount, 0);
            ffmpeg.av_opt_set_int(pSwrContext, "in_channel_layout", (long)_context.CodecContext->channel_layout, 0);
            ffmpeg.av_opt_set_int(pSwrContext, "out_channel_layout", (long)_context.CodecContext->channel_layout, 0);
            ffmpeg.av_opt_set_int(pSwrContext, "in_sample_rate", OriginalSampleRate, 0);
            ffmpeg.av_opt_set_int(pSwrContext, "out_sample_rate", TargetSampleRate, 0);
            ffmpeg.av_opt_set_sample_fmt(pSwrContext, "in_sample_fmt", _context.CodecContext->sample_fmt, 0);
            ffmpeg.av_opt_set_sample_fmt(pSwrContext, "out_sample_fmt", _targetSampleFormat, 0);
            ffmpeg.swr_init(pSwrContext);

            _context.ResamplerContext = pSwrContext;
        }

        public override bool Read(AudioBuffer buffer)
        {
            do
            {
                int decodedBytes;
                if ((decodedBytes = DecodeFrame(buffer)) == 0)
                {
                    return false;
                }

                buffer.AdvancePosition(decodedBytes);
            } while (buffer.FreeSpace >= _frameSize);

            return true;
        }

        private bool shit = false;
        private int _frameNumber = 0;
        private unsafe int DecodeFrame(AudioBuffer outBuffer)
        {
            start:
            int discard = 0;

            var pFrame = _context.CurrentFrame;
            var pPacket = _context.Packet;
            int receiveResult;
            do
            {
                if (ffmpeg.av_read_frame(_context.FormatContext, pPacket) != 0)
                {
                    ffmpeg.av_packet_unref(pPacket);
                    return 0;
                }

                ThrowIfNotZero(ffmpeg.avcodec_send_packet(_context.CodecContext, pPacket));
                ffmpeg.av_packet_unref(pPacket);
                receiveResult = ffmpeg.avcodec_receive_frame(_context.CodecContext, pFrame);
            } while (receiveResult == AVError_EAgain);

            _frameSize = Math.Max(_frameSize, _targetBytesPerSample * pFrame->nb_samples * pFrame->channels);
            _positionInSamples = pFrame->best_effort_timestamp + pFrame->nb_samples;

            if (shit && LoopStart.TotalSeconds * OriginalSampleRate - _positionInSamples > pFrame->nb_samples)
            {
                goto start;
            }

            if (shit && LoopStart.TotalSeconds * OriginalSampleRate > _positionInSamples)
            {
                discard = (int)Math.Round(LoopStart.TotalSeconds * OriginalSampleRate - _positionInSamples);
                shit = false;

                int result = _targetBytesPerSample * (pFrame->nb_samples - discard) * pFrame->channels;

                byte* b = (byte*)outBuffer.CurrentPointer;
                //discard = 0;
                pFrame->extended_data[0] -= discard * _targetBytesPerSample;
                pFrame->extended_data[1] -= discard * _targetBytesPerSample;
                //byte* b =  (byte*)ffmpeg.av_malloc(44100 * 2);
                ffmpeg.swr_convert(_context.ResamplerContext, &b, pFrame->nb_samples, pFrame->extended_data, pFrame->nb_samples);

                byte* dst = (byte*)outBuffer.CurrentPointer;
                memcpy(dst, b + discard * 2, result);

                return result;
            }

            if (Looping && _positionInSamples >= LoopEnd.TotalSeconds * OriginalSampleRate)
            {
                discard = (int)Math.Round(_positionInSamples - LoopEnd.TotalSeconds * OriginalSampleRate);
                Seek(LoopStart);
                shit = true;
            }

            

            int decodedBytes = _targetBytesPerSample * (pFrame->nb_samples - discard) * pFrame->channels;

            byte* pBuf = (byte*)outBuffer.CurrentPointer;
            ffmpeg.swr_convert(_context.ResamplerContext, &pBuf, pFrame->nb_samples - discard, pFrame->extended_data, pFrame->nb_samples - discard);

            return decodedBytes;
        }

        private unsafe void ReceiveFrame()
        {
            int receiveResult;
            do
            {
                if (ffmpeg.av_read_frame(_context.FormatContext, _context.Packet) != 0)
                {
                    ffmpeg.av_packet_unref(_context.Packet);
                    return;
                }

                ThrowIfNotZero(ffmpeg.avcodec_send_packet(_context.CodecContext, _context.Packet));
                ffmpeg.av_packet_unref(_context.Packet);
                receiveResult = ffmpeg.avcodec_receive_frame(_context.CodecContext, _context.CurrentFrame);
            } while (receiveResult == AVError_EAgain);
        }

        public unsafe static void memcpy(void* dst, void* src, int count)
        {
            const int blockSize = 4096;
            byte[] block = new byte[blockSize];
            byte* d = (byte*)dst, s = (byte*)src;
            for (int i = 0, step; i < count; i += step, d += step, s += step)
            {
                step = count - i;
                if (step > blockSize)
                {
                    step = blockSize;
                }
                Marshal.Copy(new IntPtr(s), block, 0, step);
                Marshal.Copy(block, 0, new IntPtr(d), step);
            }
        }

        public override void Seek(TimeSpan timeCode)
        {
            UnsafeSeek(timeCode);
        }

        private unsafe void UnsafeSeek(TimeSpan timeCode)
        {
            long timestamp = BclToStreamTimestamp(timeCode);
            //var flags = timestamp < _positionInSamples ? ffmpeg.AVSEEK_FLAG_BACKWARD : 0;

            ffmpeg.av_seek_frame(_context.FormatContext, 0, timestamp, ffmpeg.AVSEEK_FLAG_BACKWARD);
            ffmpeg.avcodec_flush_buffers(_context.CodecContext);

            //while (_context.CurrentFrame->best_effort_timestamp)
        }

        public override void Dispose()
        {
            _context.Dispose();
            base.Dispose();
        }

        private unsafe int IOReadPacket(void* opaque, byte* buf, int buf_size)
        {
            var managed = new byte[buf_size];
            int result = FileStream.Read(managed, 0, buf_size);
            Marshal.Copy(managed, 0, (IntPtr)buf, result);
            return result;
        }

        private unsafe int IOWritePacket(void* opaque, byte* buf, int buf_size)
        {
            return -1;
        }

        private unsafe long IOSeek(void* opaque, long offset, int whence)
        {
            if (whence == ffmpeg.AVSEEK_SIZE)
            {
                return FileStream.Length;
            }

            var origin = (SeekOrigin)whence;
            return FileStream.Seek(offset, origin);
        }

        private static void ThrowIfNotZero(int result)
        {
            if (result != 0)
            {
                throw new AudioDecodingException();
            }
        }

        private static AVSampleFormat BitDepthToSampleFormat(int bitDepth)
        {
            switch (bitDepth)
            {
                case 32:
                    return AVSampleFormat.AV_SAMPLE_FMT_FLT;
                case 16:
                default:
                    return AVSampleFormat.AV_SAMPLE_FMT_S16;
            }
        }

        private TimeSpan SamplesToTimeCode(long nbSamples) => TimeSpan.FromSeconds((double)nbSamples / OriginalSampleRate);

        private unsafe class Context : IDisposable
        {
            public byte* UnmanagedIOBuffer;
            public AVFormatContext* FormatContext;
            public AVCodecContext* CodecContext;
            public SwrContext* ResamplerContext;
            internal AVPacket* Packet;
            internal AVFrame* CurrentFrame;
            public AVStream* Stream;

            public void Dispose()
            {
                Free();
                GC.SuppressFinalize(this);
            }

            ~Context()
            {
                Free();
            }

            private void Free()
            {
                ffmpeg.av_free(UnmanagedIOBuffer);
                ffmpeg.avformat_free_context(FormatContext);
                fixed (SwrContext** ppSwr = &ResamplerContext)
                {
                    ffmpeg.swr_free(ppSwr);
                }
                fixed (AVCodecContext** ppCodecContext = &CodecContext)
                {
                    ffmpeg.avcodec_free_context(ppCodecContext);
                }
            }
        }
    }
}
