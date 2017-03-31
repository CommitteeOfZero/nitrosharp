using System;
using System.IO;

namespace HoppyFramework.Audio
{
    public abstract class AudioStream : IDisposable
    {
        protected AudioStream(Stream fileStream)
        {
            FileStream = fileStream;
        }

        public Stream FileStream { get; }
        public int OriginalBitDepth { get; protected set; }
        public int OriginalSampleRate { get; protected set; }
        public int OriginalChannelCount { get; protected set; }

        public int TargetBitDepth { get; set; }
        public int TargetSampleRate { get; set; }
        public int TargetChannelCount { get; set; }

        public TimeSpan Duration { get; protected set; }
        //public abstract TimeSpan Position { get; }

        public bool Looping { get; private set; }
        public TimeSpan LoopStart { get; private set; }
        public TimeSpan LoopEnd { get; private set; }

        public abstract void Seek(TimeSpan timeCode);
        public abstract bool Read(AudioBuffer buffer);

        public void SetLoop()
        {
            SetLoop(TimeSpan.Zero, Duration);
        }

        public virtual void SetLoop(TimeSpan loopStart, TimeSpan loopEnd)
        {
            Looping = true;
            LoopStart = loopStart;
            LoopEnd = loopEnd;
        }

        internal virtual void OnAttachedToSource()
        {
        }

        public virtual void Dispose()
        {
            FileStream.Dispose();
        }
    }
}
