using System;

namespace NitroSharp.Foundation.Graphics
{
    public class DxDrawingSession : IDisposable
    {
        private readonly DxRenderContext _rc;
        private bool _presentOnDispose;

        internal DxDrawingSession(DxRenderContext renderContext)
        {
            _rc = renderContext;
        }

        internal void Reset(RgbaValueF clearColor, bool present)
        {
            _presentOnDispose = present;
            _rc.DeviceContext.BeginDraw();
            _rc.DeviceContext.Clear(clearColor);
        }

        public void Dispose()
        {
            _rc.DeviceContext.EndDraw();
            if (_presentOnDispose)
            {
                _rc.Present();
            }
        }
    }
}
