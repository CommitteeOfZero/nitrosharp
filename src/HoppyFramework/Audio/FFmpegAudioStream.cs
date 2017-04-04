using FFmpeg.AutoGen;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace HoppyFramework.Audio
{
    public class FFmpegAudioStream : AudioStream
    {
        private const uint AVError_Eof = 0xdfb9b0bb;
        private const int AVError_EAgain = -11;

        private static AVRational s_msTimeBase;

        private const int IOBufferSize = 4096;
        private byte[] _managedIOBuffer;
        private avio_alloc_context_read_packet ReadFunc;
        private avio_alloc_context_seek SeekFunc;
        private avio_alloc_context_write_packet WriteFunc;

        private Context _context;
        private AVSampleFormat _targetSampleFormat;
        private bool _planarAudio = true;
        private int _targetBytesPerSample;
        private int _maxFrameSize;

        private bool _seeking;
        private long _seekTargetInStreamUnits;

        private long _loopStartInStreamUntis;
        private long _loopEndInStreamUnits;

        static FFmpegAudioStream()
        {
            FFmpegLibraries.Init();
            s_msTimeBase = new AVRational { num = 1, den = 1000 };
        }

        public FFmpegAudioStream(Stream fileStream) : base(fileStream)
        {
            OpenStream();
        }

        private unsafe long FrameDurationInStreamUnits => NbSamplesToStreamTime(_context.CurrentFrame->nb_samples);
        private unsafe long PositionInStreamUnits => _context.CurrentFrame->best_effort_timestamp;

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
        }

        internal override void OnAttachedToSource()
        {
            _targetSampleFormat = BitDepthToSampleFormat(TargetBitDepth);
            _targetBytesPerSample = ffmpeg.av_get_bytes_per_sample(_targetSampleFormat);
            SetupResampler();
        }

        public unsafe void SetupResampler()
        {
            _targetSampleFormat = BitDepthToSampleFormat(TargetBitDepth);
            _targetBytesPerSample = ffmpeg.av_get_bytes_per_sample(_targetSampleFormat);

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

        private static int ChannelCountToLayout(int channelCount)
        {
            return ffmpeg.AV_CH_LAYOUT_STEREO;
        }

        public override bool Read(AudioBuffer buffer)
        {
            do
            {
                if (_seeking)
                {
                    // av_seek_frame isn't always precise enough.
                    // Sometimes it's required to call av_read_frame several times after seeking
                    // in order to reach the frame that contains the specified timestamp.
                    while (!Timestamp_IsInCurrentFrame(_seekTargetInStreamUnits))
                    {
                        if (ReadFrame() == AVError_Eof)
                        {
                            return false;
                        }
                    }

                    ReadFrame();
                    _seeking = false;
                }
                else if (Looping)
                {
                    // If LoopEnd is in this frame, we need to read only a certain part of it,
                    // and then set the position to LoopStart.
                    if (Timestamp_IsInCurrentFrame(_loopEndInStreamUnits))
                    {
                        Seek(LoopStart);
                        continue;
                    }
                }

                if (ReadFrame() == AVError_Eof)
                {
                    return false;
                }

                unsafe
                {
                    int written = WriteToBuffer(_context.CurrentFrame, buffer, _context.CurrentFrame->nb_samples);
                    buffer.AdvancePosition(written);
                    _maxFrameSize = Math.Max(_maxFrameSize, written);
                }

                // Potential buffer overflow, nya.
                // Shall not pass any code review.
            } while (buffer.FreeSpace >= _maxFrameSize);

            return true;
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

        private unsafe void IncrementDataPointers(int incrementBy)
        {
            if (incrementBy == 0)
            {
                return;
            }

            if (_planarAudio)
            {
                for (int i = 0; i < OriginalChannelCount; i++)
                {
                    _context.CurrentFrame->extended_data[i] += incrementBy;
                    //_context.CurrentFrame->data[(uint)i] += incrementBy;
                }
            }
            else
            {
                _context.CurrentFrame->extended_data[0] += incrementBy;
                //_context.CurrentFrame->data[0] += incrementBy;
            }
        }

        private unsafe uint ReadFrame()
        {
            uint readResult;
            int receiveResult;
            do
            {
                if ((readResult = (uint)ffmpeg.av_read_frame(_context.FormatContext, _context.Packet)) != 0)
                {
                    ffmpeg.av_packet_unref(_context.Packet);
                    return readResult == AVError_Eof || readResult == 0xfffffffb ? AVError_Eof : throw EncodingFailed("av_read_frame() returned a non-zero value.");
                }
                try
                {
                    ThrowIfNotZero(ffmpeg.avcodec_send_packet(_context.CodecContext, _context.Packet));
                    receiveResult = ffmpeg.avcodec_receive_frame(_context.CodecContext, _context.CurrentFrame);
                }
                catch
                {
                    throw;
                }
                finally
                {
                    ffmpeg.av_packet_unref(_context.Packet);
                }
            } while (receiveResult == AVError_EAgain);

            ThrowIfNotZero(receiveResult);
            return 0;
        }

        private unsafe int WriteToBuffer(AVFrame* frame, AudioBuffer buffer, int inputSampleCount)
        {
            byte* pBuf = (byte*)buffer.CurrentPointer;

            int outSampleCount = inputSampleCount;
            if (OriginalSampleRate != TargetSampleRate)
            {
                long delay = ffmpeg.swr_get_delay(_context.ResamplerContext, OriginalSampleRate);
                outSampleCount = (int)ffmpeg.av_rescale_rnd(inputSampleCount, TargetSampleRate, OriginalSampleRate, AVRounding.AV_ROUND_DOWN);
            }

            ffmpeg.swr_convert(_context.ResamplerContext, &pBuf, outSampleCount, frame->extended_data, inputSampleCount);
            //dst_bufsize = av_samples_get_buffer_size(&dst_linesize, dst_nb_channels, ret, dst_sample_fmt, 1);

            //return ffmpeg.av_samples_get_buffer_size()
            return _targetBytesPerSample * outSampleCount * frame->channels;
        }

        public override void Seek(TimeSpan timeCode)
        {
            long timestamp = BclToStreamTimestamp(timeCode);
            Seek(timestamp);
        }

        private unsafe void Seek(long streamTimestamp)
        {
            ffmpeg.av_seek_frame(_context.FormatContext, 0, streamTimestamp, ffmpeg.AVSEEK_FLAG_BACKWARD);
            ffmpeg.avcodec_flush_buffers(_context.CodecContext);

            _seeking = true;
            _seekTargetInStreamUnits = streamTimestamp;
        }

        public override void SetLoop(TimeSpan loopStart, TimeSpan loopEnd)
        {
            base.SetLoop(loopStart, loopEnd);
            _loopStartInStreamUntis = BclToStreamTimestamp(loopStart);
            _loopEndInStreamUnits = BclToStreamTimestamp(loopEnd);
        }

        private bool Timestamp_IsInCurrentFrame(long streamTimestamp)
        {
            if (PositionInStreamUnits < 0)
            {
                return false;
            }

            long nextFramePos = PositionInStreamUnits + FrameDurationInStreamUnits;
            return streamTimestamp >= PositionInStreamUnits && nextFramePos >= streamTimestamp;
        }

        private unsafe TimeSpan StreamToBclTimestamp(long streamTimestamp)
        {
            var streamTimeBase = _context.Stream->time_base;
            long ms = ffmpeg.av_rescale_q(streamTimestamp, streamTimeBase, s_msTimeBase);
            return TimeSpan.FromMilliseconds(ms);
        }

        private unsafe long BclToStreamTimestamp(TimeSpan timeSpan)
        {
            var streamTimeBase = _context.Stream->time_base;
            long msRounded = (long)Math.Round(timeSpan.TotalMilliseconds);
            return ffmpeg.av_rescale_q(msRounded, s_msTimeBase, streamTimeBase);
        }

        private unsafe long NbSamplesToStreamTime(int nbSamples)
        {
            var streamTimeBase = _context.Stream->time_base;
            var originalTimeBase = new AVRational { num = 1, den = OriginalSampleRate };
            return ffmpeg.av_rescale_q(nbSamples, originalTimeBase, streamTimeBase);
        }

        private unsafe int StreamTimeToSamples(long streamTime)
        {
            var streamTimeBase = _context.Stream->time_base;
            var newTimeBase = new AVRational { num = 1, den = OriginalSampleRate };
            return (int)ffmpeg.av_rescale_q(streamTime, streamTimeBase, newTimeBase);
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

        public override void Dispose()
        {
            _context.Dispose();
            base.Dispose();
        }

        private static void ThrowIfNotZero(int result)
        {
            if (result != 0)
            {
                throw new AudioDecodingException();
            }
        }

        private Exception EncodingFailed(string details)
        {
            return new AudioDecodingException($"ERRA: {details}");
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

        private unsafe class Context : IDisposable
        {
            public byte* UnmanagedIOBuffer;
            public AVFormatContext* FormatContext;
            public AVCodecContext* CodecContext;
            public SwrContext* ResamplerContext;
            public AVPacket* Packet;
            public AVFrame* CurrentFrame;
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
