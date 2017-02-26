using SciAdvNet.MediaLayer.Graphics.DirectX;
using SciAdvNet.MediaLayer.Platform;
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

        internal GraphicsBackend Backend { get; }
        public ResourceFactory ResourceFactory { get; protected set; }
        public abstract DrawingSession NewSession(Color clearColor);

        public static RenderContext Create(GraphicsBackend backend, Window window)
        {
            switch (backend)
            {
                case GraphicsBackend.DirectX:
                default:
                    return new DXRenderContext(window);
            }
        }

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
