using System;

namespace ProjectHoppy
{
    public abstract class GameSystem : IDisposable
    {
        public abstract void Update();
        public abstract void Dispose();
    }
}
