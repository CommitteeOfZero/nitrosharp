using SharpDX.Direct2D1;
using System;

namespace NitroSharp.Foundation.Graphics
{
    public class DxDrawingSession : IDisposable
    {
        private readonly DxRenderContext _rc;
        private readonly bool _vsync;
        private bool _presentOnDispose;

        internal DxDrawingSession(DxRenderContext renderContext, bool vsync)
        {
            _rc = renderContext;
            _vsync = vsync;
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
                var mt = _rc.D2DFactory.QueryInterface<Multithread>();
                mt.Enter();
                _rc.SwapChain.Present(_vsync ? 1 : 0, SharpDX.DXGI.PresentFlags.None);
                mt.Leave();
            }
        }
    }
}
