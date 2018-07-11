using System;
using System.Collections.Concurrent;
using FFmpeg.AutoGen;

namespace NitroSharp.Media.Decoding
{
    public sealed class DecoderCollection
    {
        public static readonly DecoderCollection Shared = new DecoderCollection();

        private readonly ConcurrentDictionary<AVCodecID, IntPtr> _decoders;

        private DecoderCollection()
        {
            _decoders = new ConcurrentDictionary<AVCodecID, IntPtr>();
        }

        public void Preload(AVCodecID codecId)
        {
            unsafe
            {
                Get(codecId);
            }
        }

        public unsafe AVCodec* Get(AVCodecID codecId)
        {
            if (!_decoders.TryGetValue(codecId, out var decoder))
            {
                AVCodec* pCodec = ffmpeg.avcodec_find_decoder(codecId);
                ThrowIfMissing(pCodec, codecId);
                decoder = new IntPtr(pCodec);
                _decoders[codecId] = decoder;
            }

            return (AVCodec*)decoder;
        }

        private static unsafe void ThrowIfMissing(AVCodec* codec, AVCodecID codecId)
        {
            if (codec == null)
            {
                throw new FFmpegException($"Decoder '{codecId}' is missing.");
            }
        }
    }
}
