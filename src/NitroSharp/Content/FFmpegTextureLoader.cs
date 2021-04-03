using System;
using System.IO;
using FFmpeg.AutoGen;
using NitroSharp.Graphics;
using NitroSharp.Media;
using Veldrid;
using static NitroSharp.Media.FFmpegUtil;

namespace NitroSharp.Content
{
    internal sealed unsafe class FFmpegTextureLoader : TextureLoader
    {
        public FFmpegTextureLoader(GraphicsDevice graphicsDevice)
            : base(graphicsDevice)
        {
        }

        protected override Texture LoadStaging(Stream stream)
        {
            stream.Seek(0, SeekOrigin.Begin);
            using var context = new FormatContext(stream);
            AVFrame* frame = ffmpeg.av_frame_alloc();
            int streamId = ffmpeg.av_find_best_stream(
                context.Inner,
                AVMediaType.AVMEDIA_TYPE_VIDEO,
                -1, -1,
                null, 0
            );

            AVCodecContext* codecCtx = context.OpenStream(streamId);
            CheckResult(ffmpeg.av_read_frame(context.Inner, context.RecvPacket));
            CheckResult(ffmpeg.avcodec_send_packet(codecCtx, context.RecvPacket));
            CheckResult(ffmpeg.avcodec_receive_frame(codecCtx, frame));

            (uint width, uint height) = ((uint)frame->width, (uint)frame->height);
            Texture stagingTexture = _rf.CreateTexture(TextureDescription.Texture2D(
                width, height, mipLevels: 1, arrayLayers: 1,
                PixelFormat.R8_G8_B8_A8_UNorm, TextureUsage.Staging
            ));
            MappedResource map = _gd.Map(stagingTexture, MapMode.Write);

            if (frame->format == (int)AVPixelFormat.AV_PIX_FMT_RGBA)
            {
                GraphicsUtils.CopyTextureRegion(
                    src: frame->data[0], 0, 0, 0,
                    srcRowPitch: (uint)frame->linesize[0],
                    srcDepthPitch: (uint)frame->linesize[0] * height ,
                    dst: map.Data.ToPointer(), 0, 0, 0,
                    dstRowPitch: map.RowPitch, dstDepthPitch: map.DepthPitch,
                    width, height, depth: 1, bytesPerPixel: 4
                );
            }
            else
            {
                SwsContext* swsContext = ffmpeg.sws_getContext(
                    frame->width, frame->height, (AVPixelFormat)frame->format,
                    frame->width, frame->height, AVPixelFormat.AV_PIX_FMT_RGBA,
                    flags: 0, null, null, null
                );

                Span<IntPtr> srcPlanes = stackalloc IntPtr[8];
                frame->data.CopyPointers(srcPlanes);
                Span<int> srcLinesizes = stackalloc int[8];
                frame->linesize.CopyTo(srcLinesizes);

                Span<IntPtr> dstPlanes = stackalloc IntPtr[4];
                dstPlanes[0] = map.Data;
                Span<int> dstLinesizes = stackalloc int[4];
                dstLinesizes[0] = (int)map.RowPitch;

                fixed (IntPtr* pSrcPlanes = &srcPlanes[0])
                fixed (IntPtr* pDstPlanes = &dstPlanes[0])
                fixed (int* pSrcLinesizes = &srcLinesizes[0])
                fixed (int* pDstLinesizes = &dstLinesizes[0])
                {
                    int _ = ffmpeg.sws_scale(swsContext,
                        (byte**)pSrcPlanes, pSrcLinesizes,
                        srcSliceY: 0, frame->height,
                        (byte**)pDstPlanes, pDstLinesizes
                    );
                }
                ffmpeg.sws_freeContext(swsContext);
            }
            _gd.Unmap(stagingTexture);
            ffmpeg.av_frame_free(&frame);
            ffmpeg.avcodec_free_context(&codecCtx);
            return stagingTexture;
        }

        public override Size GetTextureSize(Stream stream)
        {
            stream.Seek(0, SeekOrigin.Begin);
            using var context = new FormatContext(stream);
            int streamId = ffmpeg.av_find_best_stream(
                context.Inner,
                AVMediaType.AVMEDIA_TYPE_VIDEO,
                -1, -1,
                null, 0
            );
            AVCodecParameters* codecpar = context.Inner->streams[streamId]->codecpar;
            return new Size((uint)codecpar->width, (uint)codecpar->height);
        }
    }
}
