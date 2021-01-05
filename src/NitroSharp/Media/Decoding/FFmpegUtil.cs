using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using FFmpeg.AutoGen;

namespace NitroSharp.Media.Decoding
{
    internal static class FFmpegUtil
    {
        private static readonly object s_lock = new();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void OpenStream(AVCodecContext* ctx, AVCodec* codec)
        {
            lock (s_lock)
            {
                ThrowIfNotZero(ffmpeg.avcodec_open2(ctx, codec, null));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowIfNotZero(int result)
        {
            if (result != 0)
            {
                throw new FFmpegException(GetErrorMessage(result));
            }
        }

        public static string GetErrorMessage(int error)
        {
            unsafe
            {
                const int bufferSize = 1024;
                byte* buffer = stackalloc byte[bufferSize];
                ffmpeg.av_strerror(error, buffer, bufferSize);
                return Marshal.PtrToStringAnsi((IntPtr)buffer);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsEofPacket(ref AVPacket packet)
        {
            unsafe
            {
                return packet.data == null;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void FrameMoveRef(ref AVFrame dst, ref AVFrame src)
        {
            unsafe
            {
                fixed (AVFrame* srcPtr = &src)
                fixed (AVFrame* dstPtr = &dst)
                {
                    ffmpeg.av_frame_move_ref(dstPtr, srcPtr);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void PacketMoveRef(ref AVPacket dst, ref AVPacket src)
        {
            unsafe
            {
                fixed (AVPacket* srcPtr = &src)
                fixed (AVPacket* dstPtr = &dst)
                {
                    ffmpeg.av_packet_move_ref(dstPtr, srcPtr);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void UnrefBuffers(ref AVPacket packet)
        {
            unsafe
            {
                fixed (AVPacket* ptr = &packet)
                {
                    ffmpeg.av_packet_unref(ptr);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void UnrefBuffers(ref AVFrame frame)
        {
            unsafe
            {
                fixed (AVFrame* ptr = &frame)
                {
                    ffmpeg.av_frame_unref(ptr);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double RebaseTimestamp(long timestamp, AVRational timeBase)
        {
            return timestamp * (timeBase.num / (double)timeBase.den);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long GetAVChannelLayout(ChannelLayout channelLayout)
        {
            return channelLayout == ChannelLayout.Mono
                ? ffmpeg.AV_CH_LAYOUT_MONO
                : ffmpeg.AV_CH_LAYOUT_STEREO;
        }
    }
}
