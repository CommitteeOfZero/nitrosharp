using System;
using FreeTypeBindings;

namespace NitroSharp.Text
{
    internal sealed class FreeTypeInstance : IDisposable
    {
        private IntPtr _handle;

        public FreeTypeInstance()
        {
            FT.FT_Init_FreeType(out _handle);
        }

        public IntPtr Handle => _handle;

        public void Dispose()
        {
            Destroy();
            GC.SuppressFinalize(this);
        }

        private void Destroy()
        {
            FT.FT_Done_FreeType(_handle);
            _handle = IntPtr.Zero;
        }

        ~FreeTypeInstance()
        {
            Destroy();
        }
    }
}
