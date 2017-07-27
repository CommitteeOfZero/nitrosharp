using System;
using System.IO;
using System.Threading;

namespace NitroSharp.Foundation.Audio
{
    public abstract class AudioStream : IDisposable
    {
        protected AudioStream(Stream fileStream, int targetBitDepth, int targetSampleRate, int targetChannelCount)
        {
            if (fileStream == null)
            {
                throw new ArgumentNullException(nameof(fileStream));
            }

            if (!fileStream.CanRead)
            {
                throw new ArgumentException(nameof(fileStream), "The stream must be readable.");
            }

            if (!fileStream.CanSeek)
            {
                throw new ArgumentException(nameof(fileStream), "The stream must be seekable");
            }

            FileStream = fileStream;
            TargetBitDepth = targetBitDepth;
            TargetSampleRate = targetSampleRate;
            TargetChannelCount = targetChannelCount;
        }

        public Stream FileStream { get; }
        public int OriginalBitDepth { get; protected set; }
        public int OriginalSampleRate { get; protected set; }
        public int OriginalChannelCount { get; protected set; }

        public int TargetBitDepth { get; }
        public int TargetSampleRate { get; }
        public int TargetChannelCount { get; }

        public TimeSpan Duration { get; protected set; }
        public bool Looping { get; private set; }
        public TimeSpan LoopStart { get; private set; }
        public TimeSpan LoopEnd { get; private set; }

        public abstract void Seek(TimeSpan timeCode);
        public abstract bool Read(AudioBuffer buffer, CancellationToken cancellationToken);

        public void SetLoop()
        {
            SetLoop(TimeSpan.Zero, TimeSpan.MaxValue);
        }

        public virtual void SetLoop(TimeSpan loopStart, TimeSpan loopEnd)
        {
            Looping = true;
            LoopStart = loopStart;
            LoopEnd = loopEnd;
        }

        public virtual void Dispose()
        {
            FileStream.Dispose();
        }
    }
}
