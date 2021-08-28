using System;
using System.Collections.Concurrent;
using FFmpeg.AutoGen;

namespace NitroSharp.Media
{
    internal sealed unsafe class DecoderCollection
    {
        public static readonly DecoderCollection Shared = new();

        private readonly ConcurrentDictionary<AVCodecID, IntPtr> _decoders;

        private DecoderCollection()
        {
            _decoders = new ConcurrentDictionary<AVCodecID, IntPtr>();
        }

        public void Preload(AVCodecID codecId)
        {
            Get(codecId);
        }

        public AVCodec* Get(AVCodecID codecId)
        {
            if (!_decoders.TryGetValue(codecId, out IntPtr decoder))
            {
                AVCodec* pCodec = ffmpeg.avcodec_find_decoder(codecId);
                if (pCodec is null)
                {
                    ThrowMissing(codecId);
                }
                decoder = new IntPtr(pCodec);
                _decoders[codecId] = decoder;
            }

            return (AVCodec*)decoder;
        }

        private static void ThrowMissing(AVCodecID codecId)
            => throw new FFmpegException($"Decoder '{codecId}' is missing.");
    }
}
