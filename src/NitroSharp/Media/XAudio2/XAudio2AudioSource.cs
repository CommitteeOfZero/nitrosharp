using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;
using SharpDX.Multimedia;
using SharpDX.XAudio2;

namespace NitroSharp.Media.XAudio2
{
    internal sealed class XAudio2AudioSource : System.IAsyncDisposable
    {
        private readonly int _bufferSize;
        private readonly int _bufferPoolSize;

        private readonly XAudio2AudioDevice _device;
        private readonly SourceVoice _sourceVoice;
        private readonly AudioBuffer _xaudioBuffer = new();
        private PipeReader? _audioData;

        private IntPtr _buffer;
        private readonly IntPtr[] _buffers;
        private readonly int[] _actualBufferSizes;
        private int _nextBuffer;

        private readonly AsyncManualResetEvent _playSignal;
        private readonly AsyncManualResetEvent _bufferAvailable;
        private bool _flushBuffers;
        private Task? _consumeTask;
        private CancellationTokenSource _cts = new();
        private IntPtr _currentBuffer;

        public XAudio2AudioSource(XAudio2AudioDevice audioDevice, int bufferSize, int bufferCount)
        {
            _device = audioDevice;
            _bufferSize = bufferSize;
            _bufferPoolSize = bufferCount;
            _buffers = new IntPtr[_bufferPoolSize];
            _actualBufferSizes = new int[_bufferPoolSize];
            _playSignal = new AsyncManualResetEvent(initialState: false, allowInliningAwaiters: true);
            _bufferAvailable = new AsyncManualResetEvent(initialState: true, allowInliningAwaiters: false);
            AudioParameters parameters = audioDevice.AudioParameters;
            var waveFormat = new WaveFormat(
                (int)parameters.SampleRate,
                AudioDevice.BitDepth,
                parameters.ChannelCount
            );
            _sourceVoice = new SourceVoice(
                audioDevice.Device,
                waveFormat,
                VoiceFlags.NoSampleRateConversion
            );
            _sourceVoice.BufferStart += OnBufferStart;
            _sourceVoice.BufferEnd += OnBufferEnd;

            _buffer = Marshal.AllocHGlobal(_bufferSize * _bufferPoolSize);
            for (int i = 0; i < _bufferPoolSize; i++)
            {
                _buffers[i] = IntPtr.Add(_buffer, i * _bufferSize);
            }
        }

        public bool IsPlaying
            => _audioData is not null &&  _sourceVoice.State.BuffersQueued > 0;

        public double SecondsElapsed =>
            (double)_sourceVoice.State.SamplesPlayed / _device.AudioParameters.SampleRate;

        public unsafe ReadOnlySpan<short> GetCurrentBuffer()
        {
            if (_currentBuffer == IntPtr.Zero) { return default; }
            int index = (int)((_currentBuffer.ToInt64() - _buffer.ToInt64()) / _bufferSize);
            int size = _actualBufferSizes[index];
            return new ReadOnlySpan<short>(_currentBuffer.ToPointer(), size / 2);
        }

        public float Volume
        {
            get
            {
                _sourceVoice.GetVolume(out float volume);
                return volume;
            }
            set => _sourceVoice.SetVolume(value);
        }

        public void Play(PipeReader audioData)
        {
            _ = PlayAsync(audioData);
        }

        private async Task PlayAsync(PipeReader audioData)
        {
            await StopAsync();
            _audioData = audioData;
            _sourceVoice.Start();
            _playSignal.Set();
            _cts = new CancellationTokenSource();
            _consumeTask = Task.Run(() => ConsumeLoop(_audioData));
        }

        public void Pause()
        {
            _sourceVoice.Stop();
            _playSignal.Reset();
        }

        public void Resume()
        {
            if (_audioData is not null)
            {
                _sourceVoice.Start();
                _playSignal.Set();
            }
        }

        public void Stop()
        {
            _ = StopAsync();
        }

        public async Task StopAsync()
        {
            if (_audioData is not null)
            {
                _sourceVoice.Stop();
                _cts.Cancel();
                FlushBuffers();
                _audioData = null;
                _currentBuffer = IntPtr.Zero;
                if (_consumeTask is not null)
                {
                    await _consumeTask;
                    _consumeTask = null;
                }
            }
        }

        private async Task ConsumeLoop(PipeReader audioData)
        {
            while (!_cts.IsCancellationRequested || _flushBuffers)
            {
                if (_flushBuffers)
                {
                    _flushBuffers = false;
                    _sourceVoice.FlushSourceBuffers();
                    _nextBuffer = 0;
                    while (audioData.TryRead(out ReadResult readResult))
                    {
                        audioData.AdvanceTo(readResult.Buffer.End);
                        if (readResult.IsCompleted) { break; }
                    }
                    if (_cts.IsCancellationRequested)
                    {
                        return;
                    }
                }
                // TODO: ??? await _playSignal.WaitAsync();
                while (!_cts.IsCancellationRequested)
                {
                    await _bufferAvailable.WaitAsync();
                    if (_flushBuffers)
                    {
                        break;
                    }
                    ReadResult readResult = await audioData.ReadAsync();
                    BufferData(audioData, readResult);
                    _bufferAvailable.Reset();
                }
            }
        }

        public void FlushBuffers()
        {
            if (_audioData is not null)
            {
                _flushBuffers = true;
                _bufferAvailable.Set();
            }
        }

        private unsafe void BufferData(PipeReader pipeReader, in ReadResult readResult)
        {
            ReadOnlySequence<byte> sequence = readResult.Buffer;
            while (!sequence.IsEmpty && _sourceVoice.State.BuffersQueued < _bufferPoolSize)
            {
                int size = Math.Min((int)sequence.Length, _bufferSize);
                ReadOnlySequence<byte> src = sequence.Slice(0, size);
                sequence = sequence.Slice(size, sequence.Length - size);
                IntPtr internalBuffer = _buffers[_nextBuffer];
                var dst = new Span<byte>(internalBuffer.ToPointer(), size);
                src.CopyTo(dst);

                _actualBufferSizes[_nextBuffer] = size;
                _nextBuffer = (_nextBuffer + 1) % _bufferPoolSize;
                SubmitBuffer(internalBuffer, size);
            }

            pipeReader.AdvanceTo(sequence.Start);
        }

        private void SubmitBuffer(IntPtr data, int size)
        {
            _xaudioBuffer.Context = data;
            _xaudioBuffer.AudioDataPointer = data;
            _xaudioBuffer.AudioBytes = size;
            _xaudioBuffer.Flags = BufferFlags.None;
            _sourceVoice.SubmitSourceBuffer(_xaudioBuffer, null);
        }

        private void OnBufferStart(IntPtr pointer)
        {
            _currentBuffer = pointer;
        }

        private void OnBufferEnd(IntPtr pointer)
        {
            _currentBuffer = IntPtr.Zero;
            _bufferAvailable.Set();
        }

        public async ValueTask DisposeAsync()
        {
            if (_consumeTask is not null)
            {
                await StopAsync();
            }
            if (!_sourceVoice.IsDisposed)
            {
                _sourceVoice.DestroyVoice();
                _sourceVoice.Dispose();
            }
            if (_buffer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(_buffer);
                _buffer = IntPtr.Zero;
            }
        }
    }
}
