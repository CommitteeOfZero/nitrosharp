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

        internal void Reset(RgbaValueF clearColor)
        {
            _rc.DeviceContext.BeginDraw();
            _rc.DeviceContext.Clear(clearColor);
        }

        public void Dispose()
        {
            _rc.DeviceContext.EndDraw();
            _rc.SwapChain.Present(1, SharpDX.DXGI.PresentFlags.None);
        }
    }
}
