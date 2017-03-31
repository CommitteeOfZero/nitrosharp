using System;
using System.Collections.Generic;
using System.Threading;

namespace HoppyFramework.Audio
{
    public abstract class AudioSource
    {
        private readonly AudioEngine _engineBase;
        private readonly AudioBufferPool _bufferPool;
        protected readonly Dictionary<int, AudioBuffer> _buffers;

        private AudioStream _audioStream;
        private CancellationTokenSource _cts;

        public AudioSource(AudioEngine audioEngine)
        {
            _engineBase = audioEngine;
            _bufferPool = new AudioBufferPool(2, audioEngine.SampleRate * audioEngine.ChannelCount);
            _buffers = new Dictionary<int, AudioBuffer>();

            BufferEnd += OnBufferEnd;
        }

        private void OnBufferEnd(object sender, AudioBuffer buffer)
        {
            _bufferPool.Release(buffer);
        }

        public abstract float Volume { get; set; }
        public AudioStream CurrentStream => _audioStream;
        public abstract event EventHandler<AudioBuffer> BufferEnd;

        public void SetStream(AudioStream stream)
        {
            stream.TargetBitDepth = _engineBase.BitDepth;
            stream.TargetChannelCount = _engineBase.ChannelCount;
            stream.TargetSampleRate = _engineBase.SampleRate;

            stream.OnAttachedToSource();
            _audioStream = stream;
        }

        public async void Play()
        {
            _cts = new CancellationTokenSource();
            StartAcceptingBuffers();
            while (!_cts.IsCancellationRequested)
            {
                AudioBuffer buffer = await _bufferPool.TakeAsync(_cts.Token).ConfigureAwait(false);
                buffer.ResetPosition();
                if (_audioStream.Read(buffer) == false)
                {
                    break;
                }

                AcceptBuffer(buffer);
            }
        }

        public void Stop()
        {
            _cts.Cancel();

        }

        internal abstract void StartAcceptingBuffers();
        internal virtual void AcceptBuffer(AudioBuffer buffer)
        {
            _buffers[buffer.Id] = buffer;
        }
    }
}
