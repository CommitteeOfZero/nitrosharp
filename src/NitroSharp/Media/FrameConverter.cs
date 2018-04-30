using System;
using System.Collections.Generic;
using FFmpeg.AutoGen;
using NitroSharp.Primitives;

namespace NitroSharp.Media
{
    internal sealed class FrameConverter : IDisposable
    {
        private HashSet<IntPtr> _contexts;

        public unsafe FrameConverter()
        {
            _contexts = new HashSet<IntPtr>();
        }

        public unsafe void ConvertToRgba(AVFrame* srcAvFrame, Size dstSize, byte* dstBuffer)
        {
            var dstFormat = AVPixelFormat.AV_PIX_FMT_RGBA;
            var ctx = ffmpeg.sws_getCachedContext(
                null, srcAvFrame->width, srcAvFrame->height, (AVPixelFormat)srcAvFrame->format,
                (int)dstSize.Width, (int)dstSize.Height, dstFormat, 0, null, null, null);

            _contexts.Add(new IntPtr(ctx));

            byte_ptrArray4 planes;
            int_array4 linesizes;
            planes[0] = dstBuffer;
            linesizes[0] = ffmpeg.av_image_get_linesize(dstFormat, (int)dstSize.Width, 0);

            int r = ffmpeg.sws_scale(
                ctx, srcAvFrame->data, srcAvFrame->linesize, 0, srcAvFrame->height, planes, linesizes);
        }

        public void Dispose()
        {
            Free();
            GC.SuppressFinalize(this);
        }

        ~FrameConverter()
        {
            Free();
        }

        private void Free()
        {
            foreach (IntPtr ctx in _contexts)
            {
                unsafe
                {
                    SwsContext* pCtx = (SwsContext*)ctx.ToPointer();
                    ffmpeg.sws_freeContext(pCtx);
                }
            }

            _contexts.Clear();
        }
    }
}
