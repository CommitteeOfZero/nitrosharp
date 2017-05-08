using System;
using System.Diagnostics;

namespace MoeGame.Framework.Graphics
{
    public class DxDrawingSession : IDisposable
    {
        private readonly DxRenderContext _rc;
        private readonly bool _vsync;

        internal DxDrawingSession(DxRenderContext renderContext, bool vsync)
        {
            _rc = renderContext;
            _vsync = vsync;
        }

        internal void Reset(RgbaValueF clearColor)
        {
            _rc.DeviceContext.BeginDraw();
            _rc.DeviceContext.Clear(clearColor);
        }

        public void Dispose()
        {
            _rc.DeviceContext.EndDraw();
            var result = _rc.SwapChain.TryPresent(_vsync ? 1 : 0, SharpDX.DXGI.PresentFlags.None);

            if (result.Failure)
            {
                Debugger.Break();
            }
        }
    }
}
