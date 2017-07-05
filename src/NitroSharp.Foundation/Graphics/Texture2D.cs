using System;
using System.Drawing;

namespace NitroSharp.Foundation.Graphics
{
    public abstract class Texture2D : IDisposable
    {
        public abstract SizeF Size { get; }
        public abstract SizeF PixelSize { get; }

        public abstract void CopyFrom(Texture2D source);
        public abstract void Resize(SizeF newSizeInDip);
        public abstract void Dispose();
    }
}
