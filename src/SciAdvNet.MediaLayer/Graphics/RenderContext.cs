using System;
using System.Collections.Generic;

namespace SciAdvNet.MediaLayer.Graphics
{
    public abstract class RenderContext : IDisposable
    {
        private List<DeviceResource> _gpuResources;

        protected RenderContext()
        {
            _gpuResources = new List<DeviceResource>();
        }

        public ResourceFactory ResourceFactory { get; protected set; }
        public abstract DrawingSession NewSession(RgbaValueF clearColor);

        internal void AddDeviceResource(DeviceResource resource)
        {
            _gpuResources.Add(resource);
        }

        internal void RemoveDeviceResource(DeviceResource resource)
        {
            _gpuResources.Remove(resource);
        }

        public virtual void Dispose()
        {
            var resources = _gpuResources.ToArray();
            foreach (var resource in resources)
            {
                resource.Dispose();
            }
        }
    }
}
