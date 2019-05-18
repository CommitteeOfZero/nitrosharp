using System;
using System.Collections.Generic;
using FFmpeg.AutoGen;
using NitroSharp.Primitives;
using NitroSharp.Utilities;

namespace NitroSharp.Media.Decoding
{
    public sealed unsafe class VideoFrameConverter : IDisposable
    {
        private const AVPixelFormat OutputFormat = AVPixelFormat.AV_PIX_FMT_RGBA;

        private readonly Dictionary<CacheKey, IntPtr> _ctxCache;

        private readonly struct CacheKey : IEquatable<CacheKey>
        {
            public readonly AVPixelFormat SrcPixelFormat;
            public readonly Size SrcSize;
            public readonly Size DstSize;

            public CacheKey(AVPixelFormat srcPixelFormat, Size srcSize, Size dstSize)
            {
                SrcPixelFormat = srcPixelFormat;
                SrcSize = srcSize;
                DstSize = dstSize;
            }

            public override bool Equals(object obj) => obj is CacheKey key && Equals(key);

            public bool Equals(CacheKey other)
            {
                return SrcPixelFormat == other.SrcPixelFormat
                    && SrcSize == other.SrcSize
                    && DstSize == other.DstSize;
            }

            public static bool operator ==(CacheKey x, CacheKey y) => x.Equals(y);
            public static bool operator !=(CacheKey x, CacheKey y) => !x.Equals(y);

            public override int GetHashCode()
            {
                return HashHelper.Combine(
                    (int)SrcPixelFormat,
                    SrcSize.GetHashCode(),
                    DstSize.GetHashCode());
            }
        }

        private readonly byte*[] _srcPlanes;
        private readonly byte*[] _dstPlanes;
        private readonly int[] _srcLinesizes;
        private readonly int[] _dstLinesizes;

        public VideoFrameConverter()
        {
            _ctxCache = new Dictionary<CacheKey, IntPtr>();
            _srcLinesizes = new int[8];
            _dstLinesizes = new int[1];
            _srcPlanes = new byte*[8];
            _dstPlanes = new byte*[1];
        }

        public void ConvertToRgba(ref AVFrame srcAvFrame, Size targetResolution, byte* dstBuffer)
        {
            int_array8 linesizes = srcAvFrame.linesize;
            for (uint i = 0; i < 8; i++)
            {
                int value = linesizes[i];
                if (value == 0)
                {
                    break;
                }

                _srcLinesizes[i] = value;
            }
            byte_ptrArray8 srcData = srcAvFrame.data;
            for (uint i = 0; i < 8; i++)
            {
                byte* ptr = srcData[i];
                if (ptr == null)
                {
                    break;
                }

                _srcPlanes[i] = srcData[i];
            }

            _dstLinesizes[0] = ffmpeg.av_image_get_linesize(OutputFormat, (int)targetResolution.Width, 0);
            _dstPlanes[0] = dstBuffer;

            SwsContext* ctx = GetContext(ref srcAvFrame, targetResolution);
            ffmpeg.sws_scale(
                ctx, _srcPlanes, _srcLinesizes, 0, srcAvFrame.height, _dstPlanes, _dstLinesizes);
        }

        private SwsContext* GetContext(ref AVFrame frame, Size dstSize)
        {
            var key = new CacheKey(
                (AVPixelFormat)frame.format,
                new Size((uint)frame.width, (uint)frame.height), dstSize);

            if (!_ctxCache.TryGetValue(key, out IntPtr pointer))
            {
                pointer = (IntPtr)ffmpeg.sws_getCachedContext(
                    null, frame.width, frame.height, (AVPixelFormat)frame.format,
                    (int)dstSize.Width, (int)dstSize.Height, OutputFormat, 0, null, null, null);
                _ctxCache[key] = pointer;
            }

            return (SwsContext*)pointer;
        }

        public void Dispose()
        {
            Free();
            GC.SuppressFinalize(this);
        }

        ~VideoFrameConverter()
        {
            Free();
        }

        private void Free()
        {
            unsafe
            {
                foreach (SwsContext* ctx in _ctxCache.Values)
                {
                    ffmpeg.sws_freeContext(ctx);
                }
                _ctxCache.Clear();
            }
        }
    }
}
