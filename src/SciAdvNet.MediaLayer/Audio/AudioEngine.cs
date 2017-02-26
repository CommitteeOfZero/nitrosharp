using System;

namespace SciAdvNet.MediaLayer.Audio
{
    public abstract class AudioEngine : IDisposable
    {
        public ResourceFactory ResourceFactory { get; protected set; }
        public abstract void Dispose();
    }
}
