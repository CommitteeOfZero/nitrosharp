using System;
using System.IO.Pipelines;
using System.Threading.Tasks;

namespace NitroSharp.Media
{
    internal abstract class AudioSource : IAsyncDisposable
    {
        public abstract bool IsPlaying { get; }
        public abstract double SecondsElapsed { get; }
        public abstract float Volume { get; set; }

        public abstract void Play(PipeReader audioData);
        public abstract void Pause();
        public abstract void Resume();
        public abstract void Stop();
        public abstract void FlushBuffers();
        public abstract ReadOnlySpan<short> GetCurrentBuffer();

        public abstract ValueTask DisposeAsync();
    }
}
