using System;

namespace SciAdvNet.MediaLayer.Graphics
{
    public abstract class DeviceResource : IDisposable
    {
        protected readonly RenderContext _rcBase;

        protected DeviceResource(RenderContext renderContext)
        {
            _rcBase = renderContext;
            _rcBase.AddDeviceResource(this);
        }

        public virtual void Dispose()
        {
            _rcBase.RemoveDeviceResource(this);
        }
    }
}
