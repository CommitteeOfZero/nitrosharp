using System;
using System.Runtime.CompilerServices;
using FFmpeg.AutoGen;

namespace NitroSharp.Media
{
    internal sealed class FormatContext
    {
        private unsafe AVFormatContext* _ptr;

        public unsafe FormatContext(AVFormatContext* pointer)
        {
            unsafe
            {
                _ptr = pointer;
            }
        }

        public unsafe bool IsInvalid => _ptr == null;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe AVFormatContext* Get() => _ptr;

        public void Dispose()
        {
            if (!IsInvalid)
            {
                Free();
            }

            GC.SuppressFinalize(this);
        }

        ~FormatContext()
        {
            if (!IsInvalid)
            {
                Free();
            }
        }

        private unsafe void Free()
        {
            fixed (AVFormatContext** ppFormatContext = &_ptr)
            {
                ffmpeg.avformat_close_input(ppFormatContext);
            }

            _ptr = null;
        }
    }
}
