using System;
using System.Runtime.CompilerServices;
using NitroSharp.Primitives;
using NitroSharp.Utilities;

namespace NitroSharp.Content
{
    internal sealed class FFmpegTextureData : TextureData
    {
        private readonly NativeMemory _data;

        public FFmpegTextureData(NativeMemory data, Size size)
        {
            _data = data;
            Size = size;
        }

        public override Size Size { get; }

        public override void CopyPixels(IntPtr buffer)
        {
            unsafe
            {
                Unsafe.CopyBlock((void*)buffer, (void*)_data.Pointer, _data.Size);
            }
        }

        public override void CopyPixels(IntPtr buffer, uint size)
        {
            throw new NotImplementedException();
        }

        public override void CopyPixels(IntPtr buffer, in Rectangle rectangle)
        {
            throw new NotImplementedException();
        }

        public override void Dispose()
        {
            _data.Dispose();
        }
    }
}
