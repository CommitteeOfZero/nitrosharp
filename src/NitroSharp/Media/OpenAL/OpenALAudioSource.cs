using System;
using Microsoft.VisualStudio.Threading;
using System.Runtime.InteropServices;
using System.Threading;
using System.IO.Pipelines;
using System.Buffers;
using System.Threading.Tasks;
using OpenAL;

namespace NitroSharp.Media.OpenAL
{
    internal class OpenALAudioSource : AudioSource
    {
        private readonly int _bufferSize;
        private readonly int _bufferPoolSize;

        private readonly OpenALAudioDevice _audioDevice;
        private readonly uint _source;
        private readonly uint[] _buffers;
        private readonly int _format;

        private IntPtr _buffer;
        private IntPtr[] _bufferData;
        private int _nextBuffer;
        private int _buffersLeft;
        private double _secondsElapsed;

        private Task? _consumeTask;
        private CancellationTokenSource _cts;
        private readonly AsyncManualResetEvent _flushBuffers;
        private readonly AsyncManualResetEvent _playSignal;

        public OpenALAudioSource(OpenALAudioDevice audioDevice, int bufferSize, int bufferCount)
        {
            _audioDevice = audioDevice;
            _bufferSize = bufferSize;
            _bufferPoolSize = bufferCount;
            _buffersLeft = bufferCount;

            AL10.alGenSources(1, out _source);
            _buffers = new uint[bufferCount];
            AL10.alGenBuffers(bufferCount, _buffers);
            _format = ALFormat(AudioDevice.BitDepth, _audioDevice.AudioParameters.ChannelCount);

            _bufferData = new IntPtr[bufferCount];
            _buffer = Marshal.AllocHGlobal(_bufferSize * _bufferPoolSize);
            for (int i = 0; i < _bufferPoolSize; i++)
            {
                _bufferData[i] = IntPtr.Add(_buffer, i * _bufferSize);
            }

            _flushBuffers = new();
            _playSignal = new();
        }

        public override bool IsPlaying
        {
            get
            {
                AL10.alGetSourcei(_source, AL10.AL_SOURCE_STATE, out int state);
                return state == AL10.AL_PLAYING;
            }
        }

        public override double SecondsElapsed { get => _secondsElapsed; }
        public override float Volume
        {
            get
            {
                AL10.alGetSourcef(_source, AL10.AL_GAIN, out float gain);
                return gain;
            }
            set => AL10.alSourcef(_source, AL10.AL_GAIN, value);
        }

        public override void Play(PipeReader audioData)
        {
            _ = PlayAsync(audioData);
        }

        private async Task PlayAsync(PipeReader audioData)
        {
            await StopAsync();
            _playSignal.Set();
            _cts = new CancellationTokenSource();
            _consumeTask = Task.Run(() => ConsumeLoop(audioData));
        }

        private async Task ConsumeLoop(PipeReader audioData)
        {
            Task flushEvent = _flushBuffers.WaitAsync();
            while (!_cts.IsCancellationRequested)
            {
                ReadResult readResult = await audioData.ReadAsync();
                long bytesRead = BufferData(audioData, readResult);
                double secondsRead = ((double)bytesRead / (AudioDevice.BitDepth / 8)) / _audioDevice.AudioParameters.SampleRate;
                int milliseconds = (int)(secondsRead * 100 + 1);
                _secondsElapsed += secondsRead;

                if (_playSignal.IsSet)
                {
                    if (!this.IsPlaying) AL10.alSourcePlay(_source);
                }
                else
                {
                    AL10.alSourcePause(_source);
                    try
                    {
                        _playSignal.WaitAsync().Wait(_cts.Token);
                    }
                    catch { }
                }

                if (flushEvent.Wait(milliseconds))
                {
                    AL10.alSourceStop(_source);
                    UnqueueBuffers();

                    _buffersLeft = _bufferPoolSize;
                    _nextBuffer = 0;

                    while (audioData.TryRead(out readResult))
                    {
                        audioData.AdvanceTo(readResult.Buffer.End);
                        if (readResult.IsCompleted) { break; }
                    }
                    _flushBuffers.Reset();
                    flushEvent = _flushBuffers.WaitAsync();
                }
            }
        }

        private int ALFormat(int bitDepth, int channels) => bitDepth switch
        {
            16 => channels > 1 ? AL10.AL_FORMAT_STEREO16 : AL10.AL_FORMAT_MONO16,
            8 => channels > 1 ? AL10.AL_FORMAT_STEREO8 : AL10.AL_FORMAT_MONO8,
            _ => -1,
        };

        private int UnqueueBuffers()
        {
            AL10.alGetSourcei(_source, AL10.AL_BUFFERS_PROCESSED, out int buffersProcessed);
            AL10.alSourceUnqueueBuffers(_source, buffersProcessed, new uint[buffersProcessed]);
            return buffersProcessed;
        }

        private unsafe long BufferData(PipeReader pipeReader, in ReadResult readResult)
        {
            ReadOnlySequence<byte> sequence = readResult.Buffer;
            _buffersLeft += UnqueueBuffers();

            int bytesRead = 0;
            while (!sequence.IsEmpty && _buffersLeft > 0)
            {
                int size = Math.Min((int)sequence.Length, _bufferSize);
                ReadOnlySequence<byte> src = sequence.Slice(0, size);
                sequence = sequence.Slice(size, sequence.Length - size);
                var dst = new Span<byte>(_bufferData[_nextBuffer].ToPointer(), size);
                src.CopyTo(dst);

                AL10.alBufferData(
                        _buffers[_nextBuffer],
                        _format,
                        _bufferData[_nextBuffer],
                        size,
                        (int)_audioDevice.AudioParameters.SampleRate
                );
                AL10.alSourceQueueBuffers(_source, 1, ref _buffers[_nextBuffer]);

                bytesRead += size;
                _nextBuffer = (_nextBuffer + 1) % _buffers.Length;
                _buffersLeft--;
            }

            pipeReader.AdvanceTo(sequence.Start);
            return bytesRead;
        }

        public override void Pause()
        {
            if (_consumeTask is not null)
            {
                _playSignal.Reset();
            }
        }

        public override void Resume()
        {
            if (_consumeTask is not null)
            {
                _playSignal.Set();
            }
        }

        public override void Stop()
        {
            _ = StopAsync();
        }

        private async Task StopAsync()
        {
            if (_consumeTask is not null)
            {
                _cts.Cancel();
                FlushBuffers();
                await _consumeTask;
                _consumeTask = null;
                _secondsElapsed = 0;
                AL10.alSourceStop(_source);
            }
        }

        public override void FlushBuffers()
        {
            if (_consumeTask is not null)
            {
                _flushBuffers.Set();
            }
        }

        public override unsafe ReadOnlySpan<short> GetCurrentBuffer()
        {
            int currentBuffer = (_nextBuffer + _buffersLeft) % _bufferPoolSize; // Is a lock needed here?
            if (!this.IsPlaying) return default;
            AL10.alGetBufferi(_buffers[currentBuffer], AL10.AL_SIZE, out int size);
            return new ReadOnlySpan<short>(_bufferData[currentBuffer].ToPointer(), size / 2);
        }

        public override async ValueTask DisposeAsync()
        {
            if (_consumeTask is not null)
            {
                await StopAsync();
            }

            if (_buffer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(_buffer);
                _buffer = IntPtr.Zero;

                AL10.alDeleteBuffers(_bufferPoolSize, _buffers);
                uint source = _source;
                AL10.alDeleteSources(1, ref source);
            }
        }
    }
}
