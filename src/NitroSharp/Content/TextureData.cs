using System;
using NitroSharp.Primitives;

namespace NitroSharp.Content
{
    internal abstract class TextureData : IDisposable
    {
        public abstract Size Size { get; }

        public abstract void CopyPixels(IntPtr buffer);
        public abstract void CopyPixels(IntPtr buffer, uint size);
        public abstract void CopyPixels(IntPtr buffer, in Rectangle rectangle);

        public abstract void Dispose();
    }
}
