using System;
using FFmpeg.AutoGen;

namespace NitroSharp.Media.Decoding
{
    public sealed unsafe class Resampler
    {
        private const AVSampleFormat OutputSampleFormat = AVSampleFormat.AV_SAMPLE_FMT_S16;

        private readonly AudioParameters _outputParameters;
        private SwrContext* _ctx;

        public Resampler(AVCodecContext* codec, in AudioParameters outputParameters)
        {
            _outputParameters = outputParameters;
            unsafe
            {
                _ctx = ffmpeg.swr_alloc_set_opts(null,
                    FFmpegUtil.GetAVChannelLayout(_outputParameters.ChannelLayout),
                    OutputSampleFormat, (int)_outputParameters.SampleRate,
                    (long)codec->channel_layout, codec->sample_fmt, codec->sample_rate, 0, null);

                ffmpeg.av_opt_set_int(_ctx, "in_channel_count", codec->channels, 0);
                ffmpeg.av_opt_set_int(_ctx, "out_channel_count", _outputParameters.ChannelCount, 0);
                ffmpeg.swr_init(_ctx);
            }
        }

        public int GetExpectedBufferSize(ref AVFrame srcFrame)
        {
            long delay = ffmpeg.swr_get_delay(_ctx, srcFrame.sample_rate);
            int outSampleCount = (int)ffmpeg.av_rescale_rnd(
                srcFrame.nb_samples + delay, _outputParameters.SampleRate,
                srcFrame.sample_rate, AVRounding.AV_ROUND_UP);

            int value = ffmpeg.av_samples_get_buffer_size(
                null, _outputParameters.ChannelCount, outSampleCount, OutputSampleFormat, 1);
            return value > 0 ? value : 0;
        }

        public int GetOutputBufferSize(int nbSamples)
        {
            int size = ffmpeg.av_samples_get_buffer_size(
                null, _outputParameters.ChannelCount, nbSamples, OutputSampleFormat, 1);

            return size > 0 ? size : 0;
        }

        public unsafe int Convert(ref AVFrame srcFrame, byte* dstBuffer)
        {
            SwrContext* ctx = _ctx;

            long delay = ffmpeg.swr_get_delay(ctx, srcFrame.sample_rate);
            int outSampleCount = (int)ffmpeg.av_rescale_rnd(
                srcFrame.nb_samples + delay, _outputParameters.SampleRate,
                srcFrame.sample_rate, AVRounding.AV_ROUND_UP);

            byte* pBuf = dstBuffer;
            int actualSamplesPerChannel = ffmpeg.swr_convert(
                ctx, &pBuf, outSampleCount, srcFrame.extended_data, srcFrame.nb_samples);

            int bytesWritten = ffmpeg.av_samples_get_buffer_size(
                null, _outputParameters.ChannelCount, actualSamplesPerChannel, OutputSampleFormat, 1);

            return bytesWritten < 0 ? 0 : bytesWritten;
        }

        public void Dispose()
        {
            Free();
            GC.SuppressFinalize(this);
        }

        ~Resampler()
        {
            Free();
        }

        private void Free()
        {
            fixed (SwrContext** ppCtx = &_ctx)
            {
                ffmpeg.swr_free(ppCtx);
            }
            _ctx = null;
        }
    }
}
