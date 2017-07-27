using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NitroSharp.Foundation.Audio
{
    public abstract class AudioSource : IDisposable
    {
        private readonly AudioEngine _engineBase;
        private readonly AudioBufferPool _bufferPool;
        protected readonly Dictionary<int, AudioBuffer> _buffers;

        private AudioStream _audioStream;
        private CancellationTokenSource _cts;
        private Task _playTask;
        private SemaphoreSlim _endOfStreamSignal;

        private volatile bool _isPlaying;

        public AudioSource(AudioEngine audioEngine, uint bufferSize)
        {
            _engineBase = audioEngine;
            _bufferPool = new AudioBufferPool(3, (int)bufferSize);
            _buffers = new Dictionary<int, AudioBuffer>();
            _endOfStreamSignal = new SemaphoreSlim(initialCount: 0, maxCount: 1);

            BufferEnd += OnBufferEnd;
            audioEngine.RegisterAudioSource(this);
        }

        private void OnBufferEnd(object sender, AudioBuffer buffer)
        {
            _bufferPool.Release(buffer);
            if (BuffersQueued == 0 && buffer.IsLastBuffer)
            {
                _endOfStreamSignal.Release();
            }
        }

        public bool IsPlaying
        {
            get => _isPlaying;
            set => _isPlaying = value;
        }

        public AudioStream CurrentStream => _audioStream;
        public abstract float Volume { get; set; }
        public abstract TimeSpan PlaybackPosition { get; }
        public abstract int BuffersQueued { get; }

        public event EventHandler<AudioBuffer> PreviewBufferSent;
        public abstract event EventHandler<AudioBuffer> BufferEnd;

        public void SetStream(AudioStream stream)
        {
            if (IsPlaying)
            {
                throw new InvalidOperationException();
            }

            if (stream == null)
            {
                _audioStream = null;
                return;
            }

            _audioStream = stream;
        }

        public Task Play()
        {
            if (IsPlaying)
            {
                return Task.FromResult(0);
            }

            if (CurrentStream == null)
            {
                throw new InvalidOperationException("Audio stream not set.");
            }

            _playTask = PlayCore();
            _playTask.ContinueWith(t => IsPlaying = false, TaskContinuationOptions.OnlyOnFaulted);
            return _playTask;
        }

        private async Task PlayCore()
        {
            IsPlaying = true;
            _cts = new CancellationTokenSource();

            if (_endOfStreamSignal.CurrentCount != 0)
            {
                _endOfStreamSignal.Wait();
            }

            await Task.Run(async () =>
            {
                StartAcceptingBuffers();
                while (!_cts.IsCancellationRequested)
                {
                    AudioBuffer buffer = await _bufferPool.TakeAsync(_cts.Token).ConfigureAwait(false);
                    buffer.ResetPosition();

                    bool reachedEof = !_audioStream.Read(buffer, _cts.Token);
                    if (buffer.Position > 0)
                    {
                        buffer.IsLastBuffer = reachedEof;
                        PreviewBufferSent?.Invoke(this, buffer);
                        AcceptBuffer(buffer);
                    }

                    if (reachedEof)
                    {
                        break;
                    }
                }

                _cts.Token.ThrowIfCancellationRequested();
            }).ConfigureAwait(false);

            await _endOfStreamSignal.WaitAsync(_cts.Token).ConfigureAwait(false);
            IsPlaying = false;
        }

        public void Pause() => PauseCore();
        public async Task StopAsync()
        {
            PauseCore();
            FlushBuffers();
            if (_playTask != null)
            {
                try
                {
                    await _playTask.ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                }
                finally
                {
                    _playTask = null;
                }
            }
        }

        public void Stop(bool wait = true)
        {
            PauseCore();
            FlushBuffers();

            if (!wait || (_playTask == null || _playTask.IsCompleted || _playTask.IsCanceled))
            {
                return;
            }

            try
            {
                _playTask.Wait();
            }
            catch (AggregateException e) when (e.InnerException is OperationCanceledException)
            {
            }
            finally
            {
                _playTask = null;
            }
        }

        private void PauseCore()
        {
            if (IsPlaying)
            {
                _cts?.Cancel();
                StopAcceptingBuffers();
                IsPlaying = false;
            }
        }

        internal abstract void StartAcceptingBuffers();
        internal abstract void StopAcceptingBuffers();
        internal abstract void FlushBuffers();

        internal virtual void AcceptBuffer(AudioBuffer buffer)
        {
            _buffers[buffer.Id] = buffer;
        }

        public abstract void Dispose();
    }
}
