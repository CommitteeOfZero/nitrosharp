using System;
using System.Drawing;

namespace NitroSharp.Foundation.Graphics
{
    public abstract class Texture2D : IDisposable
    {
        public abstract SizeF Size { get; }

        public abstract void CopyFrom(Texture2D source);
        public abstract void Dispose();
    }
}
