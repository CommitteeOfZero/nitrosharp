using System;
using System.Diagnostics;
using System.Drawing;

namespace CommitteeOfZero.NitroSharp.Foundation.Graphics
{
    public abstract class Texture2D : IDisposable
    {
        protected Texture2D(object resourceHandle)
        {
            Debug.Assert(resourceHandle != null);
            ResourceHandle = resourceHandle;
        }

        public object ResourceHandle { get; }
        public abstract SizeF Size { get; }

        public abstract void Dispose();
    }
}
