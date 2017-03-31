using System;

namespace HoppyFramework.Graphics
{
    public class DXDrawingSession : IDisposable
    {
        private readonly DXRenderContext _rc;

        internal DXDrawingSession(DXRenderContext renderContext)
        {
            _rc = renderContext;
        }

        public SharpDX.Direct2D1.DeviceContext DeviceContext => _rc.DeviceContext;

        internal void Reset(RgbaValueF clearColor)
        {
            _rc.DeviceContext.BeginDraw();
            _rc.DeviceContext.Clear(clearColor);
        }

        public void Dispose()
        {
            _rc.DeviceContext.EndDraw();
            _rc.SwapChain.Present(0, SharpDX.DXGI.PresentFlags.None);
        }
    }
}
