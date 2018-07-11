using System;
using FFmpeg.AutoGen;

namespace NitroSharp.Media.Decoding
{
    internal sealed class AudioProcessor : MediaProcessor
    {
        private const uint MinBufferSize = 16384 * 2;

        private readonly Resampler _resampler;

        public unsafe AudioProcessor(AVStream* stream, Resampler resampler)
            : base(stream, DetermineBufferSize(resampler, stream))
        {
            _resampler = resampler;
        }

        protected override uint BufferPoolSize => 20;

        public override uint GetExpectedOutputBufferSize(ref AVFrame srcFrame)
        {
            return (uint)_resampler.GetExpectedBufferSize(ref srcFrame);
        }

        public override unsafe int ProcessFrame(ref AVFrame frame, ref PooledBuffer outBuffer)
        {
            return _resampler.Convert(ref frame, (byte*)outBuffer.Data + outBuffer.Position);
        }

        private static unsafe uint DetermineBufferSize(Resampler resampler, AVStream* stream)
        {
            uint calculatedSize = (uint)resampler.GetOutputBufferSize((int)(stream->codecpar->sample_rate * 0.1));
            return Math.Max(MinBufferSize, calculatedSize);
        }

        public override void Dispose()
        {
            _resampler.Dispose();
            base.Dispose();
        }
    }
}
