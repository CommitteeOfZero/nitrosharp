using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MoeGame.Framework.Audio
{
    public abstract class AudioSource
    {
        private readonly AudioEngine _engineBase;
        private readonly AudioBufferPool _bufferPool;
        protected readonly Dictionary<int, AudioBuffer> _buffers;

        private AudioStream _audioStream;
        private CancellationTokenSource _cts;

        public AudioSource(AudioEngine audioEngine, uint bufferSize)
        {
            _engineBase = audioEngine;
            _bufferPool = new AudioBufferPool(2, (int)bufferSize);
            _buffers = new Dictionary<int, AudioBuffer>();

            Status = AudioSourceStatus.Idle;
            BufferEnd += OnBufferEnd;
        }

        private void OnBufferEnd(object sender, AudioBuffer buffer)
        {
            _bufferPool.Release(buffer);
        }

        public AudioSourceStatus Status { get; private set; }
        public AudioStream CurrentStream => _audioStream;
        public abstract float Volume { get; set; }

        public event EventHandler<AudioBuffer> PreviewBufferSent;
        public abstract event EventHandler<AudioBuffer> BufferEnd;

        public void SetStream(AudioStream stream)
        {
            if (Status == AudioSourceStatus.Playing)
            {
                throw new InvalidOperationException();
            }

            stream.TargetBitDepth = _engineBase.BitDepth;
            stream.TargetChannelCount = _engineBase.ChannelCount;
            stream.TargetSampleRate = _engineBase.SampleRate;

            stream.OnAttachedToSource();
            _audioStream = stream;
        }

        public async void Play()
        {
            if (Status == AudioSourceStatus.Playing)
            {
                return;
            }
            if (CurrentStream == null)
            {
                throw new InvalidOperationException("Audio stream not set.");
            }

            _cts = new CancellationTokenSource();
            StartAcceptingBuffers();
            Status = AudioSourceStatus.Playing;
            while (!_cts.IsCancellationRequested)
            {
                AudioBuffer buffer = null;
                try
                {
                    buffer = await _bufferPool.TakeAsync(_cts.Token).ConfigureAwait(false);
                }
                catch (TaskCanceledException)
                {
                    return;
                }

                buffer.ResetPosition();
                bool eof = !_audioStream.Read(buffer);

                PreviewBufferSent?.Invoke(this, buffer);

                AcceptBuffer(buffer);
                if (eof)
                {
                    break;
                }
            }
        }

        public void Pause() => PauseCore();
        public void Stop()
        {
            PauseCore();
            FlushBuffers();
        }

        private void PauseCore()
        {
            if (Status == AudioSourceStatus.Playing)
            {
                StopAcceptingBuffers();
                _cts?.Cancel();
                Status = AudioSourceStatus.Idle;
            }
        }

        internal abstract void StartAcceptingBuffers();
        internal abstract void StopAcceptingBuffers();
        internal abstract void FlushBuffers();

        internal virtual void AcceptBuffer(AudioBuffer buffer)
        {
            _buffers[buffer.Id] = buffer;
        }
    }
}
