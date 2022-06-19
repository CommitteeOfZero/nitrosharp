using System;
using System.Diagnostics;
using FFmpeg.AutoGen;
using static NitroSharp.Media.FFmpegUtil;

namespace NitroSharp.Media
{
    internal readonly record struct BufferRequirements(int SamplesPerChannel, int SizeInBytes);

    internal unsafe class Resampler : IDisposable
    {
        private const AVSampleFormat OutSampleFormat = AVSampleFormat.AV_SAMPLE_FMT_S16;

        private readonly AudioParameters _outParams;
        private SwrContext* _ctx;

        public Resampler(AVCodecContext* codecCtx, in AudioParameters outParams)
        {
            _outParams = outParams;
            _ctx = ffmpeg.swr_alloc_set_opts(
                null,
                GetAvChannelLayout(outParams.ChannelLayout),
                OutSampleFormat,
                (int)outParams.SampleRate,
                (long)codecCtx->channel_layout,
                codecCtx->sample_fmt, codecCtx->sample_rate,
                0, null
            );

            CheckResult(
                ffmpeg.av_opt_set_int(_ctx, "in_channel_count", codecCtx->channels, 0)
            );
            CheckResult(
                ffmpeg.av_opt_set_int(_ctx, "out_channel_count", outParams.ChannelCount, 0)
            );
            CheckResult(ffmpeg.swr_init(_ctx));
        }

        public int GetBufferSize(TimeSpan duration)
        {
            int nbSamples = (int)Math.Round(duration.TotalSeconds * _outParams.SampleRate);
            return nbSamples * _outParams.ChannelCount * 2;
        }

        public BufferRequirements GetBufferRequirements(in AVFrame frame)
        {
            Debug.Assert(frame.nb_samples > 0);
            SwrContext* ctx = _ctx;
            long delay = ffmpeg.swr_get_delay(ctx, frame.sample_rate);
            int samplesPerCh = (int)ffmpeg.av_rescale_rnd(
                frame.nb_samples + delay,
                _outParams.SampleRate, frame.sample_rate,
                AVRounding.AV_ROUND_UP
            );
            int bufferSize = ffmpeg.av_samples_get_buffer_size(
                null, _outParams.ChannelCount,
                samplesPerCh,
                OutSampleFormat, 1
            );
            Debug.Assert(bufferSize > 0);
            return new BufferRequirements(samplesPerCh, bufferSize);
        }

        public int Convert(in AVFrame srcFrame, BufferRequirements bufferRequirements, Span<byte> dstBuffer)
        {
            Debug.Assert(srcFrame.nb_samples > 0);
            Debug.Assert(dstBuffer.Length >= bufferRequirements.SizeInBytes);
            SwrContext* ctx = _ctx;
            fixed (byte* dst = &dstBuffer[0])
            {
                byte_ptrArray8 data = srcFrame.data;
                int actualSamplesPerChannel = ffmpeg.swr_convert(
                    ctx, &dst,
                    bufferRequirements.SamplesPerChannel,
                    (byte**)&data,
                    srcFrame.nb_samples
                );
                int bytesWritten = ffmpeg.av_samples_get_buffer_size(
                    null, _outParams.ChannelCount,
                    actualSamplesPerChannel,
                    OutSampleFormat, 1
                );

                return bytesWritten < 0 ? 0 : bytesWritten;
            }
        }

        public void Dispose()
        {
            fixed (SwrContext** ppCtx = &_ctx)
            {
                ffmpeg.swr_free(ppCtx);
            }
            _ctx = null;
        }
    }
}
