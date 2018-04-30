using System;
using System.Collections.Generic;
using FFmpeg.AutoGen;

namespace NitroSharp.Media
{
    internal sealed class DecoderCollection : IDisposable
    {
        private readonly Dictionary<AVCodecID, IntPtr> _decoders;

        public DecoderCollection()
        {
            _decoders = new Dictionary<AVCodecID, IntPtr>();
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

        public void Dispose()
        {
            _decoders.Clear();
        }
    }
}
