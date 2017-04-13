using System;

namespace MoeGame.Framework.Graphics
{
    public class DxDrawingSession : IDisposable
    {
        private readonly DxRenderContext _rc;

        internal DxDrawingSession(DxRenderContext renderContext)
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
